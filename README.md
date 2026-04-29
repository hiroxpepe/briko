# Briko

> Block-based Level Construction Tool for Germio

Briko is a Unity Editor extension that enables LLM-driven level generation
through bidirectional Scene ↔ JSON conversion.

## Status

**v0.1.0 — Draft / Pre-implementation**

The full design specification is being maintained separately.
See the spec document for the architecture, design decisions, and roadmap.

## Architecture (TL;DR)

- **Germio** = scenario framework (storyboard)
- **Briko** = level construction tool (set design)
- **Junction** = `zone_id` string only

Briko depends on Germio. Germio never depends on Briko.

## License

TBD