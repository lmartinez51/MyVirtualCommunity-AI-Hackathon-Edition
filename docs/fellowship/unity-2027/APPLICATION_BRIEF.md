# Unity Fellowship 2027 — Master Application Brief

**Project:** My Virtual Community — *A Living World Beyond Reality.*
**Applicant / Creator:** Lorenzo Martinez (`TBD` legal entity / studio name — `AUTHOR DECISION REQUIRED`)
**Engine:** Unity 6 (`6000.3.25f1`, URP `17.3.0`)
**Target Platform:** PC Windows (`x86_64`)
**Application Deadline:** December 18, 2026, at 5:00 p.m. ET
**Fellowship Program Start:** March 2027
**Internal Target Commercial Release:** February 2028, providing schedule buffer ahead of [Unity Fellowship Terms and Conditions](https://unity.com/legal/unity-fellowship-terms-and-conditions) Section 2.1(c), which requires commercial release before March 1, 2028.

Fellowship package documents: [Pitch](PITCH.md) · [Roadmap](ROADMAP.md) · [Funding Plan](FUNDING_PLAN.md) · [Trailer Script](TRAILER_SCRIPT.md) · [Build Delivery](BUILD_DELIVERY.md)
Core product references: [README](../../../README.md) · [Game Vision](../../GAME_VISION.md) · [World Atlas](../../WORLD_ATLAS.md) · [Feature Status Matrix](../../FEATURE_STATUS_MATRIX.md) · [Vertical Slice](../../VERTICAL_SLICE.md) · [IP & Asset Provenance](../../IP_AND_ASSET_PROVENANCE.md)

---

## 1. Truthful Project Summary

**My Virtual Community — A Living World Beyond Reality.** is a single-player, first-person exploration and mystery adventure built in Unity 6. Set along an authentic urban reconstruction of the **Ciudad Juárez–El Paso international border corridor** (Avenida Benito Juárez and the Paso del Norte International Bridge), the game contrasts a lived-in, schedule-driven binational city with an interconnected sequence of hidden subterranean and liminal worlds concealed beneath ordinary streets.

The project evolved from an earlier 2026 AI Hackathon foundation (archived intact in [`docs/archive/ai-hackathon-2026/`](../../archive/ai-hackathon-2026/INDEX.md)) into a dedicated commercial game track on branch `product/unity-fellowship-2027`. It is **not** an MMO, persistent multiplayer metaverse, or chatbot demo.

---

## 2. Verified Current Development Status (`CONFIRMED`)

- **What is playable today:**
  - Full first-person exploration across the Ciudad Juárez–El Paso surface corridor (`BorderCorridorScene.unity`), including East/West commercial blocks, walkable *Óptica Fashion*, West Manzana & Calle Ignacio Mariscal, Paso del Norte viaduct, and El Paso U.S. Inspection Plaza.
  - 7 autonomous, procedurally baked Bobblehead NPCs with distinct roles, schedule-driven movement, and deterministic World Core authority (`src/domain/`).
  - Physically integrated underground storm-drain network beneath `BorderCorridorScene.unity` (*La Cisterna / El Nudo*, *Sector C-0* bulkhead, `Y = -8.45 m` Abasolo inverted siphon, 4 sewer manholes, Río Bravo interactive outflow gate, `[F]` tactical flashlight).
  - Building A South Secret Room keypad door (`SecretKeypadDoorController.cs`) and opaque Threshold Veil additively loading **Remanencia — Nivel 0** (`BackroomsLevel1Scene.unity`, `40 m × 24 m`), plus a legacy alternate manhole entry via Av. Juárez × Azucenas and a clean return door back to Avenida Juárez.
  - Self-contained **Remanencia — Nivel 0** micro-puzzle (Evidence Board PDN-00 → Level-Ω Safe → *El Ojo del Umbral* → visual activation of the Dimensional Passage).
- **What is NOT yet implemented (explicit transparency):**
  - The Dimensional Passage (*Pasaje dimensional*) is currently a **visual activation only** — it does not load or transport the player to another world.
  - No title screen, save/load, clue journal, general inventory, quest/progression framework, or slice end state exists yet.
  - Audio is currently procedural C# synthesis only (no authored soundscape/music/foley files yet).
  - Current developer build requires running the local Node.js backend via `scripts/launch-demo.ps1` alongside `MyVirtualCommunity.exe` (not yet a single-click consumer installer).
  - Several prototype textures, props, and `media/arch-ads/camila_loop.mp4` contain uncleared third-party franchise or brand references that **must be replaced** before submitting the Fellowship playable build or game video.

---

## 3. Unity Fellowship Requirements Checklist

| Fellowship Requirement | Source Document | Current Readiness | Blocking Items Before Dec 18, 2026 |
| :--- | :--- | :---: | :--- |
| **Unity-based commercial game** | [Game Vision](../../GAME_VISION.md), [World Atlas](../../WORLD_ATLAS.md) | `PARTIAL` | Update `ProjectSettings.asset` branding (`DefaultCompany` → studio name; `unity-client` → game title); complete IP clearance pass. |
| **Playable build / demo** | [Build Delivery](BUILD_DELIVERY.md), [Vertical Slice](../../VERTICAL_SLICE.md) | `PARTIAL` | Execute IP replacement pass; add minimal title screen / slice loop / evaluator-friendly offline launch; package fresh verified Windows build. |
| **Creative vision** | [Game Vision](../../GAME_VISION.md), [Pitch](PITCH.md) | `PARTIAL` | Creator sign-off on open narrative/brand decisions (`AUTHOR DECISION REQUIRED`). |
| **Game video / trailer** | [Trailer Script](TRAILER_SCRIPT.md) | `PLANNED` | Capture clean in-engine footage **only after** IP/brand replacement pass is complete. |
| **Product roadmap** | [Roadmap](ROADMAP.md) | `PARTIAL` | Creator review and confirmation of milestone owners & February 2028 internal launch targets (ahead of the March 1, 2028 contractual deadline). |
| **Commercial release plans** | [Pitch](PITCH.md), [Roadmap](ROADMAP.md) | `PARTIAL` | Creator confirmation of storefront targets (e.g., Steam PC), pricing tier (`TBD`), and marketing milestones. |
| **Funding & support objectives** | [Funding Plan](FUNDING_PLAN.md) | `PARTIAL` | Creator to supply real budget figures, personal/contractor allocations, and mentorship/technical support priorities (currently `TBD`). |

---

## 4. Eligibility, Fiscal & Administrative Questions (`AUTHOR DECISION REQUIRED` / `REQUIRES WRITTEN UNITY CLARIFICATION`)

Before submitting the application, the creator must verify, document, and obtain written Unity Fellowship clarification where unresolved:
1. **Legal Entity & Applicant Eligibility (`REQUIRES WRITTEN UNITY CLARIFICATION`):** Individual applicant vs. registered studio entity (`TBD`), geographic/jurisdictional eligibility, and team composition rules under official Unity Fellowship terms.
2. **Fiscal, Tax & Grant-Disbursement Terms (`REQUIRES WRITTEN UNITY CLARIFICATION`):** Applicable cross-border/local tax treatment, invoicing or disbursement structure, and allowable budget categories under official Fellowship rules.
3. **Unity License Compliance (`REQUIRES WRITTEN UNITY CLARIFICATION`):** Confirm current Unity 6 license tier (`ProjectVersion.txt`: `6000.3.25f1`) and commercial/Fellowship licensing requirements.
4. **Commercial Ownership & IP Warranty (`AUTHOR DECISION REQUIRED`):** Confirm 100% author ownership of original code/assets (including conceptual continuity from `lmartinez51/MyVirtualCommunity`) and complete remediation of all `CRITICAL`/`HIGH` items in [IP & Asset Provenance](../../IP_AND_ASSET_PROVENANCE.md).
5. **Release Commitment Feasibility (`AUTHOR DECISION REQUIRED`):** Confirm realistic scope for an internal February 2028 PC Windows commercial v1.0 release (providing buffer ahead of [Unity Fellowship Terms and Conditions](https://unity.com/legal/unity-fellowship-terms-and-conditions) Section 2.1(c), which requires commercial release before March 1, 2028) with available solo/small-team bandwidth.

---

## 5. Immediate Pre-Submission Action Queue (Nov 24 – Dec 18, 2026)

1. Creator review and approval of PREPARATION 0.3 documentation and all `AUTHOR DECISION REQUIRED` items.
2. Execute **IP & Asset Clearance Pass** (remove all horror franchise references, replace `camila_loop.mp4`, execute chosen storefront/brand strategy, remove unlicensed consumer trademarks).
3. Implement minimal **Fellowship Playable Demo Loop** (*Slice 01 — Thresholds of Juárez* core beats, title screen, offline-friendly launch, debug-mode gating).
4. Record **IP-clean Game Trailer** following [Trailer Script](TRAILER_SCRIPT.md).
5. Fill all `TBD` fields in [Funding Plan](FUNDING_PLAN.md) and finalize submission forms ahead of **December 18, 2026, 5:00 p.m. ET**.
