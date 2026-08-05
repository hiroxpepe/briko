// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Briko.Editor.Internal;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Briko.Editor {

    /// <summary>
    /// Reorganizes the Unity scene hierarchy by floor without moving any prefab in world space.
    /// Menu: Tools > Briko > Sort Hierarchy by Floor
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class HierarchySorter {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Private constants

        const string MENU_ROOT = "Tools/Briko/";
        const string PLATFORM_NAME = "Platform";
        const string ENTITY_NAME = "Entity";
        const string GROUNDS_CONTAINER = "grounds";
        const string BLOCKS_CONTAINER = "blocks";
        const string VOL_SPAWN = "vol_spawn";
        const string VOL_EXIT = "vol_exit";

        ///////////////////////////////////////////////////////////////////////
        // Menu items

        /// <summary>
        /// Sorts the Platform hierarchy by floor, grouping Ground and Block prefabs
        /// into floor containers (1F, 2F, B1F, ...) without moving world positions.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        [MenuItem(MENU_ROOT + "Sort Hierarchy by Floor")]
        public static void SortHierarchyByFloor() {
            // Step 1: Find Platform root
            GameObject? platform_root = findRootObject(name: PLATFORM_NAME);
            if (platform_root == null) {
                Debug.LogError(message: "[Briko] 'Platform' root not found. Aborting sort.");
                return;
            }

            // Step 2: Collect all Ground and Block GameObjects from Platform hierarchy
            List<GameObject> all_grounds = new();
            List<GameObject> all_blocks = new();
            collectItemsByKind(
                parent: platform_root.transform,
                grounds: all_grounds,
                blocks: all_blocks);

            // Step 3: Detect floor anchors and collect unique surface Y values
            List<float> anchor_surfaces = new();
            foreach (GameObject ground in all_grounds) {
                string clean = ground.name.Replace("(Clone)", "").Trim();
                var dimensions = FloorDetector.ParseDimensions(name: clean);
                if (dimensions == null) { continue; }
                if (!FloorDetector.IsFloorAnchor(x: dimensions.Value.x, z: dimensions.Value.z)) { continue; }
                float surface = FloorDetector.CalculateSurfaceY(prefab_y: ground.transform.position.y);
                float rounded = MathF.Round(surface * 100f) / 100f;
                bool already_exists = false;
                for (int i = 0; i < anchor_surfaces.Count; i++) {
                    if (MathF.Abs(anchor_surfaces[i] - rounded) < 0.01f) {
                        already_exists = true;
                        break;
                    }
                }
                if (!already_exists) { anchor_surfaces.Add(rounded); }
            }
            if (anchor_surfaces.Count == 0) {
                Debug.LogWarning(message: "[Briko] No floor anchors found (no Ground with X >= 5.0 and Z >= 5.0). Aborting sort.");
                return;
            }
            anchor_surfaces.Sort((first, second) => second.CompareTo(first));

            // Step 4: Assign floor labels
            List<(float surface_y, string label)> floor_labels =
                FloorDetector.AssignFloorLabels(surface_y_values_desc: anchor_surfaces);

            // Step 5: Detect travel direction from zone positions
            float spawn_y = 0f;
            float exit_y = 0f;
            GameObject? entity_root = findRootObject(name: ENTITY_NAME);
            if (entity_root == null) {
                Debug.LogWarning(message: "[Briko] 'Entity' root not found. Defaulting to descend mode.");
            } else {
                bool found_spawn = false;
                bool found_exit = false;
                foreach (Transform child in entity_root.transform) {
                    if (child.name == VOL_SPAWN) { spawn_y = child.position.y; found_spawn = true; }
                    if (child.name == VOL_EXIT) { exit_y = child.position.y; found_exit = true; }
                }
                if (!found_spawn || !found_exit) {
                    Debug.LogWarning(message: "[Briko] vol_spawn or vol_exit not found. Defaulting to descend mode.");
                }
            }
            bool is_descending = FloorDetector.IsDescending(spawn_y: spawn_y, exit_y: exit_y);

            // Step 6: Capture old containers before building new ones
            List<Transform> old_containers = new();
            foreach (Transform child in platform_root.transform) {
                old_containers.Add(child);
            }

            // Step 7: Build new floor container GameObjects under Platform
            List<(string label, Transform grounds_t, Transform blocks_t)> floor_containers = new();
            foreach ((float sy, string label) in floor_labels) {
                GameObject floor_go = new(label);
                floor_go.transform.SetParent(platform_root.transform);
                GameObject grounds_go = new(GROUNDS_CONTAINER);
                grounds_go.transform.SetParent(floor_go.transform);
                GameObject blocks_go = new(BLOCKS_CONTAINER);
                blocks_go.transform.SetParent(floor_go.transform);
                floor_containers.Add((label, grounds_go.transform, blocks_go.transform));
            }

            // Step 8: Assign and reparent Ground GameObjects
            foreach (GameObject ground in all_grounds) {
                string clean = ground.name.Replace("(Clone)", "").Trim();
                var dimensions = FloorDetector.ParseDimensions(name: clean);
                string target_label;
                if (dimensions != null && FloorDetector.IsFloorAnchor(x: dimensions.Value.x, z: dimensions.Value.z)) {
                    float surface = FloorDetector.CalculateSurfaceY(prefab_y: ground.transform.position.y);
                    float rounded = MathF.Round(surface * 100f) / 100f;
                    target_label = findFloorLabelBySurface(
                        surface_y: rounded, floor_labels: floor_labels);
                } else {
                    float surface = FloorDetector.CalculateSurfaceY(prefab_y: ground.transform.position.y);
                    target_label = assignLandingToFloor(
                        landing_surface_y: surface,
                        floors_descending: floor_labels,
                        is_descending: is_descending);
                }
                Transform grounds_t = findGroundsContainer(
                    label: target_label, containers: floor_containers);
                ground.transform.SetParent(grounds_t, worldPositionStays: true);
            }

            // Step 9: Assign and reparent Block GameObjects
            foreach (GameObject block in all_blocks) {
                string floor_label = FloorDetector.AssignBlockToFloor(
                    block_y: block.transform.position.y, floors_descending: floor_labels);
                Transform blocks_t = findBlocksContainer(
                    label: floor_label, containers: floor_containers);
                block.transform.SetParent(blocks_t, worldPositionStays: true);
            }

            // Step 10: Remove old containers unconditionally (idempotency — re-run safe).
            // All prefab items are already reparented. Remaining children are empty structural
            // objects (grounds/blocks sub-containers) which are also destroyed via DestroyImmediate.
            foreach (Transform old in old_containers) {
                if (old == null) { continue; }
                if (FloorDetector.IsStructuralContainer(name: old.name)) {
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                } else {
                    Debug.LogWarning(message: $"[Briko] Unexpected non-structural container '{old.name}' — left in place.");
                }
            }

            // Step 11: Renumber variants in each floor container
            foreach ((string label, Transform grounds_t, Transform blocks_t) in floor_containers) {
                renumberContainerChildren(container: grounds_t);
                renumberContainerChildren(container: blocks_t);
            }

            EditorUtility.SetDirty(platform_root);
            Debug.Log(message: $"[Briko] Sort hierarchy by floor complete. {floor_labels.Count} floor(s) detected.");
        }

        ///////////////////////////////////////////////////////////////////////
        // Private methods [verb, verb phrase]

        /// <summary>
        /// Finds a root-level GameObject in the active scene by name.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static GameObject? findRootObject(string name) {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects()) {
                if (root.name == name) { return root; }
            }
            return null;
        }

        /// <summary>
        /// Recursively collects Ground and Block GameObjects from the Platform hierarchy.
        /// Recurses into containers (floor labels, grounds_*, blocks_*).
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static void collectItemsByKind(
            Transform parent,
            List<GameObject> grounds,
            List<GameObject> blocks) {

            foreach (Transform child in parent) {
                string clean = child.name.Replace("(Clone)", "").Trim();
                string? kind = PrefabNameParser.ParseKind(name: clean);
                if (kind == "Ground") {
                    grounds.Add(child.gameObject);
                } else if (kind == "Block") {
                    blocks.Add(child.gameObject);
                } else {
                    collectItemsByKind(parent: child, grounds: grounds, blocks: blocks);
                }
            }
        }

        /// <summary>
        /// Returns the floor label whose surface Y is closest to the given value.
        /// Falls back to the lowest floor if no exact match is found.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static string findFloorLabelBySurface(
            float surface_y,
            List<(float surface_y, string label)> floor_labels) {

            foreach ((float sy, string label) in floor_labels) {
                if (MathF.Abs(sy - surface_y) < 0.01f) { return label; }
            }
            return floor_labels[floor_labels.Count - 1].label;
        }

        /// <summary>
        /// Assigns a landing (small ground) to a floor based on travel direction.
        /// Descend: nearest floor below (largest surface_y <= landing surface_y).
        /// Ascend: nearest floor above (smallest surface_y >= landing surface_y).
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static string assignLandingToFloor(
            float landing_surface_y,
            List<(float surface_y, string label)> floors_descending,
            bool is_descending) {

            if (is_descending) {
                string nearest = floors_descending[floors_descending.Count - 1].label;
                foreach ((float sy, string label) in floors_descending) {
                    if (sy <= landing_surface_y) { nearest = label; break; }
                }
                return nearest;
            } else {
                string nearest = floors_descending[0].label;
                for (int i = floors_descending.Count - 1; i >= 0; i--) {
                    if (floors_descending[i].surface_y >= landing_surface_y) {
                        nearest = floors_descending[i].label;
                        break;
                    }
                }
                return nearest;
            }
        }

        /// <summary>
        /// Returns the grounds Transform for the given floor label from the containers list.
        /// Falls back to the last entry if the label is not found.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static Transform findGroundsContainer(
            string label,
            List<(string label, Transform grounds_t, Transform blocks_t)> containers) {

            foreach ((string entry_label, Transform grounds, Transform blocks) in containers) {
                if (entry_label == label) { return grounds; }
            }
            return containers[containers.Count - 1].grounds_t;
        }

        /// <summary>
        /// Returns the blocks Transform for the given floor label from the containers list.
        /// Falls back to the last entry if the label is not found.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static Transform findBlocksContainer(
            string label,
            List<(string label, Transform grounds_t, Transform blocks_t)> containers) {

            foreach ((string entry_label, Transform grounds, Transform blocks) in containers) {
                if (entry_label == label) { return blocks; }
            }
            return containers[containers.Count - 1].blocks_t;
        }

        /// <summary>
        /// Renames and reorders all children in the container by grouped order.
        /// Groups are ordered by first-appearance in global Z/X sort.
        /// Within each group: Z ascending then X ascending.
        /// Each group's variants are numbered _1, _2, ... independently.
        /// SetSiblingIndex is called so Hierarchy display matches the grouped order.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static void renumberContainerChildren(Transform container) {
            if (container.childCount == 0) { return; }
            List<(Transform transform_target, string base_name, float x, float z)> entries = new();
            foreach (Transform child in container) {
                string clean = child.name.Replace("(Clone)", "").Trim();
                var parsed = PrefabNameParser.Parse(name: clean);
                string base_name = parsed != null ? parsed.Value.prefab : clean;
                entries.Add((child, base_name, child.position.x, child.position.z));
            }
            entries.Sort((first, second) => {
                int z_comparison = first.z.CompareTo(second.z);
                if (z_comparison != 0) { return z_comparison; }
                return first.x.CompareTo(second.x);
            });
            List<string> group_order = new();
            for (int i = 0; i < entries.Count; i++) {
                string name = entries[i].base_name;
                bool already_in = false;
                for (int j = 0; j < group_order.Count; j++) {
                    if (group_order[j] == name) { already_in = true; break; }
                }
                if (!already_in) { group_order.Add(name); }
            }
            List<(Transform transform_target, string new_name)> output = new();
            for (int group_index = 0; group_index < group_order.Count; group_index++) {
                string group_name = group_order[group_index];
                int variant = 1;
                for (int i = 0; i < entries.Count; i++) {
                    if (entries[i].base_name == group_name) {
                        output.Add((entries[i].transform_target, $"{group_name}_{variant}"));
                        variant++;
                    }
                }
            }
            for (int i = 0; i < output.Count; i++) {
                output[i].transform_target.name = output[i].new_name;
                output[i].transform_target.SetSiblingIndex(i);
            }
        }
    }
}
