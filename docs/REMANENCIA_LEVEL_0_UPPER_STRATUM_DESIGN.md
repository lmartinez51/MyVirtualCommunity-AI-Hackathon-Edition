# REMANENCIA — Nivel 0: Upper Stratum Spatial Audit & Architectural Design (07.16)

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia** *(overarching dimensional universe — proper name, untranslated in ES/EN)*  
**Current Playable Territory:** **Nivel 0 — Hotel de las Palomas** (`unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`)  
**Baseline Audited:** Branch `product/unity-fellowship-2027`, HEAD `647d605978df79e05f702b077e5109feac5b558a` (Implementation 07.15 synchronized; working tree clean prior to this document)  
**Document Status:** **IMPLEMENTATION 07.16 — READ-ONLY FORENSIC SPATIAL AUDIT & ARCHITECTURAL DESIGN (DESIGN-ONLY; ZERO SCENE, BUILDER, SCRIPT, OR ASSET IMPLEMENTATION)**

> **Mandatory Measurement & Status Labels:**  
> Every coordinate, dimension, and architectural feature in this document is explicitly distinguished as:
> - **`VERIFIED EXISTING`** — Directly extracted from serialized transforms, colliders, and `fileID`s in `BackroomsLevel1Scene.unity` (`SHA-256: F2B8F4BE76DC43555FC1B61BAD1A42089826805B7A56CB4F11CF5B64BF6A1A2A`) and `BorderCorridorScene.unity` at `647d605`.
> - **`PROPOSED`** — Concrete geometric target proposed for a future authorized 07.16+ implementation increment; **not built**.
> - **`ESTIMATED`** — Approximate spatial area, lighting count, or budget projection pending prototyping.
> - **`UNRESOLVED`** — Open creative, naming, or structural decisions requiring explicit creator approval before any geometry is authored.

---

## 1. Executive Design Intent *(PROPOSED)*

Implementation 07.15 constructed the **Northwest Service Staircase** (`Hotel_NorthwestServiceStaircase`), providing Remanencia's first physically traversable vertical connection from Nivel 0 (`Y = -50.000 m`) up `3.600 m` to a sealed, unfinished upper landing at `Y = -46.400 m`.

The design intent for **07.16 — Upper Stratum** is **not** to rush into an expansive, empty second hotel floor or to prematurely activate the Twin Elevator Core. Instead, 07.16 should open the first **disciplined, architecturally coherent upper spatial fragment** (`~125–145 m²` `ESTIMATED`) that:

1. Continues organically out of the 07.15 Upper Landing's already-framed East continuation recess (`NWStair_UpperShell_SealedEastFrame*`), preserving the unfinished northern-Mexican concrete hotel identity.
2. Uses Nivel 0's **actual verified 3D geometry** — specifically the **Central Courtyard Lightwell (`Y = -47.15 → -44.64 m`) protruding `+1.76 m` directly up through the Upper Stratum elevation** — as a quiet, grounded architectural contradiction (*an interior roof trapped inside an upper enclosed floor*) rather than hiding it or treating it as an obstacle to work around.
3. Pairs that roof contradiction with a subtle **horizontal footprint contradiction** (an upper corridor extending `+5.40 m` East past Nivel 0's North Wing perimeter wall over the hidden Room 111-B pocket below), communicating through pure space — with zero lore dumps, zero supernatural VFX, and zero monsters — that Hotel de las Palomas does not obey Euclidean exterior dimensions.
4. Remains **100% reversible** back down the Northwest Service Staircase to Nivel 0 → Room 111-B → **Threshold of Trust** (the sole authorized in-world exit), while leaving clean sealed continuation seams toward the future **Upper Elevator Landing** (`07.17+`) without touching a single existing elevator object.

---

## 2. Verified 07.15 As-Built Baseline *(VERIFIED EXISTING)*

Extracted directly from `BackroomsLevel1Scene.unity` and `NorthwestServiceStaircaseBuilder.cs` at commit `647d605`:

### A. Upper Landing Shell (`Hotel_NorthwestServiceStaircase/NWStair_UpperLandingShell`)

| Serialized Object | `fileID` (GO / TF) | World Position `(X, Y, Z)` | Scale `(X, Y, Z)` | Collider | Role (`VERIFIED EXISTING`) |
| :--- | :---: | :---: | :---: | :---: | :--- |
| `NWStair_UpperLanding_FloorSlab` | `1213579175` / `1213579176` | `(-10.705, -46.500, 31.390)` | `(1.990, 0.200, 4.960)` | Solid | Walkable slab: `X ∈ [-11.700, -9.710]`, `Z ∈ [+28.910, +33.870]` (`9.87 m²`), top `Y = -46.400 m`, bottom `Y = -46.600 m`. |
| `NWStair_UpperShell_CeilingSlab` | `354343358` / `354343359` | `(-12.800, -43.450, 31.400)` | `(6.700, 0.200, 5.500)` | Solid | Sealed roof slab: bottom soffit `Y = -43.550 m` (`2.850 m` clear height above Upper Landing), top `Y = -43.350 m`. |
| `NWStair_UpperShell_WallEast` | `2109824158` / `2109824159` | `(-9.600, -45.350, 31.400)` | `(0.220, 3.600, 5.460)` | Solid | Sealed East wall (`Y ∈ [-47.150, -43.550]`, inner face `X = -9.710`, outer face `X = -9.490`). Directly aligns over `Hotel_Room110_WestServiceWall`. |
| `NWStair_UpperShell_WallNorth` | `1278456196` / `1278456197` | `(-12.800, -45.350, 34.000)` | `(6.660, 3.600, 0.260)` | Solid | Sealed North perimeter wall (inner face `Z = +33.870`, outer face `Z = +34.130`). |
| `NWStair_UpperShell_WallSouth` | `422226400` / `422226401` | `(-12.800, -45.350, 28.800)` | `(6.660, 3.600, 0.220)` | Solid | Sealed South wall (inner face `Z = +28.910`, outer face `Z = +28.690`). |
| `NWStair_UpperShell_WallWest` | `1427264638` / `1427264639` | `(-16.000, -45.350, 31.400)` | `(0.260, 3.600, 5.460)` | Solid | Sealed West stairwell shaft wall (inner face `X = -15.870`). |
| `NWStair_UpperLanding_NorthGuardParapet` | `2109228133` / `2109228134` | `(-11.780, -45.875, 32.845)` | `(0.160, 1.050, 2.050)` | Solid | Fall guard overlooking Flight 1 (`Z ∈ [+31.820, +33.870]`, top `Y = -45.350 m`). |
| `NWStair_UpperLanding_SouthGuardParapet` | `1042336553` / `1042336554` | `(-11.780, -45.875, 29.485)` | `(0.160, 1.050, 1.150)` | Solid | Fall guard overlooking South well (`Z ∈ [+28.910, +30.060]`, top `Y = -45.350 m`). |
| `NWStair_UpperShell_SealedEastFrameS/N/Top` | `1635426459` / `1149530410` / `939874756` | `X = -9.745`, `Z = 30.20 / 32.40 / 31.30` | Jambs `0.16 × 2.36 m`; Top `2.36 × 0.16 m` | None | **Pre-framed East continuation relief** centered at `Z = +31.300 m` (`2.04 m` clear width between jambs `Z ∈ [+30.28, +32.32]`, `2.24 m` clear height `Y ∈ [-46.40, -44.16]`), directly facing a player exiting Flight 2 (`Z ∈ [+30.06, +31.82]`). |
| `NWStair_UpperShell_SealedNorthFrameW/E/Top` | `720995411` / `1368300195` / `493630012` | `Z = +33.835`, `X = -11.45 / -9.95 / -10.70` | Jambs `0.16 × 2.36 m`; Top `1.66 × 0.16 m` | None | **Pre-framed North continuation relief** (`1.34 m` clear width `X ∈ [-11.37, -10.03]`, `2.24 m` clear height), vertically aligned above Nivel 0's North anchor zone. |
| `NWStair_UpperShell_CeilingBeam` | `130513915` / `130513916` | `(-11.700, -43.690, 31.390)` | `(0.260, 0.280, 4.960)` | None | Structural overhead transfer beam along `X = -11.700` (bottom `Y = -43.830 m`, `2.57 m` clearance). |
| `NWStair_UpperShell_ConduitVertical` + `ConduitBox` | `2094913134` / `688537801` | `(-9.750, -44.980, 29.150)` | `(0.035, 2.840, 0.035)` | None | Unfinished vertical conduit stub in SE corner of Upper Landing (`Z = +29.150 m`, clear of East frame). |
| `NWStair_UpperLanding_StripTube` + `ShaftWall_StripTube` | `479177714` / `1939993236` | `(-10.705, -43.605, 31.390)` / `(-15.810, -45.600, 31.920)` | Emissive only | None | Zero Unity `Light` components added; total scene lights remain **`18`** (`LightShadows.None`). |

### B. Verified Player Controller Parameters (`BorderCorridorScene.unity`, `fileID=1986755685`)
- **`VERIFIED EXISTING`:** `CharacterController` `height = 1.80 m`, `radius = 0.28 m`, `skinWidth = 0.04 m` (effective capsule radius **`0.32 m`**, diameter **`0.64 m`**), `slopeLimit = 45°`, `stepOffset = 0.30 m`, `center = (0, 0.90, 0)`. Grounded snap `verticalVelocity = -1.5f` (`FirstPersonPlayer.HandleMovement()`).

---

## 3. Existing 3D Spatial Constraints Above Nivel 0 *(VERIFIED EXISTING)*

Auditing every serialized object in `BackroomsLevel1Scene.unity` intersecting `Y ∈ [-47.150, -43.000]` reveals **four decisive 3D constraints** governing any Upper Stratum at `Y = -46.400 m`:

### Constraint 1 — Clean Structural Plenum Over Standard Nivel 0 Ceilings (`VERIFIED EXISTING`)
- Nivel 0 ceilings (`Backrooms_Ceiling`, `NorthWing_CeilingSlab_*`, `Room111B_*_CeilingSlab`) have soffit `Y ≈ -47.150 m` and top face `Y ≈ -46.948 m`.
- All 24 South Core `Backrooms_FluorPanel_*` fixtures sit strictly below `Y = -47.14 m` (verified parent `localScale.y = 0.046` at `pos.y = -47.222`).
- With Upper Stratum floor slab `Y ∈ [-46.600, -46.400]`, a clean **`0.35 m` structural plenum (`Y ∈ [-46.95, -46.60]`)** separates Nivel 0 ceilings from the Upper Stratum slab everywhere outside the Courtyard Lightwell — eliminating Z-fighting and preventing ceiling-fixture bleed!

### Constraint 2 — Central Courtyard Lightwell Protrusion (`CRITICAL — VERIFIED EXISTING`)
- **`NorthWing_Courtyard`** includes a tall enclosed lightwell rising **directly through the Upper Stratum floor elevation (`Y = -46.400 m`)**:
  - `NorthWing_Lightwell_WallN` (`GO=585720393`, `Z = +26.08`), `WallS` (`GO=963459595`, `Z = +17.92`), `WallW` (`GO=736225269`, `X = -5.08`), `WallE` (`GO=894939850`, `X = +5.08`): solid `BoxCollider`s spanning **`X ∈ [-5.18, +5.18], Z ∈ [+17.82, +26.18], Y ∈ [-47.150, -44.800]`** (`+1.600 m` above Upper Landing floor!).
  - `NorthWing_Lightwell_SkylightCap` (`GO=1795628428`): solid translucent diffuser cap spanning **`X ∈ [-5.30, +5.30], Z ∈ [+17.70, +26.30], Y ∈ [-44.800, -44.640]`** (`+1.760 m` above Upper Landing floor!).
- **Hard Geometric Rule:** No Upper Stratum floor or corridor may intersect **`X ∈ [-5.30, +5.30], Z ∈ [+17.70, +26.30]`** at `Y = -46.400 m`. Instead, Upper Stratum geometry must stay cleanly **North (`Z ≥ +26.35`)**, **West (`X ≤ -5.35`)**, **East (`X ≥ +5.35`)**, or **South (`Z ≤ +17.65`)** of the lightwell — OR intentionally frame the lightwell's outer concrete walls and glowing skylight cap (`Y = -44.64 m`) from a guarded viewing edge!

### Constraint 3 — Available Unoccupied 3D Envelopes Around the Upper Landing (`VERIFIED EXISTING`)
1. **East of `NWStair_UpperShell_WallEast` (`X ≥ -9.49`, `Z ∈ [+26.35, +34.00]`):** **`25.49 m × 7.65 m = 195 m²`** directly above Rooms `110–107` and the North Guest Corridor, PLUS open unoccupied space extending further East (`X ∈ [+16.00, +24.80]`, above Room 111-B's `Y = -46.95` ceiling!). **Best natural opening:** existing `SealedEastFrame` (`Z ∈ [+30.28, +32.32]`, `2.04 m × 2.24 m`) straight ahead off Flight 2!
2. **South of `NWStair_UpperShell_WallSouth` (`X ∈ [-16.00, -5.35]`, `Z ∈ [+12.00, +28.69]`):** **`10.65 m × 16.69 m = 178 m²`** above the Housekeeping Core and West Arcade, running directly beside `NorthWing_Lightwell_WallW` (`X = -5.18`) and projecting South toward the South Core (`Z ≤ +12.00`). Note: opening `WallSouth` requires turning 90° right off Flight 2 and adjusting `NWStair_UpperLanding_SouthGuardParapet` / `ConduitVertical`.
3. **North of `NWStair_UpperShell_WallNorth` (`X ∈ [-16.00, +16.00]`, `Z ≥ +34.13`):** Unbounded exterior void North of the hotel footprint (`SealedNorthFrame` provides `1.34 m × 2.24 m` aperture), though expanding strictly North moves *away* from both the Courtyard Lightwell and the Twin Elevator Core.
4. **Upper Projection Above `Hotel_Elevator_Core` (`X ∈ [+1.00, +3.41], Z ∈ [+4.20, +9.40]`, waiting bay `X ∈ [+3.41, +6.20]`):** Completely unoccupied at `Y ∈ [-46.60, -43.35]`, reachable in future increments (`07.17+`) either from the West (`Z = +12..+17`) or East (`X = +5.4..+9.6, Z = +12..+26`) around the Courtyard Lightwell!

### Constraint 4 — Existing Automated Test & Resource Budgets (`VERIFIED EXISTING`)
- `RemanenciaNorthwestStaircaseTests.cs` currently asserts all 4 walls of `NWStair_UpperLandingShell` are solid (`WallEast` linecast at `(-10.70 → -9.00, Z=31.30)`, `WallNorth`, `WallSouth`, `WallWest`). Any future 07.16 implementation opening a doorway in `WallEast` (or `WallSouth`) must intentionally update **only** the corresponding doorway linecast while keeping the remaining shell walls and guard parapets solid.
- **Lighting Budget:** Multiple test suites (`RemanenciaLevel0VisualTests`, `RemanenciaNorthWingExpansionTests`, `RemanenciaNorthwestStaircaseTests`) enforce exact **`18` Unity `Light` components (`LightShadows.None`)**. Unless a future increment explicitly expands the light budget with creator approval, 07.16 should rely strictly on **emissive-only ceiling/wall industrial strips (`mats.UtilityWarmOn`, `mats.LightwellDiffuser`)** + flat `RemanenciaLevel0AmbientLight`, maintaining **zero added Unity `Light` components** and full 2 GB VRAM safety!

---

## 4. Layout Alternatives Evaluated *(PROPOSED)*

Three technically feasible layout alternatives were evaluated against the verified 3D constraints above.

### Alternative 1 — **North Upper Fragment, Trapped-Skylight Overlook & East Overhang Anomaly** *(RECOMMENDED OPTION)*
- **Concept:** A focused, restrained **architectural fragment** opening straight East out of the 07.15 Upper Landing's pre-framed `SealedEastFrame` (`X = -9.60, Z ∈ [+30.28, +32.32]`), combining:
  1. An **Upper North Corridor** (`X ∈ [-9.60, +16.00], Z ∈ [+29.80, +32.80]`, `25.6 m × 3.0 m`) running above Rooms `110–107` where unfinished raw concrete gradually shifts into older, half-plastered hotel framing and mismatched room bays (sealed/bricked-over doorways or empty partial alcoves — no full rooms yet).
  2. A short **South Skylight Overlook Bay** (`X ∈ [-4.60, +4.60], Z ∈ [+26.40, +29.80]`, `9.2 m × 3.4 m`) branching South off the corridor directly up to the **North wall of Nivel 0's Courtyard Lightwell (`Z = +26.08`)** — guarded by a solid `1.05 m` concrete parapet along `Z = +26.40` so the player looks directly over the **glowing translucent skylight roof (`Y = -44.64 m`) rising `+1.76 m` inside an enclosed upper interior room!**
  3. An **Impossible East Overhang Extension** (`X ∈ [+16.00, +21.40], Z ∈ [+29.00, +33.40]`, `5.4 m × 4.4 m`): instead of stopping at Nivel 0's North Wing East wall (`X = +16.00`), the upper corridor keeps going `+5.40 m` East (directly above the hidden Room 111-B pocket below!), ending in a quiet, sealed unfinished concrete alcove with an unnumbered doorway frame and a sealed South-East continuation frame pointing toward future East/Elevator connections.
- **Proposed Spatial Envelope (`PROPOSED`):** `X ∈ [-9.60, +21.40]`, `Z ∈ [+26.40, +33.80]`, `Y_floor = -46.400 m`, `Y_ceil = -43.550 m` (`2.850 m` clear height; raised to `Y_ceil = -42.900 m` locally over the Skylight Overlook bay if desired so the player has clear sightlines over the `Y = -44.64 m` skylight cap!).
- **Traversable Area (`ESTIMATED`):** `~132 m²` (`77 m²` North Corridor + `31 m²` Skylight Overlook + `24 m²` East Overhang Alcove).
- **Opening Impact on 07.15 Shell:** Modifies **ONLY `NWStair_UpperShell_WallEast`** (converting the existing `SealedEastFrame` recess into a real `2.04 m × 2.24 m` doorway). `WallNorth`, `WallSouth`, `WallWest`, `ConduitVertical`, and all guard parapets remain **100% untouched**.
- **Future Elevator Hook:** A sealed South-facing concrete anchor frame placed on the South wall of the corridor just East of the Lightwell (`X ∈ [+6.00, +8.40], Z = +26.40`) points straight South along `X ∈ [+5.50, +9.00]` around the East side of the Lightwell directly toward the future Upper Elevator Core waiting bay (`X ∈ [+3.41, +6.20], Z ∈ [+4.20, +9.40]`)!

### Alternative 2 — **West Service Spine & Sealed Upper Elevator Approach** *(PROPOSED)*
- **Concept:** Opens `NWStair_UpperShell_WallSouth` (`Z = +28.80, X ∈ [-11.40, -9.80]`, `1.60 m` clear width) directly southward into an upper maintenance/service corridor (`X ∈ [-12.00, -8.80], Z ∈ [+12.00, +28.80]`, `16.8 m × 3.2 m`) running above Nivel 0's Housekeeping Corridor, with a recessed East viewing pocket (`X ∈ [-8.80, -5.40], Z ∈ [+18.40, +25.60]`) looking across `NorthWing_Lightwell_WallW` (`X = -5.18`), before turning East along `Z ∈ [+12.00, +15.60]` to a sealed upper vestibule above the Lobby (`X ∈ [+3.41, +6.20], Z ∈ [+4.20, +12.00]`) fronting a **100% solid sealed concrete wall** directly above `Hotel_Elevator_Core`.
- **Proposed Spatial Envelope (`PROPOSED`):** `X ∈ [-12.00, +6.20]`, `Z ∈ [+4.20, +28.80]` (L-shaped, strictly West & South of the Courtyard Lightwell), `Y_floor = -46.400 m`, `Y_ceil = -43.550 m`.
- **Traversable Area (`ESTIMATED`):** `~205 m²`.
- **Trade-Offs:**
  - *Pros:* Establishes the spatial line to the upper Elevator Core immediately and keeps the North perimeter (`Z = +34.00`) untouched.
  - *Cons:* Forces an immediate 90° South turn off Flight 2 (ignoring the naturally aligned `SealedEastFrame` straight ahead!), requires reworking `NWStair_UpperShell_WallSouth`, `SouthGuardParapet` trim, and `ConduitVertical`, builds `~205 m²` of mostly linear service corridor leading to a *sealed* elevator wall before the creator has approved Elevator Configuration 1 vs. 2, and misses the poetic spatial echo over Room 111-B (`X > +16.00`).

### Alternative 3 — **Compact Skylight-Corner Loop (Hybrid East-to-South Circuit)** *(PROPOSED)*
- **Concept:** Opens **both** `NWStair_UpperShell_WallEast` (`X = -9.60, Z ∈ [+30.28, +32.32]`) AND `NWStair_UpperShell_WallSouth` (`Z = +28.80, X ∈ [-11.30, -9.90]`) to form a compact closed architectural loop (`~140 m²` `ESTIMATED`) wrapping tightly around the **NW corner of the Courtyard Lightwell**: East along `Z ∈ [+29.60, +32.80]` to `X = +6.00`, South along the Skylight Overlook (`X ∈ [-4.60, +4.60], Z ∈ [+26.40, +29.60]`), West into an unfinished service bay (`X ∈ [-9.60, -5.40], Z ∈ [+21.00, +28.80]`) overlooking `NorthWing_Lightwell_WallW`, and back North through `WallSouth` into the 07.15 Upper Landing.
- **Proposed Spatial Envelope (`PROPOSED`):** `X ∈ [-11.50, +6.00]`, `Z ∈ [+21.00, +33.80]` (strictly outside Lightwell `[-5.30..+5.30, +17.70..+26.30]`), `Y_floor = -46.400 m`, `Y_ceil = -43.550 m`.
- **Traversable Area (`ESTIMATED`):** `~140 m²`.
- **Trade-Offs:**
  - *Pros:* Gives the player a true short 360° micro-loop on the Upper Stratum without walking back through the exact same hallway; views the trapped Courtyard Lightwell from *two* perpendicular sides (North and West).
  - *Cons:* Modifies **two** walls of the 07.15 Upper Landing shell (`WallEast` AND `WallSouth`), alters `NWStair_UpperShell_ConduitVertical`, and lacks Alternative 1's East overhang contradiction above Room 111-B unless extended further East.

---

## 5. Spatial Dimensions, Geometric Feasibility & Comparative Evaluation *(PROPOSED)*

| Metric / Criterion | Alternative 1 — North Fragment & Sky-Roof Contradiction *(RECOMMENDED)* | Alternative 2 — West Service Spine & Elevator Approach | Alternative 3 — Compact Skylight-Corner Loop |
| :--- | :---: | :---: | :---: |
| **Upper Landing Wall(s) Opened** | **`WallEast` ONLY** (`SealedEastFrame` aligned straight off Flight 2) | `WallSouth` (90° turn off Flight 2; moves conduit) | `WallEast` + `WallSouth` (higher 07.15 shell churn) |
| **Clear Corridor Width (`PROPOSED`)** | **`3.00 m`** (`4.69×` player capsule `Ø 0.64 m`) | `3.20 m` (West) / `3.40 m` (South) | `2.80–3.00 m` |
| **Doorway Aperture (`PROPOSED`)** | **`2.04 m × 2.24 m`** (`3.19×` capsule width) | `1.60 m × 2.24 m` | `2.04 m` (E) + `1.40 m` (S) |
| **Floor / Ceiling Elevation (`PROPOSED`)** | `-46.400 m` / `-43.550 m` (`2.85 m` clear; `-42.90 m` at Overlook) | `-46.400 m` / `-43.550 m` (`2.85 m` clear) | `-46.400 m` / `-43.550 m` (`2.85 m` clear) |
| **Courtyard Lightwell Clearance** | **`Z ≥ +26.40` (`+0.10 m` clear of `SkylightCap` `Z=+26.30`)** | `X ≤ -5.40`, `Z ≤ +17.60` (clear) | `Z ≥ +26.40` & `X ≤ -5.40` (clear) |
| **Estimated Playable Area** | **`~132 m²`** (tight, intentional fragment) | `~205 m²` (longer linear corridor) | `~140 m²` (short closed circuit) |
| **Architectural Anomaly Impact** | **Highest** (Trapped Courtyard Roof + `+5.4 m` East Overhang over 111-B) | Moderate (West skylight view + sealed elevator stub) | High (dual-angle trapped skylight view, short loop) |
| **Implementation Complexity** | **Low–Moderate** (single East opening, clean orthogonal slabs) | Moderate–High (long L-run, wraps South Core) | Moderate (two shell cuts, corner wrap) |
| **Elevator Independence Preserved** | **100%** (clean future SE anchor at `X=+6..+8.4, Z=+26.4`) | 100% (reaches sealed wall over elevators prematurely) | 100% (clean future East/South anchors) |
| **Overall Rank** | **#1 — RECOMMENDED** | **#3 — Better as `07.17+` South Continuation** | **#2 — Strong Optional Short-Loop Variant** |

---

## 6. Proposed Route & Traversal Safety (Recommended Alternative 1) *(PROPOSED)*

1. **Top of NW Staircase (`VERIFIED EXISTING`):** Player climbs Flight 2 (`Z ∈ [+30.06, +31.82]`) onto `NWStair_UpperLanding_FloorSlab` (`Y = -46.400 m`, `1.99 m × 4.96 m`).
2. **Entry Portal (`PROPOSED`):** Directly ahead, the previously sealed `NWStair_UpperShell_SealedEastFrame` (`X = -9.60, Z ∈ [+30.28, +32.32]`, **`2.04 m` wide × `2.24 m` tall**) opens into the Upper North Corridor.
3. **Upper North Corridor (`PROPOSED`):** Level `3.00 m`-wide poured-concrete walkway (`X ∈ [-9.60, +16.00], Z ∈ [+29.80, +32.80]`, `2.85 m` headroom, `0` stairs/ramps, continuous solid floor slab `Y ∈ [-46.60, -46.40]`, `0.35 m` plenum above Rooms `110–107`).
4. **South Skylight Overlook (`PROPOSED`):** Centered at `X ∈ [-4.60, +4.60], Z ∈ [+26.40, +29.80]` (`3.40 m` clear depth). Bounded along `Z = +26.40` by a **solid `1.05 m`-tall concrete parapet + dark-steel top rail + invisible upper glass/collider barrier** (preventing any player jump/climb onto `NorthWing_Lightwell_SkylightCap` at `Y = -44.64 m`!). Ceiling locally steps up to `Y = -42.90 m` (`3.50 m` clear headroom) to frame the eerie trapped skylight diffuser clearly without visual clipping.
5. **East Overhang Alcove (`PROPOSED`):** Continuing past `X = +16.00` into `X ∈ [+16.00, +21.40], Z ∈ [+29.00, +33.40]` (`4.40 m` wide × `5.40 m` deep, `2.85 m` headroom), completely enclosed on North (`Z = +33.50`), East (`X = +21.50`), South (`Z = +28.90`), and ceiling (`Y = -43.55`). Contains **zero open drop-offs, zero voids, and zero exit teleports**.
6. **Safe Return Path (`VERIFIED + PROPOSED`):** The player walks cleanly back West along the `3.00 m`-wide Upper North Corridor through the `2.04 m` doorway onto the 07.15 Upper Landing, descends the Northwest Service Staircase (`Y = -46.40 → -48.20 → -50.00`), follows Nivel 0's North Guest Corridor East into **Room 111-B**, and returns to Avenida Benito Juárez exclusively via the **Threshold of Trust** (`"threshold-of-trust"`).

---

## 7. Architectural & Narrative Anomaly Concepts *(PROPOSED — ZERO HORROR CLICHÉS)*

All proposed anomalies grow strictly out of Remanencia's core rule (*memory, rumors, and contradictory building records become physical architecture*) — **zero** supernatural VFX, **zero** glowing portals, **zero** creatures, and **zero** Cobalt-60 biological explanations:

1. **Primary Anomaly — The Trapped Courtyard Skylight (*Impossible Interior Roof*):** Looking South from the Upper Overlook (`Z = +26.40`), the player sees the **actual glowing translucent skylight cap of Nivel 0's Central Courtyard (`Y = -44.64 m`) sitting *inside* the enclosed upper floor**, surrounded by concrete upper walls and ceiling overhead. The building remembers both an open-sky courtyard and an upper hotel story occupying the same vertical space, resolving the contradiction by burying the courtyard's roof indoors.
2. **Secondary Anomaly — The `+5.40 m` East Overhang (*Footprint Disagreement*):** Nivel 0's North Guest Corridor stops at `X = +16.00` (Rooms `110 → 109 → 108 → 107`) before the concealed Room 111-B dogleg. Directly above it, the Upper North Corridor passes `X = +16.00` with no wall at all, continuing smoothly to `X = +21.40` (echoing the 07.6 `MaintenanceRecord`'s corridor-length drift and the phantom wing of `UnfinishedProjectPlan`).
3. **Tertiary Subtle Anomaly — Mixed Construction Stages & Bricked-In Thresholds:** Along the North wall (`Z = +32.80`), two unfinished room-doorway bays align with Rooms `109` and `108` below — except one doorway is framed in older finished wood/plaster trim (echoing Room 111-B's anachronistic finish) and **bricked/poured solid from inside** (`zero` room interior behind it yet), while its neighboring bay remains raw cast concrete with exposed formwork tie-holes.

---

## 8. Twin Elevator Core Relationship & Strict Restrictions *(VERIFIED + PROPOSED)*

- **`VERIFIED EXISTING`:** `Hotel_Elevator_Core` (`GO=331299473`, `35` children, `X ∈ [+1.00, +3.41], Z ∈ [+4.20, +9.40]`, Bay 1 South `Z = +5.75`, Bay 2 North `Z = +7.85`) remains **100% protected and untouched**.
- **Future Spatial Alignment (`PROPOSED FOR 07.17+` — NOT AUTHORIZED IN 07.16):**
  - The Upper Elevator Core projection sits at `X ∈ [+1.00, +3.41], Z ∈ [+4.20, +9.40], Y = -46.400 m` (waiting bay `X ∈ [+3.41, +6.20]`), roughly `17 m` South of Alternative 1's corridor (`Z = +26.40`).
  - Placing a **sealed South concrete anchor frame** at `X ∈ [+6.00, +8.40], Z = +26.40` (just East of `NorthWing_Lightwell_WallE` at `X = +5.18`) guarantees a clean, straight South corridor path (`X ∈ [+5.50, +9.00], Z = +26.40 → +4.20`, directly above Nivel 0's East Arcade & Main Lobby ceiling) to meet the future Upper Elevator Waiting Bay in `07.17+` **without ever modifying `Hotel_Elevator_Core` during 07.16** and **without pre-deciding Configuration 1 vs. Configuration 2**!

---

## 9. Risk Assessment & Unresolved Creator Decisions *(UNRESOLVED)*

### Technical & Creative Risks
1. **Lighting Visibility Under the Strict `18`-Light Budget:** Emissive-only utility strips (`mats.UtilityWarmOn`) + flat ambient light work cleanly in compact shafts (verified in 07.15 renders), but a `25.6 m` upper corridor could feel dim without flashlight `[F]`. *Mitigation:* Space restrained emissive ceiling strips every `~7–8 m` + leverage the bright `LightwellDiffuser` glow visible at the South Skylight Overlook; evaluate in PlayMode whether 07.16 should keep `18` lights or request creator approval for `1–2` non-shadow fill lights.
2. **Skylight Overlook Anti-Climb Collision:** Because `NorthWing_Lightwell_SkylightCap` sits at `Y = -44.64 m` (`+1.76 m` above floor) with a solid `BoxCollider`, a solid full-height invisible viewing barrier above the `1.05 m` concrete parapet along `Z = +26.40` is mandatory so the player cannot wedge or jump onto the skylight cap.
3. **Targeted Test Update Required in 07.16 Implementation:** `RemanenciaNorthwestStaircaseTests.cs` currently asserts `NWStair_UpperShell_WallEast` is solid at `Z = 31.30`. Opening the East doorway in 07.16 must split `NWStair_UpperShell_WallEast` cleanly around `Z ∈ [+30.28, +32.32]` and update that single assertion to verify doorway traversal while retaining solid North/South/West shell checks.

### Unresolved Creator Decisions (`UNRESOLVED` — Required Before Coding 07.16)
1. **Select Layout:** Confirm **Alternative 1 (North Fragment + Trapped-Skylight Overlook + East Overhang)** vs. **Alternative 3 (Compact Skylight-Corner Loop)** vs. **Alternative 2 (West Service Spine)**.
2. **Lighting Policy:** Confirm keeping exact **`18` Unity `Light` components** (emissive strips + ambient only, matching 07.5/07.11/07.12/07.15) vs. authorizing `1–2` non-shadow point lights on the Upper Stratum.
3. **Upper Stratum Naming & Room Numbering:** Confirm whether sealed upper doorway bays should remain **unnumbered** in 07.16 (recommended for restraint) or introduce provisional `200`-series / anomalous numbering.
4. **Future Elevator Core Readiness:** Confirm deferring Bay 1 vs. Bay 2 elevator assignment to `07.17+` once the Upper Stratum's tone is accepted in PlayMode.

---

## 10. Recommended Alternative Summary (Section 9 Checklist) *(RECOMMENDED OPTION)*

1. **Immediate Sightline Beyond the Opening:** Stepping through `NWStair_UpperShell_SealedEastFrame` (`X = -9.60, Z ∈ [+30.28, +32.32]`), the player looks straight down a quiet `3.00 m`-wide unfinished concrete upper corridor illuminated by a soft cool-overcast glow spilling out from the South Skylight Overlook bay midway down the hall.
2. **First Traversable Space:** The `25.6 m × 3.0 m` Upper North Corridor (`X ∈ [-9.60, +16.00]`) with its centered `9.2 m × 3.4 m` South Skylight Overlook (`X ∈ [-4.60, +4.60], Z ∈ [+26.40, +29.80]`).
3. **Principal Architectural Anomaly:** The **Trapped Courtyard Skylight** (`NorthWing_Lightwell_SkylightCap`, `Y = -44.64 m`) protruding indoors through the Upper Stratum floor, paired with the **`+5.40 m` East Overhang** continuing beyond Nivel 0's North Wing perimeter (`X = +16.00 → +21.40`) over the hidden Room 111-B void.
4. **Loop vs. Same-Route Return:** Clean linear-plus-overlook exploration returning along the wide `3.00 m` corridor back to the NW Staircase (`0` narrow choke points; keeps 07.15's `WallSouth` untouched). *(If the creator strongly prefers a closed circuit in 07.16, Alternative 3 provides the short Skylight-Corner Loop.)*
5. **Future Relation to Upper Elevator Landing:** A sealed concrete anchor frame at `X ∈ [+6.00, +8.40], Z = +26.40` aligns directly with a future South-East corridor (`07.17+`) skirting East of the Courtyard Lightwell straight to the Upper Elevator Waiting Bay (`X ∈ [+3.41, +6.20], Z ∈ [+4.20, +9.40]`).
6. **Explicitly Inaccessible in 07.16:** All upper guest-room interiors (doorways framed & sealed solid), the Courtyard Skylight roof surface itself (guarded parapet + full-height viewing barrier), the South-East corridor toward the Elevator Core (sealed anchor frame), and any third-floor / roof continuation.
7. **Logical Next Increment (`07.17`):** Either extend South from the SE anchor frame (`Z = +26.40 → +4.20`) to establish the Upper Elevator Landing shell (paired with creator-approved Elevator Configuration 1 vs. 2), OR open one contrasting anomalous room off the Upper North Corridor.

---

## 11. Suggested Implementation Phases (`07.16` and Subsequent Increments) *(PROPOSED)*

| Increment | Scope | Key Guardrails |
| :---: | :--- | :--- |
| **07.16-impl** | **Upper Stratum Fragment (Alternative 1)** via dedicated modular `UpperStratumBuilder.cs` under sibling root `Backrooms_Architecture/Hotel_UpperStratum_M1`. Opens `NWStair_UpperShell_SealedEastFrame`, builds Upper North Corridor, guarded South Skylight Overlook, East Overhang Alcove, sealed SE Elevator-approach anchor frame, and dedicated EditMode tests (including `CharacterController.Move()` traversal). | Zero edits to Elevators, Rooms 107–110, Room 111-B, Threshold of Trust, entry-only door, `BorderCorridorScene.unity`, or save schema; `2×` idempotent migration. |
| **07.17** | **Upper South-East Link & Upper Elevator Core Vestibule** extending South from `Z = +26.40` (East of Lightwell) to `X ∈ [+3.41, +6.20], Z ∈ [+4.20, +9.40]` + optional creator-approved Elevator Configuration visual differentiation. | Requires explicit creator approval of Elevator Configuration 1 vs. 2. |
| **07.18+** | **Functional Passenger Elevator & Deeper Remanencia Strata / Environmental Narrative Layer 2** (independent progression & ecology review). | Separate design approval required. |

---

## 12. Explicit Protected Areas List *(MANDATORY INVARIANTS)*

The following remain **100% protected and unmodified** in 07.16 design (and, except for the single authorized `NWStair_UpperShell_WallEast` doorway split during future 07.16 implementation, protected across future builds):

- Complete 07.15 Northwest Service Staircase (`Flight 1`, `Intermediate Landing`, `Flight 2`, `Guardrails & Handrails`, `UpperLanding_FloorSlab`, `WallNorth`, `WallSouth`, `WallWest`, `CeilingSlab`, `NorthwestServiceStaircaseBuilder.cs`, `RemanenciaNorthwestStaircaseTests.cs`).
- `Hotel_Elevator_Core` (`GO=331299473`, `35` children, Bay 1 South & Bay 2 North, `Backrooms_Floor`, `Backrooms_Ceiling`).
- `NorthWing_Courtyard` & `NorthWing_Lightwell_*` (all 4 lightwell walls + `SkylightCap` remain solid and structurally intact).
- Rooms `101–110` and Room 110 ecological Zone D (`Eco_Room110_ChaseRiser_*`).
- Room 111-B (`Hotel_Room111B_Anomaly`, `Hotel_Room111B_GuestInterior`), concealed East Wing passage, and **Threshold of Trust** (`Room111B_Threshold_TriggerVolume`, sole authorized in-world exit `"threshold-of-trust"`).
- All 07.6 narrative clues, 07.7 early ecology, 07.8 faunal traces, 07.9 living microfauna, entry-only hotel door (`Backrooms_ExitDoor_ToStreet`, silent proximity prompt), both surface entry routes, save schema (`schemaVersion == 1`), journal, and `BorderCorridorScene.unity` (`SHA-256: 0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B`).


---

## 13. Implementation 07.17 — As-Built North Upper Gallery *(IMPLEMENTED IN 07.17)*

Following creator approval of **Alternative 1**, Implementation 07.17 physically constructed the first controlled fragment — the **North Upper Gallery** (`BackroomsLevel1_Root/Backrooms_Architecture/Hotel_NorthUpperGallery`) — via the dedicated, idempotent Editor builder `NorthUpperGalleryBuilder.MigrateNorthUpperGalleryAndSave()`, while explicitly deferring the Courtyard Lightwell Overlook, East Overhang, room numbering/signage, and Twin Elevator Core.

### A. Final As-Built Coordinates & Measurements *(VERIFIED IN 07.17)*

| Element | As-Built Coordinates / Dimensions (`VERIFIED`) | Status / Notes |
| :--- | :--- | :--- |
| **Authorized East Opening** | `X = -9.600`, `Z ∈ [+30.280, +32.320]` (`2.040 m` clear width, centered `Z = +31.300`), `Y ∈ [-46.400, -44.160]` (`2.240 m` clear height) | Preserves `NWStair_UpperShell_WallEast` (`GO=2109824158`) in-place as South pier (`Z ∈ [+28.67, +30.28]`) and retains `SealedEastFrameS/N/Top` (`GO=1635426459 / 1149530410 / 939874756`) verbatim. |
| **Opening-to-Gallery Widening** | Symmetrical `0.48 m` structural concrete reveal shoulders (`NUGallery_Portal_EastPilasterS/N` + overhead lintel + flush threshold plate) | Smooth, credible hotel corridor widening from `2.04 m` portal to `3.00 m` gallery with zero collision snagging. |
| **Gallery Walkable Interior** | `X ∈ [-9.490, +15.870]` (`25.360 m` clear length), `Z ∈ [+29.800, +32.800]` (**`3.000 m` clear width**, centered `Z = +31.300`) | Continuous level concrete floor at `Y = -46.400 m` (`0.000 m` lip from 07.15 Upper Landing). |
| **Floor & Ceiling Slabs** | `NUGallery_FloorSlab` (`Y ∈ [-46.600, -46.400]`, `0.20 m` thick) & `NUGallery_CeilingSlab` (`Y ∈ [-43.550, -43.350]`, `0.20 m` thick) | `2.850 m` clear headroom throughout gallery (`+0.348 m` clean plenum above `NorthWing_CeilingSlab_North` `Y_top = -46.948 m`). |
| **North & South Walls** | `NUGallery_WallNorth` (`Z = +32.910`, `t = 0.22 m`) & `NUGallery_WallSouth` (`Z = +29.690`, `t = 0.22 m`, outer face `Z = +29.580`) | **`+3.280 m` clear North** of protected `NorthWing_Lightwell_SkylightCap` (`Z_max = +26.300`); zero overlap with Lightwell. |
| **Sealed Eastern Terminal** | `NUGallery_TerminalWall_East` (`X = +16.000`, `t = 0.26 m`, inner face `X = +15.870`, `BoxCollider=True`) + `NUGallery_Terminal_SealedFrameS/N/Top` + terminated conduit drop | Aligned strictly with `NorthWing_OuterWall_East` (`X = +16.000`); zero passage, zero East Overhang. |
| **Architectural Details & Lighting** | 5 structural pilaster/beam bays, 2 unnumbered sealed North wall recesses (raw concrete & weathered plaster contrast), 1 sealed South structural relief, off-axis floor expansion seam, overhead conduit, and **4 emissive-only utility strips (`mats.UtilityWarmOn`)** | **Zero** added Unity `Light` components (exact `18` scene lights, `LightShadows.None`); zero room numbers/signage; zero runtime scripts. |

### B. Explicitly Deferred Features & Remaining Dependencies *(DEFERRED TO 07.18+)*
- **Courtyard Lightwell Overlook (`DEFERRED`):** `NorthWing_Lightwell_WallN/S/E/W` and `NorthWing_Lightwell_SkylightCap` remain 100% untouched behind solid `NUGallery_WallSouth` (`+3.28 m` clearance).
- **East Overhang (`DEFERRED`):** Gallery stops solidly at `X = +16.000` (`NUGallery_TerminalWall_East`).
- **Twin Elevator Core (`DEFERRED`):** `Hotel_Elevator_Core` (`35` children) remains 100% untouched.
- **PlayMode Lighting Check (`PENDING MANUAL REVIEW`):** Emissive-only utility strips maintain the strict `18`-light budget; live PlayMode review should confirm corridor readability with and without flashlight `[F]`.
