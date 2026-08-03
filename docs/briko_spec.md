# Briko Specification

> **Block-based Level Construction Tool for Germio**
>
> A Unity Editor extension enabling LLM-driven level generation through bidirectional Scene ↔ JSON conversion.

---

**Document Version**: 1.1
**Last Updated**: 2026-05-05
**Status**: Stable (v1 implementation complete)
**Companion to**: [Germio Framework](https://github.com/hiroxpepe/germio)

---

## Table of Contents

```mermaid
graph LR
    Spec[Briko Spec<br/>this document] --> Plan[Development Plan<br/>implementation guide]
    Spec --> README[README.md<br/>user guide]
    Plan --> Code[Editor source<br/>v1 implementation]
    Code --> Tests[Test suite<br/>NUnit]

    style Spec fill:#fff9c4
    style Plan fill:#bbdefb
    style README fill:#c5e1a5
    style Code fill:#ffccbc
    style Tests fill:#c8e6c9
```

| Section | Topic                      |
| ------- | -------------------------- |
| 1       | Overview                   |
| 2       | Design Philosophy          |
| 3       | Relationship with Germio   |
| 4       | Coordinate System and Grid |
| 5       | Prefab Naming Convention   |
| 6       | Scene Hierarchy            |
| 7       | Layout JSON Format         |
| 8       | Bidirectional Converter    |
| 9       | Repository Structure       |
| 10      | Coding Conventions         |
| 11      | Common Layout Patterns     |
| 12      | LLM Workflow Models        |
| 13      | Failure Mode Catalog       |
| 14      | Roadmap                    |
| 15      | References                 |

---

## 1. Overview

### 1.1 What Briko Is

Briko is a Unity Editor extension that:

1. **Serializes** an existing Unity scene into structured JSON (the `Layout` document).
2. **Reconstructs** a Unity scene from a `Layout` document.
3. Maintains **lossless round-trip** between the two representations through strict discrete constraints.

The JSON format is designed to be read and written by Large Language Models (LLMs).

```mermaid
graph LR
    Scene[Unity Scene] -->|Export| JSON[Layout JSON]
    JSON -->|LLM edits| JSON2[Modified Layout JSON]
    JSON2 -->|Import| Scene2[New Unity Scene]

    style Scene fill:#90ee90
    style Scene2 fill:#90ee90
    style JSON fill:#fff9c4
    style JSON2 fill:#fff9c4
```

### 1.2 What Briko Is Not

| Briko is not                      | Justification                                                                   |
| --------------------------------- | ------------------------------------------------------------------------------- |
| A runtime level editor            | All operations execute in the Unity Editor only                                 |
| A procedural generator            | Briko has no random-generation logic; it transforms between two representations |
| An ML-based generator             | Briko provides no learned model; LLM intelligence is external                   |
| A scenario / state machine engine | That responsibility belongs to Germio                                           |
| An asset creation tool            | Prefabs must already exist in the host project                                  |
| A universal level format          | The format is specific to a constrained genre and prefab convention             |

### 1.3 Position Relative to Germio

```mermaid
graph TB
    subgraph "Germio — scenario logic"
        G1[State - flags, counters, inventory]
        G2[Rule - event conditions]
        G3[Command - actions]
        G4[Next - scene transitions]
    end

    subgraph "Briko — spatial layout"
        B1[Block - prefab placement]
        B2[Floor - hierarchical layers]
        B3[Grid - discrete coordinates]
        B4[Zone - trigger volume markers]
    end

    G2 -.zone_id string contract.-> B4

    style G1 fill:#ffd1dc
    style G2 fill:#ffd1dc
    style G3 fill:#ffd1dc
    style G4 fill:#ffd1dc
    style B1 fill:#d1e7ff
    style B2 fill:#d1e7ff
    style B3 fill:#d1e7ff
    style B4 fill:#d1e7ff
```

**Single point of contact**: a `zone_id` string. Beyond that, Germio and Briko are independent.

### 1.4 Comparison with Alternatives

```mermaid
graph TB
    subgraph "Briko"
        BR[3D + LLM-edit + bidirectional + discrete]
    end

    subgraph "Existing approaches"
        UI[UI-driven editors<br/>Mario Maker style]
        Manual[Manual 3D modeling<br/>ProBuilder style]
        Proc[Procedural generation<br/>algorithmic]
        ML[ML-based generation<br/>learned models]
        Script[Scripted construction<br/>Roblox / Lua]
    end

    style BR fill:#ffd700,stroke:#000,stroke-width:3px
    style UI fill:#ffcccc
    style Manual fill:#ffcccc
    style Proc fill:#fff4cc
    style ML fill:#fff4cc
    style Script fill:#fff4cc
```

| Approach              | LLM-friendly | Bidirectional | Deterministic | Explainable |
| --------------------- | ------------ | ------------- | ------------- | ----------- |
| UI-driven editor      | No           | No            | Yes           | Yes         |
| Manual 3D modeling    | No           | No            | Yes           | Yes         |
| Procedural generation | Partial      | No            | No (random)   | Partial     |
| ML-based generation   | Partial      | No            | No            | No          |
| Scripted construction | Partial      | No            | Yes           | Yes         |
| **Briko**             | **Yes**      | **Yes**       | **Yes**       | **Yes**     |

---

## 2. Design Philosophy

### 2.1 The LLM Spatial Reasoning Problem

LLMs operate on token sequences. 3D space is a continuous coordinate field. The translation between them is unreliable when the LLM is asked to produce coordinates directly.

```mermaid
graph TB
    LLM[LLM nature]
    LLM --> T1[1-D token sequence]
    T1 --> T2[Local probability distribution]

    Space[3D space nature]
    Space --> S1[3-D Euclidean coordinates]
    S1 --> S2[Continuous values + geometric constraints]

    T2 -.unreliable mapping.-> S2

    style LLM fill:#bbdefb
    style Space fill:#c5e1a5
    style T2 fill:#fff9c4
    style S2 fill:#fff9c4
```

Common failure modes when an LLM is asked to produce 3D coordinates directly:

```mermaid
graph TB
    Direct["Prompt: produce coordinates for a level"]

    Direct --> F1["Floating geometry<br/>(unreachable platforms)"]
    Direct --> F2["Coordinate collisions<br/>(overlapping prefabs)"]
    Direct --> F3["Impossible jump distances"]
    Direct --> F4["Off-grid placements<br/>(0.314m, 1.732m, ...)"]
    Direct --> F5["Inconsistent rotations<br/>(37°, 142°, ...)"]

    style Direct fill:#fff9c4
    style F1 fill:#ffcdd2
    style F2 fill:#ffcdd2
    style F3 fill:#ffcdd2
    style F4 fill:#ffcdd2
    style F5 fill:#ffcdd2
```

### 2.2 The Discrete Reformulation

Briko's approach: replace each continuous degree of freedom with a discrete choice.

```mermaid
graph LR
    subgraph "Continuous problem"
        C1[XYZ coordinates - real numbers]
        C2[Rotation - real angle]
        C3[Scale - real number]
        C4[Prefab choice - infinite]
    end

    subgraph "Briko discretization"
        D1["position - 0.25m integer multiples"]
        D2["rotation_y - {0, 90, 180, 270}"]
        D3[Scale - fixed, encoded in prefab name]
        D4[Prefab - finite catalog]
    end

    C1 -.snap.-> D1
    C2 -.quantize.-> D2
    C3 -.bake into prefab.-> D3
    C4 -.constrain to catalog.-> D4

    style C1 fill:#ffcdd2
    style C2 fill:#ffcdd2
    style C3 fill:#ffcdd2
    style C4 fill:#ffcdd2
    style D1 fill:#c8e6c9
    style D2 fill:#c8e6c9
    style D3 fill:#c8e6c9
    style D4 fill:#c8e6c9
```

The principle: **reducing the LLM's freedom raises output quality** by making invalid outputs unrepresentable.

### 2.3 Bidirectional as a First-Class Property

Most level generators are unidirectional: tool emits a scene, and any subsequent human edit causes the source-of-truth to diverge from the original input.

Briko's solution: treat **Scene → JSON** as a primary operation, equal in importance to **JSON → Scene**.

```mermaid
graph LR
    Scene[Scene]
    JSON1[JSON₁]
    Scene2["Scene'"]
    JSON2[JSON₂]

    Scene -->|Export| JSON1
    JSON1 -->|Import| Scene2
    Scene2 -->|Re-Export| JSON2

    JSON1 -.semantic equivalence.-> JSON2

    style Scene fill:#c8e6c9
    style Scene2 fill:#c8e6c9
    style JSON1 fill:#fff9c4
    style JSON2 fill:#fff9c4
```

The discrete constraints from §2.2 mathematically guarantee this round-trip property (see §8.3).

### 2.4 LLM-First Design Principles

Briko inherits a subset of design principles from Germio's LLM-First framework:

| Principle                      | Application in Briko                               |
| ------------------------------ | -------------------------------------------------- |
| Closed minimal vocabulary      | Fixed prefab catalog, fixed rotation values        |
| Declarative not procedural     | JSON describes state, not steps                    |
| Self-correcting error format   | Failed parse logs warning and skips, never crashes |
| snake_case throughout          | All JSON keys and C# data class properties         |
| Public JSON schema             | The format is the API                              |
| Layered namespace architecture | `Briko.Editor.{Model, Internal}`                   |

---

## 3. Relationship with Germio

### 3.1 Role Separation

Germio and Briko are designed to **never overlap** in scope:

```mermaid
graph TB
    subgraph "Germio responsibility"
        GR1[Game state]
        GR2[Scene transition logic]
        GR3[Event condition evaluation]
        GR4[Save / load]
    end

    subgraph "Briko responsibility"
        BR1[Prefab placement]
        BR2[Spatial hierarchy]
        BR3[Trigger zone markers]
        BR4[Round-trip serialization]
    end

    subgraph "Shared interface"
        SI[zone_id string]
    end

    GR3 --> SI
    BR3 --> SI

    style GR1 fill:#ffd1dc
    style GR2 fill:#ffd1dc
    style GR3 fill:#ffd1dc
    style GR4 fill:#ffd1dc
    style BR1 fill:#d1e7ff
    style BR2 fill:#d1e7ff
    style BR3 fill:#d1e7ff
    style BR4 fill:#d1e7ff
    style SI fill:#fff9c4
```

### 3.2 The zone_id Contract

The single string `zone_id` is the contract between Germio and Briko.

**Naming convention**: `zone_id` matches the regex `^vol_[a-z0-9_]+$`.

The `vol_` prefix denotes "volumetric trigger" — a 3D region that emits an event when the player enters it.

```mermaid
graph TB
    Root[zone_id namespace]

    Root --> V[vol_*<br/>volumetric triggers<br/>v1 supported]

    V --> V1[vol_spawn<br/>player spawn point]
    V --> V2[vol_boss_start<br/>boss encounter]
    V --> V3[vol_secret_*<br/>hidden areas]
    V --> V4[vol_checkpoint_*<br/>save points]
    V --> V5[vol_exit<br/>level completion]

    Root --> Sig[sig_*<br/>signals - reserved for v2]
    Root --> Pt[pt_*<br/>points - reserved for v3]

    style V fill:#c8e6c9
    style Sig fill:#fff9c4
    style Pt fill:#ffccbc
```

**v1 supports `vol_*` only.** Other prefixes are reserved for future expansion.

### 3.3 Runtime Data Flow

```mermaid
sequenceDiagram
    participant LLM
    participant Germio as germio.json
    participant Briko as level_layout.json
    participant Unity

    LLM->>Germio: write scenario rules
    LLM->>Briko: write spatial layout
    Note over Germio,Briko: Both reference the same zone_id values

    Briko->>Unity: instantiate prefabs and zone GameObjects
    Germio->>Unity: load runtime store, subscribe to triggers

    Unity-->>Germio: zone_id event on player entry
    Germio->>Germio: evaluate rule, update state
    Germio->>Unity: dispatch transition or action
```

### 3.4 Why Integration Was Rejected

```mermaid
graph TB
    subgraph "Rejected: unified document"
        U1["Single 'world.json' with both scenario and layout"]
        U1 --> U2[Tight coupling]
        U2 --> U3[LLM struggles with cross-section consistency]
        U3 --> U4[Breaks Germio independence]
    end

    subgraph "Adopted: separate documents"
        S1["germio.json + level_layout.json"]
        S1 --> S2[Single responsibility per file]
        S2 --> S3[zone_id is the only cross-reference]
        S3 --> S4[Each file remains independently editable]
    end

    style U1 fill:#ffcdd2
    style U2 fill:#ffcdd2
    style U3 fill:#ffcdd2
    style U4 fill:#ffcdd2
    style S1 fill:#c8e6c9
    style S2 fill:#c8e6c9
    style S3 fill:#c8e6c9
    style S4 fill:#c8e6c9
```

### 3.5 Dependency Direction

```mermaid
graph LR
    Briko -->|may reference - unused in v1| Germio
    Germio -.must not reference.-> Briko
    Briko -.must not reference.-> GameDev[Host project<br/>game-specific code]

    style Briko fill:#d1e7ff
    style Germio fill:#ffd1dc
    style GameDev fill:#ffe0b2
```

+ Briko → Germio: permitted but unused in v1
+ Germio → Briko: forbidden (would create circular dependency)
+ Briko → host project: forbidden (Briko remains generic)

---

## 4. Coordinate System and Grid

### 4.1 Unit System

```text
1 Briko unit = 1 meter (matches Unity's default world unit)
```

No conversion is applied between Unity coordinates and Briko coordinates. A position of `[10, 0, 5]` in Briko JSON corresponds directly to Unity world coordinates `(10, 0, 5)`.

### 4.2 Grid Hierarchy

The grid hierarchy is derived from observed prefab dimensions in the reference implementation:

```mermaid
graph TB
    subgraph "Ground tiles (walkable surface)"
        G10[10m × 0.5m × 10m<br/>large area]
        G5[5m × 0.5m × 5m<br/>medium area]
        G25[2.5m × 0.5m × 2.5m<br/>small platform]
    end

    subgraph "Block obstacles"
        B1[1m × 1m × 1m<br/>standard step]
        B05[0.5m × 0.5m × 0.5m<br/>fine step]
    end

    G10 -->|"×0.5"| G5
    G5 -->|"×0.5"| G25
    B1 -->|"×0.5"| B05

    style G10 fill:#90ee90
    style G5 fill:#90ee90
    style G25 fill:#90ee90
    style B1 fill:#87ceeb
    style B05 fill:#87ceeb
```

**Grid base unit: 0.25m. Sizes step in factors of 2 or 2.5.. Sizes step in factors of 2 or 2.5.

### 4.3 Discrete Constraints

| Quantity      | Constraint                      | Domain                       |          |
| ------------- | ------------------------------- | ---------------------------- | -------- |
| `position[i]` | Integer multiple of `grid_unit` | `{ k × 0.25 \                | k ∈ ℤ }` |
| `rotation_y`  | Discrete cardinal               | `{ 0, 90, 180, 270 }`        |          |
| `prefab`      | Member of the prefab catalog    | finite set, project-specific |          |
| `variant`     | Positive integer                | `{ 1, 2, 3, ... }`           |          |
| `grid_unit`   | Fixed value                     | `0.25` (v1)                  |          |

### 4.4 Snap Algorithm

For each axis component `v`:

```text
snapped(v) = round(v / grid_unit) × grid_unit
```

The reference implementation uses `Math.Round(v, MidpointRounding.AwayFromZero)`.

```mermaid
flowchart TB
    Input["Raw coordinate v<br/>e.g., 0.473"]
    Input --> Div["Divide by grid_unit<br/>0.473 / 0.25 = 1.892"]
    Div --> Round["Round to nearest integer<br/>0.946 → 1"]
    Round --> Mul["Multiply by grid_unit<br/>2 × 0.25 = 0.5"]
    Mul --> Output["Snapped value<br/>0.5"]

    Input -.tolerance check.-> Warn{Difference > 0.01?}
    Warn -->|yes| Log[Console warning]
    Warn -->|no| Skip[Silent]

    style Input fill:#ffccbc
    style Output fill:#c8e6c9
    style Log fill:#fff9c4
```

When the difference between raw and snapped exceeds 0.01m on any axis, the exporter emits a console warning to alert the user that source data is off-grid.

---

## 5. Prefab Naming Convention

### 5.1 Format

Prefab assets must follow this naming pattern:

```text
<Kind>_<Width>x<Height>x<Depth>_<Descriptor>_<Variant>
```

Where:

+ `Kind` is any arbitrary word (e.g., `Ground`, `Block`, `Enemy`, `Wall`, `Trap`). Open-ended and extensible.
+ `Width`, `Height`, `Depth` are decimal numbers in meters
+ `Descriptor` is a free-form identifier, may contain underscores (e.g., `Green`, `Plain_Green`)
+ `Variant` is a positive integer

### 5.2 Examples

| Prefab name                          | Kind      | Dimensions  | Descriptor  | Variant |
| ------------------------------------ | --------- | ----------- | ----------- | ------- |
| `Ground_10.0x0.5x10.0_Green_1`       | Ground    | 10×0.5×10   | Green       | 1       |
| `Block_1.0x1.0x1.0_Plain_Green_3`    | Block     | 1×1×1       | Plain_Green | 3       |
| `Ground_2.5x0.5x2.5_Stone_2`         | Ground    | 2.5×0.5×2.5 | Stone       | 2       |
| `Enemy_1.0x2.0x1.0_Red_1`            | Enemy     | 1×2×1       | Red         | 1       |
| `Bipyramid_0.5x1.0x0.5_Plain_Blue_1` | Bipyramid | 0.5×1×0.5   | Plain_Blue  | 1       |

### 5.3 Parser State Machine

```mermaid
flowchart TB
    Input["GameObject name<br/>e.g., Ground_10.0x0.5x10.0_Green_1 (Clone)"]
    Input --> Strip["Strip ' (Clone)' suffix"]
    Strip --> Regex{"Match against pattern:<br/>^(.+_([\\d.]+x[\\d.]+x[\\d.]+)_.+)_(\\d+)$"}

    Regex -->|match| Extract[Extract groups]
    Extract --> E1["Group 1: full prefab name<br/>(everything before last _variant)"]
    Extract --> E2["Group 2: dimensions (captured inside group 1)"]
    Extract --> E3["Group 3: variant number"]

    E1 --> Output["prefab = Ground_10.0x0.5x10.0_Green<br/>variant = 1"]

    Regex -->|no match| Null[Return null<br/>caller skips]

    style Input fill:#ffccbc
    style Output fill:#c8e6c9
    style Null fill:#ffcdd2
```

The greedy quantifier on group 1 correctly handles multi-word descriptors like `Plain_Green` because the trailing `_(\d+)$` anchors to the end. `Kind` is unrestricted — any word is accepted as long as the `_NxNxN_` dimension segment is present.

### 5.4 The Catalog as LLM Vocabulary

The set of available prefabs constitutes the LLM's vocabulary for level construction. By encoding dimensions in the name itself, the LLM can:

1. Reason about size without an external lookup table
2. Validate placement compatibility from the name alone
3. Generate names that conform to a known pattern

```mermaid
graph LR
    Catalog["Prefab catalog<br/>= LLM vocabulary"]

    Catalog --> S1[Size dimension<br/>1m / 2.5m / 5m / 10m]
    Catalog --> S2[Kind dimension<br/>Ground / Block / Enemy / ...]
    Catalog --> S3[Descriptor dimension<br/>Green / Stone / etc.]
    Catalog --> S4[Variant dimension<br/>_1, _2, _3]

    S1 -.semantic choice.-> Sel[Placement decision]
    S2 -.semantic choice.-> Sel
    S3 -.semantic choice.-> Sel
    S4 -.aesthetic choice.-> Sel

    style Catalog fill:#fff9c4
    style Sel fill:#bbdefb
```

LLM cognitive load is partitioned: structural choices (size, kind, descriptor) require reasoning, while variant selection can be effectively random.

---

## 6. Scene Hierarchy

### 6.1 Required Structure

Briko expects scenes to follow this top-level hierarchy:

```text
{LevelRoot}                ← scene root, name is arbitrary
├── System                 ← Briko does not read or write
├── Platform               ← Briko's primary target for grounds and blocks
│   ├── grounds_<floor>    ← floor-grouped Ground prefab containers
│   ├── blocks_<variant>   ← Block prefab containers (floor inferred)
│   └── ...
└── Entity                 ← Briko reads and writes vol_* GameObjects only
    └── vol_*              ← empty GameObjects with zone_id as name
```

### 6.2 Layer Responsibilities

```mermaid
graph TB
    Level[Scene root]
    Level --> System[System layer]
    Level --> Platform[Platform layer]
    Level --> Entity[Entity layer]

    System -.not touched by Briko.-> S1[GameSystem, SoundSystem,<br/>EventSystem, etc.]

    Platform --> P1[grounds_1f]
    Platform --> P2[grounds_2f]
    Platform --> P3[blocks_plain]
    Platform --> P4[blocks_basic]

    Entity --> E1[vol_spawn]
    Entity --> E2[vol_boss_start]
    Entity --> E3[vol_exit]
    Entity -.may also contain.-> E4[Player, Goal, etc.<br/>not touched by Briko]

    style System fill:#e0e0e0
    style Platform fill:#c5e1a5
    style Entity fill:#bbdefb
    style E4 fill:#e0e0e0
```

### 6.3 GameObject Naming Patterns

| Container        | Pattern            | Example                        | Contents         |
| ---------------- | ------------------ | ------------------------------ | ---------------- |
| Ground container | `grounds_<floor>`  | `grounds_1f`, `grounds_2f`     | Ground prefabs   |
| Block container  | `blocks_<variant>` | `blocks_plain`, `blocks_basic` | Block prefabs    |
| Zone marker      | `vol_<identifier>` | `vol_spawn`, `vol_boss_start`  | Empty GameObject |

### 6.4 Floor Inference for Blocks

Because Block containers are not floor-grouped in the reference implementation, the floor for each Block is inferred from its world Y-coordinate:

```text
floor(block) = "1f"  if block.position.y < 3.0
             = "2f"  otherwise
```

The threshold `3.0` is justified by:

+ Floor 1 ground thickness: 0.5m
+ Possible block stack height on floor 1: up to 2.5m
+ Total: 3.0m maximum reachable Y on floor 1

Future versions may eliminate this heuristic by introducing explicit `blocks_<floor>` containers.

---

## 7. Layout JSON Format

### 7.1 Schema Overview

```mermaid
graph TB
    Root["Root<br/>📋 layout_id, grid_unit,<br/>target_duration_sec, bgm_track"]
    Root -->|"platforms[]"| P[Platform<br/>floor]
    P -->|"grounds[]"| I1[Item<br/>prefab, variant,<br/>position, rotation_y]
    P -->|"blocks[]"| I2[Item<br/>prefab, variant,<br/>position, rotation_y]
    P -->|"zones[]"| Z[Zone<br/>zone_id, position]

    style Root fill:#fff9c4
    style P fill:#bbdefb
    style I1 fill:#c5e1a5
    style I2 fill:#90caf9
    style Z fill:#ffccbc
```

### 7.2 Sample Document

```json
{
  "layout_id": "stage_01",
  "grid_unit": 0.25,
  "target_duration_sec": 180,
  "bgm_track": "stage_01_theme.mp3",
  "platforms": [
    {
      "floor": "1f",
      "grounds": [
        {
          "prefab": "Ground_10.0x0.5x10.0_Green",
          "variant": 1,
          "position": [0, 0, 0]
        },
        {
          "prefab": "Ground_2.5x0.5x2.5_Green",
          "variant": 2,
          "position": [10, 0, 5]
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
          "zone_id": "vol_spawn",
          "position": [0, 0.5, 0]
        },
        {
          "zone_id": "vol_exit",
          "position": [20, 0.5, 15]
        }
      ]
    }
  ]
}
```

### 7.3 Field Reference

#### Root

| Field                 | Type              | Required | Description                                               |
| --------------------- | ----------------- | -------- | --------------------------------------------------------- |
| `layout_id`           | string            | yes      | Unique identifier; used as default scene name on import   |
| `grid_unit`           | float             | yes      | Grid quantization in meters (fixed at `0.25` in v1)       |
| `target_duration_sec` | int               | yes      | Intended play duration in seconds                         |
| `bgm_track`           | string            | optional | BGM filename (relative to host project's StreamingAssets) |
| `platforms`           | array of Platform | yes      | One or more floor layers                                  |

#### Platform

| Field     | Type          | Required | Description                             |
| --------- | ------------- | -------- | --------------------------------------- |
| `floor`   | string        | yes      | Floor identifier (`"1f"`, `"2f"`, etc.) |
| `grounds` | array of Item | optional | Ground tile placements                  |
| `blocks`  | array of Item | optional | Block obstacle placements               |
| `zones`   | array of Zone | optional | Trigger zone markers                    |

#### Item

| Field        | Type     | Required | Description                                                                                              |
| ------------ | -------- | -------- | -------------------------------------------------------------------------------------------------------- |
| `prefab`     | string   | yes      | Prefab asset name **without** trailing variant number (e.g. `"Ground_10.0x0.5x10.0_Green"`)              |
| `variant`    | int      | yes      | Variant number (1-based); used for scene object naming only, not appended to prefab asset name on import |
| `position`   | float[3] | yes      | World coordinates `[x, y, z]` in meters                                                                  |
| `rotation_y` | int      | optional | Y-axis rotation in degrees, default `0`                                                                  |

#### Zone

| Field      | Type     | Required | Description                             |
| ---------- | -------- | -------- | --------------------------------------- |
| `zone_id`  | string   | yes      | Identifier matching `^vol_[a-z0-9_]+$`  |
| `position` | float[3] | yes      | World coordinates `[x, y, z]` in meters |

### 7.4 Constraints Summary

```mermaid
graph TB
    Root[JSON document]

    Root --> C1["position must be<br/>integer multiples of grid_unit"]
    Root --> C2["rotation_y ∈ {0, 90, 180, 270}"]
    Root --> C3["prefab must exist<br/>in host project"]
    Root --> C4["zone_id must match<br/>^vol_[a-z0-9_]+$"]
    Root --> C5["No floats in position<br/>except multiples of 0.5"]
    Root --> C6["No materials, no scales,<br/>no custom transforms"]

    style C1 fill:#fff9c4
    style C2 fill:#fff9c4
    style C3 fill:#fff9c4
    style C4 fill:#fff9c4
    style C5 fill:#fff9c4
    style C6 fill:#fff9c4
```

### 7.5 Property Naming Rationale (snake_case)

Briko data classes use `snake_case` C# property names that correspond directly to JSON keys, with no `[JsonProperty]` attribute mapping.

```mermaid
graph LR
    A["C# property<br/>snake_case<br/>e.g., layout_id"]
    B["JSON key<br/>snake_case<br/>e.g., layout_id"]
    C[Newtonsoft.Json<br/>default behavior]
    D[LLM affinity]

    A --> C --> B
    B --> D

    A -.no attribute needed.-> N["No [JsonProperty]"]

    style A fill:#bbdefb
    style B fill:#fff9c4
    style D fill:#c8e6c9
    style N fill:#c5e1a5
```

This deviates from typical C# conventions (where public properties are `PascalCase` or `camelCase`) but matches Germio's `Data.cs` convention. The principle: **the property name is the wire format**.

### 7.6 Serializer Settings

```csharp
new JsonSerializerSettings {
    Formatting = Formatting.Indented,
    NullValueHandling = NullValueHandling.Ignore,
    DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
};
```

Effects:

+ Output is indented for human and LLM readability
+ Default values (e.g., `rotation_y: 0`) are omitted, reducing noise
+ Nulls are omitted

### 7.7 C# Class Diagram

```mermaid
classDiagram
    class Root {
        +string layout_id
        +float grid_unit
        +int target_duration_sec
        +string bgm_track
        +List~Platform~ platforms
    }

    class Platform {
        +string floor
        +List~Item~ grounds
        +List~Item~ blocks
        +List~Zone~ zones
    }

    class Item {
        +string prefab
        +int variant
        +float[] position
        +int rotation_y
    }

    class Zone {
        +string zone_id
        +float[] position
    }

    Root "1" --> "1..*" Platform : platforms
    Platform "1" --> "0..*" Item : grounds
    Platform "1" --> "0..*" Item : blocks
    Platform "1" --> "0..*" Zone : zones
```

All four classes reside in a single file `Editor/Model/Layout.cs` under namespace `Briko.Editor.Model`.

---

## 8. Bidirectional Converter

### 8.1 Forward Flow (JSON → Scene)

```mermaid
flowchart TB
    Start([Layout JSON input]) --> Deser[Deserialize to Root]
    Deser --> NewScene[Create empty Unity scene]
    NewScene --> Build[Build hierarchy:<br/>Level / System / Platform / Entity]

    Build --> Loop1{For each Platform}
    Loop1 --> Ground[Create grounds_floor container]
    Loop1 --> Block[Create blocks_plain container if needed]

    Ground --> Loop2{For each Item in grounds}
    Block --> Loop3{For each Item in blocks}

    Loop2 --> FindG["Search all prefabs by file name<br/>(item.prefab exact match)"]
    Loop3 --> FindB["Search all prefabs by file name<br/>(item.prefab exact match)"]

    FindG --> InstG{Found?}
    FindB --> InstB{Found?}

    InstG -->|yes| PlaceG[InstantiatePrefab + snap position]
    InstG -->|no| WarnG[Log warning, skip]

    InstB -->|yes| PlaceB[InstantiatePrefab + snap position]
    InstB -->|no| WarnB[Log warning, skip]

    Loop1 --> Loop4{For each Zone}
    Loop4 --> Empty[Create empty GameObject<br/>name = zone_id]
    Empty --> SetPos[Set position]

    PlaceG --> Save[SaveScene + AssetDatabase.Refresh]
    PlaceB --> Save
    SetPos --> Save
    WarnG --> Save
    WarnB --> Save

    Save --> End([Scene saved to disk])

    style Start fill:#fff9c4
    style End fill:#c8e6c9
    style WarnG fill:#ffcdd2
    style WarnB fill:#ffcdd2
```

### 8.2 Reverse Flow (Scene → JSON)

```mermaid
flowchart TB
    Start([Active Unity scene]) --> Find1[Find Platform GameObject]
    Find1 -->|found| Walk1[Walk Platform children]
    Find1 -->|not found| Warn1[Log warning, continue]

    Walk1 --> Each1{For each child}
    Each1 -->|name starts with grounds_| GroundC[Extract floor from name<br/>collect Ground items]
    Each1 -->|name starts with blocks_| BlockC[Infer floor from Y<br/>collect Block items]

    GroundC --> Iter1{For each Item descendant}
    BlockC --> Iter2{For each Item descendant}

    Iter1 --> Strip1["Strip ' (Clone)' suffix"]
    Iter2 --> Strip2["Strip ' (Clone)' suffix"]

    Strip1 --> Parse1[PrefabNameParser.Parse]
    Strip2 --> Parse2[PrefabNameParser.Parse]

    Parse1 -->|null| Skip1[Skip]
    Parse2 -->|null| Skip2[Skip]

    Parse1 -->|tuple| Snap1[Snap position to grid<br/>warn if drift > 0.01m]
    Parse2 -->|tuple| Snap2[Snap position to grid<br/>warn if drift > 0.01m]

    Snap1 --> Build1[Construct Item record]
    Snap2 --> Build2[Construct Item record]

    Start --> Find2[Find Entity GameObject]
    Find2 --> Walk2[Walk Entity children]
    Walk2 --> Each2{For each child}
    Each2 -->|matches vol_*| Zone[Construct Zone record]

    Build1 --> Compose[Compose Root]
    Build2 --> Compose
    Zone --> Compose

    Compose --> Out([Layout JSON output])

    style Start fill:#fff9c4
    style Out fill:#c8e6c9
    style Skip1 fill:#ffcdd2
    style Skip2 fill:#ffcdd2
```

### 8.3 Round-trip Property

The bidirectional converter satisfies:

```text
∀ valid JSON j:        Export(Import(j)) ≡_semantic j
∀ valid Scene s:       Import(Export(s)) ≈_layout s
```

Where:

+ `≡_semantic` means JSON documents are equivalent under `JToken.DeepEquals`
+ `≈_layout` means scenes contain identical prefab placements and zone positions (other scene attributes such as material instance IDs may differ)

The discreteness of the input space (§4.3) is what mathematically guarantees this property:

+ No floating-point drift accumulates across round trips
+ Every position has a unique grid-snapped representation
+ Every rotation has a unique cardinal representation
+ Every prefab has a unique name encoding

### 8.4 Round-trip Test Strategy

The reference implementation includes `RoundTripTests.cs` which:

1. Loads a fixture JSON from disk
2. Deserializes to `Root`
3. Re-serializes to a new JSON string
4. Parses both as `JToken`
5. Asserts `JToken.DeepEquals(before, after) == true`

This test exercises the JSON ↔ POCO round-trip but not the JSON ↔ Scene round-trip (which requires Unity runtime and is deferred to v2 PlayMode tests).

### 8.5 Tolerance Handling

| Quantity                     | Tolerance            | Action when exceeded             |
| ---------------------------- | -------------------- | -------------------------------- |
| Position drift from grid     | 0.01m per axis       | Console warning, continue        |
| Rotation drift from cardinal | 1.0°                 | Console warning, continue        |
| Prefab name pattern mismatch | exact match required | Skip object silently             |
| Missing prefab on import     | exact match required | Console warning, skip GameObject |

Briko **never throws fatal exceptions** on data anomalies. The principle: a partial result with warnings is more useful than a complete failure.

---

## 9. Repository Structure

### 9.1 UPM Package Layout

```text
briko/
├── package.json
├── README.md
├── Editor/
│   ├── Briko.Editor.asmdef
│   ├── BrikoLog.cs
│   ├── Exporter.cs
│   ├── ExportMenu.cs
│   ├── Importer.cs
│   ├── ImportMenu.cs
│   ├── Internal/
│   │   ├── PrefabNameParser.cs
│   │   └── GridSnapper.cs
│   └── Model/
│       └── Layout.cs
├── Tests~/
│   └── IntegrationTests/
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
└── docs/
    ├── briko_spec.md                     ← this document
    └── development_plan_v1_detail_JP.md  ← implementation guide
```

### 9.2 The Tests~ Convention

The tilde suffix on `Tests~` is a UPM convention: directories ending in `~` are **invisible to Unity** but visible to standard .NET tooling.

This allows shipping a test project alongside the package without polluting the host project's Unity assemblies.

### 9.3 Namespace Organization

```mermaid
graph TB
    subgraph "Briko.Editor"
        E1[Exporter]
        E2[Importer]
        E3[ExportMenu]
        E4[ImportMenu]
        E5[BrikoLog]
    end

    subgraph "Briko.Editor.Internal"
        I1[PrefabNameParser]
        I2[GridSnapper]
    end

    subgraph "Briko.Editor.Model"
        M1[Root]
        M2[Platform]
        M3[Item]
        M4[Zone]
    end

    subgraph "Briko.Tests.Internal"
        T1[PrefabNameParserTests]
        T2[GridSnapperTests]
    end

    subgraph "Briko.Tests.Model"
        T3[LayoutTests]
        T4[RoundTripTests]
    end

    E1 --> M1
    E2 --> M1
    E1 --> I1
    E2 --> I2
    T1 -.tests.-> I1
    T2 -.tests.-> I2
    T3 -.tests.-> M1
    T4 -.tests.-> M1

    style E1 fill:#bbdefb
    style E2 fill:#bbdefb
    style I1 fill:#fff9c4
    style I2 fill:#fff9c4
    style M1 fill:#c5e1a5
    style M2 fill:#c5e1a5
    style M3 fill:#c5e1a5
    style M4 fill:#c5e1a5
```

### 9.4 Test Project (.csproj) Structure

The test project uses **shared source compilation**: it directly compiles selected files from `Editor/` rather than referencing a compiled assembly.

```xml
<ItemGroup>
  <Compile Include="..\..\Editor\Model\Layout.cs" />
  <Compile Include="..\..\Editor\Internal\PrefabNameParser.cs" />
  <Compile Include="..\..\Editor\Internal\GridSnapper.cs" />

  <Compile Include="Scripts\Internal\PrefabNameParserTests.cs" />
  <Compile Include="Scripts\Internal\GridSnapperTests.cs" />
  <Compile Include="Scripts\Model\LayoutTests.cs" />
  <Compile Include="Scripts\Model\RoundTripTests.cs" />
</ItemGroup>
```

`<EnableDefaultItems>false</EnableDefaultItems>` ensures every file is explicitly listed.

This pattern allows:

+ Tests run on plain .NET 9 without Unity
+ No double-compilation of source files
+ Clear visibility of which Editor classes have NUnit coverage

### 9.5 Class Coverage Pattern

```mermaid
graph LR
    L[Layout.cs] -.tested by.-> LT[LayoutTests.cs]
    P[PrefabNameParser.cs] -.tested by.-> PT[PrefabNameParserTests.cs]
    G[GridSnapper.cs] -.tested by.-> GT[GridSnapperTests.cs]
    All[All Layout classes] -.cross-cutting.-> RT[RoundTripTests.cs]

    Exp[Exporter / Importer / Menus] -.testless<br/>Unity API dependent.-> X[Deferred to v2<br/>PlayMode tests]

    style L fill:#c5e1a5
    style P fill:#fff9c4
    style G fill:#fff9c4
    style Exp fill:#ffcdd2
    style X fill:#ffe0b2
```

Classes that depend on Unity APIs (GameObject, Transform, EditorSceneManager, AssetDatabase) are not unit-tested in v1. Their integration tests are deferred to v2 using Unity Test Framework PlayMode mode.

---

## 10. Coding Conventions

### 10.1 Class Naming

Classes use **single words without project prefix**:

| Used                               | Avoided                              |
| ---------------------------------- | ------------------------------------ |
| `Exporter`                         | `BrikoExporter`                      |
| `Importer`                         | `BrikoImporter`                      |
| `Root`, `Platform`, `Item`, `Zone` | `LayoutRoot`, `LayoutPlatform`, etc. |
| `PrefabNameParser`                 | `BrikoPrefabNameParser`              |

Disambiguation when needed is achieved through namespaces: `Briko.Editor.Exporter` vs `OtherPackage.Exporter`.

### 10.2 Property Naming

| Class type                 | Convention                  | Example                     |
| -------------------------- | --------------------------- | --------------------------- |
| Data class (Layout, etc.)  | `snake_case` (matches JSON) | `layout_id`, `grid_unit`    |
| Service / behavior class   | `camelCase`                 | `home`, `beat`, `mode`      |
| Private field              | `_snake_case`               | `_do_update`, `_jump_power` |
| Local variable / parameter | `snake_case`                | `base_path`, `grid_unit`    |
| Constant                   | `ALL_CAPS`                  | `GRID_UNIT`, `MENU_ROOT`    |
| `[SerializeField]` (Unity) | `_ALL_CAPS`                 | `_JUMP_POWER`               |

### 10.3 Named Arguments Rule

All calls to project-defined methods (Briko or Germio) **must** use named arguments:

```csharp
// Required
GridSnapper.Snap(raw: position, grid_unit: 0.25f);
Importer.ImportToNewScene(layout: root, scene_path: path);

// Disallowed (positional)
GridSnapper.Snap(position, 0.5f);
```

Exceptions (positional permitted):

+ .NET BCL: `Math.Round(value)`, `string.IsNullOrEmpty(s)`
+ Unity API: `GameObject.Find("Platform")`, `transform.position`
+ Newtonsoft.Json: `JsonConvert.SerializeObject(obj)`

### 10.4 File Header

Every C# file begins with:

```csharp
// Copyright (c) STUDIO MeowToon. All rights reserved.
// Licensed under GPL v2.0. See LICENSE in the project root for license information.
```

### 10.5 XML Documentation

Every class and public method has author attribution:

```csharp
/// <summary>
/// One-line description.
/// </summary>
/// <author>h.adachi (STUDIO MeowToon)</author>
public class Foo {
    /// <summary>
    /// One-line description.
    /// </summary>
    /// <author>h.adachi (STUDIO MeowToon)</author>
    public void Bar() { }
}
```

### 10.6 Nullable Annotations

`#nullable enable` is declared **inside** each class body, not at file level:

```csharp
namespace Briko.Editor {
    public class Exporter {
#nullable enable
        // ...
    }
}
```

### 10.7 File Organization for Data Classes

Data classes are grouped into a single file rather than one-class-per-file:

+ `Editor/Model/Layout.cs` contains `Root`, `Platform`, `Item`, `Zone`

This convention matches Germio's `Data.cs` (containing `Scenario`, `State`, `World`, `Level`, `Next`, `Rule`, etc.). Service and behavior classes use one-file-per-class.

---

## 11. Common Layout Patterns

These reference patterns illustrate typical level structures expressible in the Layout JSON. They serve as starting templates for LLM-assisted generation.

### 11.1 Linear Stage

```mermaid
graph LR
    S[vol_spawn] --> A[area 1]
    A --> B[area 2]
    B --> C[area 3]
    C --> Boss[vol_boss_start]
    Boss --> G[vol_exit]

    style S fill:#c8e6c9
    style Boss fill:#ff9800
    style G fill:#ffd700
```

**Characteristics**: high prefab density, narrow elongated space, sequential difficulty curve.

### 11.2 Branching Exploration

```mermaid
graph TB
    Spawn[vol_spawn] --> Path1[main path]
    Path1 --> Branch{junction}
    Branch --> Sub1[secret branch A<br/>vol_secret_a]
    Branch --> Sub2[secret branch B<br/>vol_secret_b]
    Sub1 --> Goal[vol_exit]
    Sub2 --> Goal

    style Spawn fill:#c8e6c9
    style Goal fill:#ffd700
    style Sub1 fill:#fff9c4
    style Sub2 fill:#fff9c4
```

**Characteristics**: large open space, medium density, optional collectible zones.

### 11.3 Boss Arena

```mermaid
graph TB
    Spawn[vol_spawn]
    Arena[central arena]
    Phase1[vol_boss_phase_1]
    Phase2[vol_boss_phase_2]
    Final[vol_boss_final]

    Spawn --> Arena
    Arena --> Phase1
    Phase1 --> Phase2
    Phase2 --> Final

    style Spawn fill:#c8e6c9
    style Phase1 fill:#fff9c4
    style Phase2 fill:#ffccbc
    style Final fill:#ffcdd2
```

**Characteristics**: compact, high block density, multi-phase zone progression.

### 11.4 Speedrun Stage

```mermaid
graph LR
    Spawn[vol_spawn] --> Mid[mid-stage]
    Mid --> Goal[vol_exit]

    Spawn -.timer start.-> T[vol_timer_start]
    Goal -.timer end.-> TE[vol_timer_end]

    style Spawn fill:#c8e6c9
    style Goal fill:#ffd700
    style T fill:#fff9c4
    style TE fill:#fff9c4
```

**Characteristics**: linear, medium density, paired timer zones for Germio integration.

---

## 12. LLM Workflow Models

### 12.1 Human-led with LLM Assistance (v1 Workflow)

```mermaid
sequenceDiagram
    participant H as Human
    participant L as LLM
    participant B as Briko
    participant U as Unity

    H->>U: hand-craft baseline scene
    H->>B: invoke Export menu
    B-->>H: layout.json
    H->>L: prompt with JSON, request variation
    L-->>H: modified layout.json
    H->>B: invoke Import menu
    B->>U: generate new scene
    H->>U: manual polish
```

In this model, the human owns the creative direction and uses the LLM as a generator of variations.

### 12.2 LLM-led with Human Supervision (v2 Workflow)

```mermaid
sequenceDiagram
    participant H as Human (supervisor)
    participant L as LLM (author)
    participant V as Validator (v2)
    participant B as Briko
    participant U as Unity

    H->>L: high-level brief
    loop per stage
        L->>L: generate layout
        L->>V: request validation
        V-->>L: errors or approval
        L->>B: import via API
        B->>U: scene generation
    end
    U-->>H: completed stages
    H->>U: review and select
```

The Validator (planned for v2) provides automated feedback that the LLM uses to self-correct, reducing human intervention to high-level review.

### 12.3 Autonomous Agent Loop (v3 Workflow)

```mermaid
sequenceDiagram
    participant Agent
    participant Briko
    participant Unity
    participant TestRig as AI playtest
    participant Music as BGM analysis

    Agent->>Music: analyze track structure
    Music-->>Agent: tempo, dynamics curve
    Agent->>Briko: generate layout
    Briko->>Unity: scene
    Unity->>TestRig: automated playthrough
    TestRig-->>Agent: completion time, failure points
    Agent->>Agent: derive improvement
    Agent->>Briko: refined layout
    Note over Agent,TestRig: iteration without human input
```

### 12.4 Multi-LLM Collaboration

```mermaid
graph TB
    Director[Director LLM<br/>strategic decisions]
    Designer[Designer LLM<br/>layout generation]
    Critic[Critic LLM<br/>quality review]
    Tester[Tester LLM<br/>playability check]

    Director --> Designer
    Designer --> Critic
    Critic -->|revision request| Designer
    Designer --> Tester
    Tester -->|failure report| Designer
    Tester -->|pass| Director

    Director -.final approval.-> Briko
    Briko -.confirmed.-> Output[Production-ready stage]

    style Director fill:#ffd700,stroke:#000,stroke-width:3px
    style Designer fill:#bbdefb
    style Critic fill:#fff9c4
    style Tester fill:#c5e1a5
    style Output fill:#c8e6c9
```

Specialized roles can be assigned to different LLMs (or different prompts to the same LLM), with Briko serving as the data interchange layer.

---

## 13. Failure Mode Catalog

### 13.1 Round-trip Failure Modes

```mermaid
graph TB
    Root[Round-trip failure causes]

    Root --> F1[1 - Floating-point drift]
    Root --> F2[2 - Naming convention violation]
    Root --> F3[3 - Hierarchy structure deviation]
    Root --> F4[4 - Missing prefab reference]
    Root --> F5[5 - zone_id pattern violation]

    F1 --> S1[Absorbed by grid snap<br/>warning if drift > 0.01m]
    F2 --> S2[Regex match fails<br/>object skipped silently]
    F3 --> S3[Platform / Entity not found<br/>console warning]
    F4 --> S4[File name search miss<br/>placement skipped]
    F5 --> S5[vol_* match fails<br/>excluded from zones list]

    style F1 fill:#ffcdd2
    style F2 fill:#ffcdd2
    style F3 fill:#ffcdd2
    style F4 fill:#ffcdd2
    style F5 fill:#ffcdd2
    style S1 fill:#c8e6c9
    style S2 fill:#c8e6c9
    style S3 fill:#c8e6c9
    style S4 fill:#c8e6c9
    style S5 fill:#c8e6c9
```

### 13.2 LLM Output Failure Modes

```mermaid
graph TB
    LLM[LLM output failure modes]

    LLM --> O1[Off-grid coordinates]
    LLM --> O2[Non-cardinal rotations]
    LLM --> O3[Unknown prefab names]
    LLM --> O4[Invalid zone_id format]
    LLM --> O5[Structural violations<br/>e.g., missing platforms array]

    O1 --> M1[Grid snapper coerces<br/>Console warning issued]
    O2 --> M2[Cardinal snapper coerces<br/>Console warning issued]
    O3 --> M3[Importer logs warning<br/>placement skipped]
    O4 --> M4[Excluded from import<br/>does not propagate to Germio]
    O5 --> M5[Newtonsoft.Json<br/>throws on deserialize<br/>Importer reports error to user]

    style O1 fill:#ffcdd2
    style O2 fill:#ffcdd2
    style O3 fill:#ffcdd2
    style O4 fill:#ffcdd2
    style O5 fill:#ffcdd2
    style M1 fill:#c8e6c9
    style M2 fill:#c8e6c9
    style M3 fill:#c8e6c9
    style M4 fill:#c8e6c9
    style M5 fill:#c8e6c9
```

### 13.3 Mitigation Principles

1. **Never crash on data anomalies.** Briko logs and proceeds.
2. **Surface drift to the user.** Warnings make off-spec data visible.
3. **Skip unknown elements.** Partial output is preferred over no output.
4. **Validate at deserialization time.** Structural violations are caught before processing begins.

---

## 14. Roadmap

### 14.1 Version Plan

```mermaid
graph LR
    V1[v1.0<br/>bidirectional converter] --> V2[v2.0<br/>schema and validation]
    V2 --> V3[v3.0<br/>automation and tooling]
    V3 --> V4[v4.0<br/>extended catalogs]

    style V1 fill:#c8e6c9
    style V2 fill:#fff9c4
    style V3 fill:#bbdefb
    style V4 fill:#e0e0e0
```

### 14.2 v1.0 — Implemented

+ ✅ Layout JSON format defined and stable
+ ✅ Exporter (Scene → JSON)
+ ✅ Importer (JSON → Scene)
+ ✅ Editor menu integration
+ ✅ Round-trip property guaranteed by discrete constraints
+ ✅ Test suite (NUnit, 18 tests, shared source compilation)
+ ✅ Stemic-aligned coding conventions
+ ✅ BrikoLog diagnostic logger

### 14.3 v2.0 — Planned

```mermaid
graph TB
    V2[v2.0 scope]

    V2 --> S1[Public JSON Schema<br/>level_layout.schema.json]
    V2 --> S2[Validator class<br/>zone_id consistency vs Germio]
    V2 --> S3[PlayMode integration tests<br/>Exporter / Importer coverage]
    V2 --> S4[Prompt template library<br/>standardized LLM interaction]
    V2 --> S5[blocks_<floor> hierarchy<br/>eliminate Y-coord inference]

    style V2 fill:#fff9c4
```

### 14.4 v3.0 — Envisioned

```mermaid
graph TB
    V3[v3.0 scope]

    V3 --> S1[Variant auto-selection<br/>aesthetic randomization]
    V3 --> S2[BGM integration<br/>tempo to layout density mapping]
    V3 --> S3[Multi-LLM orchestration<br/>Director / Designer / Critic / Tester]
    V3 --> S4[Web-based Briko Editor<br/>browser-side preview]

    style V3 fill:#bbdefb
```

### 14.5 Out of Scope

The following are explicitly **not** Briko's responsibility:

+ Scenario logic (delegated to Germio)
+ Procedural generation algorithms
+ Prefab asset creation
+ Game runtime (Briko is Editor-only)
+ Asset-store distribution

---

## 15. References

### 15.1 Companion Documents

| Document                                   | Purpose                                         |
| ------------------------------------------ | ----------------------------------------------- |
| `docs/development_plan_v1_detail_JP.md`    | Step-by-step v1 implementation guide (Japanese) |
| `README.md`                                | User-facing introduction                        |
| Germio specification (separate repository) | Companion framework reference                   |

### 15.2 External Resources

+ [Unity Package Manager Custom Layout](https://docs.unity3d.com/Manual/cus-layout.html) — `Tests~` directory convention
+ [com.unity.nuget.newtonsoft-json](https://docs.unity3d.com/Packages/com.unity.nuget.newtonsoft-json@latest) — JSON library
+ [NUnit Documentation](https://docs.nunit.org/) — test framework

### 15.3 Conformance

A Briko-conforming Unity Editor extension must:

1. Read and write JSON matching the format specified in §7
2. Apply discrete constraints from §4.3 on both directions of conversion
3. Implement the snap algorithm from §4.4
4. Parse prefab names per the regex `^(.+_([\d.]+x[\d.]+x[\d.]+)_.+)_(\d+)$` in §5.3
5. Recognize the scene hierarchy from §6.1
6. Match `zone_id` against `^vol_[a-z0-9_]+$`
7. Preserve the round-trip property defined in §8.3
8. Log warnings (not exceptions) on the failure modes listed in §13

---

**End of specification.**
