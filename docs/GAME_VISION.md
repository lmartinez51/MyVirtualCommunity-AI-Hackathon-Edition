# Game Vision â€” My Virtual Community

**Working Title:** My Virtual Community â€” *A Living World Beyond Reality.*
**Elevator Pitch:** *"An interconnected first-person exploration adventure where authentic border life conceals an extraordinary network of hidden worlds."*
**Document Status:** Working Design Definition (`CONFIRMED` technical baseline + `PLANNED` creative direction; open creative decisions explicitly marked `AUTHOR DECISION REQUIRED`).

Related active documents: [README](../README.md) Â· [Core Game Loop](CORE_GAME_LOOP.md) Â· [World Atlas](WORLD_ATLAS.md) Â· [Feature Status Matrix](FEATURE_STATUS_MATRIX.md) Â· [Vertical Slice](VERTICAL_SLICE.md)

---

## 1. Product Identity

**My Virtual Community â€” A Living World Beyond Reality.** is a **single-player, first-person exploration and mystery adventure** built in Unity 6 for Windows PC.

It is **not** an AI assistant, an MMO, or a persistent multiplayer metaverse. Its core proposition is a grounded, playable 3D world anchored in an authentic binational border city â€” **Avenida Benito JuÃ¡rez and the Paso del Norte International Bridge** between Ciudad JuÃ¡rez, Chihuahua, and El Paso, Texas â€” whose ordinary daytime routines conceal an extraordinary sequence of subterranean and liminal spaces waiting to be uncovered by an observant player.

---

## 2. Genre, Perspective & Target Player Experience

| Dimension | Working Definition | Status |
| :--- | :--- | :--- |
| **Genre** | First-person exploration & environmental mystery adventure | `PLANNED` (spatial foundation `CONFIRMED`) |
| **Perspective** | Grounded first-person (`1.60 m` standing eye level, handheld flashlight `[F]`, direct spatial interaction `[E]`) | `CONFIRMED` |
| **Primary Platform** | PC Windows (`x86_64`, Unity 6 Standalone) | `CONFIRMED` (developer build); self-contained consumer package `PLANNED` |
| **Play Mode** | Single-player, offline-capable core adventure | `PLANNED` (current build depends on local Node.js backend for full simulation; see [Feature Status Matrix](FEATURE_STATUS_MATRIX.md)) |
| **Target Player Experience** | Quiet everyday immersion shifting into eerie curiosity, atmospheric tension, spatial discovery, and puzzle-driven threshold crossings | `PLANNED` |
| **Tone & Pacing** | Deliberate, observant, atmospheric; blends warm binational urban realism with uncanny subterranean and liminal mystery â€” **not** combat-driven or jump-scare-reliant | `PLANNED` |

---

## 3. Creative Vision

Most exploration adventures invent fictional towns from scratch or strand players in surreal spaces without an emotional anchor. **My Virtual Community** deliberately builds its contrast from a real, deeply lived-in place: the historic downtown commercial corridor of Ciudad JuÃ¡rez leading onto the Paso del Norte pedestrian crossing toward El Paso.

The player first experiences a living border street â€” storefronts opening and closing on real schedules, commuters and attendants moving along deterministic routes, sunlight shifting across the viaduct and RÃ­o Bravo. Beneath that recognizable reality, however, another spatial architecture exists: a decades-old underground storm-drain network beneath the asphalt, a concealed doorway tucked into an ordinary building chamfer, and a non-Euclidean liminal labyrinth housing anomalous relics and a dormant dimensional threshold (*El Umbral de Lasser*).

The emotional arc of the game rests on **tonal contrast and spatial earned discovery**: the more ordinary and credible the surface city feels, the more startling it becomes when the world cracks open beneath your feet.

---

## 4. Core Design Pillars (Working Design Definition)

> **Note:** These pillars represent the current working design definition derived from the verified prototype and commercial positioning. Final pillar wording and creative prioritization remain subject to creator sign-off (`AUTHOR DECISION REQUIRED`).

1. **Authentic Border Sense of Place**
   - Ground the Main Reality layer in the real urban geometry, bilingual daily rhythm (Spanish / English), architectural vernacular, and cross-border atmosphere of Ciudad JuÃ¡rezâ€“El Paso.
   - Treat the border community with respect, specificity, and lived-in realism rather than caricature.
2. **Hidden Worlds Beneath Ordinary Reality**
   - Layer concealed subterranean and liminal realms directly beneath and behind everyday landmarks, rewarding players who observe details, follow environmental clues, and question architectural anomalies.
3. **Deterministic World Authority**
   - Preserve a clear separation between authoritative world rules (time of day, schedules, spatial state, puzzle conditions) and optional conversational presentation. The world follows consistent, inspectable logic rather than improvised or contradictory AI output.
4. **Observation-First Adventure**
   - Prioritize environmental storytelling, tactile spatial navigation, clue synthesis, and atmospheric discovery over combat, reflex twitch mechanics, or HUD clutter.
5. **Offline-First Reliability with Optional Live Bridges**
   - Ensure the commercial game plays completely offline as a self-contained PC experience, treating external data feeds and optional AI services as strictly non-blocking enhancements rather than mandatory runtime dependencies.

---

## 5. Geographic Authenticity vs. Fictional Mystery

| Layer | Role in the Experience | Design Balance |
| :--- | :--- | :--- |
| **Authentic Border Reality** | Establishes spatial credibility, cultural specificity, bilingual texture, and a human baseline (Av. Benito JuÃ¡rez, Paso del Norte bridge, RÃ­o Bravo, daily schedules). | Must remain respectful, recognizable in atmosphere, and legally cleared under a deliberate brand/signage strategy (see [IP & Asset Provenance](IP_AND_ASSET_PROVENANCE.md)). |
| **Fictional Mystery & Hidden Worlds** | Provides the adventure engine â€” secret thresholds, subterranean passages, anomalous artifacts, and interconnected realms beyond ordinary urban life. | Must feel organically anchored to the border setting rather than bolted on as unrelated pop-culture references. |

---

## 6. Social Simulation vs. Adventure Gameplay

The repository currently inherits a working civic-oriented social simulation from the hackathon foundation: 7 distinct Bobblehead NPCs with schedule-driven movement, business attendants, a border concierge, synthetic commuters, and a citizen reporting desk (`CONFIRMED`).

In the commercial game (`PLANNED`):
- **Social simulation is a living backdrop and clue ecosystem, not the sole objective.** Daily store hours, NPC routines, and grounded bilingual conversations make the surface world feel alive and can organically surface rumors, schedules, or subtle anomalies without turning the game into a customer-service utility.
- **Adventure gameplay drives player progression.** Exploration, environmental clue gathering, access puzzles, artifact discovery, and threshold transitions provide the primary motivation and structure (see [Core Game Loop](CORE_GAME_LOOP.md) and [Vertical Slice](VERTICAL_SLICE.md)).
- Existing hackathon civic workflows (such as municipal issue reporting with optional SAM 3.1 segmentation) will be evaluated by the creator for retention as an optional side mode, integration into the world's thematic loop, or deferral (`AUTHOR DECISION REQUIRED`).

---

## 7. Open Decisions Requiring Creator Approval (`AUTHOR DECISION REQUIRED`)

To avoid inventing canon or silently resolving creative direction, the following core questions remain open for creator sign-off:

1. **Protagonist Identity & Opening Motivation:** Who is the player character (local resident, commuter, urban explorer, investigator, unnamed visitor), and what specific inciting incident draws them off Avenida JuÃ¡rez into the hidden worlds?
2. **Definitive Narrative Canon & Mystery Explanation:** What is the canonical origin and nature of the underground *Sector C-0*, the liminal Backrooms layer, *El Ojo del Umbral*, and *El Umbral de Lasser*?
3. **Liminal Layer Naming & Identity:** Will the liminal labyrinth retain the name "Backrooms" (subject to legal/brand review) or adopt an original in-universe title tied to the game's mythos?
4. **Commercial Brand Strategy for Real Storefronts:** Will real Avenida JuÃ¡rez businesses be pursued for written trademark permission, or systematically adapted into culturally authentic, legally distinct fictional border establishments?
5. **Role of Civic Reporting & Optional AI:** Should Lic. Patricia Morales's citizen reporting workflow and optional Meta Muse Spark / SAM 3.1 integrations remain as optional commercial features, be re-themed into an in-world investigative mechanic, or be retired from v1.0?
6. **Art Direction & Character Style Target:** Will the stylized procedural Bobblehead aesthetic remain the signature commercial character style for v1.0, or evolve alongside higher-fidelity environment art?
