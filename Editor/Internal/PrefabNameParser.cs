// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under GPL v2.0. See LICENSE in the project root for license information.

using System.Text.RegularExpressions;

namespace Briko.Editor.Internal {
    /// <summary>
    /// Parses Unity GameObject names into prefab name and variant number
    /// based on the Briko naming convention (briko_spec.md §4.3).
    /// Convention: {type}_{dimensions}_{descriptor...}_{variant}
    /// where type is any word, dimensions is NxNxN, descriptor is one or more words,
    /// and variant is a positive integer suffix.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class PrefabNameParser {
#nullable enable

        ///////////////////////////////////////////////////////////////////////
        // Constants

        const string PATTERN = @"^(.+_([\d.]+x[\d.]+x[\d.]+)_.+)_(\d+)$";

        ///////////////////////////////////////////////////////////////////////
        // Public methods [verb, verb phrase]

        /// <summary>
        /// Parses a GameObject name into (prefab, variant). Returns null if the name
        /// does not match the Briko naming convention.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static (string prefab, int variant)? Parse(string name) {
            var match = Regex.Match(name, PATTERN);
            if (!match.Success) {
                return null;
            }
            string prefab = match.Groups[1].Value;
            int variant = int.Parse(match.Groups[3].Value);
            return (prefab, variant);
        }
    }
}