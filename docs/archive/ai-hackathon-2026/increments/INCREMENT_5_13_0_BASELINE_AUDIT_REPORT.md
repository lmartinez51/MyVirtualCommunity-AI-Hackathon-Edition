# INCREMENT 5.13.0 — UI/UX CONSOLIDATION BASELINE AUDIT
**Read-Only UX Audit, Architecture Inspection & Prioritized Implementation Plan**

- **Target Repository:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity-AI-Hackathon-Edition`
- **Active Branch:** `feat/meta-model-foundation`
- **Reference Head Commit:** `0fdc875dd4e4eae763a0d7215b986fb3a934e193`
- **Benchmark Repository:** `C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity` (Strictly Read-Only, 0 tracked modifications)
- **Audit Date & Local Time:** 2026-11-03T19:44:30-07:00
- **Mode:** Strictly Read-Only Forensic UX Audit & Roadmap Definition

---

## 1. Executive Summary

Increment 5.13.0 initiates the **UI/UX Consolidation** phase of MyVirtualCommunity AI Hackathon Edition. This increment is strictly an audit and architectural assessment; zero source files, Unity scenes, prefabs, API contracts, or backend implementations have been modified.

### Key Audit Findings:
1. **Pristine Preflight State:** The previous checkpoint (`0fdc875 Consolidate hackathon demo tooling and verification checkpoint`) has been committed by the user and pushed to remote `origin/feat/meta-model-foundation`. The working tree is 100% clean and fully synchronized with upstream. The benchmark repository remains strictly untouched.
2. **Unity UI Architecture Identified:** The entire client interface is built upon **Unity Immediate Mode GUI (IMGUI)** executed via `FirstPersonPlayer.cs:OnGUI()`. There are zero UGUI Canvases, zero UI Toolkit documents, and zero TextMeshPro components in the active scene (`BorderCorridorScene.unity`).
3. **Forensic Distinction Between Runtime and Editor Screenshot Suites:** The Unity Editor screenshot suites (`Increment511ScreenshotSuite.cs`, `Increment5114ScreenshotSuite.cs`, `ScreenshotSuite.cs`) render the 3D camera to a texture and compose procedural GDI+ mockups on top using Windows ClearType. In contrast, live gameplay executes Unity's standard IMGUI pipeline. This fundamental divergence explains discrepancies between documentation captures and actual in-game runtime appearance.
4. **Primary Usability & Visual Defects Cataloged:**
   - **Missing Font Glyphs (`□`):** Standard Unity GUI fonts fail to render Unicode emoji characters (`🏛️`, `💬`, `📝`, `🔬`, `⚠️`, `📷`, `🔍`, `⏸️`, `🚪`, `▶`, `🎮`), displaying missing glyph square boxes across character names, buttons, and status banners.
   - **Text Truncation & Collision in Header Controls:** Language switcher buttons (`[Cambiar a EN]`) collide horizontally with the adjacent Close button (`✕ Close [Esc]`).
   - **Cramped Dialogue Chat Box (115px) for Elena Ramos:** Displaying the CBP wait-time table compresses Elena's chat scrollview to 115px, truncating her initial greeting and forcing immediate vertical scrolling.
   - **Overlapping Object Legend in Enlarged Viewer:** The 3-mode buttons, bounding box checkbox, and detected object badges collide horizontally on 1280x720 resolutions.
   - **Absence of Background Scrim:** Modals open directly over high-contrast 3D scene geometry without dimming or blurring, reducing legibility.
5. **Actionable 3-Increment Consolidation Roadmap:** Formulated an incremental backlog preserving the stylized Bobblehead aesthetic, improving visual polish, repairing layout collisions, and maintaining strict hexagonal decoupling from the backend.

---

## 2. Git Checkpoint and Working-Tree Status

### Repository Preflight Inspection

```powershell
> git branch --show-current
feat/meta-model-foundation

> git status -sb
## feat/meta-model-foundation...origin/feat/meta-model-foundation

> git log -3 --oneline
0fdc875 Consolidate hackathon demo tooling and verification checkpoint
f656974 feat: add multimodal citizen reporting and SAM visual inspection
d22925e feat: populate corridor with Bobblehead NPCs and stabilize interactions

> git rev-parse HEAD
0fdc875dd4e4eae763a0d7215b986fb3a934e193
```

- **Commit Status:** Commit `0fdc875` cleanly encapsulates all Increment 5.12 through 5.12.3.2 release packaging, verification scripts, reports, and evidence integrity fixes.
- **Working Tree:** `nothing to commit, working tree clean`.
- **Remote Synchronization:** Upstream is configured to `origin/feat/meta-model-foundation` and is 100% up-to-date (0 commits ahead, 0 commits behind).
- **Benchmark Repository Integrity:** `git -C "C:\Users\leolm\Desktop\Proyectos_AI\MyVirtualCommunity" status` confirms 0 tracked changes (`nothing added to commit but untracked files present`).

---

## 3. Actual Unity UI Architecture

### 3.1 Engine & Render Pipeline
- **Unity Engine Version:** Unity 6 (`6000.3.25f1`).
- **Render Pipeline:** Universal Render Pipeline (URP 17.0.3), configured via `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` and URP assets.
- **Lighting & Post-Processing:** Real-time directional sun lighting with soft shadow maps and custom skybox gradient.

### 3.2 UI Technology Stack
- **Framework:** **Unity Immediate Mode GUI (IMGUI)**.
  - Inspection of `Assets/Scenes/BorderCorridorScene.unity` confirms:
    - **Canvas count:** 0.
    - **EventSystem count:** 0.
    - **UI Toolkit (`UIDocument`) count:** 0.
    - **TextMeshPro (`TextMeshProUGUI`) count:** 0.
- **Implementation Core:** `Assets/Scripts/FirstPersonPlayer.cs`.
  - The script contains **3,215 total lines of code**, of which **~2,100 lines** (lines 1131–3200) are dedicated purely to immediate-mode rendering routines inside `OnGUI()`.
- **Text Rendering & Typography:**
  - Standard Unity GUI font (`GUI.skin.font`), rasterized without Signed Distance Field (SDF) antialiasing.
  - No fallback font chain is configured for multi-byte Unicode or emoji glyphs.

### 3.3 Visual Asset & Style Management
- **Procedural Texture Generation:**
  - Background panels, card plates, and backdrop overlays are generated dynamically in memory as 1x1 `Texture2D` instances (`new Texture2D(1, 1)` with `SetPixel` and `Apply`) and cached across frames (`GetOpaqueCardStyle`, `GetModalBackdropStyle`, `GetDarkPlateStyle`).
- **Dynamic `GUIStyle` Allocations:**
  - Styles are constructed inline or via factory helpers (`GetLocationHeaderStyle`, `GetModalTitleStyle`, `GetMessageBodyStyle`). Several methods instantiate `new GUIStyle(GUI.skin.label)` every frame inside `OnGUI()`, which introduces unnecessary managed GC allocations.

### 3.4 Input Handling & Cursor Management
- **Input System:** Hybrid compatibility with `UnityEngine.InputSystem` and legacy `KeyCode` fallbacks.
- **Cursor Locking:** Seamlessly manages `Cursor.lockState` (`Locked` vs `None`) and `Cursor.visible` based on `PrimaryUIState`.
- **Hotkey Routing:**
  - `[E]`: Raycast proximity interaction with NPCs and desks.
  - `[Esc]`: Toggles Pause Menu, closes active dialogue modals, or dismisses enlarged image viewer.
  - `[F1]`: Toggles Controls & Exploration Guide overlay from HUD or Pause Menu.
  - `[Enter]`: Submits active text input in dialogue chat.

### 3.5 Screen Resolution & Aspect Ratio Scaling
- **Resolution Strategy:** Hardcoded pixel math relative to `Screen.width` and `Screen.height`.
- **Clamping Rules:**
  - Citizen Service modal: `Mathf.Min(Screen.width - 24f, 880f)` width × `Mathf.Min(Screen.height - 24f, 760f)` height.
  - Border Concierge modal: `Mathf.Min(Screen.width - 24f, 740f)` width × `Mathf.Min(Screen.height - 24f, 710f)` height.
  - General Dialogue modal: 740f width × `Mathf.Min(Screen.height - 24f, 600f)` height.
  - Enlarged Viewer modal: `Mathf.Min(Screen.width - 40f, 960f)` width × `Mathf.Min(Screen.height - 40f, 680f)` height.
- **Deficiency:** Lacks dynamic DPI scaling. On 4K monitors (3840×2160), modals appear as small postage stamps in the center of the screen. On sub-720p screens, controls overflow and clip.

### 3.6 Presentation Layer Decoupling
- **Backend API Boundary:** Interacts with the Node.js server strictly through `ApiClient.cs` and `ApiContracts.cs`.
- **Non-Authoritative UI:** The Unity client is strictly a presentation terminal. It does not calculate business hours, does not validate border wait times, does not invent spatial coordinates, and does not generate report folios.

---

## 4. Complete Player-Facing Interface Inventory

```
┌────────────────────────────────────────────────────────────────────────┐
│                        PRIMARY UI STATE MACHINE                        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │
         ┌──────────────────────────┼──────────────────────────┐
         ▼                          ▼                          ▼
  [ Exploration ]            [ ControlsGuide ]           [ PauseMenu ]
  • Reticle                  • Movement Keys             • Resume
  • Top Context HUD          • Inhabitant Guide          • Controls Guide
  • NPC Nameplates           • Back / Resume             • Language Switch
  • Interaction Prompt [E]                               • Quit to Desktop
         │
         ▼
    [ Dialogue ]
         │
         ├─► Elena Ramos (Border Concierge + Live CBP + Intent)
         ├─► Carlos Méndez (Pharmacy Storefront Attendant)
         ├─► Valeria Domínguez (Optical Storefront Attendant)
         ├─► Mateo Navarro (UTEP Student Witness)
         ├─► Sofía Alarcón (Retail Queue Witness)
         ├─► Don Héctor Delgado (Artisan Queue Witness)
         └─► Lic. Patricia Morales (Citizen Reporting Workflow)
               ├─ Consultation Tab (Q&A Dialogue)
               └─ Issue Reporting Tab
                     ├─ 1. Form Input (9 Categories, Description, Location, Photo)
                     ├─ 2. Analyzing State (Muse Spark + SAM 3.1)
                     ├─ 3. Draft Review (Summary, Thumbnail, Destination, Submit)
                     │     └─► Enlarged Photo Viewer Modal (Original / Mask / Combined)
                     └─ 4. Receipt Success (Folio, Category, Routing, Timestamp)
```

### A. Main World and Gameplay HUD
1. **Top Context Bar (`DrawHUD`):**
   - Left: Location & Directional Context (`Avenida Benito Juárez • Paso del Norte (⬆ Norte: Puente | ⬇ Sur: Centro)`).
   - Right: Help toggle button (`[F1]`), Language switcher (`EN` / `ES`), and Server Diagnostics indicator (`Online | Meta muse-spark-1.3`).
2. **World-Space Floating Nameplates (`DrawWorldSpaceNPCNameplates`):**
   - Projects 3D world coordinates of all 7 NPCs to screen space within 25 meters.
   - Highlights yellow with `[E]` prefix when the player is within 3.5m interaction proximity.
3. **Center Reticle (`DrawReticle`):**
   - 6×6 pixel white crosshair box centered on screen in Exploration mode when the mouse is locked.
4. **Interaction Prompt Pill (`DrawInteractionPrompt`):**
   - Bottom floating card (`560×54 px` at `y = Screen.height - 105`).
   - Title: `[E] Talk to <Avatar> <Name>`.
   - Subtitle: Character role and prompt cue.
5. **Bottom-Left Navigation Hints:**
   - Text label (`WASD: Move | Shift: Sprint | E: Interact | Esc: Pause | F1: Controls Guide`).
6. **Controls & Exploration Guide (`DrawControlsOverlay`):**
   - `520×390 px` centered card.
   - Three sections: Movement & Camera, NPC & Landmark Interaction, Inhabitants along Avenida Juárez.
7. **Pause Menu (`DrawPauseMenu`):**
   - `450×370 px` centered card.
   - Resume Exploration, Controls Guide, Language Toggle, and Quit to Desktop buttons.

---

## 5. NPC Dialogue UX Assessment

### Evaluated Characters & Roles

| Character | Identifier | Architectural Category | Specific Interface Components |
| :--- | :--- | :--- | :--- |
| **Elena Ramos** | `elena-border-concierge` | Border Concierge | CBP live wait-time comparison table, freshness badge, crossing intent selection chips, dialogue scrollview, prompt chips, input textfield. |
| **Carlos Méndez** | `carlos-farmacia-attendant` | Storefront Attendant | Grounded hours badge, pharmacy role subtitle, medical refusal guardrails, dialogue scrollview, prompt chips, input textfield. |
| **Valeria Domínguez**| `valeria-optica-attendant` | Storefront Attendant | Grounded optical boutique badge, eye exam / catalog refusal notice, dialogue scrollview, prompt chips, input textfield. |
| **Mateo Navarro** | `mateo-student` | Synthetic Witness | Synthetic witness disclaimer header, student commuter context, prompt chips, dialogue scrollview. |
| **Sofía Alarcón** | `sofia-cross-border-retail` | Synthetic Witness | Synthetic witness disclaimer header, cross-border retail worker context, prompt chips, dialogue scrollview. |
| **Don Héctor Delgado**| `hector-artisan` | Synthetic Witness | Synthetic witness disclaimer header, downtown artisan heritage context, prompt chips, dialogue scrollview. |
| **Lic. Patricia Morales**| `patricia-morales` | Citizen Service | Dual-tab header (`Inquiries & Guidance` vs `Register Citizen Report`), municipal intake badge, full reporting workflow. |

### Usability Evaluation of Dialogue System:
- **Conversation Hierarchy:** Clean message bubble structure (`DrawMessageBubble`) distinguishing player messages from NPC responses with sender avatars and timestamps.
- **Loading & Error Feedback:** Clear inline status box (`⏳ Processing inquiry with Meta Muse Spark / World Core...`) and error banner with a one-click `Retry` button.
- **Visual Continuity:** Excellent structural consistency across all 6 dialogue characters; header, subtitle, provenance banner, and input bar follow identical structural rules.
- **Identified Deficiency:** Prompt chips are arranged horizontally and overflow or wrap awkwardly when questions are lengthy in Spanish.

---

## 6. Citizen Reporting UX Assessment

### The 10-Step Lifecycle Workflow

```
[ Form Input ] ──► [ AI Analysis ] ──► [ Draft Review ] ──► [ World Core Submit ] ──► [ Receipt Success ]
(9 Categories)     (Muse Spark 1.3)    (Structured Info)     (Player Confirms)         (Folio Issued)
(Description)      (SAM 3.1 Vision)    (SAM Visual Box)                                (Local Storage)
(Location)                             (Inspect Modal)
(Photo + Consent)
```

### Detailed Evaluation of Workflow Steps:
1. **Category Selection:** 3×3 grid of buttons representing all 9 municipal categories. Selected category highlights in teal (`#40f2d9`).
2. **Problem Description:** Multiline textarea with 5 quick-fill example chips (`Pothole`, `Waste`, `Water/Drain`, `Broken Light`, `Broken Sidewalk`).
3. **Location Selection:** Textfield with 3 suggestion chips (`Av. Juárez`, `Colonia Melchor Ocampo`, `Blvd. Gómez Morín`) and explanatory note distinguishing corridor coordinates from descriptive citywide entries.
4. **Photographic Evidence:** Native Windows file dialog integration (`WindowsFileDialog.cs`) supporting PNG/JPEG up to 5 MB. Displays thumbnail preview, file size, and remove button.
5. **External AI Consent:** Explicit toggle (`Authorize external AI visual analysis (Meta SAM 3.1)`) with dynamic privacy notice when declined.
6. **Analyzing Transition:** Dedicated interstitial card explaining Muse Spark 1.3 structuring, SAM 3.1 segmentation, and World Core authority.
7. **Draft Review:** Two-column split layout. Left column shows evidence thumbnail with click-to-enlarge prompt and SAM status badge. Right column displays Muse Spark structured summary, confirmed category, simulated department routing, and geographic coverage status.
8. **Enlarged Visual Evidence Inspector:** Dedicated modal with 3 display modes (`1. Original Photo`, `2. SAM 3.1 Mask`, `3. Combined`), bounding box toggle, status banner, and detected objects legend with color badges and confidence scores.
9. **Submission Confirmation:** Prominent green action button (`Confirm and Register in World Core`).
10. **Receipt Confirmation:** Displays report folio ID (`CJ-XXXXXX`), category, location, channel, simulated department, evidence status, SAM status, World Core state (`RECEIVED`), and local persistence notice (`Persistencia local disponible: .runtime/citizen-reports.json`).

---

## 7. Border Concierge UX Assessment

### CBP Pedestrian Wait Times Presentation:
- **Comparison Table:** Displays Paso del Norte (Santa Fe) vs Bridge of the Americas (BOTA) standard pedestrian lanes side-by-side.
- **Attributes Presented:** Operational delay in minutes, lane status (`Open`/`Closed`), active lane count vs maximum lanes (`Lanes: 8/14`), source observation time (`At 4:00 pm MST`), and data freshness indicator (`Vigente (<60m)` vs `Stale`).
- **Mathematical Comparison:** Computes absolute difference and identifies lower wait crossing (`El tiempo reportado en Puente de las Américas es 13 minutos menor que en Paso del Norte`).
- **Session-Local Crossing Intent:** Toggle buttons allow players to record their personal crossing decision (`Optar por Paso del Norte` vs `Optar por Puente de las Américas`) for the active play session.
- **Fictional Notice Guardrail:** Explicit disclaimer separates verified CBP measurements from synthetic witness perspectives.
- **Identified Defect:** The table occupies ~95px vertical height, which forces the dialogue chat box below it down to 115px, truncating Elena's opening explanation.

---

## 8. Global UI States and Interaction Assessment

1. **Loading Indicators:**
   - Dialogue input: Disables textfield and send button (`GUI.enabled = !isRequestPending`), displays yellow italic status box.
   - Border wait times: `isBorderWaitTimesLoading` displays refreshing text.
   - Citizen reporting: Interstitial full-modal `Analyzing` state.
2. **Error States:**
   - Inline red alert box (`⚠️ <ErrorMessage>`) with functional `Retry` button in dialogue and `✕` dismiss button in citizen reporting.
3. **Empty States:**
   - Empty chat history renders default character greeting.
   - Report form with no photo renders clean dashed box (`Report without attached photo — Valid for simulated registration & routing`).
   - Inspector with no mask renders informative notice (`Pixel segmentation mask not available for this state`).
4. **Offline / Mock Mode Disclosures:**
   - Top-right HUD status pill displays green for live server or yellow/red for mock/offline.
   - Synthetic witnesses display persistent disclaimer headers.
   - Citizen report receipts explicitly display local simulation notices.
5. **Button Interaction Affordances:**
   - Active/selected buttons tint bright cyan/teal (`#40f2d9`).
   - Destructive buttons tint soft red (`#ff8c8c`).
   - Standard buttons use default Unity GUI button styling with slight hover brightening.

---

## 9. Visual and Usability Defects (Ranked by Severity)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    DEFECT SEVERITY DISTRIBUTION                         │
│                                                                         │
│   [ CRITICAL ]  ████████████████████  (2)  Missing Glyphs & Overlaps   │
│   [ HIGH     ]  ████████████████████  (2)  Chat Squeeze & Truncation   │
│   [ MEDIUM   ]  ████████████████████  (2)  Inspector & Chip Collisions │
│   [ LOW      ]  ████████████████████  (2)  Missing Scrim & Monolith    │
└─────────────────────────────────────────────────────────────────────────┘
```

| ID | Severity | Screen / Component | Exact Code Location | Description & Root Cause | Recommended Remediation |
| :--- | :---: | :--- | :--- | :--- | :--- |
| **UI-CRIT-01** | **CRITICAL** | All Modals, HUD & Nameplates | `FirstPersonPlayer.cs` (lines 1184-1700, 2109-3200) | **Missing Glyph Square Boxes (`□`):** Standard Unity IMGUI font skin lacks Unicode emoji coverage, causing emojis (`🏛️`, `💬`, `📝`, `🔬`, `⚠️`, `📷`, `🔍`, `⏸️`, `🚪`, `▶`, `🎮`) to render as broken square boxes `□` across all titles, buttons, and badges. | Replace emoji strings with clean, universally supported ASCII/Latin typographic symbols (`[E]`, `> `, `* `, `(i)`, `[!]`, `X`, `+`). |
| **UI-CRIT-02** | **CRITICAL** | Dialogue & Form Headers | `FirstPersonPlayer.cs:1519-1532` | **Header Button Collision & Text Overlap:** Language button `GUILayout.Width(220)` is too narrow for Spanish `"Idioma: Español [Cambiar a EN]"`, clipping to `"[Camb..."` and colliding visually with adjacent `✕ Close [Esc]`. | Refactor language toggle to a concise pill (e.g. `ES | EN`, 60px) and enforce 12px margin before Close button. |
| **UI-HIGH-01** | **HIGH** | Elena Ramos Dialogue Modal | `FirstPersonPlayer.cs:1596-1597` | **Cramped Dialogue Scrollview (115px):** Border Concierge chat box is hardcoded to 115px height to accommodate CBP wait-time table, clipping Elena's initial greeting and forcing immediate awkward scrolling. | Dynamically allocate chat box height based on window bounds, or place CBP table in a collapsible section. |
| **UI-HIGH-02** | **HIGH** | Citizen Reporting Form | `FirstPersonPlayer.cs:2182-2185` | **Emergency Notice Text Truncation:** Red emergency notice is drawn in a single-line box without dynamic height, clipping off at `... non-urgent municipal s` on 720p screens. | Enable `wordWrap = true` and calculate height based on content line count. |
| **UI-MED-01** | **MEDIUM** | Enlarged Visual Inspector | `FirstPersonPlayer.cs:2950-2977, 3145-3180` | **Controls Row & Legend Collision:** The 3 display mode buttons and Bounding Box checkbox share one horizontal row, truncating mode labels and colliding with the checkbox. Bottom legend title collides with object pill `#1`. | Use a clean segmented control bar for display modes, move bounding box toggle to secondary row, and add padding to legend. |
| **UI-MED-02** | **MEDIUM** | Citizen Report Form | `FirstPersonPlayer.cs:2219-2246` | **Example Chips Horizontal Overflow:** 5 quick-fill example buttons are packed into a single horizontal row, clipping the 5th button (`Broken sidewalk`) on narrow screens. | Arrange quick-fill chips in a 2-row wrapping flex layout. |
| **UI-LOW-01** | **LOW** | Dialogue & Citizen Service Modals | `FirstPersonPlayer.cs:1135-1140` | **Missing Modal Dimming Scrim:** Dialogue modals render directly over bright 3D street geometry without a background darkening scrim, reducing contrast. (Enlarged viewer correctly uses scrim on line 2921). | Add `DrawModalScrim()` behind all primary modal states. |
| **UI-LOW-02** | **LOW** | Code Architecture | `FirstPersonPlayer.cs` (entire file) | **Monolithic Controller (3,215 lines):** Combining player movement, camera physics, HTTP requests, and ~2,100 lines of UI rendering in one script violates single responsibility. | Factor UI rendering methods into modular view components (`CorridorHUDView`, `NpcDialogueModalView`, `CitizenReportModalView`). |

---

## 10. Initial Design System Proposal (Recommendations Only)

### 10.1 Color Palette
- **Surfaces & Layers:**
  - Base Scrim (Backdrop): `rgba(10, 14, 22, 0.72)`
  - Modal Plate Surface: `#131b26` (Solid Deep Slate, 96% opacity)
  - Card / Panel Surface: `#1c2838` (Secondary Slate)
  - Modal Border / Dividers: `#2c3e55` (1px clean steel outline)
- **Primary & Accent Colors:**
  - Regional River Teal (Primary Accent): `#22d3ee` (Cyan / River Border identity)
  - Desert Gold (Secondary Accent): `#facc15` (Amber warm highlight)
  - Focus / Interaction Highlight: `#38bdf8`
- **Semantic Colors:**
  - Success / Open / Received: `#34d399` (Mint Green)
  - Warning / Stale / Caution: `#fbbf24` (Amber Gold)
  - Error / Emergency / Closed: `#f87171` (Coral Red)
  - Muted / Secondary Text: `#94a3b8` (Slate Muted)
  - Primary Text: `#f1f5f9` (Crisp Off-White)

### 10.2 Typography Scale (Standard Font Compatible)
| Role | Size | Weight | Color | Usage |
| :--- | :---: | :---: | :--- | :--- |
| **Title** | 14pt | Bold | `#f1f5f9` | Modal titles, character display names |
| **Section Header**| 12pt | Bold | `#38bdf8` | Major section titles, receipt headers |
| **Subheading** | 11pt | Bold | `#cbd5e1` | Category headers, form steps |
| **Body** | 11pt | Regular | `#e2e8f0` | Dialogue messages, descriptions, notices |
| **Caption / Meta**| 10pt | Regular | `#94a3b8` | Addresses, timestamps, file sizes |
| **Micro** | 9pt | Italic | `#64748b` | Disclaimers, technical simulation notes |

### 10.3 Iconography & Symbol Convention
- **Rule:** Never use multi-byte OS emojis in Unity IMGUI strings.
- **Approved Typographic Replacements:**
  - Character / Identity: `[NPC]` or character initials (e.g. `[C]`, `[E]`, `[P]`)
  - Information / Notices: `[i]`, `[*]`, `[!]`
  - Navigation / Action: `->`, `<-`, `[Enter]`, `[Esc]`, `[F1]`, `[X]`
  - Checkmarks: `[v]` or `OK`
  - Audio / Dialogue: `"` (quotes) or `>`

### 10.4 Button & Control Standards
- **Button Heights:** Primary (34px), Secondary (28px), Micro/Chip (22px).
- **Corner Radius:** Modals (8px), Buttons (4px), Chips (11px rounded pill).
- **Interactive Affordance:** Explicit 1px border highlight on hover and focus.

---

## 11. Prioritized UI/UX Implementation Roadmap

```
┌────────────────────────────────────────────────────────────────────────┐
│                        3-INCREMENT ROADMAP                             │
├───────────────────────┬───────────────────────┬────────────────────────┤
│    INCREMENT 5.13.1   │    INCREMENT 5.13.2   │    INCREMENT 5.13.3    │
│  Typography, Glyphs   │  Dialogue & Border    │  Citizen Reporting &   │
│   & Layout Repair     │     Concierge UX      │   SAM Inspector Polish │
├───────────────────────┼───────────────────────┼────────────────────────┤
│ • Replace emojis      │ • Expand Elena chat   │ • Segmented mode bar   │
│ • Fix header overlap  │ • Responsive chips    │ • Non-colliding legend │
│ • Add modal scrim     │ • Card polish         │ • Receipt formatting   │
│ • Emergency wrap      │ • Tabbed CBP drawer   │ • Two-column balance   │
└───────────────────────┴───────────────────────┴────────────────────────┘
```

### Increment 5.13.1 — Typography, Glyph Repairs & Layout Stabilization
- **Objective:** Resolve all critical visual defects (broken glyph squares `□`, header button collisions, emergency text clipping, and missing modal scrim).
- **Player-Facing Improvements:**
  - Crisp, professional text without broken boxes across all 7 NPCs and menus.
  - Non-overlapping header controls (`ES | EN` pill and `✕ Close [Esc]`).
  - Darkened background scrim focusing attention on the active modal.
  - Fully readable emergency notice and wrapped quick-fill chips.
- **Affected Screens:** HUD, Controls Guide, Pause Menu, Dialogue Modals, Citizen Report Form.
- **Candidate Files:** `unity-client/Assets/Scripts/FirstPersonPlayer.cs`.
- **Implementation Risk:** Low. Pure IMGUI string and layout adjustments; zero contract changes.
- **Acceptance Criteria:**
  - Zero `□` characters visible anywhere in the UI.
  - Header buttons maintain >= 16px clearance across all resolutions.
  - Emergency banner wraps cleanly without horizontal clipping.
- **Regression Tests:** `npm test`, `npm run build`, `npm run test:launcher`.

### Increment 5.13.2 — Dialogue & Border Concierge UX Consolidation
- **Objective:** Eliminate dialogue scrollview cramping, polish suggested prompt chips, and structure CBP border wait times into a clean information card.
- **Player-Facing Improvements:**
  - Elena Ramos dialogue window displays her full greeting without immediate scrolling.
  - Suggested question chips adapt gracefully without overflowing window margins.
  - Clear visual separation between official U.S. government CBP metrics and simulated dialogue.
- **Affected Screens:** Elena Ramos Kiosk, Carlos, Valeria, Mateo, Sofia, Hector dialogue screens.
- **Candidate Files:** `unity-client/Assets/Scripts/FirstPersonPlayer.cs`.
- **Implementation Risk:** Low to Medium. Preserves all NPC chat endpoints and response structures.
- **Acceptance Criteria:**
  - Dialogue scroll area height >= 180px across all character modals.
  - Prompt chips wrap into clean rows without horizontal overflow.
- **Regression Tests:** `npm run verify:npcs`, automated dialogue test suite.

### Increment 5.13.3 — Citizen Reporting & Visual Evidence Inspector Polish
- **Objective:** Consolidate the Citizen Reporting workflow, draft review, and enlarged 3-mode visual inspector into a cohesive, non-overlapping presentation.
- **Player-Facing Improvements:**
  - Enlarged viewer features a clean segmented control bar without button/checkbox collisions.
  - Object legend chips flow cleanly below the image without overlapping title text.
  - Confirmed receipt card features balanced spacing, prominent folio ID, and clear persistence indicator.
- **Affected Screens:** Patricia Morales Citizen Service modal, Enlarged Photo Viewer modal.
- **Candidate Files:** `unity-client/Assets/Scripts/FirstPersonPlayer.cs`, `unity-client/Assets/Editor/Increment511ScreenshotSuite.cs`.
- **Implementation Risk:** Low to Medium.
- **Acceptance Criteria:**
  - Enlarged viewer controls maintain >= 12px margin between all interactive elements.
  - Detected objects legend displays all labels and confidence scores without text collision.
- **Regression Tests:** `npm run verify:recovery`, automated domain mask tests (`npm test`).

---

## 12. Exact Candidate Files and Components

| Component / Subsystem | Path | Action in Roadmap |
| :--- | :--- | :--- |
| **Player Controller & IMGUI View** | `unity-client/Assets/Scripts/FirstPersonPlayer.cs` | Refactor glyph strings, header layout, scrim rendering, scrollview heights, and chip wrapping. |
| **Interaction Point Metadata** | `unity-client/Assets/Scripts/InteractionPoint.cs` | Sanitize avatar and prompt strings to eliminate emoji glyphs. |
| **API Client & Contracts** | `unity-client/Assets/Scripts/ApiClient.cs`, `ApiContracts.cs` | **Preserve untouched** (architectural boundary). |
| **Bobblehead Characters** | `unity-client/Assets/Scripts/Characters/Bobblehead/*` | **Preserve untouched** (visual identity preserved). |
| **Screenshot Suites** | `unity-client/Assets/Editor/Increment511ScreenshotSuite.cs` | Update GDI+ mockups in 5.13.3 if screenshot parity is required. |

---

## 13. Runtime Inspection and Screenshot Evidence

### Audit Evidence Matrix

| Evidence Artifact | Nature of Capture | Key Observations |
| :--- | :--- | :--- |
| `media_1793686251439_6e511f14.jpg` | **Live In-Engine Runtime** | Confirms missing glyph boxes (`□`), header collision (`[Camb...`), emergency notice clipping, and horizontal overflow. |
| `511_01_patricia_desk_approach.png` | **Live In-Engine Runtime** | Confirms missing glyph in top bar (`□ Avenida Benito Juárez`) and bottom interaction prompt pill. |
| `511_02_citizen_reporting_modal_form.png` | **Live In-Engine Runtime** | Confirms header language collision (`[Camb` vs `Close`) and category button tinting. |
| `510_01_elena_official_comparison_and_intent.png` | **GDI+ Procedural Mockup** | Demonstrates the 115px dialogue chat squeeze and CBP table formatting. |
| `5114_01_scrollable_form_fixed_footer.png` | **GDI+ Procedural Mockup** | Illustrates the vertical scrollbar repair implemented in 5.11.4. |
| `5114_02_enlarged_viewer_combined_mode.png` | **GDI+ Procedural Mockup** | Demonstrates the controls row text truncation and bottom legend collision. |

---

## 14. Technical Risks and Regression Considerations

1. **IMGUI Event Layout Consistency:**
   - Unity IMGUI runs two passes per frame: `EventType.Layout` and `EventType.Repaint`. Adding dynamic height calculations must occur consistently across both passes to avoid `ArgumentException: Getting control ... in a group with only ... controls` errors.
2. **Preserving Hexagonal Separation:**
   - UI polish must strictly avoid introducing domain logic, state caching, or data transformations into `FirstPersonPlayer.cs`. All facts must continue flowing from the Node.js backend.
3. **Screen Resolution Variations:**
   - Any layout changes must be tested at both standard 1280×720 (minimum supported target) and 1920×1080 to ensure text wrapping and margins behave consistently.
4. **Bobblehead Aesthetic Preservation:**
   - All character models, materials, animations, and lighting must remain untouched. The UI consolidation is purely focused on 2D interface polish.

---

## 15. Final Recommendation for the First Implementation Increment

### **Recommended Action: Proceed to Increment 5.13.1**

The recommended next step is to execute **Increment 5.13.1 (Typography, Glyph Repairs & Layout Stabilization)**:
1. Replace all raw Unicode emojis with crisp, universally supported typographic symbols across `FirstPersonPlayer.cs` and `InteractionPoint.cs`.
2. Redesign the header language toggle into a clean `ES | EN` pill with guaranteed clearance before the Close button.
3. Add a modal dimming scrim behind all primary modals (`Exploration -> Dialogue / PauseMenu`).
4. Enable `wordWrap = true` on the emergency notice banner to eliminate text cutoff.
5. Wrap quick-fill suggestion chips into clean two-row layouts.

This provides maximum immediate visual quality improvement with minimum architectural risk.

