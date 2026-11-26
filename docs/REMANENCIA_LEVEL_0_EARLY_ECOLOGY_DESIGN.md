# REMANENCIA — Nivel 0: Early Anomalous Ecology Design

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia**  
**Territory:** **Nivel 0 — Hotel de las Palomas**  
**Target Scene:** `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`  
**Document Status:** First Static Ecological Manifestation (**Implemented in 07.7**; deferred categories & future evolution clearly marked as not implemented)

> **Scientific & Narrative Boundary Notice:** All fungal colonies and vegetal forms documented here are **original anomalous phenomena belonging strictly to the fictional dimensional reality of Remanencia**. No claim is made that real-world radiation or Cobalt-60 produces these biological manifestations. Their underlying origin remains deliberately unresolved.

---

## 1. Approved Initial Fungal Visual Language *(IMPLEMENTED — STATIC)*

Remanencia's early hotel fungi are adapted to raw formwork concrete, dry mineral joints, and protected plumbing chases rather than conventional woodland floors:

- **Mycelium (`M_Remanencia_MyceliumWeb.asset` + `Remanencia_Ecology_Mycelium.mat`):**
  - Recursive dendritic hyphal networks (`512×512` RGBA alpha-blended fan + 10 raised 3D tubular rhizomorph veins oriented outward along the host surface normal) radiating organically from concrete cracks, wall-floor junctions, and pipe penetrations without rectangular borders.
  - Palette: chalky bone-ivory cores feathering into weathered taupe margins (`smoothness = 0.08`, zero emission).
- **Low-Profile Fruiting Bodies (`M_Remanencia_FungalCluster_Low.asset` + `Remanencia_Ecology_FungalCap.mat`):**
  - Multi-tiered colonies of 15 sinuous tapered stems (`0.044–0.255 m` tall) crowned by 14-sided lobed caps with recessed, radially pleated dark sepia gill undersides and bright ivory rims shading toward warm ochre-taupe central umbos (`512×512` anatomical shading atlas for crisp 3D definition under flat ambient light).
- **Crevice Shelving Brackets (`M_Remanencia_FungalBracket.asset` + `Remanencia_Ecology_FungalCap.mat`):**
  - 7 overlapping, concentrically ridged polypore/oyster shelf fans with dark pored undersides emerging directly from concrete wall fissures and wrapping vertical plumbing risers.
- **Explicit Restraints:** Zero bioluminescent or radioactive glow, zero neon green, zero spores, zero animations, zero oversized fantasy mushrooms.

---

## 2. Approved Initial Vegetation Visual Language *(IMPLEMENTED — STATIC)*

Remanencia's early hotel vegetation exploits structural concrete crevices rather than blanketing the floor:

- **Exposed Crevice Roots (`M_Remanencia_VegetalRootRunner.asset` + `Remanencia_Ecology_VegetalRoot.mat`):**
  - 8 gnarled woody-mineral primary roots with forked secondary rootlets following concrete seams and dipping cleanly under plinth and column edges, shaded with circumferential bark relief (`512×512`).
- **Architectural-Crevice Foliage (`M_Remanencia_VegetalLeafTuft.asset` + `Remanencia_Ecology_VegetalLeaf.mat`):**
  - 3-tier rosettes of 22 sculpted V-channeled lanceolate blades (`0.14–0.50 m` tall) plus 3 slender woody-sage stalks, rendered with dark basal ambient occlusion grading into dusty desert sage and silvery mineral tips (`smoothness = 0.18`, zero emission).
- **Explicit Restraints:** No vibrant jungle greenery, no thick vines, no trees, no obstructed circulation paths. The building remains unmistakably an unfinished hotel.

---

## 3. Selected Environmental Zones & Spatial Hierarchy *(IMPLEMENTED)*

Organized under `BackroomsLevel1_Root/Hotel_Early_Ecology` with four subgroups establishing a deliberate spatial gradient:

| Zone | Subgroup | Ecological Presence | Anchored Coordinates `(X, Z)` | Role in Progression |
| :--- | :--- | :--- | :---: | :--- |
| **Zone A — Central Courtyard** | `Ecology_Courtyard_Vegetation` | 4 localized root-and-leaf clusters (plinth base W, plinth ceramic rim NW, SW sightline pier seam, NE corner pier seam) | `(-1.12, 21.74)`, `(-0.76, 22.36)`, `(-2.46, 20.44)`, `(4.54, 25.50)` | Most immediately recognizable organic emergence beneath the courtyard lightwell. |
| **Zone B — Maintenance Corridor** | `Ecology_Maintenance_Mycelium` | Branching wall-joint mycelium, under-wall floor mycelium on BOTH sides of solid wall `X = -8.6`, small base fruiting cluster, wispy tendrils under service panel | `(-8.72, 22.08)`, `(-8.86, 22.35)`, `(-8.36, 22.33)`, `(-8.78, 22.88)`, `(-8.72, 21.62)` | Subtle network visibly threading through structural seams and beneath solid concrete. |
| **Zone C — Housekeeping Utility Room** | `Ecology_Utility_FungalGrowth` | Protected corner colony around pipe risers A/B (2 fruiting clusters, floor & wall mycelium, 2 layered wall brackets) + low floor-baseboard mycelium well below the rumor note | `(-15.62, 18.58)`, `(-15.85, 18.86..18.92)`, `(-12.78, 23.16)` | Slightly denser fungal colonization inside a sheltered back-of-house service corner. |
| **Zone D — Room 110 (Optional Trace)** | `Ecology_Optional_GuestRoomTrace` | 1 isolated patch of fine mycelium + 1 tiny fruiting sprout tucked into the unfinished NW plumbing chase | `(-9.32, 33.54)`, `(-9.41, 33.66)` | Restrained hint that organic traces are beginning to reach unfinished guest rooms. |

---

## 4. Spatial Relationship with Implementation 07.6 Mineral Anomalies

The 9 Implementation 07.6 subtle surface anomalies (`Hotel_Subtle_Anomalies`) use feathered `512×512` RGBA conformal decals (`M_Remanencia_DecalQuad.asset`) with zero rectangular borders and zero glow, serving as organic visual anchors:

- `Anomaly_CourtyardPlinthResidue` (`-1.104, 21.82`) -> flanked at its floor seam by `Eco_Courtyard_PlinthRootCluster_W` & `LeafTuft_W`.
- `Anomaly_ServiceJointDeposit_A/B` & `Anomaly_ServiceJointStainHalo` (`-8.714, 22.06..22.88`) -> joined by `Eco_Maint_JointMycelium_WallA` & `JointBase_FungalCluster` (well below `Hotel_Evidence_MaintenanceRecord` at `Y = BaseY + 1.46`).
- `Anomaly_ServiceFloorUnderwallSeep_W/E` (`-9.02 & -8.18, 22.28`) -> mirrored on both sides of the solid divider wall by `Eco_Maint_FloorSeepMycelium_W/E`.
- `Anomaly_UtilityWallMineralEdge` (`-15.866, 18.62`) -> anchored by `Eco_Utility_WallBracket_Lower/Upper` and the riser corner colony.
- `Anomaly_UtilityWallDiscoloration_A` (`-12.75, 23.286`) -> paired strictly at the floor/baseboard seam (`1.38 m` below `Hotel_Evidence_Cobalt60RumorNote`) by `Eco_Utility_NorthBase_Mycelium`.

---

## 5. Visual Intensity, Performance & Non-Interaction Rules

- **Static Set Dressing Only:** Zero colliders, zero rigidbodies, zero `MonoBehaviour`s, zero `Light` components, zero `AudioSource`s, zero `ParticleSystem`s, zero new progression IDs or journal entries.
- **2 GB VRAM Discipline:** Reuses strictly 5 sculpted procedural `.asset` meshes and 4 matte `512×512` textures across 23 localized objects; scene lighting stays at exact `18` lights (`0` shadow-casting).
- **Evidence Accessibility:** All 4 narrative documents (`Hotel_Evidence_*`) remain completely uncovered and unobstructed within `2.75 m` player raycast inspection.

---

## 6. Historical & Scientific Boundaries

- The fictional Cobalt-60 construction rumor introduced in Implementation 07.6 is **not** confirmed as a scientific cause of fungal or vegetal growth.
- No radiation meters, exposure mechanics, contagion rules, or medical/scientific lore claiming real-world radioactive mutagenesis exist in this increment.

---

## 7. Explicitly Deferred Organism Categories *(NOT IMPLEMENTED)*

Out of Remanencia's five long-term ecological categories, **only Categories 1 and 2 are implemented**:
1. **Fungi** *(Implemented — static initial colonies)*
2. **Vegetation** *(Implemented — static crevice flora)*
3. **Insects** *(DEFERRED — zero assets, placeholders, or code)*
4. **Animals** *(DEFERRED — zero assets, placeholders, or code)*
5. **Human biological transformations** *(DEFERRED — zero assets, placeholders, or code)*

---

## 8. Future Ecological Development Possibilities *(PROPOSED — NOT IMPLEMENTED)*

- Subtle ambient micro-motion or wind/draft sway in courtyard foliage after creator review of static art direction.
- Deeper fungal stratification on upper/lower floors accessed via future vertical elevator increments.
- Gradual introduction of deferred organism categories only under dedicated, separately authorized design briefs.
