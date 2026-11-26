# REMANENCIA — Nivel 0: First Faunal Traces Design

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia**  
**Territory:** **Nivel 0 — Hotel de las Palomas**  
**Target Scene:** `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`  
**Document Status:** Intermediate Static Ecological Layer (**Implemented in 07.8**; living fauna and future ecological stages explicitly deferred)

> **Historical, Fictional & Scientific Boundary Notice:**  
> - **Real-world historical inspiration:** The unfinished Hotel de las Palomas in Ciudad Juárez serves solely as architectural inspiration. No biological anomalies, animal traces, or radioactive contamination are claimed about the real building or the real 1983 Ciudad Juárez Cobalt-60 incident.  
> - **Fictional rumor (07.6):** The unsigned Cobalt-60 rebar rumor (`Hotel_Evidence_Cobalt60RumorNote`) remains an unverified in-world hearsay fragment.  
> - **Fictional Remanencia phenomena (07.7–07.8):** The fungal colonies, crevice flora, and subtle faunal traces belong strictly to the fictional dimensional ecology of Remanencia. None of these biological traces establish, imply, or scientifically claim Cobalt-60 radiation as their cause.

---

## 1. Narrative Purpose of This Stage *(IMPLEMENTED — STATIC)*

Implementation **07.8 — First Faunal Traces** establishes a restrained intermediate ecological threshold between the purely sessile fungi and crevice vegetation introduced in Implementation 07.7 and any potential future appearance of living fauna.

- **Environmental Premise:** Players exploring closely around damp concrete seams, conduit drops, and sheltered plumbing corners begin noticing faint, physically plausible signs that something very small has occupied and moved through the unfinished hotel.
- **Deliberate Ambiguity:** At first glance from across a room, several traces can be mistaken for ordinary construction dust, mineral debris, or frayed plaster. At close inspection (`0.5–2.0 m`), their organic filaments, dry empty cuticle shards, and compact micro-nesting residue raise quiet questions without confirming whether they belong to ordinary small organisms, something native to Remanencia, or an unknown third process.
- **No Living Fauna Yet:** Zero living insects, spiders, rodents, animals, creatures, or animated entities are present. Restraint preserves the grounded architectural dread of an abandoned hotel slowly developing an unfamiliar ecosystem rather than shifting prematurely into an infestation or horror-enemy encounter.

---

## 2. Relationship to 07.6 Mineral Anomalies & 07.7 Early Ecology

The three environmental layers form a coherent, additive physical stratigraphy without altering or renaming earlier work:

1. **07.6 — Mineral & Surface Anomalies (`Hotel_Narrative_Evidence/Hotel_Subtle_Anomalies`):** Feathered concrete fractures, salt efflorescence, and damp under-wall seepage establish the physical micro-climates where moisture and mineral chemistry concentrate.
2. **07.7 — Early Anomalous Ecology (`Hotel_Early_Ecology`):** Mycelial networks, fruiting colonies, polypore brackets, gnarled roots, and desert-sage rosettes colonize those exact architectural seams.
3. **07.8 — First Faunal Traces (`Hotel_Faunal_Traces`):** Independent sibling hierarchy anchored to sheltered recesses along those same 07.6/07.7 micro-habitats—suggesting small-scale mobile life exploiting the humidity, pipe chases, roots, and fungal edges already present.

All four 07.6 narrative documents (`Hotel_Evidence_*`) and all 23 objects under `Hotel_Early_Ecology` remain completely unmodified, unobstructed, and visually dominant over the restrained 07.8 traces.

---

## 3. Approved Visual Vocabulary & Scale *(3 Reusable Meshes, 0 New Textures)*

All 16 placed traces reuse strictly **3 procedural `.asset` meshes** under `Assets/Meshes/Remanencia/FaunalTraces/` and **3 opaque non-emissive URP Lit materials** reusing existing `512×512` 07.7 procedural textures (`0` bytes of added texture VRAM):

| Element | Mesh Asset | Material (Reused Texture) | Scale & Visual Reading | Explicit Restraints |
| :--- | :--- | :--- | :--- | :--- |
| **A. Fine Biological Filaments** | `M_Remanencia_FaunalFilament.asset` | `Remanencia_Faunal_Filament.mat` (`T_Remanencia_Ecology_FungalCap.png`, pale mineral-bone tint, `smoothness = 0.20`) | 5 short, discontinuous, asymmetric catenary-sagging strands (`0.09–0.24 m` span, `Ø 3.4–7.2 mm` flared anchor tufts) + 2 irregular frayed catch-threads bridging narrow seams and pipe gaps. | Zero concentric spider-web rings, zero decorative triangular cobweb sheets, zero glow. |
| **B. Shed Cuticle Fragments** | `M_Remanencia_FaunalCuticleFragment.asset` | `Remanencia_Faunal_Cuticle.mat` (`T_Remanencia_Ecology_FungalCap.png`, dry parchment-taupe tint, `smoothness = 0.34`) | 4 tiny, empty, dry, C-curled semi-chitinous shell/husk shards (`0.024–0.046 m` length) with subtle transverse ripples and pale translucent-looking edges caught in sheltered corners. | Zero complete insect bodies, legs, wings, antennae, heads, or identifiable species silhouettes. |
| **C. Micro-Nesting Residue** | `M_Remanencia_FaunalMicroResidue.asset` | `Remanencia_Faunal_Residue.mat` (`T_Remanencia_Ecology_VegetalRoot.png`, muted earthy taupe tint, `smoothness = 0.08`) | Compact low-profile seam crust (`~0.23 × 0.016 × 0.14 m`) embedded into the wall-floor junction with 24 tiny dry organic granules (`4–12 mm`) clustered along the crevice margin. | Zero large nests, burrow holes, egg sacs, large mounds, or enemy spawn cues. |

---

## 4. Exact Scene Zones & Anchor Positions *(IMPLEMENTED)*

Organized strictly under sibling root `BackroomsLevel1_Root/Hotel_Faunal_Traces` across **three zones** (Zone D — Room 110 plumbing chase remains intentionally untouched to preserve restraint and spatial gradation):

### Zone A — Courtyard (`Faunal_Courtyard_Traces`, 4 objects)
| GameObject | Type | World Position `(X, Y, Z)` | Anchor Context |
| :--- | :--- | :---: | :--- |
| `Faunal_Courtyard_PlinthSeam_Filaments` | Filaments | `(-1.104, -49.91, 21.62)` | Protected West plinth concrete seam beside `Eco_Courtyard_PlinthRootCluster_W` (off walkways and leaf tips). |
| `Faunal_Courtyard_PierSW_Filaments` | Filaments | `(-2.478, -49.89, 20.48)` | Sheltered seam between SW colonnade pier and crevice roots. |
| `Faunal_Courtyard_PlinthBase_Cuticle` | Cuticle | `(-1.17, -49.996, 22.06)` | Tiny empty dry shell shards tucked behind plinth root crown recess. |
| `Faunal_Courtyard_PierNE_Cuticle` | Cuticle | `(4.56, -49.996, 25.64)` | Tiny dry fragments caught in sheltered NE corner pier recess. |

### Zone B — Maintenance Corridor (`Faunal_Maintenance_Traces`, 5 objects)
*(`Hotel_Evidence_MaintenanceRecord` at `(-8.73, -48.54, 22.45)` remains over `0.55 m` clear in every axis.)*
| GameObject | Type | World Position `(X, Y, Z)` | Anchor Context |
| :--- | :--- | :---: | :--- |
| `Faunal_Maint_ConduitStub_Filaments` | Filaments | `(-8.712, -49.28, 21.62)` | Short discontinuous strands anchoring across the sheared lower conduit stub to the concrete wall near `Eco_Maint_PanelLower_Mycelium`. |
| `Faunal_Maint_JointMycelium_Filaments` | Filaments | `(-8.712, -49.76, 22.04)` | Fine strands bridging lower crack A (`Anomaly_ServiceJointDeposit_A`) and baseboard lip above the floor mushroom cluster. |
| `Faunal_Maint_BracketCrack_Filaments` | Filaments | `(-8.712, -49.56, 22.88)` | Subtle strands tucked beneath the crack B polypore shelf brackets. |
| `Faunal_Maint_WallFloorJoint_ResidueA` | Residue | `(-8.775, -49.997, 21.84)` | Compact dry organic accumulation embedded in the wall-floor baseboard seam. |
| `Faunal_Maint_WallFloorJoint_ResidueB` | Residue | `(-8.775, -49.997, 22.86)` | Very small secondary seam deposit directly below crack B brackets. |

### Zone C — Housekeeping Utility Room (`Faunal_Utility_Traces`, 7 objects)
*(Densest concentration, localized strictly to the sheltered SW pipe-riser corner `> 5.2 m` from `Hotel_Evidence_Cobalt60RumorNote` at `(-12.75, -48.62, 23.27)`.)*
| GameObject | Type | World Position `(X, Y, Z)` | Anchor Context |
| :--- | :--- | :---: | :--- |
| `Faunal_Utility_RiserBridge_FilamentsA` | Filaments | `(-15.72, -49.48, 18.585)` | Discontinuous strands anchored directly between copper riser A (`Z=18.45`) and galvanized riser B (`Z=18.72`). |
| `Faunal_Utility_ValveBracket_FilamentsB` | Filaments | `(-15.74, -49.14, 18.48)` | Short strands beneath the copper valve and upper polypore shelf bracket. |
| `Faunal_Utility_LowerPipe_FilamentsC` | Filaments | `(-15.864, -49.84, 18.78)` | Low-level strands anchored against the West concrete wall beside riser B and the satellite fungal colony. |
| `Faunal_Utility_RiserBase_ResidueA` | Residue | `(-15.76, -49.997, 18.30)` | Embedded dry organic micro-residue pressed into the sheltered SW concrete corner behind copper riser A. |
| `Faunal_Utility_ColonyEdge_ResidueB` | Residue | `(-15.77, -49.997, 18.80)` | Compact granular margin tucked along the West baseboard between riser B and the satellite colony. |
| `Faunal_Utility_CornerBehindPipe_CuticleA` | Cuticle | `(-15.62, -49.996, 18.24)` | Empty dry curled cuticle shards caught along the sheltered South baseboard beside the risers. |
| `Faunal_Utility_PipeFlange_CuticleB` | Cuticle | `(-15.74, -49.996, 19.14)` | Second small cluster of dry shell fragments caught against the West wall baseboard north of the colony. |

---

## 5. Implementation Boundaries & Preserved Systems

- **Pure Static Geometry Only:** Zero colliders, zero `Rigidbody`, zero `MonoBehaviour`, zero `Light` (`18` total scene lights, `0` shadow-casting), zero `AudioSource`, zero `ParticleSystem`, zero `Animator`.
- **Zero Gameplay/Save/Narrative Expansion:** Zero new progression IDs, Discovery Journal records, save schema fields (`schemaVersion = 1` unchanged), interactions, triggers, exposure/infection mechanics, or radiation meters.
- **Scene & Asset Preservation:** Sibling root `Hotel_Faunal_Traces` adds only its parent-child reference to `BackroomsLevel1_Root`'s `Transform`. All existing 07.2–07.7 GameObjects, `fileID`s, and `.meta` GUIDs remain untouched.

---

## 6. Intended Future Progression *(PROPOSED — NOT IMPLEMENTED)*

Without prematurely defining species, taxonomy, or creature designs, future separately authorized increments may build upon this ecological gradient by:
1. Exploring subtle non-hostile micro-audio cues or rare ambient environmental signs deeper inside upper/lower hotel levels accessed via future vertical shafts.
2. Evaluating whether any small living fauna should eventually appear—and under what ecological rules—only after creator review of the static 07.7–07.8 visual vocabulary in PlayMode.
