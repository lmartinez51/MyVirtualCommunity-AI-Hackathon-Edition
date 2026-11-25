# My Virtual Community
## *A Living World Beyond Reality.*

> *"An interconnected first-person exploration adventure where authentic border life conceals an extraordinary network of hidden worlds."*

**My Virtual Community — A Living World Beyond Reality.** is a **single-player, first-person exploration and environmental mystery adventure** in active development for **Windows PC** using **Unity 6 (`6000.3.25f1`)**.

> **Development & Repository Notice:** This game is currently an **in-development playable prototype** transitioning toward its commercial vertical slice (*Unity Fellowship 2027* track on branch `product/unity-fellowship-2027`). It is **not** a finished commercial product, an AI assistant, an MMO, a persistent multiplayer metaverse, or a consumer-ready installer yet. The repository retains its original GitHub name (`MyVirtualCommunity-AI-Hackathon-Edition`) to preserve full Git history; all historical 2026 hackathon documentation is preserved verbatim in [`docs/archive/ai-hackathon-2026/`](docs/archive/ai-hackathon-2026/INDEX.md). The separate baseline repository `lmartinez51/MyVirtualCommunity` remains strictly read-only.

---

## 1. Game Overview & Creative Direction

Set along an authentic urban reconstruction of the **Ciudad Juárez–El Paso international border corridor** (**Avenida Benito Juárez** and the **Puente Internacional Paso del Norte**), *My Virtual Community* grounds the player in a living, schedule-driven binational city — and then peels back its ordinary surface to reveal concealed subterranean infrastructure and non-Euclidean liminal spaces.

- **Genre & Perspective:** Single-player first-person exploration and mystery adventure (`1.60 m` standing eye level, handheld flashlight `[F]`, direct environmental interaction `[E]`).
- **Tonal Contrast:** Everyday urban border realism (storefront schedules, bilingual street life, sunlit viaduct architecture) contrasting against dark subterranean storm drains and eerie liminal vaults.
- **Core Philosophy:** Observation-first spatial discovery backed by deterministic world rules, designed to operate offline-first with optional real-world bridges.

See [docs/GAME_VISION.md](docs/GAME_VISION.md) for the working creative pillars and open creator decisions.

---

## 2. Main Gameplay Concept

The commercial adventure loop (`PLANNED`, see [docs/CORE_GAME_LOOP.md](docs/CORE_GAME_LOOP.md) and [docs/VERTICAL_SLICE.md](docs/VERTICAL_SLICE.md)) centers on:

1. **Exploring the Living Surface:** Navigating Avenida Benito Juárez, local storefronts, and the Paso del Norte pedestrian corridor while observing daily schedules, architectural anomalies, and distinct residents.
2. **Uncovering Hidden Spaces:** Descending into the storm-drain network beneath the streets and breaching concealed doors through discovered clues and access codes.
3. **Crossing Thresholds & Solving Puzzles:** Navigating additively loaded liminal layers, assembling clues, recovering key relics (*El Ojo del Umbral*), and awakening dimensional thresholds (*El Umbral de Lasser*).

---

## 3. Verified World Inventory (`CONFIRMED`)

The current prototype physically constructs four interconnected narrative spaces across **two primary playable Unity scenes** (see [docs/WORLD_ATLAS.md](docs/WORLD_ATLAS.md) for full spatial details and the verified transition matrix):

| World Layer | Unity Scene | Verified Current State |
| :--- | :--- | :--- |
| **Main Reality Layer** *(Av. Benito Juárez, West Manzana, Paso del Norte Bridge & El Paso Inspection Plaza)* | `BorderCorridorScene.unity` | Physically constructed, playable, interactive: East & West commercial blocks, traversable *Óptica Fashion* interior, schedule-synced roll-up shutters, 4 dynamic toll barriers, Av. Juárez Digital Arch, and 7 autonomous Bobblehead NPCs with distinct roles. |
| **Underground Storm-Drain Network** *(La Cisterna / El Nudo, Sector C-0, Abasolo Siphon & Río Bravo Outflow)* | `BorderCorridorScene.unity` *(integrated beneath street level, not a separate scene)* | Physically constructed, playable, partially interactive: vaulted brick/concrete drains (`Y = -3.20 m` to `-8.45 m`), 4 sewer-entry manholes, dark fog atmosphere requiring `[F]` flashlight, interactive Río Bravo outflow gate. **No underground NPCs or puzzles exist yet.** |
| **Secret Room** *(Building A South Bay)* | `BorderCorridorScene.unity` | Physically constructed empty/dusty trapezoid vestibule behind a false wall with a 6-digit steel keypad door (default `"123456"`) and an opaque `Threshold Veil` that additively loads Backrooms Level 1. *(Artifacts, safe, and Lasser portal are inside `BackroomsLevel1Scene.unity`, not inside this small room.)* |
| **Backrooms Level 1** *(Liminal Vault)* | `BackroomsLevel1Scene.unity` *(loaded/unloaded additively)* | `40 m × 24 m` (`960 m²`) labyrinth with flickering fluorescents, 13 dispersed artifacts, 8 inspectable visual-prototype Reality-Bridge props (not wired to live APIs), Evidence Board (`654321`), Level-Ω Vault Safe, *El Ojo del Umbral*, and ***El Umbral de Lasser***. Accessible via the Secret Room veil or a silent ceiling no-clip drop from the Av. Juárez × Azucenas manhole; exits back to Av. Juárez. |

> **Verified Portal Scope (`CONFIRMED`):** ***El Umbral de Lasser* currently performs visual and local state activation only.** Inserting *El Ojo del Umbral* retracts the chains and ignites the spinning vortex, but it **does not** teleport the player or load another world yet.

---

## 4. Technical Architecture Overview (`CONFIRMED`)

- **Unity 6 Client — Presentation & Spatial Layer (`unity-client/`):** Unity `6000.3.25f1` with Universal Render Pipeline (`URP 17.3.0`, Forward+ renderer), Input System `1.20.0`, first-person navigation and UI, additive multi-scene management (`SecretBackroomsTransitionController.cs`), procedural Bobblehead character pipeline (`BobbleheadGeometry.cs` / `BobbleheadPrefabBaker.cs`), and HTTP REST client (`ApiClient.cs`). Unity handles rendering, spatial interaction, and client-side transitions while respecting authoritative backend schedules and states.
- **Authoritative World Core Backend (`src/`):** Hexagonal TypeScript / Node.js server (`src/server/index.ts`, native `node:http`) separating pure domain rules (`src/domain/`: schedules, civil time, 7-NPC autonomous locomotion, social awareness, time-of-day, barriers, citizen reports) from ports (`src/ports/`) and swappable adapters (`src/adapters/`).
- **Offline Default:** Runs locally with `InMemoryWorldCoreAdapter` and `MockMetaModelAdapter` when no external API credentials are set.

---

## 5. Current Capabilities vs. Current Limitations (`CONFIRMED`)

### Implemented Today
- Smooth first-person exploration (`WASD`, `Shift`, `Space`, mouse look, `[F]` flashlight, `[E]` interact, `[M]` minimap, compass, `[F1]` controls, `[Esc]` pause menu).
- 7 distinct canonical Bobblehead NPCs (Border Concierge, Citizen Reporting Representative, 2 Storefront Attendants, and 3 disclaimed Synthetic Community Witnesses) with deterministic movement and dialogue.
- Working multi-world transitions: Street ↔ Underground Storm Drains, Street → Backrooms (Secret Room veil & Azucenas manhole no-clip drop), and Backrooms → Street return door.
- Local Backrooms micro-puzzle (Evidence Board → Level-Ω Safe → *El Ojo del Umbral* → visual activation of *El Umbral de Lasser*).

### Current Limitations (Not Yet Implemented)
- **No complete commercial adventure loop yet:** No title screen, New Game / Continue flow, general save/load, persistent player progression, clue journal, general inventory, or slice completion state. Exiting Backrooms resets the safe, astrolabe, and portal state.
- **Visual-only Lasser portal:** No playable realm exists beyond *El Umbral de Lasser*.
- **No authored audio assets yet:** All current SFX/ambience are procedurally synthesized in C# (`AudioClip.Create`).
- **Developer-only runtime coupling:** Running the full world currently requires launching the local Node.js backend alongside the Unity executable (see Developer Quickstart below).
- **Pre-commercial IP clearance required:** Prototype scenes currently contain third-party horror references, real-world storefront/consumer trademarks, and sample arch video (`media/arch-ads/camila_loop.mp4`) tracked for replacement or permission in [docs/IP_AND_ASSET_PROVENANCE.md](docs/IP_AND_ASSET_PROVENANCE.md).

See [docs/FEATURE_STATUS_MATRIX.md](docs/FEATURE_STATUS_MATRIX.md) for the complete system-by-system breakdown.

---

## 6. Developer Requirements & Development Quickstart

> **Developer Setup vs. Future Player Experience:** The instructions below are for **developers and technical reviewers** running directly from source on Windows. A future commercial Windows release (`PLANNED`) will ship as a self-contained PC installation requiring zero Node.js, npm, or PowerShell setup.

### Prerequisites (Developer Environment)
- **OS:** Windows 10 / 11 (64-bit)
- **Node.js:** v20.18.0+ (required to run the local TypeScript World Core server and tests)
- **PowerShell:** Windows PowerShell 5.1 or PowerShell 7+
- **Unity Editor:** Unity 6 (`6000.3.25f1`) — required to open `unity-client/` or rebuild `MyVirtualCommunity.exe`

### Run From Source (Offline Local Mode)
```powershell
# 1. Install Node.js dependencies
npm install

# 2. Optional: copy environment template (not required for offline mode)
Copy-Item .env.example .env

# 3. Verify or build the Windows Standalone client (requires Unity 6000.3.25f1)
.\scripts\build-standalone.ps1

# 4. Launch the local World Core backend + Unity Standalone client safely
.\scripts\launch-demo.ps1
# (or: npm run demo)
```
When the Unity window closes, `scripts/launch-demo.ps1` cleanly shuts down only the backend process tree it started.

### Optional External Integrations
External services are **strictly optional** and are **not** assumed permanently connected or commercially cleared for shipping without review (see [docs/THIRD_PARTY_NOTICES.md](docs/THIRD_PARTY_NOTICES.md)):
- **Meta Muse Spark 1.3 & SAM 3.1:** Configured only when `MODEL_API_KEY` is provided in a local `.env` file (git-ignored). Without it, the backend runs deterministically offline via `MockMetaModelAdapter`.
- **External Live Data Feeds:** Official CBP Border Wait Times (`bwt.cbp.gov`), Open-Meteo weather, public USD/MXN exchange rates, and Fideicomiso bridge camera links degrade gracefully to `unavailable` or offline fallback when unreachable.
- **External World Core Proxy:** Set `WORLD_CORE_BASE_URL` in `.env` to proxy `WorldCorePort` to a separate HTTP World Core instance.

---

## 7. Build & Testing References

Execute verification commands directly from the repository root to observe live results:

```powershell
# TypeScript static typecheck (tsc --noEmit)
npm run build

# Automated Node.js / TypeScript domain, adapter, application, and HTTP test suites
npm test

# Additional verification & build scripts
npm run verify:recovery
npm run verify:npcs
npm run test:launcher
.\scripts\build-standalone.ps1 -CheckOnly
.\scripts\package-release.ps1
```

- **Latest Verified Backend Run (PREPARATION 0.3, November 24, 2026):** `npm run build` exited with `0` errors and `npm test` passed **615 / 615 tests across 141 suites**. This is a point-in-time verification record rather than a permanent guarantee; always re-run after code changes. Unity PlayMode automated tests have **not** been executed in this transition phase.

See [docs/fellowship/unity-2027/BUILD_DELIVERY.md](docs/fellowship/unity-2027/BUILD_DELIVERY.md) for the Windows build and Fellowship evaluation delivery checklist.

---

## 8. Active Documentation Map

### Product, World & Vertical Slice
- [docs/GAME_VISION.md](docs/GAME_VISION.md) — Creative vision, genre, core pillars, and open creator decisions
- [docs/CORE_GAME_LOOP.md](docs/CORE_GAME_LOOP.md) — Implemented mechanics vs. proposed commercial adventure loop
- [docs/WORLD_ATLAS.md](docs/WORLD_ATLAS.md) — Verified world layers, scenes, and cross-world transition matrix
- [docs/FEATURE_STATUS_MATRIX.md](docs/FEATURE_STATUS_MATRIX.md) — Truthful system-by-system status & limitations
- [docs/VERTICAL_SLICE.md](docs/VERTICAL_SLICE.md) — Provisional *Slice 01 — Thresholds of Juárez* specification

### Unity Fellowship 2027 Package
- [docs/fellowship/unity-2027/APPLICATION_BRIEF.md](docs/fellowship/unity-2027/APPLICATION_BRIEF.md) — Master application reference
- [docs/fellowship/unity-2027/PITCH.md](docs/fellowship/unity-2027/PITCH.md) — Commercial game pitch
- [docs/fellowship/unity-2027/ROADMAP.md](docs/fellowship/unity-2027/ROADMAP.md) — Production roadmap toward March 2028 PC v1.0
- [docs/fellowship/unity-2027/FUNDING_PLAN.md](docs/fellowship/unity-2027/FUNDING_PLAN.md) — Funding categories (`TBD` figures pending creator input) & Fellowship support objectives
- [docs/fellowship/unity-2027/TRAILER_SCRIPT.md](docs/fellowship/unity-2027/TRAILER_SCRIPT.md) — IP-clean game trailer shot plan
- [docs/fellowship/unity-2027/BUILD_DELIVERY.md](docs/fellowship/unity-2027/BUILD_DELIVERY.md) — Reproducible Windows evaluation build checklist

### Provenance, Licensing & Historical Archive
- [LICENSE](LICENSE) — MIT License covering original author-created code and original assets
- [docs/IP_AND_ASSET_PROVENANCE.md](docs/IP_AND_ASSET_PROVENANCE.md) — Commercial asset provenance & clearance register
- [docs/THIRD_PARTY_NOTICES.md](docs/THIRD_PARTY_NOTICES.md) — Unity, npm, vendored library, map, and API licensing inventory
- [docs/archive/ai-hackathon-2026/INDEX.md](docs/archive/ai-hackathon-2026/INDEX.md) — Preserved 2026 AI Hackathon documentation, increment reports, and evaluator evidence
