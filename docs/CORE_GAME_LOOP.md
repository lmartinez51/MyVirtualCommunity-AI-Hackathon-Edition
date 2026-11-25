# Core Game Loop & Player Experience

**Project:** My Virtual Community â€” *A Living World Beyond Reality.*
**Status:** Explicitly separates **Section A (`CONFIRMED` implemented mechanics)** from **Section B (`PLANNED` proposed commercial adventure loop)**.

Related active documents: [Game Vision](GAME_VISION.md) Â· [World Atlas](WORLD_ATLAS.md) Â· [Vertical Slice](VERTICAL_SLICE.md) Â· [Feature Status Matrix](FEATURE_STATUS_MATRIX.md)

---

## 1. Current Player-Facing Verdict (`CONFIRMED`)

**Does a complete commercial adventure gameplay loop exist today? No.**

What currently exists in the repository is a **playable, multi-layered 3D spatial exploration sandbox** with authentic border architecture, 7 schedule-driven NPCs, working cross-world transitions (Main Reality â†” Underground Storm-Drain Network â†” Backrooms Level 1), and one self-contained local puzzle inside Backrooms Level 1.

Critical adventure-loop infrastructure is **not yet implemented (`CONFIRMED`):**
- No title screen, New Game / Continue flow, or in-game session restart.
- No persistent quest or objective tracker.
- No clue journal, codex, or discovery log.
- No general inventory system (only a single transient boolean `hasAstrolabeInInventory` inside `SecretRoomDimensionVaultController`).
- No general save/load or progression persistence (exiting `BackroomsLevel1Scene` unloads the scene and resets the Level-Î© safe, astrolabe pickup, and Lasser portal state; `PlayerPrefs` persists only HUD toggles, FPS cap, and volume; `.runtime/citizen-reports.json` persists only simulated municipal reports).
- No end state or slice completion screen.

---

## 2. Section A â€” Existing Implemented Mechanics (`CONFIRMED`)

The following interactions and exploration systems are verified in the current codebase (`unity-client/Assets/Scripts/` and `src/`):

### 2.1 First-Person Navigation & Spatial Controls
- Keyboard + mouse locomotion (`W/A/S/D` walk, `Shift` sprint, `Space` jump, mouse look) at a `1.60 m` standing eye level (`FirstPersonPlayer.cs`).
- Handheld URP Forward+ flashlight (`[F]`, `PlayerFlashlightController.cs`), essential for navigating the pitch-dark underground storm-drain network and night-time street scenes.
- Proximity and raycast interaction (`[E]` or mouse click) with NPCs, manholes, doors, outflow gate, safe, and inspectable artifacts.
- Orientation HUD: 2D corridor minimap (`[M]`), directional compass ribbon, Controls Guide (`[F1]`), Pause Menu (`[Esc]`), and Settings Drawer. Surface NPC markers automatically hide when the player is underground.
- Developer/inspection aerial cameras: Free-Flight 3D Drone (`[V]`) and top-down/oblique Satellite View (`[G]`), including instant ground teleport (`[T]`). *(Note: these are currently accessible during normal play and will need to be restricted to debug/photo mode in commercial builds.)*

### 2.2 Surface Exploration & Grounded NPC Social Interaction
- Walkable Avenida Benito JuÃ¡rez commercial corridor, Calle Azucenas, Calle Gardenias, Calle Ignacio Mariscal, Plaza Peatonal, Paso del Norte International Bridge, U.S. Inspection Plaza, and traversable *Ã“ptica Fashion* interior.
- 7 distinct Bobblehead NPCs with specialized, non-identical roles (`CONFIRMED`):
  - **Elena Ramos** (`elena-border-concierge`) â€” Border Pedestrian Concierge interpreting official CBP wait-time data when available.
  - **Lic. Patricia Morales** (`patricia-citizen-rep`) â€” Municipal citizen issue intake desk (9 categories, optional photo attachment with consent gate and local `.runtime/citizen-reports.json` persistence).
  - **Carlos MÃ©ndez** (`carlos-farmacia-attendant`) â€” Storefront attendant grounded in Farmacias Benavides operating schedule.
  - **Valeria DomÃ­nguez** (`valeria-optica-attendant`) â€” Storefront attendant with indoor/outdoor *Ã“ptica Fashion* locomotion, calibration routine, and shutter-synced lifecycle.
  - **Mateo Navarro**, **SofÃ­a AlarcÃ³n**, and **Don HÃ©ctor MÃ©ndez** â€” Three explicitly disclaimed synthetic community witnesses patrolling the bridge queue.
- Interactive street infrastructure: 4 schedule-synced storefront roll-up shutters, 3 live-rate USD/MXN exchange-rate LED signs, Av. JuÃ¡rez Digital Gateway Arch billboard, 4 backend-controlled toll barrier gates, and 2 clickable Fideicomiso bridge cameras (PiP YouTube preview/link).

### 2.3 Underground Exploration & Transitions
- 5 cast-iron municipal manholes (`SewerManholeInteractable.cs`, `[E]`): 4 enter/exit the underground storm-drain network; Manhole #1 at Av. JuÃ¡rez Ã— Azucenas silently triggers a ceiling no-clip drop into `BackroomsLevel1Scene.unity`.
- Walkable underground collector tunnels, *La Cisterna / El Nudo* octagonal cistern, *Sector C-0* sealed bulkhead, deep `Y = -8.45 m` inverted siphon under Av. Abasolo, and an interactive rusted iron outflow gate (`SewerOutflowGateController.cs`, `[E]`) opening onto the RÃ­o Bravo south bank.
- Atmospheric shift controller (`UndergroundSewerAtmosphereController.cs`) with dark fog, suppressed skybox reflections, flickering vault lights, and procedurally synthesized ambient hum/water drips.

### 2.4 Secret Room Access & Backrooms Micro-Puzzle
- **Building A South Mysterious Door (`SecretKeypadDoorController.cs`):** Interactive 6-digit electronic keypad (currently hardcoded to default `"123456"`; backend daily-code hook `SetAuthoritativeDailyCode` exists in C# but is not wired to an in-world clue trail). Auto-relocks on close.
- **Threshold Veil Transition (`SecretBackroomsTransitionController.cs`):** Stepping past `X < -11.94 m` slams and relocks the street door, checks single-occupant status via `/api/secret-vault/enter` (falling back to solo access offline), and additively loads `BackroomsLevel1Scene.unity`.
- **Inspectable Artifacts & Props (`SecretContainmentArtifactDescriptor.cs`):** 13 dispersed artifacts and 8 "Reality-Bridge" props display localized ES/EN dossier popups on aim/interact. *(Note: the 8 Reality-Bridge props are inspect-only captions today, not live API feeds.)*
- **Backrooms Local Puzzle (`SecretRoomDimensionVaultController.cs`):**
  1. Inspect **Evidence Corkboard PDN-00** (South-Central alcove) â†’ reveals reverse vault code `654321`.
  2. Enter `654321` on the **Level-Î© Vault Safe** (NW room) â†’ safe door opens.
  3. Pick up ***El Ojo del Umbral*** (Astrolabe Keystone) from inside the safe.
  4. Insert *El Ojo del Umbral* into the **Octagonal Socket** on ***El Umbral de Lasser*** (SW chamber) â†’ chains retract, vortex material and spinning rings ignite visually. *(Visual activation only â€” no world transition occurs.)*
- **Return to Surface (`BackroomsExitDoorTrigger.cs`):** NE double metal return door unloads `BackroomsLevel1Scene.unity`, releases vault occupancy, restores outdoor atmosphere, and returns the player to Av. Benito JuÃ¡rez.

---

## 3. Section B â€” Proposed Future Commercial Adventure Loop (`PLANNED`)

The following loop describes the **proposed** player experience for the Unity Fellowship vertical slice and v1.0 release. None of the progression, journal, save, or narrative systems below are implemented yet.

```text
[ 1. OBSERVE & EXPLORE ]  Surface border corridor (living routines, subtle anomalies)
           â”‚
           â–¼
[ 2. GATHER CLUES ]       Environmental markings, NPC rumors, underground inscriptions
           â”‚
           â–¼
[ 3. SOLVE & UNLOCK ]     Deduce access codes, open sealed infrastructure & thresholds
           â”‚
           â–¼
[ 4. CROSS THRESHOLDS ]   Descend underground, enter concealed rooms, transition to hidden realms
           â”‚
           â–¼
[ 5. DISCOVER & SYNTHESIZE ] Recover artifacts, log evidence in persistent journal, unlock new layers
           â”‚
           â–¼
[ 6. RETURN TRANSFORMED ] Re-emerge onto the border street with new knowledge, keys, and routes
```

### Proposed Adventure Systems (`PLANNED`)
- **Opening Hook & Soft Guidance:** Minimal diegetic onboarding (e.g., an anomalous sightline, sound cue, or brief journal entry) giving the player immediate curiosity without hand-holding waypoints. `AUTHOR DECISION REQUIRED` on exact narrative hook.
- **Persistent Discovery Journal & Clue Log:** Tracks inspected landmarks, recorded codes, sketched diagrams, and anomalous artifacts across sessions and scene unloads.
- **Persistent Inventory & Key-Item System:** Replaces transient scene booleans so acquired keys, codes, and relics survive scene transitions and save/load cycles.
- **Cross-World Clue Chaining:** Connects surface anomalies â†’ underground *La Cisterna* / *Sector C-0* discoveries â†’ Secret Room keypad code â†’ Backrooms vault puzzle â†’ *El Umbral de Lasser* activation (see [Vertical Slice](VERTICAL_SLICE.md)).
- **Save/Load & Session Lifecycle:** Title screen (`Continue`, `New Journey`, `Settings`, `Quit`), local JSON save file storing player position/checkpoint, unlocked doors, journal entries, and puzzle states.
- **Diegetic Audio & Pacing Polish:** Ambient border streetscapes, footsteps by surface material, distinct subterranean acoustics, and responsive puzzle/discovery audio cues.
- **Debug Mode Gating:** Move aerial Drone (`[V]`) and Satellite (`[G]`) inspection tools behind an explicit developer flag or unlockable post-completion photo mode so exploration boundaries remain intact.
