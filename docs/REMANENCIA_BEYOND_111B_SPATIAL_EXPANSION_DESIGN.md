# REMANENCIA — Beyond Room 111-B: Vertical Access, Deeper Dimensional Architecture & Expansion Design

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia** *(overarching dimensional universe — proper name, untranslated in ES/EN)*  
**Current Playable Territory:** **Nivel 0 — Hotel de las Palomas** (`unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`)  
**Baseline Audited:** Branch `product/unity-fellowship-2027`, HEAD `a5091a1239029403cc66e86ec88f9fd8de9027d7` (Implementation 07.13 synchronized; 07.13.1 forensic builder restoration verified)  
**Document Status:** **IMPLEMENTATION 07.14 — ARCHITECTURAL FORENSIC INVESTIGATION & EXPANSION DESIGN (DESIGN-ONLY; NO NEW GEOMETRY, ELEVATORS, STAIRWELLS, OR FLOORS IMPLEMENTED)**

> **Mandatory Status Labels:**  
> Every claim, architectural element, and system in this document is explicitly tagged as one of:
> - **`VERIFIED CURRENT IMPLEMENTATION`** — Existing geometry, `fileID`s, scripts, tests, or rules verified directly in the repository at `a5091a1`.
> - **`PROPOSED DESIGN`** — Candidate architectural, spatial, or systems concepts evaluated for future increments (`07.15+`); **not implemented**.
> - **`RECOMMENDED OPTION`** — The highest-ranked expansion path based on forensic audit, architectural coherence, softlock safety, and low-VRAM feasibility; **pending creator approval**.
> - **`UNRESOLVED CREATOR DECISION`** — Creative, structural, or naming choices requiring explicit creator sign-off before any physical implementation begins.

> **Strict Creative & Factual Boundaries:**  
> - **Remanencia** is the overarching dimensional universe; **Nivel 0 — Hotel de las Palomas** is its first explorable territory. Future territories are **not yet approved or formally named**.
> - The real-world unfinished **Hotel de las Palomas** in Ciudad Juárez serves solely as grounded architectural inspiration. No real-world historical, ownership, or Cobalt-60 claims are made.
> - No imposed protagonist identity, no franchise-derived terminology, no arbitrary enemies/creatures, and zero modifications to `BorderCorridorScene.unity` or the real Avenida Benito Juárez exterior door.

---

## 1. Executive Recommendation *(RECOMMENDED OPTION — PENDING CREATOR APPROVAL)*

Nivel 0 — Hotel de las Palomas (`1,704+ m²` across the `40 m × 24 m` ground core, `32 m × 22 m` North Wing M1, concealed East Wing passage, and Room 111-B) now forms a complete, self-contained first territory with:
- Grounded unfinished northern-Mexican hotel architecture (`101–110`, Reception, Lobby, Courtyard, Banquet Hall, Housekeeping Core),
- Four non-sequential environmental discoveries (`07.6`), early fungal/vegetal ecology (`07.7`), static faunal traces (`07.8`), and two solitary living microfauna encounters (`07.9`),
- Room 111-B (`07.11`–`07.12`) and the permanent **"Check-in Without Conventional Check-out"** entry-only rule (`07.13`), with the Room 111-B **Threshold of Trust** as the sole authorized in-world return route to Avenida Benito Juárez.

After auditing all three candidate expansion paths against actual scene geometry, `fileID`s, colliders, tests, VRAM limits, and narrative continuity:

1. **Primary Recommendation (`#1 — RECOMMENDED OPTION`):** **Option B — The Reserved Northwest Stairwell** opening vertically from the already-reserved `6.16 m × 4.96 m` NW Landing (`X ∈ [-15.87, -9.71], Z ∈ [+28.91, +33.87]`) into an **unfinished upper hotel stratum** *(provisional working label: `Upper Stratum / Piso 2 — Pending Approval`)* that introduces **controlled architectural impossibility on the upper floor itself** (corridor dimensions or room layouts that contradict Nivel 0's footprint below) while keeping `Room 111-B` and the `Threshold of Trust` 100% pure, unobstructed, and continuously reachable down the exact same staircase.
2. **Recommended Second Phase (`#2 — Sequenced Follow-Up`):** **Option A — Vertical Hotel Expansion via the Twin Elevator Core** once the creator approves the **Bay 1 vs. Bay 2** assignment and the upper landing exists to connect to — turning the Passenger Elevator into an earned vertical link between floors and allowing the Unfinished Shaft to be staged with safe, non-lethal visual/maintenance depth.
3. **Recommended Third / Integrated Layer (`#3 — Embedded Inside Upper Stratum or Later Branch`):** **Option C — Non-Euclidean Architectural Extension** applied **inside the newly reached upper territory** (or later via a lateral branch from `Room111B_EastCorridor`'s South wall rather than crowding Room 111-B's quiet Threshold of Trust chamber).

**Why Option B First?** It uses space explicitly reserved since Implementation 07.5, requires **zero edits** to the monolithic South core floor/ceiling slabs (`Backrooms_Floor`/`Backrooms_Ceiling`), keeps **Elevators A & B** untouched pending creator approval, leaves **Room 111-B** emotionally uncluttered, preserves **100% of existing perimeter linecast tests**, guarantees **zero teleport/softlock risk** via continuous `CharacterController` climbing, and naturally sets up both Option A (meeting the Elevator Core on the upper floor) and Option C (impossible upper-floor geometry).

---

## 2. Current Nivel 0 Architectural State *(VERIFIED CURRENT IMPLEMENTATION)*

Audited directly from `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity` (`1,261` GameObjects, `260` `BoxCollider`s, `18` non-shadow-casting `Light`s, `SHA-256: 9ADA5552714F0B950C4D6B171FF40C404509F60A761C222CD8D9C9083C084AB4`) and `BackroomsLevel1Builder.cs` (`8,416` lines, verified intact in 07.13.1):

| Verified Root / Zone | World Bounds (m) | Key Functional Role & Status |
| :--- | :---: | :--- |
| **South Core (`Backrooms_Architecture`)** | `X ∈ [-20.0, +20.0]`, `Z ∈ [-12.0, +12.0]`, `Y ∈ [-50.00, -47.15]` | Entrance Vestibule (`Backrooms_SpawnPoint (17.80, -49.94, 8.80)`, entry-only `Backrooms_ExitDoor_ToStreet (19.86, -50.00, 8.80)`), Main Lobby & Reception, `Hotel_Elevator_Core`, Main Guest Corridor (`101–106`), West Service Wing, Level-Ω Safe (`-16.40, 11.42`), *El Umbral de Lasser* (`-19.70, -8.20`). |
| **North Wing M1 (`Hotel_NorthWing_M1`)** | `X ∈ [-16.0, +16.0]`, `Z ∈ [+12.0, +34.0]`, `Y ∈ [-50.00, -47.15]` | Dual Connections A (`X=+3.8..+7.0`) & B (`X=-12.0..-9.58`), Central Courtyard (`-5..+5, +18..+26`, skylight cap at `Y=-44.80`), East/West Arcades, North Guest Corridor (`-8.6..+16.0, +26.0..+28.8`), Rooms `107–110`, Banquet Hall (`+8.2..+16.0, +12..+26`), Housekeeping Core (`-16.0..-8.6, +12..+28.8`), **NW Stairwell Reserve Landing (`-16.0..-9.6, +28.8..+34.0`)**. |
| **Room 111-B Architecture (`Hotel_Room111B_Anomaly`)** | East Corridor `X ∈ [+16.13, +18.90], Z ∈ [+20.20, +28.68]`; Room 111-B `X ∈ [+19.12, +24.52], Z ∈ [+20.00, +24.80]` | Concealed East opening behind `NorthWing_NorthCorridor_EastOverlookNib` (`X=+16.0, Z=+26.8..+28.4`), dogleg baffle (`1.30 m` clear), East Corridor, `Room111B_Plaque_111B`, and Room 111-B shell (`5.40 × 4.80 m = 25.92 m²`). |
| **Room 111-B Interior & Exit (`Hotel_Room111B_GuestInterior`)** | Inside Room 111-B + North Directory `(9.40, -48.38, 28.68)` + Entrance Glass `(19.79, -50.00, 8.80)` | Public directory listing only `107–110`, prepared modest guest room furniture, **Threshold of Trust** (`Room111B_Threshold_TriggerVolume`, `X ∈ [+24.04, +24.48], Z ∈ [+21.85, +22.95]`), and isolated one-way Avenida Benito Juárez observation glass. |
| **Narrative, Ecology & Microfauna Roots** | `Hotel_Narrative_Evidence`, `Hotel_Early_Ecology`, `Hotel_Faunal_Traces`, `Hotel_FirstLivingMicrofauna` | 4 discoveries (`07.6`), 4 ecology zones (`07.7`), 4 static faunal trace zones (`07.8`), 2 solitary living microfauna crevices (`07.9`, Maintenance & Utility). Zero added `Light` components (`18` total). |

---

## 3. Exact Architectural Forensic Audit *(VERIFIED CURRENT IMPLEMENTATION)*

### A. Verified Hierarchy, `fileID`s & Geometry — Option A Targets (`Hotel_Elevator_Core`)
- **Parent:** `BackroomsLevel1_Root/Backrooms_Architecture/Hotel_GroundFloor_Architecture/Hotel_Elevator_Core` (`GO=331299473`, `TF=331299474`, `35` children; authored in `BackroomsLevel1Builder.cs` during 07.2.1; untouched since).
- **Structural Concrete Shell (all solid `BoxCollider`s):**
  - `Hotel_ElevatorCore_BackWall` (`GO=1951929040`, `pos=(1.12, -48.58, 6.80)`, `scale=(0.24, 2.85, 5.32)`) → inner East face `X = +1.24`.
  - `Hotel_ElevatorCore_SouthWall` (`GO=585933301`, `pos=(2.20, -48.58, 4.32)`, `scale=(0.24, 2.85, 2.52)`) → inner North face `Z = +4.44`.
  - `Hotel_ElevatorCore_NorthWall` (`GO=2097585768`, `pos=(2.20, -48.58, 9.28)`, `scale=(0.24, 2.85, 2.52)`) → inner South face `Z = +9.16`.
  - `FrontPierS` (`GO=1166525667`, `Z=4.60`), `FrontPierMid` (`GO=1160025726`, `Z=6.80`, width `0.73 m`), `FrontPierN` (`GO=139182998`, `Z=9.00`), `FrontHeader` (`GO=1472619301`, `Y=-47.48`, height `0.65 m`).
- **Two Independent Bays:**
  - **Bay 1 (South, center `Z = +5.75`):** `DoorLeafS` (`GO=2004914621`, `Z=5.38`) & `DoorLeafN` (`GO=1307326071`, `Z=6.12`) at `X = +3.14` (`scale=(0.08, 2.20, 0.76)`, solid `BoxCollider`s). Frame opening: `1.50 m` wide (`Z ∈ [+5.00, +6.50]`) × `2.20 m` tall.
  - **Bay 2 (North, center `Z = +7.85`):** `DoorLeafS` (`GO=1255285515`, `Z=7.48`) & `DoorLeafN` (`GO=1590748828`, `Z=8.22`) at `X = +3.14` (`scale=(0.08, 2.20, 0.76)`, solid `BoxCollider`s). Frame opening: `1.50 m` wide (`Z ∈ [+7.10, +8.60]`) × `2.20 m` tall.
  - **Shared Hall Controls & Signage (non-colliding):** `CallPanelPlate` (`GO=1622816346`, `(3.43, -48.82, 6.80)`), `CallButtonUp` (`GO=823213806`), `CallButtonDown` (`GO=1114928462`), `Hotel_Sign_ElevatorsHeader` (`GO=120906857`), `Hotel_Sign_ElevatorNotice` (`GO=2080536146`).
- **Critical Monolithic Slab Constraint:** `Backrooms_Floor` (`GO=1532896222`, `pos=(0,-50.15,0)`, `scale=(40.60,0.30,24.60)`) and `Backrooms_Ceiling` (`GO=322800857`, `pos=(0,-47.05,0)`, `scale=(40.60,0.20,24.60)`) are **single monolithic `BoxCollider` slabs** sealing both elevator shafts top and bottom (`2.85 m` clear vertical height). Currently there is **no internal shaft dividing wall** between Bay 1 and Bay 2 at `Z = +6.80`.
- **Affected Automated Tests:** `RemanenciaHotelArchitectureTests.ElevatorCore_HasTwoIdentifiableClosedBaysAndZeroFallHazards` (raycasts at `Z=5.75` & `Z=7.85` from `X=4.6` asserting solid closed `Hotel_ElevatorBay_*` barriers), `RemanenciaHotelEnvironmentalIdentityTests`, `RemanenciaNorthWingExpansionTests`, `RemanenciaLevel0EarlyEcologyTests`, and `RemanenciaRoom111BArchitectureTests`.

### B. Verified Hierarchy, `fileID`s & Geometry — Option B Targets (`NW Stairwell Reserve`)
- **Parent:** `BackroomsLevel1_Root/Backrooms_Architecture/Hotel_NorthWing_M1/NorthWing_HousekeepingCore` (authored in `BackroomsLevel1Builder.MigrateRemanenciaNorthWingM1InOpenScene`, `L1685–L2140`).
- **Enclosing Walls (all solid `BoxCollider`s):**
  - West: `NorthWing_OuterWall_West` (`GO=703720915`, `X=-16.00`, thick `0.26 m` → inner face `X = -15.87`).
  - East: `Hotel_Room110_WestServiceWall` (`GO=378415139`, `X=-9.60`, thick `0.22 m` → inner face `X = -9.71`).
  - South: `NorthWing_Housekeeping_LinenNorthWall` (`GO=2108350822`, `pos=(-14.00, -48.58, 28.80)`, length `4.11 m` → inner face `Z = +28.91`).
  - North: `NorthWing_OuterWall_North` (`GO=1415277172`, `Z=+34.00`, thick `0.26 m` → inner face `Z = +33.87`).
- **Clear Interior Reserve Dimensions:** **`6.16 m` (E–W) × `4.96 m` (N–S) = `30.55 m²`**, clear height `2.85 m` (`BaseY = -50.00`, `CeilingY = -47.15`).
- **Existing Entry Portal (open, non-colliding trim):** `NorthWing_NWLanding_PortalTransom` (`GO=110400040`, `pos=(-10.80, -47.43, 28.80)`), `PortalTrimW` (`GO=822863149`, `X=-11.96`), `PortalTrimE` (`GO=149578538`, `X=-9.64`) → **`2.26 m` clear width × `2.30 m` clear height** (`3.53×` player capsule `Ø 0.64 m`).
- **Existing Sealed Future Anchor Frame (non-colliding):** `NorthWing_FutureAnchor_NorthFrameW/E/Top` (`GO=1639461332`, `715370877`, `711096133`) centered at `X = -12.80, Z = +33.84` (`2.62 m` clear wide × `2.41 m` high).
- **Modular Ceiling Slab Advantage:** `NorthWing_CeilingSlab_North` (`GO=582163591`, `pos=(0.00, -47.05, 30.15)`, `scale=(32.60, 0.20, 8.30)`) spans strictly `Z ∈ [+26.0, +34.3]` — completely decoupled from the South core ceiling (`Backrooms_Ceiling`), Courtyard lightwell, and Rooms `101–106`! Splitting `NorthWing_CeilingSlab_North` at `X = -9.60` cleanly creates an isolated ceiling cutout exclusively above the NW Stairwell bay (`X ∈ [-15.87, -9.71]`) without touching Rooms `107–110`.
- **Nearby Protected Ecology & Tests:** Room 110's plumbing chase (`Hotel_Room110_ChaseWallE` `GO=71062474` at `X=-7.80`, `NorthWing_Room110_ChaseRiser` `GO=80890420` at `(-9.36, 33.62)`, and 07.7 Zone D `Eco_Room110_ChaseRiser_*` `GO=897459054/942807897`) sit **strictly East** of solid `Hotel_Room110_WestServiceWall` (`X = -9.60`) and are 100% isolated. `RemanenciaNorthWingExpansionTests` verifies `Housekeeping_NW_Landing` walkability to `(-12.80, 31.20)` and solid perimeter linecasts at `(X=0, Z=33..35)` and `(X=-16, Z=20)` — all unaffected by vertical stairwell construction inside the NW Landing!

### C. Verified Hierarchy, `fileID`s & Geometry — Option C Targets (Room 111-B & Perimeter)
- **Parents:** `Hotel_Room111B_Anomaly` (`GO=90613822`, builder `BuildOrUpdateRoom111BAnomaly`, `L3441`) and `Hotel_Room111B_GuestInterior` (`GO=1923166455`, builder `BuildOrUpdateRoom111BGuestInterior`, `L3820`).
- **Four Bounding Walls of Room 111-B (`X ∈ [+19.12, +24.52]`, `Z ∈ [+20.00, +24.80]`):**
  1. **East Wall (`Room111B_Shell_WallEast`, `GO=2028907015`, `X = +24.65`):** **UNAVAILABLE / PROTECTED.** Occupied centrally by `Room111B_ThresholdOfTrust` (`GO=1263079435`) and `Room111B_Threshold_TriggerVolume` (`GO=16005720`, `X ∈ [+24.04, +24.48], Z ∈ [+21.85, +22.95]`), flanked by `EastPilasterS` (`GO=751033213`, `Z=20.90`) and `EastPilasterN` (`GO=778081162`, `Z=23.90`).
  2. **West Wall (`Room111B_DoorWall_South/North`, `GO=790774002 / 1531965703`, `X = +19.01`):** Contains the `111-B` entrance doorway (`Z ∈ [+21.70, +23.10]`). North of the doorway (`Z ∈ [+23.44, +24.76]`) sits `Room111B_Furn_Wardrobe` (`GO=1169616951`, already exhibiting Discrepancy #2 — recessed doors implying depth where `Room111B_EastCorridor` immediately sits behind the `0.22 m` wall). Any West extension cannot exist in Euclidean space without overlapping `Room111B_EastCorridor`; it would strictly require a controlled non-Euclidean spatial transition.
  3. **North Wall (`Room111B_Shell_WallNorth`, `GO=692505506`, `Z = +24.93`, inner `Z = +24.80`):** Desk/Chair/Mirror occupy `X ∈ [+20.92, +22.18]`; Wardrobe occupies `X ∈ [+19.14, +19.78]`. The **NE wall segment `X ∈ [+22.35, +24.30]` (`1.95 m` wide)** is free of furniture and borders an unoccupied exterior void (`X ∈ [+19.1, +36.0], Z ∈ [+25.0, +34.0]`).
  4. **South Wall (`Room111B_Shell_WallSouth`, `GO=834325231`, `Z = +19.87`, inner `Z = +20.00`):** BedsideW / DoubleBed / BedsideE occupy `X ∈ [+20.02, +22.52]`. The **SE wall segment `X ∈ [+22.70, +24.30]` (`1.60 m` wide)** is free of furniture and borders an unoccupied SE pocket (`X ∈ [+16.2, +28.0], Z ∈ [+12.3, +19.8]`).
- **Alternative Lateral Anchor Outside Room 111-B (`Room111B_EastCorr_SouthWall`, `GO=1843612310`, `Z = +20.09`, `X ∈ [+16.13, +18.90]`):** Sits immediately behind `NorthWing_FutureAnchor_EastFrame` (`GO=1661350011/925316367/2041378552`, `X = +15.84, Z ∈ [+17.5, +20.5]`). Opening `Room111B_EastCorr_SouthWall` southward into `Z ∈ [+12.5, +20.0]` (`X ∈ [+16.13, +18.90]` — or extending deeper via a non-Euclidean baffle pocket at `X ≥ +36.0`) avoids modifying Room 111-B's quiet interior and keeps the `Threshold of Trust` 100% isolated, while leaving the Banquet Hall `Z = +20.0` perimeter linecast (`X = 15..17`) solid!

---

## 4. Option A — Vertical Hotel Expansion *(PROPOSED DESIGN — NOT IMPLEMENTED)*

### Concept
Activate vertical hotel circulation connecting Nivel 0 (`Y = -50.00 m`) to a second hotel floor (`Y ≈ -46.40 m`, `3.60 m` floor-to-floor = `2.85 m` clear height + `0.75 m` structural plenum/slab — or a lower subterranean level at `Y ≈ -53.60 m`), centered on the existing twin-bay `Hotel_Elevator_Core` (`X ∈ [+1.00, +3.41], Z ∈ [+4.20, +9.40]`).

### Architectural & Technical Feasibility
- **Clear Interior Dimensions:** `1.86 m` depth (`X ∈ [+1.24, +3.10]`) × `~2.21 m` width per bay once a `0.24 m` concrete shaft divider wall is inserted at `Z = +6.80`. Door openings (`1.50 m × 2.20 m`) comfortably accommodate both a standard passenger elevator cab (`~1.65 m × 1.95 m`) and an unfinished visual/service shaft.
- **Passenger Elevator Path (No Floor/Ceiling Cutout Needed):** An enclosed passenger cab can animate bi-parting doors (`DoorLeafS/N`), close securely around the player, play restrained mechanical vibration/hum, and smoothly translate the player (or additively stream an upper floor scene) without cutting `Backrooms_Floor` or `Backrooms_Ceiling`.
- **Unfinished Shaft Path (Requires Slab Split + Strict Fall Prevention):** Exposing real vertical shaft depth below `Y = -50.00` or above `Y = -47.15` requires splitting the monolithic `40.6 × 24.6 m` `Backrooms_Floor` and/or `Backrooms_Ceiling` slabs around the selected bay's `1.86 × 2.21 m` footprint, adding vertical concrete shaft walls, guide rails, counterweights, and **mandatory physical guard barriers** (waist-high construction barricade / locked maintenance grille / solid invisible safety plane) so accidental lethal falls are **structurally impossible**.
- **Safe Non-Lethal Descent Assessment:** Controlled vertical descent inside an unfinished shaft is technically feasible *only* via an explicit authored interaction (e.g., engaging a fixed concrete-wall service ladder or maintenance scaffold with scripted climb-controller constraints and a solid bottom landing) — **never** via free-fall platforming or arbitrary death states.
- **Dimensional Inconsistency Opportunity:** The passenger elevator indicator panel could display floor buttons that do not match the hotel's unfinished exterior (e.g., `PB`, `2`, `3`, or an unlabeled/scratched button), opening onto a floor whose construction stage is older, cleaner, or spatially larger than Nivel 0.

---

## 5. Option B — The Reserved Northwest Stairwell *(RECOMMENDED OPTION — NOT IMPLEMENTED)*

### Concept
Transform the already-reserved **Northwest Stairwell Landing** (`X ∈ [-15.87, -9.71], Z ∈ [+28.91, +33.87]`, `30.55 m²`) into an **unfinished poured-concrete U-turn (switchback) hotel stairwell** ascending from Nivel 0 (`BaseY = -50.00 m`) through a localized cutout in `NorthWing_CeilingSlab_North` up to an **upper hotel landing** (`Y ≈ -46.40 m`, `3.60 m` floor-to-floor rise).

### Exact Geometry & Capsule Clearance Feasibility
- **Available Reserve Footprint:** `6.16 m (E–W) × 4.96 m (N–S)`.
- **Proposed Switchback Stair Envelope (Fits Comfortably Inside Reserve):**
  - **Entry Vestibule (South-East of Landing):** `X ∈ [-12.00, -9.71], Z ∈ [+28.91, +31.30]` (`2.29 m × 2.39 m` clear landing straight through existing `NorthWing_NWLanding_PortalTrimW/E` at `Z = +28.80`).
  - **Flight 1 (Ascending West → East or South → North along West wall `X ∈ [-15.85, -14.35]`):** Width `1.45 m` (`2.26×` player capsule `Ø 0.64 m`), horizontal run `2.80 m` (`10` risers × `0.18 m` = `+1.80 m` rise → intermediate half-landing at `Y = -48.20 m`).
  - **Mid-Landing (North side, `Z ∈ [+32.30, +33.85]`):** `1.55 m` deep × `3.10 m` wide (`Y = -48.20 m`), passing safely *above* or *clear of* the sealed `NorthWing_FutureAnchor_NorthFrame` (`Z = +33.84`) — or keeping `X ∈ [-14.2, -11.4]` visually framed on the ground floor while Flight 1 climbs South-to-North along `X ∈ [-15.85, -14.40]` and Flight 2 returns North-to-South along `X ∈ [-14.20, -12.75]`!
  - **Flight 2 (Ascending to Upper Floor `Y = -46.40 m`):** Width `1.45 m`, horizontal run `2.80 m` (`10` risers × `0.18 m` = `+1.80 m` rise → upper landing `Y = -46.40 m`), vertical headroom `≥ 2.25 m` at every step (exceeding `1.80 m` player capsule height by `0.45 m`).
  - **Collision Architecture:** Visual poured-concrete step treads (`0.18 m` rise / `0.28 m` run) paired with an invisible smooth angled `BoxCollider` ramp underneath (`slope ≈ 32.7°`, well below `CharacterController.slopeLimit = 45°`) + solid concrete parapet railings (`1.05 m` high) along the open stairwell well so traversal is silky-smooth with zero jitter and **zero fall hazard**.
- **Anomalous Upper-Floor Payoff Without Breaking Ground-Floor Coherence:**
  - On Nivel 0, the stairwell reads like an ordinary unfinished concrete fire/service stair beside Housekeeping and Room 110.
  - Reaching the upper landing (`Y = -46.40 m`), the player can encounter a **subtly impossible upper stratum**: e.g., an upper corridor that continues across the space above the Central Courtyard where Nivel 0's skylight (`Y = -44.80 m`) should physically block it, or an upper wing in a noticeably different construction era (painted plaster, suspended conduit, numbered `201+` or repeating `111-B`-era finishes) — **and** can eventually loop back to the **upper doors of `Hotel_Elevator_Core`**, creating a natural bridge to Option A!
- **Alternative Staging (Inaccessible vs. Traversable):** The stairwell can be phased cleanly in two increments: (1) static unfinished concrete stairwell ascending to a guarded/barricaded upper landing preview, or (2) directly traversable into a compact first upper floor landing + exploratory corridor loop.

---

## 6. Option C — First Non-Euclidean Architectural Extension *(PROPOSED DESIGN — NOT IMPLEMENTED)*

### Concept
Expand Remanencia beyond Nivel 0's visible building footprint via an explicit **spatial contradiction** where interior distance or adjacency violates Euclidean exterior dimensions.

### Evaluated Non-Euclidean Access Candidates
| Candidate Access Point | Coordinates (m) | Spatial Mechanism | Impact on Room 111-B & Threshold of Trust | Technical Risk |
| :--- | :---: | :--- | :--- | :---: |
| **C1 — `Room111B_EastCorridor` South Continuation** *(Best Ground-Floor Option C)* | `Room111B_EastCorr_SouthWall` (`GO=1843612310`, `X ∈ [+16.13, +18.90], Z = +20.09`) extending South into `Z ∈ [+12.5, +20.0]` and/or turning East around a dark L-bend baffle into an isolated pocket (`X ≥ +36.0`) | Continuous physical extension South of the East Corridor (passing directly behind the Banquet Hall's sealed `EastFrame` at `Z = 17.5..20.5`), optionally followed by a hidden 90° L-bend seamless coordinate shift into an impossibly long hotel wing (`40+ m`) | **Zero interference with Room 111-B interior or Threshold of Trust**; Room 111-B stays untouched to the East | Moderate (if using continuous corridor) / High (if adding L-bend seamless teleport) |
| **C2 — Room 111-B North-East or South-East Wall Seam** | NE wall `X ∈ [+22.35, +24.30], Z = +24.80` (`1.95 m` clear) OR SE wall `X ∈ [+22.70, +24.30], Z = +20.00` (`1.60 m` clear) | Continuous physical doorway cut into Room 111-B's North or South plaster wall into the empty void North (`Z > +25.0`) or South (`Z < +19.8`) of Room 111-B | **Moderate–High Narrative Risk:** Adds a second doorway inside Room 111-B right beside the Threshold of Trust, risking visual competition and diluting the quiet focus of the return threshold | Low–Moderate (continuous walk) |
| **C3 — Room 111-B Wardrobe Impossible Depth (`Discrepancy #2`)** | `Room111B_Furn_Wardrobe` (`GO=1169616951`, `X ∈ [+19.14, +19.78], Z ∈ [+23.44, +24.76]`) | Stepping through the wardrobe into a space that physically overlaps `Room111B_EastCorridor` (`X ∈ [+16.13, +18.90]`) — **strictly requires a non-Euclidean teleport transition** | Thematic payoff on 07.12's Discrepancy #2, **but** risks feeling like a fantasy "Narnia wardrobe" trope unless heavily grounded, and requires dark-baffle teleport engineering | High (teleport hysteresis, tone risk) |
| **C4 — Non-Euclidean Upper Floor Reached via Option B Stairwell** *(Recommended Hybrid)* | Upper landing reached physically via NW Stairwell (`Y = -46.40 m`) | Continuous physical stair climb up `3.60 m` into an upper floor whose corridor extends farther than the ground floor permits (e.g., spanning straight across where the courtyard lightwell stands below, or shifting floor-plan proportions) | **Zero interference with Room 111-B or Threshold of Trust**; continuous physical traversal with zero teleport scripts! | **Low** (continuous geometry) |

### Controlled Spatial Transition vs. Continuous Traversal Policy
- **Default Rule:** Prefer **continuous physical traversal** (like Option B's stairwell and C1/C4 continuous extensions) wherever the scene coordinate space permits (`BackroomsLevel1Scene` has vast unused coordinate space at `Y = -46.40 m`, `Z > +34.0 m`, and `X > +25.0 m`).
- **When a Controlled Spatial Transition Is Warranted:** Only when two Euclidean volumes genuinely occupy the same `(X, Y, Z)` coordinates (e.g., an infinitely looping corridor or an interior room larger than its exterior shell). Any such transition must use: (1) a 90° dark architectural baffle hiding both ends from direct sightlines (zero portal-camera VRAM cost), (2) local-offset `CharacterController` translation preserving yaw/pitch and calling `Physics.SyncTransforms()`, (3) directional hysteresis to prevent ping-pong loops, and (4) a guaranteed return trigger back to Nivel 0.

---

## 7. Exact Coordinates & Dimensions Summary for All Viable Access Locations *(VERIFIED + PROPOSED)*

| Access Location | Status | World X (m) | World Z (m) | World Y (m) | Clear Aperture / Volume | Existing Bounding `fileID`s |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| **Elevator Bay 1 (South)** | `VERIFIED` | `+3.14` (doors), `+1.24..+3.10` (shaft) | `+5.00..+6.50` (center `+5.75`) | `-50.00..-47.80` (door) / `-47.15` (ceil) | `1.50 × 2.20 m` opening; `1.86 × 2.21 m` shaft bay | `GO=2004914621`, `1307326071`, `1951929040`, `585933301` |
| **Elevator Bay 2 (North)** | `VERIFIED` | `+3.14` (doors), `+1.24..+3.10` (shaft) | `+7.10..+8.60` (center `+7.85`) | `-50.00..-47.80` (door) / `-47.15` (ceil) | `1.50 × 2.20 m` opening; `1.86 × 2.21 m` shaft bay | `GO=1255285515`, `1590748828`, `1951929040`, `2097585768` |
| **NW Stairwell Reserve Landing** | `VERIFIED` | `-15.87..-9.71` (`6.16 m`) | `+28.91..+33.87` (`4.96 m`) | `-50.00..-47.15` (`2.85 m`) | `30.55 m²` bay; entry portal `2.26 × 2.30 m` at `Z=+28.80` | `GO=703720915`, `378415139`, `2108350822`, `1415277172`, `582163591` |
| **NW North Future Anchor Frame** | `VERIFIED` | `-14.20..-11.40` (center `-12.80`) | `+33.84` (on wall `+34.00`) | `-50.00..-47.54` | `2.62 × 2.41 m` framed bay | `GO=1639461332`, `715370877`, `711096133` |
| **West Future Anchor Frame** | `VERIFIED` | `-15.84` (on wall `-16.00`) | `+19.40..+22.00` (center `+20.70`) | `-50.00..-47.54` | `2.42 × 2.41 m` framed bay | `GO=1494615468`, `670926264`, `836121334` |
| **East Corridor South Wall (C1)** | `VERIFIED` | `+16.13..+18.90` (`2.77 m`) | `+20.09` | `-50.00..-47.15` | Up to `1.60 × 2.20 m` opening into `Z ∈ [+12.5, +20.0]` | `GO=1843612310` (`Room111B_EastCorr_SouthWall`) |
| **Room 111-B NE Wall Clear Span (C2-N)** | `VERIFIED` | `+22.35..+24.30` (`1.95 m`) | `+24.80` (wall `+24.93`) | `-50.00..-47.15` | `1.40 × 2.20 m` max opening clear of Desk & Pilaster | `GO=692505506` (`Room111B_Shell_WallNorth`) |
| **Room 111-B SE Wall Clear Span (C2-S)** | `VERIFIED` | `+22.70..+24.30` (`1.60 m`) | `+20.00` (wall `+19.87`) | `-50.00..-47.15` | `1.20 × 2.20 m` max opening clear of BedsideE & Pilaster | `GO=834325231` (`Room111B_Shell_WallSouth`) |
| **Room 111-B East Wall (PROTECTED)** | `VERIFIED` | `+24.52` (wall `+24.65`) | `+21.75..+23.05` | `-50.00..-47.66` | **RESERVED EXCLUSIVELY FOR THRESHOLD OF TRUST** | `GO=2028907015`, `1263079435`, `16005720` |

---

## 8. Comparative Matrix & Ranking *(PROPOSED DESIGN)*

| Criterion | Option A — Vertical Hotel Expansion (Elevators First) | Option B — Reserved NW Stairwell (+ Anomalous Upper Stratum) | Option C — Non-Euclidean Extension (Around Room 111-B) |
| :--- | :---: | :---: | :---: |
| **1. Architectural Coherence** | High (authentic hotel core, but requires cutting monolithic floor/ceiling for open shaft) | **Highest** (uses pre-reserved `30.55 m²` stairwell bay & modular North ceiling slab) | High for C1 (`EastCorr` South); Moderate for C2/C3 (crowds Room 111-B) |
| **2. Narrative Potential** | High (unfinished shaft vs. impossible floor stops) | **Highest** (bridges Housekeeping/110 → upper floor with impossible layout → later meets Elevators from above) | Very High (direct follow-up to Room 111-B spatial impossibility) |
| **3. Player Exploration Value** | Moderate initially (point-to-point ride rather than physical climb) | **Highest** (continuous physical climb, vertical sightlines, loop potential back toward Elevator Core) | High (surreal discovery, though dead-end risk if not looped) |
| **4. Compatibility with Current Hotel** | Moderate (edits `Hotel_Elevator_Core` & updates `5` elevator-preservation test suites; requires Bay 1/2 approval first) | **Highest** (zero edits to South Core, Elevators, Room 111-B, or Threshold of Trust; preserves all existing perimeter linecasts) | High for C1; Moderate for C2/C3 (alters Room 111-B shell/tests) |
| **5. Future Expansion Flexibility** | Very High (multi-floor hub once floors exist) | **Highest** (unlocks vertical stratum immediately AND prepares upper Elevator landing for Option A!) | High (unbounded if using off-footprint pocket) |
| **6. Implementation Complexity** | **High** (cab controller, door animation, floor UI, shaft slab cuts, safety barriers, additive streaming) | **Low–Moderate** (modular concrete stair geometry + smooth ramp collider + localized North ceiling split) | Moderate (C1 continuous) to **High** (baffle teleport + hysteresis + cross-seam tests) |
| **7. Collision & Softlock Safety** | Moderate (elevator transit containment + shaft fall prevention required) | **Highest** (continuous walkable stair + solid parapets; always walk straight back down to Nivel 0 & Room 111-B!) | High (C1 continuous) / Moderate (teleport triggers) |
| **2 GB VRAM / Rendering Cost** | Low–Moderate (animated doors + upper floor lighting budget) | **Low** (reuses existing concrete/plaster materials; zero portal cameras; strict `Light` budget discipline) | Low (continuous) / Moderate (if extra lights/pockets added) |
| **Overall Ranking** | **#2 — Ideal Second Vertical Increment** | **#1 — RECOMMENDED FIRST EXPANSION** | **#3 (Standalone at 111-B) / Integrated into Option B's Upper Stratum** |

---

## 9. The Two Elevators — Configuration Trade-Off Investigation *(UNRESOLVED CREATOR DECISION)*

Both bays inside `Hotel_Elevator_Core` (`X ∈ [+1.00, +3.41], Z ∈ [+4.20, +9.40]`) remain strictly independent and closed in `a5091a1`. Neither configuration is approved yet.

### Configuration 1 — Bay 1 South (`Z = +5.75`) = Passenger Elevator | Bay 2 North (`Z = +7.85`) = Unfinished Shaft
- **Architectural Logic:** Bay 1 aligns directly with the Main Lobby center and South Guest Corridor (`Z = -1.8..+1.4`), giving the primary working elevator an intuitive public-facing position. Bay 2 abuts the North Luggage/Service Alcove (`Z = +9.4..+12.0`), high electrical junction box (`Z = +9.05`), and North Wing M1 service seam — making exposed construction cabling, shaft barricades, and incomplete masonry feel spatially organic.
- **What the Player Sees from Nivel 0:** Standing in the Main Lobby, the player sees intact closed/operational stainless passenger doors on the left (Bay 1) and partially ajar or barricaded doors revealing raw concrete shaft walls, vertical guide rails, and hanging cables on the right (Bay 2), slightly offset from the direct `Spawn (17.8, 8.8)` → Lobby arrival sightline (`Z = +8.8` passes just north of Bay 2's North jamb at `Z = +8.6`).
- **Future Interaction & Floor Connectivity:** Bay 1 provides clean multi-floor transport (e.g., Nivel 0 ↔ Upper Floor ↔ deeper future floors via interior button panel). Bay 2 serves as a visual vertical landmark and optional late-game controlled maintenance shaft (fixed ladder/scaffold only).
- **Trade-Offs:** Visually cleaner and safer for lobby flow, but makes the dramatic unfinished shaft slightly less immediately prominent when first entering from the Main Guest Corridor.

### Configuration 2 — Bay 1 South (`Z = +5.75`) = Unfinished Shaft | Bay 2 North (`Z = +7.85`) = Passenger Elevator
- **Architectural Logic:** Places the operational Passenger Elevator (Bay 2, `Z = +7.85`) directly along the player's primary arrival sightline from `Backrooms_SpawnPoint (17.80, 8.80)` and right beside Connection A into North Wing M1 (`X = +3.8..+7.0, Z = +12.0`), turning Bay 2 into the natural North-Central vertical hub. Places the unfinished shaft (Bay 1, `Z = +5.75`) adjacent to the older South Guest Corridor (`101–106`), visibly anchoring the building's incomplete construction right at the Lobby-to-Corridor transition.
- **What the Player Sees from Nivel 0:** Players walking from the Entrance Vestibule straight West see the functioning Passenger Elevator bay first (Bay 2), then immediately discover the unfinished/barricaded shaft (Bay 1) as they turn South toward Rooms `101–106`.
- **Future Interaction & Floor Connectivity:** Same functional separation as Configuration 1, with Bay 2 serving as primary vertical cab and Bay 1 as guarded unfinished shaft.
- **Trade-Offs:** Maximizes early architectural mystery right beside the Main Guest Corridor, **but** places the open/barricaded shaft along a busier circulation turn (requiring sturdier physical construction barriers so players cannot clip or feel snagged while rounding the corner from the Lobby to the Main Corridor).

### Shared Safety & Behavioral Rules (Apply to BOTH Configurations)
1. **Never Auto-Move Without Player Agency:** The passenger elevator must **never** unexpectedly abduct the player or depart automatically upon stepping inside; doors open/close and vertical transit starts only on deliberate player interaction (button press / call action).
2. **Zero Arbitrary Fall Deaths:** The unfinished shaft must remain physically blocked by solid construction barricades / safety rails unless and until an explicit, constrained vertical-traversal mechanic (fixed ladder / maintenance platform with a solid bottom landing) is designed, implemented, and tested.
3. **Internal Divider Required:** Regardless of configuration, an internal concrete dividing wall (`0.24 m` thick at `Z = +6.80`) must be installed between Bay 1 and Bay 2 before either bay opens.
4. **Independent of Surface Exit:** Neither elevator may replace the Room 111-B Threshold of Trust as the surface-world return unless separately approved.

---

## 10. Recommended First Expansion & Future World Structure *(PROPOSED DESIGN)*

### World Taxonomy Principle (Scoped to Next Expansion Only)
- **Remanencia:** The overarching dimensional universe formed from accumulated memories, rumors, and traces of places *(approved proper name — never translated)*.
- **Nivel 0 — Hotel de las Palomas:** The ground-level territory already implemented (`a5091a1`).
- **Next Expansion Territory *(Provisional Working Label Only — UNRESOLVED CREATOR DECISION)*:** Keep it architecturally part of **Hotel de las Palomas** as an **upper unfinished hotel stratum** (provisional working label: *Upper Stratum / Nivel 0 — Piso Superior* or *Nivel 1 — pending creator naming*), proving that the hotel itself extends vertically and impossibly before introducing an unrelated Ciudad Juárez building fragment later (via West Anchor Frame M3 or *El Umbral de Lasser*). Avoid generic franchise numbering clichés ("Level 1: Habitable Zone", etc.); let local architectural language (`Planta Baja`, `Segundo Nivel / Obra Alta`, or creator-approved original names) govern signage.

### Recommended Architectural Arc (`Option B → Option C Inside Upper Stratum → Option A Elevator Link`)
1. **Climb the NW Stairwell (Option B):** Unlocks continuous vertical exploration from Nivel 0's Housekeeping/North Corridor corner (`X ∈ [-15.87, -9.71], Z ∈ [+28.91, +33.87]`) up to `Y = -46.40 m`.
2. **Encounter Architectural Impossibility Above (Option C Principles, Zero Teleport):** The upper floor shares the hotel's unfinished concrete/plaster language but violates Nivel 0's geometry — for example, a long unfinished guest corridor (`201–208` or inconsistent room numbers) extending past where Nivel 0's perimeter or courtyard skylight ends, or rooms whose interior states shift across construction eras.
3. **Meet the Elevator Core from Above (Bridging to Option A):** Exploring the upper stratum eventually brings the player to the upper landing of `Hotel_Elevator_Core` (`X ≈ +3.4, Z ∈ [+4.2, +9.4]`, at `Y = -46.40 m`), naturally motivating a future Elevator Activation increment once the creator chooses Configuration 1 vs. Configuration 2.

---

## 11. Player Discovery Sequence *(PROPOSED DESIGN)*

1. **Established Context (Nivel 0):** The player enters Hotel de las Palomas, realizes the interior front door won't yield (`07.13`), explores `101–110`, reads the four `07.6` clues, finds Room 111-B, and learns that the Threshold of Trust provides a reliable way back to Avenida Benito Juárez.
2. **Curiosity Beyond Return:** Once the player knows they are not trapped and have mastered Nivel 0, exploration naturally turns back to the hotel's unsolved architectural seams — specifically the **Northwest Landing** beyond Housekeeping and Room 110 (`X = -10.8, Z = +28.8`), where the concrete stairwell rises out of the shadows into unfinished upper formwork.
3. **Ascent:** Walking continuously up the poured-concrete switchback staircase (with subtle vertical light shift, echoing stillness, and zero popups or forced cutscenes) leads onto the upper landing.
4. **First Upper-Floor Contradiction:** Looking down a corridor that extends farther than Nivel 0's ground plan allows — or glimpsing the sealed upper elevator doors from the second floor — silently communicates that **Hotel de las Palomas** is deeper and stranger than its ground floor alone.
5. **Reversible Exploration:** At any moment, the player can walk straight back down the NW Stairwell to Nivel 0, follow the North Guest Corridor East into Room 111-B, and step through the Threshold of Trust back to Avenida Benito Juárez.

---

## 12. Continuity with Entry-Only Rule & Threshold of Trust *(MANDATORY INVARIANTS)*

- **Entry-Only Entrance Preserved:** `Backrooms_ExitDoor_ToStreet` remains permanently entry-only (with zero proximity prompt under 07.14 Annex A and restrained `"La puerta no cede."` / `"The door won't budge."` feedback only when explicitly tried).
- **Threshold of Trust Preserved Untouched:** `Room111B_Threshold_TriggerVolume` (`"threshold-of-trust"`) remains the sole authorized in-world return route to Avenida Benito Juárez (`(-11.20, 0.24, -11.99)`). Neither its geometry, trigger bounds, nor `SecretBackroomsTransitionController.IsAuthorizedInWorldExitSource(...)` authorization changes.
- **No Imposed Protagonist & Gradual Ecology:** No protagonist biography is added. No monsters, pursuers, or Cobalt-60 biological explanations are added merely to populate upper floors; future ecology remains an independent, creator-approved design track.

---

## 13. Anti-Softlock, Lighting Budget & Progression Considerations *(PROPOSED SAFEGUARDS)*

1. **Mandatory Bidirectional Return Path:** Every future upper floor or non-Euclidean wing must preserve a **continuous, zero-puzzle-lock return path** back to Nivel 0's North Guest Corridor and Room 111-B's Threshold of Trust, verified by automated `Physics.OverlapCapsule` (`r = 0.32 m, h = 1.80 m`) EditMode polyline proofs from `Backrooms_SpawnPoint`, all `10` Azucenas Manhole drop zones, AND the furthest point of the new expansion.
2. **Save & Restart Recovery:** `ProgressionSaveData.schemaVersion == 1` stays backward-compatible and decoupled from world spawn transforms — quitting from an upper floor still recovers cleanly on Avenida Benito Juárez with zero lost discoveries.
3. **2 GB VRAM & URP Performance Discipline:**
   - Keep `LightShadows.None` on all Remanencia lights; prefer emissive-only ceiling strips and shared modular concrete/plaster materials (`BackroomsLevel1Scene` currently has `18` lights).
   - For a full second floor, evaluate either a compact upper wing (`≤ 4–6` non-shadow point lights + distance/zone light culling) or additive sub-scene streaming (`BackroomsLevel1_UpperStratum.unity`) once horizontal extent grows beyond `~400–600 m²`.

---

## 14. Surface-World Preservation Constraints *(PERMANENT NON-NEGOTIABLE RULE)*

- `unity-client/Assets/Scenes/BorderCorridorScene.unity` (`SHA-256: 0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B`), `BuildingA_SouthBay_StorageDoor`, all `8` exterior door materials, `SecretKeypadDoorController.cs`, surface-world geography, both entry routes (Threshold Veil & `manhole_juarez_azucenas`), and the isolated one-way observation view **must remain 100% untouched** across all `07.14+` expansions.
- All vertical and impossible hotel architecture lives strictly inside Remanencia's own scenes (`Y ≤ -20 m`, isolated vertically from the surface corridor at `Y = 0 m`) and Remanencia-scoped assets/scripts.

---

## 15. Proposed Future Implementation Increments *(PROPOSED DESIGN — NOT IMPLEMENTED)*

| Increment | Scope | Key Prerequisites / Gates |
| :---: | :--- | :--- |
| **07.15** | **NW Concrete Stairwell Construction & Upper Landing Shell (Option B)**<br>Split `NorthWing_CeilingSlab_North` strictly above the NW Landing (`X ∈ [-15.87, -9.71]`), build the unfinished switchback concrete stairwell (`Y = -50.00 → -46.40 m`) with smooth ramp colliders & solid parapets, and construct a compact sealed upper landing vestibule (zero fall hazards, 100% reversible climb). | Creator approval of **Option B** as first expansion and floor-to-floor height (`3.60 m`). |
| **07.16** | **First Upper Hotel Stratum & Controlled Spatial Impossibility (Option B + C4)**<br>Expand outward from the upper NW landing into an unfinished upper corridor + select rooms exhibiting subtle spatial contradictions relative to Nivel 0, looping toward the upper Elevator Core landing (`X ≈ +3.4, Z ∈ [+4.2, +9.4]`). | Creator approval of upper-floor naming/tone and PlayMode sign-off on 07.15 stairwell feel. |
| **07.17** | **Elevator Core Internal Separation & Visual Differentiation (Option A — Phase 1)**<br>Insert internal shaft divider at `Z = +6.80`; visually differentiate the approved Passenger Elevator bay vs. guarded Unfinished Shaft bay on Nivel 0 and the upper floor (still zero fall hazards). | Explicit creator selection of **Elevator Configuration 1 vs. Configuration 2**. |
| **07.18** | **Functional Passenger Elevator & Optional Controlled Shaft Inspection (Option A — Phase 2)**<br>Implement player-initiated `ElevatorController` (cab interior, bi-parting doors, bilingual floor selector, deterministic vertical transit) connecting Nivel 0 and the upper stratum. | PlayMode sign-off on 07.16 & 07.17. |

---

## 16. Unresolved Creator Decisions *(UNRESOLVED CREATOR DECISION)*

Before any physical expansion geometry is authored in `07.15+`, the creator's explicit decision is required on:

1. **First Expansion Choice:** Approve **Option B — Reserved Northwest Stairwell** (`#1 Recommended`) as the first vertical expansion beyond Room 111-B, OR select **Option A (Elevators First)** or **Option C (Lateral Non-Euclidean Extension around `Room111B_EastCorridor` South wall)** instead.
2. **Elevator Bay Assignment (`Hotel_Elevator_Core`):** Choose **Configuration 1** (`Bay 1 South = Passenger Elevator`, `Bay 2 North = Unfinished Shaft`) vs. **Configuration 2** (`Bay 1 South = Unfinished Shaft`, `Bay 2 North = Passenger Elevator`), or defer elevator assignment until the upper landing is built.
3. **Unfinished Shaft Role:** Decide whether the unfinished elevator shaft should remain **permanently non-traversable visual/environmental storytelling** (guarded ajar door peering into cables/shaft) OR eventually support a **constrained maintenance ladder/scaffold descent** in a later increment.
4. **Upper Territory Naming & Taxonomy:** Decide whether the upper floor reached from Hotel de las Palomas should be labeled as part of **Nivel 0** (e.g., *Piso 2 / Obra Alta* within Hotel de las Palomas), designated as **Nivel 1**, or given an original creator-chosen territory name.
5. **Architectural Tone of the Next Stratum:** Decide whether the upper hotel floor should feel *less finished* than Nivel 0 (raw exposed concrete/rebar/sky-haze), *eerily more finished* (continuing Room 111-B's anachronistic prepared-hotel contrast), or *mixed-era* (finished corridor abruptly terminating in unfinished formwork).
