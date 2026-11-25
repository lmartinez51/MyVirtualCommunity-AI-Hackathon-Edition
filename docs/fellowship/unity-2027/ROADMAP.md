# Product Roadmap — Unity Fellowship 2027 to v1.0 Commercial Release

**Project:** My Virtual Community — *A Living World Beyond Reality.*
**Application Deadline:** December 18, 2026 (5:00 p.m. ET)
**Fellowship Program Start:** March 2027
**Internal Target Commercial Release:** February 2028, providing schedule buffer ahead of [Unity Fellowship Terms and Conditions](https://unity.com/legal/unity-fellowship-terms-and-conditions) Section 2.1(c), which requires commercial release before March 1, 2028.

> **Epistemic Separation:** Phase 0 milestones marked **`CONFIRMED`** are already verified in Git history (`dcac4fe` / `product/unity-fellowship-2027`). All subsequent phases are **`PLANNED`** proposals subject to creator sign-off and Fellowship schedule alignment.

Related documents: [Application Brief](APPLICATION_BRIEF.md) · [Vertical Slice](../../VERTICAL_SLICE.md) · [Feature Status Matrix](../../FEATURE_STATUS_MATRIX.md) · [IP & Asset Provenance](../../IP_AND_ASSET_PROVENANCE.md)

---

## Phase 0 — Completed Foundation & Repository Transition (`CONFIRMED`)

- **0.0 Multi-World Prototype (`CONFIRMED` at `dcac4fe`):**
  - Built Ciudad Juárez–El Paso corridor, Paso del Norte viaduct, West Manzana, walkable *Óptica Fashion*, 7 procedural Bobblehead NPCs, hexagonal World Core backend, underground storm-drain network (*La Cisterna*, *Sector C-0*, Abasolo siphon, Río Bravo outflow), Building A South keypad Secret Room, and additive `40 m × 24 m` `BackroomsLevel1Scene.unity` with local Level-Ω safe and visual Lasser portal activation.
- **0.1 Forensic Transition Assessment (`CONFIRMED`):** Completed full Git, world, gameplay, technical, and IP provenance audit.
- **0.2 Git Protection & Commercial Branching (`CONFIRMED`):** Tagged `pre-unity-fellowship-transition-2026-11-24` and created/published `product/unity-fellowship-2027` from `dcac4fe`.
- **0.3 / 0.3.1 Documentation Transition, Editorial Review & Publication (`CONFIRMED`):** Archived 2026 hackathon docs intact into `docs/archive/ai-hackathon-2026/`, authored active commercial + Unity Fellowship documentation, verified link/hash integrity, and published on `product/unity-fellowship-2027`.

---

## Phase 1 — Fellowship Application Readiness (`PLANNED`: Nov 25 – Dec 18, 2026)

*Goal: Deliver an IP-clean, evaluator-ready playable demo (*Slice 01*) and game video before December 18, 2026.*

1. **Creator Decisions Lock:** Finalize brand strategy (real vs. fictionalized storefronts), liminal layer name, opening hook, and Fellowship budget figures (`TBD`).
2. **Mandatory IP & Commercial Clearance Pass:**
   - Replace all third-party horror franchise references/props in `BackroomsLevel1Builder.cs` and `"SS GEORGIE"` in `UndergroundSewerBuilder.cs` with 100% original artifacts.
   - Replace `media/arch-ads/camila_loop.mp4`, unlicensed consumer trademarks (`Alexa`, `iPad`, `El Diario de Juárez`), and external YouTube/unlicensed brand assets per chosen brand policy.
   - Update Unity `ProjectSettings.asset` (`productName`, `companyName`) and remove `HandshakeScene.unity` from playable demo build settings.
3. **Minimal Playable Vertical Slice Loop (*Slice 01*):**
   - Add Title Screen, gated debug modes (`[V]`/`[G]` locked behind dev flag), underground environmental clues leading to the Secret Room code, lightweight Discovery Journal, persistent slice save, and *El Umbral de Lasser* climax + Slice Complete screen.
   - Simplify Windows demo launch so evaluators can run cleanly offline.
4. **Trailer Capture & Application Submission:** Capture IP-clean footage per [Trailer Script](TRAILER_SCRIPT.md), package verified Windows demo build per [Build Delivery](BUILD_DELIVERY.md), and submit before **December 18, 2026, 5:00 p.m. ET**.

---

## Phase 2 — Pre-Fellowship Stabilization & Architecture Refactor (`PLANNED`: Jan – Feb 2027)

*Goal: Remove technical debt before full commercial production begins in March 2027.*

- Decompose monolithic `FirstPersonPlayer.cs` (8,692 lines) into modular C# controllers and migrate core HUD/menus from legacy IMGUI to UGUI or UI Toolkit.
- Finalize **self-contained offline PC architecture** (embedding core deterministic simulation in Unity C# or bundling a managed local runtime with zero manual Node.js/PowerShell steps for players).
- Establish automated Unity EditMode/PlayMode regression tests alongside backend `npm test`.
- Complete full asset/scene hygiene (`Assets/Settings/` material reorganization, removal of unused template/PoC folders).

---

## Phase 3 — Fellowship Production: Core Systems & Content (`PLANNED`: Mar – Aug 2027)

*Goal: Expand Slice 01 into a complete, cohesive commercial campaign.*

- **Q2 2027 (Mar – May 2027) — Narrative & Systems Alpha:**
  - Implement full save/load system, Discovery Journal, key-item inventory, dialogue/clue state tracking, and creator-approved narrative campaign structure across the four verified spatial layers (Main Reality corridor, Underground Storm-Drain Network, Secret Room, and IP-cleared Backrooms Level 1).
  - Populate underground storm-drain network (*La Cisterna / El Nudo*, *Sector C-0*) with authored environmental storytelling, puzzles, and any creator-approved subterranean encounters.
  - Complete *Slice 01* playtesting and evaluate feasibility/value of optional post-Lasser expansion candidates before committing additional world scope.
- **Q3 2027 (Jun – Aug 2027) — Content Complete & Audio/Visual Upgrade:**
  - Complete authored sound design: border streetscape ambience, material-based footsteps, underground acoustics, liminal audio, puzzle foley, and original music score.
  - Upgrade URP lighting, shadow fidelity (replacing `256`-resolution shadow atlas with scalable PC quality presets), occlusion culling, LODs, and visual polish across all scenes.
  - Expand select surface interiors where narratively essential for clue discovery.

---

## Phase 4 — Beta, Playtesting & Commercial Release Prep (`PLANNED`: Sep 2027 – Feb 2028)

*Goal: Certify stability, performance, legal compliance, and commercial distribution readiness ahead of the March 1, 2028 contractual deadline.*

- **Q4 2027 (Sep – Nov 2027) — Content Lock & Closed Playtesting:**
  - Structured external playtests (bilingual English & Spanish players); tune puzzle clarity, pacing, accessibility (subtitles, font scaling, FOV, mouse sensitivity, remappable inputs), and difficulty curve.
  - Full performance profiling across minimum-spec laptop GPUs (30+ FPS) and target desktop PCs (60 FPS goal, pending hardware validation).
- **Dec 2027 – Jan 2028 — Release Candidate & Storefront Readiness:**
  - Complete final legal/IP clearance audit against [IP & Asset Provenance](../../IP_AND_ASSET_PROVENANCE.md) and [Third-Party Notices](../../THIRD_PARTY_NOTICES.md).
  - Finalize storefront page, commercial trailer, press kit, installer/Steam build pipeline, and crash-free RC verification.
- **Internal Target Commercial v1.0 Launch (February 2028 — `PLANNED`):**
  - Commercial PC Windows release of **My Virtual Community — A Living World Beyond Reality.** in **February 2028**, providing schedule buffer ahead of [Unity Fellowship Terms and Conditions](https://unity.com/legal/unity-fellowship-terms-and-conditions) Section 2.1(c), which requires commercial release before March 1, 2028.

---

## Candidate Expansion (`OPTIONAL FUTURE SCOPE — SUBJECT TO CREATOR APPROVAL AND VERTICAL-SLICE VALIDATION`)

The following expansion is **NOT** a committed v1.0 milestone and carries **no mandatory completion date**:
- **First Playable Hidden World Beyond *El Umbral de Lasser*:** A candidate additive realm whose narrative value, production feasibility, and schedule impact will be evaluated only after *Slice 01* playtesting and explicit creator approval.

---

## Post-Launch Expansion Horizon (`PLANNED` / Deferred Beyond v1.0)

Explicitly **deferred** out of v1.0 scope to protect the **February 2028** internal target release (ahead of the March 1, 2028 deadline in Unity Fellowship Terms Section 2.1(c)):
- Additional Ciudad Juárez / El Paso urban districts beyond the Av. Benito Juárez – Paso del Norte corridor.
- Further hidden worlds beyond any future post-Lasser expansion.
- Optional live API Reality-Bridge modules (live radio/news/weather/AI integrations).
- Multiplayer / shared-world modes, console ports, or macOS/Linux builds.
