using System.Collections;
using System.Linq;
using Deucarian.Diagnostics;
using Deucarian.Editor;
using NUnit.Framework;
using UnityEditor.UIElements;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Deucarian.CommandRouting.UdpIntegration.Tests
{
    public sealed class ControlCenterRegistrationTests
    {
        [UnityTest]
        public IEnumerator AdvancedProtocolKeepsSourceOwnershipAndClearsDestroyedSettings()
        {
            Assert.That(DeucarianToolRegistry.TryGet(DeucarianToolIds.CommandRoutingUdp, out var tool), Is.True);
            var settings = ScriptableObject.CreateInstance<UdpCommandTransportSettings>();
            var window = ScriptableObject.CreateInstance<TransportTestWindow>(); window.Show();
            try
            {
                using (var page = tool.CreatePage())
                {
                    window.rootVisualElement.Add(page.Root); yield return null;
                    page.Root.Q<ObjectField>("udp-settings").value = settings;
                    var project = page.Root.Query<Foldout>().ToList().Single(value => value.text == "Project settings");
                    var protocol = project.Query<Foldout>().ToList().Single(value => value.text == "Advanced protocol");
                    Assert.That(protocol.enabledInHierarchy, Is.False, "Only project-owned settings can be edited.");
                    Object.DestroyImmediate(settings);
                    Assert.DoesNotThrow(() => page.Activate(null));
                    Assert.That(project.Query<Foldout>().ToList().Any(value => value.text == "Advanced protocol"), Is.False);
                }
            }
            finally { window.Close(); if (settings != null) Object.DestroyImmediate(settings); }
        }

        private sealed class TransportTestWindow : EditorWindow { }

        [Test]
        public void OpeningTheNativeTransportPageDoesNotStartASocket()
        {
            int before = DiagnosticProviderRegistry.SnapshotProviders().Count;
            Assert.That(DeucarianToolRegistry.TryGet(DeucarianToolIds.CommandRoutingUdp, out var tool), Is.True);
            using (var page = tool.CreatePage())
            {
                Assert.That(page.Root.Query<IMGUIContainer>().ToList(), Is.Empty);
                Assert.That(page.Root.Q<Button>("udp-validate"), Is.Not.Null);
                Assert.That(DiagnosticProviderRegistry.SnapshotProviders().Count, Is.EqualTo(before));
            }
        }

        private const string PackageId =
            "com.deucarian.command-routing.udp-integration";

        [Test]
        public void PackageRegistersStableToolAndCard()
        {
            Assert.That(
                DeucarianToolRegistry.TryGet(
                    DeucarianToolIds.CommandRoutingUdp,
                    out DeucarianToolDescriptor tool),
                Is.True);
            Assert.That(tool.OwningPackage, Is.EqualTo(PackageId));

            DeucarianControlCenterSnapshot snapshot =
                DeucarianControlCenterSnapshotBuilder.Capture(true);
            Assert.That(
                snapshot.Cards.Any(
                    card => card.OwningPackage == PackageId),
                Is.True);
        }

        [Test]
        public void CardIncludesSanitizedRegisteredTransportSeverity()
        {
            using (DiagnosticProviderRegistration registration =
                   DiagnosticProviderRegistry.Register(
                       new ReviewDiagnosticProvider()))
            {
                DeucarianControlCenterCard card =
                    DeucarianControlCenterSnapshotBuilder.Capture(true)
                        .Cards.Single(candidate =>
                            candidate.Id == PackageId + ".setup");

                Assert.That(
                    card.Status,
                    Is.EqualTo(DeucarianControlCenterStatus.Error));
                Assert.That(
                    card.Details.Any(detail =>
                        detail.StartsWith("Live diagnostics:")),
                    Is.True);
                Assert.That(
                    string.Join(" ", card.Details),
                    Does.Not.Contain("raw-diagnostic-value"));
            }
        }

        private sealed class ReviewDiagnosticProvider : IDiagnosticProvider
        {
            public string ProviderId =>
                "deucarian.command-routing.udp.review";
            public string DisplayName => "Review UDP";

            public void Collect(DiagnosticReportBuilder builder)
            {
                builder.AddSection(ProviderId, DisplayName)
                    .AddItem(
                        "state",
                        "State",
                        "raw-diagnostic-value",
                        DiagnosticSeverity.Error);
            }
        }
    }
}
