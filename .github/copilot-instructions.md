# Copilot Instructions for Briko

> **Briko**: Block-based Level Construction Tool for Germio  
> A Unity Editor extension that bidirectionally converts 3D level scenes to/from LLM-friendly JSON.

## Build, Test & Lint

### Running Tests
```sh
dotnet test Tests~/IntegrationTests/IntegrationTests.csproj
```

**Single test by class:**
```sh
dotnet test Tests~/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~LayoutTests"
```

Test structure:
- `.NET 9.0` target framework with `<LangVersion>latest</LangVersion>`
- Tests compile shared sources from `Editor/Model/Layout.cs` and `Editor/Internal/*.cs`
- Test fixtures in `Tests~/IntegrationTests/Fixtures/` copied to output directory
- **No Unity API tests in v1** — Editor/Importer/Exporter (MonoBehaviour-dependent code) tested in v2 via Unity Test Framework

### No Linters or Builders
This is a UPM package (not a standalone app). It has no:
- `npm run build` / `npm run lint` equivalents
- Build output artifacts
- Formatting checkers

Tests are the primary validation. All public APIs are covered by NUnit tests.

---

## High-Level Architecture

### The Core Problem
LLMs hallucinate wildly on continuous 3D spatial reasoning (arbitrary XYZ coordinates, rotations, scales). **Briko solves this by discretizing the problem space**:

- **Prefab catalog** (finite list) instead of "place any model"
- **0.5m grid snapping** (integer multiples only) instead of arbitrary floating-point coordinates
- **4 cardinal rotations** (0°, 90°, 180°, 270°) instead of arbitrary angles
- **JSON arrays** (LLM's native abstraction) instead of 3D spatial reasoning

Result: Level design becomes an **autocomplete problem**, not an unsolved spatial hallucination problem.

### Bidirectional Conversion (Lossless)
```
Unity Scene ←→ level_layout.json

Scene → JSON: Export (serialize all Platform/Entity hierarchies)
JSON → Scene: Import (reconstruct scene from JSON, place prefabs, mark zones)
```

Losslessness is guaranteed because everything snaps to the discrete grid. You can export, let an LLM edit the JSON, re-import, and it reconstructs identically.

### The One Contract with Germio
- **Briko** (this tool): Handles **space** — prefab placements, grid snapping, scene construction
- **Germio** (sibling tool): Handles **logic** — state machines, rules, transitions, events

The **only connection** is a `zone_id` string in the JSON. When the player enters a Briko `zone`, Germio's runtime sees the zone event and decides what happens next. Neither tool knows anything else about the other.

### Namespace & Dependency Hierarchy
```
Briko.Editor.*              ← public API (Exporter, Importer, menus)
  ↓ depends on
Briko.Editor.Internal.*     ← utilities (GridSnapper, PrefabNameParser)
  ↓ depends on
Briko.Editor.Model.*        ← data classes (Root, Platform, Item, Zone)
```

**Strict rule**: `Briko → Germio` (one-way, allowed). `Germio → Briko` forbidden (would create circular dependency). `Briko → game-specific code` forbidden (Briko is generic, not Sprout Quest-specific).

---

## Key Conventions

### Naming (Strict Adherence to Stemic v2.2)

| Category | Rule | Example |
|----------|------|---------|
| **Class names** | Single word, no project prefix | `Exporter` (not `BrikoExporter`) |
| **Data class properties** | `snake_case` (matches JSON keys) | `layout_id`, `grid_unit`, `zone_id` |
| **Non-data public properties** | `camelCase` | N/A in v1 (minimal public API) |
| **Private fields** | `_snake_case` | `_grid_unit`, `_is_valid` |
| **Local variables** | `snake_case` | `raw_position`, `snapped_value` |
| **Constants** | `ALL_CAPS` | `PATTERN`, `GRID_UNIT` |
| **Method calls** | **Always use named parameters** | `Snap(raw: pos, grid_unit: 0.5f)` |

**JSON serialization**: Property names are JSON keys directly. **No `[JsonProperty]` attributes.**

### Data Model Pattern (One File, Multiple Classes)

All data model classes live in a **single file** (`Editor/Model/Layout.cs`):
```csharp
namespace Briko.Editor.Model {
    public class Root { ... }
    public class Platform { ... }
    public class Item { ... }
    public class Zone { ... }
}
```

This mirrors Stemic's `Data.cs` pattern (all scenario data in one file).

**Template for each class:**
```csharp
/// <summary>Human-readable description (one line, key responsibility).</summary>
/// <author>h.adachi (STUDIO MeowToon)</author>
public class ClassName {
#nullable enable
    /// <summary>Property documentation (one line).</summary>
    public string property_name { get; set; } = "";
}
```

### JSON Structure (Serialization Target)

```json
{
  "layout_id": "tropika_stage_01",
  "grid_unit": 0.5,
  "target_duration_sec": 180,
  "bgm_track": "track_01_tropika_morning.mp3",
  "platforms": [
    {
      "floor": "1f",
      "grounds": [{ "prefab": "Ground_10.0x0.5x10.0_Green", "variant": 1, "position": [0, 0, 0] }],
      "blocks": [{ "prefab": "Block_1.0x1.0x1.0_Plain_Green", "variant": 3, "position": [2, 0.5, 3], "rotation_y": 90 }],
      "zones": [{ "zone_id": "vol_boss_start", "position": [20, 0.5, 15] }]
    }
  ]
}
```

**Key invariants:**
- `position` is always `float[3]` with values that are **integer multiples of `grid_unit`** (0.5m)
- `rotation_y` is always `0`, `90`, `180`, or `270` (omitted if `0`)
- `variant` is always `≥ 1`
- `zone_id` matches regex `^vol_[a-z0-9_]+$` (lowercase + underscores)

### GridSnapper Pattern (Testable Pure Function)

`GridSnapper.Snap()` is a **pure static method** that converts between Unity's continuous `Vector3` and discrete `float[]`:

```csharp
// In Exporter.cs (Unity context):
Vector3 raw_position = some_game_object.transform.position;
float[] snapped = GridSnapper.Snap(
    raw: new[] { raw_position.x, raw_position.y, raw_position.z },
    grid_unit: 0.5f
);
```

This design **enables unit testing without Unity**. The test project imports `GridSnapper.cs` as shared source and can call it directly with `float[]` inputs.

### Prefab Naming Convention

Briko parses GameObject names to extract prefab identity and visual variant:

```
Ground_10.0x0.5x10.0_Green_1
     ↑    ↑ dimensions ↑  ↑
     |    +→ baked into prefab asset
     └─ type (Ground or Block)           └─ variant number (visual variation)
```

Parsing regex: `^(Ground|Block)_([\d.]+x[\d.]+x[\d.]+)_(.+)_(\d+)$`

On export: `PrefabNameParser.Parse(name)` returns `(prefab, variant)` tuple or `null`.
On import: Reconstruct full name as `"{prefab}_{variant}"` when instantiating.

### Test File Organization

```
Tests~/IntegrationTests/Scripts/
├── Model/
│   ├── LayoutTests.cs          ← tests for Root, Platform, Item, Zone
│   └── RoundTripTests.cs       ← export→import fidelity
└── Internal/
    ├── PrefabNameParserTests.cs
    └── GridSnapperTests.cs
```

**Test method naming**: `ClassName_Feature_ExpectedBehavior`  
Example: `Root_LayoutId_DeserializesFromJson()`

**Assertion pattern**: Use NUnit's fluent API
```csharp
Assert.That(value, Is.EqualTo(expected).Within(0.001f)); // floats with tolerance
Assert.That(root!.layout_id, Is.EqualTo("test_minimal"));
```

### Serialization (Newtonsoft.Json)

All tests and data models use **Newtonsoft.Json** (v13.0.3):

```csharp
private static readonly JsonSerializerSettings _settings = new() {
    Formatting = Formatting.Indented,
    NullValueHandling = NullValueHandling.Ignore,
    DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
};

Root root = JsonConvert.DeserializeObject<Root>(json, _settings);
string json = JsonConvert.SerializeObject(root, _settings);
```

### Assembly & Namespace Structure

**Editor assembly** (`Editor/Briko.Editor.asmdef`):
- Platform: Editor only
- References: `com.unity.nuget.newtonsoft-json`
- Namespaces: `Briko.Editor.*`

**Test project** (`Tests~/IntegrationTests/IntegrationTests.csproj`):
- Shares source compilation of `Editor/Model/Layout.cs` and `Editor/Internal/*.cs`
- Namespace: `Briko.Tests.*`
- **Never imports the compiled assembly** — uses shared source instead to avoid cyclic build dependency

---

## Code Style Notes

- **XML doc comments** on all public classes/methods (3 slashes `///`)
- **Author tag**: Always `<author>h.adachi (STUDIO MeowToon)</author>` in XML docs
- **`#nullable enable`** at the top of each class body
- **Section headers** in method groups:
  ```csharp
  ///////////////////////////////////////////////////////////////////////
  // Public methods [verb, verb phrase]
  
  ///////////////////////////////////////////////////////////////////////
  // Private methods
  ```
- **No blank lines inside method bodies** — compact, readable code
- **Early returns** for validation, no deeply nested blocks
- **No LINQ** (Stemic convention for performance in tight loops; data models are simple enough to use explicit loops)

---

## Design Philosophy (Critical)

### Why Discrete Over Continuous

The entire project rests on this observation:

> **"LLMs hallucinate wildly on continuous spatial reasoning, but are excellent at JSON manipulation."**

Every design decision — prefab catalog, 0.5m grid, 4 cardinal rotations — exists to **shrink the search space into LLM's native domain** (arrays, discrete choices).

This is non-negotiable. Do not add:
- Arbitrary floating-point coordinates
- 360° rotations
- Custom material properties
- Scale overrides
- Any feature that expands the continuous problem space

### Round-trip Losslessness

Export → Edit JSON → Import → Export must produce identical JSON.

This is tested by `RoundTripTests.cs`. If you change the model, you must verify this invariant still holds.

### One Tool, One Responsibility

**Briko** does **space only**. It does not:
- Define game rules (Germio's job)
- Manage state machines
- Handle player input
- Know about collectibles, enemies, or game logic

This separation is codified in the `zone_id` contract. Violating it breaks the ability to reuse Briko in other projects (and violates the stated design philosophy).

---

## References

- **README.md**: High-level vision, quick start, JSON examples
- **docs/briko_spec.md**: Design rationale (the "why")
- **docs/development_plan_v1_detail_JP.md**: Implementation details and task breakdown
- **Germio repo**: Sibling tool; Briko has one-way dependency but never imports Germio code in v1

---

## Status

- **Current version**: v0.1.0 (Phase 1 Complete)
- **Test coverage**: 18/18 passing
- **Next phase** (v2): JSON Schema lock, validator (zone_id sync), PlayMode integration tests
