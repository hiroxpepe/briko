// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Briko.Editor.Internal {
    ///////////////////////////////////////////////////////////////////////////////////////////////////
    // public Classes

    /// <summary>
    /// Detects floor structure from Ground prefab positions and assigns floor labels.
    /// Pure static methods — no Unity dependencies, fully testable with dotnet test.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class FloorDetector {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // Const [nouns]

        const float FLOOR_ANCHOR_MIN_XZ = 5.0f;
        const float GROUND_HALF_HEIGHT = 0.25f;
        const float CHARACTER_HEIGHT = 1.4f;

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // private static Fields

        static readonly Regex FLOOR_CONTAINER_PATTERN =
            new(@"^\d+F$|^B\d+F$");

        static readonly Regex DIMENSION_PATTERN =
            new(@"_([\d.]+)x([\d.]+)x([\d.]+)_");

        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Parses prefab dimensions (X, Y, Z) from a GameObject name.
        /// Returns null if the name does not contain a valid dimension segment.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static (float x, float y, float z)? ParseDimensions(string name) {
            var match = DIMENSION_PATTERN.Match(name);
            if (!match.Success) { return null; }
            float x = float.Parse(match.Groups[1].Value);
            float y = float.Parse(match.Groups[2].Value);
            float z = float.Parse(match.Groups[3].Value);
            return (x, y, z);
        }

        /// <summary>
        /// Returns true if the given dimensions qualify as a floor anchor (X >= 5.0 AND Z >= 5.0).
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static bool IsFloorAnchor(float x, float z) {
            return x >= FLOOR_ANCHOR_MIN_XZ && z >= FLOOR_ANCHOR_MIN_XZ;
        }

        /// <summary>
        /// Calculates the surface Y (top face) of a Ground prefab.
        /// surface_Y = prefab_position_Y + GROUND_HALF_HEIGHT
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static float CalculateSurfaceY(float prefab_y) {
            return prefab_y + GROUND_HALF_HEIGHT;
        }

        /// <summary>
        /// Assigns floor labels ("1F", "2F", "B1F", ...) to a sorted list of unique surface Y values.
        /// The floor whose surface Y is closest to 0.0 is assigned "1F".
        /// Floors above 1F are "2F", "3F", ... Floors below are "B1F", "B2F", ...
        /// Input list must be sorted descending (highest Y first).
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static List<(float surface_y, string label)> AssignFloorLabels(
            List<float> surface_y_values_descending) {

            if (surface_y_values_descending.Count == 0) {
                return new List<(float, string)>();
            }
            int base_index = 0;
            float min_distance = float.MaxValue;
            for (int i = 0; i < surface_y_values_descending.Count; i++) {
                float distance = MathF.Abs(surface_y_values_descending[i]);
                if (distance < min_distance) {
                    min_distance = distance;
                    base_index = i;
                }
            }
            var result = new List<(float, string)>();
            for (int i = 0; i < surface_y_values_descending.Count; i++) {
                int relative_level = base_index - i;
                string label;
                if (relative_level == 0) {
                    label = "1F";
                } else if (relative_level > 0) {
                    label = $"{relative_level + 1}F";
                } else {
                    label = $"B{-relative_level}F";
                }
                result.Add((surface_y_values_descending[i], label));
            }
            return result;
        }

        /// <summary>
        /// Returns the floor label for a block at the given world Y position.
        /// Assigns to the floor whose surface Y satisfies:
        ///   floor_surface_Y <= block_Y AND block_Y - floor_surface_Y <= CHARACTER_HEIGHT
        /// If no floor qualifies, assigns to the nearest floor below.
        /// floors_descending must be sorted descending.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static string AssignBlockToFloor(
            float block_y,
            List<(float surface_y, string label)> floors_descending) {

            foreach (var floor in floors_descending) {
                float difference = block_y - floor.surface_y;
                if (difference >= 0f && difference <= CHARACTER_HEIGHT) {
                    return floor.label;
                }
            }
            string nearest = floors_descending[floors_descending.Count - 1].label;
            foreach (var floor in floors_descending) {
                if (floor.surface_y <= block_y) {
                    nearest = floor.label;
                    break;
                }
            }
            return nearest;
        }

        /// <summary>
        /// Detects travel direction from spawn and exit zone Y positions.
        /// Returns true if the level descends (spawn_Y > exit_Y).
        /// Returns false if the level ascends (spawn_Y <= exit_Y).
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static bool IsDescending(float spawn_y, float exit_y) {
            return spawn_y > exit_y;
        }

        /// <summary>
        /// Returns true if the given container name is a floor label (e.g. "1F", "2F", "B1F").
        /// Pattern: \d+F (above / at ground) or B\d+F (below ground). Uppercase F only.
        /// Used by HierarchySorter and Exporter to distinguish floor containers.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static bool IsFloorContainer(string name) {
            return FLOOR_CONTAINER_PATTERN.IsMatch(name);
        }

        /// <summary>
        /// Sorts items by Z ascending then X ascending globally, then outputs in grouped order.
        /// Groups are ordered by first-appearance in the global Z/X sort.
        /// Within each group items are in Z/X order, numbered _1, _2, ... per group.
        /// Returns names in grouped order (all of group A, then all of group B, ...).
        /// Empty input returns an empty list.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static List<string> RenumberVariants(
            List<(string base_name, float x, float z)> items) {

            if (items.Count == 0) { return new List<string>(); }
            List<(string base_name, float x, float z)> sorted = new(items);
            sorted.Sort((first, second) => {
                int z_comparison = first.z.CompareTo(second.z);
                if (z_comparison != 0) { return z_comparison; }
                return first.x.CompareTo(second.x);
            });
            List<string> group_order = new();
            for (int i = 0; i < sorted.Count; i++) {
                string name = sorted[i].base_name;
                bool already_in = false;
                for (int j = 0; j < group_order.Count; j++) {
                    if (group_order[j] == name) { already_in = true; break; }
                }
                if (!already_in) { group_order.Add(name); }
            }
            List<string> result = new();
            for (int group_index = 0; group_index < group_order.Count; group_index++) {
                string group_name = group_order[group_index];
                int variant = 1;
                for (int i = 0; i < sorted.Count; i++) {
                    if (sorted[i].base_name == group_name) {
                        result.Add($"{group_name}_{variant}");
                        variant++;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Returns true if the container name matches the grounds pattern.
        /// Matches "grounds" (post-sort exact) or "grounds_*" (pre-sort prefix). Lowercase only.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static bool IsGroundsContainer(string name) {
            return name == "grounds" || name.StartsWith("grounds_");
        }

        /// <summary>
        /// Returns true if the container name matches the blocks pattern.
        /// Matches "blocks" (post-sort exact) or "blocks_*" (pre-sort prefix). Lowercase only.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static bool IsBlocksContainer(string name) {
            return name == "blocks" || name.StartsWith("blocks_");
        }

        /// <summary>
        /// Returns true if the name is a structural container safe to destroy on re-sort.
        /// Combines IsFloorContainer, IsGroundsContainer, and IsBlocksContainer.
        /// Used by HierarchySorter to unconditionally destroy old containers on re-run.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static bool IsStructuralContainer(string name) {
            return IsFloorContainer(name: name)
                || IsGroundsContainer(name: name)
                || IsBlocksContainer(name: name);
        }

        /// <summary>
        /// Validates that in the given sibling-order list each base_name's variants
        /// appear in strict ascending order starting from 1.
        /// Returns false if any base_name starts at a variant other than 1,
        /// or if subsequent variants are not exactly previous + 1.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static bool IsVariantOrderValid(
            List<(string base_name, int variant)> items) {

            if (items.Count == 0) { return true; }
            List<(string base_name, int last_variant)> seen = new();
            for (int i = 0; i < items.Count; i++) {
                string name = items[i].base_name;
                int variant = items[i].variant;
                bool found = false;
                for (int j = 0; j < seen.Count; j++) {
                    if (seen[j].base_name == name) {
                        if (variant != seen[j].last_variant + 1) { return false; }
                        seen[j] = (name, variant);
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    if (variant != 1) { return false; }
                    seen.Add((name, 1));
                }
            }
            return true;
        }
    }
}
