// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using System;

namespace Briko.Editor.Internal {
    /// <summary>
    /// Snaps a 3D position to the nearest grid boundary.
    /// Pure function — no Unity dependencies, safe to use in test projects.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public static class GridSnapper {
        ///////////////////////////////////////////////////////////////////////////////////////////////
        // public static Methods [verb]

        /// <summary>
        /// Snaps each component of <paramref name="raw"/> to the nearest multiple
        /// of <paramref name="grid_unit"/>.
        /// </summary>
        /// <author>h.adachi (STUDIO MeowToon)</author>
        public static float[] Snap(float[] raw, float grid_unit) {
            float[] snapped = new float[raw.Length];
            for (int i = 0; i < raw.Length; i++) {
                snapped[i] = (float)(Math.Round(raw[i] / grid_unit, MidpointRounding.AwayFromZero) * grid_unit);
            }
            return snapped;
        }
    }
}
