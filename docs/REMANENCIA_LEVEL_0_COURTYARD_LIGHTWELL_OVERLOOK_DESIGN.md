# REMANENCIA — Nivel 0: Courtyard Lightwell Overlook — Forensic Sightline, Spatial Feasibility & Architectural Design (07.18)

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia** *(overarching dimensional universe — proper name, untranslated in ES/EN)*  
**Territory:** **Nivel 0 — Hotel de las Palomas** (`unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`)  
**Baseline Audited:** Branch `product/unity-fellowship-2027`, HEAD `5c27e1fac28a8ba8081b0d18c071c304a8a4a9e5` (`5c27e1f feat(remanencia): add north upper gallery`; clean working tree synchronized with `origin/product/unity-fellowship-2027`)  
**Document Status:** **IMPLEMENTATION 07.18 — READ-ONLY FORENSIC SIGHTLINE, SPATIAL FEASIBILITY & ARCHITECTURAL DESIGN (ZERO UNITY SCENE, BUILDER, SCRIPT, MATERIAL, TEST, OR EXISTING-DOC MODIFICATIONS)**

> **Mandatory Measurement & Status Labels:**  
> Every coordinate, dimension, optical metric, and architectural element in this document is explicitly tagged as:
> - **`VERIFIED EXISTING`** — Directly extracted from serialized YAML (`BackroomsLevel1Scene.unity` `SHA-256: DF82FC0902DE44F9C1D80251085EF27144F2CACBDB8CB9B10631D20AC5F8B621`, `BorderCorridorScene.unity` `SHA-256: 0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B`, `.mat` assets, `.png` textures, and C# source files) at `5c27e1f`.
> - **`PROPOSED`** — Concrete geometric target recommended or evaluated for future authorized `07.19+` implementation; **not built**.
> - **`ESTIMATED`** — Projected area, contrast ratio, lighting coverage, or construction effort pending live Unity PlayMode inspection.
> - **`UNRESOLVED`** — Open creative, lighting, or framing decision requiring explicit creator approval before any `07.19` construction begins.

---

## 1. Executive Design Intent *(VERIFIED + PROPOSED)*

Implementations **07.15** (Northwest Service Staircase) and **07.17** (North Upper Gallery) established the first physically traversable upper-level route in **Nivel 0 — Hotel de las Palomas**: a `3.600 m` concrete switchback ascent from `Y = -50.000 m` to `Y = -46.400 m`, followed by a `25.360 m × 3.000 m` unfinished concrete gallery terminating at a sealed eastern wall (`X = +16.000 m`).

The next planned feature under approved **Alternative 1** is the **Courtyard Lightwell Overlook** — intended to let the player discover Nivel 0's existing Central Courtyard Lightwell protruding upward into the Upper Stratum without modifying any protected Lightwell object.

This read-only forensic investigation audited every serialized transform, mesh bound, collider, material shader property, light range, and first-person camera parameter in the actual Unity project and ran dense 3D ray-box intersection simulations across `377` active upper/north `MeshRenderer` AABBs. The investigation uncovered **four decisive geometric realities** that reshape the original 07.16 sketch:

1. **The Eye-Height Ceiling Trap (`CRITICAL — VERIFIED EXISTING`):** On the flat Upper Stratum floor (`Y = -46.400 m`), the player's actual runtime standing eye elevation is **`Y_eye = -44.760 m`** (`-46.400 + 0.040 skinWidth + 1.600 PlayerCamera.localPosition.y`; nominal `-44.800 m`), with **zero jump** and **zero crouch** in `FirstPersonPlayer.cs`. Because `NorthWing_Lightwell_WallN` rises solidly to **`Y = -44.800 m`** and `NorthWing_Lightwell_SkylightCap` sits at **`Y ∈ [-44.800, -44.640]`**, the Skylight Cap top surface (`-44.640 m`) is **`+0.120 m` ABOVE the player's eye**. From **any** position on a flat `Y = -46.400 m` floor, **`0.00%` of the Skylight Cap's luminous `10.60 m × 8.60 m` (`91.16 m²`) upper diffuser surface is visible** — the player sees only a `1.60 m`-tall blank concrete wall identical in material (`Backrooms_YellowWallpaper.mat`) to the gallery walls, topped by a thin `0.16 m` fascia edge (`2.7°–5.4°` tall).
2. **Opaque Emissive Roof, Not a Transparent Atrium (`VERIFIED EXISTING`):** `Remanencia_Level0_LightwellDiffuser.mat` is **`_Surface = 0` (100% Opaque URP Lit)** with cool-overcast self-emission (`_EmissionColor = (0.368, 0.414, 0.472)`). It transmits **zero** visual sightlines down into the Central Courtyard (`Y = -50.000 m`) and emits **zero** real Unity illumination. Therefore, the Lightwell cannot be perceived as a conventional open atrium looking down onto a lower floor; it must be composed as a **luminous exterior courtyard roof trapped indoors inside an enclosed upper hotel level**.
3. **Mandatory Upper Chamber Enclosure (`CRITICAL — VERIFIED EXISTING`):** Zero upper-level walls or ceilings exist South of `NUGallery_WallSouth` (`Z < +29.580 m`, `Y > -46.948 m`). Cutting any opening in `NUGallery_WallSouth` without enclosing the inaccessible space above and around the Lightwell exposes **`33%–48%` unbuilt scene fog void** above `Y = -44.640 m` and the raw top faces of Nivel 0's lower ceiling slabs (`Y = -46.948 m`).
4. **The Compact Elevated Framed Solution (`RECOMMENDED — PROPOSED`):** Rather than building the wide flat `9.20 m × 3.40 m` (`31.28 m²`) 07.16 balcony with its problematic `0.10 m` cap gap, **Alternative C (Compact Single-Bay Framed Architectural Observation with a `+0.540 m` 3-Step Viewing Dais)** opens **only one existing structural bay** (`X ∈ [-4.460, +0.060]`, `4.520 m` wide — removing **zero** gallery pilasters and **zero** ceiling beams), steps up `3` poured-concrete risers (`3 × 0.180 m = +0.540 m`, matching 07.15's exact stair module) to **`Y_dais = -45.860 m` (`Y_eye = -44.220 m`, `+0.420 m` above the Skylight Cap)**, holds the observation parapet cleanly **`1.100 m` North of `SkylightCap`** (`Z = +27.400 m`), and frames an enclosed upper skylight chamber (`0.00%` unbuilt void) revealing **`18.5%–20.0%` of the viewport as the luminous `Y+` roof plane** (`26.7%` total glowing skylight) plus **both** the Northwest and Northeast skylight corners — while leaving every existing `NorthWing_Lightwell_*` object **100% untouched**.

---

## 2. Verified Existing Geometry, Bounds & Collision Inventory *(VERIFIED EXISTING)*

All values below were extracted directly from `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity` (`1,423` GameObjects, `18` `Light` components, `SHA-256: DF82FC0902DE44F9C1D80251085EF27144F2CACBDB8CB9B10631D20AC5F8B621`). Every structural primitive below uses Unity built-in `Cube` (`fileID: 10202`, unit bounds `[-0.5, +0.5]³`) with identity rotation, so world `MeshRenderer.bounds` equal `BoxCollider.bounds` (when present) exactly.

### A. Central Courtyard Lightwell & Skylight Cap (`NorthWing_Courtyard`)

| Serialized GameObject | `fileID` (GO / TF / BoxCol) | World Pos `(X, Y, Z)` | Scale `(X, Y, Z)` | Verified World AABB `(X / Y / Z)` | Collider (`isTrigger=0`) | Material (`VERIFIED EXISTING`) |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| `NorthWing_Lightwell_WallN` | `585720393` / `585720394` / `585720396` | `(0.000, -45.975, 26.080)` | `(10.320, 2.350, 0.200)` | `X ∈ [-5.160, +5.160]`<br>`Y ∈ [-47.150, -44.800]`<br>`Z ∈ [+25.980, +26.180]` | **Solid** (`enabled=1`) | `Backrooms_YellowWallpaper.mat` *(opaque raw concrete)* |
| `NorthWing_Lightwell_WallS` | `963459595` / `963459596` / `963459598` | `(0.000, -45.975, 17.920)` | `(10.320, 2.350, 0.200)` | `X ∈ [-5.160, +5.160]`<br>`Y ∈ [-47.150, -44.800]`<br>`Z ∈ [+17.820, +18.020]` | **Solid** (`enabled=1`) | `Backrooms_YellowWallpaper.mat` *(opaque raw concrete)* |
| `NorthWing_Lightwell_WallW` | `736225269` / `736225270` / `736225272` | `(-5.080, -45.975, 22.000)` | `(0.200, 2.350, 8.320)` | `X ∈ [-5.180, -4.980]`<br>`Y ∈ [-47.150, -44.800]`<br>`Z ∈ [+17.840, +26.160]` | **Solid** (`enabled=1`) | `Backrooms_YellowWallpaper.mat` *(opaque raw concrete)* |
| `NorthWing_Lightwell_WallE` | `894939850` / `894939851` / `894939853` | `(+5.080, -45.975, 22.000)` | `(0.200, 2.350, 8.320)` | `X ∈ [+4.980, +5.180]`<br>`Y ∈ [-47.150, -44.800]`<br>`Z ∈ [+17.840, +26.160]` | **Solid** (`enabled=1`) | `Backrooms_YellowWallpaper.mat` *(opaque raw concrete)* |
| `NorthWing_Lightwell_SkylightCap` | `1795628428` / `1795628429` / `1795628431` | `(0.000, -44.720, 22.000)` | `(10.600, 0.160, 8.600)` | `X ∈ [-5.300, +5.300]`<br>`Y ∈ [-44.800, -44.640]`<br>`Z ∈ [+17.700, +26.300]` | **Solid** (`enabled=1`) | `Remanencia_Level0_LightwellDiffuser.mat` *(opaque emissive)* |
| `NorthWing_Lightwell_ConcreteBeam_0/1/2` | `310453212` / `1968150993` / `645336046` | `(-2.4 / 0.0 / +2.4, -45.160, 22.000)` | `(0.300, 0.480, 8.100)` | `X ±0.15`, `Y ∈ [-45.400, -44.920]`, `Z ∈ [+17.950, +26.050]` | None | `Remanencia_Level0_ConcreteColumn.mat` *(fully internal)* |
| `NorthWing_Lightwell_SteelPurlinN/S` | `1627440425` / `635453988` | `(0.000, -44.940, 23.40 / 20.60)` | `(10.100, 0.140, 0.140)` | `X ∈ [-5.050, +5.050]`, `Y ∈ [-45.010, -44.870]` | None | `Remanencia_Level0_IndustrialMetal.mat` *(fully internal)* |

**Forensic Corrections vs. Earlier Approximate Notes:**
- The four Lightwell walls span outer perimeter **`X ∈ [-5.180, +5.180]`** and **`Z ∈ [+17.820, +26.180]`** (specifically `WallN` spans `X ∈ [-5.160, +5.160], Z ∈ [+25.980, +26.180]`, with `0.020 m` corner recess at `|X| ∈ [5.160, 5.180]`), **not** `X ∈ [-5.30, +5.30], Z ∈ [+17.70, +26.30]`.
- The wider **`X ∈ [-5.300, +5.300], Z ∈ [+17.700, +26.300]`** envelope belongs exclusively to **`NorthWing_Lightwell_SkylightCap`**, which cantilevers **`0.120 m`** outward beyond all four Lightwell walls (`26.300 - 26.180 = 0.120 m` on the North; `5.300 - 5.180 = 0.120 m` on West/East).
- Every internal beam (`ConcreteBeam_0/1/2`) and purlin (`SteelPurlinN/S`) sits strictly inside `X ∈ [-5.05, +5.05], Z ∈ [+17.95, +26.05], Y ≤ -44.870` and is **100% sealed behind opaque walls and opaque cap** — zero rays from any upper-level position can ever reach them.

### B. Existing 07.17 North Upper Gallery & Lower Ceiling Slabs (`VERIFIED EXISTING`)

| Serialized GameObject | `fileID` (GO) | Verified World AABB `(X / Y / Z)` | Collider | Role / Relevance to Overlook |
| :--- | :---: | :--- | :---: | :--- |
| `NUGallery_FloorSlab` | `906864132` | `X ∈ [-9.710, +16.130]`, `Y ∈ [-46.600, -46.400]`, `Z ∈ [+29.580, +33.020]` | Solid | Walkable top at **`Y = -46.400 m`**, South edge at **`Z = +29.580 m`**. |
| `NUGallery_CeilingSlab` | `1775212749` | `X ∈ [-9.710, +16.130]`, `Y ∈ [-43.550, -43.350]`, `Z ∈ [+29.580, +33.020]` | Solid | Soffit at **`Y = -43.550 m`** (`2.850 m` clear height above floor). |
| `NUGallery_WallSouth` | `997392960` | `X ∈ [-9.710, +16.130]`, `Y ∈ [-46.600, -43.550]`, `Z ∈ [+29.580, +29.800]` | Solid | Single monolithic `25.840 m` concrete slab (`t = 0.220 m`, inner face `Z = +29.800`, outer face `Z = +29.580`). Currently `+3.280 m` North of `SkylightCap`. |
| `NUGallery_BaseboardSouth` | `891342187` | `X ∈ [-9.490, +15.870]`, `Y ∈ [-46.400, -46.280]`, `Z ∈ [+29.799, +29.824]` | None | Continuous South baseboard along inner face (`Z = +29.800`). |
| `NUGallery_PilasterSouth_0 / 1 / 2` | `1422694127` / `519711158` / `2056127351` | `X ∈ [-4.74, -4.46]` / `[+0.06, +0.34]` / `[+4.86, +5.14]`; `Y ∈ [-46.400, -43.550]`; `Z ∈ [+29.800, +29.880]` | None | Structural concrete pilasters directly beneath `NUGallery_CeilingBeam_0/1/2` (soffit `Y = -43.830 m`). Define two clean `4.520 m`-wide structural bays (`[-4.46, +0.06]` and `[+0.34, +4.86]`) directly North of the Lightwell! |
| `NUGallery_SealedSouthRelief_L / R / Top` | `1109116082` / `1901505073` / `383381114` | `X ∈ [-1.420, +1.820]`, `Y ∈ [-46.400, -44.030]`, `Z ∈ [+29.800, +29.860]` | None | Decorative relief centered at `X = +0.200` (straddles `PilasterSouth_1` at `X = +0.200`). |
| `NUGallery_StripTube_1 / 2` | `1414982758` / `2128371101` | `X ∈ [-2.46, -1.14]` / `[+3.14, +4.46]`; `Y ∈ [-43.623, -43.588]`; `Z ∈ [+31.268, +31.332]` | None | Emissive-only warm ceiling strips (`Remanencia_Level0_UtilityWarmOn.mat`) along gallery centerline (`Z = +31.300`). |
| `NorthWing_CeilingSlab_North` | `582163591` | `X ∈ [-9.600, +16.300]`, `Y ∈ [-47.148, -46.948]`, `Z ∈ [+26.000, +34.300]` | Solid | Nivel 0 lower ceiling slab directly beneath the gallery and the entire North perimeter of the Lightwell (`+0.348 m` clean plenum below `Y = -46.600 m` floor slab underside). |
| `NorthWing_CeilingSlab_West / East / South` | `1153658111` / `1047710948` / `766823023` | `West: X ∈ [-16.3, -5.0], Z ∈ [+18, +26]`; `East: X ∈ [+5.0, +16.3], Z ∈ [+18, +26]`; `South: X ∈ [-16.3, +16.3], Z ∈ [+12.2, +18.0]`; all `Y ∈ [-47.148, -46.948]` | Solid | Nivel 0 ceilings framing the `10 m × 8 m` courtyard shaft opening; currently exposed raw slab tops above `Y = -46.948 m` if viewed from above without an upper enclosure! |

---

## 3. Verified Player Controller, Camera & Eye Position *(VERIFIED EXISTING)*

Extracted directly from `unity-client/Assets/Scenes/BorderCorridorScene.unity` and `unity-client/Assets/Scripts/FirstPersonPlayer.cs`:

| Parameter | Verified Value (`VERIFIED EXISTING`) | Source Location |
| :--- | :--- | :--- |
| **`CharacterController` GameObject** | `FirstPersonPlayer` (`GO=1986755682`, `CharCtrl=1986755685`) | `BorderCorridorScene.unity` |
| **Capsule Dimensions** | `height = 1.800 m`, `radius = 0.280 m`, `skinWidth = 0.040 m`, `center = (0, 0.900, 0)` | `BorderCorridorScene.unity` & `FirstPersonPlayer.cs:684–685` |
| **Effective Horizontal Clearance** | Effective radius **`r_eff = 0.320 m`** (`0.28 + 0.04`), effective diameter **`Ø_eff = 0.640 m`** | Derived from verified `radius + skinWidth` |
| **Step & Slope Limits** | `stepOffset = 0.300 m`, `slopeLimit = 45.0°`, grounded snap `verticalVelocity = -1.5f` | `BorderCorridorScene.unity` & `FirstPersonPlayer.cs:1132` |
| **Jump / Crouch Capability** | **NONE** — `HandleMovement()` (`FirstPersonPlayer.cs:1118–1172`) implements horizontal walk (`4.5 m/s`), sprint (`7.0 m/s`), and gravity (`18.0 m/s²`) only. No vertical jump impulse and no crouch stance exist. | `FirstPersonPlayer.cs:54–56, 1118–1172` |
| **`PlayerCamera` Transform** | `FirstPersonPlayer/PlayerCamera` (`GO=1370291563`, `Camera=1370291566`), **`localPosition = (0.0, 1.600, 0.0)`** (hard-enforced in `Awake()`!) | `BorderCorridorScene.unity` & `FirstPersonPlayer.cs:697` |
| **Camera Projection & Look Clamp** | Vertical **`FOV = 60.0°`** (`16:9` horizontal **`FOV = 91.49°`**), `nearClipPlane = 0.100 m`, `farClipPlane = 250.0 m`, pitch clamp **`±80.0°`** (`upDownLookLimit = 80.0f`) | `BorderCorridorScene.unity` & `FirstPersonPlayer.cs:61, 1187` |

### Exact Standing Eye Elevation vs. Lightwell Vertical Geometry (`VERIFIED EXISTING`)

In Unity's `CharacterController`, the bottom of the swept collision capsule sits at `transform.position.y + center.y - height/2 - skinWidth = transform.position.y + 0.900 - 0.900 - 0.040 = transform.position.y - 0.040 m`. Therefore, when standing grounded on a horizontal slab top at `Y_floor`:

- **On existing flat gallery floor (`Y_floor = -46.400 m`):**
  - Grounded pivot: `transform.position.y = -46.360 m` (nominal without `skinWidth`: `-46.400 m`).
  - **Actual runtime standing eye elevation:** **`Y_eye = -44.760 m`** (nominal floor-relative `Y_floor + 1.600 = -44.800 m`).
  - **`NorthWing_Lightwell_WallN` top (`Y = -44.800 m`):** **`-0.040 m` below runtime eye** (`0.000 m` nominal — virtually dead-level with the horizon!).
  - **`NorthWing_Lightwell_SkylightCap` bottom (`Y = -44.800 m`):** **`-0.040 m` below runtime eye** → **underside overhang (`Z ∈ [+26.18, +26.30]`) is NOT visible at runtime** (and only `0.0°` grazing at nominal `-44.800 m`).
  - **`NorthWing_Lightwell_SkylightCap` top (`Y = -44.640 m`):** **`+0.120 m` ABOVE runtime eye** (`+0.160 m` above nominal eye) → **upper luminous surface (`Y+`) is 100% mathematically invisible from ANY point on a `Y = -46.400 m` floor!**
- **On a proposed `+0.540 m` stepped viewing dais (`Y_dais = -45.860 m`, `3` risers × `0.180 m`):**
  - Grounded pivot: `transform.position.y = -45.820 m`.
  - **Runtime standing eye elevation (`PROPOSED`):** **`Y_eye = -44.220 m`** — **`+0.420 m` ABOVE `NorthWing_Lightwell_SkylightCap` top (`-44.640 m`)**, unlocking direct downward sightlines across the entire `10.60 m × 8.60 m` (`91.16 m²`) glowing skylight roof surface!

---

## 4. Three-Dimensional Visibility & Ray-Intersection Analysis *(VERIFIED SIMULATION)*

To eliminate guesswork, exact ray-AABB intersection casts were executed against all **`377` active upper/north `MeshRenderer` AABBs** extracted from `BackroomsLevel1Scene.unity`, using the exact `PlayerCamera` frustum (`vFOV = 60.0°`, `hFOV = 91.49°` at `16:9`, `81 × 45 = 3,645` rays per viewpoint for the 07.16 baseline and `41 × 23 = 943` rays per viewpoint for enclosed variants).

### A. Vertical Angular Coverage by Floor Elevation & Distance (`VERIFIED GEOMETRY`)

| Observation Condition | Eye `Y` (m) | Dist to Cap `Z=+26.30` | `WallN` Vertical Span | Cap `Z+` North Edge Span | Cap `Y+` Top Surface Visible? | Cap `Y+` Top Vertical Span |
| :--- | :---: | :---: | :---: | :---: | :---: | :---: |
| **Flat `Y=-46.400` — Gallery Center (`Z=31.30`)** | `-44.760` | `5.00 m` | `-17.76°` to `-0.45°` (`17.31°`) | `-0.46°` to `+1.37°` (**`1.83°`**) | **NO** *(eye `-0.12 m` below cap)* | **`0.00°`** (`0%`) |
| **Flat `Y=-46.400` — Overlook Entrance (`Z=29.70`)** | `-44.760` | `3.40 m` | `-24.98°` to `-0.65°` (`24.33°`) | `-0.67°` to `+2.02°` (**`2.70°`**) | **NO** *(eye `-0.12 m` below cap)* | **`0.00°`** (`0%`) |
| **Flat `Y=-46.400` — Mid-Overlook (`Z=28.00`)** | `-44.760` | `1.70 m` | `-42.02°` to `-1.26°` (`40.76°`) | `-1.35°` to `+4.04°` (**`5.39°`**) | **NO** *(eye `-0.12 m` below cap)* | **`0.00°`** (`0%`) |
| **Flat `Y=-46.400` — Near North Edge (`Z=26.90`)** | `-44.760` | `0.60 m` | `-66.30°` to `-3.18°` (`63.12°`) | `-3.81°` to `+11.31°` (`15.12°`) | **NO** *(eye `-0.12 m` below cap)* | **`0.00°`** (`0%`) |
| **Flat `Y=-46.400` — Near Edge Nominal (`Z=26.90`)** | `-44.800` | `0.60 m` | `-65.77°` to `0.00°` (`65.77°`) | `0.00°` to `+14.93°` (`14.93°`) | **NO** *(eye `-0.16 m` below cap)* | **`0.00°`** (`0%`) |
| **Stepped `+0.36 m` (2 steps, `Z=27.00`)** *(PROPOSED)* | `-44.400` | `0.70 m` | `-67.71°` to `-26.00°` (`41.70°`) | `-29.74°` to `-18.92°` (`10.82°`) | **YES** (`+0.24 m` above cap) | `-18.92°` to `-1.48°` (**`17.45°`**) |
| **Stepped `+0.54 m` (3 steps, `Z=27.72`, Rec. Alt C)** *(PROPOSED)* | `-44.220` | `1.42 m` | `-53.92°` to `-20.64°` (`33.28°`) | `-22.22°` to `-16.48°` (`5.74°`) | **YES** (`+0.42 m` above cap) | `-16.48°` to `-2.40°` (**`14.08°`**) |
| **Stepped `+0.54 m` (3 steps, `Z=27.00`)** *(PROPOSED)* | `-44.220` | `0.70 m` | `-69.39°` to `-35.27°` (`34.11°`) | `-39.64°` to `-30.96°` (`8.68°`) | **YES** (`+0.42 m` above cap) | `-30.96°` to `-2.59°` (**`28.38°`**) |
| **Stepped `+0.72 m` (4 steps, `Z=27.00`)** *(PROPOSED)* | `-44.040` | `0.70 m` | `-70.84°` to `-42.83°` (`28.01°`) | `-47.35°` to `-40.60°` (`6.75°`) | **YES** (`+0.60 m` above cap) | `-40.60°` to `-3.69°` (**`36.91°`**) |

### B. Key Optical Findings from Ray-Casting (`VERIFIED SIMULATION`)

1. **As-Built 07.17 Scene (`NUGallery_WallSouth` intact):** `0.00%` of any Lightwell wall or Skylight Cap is visible (`77.1%` `NUGallery_WallSouth`, `9.9%` `PilasterSouth_1`, `13.0%` `SealedSouthRelief`).
2. **Flat `Y = -46.400 m` Opening (`X ∈ [-4.60, +4.60]`):**
   - **Zero** rays strike `SkylightCap` `Y+` (top) or `Y-` (underside), and **zero** rays strike internal beams/purlins (`0.00%`).
   - Across positions 2–7, `NorthWing_Lightwell_WallN` (`Z+` face, same concrete material as gallery walls) occupies **`30.6%–48.9%`** of the viewport, while `SkylightCap` (`Z+` thin vertical edge only) occupies only **`4.4%–13.3%`** (rising to `22.2%` only when pressed `0.60 m` against the wall staring straight at blank concrete).
   - **Unbuilt Void Exposure:** Without a new upper enclosure shell around and above the Lightwell, **`33.3%–46.7%`** of the viewport renders raw empty fog void (`UNBUILT_VOID`), and oblique gazes (`Position 1 & 7`) expose up to **`11.8%`** raw top face of `NorthWing_CeilingSlab_North` (`Y = -46.948 m`).
3. **Raised `+0.540 m` Viewing Dais (`Y_dais = -45.860 m`, `Y_eye = -44.220 m`) + Enclosed Chamber (`Y_ceil = -42.900 m`):**
   - `SkylightCap` `Y+` luminous top surface jumps from **`0.00%`** to **`16.0%–20.0%`** of viewport at `Z = +27.80 m` (total glowing skylight **`23.5%–28.7%`**, and **`59.3%`** if closer at `Z = +27.00 m`).
   - `UNBUILT_VOID` drops to **`0.00%`**, replaced by dark unfinished upper concrete chamber ceiling (`20.7%–37.2%`) and perimeter walls (`14%–19%`), creating a stark, unmistakable luminous contrast (`~3.6×` linear radiance) of an **exterior skylight roof trapped inside a concrete upper room**.
   - **Dual-Corner Sightline Proof (Single Bay `X ∈ [-4.46, +0.06]`):** From the bay's East side (`X = -0.30, Z = 27.72`), the ray to the Northwest corner of `SkylightCap` (`X = -5.30, Z = 26.30`) crosses the parapet South plane (`Z = 27.20`) at `X = -2.131 m` (`2.33 m` clear of West jamb `-4.46`). From the bay's West side (`X = -4.10, Z = 27.72`), the ray to the Northeast corner (`X = +5.30, Z = 26.30`) crosses `Z = 27.20` at `X = -0.658 m` (`0.72 m` clear of East jamb `+0.06`). **Both corners of the `10.60 m`-wide skylight cap are visible from inside a single `4.52 m`-wide bay!**

---

## 5. Mandatory Sightline Matrix *(VERIFIED RAY-INTERSECTION RESULTS)*

### A. Evaluated Against the Original Flat 07.16 Footprint (`X ∈ [-4.60, +4.60], Z ∈ [+26.40, +29.80], Y_floor = -46.400 m, Y_eye = -44.760 m`)

| Observation Position | Lightwell Wall Visible | Skylight Cap Visible | View Type | Obstruction |
| :--- | :--- | :--- | :--- | :--- |
| **0. As-Built Gallery Center** (`X=0.00, Z=31.30`, yaw `180°` S) | **Blocked** (`0.00%`) | **Blocked** (`0.00%`) | Blocked | Intact `NUGallery_WallSouth` (`77.1%`), `PilasterSouth_1`, `SealedSouthRelief`. |
| **1. Approaching from North Upper Gallery** (`X=±6.50, Z=31.30`, yaw `135°/225°`) | **Partial** (`WallN Z+`: `2.50%`) | **Partial — Edge Only** (`Z+` fascia: `0.30%`; `Y+` top: `0.00%`) | Partial (narrow oblique sliver) | `NUGallery_WallSouth_W/E` (`50%–55%`), gallery floor/ceiling; exposes `8.2%–8.7%` unbuilt void if unenclosed. |
| **2. Standing Immediately Inside Overlook Entrance** (`X=0.00, Z=29.70`, yaw `180°` S) | **Direct** (`WallN Z+`: `48.89%`, `24.3°` V) | **Partial — Thin Edge Only** (`Z+` fascia: `4.44%`, `2.7°` V; `Y+` top: `0.00%`; underside: `0.00%`) | Partial (wall-dominated; roof invisible) | Self-occlusion by `SkylightCap` edge (`Y_eye = -44.760 < Y_cap_top = -44.640`); `46.7%` unbuilt void above cap if unenclosed. |
| **3. Standing Near Proposed Northern Observation Edge** (`X=0.00, Z=26.90`, yaw `180°` S) | **Direct** (`WallN Z+`: `44.44%`, `63.1°` V — fills lower/mid frame at `0.72 m`!) | **Partial — Steep Fascia Edge Only** (`Z+` fascia: `22.22%`, `15.1°` V; `Y+` top: `0.00%`) | Partial (claustrophobic wall face; roof invisible) | Player eye `-0.120 m` below cap top blocks `100%` of horizontal roof surface; `33.3%` unbuilt void overhead if unenclosed. |
| **4. Standing Near Western End** (`X=-4.20, Z=27.20`, yaw `150°` SSE) | **Direct** (`WallN Z+`: `46.77%`; `WallW`: `0.00%`) | **Partial — Edge Only** (`Z+` fascia: `13.33%`; `Y+` top: `0.00%`) | Partial (wall-dominated) | Eye below cap top blocks roof plane; `X = -4.20` stays inside `WallN` span `[-5.16, +5.16]`, so `WallW` is hidden; `40.0%` unbuilt void if unenclosed. |
| **5. Standing Near Eastern End** (`X=+4.20, Z=27.20`, yaw `210°` SSW) | **Direct** (`WallN Z+`: `46.77%`; `WallE`: `0.00%`) | **Partial — Edge Only** (`Z+` fascia: `13.33%`; `Y+` top: `0.00%`) | Partial (wall-dominated) | Symmetric to Pos 4: eye below cap top, `WallE` hidden inside `[-5.16, +5.16]`, `40.0%` unbuilt void if unenclosed. |
| **6. Looking Directly Toward Lightwell (Mid-Bay)** (`X=0.00, Z=28.00`, yaw `180°` S) | **Direct** (`WallN Z+`: `48.89%`, `40.8°` V) | **Partial — Edge Only** (`Z+` fascia: `6.67%`, `5.4°` V; `Y+` top: `0.00%`) | Partial (blank wall with thin blue stripe) | Eye `-0.120 m` below cap top; `WallN` shares exact concrete texture as gallery walls; `44.4%` unbuilt void if unenclosed. |
| **7. Looking Across / Along Lightwell Structure** (`X=-4.20, Z=27.00`, yaw `115°` ESE) | **Direct — Oblique** (`WallN Z+`: `30.59%`) | **Partial — Oblique Edge Only** (`Z+` fascia: `9.60%`; `Y+` top: `0.00%`) | Partial (grazing wall corridor) | Eye below cap top; exposes `11.8%` raw `NorthWing_CeilingSlab_North` top + `40.8%` unbuilt void past East end unless enclosed. |

### B. Contrast: Recommended Framed Bay with `+0.540 m` Viewing Dais & Enclosed Chamber (`Alt C`: `X ∈ [-4.46, +0.06], Z ∈ [+27.40, +29.58], Y_dais = -45.860 m, Y_eye = -44.220 m`) *(PROPOSED)*

| Observation Position (`PROPOSED`) | Lightwell Wall Visible | Skylight Cap Visible | View Type | Obstruction |
| :--- | :--- | :--- | :--- | :--- |
| **1c. Gallery Approach** (`X=-6.50, Z=31.30`, `Y_eye=-44.760`, yaw `135°`) | **Blocked** (`0.00%`) | **Partial — Edge Glint** (`Z+`: `0.53%`) | Partial teaser | `NUGallery_WallSouth_W` frames the recessed bay; `0.00%` unbuilt void. |
| **2c. Framed Dais Center — Looking South** (`X=-2.20, Z=27.80`, `Y_eye=-44.220`, yaw `170°`, pitch `+10°`) | **Direct** (`WallN Z+`: `33.09%`) | **Direct — FULL TOP PLANE + FASCIA** (`Y+` roof: **`18.45%`**; `Z+` edge: **`8.27%`**; **Total `26.72%`**) | **Direct panoramic architectural discovery** | **None on target** (parapet at `-61.5°` below eye clears cap bottom `-22.2°` completely; `0.00%` unbuilt void; enclosed concrete chamber `39.7%`). |
| **3c. Framed Dais West Corner — Looking SE** (`X=-3.80, Z=27.80`, `Y_eye=-44.220`, yaw `150°`, pitch `+10°`) | **Direct** (`WallN Z+`: `27.78%`) | **Direct — Top Plane + NE Corner** (`Y+` roof: **`15.91%`**; `Z+` edge: **`7.64%`**; **Total `23.54%`**) | **Direct diagonal architectural composition** | Parapet foreground (`9.2%`) grounds scale; `0.00%` unbuilt void; NE corner (`X=+5.30`) visible `0.72 m` inside East jamb. |
| **4c. Framed Dais East Side — Looking SW** (`X=-0.30, Z=27.72`, `Y_eye=-44.220`, yaw `220°`, pitch `+10°`) | **Direct** (`WallN Z+` + NW corner overhang) | **Direct — Top Plane + NW Corner** (`X=-5.30, Z=26.30` visible `2.33 m` clear of West jamb) | **Direct corner-reveal composition** | **None on target**; confirms the skylight is a freestanding trapped roof block inside an enclosed concrete room. |

> **Sightline Conclusion:** The original flat `Y = -46.400 m` overlook does **not** deliver a meaningful visual discovery of the Courtyard Lightwell skylight because the player's eye (`-44.760 m`) stays strictly below the cap's top (`-44.640 m`). Elevating the observation standing plane by `+0.540 m` (`3` concrete risers) and enclosing the upper skylight cavity completely resolves both defects without modifying a single Lightwell object.

---

## 6. Structural, Clearance & Spatial Feasibility Analysis *(VERIFIED + PROPOSED)*

### A. Audit of the Original 07.16 Proposed Footprint (`X ∈ [-4.60, +4.60], Z ∈ [+26.40, +29.80], Y = -46.400 m`)

| Structural / Clearance Metric | Exact Calculated Value | Feasibility Verdict |
| :--- | :--- | :--- |
| **Nominal Envelope Area** | `9.200 m × 3.400 m = 31.28 m²` (`9.200 m × 3.180 m = 29.26 m²` strictly South of `WallSouth` outer face `Z = +29.580`) | Geometrically non-overlapping, but oversized for its poor flat-floor sightline. |
| **Usable Walkable Area (with `0.18 m` parapet inside `Z ≥ 26.40`)** | `9.200 m × 3.220 m = 29.62 m²` (`Z ∈ [+26.580, +29.800]`); closest player capsule center `Z_min = 26.580 + 0.320 = +26.900 m` | Accessible (`5.0×` capsule `Ø 0.64 m`), but forces close-range wall staring (`0.72 m` from `WallN`). |
| **Horizontal Gap to `SkylightCap` (`Z_max = +26.300`)** | **`0.100 m`** (`26.400 - 26.300`) | **PRACTICALLY PROBLEMATIC (see below).** |
| **Horizontal Gap to `Lightwell_WallN` (`Z_max = +26.180`)** | **`0.220 m`** (`26.400 - 26.180`) | Creates an open `0.220 m × 0.548 m` deep subfloor micro-trench down to `NorthWing_CeilingSlab_North` (`Y = -46.948`) if slab stops at `Z = +26.400`! |
| **Floor Slab Support & Lower Plenum** | `NorthWing_CeilingSlab_North` (`Y ∈ [-47.148, -46.948]`, `X ∈ [-9.60, +16.30], Z ∈ [+26.00, +34.30]`) underlies **100%** of the envelope. A `0.200 m` upper slab (`Y ∈ [-46.600, -46.400]`) keeps an exact **`+0.348 m` structural plenum** (`Y ∈ [-46.948, -46.600]`). | **CLEAN & FEASIBLE** — zero conflict with Nivel 0 ceilings or fixtures. |
| **Gallery South Wall Opening Conflict (`VERIFIED EXISTING`)** | Opening `X ∈ [-4.60, +4.60]` splits `NUGallery_WallSouth` (`Z ∈ [+29.58, +29.80]`), **intersects `NUGallery_PilasterSouth_0` (`X ∈ [-4.74, -4.46]`)**, **deletes `NUGallery_PilasterSouth_1` (`X ∈ [+0.06, +0.34]`)** directly beneath structural `NUGallery_CeilingBeam_1` (`X = +0.20`), and removes `NUGallery_SealedSouthRelief_L/R/Top` (`X ∈ [-1.42, +1.82]`). | **HIGH CHURN** — removes a load-bearing-looking pilaster beneath an intact overhead ceiling beam! |
| **Headroom Along Route** | Gallery: `2.850 m` (`2.570 m` under `CeilingBeam_0..4` soffit `Y = -43.830 m`). Overlook at `Y = -46.400`: `2.850 m` (under `-43.550`) or `3.500 m` (under `-42.900`). Overlook at `Y_dais = -45.860`: **`2.310 m`** under `-43.550` (`+0.510 m` over `1.80 m` capsule) or **`2.960 m`** under raised `-42.900` (`+1.160 m` over capsule). | **SAFE** (`≥ 2.31 m` everywhere; `2.96 m` under stepped chamber ceiling). |

### B. Forensic Assessment of the `0.10 m` Horizontal Gap (`Z = +26.40` vs. `SkylightCap Z = +26.30`)

Two bounding boxes separated by `0.10 m` are **not** automatically safe or clean for construction:
1. **Parapet Footing Conflict:** A structurally credible concrete parapet is `0.18–0.20 m` thick. Centering it on `Z = +26.40` places its South face at `Z = +26.30..+26.31` — colliding with or Z-fighting against `SkylightCap` (`Z = +26.300`). Shifting the parapet Northward to stay clear (`Z ∈ [+26.40, +26.60]`) still leaves a cramped `0.10 m` dark vertical slot right beside the diffuser overhang where oblique camera rays (`nearClip = 0.10 m`) can visually inspect the raw seam between the parapet and `WallN`.
2. **Unsealed Subfloor Trench:** Because `WallN` sits at `Z = +26.180` while `SkylightCap` overhangs to `Z = +26.300`, stopping the floor slab at `Z = +26.400` leaves an open `0.220 m`-wide, `0.548 m`-deep chasm down to `NorthWing_CeilingSlab_North` (`Y = -46.948`). While a `Ø 0.64 m` capsule cannot fit inside `0.22 m`, any player looking down over the rail sees an unfinished drop/gap into the lower plenum.
3. **Recommended Resolution (`PROPOSED`):** Abandon the `Z = +26.40` edge entirely. Pull the observation parapet North to **`Z ∈ [+27.200, +27.400]`** (`0.900 m` clear of `SkylightCap`, `1.020 m` clear of `WallN`), and span the inaccessible `1.00 m` gap (`Z ∈ [+26.200, +27.200]`) with a passive concrete floor apron flush at `Y = -46.400 m` (`0.020 m` clear of `WallN`, zero intersection). That turns a cramped `0.10 m` collision hazard into an intentional, spacious **`1.10 m` architectural reveal moat** showing the Lightwell emerging monolithically from the Upper Stratum floor!

---

## 7. Fall Protection & Access Safety Assessment *(VERIFIED + PROPOSED)*

### A. Can the Existing Lightwell Enclosure Alone Protect the Player?

**NO — NOT BY ITSELF.** Audited collision facts:
- `NorthWing_Lightwell_WallN/S/E/W` each have solid `BoxCollider`s (`isTrigger=0`, `Y ∈ [-47.150, -44.800]`), and `NorthWing_Lightwell_SkylightCap` has a solid `BoxCollider` (`Y ∈ [-44.800, -44.640]`).
- **On a flat `Y = -46.400 m` floor:** `WallN` stands `1.600 m` tall (`>> stepOffset = 0.300 m`) and `FirstPersonPlayer` has **zero jump**, so a player cannot step *over* `WallN` directly — **HOWEVER**:
  1. **Side Bypass Hazard:** `WallN` ends at `X = ±5.160 m`. Any overlook wider than `X = ±5.00 m` or any offset alcove (Alternative B) exposes open upper space beside `WallW` (`X < -5.18`) or `WallE` (`X > +5.18`), where a player could walk straight off the upper floor slab onto `NorthWing_CeilingSlab_West/East` (`Y = -46.948 m`, a `-0.548 m` drop that exceeds `stepOffset = 0.300 m` — creating a **permanent softlock trap** unable to climb back up without jumping!!).
  2. **Subfloor Seam & Overhang Wedging:** Approaching within `0.10 m` of `SkylightCap` (`Y = -44.800`) puts the player's `1.80 m` capsule top (`-46.360 + 1.800 = -44.560 m`) **`+0.240 m` ABOVE the cap's underside** (`-44.800 m`), risking capsule-head snagging against the `0.12 m` `SkylightCap` overhang lip if no guardrail holds the player back!
  3. **On a Raised `+0.540 m` Viewing Dais (`Y = -45.860 m`):** `WallN` top (`-44.800 m`) is `1.060 m` above the dais (still `> stepOffset 0.300 m`), and `SkylightCap` top (`-44.640 m`) is `1.220 m` above the dais. Even without a jump button, future controller updates or edge-pushing must NEVER rely solely on the Lightwell itself as the player boundary.

### B. Evaluated Protection Solutions

| Protection Option | Compatibility with `CharacterController` & Existing Geometry | Verdict |
| :--- | :--- | :---: |
| **1. Rely solely on `Lightwell_WallN`** | Unsafe: risks capsule head snagging on `SkylightCap` `0.12 m` overhang (`Y = -44.80` vs capsule top `-44.56`), fails if player moves laterally past `X = ±5.16`, and exposes `0.22 m` subfloor trench. | **REJECTED** |
| **2. Upper-level concrete parapet (`1.05 m`) ONLY** | Blocks `stepOffset = 0.30 m` cleanly under current no-jump movement, **but** would become vulnerable if a future increment ever enables jumping/mantling, and leaves an uncontained visual/physics boundary into the skylight chamber. | **INSUFFICIENT ALONE** |
| **3. Recessed Observation Bay + Solid `1.05 m` Concrete Parapet + Dark-Steel Horizontal Viewing Frame + Full-Height Invisible Solid Upper `BoxCollider`** | **100% physically airtight.** Solid concrete parapet (`Z ∈ [+27.20, +27.40], Y ∈ [-46.60, -44.81]`) + `IndustrialMetal` cap rail & restrained horizontal steel transoms + **invisible solid `BoxCollider` (`MeshRenderer` disabled, `isTrigger=0`)** spanning `Y ∈ [-44.81, -42.90]` between West and East bay walls. Zero Lightwell edits; zero climb/vault/fall/escape vectors even if jumping were ever added! | **RECOMMENDED** |
| **4. Solid glass pane across opening** | Visually redundant and risky on low-VRAM URP (transparent overdraw + alpha sorting artifacts against `ExponentialSquared` fog; no existing clean glass-pane shader in Nivel 0 besides `EntranceOneWayGlass`). Open architectural concrete + steel framing with invisible upper collider matches Nivel 0's unfinished hotel identity cleanly with zero transparency cost. | **NOT NEEDED** |

---

## 8. Evaluation of Three Architectural Alternatives *(PROPOSED)*

All coordinates below are explicitly **`PROPOSED`** and **`ESTIMATED`**; none are built. Every alternative leaves all `NorthWing_Lightwell_*` objects **100% untouched** and includes a passive enclosing upper concrete chamber (`Y_ceil = -42.900 m`) so `UNBUILT_VOID = 0.00%`.

### Alternative A — North Perimeter Overlook *(Original 07.16 Envelope, Revised for Enclosure)*

- **Concept:** Opens a wide `9.20 m` breach in `NUGallery_WallSouth` directly into a full-width northern observation terrace running parallel to `NorthWing_Lightwell_WallN`. Evaluated in both its original flat form (`Y = -46.400 m`) and a stepped variant (`+0.540 m`).
- **Proposed Dimensions & Coordinates (`PROPOSED`):**
  - Walkable footprint: `X ∈ [-4.600, +4.600]` (`9.200 m`), `Z ∈ [+26.580, +29.800]` (`3.220 m` clear depth inside a `0.18 m` parapet at `Z ∈ [+26.40, +26.58]`, OR pulled back to `Z ∈ [+27.40, +29.80]` if stepped `+0.54 m`).
  - Floor: `Y = -46.400 m` (flat) or `-45.860 m` (stepped).
  - Enclosing skylight chamber: `X ∈ [-6.20, +6.20], Z ∈ [+17.00, +29.58], Y_ceil = -42.900 m`.
- **Required Modifications to 07.17 Gallery (`PROPOSED`):**
  - Split `NUGallery_WallSouth` across `X ∈ [-4.60, +4.60]` (`9.20 m` removed — `36.3%` of gallery South wall).
  - Split `NUGallery_BaseboardSouth`.
  - **Delete `NUGallery_PilasterSouth_1` (`X = +0.20`)** right under intact `NUGallery_CeilingBeam_1`, trim/adjust `NUGallery_PilasterSouth_0` (`X = -4.60`), and remove `NUGallery_SealedSouthRelief_L/R/Top`.
- **Available Walkable Area (`ESTIMATED`):** `29.62 m²` (flat) or `22.08 m²` (if pulled back to `Z = +27.40`).
- **Sightline Quality:**
  - *Flat (`Y = -46.400 m`):* **POOR** — `0.00%` of `SkylightCap` top plane visible; `44%–49%` blank concrete wall (`WallN`).
  - *Stepped (`+0.540 m`):* **Strong** (`43%–59%` cap visibility at `Z = +27.00`), but requires a `9.20 m`-wide staircase/dais occupying nearly a third of the gallery's length.
- **Required Collision & Fall Protection:** `9.20 m` concrete parapet + steel rail + full-height solid invisible collider + sealed East/West bay return walls (`X = ±4.60`) + sealed subfloor apron across the `0.22 m` trench.
- **Construction Complexity:** **Moderate–High** (wide structural breach, removes central gallery pilaster beneath an overhead beam, wide multi-step run if elevated, largest new slab/wall count).
- **Architectural Coherence:** Feels more like a finished public observation deck than an accidental/interrupted unfinished hotel construction fragment; removing `PilasterSouth_1` while leaving `CeilingBeam_1` floating at `X = +0.20` weakens structural credibility.
- **Risks & Limitations:** Flat version fails visually; `0.10 m` cap gap causes parapet-clipping/trench issues unless pulled back; disrupts 07.17's clean `4.80 m` pilaster rhythm.
- **Future Expansion Implications:** Consumes both central bays (`[-4.60, +4.60]`), leaving the East corridor (`X > +5.00`) still free for a future SE link (`X ∈ [+6.0, +8.4]`) toward the Elevator Core.

---

### Alternative B — Offset Observation Alcove *(West NW-Corner vs. East NE-Corner)*

- **Concept:** Preserves the central gallery wall and opens a narrower side alcove strictly **West (`X ∈ [-8.20, -4.74]`)** or **East (`X ∈ [+5.14, +8.60]`)** of `NorthWing_Lightwell_WallW/E` (`X = ±5.18`), extending slightly South past `Z = +26.30` so the player views **two perpendicular sides** (`WallN + WallW` or `WallN + WallE`) and the corner overhang of `SkylightCap` diagonally.
- **Proposed Dimensions & Coordinates (`PROPOSED`):**
  - *West Candidate (evaluated primary):* Gallery opening `X ∈ [-8.200, -4.740]` (`3.460 m` wide — cleanly West of `PilasterSouth_0` at `X = -4.60`!), walkable alcove `X ∈ [-8.200, -5.500]` (`2.700 m` clear width clear of `SkylightCap` `X = -5.30`), extending South to `Z = +25.600 m` (depth `4.20 m` South of gallery inner face `Z = +29.80`).
  - *East Candidate:* Mirror at `X ∈ [+5.500, +8.600]` (NOTE: directly overlaps the 07.16-reserved future SE Elevator Core corridor anchor at `X ∈ [+6.00, +8.40], Z = +26.40`!).
  - Enclosing skylight chamber: `X ∈ [-8.20, +6.20]` (West) or `[-6.20, +8.60]` (East), `Z ∈ [+17.00, +29.58]`, `Y_ceil = -42.900 m`.
- **Required Modifications to 07.17 Gallery (`PROPOSED`):**
  - Split `NUGallery_WallSouth` & `BaseboardSouth` across `X ∈ [-8.20, -4.74]` (`3.46 m` wide).
  - Preserves **all 5** `NUGallery_PilasterSouth_0..4` AND `NUGallery_SealedSouthRelief_L/R/Top` untouched!
- **Available Walkable Area (`ESTIMATED`):** `~11.8 m²` (`2.70 m × 4.20 m` plus entry shoulder).
- **Sightline Quality:**
  - *Flat (`Y = -46.400 m`):* **Weak–Moderate** — uniquely reveals **two walls** (`WallN` `11.2%` + `WallW` `23.2%` = `35.6%`) establishing 3D box volume, **but** `SkylightCap` is still edge-only (`5.62%`: `X-` `4.03%` + `Z+` `1.59%`, `Y+` top = **`0.00%`**).
  - *Stepped (`+0.540 m` at `X = -6.60, Z = +26.80`):* **Moderate** — `SkylightCap` reaches `15.59%` (`Y+` top `10.39%` + `X-` `3.61%` + `Z+` `1.59%`) plus `26.9%` dual Lightwell walls, **less than half** the roof-plane clarity of a North-facing elevated frame because viewing from the corner increases oblique foreshortening and requires a larger enclosing chamber (`14.4 m` wide vs `12.4 m`).
- **Required Collision & Fall Protection:** **Two-sided L-shaped containment** (both East parapet along `X = -5.50` AND South parapet along `Z = +25.60`, plus L-shaped full-height invisible upper colliders) to prevent walking off onto `NorthWing_CeilingSlab_West` (`Y = -46.948 m`)!
- **Construction Complexity:** **Moderate** (L-shaped floor slab, L-shaped parapet + double-axis containment colliders, wider skylight chamber enclosure).
- **Architectural Coherence:** Good 3D corner readability, though placing it at `X = -8.20..-4.74` puts the discovery almost immediately (`1.3 m`) after entering the gallery from the NW Staircase before the corridor rhythm even establishes itself; placing it on the East (`X = +5.5..+8.6`) collides with the future SE Elevator link!
- **Risks & Limitations:** Higher softlock/fall-edge complexity (two open drop edges over `NorthWing_CeilingSlab_West`); weaker luminous skylight impact (`10.4%` vs `18.5%` top plane); East placement blocks future Elevator corridor; West placement feels crowded against the staircase portal (`X = -9.49`).
- **Future Expansion Implications:** West alcove leaves the East SE Elevator anchor (`X = +6.0..+8.4`) open, but complicates any future West service loop (07.16 Alt 3).

---

### Alternative C — Framed Architectural Observation *(Compact Single-Bay Recessed Alcove with `+0.540 m` Raised Viewing Dais)* — **RECOMMENDED**

- **Concept:** Preserves **`82.2%`** of `NUGallery_WallSouth` and **100%** of the gallery's structural pilaster/beam rhythm by opening **exactly one existing structural bay** (`X ∈ [-4.460, +0.060]`, `4.520 m` wide, neatly framed between `NUGallery_PilasterSouth_0` and `NUGallery_PilasterSouth_1`). Inside this compact `2.180 m`-deep unfinished-concrete architectural alcove (`Z ∈ [+27.400, +29.580]`), **three poured-concrete risers** (`3 × 0.180 m = +0.540 m`, exact 07.15 staircase module) lift the player smoothly to a guarded viewing dais at **`Y = -45.860 m`** (`Y_eye = -44.220 m`, **`+0.420 m` above `SkylightCap`**). Looking through a dark-steel & poured-concrete architectural frame across a `1.100 m` unwalkable concrete reveal gap, the player discovers the **entire glowing cool-overcast Courtyard Skylight roof (`Y+` surface + `Z+` edge + both NW and NE corners)** protruding through the upper concrete floor inside a quiet, enclosed unfinished concrete chamber.
- **Proposed Dimensions & Coordinates (`PROPOSED`):**
  - **Gallery South Wall Opening:** `X ∈ [-4.460, +0.060]` (**`4.520 m` clear width**), `Y ∈ [-46.400, -43.550]` (`2.850 m` tall; overhead `NUGallery_CeilingBeam_0/1` untouched at `X = -4.60` and `+0.20`).
  - **3-Step Concrete Ascent (`Z ∈ [+28.740, +29.580]`):** `3` visual concrete risers × `0.180 m` height, `2` intermediate treads × `0.280 m` depth (`0.840 m` tangent ramp collider slope = `32.735° < 45°`, identical to 07.15 stair traversal!).
  - **Level Raised Viewing Dais (`Z ∈ [+27.400, +28.740]`):** `X ∈ [-4.460, +0.060]` (`4.520 m × 1.340 m`), top floor **`Y = -45.860 m`**, clear headroom **`2.960 m`** under raised alcove/chamber ceiling `Y = -42.900 m` (`2.310 m` at gallery lintel threshold `Z = +29.580`).
  - **Guard Parapet & Architectural Frame (`Z ∈ [+27.200, +27.400]`):** Solid concrete parapet `Y ∈ [-46.600, -44.810]` (`1.050 m` above dais) + `IndustrialMetal` top rail (`Y = -44.785`) + 2 slim vertical steel mullions & upper horizontal transom (`Y = -43.550`) + **full-height invisible solid `BoxCollider`** (`X ∈ [-4.46, +0.06], Y ∈ [-44.81, -42.90], Z ∈ [+27.20, +27.40]`).
  - **Enclosed Inaccessible Skylight Chamber (`PROPOSED`):** Passive concrete walls at `X = -6.20` (W), `X = +6.20` (E), `Z = +17.00` (S), ceiling at `Y = -42.900 m` (`Z ∈ [+17.00, +29.58]`), and 4 non-overlapping passive concrete floor apron strips at `Y = -46.400 m` outside `X ∈ [-5.20, +5.20], Z ∈ [+17.80, +26.20]` (`≥ 0.020 m` clear of all `NorthWing_Lightwell_Wall*` colliders; `+0.348 m` clear above Nivel 0 ceiling slabs).
- **Required Modifications to 07.17 Gallery (`PROPOSED`):**
  - Split `NUGallery_WallSouth` cleanly into West segment (`X ∈ [-9.710, -4.460]`) and East segment (`X ∈ [+0.060, +16.130]`). *(Retains `NUGallery_WallSouth` GO in-place as one segment for clean `fileID` continuity!)*
  - Split `NUGallery_BaseboardSouth` around `X ∈ [-4.460, +0.060]`.
  - Remove `NUGallery_SealedSouthRelief_L/R/Top` (`X ∈ [-1.42, +1.82]`, replaced by the real framed bay opening).
  - **ZERO pilasters removed** (`PilasterSouth_0..4` 100% preserved!), **ZERO ceiling beams modified** (`CeilingBeam_0..4` 100% preserved!), **ZERO emissive ceiling strips moved** (`StripTube_0..3` 100% preserved!).
- **Available Walkable Area (`ESTIMATED`):** **`9.85 m²`** total (`3.80 m²` 3-step ascent + `6.06 m²` level viewing dais) — **`67%` smaller** than Alternative A (`29.62 m²`), honoring minimal-construction discipline.
- **Sightline Quality:** **HIGHEST** — `SkylightCap` = **`26.72%`** of viewport (`18.45%` glowing `Y+` roof plane + `8.27%` `Z+` fascia), `WallN` = `33.09%`, enclosed dark concrete chamber = `39.7%`, `UNBUILT_VOID` = **`0.00%`**, and **both NW (`X = -5.30`) and NE (`X = +5.30`) skylight corners are visible** by stepping left/right inside the `4.52 m` bay!
- **Required Collision & Fall Protection:** Single straight South edge (`Z = +27.40`, `4.52 m` long) protected by `1.05 m` solid concrete parapet + full-height invisible containment `BoxCollider` + solid East/West alcove sidewalls (`X = -4.46`, `X = +0.06`), sitting **`1.100 m` clear North of `SkylightCap` (`1.420 m` from player capsule center)**. Zero fall, climb, snag, or softlock paths.
- **Construction Complexity:** **Lowest** among effective designs (single straight opening between existing pilasters, 3 modular concrete steps reusing 07.15's exact `PlaceSlopedRampBox` pattern, simple orthogonal chamber enclosure).
- **Architectural Coherence:** **Highest** — reads unmistakably as an interrupted upper-hotel concrete alcove / unfinished service observation bay where the building's own courtyard roof protrudes impossibly into an enclosed upper room.
- **Risks & Limitations:** Requires updating the targeted `NUGallery_WallSouth` continuity assertion in `RemanenciaNorthUpperGalleryTests.cs` (specifically lines `126–143`), and requires PlayMode verification of lighting contrast under the `18`-light budget. *(Note: a pure window-only aperture at `Z = +29.58` without the `2.18 m` recess was also simulated — at flat `Y = -46.400`, `SkylightCap` `Y+` is `0.00%`, and even with a `+0.54 m` gallery-side step at `Z = +30.20`, `SkylightCap` `Y+` drops to `5.73%` while obstructing the `3.00 m` gallery walkway; the shallow `2.18 m` recessed dais bay solves both without encroaching a single millimeter into the `3.00 m` gallery corridor!)*
- **Future Expansion Implications:** Leaves the entire East half of `NUGallery_WallSouth` (`X ∈ [+0.06, +16.13]`, including the future SE Elevator link zone `X ∈ [+6.00, +8.40]`), the sealed East Terminal (`X = +16.00`, future East Overhang), and the entire West end (`X ∈ [-9.49, -4.46]`) **100% open and unencumbered**.

### Comparative Matrix (`PROPOSED`)

| Criterion | Alt A — North Perimeter Overlook (Flat / Stepped) | Alt B — Offset Corner Alcove (West) | **Alt C — Framed Architectural Observation (`+0.54 m` Dais)** *(RECOMMENDED)* |
| :--- | :---: | :---: | :---: |
| **Gallery Wall Opened (`X`)** | `[-4.60, +4.60]` (`9.20 m`, `36.3%`) | `[-8.20, -4.74]` (`3.46 m`, `13.6%`) | **`[-4.46, +0.06]` (`4.52 m`, `17.8%`)** |
| **Gallery Pilasters / Beams Removed** | Removes `PilasterSouth_1` + clips `PilasterSouth_0` | **Zero** | **Zero** (framed cleanly between `PilasterSouth_0` & `1`) |
| **Walkable Footprint (`ESTIMATED`)** | `29.62 m²` (flat) / `22.08 m²` (stepped) | `~11.8 m²` | **`9.85 m²`** (`3.80 m²` steps + `6.06 m²` dais) |
| **Clear Gap to `SkylightCap` Edge** | `0.10 m` (unsafe/cramped) | `0.20 m` (East) / L-shaped drop | **`1.10 m`** (`1.42 m` from capsule center) |
| **`SkylightCap` Roof (`Y+`) Visibility** | `0.00%` (flat) / `43.8%` (stepped) | `0.00%` (flat) / `10.4%` (stepped) | **`18.5%–20.0%`** (`26.7%` total cap + **both NW & NE corners!**) |
| **Fall-Protection Complexity** | Wide `9.20 m` edge + `0.22 m` trench fix | High (2-axis L-shaped drop over `CeilingSlab_West`) | **Lowest** (single straight `4.52 m` framed parapet + upper collider) |
| **Construction Complexity** | Moderate–High | Moderate | **Low–Moderate** |
| **Architectural Restraint & Coherence** | Low–Moderate (overbuilt terrace; cuts pilaster) | Moderate (crowds stair arrival / East blocks elevator) | **Highest** (focused, disciplined architectural discovery) |
| **Rank** | **#3** | **#2** | **#1 — RECOMMENDED** |

---

## 9. Recommended Architecture — Section 12 Checklist *(RECOMMENDED — PROPOSED)*

1. **Where the player stands:** Inside the compact single-bay recessed observation alcove (`X ∈ [-4.460, +0.060]`) on a level poured-concrete viewing dais at **`Z ∈ [+27.400, +28.740]`, `Y = -45.860 m`** (reached by 3 concrete risers from the gallery floor `Y = -46.400 m`). Closest standing eye position: `Z = +27.720 m`, `Y_eye = -44.220 m`.
2. **Which part of the Lightwell and skylight is visible:** The **upper `1.600 m` of `NorthWing_Lightwell_WallN`** emerging out of the `Y = -46.400 m` chamber floor apron, the **`0.160 m`-tall cool-overcast North fascia edge (`Z+`)**, the **entire `10.60 m × 8.60 m` (`91.16 m²`) luminous top diffuser roof plane (`Y+`)** receding `8.60 m` South into the enclosed chamber, AND **both the Northwest (`X = -5.30`) and Northeast (`X = +5.30`) cantilevered corners** of `NorthWing_Lightwell_SkylightCap`. *(Internal beams/purlins and the lower Courtyard floor remain authentically concealed behind the opaque existing structure.)*
3. **Enter an observation space vs. observe through a frame:** **Hybrid (Compact Framed Observation Alcove)** — the player steps `2.18 m` off the `3.00 m` gallery corridor up 3 concrete steps into a framed architectural bay and observes the trapped Lightwell through a full-height concrete-and-steel architectural aperture without entering the skylight chamber itself.
4. **How existing structure remains 100% untouched:** Every `NorthWing_Lightwell_Wall*`, `NorthWing_Lightwell_SkylightCap`, internal beam/purlin, and `NorthWing_CeilingSlab_*` stays byte-identical in transform, scale, material, and `fileID`. All new chamber floor aprons maintain `≥ 0.020 m` clearance from `Lightwell_Wall*` outer faces and `+0.348 m` vertical plenum above Nivel 0 ceilings; the viewing parapet sits `0.900–1.100 m` clear North of `SkylightCap`.
5. **New geometry necessary (`PROPOSED`, dedicated sibling root `Hotel_CourtyardLightwellOverlook` under `Backrooms_Architecture`):**
   - `3` concrete steps + smooth concealed `32.735°` ramp `BoxCollider` (`Z ∈ [+28.74, +29.58]`).
   - Level dais floor slab (`X ∈ [-4.46, +0.06], Z ∈ [+27.40, +28.74], Y_top = -45.860`) + solid sub-dais closure + sidewalls (`X = -4.46`, `X = +0.06`).
   - `1.05 m` concrete guard parapet (`Z ∈ [+27.20, +27.40], Y ∈ [-46.60, -44.81]`), dark-steel top rail, 2 slim non-colliding framing mullions, overhead lintel transition, and **full-height invisible solid containment `BoxCollider`** (`Y ∈ [-44.81, -42.90]`).
   - Passive enclosing unfinished-concrete upper skylight chamber (`X ∈ [-6.20, +6.20], Z ∈ [+17.00, +29.58], Y_ceil = -42.900 m`) with 4 non-overlapping passive concrete floor aprons at `Y = -46.400 m` outside the Lightwell footprint.
   - Zero new materials/textures required (reuses existing `WallConcrete`, `FloorConcrete`, `CeilingSoffit`, `ColumnConcrete`, `BaseboardConcrete`, `IndustrialMetal`, and optionally 1 restrained `UtilityWarmOn` emissive strip over the dais ceiling).
6. **How the player remains safely contained:** Solid alcove sidewalls (`X = -4.46, +0.06`), solid `1.05 m` parapet (`Z = +27.40`), full-height solid invisible viewing barrier (`Y ∈ [-44.81, -42.90]`), zero jump capability, and `1.100 m` horizontal standoff from `SkylightCap`.
7. **How it connects to the North Upper Gallery:** Opens directly out of the single structural bay between existing `NUGallery_PilasterSouth_0` (`X = -4.60`) and `NUGallery_PilasterSouth_1` (`X = +0.20`), leaving the entire `3.00 m`-wide gallery circulation spine `100%` unobstructed (`Z ∈ [+29.80, +32.80]`) and preserving the continuous return route to the Northwest Service Staircase and Nivel 0.
8. **What remains inaccessible:** The entire `1.10 m` Lightwell reveal moat (`Z < +27.20`), the top and sides of `NorthWing_Lightwell_SkylightCap`, the surrounding upper skylight chamber (`X ∈ [-6.20, +6.20], Z ∈ [+17.00, +27.20]`), the East Overhang (`X > +16.00`), and the Twin Elevator Core.
9. **Whether the current 07.16 proposed footprint needs to change:** **YES — SUBSTANTIALLY.** Width reduces from `9.20 m` (`[-4.60, +4.60]`) to `4.52 m` (`[-4.46, +0.06]`) to preserve `PilasterSouth_0` and `PilasterSouth_1`; South walkable edge pulls back from `Z = +26.40` (`0.10 m` gap) to `Z = +27.40` (`1.10 m` gap); and observation floor steps up from flat `Y = -46.400 m` to **`Y = -45.860 m` (`+0.540 m`)** so the player's eye clears the skylight roof by `+0.420 m`.
10. **Issues to resolve before 07.19 implementation:** Creator approval of (a) **Alternative C (`+0.54 m` Single-Bay Framed Dais)** vs. Alternative A/B, (b) keeping the strict **`18`-light budget** (default) vs. authorizing `1` non-shadow cool-overcast point light inside the enclosed upper skylight chamber, and (c) confirming targeted test updates to `RemanenciaNorthUpperGalleryTests.cs` (`L126–143`) for the authorized single-bay opening.

---

## 10. Lighting & Visual Appearance Assessment *(VERIFIED + ESTIMATED)*

### A. Verified Material & Scene Radiometry (`VERIFIED EXISTING`)

| Material / System | Verified Shader & Properties | Linear Radiance Under Current Scene (`ESTIMATED`) | Visual Role |
| :--- | :--- | :--- | :--- |
| **`Remanencia_Level0_LightwellDiffuser.mat`** (`SkylightCap`) | URP Lit, **`_Surface = 0` (OPAQUE)**, `_AlphaClip = 0`, `_BaseColor = (0.58, 0.63, 0.68)`, **`_EmissionColor = (0.368, 0.414, 0.472)`** (`_EMISSION` ON) | **`(0.536, 0.609, 0.696)`** (`~3.6×` brighter than ambient concrete; distinctly cool overcast blue-grey) | Self-luminous opaque diffuser surface; **zero** real Unity light emission; **zero** transparency. |
| **`Backrooms_YellowWallpaper.mat`** (`WallConcrete`: `WallN/S/E/W` & `NUGallery_WallSouth`) | URP Lit, `_Surface = 0`, texture mean RGB `(0.618, 0.590, 0.558)` × `_BaseColor (0.94, 0.93, 0.91)`, `_EmissionColor = (0,0,0)` | **`(0.168, 0.170, 0.168)`** (flat neutral warm-grey) | Identical material on both gallery walls and Lightwell walls — zero tonal separation under flat ambient without darker concrete framing (`ColumnConcrete`, `0.134`). |
| **`Remanencia_Level0_UtilityWarmOn.mat`** (Gallery strips `0..3`) | URP Lit, `_Surface = 0`, `_EmissionColor = (2.064, 1.677, 1.075)` | Warm amber self-emissive glow only | Strong warm-vs-cool color temperature contrast against the cool overcast `LightwellDiffuser` visible through the bay! |
| **Scene Unity `Light` Components (`18` total)** | All `18` sit strictly in South Core (`Z ∈ [-8.4, +8.8]`, `m_Shadows = 0`, max `m_Range = 10.2 m` → light reaches at most `Z = +19.0 m`) | **`0.000`** direct Unity light contribution at `Z ≥ +19.0 m` (North Gallery & Lightwell North face!) | Entire North Wing M1 & Upper Stratum rely strictly on `RenderSettings.ambientLight = (0.29, 0.31, 0.33)` (Flat) + material `_EmissionColor` + player flashlight `[F]` (`Spot`, range `42 m`, intensity `68`). |
| **Fog Attenuation (`ExponentialSquared`)** | `RemanenciaLevel0FogColor = (0.16, 0.175, 0.19)`, `density = 0.028` | Near cap edge (`1.42 m`): `99.8%` clear; far South cap edge (`10.02 m`): `92.4%` clear; chamber South wall (`10.72 m`): `91.4%` clear. | Gently softens the far enclosed chamber walls while keeping the glowing skylight plane razor-sharp! |

### B. Lighting Budget Policy (`18` Lights Default + Explicit Optional Decision)

- **Default Baseline (`RECOMMENDED FOR 07.19`):** Preserve the exact **`18` Unity `Light` components (`LightShadows.None`)** enforced across **10** existing EditMode test suites (`RemanenciaHotelEnvironmentalIdentityTests`, `RemanenciaHotelNarrativeTests`, `RemanenciaLevel0EarlyEcologyTests`, `RemanenciaLevel0FirstFaunalTracesTests`, `RemanenciaLevel0FirstLivingMicrofaunaTests`, `RemanenciaLevel0ThresholdOfTrustTests`, `RemanenciaLevel0VisualTests`, `RemanenciaNorthWingExpansionTests`, `RemanenciaNorthwestStaircaseTests`, `RemanenciaNorthUpperGalleryTests`, `RemanenciaRoom111BArchitectureTests`). Because `LightwellDiffuser` is `~3.6×` brighter than ambient concrete and cool-tinted against the warm amber `UtilityWarmOn` gallery strips, the glowing skylight roof plane (`Y+`) will read unmistakably even at `0` added lights (especially framed by darker `ColumnConcrete` & `IndustrialMetal` alcove trim!).
- **Optional Future Enhancement (`UNRESOLVED` — NOT AUTHORIZED WITHOUT EXPLICIT APPROVAL):** Adding **`1` non-shadow cool-overcast Point Light** centered directly above `SkylightCap` at `(0.0, -43.90, 22.0)`, `color = (0.68, 0.75, 0.84)`, `intensity = 0.85`, `range = 13.0 m`, `shadows = LightShadows.None` would cast real cool bounce illumination onto the surrounding concrete chamber walls and the underside of the `Y = -42.90 m` ceiling, visually reinforcing that the trapped skylight is genuinely radiating overcast light indoors. *Cost:* negligible on 2 GB VRAM (`LightShadows.None`), **but** would require explicit creator sign-off to raise the scene-wide automated light-count invariant from `18` to `19` across the test suites above. **Defer to 07.20 PlayMode review.**

---

## 11. Narrative & Architectural Intent *(PROPOSED — ZERO HORROR CLICHÉS)*

The Courtyard Lightwell Overlook delivers Remanencia's core architectural thesis purely through geometry and spatial memory:

- **The Impossible Interior Roof:** Descending earlier into Nivel 0's Central Courtyard, the player looked up at a coffered concrete skylight opening toward an overcast sky. Climbing the unfinished Northwest Service Staircase and stepping onto the framed upper concrete dais, the player discovers that exact skylight **sitting indoors beneath an enclosed upper concrete ceiling**, its walls punching straight up through the upper floor slab.
- **No Explanation, No Spectacle:** Zero notes, zero plaques, zero room numbers, zero keys, zero NPCs, zero Cobalt-60 connections, and zero supernatural VFX. The player simply encounters a building whose upper story and ground-floor courtyard occupy the same memory — resolved as quiet, solid, poured concrete.

---

## 12. Exact Implementation Requirements & Controlled Roadmap *(PROPOSED)*

### Recommended Sequenced Increments

| Increment | Scope | Key Guardrails |
| :---: | :--- | :--- |
| **07.19** | **Courtyard Lightwell Overlook Construction (Alternative C — Single-Bay Framed Dais & Enclosed Upper Skylight Chamber)** via dedicated idempotent Editor builder `CourtyardLightwellOverlookBuilder.cs` under sibling root `Backrooms_Architecture/Hotel_CourtyardLightwellOverlook`, plus dedicated `RemanenciaCourtyardLightwellOverlookTests.cs` (including `CharacterController.Move()` ascent/descent, full-height barrier linecasts, `0%` unbuilt void verification, and 2× idempotency). | Zero edits to `NorthWing_Lightwell_*`, `Hotel_NorthwestServiceStaircase`, `Hotel_Elevator_Core`, Rooms `107–110`, Room `111-B`, Threshold of Trust, entry-only entrance, or `BorderCorridorScene.unity`. Preserves exact `18` lights (`LightShadows.None`) and `PilasterSouth_0..4` / `CeilingBeam_0..4`. |
| **07.20** | **PlayMode Lighting & Framing Polish (Conditional)** — Live PlayMode review of `07.19` with and without flashlight `[F]`; optionally refine mullion/apron material contrast or request creator approval for `1` non-shadow skylight bounce light (`18 → 19`) if flat ambient reads too uniform on the enclosing chamber walls. | Only triggered if PlayMode inspection shows real visual need. |
| **07.21+** | **Independent Reassessment of East Overhang (`X > +16.00`) & Future SE Elevator Approach (`X ∈ [+6.0, +8.4], Z = +26.40 → +4.20`)** — Separate design and approval increments. | **Not authorized by 07.18 or 07.19.** |

### Specific Targeted Test Updates Anticipated in 07.19 (`VERIFIED EXISTING`)
- `RemanenciaNorthUpperGalleryTests.cs` (`L126–143`) currently asserts that `NUGallery_WallSouth` is uncut and all renderers under `Hotel_NorthUpperGallery` stay at `Z ≥ 29.40f`. Placing 07.19 geometry under sibling root `Hotel_CourtyardLightwellOverlook` keeps `Hotel_NorthUpperGallery` clean, requiring only an intentional update to `NUGallery_WallSouth`'s split-segment verification while **retaining** all `NorthWing_Lightwell_*` protection assertions (`L279–283`) and **zero** changes to `RemanenciaNorthWingExpansionTests.cs` or `RemanenciaNorthwestStaircaseTests.cs`.

---

## 13. Protected Geometry & Systems *(MANDATORY INVARIANTS)*

All of the following remained **100% untouched** during 07.18 and remain **strictly protected** during future 07.19 construction:

- All four `NorthWing_Lightwell_WallN/S/E/W` (`GO=585720393 / 963459595 / 894939850 / 736225269`), `NorthWing_Lightwell_SkylightCap` (`GO=1795628428`), `NorthWing_Lightwell_ConcreteBeam_0/1/2`, and `NorthWing_Lightwell_SteelPurlinN/S`.
- All four Nivel 0 `NorthWing_CeilingSlab_*` (`North`, `South`, `East`, `West`).
- Complete 07.15 `Hotel_NorthwestServiceStaircase` and upper landing shell.
- `Hotel_NorthUpperGallery` structural pilasters (`PilasterSouth_0..4`), ceiling beams (`CeilingBeam_0..4`), North wall, sealed East Terminal (`X = +16.00`), and emissive strips (`StripTube_0..3`). *(Only `NUGallery_WallSouth`, `BaseboardSouth`, and `SealedSouthRelief` are scoped for targeted modification in 07.19.)*
- `Hotel_Elevator_Core` (`GO=331299473`, `35` children, Bay 1 & Bay 2).
- Rooms `107–110`, Room `110` ecological Zone D, Room `111-B`, **Threshold of Trust** (`"threshold-of-trust"`), entry-only hotel entrance (`Backrooms_ExitDoor_ToStreet`), both surface entry routes, save schema (`schemaVersion == 1`), journal/progression/transition systems, and `BorderCorridorScene.unity` (`SHA-256: 0CFB32080F4ED9C30FAA513BB21C9BB48ADBA9C60B27FABABFAD5E64318DB42B`).
- Exact `18` Unity `Light` components (`LightShadows.None`) unless explicitly modified by separate creator approval.

---

## 14. Unresolved Risks & Creator Decisions *(UNRESOLVED)*

1. **Select Architectural Alternative (`UNRESOLVED`):** Approve **Alternative C — Compact Single-Bay Framed Architectural Observation with `+0.540 m` 3-Step Viewing Dais (`X ∈ [-4.46, +0.06]`, `Z ∈ [+27.40, +29.58]`, enclosed chamber `Y_ceil = -42.900 m`)**, OR choose Alternative B (Offset Corner Alcove) or Alternative A (Wide North Perimeter Overlook).
2. **Symmetric Twin-Bay vs. Single-Bay Framing (`UNRESOLVED`):** Confirm **Single-Bay (`X ∈ [-4.46, +0.06]`, `4.52 m` wide, zero pilasters removed, reveals both NW & NE skylight corners)** vs. expanding symmetrically across both bays (`X ∈ [-4.46, +4.86]`, keeping `PilasterSouth_1` as a central concrete pier). *(Single-Bay is recommended for restraint and minimal footprint.)*
3. **Lighting Policy (`UNRESOLVED`):** Confirm keeping exact **`18` Unity `Light` components** for 07.19 (default — emissive `LightwellDiffuser` contrast + flat ambient + optional `[F]` flashlight), deferring any `1`-light cool skylight bounce light (`18 → 19`) to 07.20 PlayMode evaluation.
4. **Chamber Wall Finish Contrast (`UNRESOLVED`):** Confirm using darker raw structural concrete (`Remanencia_Level0_ConcreteColumn.mat`, linear ambient `0.134`) for the passive enclosing upper skylight chamber walls and ceiling soffit so `NorthWing_Lightwell_WallN` (`0.168`) and `SkylightCap` (`0.609`) stand out cleanly against the chamber background even without added Unity lights.
