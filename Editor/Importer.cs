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
        // Private constants

        private const float GRID_UNIT = 0.5f;
        private const string LEVEL_ROOT_NAME = "Level";
        private const string SYSTEM_NAME = "System";
        private const string PLATFORM_NAME = "Platform";
        private const string ENTITY_NAME = "Entity";
        private const string BLOCKS_CONTAINER_NAME = "blocks_plain";

        ///////////////////////////////////////////////////////////////////////
        // Public methods [verb, verb phrase]

        /// <summary>
        /// Creates a new empty scene, builds the Level/System/Platform/Entity hierarchy,
        /// instantiates prefabs from <paramref name="layout"/>, and saves to <paramref name="scene_path"/>.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static void ImportToNewScene(Root layout, string scene_path) {
            var new_scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            GameObject level_root = new(LEVEL_ROOT_NAME);
            GameObject system_go = new(SYSTEM_NAME);
            GameObject platform_go = new(PLATFORM_NAME);
            GameObject entity_go = new(ENTITY_NAME);

            system_go.transform.SetParent(level_root.transform);
            platform_go.transform.SetParent(level_root.transform);
            entity_go.transform.SetParent(level_root.transform);

            foreach (Platform platform in layout.platforms) {
                string grounds_name = $"grounds_{platform.floor}";
                GameObject grounds_go = GetOrCreateChild(
                    parent: platform_go.transform,
                    child_name: grounds_name);

                foreach (Item item in platform.grounds) {
                    PlaceItem(item: item, parent: grounds_go.transform);
                }

                if (platform.blocks.Count > 0) {
                    GameObject blocks_go = GetOrCreateChild(
                        parent: platform_go.transform,
                        child_name: BLOCKS_CONTAINER_NAME);

                    foreach (Item item in platform.blocks) {
                        PlaceItem(item: item, parent: blocks_go.transform);
                    }
                }

                foreach (Zone zone in platform.zones) {
                    PlaceZone(zone: zone, parent: entity_go.transform);
                }
            }

            string dir = Path.GetDirectoryName(scene_path) ?? "";
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }
            EditorSceneManager.SaveScene(new_scene, scene_path);
            AssetDatabase.Refresh();
        }

        ///////////////////////////////////////////////////////////////////////
        // Private methods [verb, verb phrase]

        /// <summary>
        /// Instantiates a prefab for <paramref name="item"/> under <paramref name="parent"/>.
        /// Logs a warning and skips if the prefab is not found.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static void PlaceItem(Item item, Transform parent) {
            string prefab_name = $"{item.prefab}_{item.variant}";
            string[] guids = AssetDatabase.FindAssets($"t:Prefab {prefab_name}");
            if (guids.Length == 0) {
                Debug.LogWarning($"[Briko] Prefab not found: {prefab_name} (skipped)");
                return;
            }

            string asset_path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameObject? prefab = AssetDatabase.LoadAssetAtPath<GameObject>(asset_path);
            if (prefab == null) {
                Debug.LogWarning($"[Briko] Failed to load prefab at: {asset_path} (skipped)");
                return;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            float[] snapped = GridSnapper.Snap(raw: item.position, grid_unit: GRID_UNIT);
            instance.transform.position = new Vector3(snapped[0], snapped[1], snapped[2]);
            instance.transform.rotation = Quaternion.Euler(0f, item.rotation_y, 0f);
        }

        /// <summary>
        /// Creates an empty zone GameObject under <paramref name="parent"/>.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static void PlaceZone(Zone zone, Transform parent) {
            GameObject zone_go = new(zone.zone_id);
            zone_go.transform.SetParent(parent);
            float[] snapped = GridSnapper.Snap(raw: zone.position, grid_unit: GRID_UNIT);
            zone_go.transform.position = new Vector3(snapped[0], snapped[1], snapped[2]);
        }

        /// <summary>
        /// Gets or creates a child GameObject with the given name under <paramref name="parent"/>.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        private static GameObject GetOrCreateChild(Transform parent, string child_name) {
            Transform? existing = parent.Find(child_name);
            if (existing != null) {
                return existing.gameObject;
            }
            GameObject child = new(child_name);
            child.transform.SetParent(parent);
            return child;
        }
    }
}
