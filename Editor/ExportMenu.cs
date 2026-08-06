// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Briko.Editor.Model;

namespace Briko.Editor {

    /// <summary>
    /// Registers the Export menu item under Tools > Briko.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class ExportMenu {
        ///////////////////////////////////////////////////////////////////////
        // Private constants

        const string MENU_ROOT = "Tools/Briko/";

        static readonly JsonSerializerSettings JSON_SETTINGS = new JsonSerializerSettings {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
        };

        ///////////////////////////////////////////////////////////////////////
        // Menu items

        /// <summary>
        /// Exports the active scene to a JSON file chosen by the user.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        [MenuItem(MENU_ROOT + "Export Active Scene to JSON...")]
        public static void ExportActiveScene() {
            string save_path = EditorUtility.SaveFilePanel(
                "Export Level Layout",
                "",
                "level_layout.json",
                "json");

            if (string.IsNullOrEmpty(save_path)) {
                return;
            }

            Root layout = Exporter.ExportFromActiveScene();

            string json = JsonConvert.SerializeObject(layout, JSON_SETTINGS);
            File.WriteAllText(save_path, json, System.Text.Encoding.UTF8);

            int grounds_count = 0;
            int blocks_count = 0;
            int zones_count = 0;
            foreach (Platform platform in layout.platforms) {
                grounds_count += platform.grounds.Count;
                blocks_count += platform.blocks.Count;
                zones_count += platform.zones.Count;
            }

            EditorUtility.DisplayDialog(
                "Briko Export",
                $"Exported to:\n{save_path}\n\n" +
                $"Grounds: {grounds_count}\n" +
                $"Blocks:  {blocks_count}\n" +
                $"Zones:   {zones_count}",
                "OK");
        }
    }
}
