# Helper Instructions for Briko

> **Briko**: Block-based Level Construction Tool for Germio  
> A Unity Editor extension that both ways converts solid level scenes to/from LLM-friendly JSON.

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

+ `.NET 9.0` target framework with `<LangVersion>latest</LangVersion>`
+ Tests build shared sources from `Editor/Model/Layout.cs` and `Editor/Internal/*.cs`
+ Test files in `Tests~/IntegrationTests/Fixtures/` copied to output folder
+ **No Unity API tests in v1** — Editor/Importer/Exporter (MonoBehaviour-dependent code) tested in v2 via Unity Test Framework

### No Linters or Builders

This is a Unity package (not a separate program). It has no:

+ `npm run build` / `npm run lint` equivalents
+ Build output files
+ Layout checks

Tests are the primary check. All public APIs are covered by NUnit tests.

---

## High-Level Architecture

### The Core Problem

Models do badly with continuous solid space (any XYZ places, turns, scales). **Briko solves this by cutting the problem space**:

+ **part catalog** (finite list) instead of "place any model"
+ **0.25m grid snapping** (whole number multiples only) instead of any floating-point places
+ **4 main turns** (0°, 90°, 180°, 270°) instead of any angles
+ **JSON lists** (model's native idea) instead of solid spatial reasoning

Result: Level design becomes **a problem with one right answer**, not an open solid guessing problem.

### Two-way change (full)

```text
Unity Scene ←→ level_layout.json

Scene → JSON: Export (serialize all Platform/Entity hierarchies)
JSON → Scene: Import (reconstruct scene from JSON, place prefabs, mark zones)
```

Nothing is lost, because everything sits on the separate grid. You can export, let a model edit the JSON, re-import, and it builds the same.

### The One Contract with Germio

+ **Briko** (this tool): Handles **space** — part places, grid snapping, scene construction
+ **Germio** (sibling tool): Handles **logic** — state machines, rules, transitions, events

The **only connection** is a `zone_id` string in the JSON. When the player enters a Briko `zone`, Germio's run time sees the zone event and decides what happens next. Neither tool knows anything else about the other.

### Group & Need Tree

```text
Briko.Editor.*              ← public API (Exporter, Importer, menus)
  ↓ depends on
Briko.Editor.Internal.*     ← utilities (GridSnapper, PrefabNameParser)
  ↓ depends on
Briko.Editor.Model.*        ← data classes (Root, Platform, Item, Zone)
```

**Strict rule**: `Briko → Germio` (one-way, allowed). `Germio → Briko` banned (would create a loop of needs). `Briko → game-specific code` banned (Briko is common, not Sprout Quest-specific).

---

## Key Conventions

### Naming (Strict Adherence to Stemic v2.2)

| Category                       | Rule                             | Example                             |
| ------------------------------ | -------------------------------- | ----------------------------------- |
| **Class names**                | Single word, no project prefix   | `Exporter` (not `BrikoExporter`)    |
| **Data class properties**      | `snake_case` (matches JSON keys) | `layout_id`, `grid_unit`, `zone_id` |
| **Not-data public properties** | `camelCase`                      | N/A in v1 (small public API)        |
| **Private fields**             | `_snake_case`                    | `_grid_unit`, `_is_valid`           |
| **Local variables**            | `snake_case`                     | `raw_position`, `snapped_value`     |
| **Constants**                  | `ALL_CAPS`                       | `PATTERN`, `GRID_UNIT`              |
| **Method calls**               | **Always use named inputs**      | `Snap(raw: pos, grid_unit: 0.25f)`  |

**JSON saving**: Property names are JSON keys directly. **No `[JsonProperty]` attributes.**

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

**Pattern for each class:**

```csharp
/// <summary>Human-readable description (one line, key responsibility).</summary>
/// <author>h.adachi (STUDIO MeowToon)</author>
public class ClassName {
#nullable enable
    /// <summary>Property documentation (one line).</summary>
    public string property_name { get; set; } = "";
}
```

### JSON Structure (saving Target)

```json
{
  "layout_id": "tropika_stage_01",
  "grid_unit": 0.25,
  "target_duration_second": 180,
  "background_music_track": "track_01_tropika_morning.mp3",
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

**Key rules:**

+ `position` is always `float[3]` with values that are **whole number multiples of `grid_unit`** (0.25m)
+ `rotation_y` is always `0`, `90`, `180`, or `270` (omitted if `0`)
+ `variant` is always `≥ 1`
+ `zone_id` matches pattern `^vol_[a-z0-9_]+$` (small letter + marks)

### GridSnapper Pattern (Testable Pure Function)

`GridSnapper.Snap()` is a **pure static method** that converts between Unity's continuous `Vector3` and separate `float[]`:

```csharp
// In Exporter.cs (Unity context):
Vector3 raw_position = some_game_object.transform.position;
float[] snapped = GridSnapper.Snap(
    raw: new[] { raw_position.x, raw_position.y, raw_position.z },
    grid_unit: 0.25f
);
```

This design **enables unit testing without Unity**. The test project imports `GridSnapper.cs` as shared source and can call it directly with `float[]` inputs.

### part Naming Convention

Briko reads GameObject names to extract part identity and visual variant:

```text
Ground_10.0x0.5x10.0_Green_1
     ↑    ↑ dimensions ↑  ↑
     |    +→ baked into prefab asset
     └─ type (any word: Ground, Block, Enemy, ...)  └─ variant number (visual variation)
```

Reading pattern: `^(.+_([\d.]+x[\d.]+x[\d.]+)_.+)_(\d+)$`

On export: `PrefabNameParser.Parse(name)` returns `(prefab, variant)` pair or `null`.
On import: search all parts by file name exact match on `item.prefab` (variant not added).

### Test File Organization

```text
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

**Check pattern**: Use NUnit's easy API

```csharp
Assert.That(value, Is.EqualTo(expected).Within(0.001f)); // floats with tolerance
Assert.That(root!.layout_id, Is.EqualTo("test_minimal"));
```

### saving (Newtonsoft.Json)

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

### Build & Group Structure

**Editor build** (`Editor/Briko.Editor.asmdef`):

+ Platform: Editor only
+ References: `com.unity.nuget.newtonsoft-json`
+ Groups: `Briko.Editor.*`

**Test project** (`Tests~/IntegrationTests/IntegrationTests.csproj`):

+ Shares source building of `Editor/Model/Layout.cs` and `Editor/Internal/*.cs`
+ Group: `Briko.Tests.*`
+ **Never imports the built build** — uses shared source instead to avoid a build loop

---

## Code Style Notes

+ **XML document comments** on all public classes/methods (3 slashes `///`)
+ **Author tag**: Always `<author>h.adachi (STUDIO MeowToon)</author>` in XML files
+ **`#nullable enable`** at the top of each class body
+ **Section titles** in method groups:

  ```csharp
  ///////////////////////////////////////////////////////////////////////
  // Public methods [verb, verb phrase]
  
  ///////////////////////////////////////////////////////////////////////
  // Private methods
  ```

+ **No blank lines inside method bodies** — short, clear code
+ **Early returns** for check, no deeply nested blocks
+ **No LINQ** (Stemic convention for performance in tight loops; data models are simple enough to use explicit loops)

---

## Design Philosophy (Critical)

### Why separate Over Continuous

The entire project rests on this observation:

> **"Models do badly with continuous spatial work, but are excellent at changing JSON."**

Every design decision — part catalog, 0.25m grid, 4 main turns — exists to **get smaller the search space into model's native field** (lists, separate choices).

This is closed. Do not add:

+ any floating-point places
+ 360° turns
+ Custom material properties
+ Scale changes that replace a part's size
+ Any feature that expands the continuous problem space

### Round-trip with nothing lost

Export → Edit JSON → Import → Export must produce same JSON.

This is tested by `RoundTripTests.cs`. If you change the model, you must check this rule still holds.

### One Tool, One Responsibility

**Briko** does **space only**. It does not:

+ Define game rules (Germio's job)
+ Manage state machines
+ Handle player input
+ Know about items, enemies, or game logic

This split is written down in the `zone_id` contract. Breaking it breaks the ability to reuse Briko in other projects (and breaks the written design philosophy).

---

## References

+ **`README.md`**: High-level vision, quick start, JSON examples
+ **docs/briko_plan.md**: Design reason (the "why")
+ **docs/development_plan_v1_detail_JP.md**: Implementation details and task list
+ **Germio repo**: Sibling tool; Briko has one-way need but never imports Germio code in v1

---

## Status

+ **Current version**: v0.1.0 (Phase 1 Complete)
+ **Test coverage**: 19/19 passing
+ **Next phase** (v2): JSON Layout lock, check (zone_id match), PlayMode integration tests
