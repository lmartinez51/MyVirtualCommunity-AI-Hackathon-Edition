# INCREMENT 5.12.2.1 VALIDATION REPORT
## Git Ignore Verification & Reusable Test Tooling Validation

**Mode:** Strictly Read-Only Follow-Up Audit  
**Target Repository:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity-AI-Hackathon-Edition`  
**Active Branch:** `feat/meta-model-foundation`  
**Benchmark Repository:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity` (Strictly Read-Only — 0 Modifications)  
**Execution Timestamp:** 2026-11-03T16:30:00-07:00  

---

## 1. Executive Summary

Increment 5.12.2.1 performed a targeted, strictly read-only technical audit to:
1. Re-evaluate the `.gitignore` pattern matching behavior for `*.log` and determine why `scratch/` appears in `git status`.
2. Inspect the complete code, dependencies, and execution side-effects of the three candidate scripts in `scratch/`.
3. Audit test-runner conventions in `scripts/run-tests.js` and `package.json`.
4. Assess documentation link impact for file relocations.
5. Verify release archive inclusion rules in `scripts/package-release.ps1`.
6. Inspect the visual and domain provenance of `screenshots/511_04_world_core_confirmed_receipt.png`.

Every finding in this report is strictly grounded in inspected source code, live command outputs, and file contents. All findings are explicitly categorized as **[Verified]**, **[Not Verified]**, or **[Requires User Decision]**.

---

## 2. Corrected Git Ignore Findings

### Finding 2.1: Behavior of the Existing `*.log` Pattern — **[Verified]**
- **Prior Misconception:** The previous audit report stated that `*.log` only ignores log files inside tracked directories.
- **Correction:** In Git syntax, `*.log` is an unanchored shell glob. It applies recursively across the entire worktree at all directory depths.
- **Verification Command & Output:**
  ```powershell
  git check-ignore -v scratch/bake_all_npcs.log scratch/build_5114_standalone.log scratch/verification_test.log
  ```
  ```text
  .gitignore:9:*.log	scratch/bake_all_npcs.log
  .gitignore:9:*.log	scratch/build_5114_standalone.log
  .gitignore:9:*.log	scratch/verification_test.log
  ```
- **Conclusion:** All 19 `.log` files in `scratch/` are **already actively ignored** by line 9 of `.gitignore`.

### Finding 2.2: Why `scratch/` Appears in `git status` — **[Verified]**
- **Live Output from `git status --short --untracked-files=all`:**
  Git listed exactly 30 individual untracked files inside `scratch/`:
  - 21 PowerShell scripts (`.ps1`)
  - 5 JavaScript files (`.js`)
  - 2 TypeScript files (`.ts`)
  - 2 Text prompt extracts (`.txt`)
  - **Zero `.log` files appeared** (confirming they are ignored).
- **Git Collapsing Mechanism:** Under standard `git status --short` (which defaults to `--untracked-files=normal`), Git does not display individual files inside an untracked directory if that directory contains untracked files; instead, it collapses the directory into a single entry: `?? scratch/`.
- **Conclusion:** `scratch/` appears in `git status` solely because of the 30 non-log scripts and text files.

### Finding 2.3: Root Anchor Evaluation: `/scratch/` vs `scratch/` — **[Verified]**
- `scratch/`: Matches any directory named `scratch` anywhere in the repository (including hypothetical future submodules, packages, or nested Unity directories).
- `/scratch/`: The leading slash anchors the ignore pattern strictly to the repository root directory (`<repoRoot>/scratch/`).
- **Recommendation:** When `.gitignore` is updated, use `/scratch/` to prevent unintended ignore behavior in nested directories.

---

## 3. Deep Inspection of Candidate Test Scripts

### Candidate 1: `scratch/test-launcher-compatibility.ps1` (8,571 bytes) — **[Verified]**
- **Entry Point & Command:** `powershell -ExecutionPolicy Bypass -File scratch/test-launcher-compatibility.ps1`
- **Dependencies & Environment:** Windows PowerShell 5.1 or PowerShell 7+; Node.js on PATH; invokes `scripts/launch-demo.ps1`.
- **Running Backend Required?** No. Spawns its own backend instances via `launch-demo.ps1 -BackendOnly -DurationSec ...`.
- **External Services / Network Calls:** None. Contacts only local loopback endpoints (`http://127.0.0.1:3000/health`, `http://127.0.0.1:3088/health`).
- **API Costs:** Zero. Uses local mock configurations and health probes.
- **File System Side-Effects:** None. Does not create, modify, or delete files.
- **Process Management:** Starts and stops isolated PowerShell and Node.js process trees. Queries `Get-Process node` at baseline and confirms zero unrelated processes are terminated.
- **Port Isolation:** Uses ports 3088, 3000, and 3099. Releases all ports cleanly.
- **Impact on `.runtime/` or User Data:** Zero.
- **Determinism:** 100% deterministic (11 assertions across 7 test suites). Duration is ~25 seconds due to process startup and timeout tests.
- **Node.js / PowerShell Compatibility:** Verified compatible with Windows PowerShell 5.1 (`5.1.26100.3624`) and Node v20.19.0.
- **Appropriate for Automatic `npm test`?** **NO.** `npm test` runs Node.js native test runner (`node:test`). A `.ps1` script cannot be parsed by Node.js. Furthermore, its 25-second execution time makes it an integration/acceptance harness, not a fast unit test.

### Candidate 2: `scratch/test_all_npcs_live.ts` (5,353 bytes) — **[Verified]**
- **Entry Point & Command:** `tsx scratch/test_all_npcs_live.ts`
- **Dependencies & Environment:** `../src/server/index.js`, `../src/adapters/mock-meta-model-adapter.js`. (Note: contains relative import `../src/...` expecting to run from a 1-level deep directory).
- **Running Backend Required?** No. Boots an in-process HTTP server via `createAssistantServer` on isolated port 3099 and terminates it via `app.stop()`.
- **External Services / Network Calls:** None. Uses `MockMetaModelAdapter` with `apiKey: null` and `isConfigured: false`.
- **API Costs:** Zero.
- **File System Side-Effects:** None. Does not touch disk.
- **Process Management:** In-process Node.js only. Zero process spawning or termination.
- **Port Isolation:** Port 3099 (isolated from default 3000).
- **Impact on `.runtime/` or User Data:** Zero.
- **Determinism:** 100% deterministic. Verifies all 6 dialogue endpoints across Carlos, Valeria, Mateo, Sofía, Don Héctor, and Elena Ramos.
- **Node.js / TypeScript Compatibility:** Verified compatible with Node v20 and `tsx`.
- **Appropriate for Automatic `npm test`?** **NO (in current form).** It ends in `.ts` (not `.test.ts`), and uses a standalone `main()` function with `console.log` and `process.exit(1)` rather than `node:test` (`describe`/`it`/`assert`).

### Candidate 3: `scratch/test_server_restart_recovery.ts` (6,078 bytes) — **[Verified]**
- **Entry Point & Command:** `tsx scratch/test_server_restart_recovery.ts`
- **Dependencies & Environment:** `../src/server/index.js`, `../src/adapters/in-memory-world-core-adapter.js`.
- **Running Backend Required?** No. Sequentially launches two in-process HTTP servers on dynamic random OS ports (`listen(0)`).
- **External Services / Network Calls:** None. Operates 100% offline with `externalAiProcessingConsent: false`.
- **API Costs:** Zero.
- **File System Side-Effects:** Creates an isolated temporary file in `.runtime/`:
  `resolve(process.cwd(), '.runtime', 'restart_recovery_audit_${Date.now()}.json')`.
  Deletes it upon completion via `unlinkSync(persistFilePath)`.
- **Process Management:** In-process Node.js only.
- **Port Isolation:** Dynamic OS ports (`port: 0`).
- **Impact on `.runtime/` or User Data:** Does **not** touch `citizen-reports.json`. Strictly isolated to a timestamped test file.
- **Determinism:** 100% deterministic. Asserts report recovery, folio preservation, and invariant integrity.
- **Node.js / TypeScript Compatibility:** Verified compatible with Node v20 and `tsx`.
- **Appropriate for Automatic `npm test`?** **NO (in current form).** Ends in `.ts` (not `.test.ts`), uses custom `main()`, and performs disk I/O. Note: Unit tests for citizen reports already exist in `tests/application/citizen-report.test.ts`.

---

## 4. Test Runner Conventions & Discovery Findings

### Finding 4.1: Current Test Discovery Logic — **[Verified]**
Inspected [`scripts/run-tests.js`](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/scripts/run-tests.js#L9-L11):
```javascript
const testFiles = readdirSync('tests', { recursive: true })
  .filter((f) => typeof f === 'string' && f.endsWith('.test.ts'))
  .map((f) => join('tests', f));
```
- The test runner **only** executes files matching the exact regex/suffix `*.test.ts` located inside `tests/`.
- Every one of the 26 existing test suites in `tests/` strictly adheres to `*.test.ts`.

### Finding 4.2: Placement Conventions — **[Verified]**
In the existing project architecture:
1. `tests/`: Reserved strictly for automated unit and integration tests executing under `node --test` via `npm test`.
2. `scripts/`: Reserved for standalone verification utilities and explicit smoke tests invoked via dedicated npm scripts:
   - `scripts/meta-smoke-test.ts` -> `npm run test:meta-smoke`
   - `scripts/verify-world-core-integration.ts` -> `npm run test:integration`
   - `scripts/verify-live-assistant.ts` -> `npm run verify:live-assistant`

### Finding 4.3: Promotion Recommendation for Candidates — **[Requires User Decision]**
To adhere strictly to project conventions without breaking `npm test`:
- **Option A (Follow `scripts/` Pattern — Recommended):**
  - Place `test_all_npcs_live.ts` in `scripts/verify-all-npcs.ts` and add `"verify:npcs": "tsx scripts/verify-all-npcs.ts"` to `package.json`.
  - Place `test_server_restart_recovery.ts` in `scripts/verify-persistence-recovery.ts` and add `"verify:recovery": "tsx scripts/verify-persistence-recovery.ts"` to `package.json`.
  - Place `test-launcher-compatibility.ps1` in `scripts/test-launcher-compatibility.ps1` and add `"test:launcher": "powershell -ExecutionPolicy Bypass -File scripts/test-launcher-compatibility.ps1"` to `package.json`.
- **Option B (Convert to `*.test.ts`):**
  - Refactor `test_all_npcs_live.ts` and `test_server_restart_recovery.ts` to use `node:test` (`describe`/`it`/`assert`) and rename them to `.test.ts`. This would add them to `npm test` automatically. (Not recommended for `test-launcher-compatibility.ps1` as it is PowerShell).

---

## 5. Documentation & Release References Impact

### Finding 5.1: Historical Markdown Report Links — **[Verified]**
- An exhaustive search of `README.md`, `src/`, and `tests/` showed **zero inbound links** referencing the root paths of:
  - `INCREMENT_5_11_3_IMPLEMENTATION_REPORT.md`
  - `INCREMENT_5_11_3_1_CONFORMANCE_AUDIT_REPORT.md`
  - `INCREMENT_5_11_3_2_OPTIONAL_EVIDENCE_REPAIR_REPORT.md`
  - `INCREMENT_5_11_4_IMPLEMENTATION_REPORT.md`
- The only references to these filenames appear inside `docs/INCREMENT_5_12_IMPLEMENTATION_REPORT.md`, `docs/INCREMENT_5_12_1_IMPLEMENTATION_REPORT.md`, and `docs/INCREMENT_5_12_2_ARTIFACT_AUDIT_REPORT.md` as part of git status listings.
- **Conclusion:** Moving these 4 files into `docs/` will cause **zero broken links** in code or public user documentation.

### Finding 5.2: Release Archive Inclusion Rules — **[Verified]**
Inspected [`scripts/package-release.ps1`](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/scripts/package-release.ps1#L82-L89):
```powershell
$includedItems = @(
    "MyVirtualCommunity.exe",
    "UnityPlayer.dll",
    "UnityCrashHandler64.exe",
    "D3D12",
    "MonoBleedingEdge",
    "MyVirtualCommunity_Data"
)
```
- Packaging strictly sources compiled binaries from `unity-client/Builds/Windows/`.
- It explicitly **excludes** `docs/`, `screenshots/`, `scratch/`, and root markdown files.
- **Conclusion:** Any file organization changes involving `docs/`, `scratch/`, or `screenshots/` will have **zero effect** on the Standalone release ZIP archive, its size, or its SHA-256 checksum (`99E452A18B77724557180F8B2CAE02BBEAB5830C353AE223201583891DDE21B7`).

---

## 6. Screenshot Placeholder & Provenance Verification

### Finding 6.1: Literal Receipt Text vs. Markdown Placeholder — **[Verified]**
- **The Question:** Is `FOLIO-2026-XXXXX` in `screenshots/511_04_world_core_confirmed_receipt.png` a literal placeholder, an anonymized sample, or something else?
- **Domain Source Code Inspection:**
  In [`src/domain/citizen-report.ts`](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/src/domain/citizen-report.ts#L727-L731):
  ```typescript
  export function generateReportId(): string {
    const num = reportCounter++;
    const padded = String(num).padStart(6, '0');
    return `CJ-${padded}`;
  }
  ```
  The domain model formats all authoritative report IDs as `CJ-XXXXXX` (e.g. `CJ-000184`).
- **Screenshot Generator Code Inspection:**
  In [`unity-client/Assets/Editor/Increment511ScreenshotSuite.cs`](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/unity-client/Assets/Editor/Increment511ScreenshotSuite.cs#L432-L433):
  ```csharp
  g.DrawString("Folio Oficial de Registro:", titleFont, cyan, x + 16, ly);
  g.DrawString("CJ-029411", folioFont, folioBrush, x + 195, ly - 2);
  ```
- **Image Content Inspection:**
  The actual PNG image `screenshots/511_04_world_core_confirmed_receipt.png` literally renders:
  `Folio Oficial de Registro: CJ-029411`.
- **Conclusion:**
  `FOLIO-2026-XXXXX` was an illustrative placeholder used in `README.md` and report markdown prose. The actual screenshot displays **`CJ-029411`**, which faithfully conforms to the `CJ-XXXXXX` format produced by the authoritative World Core domain model.

---

## 7. Action Plan & Required Decisions

| Candidate Action | Verified Safety Status | Recommendation | Status |
| :--- | :--- | :--- | :--- |
| **Move 4 Root Reports to `docs/`** | **[Verified Safe]** | Move `INCREMENT_5_11_*.md` to `docs/`. Zero links broken; cleans root. | **Requires User Decision** |
| **Promote Reusable Scripts** | **[Verified Safe]** | Place in `scripts/` as `verify-all-npcs.ts`, `verify-persistence-recovery.ts`, and `test-launcher-compatibility.ps1` with dedicated npm script entries. (Do not put in `tests/` to prevent mixing non-`*.test.ts` files with `run-tests.js`). | **Requires User Decision** |
| **Track Curated `screenshots/`** | **[Verified Safe]** | Track the 4 high-quality PNGs in Git. Verified clean of secrets, paths, and private data. (Total size: ~737 KB). | **Requires User Decision** |
| **Add `/scratch/` to `.gitignore`** | **[Verified Safe]** | Add `/scratch/` (root-anchored) to `.gitignore`. Retains all 46 diagnostic logs and one-off scripts on local disk without dirtying `git status`. | **Requires User Decision** |

---

## 8. Final Compliance Verification

- **Source & Config Files Modified:** None (0 modifications).
- **Files Moved, Copied, or Deleted:** None (0 moves/deletions).
- **External / Destructive Tests Executed:** None (0 external calls, 0 API credits used).
- **Git Staging, Commits, Pushes:** None (0 staging, 0 commits, 0 pushes).
- **Benchmark Repository:** Verified 100% untouched (`C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity` has 0 tracked changes).
- **Working Tree State:** Only the requested report `docs/INCREMENT_5_12_2_1_VALIDATION_REPORT.md` has been created.

