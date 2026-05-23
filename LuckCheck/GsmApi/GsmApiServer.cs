using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Newtonsoft.Json;

namespace LuckCheck.GsmApi
{
    // ──────────────────── DTOs ────────────────────

    public class SimDto
    {
        [JsonProperty("sim_id")]          public string SimId          { get; set; }
        [JsonProperty("gateway_id")]      public string GatewayId      { get; set; }
        [JsonProperty("slot_index")]      public int    SlotIndex      { get; set; }
        [JsonProperty("msisdn")]          public string Msisdn         { get; set; }
        [JsonProperty("status")]          public string Status         { get; set; }  // available|busy|offline|disabled
        [JsonProperty("network")]         public string Network        { get; set; }
        [JsonProperty("signal_strength")] public int    SignalStrength { get; set; }
        [JsonProperty("is_disabled")]     public bool   IsDisabled     { get; set; }
        [JsonProperty("last_activity")]   public string LastActivity   { get; set; }
    }

    public class SimReserveRequest
    {
        [JsonProperty("sim_id")]         public string SimId         { get; set; }
        [JsonProperty("gateway_id")]     public string GatewayId     { get; set; }
        [JsonProperty("slot_index")]     public int    SlotIndex     { get; set; }
        [JsonProperty("ttl_secs")]       public int    TtlSecs       { get; set; } = 120;
        [JsonProperty("reservation_id")] public string ReservationId { get; set; }  // idempotency key
    }

    public class SimReserveResponse
    {
        [JsonProperty("reservation_id")] public string ReservationId { get; set; }
        [JsonProperty("sim_id")]         public string SimId         { get; set; }
        [JsonProperty("expires_at")]     public string ExpiresAt     { get; set; }
        [JsonProperty("is_idempotent")]  public bool   IsIdempotent  { get; set; }
    }

    public class SimReleaseRequest
    {
        [JsonProperty("reservation_id")] public string ReservationId { get; set; }
        // Legacy backward compat fields
        [JsonProperty("sim_id")]         public string SimId         { get; set; }
        [JsonProperty("gateway_id")]     public string GatewayId     { get; set; }
        [JsonProperty("ussd_session")]   public string UssdSession   { get; set; }
    }

    public class SimDisableRequest
    {
        [JsonProperty("reason")] public string Reason { get; set; }
    }

    public class SimStatusDto
    {
        [JsonProperty("sim_id")]          public string SimId          { get; set; }
        [JsonProperty("status")]          public string Status         { get; set; }
        [JsonProperty("signal_strength")] public int    SignalStrength { get; set; }
        [JsonProperty("is_disabled")]     public bool   IsDisabled     { get; set; }
        [JsonProperty("last_activity")]   public string LastActivity   { get; set; }
    }

    public class GatewayHealthResponse
    {
        [JsonProperty("gateway_id")]   public string             GatewayId   { get; set; }
        [JsonProperty("online")]       public bool               Online      { get; set; }
        [JsonProperty("sim_statuses")] public List<SimStatusDto> SimStatuses { get; set; }
    }

    public class UssdSendRequest
    {
        [JsonProperty("sim_id")]        public string SimId        { get; set; }
        [JsonProperty("msisdn")]        public string Msisdn       { get; set; }
        [JsonProperty("gateway_id")]    public string GatewayId    { get; set; }
        [JsonProperty("slot_index")]    public int    SlotIndex    { get; set; }
        [JsonProperty("phone_number")]  public string PhoneNumber  { get; set; }
        [JsonProperty("timeout_secs")]  public int    TimeoutSecs  { get; set; } = 60;
        [JsonProperty("transaction_id")]public string TransactionId{ get; set; }  // idempotency key
        [JsonProperty("reservation_id")]public string ReservationId{ get; set; }  // optional linked reservation

        // ── Encrypted PIN path (production) ──
        [JsonProperty("ussd_template")] public string UssdTemplate { get; set; }  // e.g. "*103*{PIN}#"
        [JsonProperty("pin_encrypted")] public string PinEncrypted { get; set; }  // Base64(IV|CT|MAC)
        [JsonProperty("enc_version")]   public string EncVersion   { get; set; }  // "aes256cbc-hmac-v1"

        // ── Legacy plain-text path (dev mode only) ──
        [JsonProperty("ussd_code")]     public string UssdCode     { get; set; }
    }

    public class UssdSendResponse
    {
        [JsonProperty("session_id")]    public string SessionId    { get; set; }
        [JsonProperty("status")]        public string Status       { get; set; }
        [JsonProperty("submitted_at")]  public string SubmittedAt  { get; set; }
        [JsonProperty("is_idempotent")] public bool   IsIdempotent { get; set; }
    }

    public class UssdSessionDto
    {
        [JsonProperty("session_id")]     public string SessionId     { get; set; }
        [JsonProperty("transaction_id")] public string TransactionId { get; set; }
        [JsonProperty("sim_id")]         public string SimId         { get; set; }
        [JsonProperty("gateway_id")]     public string GatewayId     { get; set; }
        [JsonProperty("status")]         public string Status        { get; set; }
        [JsonProperty("result_message")] public string ResultMessage { get; set; }
        [JsonProperty("submitted_at")]   public string SubmittedAt   { get; set; }
        [JsonProperty("completed_at")]   public string CompletedAt   { get; set; }
        [JsonProperty("target_phone")]   public string TargetPhone   { get; set; }
    }

    // ── Debug-only DTOs (locked in production) ──
    internal class DebugEncryptPinRequest
    {
        [JsonProperty("pin")]           public string Pin          { get; set; }
        [JsonProperty("master_key")]    public string MasterKey    { get; set; }
        [JsonProperty("ussd_template")] public string UssdTemplate { get; set; }
    }

    // ──────────────────── Controller Interface ────────────────────

    public interface IGsmController
    {
        string            GatewayId { get; }
        PrometheusMetrics Metrics   { get; }

        // SIM management
        List<SimDto>       HandleListSims    ();
        SimDto             HandleGetSim      (string simId);
        SimReserveResponse HandleSimReserve  (SimReserveRequest  req);
        bool               HandleSimRelease  (SimReleaseRequest  req);
        void               HandleSimDisable  (string simId, string reason);
        bool               HandleSimEnable   (string simId);

        // USSD
        UssdSendResponse  HandleUssdSend      (UssdSendRequest req);
        UssdSessionDto    HandleGetSession    (string sessionId);
        bool              HandleCancelSession (string sessionId);

        // Gateway
        GatewayHealthResponse HandleGatewayHealth (string gatewayId);
        bool                  HandleGatewayReboot  (string gatewayId);
    }

    // ──────────────────── HTTP Server ────────────────────

    public class GsmApiServer
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly HttpListener   _listener;
        private readonly IGsmController _controller;
        private readonly string         _sharedSecret;

        /// <param name="prefix">HttpListener prefix, e.g. "http://localhost:8000/"</param>
        /// <param name="sharedSecret">HMAC secret for X-Signature. Empty = auth disabled (dev only).</param>
        public GsmApiServer(string prefix, IGsmController controller, string sharedSecret = null)
        {
            _listener     = new HttpListener();
            _listener.Prefixes.Add(prefix);
            _controller   = controller;
            _sharedSecret = sharedSecret;

            if (string.IsNullOrEmpty(_sharedSecret))
                logger.Warn("GsmApiServer: ApiSharedSecret not set — request auth DISABLED. Dev/localhost only.");
        }

        public void Start()
        {
            _listener.Start();
            new Thread(Listen) { IsBackground = true, Name = "GsmApiListener" }.Start();
            logger.Info($"GsmApiServer listening on {string.Join(", ", _listener.Prefixes)}" +
                        $" [auth={(string.IsNullOrEmpty(_sharedSecret) ? "OFF" : "ON")}]");
        }

        public void Stop()
        {
            try { _listener.Stop(); } catch { }
        }

        // ──────────────────── Request loop ────────────────────

        private void Listen()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
                }
                catch (HttpListenerException ex) when (ex.ErrorCode == 995) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception ex) { logger.Error($"GsmApi Listen: {ex.Message}"); }
            }
        }

        private void Handle(HttpListenerContext ctx)
        {
            var rq = ctx.Request;
            var rs = ctx.Response;
            rs.ContentType = "application/json; charset=utf-8";

            try
            {
                // Read body as bytes first (needed for HMAC verification)
                byte[] bodyBytes = Array.Empty<byte>();
                if (rq.HasEntityBody)
                {
                    using (var ms = new MemoryStream())
                    {
                        rq.InputStream.CopyTo(ms);
                        bodyBytes = ms.ToArray();
                    }
                }

                string path   = rq.Url.AbsolutePath.TrimEnd('/').ToLowerInvariant();
                string method = rq.HttpMethod.ToUpperInvariant();

                // Auth on all POST/DELETE — skip GET health + metrics (no sensitive data)
                if (!string.IsNullOrEmpty(_sharedSecret) && method == "POST")
                {
                    if (!RequestAuth.Verify(rq, bodyBytes, _sharedSecret, out string authErr))
                    {
                        logger.Warn($"[GsmApi] Auth FAILED from {rq.RemoteEndPoint}: {authErr}");
                        WriteJson(rs, 401, new { error = "Unauthorized", detail = authErr });
                        return;
                    }
                }

                string body = bodyBytes.Length > 0 ? Encoding.UTF8.GetString(bodyBytes) : "";
                logger.Debug($"[GsmApi] {method} {path}");

                // ── GET /swagger  (Swagger UI HTML) ──
                if (method == "GET" && (path == "/swagger" || path == "/swagger/index.html"))
                {
                    WriteHtml(rs, 200, ApiDocs.GetSwaggerHtml());
                    return;
                }

                // ── GET /api-docs  (OpenAPI 3.0 JSON spec) ──
                if (method == "GET" && (path == "/api-docs" || path == "/swagger.json" || path == "/openapi.json"))
                {
                    string host = rq.Url.GetLeftPart(UriPartial.Authority);
                    WriteText(rs, 200, ApiDocs.GetOpenApiJson(host), "application/json; charset=utf-8");
                    return;
                }

                // ── GET /metrics ──
                if (method == "GET" && path == "/metrics")
                {
                    WriteText(rs, 200, _controller.Metrics?.Render() ?? "");
                    return;
                }

                // ── GET /api/v1/sims  (exact — must precede /{id} regex) ──
                if (method == "GET" && path == "/api/v1/sims")
                {
                    WriteJson(rs, 200, _controller.HandleListSims());
                    return;
                }

                // ── POST /api/v1/sims/reserve  (exact — must precede /{id} regex) ──
                if (method == "POST" && path == "/api/v1/sims/reserve")
                {
                    var req = JsonConvert.DeserializeObject<SimReserveRequest>(body) ?? new SimReserveRequest();
                    try
                    {
                        var result = _controller.HandleSimReserve(req);
                        WriteJson(rs, result.IsIdempotent ? 200 : 201, result);
                    }
                    catch (InvalidOperationException ex)
                    {
                        string msg = ex.Message;
                        int code = msg.Contains("not found") ? 404
                                 : (msg.Contains("bận") || msg.Contains("busy") || msg.Contains("disabled") || msg.Contains("vô hiệu")) ? 409
                                 : 400;
                        WriteJson(rs, code, new { error = msg });
                    }
                    return;
                }

                // ── POST /api/v1/sims/release  (exact) ──
                if (method == "POST" && path == "/api/v1/sims/release")
                {
                    var req = JsonConvert.DeserializeObject<SimReleaseRequest>(body) ?? new SimReleaseRequest();
                    bool ok = _controller.HandleSimRelease(req);
                    WriteJson(rs, ok ? 200 : 404, ok ? (object)new { } : new { error = "Reservation not found" });
                    return;
                }

                // ── POST /api/v1/sims/{id}/disable ──
                var mDisable = Regex.Match(path, @"^/api/v1/sims/([^/]+)/disable$");
                if (method == "POST" && mDisable.Success)
                {
                    var req = string.IsNullOrEmpty(body) ? new SimDisableRequest()
                            : JsonConvert.DeserializeObject<SimDisableRequest>(body) ?? new SimDisableRequest();
                    _controller.HandleSimDisable(mDisable.Groups[1].Value, req.Reason);
                    WriteJson(rs, 200, new { });
                    return;
                }

                // ── POST /api/v1/sims/{id}/enable ──
                var mEnable = Regex.Match(path, @"^/api/v1/sims/([^/]+)/enable$");
                if (method == "POST" && mEnable.Success)
                {
                    bool ok = _controller.HandleSimEnable(mEnable.Groups[1].Value);
                    WriteJson(rs, ok ? 200 : 404, ok ? (object)new { } : new { error = "SIM not found or not disabled" });
                    return;
                }

                // ── GET /api/v1/sims/{id} ──
                var mSimId = Regex.Match(path, @"^/api/v1/sims/([^/]+)$");
                if (method == "GET" && mSimId.Success)
                {
                    var sim = _controller.HandleGetSim(mSimId.Groups[1].Value);
                    if (sim == null) WriteJson(rs, 404, new { error = "SIM not found" });
                    else             WriteJson(rs, 200, sim);
                    return;
                }

                // ── POST /api/v1/ussd/send ──
                if (method == "POST" && path == "/api/v1/ussd/send")
                {
                    var req = JsonConvert.DeserializeObject<UssdSendRequest>(body);
                    if (req == null) { WriteJson(rs, 400, new { error = "Empty request body" }); return; }
                    try
                    {
                        var result = _controller.HandleUssdSend(req);
                        WriteJson(rs, result.IsIdempotent ? 200 : 202, result);
                    }
                    catch (InvalidOperationException ex)
                    {
                        string msg = ex.Message;
                        int code = msg.Contains("not found") ? 404
                                 : (msg.Contains("bận") || msg.Contains("busy") || msg.Contains("disabled")) ? 409
                                 : 400;
                        WriteJson(rs, code, new { error = msg });
                    }
                    return;
                }

                // ── POST /api/v1/ussd/session/{id}/cancel ──
                var mCancel = Regex.Match(path, @"^/api/v1/ussd/session/([^/]+)/cancel$");
                if (method == "POST" && mCancel.Success)
                {
                    bool ok = _controller.HandleCancelSession(mCancel.Groups[1].Value);
                    WriteJson(rs, ok ? 200 : 404, ok ? (object)new { } : new { error = "Session not found or already completed" });
                    return;
                }

                // ── GET /api/v1/ussd/session/{id} ──
                var mSessId = Regex.Match(path, @"^/api/v1/ussd/session/([^/]+)$");
                if (method == "GET" && mSessId.Success)
                {
                    var sess = _controller.HandleGetSession(mSessId.Groups[1].Value);
                    if (sess == null) WriteJson(rs, 404, new { error = "Session not found" });
                    else              WriteJson(rs, 200, sess);
                    return;
                }

                // ── GET /api/v1/gateways/health  (shorthand, no {id} needed) ──
                if (method == "GET" && path == "/api/v1/gateways/health")
                {
                    WriteJson(rs, 200, _controller.HandleGatewayHealth(_controller.GatewayId));
                    return;
                }

                // ── POST /api/v1/gateways/{id}/reboot ──
                var mReboot = Regex.Match(path, @"^/api/v1/gateways/([^/]+)/reboot$");
                if (method == "POST" && mReboot.Success)
                {
                    bool ok = _controller.HandleGatewayReboot(mReboot.Groups[1].Value);
                    WriteJson(rs, ok ? 202 : 404, ok ? (object)new { queued = true } : new { error = "Gateway not found" });
                    return;
                }

                // ── GET /api/v1/gateways/{id}/health ──
                var mHealth = Regex.Match(path, @"^/api/v1/gateways/([^/]+)/health$");
                if (method == "GET" && mHealth.Success)
                {
                    WriteJson(rs, 200, _controller.HandleGatewayHealth(mHealth.Groups[1].Value));
                    return;
                }

                // ── Debug endpoints — locked when ApiSharedSecret is set ──

                if (method == "POST" && path == "/api/v1/debug/encrypt-pin")
                {
                    if (!string.IsNullOrEmpty(_sharedSecret))
                    {
                        WriteJson(rs, 403, new { error = "Debug endpoint locked in production" });
                        return;
                    }
                    var dbgReq = JsonConvert.DeserializeObject<DebugEncryptPinRequest>(body);
                    if (string.IsNullOrEmpty(dbgReq?.Pin))    { WriteJson(rs, 400, new { error = "Missing 'pin'" }); return; }
                    if (string.IsNullOrEmpty(dbgReq.MasterKey)){ WriteJson(rs, 400, new { error = "Missing 'master_key'" }); return; }
                    string encrypted = PinCrypto.Encrypt(dbgReq.Pin, dbgReq.MasterKey);
                    WriteJson(rs, 200, new
                    {
                        pin_encrypted = encrypted,
                        ussd_template = dbgReq.UssdTemplate ?? "*103*{PIN}#",
                        enc_version   = "aes256cbc-hmac-v1",
                        note          = "Copy pin_encrypted into ussd/send. DO NOT use in production."
                    });
                    return;
                }

                if (method == "GET" && path == "/api/v1/debug/generate-key")
                {
                    if (!string.IsNullOrEmpty(_sharedSecret))
                    {
                        WriteJson(rs, 403, new { error = "Debug endpoint locked in production" });
                        return;
                    }
                    WriteJson(rs, 200, new
                    {
                        master_key = PinCrypto.GenerateMasterKey(),
                        note       = "Set this in App.config[PinMasterKey] AND Go BE env[PIN_MASTER_KEY]."
                    });
                    return;
                }

                WriteJson(rs, 404, new { error = "not found" });
            }
            catch (Exception ex)
            {
                logger.Error($"[GsmApi] Handle error: {ex.Message}");
                try { WriteJson(rs, 500, new { error = ex.Message }); } catch { }
            }
            finally
            {
                try { rs.Close(); } catch { }
            }
        }

        private static void WriteJson(HttpListenerResponse rs, int status, object obj)
        {
            rs.StatusCode = status;
            byte[] buf = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
            rs.ContentLength64 = buf.Length;
            rs.OutputStream.Write(buf, 0, buf.Length);
        }

        private static void WriteText(HttpListenerResponse rs, int status, string text,
            string contentType = "text/plain; version=0.0.4; charset=utf-8")
        {
            rs.StatusCode  = status;
            rs.ContentType = contentType;
            byte[] buf = Encoding.UTF8.GetBytes(text);
            rs.ContentLength64 = buf.Length;
            rs.OutputStream.Write(buf, 0, buf.Length);
        }

        private static void WriteHtml(HttpListenerResponse rs, int status, string html)
        {
            rs.StatusCode  = status;
            rs.ContentType = "text/html; charset=utf-8";
            byte[] buf = Encoding.UTF8.GetBytes(html);
            rs.ContentLength64 = buf.Length;
            rs.OutputStream.Write(buf, 0, buf.Length);
        }
    }
}
