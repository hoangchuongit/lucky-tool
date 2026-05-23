// Partial class — IGsmController implementation + USSD state machine + background workers.
// GSMForm.cs contains serial-port logic; this file handles HTTP API + persistence.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using LuckCheck.GsmApi;
using LuckCheck.Model;
using LuckCheck.Utils;
using Newtonsoft.Json;

namespace LuckCheck
{
    public partial class GSMForm : IGsmController
    {
        // ──────────────────── Fields ────────────────────

        // sessionId → session (in-memory live state)
        private readonly ConcurrentDictionary<string, UssdSession> _ussdSessions
            = new ConcurrentDictionary<string, UssdSession>();

        // portName → sessionId (only present while session is active)
        private readonly ConcurrentDictionary<string, string> _portToSession
            = new ConcurrentDictionary<string, string>();

        // portName → USSD conversation log (max 100 entries)
        private readonly ConcurrentDictionary<string, List<string>> _ussdLogs
            = new ConcurrentDictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private const int UssdLogMaxEntries = 100;

        private GsmApiServer      _apiServer;
        private GsmStore          _store;
        private CancellationTokenSource _workerCts;

        private static readonly HttpClient   _webhookClient = new HttpClient();
        private static readonly string       AuditDir       = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "logs", "audit");
        private static readonly object       _auditFileLock = new object();

        // Per-port mutex: prevents TOCTOU double-reservation / double-send on the same SIM
        private readonly ConcurrentDictionary<string, object> _portBusyLocks
            = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // IGsmController.Metrics — exposes Prometheus counters/gauges to GsmApiServer
        public PrometheusMetrics Metrics { get; private set; }

        // ──────────────────── API Server lifecycle ────────────────────

        internal void StartApiServer()
        {
            try
            {
                int    port         = int.TryParse(ConfigurationManager.AppSettings["GsmApiPort"], out int p) ? p : 8000;
                string sharedSecret = ConfigurationManager.AppSettings["ApiSharedSecret"] ?? "";
                string bindHost     = ConfigurationManager.AppSettings["GsmApiBindHost"] ?? "localhost";

                _store   = new GsmStore();
                Metrics  = new PrometheusMetrics();
                _workerCts = new CancellationTokenSource();

                RecoverIncompleteSessions();

                _apiServer = new GsmApiServer($"http://{bindHost}:{port}/", this, sharedSecret);
                _apiServer.Start();

                // Background workers (IsBackground = killed automatically on process exit)
                var cts = _workerCts;
                new Thread(() => RunTtlExpiryWorker(cts.Token))
                    { IsBackground = true, Name = "GsmTtlExpiry" }.Start();
                Task.Run(() => RunWebhookOutboxWorkerAsync(cts.Token));
            }
            catch (Exception ex)
            {
                logger.Error($"GsmApiServer start failed: {ex.Message}");
            }
        }

        internal void StopApiServer()
        {
            _workerCts?.Cancel();
            _apiServer?.Stop();
            FireGatewayOfflineWebhookDirect();
            _store?.Dispose();
        }

        // ──────────────────── IGsmController ────────────────────

        public string GatewayId => ConfigurationManager.AppSettings["GatewayId"] ?? "gw-001";

        // ── SIM management ──

        public List<SimDto> HandleListSims()
        {
            var disabledSet = new HashSet<string>(_store?.GetAllDisabledSimIds() ?? new List<string>());
            var result = new List<SimDto>();

            foreach (var dto in _comDtoMap.Values)
            {
                string simId = !string.IsNullOrEmpty(dto.ICCID) ? dto.ICCID : $"slot-{dto.STT}";
                bool isDisabled = dto.IsDisabled || disabledSet.Contains(simId);

                string status = isDisabled                            ? "disabled"
                              : dto.IsBusy                           ? "busy"
                              : string.IsNullOrEmpty(dto.PhoneNumber) ? "offline"
                              : "available";

                result.Add(new SimDto
                {
                    SimId          = simId,
                    GatewayId      = GatewayId,
                    SlotIndex      = int.TryParse(dto.STT, out int stt) ? stt : 0,
                    Msisdn         = dto.PhoneNumber,
                    Status         = status,
                    Network        = dto.Network,
                    SignalStrength = dto.SignalStrength,
                    IsDisabled     = isDisabled,
                    LastActivity   = dto.LastUssdAt?.ToString("o") ?? ""
                });
            }

            // Snapshot gauges for /metrics
            if (Metrics != null)
            {
                Metrics.SimsAvailable = result.Count(s => s.Status == "available");
                Metrics.SimsBusy      = result.Count(s => s.Status == "busy");
                Metrics.SimsOffline   = result.Count(s => s.Status == "offline");
                Metrics.SimsDisabled  = result.Count(s => s.Status == "disabled");
            }
            return result;
        }

        public SimDto HandleGetSim(string simId)
        {
            foreach (var dto in _comDtoMap.Values)
            {
                string id = !string.IsNullOrEmpty(dto.ICCID) ? dto.ICCID : $"slot-{dto.STT}";
                if (!string.Equals(id, simId, StringComparison.OrdinalIgnoreCase)) continue;

                bool isDisabled = dto.IsDisabled || (_store?.IsSimDisabled(simId) ?? false);
                string status = isDisabled                            ? "disabled"
                              : dto.IsBusy                           ? "busy"
                              : string.IsNullOrEmpty(dto.PhoneNumber) ? "offline"
                              : "available";

                return new SimDto
                {
                    SimId          = id,
                    GatewayId      = GatewayId,
                    SlotIndex      = int.TryParse(dto.STT, out int stt) ? stt : 0,
                    Msisdn         = dto.PhoneNumber,
                    Status         = status,
                    Network        = dto.Network,
                    SignalStrength = dto.SignalStrength,
                    IsDisabled     = isDisabled,
                    LastActivity   = dto.LastUssdAt?.ToString("o") ?? ""
                };
            }
            return null;
        }

        public SimReserveResponse HandleSimReserve(SimReserveRequest req)
        {
            // Idempotency: if reservation_id already in store and still valid, return it
            if (!string.IsNullOrEmpty(req.ReservationId) && _store != null)
            {
                var existing = _store.GetReservationById(req.ReservationId);
                if (existing != null && existing.ExpiresAt > DateTime.UtcNow)
                    return new SimReserveResponse
                    {
                        ReservationId = existing.ReservationId,
                        SimId         = existing.SimId,
                        ExpiresAt     = existing.ExpiresAt.ToString("o"),
                        IsIdempotent  = true
                    };
            }

            var comDto = FindSimByRequest(req.SimId, req.GatewayId, req.SlotIndex);
            if (comDto == null)
                throw new InvalidOperationException($"SIM not found: sim_id={req.SimId}, slot={req.SlotIndex}");

            string simId = !string.IsNullOrEmpty(comDto.ICCID) ? comDto.ICCID : $"slot-{comDto.STT}";

            int ttlSecs = req.TtlSecs > 0 ? req.TtlSecs : 120;
            string reservationId = !string.IsNullOrEmpty(req.ReservationId)
                ? req.ReservationId
                : "res-" + Guid.NewGuid().ToString("N").Substring(0, 12);

            // Atomic check-and-mark prevents two concurrent requests from reserving the same SIM
            lock (GetPortBusyLock(comDto.COM))
            {
                if (comDto.IsDisabled || (_store?.IsSimDisabled(simId) ?? false))
                    throw new InvalidOperationException("SIM đã bị vô hiệu hóa (disabled)");
                if (comDto.IsBusy)
                    throw new InvalidOperationException("SIM đang bận, thử lại sau");
                comDto.IsBusy = true;
            }

            var record = new SimReservationRecord
            {
                ReservationId = reservationId,
                SimId         = simId,
                GatewayId     = GatewayId,
                SlotIndex     = req.SlotIndex,
                CreatedAt     = DateTime.UtcNow,
                ExpiresAt     = DateTime.UtcNow.AddSeconds(ttlSecs)
            };

            _store?.InsertReservation(record);
            UpdateComData(comDto.COM, dto => dto.IsBusy = true, "IsBusy");  // trigger UI refresh

            return new SimReserveResponse
            {
                ReservationId = reservationId,
                SimId         = simId,
                ExpiresAt     = record.ExpiresAt.ToString("o"),
                IsIdempotent  = false
            };
        }

        public bool HandleSimRelease(SimReleaseRequest req)
        {
            // By reservation_id (preferred)
            if (!string.IsNullOrEmpty(req.ReservationId) && _store != null)
            {
                var rec = _store.GetReservationById(req.ReservationId);
                if (rec != null)
                {
                    _store.ReleaseReservation(req.ReservationId);
                    var comDto = _comDtoMap.Values.FirstOrDefault(d => d.ICCID == rec.SimId);
                    if (comDto != null)
                        UpdateComData(comDto.COM, dto => dto.IsBusy = false, "IsBusy");
                    return true;
                }
            }

            // Legacy: by ussd_session
            if (!string.IsNullOrEmpty(req.UssdSession) &&
                _ussdSessions.TryGetValue(req.UssdSession, out var sess))
            {
                _portToSession.TryRemove(sess.PortName, out _);
                _ussdSessions.TryRemove(req.UssdSession, out _);
                UpdateComData(sess.PortName, dto => dto.IsBusy = false, "IsBusy");
                return true;
            }

            // Legacy: by sim_id/slot
            var dto = FindSimByRequest(req.SimId, req.GatewayId, 0);
            if (dto == null) return false;
            UpdateComData(dto.COM, d => d.IsBusy = false, "IsBusy");
            return true;
        }

        public void HandleSimDisable(string simId, string reason)
        {
            var comDto = _comDtoMap.Values.FirstOrDefault(d => d.ICCID == simId);
            if (comDto != null)
            {
                // Abort any active session immediately
                if (_portToSession.TryGetValue(comDto.COM, out string sessId) &&
                    _ussdSessions.TryGetValue(sessId, out var session))
                    CompleteUssdSession(session, "FAILED", "SIM bị vô hiệu hóa trong khi giao dịch đang chạy");

                UpdateComData(comDto.COM, dto => { dto.IsBusy = false; dto.IsDisabled = true; }, "IsBusy", "IsDisabled");
            }
            _store?.DisableSim(simId, GatewayId, reason);
            logger.Info($"[SIM] Disabled simId={simId} reason={reason}");
        }

        public bool HandleSimEnable(string simId)
        {
            _store?.EnableSim(simId);
            var comDto = _comDtoMap.Values.FirstOrDefault(d => d.ICCID == simId);
            if (comDto != null)
                UpdateComData(comDto.COM, dto => dto.IsDisabled = false, "IsDisabled");
            logger.Info($"[SIM] Enabled simId={simId}");
            return true;
        }

        // ── USSD ──

        /// <summary>API — Submit USSD recharge. Returns session_id immediately; result arrives via webhook.</summary>
        public UssdSendResponse HandleUssdSend(UssdSendRequest req)
        {
            // Idempotency by transaction_id
            if (!string.IsNullOrEmpty(req.TransactionId) && _store != null)
            {
                string existingSessId = _store.FindSessionByTransactionId(req.TransactionId);
                if (existingSessId != null)
                {
                    var existing = HandleGetSession(existingSessId);
                    if (existing != null)
                        return new UssdSendResponse
                        {
                            SessionId    = existingSessId,
                            Status       = existing.Status,
                            SubmittedAt  = existing.SubmittedAt,
                            IsIdempotent = true
                        };
                }
            }

            var comDto = FindSimByRequest(req.SimId, req.GatewayId, req.SlotIndex);
            if (comDto == null)
                throw new InvalidOperationException($"SIM not found: sim_id={req.SimId}, slot={req.SlotIndex}");

            var sp = SerialPorts.FirstOrDefault(x => x.PortName == comDto.COM);
            if (sp == null)
                throw new InvalidOperationException($"COM port not available: {comDto.COM}");

            string simId = !string.IsNullOrEmpty(comDto.ICCID) ? comDto.ICCID : $"slot-{comDto.STT}";

            // Atomic check-and-mark prevents two concurrent sends from grabbing the same SIM
            lock (GetPortBusyLock(comDto.COM))
            {
                if (comDto.IsDisabled || (_store?.IsSimDisabled(simId) ?? false))
                    throw new InvalidOperationException("SIM đã bị vô hiệu hóa");
                if (comDto.IsBusy)
                    throw new InvalidOperationException("SIM đang bận, thử lại sau");
                comDto.IsBusy = true;
            }

            // Resolve ussdCode and pinCode — rollback IsBusy if validation fails (no session started)
            string ussdCode, pinCode;
            try
            {
                if (!string.IsNullOrEmpty(req.PinEncrypted))
                {
                    string masterKey = ConfigurationManager.AppSettings["PinMasterKey"] ?? "";
                    if (string.IsNullOrEmpty(masterKey))
                        throw new InvalidOperationException(
                            "PinMasterKey not configured in App.config. Generate with PinCrypto.GenerateMasterKey().");
                    try
                    {
                        pinCode = PinCrypto.Decrypt(req.PinEncrypted, masterKey);
                    }
                    catch (System.Security.Cryptography.CryptographicException ex)
                    {
                        logger.Error($"[{comDto.COM}] PinCrypto.Decrypt failed: {ex.Message}");
                        throw new InvalidOperationException("PIN decryption failed — wrong key or tampered data.");
                    }
                    string template = !string.IsNullOrEmpty(req.UssdTemplate) ? req.UssdTemplate : "*103*{PIN}#";
                    if (!template.Contains("{PIN}"))
                        throw new InvalidOperationException("ussd_template must contain {PIN} placeholder.");
                    ussdCode = template.Replace("{PIN}", pinCode);
                }
                else if (!string.IsNullOrEmpty(req.UssdCode))
                {
                    logger.Warn($"[{comDto.COM}] Receiving PIN as plain-text ussd_code — use pin_encrypted in production.");
                    ussdCode = req.UssdCode;
                    pinCode  = ExtractPinFromUssd(req.UssdCode);
                }
                else
                {
                    throw new InvalidOperationException("Request requires 'pin_encrypted' (production) or 'ussd_code' (legacy).");
                }
            }
            catch
            {
                UpdateComData(comDto.COM, dto => dto.IsBusy = false, "IsBusy");
                throw;
            }

            string sessionId = "ussd-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            var session = new UssdSession
            {
                SessionId     = sessionId,
                TransactionId = req.TransactionId,
                ReservationId = req.ReservationId,
                PortName      = comDto.COM,
                SimId         = simId,
                GatewayId     = req.GatewayId ?? GatewayId,
                SlotIndex     = req.SlotIndex,
                UssdCode      = ussdCode,
                TargetPhone   = req.PhoneNumber,
                SimMsisdn     = !string.IsNullOrEmpty(req.Msisdn) ? req.Msisdn : comDto.PhoneNumber,
                PinCode       = pinCode,
                Step          = UssdStep.AwaitingMenu,
                SubmittedAt   = DateTime.UtcNow,
                Status        = "submitted"
            };

            _ussdSessions[sessionId]   = session;
            _portToSession[comDto.COM] = sessionId;

            // Persist initial state for recovery across restart
            _store?.UpsertSession(session);
            Metrics?.IncrUssdTotal();

            // Update SIM state and UI
            UpdateComData(comDto.COM, dto =>
            {
                dto.IsBusy     = true;
                dto.LastUssdAt = DateTime.UtcNow;
                dto.Message101 = "Đang nạp thẻ...";
            }, "IsBusy", "LastUssdAt", "Message101");

            int    timeoutSecs     = req.TimeoutSecs > 0 ? req.TimeoutSecs : 60;
            var    capturedSp      = sp;
            var    capturedSession = session;

            Task.Run(() =>
            {
                try
                {
                    if (!capturedSp.IsOpen) capturedSp.Open();
                    lock (_portLocks[capturedSp.PortName])
                        MessageCOMs[capturedSp.PortName] = string.Empty;

                    capturedSp.Write($"AT+CUSD=1,\"{capturedSession.UssdCode}\",15\r");
                    AppendUssdLog(capturedSp.PortName, "→", capturedSession.UssdCode);
                    capturedSession.Status = "processing";

                    Task.Delay(timeoutSecs * 1000).ContinueWith(_ =>
                    {
                        if (capturedSession.Step != UssdStep.Completed)
                        {
                            logger.Warn($"USSD session {capturedSession.SessionId} timed out after {timeoutSecs}s");
                            CompleteUssdSession(capturedSession, "FAILED", "Timeout: no response from carrier");
                        }
                    }, TaskScheduler.Default);
                }
                catch (Exception ex)
                {
                    logger.Error($"HandleUssdSend send error: {ex.Message}");
                    CompleteUssdSession(capturedSession, "FAILED", $"Send error: {ex.Message}");
                }
            });

            return new UssdSendResponse
            {
                SessionId   = sessionId,
                Status      = "submitted",
                SubmittedAt = session.SubmittedAt.ToString("o"),
                IsIdempotent = false
            };
        }

        public UssdSessionDto HandleGetSession(string sessionId)
        {
            // Live in-memory session first
            if (_ussdSessions.TryGetValue(sessionId, out var session))
                return new UssdSessionDto
                {
                    SessionId     = session.SessionId,
                    TransactionId = session.TransactionId,
                    SimId         = session.SimId,
                    GatewayId     = session.GatewayId,
                    Status        = session.Status,
                    ResultMessage = session.ResultMessage,
                    SubmittedAt   = session.SubmittedAt.ToString("o"),
                    CompletedAt   = session.CompletedAt?.ToString("o"),
                    TargetPhone   = session.TargetPhone
                };

            // Fall back to SQLite (completed sessions)
            if (_store == null) return null;
            var rec = _store.GetSession(sessionId);
            if (rec == null) return null;

            return new UssdSessionDto
            {
                SessionId     = rec.SessionId,
                TransactionId = rec.TransactionId,
                SimId         = rec.SimId,
                GatewayId     = rec.GatewayId,
                Status        = rec.Status,
                ResultMessage = rec.ResultMessage,
                SubmittedAt   = rec.SubmittedAt.ToString("o"),
                CompletedAt   = rec.CompletedAt?.ToString("o"),
                TargetPhone   = rec.TargetPhone
            };
        }

        public bool HandleCancelSession(string sessionId)
        {
            if (!_ussdSessions.TryGetValue(sessionId, out var session)) return false;
            if (session.Step == UssdStep.Completed) return false;

            CompleteUssdSession(session, "FAILED", "Cancelled by caller");
            Metrics?.IncrUssdCancelled();
            return true;
        }

        // ── Gateway ──

        public GatewayHealthResponse HandleGatewayHealth(string gatewayId)
        {
            var statuses = new List<SimStatusDto>();

            foreach (var dto in _comDtoMap.Values)
            {
                string simId = !string.IsNullOrEmpty(dto.ICCID) ? dto.ICCID : $"slot-{dto.STT}";
                bool isDisabled = dto.IsDisabled;

                string status = isDisabled                            ? "disabled"
                              : dto.IsBusy                           ? "busy"
                              : string.IsNullOrEmpty(dto.PhoneNumber) ? "offline"
                              : "available";

                statuses.Add(new SimStatusDto
                {
                    SimId          = simId,
                    Status         = status,
                    SignalStrength = dto.SignalStrength,
                    IsDisabled     = isDisabled,
                    LastActivity   = dto.LastUssdAt?.ToString("o") ?? ""
                });
            }

            // Refresh metrics gauges
            if (Metrics != null)
            {
                Metrics.SimsAvailable = statuses.Count(s => s.Status == "available");
                Metrics.SimsBusy      = statuses.Count(s => s.Status == "busy");
                Metrics.SimsOffline   = statuses.Count(s => s.Status == "offline");
                Metrics.SimsDisabled  = statuses.Count(s => s.Status == "disabled");
            }

            return new GatewayHealthResponse
            {
                GatewayId   = gatewayId,
                Online      = statuses.Count > 0,
                SimStatuses = statuses
            };
        }

        public bool HandleGatewayReboot(string gatewayId)
        {
            if (!string.Equals(gatewayId, GatewayId, StringComparison.OrdinalIgnoreCase) &&
                gatewayId != "all" && gatewayId != "*")
                return false;

            logger.Warn($"[Gateway] Reboot requested for {gatewayId}");

            // Close and reopen all serial ports to simulate a reset
            Task.Run(() =>
            {
                foreach (var sp in SerialPorts.ToList())
                {
                    try
                    {
                        if (sp.IsOpen) { sp.Close(); Thread.Sleep(500); }
                        sp.Open();
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"[Gateway] Reopen {sp.PortName}: {ex.Message}");
                    }
                }
            });

            return true;
        }

        // ──────────────────── USSD State Machine ────────────────────

        internal void HandleSessionCusdResponse(SerialPort sp, UssdSession session, string cusdText)
        {
            try
            {
                string lower = StripDiacritics(cusdText.ToLower());
                UpdateComData(sp.PortName, dto => dto.Message101 = cusdText, "Message101");
                logger.Info($"[{sp.PortName}] session={session.SessionId} step={session.Step}: {cusdText}");
                AppendUssdLog(sp.PortName, "←", cusdText);

                if (session.IsManualMode)
                {
                    if (IsSuccessResult(lower))
                        CompleteUssdSession(session, "SUCCESS", cusdText);
                    else if (IsFailureResult(lower))
                        CompleteUssdSession(session, "FAILED", cusdText);
                    return;
                }

                switch (session.Step)
                {
                    case UssdStep.AwaitingMenu:
                        if (IsMenuResponse(lower))
                        {
                            bool selfTopUp = NormalizePhone(session.SimMsisdn) == NormalizePhone(session.TargetPhone);
                            if (selfTopUp)
                            {
                                session.Step = UssdStep.AwaitingPinInput;
                                SendUssdReply(sp, "2");
                            }
                            else
                            {
                                session.Step = UssdStep.AwaitingPhoneInput;
                                SendUssdReply(sp, "1");
                            }
                        }
                        else if (IsSuccessResult(lower)) CompleteUssdSession(session, "SUCCESS", cusdText);
                        else if (IsFailureResult(lower)) CompleteUssdSession(session, "FAILED",  cusdText);
                        break;

                    case UssdStep.AwaitingPhoneInput:
                        if (IsSuccessResult(lower)) { CompleteUssdSession(session, "SUCCESS", cusdText); break; }
                        if (IsFailureResult(lower)) { CompleteUssdSession(session, "FAILED",  cusdText); break; }
                        session.Step = UssdStep.AwaitingPinInput;
                        SendUssdReply(sp, NormalizePhone(session.TargetPhone));
                        break;

                    case UssdStep.AwaitingPinInput:
                        if (IsSuccessResult(lower)) { CompleteUssdSession(session, "SUCCESS", cusdText); break; }
                        if (IsFailureResult(lower)) { CompleteUssdSession(session, "FAILED",  cusdText); break; }
                        session.Step = UssdStep.AwaitingConfirm;
                        SendUssdReply(sp, session.PinCode);
                        break;

                    case UssdStep.AwaitingConfirm:
                        if (IsSuccessResult(lower)) { CompleteUssdSession(session, "SUCCESS", cusdText); break; }
                        if (IsFailureResult(lower)) { CompleteUssdSession(session, "FAILED",  cusdText); break; }
                        session.Step = UssdStep.AwaitingResult;
                        SendUssdReply(sp, "1");
                        break;

                    case UssdStep.AwaitingResult:
                        if (IsSuccessResult(lower)) CompleteUssdSession(session, "SUCCESS", cusdText);
                        else                        CompleteUssdSession(session, "FAILED",  cusdText);
                        break;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] HandleSessionCusdResponse: {ex.Message}");
                CompleteUssdSession(session, "FAILED", $"Processing error: {ex.Message}");
            }
        }

        private void CompleteUssdSession(UssdSession session, string result, string message)
        {
            // Atomic: only one of (timeout / state-machine / SIM-removal / cancel) wins
            if (Interlocked.CompareExchange(ref session.CompletionFlag, 1, 0) != 0) return;

            session.Step          = UssdStep.Completed;
            session.Status        = result == "SUCCESS" ? "success" : "failed";
            session.ResultMessage = message;
            session.CompletedAt   = DateTime.UtcNow;

            _portToSession.TryRemove(session.PortName, out _);
            // Keep session in memory 30s for Go BE polling, then release
            Task.Delay(30_000).ContinueWith(_ => _ussdSessions.TryRemove(session.SessionId, out _),
                TaskScheduler.Default);

            UpdateComData(session.PortName, dto =>
            {
                dto.IsBusy    = false;
                dto.Message101 = $"[{result}] {message}";
            }, "IsBusy", "Message101");

            logger.Info($"USSD session {session.SessionId} completed: {result} — {message}");
            AppendUssdLog(session.PortName, "●", $"[RESULT: {result}] {message}");

            // Persist + audit + webhook in background
            var capturedSession = session;
            Task.Run(() =>
            {
                _store?.UpsertSession(capturedSession);
                WriteAuditRecord(capturedSession);
                EnqueueUssdCompletedWebhook(capturedSession);
            });

            if (result == "SUCCESS") Metrics?.IncrUssdSuccess();
            else                     Metrics?.IncrUssdFailed();

            BeginInvokeOnUiThread(() => UpdateNapTheButtonState(session.PortName, false));
            if (_comDtoMap.TryGetValue(session.PortName, out var dtoForHeader))
                BeginInvokeOnUiThread(() => UpdateSimHeader(dtoForHeader));
        }

        private void SendUssdReply(SerialPort sp, string reply)
        {
            try
            {
                lock (_portLocks[sp.PortName])
                    MessageCOMs[sp.PortName] = string.Empty;

                sp.Write($"AT+CUSD=1,\"{reply}\",15\r");
                AppendUssdLog(sp.PortName, "→", reply);
                logger.Debug($"[{sp.PortName}] → USSD reply: {reply}");
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] SendUssdReply: {ex.Message}");
            }
        }

        // ──────────────────── USSD Conversation Log ────────────────────

        internal void AppendUssdLog(string portName, string direction, string text)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {direction} {text}";
            var log = _ussdLogs.GetOrAdd(portName, _ => new List<string>());
            lock (log)
            {
                log.Add(entry);
                if (log.Count > UssdLogMaxEntries) log.RemoveAt(0);
            }
            if (portName == _selectedPortName)
                BeginInvokeOnUiThread(() => RefreshUssdLog(portName));
        }

        private void BeginInvokeOnUiThread(Action action)
        {
            try
            {
                if (IsHandleCreated)
                {
                    if (InvokeRequired) BeginInvoke(action);
                    else action();
                }
            }
            catch (Exception ex) { logger.Error($"BeginInvokeOnUiThread: {ex.Message}"); }
        }

        // ──────────────────── Webhook outbox ────────────────────

        // Called by CompleteUssdSession — enqueues instead of direct HTTP to survive transient failures.
        private void EnqueueUssdCompletedWebhook(UssdSession session)
        {
            if (_store == null) return;
            string baseUrl = ConfigurationManager.AppSettings["WebhookBaseUrl"] ?? "";
            if (string.IsNullOrWhiteSpace(baseUrl)) return;

            var payload = new
            {
                event_type     = "ussd.completed",
                session_id     = session.SessionId,
                transaction_id = session.TransactionId,
                sim_id         = session.SimId,
                gateway_id     = session.GatewayId ?? GatewayId,
                result         = session.Status == "success" ? "SUCCESS" : "FAILED",
                message        = session.ResultMessage,
                submitted_at   = session.SubmittedAt.ToString("o"),
                completed_at   = (session.CompletedAt ?? DateTime.UtcNow).ToString("o")
            };
            _store.EnqueueWebhook("ussd.completed", JsonConvert.SerializeObject(payload));
        }

        // Called from ListenEventSIMStatus (GSMForm.cs) when a SIM goes offline.
        internal void EnqueueSimOfflineWebhook(string simId, string msisdn, string portName)
        {
            if (_store == null) return;
            string baseUrl = ConfigurationManager.AppSettings["WebhookBaseUrl"] ?? "";
            if (string.IsNullOrWhiteSpace(baseUrl)) return;

            var payload = new
            {
                event_type = "sim.offline",
                sim_id     = simId,
                msisdn     = msisdn,
                gateway_id = GatewayId,
                port       = portName,
                ts         = DateTime.UtcNow.ToString("o")
            };
            _store.EnqueueWebhook("sim.offline", JsonConvert.SerializeObject(payload));
        }

        // Called from StopApiServer — direct fire at shutdown (outbox worker may not run again).
        private void FireGatewayOfflineWebhookDirect()
        {
            string baseUrl = ConfigurationManager.AppSettings["WebhookBaseUrl"] ?? "";
            if (string.IsNullOrWhiteSpace(baseUrl)) return;
            try
            {
                var payload = new
                {
                    event_type = "gateway.offline",
                    gateway_id = GatewayId,
                    ts         = DateTime.UtcNow.ToString("o")
                };
                string json   = JsonConvert.SerializeObject(payload);
                string secret = ConfigurationManager.AppSettings["WebhookSecret"] ?? "";
                string sig    = HmacSha256Hex(json, secret);

                using (var req = new HttpRequestMessage(HttpMethod.Post,
                    $"{baseUrl.TrimEnd('/')}/webhooks/gateway"))
                {
                    req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                    req.Headers.TryAddWithoutValidation("X-Event-Type",         "gateway.offline");
                    req.Headers.TryAddWithoutValidation("X-Webhook-Signature",  $"sha256={sig}");
                    _webhookClient.SendAsync(req).Wait(5000);
                }
            }
            catch { }
        }

        // ──────────────────── Background workers ────────────────────

        private void RecoverIncompleteSessions()
        {
            if (_store == null) return;
            var incomplete = _store.GetIncompleteSessions();
            if (incomplete.Count == 0) return;

            logger.Warn($"[Recovery] {incomplete.Count} incomplete session(s) from previous run — marking failed");
            foreach (var rec in incomplete)
            {
                // Synthesise a minimal session object for DB update
                var s = new UssdSession
                {
                    SessionId     = rec.SessionId,
                    TransactionId = rec.TransactionId,
                    SimId         = rec.SimId,
                    GatewayId     = rec.GatewayId,
                    SlotIndex     = rec.SlotIndex,
                    PortName      = rec.PortName,
                    TargetPhone   = rec.TargetPhone,
                    SimMsisdn     = rec.SimMsisdn,
                    Status        = "failed",
                    ResultMessage = "Service restarted — session state lost",
                    SubmittedAt   = rec.SubmittedAt,
                    CompletedAt   = DateTime.UtcNow,
                    Step          = UssdStep.Completed,
                    CompletionFlag= 1
                };
                _store.UpsertSession(s);

                // Enqueue webhook so Go BE learns about the failure
                EnqueueUssdCompletedWebhook(s);
            }
        }

        // Runs every 30 s — expires SIM reservations whose TTL has elapsed.
        private void RunTtlExpiryWorker(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                ct.WaitHandle.WaitOne(30_000);
                if (ct.IsCancellationRequested) break;
                try
                {
                    var expired = _store?.ExpireAndReturnExpired();
                    if (expired == null || expired.Count == 0) continue;

                    foreach (var r in expired)
                    {
                        var comDto = _comDtoMap.Values.FirstOrDefault(d => d.ICCID == r.SimId);
                        if (comDto != null)
                            UpdateComData(comDto.COM, dto => dto.IsBusy = false, "IsBusy");
                        logger.Info($"[TTL] Reservation {r.ReservationId} expired for SIM {r.SimId}");
                    }
                }
                catch (Exception ex)
                {
                    if (!ct.IsCancellationRequested)
                        logger.Error($"[TTL] Worker error: {ex.Message}");
                }
            }
        }

        // Polls webhook outbox every 5 s — delivers pending webhooks with exponential back-off.
        private async Task RunWebhookOutboxWorkerAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try { await Task.Delay(5000, ct).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }

                if (_store == null) continue;

                List<WebhookOutboxRecord> pending;
                try { pending = _store.GetPendingWebhooks(); }
                catch (Exception ex) { logger.Error($"[Outbox] GetPending: {ex.Message}"); continue; }

                foreach (var w in pending)
                {
                    if (ct.IsCancellationRequested) break;
                    try
                    {
                        bool ok = await DeliverWebhookAsync(w.Payload).ConfigureAwait(false);
                        if (ok)
                        {
                            _store.MarkWebhookDelivered(w.Id);
                            Metrics?.IncrWebhookDelivery();
                            logger.Debug($"[Outbox] Delivered id={w.Id} type={w.EventType}");
                        }
                        else
                        {
                            ScheduleOrDeadLetter(w, "HTTP 5xx");
                        }
                    }
                    catch (Exception ex)
                    {
                        ScheduleOrDeadLetter(w, ex.Message);
                    }
                }
            }
        }

        private void ScheduleOrDeadLetter(WebhookOutboxRecord w, string error)
        {
            int next = w.Attempt + 1;
            if (next > 5)
            {
                _store.DeadLetterWebhook(w.Id, error);
                Metrics?.IncrWebhookDeadLetter();
                logger.Error($"[Outbox] Dead-letter id={w.Id} type={w.EventType}: {error}");
            }
            else
            {
                // 2^next seconds: 2, 4, 8, 16, 32
                var nextAt = DateTime.UtcNow.AddSeconds(Math.Pow(2, next));
                _store.ScheduleWebhookRetry(w.Id, next, nextAt, error);
                Metrics?.IncrWebhookRetry();
                logger.Warn($"[Outbox] Retry #{next} id={w.Id} type={w.EventType} at {nextAt:HH:mm:ss}");
            }
        }

        private async Task<bool> DeliverWebhookAsync(string payloadJson)
        {
            string baseUrl = ConfigurationManager.AppSettings["WebhookBaseUrl"] ?? "";
            if (string.IsNullOrWhiteSpace(baseUrl)) return true;  // no-op if not configured

            string secret = ConfigurationManager.AppSettings["WebhookSecret"] ?? "";
            string sig    = HmacSha256Hex(payloadJson, secret);

            // All webhook events go to {baseUrl}/webhooks/gsm — event_type is in the payload
            string url = $"{baseUrl.TrimEnd('/')}/webhooks/gsm";

            using (var reqMsg = new HttpRequestMessage(HttpMethod.Post, url))
            {
                reqMsg.Content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                reqMsg.Headers.TryAddWithoutValidation("X-Webhook-Signature", $"sha256={sig}");

                var resp = await _webhookClient.SendAsync(reqMsg).ConfigureAwait(false);
                return (int)resp.StatusCode < 500;
            }
        }

        // ──────────────────── Helpers ────────────────────

        private ComDto FindSimByRequest(string simId, string gatewayId, int slotIndex)
        {
            if (!string.IsNullOrEmpty(simId))
                foreach (var dto in _comDtoMap.Values)
                    if (dto.ICCID == simId) return dto;

            if (slotIndex > 0)
            {
                string sttStr = slotIndex.ToString();
                foreach (var dto in _comDtoMap.Values)
                    if (dto.STT == sttStr) return dto;
            }
            return null;
        }

        private int QuerySignalStrength(string portName)
        {
            try
            {
                var sp = SerialPorts.FirstOrDefault(x => x.PortName == portName);
                if (sp == null || !sp.IsOpen) return 0;

                lock (_portLocks[portName]) MessageCOMs[portName] = string.Empty;
                sp.Write("AT+CSQ\r");
                Thread.Sleep(600);

                string resp;
                lock (_portLocks[portName]) resp = MessageCOMs[portName];

                var m = Regex.Match(resp, @"\+CSQ:\s*(\d+),");
                if (!m.Success) return 0;
                int rssi = int.Parse(m.Groups[1].Value);
                return rssi == 99 ? 0 : (int)Math.Round(rssi / 31.0 * 100);
            }
            catch { return 0; }
        }

        internal static string ExtractCusdText(string content)
        {
            var m = Regex.Match(content, @"\+CUSD:\s*\d+,""(.*?)"",\d+", RegexOptions.Singleline);
            if (!m.Success) return null;
            string text = m.Groups[1].Value;
            return Common.IsValidUtf16(text) ? Common.DecodeUnicode(text) : text;
        }

        private static bool IsMenuResponse(string lower) =>
            Regex.IsMatch(lower, @"\b1[\s]*[.\-\)\:]\s*\S") &&
            Regex.IsMatch(lower, @"\b2[\s]*[.\-\)\:]\s*\S");

        private static bool IsSuccessResult(string lower) =>
            lower.Contains("thanh cong")           ||
            lower.Contains("nap thanh cong")       ||
            lower.Contains("nap tien thanh cong")  ||
            lower.Contains("so du hien tai")       ||
            lower.Contains("giao dich thanh cong") ||
            lower.Contains("da nap")               ||
            lower.Contains("nap tien vao")         ||
            lower.Contains("top up success")       ||
            lower.Contains("recharge success")     ||
            lower.Contains("successful");

        private static bool IsFailureResult(string lower) =>
            lower.Contains("that bai")            ||
            lower.Contains("khong thanh cong")    ||
            lower.Contains("giao dich that bai")  ||
            lower.Contains("khong hop le")        ||
            lower.Contains("the khong hop le")    ||
            lower.Contains("sai ma the")          ||
            lower.Contains("ma the sai")          ||
            lower.Contains("ma the khong dung")   ||
            lower.Contains("the da su dung")      ||
            lower.Contains("da su dung")          ||
            lower.Contains("het han su dung")     ||
            lower.Contains("the het han")         ||
            lower.Contains("het han")             ||
            lower.Contains("vuot qua gioi han")   ||
            lower.Contains("qua so lan")          ||
            lower.Contains("khong du")            ||
            lower.Contains("so dien thoai sai")   ||
            lower.Contains("loi giao dich")       ||
            lower.Contains("loi he thong")        ||
            lower.Contains("failed")              ||
            lower.Contains("invalid")             ||
            lower.Contains("error");

        private static string StripDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = text.Replace('đ', 'd').Replace('Đ', 'D');
            string normalized = text.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            return sb.ToString();
        }

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return "";
            phone = Regex.Replace(phone.Trim(), @"[\s\-\.]", "");
            if (phone.StartsWith("+84")) return "0" + phone.Substring(3);
            if (phone.StartsWith("84") && phone.Length >= 11) return "0" + phone.Substring(2);
            return phone;
        }

        private object GetPortBusyLock(string portName) =>
            _portBusyLocks.GetOrAdd(portName, _ => new object());

        private static string ExtractPinFromUssd(string ussdCode)
        {
            if (string.IsNullOrEmpty(ussdCode)) return "";
            string inner = ussdCode.Trim('*', '#');
            string[] parts = inner.Split('*');
            for (int i = 1; i < parts.Length; i++)
                if (Regex.IsMatch(parts[i], @"^\d{9,15}$"))
                    return parts[i];
            return parts.Length > 1 ? parts[parts.Length - 1] : "";
        }

        // ──────────────────── Audit log ────────────────────

        private void WriteAuditRecord(UssdSession session)
        {
            try
            {
                Directory.CreateDirectory(AuditDir);
                string filePath = Path.Combine(AuditDir, $"audit-{DateTime.Now:yyyy-MM-dd}.jsonl");

                string[] conversation = Array.Empty<string>();
                if (_ussdLogs.TryGetValue(session.PortName, out var log))
                    lock (log) { conversation = log.ToArray(); }

                var record = new
                {
                    ts           = DateTime.UtcNow.ToString("o"),
                    session_id   = session.SessionId,
                    transaction_id = session.TransactionId,
                    gateway_id   = session.GatewayId,
                    port         = session.PortName,
                    sim_id       = session.SimId,
                    msisdn       = session.SimMsisdn,
                    target_phone = session.TargetPhone,
                    ussd_code    = session.UssdCode,
                    pin_hash     = string.IsNullOrEmpty(session.PinCode) ? "" : HmacSha256Hex(session.PinCode, "audit-pin-mask"),
                    result       = session.Status,
                    message      = session.ResultMessage,
                    submitted_at = session.SubmittedAt.ToString("o"),
                    completed_at = session.CompletedAt?.ToString("o"),
                    conversation = conversation
                };

                string line = JsonConvert.SerializeObject(record,
                    Formatting.None, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

                lock (_auditFileLock)
                    File.AppendAllText(filePath, line + "\n", Encoding.UTF8);
            }
            catch (Exception ex)
            {
                logger.Error($"WriteAuditRecord [{session.SessionId}]: {ex.Message}");
            }
        }

        private static string HmacSha256Hex(string message, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }
    }
}
