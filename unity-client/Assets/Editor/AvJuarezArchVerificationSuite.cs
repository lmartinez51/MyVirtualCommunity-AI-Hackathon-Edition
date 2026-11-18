using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Focused Unity verification suite for the Av. Benito Juárez Digital Arch,
    /// its structural clearance, signage presence, LED garland indexing, and
    /// live telemetry + media display controller.
    /// </summary>
    public static class AvJuarezArchVerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Verify Av. Juárez Digital Arch")]
        public static void RunVerification()
        {
            Debug.Log("[AvJuarezArchVerification] Starting verification...");

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[AvJuarezArchVerification] FAIL: Could not open {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            int passed = 0;
            int total = 0;

            void Check(string label, bool condition)
            {
                total++;
                if (condition)
                {
                    passed++;
                    Debug.Log($"  [PASS] {label}");
                }
                else
                {
                    Debug.LogError($"  [FAIL] {label}");
                }
            }

            GameObject archRoot = GameObject.Find("AvJuarez_DigitalArch_Root");
            Check("Arch root exists in BorderCorridorScene", archRoot != null);

            GameObject westCol = GameObject.Find("Column_West_Lattice");
            GameObject eastCol = GameObject.Find("Column_East_Lattice");
            Check(
                "West & East lattice columns stand cleanly outside curb bounds",
                westCol != null && eastCol != null &&
                westCol.transform.position.x <= -3.0f &&
                eastCol.transform.position.x >= 7.2f &&
                Mathf.Abs(westCol.transform.position.z - AvJuarezArchBuilder.ArchZ) < 0.05f &&
                Mathf.Abs(eastCol.transform.position.z - AvJuarezArchBuilder.ArchZ) < 0.05f);

            GameObject juarezPlaque = GameObject.Find("Plaque_CiudadJuarez");
            GameObject bienvenidosPlaque = GameObject.Find("Plaque_Bienvenidos");
            GameObject speedSign = GameObject.Find("Sign_SpeedLimit_15Kmh");
            Check("Mounted plaques CIUDAD JUÁREZ, BIENVENIDOS, and 15 km/h MÁXIMO sign exist",
                juarezPlaque != null && bienvenidosPlaque != null && speedSign != null);
            Check("Lower BIENVENIDOS plaque clears vehicle height (>= 4.75m)",
                bienvenidosPlaque != null && bienvenidosPlaque.transform.position.y >= 4.75f);

            GameObject leftWing = GameObject.Find("SidePanel_West_Announce");
            GameObject rightWing = GameObject.Find("SidePanel_East_Announce");
            Check("Left and Right ROGER side panels exist", leftWing != null && rightWing != null);

            GameObject bulbsRoot = GameObject.Find("Arch_Garland_LED_Bulbs");
            Check("LED garland contains 36 bulbs", bulbsRoot != null && bulbsRoot.transform.childCount == 36);

            JuarezArchDisplayController displayCtrl = UnityEngine.Object.FindAnyObjectByType<JuarezArchDisplayController>();
            Check("JuarezArchDisplayController attached to central LED canvas", displayCtrl != null);

            if (displayCtrl != null)
            {
                // Feed synthetic authoritative weather & CBP wait time and verify label propagation
                displayCtrl.ApplyAuthoritativeWeather(new WorldWeatherObservationDto
                {
                    status = "available",
                    freshness = "live",
                    locationName = "Ciudad Juárez, Chih.",
                    temperatureCelsius = 22.4f,
                    relativeHumidityPercent = 25f,
                    windSpeedKmh = 12f,
                    conditionLabel = new BilingualTextDto { en = "Clear sky", es = "Despejado" }
                });
                displayCtrl.ApplyAuthoritativeBorderObservation(new BorderCrossingObservationDto
                {
                    operationalStatus = "available",
                    portStatus = "Open",
                    freshness = "live",
                    hasDelayMinutes = true,
                    delayMinutes = 18
                });
                displayCtrl.ActivateWeatherClockBoard();
                Check("Display controller activates Live Clock & Weather Board cleanly",
                    displayCtrl.CurrentDisplayMode == JuarezArchDisplayController.DisplayMode.WeatherAndClockBoard &&
                    displayCtrl.LatestWeather != null &&
                    Mathf.Approximately(displayCtrl.LatestWeather.temperatureCelsius, 22.4f));
                Check("Display timing settings configured cleanly (10s min, 45s max, 7s pauses)",
                    Mathf.Approximately(displayCtrl.MinVideoDurationSeconds, 10.0f) &&
                    Mathf.Approximately(displayCtrl.MaxVideoDurationSeconds, 45.0f) &&
                    Mathf.Approximately(displayCtrl.InfoSlideDurationSeconds, 7.0f) &&
                    Mathf.Approximately(displayCtrl.PromoSlideDurationSeconds, 7.0f));

                displayCtrl.ApplySyncedBackendPlaylist(new[] { "C:/temp/sponsor_40s.mp4", "C:/temp/camila_loop.mp4" });
                Check("Synced backend playlist takes precedence when populated and clears back to local fallback",
                    displayCtrl.UsingBackendPlaylist && displayCtrl.DiscoveredVideoPaths.Count == 2);
                displayCtrl.ApplySyncedBackendPlaylist(Array.Empty<string>());
                Check("Clearing backend playlist restores local fallback cleanly",
                    !displayCtrl.UsingBackendPlaylist);

                CaptureScreenshot("arch_south_wide.png", new Vector3(-2.5f, 2.3f, -43.5f), new Vector3(2.1f, 5.2f, AvJuarezArchBuilder.ArchZ), 52f);
                CaptureScreenshot("arch_closeup_board.png", new Vector3(1.2f, 4.0f, -34.0f), new Vector3(2.1f, 6.2f, AvJuarezArchBuilder.ArchZ), 46f);
                displayCtrl.ActivatePromoSlide();
                CaptureScreenshot("arch_closeup_promo.png", new Vector3(1.2f, 4.0f, -34.0f), new Vector3(2.1f, 6.2f, AvJuarezArchBuilder.ArchZ), 46f);
            }

            string archMediaDir = Path.GetFullPath("Assets/StreamingAssets/ArchMedia");
            Check("Assets/StreamingAssets/ArchMedia folder exists for camila_loop.mp4", Directory.Exists(archMediaDir));

            Debug.Log($"[AvJuarezArchVerification] SUMMARY: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }

        private static void CaptureScreenshot(string fileName, Vector3 camPos, Vector3 lookAt, float fov)
        {
            const string outDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            var camGo = new GameObject("Temp_ArchVerifyCam");
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = fov;
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 300f;
            cam.transform.position = camPos;
            cam.transform.LookAt(lookAt);

            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            var tex = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            cam.Render();
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            tex.Apply();
            File.WriteAllBytes(Path.Combine(outDir, fileName), tex.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);
            UnityEngine.Object.DestroyImmediate(camGo);
        }
    }
}
