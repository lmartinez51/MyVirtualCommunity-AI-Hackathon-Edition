# Third-Party Notices & Dependency Licensing

**Project:** My Virtual Community â€” *A Living World Beyond Reality.*
**Purpose:** Inventory of third-party software, engine packages, vendored libraries, map data, and external APIs present in or referenced by this repository, distinguishing **verified license metadata** from **unverified commercial distribution terms**.

> **Important:** The presence of an API adapter or external URL in this repository does **not** establish commercial distribution rights, SLA guarantees, or permission to redistribute third-party data or broadcasts. Verify all `UNVERIFIED` commercial terms before shipping a commercial PC release.

Related active documents: [LICENSE](../LICENSE) Â· [IP & Asset Provenance](IP_AND_ASSET_PROVENANCE.md)

---

## 1. Game Engine & Unity Packages (`CONFIRMED` in `unity-client/Packages/manifest.json`)

| Component | Version | Provider | Verified License / Status | Commercial Notes |
| :--- | :--- | :--- | :--- | :--- |
| **Unity Editor & Runtime** | `6000.3.25f1` (`e1dba0a9aba4`) | Unity Technologies | Unity Software Terms of Service (archived README cites *Personal/Educational License*) | **UNVERIFIED FOR COMMERCIAL TIER:** Confirm Unity Personal/Pro/Fellowship license eligibility and splash-screen/revenue thresholds before commercial release. |
| `com.unity.render-pipelines.universal` (URP) | `17.3.0` | Unity Technologies | Unity Companion / Package License | Covered under Unity Editor license terms; verify exact package `LICENSE.md` in `Library/PackageCache` before distribution. |
| `com.unity.inputsystem` | `1.20.0` | Unity Technologies | Unity Companion / Package License | Included in Unity project. |
| `com.unity.ai.navigation` | `2.0.14` | Unity Technologies | Unity Companion / Package License | Included in Unity project. |
| `com.unity.ugui` | `2.0.0` | Unity Technologies | Unity Companion / Package License | Included in Unity project. |
| `com.unity.timeline` | `1.8.13` | Unity Technologies | Unity Companion / Package License | Included in Unity project. |
| `com.unity.visualscripting` | `1.9.12` | Unity Technologies | Unity Companion / Package License | Included in Unity project (audit whether needed in v1.0). |
| `com.unity.test-framework` | `1.6.0` | Unity Technologies | Unity Companion / Package License | Development/testing dependency. |
| `com.unity.ide.visualstudio` / `com.unity.ide.rider` / `com.unity.collab-proxy` / `com.unity.multiplayer.center` | `2.0.26` / `3.0.40` / `2.13.6` / `1.0.1` | Unity Technologies / JetBrains | Editor-only packages | Not shipped in player runtime; review whether unused packages (`multiplayer.center`, `collab-proxy`) should be removed later. |

---

## 2. Node.js / npm Dependencies (`CONFIRMED` from `package.json` & installed `node_modules/*/package.json`)

| Package | Installed Version (`package.json` Range) | Scope | Verified License | Commercial Distribution Obligations |
| :--- | :--- | :--- | :--- | :--- |
| **`@meta-sam/parser`** | `0.1.0` (`^0.1.0`) | Runtime | **SAM License** (Last Updated: November 19, 2025 â€” Meta Platforms, Inc. / Meta Platforms Ireland Ltd.; verified in `node_modules/@meta-sam/parser/LICENSE`) | Royalty-free limited license to use/redistribute **provided** a copy of the SAM License is included with any distribution of SAM Materials or derivatives, plus trade-control/indemnity/termination clauses. **Review before bundling in commercial PC distribution** (or exclude if SAM 3.1 is not shipped in v1.0). |
| **`sharp`** | `0.35.5` (`^0.35.5`) | Runtime | **Apache-2.0** (Lovell Fuller); bundles native **libvips** (LGPL-3.0) and image codec libraries | Retain Apache-2.0 notice; verify LGPL-3.0 dynamic-linking/notice compliance if `sharp` is ever bundled into a commercial Windows installer (not needed if citizen photo processing is omitted from v1.0). |
| **`tsx`** | `4.23.15` (`^4.19.3`) | DevDependency | **MIT** (Hiroki Osame) | Currently used at runtime by `scripts/launch-demo.ps1` to run `src/server/index.ts`. Retain MIT notice if bundled, or compile TS / migrate backend before commercial release. |
| **`typescript`** | `5.9.3` (`^5.8.2`) | DevDependency | **Apache-2.0** (Microsoft Corp.) | Build/typecheck tool only. |
| **`@types/node`** | `20.19.43` (`^20.17.24`) | DevDependency | **MIT** (DefinitelyTyped) | Compile-time type definitions only. |
| **`@types/sharp`** | `0.32.0` (`^0.32.0`) | DevDependency | **MIT** (DefinitelyTyped) | Compile-time type definitions only. |

---

## 3. Vendored Libraries & Staging Notices (`CONFIRMED`)

| Component | Path | Version | Verified License | Status & Required Action |
| :--- | :--- | :---: | :--- | :--- |
| **Leaflet** | `public/vendor/leaflet/` | `1.9.4` | **BSD-2-Clause** (standard Leaflet license; **license text file not currently present inside `public/vendor/leaflet/`**) | Used only by the legacy hackathon web SPA (`public/index.html`). If `public/` is retained, add Leaflet's full BSD-2-Clause license text; if `public/` is retired before v1.0, remove cleanly. |
| **Quaternius â€” Universal Base Characters Kit (Free)** | `unity-client/Assets/Staging/ElenaPilot/License_Standard.txt` & `Animations/License_Animations.txt` | Standard Free | **CC0 1.0 Universal** (Public Domain Dedication â€” `@Quaternius`) | **No Quaternius `.fbx` models remain in `Staging/ElenaPilot/`** (all 7 NPCs use procedural `BobbleheadGeometry.cs` meshes). Verify zero Quaternius assets remain in the project before deleting the staging folder in a later phase. |

---

## 4. Map Data & External APIs Referenced in Code (`CONFIRMED` references; Commercial Terms `UNVERIFIED`)

| External Service / Dataset | Code Reference | Purpose | Verified / Known Terms | Commercial Readiness Status |
| :--- | :--- | :--- | :--- | :--- |
| **OpenStreetMap (OSM)** | `WestManzanaPerimeterBuilder.cs` comments; `public/app.js` map tiles | Urban block dimensional reference; web map tiles | **ODbL 1.0** (requires `Â© OpenStreetMap contributors` attribution for derived map data; public tile servers prohibit heavy commercial app traffic) | **UNVERIFIED:** Confirm extent of OSM data derivation and include ODbL attribution where applicable; do not rely on free OSM tile servers in commercial builds. |
| **U.S. Customs & Border Protection (CBP) BWT API** | `src/adapters/cbp-border-wait-time-adapter.ts` (`bwt.cbp.gov/api/borderwaittimes`) | Optional live pedestrian wait-time observations (Paso del Norte & BOTA) | Public U.S. federal government data feed (rate limits / availability unguaranteed) | **UNVERIFIED:** Must remain strictly optional with clean offline fallback; never guaranteed continuously active. |
| **Open-Meteo API** | `src/adapters/open-meteo-weather-adapter.ts` | Optional live Ciudad JuÃ¡rez weather observation | Open-Meteo free API tier is **non-commercial only** (commercial use requires a paid commercial API license) | **UNVERIFIED / COMMERCIAL RISK:** Do **not** ship free-tier Open-Meteo calls in a commercial release without a commercial license or offline replacement. |
| **Public Exchange Rate API** | `src/adapters/public-exchange-rate-adapter.ts` | Optional USD/MXN exchange rate for LED signs | External public endpoint(s) â€” commercial redistribution rights unverified | **UNVERIFIED:** Keep strictly optional or drive LED signs from deterministic offline world-state simulation in commercial builds. |
| **Meta Model API (Muse Spark 1.3 & SAM 3.1)** | `src/adapters/meta-model-api-adapter.ts` (`https://api.meta.ai/v1`) | Optional LLM dialogue synthesis & optional citizen-photo segmentation | Proprietary Meta API Terms + SAM License; requires `MODEL_API_KEY` | **UNVERIFIED:** Never assume continuous availability or commercial API quota; keep strictly optional / disabled by default in offline commercial builds. |
| **Fideicomiso / YouTube Live Streams** | `FirstPersonPlayer.cs` (`IcvugJWPXz8`, `0Pg3S6s76IE`), `public/index.html` | Clickable bridge-camera Easter Eggs linking external live streams | Third-party YouTube Terms of Service & broadcaster rights | **UNVERIFIED / COMMERCIAL RISK:** Requires legal review or replacement with in-world simulated camera feeds prior to commercial release. |
