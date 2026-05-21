using System;
using System.Text;
using System.Threading;

namespace LuckCheck.GsmApi
{
    // Thread-safe Prometheus metrics. Counters use Interlocked; gauges use volatile int.
    // Rendered as Prometheus text exposition format (v0.0.4).
    public class PrometheusMetrics
    {
        // Counters (monotonically increasing)
        private long _ussdTotal;
        private long _ussdSuccess;
        private long _ussdFailed;
        private long _ussdCancelled;
        private long _webhookDeliveries;
        private long _webhookRetries;
        private long _webhookDeadLetters;

        // Gauges (snapshotted during HandleListSims / HandleGatewayHealth)
        public volatile int SimsAvailable;
        public volatile int SimsBusy;
        public volatile int SimsOffline;
        public volatile int SimsDisabled;

        private static readonly long _startTicks = DateTime.UtcNow.Ticks;

        public void IncrUssdTotal()         => Interlocked.Increment(ref _ussdTotal);
        public void IncrUssdSuccess()       => Interlocked.Increment(ref _ussdSuccess);
        public void IncrUssdFailed()        => Interlocked.Increment(ref _ussdFailed);
        public void IncrUssdCancelled()     => Interlocked.Increment(ref _ussdCancelled);
        public void IncrWebhookDelivery()   => Interlocked.Increment(ref _webhookDeliveries);
        public void IncrWebhookRetry()      => Interlocked.Increment(ref _webhookRetries);
        public void IncrWebhookDeadLetter() => Interlocked.Increment(ref _webhookDeadLetters);

        public string Render()
        {
            double uptimeSecs = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - _startTicks).TotalSeconds;
            var sb = new StringBuilder(1024);

            Gauge(sb, "gsm_ussd_requests_total",    "counter", "Total USSD requests submitted",              Interlocked.Read(ref _ussdTotal));
            Gauge(sb, "gsm_ussd_success_total",     "counter", "USSD sessions completed SUCCESS",            Interlocked.Read(ref _ussdSuccess));
            Gauge(sb, "gsm_ussd_failed_total",      "counter", "USSD sessions completed FAILED or timeout",  Interlocked.Read(ref _ussdFailed));
            Gauge(sb, "gsm_ussd_cancelled_total",   "counter", "USSD sessions cancelled by caller",          Interlocked.Read(ref _ussdCancelled));
            Gauge(sb, "gsm_webhook_deliveries_total",  "counter", "Webhook deliveries succeeded",            Interlocked.Read(ref _webhookDeliveries));
            Gauge(sb, "gsm_webhook_retries_total",     "counter", "Webhook retry attempts",                  Interlocked.Read(ref _webhookRetries));
            Gauge(sb, "gsm_webhook_deadletters_total", "counter", "Webhooks exhausted all retries",          Interlocked.Read(ref _webhookDeadLetters));

            Gauge(sb, "gsm_sims_available", "gauge", "SIMs currently available",              SimsAvailable);
            Gauge(sb, "gsm_sims_busy",      "gauge", "SIMs currently busy (in transaction)",  SimsBusy);
            Gauge(sb, "gsm_sims_offline",   "gauge", "SIMs currently offline",                SimsOffline);
            Gauge(sb, "gsm_sims_disabled",  "gauge", "SIMs administratively disabled",        SimsDisabled);

            sb.AppendLine("# HELP gsm_uptime_seconds Seconds since process start");
            sb.AppendLine("# TYPE gsm_uptime_seconds gauge");
            sb.AppendLine($"gsm_uptime_seconds {uptimeSecs:F1}");

            return sb.ToString();
        }

        private static void Gauge(StringBuilder sb, string name, string type, string help, long value)
        {
            sb.AppendLine($"# HELP {name} {help}");
            sb.AppendLine($"# TYPE {name} {type}");
            sb.AppendLine($"{name} {value}");
        }
    }
}
