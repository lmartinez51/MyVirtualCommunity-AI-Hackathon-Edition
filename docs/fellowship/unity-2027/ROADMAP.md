# Product Roadmap â€” Unity Fellowship 2027 to v1.0 Commercial Release

**Project:** My Virtual Community â€” *A Living World Beyond Reality.*
**Application Deadline:** December 18, 2026 (5:00 p.m. ET)
**Fellowship Program Start:** March 2027
**Target Commercial PC v1.0 Release:** ~March 2028 (~12 months after Fellowship start)

> **Epistemic Separation:** Phase 0 milestones marked **`CONFIRMED`** are already verified in Git history (`dcac4fe` / `product/unity-fellowship-2027`). All subsequent phases are **`PLANNED`** proposals subject to creator sign-off and Fellowship schedule alignment.

Related documents: [Application Brief](APPLICATION_BRIEF.md) Â· [Vertical Slice](../../VERTICAL_SLICE.md) Â· [Feature Status Matrix](../../FEATURE_STATUS_MATRIX.md) Â· [IP & Asset Provenance](../../IP_AND_ASSET_PROVENANCE.md)

---

## Phase 0 â€” Completed Foundation & Repository Transition (`CONFIRMED`)

- **0.0 Multi-World Prototype (`CONFIRMED` at `dcac4fe`):**
  - Built Ciudad JuÃ¡rezâ€“El Paso corridor, Paso del Norte viaduct, West Manzana, walkable *Ã“ptica Fashion*, 7 procedural Bobblehead NPCs, hexagonal World Core backend, underground storm-drain network (*La Cisterna*, *Sector C-0*, Abasolo siphon, RÃ­o Bravo outflow), Building A South keypad Secret Room, and additive `40 m Ã— 24 m` `BackroomsLevel1Scene.unity` with local Level-Î© safe and visual Lasser portal activation.
- **0.1 Forensic Transition Assessment (`CONFIRMED`):** Completed full Git, world, gameplay, technical, and IP provenance audit.
- **0.2 Git Protection & Commercial Branching (`CONFIRMED`):** Tagged `pre-unity-fellowship-transition-2026-11-24` and created/published `product/unity-fellowship-2027` from `dcac4fe`.
- **0.3 / 0.3.1 Documentation Transition, Editorial Review & Publication (`CONFIRMED`):** Archived 2026 hackathon docs intact into `docs/archive/ai-hackathon-2026/`, authored active commercial + Unity Fellowship documentation, verified link/hash integrity, and published on `product/unity-fellowship-2027`.

---

## Phase 1 â€” Fellowship Application Readiness (`PLANNED`: Nov 25 â€“ Dec 18, 2026)

*Goal: Deliver an IP-clean, evaluator-ready playable demo (*Slice 01*) and game video before December 18, 2026.*

1. **Creator Decisions Lock:** Finalize brand strategy (real vs. fictionalized storefronts), liminal layer name, opening hook, and Fellowship budget figures (`TBD`).
2. **Mandatory IP & Commercial Clearance Pass:**
   - Replace all third-party horror franchise references/props in `BackroomsLevel1Builder.cs` and `"SS GEORGIE"` in `UndergroundSewerBuilder.cs` with 100% original artifacts.
   - Replace `media/arch-ads/camila_loop.mp4`, unlicensed consumer trademarks (`Alexa`, `iPad`, `El Diario de JuÃ¡rez`), and external YouTube/unlicensed brand assets per chosen brand policy.
   - Update Unity `ProjectSettings.asset` (`productName`, `companyName`) and remove `HandshakeScene.unity` from playable demo build settings.
3. **Minimal Playable Vertical Slice Loop (*Slice 01*):**
   - Add Title Screen, gated debug modes (`[V]`/`[G]` locked behind dev flag), underground environmental clues leading to the Secret Room code, lightweight Discovery Journal, persistent slice save, and *El Umbral de Lasser* climax + Slice Complete screen.
   - Simplify Windows demo launch so evaluators can run cleanly offline.
4. **Trailer Capture & Application Submission:** Capture IP-clean footage per [Trailer Script](TRAILER_SCRIPT.md), package verified Windows demo build per [Build Delivery](BUILD_DELIVERY.md), and submit before **December 18, 2026, 5:00 p.m. ET**.

---

## Phase 2 â€” Pre-Fellowship Stabilization & Architecture Refactor (`PLANNED`: Jan â€“ Feb 2027)

*Goal: Remove technical debt before full commercial production begins in March 2027.*

- Decompose monolithic `FirstPersonPlayer.cs` (8,692 lines) into modular C# controllers and migrate core HUD/menus from legacy IMGUI to UGUI or UI Toolkit.
- Finalize **self-contained offline PC architecture** (embedding core deterministic simulation in Unity C# or bundling a managed local runtime with zero manual Node.js/PowerShell steps for players).
- Establish automated Unity EditMode/PlayMode regression tests alongside backend `npm test`.
- Complete full asset/scene hygiene (`Assets/Settings/` material reorganization, removal of unused template/PoC folders).

---

## Phase 3 â€” Fellowship Production: Core Systems & Content (`PLANNED`: Mar â€“ Aug 2027)

*Goal: Expand Slice 01 into a complete, cohesive commercial campaign.*

- **Q2 2027 (Mar â€“ May 2027) â€” Narrative & Systems Alpha:**
  - Implement full save/load system, Discovery Journal, key-item inventory, dialogue/clue state tracking, and creator-approved narrative campaign structure.
  - Populate underground storm-drain network (*La Cisterna / El Nudo*, *Sector C-0*) with authored environmental storytelling, puzzles, and any creator-approved subterranean encounters.
  - Begin production of the **first playable Hidden World beyond *El Umbral de Lasser*** (real scene transition + gameplay arc).
- **Q3 2027 (Jun â€“ Aug 2027) â€” Content Complete & Audio/Visual Upgrade:**
  - Complete authored sound design: border streetscape ambience, material-based footsteps, underground acoustics, liminal audio, puzzle foley, and original music score.
  - Upgrade URP lighting, shadow fidelity (replacing `256`-resolution shadow atlas with scalable PC quality presets), occlusion culling, LODs, and visual polish across all scenes.
  - Expand select surface interiors where narratively essential for clue discovery.

---

## Phase 4 â€” Beta, Playtesting & Commercial Release Prep (`PLANNED`: Sep 2027 â€“ Mar 2028)

*Goal: Certify stability, performance, legal compliance, and commercial distribution readiness.*

- **Q4 2027 (Sep â€“ Nov 2027) â€” Content Lock & Closed Playtesting:**
  - Structured external playtests (bilingual English & Spanish players); tune puzzle clarity, pacing, accessibility (subtitles, font scaling, FOV, mouse sensitivity, remappable inputs), and difficulty curve.
  - Full performance profiling across minimum-spec laptop GPUs (30+ FPS) and mid-range PCs (60 FPS).
- **Q1 2028 (Dec 2027 â€“ Feb 2028) â€” Release Candidate & Storefront Readiness:**
  - Complete final legal/IP clearance audit against [IP & Asset Provenance](../../IP_AND_ASSET_PROVENANCE.md) and [Third-Party Notices](../../THIRD_PARTY_NOTICES.md).
  - Finalize storefront page, commercial trailer, press kit, installer/Steam build pipeline, and crash-free RC verification.
- **Target v1.0 Launch (~March 2028 â€” `PLANNED`):**
  - Commercial PC Windows release of **My Virtual Community â€” A Living World Beyond Reality.**

---

## Post-Launch Expansion Horizon (`PLANNED` / Deferred Beyond v1.0)

Explicitly **deferred** out of v1.0 scope to protect the March 2028 release deadline:
- Additional Ciudad JuÃ¡rez / El Paso urban districts beyond the Av. Benito JuÃ¡rez â€“ Paso del Norte corridor.
- Additional hidden worlds beyond the first post-Lasser realm.
- Optional live API Reality-Bridge modules (live radio/news/weather/AI integrations).
- Multiplayer / shared-world modes, console ports, or macOS/Linux builds.
