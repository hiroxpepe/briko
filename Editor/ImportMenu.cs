// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO;
using Briko.Editor.Model;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Briko.Editor {

    /// <summary>
    /// Registers the Import menu item under Tools > Briko.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class ImportMenu {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Private constants

        private const string MENU_ROOT = "Tools/Briko/";

        private static readonly JsonSerializerSettings _json_settings = new JsonSerializerSettings {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
        };

        ///////////////////////////////////////////////////////////////////////
        // Menu items

        /// <summary>
        /// Imports a JSON layout into a new Unity scene.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        [MenuItem(MENU_ROOT + "Import JSON to New Scene...")]
        public static void ImportJSONToNewScene() {
            string json_path = EditorUtility.OpenFilePanel(
                "Select Level Layout JSON",
                "",
                "json");

            if (string.IsNullOrEmpty(json_path)) {
                return;
            }

            string json = File.ReadAllText(json_path, System.Text.Encoding.UTF8);
            Root? layout = JsonConvert.DeserializeObject<Root>(json, _json_settings);

            if (layout == null) {
                EditorUtility.DisplayDialog(
                    "Briko Import",
                    "Failed to parse JSON. Check the file format.",
                    "OK");
                return;
            }

            string scene_path = EditorUtility.SaveFilePanel(
                "Save New Scene",
                "Assets/Scenes",
                $"{layout.layout_id}.unity",
                "unity");

            if (string.IsNullOrEmpty(scene_path)) {
                return;
            }

            // SaveFilePanel returns absolute path; Unity needs a path relative to project root.
            string relative_path = scene_path;
            string data_path = Application.dataPath;
            string project_root = data_path.Substring(0, data_path.Length - "Assets".Length);
            if (scene_path.StartsWith(project_root)) {
                relative_path = scene_path.Substring(project_root.Length);
            }

            Importer.ImportToNewScene(layout: layout, scene_path: relative_path);

            EditorUtility.DisplayDialog(
                "Briko Import",
                $"Scene created:\n{relative_path}",
                "OK");
        }
    }
}
