# REMANENCIA — Nivel 0: First Living Microfauna Design

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia**  
**Territory:** **Nivel 0 — Hotel de las Palomas**  
**Target Scene:** `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`  
**Document Status:** First Ambient Living Ecological Stage (**Implemented in 07.9**; species taxonomy, swarms, enemies, and biological gameplay explicitly deferred)

> **Historical, Fictional & Scientific Boundary Notice:**  
> - **Real-world inspiration:** The unfinished Hotel de las Palomas in Ciudad Juárez serves strictly as architectural inspiration. No living anomalous fauna, infestation, or radioactive contamination is claimed about the real building or the real 1983 Ciudad Juárez Cobalt-60 incident.  
> - **Fictional rumor (07.6):** The unsigned Cobalt-60 rebar rumor (`Hotel_Evidence_Cobalt60RumorNote`) remains an unverified in-world hearsay fragment.  
> - **Fictional Remanencia phenomena (07.7–07.9):** The fungal colonies, crevice flora, faunal traces, and tiny ambient microfauna belong strictly to Remanencia's fictional dimensional ecology. Nothing in this stage claims, implies, or scientifically attributes these organisms to Cobalt-60 radiation.

---

## 1. Transition from 07.8 Faunal Traces to Observed Life *(IMPLEMENTED)*

Implementations **07.6**, **07.7**, and **07.8** established a progressive environmental stratigraphy:
1. **07.6 — Mineral & Surface Anomalies:** Weeping concrete joints, salt efflorescence, and under-wall seepage.
2. **07.7 — Early Anomalous Ecology:** Mycelial webs, fruiting colonies, polypore brackets, crevice roots, and desert-sage rosettes.
3. **07.8 — First Faunal Traces:** Discontinuous biological filaments, dry curled cuticle shards, and embedded seam micro-residue.
4. **07.9 — First Living Microfauna:** For the first time, the player may briefly observe a tiny, living, unidentified organism slip out of an architectural recess, crawl `~15–18 cm` across concrete, pause momentarily, and retreat completely out of sight.

The moment is intentionally quiet, brief, and easy to miss—prompting questions (*Was that alive? Did it come from the fungi? Was it already here? Why did it hide?*) without providing any explanation, lore label, or monster reveal.

---

## 2. Intentionally Ambiguous Visual Design *(1 Shared Mesh, 1 Shared Material, 0 New Textures)*

- **Mesh Asset:** `Assets/Meshes/Remanencia/Microfauna/M_Remanencia_MicrofaunaBody.asset`
- **Material Asset:** `Assets/Settings/Remanencia_Microfauna_Body.mat` (reuses existing `T_Remanencia_Ecology_FungalCap.png`, muted mineral-taupe tint `(0.72, 0.67, 0.59)`, `smoothness = 0.28`, opaque, `_EMISSION` disabled).
- **Scale & Silhouette:**
  - Exact body length: **`3.6 cm` (`0.036 m`)**, width `1.1 cm`, dorsal height `0.85 cm`.
  - Smooth, elongated, subtly **7-segmented fusiform carapace** arched gently along the dorsal surface with a flattened ventral belly and **4 pairs of tiny (`2.6 mm`) recessed ventral locomotion pads** tucked beneath the body.
  - **Deliberately Unclassifiable:** Zero large eyes, zero face, zero distinct head capsule, zero wings, zero antennae, zero mandibles, zero spikes, zero armor plates, and zero recognizable insect, spider, or larval silhouette.

---

## 3. Exact Encounter Positions & Concealment Paths *(2 Organisms Maximum)*

Parented strictly under independent sibling root `BackroomsLevel1_Root/Hotel_FirstLivingMicrofauna` (Zone A Courtyard and Zone D Room 110 intentionally contain zero living organisms):

### Primary Encounter — Zone C: Housekeeping Utility Room (`Microfauna_Utility_Encounter`)
- **Concealment Anchor (`waypoints[0]`):** `(-15.765, -49.999, 18.445)` — completely occluded behind **Copper Pipe Riser A** (`(-15.72, 18.45)`, `Ø 8.2 cm`) and inside its `Ø 16.0 cm` floor flange next to the West concrete wall (`X = -15.87`).
- **Emergence Bend (`waypoints[1]`):** `(-15.655, -49.999, 18.385)` — slips past the SE rim of the copper pipe flange along the damp seam beside `Faunal_Utility_RiserBase_ResidueA`.
- **Brief Pause (`waypoints[2]`):** `(-15.595, -49.999, 18.435)` — pauses `0.85 s` near the edge of the floor mycelium and dry cuticle shards (**`~17.5 cm` total excursion**) before reversing smoothly back behind Copper Riser A.
- **Evidence Clearance:** `> 5.2 m` away from `Hotel_Evidence_Cobalt60RumorNote` (`(-12.75, -48.62, 23.27)`).

### Secondary Encounter — Zone B: Maintenance Corridor (`Microfauna_Maintenance_Encounter`)
- **Concealment Anchor (`waypoints[0]`):** `(-8.712, -49.999, 21.845)` — completely occluded inside the dark concrete baseboard wall-floor seam (`X = -8.723`) behind the embedded micro-residue crust (`Faunal_Maint_WallFloorJoint_ResidueA`). Invisible from the corridor entrance.
- **Emergence Bend (`waypoints[1]`):** `(-8.815, -49.999, 21.890)` — slips briefly past the dark residue margin onto the concrete slab.
- **Brief Pause (`waypoints[2]`):** `(-8.855, -49.999, 21.955)` — pauses `0.75 s` near the outer margin of `Eco_Maint_FloorSeepMycelium_W` (**`~18.8 cm` path length**) before retreating cleanly back inside the baseboard recess.
- **Evidence Clearance:** `> 1.45 m` below `Hotel_Evidence_MaintenanceRecord` (`(-8.73, -48.54, 22.45)`).

---

## 4. Deterministic Ambient Behavior & Technical Boundaries

Driven by `RemanenciaAmbientMicrofaunaController` (`unity-client/Assets/Scripts/RemanenciaAmbientMicrofaunaController.cs`):

1. **State Machine (`Concealed -> IdleWait -> Emerging -> Paused -> Retreating -> Cooldown`):**
   - Remains completely hidden at `waypoints[0]` while the player is beyond `activationRadius` (`4.2 m` Utility / `3.6 m` Maintenance).
   - On approach, waits a deterministic pseudo-irregular idle interval (`1.15–2.55 s` via deterministic hash of `cycleIndex` & `phaseSeed`, zero `UnityEngine.Random`).
   - Crawls smoothly along its serialized spline (`0.076–0.082 m/s`) with subtle cosmetic lateral undulation (`±4.5°`) and axial peristaltic pulse (`±5.5%`).
   - Pauses briefly (`0.75–0.85 s`) with a tentative heading turn (`±14°`), then retreats at `1.45–1.50×` speed cleanly back to `waypoints[0]`.
   - If the player steps within `startleRetreatRadius` (`0.85 m`) while visible, smoothly switches to `Retreating` from its exact current `pathProgress` (zero teleportation).
   - Enters a `11.5–17.0 s` deterministic concealed cooldown before any subsequent cycle.
2. **Camera Integration (Zero Controller Refactoring):** Lazily caches `Camera.main.transform` (falling back to `FindFirstObjectByType<FirstPersonPlayer>()?.transform`, throttled `>= 1.0 s` if null). Zero modifications to `FirstPersonPlayer.cs` and zero per-frame scene searches.
3. **Zero Persistence & Zero Gameplay Coupling:** Ephemeral in-memory state only; zero World Core calls, zero save/schema changes (`schemaVersion = 1`), zero colliders/triggers/physics/AI/audio/particles/lights/Animators.

---

## 5. Non-Classification & Narrative Restraint

- **Not an NPC, Enemy, or Real Species:** The organism has no health, combat, AI, pathfinding, detection meter, interaction prompt, dialogue, or taxonomic classification. It is pure ambient environmental life.
- **No Cobalt-60 Causal Explanation:** The organism does not glow, does not trigger radiation warnings, and does not validate or disprove the fictional Cobalt-60 rebar rumor.
- **Preserved Future Ecological Headroom:** By keeping the first observed organism tiny, solitary, non-hostile, and taxonomically ambiguous, future increments retain full creative freedom to decide whether similar organisms remain rare ambient background fauna or evolve along distinct Remanencia ecological branches.
