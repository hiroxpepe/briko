// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Briko.Editor.Internal;
using Briko.Editor.Model;

namespace Briko.Editor {

    /// <summary>
    /// Exports the active Unity scene to a Briko JSON layout (briko_spec.md §7.2).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Exporter {
        ///////////////////////////////////////////////////////////////////////
        // Private constants

        const float GRID_UNIT = 0.25f;
        const float GRID_WARN_THRESHOLD = 0.01f;
        const float ROTATION_WARN_THRESHOLD = 1.0f;
        const float FLOOR_2F_Y_THRESHOLD = 3.0f;
        const string ZONE_PATTERN = @"^vol_[a-z0-9_]+$";
        const string GROUNDS_PREFIX = "grounds_";
        const string BLOCKS_PREFIX = "blocks_";
        const string PLATFORM_NAME = "Platform";
        const string ENTITY_NAME = "Entity";

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

            GameObject? platform_root = findRootObject(name: PLATFORM_NAME);
            if (platform_root != null) {
                collectPlatformChildren(
                    platform_root: platform_root,
                    platform_map: platform_map);
            } else {
                Debug.LogWarning("[Briko] 'Platform' GameObject not found in scene root.");
            }

            List<Zone> zones = new();
            GameObject? entity_root = findRootObject(name: ENTITY_NAME);
            if (entity_root != null) {
                collectZones(entity_root: entity_root, zones: zones);
            }

            foreach (Platform platform in platform_map.Values) {
                layout.platforms.Add(platform);
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
        static GameObject? findRootObject(string name) {
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
        static void collectPlatformChildren(
            GameObject platform_root,
            Dictionary<string, Platform> platform_map) {

            foreach (Transform child in platform_root.transform) {
                string child_name = child.name;
                if (child_name.StartsWith(GROUNDS_PREFIX)) {
                    string floor = child_name.Substring(GROUNDS_PREFIX.Length);
                    Platform platform = getOrCreatePlatform(
                        platform_map: platform_map,
                        floor: floor);
                    collectItems(parent: child, items: platform.grounds);
                } else if (child_name.StartsWith(BLOCKS_PREFIX)) {
                    collectBlockItems(parent: child, platform_map: platform_map);
                }
            }
        }

        /// <summary>
        /// Collects Item entries from all descendants of <paramref name="parent"/>.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static void collectItems(Transform parent, List<Item> items) {
            foreach (Transform child in parent) {
                string clean_name = child.name.Replace("(Clone)", "").Trim();
                var parsed = PrefabNameParser.Parse(name: clean_name);
                if (parsed == null) {
                    continue;
                }
                float[] raw_position = new float[] {
                    child.position.x,
                    child.position.y,
                    child.position.z
                };
                float[] snapped = GridSnapper.Snap(raw: raw_position, grid_unit: GRID_UNIT);
                warnIfPositionSnapDiffers(raw_position: raw_position, snapped: snapped, go_name: child.name);

                int rotation_y = snapRotationY(
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
        static void collectBlockItems(
            Transform parent,
            Dictionary<string, Platform> platform_map) {

            foreach (Transform child in parent) {
                string clean_name = child.name.Replace("(Clone)", "").Trim();
                var parsed = PrefabNameParser.Parse(name: clean_name);
                if (parsed == null) {
                    continue;
                }
                float[] raw_position = new float[] {
                    child.position.x,
                    child.position.y,
                    child.position.z
                };
                float[] snapped = GridSnapper.Snap(raw: raw_position, grid_unit: GRID_UNIT);
                warnIfPositionSnapDiffers(raw_position: raw_position, snapped: snapped, go_name: child.name);

                int rotation_y = snapRotationY(
                    raw_y: child.rotation.eulerAngles.y,
                    go_name: child.name);

                string floor = child.position.y < FLOOR_2F_Y_THRESHOLD ? "1f" : "2f";
                Platform platform = getOrCreatePlatform(
                    platform_map: platform_map,
                    floor: floor);

                Item item = new() {
                    prefab = parsed.Value.prefab,
                    variant = parsed.Value.variant,
                    position = snapped,
                    rotation_y = rotation_y,
                };
                platform.blocks.Add(item);
            }
        }

        /// <summary>
        /// Collects Zone entries from the Entity hierarchy.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static void collectZones(GameObject entity_root, List<Zone> zones) {
            foreach (Transform child in entity_root.transform) {
                if (!Regex.IsMatch(child.name, ZONE_PATTERN)) {
                    continue;
                }
                float[] raw_position = new float[] {
                    child.position.x,
                    child.position.y,
                    child.position.z
                };
                float[] snapped = GridSnapper.Snap(raw: raw_position, grid_unit: GRID_UNIT);
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
        static Platform getOrCreatePlatform(
            Dictionary<string, Platform> platform_map,
            string floor) {

            if (!platform_map.TryGetValue(floor, out Platform? platform)) {
                platform = new Platform { floor = floor };
                platform_map[floor] = platform;
            }
            return platform;
        }

        /// <summary>
        /// Snaps rotation_y to nearest 0/90/180/270, warning if drift exceeds threshold.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static int snapRotationY(float raw_y, string go_name) {
            int[] candidates = new int[] { 0, 90, 180, 270 };
            float normalized = ((raw_y % 360f) + 360f) % 360f;
            int best = 0;
            float best_difference = float.MaxValue;
            foreach (int candidate in candidates) {
                float difference = MathF.Abs(normalized - candidate);
                if (difference < best_difference) {
                    best_difference = difference;
                    best = candidate;
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
        static void warnIfPositionSnapDiffers(
            float[] raw_position,
            float[] snapped,
            string go_name) {

            for (int i = 0; i < raw_position.Length; i++) {
                if (MathF.Abs(raw_position[i] - snapped[i]) > GRID_WARN_THRESHOLD) {
                    Debug.LogWarning(
                        $"[Briko] Non-grid position on '{go_name}' axis[{i}]: " +
                        $"{raw_position[i]:F4} -> {snapped[i]:F4}");
                    break;
                }
            }
        }
    }
}
