using System;
using System.Collections.Generic;
using Deucarian.Diagnostics;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.CommandRouting.UdpIntegration.Editor
{
    [InitializeOnLoad]
    internal static class UdpCommandTransportControlCenterRegistration
    {
        private const string PackageId =
            "com.deucarian.command-routing.udp-integration";
        private static readonly IDisposable ToolRegistration;
        private static readonly IDisposable CardRegistration;

        static UdpCommandTransportControlCenterRegistration()
        {
            ToolRegistration = DeucarianToolRegistry.Register(
                new DeucarianToolDescriptor(
                    DeucarianToolIds.CommandRoutingUdp,
                    "UDP Command Transport",
                    "Configure UDP transport and local interoperability tools.",
                    DeucarianControlCenterArea.Communication,
                    UdpCommandTransportEditorWindow.Open,
                    PackageId,
                    searchTerms: new[] { "udp", "command", "transport", "python" },
                    order: 120));

            CardRegistration = DeucarianControlCenterRegistry.RegisterCardProvider(
                new UdpTransportCardProvider());
        }

        private sealed class UdpTransportCardProvider :
            IDeucarianControlCenterCardProvider
        {
            public string Id => PackageId + ".control-center";

            public IEnumerable<DeucarianControlCenterCard> Capture(
                DeucarianControlCenterContext context)
            {
                UdpCommandTransportSettings settings =
                    AssetDatabase.LoadAssetAtPath<UdpCommandTransportSettings>(
                        UdpCommandTransportEditorWindow
                            .CanonicalSettingsPath);
                string validation =
                    UdpCommandTransportSettingsValidation
                        .Validate(settings);
                bool configured = string.IsNullOrEmpty(validation);
                DiagnosticSummary diagnostics = CaptureDiagnostics();

                return new[]
                {
                    new DeucarianControlCenterCard(
                        PackageId + ".setup",
                        DeucarianControlCenterArea.Communication,
                        "UDP Command Transport",
                        "Local settings and sanitized UDP runtime diagnostics.",
                        PackageId,
                        ResolveStatus(configured, diagnostics.Severity),
                        !configured
                            ? "Setup required"
                            : diagnostics.SectionCount == 0
                                ? "Configured; no live transport"
                                : diagnostics.SectionCount +
                                  " live transport(s)",
                        order: 120,
                        details: new[]
                        {
                            configured
                                ? "Settings asset: configured"
                                : validation,
                            diagnostics.SectionCount == 0
                                ? "Live diagnostics: no active UDP transport"
                                : "Live diagnostics: " + diagnostics.Severity +
                                  " across " + diagnostics.SectionCount +
                                  " transport(s)"
                        },
                        actions: new[]
                        {
                            new DeucarianControlCenterAction(
                                PackageId + ".open",
                                "Open UDP Transport",
                                UdpCommandTransportEditorWindow.Open)
                        },
                        searchTerms: new[]
                        {
                            "udp", "command", "transport", "diagnostics", "live"
                        })
                };
            }

            private static DiagnosticSummary CaptureDiagnostics()
            {
                List<IDiagnosticProvider> providers =
                    new List<IDiagnosticProvider>();
                foreach (IDiagnosticProvider provider in
                    DiagnosticProviderRegistry.SnapshotProviders())
                {
                    if (provider != null &&
                        !string.IsNullOrEmpty(provider.ProviderId) &&
                        provider.ProviderId.StartsWith(
                            "deucarian.command-routing.udp.",
                            StringComparison.Ordinal))
                    {
                        providers.Add(provider);
                    }
                }

                DiagnosticReport report =
                    DiagnosticReportBuilder.BuildFrom(providers);
                int sectionCount = 0;
                DiagnosticSeverity severity = DiagnosticSeverity.Info;
                foreach (DiagnosticSection section in report.Sections)
                {
                    if (string.IsNullOrEmpty(section?.Id) ||
                        !section.Id.StartsWith(
                            "deucarian.command-routing.udp.",
                            StringComparison.Ordinal))
                    {
                        continue;
                    }

                    sectionCount++;
                    if (section.Severity > severity)
                    {
                        severity = section.Severity;
                    }
                }

                return new DiagnosticSummary(sectionCount, severity);
            }

            private static DeucarianControlCenterStatus ResolveStatus(
                bool configured,
                DiagnosticSeverity severity)
            {
                if (severity == DiagnosticSeverity.Error)
                {
                    return DeucarianControlCenterStatus.Error;
                }

                if (!configured || severity == DiagnosticSeverity.Warning)
                {
                    return DeucarianControlCenterStatus.Warning;
                }

                return severity == DiagnosticSeverity.Success
                    ? DeucarianControlCenterStatus.Success
                    : DeucarianControlCenterStatus.Info;
            }
        }

        private readonly struct DiagnosticSummary
        {
            internal DiagnosticSummary(
                int sectionCount,
                DiagnosticSeverity severity)
            {
                SectionCount = sectionCount;
                Severity = severity;
            }

            internal int SectionCount { get; }
            internal DiagnosticSeverity Severity { get; }
        }
    }
}