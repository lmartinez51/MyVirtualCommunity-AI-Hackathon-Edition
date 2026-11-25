# Reuse, Attribution, and Intellectual Provenance

This document provides clear and explicit attribution of intellectual property, domain concepts, and code assets. It distinguishes work developed specifically for this hackathon from pre-existing systems.

---

## 1. Hackathon Development Context

This repository (`MyVirtualCommunity-AI-Hackathon-Edition`) was created for the **AI Hackathon** to establish a modern, clean, testable TypeScript foundation integrating Meta AI models (`muse-spark-1.3`) into local business assistance.

All code and documentation files committed to this repository's working branch (`feat/meta-model-foundation`) were newly written during this hackathon task.

---

## 2. Explicit Separation: Pre-Existing Systems vs. Newly Created Code

| Asset / Component | Provenance | Status in this Repository |
| :--- | :--- | :--- |
| **Virtual World Setting & Lore** | Pre-existing project (`MyVirtualCommunity`) | Conceptual inspiration only (Ciudad Juárez–El Paso regional geography, Gómez Morín corridor). |
| **World Core Simulation Engine** | Pre-existing project (`MyVirtualCommunity`) | **External system.** Not vendored, copied, or included in this repo. Represented cleanly via the `WorldCorePort` boundary and `InMemoryWorldCoreAdapter` fixture. |
| **Unity 3D Client & Bobblehead Character** | Pre-existing project (`MyVirtualCommunity`) | **External system.** Pre-existing 3D assets, Unity scripts, shaders, and animations are strictly excluded from this repository and were not created during this hackathon. |
| **Hexagonal Ports & Adapters Architecture** | Newly created in this hackathon | Fully implemented in TypeScript (`src/ports/`, `src/adapters/`). |
| **Business Operational Status Domain** | Newly created in this hackathon | Built from scratch in `src/domain/` with branded types, strict validation, and deterministic civil-time projection. |
| **Meta Model API Integration** | Newly created in this hackathon | Implemented in `src/adapters/meta-model-api-adapter.ts` with zero dependencies, bearer authentication, and key redaction safeguards. |
| **Offline Testing Infrastructure** | Newly created in this hackathon | Implemented in `src/adapters/mock-meta-model-adapter.ts` and `scripts/run-tests.js` with 100% offline automated test suite. |
| **HTTP Assistant Server** | Newly created in this hackathon | Implemented in `src/server/index.ts` using native `node:http`. |
| **Smoke Test Tooling** | Newly created in this hackathon | Implemented in `scripts/meta-smoke-test.ts` for safe manual execution. |

---

## 3. Strict Non-Claims

To ensure full transparency:
1. **No claim of 3D asset creation:** We do NOT claim that the 3D models, avatars, Unity scenes, or character rigs were created during this hackathon.
2. **No claim of pre-existing World Core:** We do NOT claim that the original World Core simulation engine or its milestone history was authored during this event.
3. **No vendored source code:** No files were copied or ported directly from `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity`. The interfaces in this repository are standalone, newly authored TypeScript contracts designed for clean future integration.
