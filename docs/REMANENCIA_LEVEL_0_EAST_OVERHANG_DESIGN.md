# REMANENCIA — Nivel 0: East Overhang — Forensic Spatial Audit & Architectural Design (07.20)

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia** *(overarching dimensional universe — proper name, untranslated in ES/EN)*  
**Territory:** **Nivel 0 — Hotel de las Palomas** (`unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`)  
**Baseline Audited:** Branch `product/unity-fellowship-2027`, HEAD `6ab30c9557f81a21b029a54873decf664691e75d` (`6ab30c9 feat(remanencia): add courtyard lightwell overlook`; clean working tree synchronized with `origin/product/unity-fellowship-2027`)  
**Document Status:** **IMPLEMENTATION 07.20 — READ-ONLY FORENSIC SPATIAL AUDIT & ARCHITECTURAL DESIGN (ZERO UNITY SCENE, BUILDER, SCRIPT, MATERIAL, TEST, OR EXISTING-DOC MODIFICATIONS)**

> **Mandatory Measurement & Status Labels:**  
> Every coordinate, dimension, area, and architectural proposal in this document is explicitly distinguished as:
> - **`VERIFIED EXISTING`** — Directly extracted from serialized transforms, rotation-aware mesh/collider bounds, and `fileID`s in `BackroomsLevel1Scene.unity` (`SHA-256: 565B9CE20A3E9511DF7924D1CB1C98F2EC251FC052EE472DA9DAD8F5CF3E2089`, `1,458` GameObjects, exact `18` `Light` components) and `BorderCorridorScene.unity` (`SHA-256: 0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B`) at `6ab30c9`.
> - **`PROPOSED`** — Concrete geometric target evaluated or recommended for future authorized `07.21+` construction; **not built**.
> - **`ESTIMATED`** — Approximate surface area, fog visibility, or construction complexity pending prototyping.
> - **`UNRESOLVED`** — Open architectural, sightline, or framing decisions requiring explicit creator approval before any Unity implementation begins.

---

## 1. Executive Design Intent *(VERIFIED + PROPOSED)*

Implementations **07.15** (Northwest Service Staircase), **07.17** (North Upper Gallery), and **07.19** (Courtyard Lightwell Overlook) established the first physically traversable upper hotel route at `Y = -46.400 m` and proved that Remanencia's strongest moments arise from **grounded, restrained architectural contradictions** rather than large empty floors or horror clichés.

The next proposed feature under the original Alternative 1 direction is the **East Overhang**, originally sketched in 07.16 as an extension of approximately `+5.40 m` East past `NUGallery_TerminalWall_East` (`X = +16.000 m`) and informally described as extending *"directly above the hidden Room 111-B pocket below."*

This read-only forensic investigation audited every serialized object with `X_max ≥ +15.50 m` and `Z_max ≥ +12.00 m` across all elevations (`Y ∈ [-50.30, -43.35]`) using rotation-aware world AABBs. The audit established **three decisive geometric findings**:

1. **The Room 111-B Vertical Overlap Claim Is Mathematically DISPROVEN (`CRITICAL — VERIFIED EXISTING`):** Continuing the North Upper Gallery (`Z ∈ [+29.800, +32.800]`) straight East across `X ∈ [+16.130, +21.400]` has **`0.000 m` horizontal Z overlap with Room 111-B** (`Z ∈ [+19.700, +25.100]` — separated by **`+4.700 m` North–South**) AND **`0.000 m` horizontal Z overlap with `Room111B_EastCorridor`** (`Z ∈ [+19.900, +29.000]` — separated by **`+0.800 m` North–South**). Beyond a `0.170 m` lower slab edge lip (`X ∈ [+16.130, +16.300]`), the direct East extension projects strictly over **`100%` pure unbuilt exterior void** (`X > +16.300, Z ∈ [+29.800, +32.800]`) located East of Guest Room 107 and North of Room 111-B.
2. **Existing Structural & Portal Readiness at `X = +16.000` (`VERIFIED EXISTING`):** `NUGallery_TerminalWall_East` (`X ∈ [+15.870, +16.130]`) ALREADY sits collinearly above Nivel 0's solid exterior perimeter wall `Room111B_OuterWallEast_NorthStub` (`X ∈ [+15.870, +16.130], Z ∈ [+28.400, +34.065]`) and ALREADY carries a pre-framed `2.040 m × 2.240 m` continuation relief (`NUGallery_Terminal_SealedFrameS/N/Top`, `Z ∈ [+30.280, +32.320]`) — clear of `ConduitDrop`/`JunctionBox` (`Z ∈ [+32.55, +32.69]`) and `1.800 m` East of structural frame `Pilaster_4` / `CeilingBeam_4` (`X = +14.200`).
3. **Recommended Architectural Direction (`RECOMMENDED — PROPOSED`):** Rather than building an implausibly thin `5.40 m` slab or a sprawling `21–36 m²` L-corridor to force overlap over Room 111-B, **Alternative C — Discontinuous East Fragment** (`12.69 m²` total walkable area: a short `1.200 m × 2.040 m` perimeter threshold neck crossing `X = +16.000`, marked by a recessed structural separation seam, opening into a compact `3.200 m × 3.200 m` displaced weathered-plaster/concrete fragment at `X ∈ [+17.330, +20.530], Z ∈ [+29.400, +32.600]` with laminated safety-glass observation bays) turns the verified exterior void into an unmistakable architectural discovery — leaving Room 111-B, Threshold of Trust, Lightwell, Overlook, and Twin Elevator Core **100% untouched**.

---

## 2. Verified Existing East Boundary Geometry *(VERIFIED EXISTING)*

Extracted directly from `BackroomsLevel1Scene.unity` (`SHA-256: 565B9CE20A3E9511DF7924D1CB1C98F2EC251FC052EE472DA9DAD8F5CF3E2089`) and `NorthUpperGalleryBuilder.cs`:

### A. North Upper Gallery East Terminal (`Hotel_NorthUpperGallery`, `X_max ≥ +12.00 m`)

| Serialized GameObject | `fileID` (GO / TF) | World Pos `(X, Y, Z)` | Scale `(X, Y, Z)` | Verified World AABB `(X / Y / Z)` | Collider | Role (`VERIFIED EXISTING`) |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| `NUGallery_TerminalWall_East` | `1036244486` / `1036244487` | `(16.000, -45.075, 31.300)` | `(0.260, 3.050, 3.440)` | `X ∈ [+15.870, +16.130]`<br>`Y ∈ [-46.600, -43.550]`<br>`Z ∈ [+29.580, +33.020]` | **Solid** | Sealed East perimeter terminal aligned directly above Nivel 0's `X = +16.000` outer East wall. |
| `NUGallery_Terminal_SealedFrameS` | `228660284` / `228660285` | `(15.835, -45.220, 30.200)` | `(0.070, 2.360, 0.160)` | `X ∈ [+15.800, +15.870]`, `Y ∈ [-46.400, -44.040]`, `Z ∈ [+30.120, +30.280]` | None | South jamb of pre-framed `2.040 m × 2.240 m` continuation aperture (`Z_inner = +30.280`). |
| `NUGallery_Terminal_SealedFrameN` | `1816810334` / `1816810335` | `(15.835, -45.220, 32.400)` | `(0.070, 2.360, 0.160)` | `X ∈ [+15.800, +15.870]`, `Y ∈ [-46.400, -44.040]`, `Z ∈ [+32.320, +32.480]` | None | North jamb of pre-framed continuation aperture (`Z_inner = +32.320`). |
| `NUGallery_Terminal_SealedFrameTop` | `17293860` / `17293861` | `(15.835, -44.080, 31.300)` | `(0.070, 0.160, 2.360)` | `X ∈ [+15.800, +15.870]`, `Y ∈ [-44.160, -44.000]`, `Z ∈ [+30.120, +32.480]` | None | Header of pre-framed continuation aperture (`Y_bottom = -44.160`, `2.240 m` clear height). |
| `NUGallery_Terminal_ConduitDrop` & `JunctionBox` | `1574593494` / `433002667` | `(15.83 / 15.82, -44.55 / -44.85, 32.62)` | Conduit `2.00 m`; Box `0.18×0.14 m` | `X ∈ [+15.785, +15.855]`, `Z ∈ [+32.550, +32.690]` | None | NE corner conduit stub — strictly North of `SealedFrameN` (`+0.07 m` clear; untouched if opening stays inside `Z ∈ [+30.28, +32.32]`). |
| `NUGallery_FloorSlab` | `906864132` / `906864133` | `(3.210, -46.500, 31.300)` | `(25.840, 0.200, 3.440)` | `X ∈ [-9.710, +16.130]`, `Y ∈ [-46.600, -46.400]`, `Z ∈ [+29.580, +33.020]` | **Solid** | Gallery walkable floor top `Y = -46.400 m`, ends flush at outer face `X = +16.130`. |
| `NUGallery_CeilingSlab` | `1775212749` / `1775212750` | `(3.210, -43.450, 31.300)` | `(25.840, 0.200, 3.440)` | `X ∈ [-9.710, +16.130]`, `Y ∈ [-43.550, -43.350]`, `Z ∈ [+29.580, +33.020]` | **Solid** | Gallery ceiling soffit `Y = -43.550 m` (`2.850 m` clear), ends flush at `X = +16.130`. |
| `NUGallery_WallNorth` / `WallSouth` | `1513573749` / `997392960` | North `Z=32.91`; South `Z=29.69` | `t = 0.220 m`, `h = 3.050 m` | North `Z ∈ [+32.80, +33.02]`; South `X ∈ [+0.06, +16.13], Z ∈ [+29.58, +29.80]` | **Solid** | Gallery enclosing walls terminating cleanly into `NUGallery_TerminalWall_East`. |
| `NUGallery_PilasterNorth_4` / `PilasterSouth_4` / `CeilingBeam_4` | `1453208415` / `568485978` / `1833089846` | `X = +14.200` | `0.280 m` wide | `X ∈ [+14.060, +14.340]`, `Z ∈ [+29.800, +32.800]` | None | Easternmost interior concrete portal frame (`1.800 m` West of `X = +16.000`). |
| `NUGallery_StripTube_3` | `111695598` | `(11.800, -43.605, 31.300)` | `(1.320, 0.035, 0.065)` | `X ∈ [+11.140, +12.460]`, `Z ∈ [+31.268, +31.332]` | None | Easternmost emissive utility strip (`4.07 m` West of terminal). |

### B. Existing Lower-Level Perimeter & Structures East of `X = +15.870` (`VERIFIED EXISTING`)

| Serialized Structure | `fileID` (GO) | Verified Rotation-Aware World AABB `(X / Y / Z)` | Collider | Spatial Role Relative to `X = +16.000` |
| :--- | :---: | :--- | :---: | :--- |
| `Room111B_OuterWallEast_NorthStub` | `1752685893` | `X ∈ [+15.870, +16.130]`, `Y ∈ [-50.000, -47.150]`, `Z ∈ [+28.400, +34.065]` | **Solid** | Nivel 0 East outer wall of Guest Room 107 — sits **directly beneath `NUGallery_TerminalWall_East`**! |
| `NorthWing_OuterWall_East` | `191771817` | `X ∈ [+15.870, +16.130]`, `Y ∈ [-50.000, -47.150]`, `Z ∈ [+11.935, +26.800]` | **Solid** | Nivel 0 East outer wall of Banquet Hall (South of Room 111-B concealed opening `Z ∈ [+26.80, +28.40]`). |
| `NorthWing_CeilingSlab_North` | `582163591` | `X ∈ [-9.600, +16.300]`, `Y ∈ [-47.148, -46.948]`, `Z ∈ [+26.000, +34.300]` | **Solid** | Nivel 0 ceiling over Rooms 110–107; overhangs `0.170 m` East of `X = +16.130` (`+0.348 m` below `NUGallery_FloorSlab`). |
| `NorthWing_FloorSlab` | `92874449` | `X ∈ [-16.300, +16.300]`, `Y ∈ [-50.301, -50.001]`, `Z ∈ [+12.200, +34.300]` | **Solid** | Nivel 0 ground floor slab; ends at `X = +16.300`. |
| `Room111B_EastCorr_CeilingSlab` / `FloorSlab` | `72975173` / `1684316752` | `X ∈ [+16.250, +19.200]`, `Y_ceil ∈ [-47.148, -46.947]`, `Y_flr ∈ [-50.301, -50.002]`, **`Z ∈ [+19.900, +29.000]`** | **Solid** | Concealed Room 111-B East Corridor slabs — **stop cleanly at `Z = +29.000 m`** (`0.800 m` South of gallery interior `Z = +29.800`). |
| `Room111B_Opening_NorthSeal` & `ConcealedBaffle_Wall` | `2131944295` / `79101902` | NorthSeal `X ∈ [+15.94, +17.61], Z ∈ [+28.68, +28.92]`; Baffle `X ∈ [+17.43, +17.67], Z ∈ [+25.34, +28.86]`; `Y ∈ [-50.00, -47.15]` | **Solid** | Nivel 0 concealed dogleg entry to Room 111-B — strictly South of `Z = +28.920 m`. |
| `Room111B_Shell_CeilingSlab` / `FloorSlab` | `2088030768` / `461275875` | **`X ∈ [+19.150, +24.850]`**, `Y_ceil ∈ [-47.148, -46.948]`, `Y_flr ∈ [-50.301, -50.001]`, **`Z ∈ [+19.700, +25.100]`** | **Solid** | **Room 111-B structural shell** (`WallNorth` outer face `Z = +25.060`, `WallEast` outer face `X = +24.780`). |
| `Room111B_Threshold_TriggerVolume` | `16005720` | `X ∈ [+23.760, +24.760]`, `Y ∈ [-49.350, -48.350]`, `Z ∈ [+21.900, +22.900]` | Trigger | Sole authorized in-world exit from Remanencia — `6.90+ m` South and `3.20+ m` East of recommended fragment! |

---

## 3. Exact 3D Relationship to Room 111-B *(DISPROVEN DIRECT OVERLAP — VERIFIED)*

Directly comparing the complete 3D projected footprints of the original 07.16 East Overhang (`X ∈ [+16.000, +21.400]`, continuing gallery `Z ∈ [+29.800, +32.800]`) against Room 111-B (`Hotel_Room111B_Anomaly`) proves:

| Comparison Metric | Direct East Extension (`X ∈ [+16.13, +21.40], Z ∈ [+29.80, +32.80]`) vs. **Room 111-B Shell** (`X ∈ [+19.15, +24.85], Z ∈ [+19.70, +25.10]`) | Direct East Extension vs. **Room 111-B East Corridor** (`X ∈ [+16.25, +19.20], Z ∈ [+19.90, +29.00]`) |
| :--- | :---: | :---: |
| **Horizontal X Overlap** | `2.250 m` (`X ∈ [+19.150, +21.400]`) | `2.950 m` (`X ∈ [+16.250, +19.200]`) |
| **Horizontal Z Overlap** | **`0.000 m` (ZERO OVERLAP)** | **`0.000 m` (ZERO OVERLAP)** |
| **Horizontal Z Separation Gap** | **`+4.700 m` North–South** (`29.800 - 25.100`; `+4.480 m` from outer `WallSouth` `29.580`) | **`+0.800 m` North–South** (`29.800 - 29.000`; `+0.580 m` from outer `WallSouth` `29.580`) |
| **Vertical Separation (`Y_upper_floor` to `Y_lower_ceil_top`)** | `+0.548 m` (`-46.400` vs `-46.948`; slab plenum `+0.348 m`) | `+0.547 m` (`-46.400` vs `-46.947`; slab plenum `+0.347 m`) |
| **Vertical Separation (`Y_upper_floor` to `Y_lower_floor`)** | `+3.600 m` (`-46.400` vs `-50.000`) | `+3.600 m` (`-46.400` vs `-50.000`) |
| **Vertical Projection Verdict** | **FALSE — NOT ABOVE ROOM 111-B** | **FALSE — NOT ABOVE EAST CORRIDOR** |

### Exact Plan-View Spatial Map East of `X = +12.00 m` (`VERIFIED EXISTING`)

```text
Z=+34.0  +--------------------+
         |   GUEST ROOM 107   |  <-- LOWER LEVEL (Y = -50.00..-47.15) stops at X=+16.00 (slab lip +16.30)
Z=+32.8  +====+===============+====================+
         |    | N. UPPER GAL. | [DIRECT EAST VOID] |  <-- UPPER STRATUM (Y = -46.40..-43.55)
Z=+29.8  +====+===============+====================+      100% PURE UNBUILT VOID below X > +16.30!
Z=+29.0  +----+---[111B Open]-+---------+                 (0.80m North of EastCorr; 4.70m North of 111-B!)
         |    | North Corrid. | 111-B   |
Z=+26.0  +----+---------------+ EAST    |  [VOID NORTH OF 111-B]
         |    |               | CORRID. |  X ∈ [+19.20, +24.85], Z ∈ [+25.10, +34.00]
Z=+25.1  |    |   BANQUET     |         +------------------+
         |    |    HALL       | [111-B] |    ROOM 111-B    |  <-- Lower Level ONLY (Y = -50.00..-47.15)
Z=+20.0  |    |               +---------+   (Threshold @ E)|      X ∈ [+19.12, +24.52], Z ∈ [+20.00, +24.80]
         +----+---------------+---------+------------------+
        X=+12               X=+16     X=+19.1            X=+24.8
```

### Critical Forensic Takeaway — What Actually Lies Beneath Candidate Extensions
1. **Directly beneath `X ∈ [+16.130, +21.400], Z ∈ [+29.800, +32.800]`:** Only a `0.170 m` edge lip of `NorthWing_CeilingSlab_North` / `NorthWing_FloorSlab` (`X ∈ [+16.13, +16.30]`), followed by **100% unbuilt exterior void** (`X > +16.30`).
2. **To actually stand above `Room111B_EastCorridor`:** An extension must shift South across `Z ∈ [+25.40, +29.00]` (`X ∈ [+16.25, +19.20]`).
3. **To actually stand above `Room111B_Shell`:** An extension must turn South more than `5–8 m` into `Z ∈ [+20.00, +24.80]` (`X ∈ [+19.15, +24.85]`) — creating a large `32–36 m²` secondary East wing.
4. **Unexpected Architectural Opportunity (`VERIFIED RAY MATH`):** Even WITHOUT shifting over Room 111-B, standing inside a compact extension at `(X = +18.80, Z = +29.72, Y_eye = -44.760)` and looking **South-Down (`13°–31°` downward pitch, clearing a `1.05 m` parapet at `48.6°`!)** looks directly across the exterior void onto the rooftops of **`Room111B_EastCorr_CeilingSlab` (`5.32 m` away, `97.8%` fog clarity)** and **`Room111B_Shell_CeilingSlab` (`5.43–8.28 m` away, `94.8%–97.7%` fog clarity)** protruding beyond `NorthWing_OuterWall_East` below! *(Note: whether to expose this downward glimpse or keep the South face solid concrete is flagged in Section 13 as an explicit unresolved visual review decision because those slabs currently carry ceiling-grid textures on their outer top faces.)*

---

## 4. Structural Feasibility Analysis *(VERIFIED + ARCHITECTURAL EVALUATION)*

*Note: This evaluation addresses architectural plausibility, visible load paths, and Unity collision safety within the scene — it does not claim civil-engineering structural capacity certification.*

### Existing Support Conditions at `X = +16.000` (`VERIFIED EXISTING`)
- **Primary Vertical Bearing Line (`X ∈ [+15.870, +16.130]`):** `Room111B_OuterWallEast_NorthStub` (`Y ∈ [-50.000, -47.150], Z ∈ [+28.400, +34.065]`) transfers load continuously from `NUGallery_TerminalWall_East` (`Y ∈ [-46.600, -43.550]`) straight down to `NorthWing_FloorSlab` (`Y = -50.000`).
- **Back-Span Structural Frame (`X = +14.200`):** `NUGallery_PilasterNorth_4`, `PilasterSouth_4`, and `CeilingBeam_4` form a rigid concrete frame `1.800 m` West of the terminal wall.
- **Unoccupied Void Below East Projection (`X > +16.300, Z ∈ [+29.800, +32.800]`):** Zero lower slabs, walls, pipes, rooms, or colliders exist down to `Y = -50.30 m` and below.

### Evaluation of Three Structural Approaches (`PROPOSED`)

| Approach | Required Visible Geometry & Construction Logic | Collision Behavior | Narrative Implications | Verdict |
| :--- | :--- | :--- | :--- | :---: |
| **A. Concrete Cantilever** | Floor slab (`0.20 m`) stiffened by twin haunched cast-concrete downstand edge girders (`0.45 m` deep, `Y ∈ [-47.05, -46.60]`) anchored into `X = +16.00` perimeter wall (`Room111B_OuterWallEast_NorthStub`) and visually tied back to `Pilaster_4` (`X = +14.20`). **Span discipline:** Original `5.40 m` on a `0.20 m` slab (`27:1`) reads implausibly thin; reducing projection to **`3.80 m`** over `0.65 m` combined slab+girder depth (`~5.8:1`) reads as heavy, authentic unfinished Mexican concrete construction! | Solid monolithic slab + enclosed walls/glazed terminal; zero void drop-offs. | Grounded, believable unfinished hotel projection continuing past its lower exterior wall ("the second floor didn't stop where the first floor stopped"). | **Plausible (at `≤ 3.8–4.2 m` span)** |
| **B. Independent Concrete Support** | Cast-concrete columns descending from `Y = -46.60` down to `Y = -50.30` at `X ≈ +19.8, Z = +29.8 / +32.8` inside the unoccupied NE exterior void (clear of Room 111-B by `+4.70 m` and East Corridor by `+0.80 m`), carrying a longitudinal transfer beam. | Columns strictly outside playable Nivel 0 (zero interference with Room 107 or 111-B), solid floor slab above. | **Weak architectural payoff:** Columns are 100% invisible from Nivel 0 (sealed behind Room 107's East wall), invisible from inside an enclosed upper room, and rest on unbuilt exterior void with no ground plane at `Y = -50.00` — looking like floating stilts if glimpsed! | **Not Recommended** |
| **C. Deliberately Discontinuous Structure** | Compact concrete threshold neck (`1.20 m`) crossing `X = +16.00` with a recessed dark-steel **structural separation/seismic joint seam** (walls, soffit, flush floor plate — zero physical step or gap!), opening into a compact displaced concrete/plaster fragment (`3.20 m × 3.20 m`) whose slab edge, truncated exterior beam stubs, and shifted centerline (`-0.30 m` South, `+0.30 m` taller ceiling) visibly refuse to line up with the hotel below. | **100% continuous, level, solid `BoxCollider` walking surface** (`Y = -46.400 m`) with full perimeter containment (solid walls + `1.05 m` parapets + framed transparent laminated safety glass panels with aligned solid colliders). Zero gaps, zero jumps, zero fall risk! | **Strongest Remanencia identity:** Communicates directly through architecture that an alien/older hotel fragment grafted itself onto the East boundary beyond the building's real footprint. | **RECOMMENDED** |

---

## 5. Evaluation of Three Spatial Alternatives *(PROPOSED)*

All coordinates, dimensions, and areas below are explicitly **`PROPOSED`** and **`ESTIMATED`**; none are implemented. Every alternative preserves `NUGallery_Terminal_SealedFrameS/N/Top` (`Z ∈ [+30.28, +32.32]`, `2.04 m × 2.24 m`), `ConduitDrop`/`JunctionBox`, `Pilaster_4`/`CeilingBeam_4`, the entire 07.19 Courtyard Lightwell Overlook, Lightwell, Room 111-B, Threshold of Trust, Twin Elevator Core, and exact `18`-light budget.

### Alternative A — Direct East Cantilever *(Compact Straight Continuation)*
- **Proposed Bounds (`PROPOSED`):**
  - Opening through `NUGallery_TerminalWall_East`: `X ∈ [+15.870, +16.130]`, `Z ∈ [+30.280, +32.320]` (`2.040 m × 2.240 m`).
  - Walkable interior: `X ∈ [+16.130, +19.930]` (`3.800 m` clear cantilever — tightened from `5.40 m` for credible structural proportions!), `Z ∈ [+30.000, +32.600]` (`2.600 m` clear width, centered `Z = +31.300`).
  - Outer shell: `X ∈ [+16.130, +20.150]`, `Z ∈ [+29.780, +32.820]`, Floor `Y ∈ [-46.600, -46.400]` (`0.20 m` slab + twin `0.45 m` concrete haunch beams `Y ∈ [-47.05, -46.60]`), Ceiling soffit `Y = -43.550 m` (`2.850 m` clear height).
- **Proposed Walkable Area (`ESTIMATED`):** **`9.88 m²`** (`3.80 m × 2.60 m`).
- **Structural Logic & Support:** Heavy concrete cantilever anchored back through `X = +16.00` (`Room111B_OuterWallEast_NorthStub`) and `Pilaster_4` (`X = +14.20`).
- **Lower-Level Relationship:** Projects `+3.80 m` strictly over **unbuilt NE exterior void** (`+4.90 m` North of Room 111-B, `+1.00 m` North of `Room111B_EastCorridor`; zero lower overlap beyond `0.17 m` slab lip).
- **Player View & Sightlines:** Straight East extension ending at a `1.05 m` concrete parapet + framed laminated safety-glass East window (`Z ∈ [+30.28, +32.32]`) looking into dark misty overcast void (`RemanenciaLevel0FogColor`) with truncated formwork beam tips outside the glass.
- **Guardrails, Fall Protection & Collision:** Solid North/South concrete walls (`t = 0.22 m`), East `1.05 m` concrete parapet + visibly framed `Remanencia_Level0_OverlookSafetyGlass.mat` panels with aligned solid `BoxCollider`s (zero open edges, zero fall vectors).
- **Architectural Coherence:** Very clean, restrained, and strictly orthogonal; however, reads somewhat conventionally as a dead-end corridor extension rather than an unmistakable spatial discontinuity unless paired with strong finish/geometry shifts.
- **Future Expansion Possibilities:** Could later extend further East or open a South stub, though aligned `4.90 m` North of Room 111-B and `28 m` NE of the Elevator Core.
- **Construction Complexity & Risks:** **Lowest** (~18 modular boxes); minimal risk, moderate anomaly impact.

---

### Alternative B — Offset South-East Overhang *(L-Turn South Over Room 111-B East Corridor)*
- **Proposed Bounds (`PROPOSED`):**
  - Opening through `NUGallery_TerminalWall_East`: `Z ∈ [+30.280, +32.320]` (`2.040 m × 2.240 m`).
  - East entry vestibule: `X ∈ [+16.130, +18.930]` (`2.80 m`), `Z ∈ [+29.800, +32.800]` (`3.00 m`).
  - Southward shifted overhang wing: `X ∈ [+16.130, +18.930]` (`2.80 m`), `Z ∈ [+25.400, +29.800]` (`4.40 m`) — directly above `Room111B_EastCorr_CeilingSlab` (`Z ∈ [+19.90, +29.00]`, clean `+0.347 m` plenum). *(Reaching actual Room 111-B interior `Z ≤ +24.80` would require extending further to `Z = +22.40, X = +21.40` → `~32–36 m²`.)*
  - Floor `Y ∈ [-46.600, -46.400]` (`0.20 m`), Ceiling soffit `Y = -43.550 m` (`2.850 m` clear).
- **Proposed Walkable Area (`ESTIMATED`):** **`20.72 m²`** (`8.40 m²` vestibule + `12.32 m²` South bay; `~32–36 m²` if extended over Room 111-B).
- **Structural Logic & Support:** Resting structurally across the `+0.347 m` plenum above `Room111B_EastCorr_CeilingSlab` (`X ∈ [+16.25, +19.20]`) and anchored against `NorthWing_OuterWall_East` (`X = +16.00`).
- **Lower-Level Relationship:** Overlaps `Room111B_EastCorridor` (`3.60 m` of Z overlap), while still remaining `+0.30 m` North of `Room111B_Shell_CeilingSlab` (`Z_max = +25.10`) unless doubled in length.
- **Player View & Sightlines:** Forces an abrupt 90° right turn past `X = +16.00` into a dead-end interior hall facing South (`Z = +25.40`); zero direct exterior cantilever drama unless glazing is cut along the East wall (`X = +18.93`).
- **Guardrails, Fall Protection & Collision:** Fully enclosed concrete L-corridor walls + optional East viewing glass parapet (`1.05 m` + aligned colliders).
- **Architectural Coherence:** **Moderate–Low** — turns a tight overhang anomaly into an oversized L-shaped secondary corridor (`2×` larger than Alt A/C), dilutes the crisp "projecting beyond the wall" reading, and crowds the vertical airspace directly above Room 111-B's concealed passage.
- **Future Expansion Possibilities:** Projects South along `X = +16..+19` (still `12.7 m` East of `Hotel_Elevator_Core` at `X = +3.41`, making it a roundabout path to the elevators compared to the direct South corridor zone East of the Lightwell at `X ∈ [+6.0, +8.4]`!).
- **Construction Complexity & Risks:** **Moderate–High** (largest footprint, blind 90° corner, higher material/lighting dullness under `18`-light budget).

---

### Alternative C — Discontinuous East Fragment *(Compact Threshold Neck + Displaced Concrete/Plaster Fragment)* — **RECOMMENDED**
- **Proposed Bounds (`PROPOSED`):**
  - **1. Existing Pre-Framed Opening (`X ∈ [+15.870, +16.130]`):** Preserves `NUGallery_Terminal_SealedFrameS/N/Top` (`Z ∈ [+30.280, +32.320]`, `2.040 m × 2.240 m`) in-place, matching 07.17's exact West portal preservation pattern!
  - **2. Perimeter Threshold Neck (`X ∈ [+16.130, +17.330]`):** `1.200 m` long × `2.040 m` clear width (`Z ∈ [+30.280, +32.320]`), floor `Y = -46.400 m`, compressed ceiling soffit `Y = -44.160 m` (`2.240 m` clear height matching portal header!), ending at a recessed dark-steel **structural separation joint** (`X = +17.28`, flush metal floor seam + recessed wall/soffit shadow reveal — zero lip, zero gap).
  - **3. Displaced East Fragment (`X ∈ [+17.330, +20.530]`):** `3.200 m` long × `3.200 m` clear width, **deliberately shifted `-0.300 m` off-axis South to `Z ∈ [+29.400, +32.600]`** (centered `Z = +31.000` vs gallery `Z = +31.300`!), floor continuous & flush at **`Y = -46.400 m`** (`0.20 m` thick, `Y ∈ [-46.600, -46.400]`), ceiling stepped **`+0.300 m` higher** to **`Y = -43.250 m`** (`3.150 m` clear headroom!).
  - **Outer Shell (`PROPOSED`):** `X ∈ [+16.130, +20.750]` (`+4.62 m` past `X = +16.13`), `Z ∈ [+29.180, +32.820]`, `Y ∈ [-46.600, -43.050]`.
- **Proposed Walkable Area (`ESTIMATED`):** **`12.69 m²`** total (`2.45 m²` threshold neck + `10.24 m²` displaced fragment — `22%` smaller than the original `16.20 m²` `5.40 m` corridor and `39%` smaller than Alt B!).
- **Structural Logic & Support:** Intentionally discontinuous architectural graft — the compressed concrete neck bridges the exterior perimeter line (`X = +16.00`), while the displaced fragment beyond the dark-steel seismic joint changes alignment (`-0.30 m` Z shift), ceiling height (`+0.30 m`), and finish (weathered `PlasterWall` infill panels framed by raw `ColumnConcrete` structural columns + short truncated beam stubs projecting `0.50 m` past the East glass into empty fog). Physically 100% solid, level, and continuous beneath the player's feet!
- **Lower-Level Relationship:** Strictly clear North of `Room111B_EastCorr_CeilingSlab` (`Z_max = +29.000`, gap `+0.400 m` from interior / `+0.180 m` from outer parapet) and strictly clear North of `Room111B_Shell_CeilingSlab` (`Z_max = +25.100`, gap **`+4.300 m`**). Projects cleanly over unbuilt NE exterior void while optionally offering an oblique downward glimpse from its South window (`Z = +29.40`) toward the anomalous buried rooftops of `Room111B_EastCorridor` and `Room111B_Shell` below (`94.8%–97.8%` fog clarity)!
- **Player View & Sightlines:**
  - Approaching `X = +16.00` down the raw concrete gallery, the player sees the pre-framed `2.04 m × 2.24 m` doorway opening into a short dark-rimmed perimeter sleeve, beyond which a slightly taller, off-axis, older plastered concrete chamber catches pale overcast light from framed laminated-glass openings.
  - Stepping into the fragment, looking **East** through the framed `1.05 m`-parapet glass bay (`Z ∈ [+29.90, +32.10]`) reveals truncated concrete beam stubs terminating abruptly in mid-air above dark overcast fog — proving the hotel has ended beneath them.
  - *(Optional creator-approved South viewing window at `X ∈ [+17.85, +20.05], Z = +29.40`)* reveals the impossible lower concrete roof slabs protruding past the East perimeter wall below!
- **Guardrails, Fall Protection & Collision:** Solid North wall (`Z = +32.60`), solid neck walls, solid `1.05 m` concrete parapet (`Y = -45.350`) + dark-steel coping & slim mullions + visibly represented transparent **`Remanencia_Level0_OverlookSafetyGlass.mat`** panels with aligned solid `BoxCollider`s on the East bay (and optional South bay) — zero open ledges, zero drop-offs, zero unexplained invisible walls.
- **Architectural Coherence:** **Highest** — unmistakably Remanencia (an older/displaced fragment of the hotel grafted across the lower perimeter cliff), completely faithful to the 07.19 visual vocabulary (laminated safety glass + raw/column concrete + zero horror clichés).
- **Future Expansion Possibilities:** Leaves `NUGallery_WallSouth` (`X ∈ [+0.06, +15.87]`, including the future SE Elevator link zone `X ∈ [+6.00, +8.40]`) **100% untouched**, while a sealed concrete continuation seam on the fragment's North or East frame can remain dormant indefinitely without forcing expansion.
- **Construction Complexity & Risks:** **Low–Moderate** (~26–32 modular boxes reusing existing materials including `Remanencia_Level0_OverlookSafetyGlass.mat`); zero runtime scripts, zero added Unity lights.

### Comparative Matrix (`PROPOSED`)

| Criterion | Alt A — Direct East Cantilever (Compact `3.80 m`) | Alt B — Offset SE Overhang (Over `111-B EastCorr`) | **Alt C — Discontinuous East Fragment (`Neck + Displaced Bay`)** *(RECOMMENDED)* |
| :--- | :---: | :---: | :---: |
| **Walkable Area (`ESTIMATED`)** | **`9.88 m²`** | `20.72 m²` (`~32–36 m²` to reach 111-B) | **`12.69 m²`** (`2.45 m²` neck + `10.24 m²` fragment) |
| **Uses Existing `2.04×2.24 m` East Frame** | **Yes** | Yes | **Yes** (preserves `SealedFrameS/N/Top` verbatim!) |
| **Lower-Level Conflict Risk** | Zero (`+4.90 m` N of 111-B) | Moderate (over `111B_EastCorr`) | **Zero** (`+4.30 m` N of 111-B, `+0.40 m` N of `EastCorr`) |
| **Structural Plausibility** | High (with `0.45 m` haunch beams) | High (over lower corridor) | **Highest** (intentional seismic/discontinuity joint + safe solid slab) |
| **Architectural Anomaly Impact** | Moderate (straight overhang) | Low–Moderate (ordinary L-hall) | **Highest** (axis shift, ceiling step, finish shift, truncated beams into void) |
| **Turning & Return Clearance** | `2.60 m` (`4.06×` capsule Ø) | `2.80 m` (90° blind turn) | **`3.20 m × 3.20 m`** (`5.0×` capsule Ø; straight return!) |
| **Fall-Protection Safety** | Full (parapet + safety glass) | Full (enclosed walls) | **Full** (`1.05 m` parapet + framed safety glass + aligned colliders) |
| **Complexity** | **Lowest** | Moderate–High | **Low–Moderate** |
| **Overall Rank** | **#2** | **#3** | **#1 — RECOMMENDED** |

---

## 6. Player Traversal & Physical Safety Requirements *(VERIFIED + PROPOSED)*

Verified against `FirstPersonPlayer` (`GO=1986755682`, `CharCtrl=1986755685` in `BorderCorridorScene.unity`) and `FirstPersonPlayer.cs`:
- **`VERIFIED EXISTING`:** `height = 1.800 m`, `radius = 0.280 m`, `skinWidth = 0.040 m` (effective `r_eff = 0.320 m`, **`Ø_eff = 0.640 m`**), `stepOffset = 0.300 m`, `slopeLimit = 45.0°`, `PlayerCamera.localPosition.y = 1.600 m` (runtime eye `Y_eye = -44.760 m` on `Y = -46.400 m` floor), **zero jump**, **zero crouch**.

| Safety / Traversal Requirement | Recommended Alternative C Compliance (`PROPOSED`) |
| :--- | :--- |
| **Entry Aperture (`X = +16.00`)** | `2.040 m` clear width (**`3.19×` capsule Ø**) × `2.240 m` clear height (`+0.440 m` over capsule), `0.000 m` floor lip. |
| **Threshold Neck Width & Headroom** | `2.040 m` wide × `1.200 m` long, `2.240 m` clear headroom, continuous level concrete floor `Y = -46.400 m`. |
| **Fragment Turning Clearance & Headroom** | `3.200 m × 3.200 m` unobstructed turning square (**`5.0×` capsule Ø**), `3.150 m` clear headroom (`Y_ceil = -43.250 m`). |
| **Fall Protection & Containment** | Solid walls + `1.050 m` concrete parapet (`Y = -45.350`) + visibly framed `Remanencia_Level0_OverlookSafetyGlass.mat` panels (`Y ∈ [-45.31, -43.31]`) with gap-free aligned solid `BoxCollider`s. Zero open ledges, zero jumps, zero gap crossings, zero access to lower roofs or void. |
| **Complete Reversible Return Route** | Walk straight West from fragment (`X = +20.53`) → threshold neck (`X = +16.00`) → North Upper Gallery → NW Service Staircase → Nivel 0 → Room 111-B → **Threshold of Trust** (sole authorized exit). |

---

## 7. Narrative, Lighting & Elevator Relationship *(VERIFIED + PROPOSED)*

### A. Narrative & Architectural Direction
- Reinforces Remanencia's core premise (*contradictory building memory solidified as quiet concrete*): the hotel's lower North Wing ends cleanly at `X = +16.000`, yet at the upper floor an older, slightly taller, off-axis plaster-and-concrete fragment crosses the perimeter seam and terminates abruptly in mid-air over empty dark haze, with truncated formwork beam stubs extending into nothingness.
- **Zero** horror tropes, **zero** supernatural VFX, **zero** Cobalt-60 claims, **zero** notes, room numbers, signage, keys, NPCs, or exits.

### B. Lighting Constraints (`VERIFIED EXISTING`)
- Preserve exact **`18` Unity `Light` components (`LightShadows.None`)**.
- Zero added lights required: reuse flat `RemanenciaLevel0AmbientLight (0.29, 0.31, 0.33)` + optional single restrained emissive-only ceiling strip (`Remanencia_Level0_UtilityWarmOn.mat`, matching `NUGallery_StripTube_0..3`) inside the fragment + player flashlight `[F]`.

### C. Future Twin Elevator Core Relationship *(UNAPPROVED & UNBUILT)*
- **`VERIFIED EXISTING`:** `Hotel_Elevator_Core` (`GO=331299473`, `35` children) sits at `X ∈ [+1.000, +3.410], Z ∈ [+4.200, +9.400]` — **`28.12 m` Southwest** of `NUGallery_TerminalWall_East` (`X = +16.00, Z = +31.30`)!
- **Forensic Conclusion:** The East Overhang (`X > +16.00`) is **NOT** the natural connector to the Twin Elevator Core. The geometrically cleanest future elevator connection remains the previously identified mid-gallery South anchor zone East of the Lightwell (`X ∈ [+6.00, +8.40], Z = +29.58 → +9.40`, directly over Nivel 0's East Arcade & Main Lobby, `100%` clear of both the 07.19 Overlook `X ≤ +0.28` and the 07.20 East Overhang `X ≥ +16.00`).
- Both elevator bays remain **100% protected, sealed, unassigned, and untouched**.

---

## 8. Recommended Alternative — Section 12 Checklist *(RECOMMENDED — PROPOSED)*

1. **What the player sees upon reaching `X = +16.00`:** Instead of a dead concrete wall behind `NUGallery_Terminal_SealedFrameS/N/Top`, the pre-framed `2.04 m × 2.24 m` doorway opens into a short `1.20 m` raw-concrete perimeter sleeve framed by a dark-steel separation joint, revealing a slightly taller, off-axis older plastered chamber catching faint cool haze through framed safety-glass bays.
2. **How the terminal opening works in a future increment:** Preserve `NUGallery_TerminalWall_East` (`GO=1036244486`) in-place trimmed as the South pier (`Z ∈ [+29.58, +30.28]`), add a North pier (`Z ∈ [+32.32, +33.02]`), transom (`Y ∈ [-44.16, -43.55]`), and subfloor kick (`Y ∈ [-46.60, -46.40]`), preserving `SealedFrameS/N/Top` and `ConduitDrop`/`JunctionBox` verbatim (exact 07.17 West-portal pattern!).
3. **First new space:** The `1.20 m × 2.04 m` (`2.45 m²`) compressed perimeter threshold neck leading seamlessly into the `3.20 m × 3.20 m` (`10.24 m²`) displaced concrete/plaster fragment (`12.69 m²` total walkable).
4. **Interior vs. exposed fragment:** **Enclosed interior fragment with framed laminated-glass exterior views** — the player remains safely indoors while visibly perceiving the exterior cliff edge and truncated structural beam stubs ending in dark overcast void.
5. **What exists directly beneath the extension:** Only `0.170 m` of lower North Wing slab edge (`X ∈ [+16.13, +16.30]`), followed by **100% pure unbuilt exterior void** (`X ∈ [+16.30, +20.75], Z ∈ [+29.18, +32.82]`).
6. **Is it actually above Room 111-B?** **NO** — Room 111-B sits `+4.30 m` to `+4.70 m` further South (`Z ∈ [+19.70, +25.10]`), and `Room111B_EastCorridor` stops at `Z = +29.00 m`.
7. **How the extension is supported / discontinuous:** Physically continuous level `0.20 m` slab (`Y = -46.400 m`) with solid `BoxCollider`s, visually staged as a deliberately discontinuous architectural graft across a dark-steel seismic separation seam (`X = +17.28`) with truncated exterior concrete beam stubs projecting into mid-air outside the glass.
8. **What makes the architecture anomalous:** Four restrained architectural contradictions combined: (a) extending `+4.53 m` past the lower building's solid eastern exterior wall into pure void, (b) `-0.30 m` South centerline shift, (c) `+0.30 m` taller ceiling and older weathered plaster finish beyond the raw concrete seam, and (d) truncated structural beam stubs terminating in mid-air (plus optional oblique glimpse of the impossible lower Room 111-B roof slabs below!).
9. **Safe return:** Unobstructed `3.20 m × 3.20 m` turning area returning straight West through the `2.04 m` doorway → North Upper Gallery → NW Service Staircase → Nivel 0 → Room 111-B → Threshold of Trust.
10. **What remains inaccessible:** Exterior void beyond the framed safety glass, truncated beam stubs, lower Room 111-B rooftops (`Y = -46.948`), Courtyard Lightwell, and Twin Elevator Core.
11. **Deferred to future increments:** Any further East/North wings, any South link toward `Hotel_Elevator_Core`, elevator bay role assignments, and any new lights beyond `18`.

---

## 9. Proposed Implementation Roadmap *(PROPOSED — NOT AUTHORIZED YET)*

| Increment | Scope | Key Guardrails |
| :---: | :--- | :--- |
| **07.21** | **East Overhang Construction (Alternative C — Discontinuous East Fragment)** via dedicated idempotent `EastOverhangBuilder.cs` under sibling root `Backrooms_Architecture/Hotel_EastOverhang` + `RemanenciaEastOverhangTests.cs` (including `CharacterController.Move()` walk, barrier containment, return to Nivel 0, and 2× idempotency) + targeted cross-builder update to `NorthUpperGalleryBuilder.cs` & `RemanenciaNorthUpperGalleryTests.cs` (`L137, L165–169, L220–232` for the authorized `Z ∈ [+30.28, +32.32]` East opening only). | Preserves exact `18` lights, `Pilaster_4`/`CeilingBeam_4`, `SealedFrameS/N/Top`, `ConduitDrop`/`JunctionBox`, 07.19 Overlook, Lightwell, Room 111-B, Threshold of Trust, Elevators, and `BorderCorridorScene.unity`. |
| **07.22** | **PlayMode Polish & Conditional South-Window Framing Check** — Live PlayMode review of fog visibility, finish contrast, and whether any lower-rooftop glimpse looks coherent or needs a solid South sill/wall. | Only if PlayMode review warrants tuning. |
| **07.23+** | **Independent Upper Elevator Approach & Twin Elevator Core Design** (evaluated cleanly from the mid-gallery South wall `X ∈ [+6.00, +8.40]`, independent of the East Overhang). | Requires explicit separate creator approval. |

---

## 10. Protected Geometry & Systems *(MANDATORY INVARIANTS)*

All existing project assets and systems remained **100% untouched** during 07.20 and remain strictly protected in future increments:
- Complete 07.15 `Hotel_NorthwestServiceStaircase` & upper landing shell.
- Complete 07.17 `Hotel_NorthUpperGallery` outside the single future proposed East terminal opening (`Z ∈ [+30.28, +32.32]`).
- Complete 07.19 `Hotel_CourtyardLightwellOverlook` (`Remanencia_Level0_OverlookSafetyGlass.mat` reused read-only).
- All 10 `NorthWing_Lightwell_*` objects (4 walls, skylight cap, 3 beams, 2 purlins).
- `Hotel_Room111B_Anomaly`, `Hotel_Room111B_GuestInterior`, **Threshold of Trust** (`"threshold-of-trust"`), Rooms `107–110`, Room 110 ecological Zone D, and `Hotel_Elevator_Core` (`35` children, both bays).
- All 07.6–07.9 clues/ecology/microfauna, entry-only entrance (`Backrooms_ExitDoor_ToStreet`), both surface entry routes, save schema (`schemaVersion == 1`), exact `18` Unity `Light` components (`LightShadows.None`), and `BorderCorridorScene.unity` (`SHA-256: 0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B`).

---

## 11. Unresolved Risks & Creator Decisions *(UNRESOLVED)*

1. **Select Spatial Alternative (`UNRESOLVED`):** Approve **Alternative C — Discontinuous East Fragment (`12.69 m²`, `X ∈ [+16.13, +20.53], Z ∈ [+29.40, +32.60]`)**, OR select **Alternative A — Compact Direct East Cantilever (`9.88 m²`, `3.80 m` straight overhang)** or **Alternative B — Offset SE Overhang (`20.72 m²`)**.
2. **South Glazed Window vs. Solid South Wall on the Fragment (`UNRESOLVED` — VISUAL RISK):** Opening a framed safety-glass window on the fragment's South wall (`Z = +29.40`) gives a clear oblique view down onto `Room111B_EastCorr_CeilingSlab` and `Room111B_Shell_CeilingSlab` (`94.8%–97.8%` fog clarity). **Risk:** Those existing protected lower ceiling slabs use `Backrooms_CeilingGrid.mat` on all 6 faces, so their raw top faces may read visually like acoustic ceiling tiles from above rather than exterior concrete roofs. **Creator decision needed:** Keep the South wall **solid concrete/plaster** (pure East-facing void window only — zero risk of exposing raw lower ceiling-grid tops!) OR include a high/narrow South viewing slot and inspect it in 07.21 renders before accepting.
3. **Emissive Strip Inside Fragment (`UNRESOLVED`):** Confirm adding `1` restrained emissive-only `UtilityWarmOn` ceiling strip inside the fragment (`0` Unity `Light` components added; exact `18`-light budget preserved) vs. relying solely on flat ambient light + flashlight `[F]`.
4. **Exact Projection Depth (`UNRESOLVED`):** Confirm compact `+4.53 m` total reach (`1.20 m` neck + `3.20 m` fragment, `X_max = +20.53 m`) vs. trimming further to `+3.80 m` (`X_max = +19.93 m`) or extending to the original `+5.40 m` (`X_max = +21.40 m`).


---

## 12. Implementation 07.21 — As-Built East Overhang *(IMPLEMENTED IN 07.21)*

Following creator approval of **Alternative C — Discontinuous East Fragment** (with a **100% solid South wall — zero South window/slot**, East-facing framed laminated safety glass, visible articulated cantilever/transfer support frame, and flush Architectural Separation Reveal), Implementation 07.21 constructed `BackroomsLevel1_Root/Backrooms_Architecture/Hotel_EastOverhang` via the dedicated, idempotent Editor builder `EastOverhangBuilder.MigrateEastOverhangAndSave()`.

### A. Final As-Built Coordinates & Geometry *(VERIFIED IN 07.21)*

| Element | As-Built Coordinates / Dimensions (`VERIFIED`) | Notes / Comparison vs. 07.20 Proposal |
| :--- | :--- | :--- |
| **Authorized East Portal Opening** | `X ∈ [+15.870, +16.130]`, **`Z ∈ [+30.280, +32.320]` (`2.040 m` clear width, centered `Z = +31.300`)**, **`Y ∈ [-46.400, -44.160]` (`2.240 m` clear height)** | `NUGallery_TerminalWall_East` (`GO=1036244486`) & `Terminal_Baseboard` (`GO=57348749`) preserved in-place as South pier (`Z ∈ [+29.58, +30.28]`); `SealedFrameS/N/Top`, `ConduitDrop`, `JunctionBox`, `PilasterNorth/South_4`, and `CeilingBeam_4` preserved verbatim byte-identically. |
| **Perimeter Threshold Neck (`EastOverhang_ThresholdNeck`)** | `X ∈ [+16.130, +17.330]` (`1.200 m` long), continuous level floor `Y = -46.400 m` (`0.200 m` slab), compressed ceiling soffit `Y = -44.160 m` (`2.240 m` headroom; slab ends `4 cm` inside fragment West wall at `X = +17.290` for zero bleed) | Parallel oblique concrete sidewalls (`14.036°` gentle splay) smoothly shift centerline **`-0.300 m` South** from `Z = +31.300` (`[+30.28, +32.32]`) to `Z = +31.000` (`[+29.98, +32.02]`), maintaining full **`2.040 m`** clear width everywhere with zero sharp corners and exact walkable area **`2.448 m²`**. |
| **Architectural Separation Reveal (`EastOverhang_ArchitecturalReveal`)** | Centered at **`X = +17.300 m`**, `Z ∈ [+29.980, +32.020]` (`2.040 m × 2.240 m` arrival portal) | Visual & material boundary ONLY (not a certified seismic joint): flush dark-steel floor seam plate (`h = 0.004 m`, non-colliding over unbroken `Y = -46.400 m` concrete slab), recessed dark-steel jambs & header, and dark `ColumnConcrete` shadow framing marking the transition from raw-concrete neck to older plastered fragment. |
| **Displaced East Fragment (`EastOverhang_DisplacedFragment`)** | **`X ∈ [+17.330, +20.530]` (`3.200 m`)**, **`Z ∈ [+29.400, +32.600]` (`3.200 m`, centered `Z = +31.000` = `-0.300 m` shift)**, floor `Y = -46.400 m`, raised ceiling soffit **`Y = -43.250 m` (`3.150 m` clear headroom, `+0.300 m` above gallery)** | Older weathered plaster infill walls (`PlasterWall`) framed by 4 `ColumnConcrete` corner columns and overhead ceiling beam. **South wall (`Z ∈ [+29.18, +29.40]`) and North wall (`Z ∈ [+32.60, +32.82]`) are 100% SOLID — ZERO South window, ZERO viewing slot toward Room 111-B (`+4.08 m` clear North of Room 111-B; `+0.18 m` clear North of `EastCorr_CeilingSlab`).** Walkable area = **`10.240 m²`**. |
| **Total As-Built Walkable Area** | **`12.688 m²`** (`2.448 m²` neck + `10.240 m²` displaced fragment) | Matches approved target `12.69 m²` exactly; excludes wall tops, parapets, and exterior beams. |
| **Visible Support Frame & Truncated Beam Remnants (`EastOverhang_Structure`)** | Back-span tie beams (`X ∈ [+14.34, +15.87]`, clear of `ConduitDrop`), perimeter anchor collars (`X ∈ [+16.13, +16.41]`), twin continuous `0.45 m`-deep lower cantilever girders (`Y ∈ [-47.05, -46.60]`) + twin upper roof edge beams (`Y ∈ [-43.39, -43.05]`) extending to `X = +21.350 m` (`+0.620 m` past glass), plus 4 mullion-aligned parapet & cornice beam stubs (`X ∈ [+20.73, +21.28]`) | Visually articulates a continuous concrete cantilever/transfer frame grafted onto the existing `X = +16.00` perimeter wall and `X = +14.20` portal frame — zero columns into exterior void, zero edits to protected structures, zero Room 111-B overlap. *(Architectural representation only; not civil-engineering load certification.)* |
| **East Observation Facade (`EastOverhang_EastFacade`)** | Solid `1.050 m` concrete parapet (`X ∈ [+20.53, +20.73]`, top `Y = -45.350 m`) + dark-steel sill (`-45.310`) + 3 laminated transparent safety-glass panels (`Y ∈ [-45.31, -43.31]`, `2.000 m` tall × `3.00 m` glazed width) + dark-steel jambs, header, and 2 slim mullions | Reuses `Remanencia_Level0_OverlookSafetyGlass.mat` **read-only** (`SHA-256: B9D8242443CEB7322126343993AE94DDF43CCA6AEC91B2FB1133FF6AF96A05DD` unchanged). Every glass panel and steel frame carries a directly aligned solid `BoxCollider` (gap-free containment; zero invisible wall). |
| **Interior Details & Lighting (`EastOverhang_InteriorDetails`)** | Baseboards, displaced centerline floor seam (`Z = +31.000`), and `1` restrained emissive-only ceiling utility strip (`Remanencia_Level0_UtilityWarmOn.mat`) | **Zero Unity `Light` components added** — preserves exact **`18` Unity `Light` components (`LightShadows.None`)**. *(Emissive strip changes rendered appearance only; does not emit real dynamic light.)* |

### B. Verification Pipeline & Protected-Invariant Results *(VERIFIED IN 07.21)*
- **2× Scene Migration Idempotency:** `IDEMPOTENT_2X_SHA_MATCH=True` (`BackroomsLevel1Scene.unity` SHA-256: `971A56F5FC0932802FDFE7FB999E174172D8A18179E741FFF018E85FAC03F0A3`).
- **Complete Unity EditMode Test Suite:** **`111 / 111` PASSED** (`0` failed, `0` skipped, `75.86 s`, exit `0`), including `RemanenciaEastOverhangTests` (all 41 checks: exact opening, `-0.300 m` shift, solid South wall linecasts, East glass/parapet sweep, support-frame bounds, zero Room 111-B intersection, and full end-to-end `CharacterController.Move()` walk from gallery → neck → reveal → fragment → wall/glass containment → return down NW staircase to Nivel 0).
- **`BackroomsLevel1Builder.RunVerificationChecks()`:** Passed (`ok = True`, exact `18` lights, `0` shadow-casting).
- **`SecretWedgeRoomVerificationSuite.RunVerification()`:** **`11 / 11` checks passed** (exit `0`).
- **Windows x64 Standalone Build (`scripts/build-standalone.ps1 -Force`):** Exit `0` (`33.2 s`, `MyVirtualCommunity.exe` `667,136` bytes, SHA-256 `3070e580e019576b9afcdeb4fec18d03cebc119f6ae84d597412905c749ad653`); `-CheckOnly` **`FRESH`** (exit `0`).
- **Protected Objects & Assets:** All 25 audited protected serialized scene objects (all 10 Lightwell objects, `SealedFrameS/N/Top`, `ConduitDrop`/`JunctionBox`, `Pilaster_4`/`CeilingBeam_4`, Room 111-B shell/EastCorr/Threshold, 07.19 Overlook glass panels) verified **byte-identical** vs. `d6ee0c6`; `BorderCorridorScene.unity` (`0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B`) and `Remanencia_Level0_OverlookSafetyGlass.mat` (`B9D8242443CEB7322126343993AE94DDF43CCA6AEC91B2FB1133FF6AF96A05DD`) byte-identical.
- **Exterior Fog-Void Background Fix (`SecretBackroomsTransitionController.cs`):** Updated `ApplyBackroomsAtmosphere()` / `RestoreOutdoorAtmosphere()` to switch the active player camera to `CameraClearFlags.SolidColor` with `backgroundColor = RemanenciaLevel0FogColor` (`(0.16, 0.175, 0.19)`) while inside Remanencia and restore the saved outdoor `clearFlags` and `backgroundColor` upon exit — eliminating the Unity default-blue (`#314D79`) sky fallback visible behind the East Overhang safety-glass facade when `RenderSettings.skybox == null`.
- **Deferred Features & Pending Manual PlayMode Review:** Any further East/North wing, Twin Elevator Core connections, and new lights remain deferred. Final visual acceptance of the `-0.300 m` centerline shift, material/ceiling contrast, exterior beam stubs in fog, and readability with/without flashlight `[F]` awaits the creator's live PlayMode inspection.
