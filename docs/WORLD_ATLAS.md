# World Atlas & Verified World Transitions

**Project:** My Virtual Community — *A Living World Beyond Reality.*
**Scope:** Verified architectural, spatial, and scene-level inventory grounded directly in `unity-client/Assets/Scenes/`, `EditorBuildSettings.asset`, runtime C# scripts, and Editor scene builders (`CONFIRMED`).

Related active documents: [README](../README.md) · [Game Vision](GAME_VISION.md) · [Core Game Loop](CORE_GAME_LOOP.md) · [Feature Status Matrix](FEATURE_STATUS_MATRIX.md) · [Vertical Slice](VERTICAL_SLICE.md)

---

## 1. Scene Architecture Overview (`CONFIRMED`)

The current game does **not** separate all four narrative layers into four independent Unity scenes. Instead, **three narrative layers share `BorderCorridorScene.unity`**, while **Backrooms Level 1 is an additively loaded second scene**:

| Build Index | Scene Path | Disk Size | Layers / Purpose | Build Status |
| :---: | :--- | :---: | :--- | :--- |
| **0** | `Assets/Scenes/BorderCorridorScene.unity` | ~34.07 MB | **Main Reality Layer**, **Underground Storm-Drain Network**, and **Secret Room shell + Threshold Veil** | Active primary runtime scene (`EditorBuildSettings.asset`) |
| **1** | `Assets/Scenes/HandshakeScene.unity` | ~14 KB | Historical hackathon API diagnostic scene (`HandshakeController.cs`) | Enabled in `EditorBuildSettings.asset`; never loaded by gameplay |
| **2** | `Assets/Scenes/BackroomsLevel1Scene.unity` | ~1.16 MB | **Backrooms Level 1** (`40 m × 24 m` liminal labyrinth, artifacts, Level-Ω Safe, *El Umbral de Lasser*) | Enabled in `EditorBuildSettings.asset`; loaded/unloaded additively |
| *Excluded* | `Assets/Scenes/SampleScene.unity` | ~11 KB | Default empty URP template scene | Excluded from builds; unused |
| *Excluded* | `Assets/Development/BobbleheadProofOfConcept/Scenes/BobbleheadElenaPocScene.unity` | ~2.60 MB | Character turnaround R&D scene for Elena Ramos | Excluded from builds; unused in runtime |

---

## 2. Layer A — Main Reality Layer (`BorderCorridorScene.unity`)

**Status:** `CONFIRMED` — Physically constructed, playable, accessible, interactive, connected to Underground World and Backrooms.

### Geographic & Urban Footprint
Reproduces the historic downtown binational corridor of **Avenida Benito Juárez** in Ciudad Juárez, Chihuahua, extending north from **Calle Azucenas** (`Z ≈ -37 m`) across **Calle Gardenias**, through the **Puente Internacional Paso del Norte (Santa Fe Street Bridge)** toll plaza and viaduct (`Z ≈ 40–350 m`) over under-bridge avenues (*Av. Heroico Colegio Militar*, *Francisco Villa*, *Av. Abasolo*) and the **Río Bravo / Rio Grande** channel into the **U.S. Inspection Plaza (El Paso, Texas)**.

### Verified Districts & Interactive Elements
- **East Commercial Block (`EastSidewalkBlockBuilder`):** 9 storefronts (including *Farmacias Benavides*, *Consultorio Médico*, *Comercial Uno*, *Joyería Minerva*, *Curiosidades Azteca*, *La Michoacana*, *Atención Dental*, *Cambios Puente*, *Apple Dental Care*). *(See [IP & Asset Provenance](IP_AND_ASSET_PROVENANCE.md) regarding commercial brand review.)*
- **West Commercial Block & SW Annex (`WestSidewalkBlockBuilder`, `WestAzucenasCornerBuilder`, `OpticaFashionInteriorBuilder`):** 10+ storefronts including the SW corner annex, Building A south chamfer bay, Building A–C north wing, **physically traversable *Óptica Fashion* showroom and rear consultation room**, 4 schedule-synchronized roll-up metal shutters (`StorefrontShutterController.cs`), and 3 live-rate USD/MXN cantilevered LED signs (`ExchangeRateLedSignController.cs`).
- **West Manzana Perimeter (`WestManzanaPerimeterBuilder`):** Full urban block enclosure west to `X = -52.00 m`, **Calle Ignacio Mariscal** roadway (`X = -54.50` to `-64.00 m`), crosswalks, and opposing curved **Plaza Peatonal**.
- **Av. Juárez Digital Gateway Arch (`AvJuarezArchBuilder`, `JuarezArchDisplayController`):** Steel lattice gateway arch at `Z = -23.50 m` cycling local/streamed `.mp4` videos and telemetry slides (World Core clock, weather, CBP wait times).
- **Paso del Norte International Bridge & U.S. Inspection Plaza (`Bridge*Builder`, `TollBarrierController`):** Arched concrete canopy, 4 electromechanical toll barrier gates (`lane-1` through `lane-4`), covered East/West pedestrian walkways, Línea Express / SENTRI lane barriers, overhead gantry sign, 2 clickable Fideicomiso camera Easter Eggs (*Vista Sur* & *Vista Norte*), and El Paso inspection booths.
- **Inhabitants (7 Canonical Bobblehead NPCs):** Elena Ramos (Border Concierge), Lic. Patricia Morales (Citizen Reporting Desk at `X = -7.60 m, Z = 18.80 m`), Carlos Méndez (Farmacias Benavides attendant), Valeria Domínguez (Óptica Fashion attendant with indoor/outdoor routine), and synthetic community witnesses Mateo Navarro, Sofía Alarcón, and Don Héctor Méndez.

---

## 3. Layer B — Underground Storm-Drain Network (`BorderCorridorScene.unity`)

**Status:** `CONFIRMED` — Physically constructed inside `BorderCorridorScene.unity` (**not** a separate Unity scene), playable, accessible, partially interactive (manholes & outflow gate), connected to Main Reality and Backrooms (via Manhole #1).

### Verified Architecture (`UndergroundSewerBuilder.cs`, `UndergroundSewerAtmosphereController.cs`)
- **Primary Collector Level (`Y = -3.20 m`):** Aged vaulted brick-and-concrete storm-drain tunnels with central flowing water channel, walkways, rubble, roots, corroded valves, bilingual crimson graffiti decals, and crop-circle markings.
- **Surface Street Access (5 Cast-Iron Manholes — `SewerManholeInteractable.cs`):**
  - 4 manholes open into hollow 16-sided brick chimneys with ladders connecting the surface streets directly to the storm-drain walkways (`triggerBackroomsNoClipTwist = 0`).
  - **Manhole #1 (Av. Benito Juárez × Calle Azucenas, `X = 3.30, Z = -32.20`):** Armed with `triggerBackroomsNoClipTwist = 1` — descending silently triggers a ceiling no-clip drop into `BackroomsLevel1Scene.unity` rather than entering the sewer.
- **Curbside Intake Shafts:** 6 diagonal curbside daylight intake slots (`Curbside_DrainIntakes_Root` / `DrainIntakeSlot_*`), including a purely decorative water-damaged folded collector maintenance notice (`FoldedCollectorMaintenanceNotice` / *Aviso Plegado del Colector*) caught near the Av. Juárez × Azucenas intake (`IP-02`: `REPLACED — creator visual review complete; not formal legal clearance` in [IP & Asset Provenance](IP_AND_ASSET_PROVENANCE.md)).
- **Sector C-0 Lore Bulkhead:** Sealed bulkhead directly beneath *La Esquina de Chihuas* marked with a weathered wooden Paris Catacombs plaque (*"Arrête ! C'est ici l'empire de la mort"*).
- **Grand Octagonal Central Cistern Vault (*"La Cisterna / El Nudo"*):** `16.4 m` diameter × `6.0 m` sunken flooded basin beneath Av. Juárez × Calle Gardenias with an octagonal balcony ring, flickering hanging lamps, and a dual-handrail industrial staircase descending to a central circular island apron.
- **Northern Inverted Siphon & Río Bravo Outflow (`SewerOutflowGateController.cs`):** North trunk collector dips to **`Y = -8.45 m`** beneath the sunken Av. Abasolo underpass before rising to `Y = -2.42 m` at the split levee bank (`Z = 197.20 m`), ending at a weathered concrete storm outflow headwall with an interactive rusted iron gate (`[E]`) opening onto the walkable south bank of the Río Bravo beneath the international bridge.

### Confirmed Limitations
- **No underground NPCs** currently exist (the central island apron in *La Cisterna* is physically built for future use only).
- **No underground puzzle or clue-pickup systems** are currently implemented; the network is atmospheric spatial exploration only.

---

## 4. Layer C — Secret Room (`BorderCorridorScene.unity`)

**Status:** `CONFIRMED` — Physically constructed inside `BorderCorridorScene.unity`, playable, accessible, interactive (keypad door & threshold trigger), connected to Main Reality and Backrooms Level 1.

### Verified Architecture (`SecretWedgeRoomBuilder.cs`, `SecretKeypadDoorController.cs`, `SecretBackroomsTransitionController.cs`)
- **Exterior Facade:** Ground-floor street-aligned false wall (`X = -12.00 m`, `Z = -13.45` to `-11.20 m`) beneath Building A's 2nd-story diagonal chamfer, with taupe-gray relief molding, electrical utility box, double-leaf steel door, and a 3D 6-digit electronic keypad (`door:building-a-south-secret-wedge`, default code `"123456"`, automatic relock on close).
- **Interior Space:** Small 4-sided right-trapezoid room (`~2.18 m × 1.60 m`) with concrete floor, raw plaster walls, and a warm amber caged pendant lamp.
- **CRITICAL ARCHITECTURAL FACT:** Inside `BorderCorridorScene.unity`, **the physical Secret Room behind the false wall is currently an empty, dusty access vestibule** (also serving as a `decoy_empty` room if another player ID holds World Core occupancy). An opaque **`SecretRoom_ThresholdVeil`** spans the doorway; stepping past `X < -11.94 m` when unoccupied slams and relocks the steel door and additively loads `BackroomsLevel1Scene.unity`.
- **Important Correction vs. Older Notes:** The containment artifacts, Level-Ω Vault Safe, *El Ojo del Umbral*, and *El Umbral de Lasser* are **not** located inside this small wedge room (`BuildMysteryContainmentVaultInterior()` in `SecretWedgeRoomBuilder.cs` is dead, uncalled code). All of those elements reside inside `BackroomsLevel1Scene.unity`.

---

## 5. Layer D — Backrooms Level 1 (`BackroomsLevel1Scene.unity`)

**Status:** `CONFIRMED` — Independent Unity scene, physically constructed, playable, accessible, interactive, connected to Main Reality via two entry routes and one exit route.

### Verified Architecture (`BackroomsLevel1Builder.cs`, `SecretRoomDimensionVaultController.cs`, `BackroomsExitDoorTrigger.cs`)
- **Dimensions & Placement:** `40 m × 24 m` (`960 m²`) Level 0 yellow-wallpaper liminal labyrinth positioned at `Y = -50.00 m` (`2.85 m` clear height), matching the footprint of the south urban block between Av. Benito Juárez and C. Ignacio Mariscal.
- **Atmosphere & Lighting:** Procedural yellow wallpaper, damp mustard carpet, acoustic drop-ceiling grid with missing void tiles, 24 fluorescent ceiling panels (16 point lights with steady, flickering via `BackroomsAmbientFlicker.cs`, and dead tubes), plus warm-yellow exponential fog applied on entry.
- **Arrival & Exit (NE Corner):** Arrival spawn (`Backrooms_SpawnPoint` at `17.80, -49.94, 8.80`) and double metal return door (`Backrooms_ExitDoor_ToStreet` with `BackroomsExitDoorTrigger.cs`).
- **8 "Reality-Bridge" Props (Visual Prototype / Inspect-Only):** Lone workstation (sole desk + laptop + chair), vintage FM radio, smart speaker ("Alexa"), digital newspaper tablet ("El Diario de Juárez"), Juárez telemetry wall monitor, 1990s wall payphone, CCTV street-cam frame, and thermal receipt printer. **Verified state:** All 8 attach only `SecretContainmentArtifactDescriptor` inspection captions — **none are wired to live external APIs today**. *(Brand/trademark props flagged in [IP & Asset Provenance](IP_AND_ASSET_PROVENANCE.md).)*
- **13 Dispersed Artifacts & Puzzle Elements:**
  - **Evidence Corkboard PDN-00** (South-Central alcove): Inspectable dossier revealing the safe code (`654321`).
  - **Level-Ω Vault Safe** (NW room): Interactive 6-digit keypad safe (`654321`) containing ***El Ojo del Umbral*** (Octagonal Obsidian & Brass Dimensional Astrolabe Keystone).
  - **Monumental Dimensional Mirror-Portal (*"El Umbral de Lasser"*)** (far SW chamber, ~41 m diagonally from NE arrival): Features dormant mirror, containment chains, and an interactive **Octagonal Socket** (`portal:cuenca-octagonal`).
  - **Additional 12 Inspectable Artifacts:** Including original regional lore (*Paquimé* obsidian/turquoise mask, *Anomalous Astral Compass*) alongside third-party pop-culture horror references currently flagged for commercial replacement (see [IP & Asset Provenance](IP_AND_ASSET_PROVENANCE.md)).

---

## 6. Additional Scenes (`CONFIRMED`)

- **`Assets/Scenes/HandshakeScene.unity`:** Historical hackathon diagnostic scene (Increment 5.1) running `HandshakeController.cs`, which pings `/health` and `/api/spatial/corridor` and spawns colored cube markers. Remains listed in `EditorBuildSettings.asset` (Index 1) and is subject to future disposition (removal from commercial build settings or archival) in a later code phase. No commercial gameplay role is assumed or invented here.
- **`Assets/Scenes/SampleScene.unity`:** Unused default Unity URP template scene (excluded from builds).
- **`Assets/Development/BobbleheadProofOfConcept/Scenes/BobbleheadElenaPocScene.unity`:** Historical character R&D showcase scene (excluded from builds).

---

## 7. Verified World Transition Matrix (`CONFIRMED`)

| Transition Route | Trigger Mechanism | Scene / Coordinate Behavior | Runtime Authority & Offline Behavior | Verified State |
| :--- | :--- | :--- | :--- | :---: |
| **Street → Underground** | Press `[E]` at any of the 4 standard street manholes (`triggerBackroomsNoClipTwist = 0`) | `SewerManholeInteractable.cs` slides lid, fades screen via `UndergroundSewerAtmosphereController`, teleports player inside `BorderCorridorScene.unity` to underground ladder landing (`Y ≈ -3.20 m`), suppresses sun/skybox reflections, hides surface NPC HUD. | 100% client-side; works fully offline. | **IMPLEMENTED** |
| **Underground → Surface** | (1) Press `[E]` at any of the 4 underground ladders, OR (2) open the Río Bravo outflow gate (`SewerOutflowGateController.cs`, `[E]`) and walk out onto the south bank (`Z >= 197.2 m`) | (1) Screen fade + teleport back to street surface anchor; (2) continuous physical walk from dark tunnel into outdoor daylight (`outflowDaylightStartZ = 162 m` to `190.5 m`), restoring outdoor reflections and lighting. | 100% client-side; works fully offline. | **IMPLEMENTED** |
| **Street → Backrooms (via Secret Room)** | Enter `123456` on Building A South door (`SecretKeypadDoorController.cs`), walk past opaque `SecretRoom_ThresholdVeil` (`X < -11.94 m`) | `SecretBackroomsTransitionController.cs` slams & relocks street door, requests `/api/secret-vault/enter`, fades to black, calls `SceneManager.LoadSceneAsync("BackroomsLevel1Scene", LoadSceneMode.Additive)`, suspends outdoor directional lights, applies Backrooms fog/ambient, teleports player to `Backrooms_SpawnPoint` (`Y = -49.94 m`). | Polls `/api/secret-vault/status` (ephemeral in-memory server state; if occupied by another ID shows `decoy_empty`). Gracefully falls back to solo entry offline. | **IMPLEMENTED** |
| **Street → Backrooms (Special Azucenas Manhole)** | Press `[E]` to descend Manhole #1 at Av. Benito Juárez × Calle Azucenas (`X = 3.30, Z = -32.20`, `triggerBackroomsNoClipTwist = 1`) | Silent, hint-free transition (`TriggerManholeNoClipDropToBackrooms`): plays procedural VHS no-clip glitch audio + brief flash, additively loads `BackroomsLevel1Scene.unity`, drops player physically from acoustic ceiling (`Y = -47.92 m` → `-50.00 m`) with impact thud & camera tilt recovery into one of 10 collision-checked maze zones far from the NE exit. | Claims vault occupancy in background (non-blocking); works fully offline. | **IMPLEMENTED** |
| **Backrooms → Surface** | Press `[E]` or click NE double metal return door (`BackroomsExitDoorTrigger.cs`) | `ExitBackroomsRoutine()` fades to black, restores outdoor lights/skybox/fog, teleports player to Av. Juárez sidewalk (`-11.20, 0.24, -11.99`), keeps street door relocked, calls `SceneManager.UnloadSceneAsync("BackroomsLevel1Scene")`, calls `/api/secret-vault/leave`. | Releases vault occupancy if backend reachable; works offline. Unloading resets all Backrooms puzzle/vault state. | **IMPLEMENTED** |
| **Lasser Portal Activation (*El Umbral de Lasser*)** | Insert *El Ojo del Umbral* into `portal:cuenca-octagonal` socket (`SecretRoomDimensionVaultController.cs` + `FirstPersonPlayer.cs`) | **Visual & local state toggle ONLY:** hides containment chains, activates spinning vortex & astrolabe ring, swaps mirror material to `activeVortexMaterial`, shifts point light to cyan, shows HUD banner (`PORTAL DIMENSIONAL ACTIVO`). Pressing `[E]` again removes the astrolabe and re-seals the mirror. | Local scene state only; lost when Backrooms scene unloads. | **VISUAL ACTIVATION ONLY (NO TRANSITION)** |

> **Explicit Verification Statement (`CONFIRMED`):**
> ***El Umbral de Lasser* is currently a visual activation only. It does not transport the player, trigger a collider volume, or load another world or Unity scene.** No dimensional transition beyond `BackroomsLevel1Scene.unity` exists in the current codebase.
