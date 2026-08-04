// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Briko.Editor.Internal;
using Briko.Editor.Model;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Briko.Editor {

    /// <summary>
    /// Exports the active Unity scene to a Briko JSON layout (briko_spec.md §7.2).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Exporter {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Private constants

        private const float GRID_UNIT = 0.25f;
        private const float GRID_WARN_THRESHOLD = 0.01f;
        private const float ROTATION_WARN_THRESHOLD = 1.0f;
        private const float FLOOR_2F_Y_THRESHOLD = 3.0f;
        private const string ZONE_PATTERN = @"^vol_[a-z0-9_]+$";
        private const string GROUNDS_PREFIX = "grounds_";
        private const string BLOCKS_PREFIX = "blocks_";
        private const string PLATFORM_NAME = "Platform";
        private const string ENTITY_NAME = "Entity";

        ///////////////////////////////////////////////////////////////////////
        // Public methods [verb, verb phrase]

        /// <summary>
        /// Walks the active scene and builds a Root from Platform and Entity hierarchies.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static Root ExportFromActiveScene() {
            Scene active_scene = SceneManager.GetActiveScene();
            Root layout = new() {
                layout_id = active_scene.name,
                grid_unit = GRID_UNIT,
            };

            Dictionary<string, Platform> platform_map = new();

            GameObject? platform_root = FindRootObject(name: PLATFORM_NAME);
            if (platform_root != null) {
                CollectPlatformChildren(
                    platform_root: platform_root,
                    platform_map: platform_map);
            } else {
                Debug.LogWarning("[Briko] 'Platform' GameObject not found in scene root.");
            }

            List<Zone> zones = new();
            GameObject? entity_root = FindRootObject(name: ENTITY_NAME);
            if (entity_root != null) {
                CollectZones(entity_root: entity_root, zones: zones);
            }

            foreach (Platform plat in platform_map.Values) {
                layout.platforms.Add(plat);
            }

            if (zones.Count > 0) {
                if (layout.platforms.Count == 0) {
                    layout.platforms.Add(new Platform { floor = "1f" });
                }
                layout.platforms[0].zones.AddRange(zones);
            }

            return layout;
        }

        ///////////////////////////////////////////////////////////////////////
        // Private methods [verb, verb phrase]

        /// <summary>
        /// Finds a root-level GameObject in the active scene by name.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static GameObject? FindRootObject(string name) {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) {
                if (root.name == name) {
                    return root;
                }
            }
            return null;
        }

        /// <summary>
        /// Iterates the children of the Platform GameObject and collects grounds and blocks.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static void CollectPlatformChildren(
            GameObject platform_root,
            Dictionary<string, Platform> platform_map) {

            foreach (Transform child in platform_root.transform) {
                string child_name = child.name;
                if (child_name.StartsWith(GROUNDS_PREFIX)) {
                    string floor = child_name.Substring(GROUNDS_PREFIX.Length);
                    Platform plat = GetOrCreatePlatform(
                        platform_map: platform_map,
                        floor: floor);
                    CollectItems(parent: child, items: plat.grounds);
                } else if (child_name.StartsWith(BLOCKS_PREFIX)) {
                    CollectBlockItems(parent: child, platform_map: platform_map);
                }
            }
        }

        /// <summary>
        /// Collects Item entries from all descendants of <paramref name="parent"/>.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static void CollectItems(Transform parent, List<Item> items) {
            foreach (Transform child in parent) {
                string clean_name = child.name.Replace("(Clone)", "").Trim();
                var parsed = PrefabNameParser.Parse(name: clean_name);
                if (parsed == null) {
                    continue;
                }
                float[] raw_pos = new float[] {
                    child.position.x,
                    child.position.y,
                    child.position.z
                };
                float[] snapped = GridSnapper.Snap(raw: raw_pos, grid_unit: GRID_UNIT);
                WarnIfPositionSnapDiffers(raw_pos: raw_pos, snapped: snapped, go_name: child.name);

                int rotation_y = SnapRotationY(
                    raw_y: child.rotation.eulerAngles.y,
                    go_name: child.name);

                Item item = new() {
                    prefab = parsed.Value.prefab,
                    variant = parsed.Value.variant,
                    position = snapped,
                    rotation_y = rotation_y,
                };
                items.Add(item);
            }
        }

        /// <summary>
        /// Collects Block items, inferring floor from Y coordinate.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static void CollectBlockItems(
            Transform parent,
            Dictionary<string, Platform> platform_map) {

            foreach (Transform child in parent) {
                string clean_name = child.name.Replace("(Clone)", "").Trim();
                var parsed = PrefabNameParser.Parse(name: clean_name);
                if (parsed == null) {
                    continue;
                }
                float[] raw_pos = new float[] {
                    child.position.x,
                    child.position.y,
                    child.position.z
                };
                float[] snapped = GridSnapper.Snap(raw: raw_pos, grid_unit: GRID_UNIT);
                WarnIfPositionSnapDiffers(raw_pos: raw_pos, snapped: snapped, go_name: child.name);

                int rotation_y = SnapRotationY(
                    raw_y: child.rotation.eulerAngles.y,
                    go_name: child.name);

                string floor = child.position.y < FLOOR_2F_Y_THRESHOLD ? "1f" : "2f";
                Platform plat = GetOrCreatePlatform(
                    platform_map: platform_map,
                    floor: floor);

                Item item = new() {
                    prefab = parsed.Value.prefab,
                    variant = parsed.Value.variant,
                    position = snapped,
                    rotation_y = rotation_y,
                };
                plat.blocks.Add(item);
            }
        }

        /// <summary>
        /// Collects Zone entries from the Entity hierarchy.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static void CollectZones(GameObject entity_root, List<Zone> zones) {
            foreach (Transform child in entity_root.transform) {
                if (!Regex.IsMatch(child.name, ZONE_PATTERN)) {
                    continue;
                }
                float[] raw_pos = new float[] {
                    child.position.x,
                    child.position.y,
                    child.position.z
                };
                float[] snapped = GridSnapper.Snap(raw: raw_pos, grid_unit: GRID_UNIT);
                zones.Add(new Zone {
                    zone_id = child.name,
                    position = snapped,
                });
            }
        }

        /// <summary>
        /// Gets or creates a Platform for the given floor key.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static Platform GetOrCreatePlatform(
            Dictionary<string, Platform> platform_map,
            string floor) {

            if (!platform_map.TryGetValue(floor, out Platform? plat)) {
                plat = new Platform { floor = floor };
                platform_map[floor] = plat;
            }
            return plat;
        }

        /// <summary>
        /// Snaps rotation_y to nearest 0/90/180/270, warning if drift exceeds threshold.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static int SnapRotationY(float raw_y, string go_name) {
            int[] candidates = new int[] { 0, 90, 180, 270 };
            float normalized = ((raw_y % 360f) + 360f) % 360f;
            int best = 0;
            float best_diff = float.MaxValue;
            foreach (int c in candidates) {
                float diff = MathF.Abs(normalized - c);
                if (diff < best_diff) {
                    best_diff = diff;
                    best = c;
                }
            }
            if (best_diff > ROTATION_WARN_THRESHOLD) {
                Debug.LogWarning(
                    $"[Briko] Non-standard rotation_y on '{go_name}': {raw_y:F2} -> {best}");
            }
            return best;
        }

        /// <summary>
        /// Emits a Console warning if position was not on the grid.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static void WarnIfPositionSnapDiffers(
            float[] raw_pos,
            float[] snapped,
            string go_name) {

            for (int i = 0; i < raw_pos.Length; i++) {
                if (MathF.Abs(raw_pos[i] - snapped[i]) > GRID_WARN_THRESHOLD) {
                    Debug.LogWarning(
                        $"[Briko] Non-grid position on '{go_name}' axis[{i}]: " +
                        $"{raw_pos[i]:F4} -> {snapped[i]:F4}");
                    break;
                }
            }
        }
    }
}
