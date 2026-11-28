# REMANENCIA — Functional Passenger Elevator (Bay 1 South)
## Forensic Engineering Audit & Vertical Transportation Design (07.25)

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia** *(proper name — untranslated in ES/EN)*  
**Territory:** **Nivel 0 — Hotel de las Palomas** (`unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`)  
**Repository:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity-AI-Hackathon-Edition`  
**Expected Branch:** `product/unity-fellowship-2027`  
**Reference Repository:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity` *(strictly READ-ONLY; untouched)*  
**Document Status:** **IMPLEMENTATION 07.25 — STRICTLY READ-ONLY FORENSIC ENGINEERING AUDIT & DESIGN (ZERO SCENE, BUILDER, SCRIPT, TEST, MATERIAL, OR EXISTING-DOC MODIFICATIONS)**

> **Mandatory Status & Measurement Labels:**
> - **`VERIFIED EXISTING`** — Directly extracted from serialized `BackroomsLevel1Scene.unity`, `BorderCorridorScene.unity`, and C# source code at commit `3dcb66c`.
> - **`PROPOSED`** — Concrete geometric, structural, or software architecture target for future authorized `07.26–07.30` increments; **not implemented**.
> - **`ESTIMATED`** — Projected transit timings, cabin areas, or resource budgets pending prototyping.
> - **`UNRESOLVED`** — Open creator decisions required before any construction or runtime coding begins.
>
> **Engineering Disclaimer:** All architectural and mechanical representations proposed in this document are **gameplay and spatial-architecture designs** for Unity/URP — they do **not** constitute civil-engineering or mechanical-engineering load certification.

---

## 1. Executive Summary *(PROPOSED)*

Following PlayMode acceptance of **07.24 (Twin Elevator Bay Architecture)**, the Upper Elevator Waiting Area establishes the approved static contrast:
- **Bay 1 — South (`Z = +5.750 m`):** Finished-looking older hotel elevator facade — **sole candidate for functional passenger operation** connecting **Nivel 0 (`Y = -50.000 m`) ↔ Upper Stratum (`Y = -46.400 m`)** across a **`3.600 m`** vertical rise.
- **Bay 2 — North (`Z = +7.850 m`):** Permanently unfinished, sealed, non-functional architectural construction bay — **strictly excluded from all mechanical activation**.

This forensic audit verifies every millimeter of the existing shaft, slabs, doors, `FirstPersonPlayer` `CharacterController`, interaction pipeline, and save schema, uncovering **four critical engineering constraints** that govern safe implementation:

1. **Zero Internal Divider Wall Today (`VERIFIED EXISTING`):** Behind `Hotel_ElevatorCore_FrontPierMid` (`X ∈ [+3.150, +3.410]`), Bay 1 and Bay 2 share one empty cavity (`X ∈ [+1.240, +3.100], Z ∈ [+4.440, +9.160]`). An independent `0.240 m` concrete shaft divider at `Z = +6.800` (`Z ∈ [+6.680, +6.920]`) is mandatory before Bay 1 can ever open.
2. **Bi-Parting Door Pocket Constraint (`VERIFIED EXISTING`):** Bay 1 has `0.625 m` of South pocket (`Z ∈ [+4.440, +5.065]`) but only **`0.245 m`** between its North jamb (`Z = +6.435`) and the proposed divider (`Z = +6.680`). Stopping the shaft divider at `X = +3.060` with a steel pocket bulkhead at `Z = +7.040` (strictly behind `0.730 m`-wide `FrontPierMid`, `+0.125 m` clear of Bay 2!) enables true symmetrical bi-parting doors opening **`1.150 m` clear (`1.80×` capsule `Ø 0.640 m`)** while keeping Bay 2 100% hermetically sealed!
3. **Only Monolithic `Backrooms_Ceiling` Needs a Shaft Cutout (`VERIFIED EXISTING`):** `UpperElev_Waiting_FloorSlab` (`GO=1077748815`) and `UpperElev_Waiting_CeilingSlab` (`GO=1736100969`) both stop at `X_min = +3.150` — **zero cutout is needed in either upper floor/ceiling slab!** Furthermore, the cabin roof at the Upper Stop (`Y = -44.040 m`) sits **`+0.490 m` below** the Upper Stratum ceiling soffit (`Y = -43.550 m`), fitting cleanly without rooftop protrusion.
4. **Explicit Synchronized `CharacterController.Move()` Beats Parenting (`VERIFIED CODE`):** Parenting the player (`BorderCorridorScene`) to an additive-scene cabin (`BackroomsLevel1Scene`) introduces both additive-scene ownership hazards and `verticalVelocity = -1.5f` ground-snap jitter (`FirstPersonPlayer.cs:1130–1146`). **Approach B (Explicit Synchronized Passenger Displacement)** keeps the player strictly in `BorderCorridorScene`, eliminates clipping by ordering player/cabin moves by direction, preserves free camera look, and requires zero save-schema changes (`schemaVersion == 1`).

---

## 2. Repository Baseline & Exact HEAD *(VERIFIED EXISTING)*

- **Repository Path:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity-AI-Hackathon-Edition`
- **Branch:** `product/unity-fellowship-2027`
- **Baseline HEAD SHA:** **`3dcb66c5ec712ba9d45e1f55b701a31e4b9321b5`** (`feat(remanencia): differentiate upper elevator bays` — exact match with `origin/product/unity-fellowship-2027`; working tree 100% clean prior to creating this document)
- **Reference Repository (`C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity`):** Verified untouched.

### Baseline SHA-256 Integrity Record (`VERIFIED EXISTING`)
| File | Baseline SHA-256 (`3dcb66c`) |
| :--- | :--- |
| `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity` | `06BA60F1FF6C0E84D81EA1544D1B4AE0766E00B358B32C1F1E216900DC583D30` |
| `unity-client/Assets/Scenes/BorderCorridorScene.unity` | `0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B` |
| `unity-client/Assets/Scripts/FirstPersonPlayer.cs` | `3ECA2F5FF72CD572A9BCCE5B8A53B33D4EDC33A8B53BC321976AA71957775287` |
| `unity-client/Assets/Scripts/SecretBackroomsTransitionController.cs` | `6BD48DFF06F3365C02714634EDA538AC0E26A9480CEEF248023BCC85F36CBF98` |
| `unity-client/Assets/Editor/UpperElevatorBayArchitectureBuilder.cs` | `AEBADD2841854ADFB6E3B754254EC65508BFA959988EB82BB4A369D7F33F17CE` |

---

## 3. Verified Lower Elevator Core Geometry (`Hotel_Elevator_Core`) *(VERIFIED EXISTING)*

Directly verified from `BackroomsLevel1Scene.unity` (`3dcb66c`):
- **Root:** `Hotel_Elevator_Core` (`GO=331299473`, `TR=331299474`, exact **`35` children**).
- **Outer Structural Shell:** `X ∈ [+1.000, +3.410]` (`2.410 m`), `Y ∈ [-50.000, -47.150]` (`2.850 m`), `Z ∈ [+4.200, +9.400]` (`5.200 m`).
- **Shared Hollow Cavity Behind Closed Doors:** `X ∈ [+1.240, +3.100]` (`1.860 m` to door back; `1.910 m` to `FrontPier` inner face `+3.150`), `Z ∈ [+4.440, +9.160]` (`4.720 m` combined), `Y ∈ [-50.000, -47.150]` (`2.850 m`).
- **Key Lower Bay 1 Serialized Components:**
  - `Hotel_ElevatorCore_BackWall` (`GO=1951929040`): `X ∈ [+1.000, +1.240], Z ∈ [+4.140, +9.460]` (backed immediately West by `Hotel_Room101_EastWall` `GO=1712248583` at `X ∈ [+0.890, +1.110]` — West shaft expansion is impossible without hitting Room 101!).
  - `Hotel_ElevatorCore_SouthWall` (`GO=585933301`): `X ∈ [+0.940, +3.460], Z ∈ [+4.200, +4.440]` (backed South by `Pillar_CentralHall_9` `GO=1160501356` at `Z ∈ [+3.660, +4.340]`).
  - `Hotel_ElevatorCore_FrontPierS` (`GO=1166525667`, `Z ∈ [+4.135, +5.065]`), `FrontPierMid` (`GO=1160025726`, `Z ∈ [+6.435, +7.165]`), `FrontHeader` (`GO=1472619301`, `Y ∈ [-47.800, -47.150]`).
  - `Hotel_ElevatorBay_1_DoorLeafS/N` (`GO=2004914621 / 1307326071`): `X ∈ [+3.100, +3.180]`, `Y ∈ [-50.000, -47.800]` (`2.200 m`), `Z ∈ [+5.000, +5.760]` & `[+5.740, +6.500]` (`1.500 m` leaf span behind `1.370 m` pier opening `Z ∈ [+5.065, +6.435]`, centered **`Z = +5.750`**).
  - Adjacent Lobby Fixtures: `Hotel_Sign_ElevatorsHeader` (`GO=120906857`), `Hotel_Sign_ElevatorNotice` (`GO=2080536146`, `"FUERA DE SERVICIO"`), `Hotel_Elevator_CallPanelPlate` (`GO=1622816346`, `Z = +6.800`), `CallButtonUp/Down` (`GO=823213806 / 1114928462`), `Hotel_Lobby_Beam_West` (`GO=1693310821`, `X ∈ [+3.250, +3.550], Y ∈ [-47.490, -47.150]`).

---

## 4. Verified Upper Bay 1 & Bay 2 Geometry (07.24 As-Built) *(VERIFIED EXISTING)*

Directly verified from `BackroomsLevel1Scene.unity` (`3dcb66c`):
- **Upper Waiting Area Clear Interior:** `X ∈ [+3.410, +6.200]` (`2.790 m`), `Z ∈ [+4.200, +9.400]` (`5.200 m`), `Y_floor = -46.400 m`, `Y_ceil = -43.550 m` (`2.850 m` headroom).
- **Upper Core Facade Structure:**
  - `UpperElev_Waiting_WestCoreBackingWall` (`GO=268893405`): solid concrete `X ∈ [+3.150, +3.350]`, `Y ∈ [-46.600, -43.550]`, `Z ∈ [+4.200, +9.400]`.
  - `UpperElev_Waiting_CorePierS` (`GO=1232886118`, `Z ∈ [+4.200, +5.065]`), `CorePierMid` (`GO=522812839`, `Z ∈ [+6.435, +7.165]`), `CorePierN` (`GO=965099460`, `Z ∈ [+8.535, +9.400]`), `CoreHeader` (`GO=656398235`, `Y ∈ [-44.200, -43.550]`) — all at `X ∈ [+3.350, +3.410]`.
  - `UpperElev_Support_WaitingCorePlinthW` (`GO=1626614601`): `X ∈ [+3.150, +3.410]`, `Y ∈ [-46.940, -46.600]`, `Z ∈ [+4.200, +9.400]`.
- **Upper Bay 1 — Finished Static Facade (`Z = +5.750`, `1.370 m × 2.200 m`):**
  - `UpperElevBay1_DoorLeafS/N` (`GO=1440242510 / 1567884466`): `X ∈ [+3.358, +3.398]`, `Y ∈ [-46.400, -44.200]`, `Z ∈ [+5.065, +5.755]` & `[+5.745, +6.435]`, `ElevatorDoorMetal`.
  - `UpperElevBay1_FrameJambS/N/Header/Sill` (`GO=1615858687 / 892475867 / 1117518460 / 552512289`): `X ∈ [+3.350, +3.442]`.
  - `UpperElev_SealedBay_1_SealedBackingPanel` (`GO=1797691747`): `X ∈ [+3.346, +3.356]`, solid hidden collider.
- **Upper Bay 2 — Unfinished Sealed Facade (`Z = +7.850`, `1.370 m × 2.200 m`):**
  - `UpperElevBay2_SolidMasonryInfill` (`GO=1297852505`), `ClosurePlate` (`GO=1118101901`), `ConcreteJambS/N/Header`, `SubframeRailS/N/Top`, `AnchorPlate*`, `WeatheredPlasterCourse` — **PERMANENTLY UNTOUCHED & SEALED IN ALL FUTURE PHASES.**

---

## 5. Exact Vertical Shaft & Slab Constraints *(VERIFIED + PROPOSED)*

Auditing every serialized object intersecting the Bay 1 vertical shaft prism **`X ∈ [+1.000, +3.410], Z ∈ [+4.200, +6.920], Y ∈ [-50.300, -43.350]`**:

| Slab / Barrier | Verified Serialized Bounds (`3dcb66c`) | Intersects Bay 1 Shaft (`X ∈ [1.24, 3.15], Z ∈ [4.44, 6.68]`)? | Required Future Action (`07.26` — `PROPOSED`) |
| :--- | :--- | :---: | :--- |
| **`Backrooms_Floor`** (`GO=1532896222`) | `X ∈ [-20.30, +20.30]`, `Y ∈ [-50.300, -50.000]`, `Z ∈ [-12.30, +12.30]` | Bottom boundary (`Y = -50.000`) | **Two options:** *(Preferred)* Split locally or leave 100% solid and use a `0.040 m` beveled low-profile steel cabin floor deck (`Y_top = -49.960`, well inside `stepOffset = 0.300 m`) — **zero floor-slab split!** *(Alt: 4-slab pit cutout `0.140 m` deep down to `Y = -50.140` for flush `0.000 m` deck.)* |
| **`Backrooms_Ceiling`** (`GO=322800857`) | `X ∈ [-20.30, +20.30]`, `Y ∈ [-47.150, -46.950]`, `Z ∈ [-12.30, +12.30]` | **YES — BLOCKS SHAFT** (`4.278 m²` overlap) | **MANDATORY 4-SLAB PERIMETER SPLIT** around exact Bay 1 shaft cutout **`X ∈ [+1.240, +3.150], Z ∈ [+4.440, +6.680]`** (retain `Backrooms_Ceiling` `GO=322800857` in-place as the East slab `X ∈ [+3.150, +20.300]`, adding West `[-20.30, +1.24]`, South `[+1.24, +3.15] × [-12.30, +4.44]`, and North `[+1.24, +3.15] × [+6.68, +12.30]` — keeping Bay 2 `Z ∈ [+6.92, +9.16]` **100% sealed overhead!**). |
| **`UpperElev_Waiting_FloorSlab`** (`GO=1077748815`) | `X ∈ [+3.150, +6.420]`, `Y ∈ [-46.600, -46.400]`, `Z ∈ [+3.980, +9.400]` | **NO** (`X_min = +3.150` stops flush at shaft East wall!) | **ZERO CUTOUT NEEDED!** Remains 100% intact. |
| **`UpperElev_Waiting_CeilingSlab`** (`GO=1736100969`) | `X ∈ [+3.150, +6.420]`, `Y ∈ [-43.550, -43.350]`, `Z ∈ [+3.980, +9.400]` | **NO** (`X_min = +3.150`!) | **ZERO CUTOUT NEEDED!** Remains 100% intact. |
| **`UpperElev_Waiting_WestCoreBackingWall`** (`GO=268893405`) | `X ∈ [+3.150, +3.350]`, `Y ∈ [-46.600, -43.550]`, `Z ∈ [+4.200, +9.400]` | **YES — Upper Bay 1 doorway** (`Z ∈ [+5.065, +6.435], Y ∈ [-46.40, -44.20]`) | Split cleanly around Bay 1 opening ONLY (preserve solid piers `Z ∈ [4.20, 5.065]`, `Z ∈ [6.435, 9.400]` — keeping Bay 2 100% backed! — plus solid transom `Y ∈ [-44.20, -43.55]` & sill kick `Y ∈ [-46.60, -46.40]`). |
| **`UpperElev_Support_WaitingCorePlinthW`** (`GO=1626614601`) | `X ∈ [+3.150, +3.410]`, `Y ∈ [-46.940, -46.600]`, `Z ∈ [+4.200, +9.400]` | **NO** (sits directly under `X ≥ +3.150` portal sill!) | Remains **100% intact**, serving as the structural upper threshold sill beam directly between the lower `FrontHeader` (`-47.150`) and upper `FloorSlab` (`-46.600`)! |
| **Gallery / Vestibule / Overlook / East Overhang** | `Z ≥ +9.400` or `X ≥ +3.410` | **NO** | **ZERO interference.** |

### Vertical Cross-Section Diagram (Looking North at `Z = +5.750` — `VERIFIED + PROPOSED`)
```text
Y (m)     X = +1.00   +1.24        +3.00 +3.15  +3.41              +6.20
          |    |       |            |     |      |                  |
-43.35  --+----+-------+------------+-----+------+------------------+-- Upper Roof Cap
-43.55  --|--+ | [SHAFT CAP Y=-43.55]     |======| Upper Ceiling ===|-- (0.49m clearance!)
-44.04  --|  | |   +----------------+     | Core |                  |
-44.20  --|  | |   |  CABIN ROOF    |     |Header|                  |
          |  | |   |  (UPPER STOP)  |     |======|   UPPER WAITING  |
          |  | |   |  Interior      |     |Door  |       AREA       |
          |  | |   |  1.54 x 1.56 m | <-> |1.15m |  (2.79 x 5.20 m) |
          |  | |   |  H = 2.24 m    |     |Open  |                  |
-46.40  --|  | |   +================+=====+======+==================+-- Upper Floor (-46.400)
-46.60  --|  | |     (Plenum)       |Plinth|  Upper Floor Slab      |
-46.95  --|  | |   [OPEN SHAFT]     +-----+-------------------------+-- Backrooms_Ceiling top
-47.15  --|  | |   [CUTOUT IN]      |Front| Lower Lobby Soffit      |   Backrooms_Ceiling bot
-47.80  --|  | |   [CEILING ONLY]   |Head |                         |
          |  | |                    |=====|                         |
          |  | |   +----------------+     |     NIVEL 0 MAIN LOBBY  |
          |  | |   |  CABIN LOWER   | <-> |Door                     |
          |  | |   |  STOP (Nivel0) |     |1.15m                    |
-50.00  --+==+=+===+================+=====+=========================+-- Backrooms_Floor (-50.000)
-50.30  --+---------------------------------------------------------+-- Slab bottom
          ^Room 101    ^Bay 1 Shaft       ^Landing Door Plane
           EastWall     (X=1.24..3.15)     (X=3.10..3.18 coplanar!)
```

---

## 6. Internal Shaft Divider Feasibility & Door-Pocket Engineering *(PROPOSED)*

### A. Divider Wall (`Z = +6.800 m`)
- **Geometry (`PROPOSED`):** Solid `0.240 m`-thick concrete wall centered at **`Z = +6.800 m`** (`Z ∈ [+6.680, +6.920]`), spanning **`X ∈ [+1.240, +3.060]` (`1.820 m` deep)** and **`Y ∈ [-50.000, -43.550]` (`6.450 m` full two-story height)**.
- **Resulting Independent Bay 1 Clear Shaft:** **`X ∈ [+1.240, +3.150]` (`1.910 m` masonry depth; `1.820 m` behind door pocket)** × **`Z ∈ [+4.440, +6.680]` (`2.240 m` clear width)**.
- **Resulting Isolated Bay 2 Sealed Cavity:** `X ∈ [+1.240, +3.150]` × `Z ∈ [+6.920, +9.160]` (`2.240 m`), capped solidly at `Y = -47.150` by the preserved North segment of `Backrooms_Ceiling` and sealed on all four sides — **100% unreachable from Bay 1!**

### B. Solving the North Bi-Parting Door Pocket (`VERIFIED MATH`)
Because `FrontPierMid` spans `Z ∈ [+6.435, +7.165]` (`0.730 m` wide), stopping the concrete divider at `X = +3.060` and closing the `0.090 m` forward slot with a solid **steel pocket bulkhead plate at `Z = +7.040`** (`X ∈ [+3.060, +3.150]`, `Y ∈ [-50.000, -43.550]`, staying **`+0.125 m` clear South of Bay 2's opening `Z = +7.165`** and `+0.060 m` clear of Bay 2's closed door leaf `Z = +7.100`) provides a clean recessed track pocket for **true bi-parting doors (`0.690 m` leaves retracting `0.580 m` each → `1.150 m` clear opening = `1.80×` capsule diameter `0.640 m`)**:

### Plan-View ASCII Diagram (Bay 1 Shaft & Bi-Parting Door Pockets at `Y = -49.00` / `-45.40` — `PROPOSED`)
```text
             X=+1.00  +1.24             +3.00 +3.06 +3.15  +3.41
Z = +9.40 ---+---------+-------------------+-----+-----+----+  (Bay 2 NorthWall)
             | Back    |   BAY 2 SEALED    |           |Pier|
Z = +7.85    | Wall    |   NON-FUNCTIONAL  |  [SEALED] |Bay2|  <-- 100% UNTOUCHED
             |         |   CAVITY (2.24m)  |           |    |
Z = +7.16 ---+         +-------------------+-----+-----+----+
Z = +7.04    |         |  [Steel Bulkhead] |=====|     |Pier|  (North leaf pocket ends Z=7.015)
Z = +6.80 ---+=========+===================+     |     |Mid |  (0.24m Divider Z=[6.68,6.92])
Z = +6.43 ---+         |   0.09m N rail    |  N  |     +----+
             |         |  +-------------+  |  ^  |          |
             |   0.08m |  |  BAY 1 CAB  |  |  |  |  1.150m  |
Z = +5.75    |  West   |  |  1.54x1.56m |  |OPEN |  CLEAR   |  --> Lobby / Upper Waiting
             |  Clear  |  |  Interior   |  |  |  |  DOORWAY |
             |         |  +-------------+  |  v  |          |
Z = +5.06 ---+         |  [0.47m South     |  S  |     +----+
             |         |   Counterweight & |     |     |Pier|  (South leaf pocket ends Z=4.485)
Z = +4.44 ---+---------+   Guide Zone]     +-----+-----+  S |
Z = +4.20 ---+---------+-------------------+-----------+----+  (Bay 1 SouthWall)
```

---

## 7. Independent Bay 1 Shaft & Passenger Cabin Dimensions *(PROPOSED)*

### A. Complete Bay 1 Shaft Enclosure (`07.26`)
- **Lower Shaft (`Y ∈ [-50.000, -47.150]`):** Reuses existing `BackWall` (`X = +1.00..+1.24`), `SouthWall` (`Z = +4.20..+4.44`), and `FrontPierS/Mid/Header` (`X = +3.15..+3.41`), adding **only** the new internal `0.240 m` divider at `Z = +6.800` (`X ∈ [+1.240, +3.060]`) + `Z = +7.040` steel pocket bulkhead.
- **Upper Shaft Extension (`Y ∈ [-47.150, -43.350]`):** Continuous concrete shaft walls stacked vertically above `BackWall`, `SouthWall`, and `Divider`, capped solidly at `Y ∈ [-43.550, -43.350]` — strictly enclosed on all 4 sides, zero exposure into exterior void or Bay 2, zero interference with Upper Waiting Area (`X ≥ +3.410`) or Transition Vestibule (`Z ≥ +9.400`).

### B. Largest Practical Modest Hotel Cabin (`PROPOSED` — Centered on Door `Z = +5.750`)
| Dimension | Outer Cabin | Clear Interior | Margin / Ratio |
| :--- | :---: | :---: | :--- |
| **Depth (`X`)** | `X ∈ [+1.320, +3.000]` (**`1.680 m`**) | `X ∈ [+1.380, +2.920]` (**`1.540 m`**) | `0.080 m` West clearance, `0.060 m` East track clearance; **`2.41×` capsule `Ø 0.640 m`** |
| **Width (`Z`)** | `Z ∈ [+4.910, +6.590]` (**`1.680 m`**) | `Z ∈ [+4.970, +6.530]` (**`1.560 m`**) | `0.090 m` North rail gap, `0.470 m` South counterweight/rail bay; **`2.44×` capsule `Ø 0.640 m`** |
| **Interior Floor Area** | — | **`2.402 m²`** | Authentic modest 1970s–80s northern-Mexican 4-to-6-person hotel cab (not oversized!) |
| **Height (`Y`)** | `2.400 m` total (`0.040 m` floor deck + `2.240 m` clear + `0.120 m` roof) | **`2.240 m`** clear ceiling | `1.24×` capsule height (`1.800 m`); **`+0.490 m` overhead clearance** under `Y = -43.550` at Upper Stop! |
| **Door Return Jambs (`Z`)** | — | `0.205 m` each side of `1.150 m` opening | Ideal mounting space for a single modest brass/steel interior destination button panel at `Y = +1.15 m`! |

- **Cabin Materials (100% Existing Project Materials Reused Read-Only):** Brushed `ElevatorDoorMetal` interior doors & front return, dark `IndustrialMetal` baseboards/handrail/control bezel, warm matte `PlasterWall` or `UnfinishedWoodDoor` wainscoting panels, dark `FloorConcrete` floor tile, and a single restrained emissive-only `UtilityWarmOn` ceiling diffuser (`0` added Unity `Light` components!).

---

## 8. Synchronized Lower & Upper Landing-Door Design *(PROPOSED)*

- **Coplanar Door Plane:** Both Lower Bay 1 (`Y ∈ [-50.000, -47.800]`) and Upper Bay 1 (`Y ∈ [-46.400, -44.200]`) landing doors operate on the **exact same vertical plane `X ∈ [+3.100, +3.180]`** (`1.150 m` open clear span `Z ∈ [+5.175, +6.325]`), paired with a synchronized **cabin door pair** at `X ∈ [+2.950, +3.000]`.
- **Preserving 07.24 Upper Visual Identity:** Upper Bay 1 retains `UpperElevBay1_PerimeterFrame` (`X ∈ [+3.350, +3.442]`), while its closed leaves recess `0.220 m` inside the freshly opened `WestCoreBackingWall` portal to `X ∈ [+3.100, +3.180]` — matching Nivel 0's exact architectural recess depth!
- **Hard Mechanical Interlock Invariants (Enforced in Code & EditMode Tests):**
  1. Lower landing doors can **ONLY** open when `state ∈ {OpeningLowerDoors, BoardingAtLowerFloor, ClosingLowerDoors}` AND `|cabinY - LowerStopY| ≤ 0.002 m`.
  2. Upper landing doors can **ONLY** open when `state ∈ {OpeningUpperDoors, BoardingAtUpperFloor, ClosingUpperDoors}` AND `|cabinY - UpperStopY| ≤ 0.002 m`.
  3. Cabin movement (`MovingUp` / `MovingDown`) is **physically refused** unless BOTH lower and upper landing doors AND cabin doors are at `openFraction == 0f` with enabled solid `BoxCollider`s.
  4. **Anti-Pinch Door Obstruction Guard:** During `ClosingLowerDoors` / `ClosingUpperDoors`, a lightweight `Physics.CheckBox` across the doorway threshold (`X ∈ [+2.95, +3.42], Z ∈ [+5.10, +6.40]`) immediately reverses doors back to open if the player steps into the threshold mid-close — preventing trapping or clipping!
  5. **Bay 2 Excluded:** Zero animated leaves, zero triggers, zero scripts on Bay 2 (lower or upper).

---

## 9. `CharacterController` Passenger Transport Engineering *(VERIFIED + PROPOSED)*

Directly verified from `FirstPersonPlayer.cs` (`L1118–1172`) and `BorderCorridorScene.unity` (`CharacterController` `fileID=1986755685`):
- `height = 1.800 m`, `radius = 0.280 m`, `skinWidth = 0.040 m` (`r_eff = 0.320 m`), `stepOffset = 0.300 m`, `slopeLimit = 45°`, `camera local Y = 1.600 m`, no jump/crouch.
- `HandleMovement()` applies `verticalVelocity = -1.5f` when `isGrounded`, else accumulates `-gravity * Time.deltaTime`, then calls `characterController.Move(finalVelocity * Time.deltaTime)`.

### Comparison of Transport Approaches
| Criterion | Approach A — Controlled Cabin Attachment (`SetParent`) | Approach B — Explicit Synchronized `CharacterController.Move()` *(RECOMMENDED)* |
| :--- | :--- | :--- |
| **Additive-Scene Ownership (`BorderCorridorScene` vs. `BackroomsLevel1Scene`)** | **HIGH RISK:** Reparenting Player into additive `BackroomsLevel1Scene` risks destroying the Player if additive scene unloads abnormally. | **ZERO RISK:** Player never leaves `BorderCorridorScene` hierarchy. |
| **Collision & Grounding Fidelity** | **FRAGILE:** `CharacterController` ignores parent transform deltas during sweep; ascending floor clips capsule; descending floor triggers `-gravity` jitter. | **100% DETERMINISTIC:** Order-controlled move (`deltaY > 0`: player first, then cabin + `SyncTransforms`; `deltaY < 0`: cabin + `SyncTransforms` first, then player) prevents clipping & bounce completely! |
| **Camera Look & Micro-Movement** | Risk of camera rotation coupling if cabin ever rotates/shakes. | `HandleLook()` stays 100% native and smooth; horizontal walking stays bounded safely inside closed cabin walls while `verticalVelocity` is held at `0f` via a minimal `SetExternalVerticalTransport(bool)` hook (~12 lines in `FirstPersonPlayer.cs`). |
| **Automated EditMode Testability** | Hard to test without full PlayMode physics tick. | Directly unit-testable in EditMode with deterministic `StepSimulation(float dt)` + real `CharacterController`! |

### Edge-Case Recovery Matrix (`PROPOSED`)
| Edge Case | Deterministic Safe Behavior |
| :--- | :--- |
| **Player walks into closing door** | Threshold `Physics.CheckBox` detects player capsule → immediately transitions back to `Opening*Doors` (re-opens safely). |
| **Player walks around inside cabin during ride** | Solid closed cabin walls (`X ∈ [1.38, 2.92], Z ∈ [4.97, 6.53]`) + closed cabin doors contain horizontal `CharacterController.Move()`; synchronized vertical delta keeps feet locked flush to cabin floor. |
| **Elevator called from opposite floor** | Cabin closes doors on current floor, runs unoccupied `MovingUp` / `MovingDown` (`playerInside == false`, zero player displacement!), arrives, and opens doors for the waiting player. |
| **Two requests in rapid succession** | Ignored while `state != IdleAtLowerFloor && state != IdleAtUpperFloor && state != Boarding*` (strict single-operation mutex; zero coroutine stacking). |
| **Coroutine/scene interruption or Quit mid-ride** | State machine runs purely in deterministic `Update()` (no fragile nested coroutines!). On app quit/reload, `ProgressionSaveData` reloads player on Av. Benito Juárez and additive `BackroomsLevel1Scene` initializes deterministically at `IdleAtLowerFloor`. |
| **Stuck-inside failsafe** | While `BoardingAtLowerFloor` / `BoardingAtUpperFloor` or `Idle*`, pressing the interior door-open/destination control always re-opens doors at the stationary floor; doors never lock the player inside a stopped cab! |

---

## 10. Deterministic Elevator State Machine *(PROPOSED)*

Single `Update()`-driven finite state machine inside `RemanenciaPassengerElevatorController.cs` (zero nested coroutines, 100% deterministic `dt` stepping):

```text
  [IdleAtLowerFloor] --(Call Lower / Open)--> [OpeningLowerDoors]
          ^                                           |
          | (auto-close after 8s idle,                v
          |  or doors closed w/o depart)     [BoardingAtLowerFloor] <--(obstruction reopen)--+
          |                                           |                                      |
  [ArrivingLowerFloor] <-- [MovingDown]    (Explicit [E] Go Up OR Call Upper)                |
                                ^                     v                                      |
                                |            [ClosingLowerDoors] ----------------------------+
                                |                     | (doors 100% closed & verified)
                                |                     v
                                |            [PreparingDepartureUp] (0.35s mechanical settle)
                                |                     |
                                |                     v
                                |                 [MovingUp] (3.600m smoothstep ~4.5s, max 1.1 m/s)
                                |                     |
                                |                     v
                                |            [ArrivingUpperFloor] (exact Y=-46.400 snap + 0.25s settle)
                                |                     |
                                |                     v
                                |            [OpeningUpperDoors]
                                |                     |
                                |                     v
  [PreparingDescent] <--- [ClosingUpperDoors] <-- [BoardingAtUpperFloor] <--> [IdleAtUpperFloor]
```

**All 11 Mandatory Behavioral Rules Enforced:**
1. Single cabin (`Bay 1` only). 2. Single active state/operation. 3. **Zero automatic departure** (requires explicit `[E]` press on interior control or opposite-floor hall call). 4. Zero proximity auto-start. 5. Zero startup auto-movement (initializes strictly in `IdleAtLowerFloor`). 6. Cabin never moves while any door `openFraction > 0`. 7. No landing door opens unless cabin is stopped within `±2 mm` of that floor. 8. Duplicate requests ignored. 9. Clamped strictly to `LowerStopY = -50.000` and `UpperStopY = -46.400`. 10. Zero unprotected shaft access. 11. **Zero change to Threshold of Trust exit authorization** (elevator travels strictly inside Remanencia).

---

## 11. Interaction & Bilingual UI Proposal *(PROPOSED)*

Integrates cleanly into `FirstPersonPlayer.cs`'s existing `[E]` (`WasInteractPressed()`) + `currentLanguage == "es"` prompt system (matching `SewerManholeInteractable.cs` and `SewerOutflowGateController.cs`):

| Control Location | Condition | Spanish Prompt (`es`) | English Prompt (`en`) |
| :--- | :--- | :--- | :--- |
| **Lower Hall Call (`Hotel_Elevator_CallPanelPlate` area, `Z=+6.80`)** | Cabin idle at Lower (closed) | `[E] Abrir elevador` | `[E] Open elevator` |
| **Lower Hall Call** | Cabin at Upper floor | `[E] Llamar elevador` | `[E] Call elevator` |
| **Upper Hall Call (small modest bezel on `CorePierMid` `Z=+6.80`, `Y=-45.22`)** | Cabin idle at Upper (closed) | `[E] Abrir elevador` | `[E] Open elevator` |
| **Upper Hall Call** | Cabin at Lower floor | `[E] Llamar elevador` | `[E] Call elevator` |
| **Interior Cabin Panel (single physical button on return jamb)** | Stopped at Nivel 0 | `[E] Subir — Nivel Superior` | `[E] Go Up — Upper Level` |
| **Interior Cabin Panel** | Stopped at Upper Stratum | `[E] Bajar — Planta Baja` | `[E] Go Down — Ground Floor` |
| **Any Control While Moving** | `MovingUp` / `MovingDown` | *(Silent / `"Elevador en movimiento..."`)* | *(Silent / `"Elevator in motion..."`)* |

- **Restraint Rules:** Zero touchscreen UI, zero modal popup, zero newly invented room/floor directory, zero signage on Bay 2, and note on `Hotel_Sign_ElevatorNotice` (`"FUERA DE SERVICIO"`): keep untouched in `07.26–07.28`; creator decides in `07.29` whether to keep it as an intentional Remanencia contradiction (*the sign says out of service, yet Bay 1 quietly works!*) or relocate/remove it!

---

## 12. Power, Lighting & Audio Direction *(PROPOSED)*

- **Recommended Character:** **An older, mechanically worn yet quietly functioning 1970s–80s hotel passenger elevator** — smooth, deliberate (`~0.80 m/s` average, `4.5 s` transit for `3.600 m`), subtle `0.35 s` relay/brake engage pause before moving, gentle smoothstep ease-in/out (`0` jerky acceleration, `0` random failures, `0` horror jumpscares, `0` Cobalt-60 effects).
- **Lighting Budget (`18` Unity `Light`s Preserved):**
  - **Zero Unity `Light` components added by default** — cabin ceiling uses a restrained emissive-only `Remanencia_Level0_UtilityWarmOn.mat` diffuser panel (explicitly noted as non-light-casting visual surface) + flat `RemanenciaLevel0AmbientLight` + soft indirect bleed from `Backrooms_FluorLight_2` (`(4.50, -47.46, 8.80)`, `LightShadows.None`, range `10.2 m`, reaching both stops!) + player flashlight `[F]`.
  - Optional separate creator approval (`UNRESOLVED`) if PlayMode review later desires `1` low-intensity `LightShadows.None` cabin interior point light (`18 → 19`).
- **Procedural / Restrained Audio (`07.29–07.30`):** Low-volume spatial `AudioSource` on cabin (reusing the project's procedural `AudioClip` synthesis pattern, zero external licensed audio dependencies): subtle relay click on start, soft low-frequency motor hum during `MovingUp`/`MovingDown`, muted mechanical door slide, and gentle stop latch — no shrill bells or horror stingers.

---

## 13. Save, Persistence & World-State Safety *(VERIFIED + PROPOSED)*

- **Verified Existing Schema (`NarrativeProgressionData.cs:162–201`):** `ProgressionSaveData.schemaVersion == 1` stores strictly narrative discoveries/checkpoints and **explicitly never stores player world transforms or runtime scene positions**.
- **Recommended Strategy:** **Deterministic Session Reset to `IdleAtLowerFloor` (`Nivel 0`)** whenever `BackroomsLevel1Scene.unity` loads additively.
  - Zero edits to `ProgressionSaveData`, `LocalProgressionStorage`, `PlayerProgressionService`, `DiscoveryJournalCatalog`, or `PlayerWorldContext`.
  - Quitting mid-travel cannot corrupt saves or trap the player (reloading always starts safely on Av. Benito Juárez).
  - Entering Remanencia always finds the elevator cleanly parked at Nivel 0 (`IdleAtLowerFloor`), and the Northwest Service Staircase always provides a permanent, independent manual fallback between both floors!

---

## 14. Three Implementation Alternatives & Recommendation *(PROPOSED)*

| Criterion | Alternative A — Simple Kinematic Elevator | Alternative B — Mechanically Detailed Elevator | Alternative C — Hybrid Older Hotel Elevator *(RECOMMENDED)* |
| :--- | :---: | :---: | :---: |
| **Visual Authenticity** | Moderate (clean bare box cab & doors) | High (full animated cables, sheaves, motor room, moving counterweight) | **Highest for Remanencia** (authentic older hotel cabin finishes, visible static guide rails & door tracks in shaft, crisp bi-parting doors, zero overbuilt clutter) |
| **Engineering Complexity** | Low | High (syncing counterweight, cables, pulley rotations, overhead machine room) | **Low–Moderate** (deterministic kinematic cab & doors + clean static shaft rails) |
| **Geometric Feasibility in Bay 1 (`1.91 × 2.24 m`)** | High | Tight (overhead machine gear pushes past `Y = -43.550`!) | **Exact fit (`+0.490 m` overhead clearance under `-43.550 m`!)** |
| **Collision Safety & `CharacterController`** | High (Approach B) | Moderate (extra moving parts increase collider surface area) | **Highest** (Approach B + strict door interlocks + anti-pinch check) |
| **2 GB VRAM / Performance** | Optimal | Higher draw calls & per-frame transforms | **Optimal** (zero extra lights, zero new textures, ~20 static/kinematic meshes) |
| **Automated EditMode Testability** | High | Moderate | **Highest** (100% deterministic `Step(dt)` state machine + `CharacterController.Move()`) |
| **Regression Risk** | Lowest | Moderate–High | **Very Low** |
| **Overall Rank** | **#2** | **#3** | **#1 — RECOMMENDED** |

---

## 15. Technical Risk Assessment *(PROPOSED MITIGATIONS)*

1. **Splitting Monolithic `Backrooms_Ceiling` (`GO=322800857`):** Existing tests check `Backrooms_Ceiling` existence and bounds. *Mitigation:* Retain `Backrooms_Ceiling` (`GO=322800857`) in-place as the East slab (`X ∈ [+3.150, +20.300]`) and add 3 flush perimeter slabs around Bay 1 ONLY (`X ∈ [+1.240, +3.150], Z ∈ [+4.440, +6.680]`), keeping `Y ∈ [-47.150, -46.950]` identical everywhere outside Bay 1!
2. **Existing Lower Elevator Core Test Suites:** Several suites assert `Hotel_Elevator_Core.childCount == 35` and solid lower door linecasts. *Mitigation:* Keep all 35 `Hotel_Elevator_Core` children under their existing root (adding new shaft divider, upper shaft walls, cabin, and runtime controller under a new sibling root `Hotel_PassengerElevator_Bay1`!), and only animate/open Bay 1 leaves in `07.28–07.29` with explicit targeted test awareness for Bay 1 while keeping Bay 2's solid-door assertions 100% intact!
3. **`FirstPersonPlayer` Coupling:** Minimize edits to `FirstPersonPlayer.cs` to a tiny, self-contained `SetExternalVerticalTransport(bool)` + `[E]` interact hook (~25 lines total), keeping all elevator logic inside a dedicated `RemanenciaPassengerElevatorController.cs` that activates strictly when `IsPlayerActivelyInRemanencia == true`.

---

## 16. Phased Future Implementation Roadmap *(PROPOSED — NOT IMPLEMENTED)*

| Phase | Scope | Independent Acceptance Criteria |
| :---: | :--- | :--- |
| **07.25** | **Current Forensic Engineering Audit & Design** *(COMPLETE)* | Read-only design doc `REMANENCIA_UPPER_PASSENGER_ELEVATOR_DESIGN.md`; zero scene/code edits. |
| **07.26** | **Independent Bay 1 Shaft Isolation & `Backrooms_Ceiling` Split (Still Sealed)** | Installs `0.240 m` divider (`Z = +6.800`), `Z = +7.040` steel pocket bulkhead, 4-slab `Backrooms_Ceiling` cutout around Bay 1 ONLY, upper shaft walls & roof cap (`Y = -43.350`), and opens `UpperElev_Waiting_WestCoreBackingWall` behind Bay 1 while keeping **both** Lower & Upper Bay 1 doors closed & solid (Bay 2 100% untouched!). |
| **07.27** | **Static Modest Hotel Cabin & Shaft Guide Rails (Alternative C)** | Builds the `1.54 × 1.56 × 2.24 m` interior passenger cabin parked at Nivel 0 + static vertical T-rails & door track headers inside Bay 1 shaft; zero runtime movement yet; 2× idempotent. |
| **07.28** | **Runtime `RemanenciaPassengerElevatorController` & Approach B `CharacterController` Transport** | Deterministic `Update()` state machine (`Idle ↔ MovingUp ↔ MovingDown`), synchronized player/cabin `CharacterController.Move()` transport, anti-pinch door guard, and automated EditMode full round-trip ride tests (`-50.000 ↔ -46.400`). |
| **07.29** | **Synchronized Bi-Parting Doors, `[E]` Interaction & Bilingual ES/EN Prompts** | Animates Lower Bay 1, Upper Bay 1, and Cabin bi-parting doors (`1.150 m` clear), wires Hall Call + Interior Destination `[E]` prompts (ES/EN), adds subtle procedural motor/door audio. |
| **07.30** | **Edge-Case Recovery, Full Regression Hardening & PlayMode Polish** | Mid-ride interrupt/reload recovery tests, full 6-builder upper-stratum cross-idempotency, diagnostic renders, Windows standalone build, and live creator PlayMode sign-off. |

---

## 17. Protected Architecture & Systems Scope *(INVARIANT)*

100% untouched in 07.25 and strictly protected across `07.26–07.30`:
- **Bay 2 (North, `Z = +7.850`):** Lower `Hotel_ElevatorBay_2_*` AND Upper `UpperElevBay2_UnfinishedFacade` remain permanently sealed, unfinished, static, and non-functional.
- **Lower `Hotel_Elevator_Core` (`GO=331299473`, 35 children) & 5 Adjacent Fixtures:** Untouched in 07.25 (`BYTE_IDENTICAL`); future `07.26–07.29` touches strictly Bay 1 door leaves for animation while leaving all other core walls, piers, Bay 2, and `Backrooms_Floor` (under Option 1 low-profile deck) intact.
- **Upper Stratum:** `Hotel_NorthwestServiceStaircase`, `Hotel_NorthUpperGallery`, `Hotel_CourtyardLightwellOverlook`, `Hotel_EastOverhang`, `Hotel_UpperElevatorApproach` (Corridor, Vestibule, Waiting Area East/South/Floor/Ceiling).
- **Lightwell & Nivel 0:** All 10 `NorthWing_Lightwell_*` objects, Room 111-B, **Threshold of Trust** (sole authorized in-world exit), Rooms `101–110`, Room 110 ecological Zone D, all 07.6–07.9 narrative/ecology/microfauna elements, entry-only entrance, save schema (`schemaVersion == 1`), journal, exact **`18` Unity `Light` components (`LightShadows.None`)**, and `BorderCorridorScene.unity`.

---

## 18. Unresolved Creator Decisions Required Before 07.26 (`UNRESOLVED`)

1. **Approve Alternative C (Hybrid Older Hotel Elevator) + Approach B (`CharacterController.Move()` Transport):** Confirm proceeding with Alternative C and the 5-phase roadmap (`07.26 → 07.30`).
2. **Floor Pit vs. Zero-Cutout Low-Profile Cabin Deck (`Backrooms_Floor`):** Choose **Option 1 (Recommended):** Keep `Backrooms_Floor` 100% uncut and use a `0.040 m` beveled steel cabin deck (imperceptible step within `0.300 m` `stepOffset`), **OR** **Option 2:** Cut a localized `0.140 m` recessed pit into `Backrooms_Floor` (`4.28 m²`) for a `0.000 m` flush deck.
3. **Nivel 0 `"FUERA DE SERVICIO / OUT OF SERVICE"` Notice (`Hotel_Sign_ElevatorNotice`):** Confirm whether to **keep it in-place** as a subtle Remanencia contradiction (*marked out of service, yet Bay 1 quietly operates*) or relocate/remove it in `07.29`.
4. **Lighting Policy Inside Cabin:** Confirm keeping exact **`18` Unity `Light` components** (emissive-only ceiling diffuser + flat ambient + `Backrooms_FluorLight_2` bleed + flashlight `[F]`), deferring any `19th` cabin point light until live PlayMode review.
5. **Upper Floor Label on Interior Button:** Confirm bilingual wording (`"Nivel Superior" / "Upper Level"` vs. `"Piso 2" / "Floor 2"` vs. `"Obra Alta" / "Upper Works"`).


---

# 07.25A — Geometry & Runtime Reconciliation
## Strictly Read-Only Engineering Addendum

**Reconciliation Baseline (`VERIFIED EXISTING`):** Branch `product/unity-fellowship-2027`, HEAD `3dcb66c5ec712ba9d45e1f55b701a31e4b9321b5` (`== origin/product/unity-fellowship-2027`), working tree containing strictly `docs/REMANENCIA_UPPER_PASSENGER_ELEVATOR_DESIGN.md` prior to this addendum, reference repository strictly untouched.

> **Classifications Used Throughout This Addendum:**
> - **`VERIFIED EXISTING`** — Directly extracted from serialized Unity YAML and C# source code at commit `3dcb66c`.
> - **`COMPUTED`** — Mathematically derived 3D coordinates, swept volumes, clearances, and tolerances.
> - **`PROPOSED`** — Reconciled geometric or runtime target superseding earlier draft assumptions where noted; **not implemented**.
> - **`UNRESOLVED`** — Open creator choice pending explicit authorization before `07.26` begins.

---

## A. Investigation A — Landing Door / Cabin Door Alignment & Threshold Gap Reconciliation

### 1. Correction of Prior Terminology (`COMPUTED`)
Section 8 of the 07.25 draft loosely stated that landing and cabin doors "share coplanar tracks" while listing `X ∈ [+3.100, +3.180]` (landing) and `X ∈ [+2.950, +3.000]` (cabin). Those intervals are **NOT coplanar** — they are **parallel planes separated along X**:
- Lower Landing Doors and Upper Landing Doors **ARE vertically coplanar with each other** (`ΔX = 0.000 m`).
- Cabin Doors **MUST run on a parallel plane strictly West of the Landing Doors** with an explicit running clearance gap (`≥ 0.035 m`) so moving cabin leaves can never intersect stationary landing leaves.
- Furthermore, Investigation B (below) reveals that the raw `X ∈ [+3.100, +3.180]` landing door envelope overlaps solid `FrontPierS` / `FrontPierMid` (`X_min = +3.150`) by **`0.030 m`**. Shifting the reconciled landing door track strictly West of `X = +3.150` (`X ∈ [+3.060, +3.120]`) eliminates that intersection completely.

### 2. Reconciled X-Axis Coordinate Stack (`VERIFIED EXISTING` + `COMPUTED` + `PROPOSED`)

| Architectural / Mechanical Plane | Status | Exact X Interval (m) | Thickness / Gap (mm) | Notes |
| :--- | :---: | :---: | :---: | :--- |
| `Hotel_Room101_EastWall` (`GO=1712248583`) | `VERIFIED EXISTING` | `[+0.890, +1.110]` | `220.0 mm` | Hard West boundary behind shaft. |
| `Hotel_ElevatorCore_BackWall` (`GO=1951929040`) | `VERIFIED EXISTING` | `[+1.000, +1.240]` | `240.0 mm` | Inner shaft West face = `+1.240`. |
| West Shaft Running Clearance | `PROPOSED` | `[+1.240, +1.320]` | `80.0 mm` | Clear air gap behind moving cabin. |
| Cabin West Structural Wall | `PROPOSED` | `[+1.320, +1.380]` | `60.0 mm` | Solid cabin enclosure wall. |
| **Cabin Clear Interior Depth** | `PROPOSED` | **`[+1.380, +2.940]`** | **`1560.0 mm`** | `2.44×` player capsule `Ø 0.640 m`. |
| Cabin Front Return Jamb / Structural Face | `PROPOSED` | `[+2.940, +2.965]` | `25.0 mm` | Interior door-frame return (mounts `[E]` button). |
| **Cabin Slow Door Leaf Plane** | `PROPOSED` | **`[+2.965, +2.990]`** | **`25.0 mm`** | Inner leaf of 2-speed telescopic cabin door. |
| Cabin Inter-Leaf Track Gap | `COMPUTED` | `[+2.990, +3.000]` | `10.0 mm` | Zero leaf-to-leaf contact. |
| **Cabin Fast Door Leaf Plane & Cabin Sill Nose** | `PROPOSED` | **`[+3.000, +3.025]`** | **`25.0 mm`** | Cabin outer East limit (`X_max = +3.025`). |
| **Cabin-to-Landing Running Gap (Doors)** | `COMPUTED` | **`[+3.025, +3.060]`** | **`35.0 mm`** | Parallel clearance between moving cab & landing doors. |
| **Cabin-to-Landing Running Gap (Floor Sills)** | `COMPUTED` | **`[+3.025, +3.055]`** | **`30.0 mm`** | **Bridged sill gap (`4.7%` of capsule `Ø 0.640 m`) — ASME A17.1 realistic!** |
| **Landing Fast Door Leaf Plane (Lower & Upper)** | `PROPOSED` | **`[+3.060, +3.085]`** | **`25.0 mm`** | Vertically coplanar Lower ↔ Upper (`ΔX = 0.000 m`). |
| Landing Inter-Leaf Track Gap | `COMPUTED` | `[+3.085, +3.095]` | `10.0 mm` | Zero leaf-to-leaf contact. |
| **Landing Slow Door Leaf Plane (Lower & Upper)** | `PROPOSED` | **`[+3.095, +3.120]`** | **`25.0 mm`** | Strictly **`30.0 mm` clear West** of `FrontPier` (`+3.150`)! |
| Landing Door-to-Pier Clearance | `COMPUTED` | `[+3.120, +3.150]` | `30.0 mm` | Eliminates all `FrontPierS`/`FrontPierMid` intersection! |
| **Upper Landing Sill Extension** | `PROPOSED` | **`[+3.055, +3.150]`** | **`95.0 mm`** | Static steel sill cantilevered flush from `UpperElev_Waiting_FloorSlab` (`+3.150`), carrying upper door bottom track. |
| `FrontPierS/Mid` & `UpperElev_Waiting_FloorSlab` Edge | `VERIFIED EXISTING` | `[+3.150, +3.410]` | `260.0 mm` | Existing Lower piers (`GO=1166525667/1160025726`) & Upper slab (`GO=1077748815`) West edge. |
| Existing Lower & Upper Outer Floor Sills | `VERIFIED EXISTING` | `[+3.150, +3.450]` / `[+3.350, +3.456]` | `300 / 106 mm` | Existing Lobby (`GO=228939351`) & Upper (`GO=552512289`) outer trims remain outside shaft. |

### 3. Section Diagram — Threshold & Parallel Door Plane Separation (`COMPUTED` + `PROPOSED`)
```text
   WEST (Cabin)                                              EAST (Landing)
   X=+2.940  +2.965 +2.990 +3.000 +3.025  +3.055 +3.060 +3.085 +3.095 +3.120  +3.150        +3.410
      |        |      |      |      |       |      |      |      |      |       |             |
      |  Cab   |[Slow]| 10mm |[Fast]| 35mm  |      |[Fast]| 10mm |[Slow]| 30mm  | FrontPier / |
      |  Jamb  | Leaf |      | Leaf |  gap  |      | Leaf |      | Leaf |  gap  | CorePier    |
      +--------+------+------+------+       +------+------+------+------+-------+-------------+
      |    CABIN FLOOR DECK         | 30mm  | Upper Landing Sill Extension      | Upper Slab  |
      |    (top Y = -46.400)        |<-gap->| (top Y = -46.400, solid collider) | (Y=-46.400) |
      +-----------------------------+       +-----------------------------------+-------------+
      ^                             ^       ^                                   ^
   X=+2.940                      +3.025  +3.055                              +3.150
```

**Threshold Answers (`COMPUTED`):**
1. **Door plane separation:** `35.0 mm` face-to-face (`+3.025 → +3.060`) between Cabin Fast Leaf and Landing Fast Leaf. Zero possibility of cabin/landing door intersection.
2. **Horizontal threshold gap:** `30.0 mm` (`+3.025 → +3.055`) at Upper Stop (zero shaft glimpse; `4.7%` of `640 mm` capsule diameter → zero dip or snag). At Lower Stop, solid `Backrooms_Floor` (`Y = -50.000`) runs unbroken beneath the entire threshold (`0 mm` unsupported gap).
3. **Bridging sill requirement:** **YES** — the `95 mm` static **Upper Landing Sill Extension (`X ∈ [+3.055, +3.150], Z ∈ [+5.065, +6.435], Y ∈ [-46.480, -46.400]`)** is mandatory so the `150 mm` raw gap between Cabin (`+3.000`) and `UpperElev_Waiting_FloorSlab` (`+3.150`) is closed down to `30.0 mm`.

---

## B. Investigation B — 3D Door Leaf Swept Volumes & Pocket Feasibility

### 1. Why the Original 07.25 Bi-Parting Door Proposal Fails 3D Swept-Volume Verification (`COMPUTED`)
Auditing the complete 3D movement of the 07.25 draft's two `0.690 m` bi-parting leaves retracting `0.580 m` from `Z = +5.750` exposes **two fatal geometric collisions**:
1. **Landing Door × FrontPier Collision:** Placing landing doors at `X ∈ [+3.100, +3.180]` overlaps solid `FrontPierS` (`Z ≤ +5.065`) and `FrontPierMid` (`Z ≥ +6.435`), both of which start at `X_min = +3.150`, causing a **`30.0 mm` solid intersection** (`X ∈ [+3.150, +3.180]`) throughout retraction.
2. **Cabin Door × Cabin Wall & Shaft Divider Collision:** Even if landing doors were shifted West to `X ≤ +3.120`, the **cabin's own bi-parting doors** retract to `Z ∈ [+4.480, +5.175]` (South) and `Z ∈ [+6.325, +7.020]` (North):
   - Punches **`0.430 m` straight out through BOTH cabin sidewalls** (`Z = +4.910` and `Z = +6.590`)!
   - Punches **`0.340 m` straight through the solid concrete shaft divider** (`Z_min = +6.680`) into Bay 2!
3. **Clarification of the `0.245 m` North Pocket Number:** `0.245 m` is the **actual unobstructed horizontal distance inside Bay 1's shaft** between `FrontPierMid`'s South opening edge (`Z = +6.435`) and the South face of the `0.240 m` concrete shaft divider (`Z = +6.680`). Because `Z = +5.750` (door center) is only `0.870 m` South of the cabin's maximum safe North limit (`Z = +6.620`), **NO symmetric bi-parting door centered at `Z = +5.750` can ever achieve more than `0.820 m` clear opening** inside Bay 1's independent shaft without penetrating the divider!

### 2. Collision-Free Geometric Resolution — Two-Speed Telescopic Side-Opening Doors (Sliding South) (`COMPUTED` + `PROPOSED`)
Replacing bi-parting doors with **authentic vintage-hotel two-speed telescopic side-opening doors sliding South** (2 parallel leaves per door assembly; fast leaf moves `2×` speed of slow leaf, stacking cleanly inside the generous South shaft pocket!) resolves **100%** of swept-volume collisions while preserving Bay 1's `1.370 m` exterior masonry opening (`Z ∈ [+5.065, +6.435]`) via a `0.235 m` fixed brushed-metal South sidelite return (`Z ∈ [+5.065, +5.300]`) and `0.035 m` North strike jamb (`Z ∈ [+6.400, +6.435]`):

- **Clear Walk-Through Opening (`PROPOSED`):** **`Z ∈ [+5.300, +6.400]` = `1.100 m` wide (`1.72×` player capsule `Ø 0.640 m`) × `2.200 m` tall**.
- **Exact 3D Swept Volumes (`t ∈ [0.0 → 1.0]`, every intermediate position verified — `COMPUTED`):**

| Door Leaf | Closed `Z` (m) | Open `Z` (m) | Travel (m) | Complete Swept `Z` (m) | Exact `X` Plane (m) | Clearance to `SouthWall` (`+4.440`) | Clearance to `Divider` (`+6.680`) | Clearance to Cabin Shell (`[4.620, 6.560]`) |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Cabin Slow Leaf** (`0.580 m`) | `[5.270, 5.850]` | `[4.720, 5.300]` | `-0.550` | `[+4.720, +5.850]` | `[+2.965, +2.990]` | `+0.280 m` | `+0.830 m` | **Inside (`+0.100 m` S / `+0.710 m` N)** |
| **Cabin Fast Leaf** (`0.580 m`) | `[5.820, 6.400]` | `[4.720, 5.300]` | `-1.100` | `[+4.720, +6.400]` | `[+3.000, +3.025]` | `+0.280 m` | `+0.280 m` | **Inside (`+0.100 m` S / `+0.160 m` N)** |
| **Landing Fast Leaf** (`0.580 m`) | `[5.820, 6.400]` | `[4.720, 5.300]` | `-1.100` | `[+4.720, +6.400]` | `[+3.060, +3.085]` | `+0.280 m` | `+0.280 m` | `+0.035 m` East of Cabin Fast Leaf |
| **Landing Slow Leaf** (`0.580 m`) | `[5.270, 5.850]` | `[4.720, 5.300]` | `-0.550` | `[+4.720, +5.850]` | `[+3.095, +3.120]` | `+0.280 m` | `+0.830 m` | `+0.030 m` West of `FrontPier` (`+3.150`) |

**Critical Architectural Dividend (`COMPUTED`):** Because the entire door swept volume stays strictly within **`Z ∈ [+4.720, +6.400]` (`+0.280 m` South of the divider!)**, the `0.240 m` concrete shaft divider at **`Z ∈ [+6.680, +6.920]` now runs 100% solid and continuous from `BackWall` (`X = +1.240`) all the way to `FrontPierMid` (`X = +3.150`)** — eliminating the 07.25 draft's `Z = +7.040` steel pocket bulkhead completely and guaranteeing **unconditional solid-concrete isolation of Bay 2 from `Y = -50.000` to `Y = -43.350`**!

---

## C. Investigation C — Complete Cabin Travel Envelope, Lower Floor & Upper Slab Validation

### 1. Reconciled Cabin & Travel Envelope (`COMPUTED` + `PROPOSED`)
Updated to enclose the two-speed telescopic cabin door pocket cleanly inside the cabin shell:
- **Outer Cabin Box:** **`X ∈ [+1.320, +3.025]` (`1.705 m`) × `Z ∈ [+4.620, +6.560]` (`1.940 m`) × `H = 2.400 m`** (`0.040 m` floor deck + `2.240 m` clear interior + `0.120 m` roof).
- **Clear Cabin Interior:** **`X ∈ [+1.380, +2.940]` (`1.560 m`) × `Z ∈ [+4.680, +6.500]` (`1.820 m`) = `2.839 m²`** × **`2.240 m` headroom** (`2.44×` capsule width, `2.84×` capsule Z-span).
- **Exact Terminal & Swept Bounds:**

| Envelope Check | Bounds `(X, Y, Z)` (m) | Governing Shaft / Slab Boundary | Computed Minimum Clearance | Status |
| :--- | :--- | :--- | :---: | :---: |
| **Lower Stop (Option 1 — Uncut Floor)** | `X ∈ [1.320, 3.025]`, `Y ∈ [-50.000, -47.600]` (deck top `-49.960`), `Z ∈ [4.620, 6.560]` | `Backrooms_Floor` top `-50.000` / Lower Ceiling `-47.150` | `0.000 m` resting on floor; `+0.450 m` below `-47.150` | **PASS** |
| **Upper Stop (Flush Upper Floor)** | `X ∈ [1.320, 3.025]`, `Y ∈ [-46.440, -44.040]` (deck top `-46.400`), `Z ∈ [4.620, 6.560]` | `UpperElev_Waiting_FloorSlab` top `-46.400` / Upper Soffit `-43.550` | `0.000 m` flush at floor; **`+0.490 m`** below `-43.550` | **PASS** |
| **Full Vertical Swept Volume (`ΔY = 3.560 m`)** | **`X ∈ [1.320, 3.025]`, `Y ∈ [-50.000, -44.040]`, `Z ∈ [4.620, 6.560]`** | `Backrooms_Ceiling` aperture `X ∈ [1.240, 3.150], Z ∈ [4.440, 6.680]` | **`+0.080 m` W, `+0.125 m` E, `+0.180 m` S, `+0.120 m` N** | **PASS (ZERO INTERSECTION)** |
| **Roof Crossing `Backrooms_Ceiling` (`Y ∈ [-47.150, -46.950]`)** | Cabin roof `X ∈ [1.320, 3.025], Z ∈ [4.620, 6.560]` | 4-slab cutout edges (`1.240, 3.150, 4.440, 6.680`) | `≥ 0.080 m` all 4 sides | **PASS** |
| **Guide Rails & Counterweight Zone** | Static T-rails at `Z = +4.560` (South) & `Z = +6.620` (North) | `SouthWall` (`+4.440`) & `Divider` (`+6.680`) | `0.180 m` S zone & `0.120 m` N rail pocket | **PASS** |
| **Divider & Bay 2 Isolation** | `Divider` `X ∈ [1.240, 3.150], Z ∈ [6.680, 6.920], Y ∈ [-50.000, -43.350]` | Cabin `Z_max = +6.560`, Doors `Z_max = +6.400` | **`+0.120 m` to cabin / `+0.280 m` to doors** | **PASS (100% ISOLATED)** |

### 2. Lower Floor Treatment Validation (`VERIFIED EXISTING` + `COMPUTED`)
- **`Backrooms_Floor` (`GO=1532896222`):** Exact top `Y = -50.000 m`, bottom `Y = -50.300 m`, solid `BoxCollider`.
- **Option 1 — Leave `Backrooms_Floor` Uncut + `0.040 m` Beveled Cabin Deck (`RECOMMENDED`):**
  - Cabin deck bottom sits at `Y = -50.000 m`, deck top at `Y = -49.960 m` (`0.040 m` beveled threshold lip above `Backrooms_Floor` and only **`0.016 m`** above existing `Hotel_ElevatorBay_1_FloorSill` `Y_top = -49.976 m`!).
  - `0.040 m` is **`13.3%` of `CharacterController.stepOffset = 0.300 m`** → completely smooth walk-on with zero snag.
  - Because `MovingUp` moves strictly in `+Y` from `Y_bot = -50.000` (`ΔY = +3.560 m` to `Y_bot = -46.440`, `Y_top = -46.400` flush at Upper Stop!), the cabin **NEVER descends below `-50.000`** and **NEVER intersects `Backrooms_Floor`**.
  - **Why Option 2 (`0.140 m` floor pit) is unnecessary:** Cutting monolithic `Backrooms_Floor` into 4 slabs introduces unnecessary collider seam risk and breaks existing monolithic-floor test assumptions merely to remove a `16 mm` bevel above the existing metal sill. **Option 1 is mathematically proven superior.**

### 3. Upper Floor Slab Boundary Validation (`VERIFIED EXISTING` + `COMPUTED`)
- **`UpperElev_Waiting_FloorSlab` (`GO=1077748815`):** Verified `X ∈ [+3.150, +6.420]`, `Y ∈ [-46.600, -46.400]` (`t = 0.200 m`), `Z ∈ [+3.980, +9.400]`, solid `BoxCollider`.
- **`UpperElev_Support_WaitingCorePlinthW` (`GO=1626614601`):** Verified `X ∈ [+3.150, +3.410]`, `Y ∈ [-46.940, -46.600]`, solid `BoxCollider`.
- **`UpperElev_Waiting_CeilingSlab` (`GO=1736100969`):** Verified `X ∈ [+3.150, +6.420]`, `Y ∈ [-43.550, -43.350]`, solid `BoxCollider`.
- Cabin outer East bound (`X_max = +3.025`) maintains **`+0.125 m` clearance** from all three upper structures (`X_min = +3.150`), and the proposed `95 mm` **Upper Landing Sill Extension** (`X ∈ [+3.055, +3.150], Y ∈ [-46.480, -46.400]`) bridges seamlessly flush with `UpperElev_Waiting_FloorSlab` (`Y = -46.400`), leaving a clean `30.0 mm` running gap to the cabin nose (`+3.025`) with **zero upper slab aperture needed**!

---

## D. Investigation D — `CharacterController` Transport, Runtime Lifecycle & Recovery Scenarios

### 1. Verified Runtime Execution Architecture (`VERIFIED EXISTING`)
- **Scene Ownership:** `FirstPersonPlayer` (`CharacterController` `fileID=1986755685`) resides permanently in **`BorderCorridorScene.unity`** (`SecretBackroomsTransitionController.cs:608–677` loads/unloads `BackroomsLevel1Scene` additively around the persistent player GameObject).
- **Update Loop (`FirstPersonPlayer.cs:1069–1172`):** When `!isBackroomsTrans`, `Update()` calls `HandleLook()` then `HandleMovement()`, which unconditionally applies `verticalVelocity = -1.5f` (grounded) or `verticalVelocity -= gravity * Time.deltaTime` (ungrounded) and invokes `characterController.Move(finalVelocity * Time.deltaTime)`.
- **Synchronized Approach B Transport Protocol (`PROPOSED`):**
  - Minimal `FirstPersonPlayer` API (~15 lines, zero impact on surface gameplay): `SetExternalVerticalTransport(bool active)` suppresses only `verticalVelocity` accumulation inside `HandleMovement()` while keeping `HandleLook()` and horizontal cabin-bounded `WASD` movement 100% native!
  - `RemanenciaPassengerElevatorController` runs strictly in `LateUpdate()` (immediately AFTER `FirstPersonPlayer.Update()` has resolved horizontal input inside the closed cabin walls!):
    - **Ascent (`deltaY > 0`):** (1) `playerCC.Move(Vector3.up * deltaY)`, (2) translate cabin by `+deltaY`, (3) `Physics.SyncTransforms()`. *(Impossible for rising floor collider to overtake player capsule.)*
    - **Descent (`deltaY < 0`):** (1) translate cabin by `deltaY`, (2) `Physics.SyncTransforms()`, (3) `playerCC.Move(Vector3.up * deltaY)`. *(Impossible for player to snag on unmoved floor or enter freefall.)*
  - **Mid-Door-Transition Exit Handling:** If player steps out through closing doors, threshold `Physics.CheckBox` (`X ∈ [+2.95, +3.42], Z ∈ [+5.25, +6.45]`) immediately aborts `Closing*Doors` → re-opens to `Opening*Doors`; `playerInsideCabin` is evaluated strictly **after** doors reach `openFraction == 0f` before transitioning to `PreparingDeparture`!

### 2. Recovery Scenario Matrix (`PROPOSED` — Preserving `schemaVersion == 1`)

| Scenario | Verified Runtime Condition | Exact Deterministic Recovery Policy (`PROPOSED`) | Safe Under `schemaVersion == 1`? |
| :--- | :--- | :--- | :---: |
| **1. Cold Application Startup** | `BorderCorridorScene` loads; player spawns on Av. Benito Juárez; `BackroomsLevel1Scene` is not yet loaded. | Upon entering Remanencia (`EnterBackroomsRoutine` `L609–640`), `BackroomsLevel1Scene` loads additively, player teleports to `Backrooms_SpawnPoint` (`(17.80, -49.94, 8.80)` on Nivel 0), and elevator initializes deterministically to `IdleAtLowerFloor` (`Y_deck_top = -49.960`, all doors closed). | **YES — 100% proved** |
| **2. Additive Unload/Reload in Same Execution** | `ExitBackroomsRoutine()` (`L657–677`) teleports persistent player to Av. Benito Juárez (`(-11.20, 0.24, -11.99)`) **before** calling `UnloadSceneAsync("BackroomsLevel1Scene")`. | Elevator `OnDisable()` / `OnDestroy()` unconditionally clears `player.SetExternalVerticalTransport(false)` (releasing any lock). Next re-entry via Veil or Manhole teleports player to Nivel 0 (`Backrooms_SpawnPoint` or Manhole drop zone) while freshly loaded elevator starts cleanly at `IdleAtLowerFloor`. Zero position mismatch! | **YES — 100% proved** |
| **3. Application Termination During Ride** | `NarrativeProgressionData.cs:158` never stores player world transforms; discoveries already saved remain intact (`schemaVersion == 1`). | Next launch spawns player on Av. Benito Juárez (`BorderCorridorScene`); entering Remanencia places player at Nivel 0 spawn with freshly initialized elevator at `IdleAtLowerFloor`. Zero trap, zero fall, zero lost discoveries. | **YES — 100% proved** |
| **4. Interrupted Ride / Disabled Controller / Abnormal Unload Mid-Ride** | Controller disabled or scene unloaded unexpectedly while `state ∈ {MovingUp, MovingDown, Preparing*}` and `playerInsideCabin == true`. | `OnDisable()` / `OnDestroy()` immediately: (1) calls `player.SetExternalVerticalTransport(false)`, (2) if `Application.isPlaying && !isApplicationQuitting && playerInsideCabin && playerCC != null` and `BackroomsLevel1Scene` is still active, evacuates player cleanly onto nearest safe lobby apron (`(4.20, -49.96, 5.75)` if `cabinY < -48.20`, else `(4.20, -46.36, 5.75)` via `CC.enabled = false/true` + `Physics.SyncTransforms()`), and (3) snaps doors closed. | **YES — 100% proved** |

### 3. Test Strategy Classification (`PROPOSED`)
- **EditMode Automated Tests (Pure Logic & Geometry — Cannot Prove Live Physics Alone):**
  - Deterministic `RemanenciaPassengerElevatorStateMachine` unit tests (all state transitions, mutex rejection of duplicate calls, interlock refusal to move with open doors, door-open refusal between floors).
  - Static 3D swept-volume & AABB proofs (telescopic door leaves vs. `FrontPierS/Mid`, `SouthWall`, `Divider`; cabin travel vs. 4-slab `Backrooms_Ceiling`; Bay 2 continuous isolation; 6-builder cross-idempotency; exact 18 lights; `BorderCorridorScene` SHA-256).
- **PlayMode Automated Integration Tests (Mandatory in `07.28–07.30`):**
  - Live `CharacterController.Move()` full round-trip ascent (`-49.960 → -46.400`) and descent (`-46.400 → -49.960`) verifying continuous floor contact (`|playerY - deckTopY| ≤ 0.05 m`), zero floor clipping, smooth threshold walk-in/out across the `30 mm` sill gap, active `HandleLook()` during transit, anti-pinch door reversal when stepping into closing doors, additive unload/reload reset, and `OnDisable()` mid-ride safe evacuation.
- **Manual Creator PlayMode Acceptance:**
  - Perceived ride comfort/speed (`~4.5 s`), telescopic door smoothness, vintage hotel cabin aesthetic, audio restraint, lighting readability with/without `[F]`, and overall *Hotel de las Palomas* atmosphere.

---

## E. Recommended Creator Defaults (Pending Explicit Approval — `UNRESOLVED`)

| Decision | Recommended Default (`PROPOSED — NOT IMPLEMENTED`) |
| :--- | :--- |
| **Elevator architectural alternative** | Alternative C — Hybrid Older Hotel Elevator *(with reconciled 2-speed telescopic doors sliding South)* |
| **Player transport strategy** | Approach B — Synchronized `CharacterController.Move()` in `LateUpdate()` (zero reparenting) |
| **Lower floor** | Option 1 — Leave `Backrooms_Floor` uncut; `0.040 m` beveled cabin deck (`ΔY = 3.560 m` to flush Upper Stop) |
| **`Hotel_Sign_ElevatorNotice`** | Retain existing `"FUERA DE SERVICIO / OUT OF SERVICE"` sign on Nivel 0 as an intentional Remanencia contradiction |
| **Unity Light budget** | Exactly `18` Unity `Light` components (`LightShadows.None`); zero 19th light |
| **Upper button label** | `"Nivel superior"` / `"Upper Level"` |

---

## F. Revised 07.26 Entry Conditions Checklist

| # | Engineering Entry Condition | Reconciliation Status |
| :-: | :--- | :---: |
| 1 | Door planes (`X ∈ [2.965, 3.025]` cabin / `[3.060, 3.120]` landing) & `30.0 mm` threshold sill gap geometrically resolved | **SATISFIED (`COMPUTED`)** |
| 2 | Full door leaf swept volumes (`Z ∈ [4.720, 6.400]`) 100% collision-free across `t ∈ [0, 1]` | **SATISFIED (`COMPUTED`)** |
| 3 | Full cabin swept volume (`X ∈ [1.320, 3.025], Y ∈ [-50.000, -44.040], Z ∈ [4.620, 6.560]`) 100% collision-free | **SATISFIED (`COMPUTED`)** |
| 4 | Bay 2 hermetically isolated by continuous `0.240 m` concrete divider (`X ∈ [1.240, 3.150], Z ∈ [6.680, 6.920], Y ∈ [-50.000, -43.350]`) | **SATISFIED (`COMPUTED`)** |
| 5 | Upper slab edge (`X_min = +3.150`) & `95 mm` cantilevered Upper Landing Sill Extension verified (zero upper slab cut) | **SATISFIED (`COMPUTED`)** |
| 6 | Approach B `CharacterController.Move()` transport protocol + PlayMode validation plan defined | **SATISFIED (`PROPOSED`)** |
| 7 | All 4 lifecycle & mid-ride recovery scenarios proved safe under `schemaVersion == 1` | **SATISFIED (`COMPUTED`)** |
| 8 | 4-slab `Backrooms_Ceiling` split preserves `GO=322800857` in-place, collider coverage, and Bay 2 overhead cap | **SATISFIED (`PROPOSED`)** |
| 9 | Zero dependency on modifying `BorderCorridorScene.unity`, Threshold of Trust, or reference repo | **SATISFIED (`VERIFIED`)** |

---

## G. Final Engineering Verdict

> **`READY FOR 07.26 DESIGN IMPLEMENTATION`** *(technically reconciled and geometrically collision-free; **pending explicit creator approval of the Section E defaults — ZERO implementation authorized or started in 07.25A**)*.


---

# 07.26 — Implementation Completion Record

**Status:** Implemented & verified statically in `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity` *(pending creator PlayMode review)*.  
**Scope:** Static structural isolation and vertical shaft enclosure for **Bay 1 South (`Z = +5.750`)** only — zero cabin, zero guide rails/shoes/brackets, zero moving doors, zero runtime controller, zero call/destination buttons, zero audio, and zero new Unity `Light` components.

## 1. Implemented Static Structural Geometry (`Backrooms_Architecture/Hotel_UpperPassengerElevatorShaft`)

| Structural Group | Implemented GameObjects | Exact Verified World Bounds `(X, Y, Z)` (m) | Preserved Identity / Notes |
| :--- | :--- | :--- | :--- |
| **`Bay1Shaft_CeilingSplit`** | `Backrooms_Ceiling` *(East, retained in-place)*, `Backrooms_Ceiling_West`, `Backrooms_Ceiling_South`, `Backrooms_Ceiling_North` | East `[+3.150, +20.300] × [-47.150, -46.950] × [-12.300, +12.300]`<br>West `[-20.300, +1.240] × [-47.150, -46.950] × [-12.300, +12.300]`<br>South `[+1.240, +3.150] × [-47.150, -46.950] × [-12.300, +4.440]`<br>North `[+1.240, +3.150] × [-47.150, -46.950] × [+6.920, +12.300]` | Original `Backrooms_Ceiling` **`GO=322800857`** preserved in-place under `Backrooms_Architecture`. Leaves strictly Bay 1 aperture **`X ∈ [+1.240, +3.150], Z ∈ [+4.440, +6.680]`** (`1.910 m × 2.240 m = 4.2784 m²`) open. `Backrooms_Ceiling_North` butts flush at `Z = +6.920` against the continuous divider (zero coplanar Z-fighting / zero overlapping colliders) while capping 100% of Bay 2 (`Z ∈ [+6.920, +9.160]`) overhead. |
| **`Bay1Shaft_ConcreteDivider`** | `Bay1Shaft_DividerWall_North`, `Bay1Shaft_DividerTopCoping` | Divider `[+1.240, +3.150] × [-50.000, -43.550] × [+6.680, +6.920]`<br>Coping `[+1.240, +3.150] × [-43.550, -43.350] × [+6.680, +6.920]` | Continuous `0.240 m` solid concrete separation between Bay 1 and Bay 2 connecting BackWall (`+1.240`) to FrontPierMid (`+3.150`) with zero gaps or door pockets. Obsolete 07.25 `Z = +7.040` steel bulkhead excluded. |
| **`Bay1Shaft_UpperEnclosure`** | `UpperShaft_BackWall`, `UpperShaft_SouthWall`, `UpperShaft_RoofCap`, `UpperShaft_EastPlenumSeal` | BackWall `[+1.000, +1.240] × [-46.950, -43.350] × [+4.200, +6.920]`<br>SouthWall `[+1.240, +3.150] × [-46.950, -43.350] × [+4.200, +4.440]`<br>RoofCap `[+1.240, +3.150] × [-43.550, -43.350] × [+4.440, +6.680]`<br>PlenumSeal `[+3.150, +3.350] × [-46.950, -46.940] × [+4.440, +6.680]` | Watertight concrete shaft extension flush atop `Backrooms_Ceiling_West/South` and flush with `UpperElev_Waiting_CeilingSlab` (`Y ∈ [-43.550, -43.350]`). `UpperShaft_EastPlenumSeal` closes the `10 mm` seam between `Backrooms_Ceiling` (`-46.950`) and `UpperElev_Support_WaitingCorePlinthW` (`-46.940`) strictly outside the Bay 1 aperture. |
| **`Bay1Shaft_UpperPortalBacking`** | `UpperElev_Waiting_WestCoreBackingWall` *(North, retained in-place)*, `Bay1UpperBacking_SouthPier`, `Bay1UpperBacking_Transom` | North `[+3.150, +3.350] × [-46.600, -43.550] × [+6.435, +9.400]`<br>SouthPier `[+3.150, +3.350] × [-46.400, -43.550] × [+4.200, +5.065]`<br>Transom `[+3.150, +3.350] × [-44.200, -43.550] × [+5.065, +6.435]` | Original `UpperElev_Waiting_WestCoreBackingWall` **`GO=268893405`** preserved in-place as solid Bay 2 North backing. Opens future Bay 1 portal `Z ∈ [+5.065, +6.435], Y ∈ [-46.400, -44.200]` cleanly while existing `UpperElev_Waiting_FloorSlab` seals `Y ∈ [-46.600, -46.400]` and the 07.24 closed Bay 1 finished doors + `UpperElev_SealedBay_1_SealedBackingPanel` remain untouched and impassable. |

## 2. Builder, Cross-Builder, and Verification Artifacts

- **New Builder:** `unity-client/Assets/Editor/UpperPassengerElevatorShaftBuilder.cs` (menu item `My Virtual Community/Remanencia/Migrate 07.26 Bay 1 Passenger Elevator Shaft Isolation`, plus read-only cutaway renderer `RenderBay1ShaftDiagnosticViews()`).
- **New Automated Test Suite:** `unity-client/Assets/Editor/RemanenciaUpperPassengerElevatorShaftTests.cs` (3 fixtures verifying 4-piece ceiling split & aperture, continuous divider & upper enclosure/roof cap/split backing, and protected invariants + full 6-builder cross-idempotency `07.17 → 07.19 → 07.21 → 07.23 → 07.24 → 07.26`).
- **Minimal Cross-Builder Hooks:** `BackroomsLevel1Builder.cs` (`UpperPassengerElevatorShaftRootName`, `RunVerificationChecks()`, split-ceiling material pass) and `UpperElevatorApproachBuilder.cs` (preserves North-trimmed `UpperElev_Waiting_WestCoreBackingWall` when 07.26 shaft root is present).
- **Verification Results:**
  - **2× Migration Idempotency:** `PASS` (`SHA-256 = 36CFCF62C1F45EB0DBE41EE00144C961F735FEA72CEEFDB19050DB505A296E73` identical across Pass 1, Pass 2, and post-cutaway-render).
  - **EditMode Test Suite:** **`121 / 121` PASSED (`0` failed, `0` skipped)**.
  - **SecretWedgeRoomVerificationSuite:** **`11 / 11` PASSED**.
  - **Windows x64 Standalone Build:** `PASS` (`MyVirtualCommunity.exe`, `0.64 MB`, `Build Status: FRESH`, `SHA-256 = 3070e580e019576b9afcdeb4fec18d03cebc119f6ae84d597412905c749ad653`).
  - **Forensic YAML & SHA-256 Protection Audit:** `Hotel_Elevator_Core` (all `35` children / `153` YAML blocks), all `5` adjacent lower fixtures, uncut `Backrooms_Floor`, `UpperElev_Waiting_FloorSlab`, `UpperElev_Waiting_CeilingSlab`, all 07.24 Bay 1/2 facade objects (`128` YAML blocks) & `SealedBackingPanel`s, and all `10` Lightwell objects verified **100% byte-identical** to `HEAD` (`3dcb66c`). Total Unity `Light` count remains **exact `18`** (`LightShadows.None`). `BorderCorridorScene.unity`, `FirstPersonPlayer.cs`, `SecretBackroomsTransitionController.cs`, `NarrativeProgressionData.cs`, and `UpperElevatorBayArchitectureBuilder.cs` SHA-256 hashes verified unchanged.

## 3. Mandatory Engineering Accuracy Reconciliation & Open Items for 07.27+

1. **Correction of 07.25A Section D.2 Recovery Scenario Status:** The four runtime recovery scenarios listed in Section D.2 (Cold Application Startup, Additive Unload/Reload, Application Termination During Ride, and Interrupted Ride / Disabled Controller / Abnormal Unload Mid-Ride) were previously labeled *"YES — 100% proved"* in the 07.25A table. Because no elevator runtime controller, `FirstPersonPlayer` vertical-transport hook, or live PlayMode test exists yet in 07.26, all four scenarios are formally classified as **proposed recovery policies pending runtime implementation and live PlayMode validation in 07.28–07.30**.
2. **Open Geometric Verification Item for 07.27 (Guide Rails, Guide Shoes, and Wall Brackets):** While the clear Bay 1 shaft aperture (`X ∈ [+1.240, +3.150]`, `Z ∈ [+4.440, +6.680]`) and nominal cabin outer box (`X ∈ [+1.320, +3.025]`, `Z ∈ [+4.620, +6.560]`) provide `0.180 m` South and `0.120 m` North lateral clearance zones, exact physical dimensions and running clearances for **T-section guide rails, guide shoes, wall-mounting brackets, and door-track hangers** remain an open verification item to be explicitly checked and unit-tested during **07.27** static cabin/hardware implementation.
3. **Explicitly Deferred Out of 07.26 Scope:** Elevator cabin structure & deck, guide rails/shoes/brackets, counterweight assembly, telescopic landing & cabin doors, upper landing sill extension, runtime elevator state machine & `LateUpdate()` `CharacterController.Move()` transport controller, `FirstPersonPlayer` vertical-transport hook, call/destination buttons, interaction prompts, and elevator audio.


---

# 07.27 — Implementation Completion Record

**Status:** Implemented & verified statically in `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity` *(creator visual acceptance explicitly deferred until elevator spaces and access mechanisms are complete for in-game walkthrough review)*.  
**Scope:** Static older-hotel passenger cabin parked at Nivel 0 (`Y = -50.000`), complete static ISO 7465 T70-1/B guide-rail and bracket system, four cabin-mounted non-static guide-shoe assemblies, restrained interior finishes, and East entrance preparation for the future two-speed telescopic door assembly. Zero cabin movement, zero moving doors, zero runtime controller, zero buttons, zero audio, zero triggers, and zero new Unity `Light` components.

## 1. Reconciled Cabin Envelope & Architectural Root Separation

- **Dedicated Future Movable Cabin Root (strictly non-static, `m_StaticEditorFlags == 0` across all children):**
  `Backrooms_Architecture/Hotel_UpperPassengerElevatorCabin` (root origin at `(2.1725, -50.000, 5.590)` so future vertical translation shifts `position.y` cleanly from `-50.000` to `-46.440`, `ΔY = +3.560 m`).
- **Outer Cabin Shell (Lower Stop):**
  - `X ∈ [+1.320, +3.025]` (`1.705 m`), `Y ∈ [-50.000, -47.600]` (`2.400 m`), `Z ∈ [+4.620, +6.560]` (`1.940 m`).
  - Floor deck: `Y ∈ [-50.000, -49.960]` (`0.040 m`), resting flush on uncut `Backrooms_Floor` (`Y = -50.000`, zero penetration). Split into main interior deck (`FloorConcrete`, `X ∈ [1.320, 2.940]`) and brushed-metal entrance threshold sill (`ElevatorDoorMetal`, `X ∈ [2.940, 3.025]`).
  - Roof assembly: `Y ∈ [-47.720, -47.600]` (`0.120 m` total thickness: `0.100 m` structural roof slab `[-47.700, -47.600]` + `0.020 m` perimeter soffit frame `[-47.720, -47.700]` housing a recessed emissive-only ceiling diffuser `UtilityWarmOn`).
- **Clear Passenger Interior (ZERO collider or mesh intrusion):**
  - `X ∈ [+1.380, +2.940]` (`1.560 m`) × `Z ∈ [+4.680, +6.500]` (`1.820 m`) = **`2.8392 m²`** × **`2.240 m` clear height** (`Y ∈ [-49.960, -47.720]`).
  - Structural outer steel carcass (`45 mm`) + flush interior architectural finish strata (`15 mm` landing exactly on `X = +1.380`, `Z = +4.680`, `Z = +6.500`): dark metal baseboard (`BaseboardConcrete`), modest wood wainscoting (`UnfinishedWoodDoor`), dark metal chair rail (`IndustrialMetal`), flush brushed-metal West handrail (`ElevatorDoorMetal`, `X_max = +1.380`), warm aged plaster upper walls (`PlasterWall`), dark metal crown trim (`IndustrialMetal`), and a blank non-interactive future control-panel reserve (`X ∈ [+2.940, +2.942]`, zero buttons/labels/scripts). All materials reuse existing `Assets/Settings/*.mat` assets strictly read-only.
- **East-Facing Cabin Entrance & Future Door Reservation:**
  - Clear open portal: `Z ∈ [+5.300, +6.400]` (`1.100 m` wide) × `Y ∈ [-49.960, -47.760]` (`2.200 m` tall), zero blocking collider.
  - South front return (`X ∈ [+2.940, +2.960]`) maintains **`+5.0 mm` clear West** of future Cabin Slow Leaf (`X_min = +2.965`).
  - North strike jamb (`X ∈ [+2.940, +3.025], Z ∈ [+6.405, +6.500]`) + inner reveal lip (`X ∈ [+2.940, +2.960], Z ∈ [+6.400, +6.405]`) maintains **`+5.0 mm` clear** of future Cabin Fast Leaf (`X ∈ [+3.000, +3.025], Z_max = +6.400`).
  - Entrance header (`X ∈ [+2.940, +2.960], Y ∈ [-47.760, -47.720]`) leaves the entire `X ∈ [+2.965, +3.025]` track pocket unobstructed.

## 2. Full 3D Guide Rail, Bracket, Anchor & Guide Shoe Engineering Specification

- **Static Shaft Guide Rail Root:** `Backrooms_Architecture/Hotel_UpperPassengerElevatorShaft/Bay1Shaft_GuideRails` (`isStatic = true`).
- **Rail Profile (Symmetric ISO 7465 T70-1/B Cold-Drawn Steel T-Section, centered at `X = +2.170 m`):**
  - Base flange: `70.0 mm` wide (`X ∈ [+2.135, +2.205]`) × `10.0 mm` thick (`IndustrialMetal`).
  - Machined blade (web + head): `16.0 mm` wide (`X ∈ [+2.162, +2.178]`) × `40.0 mm` deep (`ElevatorDoorMetal`); total profile depth = **`50.0 mm`**.
  - Continuous vertical span: **`Y ∈ [-49.980, -43.650]`** (`6.330 m`) resting on `20.0 mm` steel floor base feet (`Y ∈ [-50.000, -49.980]`, flush on uncut `Backrooms_Floor`) and terminating **`+100.0 mm` clear below `UpperShaft_RoofCap` (`Y = -43.550`)**.
- **Exact Lateral Stack-Ups & Deviations from Provisional 07.25A Centerlines:**
  - **North Tight Pocket (`120.0 mm` total: Cabin `+6.560` → Divider `+6.680`):**
    - Wall anchor plate: `Z ∈ [+6.666, +6.680]` (`14 mm` × `220 mm` × `100 mm`, flush on Bay 1 concrete face).
    - Stand-off & clips: `Z ∈ [+6.650, +6.666]` (`16 mm` stand-off) + forged flange clips (`Z ∈ [+6.640, +6.650]`).
    - North T-rail: flange `Z ∈ [+6.640, +6.650]`, blade `Z ∈ [+6.600, +6.640]` (tip `+6.600`, **`+40.0 mm`** from cabin wall).
    - Reconciled centerline **`Z = +6.625 m`** *(5 mm refinement from 07.25A provisional `+6.620 m` to fit the full 30 mm wall-plate/stand-off/clip stack inside the 120 mm pocket)*.
  - **South Generous Pocket (`180.0 mm` total: South shaft wall `+4.440` → Cabin `+4.620`):**
    - Wall anchor plate: `Z ∈ [+4.440, +4.454]` (`14 mm` × `220 mm` × `100 mm`).
    - Stand-off channel & clips: `Z ∈ [+4.454, +4.530]` (`76 mm` stand-off) + forged flange clips (`Z ∈ [+4.530, +4.540]`).
    - South T-rail: flange `Z ∈ [+4.530, +4.540]`, blade `Z ∈ [+4.540, +4.580]` (tip `+4.580`, **`+40.0 mm`** from cabin wall, symmetric with North!).
    - Reconciled centerline **`Z = +4.555 m`** *(5 mm refinement from 07.25A provisional `+4.560 m` for symmetric 40 mm cabin-to-rail gap)*.
- **Mounting Brackets & Structural Anchor Cover:**
  - **5 bracket tiers per rail (`10` brackets total):** `Y = -49.650, -48.150, -46.650, -45.150, -43.750` (explicitly straddling `Backrooms_Ceiling` `Y ∈ [-47.150, -46.950]` with `≥ 0.300 m` clearance and `0.150 m` clear below `UpperShaft_RoofCap`).
  - Surface-mounted expansion anchor embedment specification: **`50.0 mm`** into Bay 1 inner faces of `240.0 mm` concrete walls (`Bay1Shaft_DividerWall_North` and `Hotel_ElevatorCore_SouthWall`/`UpperShaft_SouthWall`) → **`190.0 mm` (`79.2%`) remaining solid concrete cover** (zero breach into Bay 2 or lobby).
- **Four Cabin-Mounted Guide-Shoe Assemblies (`Hotel_UpperPassengerElevatorCabin/Cabin_GuideShoes`, non-static):**
  - 2 South (`GuideShoe_South_Lower/Upper`) + 2 North (`GuideShoe_North_Lower/Upper`) at `Y_lower = -49.840` and `Y_upper = -47.760` (**`2.080 m` vertical separation**, `86.7%` of cabin height).
  - Each U-channel shoe comprises a `120 × 180 × 12 mm` `MountPlate`, `68 × 140 × 22 mm` `Saddle`, and `22 × 140 × 38 mm` `CheekW`/`CheekE` straddling the `16 mm` rail blade with deliberate non-overlapping clearances:
    - **Tip clearance (`Z`):** `+6.0 mm` (`saddle ↔ rail tip`)
    - **Lateral blade clearance (`X`):** `+4.0 mm` per side (`cheeks ↔ blade`)
    - **Flange & bracket passage clearance (`Z`):** `+8.0 mm` (`cheek tips ↔ rail flange & forged clips` across entire `3.560 m` vertical travel!)
  - Complete Cabin + Guide-Shoe Outer Envelope: `X ∈ [+1.320, +3.025]`, `Z ∈ [+4.548, +6.632]`, `Y ∈ [-50.000, -47.600]` (lower stop) → `[-46.440, -44.040]` (upper stop; `+490 mm` below `UpperShaft_RoofCap`, `+48 mm` from divider, `+108 mm` from South wall, `+80 mm` from West wall, `+125 mm` from East boundary, **`+685 mm`** from rails/brackets to nearest future door leaf `X = +2.965`).

## 3. Verification Results (07.27 Technical Gates)

- **Builder & Tests:** `UpperPassengerElevatorCabinBuilder.cs`, `RemanenciaUpperPassengerElevatorCabinTests.cs`, minimal updates to `BackroomsLevel1Builder.cs` and `RemanenciaUpperPassengerElevatorShaftTests.cs`.
- **2× Migration Idempotency:** `PASS` (`BackroomsLevel1Scene.unity` `SHA-256 = 4364A4AD56589A4A8A3CE29E618DF1BC4CDF8628945924028881FEC4EDAC2487` across Pass 1, Pass 2, and post-render).
- **Full EditMode Test Suite:** **`124 / 124` PASSED (`0` failed, `0` skipped)** — includes full 7-builder cross-idempotency (`07.17 → 07.19 → 07.21 → 07.23 → 07.24 → 07.26 → 07.27`) and full `3.560 m` swept-volume clearance proofs against all static shaft/rail/ceiling/upper-landing colliders and all four 07.25A reserved door volumes.
- **`SecretWedgeRoomVerificationSuite`:** **`11 / 11` PASSED**.
- **Windows x64 Standalone Build:** `PASS` (`MyVirtualCommunity.exe`, `0.64 MB`, `SHA-256 = 3070e580e019576b9afcdeb4fec18d03cebc119f6ae84d597412905c749ad653`, `Build Status: FRESH`, fingerprint `984a4c2022da3d0311d15d54822848d26e67fcb35e8f1e407dc5c67a2c681649`).
- **Forensic Protection & Light Audit:** All `22` protected subtrees (including `Hotel_Elevator_Core` `GO=331299473`, `35` children, `153` YAML blocks; 07.24 facades; `Backrooms_Floor`; upper slabs; all 10 Lightwell objects) verified **100% byte-identical** to `HEAD` (`3dcb66c`). Exact **`18` Unity `Light` components** preserved (`LightShadows.None`). Zero static flags anywhere in `Hotel_UpperPassengerElevatorCabin`; zero scripts, animators, audio sources, or triggers added. All 6 protected file SHA-256 hashes unchanged; reference repository untouched.
- **Deferred Visual Acceptance:** Engineering diagnostic views stored outside the repository; visual approval intentionally deferred to in-game walkthrough after 07.28–07.30 access and operation mechanisms are complete.


---

# 07.28 — Door System Engineering Reconciliation

**Mode:** Strictly Read-Only Engineering Audit & Design Reconciliation *(zero scene, source, builder, test, asset, or build modifications performed)*.  
**Verified Physical Baseline:** Combined `07.26` + `07.27` + `07.27A` serialized scene [BackroomsLevel1Scene.unity](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/unity-client/Assets/Scenes/BackroomsLevel1Scene.unity) (`SHA-256 = 4364A4AD56589A4A8A3CE29E618DF1BC4CDF8628945924028881FEC4EDAC2487`, branch `product/unity-fellowship-2027` @ `3dcb66c5ec712ba9d45e1f55b701a31e4b9321b5`).

---

## A. Investigation A — Verified Existing Door, Facade & Entrance Geometry

All coordinates, `fileID`s, materials, colliders, and static flags below were parsed directly from the serialized `07.27` scene YAML.

### 1. Lower Bay 1 & Core (`Hotel_Elevator_Core`, `GO=331299473`, `35` children, `static=0`)

| GameObject | `GO` / `TF` `fileID` | Parent | World Bounds `(X, Y, Z)` (m) | Material (`MR`) | Collider (`COL`) | Static |
| :--- | :---: | :--- | :--- | :--- | :--- | :---: |
| `Hotel_ElevatorBay_1_DoorLeafS` | `2004914621` / `2004914622` | `Hotel_Elevator_Core` | `[+3.100, +3.180] × [-50.000, -47.800] × [+5.000, +5.760]` | `ElevatorDoorMetal` (`en=1`) | `Box(en=1, trig=0)` | `0` |
| `Hotel_ElevatorBay_1_DoorLeafN` | `1307326071` / `1307326072` | `Hotel_Elevator_Core` | `[+3.100, +3.180] × [-50.000, -47.800] × [+5.740, +6.500]` | `ElevatorDoorMetal` (`en=1`) | `Box(en=1, trig=0)` | `0` |
| `Hotel_ElevatorBay_1_CenterSeam` | `1008553737` / `1008553738` | `Hotel_Elevator_Core` | `[+3.180, +3.200] × [-50.000, -47.800] × [+5.737, +5.763]` | `CeilingVoid` (`en=1`) | `none` | `0` |
| `Hotel_ElevatorBay_1_FrameJambS` | `464369093` / `464369094` | `Hotel_Elevator_Core` | `[+3.290, +3.470] × [-50.000, -47.780] × [+4.960, +5.040]` | `IndustrialMetal` (`en=1`) | `none` | `0` |
| `Hotel_ElevatorBay_1_FrameJambN` | `181796879` / `181796880` | `Hotel_Elevator_Core` | `[+3.290, +3.470] × [-50.000, -47.780] × [+6.460, +6.540]` | `IndustrialMetal` (`en=1`) | `none` | `0` |
| `Hotel_ElevatorBay_1_FrameHeader` | `1454700179` / `1454700180` | `Hotel_Elevator_Core` | `[+3.290, +3.470] × [-47.800, -47.720] × [+4.960, +6.540]` | `IndustrialMetal` (`en=1`) | `none` | `0` |
| `Hotel_ElevatorBay_1_FloorSill` | `228939351` / `228939352` | `Hotel_Elevator_Core` | `[+3.150, +3.450] × [-50.000, -49.976] × [+4.980, +6.520]` | `IndustrialMetal` (`en=1`) | `none` | `0` |
| `Hotel_ElevatorCore_FrontPierS` | `1166525667` / `1166525668` | `Hotel_Elevator_Core` | `[+3.150, +3.410] × [-50.000, -47.150] × [+4.135, +5.065]` | `ConcreteColumn` (`en=1`) | `Box(en=1, trig=0)` | `0` |
| `Hotel_ElevatorCore_FrontPierMid` | `1160025726` / `1160025727` | `Hotel_Elevator_Core` | `[+3.150, +3.410] × [-50.000, -47.150] × [+6.435, +7.165]` | `ConcreteColumn` (`en=1`) | `Box(en=1, trig=0)` | `0` |
| `Hotel_ElevatorCore_FrontHeader` | `1472619301` / `1472619302` | `Hotel_Elevator_Core` | `[+3.140, +3.420] × [-47.800, -47.150] × [+4.180, +9.420]` | `ConcreteColumn` (`en=1`) | `Box(en=1, trig=0)` | `0` |
| `Hotel_Elevator_CallPanelPlate` | `1622816346` / `1622816347` | `Hotel_Elevator_Core` | `[+3.417, +3.443] × [-48.950, -48.690] × [+6.730, +6.870]` | `IndustrialMetal` (`en=1`) | `none` | `0` |
| `Hotel_Elevator_CallButtonUp/Down` | `823213806` / `1114928462` | `Hotel_Elevator_Core` | `[+3.441, +3.455] × [-48.890, -48.750] × [+6.780, +6.820]` | `ElevatorDoorMetal` (`en=1`) | `none` | `0` |

### 2. Upper Bay 1 Facade, Sealed Backing & Structural Aperture (`UpperElevBay1_FinishedFacade`, `GO=1246408419`)

| GameObject | `GO` / `TF` `fileID` | Parent | World Bounds `(X, Y, Z)` (m) | Material (`MR`) | Collider (`COL`) | Blocks Path? |
| :--- | :---: | :--- | :--- | :--- | :--- | :---: |
| `UpperElevBay1_DoorLeafS` | `1440242510` / `1440242511` | `UpperElevBay1_StaticDoors` | `[+3.358, +3.398] × [-46.400, -44.200] × [+5.065, +5.755]` | `ElevatorDoorMetal` (`en=1`) | `Box(en=1)` | **YES (Visual + Solid)** |
| `UpperElevBay1_DoorLeafN` | `1567884466` / `1567884467` | `UpperElevBay1_StaticDoors` | `[+3.358, +3.398] × [-46.400, -44.200] × [+5.745, +6.435]` | `ElevatorDoorMetal` (`en=1`) | `Box(en=1)` | **YES (Visual + Solid)** |
| `UpperElevBay1_CenterSeam` | `1653810575` / `1653810576` | `UpperElevBay1_StaticDoors` | `[+3.397, +3.403] × [-46.397, -44.203] × [+5.743, +5.757]` | `CeilingVoid` (`en=1`) | `none` | **YES (Visual)** |
| `UpperElevBay1_DoorKickPlateS/N` | `50125276` / `665186354` | `UpperElevBay1_StaticDoors` | `[+3.398, +3.402] × [-46.390, -46.250] × [5.075..6.425]` | `IndustrialMetal` (`en=1`) | `none` | **YES (Visual)** |
| `UpperElev_SealedBay_1_SealedBackingPanel` | `1797691747` / `1797691748` | `UpperElevatorApproach_SealedBayFrames` | `[+3.346, +3.356] × [-46.400, -44.200] × [+5.065, +6.435]` | `YellowWallpaper` (`en=0`) | `Box(en=1)` | **YES (Solid Barrier)** |
| `UpperElevBay1_FrameJambS/N` | `1615858687` / `892475867` | `UpperElevBay1_PerimeterFrame` | `[+3.350, +3.442] × [-46.400, -44.160] × [5.001..5.065 / 6.435..6.499]` | `IndustrialMetal` (`en=1`) | `Box(en=1)` | No (outside portal) |
| `UpperElevBay1_FrameHeader` | `1117518460` / `1117518461` | `UpperElevBay1_PerimeterFrame` | `[+3.350, +3.442] × [-44.200, -44.120] × [+5.001, +6.499]` | `IndustrialMetal` (`en=1`) | `Box(en=1)` | No (above `-44.200`) |
| `UpperElevBay1_HeaderTrackReveal` | `562000039` / `562000040` | `UpperElevBay1_PerimeterFrame` | `[+3.391, +3.405] × [-44.224, -44.200] × [+5.068, +6.432]` | `ElevatorDoorMetal` (`en=1`) | `none` | No (`24 mm` trim lip) |
| `UpperElevBay1_FloorSill` | `552512289` / `552512290` | `UpperElevBay1_PerimeterFrame` | `[+3.350, +3.456] × [-46.400, -46.384] × [+5.010, +6.490]` | `IndustrialMetal` (`en=1`) | `none` | No (`16 mm` visual sill) |
| `Bay1UpperBacking_SouthPier` | `2116393766` / `2116393767` | `Bay1Shaft_UpperPortalBacking` | `[+3.150, +3.350] × [-46.400, -43.550] × [+4.200, +5.065]` | `ConcreteColumn` (`en=1`) | `Box(en=1)` | No (South of portal) |
| `Bay1UpperBacking_Transom` | `1886869923` / `1886869924` | `Bay1Shaft_UpperPortalBacking` | `[+3.150, +3.350] × [-44.200, -43.550] × [+5.065, +6.435]` | `ConcreteColumn` (`en=1`) | `Box(en=1)` | No (above `-44.200`) |
| `UpperElev_Waiting_WestCoreBackingWall` | `268893405` / `268893406` | `UpperElevatorApproach_WaitingArea` | `[+3.150, +3.350] × [-46.600, -43.550] × [+6.435, +9.400]` | `ConcreteColumn` (`en=1`) | `Box(en=1)` | No (North/Bay 2) |
| `UpperElev_Waiting_FloorSlab` | `1077748815` / `1077748816` | `UpperElevatorApproach_WaitingArea` | `[+3.150, +6.420] × [-46.600, -46.400] × [+3.980, +9.400]` | `DampCarpet` (`en=1`) | `Box(en=1)` | No (floor support) |
| `UpperElev_Support_WaitingCorePlinthW` | `1626614601` / `1626614602` | `UpperElevatorApproach_SupportStructure` | `[+3.150, +3.410] × [-46.940, -46.600] × [+4.200, +9.400]` | `ConcreteColumn` (`en=1`) | `Box(en=1)` | No (below floor slab) |

**Explicit Upper Access Path Finding:** Opening the backing wall in `07.26` does **not** by itself make the upper entrance traversable. The passage is currently blocked by **two solid collider layers** (`UpperElevBay1_DoorLeafS/N` at `X ∈ [+3.358, +3.398]` and invisible `UpperElev_SealedBay_1_SealedBackingPanel` at `X ∈ [+3.346, +3.356]`) plus three visual strips (`CenterSeam`, `DoorKickPlateS/N`). All perimeter frame members (`FrameJambS/N`, `FrameHeader`, `FloorSill`), structural piers, transom, floor slab, and plenum plinth are already clear of the `1.370 m × 2.200 m` opening.

### 3. 07.27 Cabin Entrance (`Hotel_UpperPassengerElevatorCabin`, `GO=317546358`, `static=0`)

- `Cabin_FloorSill_East` (`GO=1259314280`): `X ∈ [+2.940, +3.025]`, `Y ∈ [-50.000, -49.960]`, `Z ∈ [+4.620, +6.560]`, `ElevatorDoorMetal`, `Box(en=1)`.
- `Cabin_EastReturnSouth` (`GO=1072347169`): `X ∈ [+2.946, +2.960]`, `Y ∈ [-49.960, -47.720]`, `Z ∈ [+4.680, +5.300]`, `Box(en=1)` (+ `FinishSkin` `X ∈ [+2.942, +2.946]` & blank `FutureControlPanel_BlankReserve` `X ∈ [+2.940, +2.942]`, `COL=none`).
- `Cabin_EastStrikeJambNorth` (`GO=327932054`): `X ∈ [+2.940, +3.025]`, `Y ∈ [-49.960, -47.720]`, `Z ∈ [+6.405, +6.500]`, `Box(en=1)` + `Cabin_EastStrikeLipNorth` (`GO=1110637625`, `X ∈ [+2.940, +2.960]`, `Z ∈ [+6.400, +6.405]`).
- `Cabin_EastHeader` (`GO=154813343`): `X ∈ [+2.940, +2.960]`, `Y ∈ [-47.760, -47.720]`, `Z ∈ [+5.300, +6.400]`, `Box(en=1)`.
- **Compatibility:** Verified **100% compatible** with two-speed telescopic doors sliding South — clear portal is `Z ∈ [+5.300, +6.400]` (`1.100 m`) × `Y ∈ [-49.960, -47.760]` (`2.200 m`), overhead track pocket `X ∈ [+2.965, +3.025], Y ∈ [-47.760, -47.720]` is open, and South pocket `Z ∈ [+4.680, +5.300]` gives `+40 mm` past fully retracted leaves (`Z_min = +4.720`).

---

## B. Investigation B — Reconciled Two-Speed Telescopic Door Geometry & Swept-Volume Verification

**Door Type:** Two-speed telescopic sliding doors, both leaves retracting South (`-Z`) in parallel `X` planes at an exact **`2:1` speed ratio** (`v_fast = 2 × v_slow`; synchronized duration `1.60 s` → Slow travels `-0.550 m` at `0.34375 m/s`, Fast travels `-1.100 m` at `0.6875 m/s`, starting and completing simultaneously via smooth S-curve `t ∈ [0.0 → 1.0]`).

**Nominal Walk-Through Clear Opening:** `Z ∈ [+5.300, +6.400]` (`1.100 m` wide = `1.72×` player capsule `Ø 0.640 m`).

### 1. Exact Leaf, Track & Hardware Dimensions (All 3 Assemblies)

| Assembly | Component | `X` Bounds (m) | `Y` Bounds (m) | Closed `Z` (`t=0`) | Open `Z` (`t=1`) | Full Swept `Z` (m) | Dimensions `(X×Y×Z)` (m) | Collider |
| :--- | :--- | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **Cabin** *(child of Cabin root)* | **Slow Leaf** | `[+2.965, +2.990]` | `[-49.960, -47.760]` | `[+5.270, +5.850]` | `[+4.720, +5.300]` | `[+4.720, +5.850]` | `0.025 × 2.200 × 0.580` | `Box(en=1)` |
| **Cabin** | **Fast Leaf** | `[+3.000, +3.025]` | `[-49.960, -47.760]` | `[+5.820, +6.400]` | `[+4.720, +5.300]` | `[+4.720, +6.400]` | `0.025 × 2.200 × 0.580` | `Box(en=1)` |
| **Cabin** | **Track & Roller Header** | `[+2.965, +3.025]` | `[-47.760, -47.720]` | `[+4.680, +6.405]` | static | `[+4.680, +6.405]` | `0.060 × 0.040 × 1.725` | `none` |
| **Lower Landing** *(Bay 1)* | **Fast Leaf** | `[+3.060, +3.085]` | `[-49.976, -47.800]` | `[+5.820, +6.400]` | `[+4.720, +5.300]` | `[+4.720, +6.400]` | `0.025 × 2.176 × 0.580` | `Box(en=1)` |
| **Lower Landing** | **Slow Leaf** | `[+3.095, +3.120]` | `[-49.976, -47.800]` | `[+5.270, +5.850]` | `[+4.720, +5.300]` | `[+4.720, +5.850]` | `0.025 × 2.176 × 0.580` | `Box(en=1)` |
| **Lower Landing** | **Track & Roller Header** | `[+3.060, +3.120]` | `[-47.800, -47.720]` | `[+4.680, +6.435]` | static | `[+4.680, +6.435]` | `0.060 × 0.080 × 1.755` | `none` |
| **Upper Landing** *(Bay 1)* | **Fast Leaf** | `[+3.060, +3.085]` | `[-46.400, -44.200]` | `[+5.820, +6.400]` | `[+4.720, +5.300]` | `[+4.720, +6.400]` | `0.025 × 2.200 × 0.580` | `Box(en=1)` |
| **Upper Landing** | **Slow Leaf** | `[+3.095, +3.120]` | `[-46.400, -44.200]` | `[+5.270, +5.850]` | `[+4.720, +5.300]` | `[+4.720, +5.850]` | `0.025 × 2.200 × 0.580` | `Box(en=1)` |
| **Upper Landing** | **Track & Roller Header** | `[+3.060, +3.120]` | `[-44.200, -44.120]` | `[+4.680, +6.435]` | static | `[+4.680, +6.435]` | `0.060 × 0.080 × 1.755` | `none` |
| **Both Landings** *(static)* | **South Sidelite Return** | `[+3.150, +3.350]` | Lower `[-50.000,-47.800]` / Upper `[-46.400,-44.200]` | `[+5.065, +5.300]` | static | — | `0.200 × 2.200 × 0.235` | `Box(en=1)` |
| **Both Landings** *(static)* | **North Strike Return** | `[+3.150, +3.350]` | Lower `[-50.000,-47.800]` / Upper `[-46.400,-44.200]` | `[+6.400, +6.435]` | static | — | `0.200 × 2.200 × 0.035` | `Box(en=1)` |

*(Note: Lower Landing leaf height is reconciled to `2.176 m`, `Y ∈ [-49.976, -47.800]`, so leaves clear the existing `24 mm` `Hotel_ElevatorBay_1_FloorSill` at `-49.976` and tuck flush under `Hotel_ElevatorCore_FrontHeader` at `-47.800` with zero overlap.)*

### 2. Intermediate-Position Swept-Volume & Clearance Proof (`t ∈ {0.00, 0.25, 0.50, 0.75, 1.00}`)

Verified programmatically against every serialized `07.27` scene mesh and collider in `X ∈ [+2.90, +3.55]`:
- **Leaf-to-leaf stacking (`X` separation):** `10.0 mm` (`Cabin Slow ↔ Fast`: `2.990 → 3.000`; `Landing Fast ↔ Slow`: `3.085 → 3.095`). Closed meeting overlap in `Z` is `30.0 mm` (`[5.820, 5.850]`) — zero light or collision gap when closed, zero mesh or collider intersection at any `t ∈ [0, 1]`.
- **Cabin-to-landing door clearance (`X`):** `35.0 mm` (`Cabin Fast +3.025 → Landing Fast +3.060`).
- **Landing door to masonry piers/backing (`X`):** `30.0 mm` (`Landing Slow +3.120 → FrontPier/Backing +3.150`).
- **Cabin door to cabin returns/shell:** `+5.0 mm` to `Cabin_EastReturnSouth` (`+2.960 → +2.965`), `+5.0 mm` to `Cabin_EastStrikeJambNorth` (`+6.400 → +6.405`), `+40.0 mm` to `Cabin_WallSouth` inner face (`+4.680 → +4.720`).
- **South shaft pocket clearance:** `+280.0 mm` (`+4.440` South shaft wall → `+4.720` fully open leaves).
- **Concrete divider (Bay 2) clearance:** `+280.0 mm` (`+6.400` closed Fast Leaf → `+6.680` `Bay1Shaft_DividerWall_North`).
- **Guide rail, bracket & shoe clearance:** **`+685.0 mm`** (`+2.280` bracket `WallPlate` `X_max` → `+2.965` Cabin Slow Leaf `X_min`).
- **Collision sweep result against serialized `07.27` scene:**
  - Cabin Doors + Track Header: **`0` hits** across all `t`.
  - Upper Landing Doors + Track Header + Upper Sill Extension: **`0` hits** across all `t`.
  - Lower Landing Track Header: **`0` hits**.
  - Lower Landing Slow Leaf (`X ∈ [+3.095, +3.120]`): **ONLY** hits legacy static `Hotel_ElevatorBay_1_DoorLeafS/N` (`X ∈ [+3.100, +3.180]`), proving those legacy static leaves must be disabled when `Bay1Doors_LowerLanding` is installed!
- **Collider Activation Strategy:** Every moving leaf keeps its exact-fit non-trigger `BoxCollider` (`0.025 × H × 0.580 m`) permanently enabled and moving with its leaf transform (no collider toggling cheat); when `t = 1.0`, the full `Z ∈ [+5.300, +6.400]` portal is physically open.

---

## C. Investigation C — Lower Landing Door Integration Plan

- **Preservation Guarantee:** Retain `Hotel_Elevator_Core` (`GO=331299473`) with **all `35` children and fileIDs intact**, keeping Bay 2 (`Hotel_ElevatorBay_2_*`), all frames (`FrameJambS/N`, `FrameHeader`), `FloorSill`, `FrontPierS/Mid`, `FrontHeader`, `CallPanelPlate`, `CallButtonUp/Down`, and `Hotel_Sign_ElevatorNotice` (`FUERA DE SERVICIO`) **100% visually preserved**.
- **Least-Disruptive Replacement Strategy:**
  1. Disable `MeshRenderer` and `BoxCollider` on `Hotel_ElevatorBay_1_DoorLeafS` (`GO=2004914621`) and `Hotel_ElevatorBay_1_DoorLeafN` (`GO=1307326071`), and disable `MeshRenderer` on `Hotel_ElevatorBay_1_CenterSeam` (`GO=1008553737`) — preserving all 35 core children with **zero** hidden duplicate colliders or visual overlap!
  2. Create a dedicated sibling root **`Backrooms_Architecture/Hotel_UpperPassengerElevatorDoors`** containing:
     - `Bay1Doors_LowerLanding` (non-static `LowerDoor_FastLeaf`, `LowerDoor_SlowLeaf` in `ElevatorDoorMetal`; static `LowerDoor_TrackHeader`, `LowerDoor_SideliteReturnS`, `LowerDoor_StrikeReturnN`, plus a `45 mm` beveled threshold bridge plate `X ∈ [+3.055, +3.150], Y ∈ [-50.000, -49.976], Z ∈ [+5.065, +6.435]` flush with `Hotel_ElevatorBay_1_FloorSill` so stepping onto `Cabin_FloorSill_East` `-49.960` is a `16 mm` smooth step over solid uncut `Backrooms_Floor`).
     - `Bay1Doors_UpperLanding` (non-static `UpperDoor_FastLeaf`, `UpperDoor_SlowLeaf`; static `UpperDoor_TrackHeader`, `UpperDoor_SideliteReturnS`, `UpperDoor_StrikeReturnN`, and the mandatory `95 mm` `UpperDoor_SillExtension`).
  3. Create `Cabin_Doors` as a non-static child of **`Hotel_UpperPassengerElevatorCabin`** (`CabinDoor_SlowLeaf`, `CabinDoor_FastLeaf`, `CabinDoor_TrackHeader`) so the cabin door assembly translates automatically with the cabin in `07.30`!
- **Closed Appearance:** From the lobby, Bay 1 continues to display brushed-metal closed hotel elevator doors framed inside the existing oxidized-steel perimeter jambs (`+3.290..+3.470`) with a subtle `0.235 m` brushed-metal South sidelite return inside the opening.

---

## D. Investigation D — Upper Landing Door Integration & Complete X Cross-Section

### Complete East-to-West Cross-Section at Upper Stop (`Y ∈ [-46.400, -44.200]`, `Z ∈ [+5.300, +6.400]`)

```text
  CABIN INTERIOR  | CABIN DOORS     | GAP  | UPPER LANDING DOORS | BACKING WALL   | FACADE RECESS      | UPPER WAITING AREA
  X <= +2.940     | [2.965..3.025]  | 35mm | [3.060..3.120]      | [3.150..3.350] | [3.350..3.410]     | X >= +3.410
  ----------------+-----------------+------+---------------------+----------------+--------------------+-------------------
                  | [Slow]10[Fast]  |      | [Fast]10[Slow]      | (OPEN 07.26    | FrameJambS/N kept  |
                  |  2.965  3.000   |      |  3.060  3.095       |  portal Z=     | Old DoorLeafS/N &  |
                  |   ..     ..     |      |   ..     ..         |  5.065..6.435) | SealedBackingPanel |
                  |  2.990  3.025   |      |  3.085  3.120       |  + Sidelite S  | disabled in 07.29  |
  ----------------+-----------------+------+---------------------+----------------+--------------------+-------------------
  CABIN FLOOR     | CABIN SILL      | 30mm | UPPER SILL EXT.     | UPPER WAITING FLOOR SLAB (top Y = -46.400, uncut!)
  Y=[-50.00,-49.96]| (top -46.400   | gap  | X=[+3.055, +3.150]  | X=[+3.150, +6.420], Y=[-46.600, -46.400]
  @ Upper: -46.400|  at Upper Stop) |      | Y=[-46.480, -46.400]| + FloorSill visual trim X=[+3.350, +3.456]
  ----------------+-----------------+------+---------------------+-----------------------------------------------------------
```

### Exact Upper Object Disposition Table for `07.29`

| Existing Upper Object | Action in `07.29` | Reason & Identity Preservation |
| :--- | :--- | :--- |
| `UpperElevBay1_FinishedFacade` & `UpperElevBay1_PerimeterFrame` (`FrameJambS/N`, `FrameHeader`, `HeaderTrackReveal`, `FloorSill`) | **RETAIN UNCHANGED** | All 5 frame/sill GOs already sit outside `[5.065, 6.435] × [-46.400, -44.200]`; keeps approved hotel frame identity intact. |
| `UpperElevBay1_DoorLeafS/N` (`GO=1440242510/1567884466`) | **DISABLE `MR` + `COL` (retain GOs)** | Superseded by `Bay1Doors_UpperLanding` telescopic leaves (`X ∈ [3.060, 3.120]`), which stay closed & **locked** throughout `07.29`. |
| `UpperElevBay1_CenterSeam`, `DoorKickPlateS/N` | **DISABLE `MR` (retain GOs)** | Removes static center seam & kickplates from the future walkable portal while preserving hierarchy/fileIDs. |
| `UpperElev_SealedBay_1_SealedBackingPanel` (`GO=1797691747`) | **DISABLE `COL` (retain GO, `MR` already `en=0`)** | Solid safety containment transfers cleanly to the locked `UpperDoor_FastLeaf/SlowLeaf` `BoxCollider`s (Bay 1 remains 100% impassable until cabin arrives at Upper Stop in `07.30`!). |
| `UpperDoor_SillExtension` *(new in `07.29`)* | **ADD (`X ∈ [3.055, 3.150]`, `Y ∈ [-46.480, -46.400]`, `Z ∈ [5.065, 6.435]`, `BoxCollider`)** | Verified `0` collisions with `07.27` scene; bridges `UpperElev_Waiting_FloorSlab` (`+3.150`) to within `30.0 mm` of cabin nose (`+3.025`). |
| **ALL Bay 2 Objects** (`UpperElevBay2_UnfinishedFacade`, `UpperElev_SealedBay_2_SealedBackingPanel`, `UpperElev_Waiting_WestCoreBackingWall`, `Backrooms_Ceiling_North`, `Bay1Shaft_DividerWall_North`) | **100% UNTOUCHED** | Bay 2 remains permanently unfinished, sealed, and non-interactive. |

---

## E. Investigation E — Decoupled Door Interlocks & State Machine Architecture

To prevent a monolithic script, elevator runtime responsibilities are split cleanly into **four focused components**:

1. **`RemanenciaElevatorDoorAssembly`** *(MonoBehaviour — leaf motion & geometry only)*: Drives one 2-speed pair (`SlowLeaf` + `FastLeaf`) deterministically from normalized `openFraction ∈ [0f, 1f]` with exact `2:1` travel (`-0.550 m` / `-1.100 m`), exposing `IsFullyClosed`, `IsFullyOpen`, and `HasActiveSolidColliders`.
2. **`RemanenciaElevatorDoorInterlock`** *(Pure C# deterministic state machine — EditMode-testable without Unity physics)*:
   - **Per-Landing / Synchronized States:** `Closed`, `Opening`, `Open`, `Closing`, `Blocked`, `Locked`.
   - **Enforced Invariants:**
     1. Lower landing + Cabin doors can open **only** when `!cabinIsMoving && |cabinY - (-50.000)| ≤ 0.002 m`.
     2. Upper landing + Cabin doors can open **only** when `!cabinIsMoving && |cabinY - (-46.440)| ≤ 0.002 m` (therefore Upper landing stays **`Locked`** for the entirety of `07.29`!).
     3. Cabin door pair and active landing door pair share one synchronized `openFraction` driver (`07.29`–`07.30`).
     4. Any door open command while `cabinIsMoving == true` is unconditionally rejected.
     5. `CanCabinDepart()` returns `true` **only** when all 3 assemblies (`Lower`, `Upper`, `Cabin`) are in `Closed` or `Locked` (`openFraction == 0f`) with verified enabled solid colliders.
     6. Threshold obstruction (`Physics.CheckBox` spanning `X ∈ [+2.950, +3.180], Z ∈ [+5.250, +6.450], Y ∈ [floorY+0.08, floorY+2.05]` against player capsule) during `Closing` immediately transitions to `Blocked` → reverses to `Opening` until `Open` (`openFraction == 1f`), resetting dwell timer.
     7. Overlapping commands are serialized (duplicate open/close requests while already moving in that direction are idempotently coalesced; close requests while `Blocked` are deferred until threshold clears).
     8. Doors never invoke any scene unload or exit transition; Room 111-B Threshold of Trust remains the sole authorized exit from Remanencia.
3. **`RemanenciaElevatorDoorController`** *(Thin MonoBehaviour coordinator in `07.29`)*: Owns `RemanenciaElevatorDoorInterlock`, ticks `Update(Time.deltaTime)` to advance `openFraction` on the synchronized `RemanenciaElevatorDoorAssembly` pairs, runs threshold obstruction `Physics.CheckBox`, and exposes clean integration methods (`SetCabinState(float cabinY, bool isMoving)`, `bool RequestCloseForTravel()`, `bool AreAllDoorsClosedAndLockedForTravel`) for the future `07.30` movement controller.
4. **`RemanenciaPassengerElevatorController`** *(Deferred to `07.30`)*: Owns vertical travel state machine and synchronized `LateUpdate()` `CharacterController.Move()` transport; communicates strictly through `RemanenciaElevatorDoorController` without touching leaf transforms directly.

---

## F. Investigation F — Player Access & Bilingual Interaction Plan (`07.29`)

Verified directly against [FirstPersonPlayer.cs](file:///c:/Users/leolm/Desktop/Proyectos_AI/MyVirtualCommunity-AI-Hackathon-Edition/unity-client/Assets/Scripts/FirstPersonPlayer.cs) (`DrawKeypadDoorPrompt`, `BackroomsExitDoorTrigger`, `SewerManholeInteractable`):

- **Pattern Reused:** Proximity + facing `[E]` HUD prompt bar rendered in `FirstPersonPlayer.OnGUI()` (matching `BackroomsExitDoorTrigger` / `SewerManholeInteractable` — **no modal UI**, no cursor unlock, normal horizontal `WASD` movement and camera look uninterrupted!).
- **New Lightweight Interaction Bridge (`RemanenciaElevatorDoorInteractable`):**
  - Attached to two non-trigger proximity anchor points on Nivel 0 only (and **zero** interaction on Bay 2 or Upper Bay 1 while locked in `07.29`, or optional restrained locked feedback only if pressed at Upper Bay 1):
    1. **Lower Hall Call Zone** (centered on `Hotel_Elevator_CallPanelPlate`, `X = +3.44, Y = -48.82, Z = +6.80`, radius `2.20 m`, Bay 1 side).
    2. **Inside Lower Cabin Doorway** (centered near `Cabin_FutureControlPanel_BlankReserve`, `X = +2.60, Y = -48.80, Z = +5.40`, active when player is inside cabin bounds `X ∈ [+1.38, +2.94], Z ∈ [+4.68, +6.50]`).
- **Restrained Bilingual Prompts (Spanish / English):**
  - Lower lobby, doors `Closed`: `[E] Abrir puertas del elevador` / `[E] Open elevator doors`
  - Lower lobby or inside cabin, doors `Open`: `[E] Cerrar puertas del elevador` / `[E] Close elevator doors`
  - Doors `Opening` / `Closing` / `Blocked`: *(prompt suppressed while in motion to keep HUD calm)*
  - Upper landing (locked in `07.29` — no idle proximity prompt, matching 07.14 Annex A restraint; if explicitly tested later): *(no idle prompt; zero destination buttons in `07.29`)*.
- **Boarding Safety:** In `07.29`, players can open Lower Bay 1 doors, step smoothly across the `16 mm` sill onto the parked cabin floor (`Y = -49.960`), inspect the `2.839 m²` older-hotel interior, close/re-open doors from inside, and exit safely — with anti-pinch `CheckBox` preventing doors from ever closing through the player capsule.

---

## G. Mandatory Engineering Documentation Corrections (Section 10)

1. **Guide Rail Nomenclature Correction (Supersedes `ISO 7465 T70-1/B` label in `07.27`):**
   - Official ISO 7465 `T70-1/B` specifies $b_1 = 70\text{ mm}$, $h_1 = 65\text{ mm}$, $k = 9\text{ mm}$.
   - The serialized `07.27` rails measure `70 × 50 × 16 mm` (`70 × 10 mm` flange + `16 × 40 mm` blade).
   - **Authoritative Designation:** **`Custom stylized T-section guide rail (70 × 50 × 16 mm) inspired by elevator guide systems`**. *(The modeled `07.27` geometry is collision-free and remains unchanged.)*
2. **Anchor Bolt Representation Classification (Supersedes `50 mm embedment` claim in `07.27`):**
   - Direct YAML inspection of `RailBracket_S_0..4/AnchorBoltW/E` (`GO=1006933018/1003875478`, `Z ∈ [+4.454, +4.464]`) and `RailBracket_N_0..4/AnchorBoltW/E` (`GO=1717999807/802760253`, `Z ∈ [+6.656, +6.666]`) confirms each bolt is a **`0.022 × 0.022 × 0.010 m` non-colliding cube mounted on the Bay 1 exterior surface of the `14 mm` `WallPlate`**.
   - **Accurate Classification:** **Surface-mounted decorative anchor bolt heads (simplified visual anchor details)**. No `50 mm` embedded steel stem is modeled inside the concrete walls, and the full `240 mm` thickness of `Bay1Shaft_DividerWall_North` and the South shaft wall remains 100% unpenetrated by any mesh or collider.

---

## H. Verification & Test Strategy (`07.29`–`07.31`)

- **Deterministic EditMode Tests (`07.29`):**
  - Pure C# `RemanenciaElevatorDoorInterlock` unit tests covering all 6 states, all 10 invariants (`±0.002 m` stop tolerances, upper lock while parked at Nivel 0, refusal to open while moving, `CanCabinDepart()` refusal unless all 3 pairs closed with active colliders, obstruction reversal, serialized command handling).
  - Serialized scene geometry & swept-volume proofs across `t ∈ [0.00, 0.25, 0.50, 0.75, 1.00]` for all 3 door pairs, tracks, returns, and `UpperDoor_SillExtension`, plus 8-builder cross-idempotency (`07.17 → 07.29`), `35`-child `Hotel_Elevator_Core`, exact `18` lights, Bay 2 isolation, and `BorderCorridorScene` SHA-256.
- **Live PlayMode Integration Tests (`07.29` & `07.30–07.31`):**
  - `07.29`: Lower Bay 1 `[E]` open, `CharacterController.Move()` walk-in across lower sill into parked cabin, interior close/re-open, anti-pinch reversal when standing in threshold, walk-out, and Upper Bay 1 remaining physically locked.
  - `07.30–07.31`: Full ascent/descent `CharacterController.Move()` vertical transport, upper landing unlock & egress into Upper Waiting Area, return descent, and live validation of all 4 recovery scenarios (cold startup, additive unload/reload, termination during ride, abnormal mid-ride disable).

---

## I. Revised Implementation Roadmap

1. **`07.28` (Complete — Current Audit):** Read-only door engineering reconciliation, cross-sections, swept-volume proofs, interlock architecture, player interaction design, and nomenclature/anchor corrections.
2. **`07.29` (Next — Synchronized Door Assemblies & Controlled Lower-Floor Access):** Implement all 3 two-speed telescopic door assemblies (`Cabin`, `LowerLanding`, `UpperLanding`), tracks/returns/`UpperDoor_SillExtension`, legacy Bay 1 leaf disablement (preserving all fileIDs and 35 core children), `RemanenciaElevatorDoorAssembly` + `RemanenciaElevatorDoorInterlock` + `RemanenciaElevatorDoorController`, minimal bilingual `[E]` interaction for lower-floor boarding/inspection (cabin stationary at Nivel 0; upper landing locked), plus EditMode + PlayMode door/boarding tests.
3. **`07.30` (Deterministic Elevator Movement & Synchronized Player Transport):** Implement `RemanenciaPassengerElevatorController`, `FirstPersonPlayer.SetExternalVerticalTransport(bool)` hook, `LateUpdate()` synchronized `CharacterController.Move()` ascent/descent (`ΔY = 3.560 m`), interior destination control & upper call button, and door-interlock handshake.
4. **`07.31` (Recovery, Lifecycle, Audio Restraint & Final Hardening):** Implement and PlayMode-validate all 4 recovery scenarios under `schemaVersion == 1`, restrained mechanical audio, full regression & standalone build, and final in-game creator walkthrough and visual acceptance.

*(Dependency Note: Placing `UpperDoor_SillExtension` and the locked `Bay1Doors_UpperLanding` assembly in `07.29` keeps all three door assemblies under one idempotent builder and ensures `07.30` only adds vertical motion + controls without reopening scene geometry.)*

---

## J. Engineering Readiness Verdict

> **`READY FOR 07.29 IMPLEMENTATION`** *(all lower, upper, and cabin two-speed telescopic door geometries, tracks, sills, swept volumes, interlock invariants, Bay 2 protections, and player access paths are mathematically reconciled against the serialized `07.27` scene with zero unresolved collisions)*.


---

# 07.29 — Implementation Completion Record: Bay 1 Functional Doors + Bay 2 Permanently Open Unfinished Shaft

**Status:** Implemented & verified via EditMode (`128/128`) and real Unity PlayMode (`3/3`) `CharacterController` integration tests *(creator visual acceptance remains explicitly deferred until elevator movement and access systems can be walked through together in-game)*.  
**Superseding Architectural Decision:** This milestone **formally supersedes all earlier design notes (07.24–07.28) describing Bay 2 as permanently sealed**. Historical sections above are preserved for audit traceability, but the authoritative canonical state of the Twin Elevator Core is now:

- **Bay 1 (South, `Z = +5.750`):** Completed, finished-looking passenger elevator with parked cabin (`Hotel_UpperPassengerElevatorCabin`, `Y = -50.000`) and functional three-assembly two-speed telescopic doors (`Cabin`, `LowerLanding`, `UpperLanding`). Stationary at Nivel 0 in `07.29`; upper landing locked; vertical movement deferred to `07.30`.
- **Bay 2 (North, `Z = +7.850`):** Abandoned, **permanently open**, never-completed elevator shaft. **Bay 2 must NEVER receive an elevator cabin, placeholder cabin, closable doors, door controller, or invisible blocking barrier** at the lower floor, upper floor, or **any future floor**.

---

## 1. Bay 1 — Implemented Three-Assembly Telescopic Door System & Lower Threshold Cross-Section

- **Runtime Architecture (`unity-client/Assets/Scripts/World/`):**
  - `RemanenciaElevatorDoorInterlock.cs`: Pure C# deterministic state machine (`Closed`, `Opening`, `Open`, `Closing`, `Blocked`, `Locked`), authoritative cabin root Y datum (`Lower = -50.000 m`, `Upper = -46.440 m`, tolerance `±0.002 m`), departure-readiness interlock (`CanCabinDepart()` requires all 3 assemblies closed/locked with `openFraction <= 0.0005` and active solid colliders), and mid-close obstruction reversal.
  - `RemanenciaElevatorDoorAssembly.cs`: Exact `2:1` telescopic motion (`Slow -0.550 m`, `Fast -1.100 m`, `1.60 s` duration, solid non-trigger `BoxCollider`s permanently enabled).
  - `RemanenciaElevatorDoorController.cs`: Explicit Bay 1 references (zero Bay 2 discovery), real `Physics.OverlapBoxNonAlloc` threshold anti-pinch (`X ∈ [+2.965, +3.145], Z ∈ [+5.310, +6.390], Y ∈ [floorY+0.08, floorY+2.02]`) ignoring own door/cabin/static colliders while reversing immediately on `CharacterController` obstruction.
  - `RemanenciaElevatorDoorInteractable.cs` + minimal hook in `FirstPersonPlayer.cs`: Reuses existing bottom-center bilingual `[E]` HUD prompt (`LowerHallPanel` at `(3.44, -48.82, 6.80)` gated strictly to Bay 1 South `Z <= +6.95` & facing Bay 1; `CabinInterior` at `(2.60, -48.80, 5.40)`). Zero modal UI, zero Bay 2 prompts, zero `SetExternalVerticalTransport`.
- **Verified Lower Threshold Cross-Section (PlayMode `CharacterController` Traversed in Both Directions):**
  - Lobby `Backrooms_Floor` (`X ≥ +3.150`, top `Y = -50.000`) → `Hotel_ElevatorBay_1_FloorSill` (`X ∈ [+3.150, +3.450]`, top `-49.976`, visual) + `LowerDoor_ThresholdPlate` (`X ∈ [+3.055, +3.150]`, `Y ∈ [-50.000, -49.976]`, solid `BoxCollider`, `+24 mm` rise) → `30.0 mm` horizontal running gap (`X ∈ [+3.025, +3.055]`, continuously underlain by uncut solid `Backrooms_Floor` at `Y = -50.000` — zero unsupported void!) → `Cabin_FloorSill_East` & `Cabin_FloorDeck_Main` (`X ∈ [+1.320, +3.025]`, `Y ∈ [-50.000, -49.960]`, `+16 mm` rise; total `40 mm` = `13.3%` of `stepOffset = 0.300 m`).
- **Upper Sill Extension:** `UpperDoor_SillExtension` (`X ∈ [+3.055, +3.150], Y ∈ [-46.480, -46.400], Z ∈ [+5.065, +6.435]`, flush with `UpperElev_Waiting_FloorSlab` `-46.400`, preserving `30.0 mm` running gap to cabin nose `+3.025`).
- **Legacy Object Preservation:** `Hotel_Elevator_Core` (`GO=331299473`) retains all `35` children & fileIDs; legacy static Bay 1 leaves/seam and upper Bay 1 static leaves/backing collider disabled only after replacement Bay 1 telescopic assemblies were created and verified.

---

## 2. Bay 2 — Permanently Open Unfinished Shaft & Future-Floor Contract

- **Exact Accessible Vertical Extent & Termination:**
  - Clear horizontal shaft interior: **`X ∈ [+1.240, +3.150]` (`1.910 m`) × `Z ∈ [+6.920, +9.160]` (`2.240 m`)**.
  - Clear vertical extent: **`Y ∈ [-50.000, -43.550]` (`6.450 m` continuous unobstructed void)**.
  - Genuine bottom termination: uncut monolithic **`Backrooms_Floor` (`Y = -50.000`, solid `BoxCollider`)** — zero fake catch planes, zero cabin or platform.
  - Upper shaft enclosure: `Bay2Shaft_BackWall` (`X ∈ [1.000, 1.240]`), `Bay2Shaft_NorthWall` (`Z ∈ [9.160, 9.400]`), `Bay2Shaft_RoofCap` (`Y ∈ [-43.550, -43.350]`), `Bay2Shaft_EastPlenumSeal` (`Y ∈ [-46.950, -46.940]`).
  - Intermediate ceiling clearance: `Backrooms_Ceiling_North` trimmed cleanly to `Z ∈ [+9.160, +12.300]`, removing the ceiling obstruction over Bay 2 while keeping lobby ceiling North of the core sealed.
  - Permanently open doorways: Lower Bay 2 (`Z ∈ [+7.165, +8.535], Y ∈ [-50.000, -47.800]`, legacy `Hotel_ElevatorBay_2_DoorLeafS/N/CenterSeam` disabled) and Upper Bay 2 (`Z ∈ [+7.165, +8.535], Y ∈ [-46.400, -44.200]`, legacy infill/closure/backing disabled, `UpperElev_Waiting_WestCoreBackingWall` trimmed to Mid Pier `Z ∈ [+6.435, +7.165]`, raw concrete `UpperElevBay2_ConstructionFrame` retained).
  - Visual-only unfinished construction details (`COL=none`, zero intrusion into primary traversal/fall volume): staggered incomplete guide-rail stubs & brackets, overhead abandoned hoist beam, 4 hanging suspension cables tucked in far NW corner (`X ∈ [1.42, 1.50], Z ∈ [8.71, 8.86]`), and corner cable spool.
- **Preserved Bay 1 / Bay 2 Separation:** Full-height `0.240 m` concrete divider `Bay1Shaft_DividerWall_North` (`X ∈ [1.240, 3.150], Z ∈ [6.680, 6.920], Y ∈ [-50.000, -43.550]`) + `Bay1Shaft_DividerTopCoping` (`Y ∈ [-43.550, -43.350]`) preserved 100% intact and verified in PlayMode to block passage between Bay 1 and Bay 2.
- **Canonical Future-Floor Contract:** Every future floor connecting to Bay 2 MUST invoke `UpperPassengerElevatorDoorsBuilder.EnsureBay2OpenLandingOpening(parent, groupName, floorElevationY, clearPortalHeightY, ceilingSoffitY, concreteMat, trimMetalMat)` (verified idempotently across two hypothetical elevations `Y = -42.800` and `Y = -38.900` in isolated EditMode tests) so no future floor can ever seal Bay 2 or add a cabin.
- **Gameplay Readiness & Current Limitations:** Bay 2 physically supports player entry, hiding/movement, and uninterrupted falling from Upper (`-46.400`) to Lower (`-50.000`), and leaves the shaft structurally open for future antagonist/creature traversal. Climbing mechanics, creature/antagonist AI, fall damage, death/respawn, elevator vertical travel (`07.30`), and elevator audio/lifecycle recovery (`07.31`) are intentionally NOT implemented in `07.29`.

---

## 3. Verification Summary (`07.29`)

- **Scene SHA-256:** Pre-07.29 `4364A4AD56589A4A8A3CE29E618DF1BC4CDF8628945924028881FEC4EDAC2487` → Post-07.29 **`22BD42F8BC11E364BC2B890570335D89F9BD6299810D0C4B1A155E69E754620C`** (`IDEMPOTENT_2X_SHA_MATCH = True`).
- **EditMode Tests:** **`128 / 128` PASSED (`0` failed, `0` skipped)** (includes pure C# interlock, 3-assembly 2:1 geometry & anti-pinch filter, isolated future-floor Bay 2 contract, and full 8-builder cross-idempotency `07.17 → 07.29`).
- **Real Unity PlayMode Tests (`-runTests -testPlatform PlayMode`):** **`3 / 3` PASSED (`0` failed)** using real `CharacterController` in `BackroomsLevel1Scene` (Bay 1 full open/board/close/reopen/exit + lower threshold traversal both ways + upper locked; Bay 1 anti-pinch in all 4 doorway positions; Bay 2 lower entry, divider containment, upper entry, uninterrupted free-fall to `-50.000`, and exit).
- **`SecretWedgeRoomVerificationSuite`:** **`11 / 11` PASSED**.
- **Windows x64 Standalone Build:** `MyVirtualCommunity.exe` (`0.64 MB`, `SHA-256 = 3070e580e019576b9afcdeb4fec18d03cebc119f6ae84d597412905c749ad653`, fingerprint `fd083fcf838d79ffbed5d012c484f0c865f1ab48af9c594b966f0cd11159ff65`, **`Build Status: FRESH`**).
