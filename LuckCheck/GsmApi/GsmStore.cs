using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace LuckCheck.GsmApi
{
    // SQLite persistence for: reservations (TTL), sessions, disabled SIMs, webhook outbox.
    // One connection per operation + WAL mode — safe for concurrent WinForms threads at this scale.
    public class GsmStore : IDisposable
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _connStr;

        public GsmStore(string dbPath = null)
        {
            string path = dbPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gsm_store.db");
            _connStr = $"Data Source={path};Version=3;";
            EnsureSchema();
        }

        private SQLiteConnection Open()
        {
            var conn = new SQLiteConnection(_connStr);
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA journal_mode=WAL; PRAGMA synchronous=NORMAL; PRAGMA foreign_keys=ON;";
                cmd.ExecuteNonQuery();
            }
            return conn;
        }

        private void EnsureSchema()
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    CREATE TABLE IF NOT EXISTS sim_reservations (
                        reservation_id TEXT PRIMARY KEY,
                        sim_id         TEXT NOT NULL,
                        gateway_id     TEXT NOT NULL,
                        slot_index     INTEGER NOT NULL DEFAULT 0,
                        created_at     TEXT NOT NULL,
                        expires_at     TEXT NOT NULL,
                        released       INTEGER NOT NULL DEFAULT 0
                    );
                    CREATE INDEX IF NOT EXISTS idx_res_sim ON sim_reservations(sim_id, released);
                    CREATE INDEX IF NOT EXISTS idx_res_exp ON sim_reservations(expires_at, released);

                    CREATE TABLE IF NOT EXISTS ussd_sessions (
                        session_id     TEXT PRIMARY KEY,
                        transaction_id TEXT,
                        sim_id         TEXT,
                        gateway_id     TEXT,
                        slot_index     INTEGER NOT NULL DEFAULT 0,
                        port_name      TEXT,
                        status         TEXT NOT NULL,
                        result_message TEXT,
                        submitted_at   TEXT NOT NULL,
                        completed_at   TEXT,
                        target_phone   TEXT,
                        sim_msisdn     TEXT
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS idx_sess_txn ON ussd_sessions(transaction_id)
                        WHERE transaction_id IS NOT NULL;
                    CREATE INDEX IF NOT EXISTS idx_sess_status ON ussd_sessions(status);

                    CREATE TABLE IF NOT EXISTS sim_disabled (
                        sim_id      TEXT PRIMARY KEY,
                        gateway_id  TEXT,
                        reason      TEXT,
                        disabled_at TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS webhook_outbox (
                        id           INTEGER PRIMARY KEY AUTOINCREMENT,
                        event_type   TEXT NOT NULL,
                        payload      TEXT NOT NULL,
                        created_at   TEXT NOT NULL,
                        next_attempt TEXT NOT NULL,
                        attempt      INTEGER NOT NULL DEFAULT 0,
                        delivered    INTEGER NOT NULL DEFAULT 0,
                        last_error   TEXT
                    );
                    CREATE INDEX IF NOT EXISTS idx_wb_pending ON webhook_outbox(delivered, next_attempt);
                ";
                cmd.ExecuteNonQuery();
            }
        }

        // ── Reservations ──────────────────────────────────────────────

        public SimReservationRecord GetReservationById(string reservationId)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT reservation_id, sim_id, gateway_id, slot_index, created_at, expires_at
                    FROM sim_reservations WHERE reservation_id=@id";
                cmd.Parameters.AddWithValue("@id", reservationId);
                using (var r = cmd.ExecuteReader())
                    if (r.Read()) return ReadReservation(r);
            }
            return null;
        }

        public void InsertReservation(SimReservationRecord rec)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO sim_reservations
                        (reservation_id, sim_id, gateway_id, slot_index, created_at, expires_at, released)
                    VALUES (@id, @sim, @gw, @slot, @ca, @ea, 0)";
                cmd.Parameters.AddWithValue("@id",   rec.ReservationId);
                cmd.Parameters.AddWithValue("@sim",  rec.SimId);
                cmd.Parameters.AddWithValue("@gw",   rec.GatewayId);
                cmd.Parameters.AddWithValue("@slot",  rec.SlotIndex);
                cmd.Parameters.AddWithValue("@ca",   rec.CreatedAt.ToString("o"));
                cmd.Parameters.AddWithValue("@ea",   rec.ExpiresAt.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        public bool ReleaseReservation(string reservationId)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE sim_reservations SET released=1 WHERE reservation_id=@id";
                cmd.Parameters.AddWithValue("@id", reservationId);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        // Returns the expired records THEN marks them released atomically.
        public List<SimReservationRecord> ExpireAndReturnExpired()
        {
            var list = new List<SimReservationRecord>();
            using (var conn = Open())
            {
                using (var sel = conn.CreateCommand())
                {
                    sel.CommandText = @"
                        SELECT reservation_id, sim_id, gateway_id, slot_index, created_at, expires_at
                        FROM sim_reservations WHERE released=0 AND expires_at <= @now";
                    sel.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
                    using (var r = sel.ExecuteReader())
                        while (r.Read()) list.Add(ReadReservation(r));
                }
                if (list.Count > 0)
                {
                    using (var upd = conn.CreateCommand())
                    {
                        upd.CommandText = "UPDATE sim_reservations SET released=1 WHERE released=0 AND expires_at <= @now";
                        upd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
                        int n = upd.ExecuteNonQuery();
                        if (n > 0) logger.Info($"[GsmStore] TTL expired {n} reservation(s)");
                    }
                }
            }
            return list;
        }

        private static SimReservationRecord ReadReservation(SQLiteDataReader r) => new SimReservationRecord
        {
            ReservationId = r.GetString(0),
            SimId         = r.GetString(1),
            GatewayId     = r.GetString(2),
            SlotIndex     = r.GetInt32(3),
            CreatedAt     = DateTime.Parse(r.GetString(4)),
            ExpiresAt     = DateTime.Parse(r.GetString(5))
        };

        // ── Sessions ──────────────────────────────────────────────────

        public string FindSessionByTransactionId(string transactionId)
        {
            if (string.IsNullOrEmpty(transactionId)) return null;
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT session_id FROM ussd_sessions WHERE transaction_id=@txn LIMIT 1";
                cmd.Parameters.AddWithValue("@txn", transactionId);
                return cmd.ExecuteScalar() as string;
            }
        }

        public void UpsertSession(UssdSession s)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO ussd_sessions
                        (session_id, transaction_id, sim_id, gateway_id, slot_index, port_name,
                         status, result_message, submitted_at, completed_at, target_phone, sim_msisdn)
                    VALUES
                        (@sid, @txn, @sim, @gw, @slot, @port,
                         @status, @msg, @sub, @comp, @phone, @msisdn)";
                cmd.Parameters.AddWithValue("@sid",    s.SessionId);
                cmd.Parameters.AddWithValue("@txn",    (object)s.TransactionId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sim",    (object)s.SimId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gw",     (object)s.GatewayId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@slot",   s.SlotIndex);
                cmd.Parameters.AddWithValue("@port",   (object)s.PortName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@status", s.Status);
                cmd.Parameters.AddWithValue("@msg",    (object)s.ResultMessage ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sub",    s.SubmittedAt.ToString("o"));
                cmd.Parameters.AddWithValue("@comp",   s.CompletedAt.HasValue ? (object)s.CompletedAt.Value.ToString("o") : DBNull.Value);
                cmd.Parameters.AddWithValue("@phone",  (object)s.TargetPhone ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@msisdn", (object)s.SimMsisdn ?? DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        public UssdSessionRecord GetSession(string sessionId)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT session_id, transaction_id, sim_id, gateway_id, slot_index, port_name,
                           status, result_message, submitted_at, completed_at, target_phone, sim_msisdn
                    FROM ussd_sessions WHERE session_id=@id";
                cmd.Parameters.AddWithValue("@id", sessionId);
                using (var r = cmd.ExecuteReader())
                    if (r.Read()) return ReadSession(r);
            }
            return null;
        }

        public List<UssdSessionRecord> GetIncompleteSessions()
        {
            var list = new List<UssdSessionRecord>();
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT session_id, transaction_id, sim_id, gateway_id, slot_index, port_name,
                           status, result_message, submitted_at, completed_at, target_phone, sim_msisdn
                    FROM ussd_sessions WHERE status NOT IN ('success','failed','cancelled')";
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(ReadSession(r));
            }
            return list;
        }

        private static UssdSessionRecord ReadSession(SQLiteDataReader r) => new UssdSessionRecord
        {
            SessionId     = r.GetString(0),
            TransactionId = r.IsDBNull(1) ? null : r.GetString(1),
            SimId         = r.IsDBNull(2) ? null : r.GetString(2),
            GatewayId     = r.IsDBNull(3) ? null : r.GetString(3),
            SlotIndex     = r.GetInt32(4),
            PortName      = r.IsDBNull(5) ? null : r.GetString(5),
            Status        = r.GetString(6),
            ResultMessage = r.IsDBNull(7) ? null : r.GetString(7),
            SubmittedAt   = DateTime.Parse(r.GetString(8)),
            CompletedAt   = r.IsDBNull(9) ? (DateTime?)null : DateTime.Parse(r.GetString(9)),
            TargetPhone   = r.IsDBNull(10) ? null : r.GetString(10),
            SimMsisdn     = r.IsDBNull(11) ? null : r.GetString(11)
        };

        // ── Disabled SIMs ────────────────────────────────────────────

        public void DisableSim(string simId, string gatewayId, string reason)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT OR REPLACE INTO sim_disabled (sim_id, gateway_id, reason, disabled_at)
                    VALUES (@sim, @gw, @reason, @ts)";
                cmd.Parameters.AddWithValue("@sim",    simId);
                cmd.Parameters.AddWithValue("@gw",     (object)gatewayId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@reason", (object)reason ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ts",     DateTime.UtcNow.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        public bool EnableSim(string simId)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "DELETE FROM sim_disabled WHERE sim_id=@sim";
                cmd.Parameters.AddWithValue("@sim", simId);
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool IsSimDisabled(string simId)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT 1 FROM sim_disabled WHERE sim_id=@sim LIMIT 1";
                cmd.Parameters.AddWithValue("@sim", simId);
                return cmd.ExecuteScalar() != null;
            }
        }

        public List<string> GetAllDisabledSimIds()
        {
            var list = new List<string>();
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT sim_id FROM sim_disabled";
                using (var r = cmd.ExecuteReader())
                    while (r.Read()) list.Add(r.GetString(0));
            }
            return list;
        }

        // ── Webhook outbox ────────────────────────────────────────────

        public void EnqueueWebhook(string eventType, string payload)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    INSERT INTO webhook_outbox (event_type, payload, created_at, next_attempt, attempt, delivered)
                    VALUES (@et, @payload, @now, @now, 0, 0)";
                cmd.Parameters.AddWithValue("@et",      eventType);
                cmd.Parameters.AddWithValue("@payload", payload);
                cmd.Parameters.AddWithValue("@now",     DateTime.UtcNow.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        public List<WebhookOutboxRecord> GetPendingWebhooks(int limit = 50)
        {
            var list = new List<WebhookOutboxRecord>();
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    SELECT id, event_type, payload, attempt, next_attempt
                    FROM webhook_outbox
                    WHERE delivered=0 AND next_attempt <= @now
                    ORDER BY id ASC LIMIT @limit";
                cmd.Parameters.AddWithValue("@now",   DateTime.UtcNow.ToString("o"));
                cmd.Parameters.AddWithValue("@limit", limit);
                using (var r = cmd.ExecuteReader())
                    while (r.Read())
                        list.Add(new WebhookOutboxRecord
                        {
                            Id          = r.GetInt64(0),
                            EventType   = r.GetString(1),
                            Payload     = r.GetString(2),
                            Attempt     = r.GetInt32(3),
                            NextAttempt = DateTime.Parse(r.GetString(4))
                        });
            }
            return list;
        }

        public void MarkWebhookDelivered(long id)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE webhook_outbox SET delivered=1 WHERE id=@id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public void ScheduleWebhookRetry(long id, int attempt, DateTime nextAttempt, string lastError)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
                    UPDATE webhook_outbox SET attempt=@attempt, next_attempt=@next, last_error=@err
                    WHERE id=@id";
                cmd.Parameters.AddWithValue("@attempt", attempt);
                cmd.Parameters.AddWithValue("@next",    nextAttempt.ToString("o"));
                cmd.Parameters.AddWithValue("@err",     (object)lastError ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id",      id);
                cmd.ExecuteNonQuery();
            }
        }

        public void DeadLetterWebhook(long id, string lastError)
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE webhook_outbox SET delivered=2, last_error=@err WHERE id=@id";
                cmd.Parameters.AddWithValue("@err", (object)lastError ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@id",  id);
                cmd.ExecuteNonQuery();
            }
        }

        public void Dispose() { }
    }

    // ── Value objects ─────────────────────────────────────────────────

    public class SimReservationRecord
    {
        public string   ReservationId { get; set; }
        public string   SimId         { get; set; }
        public string   GatewayId     { get; set; }
        public int      SlotIndex     { get; set; }
        public DateTime CreatedAt     { get; set; }
        public DateTime ExpiresAt     { get; set; }
    }

    public class UssdSessionRecord
    {
        public string    SessionId     { get; set; }
        public string    TransactionId { get; set; }
        public string    SimId         { get; set; }
        public string    GatewayId     { get; set; }
        public int       SlotIndex     { get; set; }
        public string    PortName      { get; set; }
        public string    Status        { get; set; }
        public string    ResultMessage { get; set; }
        public DateTime  SubmittedAt   { get; set; }
        public DateTime? CompletedAt   { get; set; }
        public string    TargetPhone   { get; set; }
        public string    SimMsisdn     { get; set; }
    }

    public class WebhookOutboxRecord
    {
        public long     Id          { get; set; }
        public string   EventType   { get; set; }
        public string   Payload     { get; set; }
        public int      Attempt     { get; set; }
        public DateTime NextAttempt { get; set; }
    }
}
