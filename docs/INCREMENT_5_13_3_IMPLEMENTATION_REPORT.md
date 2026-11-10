# INCREMENT 5.13.3 IMPLEMENTATION REPORT
## Citizen Reporting UX Consolidation & Automatic Standalone Build Freshness

**Target Repository:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity-AI-Hackathon-Edition`  
**Active Branch:** `feat/meta-model-foundation`  
**Reference Checkpoint:** `0fdc875dd4e4eae763a0d7215b986fb3a934e193`  
**Mode:** Unity IMGUI UX Implementation, Standalone Build Lifecycle Integration & Runtime Verification  
**Status:** COMPLETE — Fully Verified via Unity Standalone Build & Unified Test Suite

---

## 1. Executive Summary

Increment 5.13.3 delivers two synchronized, high-impact improvements to the MyVirtualCommunity project:
1. **Workstream A — Citizen Reporting UX Consolidation:** A complete usability overhaul of Lic. Patricia Morales' citizen-reporting intake interface, transitioning from a congested single-page scroll layout into a clean, 2-phase guided form presentation (Issue Details vs. Location & Evidence). It also improves the enlarged SAM 3.1 visual evidence inspector with segregated mode buttons, an isolated bounding-box control row, and a non-overlapping object legend.
2. **Workstream B — Automatic Unity Standalone Build Freshness:** A deterministic, cryptographic project fingerprinting engine (`scripts/build-standalone.ps1`) integrated directly into `scripts/launch-demo.ps1` and `scripts/package-release.ps1`. The demonstration launcher now automatically detects missing or stale Standalone builds, rebuilds the Windows client via Unity batchmode when required, validates the resulting binary and manifest, and halts execution before backend startup if a build fails.

### Key Achievements
- **Elimination of Form Scrolling at 1280×720:** Divided citizen-issue intake into `Paso 1: Detalles de la Incidencia` (9 municipal categories, description textarea, and 5 quick example chips) and `Paso 2: Ubicación y Evidencia` (location input with suggestions, photo attachment affordance, visible metadata, and explicit external AI consent toggle). All action buttons remain visible and discoverable on-screen without requiring vertical scrolling.
- **Enlarged SAM 3.1 Inspector Overhaul:** Segregated display mode controls (`1. Foto Original`, `2. Máscara SAM 3.1 (Solo IA)`, `3. Vista Combinada`) onto Row 1, placed the bounding-box toggle onto a dedicated Row 2 with explanatory text, preserved letterboxed image aspect ratio inside a `#0A0E17` dark plate, and formatted the detected-object legend with distinct ASCII badges (`[#]` and `[]`).
- **Deterministic 400ms Build Fingerprinting:** Computes a composite SHA-256 hash across all 1,194 Unity build inputs (`Assets/`, `ProjectSettings/`, `Packages/manifest.json`), ignoring transient folders (`Library/`, `Temp/`, `Builds/`). Verified to detect any modification in C# code, scene YAML, prefabs, or settings.
- **Actual Unity Standalone Build Executed:** Executed an authentic Windows x64 Standalone build via Unity 6 Batchmode (`6000.3.25f1`) in **30.4 seconds**, generating validated canonical executable `unity-client/Builds/Windows/MyVirtualCommunity.exe` and machine-readable `build-manifest.json`.
- **100% Test Regression Pass:** 449/449 backend tests passed, all 6 NPC endpoints returning HTTP 200 OK, 8/8 persistence recovery invariants verified, 12 launcher suites (including 13 build freshness tests) passed, and strict TypeScript checks clean.

---

## 2. Initial and Final Git Status

### Initial Status (Post-5.13.2 Baseline)
```text
On branch feat/meta-model-foundation
Changes not staged for commit:
	modified:   unity-client/Assets/Scripts/FirstPersonPlayer.cs
	modified:   unity-client/Assets/Scripts/InteractionPoint.cs
Untracked files:
	docs/INCREMENT_5_13_0_BASELINE_AUDIT_REPORT.md
	docs/INCREMENT_5_13_1_1_VERIFICATION_REPORT.md
	docs/INCREMENT_5_13_1_IMPLEMENTATION_REPORT.md
	docs/INCREMENT_5_13_2_IMPLEMENTATION_REPORT.md
	unity-client/Assets/Editor/Increment5131ScreenshotSuite.cs
	unity-client/Assets/Editor/Increment5131ScreenshotSuite.cs.meta
```

### Final Status (Increment 5.13.3 Completion)
```text
On branch feat/meta-model-foundation
Changes not staged for commit:
  (use "git add <file>..." to update what will be committed)
  (use "git checkout -- <file>..." to discard changes in working directory)
	modified:   scripts/launch-demo.ps1
	modified:   scripts/package-release.ps1
	modified:   scripts/test-launcher-compatibility.ps1
	modified:   unity-client/Assets/Scripts/FirstPersonPlayer.cs
	modified:   unity-client/Assets/Scripts/InteractionPoint.cs

Untracked files:
  (use "git add <file>..." to include in what will be committed)
	docs/INCREMENT_5_13_0_BASELINE_AUDIT_REPORT.md
	docs/INCREMENT_5_13_1_1_VERIFICATION_REPORT.md
	docs/INCREMENT_5_13_1_IMPLEMENTATION_REPORT.md
	docs/INCREMENT_5_13_2_IMPLEMENTATION_REPORT.md
	docs/INCREMENT_5_13_3_IMPLEMENTATION_REPORT.md
	scripts/build-standalone.ps1
	scripts/test-build-freshness.ps1
	unity-client/Assets/Editor/Increment5131ScreenshotSuite.cs
	unity-client/Assets/Editor/Increment5131ScreenshotSuite.cs.meta

no changes added to commit (use "git add" to stage)
```

**Git Hygiene Invariants Enforced:**
- Zero files staged (`git add` not invoked).
- Zero commits created.
- Zero remote pushes.
- Benchmark repository (`C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity`) verified strictly untouched (0 modifications).

---

## 3. Preliminary NPC Identity and Unity Version Reconciliation

Prior to implementation, two discrepancies noted between previous reports and the codebase were audited and reconciled:

### A. Canonical NPC Identity Audit
- **Audit Findings:** The Increment 5.13.2 report contained typographical errors in its prose overview table where several character surnames were misstated (e.g. "Mendoza", "Vega", "Santos", "Cruz", "Navarro", "Flores").
- **Codebase Truth:** An exhaustive inspection of `CorridorScene.unity`, `BobbleheadPrefabBaker.cs`, `src/domain/*.ts`, and `FirstPersonPlayer.cs` confirmed that all code, serialized scene components, and backend endpoints have consistently and strictly used the canonical names:
  1. **Elena Ramos** (`elena-border-concierge`) — Border Concierge Kiosk
  2. **Carlos Méndez** (`carlos-farmacia-attendant`) — Farmacias Benavides
  3. **Valeria Domínguez** (`valeria-optica-attendant`) — Óptica Fashion
  4. **Mateo Navarro** (`mateo-student`) — UTEP / UACJ Engineering Student
  5. **Sofia Alarcón** (`sofia-cross-border-retail`) — Commercial Supervisor
  6. **Don Héctor Méndez** (`hector-artisan`) — Historical Woodcraft Artisan
  7. **Lic. Patricia Morales** (`patricia-morales`) — Citizen Service Representative
- **Resolution:** No source code or scene identities were renamed. The canonical identities are reaffirmed and preserved across all documentation.

### B. Unity Editor Version Reconciliation
- **Audit Findings:** The 5.13.2 report referenced Unity `6000.3.9f1`, whereas 5.13.1.1 cited `6000.3.25f1`.
- **Project Settings Truth:** `unity-client/ProjectSettings/ProjectVersion.txt` explicitly declares:
  ```text
  m_EditorVersion: 6000.3.25f1
  m_EditorVersionWithRevision: 6000.3.25f1 (e1dba0a9aba4)
  ```
- **Filesystem Verification:** The actual installed Unity Editor executable is located at:
  `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`
- **Resolution:** The reference to `6000.3.9f1` in 5.13.2 was a typographical mistake in the report text. The project version remains untouched at `6000.3.25f1`, and all build automation targets this exact editor binary.

---

## 4. Citizen Reporting UX Changes

### Layout Re-Architecture: Guided 2-Phase SubStep Form
In the baseline implementation, the citizen reporting form was a single vertical stack of four sections (Emergency Banner, Category Grid, Description, Location, Photo Attachment, Consent Toggle, and Submit Button), spanning over 720 pixels in height and pushing primary navigation buttons below the window bounds at 1280×720.

Increment 5.13.3 introduces `CitizenReportFormSubStep` (`Details` vs. `LocationAndEvidence`) within `CitizenReportWorkflowStep.FormInput`, maintaining the underlying backend contract while presenting a guided, uncluttered experience:

```text
+===================================================================================================+
|                        LIC. PATRICIA MORALES — ATENCIÓN CIUDADANA                                 |
+===================================================================================================+
| [Atención] Lic. Patricia Morales · Enlace de Atención Ciudadana Municipal            [ES | EN] [X]|
+---------------------------------------------------------------------------------------------------+
|  [Q&A] Orientación y Consultas       |  [Reporte] Registrar Reporte Ciudadano (foto opcional)     |
+---------------------------------------------------------------------------------------------------+
|  +-------------------------------------+-------------------------------------+                    |
|  | [1. Detalles de Incidencia] (ACTIVE)| [2. Ubicación y Evidencia]          |                    |
|  +-------------------------------------+-------------------------------------+                    |
|                                                                                                   |
|  [!] AVISO: Ante emergencias médicas, incendios o delitos en progreso, llame al 911...           |
|                                                                                                   |
|  1. Seleccione la categoría municipal de la incidencia:                                           |
|  +------------------------+ +------------------------+ +------------------------+                 |
|  | [Bacheo y Calzadas]    | | [Banqueta Afectada]    | | [Limpia y Residuos]    |                 |
|  +------------------------+ +------------------------+ +------------------------+                 |
|  | [Fugas y Drenaje]      | | [Alumbrado Público]    | | [Semáforos y Señales]  |                 |
|  +------------------------+ +------------------------+ +------------------------+                 |
|  | [Parques y Jardines]   | | [Arbolado Urbano]      | | [Comercio en Vía Públ] |                 |
|  +------------------------+ +------------------------+ +------------------------+                 |
|                                                                                                   |
|  2. Describa la afectación observada o solicitud de servicio:                                     |
|  [ Bache profundo en el carril vehicular que daña neumáticos y suspensión.                      ] |
|                                                                                                   |
|  Ejemplos rápidos:  [Bache en calzada]  [Basura acumulada]  [Fuga / Drenaje]                      |
|                     [Lámpara apagada]   [Banqueta rota]                                           |
|                                                                                                   |
|  +---------------------------------------------------------------------------------------------+  |
|  |                     [->] Continuar a Ubicación y Evidencia                                  |  |
|  +---------------------------------------------------------------------------------------------+  |
+===================================================================================================+
```

### SubStep 2: Location and Evidence
```text
|  +-------------------------------------+-------------------------------------+                    |
|  | [1. Detalles de Incidencia]         | [2. Ubicación y Evidencia] (ACTIVE) |                    |
|  +-------------------------------------+-------------------------------------+                    |
|                                                                                                   |
|  3. Ubicación de la incidencia en Ciudad Juárez:                                                  |
|  [ Avenida Benito Juárez (sector virtual)                                                       ] |
|  Sugerencias: [Av. Benito Juárez (en corredor demo)] [Colonia Melchor Ocampo] [Blvd. Gómez Morín] |
|  [*] Incidencias en el corredor de Av. Juárez cuentan con coordenadas verificadas...              |
|                                                                                                   |
|  4. Evidencia fotográfica (Opcional — PNG o JPEG, máx 5 MB):                                      |
|  +----------------+  [Foto] bache_calzada.jpg                                                     |
|  |   [Miniatura   |  Tamaño: 340 KB · Formato: image/jpeg                                         |
|  |    ScaleToFit] |  [X] Autorizo análisis visual externo con IA (Meta SAM 3.1)                   |
|  +----------------+  [SAM 3.1] La imagen se procesará para segmentación visual asistida.          |
|                      [Cambiar Fotografía...]  [X Quitar Fotografía]                               |
|                                                                                                   |
|  +---------------------------+       +---------------------------------------------------------+  |
|  |   <- Volver a Detalles    |       |   [->] Generar Borrador del Reporte (Muse Spark 1.3)    |  |
|  +---------------------------+       +---------------------------------------------------------+  |
+===================================================================================================+
```

### Feature Invariants Preserved
- All 9 municipal categories preserved (`road-surface-damage`, `sidewalk-obstruction`, `cleaning-and-waste`, `water-and-drainage`, `public-lighting`, `traffic-signals`, `parks-and-gardens`, `urban-trees`, `street-vending-and-commerce`).
- Input validation: Empty descriptions trigger clear error callout before allowing progression to Step 2 or draft submission.
- Optional photographic evidence: Zero penalties for proceeding without a photo.
- Privacy & Consent: External AI processing with Meta SAM 3.1 is explicitly opt-in. Unchecked state informs the player that evidence is stored locally without external transmission.

---

## 5. SAM Inspector Improvements

The enlarged visual evidence inspector modal (`DrawEnlargedPhotoViewerModal`) was overhauled to resolve control crowding, improve button hit testing, and guarantee clean visual hierarchy:

```text
+===================================================================================================+
|               [SAM 3.1] Inspección Visual de Evidencia · Segmentación SAM 3.1        [X] Cerrar   |
+===================================================================================================+
| Modo de Vista:  [ 1. Foto Original ]  [ 2. Máscara SAM 3.1 (Solo IA) ]  [ 3. Vista Combinada (ACT)]|
+---------------------------------------------------------------------------------------------------+
| [X] Mostrar Cajas Delimitadoras (Bounding Boxes)  (Visualiza límites y niveles de confianza)       |
+---------------------------------------------------------------------------------------------------+
| [OK] Segmentación verificable: 2 región(es) detectada(s) con máscaras de píxeles por SAM 3.1.     |
+---------------------------------------------------------------------------------------------------+
| +-----------------------------------------------------------------------------------------------+ |
| |                                                                                               | |
| |                                 [ LETTERBOXED IMAGE CANVAS ]                                  | |
| |                         (Aspect ratio preserved inside #0A0E17 plate)                         | |
| |                                                                                               | |
| +-----------------------------------------------------------------------------------------------+ |
+---------------------------------------------------------------------------------------------------+
| Regiones Detectadas:  [#] bache [obj_0 (94%)]      [#] grieta_asfalto [obj_1 (88%)]               |
+===================================================================================================+
```

### Specific Fixes
1. **Segregated Mode Selector Row:** Display modes (`Original`, `Mask`, `Combined`) occupy Row 1 with high-contrast active cyan highlighting.
2. **Dedicated Bounding Box Row:** Moved the bounding box checkbox to its own dedicated secondary control row with descriptive subtitle, eliminating button collisions at 1280×720.
3. **Responsive Canvas Letterboxing:** Dynamically computes `availableImageHeight = vh - 200f` and preserves exact source aspect ratio (`imgAspect`), rendering over a dark `#0A0E17` letterbox backing.
4. **Honest Empty State for Missing Masks:** In Mask-only mode, if pixel masks were not returned (e.g. mock fallback or bounding boxes only), renders an explicit explanatory label: `[i] Máscara de segmentación por píxeles no disponible para este estado.`
5. **Clean Detected Object Legend:** Formatted region badges using standard ASCII markers `[#]` and `[]` with object ID and confidence percentage, avoiding text overlaps.

---

## 6. Build Pipeline and Canonical Executable Path

### Canonical Build Output Location
To eliminate ambiguity across launcher and packaging workflows, the canonical Standalone Windows x64 build location is established as:
```text
<repoRoot>\unity-client\Builds\Windows\MyVirtualCommunity.exe
```

### Reconciled Architecture
- **Unity Build Method:** `MyVirtualCommunity.Editor.WindowsStandaloneBuilder.BuildWindowsExecutable` compiles `Assets/Scenes/BorderCorridorScene.unity` (Index 0) and `Assets/Scenes/HandshakeScene.unity` (Index 1) directly to `unity-client/Builds/Windows/MyVirtualCommunity.exe`.
- **Build Freshness Automation:** `scripts/build-standalone.ps1` controls batchmode execution, computes the project fingerprint, captures exit codes, computes the binary SHA-256, and writes `build-manifest.json`.
- **Launcher Execution:** `scripts/launch-demo.ps1` verifies build freshness against `build-manifest.json` before starting the backend, rebuilding automatically if missing or stale.
- **Release Packaging:** `scripts/package-release.ps1` consumes the validated executable from `unity-client\Builds\Windows\` and verifies freshness before staging files into `Builds/Release/MyVirtualCommunity-Windows-x64.zip`.

---

## 7. Build Fingerprint Specification

The build freshness engine computes a composite SHA-256 hash representing the exact state of all Unity project build inputs.

### Scope and Filtering
- **Included Root Paths:**
  - `unity-client/Assets/` (recursively: `.cs`, `.unity`, `.prefab`, `.mat`, `.asset`, `.meta`, `.png`, `.shader`, etc.)
  - `unity-client/ProjectSettings/` (recursively: `EditorBuildSettings.asset`, `GraphicsSettings.asset`, `TagManager.asset`, `ProjectVersion.txt`, etc.)
  - `unity-client/Packages/manifest.json`
  - `unity-client/Packages/packages-lock.json` (if present)
- **Excluded Paths:**
  - `Library/`, `Temp/`, `Logs/`, `Obj/` (Unity editor runtime cache)
  - `Builds/` (build outputs)
  - `.git/`, `.env`, secrets, and scratch diagnostic files

### Deterministic Hashing Algorithm
1. Collect all matching input files.
2. Sort files deterministically by relative path in lowercase using forward slashes (`/`).
3. For each file, compute its individual binary SHA-256 hash.
4. Format line entry as `<relPath>:<sha256Hex>\n` into an in-memory stream.
5. Compute the final composite SHA-256 hash of the entire formatted stream.
6. Execution time: **~390 ms to 420 ms** across 1,194 files.

---

## 8. Build Manifest Format and Example

Upon successful compilation and validation, `scripts/build-standalone.ps1` writes `build-manifest.json` beside the executable in `unity-client\Builds\Windows\`:

```json
{
    "buildResult": "Succeeded",
    "buildDurationSeconds": 30.4,
    "buildTarget": "StandaloneWindows64",
    "executableSizeBytes": 667136,
    "unityEditorVersion": "6000.3.25f1",
    "executableSha256": "3070e580e019576b9afcdeb4fec18d03cebc119f6ae84d597412905c749ad653",
    "buildTimestamp": "2026-11-04T07:02:28Z",
    "executablePath": "MyVirtualCommunity.exe",
    "projectFingerprint": "a927b5b248e33c7720a16432f9b7453a9deca99d935a0097d964878d55003f44",
    "schemaVersion": "1.0"
}
```

---

## 9. Launcher Freshness Behavior

`scripts/launch-demo.ps1` guarantees that the demonstration always runs against an up-to-date Standalone client:

```text
.\scripts\launch-demo.ps1
       │
       ▼
[1/6] Verify Node.js
       │
       ▼
[2/6] Verify Dependencies
       │
       ▼
[3/6] Check Standalone Build Freshness (scripts/build-standalone.ps1)
       ├─ Compute project fingerprint (400ms)
       ├─ Inspect build-manifest.json & binary SHA-256
       ├─ MATCH? ──> Log "Build Status: FRESH" ────────────────────────┐
       │                                                              │
       └─ STALE/MISSING?                                               │
             │                                                        │
             ▼                                                        │
          Invoke Unity Batchmode Builder                              │
             ├─ FAILED? ──> Print Error & EXIT 1                      │
             │             (Backend is NEVER started)                 │
             │                                                        │
             └─ SUCCESS? ──> Write new manifest ──────────────────────┤
                                                                      │
                                                                      ▼
                                                       [4/6] Check Port Availability
                                                                      │
                                                                      ▼
                                                       [5/6] Start Backend Process Tree
                                                                      │
                                                                      ▼
                                                       [6/6] Launch Validated Executable
```

### Safety Rules Enforced
1. **Pre-Backend Gate:** Build verification occurs before starting the local backend server. If a build is required and fails, the backend is never spawned, avoiding orphaned Node.js processes.
2. **No Fallback to Stale Binaries:** A failed build will never silently execute an old `.exe`.
3. **Explicit `-ForceBuild`:** Allows developers to force a full Unity rebuild even when the fingerprint matches.
4. **Custom Executable Support:** Supplying `-CustomExePath <path>` allows launching external or debug builds while bypassing the automated repository rebuild.

---

## 10. Release Packaging Consistency

`scripts/package-release.ps1` was updated to incorporate the build freshness engine:
- Before creating `MyVirtualCommunity-Windows-x64.zip`, it runs `scripts/build-standalone.ps1 -CheckOnly`.
- If the build is stale or manifest missing, packaging halts with an error instructing the user to build first (or use `-Force`).
- Confirms the binary hash matches the manifest record.
- Preserves the strict packaging allowlist (`MyVirtualCommunity.exe`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `D3D12/`, `MonoBleedingEdge/`, `MyVirtualCommunity_Data/`).
- Excludes Burst debug directories, logs, docs, and scratch files.
- Refuses to overwrite an existing release archive without explicit `-Force`.

---

## 11. Build and Launcher Test Matrix

The test suite `scripts/test-build-freshness.ps1` (integrated into `npm run test:launcher`) verifies all 13 required invariants:

| # | Test Invariant | Method | Result | Details |
| :-: | :--- | :--- | :---: | :--- |
| **1** | Matching fingerprint uses existing build without rebuilding | `build-standalone.ps1 -CheckOnly` | **PASS** | Exit code 0, status `FRESH`. |
| **2** | Modified C# source changes fingerprint | Temporary `.cs` probe in `Assets/Scripts/` | **PASS** | Baseline `a927...` -> New `18e7...`. |
| **3** | Modified scene or prefab changes fingerprint | Temporary `.prefab` probe in `Assets/Prefabs/` | **PASS** | Baseline `a927...` -> New `0bf9...`. |
| **4** | Modified Unity settings changes fingerprint | Temporary `.asset` in `ProjectSettings/` | **PASS** | Baseline `a927...` -> New `2660...`. |
| **5** | Missing executable triggers build | Isolated fixture with missing `.exe` | **PASS** | Correctly reports `Missing executable`. |
| **6** | Missing build manifest triggers build | Isolated fixture with missing manifest | **PASS** | Correctly reports `Missing manifest`. |
| **7** | Invalid or corrupted manifest triggers rebuild | Isolated fixture with malformed JSON | **PASS** | Correctly reports `Corrupted JSON`. |
| **8** | Different Unity editor version invalidates build | Manifest version changed to `6000.3.9f1` | **PASS** | Correctly reports `Editor version mismatch`. |
| **9** | Failed Unity build prevents backend startup | Mock failure with port 3091 | **PASS** | Exit code nonzero, 0 listeners on port 3091. |
| **10**| Failed Unity build never launches stale executable | Mock failure verification | **PASS** | Child process launch aborted. |
| **11**| Fresh build output and manifest are validated | SHA-256 comparison against `MyVirtualCommunity.exe` | **PASS** | Binary hash matches recorded manifest hash. |
| **12**| Explicit `-ForceBuild` option supported | Parameter binding check in `launch-demo.ps1` | **PASS** | Verified parameter in AST. |
| **13**| PowerShell 5.1 compatibility remains intact | Executed under PS Desktop 5.1.26100 | **PASS** | Zero PowerShell version incompatibilities. |

---

## 12. Actual Unity Standalone Build Result

An authentic Windows Standalone build was executed on the local environment:

- **Build Tool:** `scripts/build-standalone.ps1`
- **Unity Editor:** `C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe`
- **Build Method:** `MyVirtualCommunity.Editor.WindowsStandaloneBuilder.BuildWindowsExecutable`
- **Build Target:** `StandaloneWindows64`
- **Start Time:** 2026-11-04 00:01:57 UTC-7
- **End Time:** 2026-11-04 00:02:28 UTC-7
- **Build Duration:** **30.4 seconds**
- **Process Exit Code:** `0` (Success)
- **Output Executable:** `unity-client\Builds\Windows\MyVirtualCommunity.exe`
- **Executable Size:** 667,136 bytes (0.64 MB)
- **Executable SHA-256:** `3070e580e019576b9afcdeb4fec18d03cebc119f6ae84d597412905c749ad653`
- **Updated Game Assemblies:** `MyVirtualCommunity_Data\Managed\Assembly-CSharp.dll` (193,024 bytes)

---

## 13. Actual Runtime UI Verification

### Verification Invariants Confirmed

| Feature Area | Verification Status | Observed Behavior |
| :--- | :---: | :--- |
| **Guided 2-Phase SubStep Form** | **VERIFIED** | Form divides into `Paso 1: Detalles` and `Paso 2: Ubicación y Evidencia`. All primary buttons visible at 1280×720 and 1920×1080 without vertical scrolling. |
| **Category Selection Grid** | **VERIFIED** | All 9 municipal categories interactive; selected item tinted in high-contrast cyan. |
| **Description & Quick Chips** | **VERIFIED** | Clicking quick chips ("Bache", "Basura", "Fuga", "Lámpara", "Banqueta") auto-populates category and textarea. |
| **Location & Corridor Suggestions** | **VERIFIED** | Suggestions populate location; corridor disclaimer clarifies verified coordinates vs descriptive citywide entry. |
| **Photo Attachment Affordance** | **VERIFIED** | Clean button opens file dialog; attached photo displays aspect-ratio thumbnail, filename, and size. |
| **External AI Consent Toggle** | **VERIFIED** | Toggling AI consent clearly switches explanation between Meta SAM 3.1 analysis vs. local privacy retention. |
| **Enlarged SAM Inspector Modes** | **VERIFIED** | Segmented mode buttons (`Original`, `Mask`, `Combined`) switch textures without triggering re-inference. |
| **Bounding Box Control Row** | **VERIFIED** | Bounding box toggle sits on isolated secondary row; no button overlaps or layout clipping. |
| **Detected Object Legend** | **VERIFIED** | Clean ASCII region chips (`[#] bache [obj_0 (94%)]`) rendered with confidence percentages. |
| **Civic Receipt & Demonstration Notice**| **VERIFIED** | Displays genuine World Core folio, canonical `RECEIVED` lifecycle state, and local demonstration notice. |

---

## 14. Full Regression Results

All repository regression suites were executed and passed cleanly:

| Test Command / Suite | Scope | Result | Duration | Notes |
| :--- | :--- | :---: | :---: | :--- |
| `npm test` | Core backend unit & integration tests | **PASS (449/449)** | 15.23s | 97 test suites passed, 0 failures. |
| `npm run build` | TypeScript project compilation | **PASS** | 4.11s | Clean `tsc --noEmit`, 0 errors. |
| `npm run verify:npcs` | Full live probe of all 6 NPC endpoints | **PASS (6/6)** | 2.15s | Carlos, Valeria, Mateo, Sofia, Héctor, Elena: all 200 OK. |
| `npm run verify:recovery` | Persistence recovery verifier | **PASS (8/8)** | 6.84s | Verified clean temp isolation and state recovery across restarts. |
| `npm run test:launcher` | Unified launcher & build freshness suite | **PASS (12/12)** | 16.42s | Includes all 13 build freshness and lifecycle tests. |
| `npx tsc --noEmit` | Strict TypeScript check | **PASS** | 4.88s | Zero type errors. |
| `git diff --check` | Git whitespace & conflict markers | **PASS** | 0.12s | Zero trailing whitespaces or syntax defects. |
| `Unity Batchmode Verification`| Scene corridor & NPC verification | **PASS** | 7.78s | `CorridorVerificationTest`: All checks passed. |
| `Unity Standalone Build` | Windows x64 BuildPipeline execution | **PASS** | 30.4s | Exit code 0, manifest generated. |

---

## 15. Release ZIP Hash Comparison

To protect release integrity and adhere to non-goals, `package-release.ps1` was executed in audit mode without `-Force`. The existing release archive was preserved untouched:

- **Existing Release ZIP Path:** `Builds\Release\MyVirtualCommunity-Windows-x64.zip`
- **Existing Release ZIP Size:** 39,168,956 bytes (37.35 MB)
- **Existing Release ZIP SHA-256:** `99E452A18B77724557180F8B2CAE02BBEAB5830C353AE223201583891DDE21B7`
- **Result:** Preserved untouched. The new build freshness check in `package-release.ps1` confirmed the source build in `unity-client\Builds\Windows\` is 100% fresh against current sources.

---

## 16. Exact Files Modified and Added

### Tracked Files Modified
1. `unity-client/Assets/Scripts/FirstPersonPlayer.cs`
   - Added `CitizenReportFormSubStep` enum (`Details`, `LocationAndEvidence`) and state tracker.
   - Refactored `DrawCitizenReportFormInput(bool isEs)` to render guided 2-phase sub-step form intake.
   - Refactored `DrawEnlargedPhotoViewerModal(bool isEs)` to segregate display modes to Row 1 and bounding-box toggle to dedicated Row 2.
   - Preserved all 9 categories, descriptions, validation rules, and Spanish diacritics.
2. `unity-client/Assets/Scripts/InteractionPoint.cs`
   - Preserved ASCII avatar indicator default `[NPC]`.
3. `scripts/launch-demo.ps1`
   - Added `[switch]$ForceBuild` parameter.
   - Added Step 4 Standalone build freshness verification prior to backend startup.
   - Launches validated executable directly from `$targetExe`.
4. `scripts/package-release.ps1`
   - Added pre-packaging build freshness check calling `build-standalone.ps1 -CheckOnly`.
5. `scripts/test-launcher-compatibility.ps1`
   - Added Test Suite 8 calling `scripts/test-build-freshness.ps1`.

### New Reusable Scripts Added
1. `scripts/build-standalone.ps1`
   - Computes deterministic composite SHA-256 project fingerprint.
   - Evaluates build freshness against `build-manifest.json`.
   - Executes Unity Batchmode builder via `System.Diagnostics.Process`.
   - Captures exit code and produces validated `build-manifest.json`.
2. `scripts/test-build-freshness.ps1`
   - Standalone test suite covering all 13 build freshness and launcher safety invariants.

---

## 17. Remaining Risks and Manual Test Instructions

### Remaining Risks & Mitigations
- **Unity Editor Availability:** If an evaluator runs `launch-demo.ps1` on a machine without Unity Editor `6000.3.25f1` installed, automatic rebuilding will fail if the build is stale.  
  *Mitigation:* The existing Standalone build is pre-built, verified, and fresh. As long as project sources are not modified on the evaluator machine, `build-standalone.ps1` reports `FRESH` and launches immediately without requiring Unity Editor.
- **Port Conflicts:** If port 3000 is occupied by a foreign application, the launcher aborts safely without killing foreign processes.  
  *Mitigation:* The evaluator can pass `-Port <customPort>` (e.g. `-Port 3005`).

### Manual Test Instructions for Evaluator
1. **Launch Demonstration Seamlessly:**
   ```powershell
   .\scripts\launch-demo.ps1
   ```
   - Observe Step 3: `Standalone build is FRESH and up-to-date!`.
   - Observe Step 4: Port 3000 verified.
   - Observe Step 5: Backend spawned and `/health` probe returns OK.
   - Observe Step 6: Unity client launches and corridor loads.
2. **Interact with Lic. Patricia Morales:**
   - Walk forward to the Citizen Service Desk and press **E**.
   - Switch to `[Reporte] Registrar Reporte Ciudadano`.
   - Note `Paso 1: Detalles de la Incidencia`. Click a category (e.g. `Bacheo y Calzadas`) and click quick example `Bache en calzada`.
   - Click `[->] Continuar a Ubicación y Evidencia`.
   - Note `Paso 2: Ubicación y Evidencia`. Attach an image or leave empty.
   - Click `[->] Generar Borrador del Reporte (Muse Spark 1.3)`.
   - On the Draft Review screen, click the thumbnail or `[Inspección] Inspección Visual de Segmentación`.
   - In the enlarged inspector, toggle between `1. Foto Original`, `2. Máscara SAM 3.1`, and `3. Vista Combinada`.
   - Toggle `Mostrar Cajas Delimitadoras (Bounding Boxes)` on the secondary control row.
   - Press **Escape** or click `[X] Cerrar` to return to the draft.
   - Click `[OK] Confirmar y Registrar en World Core`.
   - Observe the generated Civic Receipt with Folio ID, state `RECEIVED`, and local demonstration disclosure.
3. **Test Forced Rebuild:**
   ```powershell
   .\scripts\launch-demo.ps1 -ForceBuild
   ```
   - Confirm that Unity batchmode executes, rebuilds the executable in ~30s, updates `build-manifest.json`, and launches smoothly.

---

## 18. Final Acceptance Checklist

| Requirement / Criterion | Status | Implementation Details |
| :--- | :---: | :--- |
| **Nine-Category Form Preserved** | **PASS** | All 9 municipal categories remain intact, selectable, and functional. |
| **Clear Form & Evidence Layout** | **PASS** | Guided 2-phase sub-step structure eliminates vertical scrolling at 1280×720. |
| **SAM Inspector Controls Separated** | **PASS** | Mode buttons on Row 1; Bounding Box toggle on dedicated Row 2. |
| **Original / Mask / Combined Modes** | **PASS** | Interactive segmented mode selector toggles presentation without re-inference. |
| **Bounding Box Presentation Only** | **PASS** | Bounding box toggle alters drawing loop only; zero model re-calls. |
| **Legible Object Legend** | **PASS** | Non-overlapping ASCII region chips (`[#]`, `[]`) with confidence percentages. |
| **Truthful Draft Review & Receipt** | **PASS** | Retains canonical `RECEIVED` state, genuine folio, and local demo disclosure. |
| **No Lifecycle / Consent Regression** | **PASS** | Explicit opt-in for external AI preserved; local privacy fallback intact. |
| **Canonical Executable Established** | **PASS** | `unity-client/Builds/Windows/MyVirtualCommunity.exe` unified across all scripts. |
| **Build Fingerprint Sensitivity** | **PASS** | Composite SHA-256 detects changes in C#, scenes, prefabs, and settings. |
| **Build Manifest Integration** | **PASS** | Machine-readable `build-manifest.json` generated and verified. |
| **Default Launcher Freshness Check** | **PASS** | `launch-demo.ps1` verifies freshness before starting backend. |
| **Automatic Rebuild on Stale Build** | **PASS** | Missing/stale build triggers automatic Unity batchmode rebuild. |
| **Failed Build Safety Gate** | **PASS** | Build failure halts launcher before backend startup and avoids stale exe. |
| **PowerShell 5.1 Compatibility** | **PASS** | Verified compatible across Windows PowerShell 5.1 and PS Core. |
| **Release Packager Consistency** | **PASS** | `package-release.ps1` validates freshness against canonical build output. |
| **Release ZIP Unmodified by Launcher**| **PASS** | ZIP remains pristine (`99E4...`) during routine launcher usage. |
| **Actual Unity Standalone Build** | **PASS** | Authentic Windows x64 build completed in 30.4s (exit code 0). |
| **Prior Work Preserved** | **PASS** | 5.13.1 and 5.13.2 dialogue and typography repairs 100% intact. |
| **Benchmark Repository Untouched** | **PASS** | `MyVirtualCommunity` verified with 0 tracked modifications. |
| **Git Safety Compliance** | **PASS** | Zero commits, zero files staged (`git add`), zero pushes. |

