# REMANENCIA — Nivel 0: Room 111-B ("The Impossible Room") & Exit Architecture Design

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia**  
**Territory:** **Nivel 0 — Hotel de las Palomas**  
**Target Scene:** `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`
**Baseline Audited (07.10):** `2c7ad5a949232fa64ba004b71a39ca72e5c5f584` → **07.11 Baseline:** Branch `product/unity-fellowship-2027`, HEAD `0d325c21b1d27943694c2e5bf6c98af503078976`
**Document Status:** **UPDATED FOR IMPLEMENTATION 07.11 — PHYSICAL ROOM 111-B ARCHITECTURE IMPLEMENTED (INTERIOR, ENTRANCE INTERPRETATION, ONE-WAY OBSERVATION & ALTERNATIVE EXIT REMAIN PROPOSED FUTURE DESIGN)**

> **Strict Implementation Boundary & Status Labels:**
> Every section and requirement in this document is explicitly tagged as one of:
> - **`VERIFIED CURRENT IMPLEMENTATION`** — Existing pre-07.11 architecture, evidence, ecology, transitions, or surface-world assets verified directly in the repository.
> - **`IMPLEMENTED IN 07.11`** — Physical Room 111-B architecture (`Hotel_Room111B_Anomaly`), concealed East passage, empty Room 111-B shell, `111-B` signage, and automated verification added in Implementation 07.11. Visual acceptance remains pending the creator's personal PlayMode review.
> - **`PROPOSED FUTURE DESIGN`** — Approved or candidate concepts for future increments (`07.12+`), strictly **not implemented** in 07.11.
> - **`UNRESOLVED CREATOR DECISION`** — Open creative or systems questions awaiting explicit creator selection.

> **Historical, Fictional & Dimensional Separation Notice:**  
> - **Real-world historical inspiration:** The unfinished Hotel de las Palomas in Ciudad Juárez serves solely as architectural inspiration. No claim is made that a "Room 111-B", hidden corridor, or anomalous exit ever existed in the real building.  
> - **Fictional hotel records (07.6):** All four wall-mounted documents (`Hotel_Evidence_*`) are original fictional game props.  
> - **Fictional Remanencia phenomena (07.6–07.10+):** Room 111-B, spatial contradictions, metric drift, anomalous ecology, and non-conventional exits belong strictly to Remanencia's fictional dimensional reality.

---

## 1. Executive Design Summary

Implementations 07.2 through 07.9 established **Nivel 0 — Hotel de las Palomas** as a coherent `1,664 m²` unfinished concrete hotel (`40 m × 24 m` original ground floor + `32 m × 22 m` North Wing M1) with four bilingual narrative discoveries, feathered mineral anomalies, early fungal/vegetal ecology, static faunal traces, and two solitary living microfauna encounters.

Two already-implemented 07.6 discoveries intentionally planted the seeds for the hotel's first major impossible architectural mystery:
1. **`Hotel_Evidence_UnfinishedProjectPlan`** depicts a red-ink dashed corridor and room extending **East beyond the North Wing's solid East concrete perimeter wall (`X = +16.0`)**.
2. **`Hotel_Evidence_RoomRegistrationRecord`** lists Rooms `107–110` alongside an impossible, unlisted **`111-B`** registered before construction ever concluded.

This forensic investigation confirms two high-leverage architectural findings:
- **Physical Void Inside the Hotel's Own Footprint (`VERIFIED`):** Because the South core spans `X ∈ [-20.0, +20.0]` (`40 m`) while North Wing M1 spans `X ∈ [-16.0, +16.0]` (`32 m`), an **`88 m²` completely unoccupied East void (`X ∈ [+16.0, +20.0], Z ∈ [+12.0, +34.0]`)** sits directly behind `NorthWing_OuterWall_East` (`X = +16.0`) — immediately matching the exact phantom East wing drawn on the 07.6 blueprint, with zero collision against any existing room, artifact, light, or test raycast!
- **Safe Architecture for "Check-in Without Conventional Check-out" (`VERIFIED`):** The existing `SecretBackroomsTransitionController` + decoupled `ProgressionSaveData` (`latestCheckpointId` never overrides surface spawn on restart) can safely support an entry-only front door and an unconventional alternative exit **provided** the alternative exit is implemented, verified reachable without fragile puzzle locks, and activated *before* or *atomically alongside* disabling `Backrooms_ExitDoor_ToStreet`.

**Top Recommendations (`PROPOSED DESIGN — PENDING CREATOR APPROVAL`):**
- **Room 111-B Spatial Architecture:** **Option A — Hidden Architectural Passage** opening behind `NorthWing_NorthCorridor_EastOverlookNib` at the East dead-end of the North Guest Corridor (`X = +16.0, Z ∈ [+26.8, +28.4]`) into the vacant East void (`X ∈ [+16.0, +24.4], Z ∈ [+19.0, +28.6]`), optionally designed with a dark L-bend baffle ready for future Option B non-Euclidean extensions.
- **Unconventional Exit Direction:** **Hybrid Concept A + C (Concealed Exit via Room 111-B with a Quiet Threshold-of-Trust Moment)** — keeping `Backrooms_ExitDoor_ToStreet` visible as a dead/entry-only threshold while routing return to Avenida Benito Juárez through an impossible architectural exit discovered via Room 111-B, independent of Elevators A/B and *El Umbral de Lasser*.

---

## 2. Exact Analysis of the 07.6 Clues Supporting Room 111-B *(VERIFIED CURRENT IMPLEMENTATION)*

All four 07.6 discoveries under `BackroomsLevel1_Root/Hotel_Narrative_Evidence` were inspected verbatim in `BackroomsLevel1Builder.cs` and `BackroomsLevel1Scene.unity`. None will be altered or rewritten.

| # | GameObject & Progression ID | Exact World Position | Established In-World Contradiction | Direct Role in Room 111-B Mystery |
| :-: | :--- | :---: | :--- | :--- |
| **01** | `Hotel_Evidence_UnfinishedProjectPlan`<br>`clue:remanencia-hotel-plan-01` | `(8.85, -48.52, 11.83)`<br>*(Reception North Wall, facing South)* | Blueprint texture (`T_Remanencia_Evidence_ProjectPlan.png`, `256×192`) draws `outerRect` (`x∈[26,216], y∈[24,162]`), `courtyardRect` (`x∈[86,150], y∈[78,126]`), `wingDivide` (`y=68` = `Z=+12.0`, `x=176` = `X=+8.2`), and in **red anomaly ink** (`phantomWing`): a dashed corridor (`y=108, x∈[176,236]`) crossing **East through the solid East wall (`x=216` = `X=+16.0`)** into an unbuilt room (`x∈[216,238], y∈[82,136]`). | **Primary spatial map clue:** Pinpoints the exact wall (`NorthWing_OuterWall_East`, `X = +16.0`) beyond which an extra corridor and room "ought" to exist according to the hotel's own drawings. |
| **02** | `Hotel_Evidence_RoomRegistrationRecord`<br>`clue:remanencia-hotel-register-02` | `(-3.85, -48.56, 28.67)`<br>*(North Guest Corridor between Rooms 109 & 110, facing South)* | Torn ledger page (`T_Remanencia_Evidence_RoomRegister.png`) lists `107`, `108`, `109`, `110`, followed in **reddish ink** by **`111-B`**, with incomplete stay dates recorded before construction ended. | **Primary identity & numbering clue:** Names the impossible room (`111-B`) and ties it directly to the North Guest Corridor's `107–110` numbering sequence. |
| **03** | `Hotel_Evidence_MaintenanceRecord`<br>`clue:remanencia-hotel-maintenance-03` | `(-8.73, -48.54, 22.45)`<br>*(Maintenance Corridor West-facing wall)* | Logs dry joint stains, unsourced power drops, repairs appearing before damage, and a corridor measuring **`+1.4 m` longer than drawn**. | **Thematic priming clue:** Establishes that the building's measured physical geometry refuses to agree with fixed construction blueprints. |
| **04** | `Hotel_Evidence_Cobalt60RumorNote`<br>`clue:remanencia-hotel-cobalt-rumor-04` | `(-12.75, -48.62, 23.27)`<br>*(Housekeeping Utility Room North wall)* | Unsigned pencil hearsay about allegedly contaminated rebar ("nobody has shown proof"), noting anomalies return even where material was removed. | **Boundary & ambiguity clue:** Demonstrates that conventional material/industrial rumors fail to explain the hotel's behavior; Room 111-B must remain unexplained and never attributed to Cobalt-60. |

### Exact Pixel-to-World Mapping of the 07.6 Phantom Wing (`VERIFIED`)
In `BackroomsLevel1Builder.EnsureEvidenceProjectPlanTexture()`:
- North Wing M1 maps vertically from `y = 68` (`Z = +12.0 m`) to `y = 162` (`Z = +34.0 m`), a scale of `22.0 m / 94 px = 0.234 m/px`.
- Horizontally, North Wing M1 maps from `x = 26` (`X = -16.0 m`) to `x = 216` (`X = +16.0 m`), a scale of `32.0 m / 190 px = 0.168 m/px`.
- Therefore, the red dashed `phantomWing` maps directly onto Nivel 0 world coordinates:
  - **Dashed Phantom Corridor (`y = 108, x ∈ [176, 236]`):** Starts at `X ≈ +9.2 m` (Banquet Hall / East Arcade boundary `X = +8.2 m`) and runs due East through `X = +16.0 m` out to `X ≈ +19.4 m` along `Z ≈ +21.36 m`.
  - **Dashed Phantom Room (`x ∈ [216, 238], y ∈ [82, 136]`):** Sits strictly East of `X = +16.0 m` (`X ≈ +16.0 .. +19.7 m`) spanning `Z ≈ +15.3 .. +27.9 m` — running right alongside both the **Banquet Hall East wall** (`Z = +12..+26`) and the **North Guest Corridor East dead-end** (`Z = +26.0..+28.8`)!

---

## 3. Architectural Forensic Audit of Nivel 0 *(VERIFIED CURRENT IMPLEMENTATION)*

Base floor elevation: `BaseY = -50.00 m`. Clear height: `ClearHeight = 2.85 m` (`CeilingY = -47.15 m`). Standard interior partition wall thickness: `0.22 m` (`0.16 m` bathroom nibs). Outer perimeter wall thickness: `0.26 m` (`±0.13 m` from centerline). Total scene light budget: exact `18` lights (`0` shadow-casting).

### A. Verified Zone Coordinate Inventory

| Architectural Zone | World X Range (m) | World Z Range (m) | Clear Area / Width | Key Bounding Walls & Notable Existing Features |
| :--- | :---: | :---: | :---: | :--- |
| **Entrance Vestibule** | `+16.2 .. +20.0` | `+5.4 .. +12.0` | `3.8 × 6.6 m` | `Backrooms_SpawnPoint (17.80, -49.94, 8.80)`, `Backrooms_ExitDoor_ToStreet (19.86, -50.00, 8.80)` on East wall, North wall `NorthWing_PerimeterSeal_East` at `Z = +12.0`. |
| **Main Lobby & Reception** | `+3.4 .. +16.2` | `+1.4 .. +12.0` | Open hall | Reception Alcove (`X=+7.4..+14.2, Z=+9.2..+12.0`), `Hotel_Evidence_UnfinishedProjectPlan (8.85, -48.52, 11.83)`, **Connection A** North Archway (`X=+3.8..+7.0, Z=+12.0`, `3.20 m` clear). |
| **Twin Elevator Core** | `+1.0 .. +3.4` | `+4.2 .. +9.4` | `2.4 × 5.2 m` | `Hotel_Elevator_Core` (`35` children, strictly untouched). Bay 1 South (`Z=5.75`), Bay 2 North (`Z=7.85`), closed bi-parting metal doors at `X=+3.14`. |
| **Main Guest Corridor & Rooms 101–106** | `-9.5 .. +20.0` | `-12.0 .. +12.0` | Corridor `3.2 m` wide (`Z=-1.8..+1.4`) | 6 empty guest-room shells: `101 (-3.6..+1.0, N)`, `102 (-9.5..-3.6, N)`, `103 (+12.6..+20.0, S)`, `104 (+7.8..+12.6, S)`, `105 (+1.4..+7.8, S)`, `106 (-5.6..+1.4, S)`. |
| **South Service Wing, Vault & Portal** | `-20.0 .. -5.6` | `-12.0 .. +12.0` | Back-of-house | N–S Service Hall (`X=-12.5..-9.5`), **Connection B** (`X=-12.0..-9.58, Z=+12.0`, `2.42 m` clear), Level-Ω Safe (`-16.40, 11.42`), *El Umbral de Lasser* (`-19.70, -8.20`). |
| **North Wing M1 Outer Shell** | `-16.0 .. +16.0` | `+12.0 .. +34.0` | `32 × 22 m` (`704 m²`) | Enclosed by `NorthWing_OuterWall_North (Z=+34.0)`, `East (X=+16.0)`, `West (X=-16.0)` (`0.26 m` thick; inner faces `Z=+33.87`, `X=+15.87`, `X=-15.87`). |
| **Central Courtyard & Arcades** | Court `-5.0..+5.0`<br>Arcades `±5.0..±8.2` | Court `+18.0..+26.0`<br>South Prom `+15..+18` | `10 × 8 m` court<br>`2.8–3.2 m` loop | Sealed skylight cap (`Y=-44.80 m`), 4 cardinal portals (`2.8–3.2 m`), central plinth (`0, 22`), 07.7 vegetation & 07.8 Courtyard traces. |
| **North Guest Corridor** | `-8.6 .. +16.0` | `+26.0 .. +28.8` | `24.6 × 2.8 m` | Connects West Service Bulkhead N (`X=-8.6`) past Rooms `110, 109, 108, 107` to a **dead-end East concrete wall (`X=+16.0`)**, partially screened by `NorthWing_NorthCorridor_EastOverlookNib (X=+13.6, Z=+26.0..+27.0)`. |
| **Guest Rooms 107–110** | `107: +9.6..+16.0`<br>`108: +3.2..+9.6`<br>`109: -3.6..+3.2`<br>`110: -9.6..-3.6` | `+28.8 .. +34.0` | `5.2 m` deep | `109↔110` interconnecting doorway (`X=-3.6, Z=30.0..31.3`); `110` NW plumbing chase (`X=-9.6..-7.8, Z=31.6..34.0`) holds 07.7 Zone D trace. |
| **Banquet / Dining Hall** | `+8.2 .. +16.0` | `+12.0 .. +26.0` | `7.8 × 14.0 m` (`109 m²`) | Dual West portals to East Arcade (`Z=15.4..18.2`, `22.4..25.0`), North door to North Corridor (`X=10.4..12.8, Z=26.0`), **Sealed Future East Anchor Frame** (`NorthWing_FutureAnchor_EastFrameL/R/Top` at `X=+15.84, Z=+17.5..+20.5`). |
| **Housekeeping Core & Maintenance** | `-16.0 .. -8.6` | `+12.0 .. +34.0` | Corridor `3.4 m` wide (`X=-12..-8.6`) | Storage (`Z=12..18`), Utility (`Z=18..23.4`, 07.7/07.8/07.9 Zone C + Cobalt-60 note), Linen (`Z=23.4..28.8`), NW Stairwell Reserve Landing (`X=-16.0..-9.6, Z=28.8..34.0`), Sealed Future West (`X=-15.84, Z=19.4..22.0`) & North (`Z=+33.84, X=-14.2..-11.4`) frames. |

### B. Critical Perimeter Collision & Test Constraints (`VERIFIED`)
Any future opening cut into North Wing M1 perimeter walls must respect existing automated spatial tests (`RemanenciaNorthWingExpansionTests.cs:144–149`):
- North perimeter linecast at **`(X=0.0, Z=33..35)`** — must remain solid.
- East perimeter linecast at **`(X=15..17, Z=20.0)`** — currently asserts `NorthWing_OuterWall_East` is solid at `Z = 20.0`!
- West perimeter linecast at **`(X=-15..-17, Z=20.0)`** — must remain solid.
- **Implication:** Opening the East perimeter at `Z ∈ [+26.8, +28.4]` (North Guest Corridor East dead-end) leaves `Z = +20.0` **100% solid**, preserving every existing M1 perimeter test untouched!

### C. Available Unoccupied Exterior Pockets Adjacent to Nivel 0 (`VERIFIED`)
```text
Z=+34.0  +---------+------+------+------+------+
         | NW Land | R110 | R109 | R108 | R107 |  <-- [VACANT EAST POCKET]
Z=+28.8  +---   ---+--  --+--  --+--  --+--  --+      X ∈ [+16.0, +24.8]
         | Maint.  | ==== NORTH CORRIDOR ===[N]| ===> Z ∈ [+12.0, +34.0]
Z=+26.0  | Corr.   +------+----------+---+--  -+      (88 m² inside X<=+20 alone;
         | & House-| West | COURTYARD| E |     |       zero existing geometry!)
         | keeping | Arc. | (-5..+5) | A | BAN |
         | (-16..  |      |          | r | QUET|  [East Anchor Frame: Z=17.5..20.5]
         |  -8.6)  |      |          | c |     |
Z=+12.0  +===[B]===+======+====[A]===+===+=====+==========+
         | NW Safe | R102 | R101 | E | RECEPTION | ENTR.  |  (South block extends
         | (-20..) |      |      | l |  LOBBY    | VEST.  |   out to X = +20.0!)
Z=+5.4   +---------+------+------+---+-----------+--------+
         X=-20   X=-16          X=0            X=+16   X=+20
```
*Legend: `[N]` = `NorthWing_NorthCorridor_EastOverlookNib` (`X=+13.6, Z=+26..+27`); `[A]`/`[B]` = Connections A & B.*

---

## 4. Candidate Implementations for Room 111-B *(PROPOSED DESIGN — NOT IMPLEMENTED)*

Three distinct spatial architectures were evaluated against the verified coordinates above.

### Option A — Hidden Architectural Passage *(East Corridor Extension Behind `X = +16.0`)*
- **Concept:** A concealed unfinished construction opening at the far East end of the **North Guest Corridor (`X = +16.0, Z ∈ [+26.8, +28.4]`)** — partially shielded from casual glance by the existing `NorthWing_NorthCorridor_EastOverlookNib` (`X = +13.6, Z = 26.0..27.0`) — reveals a narrow, unplastered raw-concrete service/guest corridor turning South along **`X ∈ [+16.2, +19.0], Z ∈ [+20.0, +28.6]`** (occupying the exact red-ink phantom wing zone from `UnfinishedProjectPlan`!) and terminating at a single framed doorway bearing plaque **`111-B`**, opening into **Room 111-B** (`X ∈ [+19.2, +24.6], Z ∈ [+20.0, +24.8]`).
- **Exact Proposed World Coordinates & Dimensions:**
  - **Passage Opening:** `X = +16.00`, `Z ∈ [+26.80, +28.40]` (`1.60 m` clear width × `2.20 m` clear height; exceeds player capsule `Ø 0.64 m` by `2.5×`). Concealed initially by a misaligned leaning concrete/plywood formwork board or narrow dark shadow-recess gap (`0.95 m` squeeze-clear or full `1.60 m` recessed step-in behind an offset concrete baffle wall at `X = +16.8`, so from down the corridor the East end still reads like a solid dead-end wall until the player walks right past `X = +13.6`!).
  - **Phantom East Corridor:** `X ∈ [+16.13, +18.90]`, `Z ∈ [+20.20, +28.67]` (`2.77 m` wide × `8.47 m` long, `BaseY = -50.00 m`, `ClearHeight = 2.85 m`). Notice: its South end (`Z = +20.2`) stops directly behind the Banquet Hall's `NorthWing_FutureAnchor_EastFrame` (`X = +15.84, Z = +17.5..+20.5`), visually confirming why the blueprint drew a corridor straight out of the Banquet Hall's East wall!
  - **Room 111-B Shell:** `X ∈ [+19.12, +24.52]`, `Z ∈ [+20.00, +24.80]` (`5.40 m × 4.80 m = 25.9 m²`, `BaseY = -50.00 m`, `ClearHeight = 2.85 m`, matching Rooms 107–110 architectural scale while feeling eerily *more finished* or *out-of-era* — e.g., partial completed plaster, a different tile pattern, or mismatched proportions).
- **Required Architectural Modifications:**
  - Split `NorthWing_OuterWall_East` (`X = +16.0, Z = 12..34`) into two segments (`Z = 12.0..26.8` and `Z = 28.4..34.0`) — keeping `Z = +20.0` **100% solid** so existing M1 tests pass without weakening!
  - Add new sibling subgroup `Hotel_Room111B_Anomaly` under `Backrooms_Architecture` (continuous floor slab `X ∈ [+16.0, +24.8], Z ∈ [+19.8, +28.9]`, ceiling slab, enclosing walls, offset shadow baffle, `111-B` upright plaque).
- **New Objects Required:** `~28–38` modular concrete/plaster boxes, `1` room plaque (`111-B`, reusing existing room-plaque bitmap generator), `1–2` emissive-only ceiling strips (`0` new `Light` components to preserve exact `18`-light budget).
- **Player Discovery Sequence:**
  1. Player inspects `UnfinishedProjectPlan` in Reception (sees red-ink phantom East corridor/room beyond East wall).
  2. Player walks North Guest Corridor past `107–110` and inspects `RoomRegistrationRecord` (discovers `111-B` listed right after `110`).
  3. Player explores East past `Room 107` and `NorthWing_NorthCorridor_EastOverlookNib` (`X = +13.6`), noticing the "dead-end" East concrete wall has a misaligned shadow-gap / recessed construction offset opening into an unmapped concrete passage leading South to **`111-B`**.
- **Risks & Limitations:** Less literally "impossible" at the threshold itself unless enhanced with subtle spatial storytelling inside (e.g., Room 111-B's interior measurements or windowless proportions disagreeing with the corridor outside, or an unexpected interior state). Must carefully seal `Z = +12..+19.8` so the player cannot glimpse exterior void from the Entrance Vestibule (`Z = +5.4..+12.0`).
- **Estimated Implementation Complexity:** **Low–Moderate (1 focused increment).** Zero new runtime teleport scripts, 100% continuous `CharacterController` & `Physics` walkability, trivial automated EditMode capsule verification.

---

### Option B — Non-Euclidean Spatial Contradiction *(Controlled Seamless Offset Pocket)*
- **Concept:** The hotel's `NorthWing_OuterWall_East` (`X = +16.0`) remains visually intact from the Banquet Hall, EXCEPT the recessed concrete frame `NorthWing_FutureAnchor_EastFrame` (`X = +15.84, Z ∈ [+17.5, +20.5]`) — or a narrow dark vertical seam inside it — functions as a **controlled non-Euclidean spatial anomaly**. Walking straight into a dark, unlit shadow-recess (`~1.2 m` deep) or crossing a seemingly solid cracked concrete plane silently shifts the player via an exact local-offset `CharacterController` translation into an isolated off-footprint architectural pocket (`X ∈ [+36.0, +48.0], Z ∈ [+18.0, +28.0], Y = -50.00 m`) where the corridor extends `14+ m` deeper than the hotel's exterior permits, culminating in **Room 111-B**.
- **Exact Proposed World Coordinates & Dimensions:**
  - **Entry Anomaly Threshold:** Banquet Hall East recessed frame `X = +15.84..+16.00`, `Z ∈ [+18.10, +19.90]` (or a shallow `1.2 m` dark concrete pocket `X ∈ [+16.0, +17.2]` with a 90° L-bend so the player cannot see the teleport seam in either direction!).
  - **Isolated Non-Euclidean Pocket:** `X ∈ [+36.0, +48.0]`, `Z ∈ [+18.0, +28.0]`, `BaseY = -50.00 m` (`12 m × 10 m = 120 m²` totally clear of all existing and planned M1–M4 horizontal modules!).
  - **Room 111-B Interior:** `X ∈ [+42.0, +47.8]`, `Z ∈ [+20.0, +25.4]` (`5.8 m × 5.4 m`, `ClearHeight = 3.20 m` — subtly taller ceiling than Nivel 0's `2.85 m` to reinforce impossible scale!).
- **Required Architectural Modifications:**
  - Create shallow dark baffle vestibule at `X = +16.0, Z = +18.1..+19.9` (Note: `Z = +20.0` remains solid!) OR make a collider-disabled pass-through concrete decal plane with a dark interior vestibule.
  - Build isolated pocket geometry at `X ∈ [+36, +48]` with a matching return baffle teleporting back to `X = +16.4, Z = +19.0`.
  - Add a lightweight `NonEuclideanCorridorTransition` MonoBehaviour (preserving exact player relative offset, camera pitch/yaw, and `Physics.SyncTransforms()`, zero fade flash if hidden around an L-bend!).
- **New Objects Required:** `~35–45` modular architectural pieces + `2` trigger zones (`EnterPocket` / `ReturnPocket`) + `1` dedicated transition MonoBehaviour.
- **Player Discovery Sequence:**
  1. Player reads `UnfinishedProjectPlan` (phantom East corridor at `Z ≈ +21`) and `MaintenanceRecord` (`+1.4 m` corridor length anomaly).
  2. In the Banquet Hall, player examines the sealed concrete East Anchor Frame (`Z = 17.5..20.5`), noticing hairline mineral seams or a pitch-dark gap where solid concrete should be; stepping inside leads around a dark turn into a corridor far longer than the building footprint allows, ending at **`111-B`**.
- **Risks & Limitations:**
  - **Orientation & Seam Fragility:** Without a 90° dark baffle, a mid-corridor teleport causes visible geometry pop unless a render-texture portal camera is used (which risks exceeding the **2 GB VRAM / low-end GPU** budget!).
  - **Return-Trip Edge Cases:** Rapid back-and-forth walking across a trigger boundary requires hysteresis/cooldown guards to prevent infinite teleport ping-pong.
  - **Coordinate Isolation:** Automated path tests must explicitly bridge across the teleport pair rather than simple straight-line capsule walks.
- **Estimated Implementation Complexity:** **High (2 increments: geometry + transition controller + edge-case testing).**

---

### Option C — Displaced Architectural Access *(NW Landing / Room 110 / Elevator Threshold)*
- **Concept:** Room 111-B is placed immediately West/North of **Room 110** (`107 -> 108 -> 109 -> 110 -> 111-B`), accessed through an unexpected existing hotel feature:
  - **Sub-option C1 (NW Stairwell Reserve Landing North Frame):** Opening `NorthWing_FutureAnchor_NorthFrame` (`X ∈ [-14.2, -11.4], Z = +34.0`) northward into `Z ∈ [+34.0, +41.0]` (`X ∈ [-15.0, -9.6]`).
  - **Sub-option C2 (Room 110 Plumbing Chase False Wall):** Cutting a hidden break/doorway inside Room 110's unfinished NW plumbing chase (`X = -9.6, Z ∈ [+31.8, +33.0]`) into an impossible tucked chamber overlapping the NW Landing space.
  - **Sub-option C3 (Elevator Bay Threshold — Evaluated with Caution):** Using Bay 1 (`Z = 5.75`) or Bay 2 (`Z = 7.85`) as a displaced doorway directly to Room 111-B.
- **Exact Proposed World Coordinates & Dimensions (Sub-option C1):**
  - **Access Frame:** `X ∈ [-13.6, -12.0]`, `Z = +34.0` (`1.60 m × 2.20 m`, preserving `X = 0.0` North perimeter test!).
  - **Corridor & Room 111-B:** `X ∈ [-15.4, -9.6]`, `Z ∈ [+34.2, +41.2]` (`5.8 m × 7.0 m = 40.6 m²`, `BaseY = -50.00 m`).
- **Required Architectural Modifications:**
  - C1 splits `NorthWing_OuterWall_North` (`Z = +34.0`) between `X = -13.6..-12.0` and builds a North annex beyond `Z = +34.0`.
  - C2 disturbs **Zone D (Room 110 plumbing chase)**, risking interference with 07.7's `Ecology_Optional_GuestRoomTrace` and `NorthWing_Room110_ChaseRiser`.
  - C3 alters `Hotel_Elevator_Core` (`35` children), violating the architectural independence of Elevators A & B before vertical access is approved.
- **Player Discovery Sequence:**
  1. Player reads `RoomRegistrationRecord` (`107–110`, `111-B`) and walks West past Room 110 into the NW Service Landing.
  2. Player finds an anomalous opening in the sealed North concrete anchor frame leading to `111-B`.
- **Risks & Limitations:**
  - **Blueprint Contradiction:** `Hotel_Evidence_UnfinishedProjectPlan` explicitly draws the red-ink phantom corridor & room on the **EAST** side (`x > 216`, `X > +16.0`), NOT the Northwest! Placing 111-B in the NW creates an unnecessary disconnect from Evidence 01.
  - **Vertical Reserve Conflict:** C1 consumes the **NW Future Stairwell Reserve Landing** (`docs/REMANENCIA_LEVEL_0_EXPANSION_AND_VERTICAL_ACCESS_PLAN.md`), complicating future vertical stairwell construction.
  - **Elevator Constraint:** C3 would prematurely entangle Elevators A & B before their multi-floor vertical roles are approved.
- **Estimated Implementation Complexity:** **Moderate (C1)** to **High (C3 — breaks elevator independence).**

---

## 5. Comparative Matrix & Recommendation *(PROPOSED DESIGN)*

| Criterion | Option A — Hidden East Architectural Passage | Option B — Non-Euclidean East Offset Pocket | Option C — Displaced NW / Elevator Access |
| :--- | :---: | :---: | :---: |
| **Matches 07.6 `UnfinishedProjectPlan` (East Phantom Wing)** | **Exact match** (`X > +16.0`) | **Exact match** (`X > +16.0`) | Contradicts East blueprint |
| **Matches 07.6 `RoomRegistrationRecord` (`107–110`, `111-B`)** | **Strong** (at East end of `107–110` corridor) | Moderate (accessed from Banquet Hall) | Strong (`110 -> 111-B` adjacency) |
| **Preserves All Existing M1 Tests (`Z=20`, `X=0`)** | **Yes (100%)** | Yes (if opening avoids `Z=20.0`) | Yes (C1), No (C3 breaks Elevator Core) |
| **Preserves Elevators A/B & NW Stairwell Reserve** | **Yes (100%)** | **Yes (100%)** | Conflicts with NW Stairwell or Elevators |
| **2 GB VRAM & Rendering Safety (No Portal Cameras)** | **Excellent** (pure static geometry) | Good (only with dark L-bend baffle) | Excellent (C1) |
| **Zero Teleport / Physics Edge-Case Risk** | **Excellent** (100% continuous walk) | Moderate (requires trigger hysteresis) | Excellent (C1) |
| **Future Expandability to Impossible Geometry** | **High** (can host a baffle deeper inside!) | **Highest** (unbounded off-footprint space) | Moderate (constrained by NW reserve) |
| **Implementation Complexity** | **Low–Moderate** | High | Moderate–High |
| **Overall Ranking** | **#1 — RECOMMENDED** | **#2 — Strong Phase-2 Upgrade** | **#3 — Not Recommended** |

### Recommended Spatial Architecture (`#1`: Option A + Future Option B Hook)
**Recommend Option A** anchored at the **East dead-end of the North Guest Corridor (`X = +16.0, Z ∈ [+26.8, +28.4]`)**:
1. From the corridor (`X < +13.6`), the existing `NorthWing_NorthCorridor_EastOverlookNib` (`X = +13.6, Z = 26.0..27.0`) and an offset concrete shadow-baffle wall (`X = +16.85`) make the East end read as a closed dead-end wall until the player walks right up to the corner past Room 107.
2. Stepping past the offset concrete baffle reveals the **unmapped phantom East corridor** (`X ∈ [+16.13, +18.90], Z ∈ [+20.20, +28.67]`) running South directly behind the Banquet Hall's East wall (matching `UnfinishedProjectPlan`!), terminating at a framed doorway marked **`111-B`** into **Room 111-B** (`X ∈ [+19.12, +24.52], Z ∈ [+20.00, +24.80]`).
3. **Architectural Payoff of the Contradiction:** Inside the phantom East corridor at `Z = +19.0`, the player stands on the *opposite side* of the Banquet Hall's sealed concrete frame (`NorthWing_FutureAnchor_EastFrame` at `X = +15.84, Z = +17.5..+20.5`), realizing the hotel's blueprint was remembering a real hidden corridor whose Banquet entrance was walled shut—while the North Corridor end was left fractionally offset!
4. **Clean Future Hook:** If deeper impossible spaces are desired later, a dark L-bend threshold inside or beyond Room 111-B (`X > +24.5`) can cleanly host an Option B non-Euclidean transition into deeper Remanencia territories without ever re-editing Nivel 0's core!

---

## 6. Player Discovery Sequence for Room 111-B *(PROPOSED DESIGN)*

1. **Seed 1 (Lobby / Reception):** Player inspects `Hotel_Evidence_UnfinishedProjectPlan` (`8.85, 11.83`) and notices the red dashed corridor/room drawn East beyond the solid hotel wall.
2. **Seed 2 (North Guest Corridor):** Exploring North Wing M1, the player passes `107`, `108`, `109`, `110` and inspects `Hotel_Evidence_RoomRegistrationRecord` (`-3.85, 28.67`), discovering **`111-B`** recorded as assigned.
3. **Seed 3 (Maintenance Corridor — Optional Reinforcement):** Inspecting `Hotel_Evidence_MaintenanceRecord` (`-8.73, 22.45`) primes the player that the hotel's physical dimensions exceed its drawings (`+1.4 m` drift).
4. **Investigation & Discovery:** Returning to the North Guest Corridor and walking all the way East past Room `107` and `NorthWing_NorthCorridor_EastOverlookNib` (`X = +13.6`), the player notices subtle floor expansion-joint lines / faint light-or-draft discontinuity continuing behind the misaligned East end concrete slab (`X = +16.0, Z = +26.8..+28.4`).
5. **Entry into the Phantom Corridor:** Rounding the shadow-baffle enters the unmapped raw-concrete East corridor (`Z = +28.4 -> +20.2`), arriving at an anachronistic hotel doorway bearing weathered plaque **`111-B`**.
6. **Inside Room 111-B:** Rather than a monster ambush or lore dump, Room 111-B feels unsettlingly *different* from the empty concrete shells `101–110` (e.g., subtly completed plaster/tile finishes from another construction era, faint acoustic/visual stillness, and—if approved under Section 7–8—the unconventional threshold leading back out of Remanencia).

---

## 7. The New Remanencia Rule — "Check-in Without Conventional Check-out" *(PROPOSED DESIGN)*

### Core Thematic & Mechanical Principle
- **Narrative Rule:** Entering Remanencia is a one-way threshold at the point of entry (*"Check-in, but no conventional check-out"*). Turning around and pushing the exact same door back open must **not** casually return the player to Avenida Benito Juárez.
- **Anti-Trap Guarantee:** This rule removes **conventional reversal through the entry door**, NEVER the player's ability to return to the surface world. Every major Remanencia territory must guarantee at least one coherent, discoverable, non-softlocking unconventional exit.

### Desired Future Behavior of the Entrance Vestibule (`Backrooms_ExitDoor_ToStreet`)
- Upon arriving at `Backrooms_SpawnPoint (17.80, -49.94, 8.80)`, the physical metal double door `Backrooms_ExitDoor_ToStreet (19.86, -50.00, 8.80)` **remains visible** behind the player.
- Approaching and pressing `[E]` / clicking it no longer calls `RequestExitBackrooms()`. Instead, it delivers restrained, grounded bilingual environmental feedback (e.g., latch refuses to yield / hollow rattle against solid poured concrete behind the frame — *zero UI tutorial popups spoiling the mystery*), signaling immediately that the way in is not the way out.

### Cross-Territory Design Questions (`UNRESOLVED CREATOR DECISIONS`)
1. **Per-Territory Unconventional Exits:** Recommended — each future Remanencia territory (Nivel 0, deeper hotel floors, future unrelated Ciudad Juárez echoes) should feature its own architecturally coherent exit rather than a single universal menu teleport.
2. **Discovered Route Persistence:** Once a player discovers Nivel 0's unconventional exit, should it remain permanently usable on repeat visits (recommended: **Yes**, honoring player mastery without tedious repetition), or shift location?
3. **Independence from Save Integrity:** Exit state must remain strictly independent from permanent discoveries, journal entries, learned codes, and solved puzzles — returning to the surface world must NEVER roll back progress, and quitting mid-exploration must NEVER trap a save file inside Remanencia.

---

## 8. Evaluation of Three Alternative Exit Concepts *(PROPOSED DESIGN — NOT IMPLEMENTED)*

### Concept A — The Concealed Exit *(Architectural Exit Connected to Room 111-B)*
- **Concept:** Beyond or inside **Room 111-B**, an unexpected exterior-style service exit or narrow unfinished vestibule door opens directly back onto the Avenida Benito Juárez sidewalk (reusing `ExitBackroomsRoutine()`).
- **Discovery Path:** Driven entirely by environmental evidence already in the game (`UnfinishedProjectPlan` -> East phantom corridor -> `RoomRegistrationRecord` -> `111-B` -> concealed exit door). Zero arbitrary pixel-hunting!
- **Advantages:**
  - Directly rewards exploring the hotel's core 07.6 mystery; gives Room 111-B both narrative and functional payoff.
  - Cleanest anti-softlock profile: reachable purely through navigation and observation, requiring zero hidden numeric codes.
  - 100% compatible with existing `ExitBackroomsRoutine()` and `PlayerProgressionService`.
- **Risks & Limitations:** Once discovered, feels relatively conventional unless the transition itself conveys spatial impossibility (e.g., an interior windowless room opening impossibly onto Ciudad Juárez street air).

### Concept B — The Impossible Threshold *(Comprehension-Gated Spatial Contradiction)*
- **Concept:** A seemingly ordinary hotel element (e.g., an interior door frame, mirror alcove, or sealed concrete bay such as the Reception ghost outline or Banquet East frame) initially behaves as a solid barrier or loops back until the player uncovers or observes a specific spatial contradiction (e.g., inspecting `UnfinishedProjectPlan` + `RoomRegistrationRecord`, or noticing an impossible alignment), after which the threshold becomes passable back to the surface.
- **Foreshadowing Without Spoiling:** Faint drafts, hairline concrete discontinuities, or mismatched floor tiling subtly suggest the surface is "thin" before it yields.
- **Advantages:** Strongly reinforces Remanencia's core rule that *perception and memory shape geography*.
- **Risks & Limitations:**
  - **Highest Softlock / Frustration Risk:** If a player misses an obscure condition or doesn't realize a previously solid wall changed state, they may think the game is broken.
  - Requires explicit state tracking to determine when the threshold "unlocks," creating subtle puzzle-dependency coupling.

### Concept C — The Leap of Faith *(Deliberate Agency Across Apparent Spatial Impossibility)*
- **Concept:** Rather than jumping off a lethal ledge, the player encounters an unmistakably anomalous architectural dead-end — for example, inside **Room 111-B**, a tall unfinished doorway or unglazed balcony/alcove framing **featureless overcast concrete haze / a bright surface-air glare / a seemingly sealed wall plane whose floor joint continues unbroken underneath it**. Environmental clues (footprints/dust stopping at the plane, the `+1.4 m` corridor drift log, faint distant street air or warm outdoor light seeping around the jamb) tell the attentive player that stepping deliberately forward across the apparent impossibility is intentional. Crossing the plane smoothly transitions the player back onto Avenida Benito Juárez!
- **Advantages:**
  - Delivers an unforgettable, signature **Remanencia** experiential moment (*agency + trust + spatial uncanny*) without lethal fall gimmicks or monster tropes.
  - Zero hard puzzle-code locks: even a player who enters via the silent Azucenas Manhole Easter Egg can observe the environmental cues and choose to step through!
- **Risks & Limitations:**
  - Requires careful visual art direction so the threshold reads as *"deliberately inviting and strange"* rather than *"unfinished collision bug / missing wall."*

### Comparative Ranking & Recommended Exit Architecture (`PROPOSED`)
| Exit Concept | Narrative Resonance | Player Agency | Softlock Safety | Synergy with Room 111-B | Rank |
| :--- | :---: | :---: | :---: | :---: | :-: |
| **Concept A — Concealed Exit (via 111-B)** | High | High | **Highest** | **Direct** | **#1 (Foundation)** |
| **Concept C — Leap of Faith (Deliberate Threshold)** | **Highest** | **Highest** | High (with clear cues) | **Direct (inside 111-B)** | **#1 (Paired Climax)** |
| **Concept B — Impossible Threshold (State-Gated)** | High | Moderate | Moderate (gating risk) | Good (better for entering 111-B) | **#2 (Optional Layer)** |

**Recommended Direction:** **Hybrid Concept A + C** — Place the return route inside **Room 111-B** (Concept A: discovered organically through the existing 07.6 clues with zero arbitrary searching), and stage the actual exit threshold inside Room 111-B as a quiet, unmistakable **Threshold of Trust** (Concept C: e.g., an impossible windowless concrete doorway/alcove filled with muted borderland daylight/haze and a continuous floor seam that requires the player to deliberately step through, transitioning seamlessly back to Avenida Benito Juárez). Keep Elevators A & B and *El Umbral de Lasser* strictly independent for future multi-floor / deeper-dimension increments!

---

## 9. Existing Transition Architecture Audit *(VERIFIED CURRENT IMPLEMENTATION — READ-ONLY)*

### 1. Exact Current Entry Mechanisms
| Entry Route | Controlling Scripts & Methods | Scene Objects & Coordinates | Progression & World-Context Effects |
| :--- | :--- | :--- | :--- |
| **Primary: Building A South Threshold Veil** | `SecretKeypadDoorController` (`door:building-a-south-secret-wedge`, code `581926`) → `SecretBackroomsTransitionController.Update()` (lines 133–141) → `EnterBackroomsRoutine()` (lines 467–551) | `BuildingA_SouthBay_StorageDoor` (`BorderCorridorScene`), `veilTriggerX = -11.94f`, `Z ∈ [-13.50, -11.15]`, `Y > -5f`. Spawns player at `Backrooms_SpawnPoint (17.80, -49.94, 8.80)`, yaw `-90°`. | Closes/relocks street door; claims World Core occupancy (`EnterSecretVault`); additively loads `BackroomsLevel1Scene`; sets `isPlayerInBackrooms = true`; calls `RecordBackroomsEntered(viaAzucenasManhole: false)` (`milestone:backrooms-entered`, `milestone:backrooms-entered-via-veil`, `checkpoint:backrooms-entered`); calls `RestoreLoadedBackroomsProgression()`; switches `RenderSettings` via `ApplyBackroomsAtmosphere()`. |
| **Secondary: Azucenas Manhole Silent Drop** | `SewerManholeInteractable.ClimbRoutine()` (lines 115–120, gated by `triggerBackroomsNoClipTwist`) → `SecretBackroomsTransitionController.TriggerManholeNoClipDropToBackrooms()` → `EnterBackroomsCeilingDropRoutine()` (lines 277–397) | `manhole_juarez_azucenas` → drops player from `Y = -47.92` onto one of 10 `RandomCeilingDropZonesXZ` (`PickRandomSafeDropZoneXZ()`). | Same additive load & atmosphere switch; calls `RecordBackroomsEntered(viaAzucenasManhole: true)` (`milestone:backrooms-entered-via-azucenas-manhole`, `checkpoint:backrooms-entered`). |

### 2. Exact Current Return Mechanism
- **Scene Object:** `Backrooms_ExitDoor_ToStreet` at `(19.86, -50.00, 8.80)` (flush on Entrance Vestibule East wall, `2.06 m` behind `Backrooms_SpawnPoint`) with `BackroomsExitDoorTrigger` (`interactionRange = 2.4f`).
- **Player Input Detection (`FirstPersonPlayer.cs`):**
  - Proximity poll (`lines 1263–1270`): `FindFirstObjectByType<BackroomsExitDoorTrigger>()` → sets `currentNearbyBackroomsExit`.
  - Prompt (`line 2884` / `8736`): `DrawBackroomsExitPrompt()` displays `[E] Abrir puerta y regresar a Av. Benito Juárez` / `[E] Open door and return to Av. Benito Juárez`.
  - Trigger (`lines 1086–1088` for `[E]` key; `lines 8442–8447` for left-click raycast): calls `cachedBackroomsTransition.RequestExitBackrooms()`.
- **Execution (`SecretBackroomsTransitionController.ExitBackroomsRoutine()`, lines 553–601):**
  1. Fades screen to black (`0.26 s`).
  2. Calls `RestoreOutdoorAtmosphere()` (re-enables suspended outdoor directional lights, restores skybox/ambient/fog).
  3. Teleports player to `streetExitReturnPosition = (-11.20, 0.24, -11.99)`, `streetExitReturnYaw = 90°`, relocks `outerStreetDoor`.
  4. Unloads `BackroomsLevel1Scene` asynchronously (`SceneManager.UnloadSceneAsync`).
  5. Calls `cachedApiClient.LeaveSecretVault("player-1")`.
  6. Sets `isPlayerInBackrooms = false`, `occupiedByOtherPlayer = false`, refreshes Threshold Veil.
  7. Calls `PlayerProgressionService.Shared.RecordReturnedFromBackroomsToCorridor()` (`milestone:returned-to-border-corridor-from-backrooms`) — never resets discoveries or checkpoints.
  8. Fades back in (`0.32 s`), restoring `CurrentWorldContext == PlayerWorldContext.SurfaceCorridor`.

### 3. Existing Independent Artifact (`Portal_DimensionalMirror_Root` / *El Umbral de Lasser*)
- Located at `(-19.70, -50.00, -8.20)` in the SW Chamber; controlled by `SecretRoomDimensionVaultController`. Requires Level-Ω Safe (`654321`) → *El Ojo del Umbral*. Currently toggles the visual vortex and records `milestone:lasser-portal-activated` / `checkpoint:lasser-portal-activated`, with **zero scene transition**. Keeping it separate from Nivel 0's surface return preserves it for deeper dimensional travel later.

### 4. What Must Change Later to Make the Entrance Entry-Only *(PROPOSED — NOT IMPLEMENTED)*
1. **Re-purpose `Backrooms_ExitDoor_ToStreet`:** Keep the physical double door GameObject at `(19.86, -50.00, 8.80)` intact, but configure `BackroomsExitDoorTrigger` with an `isEntryOnlySealed` / `ExitDoorMode` flag (or dedicated sealed-door inspect response) so `[E]` / click displays a restrained bilingual observation (*e.g., "La puerta no cede. Del otro lado solo se escucha concreto macizo."*) instead of calling `RequestExitBackrooms()`.
2. **Reuse `ExitBackroomsRoutine()` Safely for Alternate Exits:** Refactor `RequestExitBackrooms()` (keeping its existing zero-arg overload for backward compatibility during migration) to delegate to `RequestExitRemanencia(Vector3? customReturnPos = null, float? customReturnYaw = null, string optionalExitMilestoneId = null)`. Because `ExitBackroomsRoutine()` already encapsulates atmosphere restoration, additive unload, World Core `LeaveSecretVault`, `PlayerWorldContext` switch, and progression recording, **any alternate exit (door trigger OR step-through volume inside Room 111-B) can safely invoke it without duplicating transition logic!**
3. **Update `FirstPersonPlayer.cs` Detection:** Replace single-instance `FindFirstObjectByType<BackroomsExitDoorTrigger>()` (`line 1265`) with a cached array or proximity volume (`RemanenciaExitThresholdTrigger`) so multiple distinct thresholds (a sealed entry door + an active Room 111-B exit threshold) never shadow each other!
4. **Update Existing Automated Tests Intentionally:** `RemanenciaLevel0VisualTests` (`line 49`) checks `exitDoor.GetComponent<BackroomsExitDoorTrigger>() != null`; keeping `BackroomsExitDoorTrigger` attached in `SealedEntryOnly` mode preserves component presence while clearly testing the new rule in dedicated unit tests.

---

## 10. Save Integrity & Softlock Prevention Considerations *(VERIFIED + PROPOSED SAFEGUARDS)*

### Verified Save-System Resilience (`PlayerProgressionService.cs` & `NarrativeProgressionData.cs`)
- **Schema Compatibility:** `ProgressionSaveData.schemaVersion == 1`. `latestCheckpointId` is explicitly **decoupled from world spawn position** (`NarrativeProgressionData.cs:112`: *"strictly independent from world spawn transform"*). Quitting and relaunching the application always spawns the player safely on Avenida Benito Juárez in `BorderCorridorScene`, even if `latestCheckpointId == checkpoint:backrooms-entered`.
- **Atomic Persistence:** `PlayerProgressionService.SaveNow()` writes atomically with backup recovery and write-protection on corrupt files. Returning from Remanencia via `RecordReturnedFromBackroomsToCorridor()` only appends `milestone:returned-to-border-corridor-from-backrooms` and never wipes discoveries, codes, or artifacts.

### Mandatory Anti-Softlock Safeguards Before Activating Entry-Only Rule (`PROPOSED`)
1. **Atomic Rollout Order:** **NEVER** disable `Backrooms_ExitDoor_ToStreet` before Room 111-B and its alternative exit are fully built, capsule-walkability tested, and verified in PlayMode!
2. **Zero External-Puzzle Deadlocks:** The primary unconventional exit through Room 111-B must NOT require solving the underground sewer sequence or the Level-Ω Safe (`654321`), because a player entering via the Azucenas Manhole drop or exploring casually must always be able to discover the exit purely from in-hotel observation.
3. **Deterministic Walkability Proof:** Extend EditMode Physics capsule verification (`RemanenciaNorthWingExpansionTests` pattern) to prove a continuous unobstructed `PlayerRadius = 0.32 m` walkable path from `Backrooms_SpawnPoint (17.80, 8.80)` AND from all 10 `RandomCeilingDropZonesXZ` all the way to the Room 111-B exit threshold!
4. **Pause-Menu / Recovery Safety Valve (`UNRESOLVED DECISION`):** Decide whether the Pause Menu (`ESC`) should offer a non-destructive `"Return to Surface Street"` safety-recovery option (disabled or clearly labeled as unstuck/recovery) OR whether relying on quitting-to-title + discoverable exit is preferred for maximum immersion.
5. **World Core Occupancy Cleanup:** Ensure `OnDestroy()` (`SecretBackroomsTransitionController.cs:169`) and any alternate exit route always release `LeaveSecretVault("player-1")` so a disconnected session never leaves the vault locked in `"decoy_empty"` state.

---

## 11. Relationship Between Room 111-B & Future Remanencia Spaces *(PROPOSED)*

- **First Architectural Proof of Remanencia's Core Law:** Until now, spatial impossibility in Nivel 0 has only been *reported* in text (`UnfinishedProjectPlan`, `RoomRegistrationRecord`, `MaintenanceRecord`). Room 111-B makes the impossibility *physically traversable* without breaking the hotel's grounded northern-Mexican construction identity.
- **Gateway Without Premature Expansion:** Anchoring Room 111-B in the vacant East pocket (`X ∈ [+16.0, +24.8]`) preserves:
  - **North expansion reserve** (`Z > +34.0` / NW Stairwell Landing) for future stairwells or upper-floor access.
  - **South expansion reserve** (`Z < -12.0`) for future parking/loading deck (Module M2).
  - **West expansion reserve** (`X < -20.0`) for future mechanical annex / dimensional building connector (Module M3).
  - **Elevator A & B independence** (`Hotel_Elevator_Core` untouched; neither elevator is prematurely assigned as a surface exit).
  - ***El Umbral de Lasser* independence** (`Portal_DimensionalMirror_Root` remains reserved for deeper dimensional progression).

---

## 12. Proposed Sequence of Future Implementation Increments *(PROPOSED — NOT IMPLEMENTED)*

| Increment | Scope | Dependencies & Gates |
| :---: | :--- | :--- |
| **07.11** | **Room 111-B Physical Construction (Option A — Hidden East Passage & Room Shell)**<br>Split `NorthWing_OuterWall_East` at `Z ∈ [26.8, 28.4]`, build shadow-baffle offset, phantom East corridor (`X ∈ [16.13, 18.90], Z ∈ [20.20, 28.67]`), `111-B` plaque, and Room 111-B shell (`X ∈ [19.12, 24.52], Z ∈ [20.00, 24.80]`). **Existing exit door stays active during 07.11!** | Creator approval of Option A & Room 111-B visual tone; EditMode capsule walkability + `Z=20` solid perimeter test preserved. |
| **07.12** | **Room 111-B Interior Identity & Unconventional Exit Implementation (Hybrid A + C)**<br>Implement generalized `RequestExitRemanencia(...)` reusing `ExitBackroomsRoutine()`, add the Room 111-B unconventional exit threshold + clear environmental foreshadowing, and verify exit-to-street in PlayMode **while keeping entry door temporarily testable or flipping atomically once verified**. | Creator approval of Hybrid A + C exit concept & return-drop coordinates on Av. Juárez. |
| **07.13** | **Activate "Check-in Without Conventional Check-out" Rule**<br>Flip `Backrooms_ExitDoor_ToStreet` to sealed entry-only behavior with bilingual feedback; update `FirstPersonPlayer` multi-threshold handling; add automated anti-softlock walkability proofs from Spawn & all 10 Manhole drop zones to the Room 111-B exit. | Confirmed PlayMode sign-off on 07.12 alternate exit reliability. |
| **07.14+** | **Deeper Remanencia Vertical / Non-Euclidean Expansion**<br>Evaluate Elevator A/B assignment, NW Stairwell core, or an Option B non-Euclidean branch beyond Room 111-B (`X > +24.5`). | Separate creator design review. |

---

## 13. Unresolved Creator Decisions *(UNRESOLVED CREATOR DECISION)*

1. **Room 111-B Spatial Option *(RESOLVED & IMPLEMENTED IN 07.11)*:** **Option A — Hidden East Architectural Passage** (`X = +16.0, Z ∈ [+26.80, +28.40]`) is now physically constructed in `BackroomsLevel1Scene.unity` under `Hotel_Room111B_Anomaly`. Visual acceptance remains pending personal PlayMode inspection.
2. **Room 111-B Interior Identity *(UNRESOLVED CREATOR DECISION — 07.12)*:** Room 111-B is intentionally empty in 07.11 (weathered plaster shell only, zero furniture/props/clues/organisms). Decide whether its 07.12 interior should remain austere/minimal architectural space, host restrained period-mismatched hotel remnants, or frame a stark spatial anomaly.
3. **Interior-Only Entrance Appearance & One-Way Observation Illusion *(UNRESOLVED CREATOR DECISION — 07.12)*:** Decide whether to adopt an interior-only hotel-style entrance treatment on `Backrooms_ExitDoor_ToStreet` (strictly isolated inside `BackroomsLevel1Scene.unity`) and whether to pursue a single-shot entry-captured frosted exterior observation view or omit the observation illusion under the mandatory fallback rule (see Section 15).
4. **Alternative Exit Mechanism *(UNRESOLVED CREATOR DECISION — 07.12)*:** Confirm **Hybrid Concept A + C** (Concealed Threshold-of-Trust exit inside Room 111-B) vs. pure Concept A (concealed service door) vs. Concept B (state-gated threshold).
5. **Entry-Only Rollout Timing & Recovery Policy *(UNRESOLVED CREATOR DECISION — 07.13)*:** Confirm that `Backrooms_ExitDoor_ToStreet` flips to entry-only only after Room 111-B's alternative exit is PlayMode-verified, and decide whether the Pause Menu should include an emergency unstuck return option.
6. **Surface Return Location *(UNRESOLVED CREATOR DECISION — 07.12)*:** Decide whether exiting via Room 111-B returns the player to `Building A South` (`-11.20, 0.24, -11.99`) or to a subtly displaced spot along Avenida Benito Juárez.

---

## 14. Implementation 07.11 — As-Built Room 111-B Architecture *(IMPLEMENTED IN 07.11)*

Implementation 07.11 physically constructed approved **Option A — Hidden East Architectural Passage** inside `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity` via `BackroomsLevel1Builder.MigrateRemanenciaRoom111BAndSave()`, under dedicated root `BackroomsLevel1_Root/Backrooms_Architecture/Hotel_Room111B_Anomaly`.

### A. Exact As-Built Coordinates, Dimensions & Subgroups *(IMPLEMENTED IN 07.11)*

| Subgroup / Element | As-Built World Bounds / Position (m) | Clear Dimensions | Material & Architectural Role |
| :--- | :--- | :--- | :--- |
| **Retained Wall In-Place Edit:** `NorthWing_OuterWall_East` (+ `_Baseboard`) | `X ∈ [+15.87, +16.13]`, `Z ∈ [+11.935, +26.800]` | Length `14.865 m`, Height `2.85 m` | Existing GameObject/Transform/Collider `fileID`s preserved in-place; Banquet Hall East wall and `Z = +20.0` perimeter assertion remain 100% solid. |
| **`Room111B_ConcealedOpening`** | Opening at `X = +16.00`, `Z ∈ [+26.80, +28.40]`; North stub `Z ∈ [+28.40, +34.065]`; Overhead transom `Y ∈ [-47.80, -47.15]`; Offset baffle wall at `X = +17.55` (`West face +17.43`, `Z ∈ [+25.28, +28.92]`); North seal `Z = +28.80`; East shoulder `Z = +25.40` (`X ∈ [+17.55, +19.01]`). | Opening `1.60 m` wide × `2.20 m` high; South dogleg baffle throat **`1.30 m` clear width** (`X ∈ [+16.13, +17.43]`). | Raw concrete reveal trim mirrors the hotel's sealed concrete anchor frames (`mats.ColumnConcrete`); offset concrete baffle wall (`mats.WallConcrete`) completely occludes the East Corridor from normal North Guest Corridor approach (`X ≤ +13.5`). Flush non-glowing metal floor joint cue (`Room111B_Opening_FloorJointCue`). |
| **`Room111B_EastCorridor`** | Clear interior `X ∈ [+16.13, +18.90]`, `Z ∈ [+20.20, +28.68]`; Floor slab `X ∈ [+16.25, +19.20]`, `Z ∈ [+19.90, +29.00]` (`Ytop = -50.0015`); Ceiling slab matching (`Ybot = -47.1475`); South wall inner face `Z = +20.20`; Dividing doorway wall `X = +19.01` (`thickness 0.22 m`), doorway `Z ∈ [+21.70, +23.10]`. | Width `2.77 m` (main hall) / `1.30 m` (baffle throat); Total length `8.48 m`; Clear height `2.85 m`; Doorway `1.40 m` masonry clear (`1.30 m` between trim jambs) × `2.20 m` high. | Raw unfinished concrete (`mats.WallConcrete`), engaged West structural pilaster at `Z = +22.40`, subtle off-axis misaligned dark floor expansion seam (`X = +16.82`), older brushed-metal doorway jambs (`mats.ElevatorDoorMetal`) & floor threshold plate, 1 emissive-only ceiling strip (`0` Light components). |
| **`Room111B_RoomShell`** | Clear interior `X ∈ [+19.12, +24.52]`, `Z ∈ [+20.00, +24.80]`; Floor & ceiling slabs `X ∈ [+19.15, +24.85]`, `Z ∈ [+19.70, +25.10]`; Perimeter walls East `X = +24.65`, South `Z = +19.87`, North `Z = +24.93` (`outerT = 0.26 m`). | Footprint **`5.40 m × 4.80 m` (`25.92 m²`)**, Clear height `2.85 m`. Extends **`+4.52 m` East** beyond original hotel boundary `X = +20.0`. | Weathered plaster walls (`mats.WallPlaster`) subtly contrasting with raw corridor concrete, 2 engaged East pilasters (`mats.ColumnConcrete`), 1 overhead soffit beam, 1 warm emissive-only ceiling strip (`0` Light components). **Strictly empty shell** — zero furniture, props, documents, organisms, scripts, or lights. |
| **`Room111B_Signage`** | `Room111B_Plaque_111B` at `(18.888, -48.420, 23.450)`, rotation `Euler(0, -90, 0)`, scale `(0.28, 0.11, 0.016)`. | Upright `0.28 m × 0.11 m` matte plaque facing West (`-X`). | Dedicated `192×64` procedural stencil texture `T_Remanencia_RoomPlaque_111B.png` & matte non-emissive material `Remanencia_Level0_RoomPlaque_111B.mat`, matching Rooms `101–110` conventions. |

### B. Documented Deviations from 07.10 Proposal *(IMPLEMENTED IN 07.11)*

1. **Baffle Wall Offset (`X = +17.55` instead of proposed `X = +16.85`):** The 07.10 proposal suggested an offset baffle around `X = +16.85`, which (accounting for wall half-thicknesses `0.13 m + 0.12 m = 0.25 m`) would have left only `0.47 m` of clear space — narrower than the player's `Ø 0.64 m` `CharacterController` capsule! Moving the baffle centerline to **`X = +17.55`** (`West face = +17.43`) against `NorthWing_OuterWall_East` (`East face = +16.13`) yields a comfortable **`1.30 m` clear dogleg throat (`2.03×` capsule diameter)** while still occluding 100% of approach sightlines from `X ≤ +13.5` down the North Guest Corridor.
2. **Monolithic Baffle-to-Corridor Step-Out (`Z = +25.40`):** Rather than leaving a hollow inaccessible dead pocket behind the baffle wall (`X ∈ [+17.67, +18.90], Z ∈ [+25.40, +28.80]`), `Room111B_Baffle_EastShoulder` (`Z = +25.40`) cleanly closes the step-out from `X = +17.55` to `X = +19.01`, preventing exterior void leaks and simplifying enclosure guarantees.
3. **Doorway Wall South Extension (`minZ = +19.87` instead of `+20.09`):** Extending `Room111B_DoorWall_South` `22 cm` further South to `Z = +19.87` seals seamlessly into `Room111B_Shell_WallSouth` (`Z = +19.87`), eliminating any corner gap at the corridor-to-room junction.
4. **0.5 mm Slab Elevation Stagger (`-50.0015` / `-50.0010` vs `-50.0010`):** Overlapping `NorthWing_FloorSlab`, `Room111B_EastCorr_FloorSlab`, and `Room111B_Shell_FloorSlab` by `5 cm` horizontally with `0.5 mm` vertical separation eliminates both collider seams and Z-fighting shimmer while staying imperceptible to `CharacterController`.

### C. Final Concealed-Opening Discovery Sequence & Navigation Verification *(IMPLEMENTED IN 07.11)*

1. Player inspects `Hotel_Evidence_UnfinishedProjectPlan` (`07.6`) and `Hotel_Evidence_RoomRegistrationRecord` (`07.6`), learning of an unbuilt East corridor and unlisted Room `111-B`.
2. Walking East along the North Guest Corridor past Rooms `110 → 109 → 108 → 107`, the corridor narrows at `NorthWing_NorthCorridor_EastOverlookNib` (`X = +13.6`) and terminates visually against `Room111B_ConcealedBaffle_Wall` (`X = +17.43`), reading as a recessed concrete dead-end bay identical in language to the hotel's sealed concrete expansion anchors.
3. Moving past `X = +13.6` up to `X ≈ +15.0..+16.5`, the player notices the flush floor joint continuing through the concrete reveal and discovers the `1.30 m` clear South dogleg passage behind the baffle wall.
4. Walking South into `Room111B_EastCorridor` (`2.77 m` wide, cool emissive ceiling strip, misaligned floor joint) leads directly to the brushed-metal doorway marked **`111-B`** (`1.40 m` masonry clear × `2.20 m` high) and into the empty `5.40 m × 4.80 m` weathered-plaster Room 111-B shell extending East past `X = +20.0`.
5. **Clearance & Existing Perimeter Preservation (`VERIFIED`):** Automated `Physics.OverlapCapsule` (`r = 0.32 m, h = 1.80 m`) passes continuously along the entire route (`0.25 m` sampling step), while existing `RemanenciaNorthWingExpansionTests` perimeter linecasts at North `(X=0, Z=33..35)`, East `(X=15..17, Z=20.0)`, and West `(X=-15..-17, Z=20.0)` remain 100% solid.

---

## 15. Permanent Surface-World Preservation & Read-Only One-Way Observation Feasibility *(VERIFIED + PROPOSED FUTURE DESIGN)*

### A. Permanent Non-Negotiable Rule: Avenida Benito Juárez Exterior Entrance is Immutable *(PERMANENT PROJECT LAW)*
- `unity-client/Assets/Scenes/BorderCorridorScene.unity` and the real-world street-facing door **`BuildingA_SouthBay_StorageDoor`** (`fileID 580649218`, `37` nodes, `SecretKeypadDoorController.cs`, and its `8` exterior materials: `West_SecretWedge_TaupeFrame.mat`, `West_SecretWedge_DoorMetal.mat`, `West_StoneMasonry.mat`, `West_DarkCornice.mat`, `West_SecretWedge_KeypadBody.mat`, `West_Aluminum.mat`, `West_SecretWedge_KeypadLed.mat`, `West_SecretWedge_KeypadKeys.mat`) **MUST NEVER BE MODIFIED** for Remanencia.
- **Verified in 07.11 (`VERIFIED CURRENT IMPLEMENTATION`):** `BorderCorridorScene.unity` retains exact SHA-256 `0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B`, and zero exterior door assets or scripts are shared with `BackroomsLevel1Scene.unity`.

### B. Separation Between Exterior Door and Future Interior-Only Interpretation *(PROPOSED FUTURE DESIGN — NOT IMPLEMENTED)*
- Any future hotel-style entrance appearance (double door leaves, interior glass panes, restrained hotel hardware) must exist **exclusively inside `BackroomsLevel1Scene.unity`** on `Backrooms_ExitDoor_ToStreet` (`X = +19.86, Y = -50.00, Z = +8.80`) using strictly independent Remanencia-scoped materials and meshes (`Remanencia_EntranceInterior_*`), never sharing mutable materials or prefabs with `BorderCorridorScene.unity`.

### C. Read-Only Technical Feasibility Assessment: Optional One-Way Exterior Observation Illusion *(READ-ONLY AUDIT IN 07.11 — NOT IMPLEMENTED)*

Audited strictly read-only against `SecretBackroomsTransitionController.cs`, `BorderCorridorScene.unity`, and `ProjectSettings/TagManager.asset`:

| Evaluation Criterion | Verified Codebase Findings & Feasibility Assessment |
| :--- | :--- |
| **1. Additive `BorderCorridorScene` Availability Without Editing It** | `BorderCorridorScene` (`10,997` GameObjects) remains loaded additively while `BackroomsLevel1Scene` runs at `Y = -50.0 m`. **Critical caveat (`SecretBackroomsTransitionController.cs:651–670`):** `ApplyBackroomsAtmosphere()` disables the outdoor `Directional` sun light (`suspendedOutdoorLights`) and overwrites global `RenderSettings` (`skybox = null`, `ambientLight = RemanenciaLevel0AmbientLight`, `fog = ExponentialSquared 0.028`). A live secondary camera pointed at Avenida Benito Juárez during Remanencia would render a dark, skybox-less, fogged street unless fragile `RenderPipelineManager.beginCameraRendering`/`endCameraRendering` hooks temporarily swapped global `RenderSettings` and re-enabled the sun per sub-pass. |
| **2. Dedicated Low-Cost Camera + RenderTexture** | A live throttled camera (`256×256`–`384×256` RT, `2–5 Hz` only while player is within `4 m` of the interior door, shadows off) uses little VRAM (`~0.2–0.4 MB`), **but** culling & drawing `10,997` surface GameObjects plus mutating global `RenderSettings` mid-frame carries noticeable frame-time spikes and URP ambient/fog bleed risk on low-end GPUs. |
| **3. Static / Entry-Snapshot Exterior Composition (Recommended Alternative)** | Capturing a **single one-shot RenderTexture (`384×384`, ~0.56 MB)** from `(-11.85, 1.50, -11.99)` facing East (`+X`) during `EnterBackroomsRoutine()` *immediately before* `ApplyBackroomsAtmosphere()` disables outdoor lighting — OR displaying a pre-baked `512×512` frosted/misted Avenida Benito Juárez glass texture (`~0.34 MB`) — delivers **zero per-frame draw-call cost**, zero `RenderSettings` churn, exact time-of-day matching (for one-shot entry capture), and a restrained uncanny frosted-glass impression. |
| **4. Preventing Player, Microfauna & Remanencia Geometry from Appearing** | **100% geometrically isolated:** `BackroomsLevel1Scene`, the player (`Y = -49.94`), and microfauna sit `50 m` below Avenida Benito Juárez (`Y ≈ 0.0 m`). An observation camera at `Y = +1.50 m` facing East (`+X`) with `farClipPlane = 45 m` cannot intersect Remanencia geometry. (`TagManager.asset` currently defines only built-in layers `0, 1, 2, 4, 5`; adding a dedicated `RemanenciaOnly` layer is optional defense-in-depth if live rendering were ever attempted.) |
| **5. Acoustic Isolation (Player Sounds Never Reach Exterior)** | Verified `BorderCorridorScene.unity` has `1` `AudioListener` (on the player) and `0` scene-baked `AudioSource`s; surface NPCs do not subscribe to Unity audio output. Player footsteps/actions already cannot reach the surface world. Any subtle muted street ambience behind the interior door would be a purely local 3D `AudioSource` (`maxDistance ≤ 3.5 m`) inside `BackroomsLevel1Scene`. |
| **6. 2 GB VRAM & Performance Budget Impact** | Live 60 Hz RenderTexture rendering of `10,997` objects is **unsafe** for the 2 GB VRAM / low-end GPU target. A single-shot entry-capture RT (`≤ 0.56 MB`) or pre-baked compressed texture (`≤ 0.34 MB`) is completely safe. |
| **7. Complete Independence from Exterior Door Geometry/Assets** | **Confirmed 100% independent.** `BuildingA_SouthBay_StorageDoor` shares zero materials, meshes, or scripts with `Backrooms_ExitDoor_ToStreet`. Any future interior-only visual panel would live solely in `BackroomsLevel1Scene.unity`. |

### D. Mandatory Fallback Rule *(NON-NEGOTIABLE)*
> **If a reliable, performance-safe, artifact-free one-way observation illusion cannot be implemented without modifying the surface-world entrance or destabilizing URP rendering/VRAM budgets: DO NOT IMPLEMENT THE ILLUSION.** Leave the existing entrance completely unchanged, including its interior-facing appearance. Preserving surface-world fidelity always takes precedence over visual spectacle.

---

## 16. Future Rollout & Anti-Softlock Requirements *(PROPOSED FUTURE DESIGN — NOT IMPLEMENTED IN 07.11)*

- **07.11 *(IMPLEMENTED)*:** Physical Room 111-B & concealed East passage constructed; `Backrooms_ExitDoor_ToStreet`, `BackroomsExitDoorTrigger`, `SecretBackroomsTransitionController.cs`, `FirstPersonPlayer.cs`, progression IDs, and `BorderCorridorScene.unity` remain **100% unchanged and operational**.
- **07.12 *(PROPOSED FUTURE DESIGN)*:** Author Room 111-B interior identity, evaluate interior-only entrance treatment & optional one-way observation (subject to the Mandatory Fallback Rule above), and implement the unconventional alternative exit (recommended: Hybrid Concept A + C Threshold of Trust connected to Room 111-B) reusing `ExitBackroomsRoutine()`.
- **07.13 *(PROPOSED FUTURE DESIGN)*:** Activate **"Check-in Without Conventional Check-out"** *only after* the 07.12 alternative exit is constructed, capsule-walkability tested from `Backrooms_SpawnPoint` AND all `10` `RandomCeilingDropZonesXZ`, and personally accepted in PlayMode. Keep `Backrooms_ExitDoor_ToStreet` visible with restrained bilingual feedback when tried; preserve World Core `LeaveSecretVault` cleanup and `ProgressionSaveData` (`schemaVersion == 1`) integrity.
- **07.14+ *(PROPOSED FUTURE DESIGN)*:** Evaluate vertical access (Elevators A/B, NW Stairwell reserve), non-Euclidean spaces, and deeper Remanencia territories in separate increments.
