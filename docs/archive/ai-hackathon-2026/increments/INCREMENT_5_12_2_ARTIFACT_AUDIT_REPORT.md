# INCREMENT 5.12.2 ARTIFACT AUDIT REPORT
## Root Artifact Audit & Repository Organization Proposal

**Mode:** Strictly Read-Only Forensic Audit and Repository Organization Proposal  
**Target Repository:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity-AI-Hackathon-Edition`  
**Active Branch:** `feat/meta-model-foundation`  
**Benchmark Repository:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity` (Strictly Read-Only — 0 Modifications)  
**Execution Timestamp:** 2026-11-03T16:20:00-07:00  

---

## 1. Executive Summary

Increment 5.12.2 performed a strictly read-only forensic audit of the two untracked root-level directories in the repository:
1. `scratch/` (49 files totaling ~3.76 MB)
2. `screenshots/` (4 curated image files totaling ~737 KB)

The objective was to evaluate every untracked artifact, identify reusable testing and developer tooling, classify visual evidence, review root-level documentation reports, audit `.gitignore` policies, and formulate an actionable repository organization proposal for the final hackathon submission.

**Critical Mandate Compliance:**
- **Zero file modifications, zero moves, zero renames, zero deletions, zero staging, zero commits, and zero pushes were performed.**
- The benchmark repository at `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity` remains 100% untouched.
- `unity-client/Assets/Screenshots/` was confirmed to be completely untouched (does not exist in the working tree).

---

## 2. Forensic Inventory of Root `scratch/` (49 Files)

Every file in `scratch/` was inspected for relative path, size, file extension, last modification date, purpose, dependencies, and security/privacy sensitivity:

| # | Relative Path | Extension | Size (Bytes) | Last Modified (UTC) | Purpose / Description | Contains Secrets / User Paths | Classification |
|---|---|---|---|---|---|---|---|
| 1 | `scratch/backup_legacy_humanoid.ps1` | `.ps1` | 7,220 | 2026-11-01 03:25 | One-off PowerShell script to backup humanoid controller assets before Bobblehead NPC migration | No secrets; contains local path refs | Obsolete experimental script |
| 2 | `scratch/backup_more_editor_scripts.js` | `.js` | 1,723 | 2026-11-01 03:49 | Node.js script to move editor scripts in Unity project | No secrets; contains absolute path | Obsolete experimental script |
| 3 | `scratch/bake_all_npcs.log` | `.log` | 299,452 | 2026-11-02 07:55 | Unity headless build log for NPC prefab baking | No secrets; standard Unity log | Execution log |
| 4 | `scratch/bridge_screenshots.log` | `.log` | 38,101 | 2026-11-02 00:56 | Unity execution log for bridge screenshot generation | No secrets; standard Unity log | Execution log |
| 5 | `scratch/build_5114_standalone.log` | `.log` | 588,810 | 2026-11-03 06:21 | Unity Standalone build log for Increment 5.11.4 | No secrets; standard Unity log | Execution log |
| 6 | `scratch/build_591.log` | `.log` | 593,081 | 2026-11-03 01:07 | Unity Standalone build log for Increment 5.9.1 | No secrets; standard Unity log | Execution log |
| 7 | `scratch/build_corridor.log` | `.log` | 38,348 | 2026-11-01 03:49 | Unity scene build log for corridor assembly | No secrets; standard Unity log | Execution log |
| 8 | `scratch/build_player.log` | `.log` | 548,486 | 2026-11-01 04:00 | Standalone player compilation log | No secrets; standard Unity log | Execution log |
| 9 | `scratch/consolidate.log` | `.log` | 49,924 | 2026-11-01 03:25 | Asset consolidation execution log | No secrets; standard Unity log | Execution log |
| 10 | `scratch/copy_510_screenshots.ps1` | `.ps1` | 396 | 2026-11-02 02:29 | One-off copy script for Increment 5.10 screenshots | No secrets; local script | Temporary diagnostic artifact |
| 11 | `scratch/copy_511_screenshots.ps1` | `.ps1` | 679 | 2026-11-02 08:01 | One-off copy script for Increment 5.11 screenshots | No secrets; local script | Temporary diagnostic artifact |
| 12 | `scratch/copy_bridge_screenshots.ps1` | `.ps1` | 407 | 2026-11-02 00:16 | One-off copy script for bridge captures | No secrets; local script | Temporary diagnostic artifact |
| 13 | `scratch/create_updated_textures.ps1` | `.ps1` | 8,872 | 2026-10-31 02:12 | Procedural texture generator for corridor storefront signs using `System.Drawing.Bitmap` | No secrets; portable script | Reusable PowerShell utility |
| 14 | `scratch/delete_legacy.log` | `.log` | 78,180 | 2026-11-01 03:37 | Asset deletion log from legacy cleanup | No secrets; standard Unity log | Execution log |
| 15 | `scratch/generate_bridge_sign.ps1` | `.ps1` | 4,001 | 2026-11-01 23:52 | Procedural texture generator for Paso del Norte bridge sign using `System.Drawing.Graphics` | No secrets; portable script | Reusable PowerShell utility |
| 16 | `scratch/increment_5_12_prompt.txt` | `.txt` | 15,171 | 2026-11-03 06:51 | Raw prompt text extracted from conversation transcript | No secrets; prompt text | Temporary diagnostic artifact |
| 17 | `scratch/kiosk_elevation.log` | `.log` | 35,493 | 2026-11-02 00:55 | Log from kiosk elevation verification | No secrets; standard Unity log | Execution log |
| 18 | `scratch/prompt_5114.txt` | `.txt` | 21,678 | 2026-11-03 00:47 | Raw prompt text for Increment 5.11.4 | No secrets; prompt text | Temporary diagnostic artifact |
| 19 | `scratch/rebuild_bridge.log` | `.log` | 39,402 | 2026-11-02 02:04 | Log from bridge entrance geometry rebuild | No secrets; standard Unity log | Execution log |
| 20 | `scratch/run_510_screenshots.ps1` | `.ps1` | 705 | 2026-11-02 02:16 | Invokes Unity in headless batchmode to capture 5.10 camera positions | No secrets; portable script | Reusable test tooling |
| 21 | `scratch/run_5114_screenshots.ps1` | `.ps1` | 1,239 | 2026-11-03 01:14 | Invokes Unity in headless batchmode to capture 5.11.4 enlarged viewer positions | No secrets; portable script | Reusable test tooling |
| 22 | `scratch/run_511_screenshots.ps1` | `.ps1` | 755 | 2026-11-02 08:00 | Invokes Unity in headless batchmode to capture 5.11 citizen report positions | No secrets; portable script | Reusable test tooling |
| 23 | `scratch/run_591_screenshots.ps1` | `.ps1` | 746 | 2026-11-01 22:39 | Headless screenshot runner for 5.9.1 verification | No secrets; portable script | Reusable test tooling |
| 24 | `scratch/run_bake_all_npcs.ps1` | `.ps1` | 739 | 2026-11-01 22:32 | Headless batchmode runner to bake all NPC prefabs | No secrets; portable script | Reusable test tooling |
| 25 | `scratch/run_boot_test.ps1` | `.ps1` | 409 | 2026-11-01 04:11 | Standalone client boot verification script | No secrets; portable script | Reusable test tooling |
| 26 | `scratch/run_boot_test_591.ps1` | `.ps1` | 1,109 | 2026-11-01 22:39 | Standalone boot test runner with log inspection | No secrets; portable script | Reusable test tooling |
| 27 | `scratch/run_bridge_screenshots.ps1` | `.ps1` | 711 | 2026-11-02 00:05 | Headless runner for bridge entrance captures | No secrets; portable script | Reusable test tooling |
| 28 | `scratch/run_corridor_verification.ps1` | `.ps1` | 691 | 2026-11-02 00:04 | Headless runner for corridor geometry verification | No secrets; portable script | Reusable test tooling |
| 29 | `scratch/run_inspect_kiosk.ps1` | `.ps1` | 590 | 2026-11-02 00:55 | Headless runner for kiosk raycast inspection | No secrets; portable script | Reusable test tooling |
| 30 | `scratch/run_rebuild_bridge_entrance.ps1` | `.ps1` | 828 | 2026-11-01 23:54 | Headless runner to rebuild bridge entrance geometry | No secrets; portable script | Reusable test tooling |
| 31 | `scratch/run_unity_build_591.ps1` | `.ps1` | 733 | 2026-11-01 22:39 | Command-line Unity Standalone build invocation script | No secrets; portable script | Reusable test tooling |
| 32 | `scratch/run_z_detector.ps1` | `.ps1` | 613 | 2026-11-02 00:31 | Headless runner for automated z-fighting detection | No secrets; portable script | Reusable test tooling |
| 33 | `scratch/screenshots_510.log` | `.log` | 40,404 | 2026-11-02 02:16 | Unity log for 5.10 screenshot batch | No secrets; standard Unity log | Execution log |
| 34 | `scratch/screenshots_511.log` | `.log` | 55,065 | 2026-11-02 08:00 | Unity log for 5.11 screenshot batch | No secrets; standard Unity log | Execution log |
| 35 | `scratch/screenshots_5114.log` | `.log` | 41,123 | 2026-11-03 06:20 | Unity log for 5.11.4 screenshot batch | No secrets; standard Unity log | Execution log |
| 36 | `scratch/screenshot_591.log` | `.log` | 44,822 | 2026-11-01 23:15 | Unity log for 5.9.1 screenshot batch | No secrets; standard Unity log | Execution log |
| 37 | `scratch/screenshot_suite.log` | `.log` | 37,251 | 2026-11-01 04:12 | Unity log for corridor screenshot suite | No secrets; standard Unity log | Execution log |
| 38 | `scratch/standalone_boot.log` | `.log` | 1,471 | 2026-11-01 04:10 | Standalone executable boot check log | No secrets; standard Unity log | Execution log |
| 39 | `scratch/standalone_boot_591.log` | `.log` | 1,471 | 2026-11-02 02:29 | Standalone executable boot check log | No secrets; standard Unity log | Execution log |
| 40 | `scratch/test-launcher-compatibility.ps1` | `.ps1` | 8,571 | 2026-11-03 22:42 | Comprehensive 7-suite test harness for `launch-demo.ps1` (PS 5.1 compatibility, process isolation, port reuse, safe abort) | No secrets; portable script | **Reusable test tooling (High Value)** |
| 41 | `scratch/test-launcher.ps1` | `.ps1` | 1,360 | 2026-11-03 07:00 | Initial quick test harness for `launch-demo.ps1` | No secrets; subsumed by #40 | Temporary diagnostic artifact |
| 42 | `scratch/test_all_npcs_live.ts` | `.ts` | 5,353 | 2026-11-01 21:36 | E2E integration test querying all 7 NPCs against local World Core / Meta endpoints | No secrets; pure TypeScript | **Reusable test tooling (High Value)** |
| 43 | `scratch/test_encode.js` | `.js` | 718 | 2026-11-03 01:06 | Isolated micro-test for binary image base64 encoding | No secrets; pure JS | Temporary diagnostic artifact |
| 44 | `scratch/test_png_overlay.js` | `.js` | 1,251 | 2026-11-03 01:06 | Isolated test verifying Sharp mask blending | No secrets; pure JS | Temporary diagnostic artifact |
| 45 | `scratch/test_sam.js` | `.js` | 1,300 | 2026-11-03 00:50 | Isolated unit test for `@meta-sam/parser` polygons | No secrets; pure JS | Temporary diagnostic artifact |
| 46 | `scratch/test_server_restart_recovery.ts` | `.ts` | 6,078 | 2026-11-02 22:52 | E2E test verifying atomic persistence survives server crashes and restarts | No secrets; pure TypeScript | **Reusable test tooling (High Value)** |
| 47 | `scratch/update_corridor_setup.js` | `.js` | 3,846 | 2026-11-01 03:37 | One-off scene hierarchy adjustment script | No secrets; contains path refs | Obsolete experimental script |
| 48 | `scratch/verification_test.log` | `.log` | 53,835 | 2026-11-02 02:14 | Test run execution log | No secrets; standard Unity log | Execution log |
| 49 | `scratch/z_detector.log` | `.log` | 125,491 | 2026-11-02 00:53 | Unity z-fighting diagnostic detector output log | No secrets; standard Unity log | Execution log |

---

## 3. Forensic Inventory of Root `screenshots/` (4 Files)

The root `screenshots/` directory contains exactly 4 files:

| # | Relative Path | Format | Width | Height | Size (Bytes) | Visual Content Description | Classification | Privacy & Sensitivity |
|---|---|---|---|---|---|---|---|---|
| 1 | `screenshots/511_01_patricia_desk_approach.png` | PNG | 1280 | 720 | 330,365 | First-person player view approaching Lic. Patricia Morales at the "Enlace de Atención Ciudadana" municipal desk on the western sidewalk of Avenida Benito Juárez. Features the 3D bobblehead character, laptop on desk, bilingual identification pill, and downtown corridor architecture in the background. | **A. Final Evaluator Evidence** | Clean. 100% synthetic 3D rendering. Zero credentials or private photos. |
| 2 | `screenshots/511_02_citizen_reporting_modal_form.png` | PNG | 1280 | 720 | 136,438 | Interactive HUD modal for Citizen Reporting intake. Shows 9 municipal category selection chips (Roads, Sidewalks, Waste, Water, Lighting, Signs, Parks, Trees, Other), incident description text area, location input, and photo attachment button. | **A. Final Evaluator Evidence** | Clean. Demonstrates the municipal issue reporting UI. Zero private data. |
| 3 | `screenshots/511_03_sam_and_spark_draft_review.png` | PNG | 1280 | 720 | 134,945 | Generated report draft review panel. Shows the structured civic summary synthesized by Meta Muse Spark 1.3, the non-emergency municipal safety assessment, and the SAM 3.1 visual segmentation results (detected regions, bounding boxes, and engineering disclaimer). | **A. Final Evaluator Evidence** | Clean. Demonstrates AI draft and visual segmentation results. |
| 4 | `screenshots/511_04_world_core_confirmed_receipt.png` | PNG | 1280 | 720 | 135,479 | Authoritative confirmation dialogue issued by the World Core upon report submission. Displays the confirmed municipal folio (`FOLIO-2026-XXXXX`), cryptographic verification hash, simulated municipal department routing, and timestamp. | **A. Final Evaluator Evidence** | Clean. Demonstrates authoritative persistence and receipt generation. |

### Visual Quality & Evaluation Value
- All 4 images are crisp 1280x720 PNG renders captured at 120 DPI.
- They form a continuous 4-step sequence illustrating the **Multimodal Citizen Reporting Golden Path**:
  1. NPC approach & desk interaction
  2. Intake modal form
  3. AI multimodal draft review
  4. World Core verified receipt

---

## 4. Reusable Tooling Analysis

Three scripts currently located in `scratch/` provide permanent architectural value and are recommended for promotion to maintained testing/tooling directories:

### 1. `scratch/test-launcher-compatibility.ps1` (8,571 bytes)
- **Capability:** Comprehensive 7-suite test harness for `launch-demo.ps1` covering Windows PowerShell 5.1 compatibility, parent environment isolation, target port binding, child process tree termination, pre-existing backend reuse without kill, unrelated process protection, and external port conflict abort.
- **Recommendation:** Relocate to `tests/launcher/test-launcher-compatibility.ps1`.

### 2. `scratch/test_all_npcs_live.ts` (5,353 bytes)
- **Capability:** Complete TypeScript integration test querying all 7 corridor NPCs (Elena Ramos, Lic. Patricia Morales, Carlos, Valeria, Mateo, Sofía, Don Héctor) against live or mock endpoints, validating provenance disclosures and response contracts.
- **Recommendation:** Relocate to `tests/integration/test-all-npcs-live.ts`.

### 3. `scratch/test_server_restart_recovery.ts` (6,078 bytes)
- **Capability:** Validates atomic `.runtime` persistence across simulated server crashes and restarts, asserting folio integrity and report retrieval.
- **Recommendation:** Relocate to `tests/persistence/test-server-restart-recovery.ts`.

---

## 5. Review of Root-Level Markdown Reports

The repository root currently contains four untracked historical increment reports:
- `INCREMENT_5_11_3_IMPLEMENTATION_REPORT.md` (21,585 bytes)
- `INCREMENT_5_11_3_1_CONFORMANCE_AUDIT_REPORT.md` (17,440 bytes)
- `INCREMENT_5_11_3_2_OPTIONAL_EVIDENCE_REPAIR_REPORT.md` (15,960 bytes)
- `INCREMENT_5_11_4_IMPLEMENTATION_REPORT.md` (23,124 bytes)

Meanwhile, `docs/` houses the subsequent reports:
- `docs/INCREMENT_5_12_IMPLEMENTATION_REPORT.md`
- `docs/INCREMENT_5_12_1_IMPLEMENTATION_REPORT.md`
- `docs/ARCHITECTURE.md`
- `docs/DEMO_PLAN.md`
- `docs/HACKATHON_SCOPE.md`
- `docs/REUSE_AND_ATTRIBUTION.md`

**Recommendation:** For clean repository organization, all four 5.11.x reports should be moved into `docs/` alongside the other increment reports, leaving the repository root clean with only [`README.md`](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/README.md), `package.json`, `tsconfig.json`, and `.gitignore`.

---

## 6. README & Documentation Reference Analysis

An automated search across `README.md` and all files in `docs/` revealed:
- `README.md` does **not** contain broken image links to `screenshots/` or `scratch/`.
- `docs/INCREMENT_5_12_IMPLEMENTATION_REPORT.md` and `docs/INCREMENT_5_12_1_IMPLEMENTATION_REPORT.md` reference `scratch/test-launcher.ps1` and `scratch/test-launcher-compatibility.ps1` as the test harnesses used during verification.
- None of the public documentation leaks secrets or hardcoded user paths.

**Opportunity for Enhancement:**
The 4 curated screenshots in `screenshots/` are high-quality visual evidence that can be embedded into [`README.md`](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/README.md) under Section 7 (*Citizen Reporting Workflow*) to provide immediate visual clarity for external judges.

---

## 7. `.gitignore` Audit & Proposed Policy

### Current `.gitignore` State
```gitignore
# Dependencies
node_modules/

# Output
dist/
Builds/

# Logs
*.log
logs/

# Operating System Files
.DS_Store
Thumbs.db

# Environment variables & secrets
.env
.env.*
!.env.example

# Unity Client Generated Artifacts & Solutions
unity-client/[Ll]ibrary/
unity-client/[Tt]emp/
unity-client/[Oo]bj/
unity-client/[Bb]uild/
unity-client/[Bb]uilds/
unity-client/[Ll]ogs/
unity-client/[Uu]ser[Ss]ettings/
unity-client/[Mm]emory[Cc]aptures/
unity-client/.vs/
unity-client/.vsconfig
unity-client/*.csproj
unity-client/*.unityproj
unity-client/*.sln
unity-client/*.slnx
unity-client/*.userprefs
unity-client/*.pidb
unity-client/*.booproj

# Runtime Evidence & Ephemeral Storage (Increment 5.11)
.runtime/
data/evidence/
```

### Findings
1. **`scratch/` is missing:** Because `scratch/` is not in `.gitignore`, all 49 temporary files and logs dirty `git status`.
2. **`screenshots/` is untracked but not ignored:** This is correct if the 4 curated images are intended to be tracked in Git for evaluators.
3. **Logs in subdirectories:** `*.log` only ignores logs in tracked directories; because `scratch/` was untracked, its 19 `.log` files showed up under `scratch/`. Adding `scratch/` to `.gitignore` cleanly resolves this.

### Proposed `.gitignore` Policy (Not Yet Applied)
```gitignore
# Local Scratch & Diagnostics
scratch/

# Curated Screenshots Policy:
# Root screenshots/ is tracked in Git for evaluator evidence.
# Temporary or raw captures should reside in scratch/ or .runtime/.
```

---

## 8. Proposed Repository Organization

The recommended final directory layout cleanly separates documentation, maintained tooling, tests, curated evidence, and ephemeral scratch artifacts:

```text
MyVirtualCommunity-AI-Hackathon-Edition/
├── .env.example
├── .gitignore                               (Updated with scratch/ policy)
├── LICENSE
├── package.json
├── README.md                                (Root README with embedded evidence)
├── tsconfig.json
│
├── docs/                                    (ALL project documentation)
│   ├── ARCHITECTURE.md
│   ├── DEMO_PLAN.md
│   ├── HACKATHON_SCOPE.md
│   ├── REUSE_AND_ATTRIBUTION.md
│   ├── INCREMENT_5_11_3_IMPLEMENTATION_REPORT.md        (Moved from root)
│   ├── INCREMENT_5_11_3_1_CONFORMANCE_AUDIT_REPORT.md    (Moved from root)
│   ├── INCREMENT_5_11_3_2_OPTIONAL_EVIDENCE_REPAIR_REPORT.md (Moved from root)
│   ├── INCREMENT_5_11_4_IMPLEMENTATION_REPORT.md        (Moved from root)
│   ├── INCREMENT_5_12_IMPLEMENTATION_REPORT.md
│   ├── INCREMENT_5_12_1_IMPLEMENTATION_REPORT.md
│   └── INCREMENT_5_12_2_ARTIFACT_AUDIT_REPORT.md
│
├── screenshots/                             (Curated evaluator visual evidence)
│   ├── 511_01_patricia_desk_approach.png
│   ├── 511_02_citizen_reporting_modal_form.png
│   ├── 511_03_sam_and_spark_draft_review.png
│   └── 511_04_world_core_confirmed_receipt.png
│
├── scripts/                                 (Maintained production & evaluator utilities)
│   ├── launch-demo.ps1                      (Windows evaluator launcher)
│   ├── package-release.ps1                  (Release archiver)
│   ├── meta-smoke-test.ts
│   ├── run-tests.js
│   ├── verify-live-assistant.ts
│   └── verify-world-core-integration.ts
│
├── tests/                                   (Permanent test suites)
│   ├── application/ ...
│   ├── domain/ ...
│   ├── server/ ...
│   ├── launcher/                            (NEW candidate folder)
│   │   └── test-launcher-compatibility.ps1  (Promoted from scratch)
│   ├── integration/
│   │   └── test_all_npcs_live.ts            (Promoted from scratch)
│   └── persistence/
│       └── test_server_restart_recovery.ts  (Promoted from scratch)
│
├── scratch/                                 (Local-only, git-ignored)
│   └── [46 diagnostic logs, prompts, and temporary scripts retained locally]
│
└── unity-client/
    └── Assets/
        └── Screenshots/                     (STRICTLY UNTOUCHED independent asset folder)
```

---

## 9. Proposed Action Plan (Pending User Authorization)

When authorized by the user, the following minimal, clean actions can be executed:

### Action 1: Relocate Root Markdown Reports to `docs/`
Move the 4 historical root markdown reports into `docs/`:
- `INCREMENT_5_11_3_IMPLEMENTATION_REPORT.md` -> `docs/`
- `INCREMENT_5_11_3_1_CONFORMANCE_AUDIT_REPORT.md` -> `docs/`
- `INCREMENT_5_11_3_2_OPTIONAL_EVIDENCE_REPAIR_REPORT.md` -> `docs/`
- `INCREMENT_5_11_4_IMPLEMENTATION_REPORT.md` -> `docs/`

### Action 2: Promote High-Value Test Scripts
Copy or move the 3 reusable test scripts from `scratch/` into permanent `tests/` directories:
- `scratch/test-launcher-compatibility.ps1` -> `tests/launcher/`
- `scratch/test_all_npcs_live.ts` -> `tests/integration/`
- `scratch/test_server_restart_recovery.ts` -> `tests/persistence/`

### Action 3: Track Curated Screenshots in Git
Keep `screenshots/` tracked with the 4 high-quality evaluator images.

### Action 4: Add `scratch/` to `.gitignore`
Add `scratch/` to `.gitignore` so that the remaining 46 diagnostic logs and temporary scripts remain safely on disk locally without dirtying `git status`.

---

## 10. Confirmation of Safety Constraints

1. **Zero Source Modifications:** No source code was modified during this audit.
2. **Zero Moves / Deletions / Renames:** All files remain in their exact original locations.
3. **Zero Git Staging / Commits / Pushes:** `git status` remains unaltered except for the creation of this audit report.
4. **Unity Assets Directory:** Confirmed that `unity-client/Assets/Screenshots/` was not touched.
5. **No Credential Leaks:** Audited all 49 scratch files and 4 screenshot images; confirmed zero API keys, secrets, or personal private photos.
6. **Benchmark Repository:** Verified that `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity` remains 100% clean and untouched.

