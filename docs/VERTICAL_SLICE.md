# Vertical Slice Specification (Provisional)

**Provisional Working Title:** *Slice 01 — Thresholds of Juárez*
**Target Experience Duration:** `15–20 minutes` *(design target only — **not** a playtested or verified duration)*
**Target Audience:** Unity Fellowship 2027 evaluators, early playtesters, and commercial proof-of-concept validation.

Related active documents: [Game Vision](GAME_VISION.md) · [Core Game Loop](CORE_GAME_LOOP.md) · [World Atlas](WORLD_ATLAS.md) · [Feature Status Matrix](FEATURE_STATUS_MATRIX.md) · [IP & Asset Provenance](IP_AND_ASSET_PROVENANCE.md)

---

## 1. Vertical Slice Objective

Prove the commercial core of **My Virtual Community — A Living World Beyond Reality.** in one cohesive, IP-cleared, self-contained first-person adventure loop connecting all four already-built spaces:

1. **Main Reality Layer** (Ciudad Juárez–El Paso corridor)
2. **Underground Storm-Drain Network** (*La Cisterna / El Nudo*, *Sector C-0*, Río Bravo outflow)
3. **Secret Room Threshold** (Building A South false wall & keypad door)
4. **Backrooms Level 1** (IP-cleared liminal vault, Level-Ω Safe, *El Ojo del Umbral*, *El Umbral de Lasser*)

> **Scope Boundary (`PLANNED`):** A playable fourth hidden world *beyond* *El Umbral de Lasser* is **explicitly NOT required** for this initial Unity Fellowship demo slice. Activating *El Umbral de Lasser* serves as the vertical slice climax and completion beat, pointing forward to the full commercial campaign.

---

## 2. Eight-Beat Proposed Experience Structure

Each beat below explicitly separates **`[EXISTING NOW]` (`CONFIRMED`)** from **`[PROPOSED FOR SLICE]` (`PLANNED`)**.

### Beat 1 — Main Reality Exploration (Avenida Benito Juárez & Paso del Norte)
- **`[EXISTING NOW]`:** Player spawns on Avenida Benito Juárez with WASD/mouse controls, `[F]` flashlight, `[E]` interaction, 7 autonomous Bobblehead NPCs, live schedules/shutters, walkable *Óptica Fashion*, and the Paso del Norte International Bridge.
- **`[PROPOSED FOR SLICE]`:**
  - Add a clean **Title Screen** (`Start Slice`, `Continue`, `Settings`, `Quit`) and gate `[V]`/`[G]` aerial debug cameras out of normal play.
  - Introduce a brief **opening curiosity hook** (`AUTHOR DECISION REQUIRED` on exact narrative framing) that draws the player to investigate the street corner around Av. Juárez × Azucenas, the locked Building A South steel door, and the storm-drain manholes.
  - Complete the commercial **IP/brand clearance pass** on street signage and arch video (`camila_loop.mp4` replacement).

### Beat 2 — Discovery of Underground Clues (*La Cisterna / El Nudo* & *Sector C-0*)
- **`[EXISTING NOW]`:** 4 functional cast-iron street manholes descend into the `Y = -3.20 m` storm-drain network beneath `BorderCorridorScene.unity`, featuring near-black atmosphere (`[F]` flashlight required), *La Cisterna*, *Sector C-0* bulkhead, the `Y = -8.45 m` Abasolo siphon, and the interactive Río Bravo outflow gate.
- **`[PROPOSED FOR SLICE]`:**
  - Place **2–3 inspectable original environmental clues** inside the storm-drain network (e.g., near *Sector C-0* and on the *La Cisterna* balcony/island) that can be logged into a lightweight **Discovery Journal**.
  - Have those underground clues reveal (or allow the player to deduce) the **6-digit access code** for the Building A South mystery door, replacing the unmotivated hardcoded `"123456"` default. (`AUTHOR DECISION REQUIRED` on exact clue text and code-puzzle design.)
  - Retain Manhole #1 (`Av. Juárez × Azucenas`) as a hidden alternate/Easter-egg ceiling no-clip entry into Backrooms, or decide whether to gate it until after first discovery (`AUTHOR DECISION REQUIRED`).

### Beat 3 — Return to the Surface & Access to the Secret Room
- **`[EXISTING NOW]`:** Player climbs any underground ladder (or exits via the Río Bravo outflow gate and walks south along Av. Juárez), enters the 6-digit code on `SecretKeypadDoorController.cs`, opens the steel double doors, and steps through the opaque `SecretRoom_ThresholdVeil` to trigger `SecretBackroomsTransitionController.cs`.
- **`[PROPOSED FOR SLICE]`:**
  - Persist keypad discovery/unlock status in the local slice save state.
  - Add responsive audio/visual cues (keypad beeps, heavy latch, low hum behind the Threshold Veil) reinforcing the shift from grounded border street into liminal space.

### Beat 4 — Backrooms Level 1 Exploration (IP-Cleared Liminal Labyrinth)
- **`[EXISTING NOW]`:** `BackroomsLevel1Scene.unity` (`40 m × 24 m` at `Y = -50.0 m`) loads additively with flickering fluorescent fixtures, central pillared hall, NE return door, 13 dispersed artifacts, and 8 Reality-Bridge prop models.
- **`[PROPOSED FOR SLICE]`:**
  - **Mandatory IP Replacement:** Replace all third-party horror franchise artifacts and trademarked consumer/newspaper props with **100% original borderland/anomalous artifacts and generic diegetic devices** (retaining original assets such as the *Paquimé* mask and *Anomalous Astral Compass*).
  - Wire at least **1–2 Reality-Bridge props** (such as the Juárez telemetry wall monitor or thermal printer) to display deterministic local World Core clock/corridor state offline so the "living world beyond reality" theme pays off diegetically.

### Beat 5 — Evidence Board & Level-Ω Safe Puzzle
- **`[EXISTING NOW]`:** Inspecting **Evidence Corkboard PDN-00** (South-Central alcove) reveals the reverse code `654321`; entering `654321` on the **Level-Ω Vault Safe** (NW room) swings open the safe door.
- **`[PROPOSED FOR SLICE]`:**
  - Record the Evidence Board clue automatically in the player's **Discovery Journal** so players do not have to memorize codes across dark corridors.
  - Ensure all Evidence Board notes and visual graphics are IP-clean and legible in-world without relying solely on raw IMGUI popups.

### Beat 6 — Discovery of *El Ojo del Umbral*
- **`[EXISTING NOW]`:** Player picks up the rotating obsidian-and-brass **Dimensional Astrolabe Keystone (*El Ojo del Umbral*)** from inside the open safe (`TryTakeAstrolabeFromSafe()`).
- **`[PROPOSED FOR SLICE]`:**
  - Promote *El Ojo del Umbral* from a transient boolean to a **persistent key-item inventory entry** with an audiovisual pickup beat and subtle guidance toward the SW chamber (complementing the *Anomalous Astral Compass* already pointing SW).

### Beat 7 — Activation of *El Umbral de Lasser*
- **`[EXISTING NOW]`:** Placing *El Ojo del Umbral* into `portal:cuenca-octagonal` in the far SW chamber retracts the containment chains, activates the cyan vortex shader/spinning rings, and updates the HUD banner (`SecretRoomDimensionVaultController.cs`). **No scene transition occurs.**
- **`[PROPOSED FOR SLICE]`:**
  - Enhance the visual activation with a dramatic audiovisual crescendo (harmonic resonance, light pulse, camera micro-shake, updated journal revelation entry) so activating the portal feels like an unmistakable climax — while remaining honest that the realm beyond *El Umbral de Lasser* is reserved for the full commercial game.

### Beat 8 — Satisfying Proposed Slice Ending
- **`[EXISTING NOW]`:** None. Player can only remove the astrolabe or walk back to the NE exit door and return to Av. Juárez with all Backrooms state reset.
- **`[PROPOSED FOR SLICE]`:**
  - After activating *El Umbral de Lasser*, persist the "Threshold Awakened" state, guide the player back through the NE return door onto Avenida Benito Juárez (optionally showing a subtle environmental shift or closing journal reflection), and present a clean **Vertical Slice Complete** screen summarizing discoveries found, with options to **Continue Exploring** or **Return to Title Screen**.

---

## 3. Slice Engineering Prerequisites (`PLANNED`)

To deliver *Slice 01* cleanly for evaluation and playtesting:
1. **IP & Commercial Clearance Pass** (remove/replace all `CRITICAL` and `HIGH` risk items listed in [IP & Asset Provenance](IP_AND_ASSET_PROVENANCE.md)).
2. **Self-Contained Launch & Offline Core** (remove the need for evaluators/players to run PowerShell/Node manually, or provide a zero-friction bundled launcher with 100% offline fallback).
3. **Lightweight Adventure Framework** (Title screen, Discovery Journal, Persistent Key-Item Inventory, Local JSON Save/Load, Slice Completion state).
4. **Targeted UI & Audio Polish** (clean readable HUD/prompts, basic surface/underground/liminal ambience, footsteps, and puzzle SFX).
5. **Debug Mode Gating & Performance Pass** (lock `[V]`/`[G]` aerial modes behind a dev flag; verify stable 30–60 FPS and crisp lighting/shadows on target Windows GPUs).

---

## 4. Narrative & Design Decisions Requiring Creator Approval (`AUTHOR DECISION REQUIRED`)

The following artistic and narrative choices are intentionally left open for the creator rather than invented here:
1. **Opening Motivation:** What specific inciting hook greets the player on Avenida Juárez at the start of *Slice 01*?
2. **Underground Clue Content & Narrative Voice:** Who left the clues in *La Cisterna* and *Sector C-0*, and what exact puzzle yields the Building A South keypad code?
3. **Mystery Explanation Level:** How much of the underlying lore behind *El Ojo del Umbral* and *El Umbral de Lasser* should be explained in *Slice 01* versus held back as an intriguing mystery?
4. **Final Slice Revelation:** What exact closing visual beat, journal note, or teaser message concludes *Slice 01* after *El Umbral de Lasser* ignites?
5. **Azucenas Manhole Easter Egg Handling:** Should Manhole #1's silent ceiling drop into Backrooms remain discoverable at any time in *Slice 01*, or unlock after the primary Secret Room sequence?
