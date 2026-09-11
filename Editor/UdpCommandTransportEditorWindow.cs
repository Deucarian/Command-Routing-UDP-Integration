using System;
using System.Linq;
using Deucarian.Diagnostics;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Controls = Deucarian.Editor.DeucarianEditorWorkspaceControls;

namespace Deucarian.CommandRouting.UdpIntegration.Editor
{
    public sealed class UdpCommandTransportEditorWindow : EditorWindow
    {
        public const string CanonicalSettingsPath = "Assets/Deucarian/CommandRouting/UdpCommandTransportSettings.asset";
        private DeucarianEditorPageSession navigation;
        public static void Open() => DeucarianEditorWindowPages.ShowStandalone<UdpCommandTransportEditorWindow>(
            "UDP transport", new Vector2(560, 500));
        public static IDeucarianEditorPage CreatePage() => new UdpTransportPage().Page;
        public void CreateGUI()
        {
            navigation?.Dispose();
            navigation = new DeucarianEditorPageSession(this, "udp-home", _ => { });
            navigation.Navigate(DeucarianToolIds.CommandRoutingUdp);
        }
        private void OnDisable() { navigation?.Dispose(); navigation = null; }
    }

    internal sealed class UdpTransportPage : IDisposable
    {
        private readonly DeucarianEditorWorkspace workspace;
        private readonly DeucarianEditorWorkspaceForm scope;
        private readonly VisualElement fields;
        private readonly Label validation, status;
        private readonly VisualElement diagnostics;
        private readonly VisualElement protocolDetails;
        private readonly Button validate, locate;
        private UdpCommandTransportSettings settings;
        private DeucarianEditorSerializedForm serialized;
        private double nextRefresh;
        internal IDeucarianEditorPage Page { get; }

        internal UdpTransportPage()
        {
            var root = new VisualElement();
            workspace = new DeucarianEditorWorkspace(root, Application.productName);
            workspace.Title.text = "UDP transport";
            workspace.Subtitle.text = "Exchange local commands with your app.";
            DeucarianEditorWorkspaceNavigation.Populate(workspace, DeucarianToolIds.CommandRoutingUdp);
            var scroll = Controls.Scroll("udp-transport"); workspace.Content.Add(scroll);
            scope = new DeucarianEditorWorkspaceForm(workspace.Scope);
            scope.Asset("udp-settings", "Settings", typeof(UdpCommandTransportSettings), () => settings,
                value => Select(value as UdpCommandTransportSettings));
            var feature = new DeucarianEditorFeatureSection("udp-local", "Local transport",
                "Your app starts and stops the listener.", "radio-tower");
            scroll.Add(feature.Root);
            fields = new VisualElement(); feature.Details.Add(fields);
            validate = Controls.Button("Validate settings", Validate, true); validate.name = "udp-validate";
            feature.Actions.Add(validate);
            feature.Actions.Add(Controls.IconButton("Test commands", DeucarianEditorIconIds.Send,
                () => DeucarianEditorNavigation.Open(workspace.Root, DeucarianToolIds.CommandRouting)));
            validation = Controls.Label(string.Empty, "dw-note"); feature.Details.Add(validation);
            feature.UseFormLayout();
            feature.Details.Add(Controls.Divider());
            status = Controls.Label(string.Empty, "dw-readonly");
            var liveStatus = Controls.Region("udp-live-status", "dw-inline-status");
            liveStatus.AddToClassList("dw-inline-status-compact");
            liveStatus.Add(Controls.Icon(DeucarianEditorIconIds.Optional)); liveStatus.Add(status); feature.Details.Add(liveStatus);
            var advanced = new DeucarianEditorWorkspaceForm(scroll).Section("Project settings", true);
            advanced.Root.AddToClassList("dw-foldout-panel"); advanced.Root.AddToClassList("dw-foldout-followup");
            protocolDetails = new VisualElement(); advanced.Root.Add(protocolDetails);
            advanced.Action("udp-create", "Create settings", CreateSettings);
            locate = advanced.Action("udp-locate", "Locate settings", () =>
            {
                if (settings != null) { Selection.activeObject = settings; EditorGUIUtility.PingObject(settings); }
            });
            advanced.Action("udp-python", "Copy Python example", () => EditorGUIUtility.systemCopyBuffer = PythonExample(settings));
            advanced.Note(() => "Python~ contains the dependency-free client. No socket is opened by this editor.");
            diagnostics = new VisualElement(); advanced.Root.Add(diagnostics);
            Page = new DeucarianEditorPage(root, activate: _ => Refresh(), update: _ =>
            {
                if (EditorApplication.timeSinceStartup < nextRefresh) return;
                nextRefresh = EditorApplication.timeSinceStartup + 1;
                Refresh();
            }, dispose: Dispose);
            Select(FindSettings());
        }

        private void Select(UdpCommandTransportSettings selected)
        {
            serialized?.Dispose(); serialized = null;
            settings = selected; fields.Clear(); fields.SetEnabled(true); protocolDetails.Clear();
            if (settings == null) fields.Add(Controls.Label("Select settings, or create them below.", "dw-note"));
            else
            {
                serialized = new DeucarianEditorSerializedForm(fields, settings);
                serialized.Property("bindAddress", "Address");
                serialized.Property("port", "Listen port");
                serialized.Property("sendResponses", "Send responses");
                var more = new Foldout { text = "Advanced protocol", value = false }; more.AddToClassList("dw-foldout");
                protocolDetails.Add(more);
                var maximum = serialized.Property("maximumDatagramBytes", "Maximum datagram bytes");
                more.Add(maximum.parent);
                var format = serialized.Property("messageFormat", "Message format");
                more.Add(format);
                bool editable = AssetDatabase.GetAssetPath(settings).Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal);
                fields.SetEnabled(editable); more.SetEnabled(editable);
            }
            Controls.Show(validation, false);
            Refresh();
        }

        private void Validate()
        {
            string issue = UdpCommandTransportSettingsValidation.Validate(settings);
            validation.text = string.IsNullOrEmpty(issue) ? "Settings are valid." : issue;
            Controls.Show(validation, true);
        }
        private void Refresh()
        {
            if (settings == null && serialized != null) { Select(null); return; }
            scope.Refresh(); validate.SetEnabled(settings != null); locate.SetEnabled(settings != null);
            var providers = DiagnosticProviderRegistry.SnapshotProviders().Where(value =>
                value?.ProviderId?.StartsWith("deucarian.command-routing.udp.", StringComparison.Ordinal) == true);
            var sections = DiagnosticReportBuilder.BuildFrom(providers).Sections;
            status.text = sections.Count == 0 ? "No live transport" : sections.Count + " registered transport(s)";
            diagnostics.Clear();
            foreach (var section in sections)
            {
                var form = new DeucarianEditorWorkspaceForm(diagnostics).Section(section.Title, true);
                foreach (var item in section.Items) form.ReadOnly(null, item.Label, () => item.Value);
            }
        }
        internal static UdpCommandTransportSettings FindSettings()
        {
            var canonical = AssetDatabase.LoadAssetAtPath<UdpCommandTransportSettings>(UdpCommandTransportEditorWindow.CanonicalSettingsPath);
            if (canonical != null) return canonical;
            string guid = AssetDatabase.FindAssets("t:UdpCommandTransportSettings").OrderBy(value => value, StringComparer.Ordinal).FirstOrDefault();
            return guid == null ? null : AssetDatabase.LoadAssetAtPath<UdpCommandTransportSettings>(AssetDatabase.GUIDToAssetPath(guid));
        }
        private void CreateSettings()
        {
            const string folder = "Assets/Deucarian/CommandRouting";
            string current = "Assets";
            foreach (string part in folder.Split('/').Skip(1))
            { if (!AssetDatabase.IsValidFolder(current + "/" + part)) AssetDatabase.CreateFolder(current, part); current += "/" + part; }
            var existing = AssetDatabase.LoadAssetAtPath<UdpCommandTransportSettings>(UdpCommandTransportEditorWindow.CanonicalSettingsPath);
            if (existing != null) { Select(existing); return; }
            var created = ScriptableObject.CreateInstance<UdpCommandTransportSettings>();
            AssetDatabase.CreateAsset(created, UdpCommandTransportEditorWindow.CanonicalSettingsPath);
            AssetDatabase.SaveAssetIfDirty(created);
            Select(created); Selection.activeObject = created;
        }
        internal static string PythonExample(UdpCommandTransportSettings selected)
        {
            string address = selected == null ? "127.0.0.1" : selected.BindAddress;
            if (!System.Net.IPAddress.TryParse(address, out _) || address == "0.0.0.0" || address == "::") address = "127.0.0.1";
            int port = selected != null ? selected.Port : UdpCommandTransportSettings.DefaultPort;
            return "from deucarian_udp_commands import UdpCommandClient\n\nwith UdpCommandClient(\"" + address + "\", " + port +
                ") as client:\n    result = client.send(\"example_command\", {})\n    print(result)\n";
        }
        public void Dispose() { serialized?.Dispose(); serialized = null; workspace.Dispose(); }
    }
}
