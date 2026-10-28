# MyVirtualCommunity AI Hackathon Edition

A clean, testable TypeScript foundation for an authoritative, Meta AI-powered local business assistant in the **MyVirtualCommunity** setting (Ciudad Juárez–El Paso region).

---

## 1. Project Purpose

When users or NPCs ask questions about local businesses (e.g. *"Is Wing Spot open right now?"* or *"What time does Pastelería La Tía Lupe close?"*), conversational language models frequently hallucinate hours, invent schedules, or misunderstand timezones.

This project implements a **grounded hexagonal architecture** where:
1. **Authoritative State:** Operating schedules, timezones, and operational status are derived deterministically in pure domain code.
2. **Anti-Hallucination Boundary:** The language model (Meta Model API `muse-spark-1.3`) is provided with verified operational facts and constrained to natural-language synthesis without ever deciding or inventing the hours itself.
3. **Zero Runtime Dependencies:** Built purely on native Node.js ES modules and built-in APIs (`node:http`, `Intl.DateTimeFormat`, native `fetch`).

---

## 2. Prerequisites

- **Node.js**: v20.18.0 or higher (tested on Node v20.19.0)
- **npm**: v10 or higher
- **Operating System**: Cross-platform (Windows, Linux, macOS)

---

## 3. Quickstart & Setup

Clone the repository and install development dependencies:

```bash
npm install
```

*(Zero runtime dependencies are installed; only TypeScript, `tsx`, and `@types/node` are downloaded as devDependencies).*

### Available Scripts

| Command | Description |
| :--- | :--- |
| `npm run dev` | Launches the local interactive web interface & API server at `http://127.0.0.1:3000`. |
| `npm run build` | Runs strict TypeScript type checking (`tsc --noEmit`). |
| `npm test` | Runs the 100% offline automated test suite using Node.js native test runner and `tsx`. |
| `npm run verify:live-assistant` | **Explicit live command** demonstrating exactly one live Muse Spark query grounded in local World Core data. |
| `npm run test:meta-smoke` | **Explicit live smoke test** verifying the raw Meta Model API handshake. |
| `npm run test:integration` | Runs the offline end-to-end integration test pairing `RemoteWorldCoreAdapter` with `MockMetaModelAdapter`. |

---

## 4. Interactive Web Demo (Judge-Ready)

The project includes a clean, responsive, single-origin web application served directly by the Node.js server.

### What the Interactive Demo Does
- **Bilingual Interface**: Toggles dynamically between English (default for judges) and Spanish while strictly preserving canonical business names returned by the simulation.
- **Verified Business Selector**: Provides point-and-click selection for only the verified customer-facing establishments in the World Core:
  - `restaurant:wing-spot` (`Wing Spot Gómez Morín`)
  - `restaurant:pasteleria-tia-lupe` (`Pastelerías La Tía Lupe`)
- **Interactive Question Input & Suggested Chips**: Users can type custom questions (up to 500 characters) or click contextual query chips.
- **Explicit Submission**: Requests are sent to `POST /api/assistant/query` without automated polling or premature execution.
- **Dual Presentation Display**:
  1. **AI Assistant Natural Language Answer**: Powered by Meta Muse Spark 1.3 with round-trip latency and model badge.
  2. **Authoritative World Core Facts Panel**: Deterministic factual panel displaying the canonical name, operational status (Open / Closing Soon / Closed), admission status (Permitted / Not Permitted), scheduled hours, last entry cutoff, authoritative world timestamp, and regional timezone (`America/Ciudad_Juarez`).
- **Resilient Error State**: Visibly and cleanly alerts users if the World Core simulation is unavailable, ensuring no model calls are made when facts cannot be retrieved.

### How to Run the Interactive Demo Locally

#### Step 1: Start the World Core Simulation Server
In a separate terminal, start the World Core on port 3001:

```powershell
cd C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity
$env:PORT="3001"
npx tsx src/index.ts
```

*Confirm the terminal logs `WORLD SERVER READY`.*

#### Step 2: Start the Hackathon Assistant Web Application
In your hackathon project directory:

```powershell
cd C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity-AI-Hackathon-Edition
npm run dev
```

*The server will start at `http://127.0.0.1:3000`.*

#### Step 3: Open the Interface in Your Browser
Navigate to:
```
http://127.0.0.1:3000/
```

- Click **EN** or **ES** in the top header to toggle between American English and Spanish.
- Select a business from the dropdown and select a suggested query or type your own question.
- Click **Ask Assistant** (or press `Ctrl+Enter`).
- The assistant retrieves an atomic snapshot from the local World Core and synthesizes a grounded answer with Meta Muse Spark.

### 4.1 Milestone 2: Time-Specific Business Admission & Operating Hours

The demo supports evaluating business operational status and admission eligibility at an explicit point in time:

- **Business-Local Time Semantics**: Any requested evaluation time is interpreted strictly in the business's authoritative IANA timezone (`America/Ciudad_Juarez`), **never** as the browser user's local timezone. Ciudad Juárez observes Mountain Time with daylight saving time under Northern Mexico border regulations:
  - **Mountain Daylight Time (MDT, UTC-6)**: In summer/autumn (from second Sunday in March through first Sunday in November, e.g. Oct 31, 2026).
  - **Mountain Standard Time (MST, UTC-7)**: In winter (from first Sunday in November through second Sunday in March, e.g. Nov 1, 2026 post-transition).
- **Critical Business Rule (Open vs. Admission Eligibility)**:
  - **Operating Hours**: Wing Spot Gómez Morín operates Mon–Sat from 09:00 to 18:00 with a 15-minute last-entry cutoff (17:45).
  - **At 5:40 PM**: The business is **OPEN** and new admissions are **PERMITTED**.
  - **At 5:50 PM**: The business is **OPEN** (status `CLOSING_SOON`), but new admissions are **DENIED** (`admission-cutoff-reached`).
  - **At 6:05 PM**: The business is **CLOSED** (`business-closed`).
  - **On Sunday**: The business is **CLOSED** (non-operating day).
- **Daylight Saving Time (DST) & Calendar Guardrails**:
  - Rejects non-existent calendar dates (e.g. Feb 30, non-leap Feb 29).
  - Detects spring-forward gaps (e.g. 2026-03-08 02:30 in `America/Ciudad_Juarez`) and rejects with HTTP 400 `invalid-evaluation-time`.
  - Flags fall-back overlaps (e.g. 2026-11-01 01:30, which occurs at both 07:30 UTC [MDT, UTC-6] and 08:30 UTC [MST, UTC-7]) with a clear warning communicating that the earlier occurrence (01:30 MDT / 07:30 UTC, offset -06:00) was evaluated.
- **Distinct Clock vs. Scenario Display**:
  - The UI facts panel clearly labels whether the evaluation was run against the **Live Simulation Clock** or a **Specific Scenario**, and displays both the local civil timestamp (e.g. `2026-10-31 17:50:00 -06:00` in MDT or `2026-11-01 12:00:00 -07:00` in MST) and the authoritative UTC world time (`2026-10-31T23:50:00.000Z` or `2026-11-01T19:00:00.000Z`).
- **Quick-Test Scenario Chips**:
  - The interface provides one-click scenario chips for judges to easily test:
    - *Sat 5:40 PM (Open & Admitting)*
    - *Sat 5:50 PM (Closing Soon / Cutoff)*
    - *Sat 6:05 PM (Closed)*
    - *Sun 12:00 PM (Closed / Sunday)*

### 4.2 Milestone 3: Spatial Slice v0.1 — Avenida Benito Juárez & Puente Internacional Paso del Norte

Milestone 3 implements the first interactive spatial slice for MyVirtualCommunity, centered on the real-world international border corridor around the **Puente Internacional Paso del Norte** (locally known as **Puente Santa Fe**) and the immediate Mexican access of **Avenida Benito Juárez** in Ciudad Juárez, Chihuahua, Mexico.

- **Real Geographic Coordinates & Authentic Geometry**:
  All landmark coordinates and geometries are verified against real authoritative sources. Zero coordinates, streets, or building footprints are fabricated:
  - **Puente Internacional Paso del Norte (Puente Santa Fe)**:
    - *Type*: `infrastructure` (Public border crossing / POI)
    - *Coordinates*: `31.74765, -106.48698` (Bridge structure) & `31.74588, -106.48641` (Plaza de Cobro C-51 toll plaza)
    - *Verified Sources*: Fideicomiso de Puentes Fronterizos de Chihuahua (`https://puentesfronterizos.gob.mx/`) • OpenStreetMap (`way/436474488`)
    - *Semantics*: Non-commercial public infrastructure. Does not have commercial opening hours, retail admission policies, live wait times, or traffic simulations in this slice.
  - **Farmacias Benavides**:
    - *Type*: `pharmacy` (Stable ID: `pharmacy:farmacias-benavides`)
    - *Address*: `Av. Benito Juárez 610, Chamizal, 32000 Juárez, Chih., México`
    - *Coordinates*: `31.745518, -106.486266`
    - *Verified Sources*: Google Maps Place CID `0x86e75c26ea377181:0x34ac7a36f923b0a1` (`https://maps.app.goo.gl/5i87hJyNoYhHDm6XA`) • World Core Authoritative Registry
    - *Verified Schedule*: Monday–Saturday 07:00–22:00, Sunday Closed. Zero last-entry cutoff (`lastEntryCutoffMinutes: 0`).
  - **Óptica Fashion**:
    - *Type*: `optical` (Stable ID: `optical:optica-fashion`)
    - *Address*: `Av. Benito Juárez 859, Centro, 32000 Juárez, Chih., México`
    - *Coordinates*: `31.745562, -106.486492`
    - *Verified Sources*: Google Maps Place CID `0x86e75c24e62247f1:0x17281bd8f8ba4b4d` (`https://maps.app.goo.gl/Si2SQah29Cxm13cRA`) • World Core Authoritative Registry
    - *Verified Schedule*: Monday–Saturday 09:00–17:00, Sunday Closed. Zero last-entry cutoff (`lastEntryCutoffMinutes: 0`).

- **Interactive Leaflet Web Map**:
  - Embedded lightweight interactive web map using Leaflet and OpenStreetMap tiles with verified attribution (`© OpenStreetMap contributors`).
  - Custom dark-theme markers distinguish border infrastructure (`🌉`), retail pharmacy (`💊`), and optical services (`👓`).
  - Clicking any map marker or quick-select chip centers and zooms the map onto that landmark and opens its contextual detail panel.
  - Keyboard accessible (`Tab`, `Enter`, `Space`) with descriptive ARIA landmark roles.
  - Offline-resilient fallback: If map tiles or Leaflet fail, an informative fallback banner appears without blocking or breaking the assistant query workflow.

- **Spatial-to-Assistant Contextual Synchronization**:
  - Selecting a business on the map immediately activates that business in the query dropdown and populates contextual question suggestions.
  - Clicking **"Ask About This Business"** smoothly focuses the question input and submits queries directly through `POST /api/assistant/query`.
  - World Core returns authoritative schedule and admission facts, which Meta Muse Spark explains in natural language in either American English or Spanish.

- **Clean Decoupling for Future 3D Clients**:
  - The map library is purely a visual presentation layer and has zero authority over world state.
  - Spatial corridor entities and bounding geometries are exposed through `GET /api/spatial/corridor` and pure domain models (`src/domain/spatial-corridor.ts`), ensuring future 3D clients (such as Unity or WebGL) can consume identical entity IDs, coordinates, and World Core rules.

### 4.3 Increment 1: Border Intelligence — Official Pedestrian Wait Times & Cameras

Integrates official real-world public border crossing data for northbound pedestrian travelers crossing from Ciudad Juárez into El Paso:

- **Official Data Integration**: Direct read-only integration with U.S. Customs and Border Protection (`https://bwt.cbp.gov/`) for **Paso del Norte (Santa Fe)** (Port `06240202`) and **Bridge of the Americas (BOTA / Puente Libre)** (Port `06240201`).
- **Explicit Lane Scope**: Evaluates and compares strictly the **General / Standard Pedestrian Lane** (excluding Ready Lane, SENTRI, and vehicular queues) with clear bilingual notices.
- **Data Freshness & Provenance**: Classifies CBP source reporting timestamp freshness (`fresh` <= 60 min, `stale` > 60 min, `pending`, `unavailable`) without fabricating missing observations.
- **Deterministic Comparison**: Generates a safe, non-hallucinatory comparison statement only when both crossings report compatible, fresh numeric delays and are officially open.
- **Verified Official Cameras**: Direct links to authentic live visual feeds from the Chihuahua Bridge Authority and the City of El Paso international bridges camera portal.

### 4.4 Increment 2: Synthetic Community Witnesses — Simulated Queue NPCs

Introduces an interactive community of three explicitly synthetic conversational NPCs waiting in the simulated pedestrian queue at `infrastructure:puente-paso-del-norte`:

- **Purpose & Scope**: Demonstrates how generative AI can bring a simulated border community to life while maintaining an impenetrable boundary between fictional scenario testimony and authoritative real-world measurements.
- **Uncompromised Truth Invariant & Synthetic Disclosure**:
  - Every NPC and response carries explicit synthetic provenance:
    - English: **`Synthetic witness · Simulation`**
    - Spanish: **`Testigo sintético · Simulación`**
  - Prominent UI disclosures inform the player that all three characters are simulated agents with fictional scenario perspectives, distinct from official CBP measurements.
  - Synthetic testimony is never presented as a real-time measurement or proof of actual traveler delay.
- **Three Configured Synthetic Witnesses**:
  1. **Mateo Navarro (`mateo-student`, 🎓, Age 21)**: UTEP engineering student commuting to an evening robotics study group in El Paso. Scenario wait: ~25 minutes. Focused on study materials, pedestrian walking routine, and binational student commute.
  2. **Sofia Alarcón (`sofia-cross-border-retail`, 💼, Age 34)**: Downtown El Paso apparel store supervisor crossing for an evening inventory shift. Scenario wait: ~40 minutes. Experienced commuter perspective, evaluates bridge movement pace, observes shift worker lines.
  3. **Héctor Méndez (`hector-artisan`, 👴, Age 67)**: Retired leather artisan visiting his granddaughter for her birthday in Sunset Heights. Scenario wait: ~15 minutes. Patient, conversational demeanor, reminisces about historic downtown bridge crossings over decades.
- **Bilingual Conversations (EN / ES)**:
  - Users can select any witness, choose a suggested quick question chip, or type a custom question (up to 500 characters).
  - Responds in the question's detected language (English or Spanish) or the user's selected UI language.
  - If asked about current delays or which bridge is faster, the witness speaks only from personal scenario experience and explicitly directs the user to the official CBP Border Intelligence tracker above.
- **Authority Boundaries & Deterministic Fallback**:
  - Neither the NPCs nor the language model have authority to change world state, modify business schedules, alter CBP metrics, or invent authoritative facts.
  - If the Meta Model API is unavailable or returns unusable output, the application falls back to a deterministic domain response engine (`provider: 'deterministic-fallback'`) with zero crashes or error leaks.
- **Known Limitations**:
  - Stateless single-turn dialogue: each query is processed independently against the selected NPC's configured scenario profile without model-level conversation history or session persistence.
  - Scope confined to the pedestrian queue at Paso del Norte (excludes commercial cargo and vehicular lines).

### 4.5 Increment 3: Border Concierge — Grounded Traveler Guidance

Introduces an interactive, bilingual Border Concierge NPC (**Elena Ramos**, `elena-border-concierge`, 🛂) at the simulated Paso del Norte pedestrian crossing:

- **Purpose & Role**: Helps pedestrian travelers understand official border observations, compare Paso del Norte with Bridge of the Americas (BOTA), interpret data freshness and operational status, and find official live camera views without giving the model authority over world state or allowing it to invent official border conditions.
- **Concierge Capabilities & Supported Inquiries**:
  - *Reported Wait at Paso del Norte*: Retrieves the official CBP observation for Port `240202` (Standard pedestrian lane) with observation timestamp and fetch time.
  - *Reported Wait at BOTA*: Retrieves the official CBP observation for Port `240201` (Standard pedestrian lane) with observation timestamp and fetch time.
  - *Crossing Comparison*: Determines which crossing reports a lower standard pedestrian wait in the available observations, only when both crossings are open, fresh, and numeric.
  - *Data Freshness & Provenance*: Explains when observations were recorded by CBP versus when they were fetched by the application, clearly distinguishing `fresh`, `stale`, `pending`, and `unavailable`.
  - *Official Cameras*: Provides direct references to the official Fideicomiso de Puentes Fronterizos de Chihuahua live camera portal and official CBP pedestrian wait pages.
  - *Traveler Guidance*: Explains why point-in-time snapshots do not guarantee an end-to-end crossing duration, how conditions can fluctuate, and geographic destination differences (Downtown El Paso vs. Chamizal/I-110).
- **Truth, Freshness, and Comparison Rules**:
  - *Strict Observation Semantics*: Every factual answer preserves the distinction between the CBP observation timestamp (`sourceObservationTime`, e.g. `10/27/2026 21:45:00`), the application's fetch timestamp (`fetchedAt`, stored as ISO UTC but always localized in user-facing presentation to `America/Ciudad_Juarez` local time), and the freshness rating. The application never substitutes fetch time for observation time.
  - *Comparison Eligibility Guardrails*: The concierge only compares crossings if both are officially `Open`, both report valid numeric wait times, both are `fresh` (within the canonical 60-minute window), and both share the same Standard / General pedestrian lane. If any condition fails, the concierge refuses the comparison and clearly explains why (stale data, closed port, pending update, or incompatible lanes). The concierge never upgrades stale observations to fresh.
  - *Closed Crossing Invariant*: A closed port is never represented as a 0-minute wait; its status is reported explicitly as `Closed`.
  - *Point-in-Time Caveat*: When a valid comparison exists, the concierge explicitly caveats that reported wait times are historic snapshots and conditions can change rapidly before arrival.
- **Camera References & Video Analysis Boundaries**:
  - The concierge provides direct, verified links to the official Chihuahua Bridge Authority live camera portal (`https://www.puentesfronterizos.gob.mx/camaras-en-vivo.php`) and CBP wait pages.
  - The concierge **explicitly disclaims automated video analysis**: camera feeds are linked for human travelers, not visually inspected, computer-vision analyzed, or used to measure queue density.
- **Meta Muse Spark Integration & Deterministic Fallback**:
  - Uses Meta Model API (`muse-spark-1.3`) constrained by pure domain prompt boundaries. The model receives only verified CBP observations and static explanatory context; it has zero authority to alter numeric delays, change operational status, or access synthetic witness profiles.
  - If the Meta API is unavailable or returns an error, the concierge transparently falls back to pure deterministic domain formatting (`formatDeterministicConciergeResponse`), ensuring 100% availability with exact measurements and provenance.
- **Distinct Tri-Fold Border Experience**:
  1. *Authoritative Border Intelligence*: Live, normalized CBP cards with minute delays, lane counts, and freshness pills.
  2. *Synthetic Community Witnesses*: Simulated queue commuters (Mateo, Sofia, Héctor) with personal narratives and clear simulation disclosures.
  3. *Grounded Border Concierge*: Interactive, grounded guidance (Elena Ramos) explaining observations, comparison rules, and camera resources.

---

The project follows a strict Hexagonal / Clean Architecture pattern:

```
src/
├── domain/            # Pure business logic & contracts (zero dependencies)
│   ├── business-id.ts          # Branded type <category>:<slug>
│   ├── civil-time.ts           # Pure mathematical timezone projection
│   ├── business-schedule.ts    # Schedule schema and strict validation
│   ├── operational-status.ts   # Deterministic status derivation (OPEN/CLOSED/etc.)
│   ├── spatial-corridor.ts     # Authentic spatial corridor & POI definitions (Milestone 3)
│   ├── border-crossing.ts      # Border crossing contracts & deterministic comparison (Increment 1)
│   ├── synthetic-witness.ts    # Synthetic NPC profiles, boundaries & fallback engine (Increment 2)
│   └── border-concierge.ts     # Concierge profile, intent classifier & prompt boundaries (Increment 3)
├── ports/             # Boundary contracts
│   ├── world-core-port.ts      # Read-only World Core query interface
│   ├── border-crossing-port.ts # CBP wait time data provider port
│   └── meta-model-port.ts      # Meta Model prompt context & response port (assistant + witnesses + concierge)
├── adapters/          # Port implementations
│   ├── in-memory-world-core-adapter.ts # Seeded development fixture
│   ├── cbp-border-wait-time-adapter.ts # Official CBP REST API adapter with caching
│   ├── meta-model-api-adapter.ts       # Live Meta Model API adapter (muse-spark-1.3)
│   └── mock-meta-model-adapter.ts      # Offline deterministic mock adapter
├── application/       # Use case orchestration
│   ├── answer-business-hours-query.ts  # End-to-end grounded assistant flow
│   ├── compare-border-wait-times.ts    # Border wait times comparison use case
│   ├── talk-to-synthetic-witness.ts    # Synthetic witness dialogue orchestration
│   └── ask-border-concierge.ts         # Border Concierge grounded guidance use case
├── config/            # Environment parsing & secret masking
│   └── index.ts
└── server/            # HTTP transport
    └── index.ts                # Lightweight REST API server
scripts/
├── run-tests.js       # Cross-platform test runner
└── meta-smoke-test.ts # Dedicated, explicitly invoked live smoke test
tests/                 # 100% offline test suite (275 tests across 71 suites)
docs/
├── HACKATHON_SCOPE.md         # Explicit MVP scope and boundaries
├── ARCHITECTURE.md            # Complete architecture diagrams and data flows
├── REUSE_AND_ATTRIBUTION.md   # Provenance & separation from pre-existing project
└── DEMO_PLAN.md               # Step-by-step testable verification scenarios
```

---

## 5. Two-Project Architecture & World Core Integration

This project integrates with the pre-existing **MyVirtualCommunity World Core** through a strict, read-only HTTP boundary.

```
┌────────────────────────────────────────────────────────┐
│         Existing World Core (MyVirtualCommunity)       │
│  - Authoritative World Clock (ISO UTC)                 │
│  - Spatial topology & entity persistence               │
│  - Read-Only Endpoint:                                 │
│    GET /api/businesses/:businessId/schedule            │
└───────────────────────────▲────────────────────────────┘
                            │ HTTP GET (JSON)
                            │ Read-Only Boundary
┌───────────────────────────┴────────────────────────────┐
│      AI Hackathon Edition (This Repository)            │
│  - RemoteWorldCoreAdapter (HTTP client)                │
│  - InMemoryWorldCoreAdapter (Development fixture)      │
│  - Pure Domain Derivation (CivilTime, Status)          │
│  - Anti-hallucination prompt boundary                  │
│  - Meta Model API Adapter (muse-spark-1.3)             │
└────────────────────────────────────────────────────────┘
```

### 5.1 How to Start the Existing World Core Server

In the `MyVirtualCommunity` repository:

```powershell
cd C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity
$env:PORT="3001"
npx tsx src/index.ts
```

The server starts listening on `http://127.0.0.1:3001` and logs `WORLD SERVER READY`.

### 5.2 How to Verify the World Core Endpoint Independently

Execute a direct HTTP request to the running World Core:

```powershell
# Windows PowerShell
Invoke-RestMethod -Uri "http://127.0.0.1:3001/api/businesses/restaurant:wing-spot/schedule" | ConvertTo-Json -Depth 5

# Or via curl
curl http://127.0.0.1:3001/api/businesses/restaurant:wing-spot/schedule
```

**Expected Response Contract:**
```json
{
  "success": true,
  "result": {
    "businessId": "restaurant:wing-spot",
    "name": "Wing Spot Gómez Morín",
    "timeZone": "America/Ciudad_Juarez",
    "worldTime": "2026-10-27T04:30:03.572Z",
    "schedule": {
      "operatingDays": [1, 2, 3, 4, 5, 6],
      "hours": { "openHour": 9, "openMinute": 0, "closeHour": 18, "closeMinute": 0 },
      "lastEntryCutoffMinutes": 15,
      "closureOverrides": [],
      "version": 1
    },
    "operationalStatus": {
      "status": "closed",
      "isOpen": false,
      "admissionPermitted": false,
      "code": "business-closed",
      "message": "El establecimiento está cerrado. No se permite el ingreso. Cierre: 18:00.",
      "openTimeStr": "09:00",
      "closingTimeStr": "18:00"
    }
  }
}
```

### 5.3 Configuring `WORLD_CORE_BASE_URL` in the Hackathon Application

Configure the base URL in your `.env` file or environment:

```env
WORLD_CORE_BASE_URL=http://127.0.0.1:3001
```

- When `WORLD_CORE_BASE_URL` is set, the assistant server instantiates `RemoteWorldCoreAdapter`.
- When omitted or unset, the application automatically falls back to `InMemoryWorldCoreAdapter` for offline development.

### 5.4 Running the Local End-to-End Integration Test

With the World Core running on port 3001, execute:

```powershell
$env:WORLD_CORE_BASE_URL="http://127.0.0.1:3001"
npm run test:integration
```

This executes `scripts/verify-world-core-integration.ts`, which:
1. Queries the real local World Core endpoint directly via native `fetch`.
2. Validates `RemoteWorldCoreAdapter` against Wing Spot, Tía Lupe, and unknown businesses.
3. Pairs `RemoteWorldCoreAdapter` with `MockMetaModelAdapter` to execute `AnswerBusinessHoursQueryUseCase` end-to-end with **zero cloud API calls and zero credit consumption**.

### 5.5 Real World Core Data vs. Development Fixture

| Aspect | Real World Core (`MyVirtualCommunity`) | In-Memory Fixture (`InMemoryWorldCoreAdapter`) |
| :--- | :--- | :--- |
| **Source of Truth** | Authoritative `WorldClock` and engine state | Fixed in-memory development objects |
| **Wing Spot Hours** | Mon–Sat, 09:00–18:00 (Closed Sunday) | Mon–Sat, 09:00–18:00 (Closed Sunday) |
| **Tía Lupe Hours** | Mon–Sat, 09:00–18:00 (Closed Sunday) | Everyday, 08:30–20:00 (Provisional mock) |
| **Time Source** | Live `WorldClock.getTime()` | Simulated or local machine fallback |
| **Failure Mode** | Returns 404 / network errors explicitly | Never fails unless programmed |

### 5.6 Boundary Between Pre-Existing and Hackathon Code

- **Pre-existing World Core (`MyVirtualCommunity`):**
  Owns spatial navigation, 3D world topology, entity identities, NPC behavior, simulation ticks, and persistent world state. The only modification made is the addition of the read-only endpoint `GET /api/businesses/:businessId/schedule` via `getBusinessScheduleView` on `WorldSessionController`.
- **Hackathon Edition (`MyVirtualCommunity-AI-Hackathon-Edition`):**
  Newly developed repository containing the hexagonal architecture, `RemoteWorldCoreAdapter`, anti-hallucination domain rules, use case orchestrators, REST server, and Meta Model API integration. **No files are vendored or copied from the original project.**

---

## 6. Security & Credential Safety

- **Zero Credential Leaks:** API keys are never printed in logs or test outputs. The `maskApiKey` utility ensures sensitive tokens are always masked (e.g. `[CONFIGURED]` or `[NOT CONFIGURED]`).
- **No Automatic API Calls:** The automated test suite (`npm test`), build (`npm run build`), integration tests (`npm run test:integration`), and default server startup (`npm run dev`) never call the live Meta API or consume credits.
- **Environment Isolation:** `.env` and `.env.*` files are strictly excluded via `.gitignore`. An example template is provided in `.env.example`.

---

## 7. Further Documentation

For in-depth details, see:
- [`docs/HACKATHON_SCOPE.md`](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/docs/HACKATHON_SCOPE.md)
- [`docs/ARCHITECTURE.md`](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/docs/ARCHITECTURE.md)
- [`docs/REUSE_AND_ATTRIBUTION.md`](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/docs/REUSE_AND_ATTRIBUTION.md)
- [`docs/DEMO_PLAN.md`](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/docs/DEMO_PLAN.md)