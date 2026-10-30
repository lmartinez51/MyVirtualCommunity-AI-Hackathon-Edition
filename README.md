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

### 4.6 Increment 4: Local Business Attendants — Grounded Storefront NPCs

Introduces interactive virtual storefront attendants stationed at local businesses along the simulated Avenida Benito Juárez commercial corridor:

- **Attendant Personas & Business Associations**:
  - **Carlos Méndez** (`carlos-farmacia-attendant`, 💊) — Virtual Pharmacy Attendant at **Farmacias Benavides** (`pharmacy:farmacias-benavides`). Courteous, health-conscious, and attentive to operating hours and the pharmacy's physical address (Av. Benito Juárez 610) near the international bridge.
  - **Valeria Domínguez** (`valeria-optica-attendant`, 👓) — Virtual Optical Attendant at **Óptica Fashion** (`optical:optica-fashion`). Welcoming, stylish, and knowledgeable about confirmed business hours and boutique location (Av. Benito Juárez 859) across from Farmacias Benavides.
- **Fictional Virtual Character Disclosure**:
  - Every attendant is visibly labeled in English (`Virtual attendant · AI simulation`) and Spanish (`Encargado virtual · Simulación de IA`).
  - Strict disclosure: These characters are fictional virtual simulations, **not real human employees, licensed pharmacists, or certified eye care practitioners**. They speak only for the simulated community experience and cannot execute commercial or clinical transactions.
  - The canonical business names `Farmacias Benavides` and `Óptica Fashion` remain strictly untranslated in both languages.
- **How to Interact in the Demo**:
  1. Select a business on the interactive Leaflet map or via the corridor entity chips (`Farmacias Benavides` or `Óptica Fashion`). The Storefront Attendants panel automatically synchronizes to the corresponding character.
  2. Alternatively, select an attendant card directly in the Storefront Attendants panel, which smoothly pans the map and highlights the business storefront pin above.
  3. Use the one-click suggested question chips (e.g. Operating Hours, Current Open Status, Store Location, Inventory Check, or Appointment Booking) or enter any custom question in English or Spanish.
- **Supported Business Inquiries (Grounded in World Core Facts)**:
  - *Store Location & Address*: Reports verified physical addresses (`Av. Benito Juárez 610` and `Av. Benito Juárez 859`) and spatial orientation along the Avenida Juárez corridor.
  - *Authoritative Operating Hours*: Reports confirmed schedules (`Mon–Sat 07:00–22:00` for Benavides, `Mon–Sat 09:00–17:00` for Óptica Fashion; both closed Sunday).
  - *Current Open / Closed Status*: Grounds answers in the authoritative point-in-time snapshot from World Core, reporting whether doors are currently open and whether new admissions are permitted, with evaluated timestamps formatted in local Ciudad Juárez time (`America/Ciudad_Juarez`).
- **Knowledge Boundaries & Refusal Guardrails**:
  - *Farmacias Benavides Knowledge Limits*:
    - **No Medication Inventory or Stock Levels**: Cannot confirm specific drug stocks, pill counts, or brand availability.
    - **No Pricing or Promotions**: Does not quote medication costs, discounts, or insurance acceptance.
    - **No Medical Diagnoses or Prescriptions**: Strictly refuses to prescribe treatments, evaluate symptoms, or validate prescriptions. Politely directs visitors to consult a licensed physician or visit the pharmacy in person during open hours.
  - *Óptica Fashion Knowledge Limits*:
    - **No Frame/Lens Inventory**: Cannot verify live frame brands, lens treatments, or stock.
    - **No Pricing or Insurance Quotes**: Does not quote glasses or exam pricing.
    - **No Appointment Scheduling**: The system does not maintain an appointment booking calendar. Directs visitors to visit the boutique in person during operating hours.
  - *Explicit Unavailable Facts Notice*: When an unsupported topic (inventory, pricing, appointments, medical) is queried, the response includes an explicit notice identifying that these data categories are absent from trusted business records.
- **Meta Muse Spark & Deterministic Fallback**:
  - Uses Meta Model API (`muse-spark-1.3`) constrained strictly by prompt context containing verified World Core snapshots.
  - When the user asks boundary-violating questions (inventory, medical, pricing, appointments), the system invokes a deterministic, non-hallucinatory domain response path (`getDeterministicAttendantResponse`).
  - If the Meta API fails or is unreachable, the attendant falls back to the deterministic domain response path, ensuring 100% demo availability.
  - If the authoritative World Core source itself is unavailable, the attendant returns an explicit source-unavailable error (`502 business-source-unavailable`) and **never falsely claims a live-confirmed status**.
- **Known Limitations**:
  - No live inventory, point-of-sale, or payment integration.
  - No appointment booking database.
  - Stateless single-turn interaction (no persistent multi-turn conversational memory or session state).
  - Scope confined to the two configured commercial establishments on Avenida Juárez.

### 4.7 Increment 5.1: Unity 3D Foundation & API Handshake

Establishes the new Unity 3D client foundation and non-blocking HTTP integration with the authoritative Node.js server:

- **Project Location**: `unity-client/` (new, clean Unity 6 project created specifically for this hackathon).
- **Required Unity Editor Version**: **`6000.3.25f1`** (Unity 6 LTS) with Universal Render Pipeline (URP).
- **Architecture**:
  - **Unity 3D Client**: Presentation and interaction layer only. Owns camera, ground plane, visual markers, and UI display. Does not evaluate business logic or invent world facts.
  - **Node.js Assistant Server**: Authoritative backend running on `http://127.0.0.1:3000`. Evaluates operational status, CBP wait-time normalization, and Meta model queries.
  - **C# ApiClient**: Lightweight, non-blocking HTTP service (`Assets/Scripts/ApiClient.cs`) utilizing `UnityWebRequest` to communicate with the local Node.js endpoints.
  - **Strongly Typed DTOs**: Compatible C# data transfer objects (`Assets/Scripts/ApiContracts.cs`) matching the TypeScript contracts for `GET /health` and `GET /api/spatial/corridor`.
- **Foundation Scene (`Assets/Scenes/HandshakeScene.unity`)**:
  - Ground plane (30m × 30m) with dark asphalt styling.
  - Directional light and main camera positioned for a clear perspective view.
  - `HandshakeController` managing immediate-mode UI panel (`OnGUI`) with live status indicators, diagnostic readouts, action buttons, and spatial landmark marker cubes spawned in 3D.
  - Graceful failure handling: Displays informative troubleshooting guidance when the Node.js server is stopped or unreachable, never crashing or throwing unhandled exceptions.
- **How to Run the Handshake Demo**:
  1. **Start the Node.js Server**:
     ```powershell
     npm run dev
     ```
     Confirms: `[Assistant Server] Running at http://127.0.0.1:3000`.
  2. **Open the Unity Project**:
     - Launch **Unity Hub**, click **Add** > **Add project from disk**, and select:
       `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity-AI-Hackathon-Edition\unity-client`
     - Open the project using Editor **`6000.3.25f1`**.
  3. **Play the Scene**:
     - In the Unity Project window, navigate to `Assets/Scenes/` and double-click `HandshakeScene.unity`.
     - Press the **Play** button in the Unity Editor toolbar.
     - On startup, the scene automatically queries `/health` and `/api/spatial/corridor`.
     - The on-screen panel displays `[CONNECTED]`, server health metrics, model information (`muse-spark-1.3`), and 3D colored marker cubes for the corridor landmarks (Puente Paso del Norte, Farmacias Benavides, Óptica Fashion).
     - Test offline resilience: Stop the Node.js server (`Ctrl+C`) and click **Test Handshake** to observe the graceful `[DISCONNECTED]` state with troubleshooting guidance. Restart the server and click **Test Handshake** to re-establish the connection.

### 4.8 Increment 5.2: Playable 3D Border Corridor

Transforms the foundation into a recognizable, explorable 3D vertical slice of Avenida Benito Juárez leading toward the Paso del Norte / Santa Fe pedestrian border crossing in Ciudad Juárez, following the hackathon principle **"Build for your World."**

- **Primary Scene (`Assets/Scenes/BorderCorridorScene.unity`)**:
  - Configured at index `0` of `EditorBuildSettings.scenes` as the default launch scene (`HandshakeScene.unity` at index `1`).
- **Authentic Street & Urban Architecture**:
  - **Avenida Juárez Corridor**: 80m paved asphalt roadway (14m wide) running South-to-North with white broken centerlines, yellow shoulder lines, pedestrian crosswalks, raised concrete curbs, and wide sidewalks (4m wide each).
  - **Farmacias Benavides (East side, Av. Juárez 610)**:
    - Recognizable facade with royal blue canopy fascia, crimson red company accent signage, and illuminated glass display windows.
    - Located at metric coordinates `(X = +10.5, Z = 0)`.
  - **Óptica Fashion (West side, Av. Juárez 859)**:
    - Recognizable storefront with warm cream facade, gold metallic sign banner, and showroom windows across from Farmacias Benavides.
    - Located at metric coordinates `(X = -10.5, Z = +1.5)`.
    - **Authentic Avenue Width**: Physical street crossing distance is **21.05m**, closely matching the authoritative metric corridor distance (**21.92m**) defined in `src/domain/spatial-corridor.ts`.
  - **Surrounding Urban Context**: 4 adjacent mixed-use storefront buildings with authentic northern Mexican stucco tones (warm terracotta, soft ochre, sand, and cream).
  - **Street Furniture & Infrastructure**: Double-arm highway streetlights with warm point illumination along both sidewalks.
- **Paso del Norte Border Approach (North End)**:
  - **Directional Overhead Highway Gantry**: Spans the roadway at `Z = +30` with realistic green overhead highway signage directing travelers:
    - *"MÉXICO - EL PASO / PUENTE INTERNACIONAL PASO DEL NORTE - PEDESTRIANS & VEHICLES"*.
  - **Plaza de Cobro C-51 Toll Canopy**: Mexican toll plaza canopy structure (`Z = +45`) with 4 steel support pillars and teal roof canopy ~45m North of the storefronts.
  - **Pedestrian Staging & Queue Stanchions**: Multi-row queue stanchion barrier poles guiding pedestrians toward the crossing gates.
  - **Puente Internacional Deck**: Bridge roadway deck elevated over the approach leading across toward Texas.

### 4.9 Increment 5.3: Interactive NPC Conversations & Live Border Intelligence

Connects the 3D playable corridor in Unity to our authoritative Node.js backend, allowing travelers to speak directly with simulated inhabitants and inspect official border wait times in real time:

- **Guiding Principle**: **“Build for Your World.”** Brings the physical urban recreation of Avenida Benito Juárez to life with grounded, bilingual virtual characters and official CBP border observations.
- **Integrated Server Endpoints & Canonical NPCs**:
  1. **Storefront Business Attendants (`POST /api/businesses/attendants/chat`)**:
     - **Carlos Méndez** (`carlos-farmacia-attendant`, 💊) at **Farmacias Benavides** (`Av. Juárez 610`): Answers verified operating schedule (Mon–Sat 07:00–22:00, closed Sun) and store location questions. Guardrails strictly refuse medical diagnoses, prescription advice, and live inventory levels.
     - **Valeria Domínguez** (`valeria-optica-attendant`, 👓) at **Óptica Fashion** (`Av. Juárez 859`): Answers verified boutique hours (Mon–Sat 09:00–17:00, closed Sun) and location questions. Strictly refuses live frame catalog inventory and eye exam appointment bookings.
  2. **Synthetic Community Witnesses (`POST /api/border/witnesses/chat`)**:
     - **Mateo Navarro** (`mateo-student`, 🎒) at Pedestrian Queue Station 1 (`Z = 37.5m`): University student commuting to UTEP in El Paso. Shares his wait experience and daily commuter perspective.
     - **Sofia Alarcón** (`sofia-cross-border-retail`, 💼) at Pedestrian Queue Station 2 (`Z = 41.5m`): Cross-border retail worker. Shares her observations on queue flow and travel routines.
     - **Don Héctor Méndez** (`hector-artisan`, 🪵) at Pedestrian Queue Station 3 (`Z = 45.5m`): Long-time artisan selling traditional crafts. Reflects on bridge crossing routines and community memory.
  3. **Border Concierge & Traveler Guidance (`POST /api/border/concierge/chat`)**:
     - **Elena Ramos** (`elena-border-concierge`, 🛂) at the **Border Information Kiosk** (`Z = 40m`): Guides travelers on official wait observations, bridge comparisons (Paso del Norte vs. BOTA), operational hours, and bridge camera access.
  4. **Live Official Border Intelligence (`GET /api/border/wait-times`)**:
     - Rendered inside Elena's kiosk dialogue window.
     - Shows real-time pedestrian wait times for **Paso del Norte** (PDN) and **Bridge of the Americas** (BOTA).
     - Reports port operational status, open lanes, CBP source observation timestamps, and normalized freshness (`🟢 Fresh (<60m)` vs `🟠 Stale`).
     - Includes official bridge comparison statement and a user-triggered `[🔄 Refresh]` button calling `/api/border/wait-times?refresh=true`.
     - Preserves data integrity: `null` or missing wait values are honestly displayed as *"Unavailable / Pending"* and are never fabricated as 0 minutes.
- **In-World Dialogue Panel Features**:
  - **Thread Isolation**: Conversation histories are stored and indexed by unique `characterId`, ensuring responses from one character are never mixed with another.
  - **Visual Provenance Badges**:
    - `🏛️ Official CBP Pedestrian Wait Times (Not AI Generated)`
    - `🛂 AI Border Concierge · Grounded in official CBP observations`
    - `🎒 Synthetic Community Witness · Simulated local scenario`
    - `🏪 AI Storefront Attendant · Grounded in verified World Core snapshot`
  - **Bilingual Support (`EN` | `ES`)**: One-click language toggle updates UI labels, suggested prompt chips, request payload language (`"language": "en" | "es"`), and server response language.
  - **Interactive Usability**: Clickable suggested question chips, text input field, `[Send / Enviar]` button, Enter-to-send key handling, in-flight request disabling, and recoverable error state with `[Retry]`.
- **How to Run & Experience Increment 5.3**:
  1. Start the backend server:
     ```powershell
     npm run dev
     ```
     Verifies: `[Assistant Server] Running at http://127.0.0.1:3000`.
  2. Open the Unity project in Unity Hub with Editor **`6000.3.25f1`**:
     `MyVirtualCommunity-AI-Hackathon-Edition/unity-client`
  3. In `Assets/Scenes/`, open `BorderCorridorScene.unity` and press **Play**.
  4. Explore the 3D corridor:
     - Approach Farmacias Benavides and press `[E]` to talk with **Carlos Méndez**.
     - Cross the avenue to Óptica Fashion and press `[E]` to talk with **Valeria Domínguez**.
     - Walk North toward the international bridge approach to speak with **Mateo**, **Sofia**, or **Don Héctor** in the pedestrian line.
     - Visit the blue municipal kiosk at the crossing gates and press `[E]` to view live **Official Border Intelligence** and speak with **Elena Ramos**.
     - Toggle between English and Spanish using the `[EN | ES]` button in the top HUD or dialogue header.
- **Known Limitations & Scope Boundaries**:
  - Single-turn conversational memory (server evaluates each inquiry statelessly against authoritative domain rules).
  - Standee character representations use stylized geometric primitives with distinctive palette styling and highlight rings rather than rigged skeletal animations.

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
│   ├── border-concierge.ts     # Concierge profile, intent classifier & prompt boundaries (Increment 3)
│   └── business-attendant.ts   # Storefront attendant profiles & boundary engines (Increment 4)
├── ports/             # Boundary contracts
│   ├── world-core-port.ts      # Read-only World Core query interface
│   ├── border-crossing-port.ts # CBP wait time data provider port
│   └── meta-model-port.ts      # Meta Model prompt context & response port (assistant + witnesses + concierge + attendants)
├── adapters/          # Port implementations
│   ├── in-memory-world-core-adapter.ts # Seeded development fixture
│   ├── cbp-border-wait-time-adapter.ts # Official CBP REST API adapter with caching
│   ├── meta-model-api-adapter.ts       # Live Meta Model API adapter (muse-spark-1.3)
│   └── mock-meta-model-adapter.ts      # Offline deterministic mock adapter
├── application/       # Use case orchestration
│   ├── answer-business-hours-query.ts  # End-to-end grounded assistant flow
│   ├── compare-border-wait-times.ts    # Border wait times comparison use case
│   ├── talk-to-synthetic-witness.ts    # Synthetic witness dialogue orchestration
│   ├── ask-border-concierge.ts         # Border Concierge grounded guidance use case
│   └── talk-to-business-attendant.ts   # Storefront attendant grounded interaction use case
├── config/            # Environment parsing & secret masking
│   └── index.ts
└── server/            # HTTP transport
    └── index.ts                # Lightweight REST API server
scripts/
├── run-tests.js       # Cross-platform test runner
└── meta-smoke-test.ts # Dedicated, explicitly invoked live smoke test
tests/                 # 100% offline test suite (326 tests across 75 suites)
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

---

## 8. Increment 5 — Interactive 3D Border Corridor & Windows Release Candidate

### 8.1 Overview
The **Unity 3D Client** (`unity-client/`, built with **Unity 6 (6000.3.25f1)** and Universal Render Pipeline) provides a playable first-person 3D slice of Avenida Benito Juárez leading toward the Paso del Norte / Santa Fe international pedestrian bridge in Ciudad Juárez, Mexico.

It connects to the authoritative Node.js backend (`http://127.0.0.1:3000`) to provide live, grounded conversational AI characters and official border intelligence:
1. **Storefront Business Attendants:**
   - **Carlos Méndez** (`carlos-farmacia-attendant`, 💊) at Farmacias Benavides (`pharmacy:farmacias-benavides`).
   - **Valeria Domínguez** (`valeria-optica-attendant`, 👓) at Óptica Fashion (`optical:optica-fashion`).
2. **Synthetic Community Witnesses (Pedestrian Queue):**
   - **Mateo Navarro** (`mateo-student`, 🎒), bilingual college commuter.
   - **Sofía Alarcón** (`sofia-cross-border-retail`, 💼), cross-border retail worker.
   - **Don Héctor Méndez** (`hector-artisan`, 🪵), historic downtown Juárez artisan.
3. **Elena Ramos Border Concierge & Official CBP Kiosk:**
   - **Elena Ramos** (`elena-border-concierge`, 🛂), virtual border concierge.
   - Real-time CBP pedestrian wait times for **Paso del Norte (Santa Fe)** and **Bridge of the Americas (BOTA)**.
   - Freshness indicators, port operational status, and on-demand refresh.

### 8.2 Windows Standalone Release Candidate
A standalone 64-bit Windows binary is packaged at:
```
unity-client/Builds/Windows/MyVirtualCommunity.exe
```
- **Engine Version:** Unity 6000.3.25f1 StandaloneWindows64
- **Included Scenes:**
  - `Assets/Scenes/BorderCorridorScene.unity` (Build Index 0 — Active Gameplay Scene)
  - `Assets/Scenes/HandshakeScene.unity` (Build Index 1 — Diagnostic Scene)
- **Standalone Build Command:**
  ```powershell
  & "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe" `
    -batchmode -quit `
    -projectPath "unity-client" `
    -executeMethod MyVirtualCommunity.Editor.WindowsStandaloneBuilder.BuildWindowsExecutable `
    -logFile "unity-client/Build.log"
  ```

### 8.3 Character Provenance & Execution Matrix

| Character | Role / Location | API Endpoint | Default Language | Representative Provider | Model vs. Fallback Logic | Data Source |
| :--- | :--- | :--- | :--- | :--- | :--- | :--- |
| **Carlos Méndez** | Attendant · Farmacias Benavides (Av. Juárez 610) | `POST /api/businesses/attendants/chat` | EN / ES | `meta-muse-spark-1.3` (or `deterministic-fallback`) | General/hours queries use Meta Muse Spark. Inventory/prescription queries route deterministically to safeguard medical boundaries. | Grounded in verified business schedule (Mon–Sat 07:00–22:00). In-memory fixture (`InMemoryWorldCoreAdapter`) or remote World Core. |
| **Valeria Domínguez** | Attendant · Óptica Fashion (Av. Juárez 859) | `POST /api/businesses/attendants/chat` | ES / EN | `meta-muse-spark-1.3` (or `deterministic-fallback`) | Boutique hours and location queries synthesized by Meta Muse Spark using verified factual constraints. | Grounded in verified boutique schedule (Mon–Sat 09:00–17:00). In-memory fixture (`InMemoryWorldCoreAdapter`) or remote World Core. |
| **Mateo Navarro** | Student Commuter · Pedestrian Bridge Line | `POST /api/border/witnesses/chat` | EN / ES | `meta-muse-spark-1.3` | Persona-constrained narrative (student carrying backpack/laptop heading to evening classes). | Synthetic witness simulation (`type: "synthetic-witness-simulation"`). Not official CBP data. |
| **Sofía Alarcón** | Retail Worker · Pedestrian Bridge Line | `POST /api/border/witnesses/chat` | ES / EN | `meta-muse-spark-1.3` | Persona-constrained commuter narrative (retail worker commute routine). | Synthetic witness simulation (`type: "synthetic-witness-simulation"`). Not official CBP data. |
| **Don Héctor** | Downtown Artisan · Pedestrian Bridge Line | `POST /api/border/witnesses/chat` | ES / EN | `meta-muse-spark-1.3` | Persona-constrained artisan narrative (historic downtown woodcraft artisan). | Synthetic witness simulation (`type: "synthetic-witness-simulation"`). Not official CBP data. |
| **Elena Ramos** | AI Border Concierge · Official Bridge Kiosk | `POST /api/border/concierge/chat` | EN / ES | `meta-muse-spark-1.3` (with `deterministic-fallback`) | Synthesizes comparative bridge crossing advice strictly from authoritative CBP observations. Deterministic fallback if API offline. | Grounded in official U.S. Customs and Border Protection (CBP) wait times. |

### 8.4 Stateless Conversation Architecture & UI Disclosure
To maintain strict adherence to authoritative factual constraints without risk of compounding conversational drift:
- Each character interaction is evaluated **statelessly**. The backend receives the current user message and evaluates it against authoritative facts.
- The Unity UI explicitly discloses this behavior to users and judges:
  > *"ℹ️ Each inquiry is processed independently."* / *"ℹ️ Cada consulta se procesa de forma independiente."*
- Chat history displayed in the UI is tracked locally on the client for user readability but isolated strictly per-character so that threads never cross-contaminate.

### 8.5 Automated Verification Commands
Two batchmode automated suites validate the client inside Unity without manual GUI interaction:

```powershell
# 1. Physical Environment, Metrics, and Invariant Verification:
& "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "unity-client" `
  -executeMethod MyVirtualCommunity.Editor.CorridorVerificationTest.RunVerification `
  -logFile -

# 2. End-to-End Live HTTP Integration Suite (Requires Assistant Server on :3000):
& "C:\Program Files\Unity\Hub\Editor\6000.3.25f1\Editor\Unity.exe" `
  -batchmode -quit `
  -projectPath "unity-client" `
  -executeMethod MyVirtualCommunity.Editor.EndToEndCorridorTest.RunE2EValidation `
  -logFile -
```

### 8.6 Concise 90–120 Second Demo Walkthrough Script
1. **Spawn (0:00–0:15):** Launch `MyVirtualCommunity.exe`. Player spawns on Avenida Benito Juárez facing North toward the border. Notice HUD showing active server connection (`🟢 Server Connected · Meta Muse Spark 1.3`).
2. **Farmacias Benavides & Carlos Méndez (0:15–0:35):** Walk to the West sidewalk. Approach Carlos (💊). Press `E`. Click quick prompt *"What are your business hours today?"*. Observe grounded answer (07:00–22:00) with `[Meta Muse Spark 1.3]` provenance badge. Ask *"Do you sell antibiotics without prescription?"* and observe the attendant politely enforces safety boundaries. Press `Esc` to close.
3. **Óptica Fashion & Valeria Domínguez (0:35–0:50):** Cross the 22-meter roadway to the East sidewalk. Approach Valeria (👓). Press `E`. Ask in Spanish *"¿Dónde se encuentra ubicada la óptica?"*. Observe response grounding location on Av. Benito Juárez 859 and closing time of 17:00. Press `Esc`.
4. **Pedestrian Queue & Mateo Navarro (0:50–1:10):** Walk North toward the Plaza de Cobro canopy. Approach Mateo (🎒) in the pedestrian line. Press `E`. Ask *"How is the line moving today?"*. Observe simulated commuter perspective clearly tagged with `[Synthetic Witness · Simulation]`. Press `Esc`.
5. **Border Information Kiosk & Elena Ramos (1:10–1:35):** Step forward to the official CBP Kiosk. Approach Elena Ramos (🛂). Press `E`.
   - Inspect the **Official CBP Wait Times** panel: observe Paso del Norte vs. Bridge of the Americas pedestrian delays, freshness tag (`🟢 Fresh (<60m)`), and observation timestamp.
   - Click `🔄 Refresh` to execute an on-demand cache-bypassing live update.
   - Click `[Switch to ES]` to demonstrate full bilingual support.
   - Ask: *"¿Cuál puente me recomiendas para cruzar a pie?"* to receive grounded comparative advice.
6. **Wrap-up (1:35–1:50):** Point out provenance badges on every response bubble, showing complete transparency between Meta AI generation, deterministic safety rules, synthetic traveler testimony, and official CBP observations.

---

## 9. Submission Scope, Attribution & Boundaries

### 9.1 Work Newly Authored for This Hackathon
- **Complete Hexagonal Backend Architecture:** Domain anti-hallucination models, input validators, timezone math (`America/Ciudad_Juarez`), use cases, ports, and adapters.
- **Meta Model API Integration (`MetaModelApiAdapter`):** Native HTTPS client with strict credential masking, usage parsing, and error-handling guardrails.
- **CBP Border Intelligence System (`CbpBorderWaitTimeAdapter`):** Live CBP scraper/parser, multi-port comparison logic, zero-delay prevention, and freshness caching.
- **Storefront Business Attendant System:** Deterministic topic classification, boundary guardrails for medical/inventory refusal, and grounded persona synthesis.
- **Synthetic Community Witnesses:** Profile system and prompt isolation for simulated border commuters.
- **Complete Unity 6 3D Client (`unity-client/`):** Procedural 3D scene construction, player controller, raycast proximity HUD, dialogue system, bilingual localization, REST API client, batchmode test suites, and Windows standalone executable.

### 9.2 Attribution & Separation from Pre-Existing Project
- The pre-existing repository (`MyVirtualCommunity`) served as the conceptual inspiration and world simulation engine (ticks, spatial topology, entity IDs).
- **The pre-existing repository remains completely untouched.**
- Zero files or assets were copied or vendored from the pre-existing project. All code in this repository was newly authored for the hackathon.

### 9.3 Asset Attribution & Licensing
- **3D Geometry:** 100% procedural primitive meshes (cubes, cylinders, planes) constructed via editor scripts.
- **Materials & Shaders:** Standard Universal Render Pipeline (URP) lit/unlit materials generated programmatically.
- **Character Standees & Icons:** Unicode emoji glyphs (`💊`, `👓`, `🛂`, `🎒`, `💼`, `🪵`) rendered via standard Unity UI text components.
- **Zero Third-Party Assets:** No commercial 3D asset packs, textures, or paid audio/character libraries were imported.

### 9.4 Known Limitations
- **Stateless Dialogues:** Inquiries are processed independently without multi-turn context retention, ensuring strict fact grounding and preventing hallucination drift.
- **Pedestrian Scope:** Covers General / Standard pedestrian bridge crossings; vehicular, commercial cargo, and SENTRI lanes are excluded.
- **Read-Only:** The system does not alter real-world business schedules, CBP metrics, or commercial transactions.
