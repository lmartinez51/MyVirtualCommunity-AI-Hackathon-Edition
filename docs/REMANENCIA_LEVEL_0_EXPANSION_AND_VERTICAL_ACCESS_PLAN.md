# REMANENCIA — Nivel 0: Horizontal Expansion & Vertical Access Plan

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia**  
**Territory:** **Nivel 0 — Hotel de las Palomas**  
**Target Scene:** `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`  
**Document Status:** **PROPOSED DESIGN & ARCHITECTURAL PLAN — NOT IMPLEMENTED**

> **Implementation Boundary Notice:** Every horizontal expansion module, pursuit/escape route, hiding architecture, and elevator mechanic described in Sections 2–12 of this document is **PROPOSED ONLY** and is **NOT implemented** in the current Unity scene or codebase. No horizontal expansion, additional hotel floors, functioning elevators, vertical shaft access, enemies, stealth systems, or biological/radiation gameplay exist in this increment.

> **Factual & Historical Boundary:** All proposed dimensions and room layouts below are **game-design estimates** derived from the existing Unity scene scale (`40 m × 24 m`). They do **not** claim to reproduce the exact dimensions, floor plan, or present-day physical state of the real-world Hotel de las Palomas in Ciudad Juárez.

---

## 1. Existing Hotel Geometry Baseline (`IMPLEMENTED` as of 07.2.1 / 07.3)

The current playable ground floor of **Nivel 0 — Hotel de las Palomas** occupies a single-story `40 m × 24 m` (`960 m²`) footprint at base elevation `Y = -50.00 m`, clear height `2.85 m` (`CeilingY = -47.15 m`), spanning `X ∈ [-20.0, +20.0]` and `Z ∈ [-12.0, +12.0]`.

| Existing Zone (`IMPLEMENTED`) | X Range (m) | Z Range (m) | Current Role & Immutable Anchors |
| :--- | :---: | :---: | :--- |
| **Entrance Vestibule** | `+16.2 .. +20.0` | `+5.4 .. +12.0` | `Backrooms_SpawnPoint (17.80, 8.80)`, `Backrooms_ExitDoor_ToStreet (19.86, 8.80)`, `HOTEL DE LAS PALOMAS` marquee |
| **Main Lobby & Reception** | `+3.4 .. +16.2` *(+SE Foyer `+10.2..+20.0, Z=-4.4..+5.4`)* | `+1.4 .. +12.0` | Open public hall, North Reception alcove (`RECEPCIÓN / RECEPTION`), `SealedNotebook`, `StepLadder`, `FmRadioTable`, `FireExtinguisher` |
| **Twin Elevator Core & Waiting Bay** | Core `+1.0 .. +3.4` / Bay `+3.4 .. +6.2` | `+4.2 .. +9.4` | Enclosed concrete shaft, 2 closed bi-parting metal bays (`Z=5.75`, `Z=7.85`), `ELEVADORES / ELEVATORS`, `FUERA DE SERVICIO / OUT OF SERVICE`, North Luggage Alcove (`ArchiveCase06`) |
| **Double-Loaded Guest Corridor** | `-9.5 .. +10.2` | `-1.8 .. +1.4` | `~3.0 m` wide circulation spine, engaged structural columns, overhead soffit beams & cable tray, `ServiceIntercom`, `AnomalousCompass`, Front-Office Alcove (`LaptopWorkstation`) |
| **Guest Rooms `101`–`106`** | `101: -3.6..+1.0`, `102: -9.5..-3.6`, `103: +12.6..+20.0`, `104: +7.8..+12.6`, `105: +1.4..+7.8`, `106: -5.6..+1.4` | North `+1.4..+12.0` / South `-12.0..-1.8` | 6 empty unfinished guest-room shells with framed doorways, room plaques `101`–`106`, interior bath/foyer partitions, and preserved artifacts (`SpecimenVitrine`, `ShiftClock0307`, `EvidenceCorkboard`, `CoordinatePlottingPlate`, `FracturedInsulator`, `ThermalPrinter`, `PayphoneTerminal`) |
| **Back-of-House Service Wing** | `-20.0 .. -5.6` | `-12.0 .. +12.0` | `SOLO PERSONAL / STAFF ONLY` bulkhead (`X=-9.5`), N–S Service Corridor (`X=-12.5..-9.5`), West Utility Room (`PaquimeMask`, `SignalMonitor`, `CctvPhotoFrame`), SW Maintenance Hall (`AlexaCornerStand`, `BorderClockTerminal`, `SealedCustodyCase`) |
| **NW Level-Ω Vault Safe Room** | `-20.0 .. -12.5` | `+4.8 .. +12.0` | `SecretRoom_HeavyVaultSafe (-16.4, 11.42)` (`654321`), `El Ojo del Umbral`, `AnomalousGeometryAtlas`, `FoldedTopologyCore` |
| **SW Dimensional Passage Chamber** | `-20.0 .. -12.5` | `-12.0 .. -4.0` | `Portal_DimensionalMirror_Root (-19.7, -8.2)` (**Pasaje dimensional / Dimensional Passage**) |

**Core Limitation for Future Pursuit Gameplay:** At `960 m²` with a single primary East–West guest corridor and single-exit rooms, a future pursuing threat could corner the player quickly in the West service chambers or inside single-door guest rooms (`101`–`106`). Horizontal modular expansion directly addresses this before multi-floor vertical traversal is required.

---

## 2. Horizontal Expansion Opportunities (Perimeter Seam Evaluation)

| Perimeter | Boundary Coordinate | Open Existing Seam(s) | Nearby Gameplay Anchors | Assessment |
| :--- | :---: | :--- | :--- | :--- |
| **North** | `Z = +12.0` | **Two** open seams: Lobby NW Bay (`X = +3.6..+7.2`) and N–S Service Corridor North (`X = -12.4..-9.6`), plus Luggage Alcove (`X = +1.0..+3.4`) | `ArchiveCase06` at `(1.85, 10.9)`; `Safe` safely isolated West of `X = -12.5`; Reception safely East of `X = +7.4` | **BEST FIRST EXPANSION SEAM.** Dual connection points naturally form a full hotel-wide **circulation loop** connecting Public Lobby ↔ North Wing ↔ Service Wing with zero interference to Spawn, Exit, Safe, or Portal. |
| **South** | `Z = -12.0` | One service seam: SW Maintenance Hall (`X = -12.4..-5.8`); Rooms `103`–`106` occupy `X = -5.6..+20.0` | `SealedCustodyCase (-8.4, -10.4)`, `EvidenceCorkboard (4.0, -11.78)` on South outer wall of Room 105 | **Strong Phase-2 Service/Parking Expansion.** Ideal for a future unfinished parking deck / loading dock / ramp; requires a secondary East service stair or cross-passage to avoid creating a single-entry pocket. |
| **West** | `X = -20.0` | One utility seam: West Mechanical Room (`Z = -3.8..+4.6`); NW Vault (`Z = +4.8..+12.0`) and SW Portal (`Z = -12.0..-4.0`) occupy corners | `PaquimeMask (-18.98, 2.6)` & `SignalMonitor (-18.88, -1.6)` sit near West wall (`X = -19.0`) | **Strong Dimensional Connector Seam.** Best reserved for a future dimensional service corridor linking Hotel de las Palomas to an adjacent building in Remanencia, keeping `PaquimeMask` & `SignalMonitor` safely off to the flanks of a centered portal opening (`Z = -0.8..+1.8`). |
| **East** | `X = +20.0` | SE Lobby Foyer (`Z = -4.0..+4.8`); Entrance/Exit occupies `Z = +5.4..+12.0` | `Backrooms_ExitDoor_ToStreet (19.86, 8.80)` & `Backrooms_SpawnPoint (17.80, 8.80)` | **Secondary Public Expansion.** Opening `Z = -3.6..+4.4` could add a future unfinished storefront arcade, veranda, or secondary lobby wing without touching the NE Exit Door, though it does not create a West-side loop by itself. |

---

## 3. Recommended First Expansion Direction (`PROPOSED — NOT IMPLEMENTED`)

### Recommended Module 1: **North Hotel Wing, Secondary Guest Corridor & Unfinished Courtyard Loop**
- **Boundary:** North perimeter `Z = +12.0`, expanding northward into `Z = +12.0 .. +34.0` (`~22 m` depth, game-design estimate: `20–24 m` range) across `X = -14.0 .. +14.0` (`~28 m` width, `~616 m²` additional playable area → expanding Nivel 0 from `960 m²` to **`~1,576 m²`**).
- **Why North Is the Best First Expansion:**
  1. **Dual Natural Anchor Seams:** Connects simultaneously to the **Main Lobby NW (`X = +3.6..+7.2, Z = +12.0`)** AND the **North end of the N–S Service Corridor (`X = -12.4..-9.6, Z = +12.0`)**. Opening just those two perimeter wall segments creates a complete **macro pursuit loop** around the entire hotel core without moving a single existing artifact or wall!
  2. **Zero Disruption to Core Slice 01 Flow:** Spawn (`17.8, 8.8`), Exit Door (`19.86, 8.8`), Reception (`7.4..14.2`), Elevators (`1.0..3.4`), Level-Ω Safe (`-16.4, 11.42`), and Dimensional Passage (`-19.7, -8.2`) stay 100% untouched.
  3. **Architectural Variety:** Supports an unfinished **interior service/light courtyard** (walled open-air/skylight concrete shaft with structural pillars and broken sightlines), a **North Guest Wing (`Rooms 107–110`)** including at least two **connecting / dual-doorway suites** for escape routing, and a **Housekeeping / Linen & Laundry Service Corridor** looping straight back to the West Service Wing!
  4. **Stairwell Core Anchor:** Provides clean space for a future **North Concrete Emergency Stairwell Core** (`X = -14.0..-10.0, Z = +28.0..+34.0`) that naturally prepares multi-floor vertical circulation alongside Elevator A & B.

---

## 4. Alternative Expansion Concepts (`PROPOSED — NOT IMPLEMENTED`)

### Alternative Concept A — **South Unfinished Parking Structure & Loading Dock (`Z = -12.0 .. -34.0`)**
- **Concept:** Open `Z = -12.0` at the SW Maintenance Hall (`X = -12.4..-5.8`) and optionally behind Room 103 (`X = +16.0..+19.0`) into a `32 m × 22 m` (`~700 m²` estimate) unfinished subterranean/ground-level concrete parking deck, vehicle ramp stub, loading bay, and boiler/generator rooms.
- **Advantages:** Dramatically different spatial scale (wide columned concrete parking bays for tense open-space evasion and long sightlines); reinforces the unfinished northern Mexican hotel construction theme; strong natural location for future vehicles, construction barriers, and service equipment.
- **Disadvantages:** Only one clean existing service seam at `Z = -12.0` without cutting through Guest Room `103` or `105`; risks feeling like an isolated pocket unless paired with an East service bypass; moves exploration further away from the Reception/Elevator hub.

### Alternative Concept B — **West Mechanical Annex & Dimensional Building Connector (`X = -20.0 .. -42.0`)**
- **Concept:** Open `X = -20.0` at the West Mechanical Room (`Z = -0.8..+1.8`) into a `22 m × 24 m` (`~528 m²` estimate) heavy back-of-house plant (chiller room, unfinished laundry hall, electrical vault) terminating in a distorted **Remanencia dimensional corridor** connecting into a second unrelated Ciudad Juárez building fragment.
- **Advantages:** Directly expresses Remanencia's core lore pillar (*impossible spatial connections between unrelated places*); concentrates eerie industrial mood near the existing Vault and Dimensional Passage.
- **Disadvantages:** Single choke-point entrance at `X = -20.0` unless looped north/south around the Vault & Portal chambers; narrows sightlines and places the expansion deeper behind the service wing rather than expanding the recognizable hotel itself first.

---

## 5. Proposed Modular Dimensions, Coordinate Ranges & Floor-Plan Concept

> *All proposed expansion coordinates and dimensions below are **game-design estimates (`PROPOSED — NOT IMPLEMENTED`)**, aligned to the existing Unity coordinate grid (`BaseY = -50.00 m`, `ClearHeight = 2.85 m`).*

| Module (`PROPOSED`) | Priority | Est. Dimensions (m) | Est. Coordinate Range | Est. Area | Connection Seam(s) |
| :--- | :---: | :---: | :---: | :---: | :--- |
| **M1 — North Hotel Wing & Courtyard Loop** *(Recommended First)* | 1 | `26–30 m × 20–24 m` | `X ∈ [-14, +14]`, `Z ∈ [+12, +34]` | `~560–680 m²` | Lobby NW (`X=+3.6..+7.2, Z=+12`) + Service N (`X=-12.4..-9.6, Z=+12`) |
| **M2 — South Parking & Loading Wing** *(Future Phase)* | 2 | `28–34 m × 18–24 m` | `X ∈ [-16, +16]`, `Z ∈ [-34, -12]` | `~550–750 m²` | SW Maintenance (`X=-12.4..-5.8, Z=-12`) + future East Service Bypass |
| **M3 — West Plant & Dimensional Connector** *(Future Phase)* | 3 | `18–24 m × 18–24 m` | `X ∈ [-42, -20]`, `Z ∈ [-10, +10]` | `~360–520 m²` | West Utility (`X=-20, Z=-0.8..+1.8`) |
| **M4 — East Public Arcade / Frontage** *(Optional)* | 4 | `14–18 m × 12–16 m` | `X ∈ [+20, +36]`, `Z ∈ [-6, +6]` | `~180–260 m²` | SE Lobby Foyer (`X=+20, Z=-3.6..+4.4`) |

### Top-Down Concept Diagram (`EXISTING` vs. `PROPOSED — NOT IMPLEMENTED`)
```text
                        [PROPOSED M1 — NORTH HOTEL WING & COURTYARD LOOP]
                        (Est. X=-14..+14, Z=+12..+34 — NOT IMPLEMENTED)
Z=+34        +---------+-------------------+------------------+-----------+
             | [STAIR  | [ROOM 109]  <===> | [ROOM 110]       | [NORTH    |
             |  CORE]  | (Dual-Exit Suite) | (Balcony Recess) |  LOUNGE]  |
Z=+26        +---   ---+----   ------------+------   ---------+---   -----+
             | [NORTH  | === NORTH GUEST CORRIDOR (Escape Loop) ===       |
             | SERVICE |     +--------------------------+                 |
             | LAUNDRY]|     | [UNFINISHED INTERIOR     | [ROOM 108]      |
             |         |     |  COURTYARD / LIGHTWELL]  |   <==>          |
             | [Hiding |     |  (Columns / Sight Breaks)| [ROOM 107]      |
             |  Alcove]|     +--------------------------+                 |
Z=+12 ====== +==[S1]===+===========+=======+===[S2]=====+=================+=====
             | NW Safe | S | RM102 | RM101 |Lug| RECEPTION  |  ENTRANCE   |
  [PROPOSED  | (Vault) | E | [102] | [101] |===+ MAIN LOBBY |  VESTIBULE  |
   M3 WEST   +--   ----+ R |               |E2 | (Open)     |  Spawn/Exit |
   PLANT &   | West    | V |=== MAIN GUEST |E1 |            +-------------+
   DIM.    [S4]Utility | I |   CORRIDOR    +===+ SE LOBBY FOYER  [S5]  [PROPOSED
   CONN.]    |         | C |   (Existing)      |  (Existing)             M4 EAST]
             +---   ---+ E +-[SW]-[106]-[105]--+-[104]--[103]-------------+
             | SW Dim. |   | SW  | RM   | RM   | RM   | RM   |            |
             | Passage |   | Serv| 106  | 105  | 104  | 103  |            |
Z=-12 ====== +=========+===[S3]===+======+======+======+======+===========+=====
             |           [PROPOSED M2 — SOUTH PARKING & LOADING DOCK]     |
             |           (Est. X=-16..+16, Z=-34..-12 — NOT IMPLEMENTED)  |
Z=-34        +------------------------------------------------------------+

Legend:
  [S1] Existing North Service Seam (X=-12.4..-9.6, Z=+12) -> Connects to M1 Laundry/Service Loop
  [S2] Existing North Lobby Seam   (X=+3.6..+7.2,  Z=+12) -> Connects to M1 North Wing & Courtyard
  [S3] Existing South Service Seam (X=-12.4..-5.8, Z=-12) -> Connects to M2 Parking/Loading Deck
  [S4] Existing West Utility Seam  (X=-20.0, Z=-0.8..+1.8)-> Connects to M3 Plant / Dim. Connector
  [S5] Existing East Foyer Seam    (X=+20.0, Z=-3.6..+4.4)-> Connects to M4 East Public Arcade
  <==> Proposed interconnecting doorway between adjacent suites for non-linear escape loops
```

---

## 6. Future Pursuit & Escape-Space Design Principles (`PROPOSED`)
- **Macro & Micro Loops:** Every major expansion wing must close at least one full loop back to the existing `40 m × 24 m` core (`M1` achieves this immediately via `[S1]` + `[S2]`), preventing unavoidable trap-corridors.
- **Dual-Exit Rooms & Interconnecting Suites:** In future guest wings (`Rooms 107–110`), pair selected adjacent rooms with unfinished internal connecting doorways or rear housekeeping service doors so players fleeing into a room can break line of sight and exit into a parallel corridor.
- **Sightline Breaks & Intersections:** Place L-bends, structural courtyard piers, and recessed stairwell/elevator lobbies every `10–14 m` along long hallways so players can break visual contact during pursuits.
- **Spatial Rhythm (Wide vs. Narrow):** Contrast wide maneuvering zones (Main Lobby, Interior Courtyard, Parking Deck) with tighter, higher-tension routes (Service Corridors, Utility Passages, Laundry Chutes) to vary pacing.
- **Deliberate Risk Dead Ends:** Retain select single-exit chambers (e.g., Level-Ω Safe Room, Dimensional Passage Sanctuary, locked utility closets) strictly where high-value discoveries or puzzle anchors justify deliberate risk.

---

## 7. Hiding-Location Design Principles (`PROPOSED`)
- **Architectural Concealment (No Furniture Dependency):** Design future hiding pockets directly into unfinished construction geometry — recessed en-suite bathroom shells with partial sight-blocking nib walls, maintenance pipe chases, laundry alcoves, recessed concrete stairwell undersides, and partial drywall/masonry partitions.
- **Multiple Observation Angles:** Provide narrow visual slivers (e.g., unfinished doorframe reveals or high pass-through openings) so players can gauge danger without blind guessing.
- **Exit Affordance:** Every intended hiding alcove should allow rapid exit toward at least two escape directions once a threat passes, avoiding unfair single-tile lock-in spots.

---

## 8. Elevator A — Unfinished Empty Shaft Concept (`PROPOSED — NOT IMPLEMENTED`)
- **Creative Intent:** Exterior hotel elevator doors are mounted, but the passenger cabin was never installed, revealing an empty vertical concrete shaft with exposed guide rails, counterweights, hanging hoist cables, and unfinished maintenance catwalk/ladder brackets.
- **Future Gameplay Possibilities *(Conceptual Only)*:**
  - Inspecting into the vertical void from a safely guarded threshold or ajar door.
  - Controlled vertical maintenance descent/climbing along fixed structural ladders or cables to a lower basement or upper unfinished floor.
  - Discovering an anomalous vertical route into another Remanencia territory.
- **Mandatory Future Safety Requirements:**
  - The shaft **must remain 100% physically sealed** until a dedicated vertical-traversal increment is approved.
  - No fall hazard may be enabled until dedicated player-controller climbing/ledge mechanics, camera framing, fall-damage/recovery rules, and checkpoint/respawn handling are designed, implemented, and tested.

---

## 9. Elevator B — Functioning Passenger Elevator Concept (`PROPOSED — NOT IMPLEMENTED`)
- **Creative Intent:** A partially completed conventional passenger elevator with an enclosed cab, animated bi-parting doors, interior floor-button panel, and status indicator connecting Nivel 0 to future hotel floors (e.g., upper unfinished guest floors, rooftop construction zone, basement) or distorted Remanencia destinations.
- **Required Future Systems *(Not Implemented)*:**
  - Cab interior geometry & lighting synced to the player's world context.
  - Door open/close animation with solid transit containment colliders.
  - Floor-selection interaction UI & bilingual labels.
  - Deterministic vertical teleport or additive-scene floor streaming integrated with `PlayerProgressionService` and `PlayerWorldContext`.

---

## 10. Scene-Verified Elevator Assessment & Proposed Assignment (`DECISION PENDING CREATOR APPROVAL`)

### Exact Current Scene Inventory (`BackroomsLevel1Scene.unity`, Untouched in 07.4)
- **Parent Container:** `Hotel_Elevator_Core` (`GO=331299473`, `TF=331299474`)
- **Core Concrete Enclosure (`X = +1.00 .. +3.41`, `Z = +4.20 .. +9.40`, height `2.85 m`, ALL solid `BoxCollider`s):**
  - `Hotel_ElevatorCore_BackWall` (`GO=1951929040`, `pos=(1.12, -48.575, 6.80)`, `scale=(0.24, 2.85, 5.32)`) — interior back face at `X = +1.24`.
  - `Hotel_ElevatorCore_SouthWall` (`GO=585933301`, `pos=(2.20, -48.575, 4.32)`, `scale=(0.24, 2.85, 2.52)`) — interior south face at `Z = +4.44`.
  - `Hotel_ElevatorCore_NorthWall` (`GO=2097585768`, `pos=(2.20, -48.575, 9.28)`, `scale=(0.24, 2.85, 2.52)`) — interior north face at `Z = +9.16`.
  - `Hotel_ElevatorCore_FrontPierS` (`GO=1166525667`, `pos=(3.28, -48.575, 4.60)`), `FrontPierMid` (`GO=1160025726`, `pos=(3.28, -48.575, 6.80)`), `FrontPierN` (`GO=139182998`, `pos=(3.28, -48.575, 9.00)`), and `FrontHeader` (`GO=1472619301`, `pos=(3.28, -47.475, 6.80)`, `scale=(0.28, 0.65, 5.24)`).
  - **Shared Hall-Call Plate:** `Hotel_Elevator_CallPanelPlate` (`GO=1622816346`, `pos=(3.43, -48.82, 6.80)`), `CallButtonUp` (`GO=823213806`), `CallButtonDown` (`GO=1114928462`) — non-colliding.
- **Bay 1 — South Bay (`Z = +5.00 .. +6.50`, center `Z = +5.75`):**
  - `Hotel_ElevatorBay_1_DoorLeafS` (`GO=2004914621`, `pos=(3.14, -48.90, 5.38)`, `scale=(0.08, 2.20, 0.76)`, `BoxCollider=True`)
  - `Hotel_ElevatorBay_1_DoorLeafN` (`GO=1307326071`, `pos=(3.14, -48.90, 6.12)`, `scale=(0.08, 2.20, 0.76)`, `BoxCollider=True`)
  - Non-colliding trim: `CenterSeam` (`GO=1008553737`), `FrameJambS` (`GO=464369093`), `FrameJambN` (`GO=181796879`), `FrameHeader` (`GO=1454700179`), `FloorSill` (`GO=228939351`), `IndicatorHousing` (`GO=1410903630`), `IndicatorStrip` (`GO=137003656`).
- **Bay 2 — North Bay (`Z = +7.10 .. +8.60`, center `Z = +7.85`):**
  - `Hotel_ElevatorBay_2_DoorLeafS` (`GO=1255285515`, `pos=(3.14, -48.90, 7.48)`, `scale=(0.08, 2.20, 0.76)`, `BoxCollider=True`)
  - `Hotel_ElevatorBay_2_DoorLeafN` (`GO=1590748828`, `pos=(3.14, -48.90, 8.22)`, `scale=(0.08, 2.20, 0.76)`, `BoxCollider=True`)
  - Non-colliding trim: `CenterSeam` (`GO=1794556672`), `FrameJambS` (`GO=552356555`), `FrameJambN` (`GO=664166405`), `FrameHeader` (`GO=1061255800`), `FloorSill` (`GO=242779403`), `IndicatorHousing` (`GO=908471151`), `IndicatorStrip` (`GO=1469709015`).

### Measured Interior Clearances & Obstructions
- **Clear Door Opening per Bay:** `1.50 m` wide × `2.20 m` tall (meets standard commercial passenger elevator proportions).
- **Available Interior Shaft Depth (X):** `3.10 - 1.24 = 1.86 m` clear inside existing `BackWall` (`X=1.24`) and closed doors (`X=3.10`) — **sufficient for both a compact passenger cab (`~1.65 m × 1.90 m`) and a visual/traversable maintenance shaft!**
- **Available Interior Bay Width (Z):** Bay 1 spans `Z = 4.44 .. 6.65` (`~2.21 m`); Bay 2 spans `Z = 6.95 .. 9.16` (`~2.21 m`) if an internal shaft divider wall is added at `Z = 6.80`.
- **Current Headroom & Floor/Ceiling Obstructions:**
  - `Backrooms_Floor` (continuous solid `40.6 m × 24.6 m` slab at `Y = -50.00 m`) and `Backrooms_Ceiling` (continuous solid slab at `Y = -47.15 m`) currently seal the top and bottom of both shafts (`2.85 m` clear height).
  - **Required Future Structural Changes for Elevator A (Empty Shaft):** Add an internal dividing wall at `Z = 6.80` separating Bay 1 from Bay 2; split/cut a localized shaft cutout in `Backrooms_Floor` and `Backrooms_Ceiling` strictly inside the chosen bay's shaft footprint (`X ∈ [1.24, 3.10]`), extend shaft concrete walls vertically below `-50.00 m` and above `-47.15 m`, install guide rails/cables/guard barriers, and add a dedicated vertical traversal/safety controller.
  - **Required Future Structural Changes for Elevator B (Functional Passenger Elevator):** Add internal dividing wall at `Z = 6.80`, build enclosed cab interior inside `X ∈ [1.28, 3.06]` (no floor cutout required if vertical floor transitions use additive scene loading or smooth elevator-ride teleportation!), wire animated sliding door leaves (`DoorLeafS/N`), interior button panel, and `ElevatorController`.

### Recommended Assignment (`PENDING CREATOR APPROVAL`)
- **Recommend Bay 2 (North Bay, `Z = 7.85`) → Elevator A (Unfinished Empty Shaft):**
  - Sits directly adjacent to the **North Luggage / Service Alcove (`Z = 9.4..12.0`)**, existing high electrical junction box (`Z = 9.05`), and the recommended **M1 North Expansion** seam — making exposed construction cabling, service scaffolding, and vertical maintenance access visually and spatially cohesive.
- **Recommend Bay 1 (South Bay, `Z = 5.75`) → Elevator B (Functioning Passenger Elevator):**
  - Aligns most directly with the central Lobby waiting axis and the Main Guest-Room Corridor (`Z = -1.8..+1.4`), giving players a clean, unmistakable primary passenger elevator arrival/departure experience when ascending or descending to future hotel floors.

---

## 11. Structural, Streaming & Save/Persistence Compatibility
- **Modular Perimeter Extensions (Zero Scene Rebuild):** All future horizontal wings can attach cleanly under `Backrooms_Architecture/Hotel_GroundFloor_Architecture` (or dedicated additive sub-scenes for large wings) by replacing only the specific perimeter wall segment at seam `[S1]`–`[S5]` with a framed portal opening, preserving every existing `BackroomsLevel1Scene.unity` fileID.
- **Additive Scene & World Context (`PlayerWorldContext`):** `BorderCorridorScene` remains the outdoor host scene while `BackroomsLevel1Scene` (and any future additive Nivel 0 wing/floor scenes) runs under `PlayerWorldContext.RemanenciaLevel0`; surface relic-HUD isolation and outdoor atmosphere restoration remain unaffected.
- **Offline Progression & Save Compatibility:** Existing `ProgressionSaveData` schema and all 7 persistent progression IDs remain unchanged. Future elevator floor unlocks or expansion checkpoints should append new additive IDs without mutating existing keys.
- **2 GB VRAM Performance Discipline:** Future expansion wings must maintain non-shadow-casting point lights, shared modular concrete/plaster materials, and occlusion/zone lighting budgets consistent with the current `16`-light ground floor.

---

## 12. Dependencies & Recommended Implementation Sequence (`PROPOSED`)
1. **Creator Sign-Off on 07.3 + 07.4 Signage & This Plan** *(current gate)*.
2. **Phase A — Horizontal Expansion M1 (North Hotel Wing & Courtyard Loop):** Implement the `~28 m × 22 m` North wing connected via `[S1]` and `[S2]` to establish full loop circulation before introducing threats.
3. **Phase B — Elevator Structural Preparation (Static Visual Distinction Only):** Once creator approves Bay 1 vs. Bay 2 assignment, add internal shaft divider (`Z = 6.80`), visually distinguish Elevator A (partially ajar door + guarded view into unfinished shaft) vs. Elevator B (intact passenger doors), still with zero fall hazards.
4. **Phase C — Elevator B Passenger System & First Additional Floor** *(separate vertical gameplay increment)*.
5. **Phase D — Pursuit/Stealth Systems, Ecology & Elevator A Vertical Traversal** *(requires dedicated AI, stealth, climbing, camera, and checkpoint specs)*.

---

## 13. Unresolved Creator Decisions (`AUTHOR DECISION REQUIRED`)
1. **Approve First Horizontal Expansion Direction:** Confirm **M1 North Hotel Wing & Courtyard Loop** vs. Alternative A (South Parking/Loading) or Alternative B (West Mechanical/Dimensional Connector).
2. **Approve Elevator Bay Assignment:** Confirm **Bay 2 (North, `Z = 7.85`) = Elevator A (Unfinished Shaft)** and **Bay 1 (South, `Z = 5.75`) = Elevator B (Functional Passenger Elevator)**, or invert the assignment.
3. **Approve Target Scale for Nivel 0 Ground Floor:** Confirm whether `~1,500–1,600 m²` (Current `960 m²` + M1) is the desired playable milestone before vertical floors, or whether M2 South Parking should also be scoped for Nivel 0.
4. **Confirm Signage Visual Acceptance:** Verify upright orientation, Spanish accents (`RECEPCIÓN`, `ÁREA DE MANTENIMIENTO`), and readability of all 7 signs and 6 room plaques in PlayMode.
