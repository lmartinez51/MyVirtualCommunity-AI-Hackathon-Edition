# REMANENCIA — Nivel 0: Upper Elevator Approach & Twin Elevator Core (07.22)
## Forensic Spatial Audit & Architectural Design

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia** *(overarching dimensional universe — proper name, untranslated in ES/EN)*  
**Territory Audited:** **Nivel 0 — Hotel de las Palomas** (`unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`)  
**Baseline Audited:** Branch `product/unity-fellowship-2027`, HEAD `0b63506c36546d3f6d48df56538671fd49368073` (`feat(remanencia): add discontinuous east fragment` — 07.21 PlayMode-accepted and synchronized with `origin/product/unity-fellowship-2027`; working tree clean prior to this document)  
**Reference Repository:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity` *(strictly READ-ONLY; untouched)*  
**Document Status:** **IMPLEMENTATION 07.22 — STRICTLY READ-ONLY FORENSIC AUDIT & ARCHITECTURAL DESIGN (ZERO UNITY SCENE, BUILDER, SCRIPT, TEST, MATERIAL, OR EXISTING-DOC MODIFICATIONS)**

> **Mandatory Measurement & Status Labels:**  
> Every coordinate, dimension, and architectural claim in this document is explicitly distinguished as:
> - **`VERIFIED EXISTING`** — Directly extracted from serialized `GameObject` / `Transform` / `BoxCollider` / `MeshRenderer` / `Light` YAML records in `BackroomsLevel1Scene.unity` and `BorderCorridorScene.unity`, and verified C# source code at commit `0b63506`.
> - **`PROPOSED`** — Concrete geometric target proposed for a future authorized `07.23+` increment; **not implemented**.
> - **`ESTIMATED`** — Projected walkable area, sightline clarity, or lighting/complexity figure pending future prototyping.
> - **`UNRESOLVED`** — Open creator decision required before any corridor, waiting area, or elevator work is authorized.

---

## 1. Executive Design Intent *(PROPOSED)*

Following PlayMode acceptance of **07.15 (Northwest Service Staircase)**, **07.17 (North Upper Gallery)**, **07.19 (Courtyard Lightwell Overlook)**, and **07.21 (East Overhang — Discontinuous East Fragment)**, the upper stratum of *Hotel de las Palomas* now establishes a clear, restrained vertical sequence: climbing a raw-concrete service stair, discovering an enclosed courtyard skylight trapped indoors, and stepping beyond the building's eastern lower footprint into an off-axis cantilevered fragment.

The purpose of **07.22** is to forensically audit how that upper stratum can connect southward to the existing **Twin Elevator Core (`Hotel_Elevator_Core`)** — **without** breaking any protected architecture, **without** pre-assigning Bay 1 or Bay 2, and **without** prematurely activating elevator mechanics.

### Key Forensic Findings Established by 07.22
1. **The Previously Projected `0.20 m` Overlap Is Physically Impassable (`VERIFIED`):** The nominal East-side corridor (`X ∈ [+6.000, +8.400]`) and previously projected Upper Elevator Waiting Area (`X ∈ [+3.410, +6.200]`) share only **`0.200 m`** of horizontal X overlap at `Z = +9.400` (`0.31×` player capsule diameter `0.640 m`; shrinks to `≤ 0.120 m` with wall reveals). Direct connection without geometric resolution is impossible.
2. **No Internal Shaft Divider Wall Exists Yet (`VERIFIED`):** Inside `Hotel_Elevator_Core` (`35` children), `Hotel_ElevatorCore_FrontPierMid` (`X ∈ [+3.150, +3.410]`) is only a **`0.260 m`-deep front pier** at `Z = +6.800`. Behind the closed doors (`X ∈ [+1.240, +3.100]`), Bay 1 and Bay 2 currently share **one single empty hollow shell cavity** (`1.860 m × 4.720 m × 2.850 m`) with **zero internal divider wall**, **zero cabins**, **zero shaft cutouts** in the monolithic `Backrooms_Ceiling`, and **zero runtime scripts**.
3. **Uniform Lower Structural Plenum Confirmed (`VERIFIED`):** Every ceiling slab beneath the entire East, South, and West upper routes (`Backrooms_Ceiling` `Y_top = -46.950 m`, `NorthWing_CeilingSlab_*` `Y_top = -46.948 m`) leaves an unbroken **`0.348 m–0.350 m` plenum** beneath the `Y = -46.600 m` upper slab soffit, with **zero** non-Lightwell protrusions above `Y = -46.948 m`.
4. **Smallest Coherent Route Identified (`PROPOSED` — Alternative B):** Departing **Bay 2 of `NUGallery_WallSouth`** (`X ∈ [+6.000, +8.400]`, `+0.860 m` clear of `PilasterSouth_2`, `+1.260 m` clear of `PilasterSouth_3`), running South along the East side of the Lightwell (`+0.480 m` outside `SkylightCap` after `0.220 m` wall thickness) to `Z = +11.800` (aligned directly over Nivel 0's `Z = +12.00` Connection A spine), shifting West through a **compact Transition Vestibule (`X ∈ [+3.410, +8.400], Z ∈ [+9.400, +11.800]`, `11.98 m²`)**, and entering a **focused Upper Elevator Waiting Area (`X ∈ [+3.410, +6.200], Z ∈ [+4.200, +9.400]`, `14.51 m²`)** via a full `2.400 m` portal (`3.75×` capsule width). Total walkable area: **`69.16 m²`** (`64.60 m²` in `2.20 m`-compact variant) with **zero edits** to 07.15, 07.19, 07.21, the Lightwell, Room 111-B, or `Hotel_Elevator_Core`.

---

## 2. Verified Twin Elevator Core Inventory (`Hotel_Elevator_Core`) *(VERIFIED EXISTING)*

Directly extracted from `BackroomsLevel1Scene.unity` at commit `0b63506` and cross-checked against `BackroomsLevel1Builder.cs` (`L1458–1490`, `L5571–5610`):

- **Root Container:** `BackroomsLevel1_Root/Backrooms_Architecture/Hotel_GroundFloor_Architecture/Hotel_Elevator_Core`
  - **`GO=331299473`, `TR=331299474`**, active=`1`, child count = **`35`**, world transform `(0, 0, 0)`, scale `(1, 1, 1)`.
- **Overall Structural Shell Bounds (`VERIFIED EXISTING`):**
  - **X:** `[+1.000, +3.410]` (`2.410 m` masonry depth; front trim/buttons project to `+3.470`, soffit projects East to `+5.800`; South/North wall end caps span `+0.940..+3.460`).
  - **Y:** `[-50.000, -47.150]` (**`2.850 m`** — strictly Nivel 0 floor-to-ceiling height; sealed above by monolithic `Backrooms_Ceiling`).
  - **Z:** `[+4.200, +9.400]` (`5.200 m` core span; BackWall/FrontPier end overlaps extend to `[+4.135, +9.465]`, Lobby Soffit spans `[+4.100, +9.500]`).
- **Orientation:** Both elevator bays face **East (`+X`)** into Nivel 0's Main Lobby (`X > +3.410`). Directly West behind `BackWall` (`X ∈ [+1.000, +1.240]`) sits `Hotel_Room101_EastWall` (`GO=1712248583`, `X ∈ [+0.89, +1.11]`) and Room 101's bathroom (`Z ∈ [+4.32, +7.08]`).

### Complete Serialized Component Inventory (All 35 Children — `VERIFIED EXISTING`)

| # | GameObject Name | `GO` / `TR` `fileID` | World `X` Bounds (m) | World `Y` Bounds (m) | World `Z` Bounds (m) | Material | `BoxCollider` | Scripts |
| :-: | :--- | :---: | :---: | :---: | :---: | :--- | :---: | :---: |
| 1 | `Hotel_ElevatorCore_BackWall` | `1951929040` / `1951929041` | `[1.000, 1.240]` | `[-50.000, -47.150]` | `[4.140, 9.460]` | `ConcreteColumn` | Solid | None |
| 2 | `Hotel_ElevatorCore_BackWall_Baseboard` | `1029979850` / `1029979851` | `[0.988, 1.253]` | `[-50.000, -49.880]` | `[4.138, 9.462]` | `Baseboard` | None | None |
| 3 | `Hotel_ElevatorCore_SouthWall` | `585933301` / `585933302` | `[0.940, 3.460]` | `[-50.000, -47.150]` | `[4.200, 4.440]` | `ConcreteColumn` | Solid | None |
| 4 | `Hotel_ElevatorCore_SouthWall_Baseboard` | `1505220348` / `1505220349` | `[0.938, 3.462]` | `[-50.000, -49.880]` | `[4.187, 4.453]` | `Baseboard` | None | None |
| 5 | `Hotel_ElevatorCore_NorthWall` | `2097585768` / `2097585769` | `[0.940, 3.460]` | `[-50.000, -47.150]` | `[9.160, 9.400]` | `ConcreteColumn` | Solid | None |
| 6 | `Hotel_ElevatorCore_NorthWall_Baseboard` | `477129982` / `477129983` | `[0.938, 3.462]` | `[-50.000, -49.880]` | `[9.147, 9.413]` | `Baseboard` | None | None |
| 7 | `Hotel_ElevatorCore_FrontPierS` | `1166525667` / `1166525668` | `[3.150, 3.410]` | `[-50.000, -47.150]` | `[4.135, 5.065]` | `ConcreteColumn` | Solid | None |
| 8 | `Hotel_ElevatorCore_FrontPierS_Baseboard` | `327610998` / `327610999` | `[3.137, 3.422]` | `[-50.000, -49.880]` | `[4.132, 5.068]` | `Baseboard` | None | None |
| 9 | `Hotel_ElevatorCore_FrontPierMid` | `1160025726` / `1160025727` | `[3.150, 3.410]` | `[-50.000, -47.150]` | `[6.435, 7.165]` | `ConcreteColumn` | Solid | None |
| 10 | `Hotel_ElevatorCore_FrontPierMid_Baseboard` | `555681331` / `555681332` | `[3.137, 3.422]` | `[-50.000, -49.880]` | `[6.432, 7.168]` | `Baseboard` | None | None |
| 11 | `Hotel_ElevatorCore_FrontPierN` | `139182998` / `139182999` | `[3.150, 3.410]` | `[-50.000, -47.150]` | `[8.535, 9.465]` | `ConcreteColumn` | Solid | None |
| 12 | `Hotel_ElevatorCore_FrontPierN_Baseboard` | `2075502628` / `2075502629` | `[3.137, 3.422]` | `[-50.000, -49.880]` | `[8.532, 9.468]` | `Baseboard` | None | None |
| 13 | `Hotel_ElevatorCore_FrontHeader` | `1472619301` / `1472619302` | `[3.140, 3.420]` | `[-47.800, -47.150]` | `[4.180, 9.420]` | `ConcreteColumn` | Solid | None |
| 14 | `Hotel_ElevatorLobby_Soffit` | `32837909` / `32837910` | `[3.400, 5.800]` | `[-47.510, -47.150]` | `[4.100, 9.500]` | `ConcreteColumn` | None | None |
| 15 | `Hotel_ElevatorBay_1_DoorLeafS` | `2004914621` / `2004914622` | `[3.100, 3.180]` | `[-50.000, -47.800]` | `[5.000, 5.760]` | `ElevatorDoorMetal` | Solid | None |
| 16 | `Hotel_ElevatorBay_1_DoorLeafN` | `1307326071` / `1307326072` | `[3.100, 3.180]` | `[-50.000, -47.800]` | `[5.740, 6.500]` | `ElevatorDoorMetal` | Solid | None |
| 17 | `Hotel_ElevatorBay_1_CenterSeam` | `1008553737` / `1008553738` | `[3.180, 3.200]` | `[-50.000, -47.800]` | `[5.737, 5.763]` | `CeilingVoid` | None | None |
| 18 | `Hotel_ElevatorBay_1_FrameJambS` | `464369093` / `464369094` | `[3.290, 3.470]` | `[-50.000, -47.780]` | `[4.960, 5.040]` | `IndustrialMetal` | None | None |
| 19 | `Hotel_ElevatorBay_1_FrameJambN` | `181796879` / `181796880` | `[3.290, 3.470]` | `[-50.000, -47.780]` | `[6.460, 6.540]` | `IndustrialMetal` | None | None |
| 20 | `Hotel_ElevatorBay_1_FrameHeader` | `1454700179` / `1454700180` | `[3.290, 3.470]` | `[-47.800, -47.720]` | `[4.960, 6.540]` | `IndustrialMetal` | None | None |
| 21 | `Hotel_ElevatorBay_1_FloorSill` | `228939351` / `228939352` | `[3.150, 3.450]` | `[-50.000, -49.976]` | `[4.980, 6.520]` | `IndustrialMetal` | None | None |
| 22 | `Hotel_ElevatorBay_1_IndicatorHousing` | `1410903630` / `1410903631` | `[3.410, 3.450]` | `[-47.620, -47.460]` | `[5.470, 6.030]` | `IndustrialMetal` | None | None |
| 23 | `Hotel_ElevatorBay_1_IndicatorStrip` | `137003656` / `137003657` | `[3.449, 3.461]` | `[-47.570, -47.510]` | `[5.530, 5.970]` | `CeilingVoid` | None | None |
| 24 | `Hotel_ElevatorBay_2_DoorLeafS` | `1255285515` / `1255285516` | `[3.100, 3.180]` | `[-50.000, -47.800]` | `[7.100, 7.860]` | `ElevatorDoorMetal` | Solid | None |
| 25 | `Hotel_ElevatorBay_2_DoorLeafN` | `1590748828` / `1590748829` | `[3.100, 3.180]` | `[-50.000, -47.800]` | `[7.840, 8.600]` | `ElevatorDoorMetal` | Solid | None |
| 26 | `Hotel_ElevatorBay_2_CenterSeam` | `1794556672` / `1794556673` | `[3.180, 3.200]` | `[-50.000, -47.800]` | `[7.837, 7.862]` | `CeilingVoid` | None | None |
| 27 | `Hotel_ElevatorBay_2_FrameJambS` | `552356555` / `552356556` | `[3.290, 3.470]` | `[-50.000, -47.780]` | `[7.060, 7.140]` | `IndustrialMetal` | None | None |
| 28 | `Hotel_ElevatorBay_2_FrameJambN` | `664166405` / `664166406` | `[3.290, 3.470]` | `[-50.000, -47.780]` | `[8.560, 8.640]` | `IndustrialMetal` | None | None |
| 29 | `Hotel_ElevatorBay_2_FrameHeader` | `1061255800` / `1061255801` | `[3.290, 3.470]` | `[-47.800, -47.720]` | `[7.060, 8.640]` | `IndustrialMetal` | None | None |
| 30 | `Hotel_ElevatorBay_2_FloorSill` | `242779403` / `242779404` | `[3.150, 3.450]` | `[-50.000, -49.976]` | `[7.080, 8.620]` | `IndustrialMetal` | None | None |
| 31 | `Hotel_ElevatorBay_2_IndicatorHousing` | `908471151` / `908471152` | `[3.410, 3.450]` | `[-47.620, -47.460]` | `[7.570, 8.130]` | `IndustrialMetal` | None | None |
| 32 | `Hotel_ElevatorBay_2_IndicatorStrip` | `1469709015` / `1469709016` | `[3.449, 3.461]` | `[-47.570, -47.510]` | `[7.630, 8.070]` | `CeilingVoid` | None | None |
| 33 | `Hotel_Elevator_CallPanelPlate` | `1622816346` / `1622816347` | `[3.417, 3.443]` | `[-48.950, -48.690]` | `[6.730, 6.870]` | `IndustrialMetal` | None | None |
| 34 | `Hotel_Elevator_CallButtonUp` | `823213806` / `823213807` | `[3.441, 3.455]` | `[-48.790, -48.750]` | `[6.780, 6.820]` | `ElevatorDoorMetal` | None | None |
| 35 | `Hotel_Elevator_CallButtonDown` | `1114928462` / `1114928463` | `[3.441, 3.455]` | `[-48.890, -48.850]` | `[6.780, 6.820]` | `ElevatorDoorMetal` | None | None |

### Adjacent Non-Child Elevator Fixtures (Nivel 0 Lobby — `VERIFIED EXISTING`)
- `Hotel_Sign_ElevatorsHeader` (`GO=120906857`): `X ∈ [3.42, 3.45], Y ∈ [-47.43, -47.21], Z ∈ [6.02, 7.58]` — mounted on East face of `FrontHeader` (`"ELEVADORES / ELEVATORS"`).
- `Hotel_Sign_ElevatorNotice` (`GO=2080536146`): `X ∈ [3.43, 3.44], Y ∈ [-49.20, -49.04], Z ∈ [6.62, 6.98]` — mounted below Call Panel on `FrontPierMid` (`"FUERA DE SERVICIO / OUT OF SERVICE"`).
- `Hotel_Elec_ElevatorJunctionBox` (`GO=376910389`, `Z ∈ [8.96, 9.14], Y ∈ [-47.61, -47.43]`) & `Hotel_Elec_ElevatorConduitRun` (`GO=579283014`, `Z ∈ [9.04, 9.07], Y ∈ [-47.51, -47.17]`) — unfinished surface electrical stub beside Bay 2's North corner.
- `Hotel_Lobby_Beam_West` (`GO=1693310821`): `X ∈ [3.25, 3.55], Y ∈ [-47.49, -47.15], Z ∈ [-1.80, +12.00]` — longitudinal concrete lobby beam running directly along the elevator core East face.

---

## 3. Exact Bay 1 & Bay 2 Geometry and Current Behavior *(VERIFIED EXISTING)*

| Property | Bay 1 (South) | Bay 2 (North) | Forensic Verification Note |
| :--- | :---: | :---: | :--- |
| **Door Center `Z`** | **`+5.750 m`** | **`+7.850 m`** | Center-to-center spacing = **`2.100 m`**. |
| **Door Orientation** | **East (`+X`)** | **East (`+X`)** | Both face Nivel 0 Main Lobby (`X > +3.410`). |
| **Door Leaf Span (`Z`)** | `[5.000, 5.760]` + `[5.740, 6.500]` (`1.500 m` total, `0.020 m` overlap) | `[7.100, 7.860]` + `[7.840, 8.600]` (`1.500 m` total, `0.020 m` overlap) | Recessed at `X ∈ [3.100, 3.180]` (`0.080 m` thick), both solid `BoxCollider=True`. |
| **Door Height (`Y`)** | `[-50.000, -47.800]` (`2.200 m`) | `[-50.000, -47.800]` (`2.200 m`) | Below `FrontHeader` (`Y ∈ [-47.800, -47.150]`). |
| **Masonry Pier Clear Width** | **`1.370 m`** (`Z ∈ [5.065, 6.435]`) | **`1.370 m`** (`Z ∈ [7.165, 8.535]`) | Between `FrontPierS/Mid` & `FrontPierMid/N` (`0.260 m` thick). *(Note: prior docs cited `1.50 m` door leaf width; actual solid pier-to-pier clear opening is `1.370 m`, and steel jamb-to-jamb is `1.420 m` — both `> 2.1×` player capsule `Ø 0.640 m`.)* |
| **Internal Divider Wall** | **NONE PRESENT** (`FrontPierMid` stops at `X = +3.150`) | **NONE PRESENT** (`FrontPierMid` stops at `X = +3.150`) | **Critical discovery:** `Z = +6.800` has only the `0.260 m`-deep front pier (`X ∈ [3.150, 3.410]`). No interior wall divides `X ∈ [1.240, 3.150]` yet. |
| **Shared Interior Cavity** | `X ∈ [1.240, 3.100]` (`1.860 m` deep) × `Z ∈ [4.440, 9.160]` (`4.720 m` combined) × `Y ∈ [-50.000, -47.150]` (`2.850 m` high) | Same shared empty cavity | **Zero interior shaft geometry, zero cabins, zero rails, zero pit, zero overhead overrun.** |
| **Floor / Ceiling Slabs** | Monolithic `Backrooms_Floor` (`Y ∈ [-50.300, -50.000]`) & `Backrooms_Ceiling` (`Y ∈ [-47.150, -46.950]`) | Same monolithic slabs | **Zero vertical shaft cutout** in floor or ceiling. |
| **Runtime Scripts / Behavior** | **None (`0` scripts, `0` triggers, `0` animations)** | **None (`0` scripts, `0` triggers, `0` animations)** | Completely static, sealed architectural prop. |
| **Current Player Access** | Visible closed metal doors from Main Lobby (`X > +3.410`); **100% impassable solid collision** | Same | Zero fall hazard; permanently sealed until future authorized construction. |
| **Sealed vs. Unfinished** | Architecturally **unfinished & temporarily sealed** (empty shaft behind solid doors; `"OUT OF SERVICE"` sign) | Same | Neither bay is currently assigned a role (`UNRESOLVED`). |

---

## 4. Verified North Upper Gallery Starting Point & Lightwell Clearance *(VERIFIED + PROPOSED)*

### A. North Upper Gallery South Wall Audit (`VERIFIED EXISTING`)
- `NUGallery_WallSouth` (`GO=997392960`, `TR=997392961`): spans **`X ∈ [+0.060, +16.130]`**, `Y ∈ [-46.600, -43.550]`, `Z ∈ [+29.580, +29.800]` (`t = 0.220 m`, solid `BoxCollider=True`).
- Structural bay pilasters (`0.280 m` wide) and ceiling beams (`Y ∈ [-43.830, -43.550]`):
  - Bay 0 (`X ∈ [-4.460, +0.060]`): Occupied by **07.19 Courtyard Lightwell Overlook** (`X ∈ [-4.680, +0.280], Z ∈ [+27.200, +29.580]`) — **100% PROTECTED**.
  - Bay 1 (`X ∈ [+0.340, +4.860]`): Directly North of `NorthWing_Lightwell_SkylightCap` (`X_max = +5.300`) — blocked by Lightwell.
  - **Bay 2 (`X ∈ [+5.140, +9.660]`, `4.520 m` clear span between `NUGallery_PilasterSouth_2` `X_max = +5.140` and `NUGallery_PilasterSouth_3` `X_min = +9.660`):**
    - Completely clear of all utility strips (`NUGallery_StripTube_2` ends `X = +4.460`; `NUGallery_StripTube_3` starts `X = +11.140`).
    - Directly opposite `NUGallery_SealedNorthBay_02` (weathered plaster infill centered at `X = +7.400`, `X ∈ [+6.620, +8.180]`) on the North wall!
    - Clear of **07.19 Overlook** by **`+5.720 m`** (`+6.000 - +0.280`) and clear of **07.21 East Overhang** (`X ≥ +15.870`) by **`+7.470 m`** (`+15.870 - +8.400`).
- **Proposed Gallery Departure Portal (`PROPOSED`):**
  - Opening inside Bay 2 at **`X ∈ [+6.000, +8.400]`** (`2.400 m` clear width, centered `X = +7.200`; or `2.200 m` compact `[+6.000, +8.200]`), **`Y ∈ [-46.400, -44.160]` (`2.240 m` clear height)** — matching the `2.240 m` vertical portal language of 07.17 and 07.21, with a `0.610 m` concrete transom (`Y ∈ [-44.160, -43.550]`), `0.200 m` subfloor kick (`Y ∈ [-46.600, -46.400]`), and concrete jamb reveals.
  - Margins to existing pilasters: **`+0.860 m`** East of `PilasterSouth_2` (`+5.140`) and **`+1.260 m`** West of `PilasterSouth_3` (`+9.660`) — preserving all 5 gallery pilasters, all 5 ceiling beams, and all 4 utility strips verbatim!
  - Preserves `NUGallery_WallSouth` (`GO=997392960`) in-place trimmed as the West pier (`X ∈ [+0.060, +6.000]`), adding an East pier (`X ∈ [+8.400, +16.130]`) — following the exact proven in-place trim pattern from 07.17, 07.19, and 07.21.

### B. East Side of the Lightwell Clearance Analysis (`VERIFIED + PROPOSED`)
Verified serialized Lightwell East bounds (`Z ∈ [+17.700, +26.300]`):
- `NorthWing_Lightwell_WallE` (`GO=894939850`): **`X ∈ [+4.980, +5.180]`**, `Y ∈ [-47.150, -44.800]`, `Z ∈ [+17.840, +26.160]`.
- `NorthWing_Lightwell_SkylightCap` (`GO=1795628428`): **`X ∈ [-5.300, +5.300]`**, `Y ∈ [-44.800, -44.640]`, `Z ∈ [+17.700, +26.300]`.

With the candidate East-side corridor clear interior **`X ∈ [+6.000, +8.400]` (`2.400 m` wide)** and standard **`0.220 m` solid concrete walls** (`West wall X ∈ [+5.780, +6.000]`, `East wall X ∈ [+8.400, +8.620]`):

| Clearance Measure | Verified Value | Assessment |
| :--- | :---: | :--- |
| Corridor West wall **outer face** (`+5.780`) to `SkylightCap` outer edge (`+5.300`) | **`+0.480 m`** | **PASS** — clean air gap; zero contact, zero Z-fighting, zero slab overhang conflict. |
| Corridor West wall **outer face** (`+5.780`) to `Lightwell_WallE` outer face (`+5.180`) | **`+0.600 m`** | **PASS** — ample structural clearance; zero Lightwell edits. |
| Corridor West wall with thicker `0.260 m` structural wall (`+5.740`) to `SkylightCap` | **`+0.440 m`** | **PASS** — still well clear even with heavier construction. |
| Player capsule (`r_eff = 0.320 m`) minimum center `X` (`+6.320`) to `SkylightCap` (`+5.300`) | **`+1.020 m`** (`+0.700 m` capsule edge) | **PASS** — physically isolated behind solid `0.220 m` concrete wall; zero collision/visibility hazard. |
| Upper slab bottom (`Y = -46.600`) to `NorthWing_CeilingSlab_East` top (`Y = -46.948`) | **`+0.348 m`** | **PASS** — uniform structural plenum along entire Lightwell East flank. |

**Conclusion:** The `0.700 m` nominal offset between `X = +6.000` and `SkylightCap` (`+5.300`) **is verified sufficient** once wall thickness (`0.220 m`) is deducted, leaving a real **`+0.480 m`** solid-to-solid gap. The corridor fits cleanly at **`2.400 m`** (or `2.200 m`) clear width without pinching or needing local bypasses around the Lightwell.

---

## 5. Lower-Level Spatial & Structural Audit Beneath Candidate Route *(VERIFIED EXISTING)*

Mapping every serialized lower-level structure beneath **`X ∈ [+5.78, +8.62], Z ∈ [+4.20, +29.58]`** (`Y ∈ [-50.30, -46.94]`):

| Z Segment (m) | Lower-Level Space (`VERIFIED EXISTING`) | Lower Ceiling Slab & Top `Y` | Vertical Plenum to `Y = -46.600` | Load-Bearing Walls / Beams Directly Beneath Corridor | Conflicts Above `Y = -46.948` |
| :--- | :--- | :--- | :---: | :--- | :---: |
| **`+28.80 → +29.58`** | **Room 108** (`FoyerNib` `X=6.15`, `BathWallE` `X=5.40`, `CorridorWall` `Z=28.80`) | `NorthWing_CeilingSlab_North` (`GO=582163591`, `Y_top = -46.948`) | **`+0.348 m`** | `Hotel_Room108_CorridorWall` (`Z = +28.80`), `FoyerNib` (`X = +6.15`) | **ZERO** |
| **`+26.00 → +28.80`** | **North Guest Corridor** (`NorthCorrConduit` `Y=-47.25`, `FixtureStrip_NorthCorrE` `Y=-47.16`) | `NorthWing_CeilingSlab_North` (`Y_top = -46.948`) | **`+0.348 m`** | `NorthWing_NorthCorridor_Beam_0` (`X ∈ [8.06, 8.34]`, `Z ∈ [26.00, 28.80]`) | **ZERO** |
| **`+18.00 → +26.00`** | **East Arcade** (`X ∈ [5.12, 8.09]`) & **Banquet Hall West Wall** (`X ∈ [8.09, 8.31]`) | `NorthWing_CeilingSlab_East` (`GO=1047710948`, `X ∈ [5.00, 16.30]`, `Y_top = -46.948`) | **`+0.348 m`** | **Continuous structural bearing!** `EastArcade_Beam_1..3` (`X ∈ [5.00, 8.20]`) + `Banquet_WestWallS/Mid/N` (`X = +8.20`) + `Courtyard_CornerPier_1/3` (`X = +4.85`) | **ZERO** *(all fixtures `Y ≤ -47.16`)* |
| **`+12.20 → +18.00`** | **South Promenade & Banquet SW** (`EastArcade_Beam_0` `Z=15.00`, `Banquet_WestWallS` `X=8.20`, `LobbyNibPier` `X=3.60`) | `NorthWing_CeilingSlab_South` (`GO=766823023`, `Y_top = -46.948`) | **`+0.348 m`** | `Banquet_WestWallS` (`X = +8.20`) + `EastArcade_Beam_0` (`Z = +15.00`) + `SouthPromenade_Beam_2` (`X = +3.60`) | **ZERO** |
| **`+11.82 → +12.20`** | **Connection A Portal & Perimeter Spine** (`ConnA_ArchPierW/E` `X=3.70/7.10`, `PerimeterSeal_East` `Z=12.00`, `UnfinishedProjectPlan` `Z=11.83`) | Overlap seam: `Backrooms_Ceiling` (`Z_max=12.30, Y_top=-46.950`) & `NorthWing_CeilingSlab_South` (`Z_min=12.20, Y_top=-46.948`) | **`+0.348 m–0.350 m`** | **Primary E–W bearing spine!** `NorthWing_PerimeterSeal_Mid/East` & `ConnA_ArchPierW/E` (`Z = +12.00`) | **ZERO** |
| **`+9.40 → +11.82`** | **Main Lobby North Apron & Reception West Edge** (`Reception_PilasterW` `X=7.40`, `CanopySoffit` `X ≥ 7.23`, `DiarioJuarezIpad` `X=4.50, Z=8.80`, `FluorPanel_2` `Y_max=-47.14`) | `Backrooms_Ceiling` (`GO=322800857`, `X ∈ [-20.30, +20.30], Z ∈ [-12.30, +12.30], Y_top = -46.950`) | **`+0.350 m`** | `Hotel_Reception_PilasterW` (`X ∈ [7.23, 7.57]`), `Hotel_Lobby_Beam_West` (`X ∈ [3.25, 3.55]`) | **ZERO** *(rebar stubs `Y ≤ -47.53`)* |
| **`+4.20 → +9.40`** | **Elevator Waiting Lobby (`X ∈ [3.41, 5.80]`) & Open Lobby (`X ∈ [5.80, 9.50]`)** (`ElevatorLobby_Soffit` `Y=-47.51..-47.15`, `CrookedCeilingTile` `X=[8.48, 9.28], Y_max=-47.07`, `MissingTileVoid` `Y_max=-47.15`) | `Backrooms_Ceiling` (`Y_top = -46.950`) | **`+0.350 m`** | `Hotel_Elevator_Core` East bearing piers (`X = +3.28`) + `Hotel_Lobby_Beam_West` (`X = +3.40`) | **ZERO** *(`CrookedCeilingTile` top `-47.07` is `0.47 m` below upper slab!)* |

**Structural Conclusion:** A continuous `0.200 m`-thick upper concrete floor slab (`Y ∈ [-46.600, -46.400]`) can be built across the **entire** East approach, Transition Vestibule, and Upper Waiting Area with **zero lower-level modifications**, **zero fixture collisions**, and **authentic vertical load-path continuity** directly above Nivel 0's East Arcade, Banquet Hall West wall (`X = +8.20`), Connection A spine (`Z = +12.00`), and Elevator Core piers (`X = +3.28`).

---

## 6. Critical 3D Audit — Corridor-to-Core Connection & Resolution of the `0.20 m` Overlap *(VERIFIED + PROPOSED)*

### A. Exact Proof That the Raw Nominal Coordinates Fail (`VERIFIED MATH`)
Testing the previously projected volumes at their shared boundary plane **`Z = +9.400 m`**:
- **Candidate East Corridor clear interior:** `X ∈ [+6.000, +8.400]`, `Z ∈ [+9.400, +29.580]` (with `0.220 m` concrete walls at `X ∈ [+5.780, +6.000]` and `[+8.400, +8.620]`).
- **Previously projected Upper Waiting Area clear interior:** `X ∈ [+3.410, +6.200]`, `Z ∈ [+4.200, +9.400]` (with `0.220 m` East wall at `X ∈ [+6.200, +6.420]`).
- **Common clear X interval at `Z = +9.400`:**
  $$[+6.000, +8.400] \cap [+3.410, +6.200] = [+6.000, +6.200] \implies \Delta X = \mathbf{0.200\text{ m}}$$
- **Effect of wall reveals / trim (`0.040 m` each side):** Effective clear width drops to **`0.120 m`**.
- **Player capsule comparison (`Ø = 0.640 m`):** `0.200 m / 0.640 m = 0.3125×` — **COMPLETELY IMPASSABLE.**
- **Why shifting the corridor West does NOT solve it:** Aligning a `2.400 m` corridor with `X ∈ [+3.410, +6.200]` requires `X_min = +3.800`, which would plow **`1.500 m` straight through `NorthWing_Lightwell_SkylightCap` (`X_max = +5.300`)** across `Z ∈ [+17.700, +26.300]` — violating the protected Lightwell!

### B. Minimum Clean 3D Resolution (`PROPOSED` — Transition Vestibule at `Z ∈ [+9.400, +11.800]`)
South of the Lightwell (`Z < +17.700`), the entire upper plane `X ∈ [+3.410, +8.620]` sits over open Nivel 0 South Promenade & Main Lobby ceilings (`Y_top = -46.948 / -46.950 m`). The minimum non-bloated geometric solution is to terminate the East-side corridor at **`Z = +11.800 m`** (directly over Nivel 0's `Z = +12.00` Connection A structural wall) and bridge the `2.200 m` Westward offset with a **Compact Transition Vestibule**:

- **Transition Vestibule clear bounds (`PROPOSED`):**
  - **`X ∈ [+3.410, +8.400]` (`4.990 m` E–W)** × **`Z ∈ [+9.400, +11.800]` (`2.400 m` N–S)** × **`Y ∈ [-46.400, -43.550]` (`2.850 m` clear height)** → **`11.98 m²`** walkable area.
  - North portal from corridor (`Z = +11.800`): **`X ∈ [+6.000, +8.400]` = `2.400 m` clear width** (`3.75×` capsule `Ø 0.640 m`).
  - Turning zone inside vestibule: **`2.400 m × 2.400 m`** clear turning squares at both ends (`3.75×` capsule).
  - South portal into Upper Elevator Waiting Area (`Z = +9.400`): **`X ∈ [+3.650, +6.050]` = `2.400 m` framed portal** (or full `2.790 m` open span `X ∈ [+3.410, +6.200]`) — **`3.75×–4.36×` player capsule diameter!**
  - Zero penetration of `Hotel_Elevator_Core` (`X ≤ +3.410, Z ∈ [+4.200, +9.400]`): Vestibule West wall sits at `X ∈ [+3.190, +3.410], Z ∈ [+9.400, +11.800]` (cleanly North of `Hotel_ElevatorCore_NorthWall` `Z_max = +9.400`), aligning flush with the upper projection of the Elevator Core's front face (`X = +3.410`)!

---

## 7. Evaluation of Three Route Alternatives *(PROPOSED)*

### Alternative A — East-Side Direct Gallery Corridor + Extended Waiting Hall
- **Concept:** Straight `20.180 m` corridor (`X ∈ [+6.000, +8.400], Z ∈ [+9.400, +29.580]`) entering an Eastward-widened Upper Elevator Waiting Hall (`X ∈ [+3.410, +8.400], Z ∈ [+4.200, +9.400]`) so the `Z = +9.400` connection stays a full `2.400 m` flush opening with zero dogleg.
- **Exact Proposed Bounds:**
  - Corridor: `X ∈ [+6.000, +8.400]`, `Z ∈ [+9.400, +29.580]`, `Y_floor = -46.400`, `Y_ceil = -43.550` (`48.43 m²`).
  - Extended Waiting Hall: `X ∈ [+3.410, +8.400]`, `Z ∈ [+4.200, +9.400]`, `Y_floor = -46.400`, `Y_ceil = -43.550` (`25.95 m²`).
  - **Total Walkable Area:** **`74.38 m²`**.
- **Assessment:** Simplest box geometry and zero protected-object conflicts, **but** expands the waiting area to `4.99 m × 5.20 m` (`25.95 m²`) — over twice Nivel 0's `2.40 m`-deep `Hotel_ElevatorLobby_Soffit` — leaving a wide empty SE pocket (`X ∈ [+6.20, +8.40], Z ∈ [+4.20, +9.40]`) that dilutes the tight architectural framing of the twin elevator doors.

### Alternative B — Offset East-Side Approach & Compact Transition Vestibule *(RECOMMENDED)*
- **Concept:** Straight `17.780 m` East-side corridor (`X ∈ [+6.000, +8.400], Z ∈ [+11.800, +29.580]`, or `2.20 m` compact `[+6.000, +8.200]`) terminating directly above Nivel 0's `Z = +12.00` Connection A perimeter wall, turning West through a compact `2.40 m`-deep **Transition Vestibule** (`X ∈ [+3.410, +8.400], Z ∈ [+9.400, +11.800]`), and arriving through a `2.400 m` framed portal into a focused **Upper Elevator Waiting Area** (`X ∈ [+3.410, +6.200], Z ∈ [+4.200, +9.400]`) matching Nivel 0's exact waiting-bay proportions.
- **Exact Proposed Bounds (Standard `2.40 m` / Compact `2.20 m`):**
  - Gallery Portal (`Z = +29.580`): `X ∈ [+6.000, +8.400]` (`2.400 m × 2.240 m`; compact `2.200 m × 2.240 m`).
  - East Corridor: `X ∈ [+6.000, +8.400]`, `Z ∈ [+11.800, +29.580]` (`42.67 m²`; compact `39.56 m²`).
  - Transition Vestibule: `X ∈ [+3.410, +8.400]`, `Z ∈ [+9.400, +11.800]` (`11.98 m²`; compact `10.54 m²`).
  - Upper Waiting Area: `X ∈ [+3.410, +6.200]`, `Z ∈ [+4.200, +9.400]` (`14.51 m²`, `2.790 m × 5.200 m`).
  - Slab & Height: Continuous `0.200 m` floor slab (`Y ∈ [-46.600, -46.400]`), ceiling soffit `Y = -43.550 m` (`2.850 m` headroom; optional subtle `-0.250 m` dropped concrete soffit `Y = -43.800 m` inside the Waiting Area echoing Nivel 0's `Hotel_ElevatorLobby_Soffit`!).
  - **Total Walkable Area:** **`69.16 m²`** (`64.60 m²` compact).
- **Assessment:** Smallest coherent traversable route; resolves the `0.200 m` mismatch cleanly; zero wasted empty hall area; natural 2-beat architectural reveal (raw concrete approach → structural seam at `Z = +12.00` → older/quieter elevator waiting bay); zero edits to any protected 07.15/07.19/07.21/Lightwell/Core object.

### Alternative C — West-Side Lightwell Bypass & South Promenade Link
- **Concept:** Departs West of the 07.19 Overlook (`X ∈ [-8.400, -6.000], Z = +29.580`), travels South along the West side of the Lightwell (`+0.480 m` West of `SkylightCap` `X_min = -5.300`) above Nivel 0's West Arcade/Housekeeping (`Z = +29.580 → +11.800`), turns East South of the Lightwell (`Z ∈ [+11.800, +14.200], X ∈ [-6.000, +6.200]`), and enters the Upper Waiting Area (`X ∈ [+3.410, +6.200], Z ∈ [+4.200, +9.400]`).
- **Exact Proposed Bounds:**
  - West Spine: `X ∈ [-8.400, -6.000], Z ∈ [+11.800, +29.580]` (`42.67 m²`).
  - South-of-Lightwell Cross-Corridor: `X ∈ [-6.000, +6.200], Z ∈ [+11.800, +14.200]` (`29.28 m²`).
  - Waiting Approach Stub + Waiting Area: `X ∈ [+3.410, +6.200], Z ∈ [+4.200, +11.800]` (`21.21 m²`).
  - **Total Walkable Area:** **`93.16 m²`**.
- **Assessment (NOT RECOMMENDED):**
  1. **Directly conflicts with protected 07.19 geometry:** `Overlook_WallSouth_WestSegment` (`GO=1352730105`, `X ∈ [-9.710, -4.460]`) is owned by `Hotel_CourtyardLightwellOverlook` (and the only other West departure cuts protected 07.15 `NWStair_UpperShell_WallSouth` and relocates `ConduitVertical`).
  2. **Overbuilt & indirect:** `~93.16 m²` (`+35%–44%` larger than Alternative B), `~32.4 m` walking distance, three 90° turns, and wraps around the solid `BackWall` (`X = +1.00`) of the Elevator Core just to reach its East-facing doors (`X = +3.41`).

### Comparative Matrix *(PROPOSED)*

| Criterion | Alt A — Direct East + Extended Hall | Alt B — East + Compact Vestibule *(RECOMMENDED)* | Alt C — West-Side Lightwell Bypass |
| :--- | :---: | :---: | :---: |
| **Gallery Opening** | Bay 2 (`X ∈ [+6.00, +8.40]`, `NUGallery_WallSouth`) | **Bay 2 (`X ∈ [+6.00, +8.40]`, `NUGallery_WallSouth`)** | Cuts protected 07.19 `Overlook_WallSouth_WestSegment` |
| **Lightwell Clearance (outer wall to Cap)** | `+0.480 m` (East) | **`+0.480 m` (East)** | `+0.480 m` (West) + `+3.50 m` (South) |
| **Resolves `0.200 m` Overlap** | Yes (widens waiting hall East to `+8.40`) | **Yes (`2.40 m` Vestibule + `2.40 m` South portal)** | Yes (enters from North after long East leg) |
| **Walkable Area (`ESTIMATED`)** | `74.38 m²` (`25.95 m²` oversized hall) | **`69.16 m²` (`64.60 m²` at `2.20 m` width)** | `93.16 m²` (excessive corridor length) |
| **Lower Structural Conflicts** | Zero (`+0.348–0.350 m` plenum) | **Zero (`+0.348–0.350 m` plenum; aligned on `Z=12` spine)** | Zero (`+0.348–0.350 m` plenum) |
| **Protected 07.15/07.19/07.21 Impact** | Zero | **Zero** | Violates 07.19 (or 07.15) shell |
| **Architectural Coherence** | Good (but SE corner of hall feels empty) | **Highest (staged seam + focused hotel elevator bay)** | Low–Moderate (long detour around back of core) |
| **Construction Complexity** | Low | **Low–Moderate (clean orthogonal slabs)** | High (3 legs, 3 corners, 07.19 cross-edit) |
| **Overall Rank** | **#2** | **#1 — RECOMMENDED** | **#3 — Rejected** |

---

## 8. Elevator Bay Assignment Comparison *(UNRESOLVED — NO ROLE ASSIGNED)*

Both bays inside `Hotel_Elevator_Core` remain **100% protected, sealed, and unassigned**. Entering the proposed Upper Elevator Waiting Area (`X ∈ [+3.410, +6.200], Z ∈ [+4.200, +9.400]`) from the North (`Z = +9.400`), the player sees **Bay 2 (North, `Z = +7.850`, `2.24 m` away, `99.6%` fog clarity) FIRST**, and **Bay 1 (South, `Z = +5.750`, `3.99 m` away, `98.8%` fog clarity) SECOND**.

| Dimension | Configuration 1 — Bay 1 Passenger / Bay 2 Unfinished *(RECOMMENDED)* | Configuration 2 — Bay 2 Passenger / Bay 1 Unfinished |
| :--- | :--- | :--- |
| **Upper Waiting Area Discovery Beat** | Player enters at `Z = +9.40`, passes the **unfinished/barricaded Bay 2 first (`2.24 m`)**, then discovers the **intact Bay 1 passenger door (`3.99 m`)** anchored at the far end of the waiting bay. Strongest two-beat architectural tension. | Player enters at `Z = +9.40` and reaches the **intact Bay 2 passenger door immediately (`2.24 m`)**, with the **unfinished Bay 1 shaft** tucked safely at the South dead-end (`3.99 m`). Cleanest utilitarian flow. |
| **Nivel 0 Lobby Alignment** | **Bay 1 (`Z = +5.75`)** faces the Main Lobby heart & South Guest Corridor (`101–106`). **Bay 2 (`Z = +7.85`)** sits directly beside `Hotel_Elec_ElevatorJunctionBox` (`Z = +9.05`), `ArchiveCase06` luggage stack, and the North Wing seam — making exposed construction cabling & unfinished masonry feel organically placed. | **Bay 2 (`Z = +7.85`)** aligns closely with `Backrooms_SpawnPoint (17.8, 8.8)` (`14.65 m`, `84.5%` fog clarity) and Connection A (`Z = +12.00`). **Bay 1 (`Z = +5.75`)** puts an unfinished barricaded bay right beside the busier Main Lobby ↔ Guest Corridor turn. |
| **Safety Around Unfinished Bay** | Players walking past Bay 2 on the Upper Stratum need a **flush, solid recessed barricade / sealed frame** (zero protrusion into the `2.79 m` waiting zone, zero snagging). | Unfinished Bay 1 sits at the South dead-end of the Upper Waiting Area (players never need to walk past it to board Bay 2), though Nivel 0 lobby corner traffic still requires solid barriers. |
| **Central Divider Impact** | Identical: both configurations require adding an internal **`0.240 m` concrete shaft divider at `Z = +6.800`** (`Z ∈ [+6.680, +6.920]`, `X ∈ [+1.240, +3.150]`) in a future `07.24/07.25+` increment before either door opens. | Identical. |
| **Symmetric Shaft Cavity** | Divided Bay 1 shaft: `Z ∈ [+4.440, +6.680]` (`2.240 m` wide × `1.860 m` deep); door center `+5.750` is `+0.190 m` N of shaft center `+5.560` (`0.625 m` S pocket / `0.245 m` N pocket). | Divided Bay 2 shaft: `Z ∈ [+6.920, +9.160]` (`2.240 m` wide × `1.860 m` deep); door center `+7.850` is `-0.190 m` S of shaft center `+8.040` (exact mirror!). |

**Tentative Recommendation (`UNRESOLVED`):** **Configuration 1 (`Bay 1 Passenger / Bay 2 Unfinished`)** offers superior cross-floor architectural coherence (aligning Bay 2's unfinished state with Nivel 0's exposed electrical junction/conduit corner at `Z = +9.05` and staging a compelling two-beat reveal on the Upper Stratum). **Configuration 2 (`Bay 2 Passenger / Bay 1 Unfinished`)** remains an entirely viable utilitarian alternative if the creator prefers shortest walk distance and dead-end placement of the unfinished bay on the Upper Stratum. **Neither is approved yet.**

---

## 9. Upper Elevator Landing Design *(PROPOSED — WITHOUT MODIFYING CORE)*

- **Shared Waiting Area vs. Split Vestibules:** A **single shared Upper Elevator Waiting Area** (`X ∈ [+3.410, +6.200], Z ∈ [+4.200, +9.400]`, **`2.790 m` E–W depth × `5.200 m` N–S width = `14.51 m²`**) is dramatically more coherent than two split vestibules — it mirrors Nivel 0's open twin-bay waiting apron (`Hotel_ElevatorLobby_Soffit` `2.40 m × 5.40 m`), keeps **both** elevator bays visibly side-by-side so their contrast reads instantly, and provides generous `2.790 m` standing/turning clearance (`4.36×` player capsule diameter) directly in front of both `1.370 m` openings.
- **Material Progression (Hybrid Concrete-to-Older-Plaster):**
  - *East Approach Corridor (`Z = +29.58 → +11.80`):* Unfinished cast-concrete gallery continuation (`WallConcrete` + `FloorConcrete` + rhythmic concrete pilasters/beams every `~4.4 m`).
  - *Transition Vestibule (`Z = +11.80 → +9.40`):* Dark-steel architectural seam plate + slightly compressed concrete threshold over Nivel 0's `Z = +12.00` spine, shifting subtly into older weathered plaster (`PlasterWall`) on the waiting-area walls while keeping raw `ColumnConcrete` around the elevator shaft piers and a subtle `-0.250 m` dropped waiting soffit (`Y = -43.800 m`, `2.600 m` headroom).
- **Safe Representation of Sealed / Unfinished Bays (Zero Signage/Numbers Yet):**
  - In **Phase 1 (07.23)**, the West wall of the Upper Waiting Area (`X = +3.410, Z ∈ [+4.200, +9.400]`) can be built either as a **100% solid unfinished elevator core wall with two recessed, sealed concrete/steel door-bay frames** aligned directly above Bay 1 (`Z = +5.750`) and Bay 2 (`Z = +7.850`) — keeping **100% of Nivel 0's `Hotel_Elevator_Core` untouched** until Phase 2 (07.24) applies the creator-approved Bay 1 vs. Bay 2 differentiation!
  - Even in Phase 2 (07.24), any unfinished bay must remain **physically enclosed** (e.g., recessed solid masonry/boarded-concrete infill or laminated safety glass / solid steel barricade bars with aligned `BoxCollider`s — **never** an open unprotected shaft drop).
  - **Zero room numbers, floor numbers, or new signage** until separately approved.

---

## 10. Vertical Elevator Shaft Analysis & Future Functionality *(VERIFIED + PROPOSED)*

- **Verified Existing Shaft Extent:** Strictly **`Y ∈ [-50.000, -47.150]` (`2.850 m`)** — bounded by `Backrooms_Floor` below (`Y_top = -50.000`) and monolithic `Backrooms_Ceiling` (`GO=322800857`, `40.6 m × 24.6 m`) above (`Y_bot = -47.150`).
- **Existing Obstructions / Cabins:**
  - **Zero elevator cabins** exist inside `X ∈ [+1.240, +3.100]`.
  - **Monolithic `Backrooms_Ceiling` (`Y ∈ [-47.150, -46.950]`) completely blocks vertical continuity** between Nivel 0 and the Upper Stratum (`Y = -46.400`).
  - **Zero vertical divider wall** currently separates Bay 1 and Bay 2 behind `FrontPierMid`.
- **Geometric Feasibility for Future Vertical Travel (`07.25+` — NOT AUTHORIZED YET):**
  - Floor-to-floor vertical travel is exactly **`3.600 m`** (`Y = -50.000 m → -46.400 m`).
  - Available per-bay clear shaft footprint once the `0.240 m` divider at `Z = +6.800` is added: **`1.860 m` (X) × `2.240 m` (Z) = `4.17 m²`** — comfortably accommodates a realistic hotel passenger cab (`~1.60 m × 1.85 m` interior × `2.20 m` height, fitting cleanly inside both Nivel 0's `2.85 m` ceiling and Upper Stratum's `2.85 m` ceiling with a modest `~0.65 m` overhead shaft overrun above `Y = -43.55 m`!).
  - **Prerequisites for any future `07.25+` functional elevator:**
    1. Split monolithic `Backrooms_Ceiling` (`GO=322800857`) into 4 perimeter slabs around the shaft cutout (`X ∈ [+1.240, +3.150], Z ∈ [+4.440, +9.160]`).
    2. Extend shaft perimeter walls + internal `Z = +6.800` divider vertically from `Y = -50.000` through `Y = -43.350+` (plus sealed overrun cap).
    3. Author dedicated `ElevatorController` runtime script (explicit player-triggered door & travel controls — **never** auto-abducting, zero fall gaps, `CharacterController` safe vertical transport, and strictly preserving Threshold of Trust as sole surface exit).
  - **Deferred completely from 07.22 / 07.23 / 07.24.**

---

## 11. Traversal, Accessibility & Safety Verification *(VERIFIED + PROPOSED)*

Directly verified from `BorderCorridorScene.unity` (`CharacterController` `fileID=1986755685`, `Camera` `GO=1370291563` / `TR=1370291564`) and `FirstPersonPlayer.cs` (`L682–685`):
- **Height:** `1.800 m` (`center = (0, 0.900, 0)`) | **Radius:** `0.280 m` | **Skin width:** `0.040 m` | **Effective radius:** **`0.320 m`** (**`Ø = 0.640 m`**) | **Step offset:** `0.300 m` | **Slope limit:** `45°` | **Camera local height:** `1.600 m`.

| Safety / Accessibility Metric | Proposed Value (Alt B) | Ratio vs. Player Capsule | Status |
| :--- | :---: | :---: | :---: |
| **Gallery Departure Portal Width × Height** | `2.400 m × 2.240 m` (compact `2.200 m`) | `3.75×` width / `1.24×` height | **PASS** |
| **East Approach Corridor Width** | `2.400 m` (compact `2.200 m`) | `3.75×` (`3.44×` compact) | **PASS** |
| **Transition Vestibule Turning Squares** | `2.400 m × 2.400 m` (compact `2.200 m`) | `3.75×` | **PASS** |
| **Vestibule → Waiting Area Portal** | `2.400 m × 2.240 m` | `3.75×` width | **PASS** |
| **Waiting Area Approach Depth (in front of doors)** | `2.790 m × 5.200 m` | `4.36×` depth | **PASS** |
| **Headroom (Corridor / Vestibule / Waiting Soffit)** | `2.850 m` / `2.850 m` / `2.600 m` | `1.58×` / `1.58×` / `1.44×` | **PASS** |
| **Level Elevation / Stairs / Ramps / Jumps** | Continuous level `Y = -46.400 m` (`0` stairs, `0` jumps) | — | **PASS** |
| **Unprotected Shaft Exposure** | **Zero** (both upper bays sealed/enclosed; solid colliders) | — | **PASS** |
| **Bidirectional Return Route** | Waiting Area → Vestibule → East Corridor → North Upper Gallery → 07.15 NW Staircase → Nivel 0 → Room 111-B → **Threshold of Trust** | 100% reversible; zero softlock | **PASS** |

---

## 12. Architectural & Narrative Direction *(PROPOSED — RESTRAINED ANOMALY)*

Keeping strictly within Remanencia's grounded *Hotel de las Palomas* architectural identity (zero horror clichés, zero supernatural particles, zero Cobalt-60 claims, zero invented lore):
1. **Structural Seam Over Connection A (`Z = +12.00`):** As the player walks South from the unfinished raw-concrete North Upper Gallery past the sealed Lightwell flank, crossing `Z = +11.800` into the Transition Vestibule introduces a flush dark-steel floor reveal and subtle finish shift — echoing Nivel 0's boundary between the newer North Wing extension and the original South Lobby core.
2. **Older Waiting Area Contrast:** The Upper Elevator Waiting Area feels slightly more finished and older than the raw-concrete approach corridor (weathered `PlasterWall`, dropped concrete soffit echoing Nivel 0's `Hotel_ElevatorLobby_Soffit`, recessed metal elevator surrounds), silently implying the elevator core was built for an upper floor that was never completed around it.
3. **Subtle Bay Asymmetry (Pending Phase 2 Approval):** Once Bay 1 vs. Bay 2 is assigned, one bay can present intact, dark, waiting stainless doors while the other reveals recessed unfinished masonry/steel framing — communicating interrupted construction through pure architectural detail without a single word of exposition.

---

## 13. Lighting & Performance Assessment *(VERIFIED + PROPOSED)*

- **Verified Baseline:** Exact **`18` Unity `Light` components (`LightShadows.None`)** across `BackroomsLevel1Scene.unity`. The nearest existing point light to the upper waiting area is `Backrooms_FluorLight_2` (`GO=1863074836`, pos `(4.50, -47.46, 8.80)`, range `10.2 m`, intensity `1.12`) — because `LightShadows.None` is active on all 18 lights, soft indirect light bleed from `Backrooms_FluorLight_2` (at `1.06 m` below the upper waiting floor!) will gently reach the upper waiting area, while the `17.78 m` East approach corridor (`Z = +11.80 → +29.58`) sits outside all 18 lower point-light ranges (relying on flat `RemanenciaLevel0AmbientLight (0.29, 0.31, 0.33)` + player flashlight `[F]`).
- **Default Policy (`0` Added Unity `Light`s — Strict `18`-Light Budget):**
  - Place **3–4 restrained emissive-only ceiling utility strips** (`Remanencia_Level0_UtilityWarmOn.mat`) along the East approach corridor (`Z ≈ +25.0, +19.5, +14.0`), `1` in the Transition Vestibule (`X = +5.9, Z = +10.6`), and `1` recessed under the Waiting Area soffit (`X = +4.8, Z = +6.8`).
  - **Honest Rendering Clarification:** Emissive-only materials (`UtilityWarmOn`) **do NOT cast real dynamic light** onto surrounding walls/floors in URP without baked GI; they provide crisp visual spatial guidance and depth landmarks, while actual surface illumination comes from flat `RemanenciaLevel0AmbientLight`, `Backrooms_FluorLight_2` bleed near the elevator waiting area, and player flashlight `[F]`.
  - **Optional Separate Future Approval (`UNRESOLVED`):** If live PlayMode inspection after 07.23 shows the `17.78 m` East corridor reads too dim without flashlight `[F]`, request explicit separate creator approval for `1` low-intensity non-shadow point light (`LightShadows.None`, range `9 m`) in the Transition Vestibule (increasing scene light count from `18` → `19`). Do **not** add any light without explicit approval.

---

## 14. Recommended Approach, Section 15 Checklist & Unresolved Creator Decisions

### Recommended Direction Summary *(PROPOSED — NOT YET AUTHORIZED)*
1. **Leaving North Upper Gallery:** Through a new `2.400 m × 2.240 m` framed South opening in Bay 2 of `NUGallery_WallSouth` (`X ∈ [+6.000, +8.400], Z = +29.580`, cleanly between `PilasterSouth_2` and `PilasterSouth_3`, opposite `SealedNorthBay_02`).
2. **Side of Lightwell:** **East side**, with `+0.480 m` hard outer-wall clearance from `NorthWing_Lightwell_SkylightCap` (`X_max = +5.300`) and `+0.600 m` from `NorthWing_Lightwell_WallE` (`X_max = +5.180`).
3. **Lower-Level Support:** Continuous `0.200 m` concrete floor slab (`Y ∈ [-46.600, -46.400]`) across a uniform `+0.348 m–0.350 m` structural plenum directly over Nivel 0's East Arcade, Banquet Hall West wall (`X = +8.20`), Connection A bearing wall (`Z = +12.00`), and Main Lobby ceiling (`0` lower-level conflicts).
4. **Transition into Waiting Area:** Terminates East corridor at `Z = +11.800` and bridges the `2.200 m` Westward offset via a compact **Transition Vestibule (`X ∈ [+3.410, +8.400], Z ∈ [+9.400, +11.800]`, `11.98 m²`)**, resolving the impassable `0.200 m` raw overlap.
5. **Final Connection Width:** **`2.400 m` clear portal** at `Z = +9.400` (`X ∈ [+3.650, +6.050]`, `3.75×` player capsule diameter).
6. **Elevator Bay Alignment:** Entering from the North (`Z = +9.400`), **Bay 2 (`Z = +7.850`, `2.24 m`)** is encountered first and **Bay 1 (`Z = +5.750`, `3.99 m`)** sits deeper South. Recommended tentative assignment: **Configuration 1 (`Bay 1 Passenger / Bay 2 Unfinished`)** for cross-floor coherence and two-beat upper discovery (with Configuration 2 documented as a clean utilitarian alternative).
7. **Unfinished Bay Treatment:** Remains safely enclosed/sealed behind solid recessed masonry/barricade colliders (zero unprotected shaft drop).
8. **What the Player Sees in Waiting Area:** A quiet `2.79 m × 5.20 m` hybrid concrete/plaster landing with dropped soffit and two side-by-side elevator door bays vertically aligned above Nivel 0's `Hotel_Elevator_Core`.
9. **Safe Return:** Walk back North through Vestibule → East Corridor → North Upper Gallery → 07.15 NW Staircase → Nivel 0 → Room 111-B → **Threshold of Trust**.
10. **Inaccessible Spaces:** Both elevator shafts (sealed), Courtyard Lightwell, exterior void beyond East Overhang, and all upper guest-room interiors.
11. **Deferred:** Internal shaft divider wall, `Backrooms_Ceiling` cutout, elevator cabins, runtime `ElevatorController`, floor buttons/UI, signage/numbering, and any new Unity `Light` components.

### Unresolved Creator Decisions Required Before 07.23 (`UNRESOLVED`)
1. **Corridor Route Selection:** Approve **Alternative B — East-Side Approach + Compact Transition Vestibule** (`2.40 m` standard `69.16 m²` vs. `2.20 m` compact `64.60 m²`), OR select Alternative A (`74.38 m²` extended hall).
2. **Waiting-Area Configuration & Materiality:** Confirm shared `2.79 m × 5.20 m` waiting area (`X ∈ [+3.410, +6.200], Z ∈ [+4.200, +9.400]`) with concrete-to-older-plaster transition and dropped waiting soffit (`Y = -43.800 m`).
3. **Elevator Bay Assignment (`Configuration 1` vs. `Configuration 2`):** Select **Configuration 1 (`Bay 1 South = Passenger, Bay 2 North = Unfinished`)** vs. **Configuration 2 (`Bay 2 North = Passenger, Bay 1 South = Unfinished`)**, or defer bay role selection until after 07.23 PlayMode walk-through.
4. **Unfinished Bay Architectural Treatment:** Confirm recessed solid concrete/steel barricade vs. framed safety-glass peep into an incomplete shaft (both 100% solid-collider enclosed).
5. **Lighting Policy:** Confirm keeping exact **`18` Unity `Light` components** (emissive-only utility strips + flat ambient + flashlight `[F]`), deferring any light-budget expansion until PlayMode review.
6. **Future Functional Elevator Scope (`07.25+`):** Confirm whether an interactive moving elevator between Nivel 0 and Upper Stratum is desired in a later phase, or whether the upper landing should remain purely architectural indefinitely.

---

## 15. Suggested Phased Implementation Roadmap *(PROPOSED — NOT IMPLEMENTED)*

| Phase | Scope | Protected Guardrails |
| :---: | :--- | :--- |
| **07.23** | **Upper Elevator Approach Corridor & Transition Vestibule (Sealed Landing Shell)** via dedicated idempotent `UpperElevatorApproachBuilder.cs` under `Backrooms_Architecture/Hotel_UpperElevatorApproach` + `RemanenciaUpperElevatorApproachTests.cs` (with full `CharacterController.Move()` walk & 4-builder cross-idempotency). Opens Bay 2 of `NUGallery_WallSouth`, builds East Approach Corridor (`Z = +29.58 → +11.80`), Transition Vestibule (`Z = +11.80 → +9.40`), and sealed Upper Waiting Area shell (`X ∈ [+3.41, +6.20], Z ∈ [+4.20, +9.40]`) with two recessed **sealed** upper elevator bay frames. | **Zero edits** to `Hotel_Elevator_Core` (`35` children byte-identical), `Backrooms_Ceiling`, Lightwell, 07.15/07.19/07.21, Room 111-B, Threshold of Trust, exact `18` lights, or `BorderCorridorScene.unity`. |
| **07.24** | **Creator-Approved Elevator Bay Architectural Treatment (Static, Non-Functional)** — Applies approved Configuration 1 vs. 2 visual differentiation on the Upper Waiting Area (and optionally matching subtle cues on Nivel 0 if approved), installs the internal `0.240 m` shaft divider at `Z = +6.800` if opening a recessed unfinished view, and keeps both bays 100% fall-safe. | Requires explicit creator approval of Configuration 1 vs. 2 and unfinished-bay treatment. Zero moving mechanics yet. |
| **07.25+** | **Functional Passenger Elevator System (Optional Future Track)** — Separate design & engineering increment splitting monolithic `Backrooms_Ceiling` around the shaft cutout, extending shaft masonry vertically, building cabin + bi-parting doors + player-triggered `ElevatorController` + EditMode/PlayMode vertical transit verification. | Requires separate design approval; Threshold of Trust remains sole surface exit. |

---

## 16. Mandatory Protected Architecture & Systems Inventory *(INVARIANT)*

The following remain **100% untouched** during 07.22 and strictly protected across future increments:
- **Upper Stratum:** Complete 07.15 `Hotel_NorthwestServiceStaircase`, complete 07.17 `Hotel_NorthUpperGallery` (except the single future authorized Bay 2 South wall opening in 07.23, preserving all 5 pilasters, 5 beams, and 4 strips), complete 07.19 `Hotel_CourtyardLightwellOverlook` (including `Overlook_WallSouth_WestSegment` and `Remanencia_Level0_OverlookSafetyGlass.mat`), and complete 07.21 `Hotel_EastOverhang`.
- **Central Courtyard Lightwell:** All 10 `NorthWing_Lightwell_*` objects (`WallN/S/E/W`, `SkylightCap`, `ConcreteBeam_0..2`, `SteelPurlinN/S`).
- **Twin Elevator Core:** Complete `Hotel_Elevator_Core` (`GO=331299473`, all `35` children), `Hotel_Sign_ElevatorsHeader`, `Hotel_Sign_ElevatorNotice`, `Hotel_Elec_ElevatorJunctionBox/ConduitRun`, `Backrooms_Floor`, and `Backrooms_Ceiling`.
- **Nivel 0 & Progression:** Room 111-B (`Hotel_Room111B_Anomaly`, `Hotel_Room111B_GuestInterior`), **Threshold of Trust** (`"threshold-of-trust"`), Rooms `101–110`, Room 110 ecological Zone D, all 07.6–07.9 narrative/ecology/microfauna elements, entry-only `Backrooms_ExitDoor_ToStreet`, both surface entry routes, save schema (`schemaVersion == 1`), journal, exact **`18` Unity `Light` components (`LightShadows.None`)**, `SecretBackroomsTransitionController.cs`, and `BorderCorridorScene.unity` (`SHA-256: 0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B`).
