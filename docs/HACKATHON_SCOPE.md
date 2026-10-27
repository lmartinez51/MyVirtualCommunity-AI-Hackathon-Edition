# MyVirtualCommunity AI Hackathon Scope

This document defines the explicit MVP scope for the **MyVirtualCommunity AI Hackathon Edition** and outlines boundaries between current deliverables and future iterations.

---

## 1. Hackathon Objective & Problem Statement

In virtual communities and open worlds representing authentic geographic settings (such as the Ciudad Juárez–El Paso border region), non-player characters (NPCs) and local business assistants must communicate accurate, realistic information about business availability.

If an assistant relies entirely on unconstrained generative language models, it frequently hallucinates operating hours, provides incorrect time offsets, or contradicts the authoritative world state.

**Core Solution:**
A strictly grounded, hexagonal architecture where:
1. Authoritative business schedules and civil time projections are evaluated deterministically in the domain layer.
2. The Meta Model API (`muse-spark-1.3`) receives verified, tamper-proof operational facts as ground truth.
3. The language model formulates conversational, courteous responses without ever deciding or inventing the hours itself.

---

## 2. In-Scope MVP Capabilities (Deliverables)

The following capabilities are implemented, tested, and included in this foundation:

- **TypeScript / Node.js Foundation:**
  - Strict type checking with NodeNext native ES modules (`type: "module"`).
  - Native Node.js test runner with `tsx` (`npm test`).
  - Zero runtime dependencies for security, speed, and determinism.
- **Strongly Typed Domain Contracts:**
  - Branded `BusinessId` (`<category>:<slug>`) with strict validation.
  - Mathematical `CivilTime` projection using native `Intl` for deterministic IANA timezone calculations (e.g. `America/Ciudad_Juarez`, `America/Denver`).
  - Validated `BusinessSchedule` contract with operating days, hours, admission cutoffs, and scheduled closures.
  - Authoritative, deterministic `evaluateBusinessOperationalStatus` deriving `OPEN`, `CLOSING_SOON`, `CLOSED`, and `SPECIAL_CLOSURE` states.
- **Boundary Ports:**
  - Read-only `WorldCorePort` defining access to authoritative business data without direct world mutation.
  - `MetaModelPort` defining structured, grounded interaction with Meta's reasoning models.
- **Adapters:**
  - `InMemoryWorldCoreAdapter`: Clearly labeled development fixture seeded with authentic Gómez Morín establishments (`restaurant:wing-spot`, `restaurant:pasteleria-tia-lupe`).
  - `MetaModelApiAdapter`: Native `fetch`-based adapter connecting to Meta Model API (`https://api.meta.ai/v1/chat/completions`) using `muse-spark-1.3`. Includes credential protection (keys are never logged or leaked in errors).
  - `MockMetaModelAdapter`: Offline, deterministic adapter enabling automated testing without internet or Meta credits.
- **Application Use Case:**
  - `AnswerBusinessHoursQueryUseCase`: Orchestrates schedule retrieval, status evaluation, and grounded prompt construction.
- **Server & CLI:**
  - Lightweight HTTP server (`src/server/index.ts`) providing `/health`, `/api/businesses`, `/api/businesses/:id/status`, and `/api/assistant/query`.
  - Dedicated developer smoke test script (`scripts/meta-smoke-test.ts`) for explicit manual verification.

---

## 3. Explicitly Out of Scope for This Foundation

The following features belong to broader world simulation milestones and are **strictly excluded** from this foundation task:

- **Economy & Payments:** Currency, wallets, bank accounts, prices, credit cards, or cash transactions.
- **Order Fulfillment & Inventory:** Kitchen workflows, inventory stock decrementing, cooking queues, or physical item pickup.
- **NPC Autonomy & Navigation:** Pathfinding, NavMesh traversal, goal trees, steering, or proactive roaming.
- **Multimodal Media Generation:** Voice synthesis/transcription (TTS/STT), Segment Anything Model (SAM), image generation, or video creation.
- **Unity 3D Client Integration:** WebGL/Windows Unity client rendering, avatar animation, or spatial 3D audio.
- **Real-time World Replication:** WebSockets, UDP sync, or multi-client replication.

---

## 4. Current Limitations & Transition Path

1. **World Core Boundary:**
   - The pre-existing project repository lacks an external HTTP or RPC endpoint for business hours.
   - Currently, `InMemoryWorldCoreAdapter` serves as the development fixture.
   - Transition requirement: When World Core exposes `GET /api/businesses/:id/schedule` or an RPC service, implement a `RemoteWorldCoreAdapter` satisfying `WorldCorePort`.
2. **Meta Model Live Access:**
   - Requires setting `MODEL_API_KEY` in `.env` and explicitly running `npm run test:meta-smoke`.
   - Offline mode automatically uses `MockMetaModelAdapter` when no key is set.
