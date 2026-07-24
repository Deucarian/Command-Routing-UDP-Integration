using System.Collections.Generic;
using Deucarian.Diagnostics;
using Deucarian.Editor;
using UnityEditor;
using UnityEngine;

namespace Deucarian.CommandRouting.UdpIntegration.Editor
{
    public sealed class UdpCommandTransportEditorWindow :
        EditorWindow
    {
        public const string MenuPath =
            "Tools/Deucarian/Communication/UDP Command Transport";
        public const string CanonicalSettingsPath =
            "Assets/Deucarian/CommandRouting/" +
            "UdpCommandTransportSettings.asset";

        private static readonly string[] Tabs =
        {
            "Overview",
            "Settings",
            "Python",
            "Diagnostics"
        };

        private UdpCommandTransportSettings settings;
        private SerializedObject serializedSettings;
        private Vector2 scrollPosition;
        private int selectedTab;

        [MenuItem(MenuPath, priority = 320)]
        public static void Open()
        {
            var window =
                GetWindow<
                    UdpCommandTransportEditorWindow>(
                    "UDP Commands");
            window.minSize = new Vector2(560f, 500f);
            window.Show();
        }

        private void OnEnable()
        {
            SelectSettings(FindPreferredSettings());
        }

        private void OnSelectionChange()
        {
            if (Selection.activeObject is
                UdpCommandTransportSettings selected)
            {
                SelectSettings(selected);
                Repaint();
            }
        }

        private void OnGUI()
        {
            using (DeucarianEditorWorkbenchPanelScope page =
                   DeucarianEditorWorkbenchGUI
                       .BeginSettingsPage(
                           GUILayout.ExpandHeight(true)))
            {
                scrollPosition =
                    EditorGUILayout.BeginScrollView(
                        scrollPosition);
                DeucarianEditorChrome.DrawPackageHeader(
                    "network",
                    "UDP Command Transport",
                    "Configure UDP and Python command interoperability.");
                selectedTab =
                    GUILayout.Toolbar(selectedTab, Tabs);
                GUILayout.Space(
                    DeucarianEditorWorkbenchGUI.PanelSpacing);

                switch (selectedTab)
                {
                    case 1:
                        DrawSettings();
                        break;
                    case 2:
                        DrawPython();
                        break;
                    case 3:
                        DrawDiagnostics();
                        break;
                    default:
                        DrawOverview();
                        break;
                }

                DeucarianEditorChrome.DrawFooterVersion(
                    "com.deucarian.command-routing.udp-integration",
                    "0.1.0");
                EditorGUILayout.EndScrollView();
            }
        }

        private void DrawOverview()
        {
            DeucarianEditorChrome.DrawSectionHeader(
                "Project Configuration");
            DeucarianEditorChrome.BeginSection();
            UdpCommandTransportSettings selected =
                (UdpCommandTransportSettings)
                EditorGUILayout.ObjectField(
                    "Settings Asset",
                    settings,
                    typeof(UdpCommandTransportSettings),
                    false);
            if (selected != settings)
            {
                SelectSettings(selected);
            }

            string validation =
                UdpCommandTransportSettingsValidation
                    .Validate(settings);
            bool valid =
                string.IsNullOrEmpty(validation);
            DeucarianEditorWorkbenchGUI.DrawStatusIconRow(
                valid ? "circle-check" : "circle-alert",
                valid
                    ? "UDP transport settings are valid."
                    : validation,
                valid
                    ? DeucarianEditorStatus.Success
                    : DeucarianEditorStatus.Warning);
            GUILayout.Space(
                DeucarianEditorWorkbenchGUI.PanelSpacing);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(
                        "Create Project Settings",
                        DeucarianEditorWorkbenchGUI
                            .PrimaryButtonStyle))
                {
                    SelectSettings(
                        CreateProjectSettings());
                }

                using (new EditorGUI.DisabledScope(
                           settings == null))
                {
                    if (GUILayout.Button(
                            "Ping Active Asset",
                            DeucarianEditorWorkbenchGUI
                                .SecondaryButtonStyle))
                    {
                        Selection.activeObject = settings;
                        EditorGUIUtility.PingObject(settings);
                    }
                }
            }

            DeucarianEditorChrome.EndSection();

            DeucarianEditorChrome.DrawSectionHeader(
                "Composition");
            DeucarianEditorChrome.BeginSection();
            EditorGUILayout.LabelField(
                "UdpCommandRoutingHost composes UDP with the " +
                "transport-independent Command Routing runtime. " +
                "Application handlers remain independent of sockets.",
                EditorStyles.wordWrappedLabel);
            DeucarianEditorChrome.EndSection();
        }

        private void DrawSettings()
        {
            DeucarianEditorChrome.DrawSectionHeader(
                "UDP Listener");
            DeucarianEditorChrome.BeginSection();
            if (settings == null ||
                serializedSettings == null)
            {
                EditorGUILayout.HelpBox(
                    "Create or select a UDP settings asset.",
                    MessageType.Info);
                DeucarianEditorChrome.EndSection();
                return;
            }

            serializedSettings.Update();
            DrawVisibleProperties(serializedSettings);
            if (serializedSettings.ApplyModifiedProperties())
            {
                EditorUtility.SetDirty(settings);
            }

            string validation =
                UdpCommandTransportSettingsValidation
                    .Validate(settings);
            if (!string.IsNullOrEmpty(validation))
            {
                EditorGUILayout.HelpBox(
                    validation,
                    MessageType.Warning);
            }

            DeucarianEditorChrome.EndSection();
        }

        private static void DrawPython()
        {
            DeucarianEditorChrome.DrawSectionHeader(
                "Python Client");
            DeucarianEditorChrome.BeginSection();
            EditorGUILayout.LabelField(
                "The package includes a dependency-free Python client " +
                "under Python~. Use the same host and port configured " +
                "for the Unity listener.",
                EditorStyles.wordWrappedLabel);
            GUILayout.Space(
                DeucarianEditorWorkbenchGUI.PanelSpacing);
            if (GUILayout.Button(
                    "Copy Python Example",
                    DeucarianEditorWorkbenchGUI
                        .PrimaryButtonStyle))
            {
                EditorGUIUtility.systemCopyBuffer =
                    CreatePythonExample();
            }

            DeucarianEditorChrome.EndSection();
        }

        private static void DrawDiagnostics()
        {
            DiagnosticReport report =
                DiagnosticProviderRegistry.BuildReport();
            var matching =
                new List<DiagnosticSection>();
            foreach (DiagnosticSection section
                     in report.Sections)
            {
                if (section.Id.StartsWith(
                        "deucarian.command-routing.udp."))
                {
                    matching.Add(section);
                }
            }

            DeucarianEditorChrome.DrawSectionHeader(
                "Runtime Diagnostics");
            DeucarianEditorChrome.BeginSection();
            if (matching.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No active UDP command transport is registered. " +
                    "Enter Play Mode or construct a host explicitly.",
                    MessageType.Info);
            }
            else
            {
                foreach (DiagnosticSection section
                         in matching)
                {
                    EditorGUILayout.LabelField(
                        section.Title,
                        EditorStyles.boldLabel);
                    foreach (DiagnosticItem item
                             in section.Items)
                    {
                        EditorGUILayout.LabelField(
                            item.Label,
                            item.Value);
                    }
                }
            }

            DeucarianEditorChrome.EndSection();
        }

        private void SelectSettings(
            UdpCommandTransportSettings selected)
        {
            settings = selected;
            serializedSettings =
                settings == null
                    ? null
                    : new SerializedObject(settings);
        }

        private static UdpCommandTransportSettings
            FindPreferredSettings()
        {
            UdpCommandTransportSettings canonical =
                AssetDatabase.LoadAssetAtPath<
                    UdpCommandTransportSettings>(
                    CanonicalSettingsPath);
            if (canonical != null)
            {
                return canonical;
            }

            string[] guids =
                AssetDatabase.FindAssets(
                    "t:UdpCommandTransportSettings");
            return guids.Length == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<
                    UdpCommandTransportSettings>(
                    AssetDatabase.GUIDToAssetPath(
                        guids[0]));
        }

        private static UdpCommandTransportSettings
            CreateProjectSettings()
        {
            EnsureFolder(
                "Assets/Deucarian/CommandRouting");
            UdpCommandTransportSettings existing =
                AssetDatabase.LoadAssetAtPath<
                    UdpCommandTransportSettings>(
                    CanonicalSettingsPath);
            if (existing != null)
            {
                return existing;
            }

            UdpCommandTransportSettings created =
                CreateInstance<
                    UdpCommandTransportSettings>();
            AssetDatabase.CreateAsset(
                created,
                CanonicalSettingsPath);
            AssetDatabase.SaveAssets();
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            return created;
        }

        private static void DrawVisibleProperties(
            SerializedObject serializedObject)
        {
            SerializedProperty property =
                serializedObject.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name != "m_Script")
                {
                    EditorGUILayout.PropertyField(
                        property,
                        true);
                }
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int index = 1;
                 index < parts.Length;
                 index++)
            {
                string next =
                    current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(
                        current,
                        parts[index]);
                }

                current = next;
            }
        }

        private static string CreatePythonExample()
        {
            return
                "from deucarian_udp_commands import " +
                "UdpCommandClient\n\n" +
                "with UdpCommandClient(" +
                "\"127.0.0.1\", 9050) as client:\n" +
                "    result = client.send(" +
                "\"example_command\", {})\n" +
                "    print(result)\n";
        }
    }
}
