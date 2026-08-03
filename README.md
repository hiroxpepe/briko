# Briko

> 🧱 **Block-based Level Construction Tool for Germio**
>
> LLM-driven Unity level generation via bidirectional Scene ↔ JSON conversion.

[![Unity](https://img.shields.io/badge/Unity-6%20LTS-black?logo=unity)](https://unity.com/)
![Phase](https://img.shields.io/badge/phase-1-blue)
![Version](https://img.shields.io/badge/version-v0.1.9-orange)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

---

## What is Briko?

**Briko** (Esperanto for *brick* 🧱) is a Unity Editor extension that lets you serialize a 3D level scene into clean structured JSON — and reconstruct it back into a working Unity scene. The JSON is designed to be **read and written by Large Language Models**, which means you can hand a LLM your existing level and ask it to generate variations, sequels, or entire new stages.

Briko is the **set-design half** of a two-tool framework. Its sibling [Germio](https://github.com/hiroxpepe/germio) handles scenario logic (state machines, rules, transitions). Together they enable LLM-driven game development without losing creative control.

```mermaid
graph LR
    Scene[🎮 Unity Scene<br/>Level 1] -->|Briko Export| JSON1[📄 level_layout.json]
    JSON1 -->|✨ LLM generates variant| JSON2[📄 level_layout_v2.json]
    JSON2 -->|Briko Import| Scene2[🎮 Unity Scene<br/>Level 2]

    style Scene fill:#90ee90
    style Scene2 fill:#90ee90
    style JSON1 fill:#fff9c4
    style JSON2 fill:#fff9c4
```

---

## Why Briko?

> **"LLMs cannot meaningfully place 3D models in space."**

This was the founding observation. Ask Claude or GPT to "design a Mario level" and you get 60 mismatched coordinates with floating platforms and impossible jumps. The hallucination rate on continuous spatial reasoning is brutal.

Briko sidesteps the problem by **shrinking the LLM's freedom**:

| Continuous problem                   | Briko's discrete reformulation               |
| ------------------------------------ | -------------------------------------------- |
| "Place a block somewhere reasonable" | Pick from a fixed prefab catalog             |
| "Choose XYZ coordinates"             | Snap to 0.25m grid (integer multiples only)  |
| "Set the rotation"                   | Choose from `{0°, 90°, 180°, 270°}`          |
| "Imagine a full 3D scene"            | Edit JSON arrays (a thing LLMs are great at) |

By constraining the search space to **a discrete vocabulary the LLM can actually reason over**, Briko turns level design from an unsolved problem into an **autocomplete problem**.

---

## Position in the STUDIO MeowToon Ecosystem

Briko is one tool in a larger picture. The center is music; tools serve content; content reaches people.

```mermaid
graph TB
    Music[🎵 Music<br/>The creative center]

    subgraph Tools["🛠️ Tools (self-use, OSS)"]
        Germio[Germio<br/>Scenario framework]
        Briko[Briko<br/>Level construction]
        GenToon[GenToon<br/>Comic distribution]
    end

    subgraph Content["📦 Content (monetized)"]
        SQ[Sprout Quest<br/>The game]
        Comics[4-panel comics]
        BGM[Original soundtrack]
    end

    Music --> Tools
    Tools --> Content
    Germio -.zone_id.-> Briko

    SQ -.uses.-> Germio
    SQ -.uses.-> Briko
    Comics -.distributed by.-> GenToon

    classDef center fill:#ff6b6b,stroke:#000,color:#fff
    classDef tool fill:#4ecdc4,stroke:#000
    classDef content fill:#ffe66d,stroke:#000

    class Music center
    class Germio,Briko,GenToon tool
    class SQ,Comics,BGM content
```

---

## Briko vs Germio: Storyboard vs Set Design

The cleanest mental model: **Germio writes the script, Briko builds the stage**. They never overlap.

```mermaid
graph TB
    subgraph Germio["📜 Germio = Storyboard (logic)"]
        G1[State - flags, counters, inventory]
        G2[Rule - event conditions]
        G3[Command - actions]
        G4[Next - scene transitions]
    end

    subgraph Briko["🏗️ Briko = Set Design (space)"]
        B1[Block - prefab placements]
        B2[Floor - hierarchical layers]
        B3[Grid - 0.25m discrete units]
    end

    G2 -.zone_id string.-> B1

    style G1 fill:#ffd1dc
    style G2 fill:#ffd1dc
    style G3 fill:#ffd1dc
    style G4 fill:#ffd1dc
    style B1 fill:#d1e7ff
    style B2 fill:#d1e7ff
    style B3 fill:#d1e7ff
```

The **only contract** between them is a `zone_id` string. When the player enters a Briko zone, Germio's runtime sees a `zone_id` event and decides what happens. Neither side knows anything else about the other.

This separation is non-negotiable. Combining the two breaks both.

---

## How It Works

### The Bidirectional Flow

```mermaid
flowchart LR
    A[🏗️ Hand-crafted<br/>Unity Scene] -->|Export| B[📄 level_layout.json]
    B -->|🤖 LLM generates<br/>variant| C[📄 level_layout_v2.json]
    C -->|Import| D[🎮 New Unity Scene]
    D -->|✏️ Manual tweaks| E[🎮 Polished Scene]
    E -->|Re-Export| F[📄 Updated JSON]
    F -.feeds back.-> C

    style B fill:#fff9c4
    style C fill:#fff9c4
    style F fill:#fff9c4
    style A fill:#90ee90
    style D fill:#90ee90
    style E fill:#90ee90
```

### Why "Round-trip" Matters

Most level generators are **one-way**: a tool spits out a level, and the moment a human edits it in Unity, the source-of-truth diverges. Briko fixes this by treating **Scene → JSON** as a first-class operation, not just an afterthought.

This is what enables the iterative LLM workflow: human and LLM take turns editing, and the JSON stays canonical.

The discreteness guarantees lossless conversion:

```mermaid
graph LR
    A[Scene] -->|Export| B[JSON]
    B -->|Import| C["Scene'"]
    A -.lossless equality.-> C

    style A fill:#c8e6c9
    style B fill:#fff9c4
    style C fill:#c8e6c9
```

---

## Quick Start

### Prerequisites

+ **Unity 6 LTS** or **Unity 2022.3+**
+ **.NET 9 SDK** (only for running the test suite)

### Installation (UPM `file:` reference)

In your Unity project's `Packages/manifest.json`:

```jsonc
{
  "dependencies": {
    "com.meowtoon.briko": "file:../../briko",
    "com.unity.nuget.newtonsoft-json": "3.2.1"
  }
}
```

Adjust the relative path based on where you cloned this repository.

### Usage

Once installed, two menu items appear in the Unity Editor:

```mermaid
graph LR
    Menu[Tools menu] --> Briko[Briko submenu]
    Briko --> Export[📤 Export Active Scene to JSON...]
    Briko --> Import[📥 Import JSON to New Scene...]

    style Briko fill:#4ecdc4,stroke:#000
    style Export fill:#ffe66d,stroke:#000
    style Import fill:#ffe66d,stroke:#000
```

+ **Export**: dumps the active scene's `Platform` and `Entity` hierarchies into a JSON file.
+ **Import**: reads a JSON file and constructs a new scene with prefabs placed and zones marked.

---

## JSON Format

Briko's JSON is human-readable and LLM-friendly. Here's the minimal shape:

```json
{
  "layout_id": "tropika_stage_01",
  "grid_unit": 0.25,
  "target_duration_sec": 180,
  "bgm_track": "track_01_tropika_morning.mp3",
  "platforms": [
    {
      "floor": "1f",
      "grounds": [
        {
          "prefab": "Ground_10.0x0.5x10.0_Green",
          "variant": 1,
          "position": [0, 0, 0]
        }
      ],
      "blocks": [
        {
          "prefab": "Block_1.0x1.0x1.0_Plain_Green",
          "variant": 3,
          "position": [2, 0.5, 3]
        }
      ],
      "zones": [
        {
          "zone_id": "vol_boss_start",
          "position": [20, 0.5, 15]
        }
      ]
    }
  ]
}
```

### Data Model

```mermaid
graph TB
    Root[Root<br/>📋 layout_id, grid_unit,<br/>target_duration_sec, bgm_track]
    Root -->|"platforms[]"| P[Platform<br/>🏢 floor]
    P -->|"grounds[]"| I1[Item<br/>🟩 prefab, variant,<br/>position, rotation_y]
    P -->|"blocks[]"| I2[Item<br/>🟦 prefab, variant,<br/>position, rotation_y]
    P -->|"zones[]"| Z[Zone<br/>🔔 zone_id, position]

    Z -.synced with.-> Germio[Germio<br/>germio.json]

    style Root fill:#fff9c4
    style P fill:#bbdefb
    style I1 fill:#c5e1a5
    style I2 fill:#90caf9
    style Z fill:#ffccbc
    style Germio fill:#f8bbd0
```

### Design Principles

| Principle                           | Rationale                                                                          |
| ----------------------------------- | ---------------------------------------------------------------------------------- |
| **Integer multiples of grid_unit**  | Float drift is eliminated; LLMs can never produce "almost on the grid" placements  |
| **rotation_y ∈ {0, 90, 180, 270}**  | Discretization makes intent unambiguous                                            |
| **Prefab name + variant separated** | LLM picks from a finite catalog; visual variation is decoupled from spatial choice |
| **No materials, no scales**         | Already baked into prefabs; nothing for the LLM to hallucinate                     |

---

## Project Structure

```text
briko/
├── Editor/                              ← all package code is editor-only
│   ├── Briko.Editor.asmdef             ← assembly definition (Editor platform only)
│   ├── Exporter.cs                     ← Scene → Root
│   ├── ExportMenu.cs                   ← Tools/Briko/Export menu wiring
│   ├── Importer.cs                     ← Root → Scene
│   ├── ImportMenu.cs                   ← Tools/Briko/Import menu wiring
│   ├── Internal/
│   │   ├── PrefabNameParser.cs         ← parses naming convention regex
│   │   └── GridSnapper.cs              ← 0.25m discrete snapping
│   └── Model/
│       └── Layout.cs                   ← Root, Platform, Item, Zone (single file)
├── Tests~/                              ← UPM convention: hidden from Unity
│   └── IntegrationTests/                  but visible to dotnet tooling
│       ├── IntegrationTests.csproj
│       ├── Fixtures/
│       │   └── sample_level_minimal.json
│       └── Scripts/
│           ├── Internal/
│           │   ├── PrefabNameParserTests.cs
│           │   └── GridSnapperTests.cs
│           └── Model/
│               ├── LayoutTests.cs
│               └── RoundTripTests.cs
├── docs/
│   ├── briko_spec.md                   ← design specification (the why)
│   └── development_plan_v1_detail_JP.md ← implementation plan (the how)
├── package.json
└── README.md
```

### Namespace Layers

```mermaid
graph TB
    subgraph Editor["Briko.Editor — Editor extension"]
        Exp[Exporter]
        Imp[Importer]
        EM[ExportMenu]
        IM[ImportMenu]
    end

    subgraph Internal["Briko.Editor.Internal — utilities"]
        Parse[PrefabNameParser]
        Snap[GridSnapper]
    end

    subgraph Model["Briko.Editor.Model — data classes"]
        Root[Root]
        Plat[Platform]
        Item[Item]
        Zone[Zone]
    end

    subgraph Tests["Briko.Tests.* — test suites"]
        TestI[Briko.Tests.Internal]
        TestM[Briko.Tests.Model]
    end

    Editor --> Internal
    Editor --> Model
    Tests --> Internal
    Tests --> Model

    style Editor fill:#d1e7ff
    style Internal fill:#fff9c4
    style Model fill:#c5e1a5
    style Tests fill:#ffccbc
```

---

## Dependency Direction (Strict Rule)

```mermaid
graph LR
    Briko -->|✅ may reference| Germio
    Germio -.❌ never references.-> Briko
    Briko -.❌ never references.-> GameDev[Game-specific code]

    style Briko fill:#d1e7ff
    style Germio fill:#ffd1dc
    style GameDev fill:#ffe0b2
```

+ **Briko → Germio**: one-way reference allowed (currently unused in v1)
+ **Germio → Briko**: forbidden (would create circular dependency)
+ **Briko → game-specific code**: forbidden (Briko is a generic tool, not Sprout Quest's helper)

---

## Naming Conventions

Briko follows the conventions of [Stemic](https://github.com/hiroxpepe/stemic) (the parent game project) precisely. Key rules:

| Element                          | Convention                       | Example                            |
| -------------------------------- | -------------------------------- | ---------------------------------- |
| Class name                       | Single word, no project prefix   | `Exporter`, not `BrikoExporter`    |
| Public properties (data classes) | `snake_case` (matches JSON keys) | `layout_id`, `grid_unit`           |
| Public properties (other)        | `camelCase`                      | `home`, `beat`, `mode`             |
| Private fields                   | `_snake_case`                    | `_do_update`, `_jump_power`        |
| Local variables / parameters     | `snake_case`                     | `base_path`, `grid_unit`           |
| Constants                        | `ALL_CAPS`                       | `GRID_UNIT`, `MENU_ROOT`           |
| Method calls (project-defined)   | **Always use named parameters**  | `Snap(raw: pos, grid_unit: 0.25f)` |

JSON serialization uses no `[JsonProperty]` attributes — property names are the JSON keys directly.

---

## Development

### Running Tests

```sh
dotnet test Tests~/IntegrationTests/IntegrationTests.csproj
```

Single test:

```sh
dotnet test Tests~/IntegrationTests/IntegrationTests.csproj --filter "FullyQualifiedName~LayoutTests"
```

The test project shares source compilation with `Editor/Model/Layout.cs` and the `Internal/` utilities, so you can test pure C# logic without spinning up Unity.

### Test Coverage Pattern

Briko follows Stemic's **1:1 test mapping** rule:

```mermaid
graph LR
    L[Layout.cs] -.->|tested by| LT[LayoutTests.cs]
    P[PrefabNameParser.cs] -.->|tested by| PT[PrefabNameParserTests.cs]
    G[GridSnapper.cs] -.->|tested by| GT[GridSnapperTests.cs]
    All[All Layout classes] -.->|cross-cutting| RT[RoundTripTests.cs]

    Exp[Exporter.cs / Importer.cs] -.testless<br/>Unity API dependent.-> X[v2: PlayMode tests]

    style L fill:#c5e1a5
    style P fill:#fff9c4
    style G fill:#fff9c4
    style Exp fill:#ffcdd2
    style X fill:#ffe0b2
```

Unity-API-dependent classes (Exporter, Importer, menus) have no NUnit tests in v1, matching Stemic's convention for `MonoBehaviour`-class code (CameraSystem, GameSystem, etc.). Integration tests for these arrive in v2 via Unity Test Framework.

---

## Roadmap

```mermaid
gantt
    title Briko Phase Plan
    dateFormat YYYY-MM
    section Phase 1 (v1)
    Repo skeleton          :done, p1a, 2026-04, 2w
    Data model + Exporter  :done, p1b, 2026-04, 2w
    Importer + tests       :done, p1c, 2026-04, 2w
    Manual round-trip demo :active, p1d, 2026-05, 4w

    section Phase 2 (v2)
    JSON Schema lock         :p2a, 2026-06, 4w
    Validator (zone_id, etc) :p2b, 2026-07, 4w
    PlayMode integration tests :p2c, 2026-07, 4w
    English/Japanese READMEs :p2d, 2026-08, 2w

    section Phase 3 (v3)
    Mass production for Tropika :p3a, 2026-09, 12w
    SoundSystem integration     :p3b, 2026-10, 8w
    Auto-variant selection      :p3c, 2026-11, 4w
```

### Current Status: **v0.1.0 — Phase 1 Complete** ✅

+ ✅ Bidirectional Scene ↔ JSON conversion working
+ ✅ 18/18 tests passing
+ ✅ Stemic conventions enforced
+ ⏳ Manual round-trip validation in progress (Tasks 5-6)

See [`docs/development_plan_v1_detail_JP.md`](docs/development_plan_v1_detail_JP.md) for detailed implementation status.

---

## License

MIT — see [LICENSE](LICENSE).

---

## Background

Briko is the second tool in a 4-year arc. After [Germio](https://github.com/hiroxpepe/germio) crystallized the LLM-first scenario framework over more than 20 iterations, Briko was conceived to handle the spatial half — the part Germio deliberately refused to absorb.

The driving question:

> **"Can a single creator with an LLM produce a complete 3D platformer at the production scale of a 1990s studio?"**

The answer is gated on **whether level construction can be automated without spatial hallucination**. Briko is the bet on that answer.

The target is [Sprout Quest](https://github.com/hiroxpepe/sprout-quest), a 4-years-in-the-making revenge for an unfinished first attempt. Built on Germio for logic, populated by Briko for space, scored by hand-crafted music.

Rome wasn't built in a day. But Rome **does** eventually get built.

---

> *"LLMs cannot place 3D models in space — but they can write JSON. So we make space into JSON."*

🐱 **STUDIO MeowToon** — 2026
