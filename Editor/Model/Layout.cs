// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under GPL v2.0. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Briko.Editor.Model {

    /// <summary>
    /// Root container for a serialized level layout.
    /// Maps to level_layout.json (briko_spec.md §7.2).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Root {
#nullable enable
        /// <summary>Level identifier, used as scene name on import.</summary>
        public string layout_id { get; set; } = "";

        /// <summary>Grid quantization unit in meters. Fixed at 0.5 for v1.</summary>
        public float grid_unit { get; set; } = 0.5f;

        /// <summary>Target play duration. Fixed at 180 (Beatles single rule) for v1.</summary>
        public int target_duration_sec { get; set; } = 180;

        /// <summary>BGM track filename (placed under StreamingAssets/).</summary>
        public string bgm_track { get; set; } = "";

        /// <summary>Per-floor layout layers.</summary>
        public List<Platform> platforms { get; set; } = new();
    }

    /// <summary>
    /// Per-floor layer holding ground tiles, block obstacles, and trigger zones.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Platform {
#nullable enable
        /// <summary>Floor identifier ("1f", "2f", ...).</summary>
        public string floor { get; set; } = "";

        /// <summary>Ground tiles forming the walkable surface.</summary>
        public List<Item> grounds { get; set; } = new();

        /// <summary>Block obstacles on top of grounds.</summary>
        public List<Item> blocks { get; set; } = new();

        /// <summary>Trigger zones (volumetric markers) for Germio integration.</summary>
        public List<Zone> zones { get; set; } = new();
    }

    /// <summary>
    /// Single prefab placement (ground tile or block obstacle).
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Item {
#nullable enable
        /// <summary>Prefab name without trailing variant number (e.g. "Ground_10.0x0.5x10.0_Green").</summary>
        public string prefab { get; set; } = "";

        /// <summary>Variant number (1-based). Combined with prefab on import: "{prefab}_{variant}".</summary>
        public int variant { get; set; } = 1;

        /// <summary>World position in meters [x, y, z]. All values multiples of grid_unit.</summary>
        public float[] position { get; set; } = new float[3];

        /// <summary>Y-axis rotation in degrees (0/90/180/270). Defaults to 0.</summary>
        public int rotation_y { get; set; } = 0;
    }

    /// <summary>
    /// Trigger zone marker. The zone_id string is the contract with Germio.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public class Zone {
#nullable enable
        /// <summary>Zone identifier matching Germio's germio_config.json (e.g. "vol_boss_start").</summary>
        public string zone_id { get; set; } = "";

        /// <summary>World position in meters [x, y, z].</summary>
        public float[] position { get; set; } = new float[3];
    }
}
