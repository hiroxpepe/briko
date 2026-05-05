// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under GPL v2.0. See LICENSE in the project root for license information.

using System.IO;
using Briko.Editor.Internal;
using Briko.Editor.Model;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Briko.Editor {

    /// <summary>
    /// Imports a Briko JSON layout into a new Unity scene (briko_spec.md §7.2).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class Importer {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Public methods [verb, verb phrase]

        /// <summary>
        /// Creates a new empty scene, builds the Level/System/Platform/Entity hierarchy,
        /// instantiates prefabs from <paramref name="layout"/>, and saves to <paramref name="scene_path"/>.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static void ImportToNewScene(Root layout, string scene_path) {
            BrikoLog.Write($"[Briko] ImportToNewScene start: layout_id={layout.layout_id} scene_path={scene_path}");
            BrikoLog.Write($"[Briko] platforms count={layout.platforms.Count}");

            var new_scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            BrikoLog.Write($"[Briko] NewScene created.");

            GameObject level_root = new("Level");
            GameObject system_go = new("System");
            GameObject platform_go = new("Platform");
            GameObject entity_go = new("Entity");

            system_go.transform.SetParent(level_root.transform);
            platform_go.transform.SetParent(level_root.transform);
            entity_go.transform.SetParent(level_root.transform);
            BrikoLog.Write($"[Briko] Hierarchy built: Level > System / Platform / Entity");

            // Dump all prefab names once for diagnostics
            string[] all_guids = AssetDatabase.FindAssets("t:Prefab");
            BrikoLog.Write($"[Briko] Total prefabs in project: {all_guids.Length}");
            foreach (string g in all_guids) {
                BrikoLog.Write($"[Briko] available prefab: {Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(g))}");
            }

            foreach (Platform platform in layout.platforms) {
                BrikoLog.Write($"[Briko] Processing platform: floor={platform.floor} grounds={platform.grounds.Count} blocks={platform.blocks.Count} zones={platform.zones.Count}");

                string grounds_name = $"grounds_{platform.floor}";
                GameObject grounds_go = GetOrCreateChild(
                    parent: platform_go.transform,
                    child_name: grounds_name);
                BrikoLog.Write($"[Briko] Container created: {grounds_name}");

                foreach (Item item in platform.grounds) {
                    PlaceItem(item: item, parent: grounds_go.transform, all_guids: all_guids);
                }

                if (platform.blocks.Count > 0) {
                    GameObject blocks_go = GetOrCreateChild(
                        parent: platform_go.transform,
                        child_name: "blocks_plain");
                    BrikoLog.Write($"[Briko] Container created: blocks_plain");

                    foreach (Item item in platform.blocks) {
                        PlaceItem(item: item, parent: blocks_go.transform, all_guids: all_guids);
                    }
                }

                foreach (Zone zone in platform.zones) {
                    PlaceZone(zone: zone, parent: entity_go.transform);
                }
            }

            string dir = Path.GetDirectoryName(scene_path) ?? "";
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
                BrikoLog.Write($"[Briko] Directory created: {dir}");
            }
            EditorSceneManager.SaveScene(new_scene, scene_path);
            BrikoLog.Write($"[Briko] Scene saved: {scene_path}");
            AssetDatabase.Refresh();
            BrikoLog.Write($"[Briko] ImportToNewScene complete.");
        }

        ///////////////////////////////////////////////////////////////////////
        // Methods

        /// <summary>
        /// Instantiates a prefab for <paramref name="item"/> under <paramref name="parent"/>.
        /// Logs a warning and skips if the prefab is not found.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static void PlaceItem(Item item, Transform parent, string[] all_guids) {
            string prefab_name = item.prefab;
            BrikoLog.Write($"[Briko] PlaceItem: searching prefab_name={prefab_name}");

            string? asset_path = null;
            foreach (string guid in all_guids) {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string file_name = Path.GetFileNameWithoutExtension(path);
                if (file_name == prefab_name) {
                    asset_path = path;
                    BrikoLog.Write($"[Briko] PlaceItem: found at path={asset_path}");
                    break;
                }
            }

            if (asset_path == null) {
                BrikoLog.Write($"[Briko] PlaceItem: NOT FOUND prefab_name={prefab_name} (skipped)");
                return;
            }

            GameObject? prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset_path);
            if (prefab == null) {
                BrikoLog.Write($"[Briko] PlaceItem: load failed asset_path={asset_path} (skipped)");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            float[] snapped = GridSnapper.Snap(raw: item.position, grid_unit: 0.25f);
            instance.transform.position = new Vector3(snapped[0], snapped[1], snapped[2]);
            instance.transform.rotation = Quaternion.Euler(0f, item.rotation_y, 0f);
            BrikoLog.Write($"[Briko] PlaceItem: placed {prefab_name} at ({snapped[0]}, {snapped[1]}, {snapped[2]}) rotation_y={item.rotation_y}");
        }

        /// <summary>
        /// Creates an empty zone GameObject under <paramref name="parent"/>.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static void PlaceZone(Zone zone, Transform parent) {
            BrikoLog.Write($"[Briko] PlaceZone: zone_id={zone.zone_id}");
            GameObject zone_go = new(zone.zone_id);
            zone_go.transform.SetParent(parent);
            float[] snapped = GridSnapper.Snap(raw: zone.position, grid_unit: 0.25f);
            zone_go.transform.position = new Vector3(snapped[0], snapped[1], snapped[2]);
            BrikoLog.Write($"[Briko] PlaceZone: placed {zone.zone_id} at ({snapped[0]}, {snapped[1]}, {snapped[2]})");
        }

        /// <summary>
        /// Gets or creates a child GameObject with the given name under <paramref name="parent"/>.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static GameObject GetOrCreateChild(Transform parent, string child_name) {
            Transform? existing = parent.Find(child_name);
            if (existing != null) {
                BrikoLog.Write($"[Briko] GetOrCreateChild: reused existing {child_name}");
                return existing.gameObject;
            }
            GameObject child = new(child_name);
            child.transform.SetParent(parent);
            BrikoLog.Write($"[Briko] GetOrCreateChild: created {child_name}");
            return child;
        }
    }
}
