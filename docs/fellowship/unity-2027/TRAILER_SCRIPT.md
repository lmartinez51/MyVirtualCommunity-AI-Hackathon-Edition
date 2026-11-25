# Proposed Game Trailer Script â€” Unity Fellowship 2027

**Project:** My Virtual Community â€” *A Living World Beyond Reality.*
**Target Runtime:** `75â€“90 seconds` *(proposed structure for Fellowship application video)*
**Core Rule:** **ZERO uncleared third-party logos, recognizable horror franchise props, or unauthorized videos may appear in captured footage.**

> **MANDATORY PRE-CAPTURE CLEARANCE GATE:** Do **not** record final Fellowship trailer footage until all scenes flagged `[REQUIRES IP REPLACEMENT BEFORE CAPTURE]` below have completed their clearance pass (see [IP & Asset Provenance](../../IP_AND_ASSET_PROVENANCE.md)).

Related documents: [Application Brief](APPLICATION_BRIEF.md) Â· [Pitch](PITCH.md) Â· [Vertical Slice](../../VERTICAL_SLICE.md) Â· [World Atlas](../../WORLD_ATLAS.md)

---

## 1. Visual & Editorial Guardrails

- **Feature prominently:** First-person atmosphere, authentic Ciudad JuÃ¡rezâ€“El Paso urban geometry, Paso del Norte bridge viaduct & RÃ­o Bravo, day-to-night lighting contrast, handheld flashlight exploration in the vaulted storm drains (*La Cisterna / El Nudo*, Abasolo siphon, RÃ­o Bravo outflow), the Building A South keypad doorway, additive entry into the liminal labyrinth, and awakening *El Umbral de Lasser*.
- **Exclude strictly until cleared/replaced:**
  - All horror franchise references in `BackroomsLevel1Scene.unity` (`IP-01`: Annabelle, Babadook, Saw/Billy, Chucky, Ring, Talk to Me, Black Phone, Dybbuk, Evil Dead, Lament Configuration, Ouija, 03:07 Conjuring clock).
  - `"SS GEORGIE"` origami boat texture in `UndergroundSewerBuilder.cs` (`IP-02`).
  - Unlicensed real-world storefront brand signs (`IP-04`) unless replaced or formally cleared prior to capture.
  - Trademarked consumer/media props (`IP-05`: Alexa, iPad, El Diario de JuÃ¡rez, YouTube UI).
  - `media/arch-ads/camila_loop.mp4` on the Av. JuÃ¡rez Digital Arch (`IP-06`) and external YouTube live-camera PiP modals (`IP-07`).
  - Raw developer IMGUI debug text or aerial `[V]`/`[G]` debug HUD overlays (capture with clean cinematic / gameplay framing).

---

## 2. Beat-by-Beat Shot Plan (`PLANNED`)

| Timecode | Beat | Visual Shot (In-Engine Unity 6) | Audio / On-Screen Text (`PLANNED`) | Clearance Requirement |
| :--- | :--- | :--- | :--- | :--- |
| **0:00 â€“ 0:14** | **1. Living Border Reality** | Slow first-person walk north along Avenida Benito JuÃ¡rez in golden late-afternoon light toward the monumental Paso del Norte International Bridge canopy. Commuters and attendants move along the sidewalk; storefront shutters respond to dusk; sunlight sweeps across the RÃ­o Bravo viaduct. | Warm street ambience (placeholder/authored); subtle acoustic chord. **Title card / caption:** *"Every border city has a rhythm everyone knows."* | `[REQUIRES IP REPLACEMENT BEFORE CAPTURE]` Replace or frame cleanly away from uncleared storefront logos (`IP-04`) and replace `camila_loop.mp4` (`IP-06`) on the gateway arch. |
| **0:14 â€“ 0:28** | **2. Something Beneath the Asphalt** | Dusk deepens. Player stops by a cast-iron street manhole on Avenida JuÃ¡rez, presses `[E]`, lid slides aside. Cut to black, then the `[F]` flashlight beam snaps on â€” revealing wet aged brick arches, shallow flowing drain water, roots, and flickering maintenance lamps inside *La Cisterna / El Nudo*. | Heavy iron scrape; deep subterranean hum and sparse echoing water drips. **Caption:** *"Beneath ordinary streets..."* | `[REQUIRES IP REPLACEMENT BEFORE CAPTURE]` Exclude `"SS GEORGIE"` boat (`IP-02`); confirm graffiti/crop-circle decals are original. |
| **0:28 â€“ 0:40** | **3. Hidden Infrastructure & Clues** | Tracking shots through the deep `-8.45 m` under-Abasolo siphon gallery, inspecting original environmental clue markings near *Sector C-0*, then swinging open the rusted iron storm gate (`[E]`) to reveal the RÃ­o Bravo south bank beneath the towering international bridge. | Creaking iron gate, distant river air, tense rising harmonic tone. **Caption:** *"...hidden clues lead back to the surface."* | `[CLEAN / VERIFY]` Confirm any new clue props are 100% original art. |
| **0:40 â€“ 0:54** | **4. The Concealed Doorway** | Back on Avenida JuÃ¡rez at night. Player approaches the unassuming Building A South steel double doors beneath the diagonal chamfer. Close-up of the glowing 3D 6-digit keypad turning from red to green; doors swing outward revealing the dark opaque Threshold Veil. Player steps across â€” doors slam shut behind them. | Sharp electronic keypad beeps, heavy steel latch, sudden low-frequency pressure drop. **Caption:** *"Behind a door no one notices..."* | `[REQUIRES IP REPLACEMENT BEFORE CAPTURE]` Ensure adjacent storefront framing avoids uncleared logos (`IP-04`). |
| **0:54 â€“ 1:10** | **5. The Liminal Vault** | Seamless cut into the flickering yellow fluorescent corridors and central pillared hall of the `40 m Ã— 24 m` liminal labyrinth. Quick montage: inspecting the IP-cleared Evidence Board, unlocking the Level-Î© Vault Safe, and lifting the rotating obsidian-and-brass ***El Ojo del Umbral*** astrolabe. | Fluorescent buzz, muted footsteps on damp carpet, rising mystery motif. **Caption:** *"...lies a network of hidden worlds."* | `[REQUIRES IP REPLACEMENT BEFORE CAPTURE]` **CRITICAL:** Show **only** original/cleared artifacts (e.g., *PaquimÃ©* mask, *Anomalous Astral Compass*, new original relics, Level-Î© Safe, *El Ojo del Umbral*). Zero `IP-01` horror franchise props or `IP-05` consumer brands in frame. |
| **1:10 â€“ 1:22** | **6. Awakening the Threshold** | Player approaches the monumental obsidian-and-brass mirror-portal (***El Umbral de Lasser***) in the dim SW sanctuary. Inserting *El Ojo del Umbral* into the octagonal socket retracts the containment chains and ignites the spinning cyan dimensional vortex illuminating the chamber. | Resonant harmonic swell culminating in a crisp silence cut. *(Do **not** fake or imply a playable post-Lasser walk-through unless actually implemented.)* | `[CLEAN]` Original asset (`IP-16`/`IP-17`). |
| **1:22 â€“ 1:30** | **7. Title Card & Positioning** | Cut to dark title lockup over subtle cyan-and-amber ambient light: **MY VIRTUAL COMMUNITY** / *A Living World Beyond Reality.* / *"Built in Unity 6 â€¢ Coming to PC Windows"* *(avoid inventing an unconfirmed exact launch date)*. | Lingering final chord fade-out. | `[CLEAN]` Confirm final title typography & studio logo (`AUTHOR DECISION REQUIRED`). |

---

## 3. Capture & Production Notes (`PLANNED`)
- Capture at native `1920Ã—1080` (60 FPS where hardware permits, or locked smooth 30 FPS with zero stutter) using actual Unity 6 runtime gameplay.
- Avoid showing any unimplemented features, mockups, or misleadingly staged transitions.
- Creator to confirm whether narration/voiceover (bilingual subtitles) or text-only intertitles will be used (`AUTHOR DECISION REQUIRED`).
