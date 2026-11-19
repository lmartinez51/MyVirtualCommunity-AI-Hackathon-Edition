using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using MyVirtualCommunity.Client.Api;

namespace MyVirtualCommunity.Client.World
{
    /// <summary>
    /// Drives an authentic outdoor double-sided SMD LED currency exchange rate sign
    /// (such as the large hanging board between Óptica Fashion and Cambios Santa Fe).
    ///
    /// Alternates every ~4 seconds between:
    /// - COMPRA (bright green header + huge bold green digits, flanked by yellow accent bars)
    /// - VENTA  (bright yellow header + huge bold yellow digits, flanked by green accent bars)
    ///
    /// Strictly respects single-source-of-truth architecture: polls the backend endpoint
    /// GET /api/world/exchange-rate via ApiClient and displays honest "--.--" when offline.
    /// </summary>
    public class ExchangeRateLedSignController : MonoBehaviour
    {
        [Header("Display Faces & Lighting (South & North)")]
        public Renderer[] displayFaceRenderers;
        [Tooltip("Serialized URP/Lit emissive template material so Unity never strips _EMISSION in standalone builds.")]
        public Material baseLedMaterial;
        [Tooltip("Real-time point lights in front of each LED face that cast dynamic green/yellow glow at night.")]
        public Light[] glowLights;

        [Header("Emission & Night Glow")]
        public float dayEmissionIntensity = 2.6f;
        public float nightEmissionIntensity = 5.2f;
        public float dayLightIntensity = 0.0f;
        public float nightLightIntensity = 1.85f;
        public float dayLightRange = 2.0f;
        public float nightLightRange = 4.6f;

        [Header("Timing & Refresh")]
        [Tooltip("Seconds each slide (COMPRA / VENTA) stays visible before alternating.")]
        public float slideIntervalSeconds = 4.0f;

        [Tooltip("Seconds between backend polling refreshes for authoritative FX quotes.")]
        public float pollIntervalSeconds = 300.0f;

        [Header("Runtime Inspection")]
        [SerializeField] private bool showingSellSlide = false;
        [SerializeField] private string latestStatus = "unavailable";
        [SerializeField] private float latestBuyRate = 0f;
        [SerializeField] private float latestSellRate = 0f;
        [SerializeField] private bool hasLiveQuote = false;

        private const int TexWidth = 512;
        private const int TexHeight = 300;

        private Texture2D ledTexture;
        private Color32[] pixelBuffer;
        private Material runtimeLedMaterial;
        private float slideTimer = 0f;
        private ApiClient apiClient;
        private TimeOfDayEnvironmentController timeOfDayController;
        private Coroutine pollCoroutine;
        private bool? nightOverrideForPreview = null;
        private bool lastAppliedNightState = false;

        public bool ShowingSellSlide => showingSellSlide;
        public bool HasLiveQuote => hasLiveQuote;
        public float LatestBuyRate => latestBuyRate;
        public float LatestSellRate => latestSellRate;
        public string LatestStatus => latestStatus;

        private static readonly Color32 BgBlack = new Color32(3, 4, 4, 255);
        private static readonly Color32 GridDim = new Color32(10, 12, 11, 255);
        private static readonly Color32 LedGreenBright = new Color32(48, 255, 92, 255);
        private static readonly Color32 LedGreenGlow = new Color32(14, 158, 44, 255);
        private static readonly Color32 LedYellowBright = new Color32(255, 232, 36, 255);
        private static readonly Color32 LedYellowGlow = new Color32(178, 150, 16, 255);
        private static readonly Color32 LedOfflineAmber = new Color32(228, 168, 48, 255);
        private static readonly Color LightGreenColor = new Color(0.18f, 1.0f, 0.36f, 1f);
        private static readonly Color LightYellowColor = new Color(1.0f, 0.88f, 0.16f, 1f);
        private static readonly Color LightOfflineAmberColor = new Color(1.0f, 0.68f, 0.18f, 1f);

        private void Awake()
        {
            EnsureRenderResources();
            RenderCurrentSlide();
        }

        private void OnEnable()
        {
            EnsureRenderResources();
            RenderCurrentSlide();
            if (Application.isPlaying)
            {
                if (pollCoroutine != null) StopCoroutine(pollCoroutine);
                pollCoroutine = StartCoroutine(PollExchangeRateLoop());
            }
        }

        private void OnDisable()
        {
            if (pollCoroutine != null)
            {
                StopCoroutine(pollCoroutine);
                pollCoroutine = null;
            }
        }

        private void OnDestroy()
        {
            if (ledTexture != null)
            {
                if (Application.isPlaying) Destroy(ledTexture);
                else DestroyImmediate(ledTexture);
            }
            if (runtimeLedMaterial != null)
            {
                if (Application.isPlaying) Destroy(runtimeLedMaterial);
                else DestroyImmediate(runtimeLedMaterial);
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            bool currentNight = IsNightModeActive();
            if (currentNight != lastAppliedNightState)
            {
                UpdateEmissionAndGlowLights();
            }

            float interval = Mathf.Max(1.0f, slideIntervalSeconds);
            slideTimer += Time.deltaTime;
            if (slideTimer >= interval)
            {
                slideTimer -= interval;
                showingSellSlide = !showingSellSlide;
                RenderCurrentSlide();
            }
        }

        public void SetNightPreviewOverride(bool? forceNight)
        {
            nightOverrideForPreview = forceNight;
            EnsureRenderResources();
            UpdateEmissionAndGlowLights();
        }

        private bool IsNightModeActive()
        {
            if (nightOverrideForPreview.HasValue)
            {
                return nightOverrideForPreview.Value;
            }
            if (timeOfDayController == null && Application.isPlaying)
            {
                timeOfDayController = FindAnyObjectByType<TimeOfDayEnvironmentController>();
            }
            if (timeOfDayController != null)
            {
                return timeOfDayController.IsNight;
            }
            return RenderSettings.ambientIntensity < 0.55f;
        }

        public void ApplyAuthoritativeExchangeRate(WorldExchangeRateObservationDto obs)
        {
            if (obs == null || !string.Equals(obs.status, "available", StringComparison.OrdinalIgnoreCase)
                || !obs.hasBuyRate || !obs.hasSellRate || obs.buyRate <= 0f || obs.sellRate <= 0f)
            {
                latestStatus = obs != null && !string.IsNullOrEmpty(obs.status) ? obs.status : "unavailable";
                hasLiveQuote = false;
                latestBuyRate = 0f;
                latestSellRate = 0f;
            }
            else
            {
                latestStatus = "available";
                hasLiveQuote = true;
                latestBuyRate = obs.buyRate;
                latestSellRate = obs.sellRate;
            }
            EnsureRenderResources();
            RenderCurrentSlide();
        }

        public void ForceSlide(bool showSell)
        {
            showingSellSlide = showSell;
            slideTimer = 0f;
            EnsureRenderResources();
            RenderCurrentSlide();
        }

        private IEnumerator PollExchangeRateLoop()
        {
            // Small initial delay so ApiClient initializes cleanly
            yield return new WaitForSeconds(0.5f);

            while (true)
            {
                if (apiClient == null)
                {
                    apiClient = FindAnyObjectByType<ApiClient>();
                }

                if (apiClient != null)
                {
                    bool done = false;
                    apiClient.GetWorldExchangeRate(false,
                        onSuccess: resp =>
                        {
                            if (resp != null && resp.success && resp.exchangeRate != null)
                            {
                                ApplyAuthoritativeExchangeRate(resp.exchangeRate);
                            }
                            done = true;
                        },
                        onError: err =>
                        {
                            Debug.LogWarning($"[ExchangeRateLedSign] Failed to poll exchange rate: {err}");
                            done = true;
                        });

                    float wait = 0f;
                    while (!done && wait < 8f)
                    {
                        wait += Time.deltaTime;
                        yield return null;
                    }
                }

                yield return new WaitForSeconds(Mathf.Max(30f, pollIntervalSeconds));
            }
        }

        private void EnsureRenderResources()
        {
            if (ledTexture == null)
            {
                ledTexture = new Texture2D(TexWidth, TexHeight, TextureFormat.RGBA32, false)
                {
                    name = "CambiosSantaFe_LiveLedTex",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                pixelBuffer = new Color32[TexWidth * TexHeight];
            }

            if (runtimeLedMaterial == null)
            {
                if (baseLedMaterial != null)
                {
                    runtimeLedMaterial = new Material(baseLedMaterial) { name = "CambiosSantaFe_LiveLedMat" };
                }
                else
                {
                    Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                        ?? Shader.Find("Universal Render Pipeline/Unlit")
                        ?? Shader.Find("Standard");
                    runtimeLedMaterial = new Material(shader) { name = "CambiosSantaFe_LiveLedMat" };
                }

                runtimeLedMaterial.SetTexture("_BaseMap", ledTexture);
                runtimeLedMaterial.SetTexture("_MainTex", ledTexture);
                runtimeLedMaterial.SetTexture("_EmissionMap", ledTexture);
                // Keep diffuse base low so ambient sky light never washes out the deep black LED panel;
                // all visual radiance comes cleanly from the HDR _EmissionMap (vivid day & blazing night!).
                runtimeLedMaterial.SetColor("_BaseColor", new Color(0.10f, 0.10f, 0.10f, 1.0f));
                runtimeLedMaterial.SetColor("_Color", new Color(0.10f, 0.10f, 0.10f, 1.0f));
                runtimeLedMaterial.SetFloat("_Smoothness", 0.18f);
                runtimeLedMaterial.SetFloat("_Metallic", 0.0f);
                runtimeLedMaterial.EnableKeyword("_EMISSION");
                runtimeLedMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            if (displayFaceRenderers != null)
            {
                for (int i = 0; i < displayFaceRenderers.Length; i++)
                {
                    if (displayFaceRenderers[i] != null && displayFaceRenderers[i].sharedMaterial != runtimeLedMaterial)
                    {
                        displayFaceRenderers[i].sharedMaterial = runtimeLedMaterial;
                    }
                }
            }
        }

        private void UpdateEmissionAndGlowLights()
        {
            bool isNight = IsNightModeActive();
            lastAppliedNightState = isNight;

            float emissionBoost = isNight ? nightEmissionIntensity : dayEmissionIntensity;
            if (!hasLiveQuote)
            {
                emissionBoost *= 0.85f;
            }

            if (runtimeLedMaterial != null)
            {
                runtimeLedMaterial.EnableKeyword("_EMISSION");
                runtimeLedMaterial.SetColor("_EmissionColor", new Color(emissionBoost, emissionBoost, emissionBoost, 1.0f));
            }

            if (glowLights != null)
            {
                Color lightCol = !hasLiveQuote
                    ? LightOfflineAmberColor
                    : (showingSellSlide ? LightYellowColor : LightGreenColor);
                float lightInt = isNight ? nightLightIntensity : dayLightIntensity;
                float lightRng = isNight ? nightLightRange : dayLightRange;
                if (!hasLiveQuote) lightInt *= 0.55f;

                for (int i = 0; i < glowLights.Length; i++)
                {
                    if (glowLights[i] == null) continue;
                    glowLights[i].enabled = lightInt > 0.01f;
                    glowLights[i].color = lightCol;
                    glowLights[i].intensity = lightInt;
                    glowLights[i].range = lightRng;
                }
            }
        }

        private void RenderCurrentSlide()
        {
            if (ledTexture == null || pixelBuffer == null) return;

            // Fill subtle SMD LED matrix dark grid background
            for (int y = 0; y < TexHeight; y++)
            {
                bool rowLine = (y % 6) == 0;
                int rowOff = y * TexWidth;
                for (int x = 0; x < TexWidth; x++)
                {
                    pixelBuffer[rowOff + x] = (rowLine || (x % 6) == 0) ? GridDim : BgBlack;
                }
            }

            bool isSell = showingSellSlide;
            string headerText = isSell ? "VENTA" : "COMPRA";
            string valueText = hasLiveQuote
                ? (isSell ? latestSellRate : latestBuyRate).ToString("0.00", CultureInfo.InvariantCulture)
                : "--.--";

            Color32 mainColor = !hasLiveQuote
                ? LedOfflineAmber
                : (isSell ? LedYellowBright : LedGreenBright);
            Color32 glowColor = !hasLiveQuote
                ? BgBlack
                : (isSell ? LedYellowGlow : LedGreenGlow);
            Color32 sideBarColor = isSell ? LedGreenBright : LedYellowBright;
            Color32 sideBarGlow = isSell ? LedGreenGlow : LedYellowGlow;

            // Side vertical LED accent bars flanking the big numbers (signature detail from reference video!)
            FillLedRectWithGlow(16, 26, 14, 198, sideBarColor, sideBarGlow);
            FillLedRectWithGlow(TexWidth - 30, 26, 14, 198, sideBarColor, sideBarGlow);

            // Top small header text ("COMPRA" or "VENTA") in upper band (Y = 236..282 in Texture2D bottom-up coords)
            DrawSmallMatrixTextCentered(headerText, centerX: TexWidth / 2, baseY: 238, pixelScale: 6, letterSpacing: 16, mainColor, glowColor);

            // Huge tall bold numeric readout (Y = 26..218 -> 192px tall, >4x taller than header letters!)
            DrawHugeDigitStringCentered(valueText, centerX: TexWidth / 2, baseY: 26, digitWidth: 82, digitHeight: 192, thickness: 18, spacing: 18, mainColor, glowColor);

            ledTexture.SetPixels32(pixelBuffer);
            ledTexture.Apply(false, false);
            UpdateEmissionAndGlowLights();
        }

        private void FillRect(int x0, int y0, int w, int h, Color32 col)
        {
            int xMin = Mathf.Clamp(x0, 0, TexWidth);
            int xMax = Mathf.Clamp(x0 + w, 0, TexWidth);
            int yMin = Mathf.Clamp(y0, 0, TexHeight);
            int yMax = Mathf.Clamp(y0 + h, 0, TexHeight);
            for (int y = yMin; y < yMax; y++)
            {
                int row = y * TexWidth;
                for (int x = xMin; x < xMax; x++)
                {
                    pixelBuffer[row + x] = col;
                }
            }
        }

        private void FillRectMax(int x0, int y0, int w, int h, Color32 col)
        {
            int xMin = Mathf.Clamp(x0, 0, TexWidth);
            int xMax = Mathf.Clamp(x0 + w, 0, TexWidth);
            int yMin = Mathf.Clamp(y0, 0, TexHeight);
            int yMax = Mathf.Clamp(y0 + h, 0, TexHeight);
            for (int y = yMin; y < yMax; y++)
            {
                int row = y * TexWidth;
                for (int x = xMin; x < xMax; x++)
                {
                    Color32 cur = pixelBuffer[row + x];
                    pixelBuffer[row + x] = new Color32(
                        cur.r > col.r ? cur.r : col.r,
                        cur.g > col.g ? cur.g : col.g,
                        cur.b > col.b ? cur.b : col.b,
                        255);
                }
            }
        }

        private void FillLedRectWithGlow(int x0, int y0, int w, int h, Color32 core, Color32 glow)
        {
            Color32 softOuter = new Color32((byte)(glow.r / 2), (byte)(glow.g / 2), (byte)(glow.b / 2), 255);
            FillRectMax(x0 - 6, y0 - 6, w + 12, h + 12, softOuter);
            FillRectMax(x0 - 3, y0 - 3, w + 6, h + 6, glow);
            FillRectMax(x0, y0, w, h, core);
        }

        private void DrawSmallMatrixTextCentered(string text, int centerX, int baseY, int pixelScale, int letterSpacing, Color32 core, Color32 glow)
        {
            int charW = 5 * pixelScale;
            int totalW = text.Length * charW + Mathf.Max(0, text.Length - 1) * letterSpacing;
            int startX = centerX - totalW / 2;
            for (int i = 0; i < text.Length; i++)
            {
                byte[] rows = GetLetterGlyph5x7(text[i]);
                int cx = startX + i * (charW + letterSpacing);
                for (int r = 0; r < 7; r++)
                {
                    byte bits = rows[r];
                    int py = baseY + (6 - r) * pixelScale;
                    for (int c = 0; c < 5; c++)
                    {
                        if ((bits & (1 << (4 - c))) != 0)
                        {
                            FillLedRectWithGlow(cx + c * pixelScale, py, pixelScale - 1, pixelScale - 1, core, glow);
                        }
                    }
                }
            }
        }

        private void DrawHugeDigitStringCentered(string text, int centerX, int baseY, int digitWidth, int digitHeight, int thickness, int spacing, Color32 core, Color32 glow)
        {
            int totalW = 0;
            for (int i = 0; i < text.Length; i++)
            {
                totalW += (text[i] == '.') ? (thickness + 8) : digitWidth;
                if (i < text.Length - 1) totalW += spacing;
            }

            int cursorX = centerX - totalW / 2;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '.')
                {
                    FillLedRectWithGlow(cursorX + 4, baseY + 4, thickness + 2, thickness + 2, core, glow);
                    cursorX += thickness + 8 + spacing;
                }
                else
                {
                    DrawSevenSegmentChar(ch, cursorX, baseY, digitWidth, digitHeight, thickness, core, glow);
                    cursorX += digitWidth + spacing;
                }
            }
        }

        private void DrawSevenSegmentChar(char ch, int x, int y, int w, int h, int t, Color32 core, Color32 glow)
        {
            // Bitmask: 0=A(top), 1=B(top-right), 2=C(bot-right), 3=D(bot), 4=E(bot-left), 5=F(top-left), 6=G(mid)
            int mask = ch switch
            {
                '0' => 0b0111111,
                '1' => 0b0000110,
                '2' => 0b1011011,
                '3' => 0b1001111,
                '4' => 0b1100110,
                '5' => 0b1101101,
                '6' => 0b1111101,
                '7' => 0b0000111,
                '8' => 0b1111111,
                '9' => 0b1101111,
                '-' => 0b1000000,
                _ => 0
            };

            int halfH = h / 2;
            int pad = 4;

            // A: top horizontal
            if ((mask & (1 << 0)) != 0) FillLedRectWithGlow(x + pad, y + h - t, w - pad * 2, t, core, glow);
            // B: top-right vertical
            if ((mask & (1 << 1)) != 0) FillLedRectWithGlow(x + w - t, y + halfH + pad / 2, t, halfH - pad, core, glow);
            // C: bottom-right vertical
            if ((mask & (1 << 2)) != 0) FillLedRectWithGlow(x + w - t, y + pad, t, halfH - pad, core, glow);
            // D: bottom horizontal
            if ((mask & (1 << 3)) != 0) FillLedRectWithGlow(x + pad, y, w - pad * 2, t, core, glow);
            // E: bottom-left vertical
            if ((mask & (1 << 4)) != 0) FillLedRectWithGlow(x, y + pad, t, halfH - pad, core, glow);
            // F: top-left vertical
            if ((mask & (1 << 5)) != 0) FillLedRectWithGlow(x, y + halfH + pad / 2, t, halfH - pad, core, glow);
            // G: middle horizontal
            if ((mask & (1 << 6)) != 0) FillLedRectWithGlow(x + pad, y + halfH - t / 2, w - pad * 2, t, core, glow);
        }

        private static byte[] GetLetterGlyph5x7(char c)
        {
            return char.ToUpperInvariant(c) switch
            {
                'C' => new byte[] { 0b01110, 0b10001, 0b10000, 0b10000, 0b10000, 0b10001, 0b01110 },
                'O' => new byte[] { 0b01110, 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01110 },
                'M' => new byte[] { 0b10001, 0b11011, 0b10101, 0b10101, 0b10001, 0b10001, 0b10001 },
                'P' => new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10000, 0b10000, 0b10000 },
                'R' => new byte[] { 0b11110, 0b10001, 0b10001, 0b11110, 0b10100, 0b10010, 0b10001 },
                'A' => new byte[] { 0b01110, 0b10001, 0b10001, 0b11111, 0b10001, 0b10001, 0b10001 },
                'V' => new byte[] { 0b10001, 0b10001, 0b10001, 0b10001, 0b10001, 0b01010, 0b00100 },
                'E' => new byte[] { 0b11111, 0b10000, 0b10000, 0b11110, 0b10000, 0b10000, 0b11111 },
                'N' => new byte[] { 0b10001, 0b11001, 0b10101, 0b10011, 0b10001, 0b10001, 0b10001 },
                'T' => new byte[] { 0b11111, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100, 0b00100 },
                _ => new byte[] { 0, 0, 0, 0, 0, 0, 0 }
            };
        }
    }
}
