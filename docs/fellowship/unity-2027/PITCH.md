# Commercial Pitch — My Virtual Community

**Title:** My Virtual Community — *A Living World Beyond Reality.*
**Logline:** *"An interconnected first-person exploration adventure where authentic border life conceals an extraordinary network of hidden worlds."*
**Genre:** First-Person Exploration & Environmental Mystery Adventure
**Platform:** PC Windows (Unity 6)
**Internal Target Commercial Release:** February 2028, providing schedule buffer ahead of [Unity Fellowship Terms and Conditions](https://unity.com/legal/unity-fellowship-terms-and-conditions) Section 2.1(c), which requires commercial release before March 1, 2028.

Related documents: [Application Brief](APPLICATION_BRIEF.md) · [Game Vision](../../GAME_VISION.md) · [Vertical Slice](../../VERTICAL_SLICE.md) · [Roadmap](ROADMAP.md)

---

## 1. The Hook

Most mystery games ask you to believe in a secret world inside an invented fantasy town. **My Virtual Community** starts somewhere unmistakably real: **Avenida Benito Juárez** in historic downtown Ciudad Juárez, stretching north onto the **Puente Internacional Paso del Norte** crossing into El Paso, Texas.

At street level, the city breathes with everyday border rhythm — local storefronts opening and closing on real schedules, commuters crossing between two nations, sunlight arcing over the viaduct and the Río Bravo. Yet beneath the asphalt lies an aged storm-drain network leading to a flooded octagonal cistern (*La Cisterna / El Nudo*), and tucked behind an ordinary building chamfer sits a steel keypad door opening onto **Remanencia — Nivel 0**, an impossible `960 m²` liminal space guarding a dormant **Dimensional Passage (*Pasaje dimensional*)**.

The game's magic comes from **contrast**: ordinary cultural realism giving way, step by step, to hidden architecture beyond reality.

---

## 2. Why This Game Is Original

1. **An Underrepresented Real-World Setting Treated With Craft:** The Ciudad Juárez–El Paso binational corridor is one of the most iconic, culturally rich urban crossings in North America — yet virtually absent from narrative PC exploration games. Rather than reducing the border to clichés, the game celebrates its everyday streets, bilingual texture, architecture, and human cadence.
2. **Seamless Multi-Layer Spatial Discovery:** Players move seamlessly from sunlit international bridge infrastructure into claustrophobic, flashlight-lit underground storm drains (`Y = -3.20 m` down to an `-8.45 m` inverted siphon) and across a non-Euclidean threshold into an additively loaded liminal realm, supporting a long-term vision of an interconnected network of hidden worlds.
3. **Deterministic World Simulation Serving Adventure:** Underneath the Unity 6 client sits a tested hexagonal World Core architecture (`src/domain/`) originally built for strict schedule and behavioral determinism. As the game transitions to commercial production, this deterministic foundation powers believable daily routines, storefront shutters, time-of-day shifts, and diegetic world state — keeping the world coherent and offline-capable without relying on unpredictable cloud AI.

---

## 3. The Player Experience (`PLANNED` Commercial Loop Built on `CONFIRMED` Worlds)

- **Observe the Surface:** Walk Avenida Juárez and the Paso del Norte bridge, converse with distinct residents and commuters, notice schedules and architectural anomalies.
- **Descend Into the Unknown:** Pry open municipal manholes, switch on your tactical flashlight (`[F]`), navigate vaulted storm drains, uncover environmental clues inside *La Cisterna* and *Sector C-0*, and emerge along the Río Bravo levee beneath the bridge.
- **Unlock Hidden Thresholds:** Piece together clues across layers to breach concealed doors, navigate the flickering yellow corridors of **Remanencia — Nivel 0**, unlock the Level-Ω containment safe, recover ***El Ojo del Umbral***, and awaken the **Dimensional Passage (*Pasaje dimensional*)**.
- **Journey Beyond Reality (`OPTIONAL FUTURE SCOPE — SUBJECT TO CREATOR APPROVAL AND VERTICAL-SLICE VALIDATION`):** The creative vision encompasses a larger network of hidden worlds beyond the Dimensional Passage; however, any additional playable post-passage world is **not** guaranteed for v1.0 and will be evaluated only after *Slice 01* playtesting and creator sign-off.

---

## 4. Technical Differentiators (`CONFIRMED` Foundation + `PLANNED` Commercial Hardening)

- **Proven Working Prototype in Unity 6 (`6000.3.25f1`, URP Forward+):** Not a slide deck — four interconnected spatial layers, additive scene loading/unloading, atmospheric lighting/fog state transitions, procedural character baking, and automated SHA-256 build-freshness tooling already run on Windows PC today.
- **Accessible Hardware Support & Performance Targets:** The prototype currently implements a thermal-safe 30 FPS VSync fallback configuration (`vSyncCount = 2`) for GPUs with `<= 2560 MB` VRAM (`CONFIRMED`). A 60 FPS desktop target is a proposed commercial performance goal (`PLANNED`); representative performance profiling and hardware validation across desktop and laptop GPUs remain required (`REQUIRES VALIDATION`).
- **Offline-First Commercial Architecture (`PLANNED`):** Commercial v1.0 will package core deterministic simulation cleanly for 100% offline single-player play, keeping any live/AI bridges strictly optional.

---

## 5. Commercial Potential & Audience (`INFERRED` / `TBD`)

- **Target Audience:** Fans of atmospheric first-person exploration, architectural mysteries, and uncanny liminal adventures (e.g., players drawn to *Firewatch*, *Control*'s uncanny architecture, *Outer Wilds*-style clue discovery, *Superliminal*, and liminal-space exploration games — *cited as genre reference points only, not claimed equivalencies*).
- **Cultural & Regional Appeal:** Natural bilingual (English / Spanish) identity appealing simultaneously to global indie PC exploration audiences and the underserved Latin American / U.S. borderlands gaming market.
- **Commercial Release Strategy (`AUTHOR DECISION REQUIRED`):**
  - **Primary Storefront:** Windows PC (e.g., Steam — `TBD`).
  - **Pricing Tier & Positioning:** Mid-sized indie premium single-player adventure (`TBD`).
  - **Post-Launch Expansion Path:** Modular additive-scene architecture naturally supports optional future neighborhood districts and candidate hidden worlds beyond the Dimensional Passage if approved after v1.0 or post-slice validation.
