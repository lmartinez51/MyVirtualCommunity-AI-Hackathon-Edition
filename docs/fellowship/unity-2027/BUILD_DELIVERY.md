# Windows Build & Evaluation Delivery Checklist

**Project:** My Virtual Community — *A Living World Beyond Reality.*
**Target Platform:** Windows 10 / 11 (`x86_64` Standalone)
**Unity Editor Version:** `6000.3.25f1` (`unity-client/ProjectSettings/ProjectVersion.txt`)

> **Important Packaging Distinction (`CONFIRMED` vs. `PLANNED`):**
> - **Current State (Developer Build):** The repository builds `unity-client/Builds/Windows/MyVirtualCommunity.exe` via `WindowsStandaloneBuilder.cs` and `scripts/build-standalone.ps1`, and currently runs alongside the local Node.js/TypeScript backend started by `scripts/launch-demo.ps1`. It is **not** yet a single-click, zero-prerequisite consumer installation.
> - **Target Fellowship / Commercial Package (`PLANNED`):** A polished, IP-cleared, self-contained Windows PC package playable 100% offline with zero manual Node.js or PowerShell setup required by evaluators or players.

Related documents: [Application Brief](APPLICATION_BRIEF.md) · [Feature Status Matrix](../../FEATURE_STATUS_MATRIX.md) · [IP & Asset Provenance](../../IP_AND_ASSET_PROVENANCE.md) · [Vertical Slice](../../VERTICAL_SLICE.md)

---

## 1. Current Verified Developer Build Pipeline (`CONFIRMED`)

The following build and packaging scripts exist in the repository today:

| Tool / Script | Verified Role | Notes |
| :--- | :--- | :--- |
| `unity-client/Assets/Editor/WindowsStandaloneBuilder.cs` | Unity batchmode builder outputting `unity-client/Builds/Windows/MyVirtualCommunity.exe` (`StandaloneWindows64`). | Currently compiles 3 scenes: `BorderCorridorScene.unity`, `HandshakeScene.unity`, `BackroomsLevel1Scene.unity`. |
| `scripts/build-standalone.ps1` | Computes deterministic SHA-256 fingerprint across `Assets/`, `ProjectSettings/`, `Packages/`, and itself; rebuilds via Unity batchmode when stale; writes `build-manifest.json` with hashes of 8 critical runtime files. | Latest local manifest on disk (`2026-11-24T05:31:17Z`) recorded a `57.4 s` build (`153.6 MB` payload). |
| `scripts/package-release.ps1` | Verifies build freshness (`-CheckOnly`), stages `MyVirtualCommunity.exe`, `UnityPlayer.dll`, `UnityCrashHandler64.exe`, `D3D12/`, `MonoBleedingEdge/`, and `MyVirtualCommunity_Data/`, and compresses to `Builds/Release/MyVirtualCommunity-Windows-x64.zip`. | **Note:** Existing local `Builds/Release/MyVirtualCommunity-Windows-x64.zip` is from **Nov 10, 2026** and is **stale** relative to the Nov 24 build — always rebuild and re-package fresh before any delivery. |
| `scripts/launch-demo.ps1` | Starts the local Node.js backend (`src/server/index.ts` via `tsx`), verifies `/health`, launches `MyVirtualCommunity.exe`, and cleanly shuts down only its own child backend tree on exit. | Requires Node.js v20.18+ and `node_modules` installed; still checks historical service string `"MyVirtualCommunity AI Hackathon Assistant"`. |

---

## 2. Pre-Delivery Checklist for Unity Fellowship Playable Demo (`PLANNED`)

Execute **every gate below** before uploading any playable build to Unity Fellowship reviewers (deadline **December 18, 2026, 5:00 p.m. ET**):

### Gate A — IP & Content Clearance
- [ ] Zero `CRITICAL` horror franchise props/strings remain in `BackroomsLevel1Scene.unity` or scripts (`IP-01`).
- [ ] `"SS GEORGIE"` texture/prop replaced (`IP-02`).
- [ ] Storefront signage, consumer device props, and arch video (`camila_loop.mp4`) remediated per creator-approved brand strategy (`IP-04`, `IP-05`, `IP-06`, `IP-07`).
- [ ] `docs/THIRD_PARTY_NOTICES.md` and `LICENSE` bundled cleanly inside the delivery archive.

### Gate B — Build Configuration & Branding
- [ ] Update `ProjectSettings.asset`: replace `DefaultCompany` and `unity-client` with final studio/creator name and **My Virtual Community**.
- [ ] Remove legacy `HandshakeScene.unity` from production build settings (unless deliberately retained for diagnostics).
- [ ] Gate aerial `[V]` Drone / `[G]` Satellite debug cameras out of normal demo play.
- [ ] Confirm zero secrets (`.env`, API keys) or ephemeral local artifacts (`.runtime/`, logs, `scratch/`) are included in the archive.

### Gate C — Self-Contained Offline Evaluation Experience
- [ ] Verify the demo runs **100% offline** with airplane mode enabled (no Meta API keys, no CBP/Open-Meteo/YouTube network calls required).
- [ ] Confirm evaluator launch flow requires zero manual terminal troubleshooting (either fully embedded Unity C# offline simulation or a single self-contained launcher executable tested on a clean Windows 10/11 machine without Unity Editor or Node.js pre-installed).
- [ ] Include a concise 1-page `EVALUATOR_README.txt` inside the ZIP with system requirements, controls (`WASD`, `Shift`, `Space`, `E`, `F`, `M`, `Esc`), and known demo scope boundaries (including honest disclosure that *El Umbral de Lasser* concludes *Slice 01*).

### Gate D — Clean-Machine Smoke Test & Artifact Integrity
- [ ] Run backend verification (`npm run build`, `npm test`) if backend runtime is still included.
- [ ] Run fresh `.\scripts\build-standalone.ps1 -Force` and `.\scripts\package-release.ps1 -Force` from a clean Git commit on the release branch (`release/fellowship-demo-2026-12`).
- [ ] Extract the ZIP into a clean directory outside the repo and verify:
  1. Boot to Title Screen → enter Avenida Benito Juárez with stable framerate and zero console crashes.
  2. Complete full *Slice 01* path: Surface Corridor → Underground Storm Drain → Secret Room Keypad → Backrooms Level 1 → Level-Ω Safe → *El Ojo del Umbral* → *El Umbral de Lasser* activation → Return to surface / Slice Complete.
  3. Clean exit with zero orphaned background processes.
- [ ] Record final fresh SHA-256 checksum, compressed ZIP size, uncompressed size, and exact Git commit SHA in the submission record.
