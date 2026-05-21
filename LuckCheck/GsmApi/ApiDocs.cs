using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace LuckCheck.GsmApi
{
    // Builds and caches the OpenAPI 3.0.3 spec. Served at GET /api-docs and GET /swagger.
    internal static class ApiDocs
    {
        private static volatile string _cachedJson;

        public static string GetOpenApiJson(string serverUrl = "http://localhost:8080")
        {
            if (_cachedJson != null) return _cachedJson;

            var spec = new JObject
            {
                ["openapi"] = "3.0.3",
                ["info"]    = JObject.FromObject(new
                {
                    title       = "GSM Gateway API",
                    version     = "1.0.0",
                    description = "API for GSM gateway service — manages SIM pools, USSD recharge sessions, " +
                                  "gateway health, and delivers results via webhook outbox.\n\n" +
                                  "**Authentication (production):** All `POST` endpoints require two headers when `ApiSharedSecret` is configured:\n" +
                                  "- `X-Timestamp`: Unix epoch seconds (string)\n" +
                                  "- `X-Signature`: `HmacSha256Hex(ApiSharedSecret, \"{X-Timestamp}\\n{SHA256Hex(body)}\")`\n\n" +
                                  "**PIN encryption:** Use `pin_encrypted` + `ussd_template` (AES-256-CBC + HMAC-SHA256). " +
                                  "Plain-text `ussd_code` is supported only in dev mode."
                }),
                ["servers"] = new JArray(
                    new JObject { ["url"] = serverUrl, ["description"] = "GSM machine (current)" }
                ),
                ["tags"] = new JArray(
                    Tag("SIM Management",            "Reserve, release, disable/enable SIMs in the pool"),
                    Tag("USSD Sessions",             "Submit recharge requests and poll results"),
                    Tag("Gateway",                   "Health check and gateway control"),
                    Tag("Observability",             "Prometheus metrics endpoint"),
                    Tag("Debug (dev mode only)",     "Only available when ApiSharedSecret is empty")
                ),
                ["paths"]      = BuildPaths(),
                ["components"] = BuildComponents()
            };

            _cachedJson = spec.ToString(Formatting.Indented);
            return _cachedJson;
        }

        // ── Swagger UI HTML (loads spec from /api-docs) ──────────────────

        public static string GetSwaggerHtml()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
  <title>GSM Gateway — API Docs</title>
  <link rel=""stylesheet"" href=""https://unpkg.com/swagger-ui-dist@5/swagger-ui.css"">
  <style>
    body { margin: 0; }
    .topbar { display: none; }
  </style>
</head>
<body>
  <div id=""swagger-ui""></div>
  <script src=""https://unpkg.com/swagger-ui-dist@5/swagger-ui-bundle.js""></script>
  <script src=""https://unpkg.com/swagger-ui-dist@5/swagger-ui-standalone-preset.js""></script>
  <script>
    window.onload = function() {
      SwaggerUIBundle({
        url:          '/api-docs',
        dom_id:       '#swagger-ui',
        deepLinking:  true,
        presets:      [SwaggerUIBundle.presets.apis, SwaggerUIStandalonePreset],
        plugins:      [SwaggerUIBundle.plugins.DownloadUrl],
        layout:       'StandaloneLayout',
        tryItOutEnabled: true,
        requestInterceptor: function(req) {
          // Auto-add X-Timestamp (signature still needs to be set manually in prod)
          req.headers['X-Timestamp'] = String(Math.floor(Date.now() / 1000));
          return req;
        }
      });
    };
  </script>
</body>
</html>";
        }

        // ──────────────────── Path builders ────────────────────

        private static JObject BuildPaths()
        {
            return new JObject
            {
                // ── SIM management ──
                ["/api/v1/sims"] = new JObject
                {
                    ["get"] = Op("listSims", "SIM Management", "List all SIMs",
                        "Returns status of every SIM slot this gateway manages.",
                        security: false,
                        response200: ArrayOf(Ref("SimDto")))
                },

                ["/api/v1/sims/{sim_id}"] = new JObject
                {
                    ["get"] = Op("getSim", "SIM Management", "Get single SIM",
                        security: false,
                        parameters: new JArray(PathParam("sim_id", "ICCID or slot-N identifier", "89849000012345678901")),
                        response200: Ref("SimDto"),
                        extra404: true)
                },

                ["/api/v1/sims/reserve"] = new JObject
                {
                    ["post"] = Op("reserveSim", "SIM Management", "Reserve a SIM (with TTL)",
                        "Atomically reserves a SIM. " +
                        "Pass `reservation_id` for idempotency — if it already exists and is still valid the same record is returned with `is_idempotent: true`. " +
                        "`ttl_secs` defaults to 120 s; the reservation auto-expires if not released.",
                        body: Ref("SimReserveRequest"),
                        response200: Ref("SimReserveResponse"),
                        statusCode: 201,
                        extra409: true, extra404: true)
                },

                ["/api/v1/sims/release"] = new JObject
                {
                    ["post"] = Op("releaseSim", "SIM Management", "Release a reserved SIM",
                        "Prefer `reservation_id`. Legacy fields `ussd_session` / `sim_id` are also accepted for backward compatibility.",
                        body: Ref("SimReleaseRequest"),
                        extra404: true)
                },

                ["/api/v1/sims/{sim_id}/disable"] = new JObject
                {
                    ["post"] = Op("disableSim", "SIM Management", "Disable a SIM",
                        "Persists to SQLite. Any active session on the SIM is aborted immediately.",
                        parameters: new JArray(PathParam("sim_id", "ICCID")),
                        body: Ref("SimDisableRequest"))
                },

                ["/api/v1/sims/{sim_id}/enable"] = new JObject
                {
                    ["post"] = Op("enableSim", "SIM Management", "Re-enable a disabled SIM",
                        parameters: new JArray(PathParam("sim_id", "ICCID")),
                        extra404: true)
                },

                // ── USSD ──
                ["/api/v1/ussd/send"] = new JObject
                {
                    ["post"] = Op("ussdSend", "USSD Sessions", "Submit a USSD recharge request",
                        "Returns `session_id` immediately (HTTP 202). " +
                        "The actual result arrives asynchronously via `POST {WebhookBaseUrl}/webhooks/gsm` with `event_type: ussd.completed`. " +
                        "Pass `transaction_id` for idempotency — duplicate calls return the existing session.",
                        body: Ref("UssdSendRequest"),
                        response200: Ref("UssdSendResponse"),
                        statusCode: 202,
                        extra404: true, extra409: true)
                },

                ["/api/v1/ussd/session/{session_id}"] = new JObject
                {
                    ["get"] = Op("getSession", "USSD Sessions", "Poll USSD session status",
                        "Live sessions are served from memory; completed sessions from SQLite (available for 30 s after completion).",
                        security: false,
                        parameters: new JArray(PathParam("session_id", "Session ID returned by /ussd/send", "ussd-a1b2c3d4e5f6")),
                        response200: Ref("UssdSessionDto"),
                        extra404: true)
                },

                ["/api/v1/ussd/session/{session_id}/cancel"] = new JObject
                {
                    ["post"] = Op("cancelSession", "USSD Sessions", "Cancel an active USSD session",
                        "Has no effect if the session is already completed.",
                        parameters: new JArray(PathParam("session_id", "Session ID")),
                        extra404: true)
                },

                // ── Gateway ──
                ["/api/v1/gateways/health"] = new JObject
                {
                    ["get"] = Op("gatewayHealth", "Gateway", "Gateway health (shorthand)",
                        "Uses the gateway's own configured ID. Identical to `/api/v1/gateways/{gateway_id}/health`.",
                        security: false,
                        response200: Ref("GatewayHealthResponse"))
                },

                ["/api/v1/gateways/{gateway_id}/health"] = new JObject
                {
                    ["get"] = Op("gatewayHealthById", "Gateway", "Gateway health (explicit ID)",
                        security: false,
                        parameters: new JArray(PathParam("gateway_id", "Gateway ID configured in App.config", "gw-001")),
                        response200: Ref("GatewayHealthResponse"))
                },

                ["/api/v1/gateways/{gateway_id}/reboot"] = new JObject
                {
                    ["post"] = Op("gatewayReboot", "Gateway", "Soft-reboot gateway",
                        "Closes and reopens all serial ports. Active USSD sessions are NOT automatically aborted first — caller should wait for sessions to complete before rebooting.",
                        parameters: new JArray(PathParam("gateway_id", "Gateway ID or '*' for all", "gw-001")),
                        statusCode: 202,
                        extra404: true)
                },

                // ── Observability ──
                ["/metrics"] = new JObject
                {
                    ["get"] = Op("prometheusMetrics", "Observability", "Prometheus metrics",
                        "Counters: `gsm_ussd_requests_total`, `gsm_ussd_success_total`, `gsm_ussd_failed_total`, " +
                        "`gsm_ussd_cancelled_total`, `gsm_webhook_deliveries_total`, `gsm_webhook_retries_total`, `gsm_webhook_deadletters_total`.\n" +
                        "Gauges: `gsm_sims_available`, `gsm_sims_busy`, `gsm_sims_offline`, `gsm_sims_disabled`, `gsm_uptime_seconds`.",
                        security: false,
                        responseMimeType: "text/plain")
                },

                // ── Debug ──
                ["/api/v1/debug/generate-key"] = new JObject
                {
                    ["get"] = Op("debugGenerateKey", "Debug (dev mode only)",
                        "Generate a random 32-byte PinMasterKey",
                        "Returns a base64-encoded key. **Copy both to `App.config[PinMasterKey]` and Go BE `env[PIN_MASTER_KEY]`.** " +
                        "Rotating the key invalidates all existing encrypted PINs.",
                        security: false)
                },

                ["/api/v1/debug/encrypt-pin"] = new JObject
                {
                    ["post"] = Op("debugEncryptPin", "Debug (dev mode only)",
                        "Encrypt a PIN for testing",
                        "Encrypts a plain-text PIN with AES-256-CBC + HMAC-SHA256. " +
                        "Copy `pin_encrypted` and `ussd_template` into the `POST /api/v1/ussd/send` request. " +
                        "**Do not use in production — only available when `ApiSharedSecret` is empty.**",
                        security: false,
                        body: Ref("DebugEncryptPinRequest"),
                        response200: Ref("DebugEncryptPinResponse"))
                }
            };
        }

        // ──────────────────── Schema builders ────────────────────

        private static JObject BuildComponents()
        {
            return new JObject
            {
                ["securitySchemes"] = new JObject
                {
                    ["HmacSignature"] = new JObject
                    {
                        ["type"]        = "apiKey",
                        ["in"]          = "header",
                        ["name"]        = "X-Signature",
                        ["description"] = "HMAC-SHA256 of `\"{X-Timestamp}\\n{SHA256Hex(body)}\"` keyed with `ApiSharedSecret`. " +
                                          "Also requires `X-Timestamp` header (Unix epoch seconds, ±300 s tolerance). " +
                                          "Required on all POST endpoints when `ApiSharedSecret` is configured."
                    }
                },
                ["schemas"] = new JObject
                {
                    ["SimDto"] = SchemaObj(
                        ("sim_id",          "string",  "ICCID or 'slot-N' if ICCID unavailable",  "89849000012345678901"),
                        ("gateway_id",      "string",  "Gateway that owns this SIM",               "gw-001"),
                        ("slot_index",      "integer", "Physical slot number",                     null),
                        ("msisdn",          "string",  "Phone number of the SIM",                  "0912345678"),
                        ("status",          "string",  "available | busy | offline | disabled",    "available"),
                        ("network",         "string",  "Carrier name from AT+COPS",                "Viettel"),
                        ("signal_strength", "integer", "Signal 0–100 (from AT+CSQ, cached)",       null),
                        ("is_disabled",     "boolean", "Administratively disabled",                null),
                        ("last_activity",   "string",  "ISO-8601 UTC timestamp of last USSD",      null)
                    ),

                    ["SimReserveRequest"] = SchemaObj(
                        ("sim_id",         "string",  "ICCID to reserve (preferred over slot_index)", "89849000012345678901"),
                        ("gateway_id",     "string",  "Target gateway ID",                            "gw-001"),
                        ("slot_index",     "integer", "Slot index as fallback if sim_id is empty",    null),
                        ("ttl_secs",       "integer", "Reservation TTL in seconds (default 120)",     null),
                        ("reservation_id", "string",  "Caller-supplied idempotency key",              "res-abc123xyz")
                    ),

                    ["SimReserveResponse"] = SchemaObj(
                        ("reservation_id", "string",  "Use this in SimReleaseRequest",       "res-abc123xyz"),
                        ("sim_id",         "string",  "ICCID of the reserved SIM",           "89849000012345678901"),
                        ("expires_at",     "string",  "ISO-8601 UTC expiry time",             null),
                        ("is_idempotent",  "boolean", "true if this was a duplicate request", null)
                    ),

                    ["SimReleaseRequest"] = SchemaObj(
                        ("reservation_id", "string", "ID from SimReserveResponse (preferred)", "res-abc123xyz"),
                        ("sim_id",         "string", "Legacy: ICCID (fallback)",               null),
                        ("gateway_id",     "string", "Legacy: gateway ID (fallback)",           null),
                        ("ussd_session",   "string", "Legacy: session ID (backward compat)",    null)
                    ),

                    ["SimDisableRequest"] = SchemaObj(
                        ("reason", "string", "Human-readable reason for disabling", "Defective SIM card")
                    ),

                    ["UssdSendRequest"] = new JObject
                    {
                        ["type"] = "object",
                        ["required"] = new JArray("phone_number"),
                        ["properties"] = new JObject
                        {
                            ["sim_id"]         = Prop("string",  "ICCID of SIM to use",                                                       "89849000012345678901"),
                            ["gateway_id"]     = Prop("string",  "Target gateway (defaults to gateway's own ID)",                             "gw-001"),
                            ["slot_index"]     = Prop("integer", "Slot fallback if sim_id empty"),
                            ["msisdn"]         = Prop("string",  "Phone number of the SIM (for self vs. other logic)",                        "0912345678"),
                            ["phone_number"]   = Prop("string",  "Target phone number to top up",                                             "0987654321"),
                            ["timeout_secs"]   = Prop("integer", "USSD session timeout in seconds (default 60)"),
                            ["transaction_id"] = Prop("string",  "Caller idempotency key — duplicate calls return existing session",          "txn-20240101-001"),
                            ["reservation_id"] = Prop("string",  "Optional: link to an existing SIM reservation",                            "res-abc123xyz"),
                            ["ussd_template"]  = Prop("string",  "**Production** USSD code template with {PIN} placeholder",                 "*103*{PIN}#"),
                            ["pin_encrypted"]  = Prop("string",  "**Production** Base64(IV[16] | AES-256-CBC ciphertext | HMAC-SHA256[32])", null),
                            ["enc_version"]    = Prop("string",  "Encryption version (currently 'aes256cbc-hmac-v1')",                       "aes256cbc-hmac-v1"),
                            ["ussd_code"]      = Prop("string",  "**Dev/legacy only** Full USSD code with PIN in plain text — avoid in production", "*103*123456789012#")
                        },
                        ["example"] = new JObject
                        {
                            ["sim_id"]         = "89849000012345678901",
                            ["phone_number"]   = "0987654321",
                            ["transaction_id"] = "txn-20240101-001",
                            ["ussd_template"]  = "*103*{PIN}#",
                            ["pin_encrypted"]  = "BASE64_ENCRYPTED_PIN_FROM_DEBUG_ENDPOINT"
                        }
                    },

                    ["UssdSendResponse"] = SchemaObj(
                        ("session_id",    "string",  "Use to poll GET /api/v1/ussd/session/{id}", "ussd-a1b2c3d4e5f6"),
                        ("status",        "string",  "submitted | processing | success | failed",  "submitted"),
                        ("submitted_at",  "string",  "ISO-8601 UTC",                               null),
                        ("is_idempotent", "boolean", "true if transaction_id was already known",   null)
                    ),

                    ["UssdSessionDto"] = SchemaObj(
                        ("session_id",     "string", "Session identifier",                                      "ussd-a1b2c3d4e5f6"),
                        ("transaction_id", "string", "Caller-supplied idempotency key (if provided)",           "txn-20240101-001"),
                        ("sim_id",         "string", "ICCID of the SIM used",                                   null),
                        ("gateway_id",     "string", "Gateway that processed the session",                      "gw-001"),
                        ("status",         "string", "submitted | processing | success | failed | cancelled",   "success"),
                        ("result_message", "string", "Human-readable carrier response",                         "Nạp thành công 50,000 VNĐ"),
                        ("submitted_at",   "string", "ISO-8601 UTC",                                            null),
                        ("completed_at",   "string", "ISO-8601 UTC (null while in progress)",                   null),
                        ("target_phone",   "string", "Phone number that was topped up",                         "0987654321")
                    ),

                    ["GatewayHealthResponse"] = new JObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JObject
                        {
                            ["gateway_id"]    = Prop("string",  "Gateway identifier",     "gw-001"),
                            ["online"]        = Prop("boolean", "true if any SIM is known"),
                            ["sim_statuses"]  = new JObject
                            {
                                ["type"]  = "array",
                                ["items"] = Ref("SimStatusDto")
                            }
                        }
                    },

                    ["SimStatusDto"] = SchemaObj(
                        ("sim_id",          "string",  "ICCID or slot-N",                        null),
                        ("status",          "string",  "available | busy | offline | disabled",  "available"),
                        ("signal_strength", "integer", "0–100",                                  null),
                        ("is_disabled",     "boolean", "Administratively disabled",              null),
                        ("last_activity",   "string",  "ISO-8601 UTC of last USSD",              null)
                    ),

                    ["DebugEncryptPinRequest"] = SchemaObj(
                        ("pin",           "string", "Plain-text PIN to encrypt",                     "123456789012"),
                        ("master_key",    "string", "Base64 PinMasterKey (from App.config or /debug/generate-key)", null),
                        ("ussd_template", "string", "USSD template (default: *103*{PIN}#)",          "*103*{PIN}#")
                    ),

                    ["DebugEncryptPinResponse"] = SchemaObj(
                        ("pin_encrypted", "string", "Pass this to UssdSendRequest.pin_encrypted", null),
                        ("ussd_template", "string", "Pass this to UssdSendRequest.ussd_template", "*103*{PIN}#"),
                        ("enc_version",   "string", "Always 'aes256cbc-hmac-v1'",                 "aes256cbc-hmac-v1"),
                        ("note",          "string", "Reminder not to use in production",          null)
                    ),

                    ["ErrorResponse"] = SchemaObj(
                        ("error", "string", "Human-readable error description", "SIM not found")
                    )
                },

                ["responses"] = new JObject
                {
                    ["NotFound"] = new JObject
                    {
                        ["description"] = "Resource not found",
                        ["content"] = JsonContent(Ref("ErrorResponse"))
                    },
                    ["Conflict"] = new JObject
                    {
                        ["description"] = "SIM busy or disabled",
                        ["content"] = JsonContent(Ref("ErrorResponse"))
                    }
                }
            };
        }

        // ──────────────────── DSL helpers ────────────────────

        private static JObject Tag(string name, string desc) =>
            new JObject { ["name"] = name, ["description"] = desc };

        private static JObject Ref(string name) =>
            new JObject { ["$ref"] = $"#/components/schemas/{name}" };

        private static JObject ArrayOf(JObject schema) =>
            new JObject { ["type"] = "array", ["items"] = schema };

        private static JObject JsonContent(JObject schema) =>
            new JObject { ["application/json"] = new JObject { ["schema"] = schema } };

        private static JObject Prop(string type, string desc, string example = null)
        {
            var p = new JObject { ["type"] = type, ["description"] = desc };
            if (example != null) p["example"] = example;
            return p;
        }

        private static JObject PathParam(string name, string desc, string example = null)
        {
            var p = new JObject
            {
                ["name"]        = name,
                ["in"]          = "path",
                ["required"]    = true,
                ["description"] = desc,
                ["schema"]      = new JObject { ["type"] = "string" }
            };
            if (example != null) p["example"] = example;
            return p;
        }

        private static JObject SchemaObj(params (string name, string type, string desc, string example)[] fields)
        {
            var props = new JObject();
            foreach (var (name, type, desc, example) in fields)
                props[name] = Prop(type, desc, example);
            return new JObject { ["type"] = "object", ["properties"] = props };
        }

        private static JObject Op(
            string operationId,
            string tag,
            string summary,
            string description   = null,
            bool security        = true,
            JArray parameters    = null,
            JObject body         = null,
            JObject response200  = null,
            int statusCode       = 200,
            bool extra404        = false,
            bool extra409        = false,
            string responseMimeType = "application/json")
        {
            var op = new JObject
            {
                ["operationId"] = operationId,
                ["summary"]     = summary,
                ["tags"]        = new JArray(tag)
            };

            if (description != null)
                op["description"] = description;

            if (!security)
                op["security"] = new JArray(); // override global auth

            if (parameters != null)
                op["parameters"] = parameters;

            if (body != null)
                op["requestBody"] = new JObject
                {
                    ["required"] = true,
                    ["content"]  = new JObject
                    {
                        ["application/json"] = new JObject { ["schema"] = body }
                    }
                };

            var responses = new JObject();

            var okDesc = statusCode == 202 ? "Request accepted" : statusCode == 201 ? "Created" : "Success";
            if (response200 != null)
            {
                var mimeType = responseMimeType == "text/plain"
                    ? new JObject { [responseMimeType] = new JObject { ["schema"] = new JObject { ["type"] = "string" } } }
                    : new JObject { [responseMimeType] = new JObject { ["schema"] = response200 } };
                responses[statusCode.ToString()] = new JObject { ["description"] = okDesc, ["content"] = mimeType };
            }
            else
            {
                responses[statusCode.ToString()] = new JObject { ["description"] = okDesc };
            }

            // 200 idempotent response if main code is 201/202
            if (statusCode == 201 && response200 != null)
                responses["200"] = new JObject
                {
                    ["description"] = "Idempotent — resource already exists",
                    ["content"] = JsonContent(response200)
                };
            if (statusCode == 202 && response200 != null)
                responses["200"] = new JObject
                {
                    ["description"] = "Idempotent — already submitted",
                    ["content"] = JsonContent(response200)
                };

            if (extra404) responses["404"] = new JObject { ["$ref"] = "#/components/responses/NotFound" };
            if (extra409) responses["409"] = new JObject { ["$ref"] = "#/components/responses/Conflict" };

            op["responses"] = responses;
            return op;
        }
    }
}
