using System.Threading;
using Deucarian.Diagnostics;

namespace Deucarian.CommandRouting.UdpIntegration
{
    internal sealed class UdpCommandTransportDiagnostics :
        IDiagnosticProvider
    {
        private readonly string providerId;
        private readonly UdpCommandTransportOptions options;
        private long receivedCount;
        private long sentCount;
        private long droppedCount;
        private string lastErrorType = string.Empty;
        private int running;

        public UdpCommandTransportDiagnostics(
            string instanceId,
            UdpCommandTransportOptions transportOptions)
        {
            providerId =
                "deucarian.command-routing.udp." +
                instanceId;
            options = transportOptions;
        }

        public string ProviderId => providerId;

        public string DisplayName =>
            "UDP Command Transport";

        public void SetRunning(bool value)
        {
            Interlocked.Exchange(
                ref running,
                value ? 1 : 0);
        }

        public void RecordReceived()
        {
            Interlocked.Increment(ref receivedCount);
        }

        public void RecordSent()
        {
            Interlocked.Increment(ref sentCount);
        }

        public void RecordDropped()
        {
            Interlocked.Increment(ref droppedCount);
        }

        public void RecordError(string errorType)
        {
            Interlocked.Exchange(
                ref lastErrorType,
                errorType ?? string.Empty);
        }

        public void Collect(DiagnosticReportBuilder builder)
        {
            bool isRunning =
                Interlocked.CompareExchange(
                    ref running,
                    0,
                    0) == 1;
            long dropped =
                Interlocked.Read(ref droppedCount);
            string error =
                Interlocked.CompareExchange(
                    ref lastErrorType,
                    null,
                    null) ??
                string.Empty;
            DiagnosticSeverity health =
                error.Length > 0
                    ? DiagnosticSeverity.Warning
                    : isRunning
                        ? DiagnosticSeverity.Success
                        : DiagnosticSeverity.Info;

            DiagnosticSection section =
                builder.AddSection(
                    ProviderId,
                    DisplayName);
            section.AddItem(
                "status",
                "Status",
                isRunning ? "Listening" : "Stopped",
                health);
            section.AddItem(
                "endpoint",
                "Bind Endpoint",
                options.BindAddress + ":" + options.Port,
                DiagnosticSeverity.Info);
            section.AddItem(
                "received_count",
                "Datagrams Received",
                Interlocked.Read(ref receivedCount).ToString(),
                DiagnosticSeverity.Success);
            section.AddItem(
                "sent_count",
                "Datagrams Sent",
                Interlocked.Read(ref sentCount).ToString(),
                DiagnosticSeverity.Success);
            section.AddItem(
                "dropped_count",
                "Datagrams Dropped",
                dropped.ToString(),
                dropped > 0
                    ? DiagnosticSeverity.Warning
                    : DiagnosticSeverity.Success);
            if (error.Length > 0)
            {
                section.AddItem(
                    "last_error",
                    "Last Error Type",
                    error,
                    DiagnosticSeverity.Warning);
            }
        }
    }
}
