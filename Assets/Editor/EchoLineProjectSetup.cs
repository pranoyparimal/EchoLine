using System.IO;
using UnityEditor;
using UnityEngine;

namespace EchoLine.Editor
{
    /// <summary>
    /// One-shot project scaffolding tool.
    /// Menu: EchoLine > Setup Project Structure
    /// Run once immediately after creating a fresh Unity 6 URP project.
    /// Safe to re-run — existing folders and files are never overwritten.
    /// </summary>
    public static class EchoLineProjectSetup
    {
        // ─────────────────────────────────────────────────────────────
        //  Menu entry
        // ─────────────────────────────────────────────────────────────

        [MenuItem("EchoLine/Setup Project Structure", priority = 1)]
        public static void Run()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Echo Line — Project Setup",
                "This will scaffold all folders and .asmdef files under Assets/.\n\n" +
                "Existing files will NOT be overwritten.\n\nContinue?",
                "Create Structure",
                "Cancel");

            if (!confirmed) return;

            CreateFolders();
            CreateAsmDefs();
            CreateGitKeeps();

            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Echo Line — Done",
                "Project structure created successfully.\n\n" +
                "Next step: add your LevelData.cs and LevelManager.cs to Scripts/Core/ and Scripts/Gameplay/.",
                "OK");

            Debug.Log("[EchoLine Setup] Project scaffolding complete.");
        }

        // Validate — grey out the menu item if we detect it has already run
        [MenuItem("EchoLine/Setup Project Structure", validate = true)]
        private static bool ValidateRun()
        {
            // Re-running is safe, so always allow it — just logs a warning if already done
            return true;
        }

        // ─────────────────────────────────────────────────────────────
        //  Folder manifest
        // ─────────────────────────────────────────────────────────────

        private static readonly string[] Folders =
        {
            // Audio
            "Assets/Audio/Music",
            "Assets/Audio/SFX",
            "Assets/Audio/Mixers",

            // Levels
            "Assets/Levels/World1",
            "Assets/Levels/World2",

            // Materials (one per shader)
            "Assets/Materials",

            // Prefabs
            "Assets/Prefabs/Gameplay",
            "Assets/Prefabs/Hazards",
            "Assets/Prefabs/VFX",
            "Assets/Prefabs/UI",

            // Scenes
            "Assets/Scenes",

            // Scripts — mirrors assembly definition boundaries
            "Assets/Scripts/Utilities",
            "Assets/Scripts/Core",
            "Assets/Scripts/Gameplay",
            "Assets/Scripts/Gameplay/Hazards",
            "Assets/Scripts/UI",

            // Shaders (Shader Graph assets)
            "Assets/Shaders",

            // Editor scripts live here (this file included)
            "Assets/Editor",
        };

        // ─────────────────────────────────────────────────────────────
        //  Assembly definition manifest
        //
        //  Dependency graph (read bottom-up):
        //    UI        → Core, Utilities
        //    Gameplay  → Core, Utilities
        //    Core      → Utilities
        //    Utilities → (none)
        // ─────────────────────────────────────────────────────────────

        private static readonly AsmDefData[] AsmDefs =
        {
            new AsmDefData
            {
                Path         = "Assets/Scripts/Utilities/EchoLine.Utilities.asmdef",
                Name         = "EchoLine.Utilities",
                References   = new string[0],
                IncludePlatforms = new string[0],   // all platforms
                AutoReferenced   = false,
            },
            new AsmDefData
            {
                Path         = "Assets/Scripts/Core/EchoLine.Core.asmdef",
                Name         = "EchoLine.Core",
                References   = new[] { "EchoLine.Utilities" },
                IncludePlatforms = new string[0],
                AutoReferenced   = false,
            },
            new AsmDefData
            {
                Path         = "Assets/Scripts/Gameplay/EchoLine.Gameplay.asmdef",
                Name         = "EchoLine.Gameplay",
                References   = new[] { "EchoLine.Core", "EchoLine.Utilities" },
                IncludePlatforms = new string[0],
                AutoReferenced   = false,
            },
            new AsmDefData
            {
                Path         = "Assets/Scripts/UI/EchoLine.UI.asmdef",
                Name         = "EchoLine.UI",
                // UI intentionally does NOT reference Gameplay — use ScriptableObject
                // event channels in Core to bridge Gameplay → UI communication.
                References   = new[] { "EchoLine.Core", "EchoLine.Utilities" },
                IncludePlatforms = new string[0],
                AutoReferenced   = false,
            },
            new AsmDefData
            {
                Path         = "Assets/Editor/EchoLine.Editor.asmdef",
                Name         = "EchoLine.Editor",
                References   = new[] { "EchoLine.Core", "EchoLine.Utilities" },
                IncludePlatforms = new[] { "Editor" },   // Editor-only
                AutoReferenced   = false,
            },
        };

        // ─────────────────────────────────────────────────────────────
        //  Folder creation
        // ─────────────────────────────────────────────────────────────

        private static void CreateFolders()
        {
            int created = 0;
            foreach (string folder in Folders)
            {
                if (Directory.Exists(folder))
                {
                    Debug.Log($"[EchoLine Setup] Folder already exists, skipping: {folder}");
                    continue;
                }

                Directory.CreateDirectory(folder);
                created++;
                Debug.Log($"[EchoLine Setup] Created folder: {folder}");
            }

            Debug.Log($"[EchoLine Setup] Folders done — {created} created, {Folders.Length - created} already existed.");
        }

        // ─────────────────────────────────────────────────────────────
        //  .asmdef creation
        // ─────────────────────────────────────────────────────────────

        private static void CreateAsmDefs()
        {
            foreach (AsmDefData def in AsmDefs)
            {
                if (File.Exists(def.Path))
                {
                    Debug.Log($"[EchoLine Setup] .asmdef already exists, skipping: {def.Path}");
                    continue;
                }

                File.WriteAllText(def.Path, def.ToJson());
                Debug.Log($"[EchoLine Setup] Created .asmdef: {def.Path}");
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  .gitkeep stubs — keeps empty folders tracked by Git
        // ─────────────────────────────────────────────────────────────

        private static void CreateGitKeeps()
        {
            string[] leafFolders =
            {
                "Assets/Audio/Music",
                "Assets/Audio/SFX",
                "Assets/Audio/Mixers",
                "Assets/Levels/World1",
                "Assets/Levels/World2",
                "Assets/Materials",
                "Assets/Prefabs/Gameplay",
                "Assets/Prefabs/Hazards",
                "Assets/Prefabs/VFX",
                "Assets/Prefabs/UI",
                "Assets/Scenes",
                "Assets/Shaders",
                "Assets/Scripts/Gameplay/Hazards",
            };

            foreach (string folder in leafFolders)
            {
                string keepPath = Path.Combine(folder, ".gitkeep");
                if (File.Exists(keepPath)) continue;
                File.WriteAllText(keepPath, "");
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  Data container + JSON serialiser for .asmdef files
        //
        //  Unity's .asmdef format is plain JSON — we write it manually
        //  to avoid a dependency on any JSON library.
        // ─────────────────────────────────────────────────────────────

        private struct AsmDefData
        {
            public string   Path;
            public string   Name;
            public string[] References;
            public string[] IncludePlatforms;
            public bool     AutoReferenced;

            public string ToJson()
            {
                string refs = References.Length == 0
                    ? "[]"
                    : "[\n        \"" + string.Join("\",\n        \"", References) + "\"\n    ]";

                string platforms = IncludePlatforms.Length == 0
                    ? "[]"
                    : "[\n        \"" + string.Join("\",\n        \"", IncludePlatforms) + "\"\n    ]";

                return
$@"{{
    ""name"": ""{Name}"",
    ""references"": {refs},
    ""includePlatforms"": {platforms},
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": {(AutoReferenced ? "true" : "false")},
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}}";
            }
        }
    }
}
