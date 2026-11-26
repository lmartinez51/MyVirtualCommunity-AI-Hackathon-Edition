# REMANENCIA — Nivel 0: Hotel de las Palomas Narrative Design

**Project:** My Virtual Community — *A Living World Beyond Reality.*  
**Universe:** **Remanencia**  
**Territory:** **Nivel 0 — Hotel de las Palomas**  
**Target Scene:** `unity-client/Assets/Scenes/BackroomsLevel1Scene.unity`  
**Document Status:** First Environmental Narrative Layer (**Implemented in 07.6**; future directions clearly marked as proposed)

> **Fictional & Historical Separation Notice:** Every narrative document, architectural discrepancy, maintenance entry, anomalous residue, and Cobalt-60 construction rumor described in this document is **original game fiction** created exclusively for the dimensional counterpart of **Hotel de las Palomas** inside **Remanencia**. None of these elements claim historical facts about the real-world building in Ciudad Juárez, its owners, its construction workers, or the real 1983 Ciudad Juárez Cobalt-60 incident.

---

## 1. Fictional History of the Dimensional Hotel *(FICTIONAL — REMANENCIA CANON)*

Within **Remanencia**, **Nivel 0 — Hotel de las Palomas** does not preserve a single verifiable construction timeline. Instead, the structure behaves as an architectural composite that appears to have remained under unfinished construction for an indeterminate span:

- **Coexisting Construction Stages:** The ground floor holds areas that look nearly ready for interior finishing (Reception, plastered guest corridors, numbered rooms `101`–`108`) directly adjacent to raw cast-concrete shells (`Room 110`, the unfinished central Courtyard lightwell, and the North Wing housekeeping core).
- **Contradictory Records:** Fragments left behind on walls describe a hotel that was never completed yet also suggest rooms were logged before work ended, repairs appeared prior to recorded damage, and drawings include spaces occupied today by solid concrete.
- **No Final Explanation:** The first narrative layer intentionally establishes unease, ambiguity, and layered questions without confirming whether the hotel's contradictions stem from dimensional memory drift, incomplete records, unverified contamination, or an unknown third cause.

---

## 2. Memory, Place, and Architectural Contradictions *(FICTIONAL)*

Remanencia forms gradually from the accumulated memories, rumors, stories, and physical traces attached to real places. Because collective memory is fragmentary and contradictory:

- Architectural drawings can remember a corridor or room that the concrete structure never built—or that no longer connects here.
- Paperwork can preserve expectations of guests and numbered rooms (`107`–`110`, plus an unbuilt `111-B`) that never arrived in reality.
- Maintenance records can log measurements (`+1.4 m` discrepancy) and out-of-sequence repairs as the building's geometry subtly shifts or refuses to settle into one consistent state.

---

## 3. Role of the Cobalt-60 Rumor *(FICTIONAL RUMOR — UNVERIFIED IN-WORLD)*

Inside this fictional dimensional counterpart, an unsigned pencil note introduces a local hearsay rumor that part of the rebar allegedly arrived contaminated with **Cobalt-60**, while explicitly admitting that **nobody has ever shown proof** and noting the stranger observation that dry stains continue reappearing even where material was removed.

- **Narrative Function:** Introduces regional borderland dread and grounded skepticism without reducing Remanencia to simple real-world industrial realism or cartoon sci-fi radiation.
- **Deliberate Ambiguity:** The player cannot prove whether the rumor is pure urban legend projected into Remanencia by collective memory, a misinterpretation of an anomalous material process, or a partial clue toward deeper phenomena in future increments.

---

## 4. Explicit Separation Between Real-World History and Game Fiction

| Domain | Real-World Building (Ciudad Juárez) | Fictional Dimensional Counterpart (Remanencia Nivel 0) |
| :--- | :--- | :--- |
| **Architectural Status** | Historical unfinished hotel project later associated with redevelopment proposals. | Fictional parallel spatial echo combining multiple unfinished construction eras (`40 m × 24 m` ground floor + `32 m × 22 m` North Wing M1). |
| **Ownership, Workers & Guests** | Not referenced, invented, or implicated in any way. | Anonymous, unsigned fragments only; zero real owners, companies, government seals, or real individuals. |
| **Cobalt-60 Contamination** | **No claim is made** that the real Hotel de las Palomas contained radioactive steel or was contaminated by the real 1983 Cobalt-60 accident. | Strictly a **fictional, explicitly unverified rumor** written on an anonymous pencil note (`Hotel_Evidence_Cobalt60RumorNote`). |
| **Documents & Records** | No real blueprints, ledgers, or medical/contamination records are reproduced or claimed. | All four wall-mounted documents are original fictional game props. |

---

## 5. Four Environmental Evidence Discoveries & Placements *(IMPLEMENTED)*

All four discoveries are non-sequential, optional informational records integrated into `SecretContainmentArtifactDescriptor`, `PlayerProgressionService`, `DiscoveryJournalCatalog`, and offline JSON persistence (`schemaVersion = 1`). None unlock doors, generate codes, or gate existing progression.

| # | Internal Progression ID | GameObject (`Hotel_Narrative_Evidence/*`) | Functional Zone | World Position `(X, Y, Z)` | Visual Presentation |
| :-: | :--- | :--- | :--- | :---: | :--- |
| **01** | `clue:remanencia-hotel-plan-01` | `Hotel_Evidence_UnfinishedProjectPlan` | Reception Alcove (North wall, facing South) | `(8.85, -48.52, 11.83)` | Faded slate-blue blueprint fragment taped directly to concrete wall; depicts phantom corridor/room beyond solid wall. |
| **02** | `clue:remanencia-hotel-register-02` | `Hotel_Evidence_RoomRegistrationRecord` | North Guest Corridor between Rooms `109` & `110` (facing South) | `(-3.85, -48.56, 28.67)` | Torn aged ledger sheet taped to corridor wall; lists `107`–`110` plus contradictory `111-B` entry. |
| **03** | `clue:remanencia-hotel-maintenance-03` | `Hotel_Evidence_MaintenanceRecord` | North Wing Maintenance Access Corridor beside service panel (facing West) | `(-8.73, -48.54, 22.45)` | Smudged lined maintenance log sheet pinned beside concrete joints and service panel box. |
| **04** | `clue:remanencia-hotel-cobalt-rumor-04` | `Hotel_Evidence_Cobalt60RumorNote` | Housekeeping Utility Room, deep back-of-house (facing South) | `(-12.75, -48.62, 23.27)` | Small tilted, torn-edge scrap note with folded corner and faint pencil scrawl; zero letterhead or official seals. |

---

## 6. Final Bilingual Text for All Four Discoveries *(IMPLEMENTED)*

### Evidence 01 — `clue:remanencia-hotel-plan-01`
- **Title (ES):** `Plano de Obra Inconclusa — Nivel de Acceso`
- **Title (EN):** `Unfinished Project Plan — Ground Level`
- **In-World Inspection Text (ES):** `"Pliego de obra pegado al muro sin firma ni fecha definitiva. Muestra el vestíbulo y el patio norte, pero traza un pasillo adicional y una habitación donde hoy existe un muro macizo de concreto."`
- **In-World Inspection Text (EN):** `"Unsigned, undated construction sheet taped to the wall. It charts the lobby and north courtyard, yet draws an extra corridor and room where a solid concrete structural wall stands today."`
- **Discovery Journal Entry (ES):** `"Plano arquitectónico de obra inconclusa hallado junto a Recepción. Aunque coincide parcialmente con la planta actual, traza un pasillo y una estancia donde hoy existe un muro macizo de concreto; discrepancia sin explicación confirmada."`
- **Discovery Journal Entry (EN):** `"Unfinished architectural plan found near Reception. While it partially matches the current ground floor, it maps an extra corridor and room where a solid concrete wall stands today—an unexplained discrepancy."`

### Evidence 02 — `clue:remanencia-hotel-register-02`
- **Title (ES):** `Hoja de Registro de Habitaciones (Fragmento)`
- **Title (EN):** `Room Registration Sheet (Fragment)`
- **In-World Inspection Text (ES):** `"Registro arrancado de una bitácora sin encabezado. Anota las habitaciones 107 a 110 como asignadas, junto a un cuarto '111-B' y fechas incompletas de estancias previas a que la obra concluyera."`
- **In-World Inspection Text (EN):** `"Torn ledger page with no letterhead. Lists Rooms 107 through 110 as assigned alongside a '111-B' entry, bearing incomplete stay dates recorded before construction was ever finished."`
- **Discovery Journal Entry (ES):** `"Hoja dañada de registro hallada junto a las habitaciones 109 y 110. Anota cuartos aún vacíos y una habitación inexistente ('111-B') con fechas incompletas anteriores al fin de la obra; contradicción sin causa verificada."`
- **Discovery Journal Entry (EN):** `"Damaged room ledger sheet found near Rooms 109 and 110. Lists unfinished empty rooms alongside a non-existent '111-B' entry with incomplete dates recorded before construction ended—an unresolved contradiction."`

### Evidence 03 — `clue:remanencia-hotel-maintenance-03`
- **Title (ES):** `Parte de Mantenimiento — Obra Norte`
- **Title (EN):** `Maintenance Log — North Wing Works`
- **In-World Inspection Text (ES):** `"Hoja de servicio fijada junto al tablero: 'Manchas grisáceas en juntas secas del concreto. Cortes eléctricos sin falla en línea. Dos resanes anotados el día 14 ya estaban hechos antes del daño; pasillo mide 1.4 m más que en plano.'"`
- **In-World Inspection Text (EN):** `"Service sheet fixed near the panel: 'Greyish stains along dry concrete joints. Power drops with no line fault. Two patches logged on the 14th appeared before the damage; corridor measures 1.4 m longer than drawn.'"`
- **Discovery Journal Entry (ES):** `"Registro de servicio localizado en el pasillo de mantenimiento del Ala Norte. Reporta manchas grisáceas en juntas secas del concreto, cortes eléctricos sin falla, resanes previos al daño y medidas que no cuadran con los planos."`
- **Discovery Journal Entry (EN):** `"Service log located in the North Wing maintenance corridor. Reports greyish stains along dry concrete joints, unsourced power drops, repairs logged before damage occurred, and dimensions that contradict the drawings."`

### Evidence 04 — `clue:remanencia-hotel-cobalt-rumor-04`
- **Title (ES):** `Nota Anónima — Rumor de Obra`
- **Title (EN):** `Anonymous Note — Construction Rumor`
- **In-World Inspection Text (ES):** `"Nota doblada a lápiz sin firma: 'Dicen que parte de la varilla llegó contaminada. Algunos mencionan Cobalto-60, pero nadie ha mostrado pruebas. Lo extraño es que las manchas siguen apareciendo incluso donde ya se retiró el material.'"`
- **In-World Inspection Text (EN):** `"Unsigned folded pencil note: 'Word is some of the rebar arrived contaminated. A few mention Cobalt-60, though nobody has shown proof. The strange part is the stains keep returning even where the material was removed.'"`
- **Discovery Journal Entry (ES):** `"Nota anónima a lápiz sin sustento oficial hallada al fondo del área de servicio. Recoge un rumor no verificado sobre supuesta varilla contaminada con Cobalto-60 dentro de esta versión del hotel; sin pruebas, deja abierto el origen real de las manchas."`
- **Discovery Journal Entry (EN):** `"Unsigned pencil note with no official source found deep in the back-of-house service core. Passes along an unverified rumor about allegedly Cobalt-60-contaminated rebar inside this version of the hotel; offers no proof and leaves the stains' true cause open."`

---

## 7. Meaning of Each Discovery in the Larger Mystery

1. **Unfinished Project Plan (`01`):** Anchors the spatial premise early in Reception—Remanencia's hotel remembers blueprints that disagree with its own solid concrete.
2. **Room Registration Sheet (`02`):** Extends the contradiction from geometry into time and occupation—who logged unfinished rooms and an unbuilt `111-B`?
3. **Maintenance Log (`03`):** Introduces physical material unease—dry joint discoloration, unsourced power drops, acausal patches, and metric drift (`+1.4 m`).
4. **Anonymous Rumor Note (`04`):** Bridges the physical anomalies to local borderland folklore without confirming it, showing that rumors themselves may persist inside Remanencia even when unsupported by proof.

---

## 8. Subtle Environmental Anomaly Design *(IMPLEMENTED — NON-COLLIDING, NON-EMISSIVE)*

Parented under `Hotel_Narrative_Evidence/Hotel_Subtle_Anomalies`, nine restrained, matte architectural traces reinforce the documents visually with zero neon glow, zero radiation symbols, zero added lights, and zero biological/fungal structures:

- **`Anomaly_ServiceJointDeposit_A/B` & `Anomaly_ServiceJointStainHalo`:** Dry mineral-grey encrustations creeping along concrete formwork joints immediately beside the Maintenance Log (`X = -8.72, Z = 22.08..22.86`).
- **`Anomaly_ServiceFloorUnderwallSeep_W/E`:** Muted dark surface stain appearing on both the Maintenance Corridor (`X = -8.96`) and West Arcade (`X = -8.22`) sides of the solid concrete divider wall (`X = -8.6`), suggesting a continuous stain passing beneath solid masonry.
- **`Anomaly_UtilityWallDiscoloration_A` & `Anomaly_UtilityWallMineralEdge`:** Dry non-water-pattern wall discoloration near the Utility Room copper risers and anonymous rumor note.
- **`Anomaly_NorthCorrConcreteMismatch`:** Subtly mismatched concrete tone beneath the Room Registration Sheet between Rooms `109` and `110`.
- **`Anomaly_CourtyardPlinthResidue`:** Faint dry mineral streak along the West face of the Central Courtyard concrete plinth.

---

## 9. Questions Deliberately Left Unanswered

- Why do the hotel's drawings, room numbers, and measured corridor lengths contradict one another?
- Who wrote the unsigned notes and registration entries—former workers, imagined guests, or residual traces left by Remanencia itself?
- Why do stains return along dry concrete joints even where materials were allegedly replaced?
- Is the Cobalt-60 rumor merely collective hearsay folded into the dimension, or a distorted memory of something else altering the building?

---

## 10. Potential Future Narrative Directions *(PROPOSED — NOT IMPLEMENTED)*

- **Upper/Lower Floors via Future Elevator B & Unfinished Shaft A:** Discovering earlier or later architectural strata where the missing corridor from Plan `01` or Room `111-B` from Register `02` actually manifests.
- **Material & Environmental Escalation:** Future increments may evolve the dry mineral joint anomalies into deeper spatial distortions or carefully scoped ecological/anomaly systems once approved by the creator.
- **Cross-Place Remanencia Echoes:** Connecting the hotel's West or South future expansion anchors to other remembered northern-Mexican architectural fragments.
