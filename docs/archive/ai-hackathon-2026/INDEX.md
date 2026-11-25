# AI Hackathon 2026 — Historical Documentation Archive

> **Historical Notice:** Everything in `docs/archive/ai-hackathon-2026/` is a preserved historical record from the **MyVirtualCommunity — AI Hackathon Edition** phase (November 2026). These files are **not** authoritative descriptions of the current commercial game, **My Virtual Community — A Living World Beyond Reality.** For active product, world, technical, and Fellowship documentation, see the root [README.md](../../../README.md) and the active documents under [`docs/`](../../).

---

## 1. Why This Archive Exists

This repository began as `MyVirtualCommunity-AI-Hackathon-Edition` during a 2026 AI hackathon focused on grounded local-business assistance, official border wait-time guidance, and multimodal civic reporting using a hexagonal Node.js/TypeScript backend and Meta AI adapters. Over 44 commits across `feat/meta-model-foundation` and `feat/bridge-paso-del-norte`, the project expanded dramatically into a multi-layered Unity 6 first-person 3D exploration world encompassing the Ciudad Juárez–El Paso corridor, an underground storm-drain network, a concealed Building A doorway and Secret Room threshold, and an additively loaded Backrooms Level 1 labyrinth.

To transition cleanly to the commercial video game track on `product/unity-fellowship-2027` (originating from verified commit `dcac4fedcbcb5035fa1046a461507744509cd96e`, tagged `pre-unity-fellowship-transition-2026-11-24`), all hackathon-era documentation and evaluator evidence have been preserved verbatim here rather than deleted or rewritten.

---

## 2. Preserved Contents Inventory

### 2.1 Snapshot & Foundational Hackathon Documents

| Archived File | Original Location | Historical Role |
| :--- | :--- | :--- |
| [README_HACKATHON_SNAPSHOT.md](README_HACKATHON_SNAPSHOT.md) | `README.md` | Byte-for-byte snapshot of the pre-transition root README (`SHA-256: 40C8299845A6279A471F64F8425EE0CE9DCF3D77EE049FB6C2E1C5EDE35B9F3E`). |
| [ARCHITECTURE.md](ARCHITECTURE.md) | `docs/ARCHITECTURE.md` | Early Milestone 1 hexagonal architecture specification (4 HTTP endpoints, Wing Spot / Tía Lupe fixtures). |
| [DEMO_PLAN.md](DEMO_PLAN.md) | `docs/DEMO_PLAN.md` | Early CLI/HTTP demo plan (7 test suites / 41 tests). |
| [HACKATHON_SCOPE.md](HACKATHON_SCOPE.md) | `docs/HACKATHON_SCOPE.md` | Early MVP scope statement defining hackathon boundaries. |
| [REUSE_AND_ATTRIBUTION.md](REUSE_AND_ATTRIBUTION.md) | `docs/REUSE_AND_ATTRIBUTION.md` | Early hackathon non-claim provenance statement written before Unity 3D assets were added to this repository. |
| [FINAL_PRE_COMMIT_GATE_REPORT.md](FINAL_PRE_COMMIT_GATE_REPORT.md) | `docs/FINAL_PRE_COMMIT_GATE_REPORT.md` | Pre-commit gate audit snapshot from November 3, 2026 (`feat/meta-model-foundation`). |

### 2.2 Increment Implementation & Verification Reports (`increments/`)

All 24 historical increment reports (November 2–4, 2026, `feat/meta-model-foundation`) are preserved unmodified in [`increments/`](increments/):

- `INCREMENT_5_11_3_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_11_3_1_CONFORMANCE_AUDIT_REPORT.md`
- `INCREMENT_5_11_3_2_OPTIONAL_EVIDENCE_REPAIR_REPORT.md`
- `INCREMENT_5_11_4_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_12_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_12_1_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_12_2_ARTIFACT_AUDIT_REPORT.md`
- `INCREMENT_5_12_2_1_VALIDATION_REPORT.md`
- `INCREMENT_5_12_3_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_12_3_1_AUDIT_REPORT.md`
- `INCREMENT_5_12_3_2_FINAL_AUDIT_REPORT.md`
- `INCREMENT_5_13_0_BASELINE_AUDIT_REPORT.md`
- `INCREMENT_5_13_1_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_13_1_1_VERIFICATION_REPORT.md`
- `INCREMENT_5_13_2_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_13_3_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_13_3_1_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_13_4_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_14_0_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_14_0_1_VERIFICATION_REPORT.md`
- `INCREMENT_5_15_0_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_16_0_IMPLEMENTATION_REPORT.md`
- `INCREMENT_5_16_0_1_VERIFICATION_REPORT.md`
- `INCREMENT_5_17_0_IMPLEMENTATION_REPORT.md`

### 2.3 Preserved Visual Assets (`assets/` & `screenshots/`)

- [`assets/myvirtualcommunity-hackathon-overview.jpg`](assets/myvirtualcommunity-hackathon-overview.jpg) — Moved verbatim from `docs/assets/myvirtualcommunity-hackathon-overview.jpg`.
- [`screenshots/511_01_patricia_desk_approach.png`](screenshots/511_01_patricia_desk_approach.png) — Moved verbatim from `screenshots/511_01_patricia_desk_approach.png`.
- [`screenshots/511_02_citizen_reporting_modal_form.png`](screenshots/511_02_citizen_reporting_modal_form.png) — Moved verbatim from `screenshots/511_02_citizen_reporting_modal_form.png`.
- [`screenshots/511_03_sam_and_spark_draft_review.png`](screenshots/511_03_sam_and_spark_draft_review.png) — Moved verbatim from `screenshots/511_03_sam_and_spark_draft_review.png` *(historical procedural GDI+ UI mockup, not live model inference)*.
- [`screenshots/511_04_world_core_confirmed_receipt.png`](screenshots/511_04_world_core_confirmed_receipt.png) — Moved verbatim from `screenshots/511_04_world_core_confirmed_receipt.png`.

> **Note on Relative Paths Inside Archived Files:** To preserve byte-for-byte historical fidelity, archived Markdown files were **not** edited to rewrite relative links. References inside `README_HACKATHON_SNAPSHOT.md` such as `docs/assets/myvirtualcommunity-hackathon-overview.jpg` and `screenshots/511_*.png` now correspond to [`assets/myvirtualcommunity-hackathon-overview.jpg`](assets/myvirtualcommunity-hackathon-overview.jpg) and [`screenshots/511_*.png`](screenshots/) within this archive directory. Original Unity screenshot captures under `unity-client/screenshots/` remain untouched in place.

---

## 3. Historical Claims That Are Now Obsolete

Readers consulting archived files must account for major discrepancies between early hackathon statements and the current codebase:

1. **Unity 3D Client & World Core Exclusion (`REUSE_AND_ATTRIBUTION.md`, `HACKATHON_SCOPE.md`):** Both documents state that the Unity 3D client, Bobblehead characters, NPC autonomy/navigation, SAM 3.1 segmentation, and World Core simulation are external or excluded from this repository. All of those systems are now physically present in `unity-client/` and `src/`.
2. **Zero Runtime Dependencies (`HACKATHON_SCOPE.md`, `DEMO_PLAN.md`):** Early milestones had zero runtime npm dependencies; `package.json` now includes `@meta-sam/parser` and `sharp` as runtime dependencies.
3. **API & Fixture Scope (`ARCHITECTURE.md`, `DEMO_PLAN.md`):** Early docs describe only 4 REST endpoints and Gómez Morín fixtures (`restaurant:wing-spot`, `restaurant:pasteleria-tia-lupe`). The current backend exposes ~30 REST routes covering Avenida Benito Juárez, Paso del Norte bridge, 7 autonomous NPCs, weather, exchange rates, arch media, citizen reporting, and secret-vault occupancy.
4. **Outdated Test Counts & Release Archive Hashes (`FINAL_PRE_COMMIT_GATE_REPORT.md`, `README_HACKATHON_SNAPSHOT.md`):** Historical reports cite 41 tests (Milestone 1), 449 tests (Nov 3), or 527 tests (mid-Nov), along with older ZIP hashes (`99E452A1...` on Nov 3 and `A3B619F7...` on Nov 10). Always refer to active documentation and fresh verification runs for current status.
5. **Commercial Positioning (`README_HACKATHON_SNAPSHOT.md`):** The archived README frames the repository as an "AI Hackathon Edition" evaluator demo rather than the commercial first-person exploration adventure documented on `product/unity-fellowship-2027`.

---

## 4. Technical & Provenance Information That Remains Useful

- **Hexagonal Architecture Invariants (`ARCHITECTURE.md`):** Separation of deterministic domain logic (`src/domain/`), port contracts (`src/ports/`), and swappable adapters (`src/adapters/`) remains the backbone of the World Core backend.
- **Anti-Hallucination & Evidence Provenance Taxonomy (`FINAL_PRE_COMMIT_GATE_REPORT.md`, `README_HACKATHON_SNAPSHOT.md`):** Strict separation of deterministic state from optional LLM synthesis, plus transparent distinction between mockups, domain tests, adapter contracts, and empirical runs.
- **Persistence & Launcher Safety Design (`INCREMENT_5_12_*`, `INCREMENT_5_13_3_*`):** Atomic `.runtime/citizen-reports.json` write-fsync-rename recovery and SHA-256 build-freshness verification design history.
- **Historical Attribution (`REUSE_AND_ATTRIBUTION.md`):** Preserves the original attribution record distinguishing the read-only baseline repository (`lmartinez51/MyVirtualCommunity`) from hackathon-created TypeScript modules. See [`docs/IP_AND_ASSET_PROVENANCE.md`](../../IP_AND_ASSET_PROVENANCE.md) for the current, repository-wide commercial provenance register.
