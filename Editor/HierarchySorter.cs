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
            GameObject? platform_root = FindRootObject(name: PLATFORM_NAME);
            if (platform_root == null) {
                Debug.LogError(message: "[Briko] 'Platform' root not found. Aborting sort.");
                return;
            }

            // Step 2: Collect all Ground and Block GameObjects from Platform hierarchy
            List<GameObject> all_grounds = new();
            List<GameObject> all_blocks = new();
            CollectItemsByKind(
                parent: platform_root.transform,
                grounds: all_grounds,
                blocks: all_blocks);

            // Step 3: Detect floor anchors and collect unique surface Y values
            List<float> anchor_surfaces = new();
            foreach (GameObject g in all_grounds) {
                string clean = g.name.Replace("(Clone)", "").Trim();
                var dims = FloorDetector.ParseDimensions(name: clean);
                if (dims == null) { continue; }
                if (!FloorDetector.IsFloorAnchor(x: dims.Value.x, z: dims.Value.z)) { continue; }
                float surf = FloorDetector.CalcSurfaceY(prefab_y: g.transform.position.y);
                float rounded = MathF.Round(surf * 100f) / 100f;
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
            anchor_surfaces.Sort((a, b) => b.CompareTo(a));

            // Step 4: Assign floor labels
            List<(float surface_y, string label)> floor_labels =
                FloorDetector.AssignFloorLabels(surface_y_values_desc: anchor_surfaces);

            // Step 5: Detect travel direction from zone positions
            float spawn_y = 0f;
            float exit_y = 0f;
            GameObject? entity_root = FindRootObject(name: ENTITY_NAME);
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
            foreach (GameObject g in all_grounds) {
                string clean = g.name.Replace("(Clone)", "").Trim();
                var dims = FloorDetector.ParseDimensions(name: clean);
                string target_label;
                if (dims != null && FloorDetector.IsFloorAnchor(x: dims.Value.x, z: dims.Value.z)) {
                    float surf = FloorDetector.CalcSurfaceY(prefab_y: g.transform.position.y);
                    float rounded = MathF.Round(surf * 100f) / 100f;
                    target_label = FindFloorLabelBySurface(
                        surface_y: rounded, floor_labels: floor_labels);
                } else {
                    float surf = FloorDetector.CalcSurfaceY(prefab_y: g.transform.position.y);
                    target_label = AssignLandingToFloor(
                        landing_surface_y: surf,
                        floors_desc: floor_labels,
                        is_descending: is_descending);
                }
                Transform grounds_t = FindGroundsContainer(
                    label: target_label, containers: floor_containers);
                g.transform.SetParent(grounds_t, worldPositionStays: true);
            }

            // Step 9: Assign and reparent Block GameObjects
            foreach (GameObject b in all_blocks) {
                string floor_label = FloorDetector.AssignBlockToFloor(
                    block_y: b.transform.position.y, floors_desc: floor_labels);
                Transform blocks_t = FindBlocksContainer(
                    label: floor_label, containers: floor_containers);
                b.transform.SetParent(blocks_t, worldPositionStays: true);
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
                RenumberContainerChildren(container: grounds_t);
                RenumberContainerChildren(container: blocks_t);
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
        static GameObject? FindRootObject(string name) {
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
        static void CollectItemsByKind(
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
                    CollectItemsByKind(parent: child, grounds: grounds, blocks: blocks);
                }
            }
        }

        /// <summary>
        /// Returns the floor label whose surface Y is closest to the given value.
        /// Falls back to the lowest floor if no exact match is found.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static string FindFloorLabelBySurface(
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
        static string AssignLandingToFloor(
            float landing_surface_y,
            List<(float surface_y, string label)> floors_desc,
            bool is_descending) {

            if (is_descending) {
                string nearest = floors_desc[floors_desc.Count - 1].label;
                foreach ((float sy, string label) in floors_desc) {
                    if (sy <= landing_surface_y) { nearest = label; break; }
                }
                return nearest;
            } else {
                string nearest = floors_desc[0].label;
                for (int i = floors_desc.Count - 1; i >= 0; i--) {
                    if (floors_desc[i].surface_y >= landing_surface_y) {
                        nearest = floors_desc[i].label;
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
        static Transform FindGroundsContainer(
            string label,
            List<(string label, Transform grounds_t, Transform blocks_t)> containers) {

            foreach ((string l, Transform g, Transform b) in containers) {
                if (l == label) { return g; }
            }
            return containers[containers.Count - 1].grounds_t;
        }

        /// <summary>
        /// Returns the blocks Transform for the given floor label from the containers list.
        /// Falls back to the last entry if the label is not found.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        static Transform FindBlocksContainer(
            string label,
            List<(string label, Transform grounds_t, Transform blocks_t)> containers) {

            foreach ((string l, Transform g, Transform b) in containers) {
                if (l == label) { return b; }
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
        static void RenumberContainerChildren(Transform container) {
            if (container.childCount == 0) { return; }
            List<(Transform t, string base_name, float x, float z)> entries = new();
            foreach (Transform child in container) {
                string clean = child.name.Replace("(Clone)", "").Trim();
                var parsed = PrefabNameParser.Parse(name: clean);
                string base_name = parsed != null ? parsed.Value.prefab : clean;
                entries.Add((child, base_name, child.position.x, child.position.z));
            }
            entries.Sort((a, b) => {
                int cz = a.z.CompareTo(b.z);
                if (cz != 0) { return cz; }
                return a.x.CompareTo(b.x);
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
            List<(Transform t, string new_name)> output = new();
            for (int g = 0; g < group_order.Count; g++) {
                string group_name = group_order[g];
                int variant = 1;
                for (int i = 0; i < entries.Count; i++) {
                    if (entries[i].base_name == group_name) {
                        output.Add((entries[i].t, $"{group_name}_{variant}"));
                        variant++;
                    }
                }
            }
            for (int i = 0; i < output.Count; i++) {
                output[i].t.name = output[i].new_name;
                output[i].t.SetSiblingIndex(i);
            }
        }
    }
}
