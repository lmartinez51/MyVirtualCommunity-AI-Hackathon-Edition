using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Programmatic generator for the Increment 5.2/5.3 3D playable corridor scene.
    /// Builds Avenida Benito Juárez, storefronts, pedestrian queue, bridge canopy,
    /// lighting, colliders, player character, and interactive NPC stations.
    /// </summary>
    public static class CorridorSceneSetup
    {
        private const string CorridorScenePath = "Assets/Scenes/BorderCorridorScene.unity";
        private const string HandshakeScenePath = "Assets/Scenes/HandshakeScene.unity";

        [MenuItem("MyVirtualCommunity/Setup Border Corridor Scene")]
        public static void BuildCorridorScene()
        {
            Debug.Log("[CorridorSceneSetup] Building Playable Border Corridor Scene...");

            string dir = Path.GetDirectoryName(CorridorScenePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Setup Materials
            Material asphaltMat = CreateOrGetMaterial("Assets/Settings/AsphaltMaterial.mat", new Color(0.18f, 0.18f, 0.20f), 0.2f);
            Material concreteMat = CreateOrGetMaterial("Assets/Settings/ConcreteMaterial.mat", new Color(0.68f, 0.67f, 0.64f), 0.1f);
            Material curbMat = CreateOrGetMaterial("Assets/Settings/CurbMaterial.mat", new Color(0.82f, 0.80f, 0.78f), 0.1f);
            Material stripeMat = CreateOrGetMaterial("Assets/Settings/YellowStripeMaterial.mat", new Color(0.95f, 0.80f, 0.15f), 0.2f);
            Material whiteStripeMat = CreateOrGetMaterial("Assets/Settings/WhiteStripeMaterial.mat", new Color(0.95f, 0.95f, 0.95f), 0.2f);
            Material glassMat = CreateOrGetMaterial("Assets/Settings/GlassMaterial.mat", new Color(0.2f, 0.35f, 0.45f, 0.85f), 0.9f);

            // Benavides Materials (Royal Blue / Red / White)
            Material benavidesFacadeMat = CreateOrGetMaterial("Assets/Settings/BenavidesFacade.mat", new Color(0.92f, 0.93f, 0.95f), 0.1f);
            Material benavidesBlueMat = CreateOrGetMaterial("Assets/Settings/BenavidesBlue.mat", new Color(0.04f, 0.28f, 0.68f), 0.4f);
            Material benavidesRedMat = CreateOrGetMaterial("Assets/Settings/BenavidesRed.mat", new Color(0.82f, 0.12f, 0.12f), 0.3f);

            // Óptica Fashion Materials (Matte Charcoal / Gold Accent)
            Material opticaFacadeMat = CreateOrGetMaterial("Assets/Settings/OpticaFacade.mat", new Color(0.22f, 0.23f, 0.25f), 0.3f);
            Material opticaGoldMat = CreateOrGetMaterial("Assets/Settings/OpticaGold.mat", new Color(0.88f, 0.72f, 0.25f), 0.6f);

            // Border Infrastructure Materials
            Material bridgeTurquoiseMat = CreateOrGetMaterial("Assets/Settings/BridgeTurquoise.mat", new Color(0.08f, 0.62f, 0.58f), 0.3f);
            Material bridgeSteelMat = CreateOrGetMaterial("Assets/Settings/BridgeSteel.mat", new Color(0.75f, 0.77f, 0.80f), 0.7f);
            Material greenSignMat = CreateOrGetMaterial("Assets/Settings/GreenSign.mat", new Color(0.05f, 0.45f, 0.22f), 0.2f);

            // Witness Standee Materials
            Material witnessMateoMat = CreateOrGetMaterial("Assets/Settings/WitnessMateo.mat", new Color(0.15f, 0.35f, 0.65f), 0.3f);
            Material witnessSofiaMat = CreateOrGetMaterial("Assets/Settings/WitnessSofia.mat", new Color(0.65f, 0.18f, 0.38f), 0.3f);
            Material witnessHectorMat = CreateOrGetMaterial("Assets/Settings/WitnessHector.mat", new Color(0.62f, 0.38f, 0.22f), 0.3f);
            Material highlightGoldMat = CreateOrGetMaterial("Assets/Settings/HighlightRing.mat", new Color(1.0f, 0.85f, 0.3f), 0.5f);

            // Environment Root
            GameObject envRoot = new GameObject("Environment_AvenidaJuarez");

            // 1. Roadbed (Avenida Juárez: 14m wide, 90m long, Z from -25 to +65)
            GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);
            road.name = "Road_AvenidaJuarez";
            road.transform.SetParent(envRoot.transform);
            road.transform.position = new Vector3(0f, -0.05f, 20f);
            road.transform.localScale = new Vector3(14f, 0.1f, 90f);
            SetMaterial(road, asphaltMat);

            // 2. Road Markings: Double Yellow Centerline & White Shoulder Lines
            GameObject centerLine1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine1.name = "Stripe_Yellow_Center_1";
            centerLine1.transform.SetParent(envRoot.transform);
            centerLine1.transform.position = new Vector3(-0.15f, 0.01f, 20f);
            centerLine1.transform.localScale = new Vector3(0.12f, 0.01f, 90f);
            SetMaterial(centerLine1, stripeMat);
            UnityEngine.Object.DestroyImmediate(centerLine1.GetComponent<Collider>());

            GameObject centerLine2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine2.name = "Stripe_Yellow_Center_2";
            centerLine2.transform.SetParent(envRoot.transform);
            centerLine2.transform.position = new Vector3(0.15f, 0.01f, 20f);
            centerLine2.transform.localScale = new Vector3(0.12f, 0.01f, 90f);
            SetMaterial(centerLine2, stripeMat);
            UnityEngine.Object.DestroyImmediate(centerLine2.GetComponent<Collider>());

            // Pedestrian Crosswalk near storefronts (Z = -15m)
            for (int i = -6; i <= 6; i++)
            {
                if (i == 0) continue;
                GameObject crossStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crossStripe.name = $"Crosswalk_Stripe_{i}";
                crossStripe.transform.SetParent(envRoot.transform);
                crossStripe.transform.position = new Vector3(i * 1.0f, 0.01f, -15f);
                crossStripe.transform.localScale = new Vector3(0.5f, 0.01f, 3.5f);
                SetMaterial(crossStripe, whiteStripeMat);
                UnityEngine.Object.DestroyImmediate(crossStripe.GetComponent<Collider>());
            }

            // 3. Sidewalks & Curbs
            // East Sidewalk (Positive X: X = 7.0 to 12.0)
            GameObject eastSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eastSidewalk.name = "Sidewalk_East";
            eastSidewalk.transform.SetParent(envRoot.transform);
            eastSidewalk.transform.position = new Vector3(9.5f, 0.1f, 20f);
            eastSidewalk.transform.localScale = new Vector3(5f, 0.2f, 90f);
            SetMaterial(eastSidewalk, concreteMat);

            CreateCurb("Curb_East", new Vector3(6.9f, 0.1f, 20f), new Vector3(0.2f, 0.2f, 90f), envRoot, curbMat);

            // West Sidewalk (Negative X: X = -7.0 to -12.0)
            GameObject westSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            westSidewalk.name = "Sidewalk_West";
            westSidewalk.transform.SetParent(envRoot.transform);
            westSidewalk.transform.position = new Vector3(-9.5f, 0.1f, 20f);
            westSidewalk.transform.localScale = new Vector3(5f, 0.2f, 90f);
            SetMaterial(westSidewalk, concreteMat);

            CreateCurb("Curb_West", new Vector3(-6.9f, 0.1f, 20f), new Vector3(0.2f, 0.2f, 90f), envRoot, curbMat);

            // Buildings Root
            GameObject buildingsRoot = new GameObject("Buildings_Corridor");

            // 4. Commercial Buildings: East Side (Farmacias Benavides @ Av. Juárez 610)
            GameObject benavides = new GameObject("Storefront_FarmaciasBenavides");
            benavides.transform.SetParent(buildingsRoot.transform);

            // Main Building Mass (14m wide, 7m high, 12m deep)
            GameObject benMain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            benMain.name = "Benavides_MainBuilding";
            benMain.transform.SetParent(benavides.transform);
            benMain.transform.position = new Vector3(18.3f, 3.5f, 0f);
            benMain.transform.localScale = new Vector3(12f, 7.0f, 15f);
            SetMaterial(benMain, benavidesFacadeMat);

            // Royal Blue Upper Fascia
            GameObject benFascia = GameObject.CreatePrimitive(PrimitiveType.Cube);
            benFascia.name = "Benavides_Fascia";
            benFascia.transform.SetParent(benavides.transform);
            benFascia.transform.position = new Vector3(12.2f, 5.3f, 0f);
            benFascia.transform.localScale = new Vector3(0.5f, 1.8f, 14.6f);
            SetMaterial(benFascia, benavidesBlueMat);

            // Red Trim Accent Stripe
            GameObject benRedStripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            benRedStripe.name = "Benavides_RedStripe";
            benRedStripe.transform.SetParent(benavides.transform);
            benRedStripe.transform.position = new Vector3(12.15f, 4.35f, 0f);
            benRedStripe.transform.localScale = new Vector3(0.5f, 0.15f, 14.6f);
            SetMaterial(benRedStripe, benavidesRedMat);

            // Signboard
            GameObject benSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            benSign.name = "Benavides_Signboard";
            benSign.transform.SetParent(benavides.transform);
            benSign.transform.position = new Vector3(11.9f, 5.4f, 0f);
            benSign.transform.localScale = new Vector3(0.15f, 1.1f, 10f);
            SetMaterial(benSign, benavidesBlueMat);

            // Display Windows
            CreateDisplayWindow("Benavides_Window_North", new Vector3(12.25f, 2.0f, 4.0f), new Vector3(0.2f, 2.8f, 4.5f), benavides.transform, glassMat);
            CreateDisplayWindow("Benavides_Window_South", new Vector3(12.25f, 2.0f, -4.0f), new Vector3(0.2f, 2.8f, 4.5f), benavides.transform, glassMat);

            // Awning
            GameObject benAwning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            benAwning.name = "Benavides_Awning";
            benAwning.transform.SetParent(benavides.transform);
            benAwning.transform.position = new Vector3(11.2f, 3.8f, 0f);
            benAwning.transform.rotation = Quaternion.Euler(0f, 0f, 12f);
            benAwning.transform.localScale = new Vector3(2.2f, 0.1f, 13f);
            SetMaterial(benAwning, benavidesBlueMat);

            // Farmacias Benavides Interaction Point (Carlos Méndez)
            GameObject benInteractGo = new GameObject("InteractionPoint_FarmaciasBenavides");
            benInteractGo.transform.SetParent(benavides.transform);
            benInteractGo.transform.position = new Vector3(10.5f, 1.0f, 0f);
            var benInteract = benInteractGo.AddComponent<InteractionPoint>();
            benInteract.category = InteractionCategory.StorefrontAttendant;
            benInteract.characterId = "carlos-farmacia-attendant";
            benInteract.characterName = "Carlos Méndez";
            benInteract.characterRole = "Virtual Pharmacy Attendant";
            benInteract.avatar = "💊";
            benInteract.landmarkId = "pharmacy:farmacias-benavides";
            benInteract.title = "Farmacias Benavides (Av. Benito Juárez 610)";
            benInteract.subtitle = "Virtual Retail Pharmacy • Carlos Méndez";
            benInteract.address = "Av. Benito Juárez 610, Centro (colonia Chamizal unverified)";
            benInteract.description = "Verified retail pharmacy offering prescription medications, personal care goods, and convenience items along the Avenida Juárez corridor near the international border crossing.";
            benInteract.backendFeatureNotice = "Connected to Increment 4: Grounded Storefront Attendant (Carlos Méndez).\nEvaluated via authoritative World Core snapshot (Mon-Sat 07:00-22:00, closed Sun).\nGuardrails enforce strict refusal of medical diagnoses, prescription advice, and live inventory levels.";
            benInteract.interactionRadius = 4.2f;
            benInteract.promptText = "[E] Speak with Carlos Méndez · Farmacias Benavides";
            benInteract.suggestedPromptsEn = new string[]
            {
                "Is the pharmacy open right now?",
                "What are your business hours?",
                "Can you check if you have medication in stock?"
            };
            benInteract.suggestedPromptsEs = new string[]
            {
                "¿Está abierta la farmacia en este momento?",
                "¿Cuáles son los horarios de atención?",
                "¿Puedo surtir una receta médica aquí?"
            };

            // Visual Highlight Indicator
            GameObject benHighlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            benHighlight.name = "Highlight_Indicator";
            benHighlight.transform.SetParent(benInteractGo.transform);
            benHighlight.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            benHighlight.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
            SetMaterial(benHighlight, benavidesBlueMat);
            UnityEngine.Object.DestroyImmediate(benHighlight.GetComponent<Collider>());

            // 5. Commercial Buildings: West Side (Óptica Fashion @ Av. Juárez 859)
            GameObject optica = new GameObject("Storefront_OpticaFashion");
            optica.transform.SetParent(buildingsRoot.transform);

            // Main Building Mass (14m wide, 7m high, 12m deep)
            GameObject optMain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            optMain.name = "Optica_MainBuilding";
            optMain.transform.SetParent(optica.transform);
            optMain.transform.position = new Vector3(-18.3f, 3.5f, 1.5f);
            optMain.transform.localScale = new Vector3(12f, 7.0f, 15f);
            SetMaterial(optMain, opticaFacadeMat);

            // Matte Black Upper Fascia
            GameObject optFascia = GameObject.CreatePrimitive(PrimitiveType.Cube);
            optFascia.name = "Optica_Fascia";
            optFascia.transform.SetParent(optica.transform);
            optFascia.transform.position = new Vector3(-12.2f, 5.3f, 1.5f);
            optFascia.transform.localScale = new Vector3(0.5f, 1.8f, 14.6f);
            SetMaterial(optFascia, opticaFacadeMat);

            // Gold Trim Line
            GameObject optGoldLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            optGoldLine.name = "Optica_GoldLine";
            optGoldLine.transform.SetParent(optica.transform);
            optGoldLine.transform.position = new Vector3(-12.15f, 4.35f, 1.5f);
            optGoldLine.transform.localScale = new Vector3(0.5f, 0.15f, 14.6f);
            SetMaterial(optGoldLine, opticaGoldMat);

            // Signboard
            GameObject optSign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            optSign.name = "Optica_Signboard";
            optSign.transform.SetParent(optica.transform);
            optSign.transform.position = new Vector3(-11.9f, 5.4f, 1.5f);
            optSign.transform.localScale = new Vector3(0.15f, 1.1f, 10f);
            SetMaterial(optSign, opticaGoldMat);

            // Display Windows
            CreateDisplayWindow("Optica_Window_North", new Vector3(-12.25f, 2.0f, 5.5f), new Vector3(0.2f, 2.8f, 4.5f), optica.transform, glassMat);
            CreateDisplayWindow("Optica_Window_South", new Vector3(-12.25f, 2.0f, -2.5f), new Vector3(0.2f, 2.8f, 4.5f), optica.transform, glassMat);

            // Awning
            GameObject optAwning = GameObject.CreatePrimitive(PrimitiveType.Cube);
            optAwning.name = "Optica_Awning";
            optAwning.transform.SetParent(optica.transform);
            optAwning.transform.position = new Vector3(-11.2f, 3.8f, 1.5f);
            optAwning.transform.rotation = Quaternion.Euler(0f, 0f, -12f);
            optAwning.transform.localScale = new Vector3(2.2f, 0.1f, 13f);
            SetMaterial(optAwning, opticaFacadeMat);

            // Óptica Fashion Interaction Point (Valeria Domínguez)
            GameObject optInteractGo = new GameObject("InteractionPoint_OpticaFashion");
            optInteractGo.transform.SetParent(optica.transform);
            optInteractGo.transform.position = new Vector3(-10.5f, 1.0f, 1.5f);
            var optInteract = optInteractGo.AddComponent<InteractionPoint>();
            optInteract.category = InteractionCategory.StorefrontAttendant;
            optInteract.characterId = "valeria-optica-attendant";
            optInteract.characterName = "Valeria Domínguez";
            optInteract.characterRole = "Virtual Optical Attendant";
            optInteract.avatar = "👓";
            optInteract.landmarkId = "optical:optica-fashion";
            optInteract.title = "Óptica Fashion (Av. Benito Juárez 859)";
            optInteract.subtitle = "Virtual Optical Boutique • Valeria Domínguez";
            optInteract.address = "Av. Benito Juárez 859, Centro";
            optInteract.description = "Specialty prescription eyewear, optometry consultations, and frame styling boutique located directly across Avenida Juárez from Farmacias Benavides.";
            optInteract.backendFeatureNotice = "Connected to Increment 4: Grounded Storefront Attendant (Valeria Domínguez).\nEvaluated via authoritative World Core snapshot (Mon-Sat 09:00-17:00, closed Sun).\nStrictly refuses live frame catalog inventory and eye exam appointment bookings.";
            optInteract.interactionRadius = 4.2f;
            optInteract.promptText = "[E] Speak with Valeria Domínguez · Óptica Fashion";
            optInteract.suggestedPromptsEn = new string[]
            {
                "Are you open right now?",
                "What are your boutique hours?",
                "Can I schedule an eye exam appointment?"
            };
            optInteract.suggestedPromptsEs = new string[]
            {
                "¿Está abierta la óptica en este momento?",
                "¿Cuáles son los horarios de atención?",
                "¿Puedo agendar una cita para examen de la vista?"
            };

            // Visual Highlight Indicator
            GameObject optHighlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            optHighlight.name = "Highlight_Indicator";
            optHighlight.transform.SetParent(optInteractGo.transform);
            optHighlight.transform.localPosition = new Vector3(0f, -0.9f, 0f);
            optHighlight.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
            SetMaterial(optHighlight, opticaGoldMat);
            UnityEngine.Object.DestroyImmediate(optHighlight.GetComponent<Collider>());

            // 6. Adjacent Urban Buildings (Flanking Storefronts to complete street canyon)
            CreateUrbanBuilding("UrbanBuilding_East_South", new Vector3(18.3f, 4.0f, -17f), new Vector3(12f, 8f, 16f), buildingsRoot, CreateOrGetMaterial("Assets/Settings/StuccoWarm.mat", new Color(0.85f, 0.76f, 0.65f), 0.1f));
            CreateUrbanBuilding("UrbanBuilding_East_North", new Vector3(18.3f, 4.5f, 17f), new Vector3(12f, 9f, 16f), buildingsRoot, CreateOrGetMaterial("Assets/Settings/StuccoOchre.mat", new Color(0.78f, 0.65f, 0.50f), 0.1f));
            CreateUrbanBuilding("UrbanBuilding_West_South", new Vector3(-18.3f, 4.0f, -16f), new Vector3(12f, 8f, 16f), buildingsRoot, CreateOrGetMaterial("Assets/Settings/StuccoSand.mat", new Color(0.80f, 0.75f, 0.68f), 0.1f));
            CreateUrbanBuilding("UrbanBuilding_West_North", new Vector3(-18.3f, 4.5f, 18.5f), new Vector3(12f, 9f, 15f), buildingsRoot, CreateOrGetMaterial("Assets/Settings/StuccoTerracotta.mat", new Color(0.75f, 0.45f, 0.35f), 0.1f));

            // 7. Directional Highway Gantry Spanning Roadway (Z = +28m)
            GameObject gantryRoot = new GameObject("Overhead_Highway_Gantry");
            gantryRoot.transform.SetParent(envRoot.transform);

            // Left Post
            CreateCylinderPost("Gantry_Post_West", new Vector3(-7.5f, 3.5f, 28f), 7f, 0.35f, gantryRoot, bridgeSteelMat);
            // Right Post
            CreateCylinderPost("Gantry_Post_East", new Vector3(7.5f, 3.5f, 28f), 7f, 0.35f, gantryRoot, bridgeSteelMat);

            // Overhead Cross Beam
            GameObject gantryBeam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gantryBeam.name = "Gantry_Beam";
            gantryBeam.transform.SetParent(gantryRoot.transform);
            gantryBeam.transform.position = new Vector3(0f, 6.8f, 28f);
            gantryBeam.transform.localScale = new Vector3(15.5f, 0.6f, 0.6f);
            SetMaterial(gantryBeam, bridgeSteelMat);

            // Directional Signs on Gantry
            GameObject signPDN = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signPDN.name = "Sign_PasoDelNorte";
            signPDN.transform.SetParent(gantryRoot.transform);
            signPDN.transform.position = new Vector3(-3.5f, 5.5f, 28.1f);
            signPDN.transform.localScale = new Vector3(6.5f, 1.8f, 0.1f);
            SetMaterial(signPDN, greenSignMat);

            GameObject signPed = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signPed.name = "Sign_PedestrianElPaso";
            signPed.transform.SetParent(gantryRoot.transform);
            signPed.transform.position = new Vector3(3.5f, 5.5f, 28.1f);
            signPed.transform.localScale = new Vector3(6.5f, 1.8f, 0.1f);
            SetMaterial(signPed, greenSignMat);

            // 8. Northern Border Approach: Plaza de Cobro C-51 & Bridge Canopy (Z = +45m)
            GameObject borderRoot = new GameObject("Border_Crossing_Plaza");

            // Canopy Structure
            GameObject canopy = new GameObject("Bridge_Canopy_PlazaDeCobro");
            canopy.transform.SetParent(borderRoot.transform);

            // 4 Main Canopy Columns
            CreateCylinderPost("Canopy_Col_1", new Vector3(-7.5f, 3.5f, 42f), 7f, 0.45f, canopy, bridgeTurquoiseMat);
            CreateCylinderPost("Canopy_Col_2", new Vector3(7.5f, 3.5f, 42f), 7f, 0.45f, canopy, bridgeTurquoiseMat);
            CreateCylinderPost("Canopy_Col_3", new Vector3(-7.5f, 3.5f, 49f), 7f, 0.45f, canopy, bridgeTurquoiseMat);
            CreateCylinderPost("Canopy_Col_4", new Vector3(7.5f, 3.5f, 49f), 7f, 0.45f, canopy, bridgeTurquoiseMat);

            // Canopy Roof Slab
            GameObject canopyRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopyRoof.name = "Canopy_RoofSlab";
            canopyRoof.transform.SetParent(canopy.transform);
            canopyRoof.transform.position = new Vector3(0f, 7.2f, 45.5f);
            canopyRoof.transform.localScale = new Vector3(20f, 0.8f, 12f);
            SetMaterial(canopyRoof, CreateOrGetMaterial("Assets/Settings/CanopyRoof.mat", new Color(0.95f, 0.95f, 0.95f), 0.2f));

            // Canopy Front Fascia
            GameObject canopyFascia = GameObject.CreatePrimitive(PrimitiveType.Cube);
            canopyFascia.name = "Canopy_FrontFascia";
            canopyFascia.transform.SetParent(canopy.transform);
            canopyFascia.transform.position = new Vector3(0f, 7.2f, 39.4f);
            canopyFascia.transform.localScale = new Vector3(19.8f, 1.1f, 0.2f);
            SetMaterial(canopyFascia, bridgeTurquoiseMat);

            // Toll Collection Booths
            CreateTollBooth("TollBooth_Lane1", new Vector3(-3.5f, 1.5f, 45f), canopy.transform, bridgeSteelMat, glassMat);
            CreateTollBooth("TollBooth_Lane2", new Vector3(0f, 1.5f, 45f), canopy.transform, bridgeSteelMat, glassMat);

            // Pedestrian Queue Stanchions and Railings (East side approach)
            GameObject queueArea = new GameObject("Pedestrian_Queue_Area");
            queueArea.transform.SetParent(borderRoot.transform);

            for (int z = 36; z <= 48; z += 3)
            {
                CreateQueueRailing($"Queue_Rail_L_{z}", new Vector3(8.5f, 0.5f, z), queueArea.transform, bridgeSteelMat);
                CreateQueueRailing($"Queue_Rail_R_{z}", new Vector3(10.5f, 0.5f, z), queueArea.transform, bridgeSteelMat);
            }

            // Pedestrian Queue Canopy Cover
            GameObject pedQueueCanopy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pedQueueCanopy.name = "Pedestrian_Queue_Canopy";
            pedQueueCanopy.transform.SetParent(queueArea.transform);
            pedQueueCanopy.transform.position = new Vector3(9.5f, 3.2f, 42f);
            pedQueueCanopy.transform.localScale = new Vector3(3.2f, 0.1f, 14f);
            SetMaterial(pedQueueCanopy, bridgeTurquoiseMat);

            // Synthetic Community Witnesses Waiting in Pedestrian Queue:
            // Witness 1: Mateo Navarro (University Student, Z = 37.5m)
            CreateQueueWitness(
                "Witness_MateoNavarro",
                "mateo-student",
                "Mateo Navarro",
                "University Student (Commuter)",
                "🎒",
                new Vector3(9.5f, 0.1f, 37.5f),
                queueArea.transform,
                witnessMateoMat,
                highlightGoldMat,
                new string[] { "How long have you been in line?", "Where are you heading today?", "Is the queue moving steadily?" },
                new string[] { "¿Cuánto tiempo llevas en la fila?", "¿Hacia dónde te diriges hoy?", "¿La fila avanza rápido?" }
            );

            // Witness 2: Sofia Alarcón (Retail Worker, Z = 41.5m)
            CreateQueueWitness(
                "Witness_SofiaAlarcon",
                "sofia-cross-border-retail",
                "Sofia Alarcón",
                "Cross-Border Retail Worker",
                "💼",
                new Vector3(9.5f, 0.1f, 41.5f),
                queueArea.transform,
                witnessSofiaMat,
                highlightGoldMat,
                new string[] { "How does the pace feel today?", "Do you cross every day for work?", "Is BOTA better than Paso del Norte?" },
                new string[] { "¿Cómo se siente el ritmo de la fila hoy?", "¿Cruzas diario a trabajar?", "¿Prefieres Córdova o Paso del Norte?" }
            );

            // Witness 3: Don Héctor Méndez (Artisan, Z = 45.5m)
            CreateQueueWitness(
                "Witness_DonHector",
                "hector-artisan",
                "Don Héctor Méndez",
                "Local Artisan & Regular Traveler",
                "🪵",
                new Vector3(9.5f, 0.1f, 45.5f),
                queueArea.transform,
                witnessHectorMat,
                highlightGoldMat,
                new string[] { "How is the bridge today, Don Héctor?", "What crafts are you taking across?", "How has the wait changed recently?" },
                new string[] { "¿Cómo ve la fila del puente hoy, Don Héctor?", "¿Qué artesanías lleva a vender?", "¿Qué tal está el paso a esta hora?" }
            );

            // 9. Border Information Kiosk (Elena Ramos Concierge Station)
            GameObject kioskGo = new GameObject("Border_Information_Kiosk");
            kioskGo.transform.SetParent(borderRoot.transform);
            kioskGo.transform.position = new Vector3(4.5f, 0.1f, 40f);

            // Kiosk Desk / Booth
            GameObject kioskDesk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kioskDesk.name = "Kiosk_Desk";
            kioskDesk.transform.SetParent(kioskGo.transform);
            kioskDesk.transform.localPosition = new Vector3(0f, 0.6f, 0f);
            kioskDesk.transform.localScale = new Vector3(2.5f, 1.2f, 1.5f);
            SetMaterial(kioskDesk, CreateOrGetMaterial("Assets/Settings/KioskBlue.mat", new Color(0.12f, 0.35f, 0.75f), 0.3f));

            // Kiosk Roof Canopy
            GameObject kioskRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            kioskRoof.name = "Kiosk_Roof";
            kioskRoof.transform.SetParent(kioskGo.transform);
            kioskRoof.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            kioskRoof.transform.localScale = new Vector3(3.0f, 0.15f, 2.2f);
            SetMaterial(kioskRoof, bridgeTurquoiseMat);

            // Kiosk Posts
            CreateCylinderPost("Kiosk_Pillar_1", new Vector3(3.3f, 1.35f, 39.2f), 2.7f, 0.1f, kioskGo, bridgeSteelMat);
            CreateCylinderPost("Kiosk_Pillar_2", new Vector3(5.7f, 1.35f, 39.2f), 2.7f, 0.1f, kioskGo, bridgeSteelMat);

            // Kiosk Interaction Point (Elena Ramos)
            var kioskInteract = kioskGo.AddComponent<InteractionPoint>();
            kioskInteract.category = InteractionCategory.BorderConcierge;
            kioskInteract.characterId = "elena-border-concierge";
            kioskInteract.characterName = "Elena Ramos";
            kioskInteract.characterRole = "Virtual Border Concierge";
            kioskInteract.avatar = "🛂";
            kioskInteract.landmarkId = "infrastructure:puente-paso-del-norte";
            kioskInteract.title = "Paso del Norte Border Information Point";
            kioskInteract.subtitle = "Border Concierge & Traveler Guidance • Elena Ramos";
            kioskInteract.address = "Puente Internacional Paso del Norte / Santa Fe (Port 240202)";
            kioskInteract.description = "Official international pedestrian crossing linking downtown Ciudad Juárez with South El Paso Street in El Paso, Texas across the Rio Grande.";
            kioskInteract.backendFeatureNotice = "Connected to Increment 3: Grounded Border Concierge (Elena Ramos) & Increment 1: Border Intelligence.\nRetrieves normalized CBP pedestrian wait times (General Lane), data freshness ratings, bridge comparisons (PDN vs BOTA), and official Chihuahua Bridge Authority camera portal links.";
            kioskInteract.interactionRadius = 4.2f;
            kioskInteract.promptText = "[E] Consult Border Concierge · Elena Ramos";
            kioskInteract.suggestedPromptsEn = new string[]
            {
                "What is the pedestrian wait time right now?",
                "Is Paso del Norte faster than BOTA?",
                "Where can I view live bridge cameras?"
            };
            kioskInteract.suggestedPromptsEs = new string[]
            {
                "¿Cuál es el tiempo de espera peatonal ahora?",
                "¿Paso del Norte está más rápido que Córdova-Américas?",
                "¿Dónde puedo ver las cámaras en vivo del puente?"
            };

            // Visual Highlight Indicator
            GameObject kioskHighlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            kioskHighlight.name = "Highlight_Indicator";
            kioskHighlight.transform.SetParent(kioskGo.transform);
            kioskHighlight.transform.localPosition = new Vector3(0f, 0.05f, -1.0f);
            kioskHighlight.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
            SetMaterial(kioskHighlight, bridgeTurquoiseMat);
            UnityEngine.Object.DestroyImmediate(kioskHighlight.GetComponent<Collider>());

            // 10. Bridge Structure Extension toward Rio Grande (Z > +52m)
            GameObject bridgeDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bridgeDeck.name = "Bridge_Span_Deck";
            bridgeDeck.transform.SetParent(borderRoot.transform);
            bridgeDeck.transform.position = new Vector3(0f, 0.2f, 60f);
            bridgeDeck.transform.localScale = new Vector3(16f, 0.4f, 18f);
            SetMaterial(bridgeDeck, concreteMat);

            // Bridge Perimeter Barriers
            CreateCurb("Bridge_Barrier_West", new Vector3(-8.1f, 1.0f, 60f), new Vector3(0.4f, 1.2f, 18f), borderRoot, bridgeSteelMat);
            CreateCurb("Bridge_Barrier_East", new Vector3(8.1f, 1.0f, 60f), new Vector3(0.4f, 1.2f, 18f), borderRoot, bridgeSteelMat);

            // 11. Streetlights along curbs
            GameObject lightsRoot = new GameObject("Street_Lighting");
            CreateStreetlight("Streetlight_W1", new Vector3(-6.8f, 0f, -12f), lightsRoot, bridgeSteelMat);
            CreateStreetlight("Streetlight_E1", new Vector3(6.8f, 0f, -12f), lightsRoot, bridgeSteelMat);
            CreateStreetlight("Streetlight_W2", new Vector3(-6.8f, 0f, 10f), lightsRoot, bridgeSteelMat);
            CreateStreetlight("Streetlight_E2", new Vector3(6.8f, 0f, 10f), lightsRoot, bridgeSteelMat);

            // 12. Sun Directional Light & Atmosphere
            GameObject sunGo = new GameObject("Sun_DirectionalLight");
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.3f;
            sun.color = new Color(1.0f, 0.96f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sunGo.transform.position = new Vector3(0f, 15f, 0f);
            sunGo.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            // 13. First-Person Player Setup
            GameObject playerGo = new GameObject("FirstPersonPlayer");
            playerGo.transform.position = new Vector3(0f, 1.0f, -15f); // On Avenida Juárez facing North towards bridge
            playerGo.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            var charController = playerGo.AddComponent<CharacterController>();
            charController.height = 1.8f;
            charController.radius = 0.4f;
            charController.center = new Vector3(0f, 0.9f, 0f);

            // Player Camera
            GameObject camGo = new GameObject("PlayerCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(playerGo.transform);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            camGo.transform.localRotation = Quaternion.identity;

            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();

            // ApiClient component attached for backend connection status
            var client = playerGo.AddComponent<ApiClient>();
            client.BaseUrl = "http://127.0.0.1:3000";

            var playerComp = playerGo.AddComponent<FirstPersonPlayer>();

            // Save Scene
            EditorSceneManager.SaveScene(scene, CorridorScenePath);
            Debug.Log($"[CorridorSceneSetup] BorderCorridorScene successfully saved to {CorridorScenePath}");

            // Configure Build Settings: CorridorScene as 0, HandshakeScene as 1
            var buildScenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(CorridorScenePath, true),
                new EditorBuildSettingsScene(HandshakeScenePath, true)
            };
            EditorBuildSettings.scenes = buildScenes;
            Debug.Log("[CorridorSceneSetup] EditorBuildSettings configured: [0] BorderCorridorScene, [1] HandshakeScene");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        #region Helper Creators

        private static GameObject CreateQueueWitness(
            string name,
            string charId,
            string charName,
            string charRole,
            string avatar,
            Vector3 position,
            Transform parent,
            Material standeeMat,
            Material highlightMat,
            string[] promptsEn,
            string[] promptsEs)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;

            // 3D Standee Body (Capsule representing individual standing in line)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Standee_Body";
            body.transform.SetParent(go.transform);
            body.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            body.transform.localScale = new Vector3(0.5f, 0.85f, 0.5f);
            SetMaterial(body, standeeMat);

            // Standee Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Standee_Head";
            head.transform.SetParent(go.transform);
            head.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            head.transform.localScale = new Vector3(0.42f, 0.42f, 0.42f);
            SetMaterial(head, standeeMat);

            // Ground Highlight Ring
            GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            highlight.name = "Highlight_Indicator";
            highlight.transform.SetParent(go.transform);
            highlight.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            highlight.transform.localScale = new Vector3(1.3f, 0.04f, 1.3f);
            SetMaterial(highlight, highlightMat);
            UnityEngine.Object.DestroyImmediate(highlight.GetComponent<Collider>());

            // Interaction Point Component
            var interact = go.AddComponent<InteractionPoint>();
            interact.category = InteractionCategory.SyntheticWitness;
            interact.characterId = charId;
            interact.characterName = charName;
            interact.characterRole = charRole;
            interact.avatar = avatar;
            interact.landmarkId = $"witness:{charId}";
            interact.title = $"{avatar} {charName}";
            interact.subtitle = $"{charRole} • Pedestrian Queue";
            interact.address = "Puente Internacional Paso del Norte (Fila Peatonal)";
            interact.description = $"Synthetic community witness waiting in the pedestrian queue toward El Paso, Texas.";
            interact.backendFeatureNotice = "Connected to Increment 2: Synthetic Community Witnesses (POST /api/border/witnesses/chat).\nSimulated scenario grounded in personal border transit context. Refuses authoritative real-time CBP declarations.";
            interact.interactionRadius = 4.2f;
            interact.promptText = $"[E] Speak with {charName} · {avatar}";
            interact.suggestedPromptsEn = promptsEn;
            interact.suggestedPromptsEs = promptsEs;

            return go;
        }

        private static Material CreateOrGetMaterial(string assetPath, Color albedo, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                mat = new Material(shader)
                {
                    color = albedo
                };
                mat.SetFloat("_Smoothness", smoothness);
                AssetDatabase.CreateAsset(mat, assetPath);
            }
            else
            {
                mat.color = albedo;
                mat.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(mat);
            }
            return mat;
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend != null) rend.sharedMaterial = mat;
        }

        private static void CreateCurb(string name, Vector3 pos, Vector3 size, GameObject parent, Material mat)
        {
            GameObject curb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curb.name = name;
            curb.transform.SetParent(parent.transform);
            curb.transform.position = pos;
            curb.transform.localScale = size;
            SetMaterial(curb, mat);
        }

        private static void CreateDisplayWindow(string name, Vector3 pos, Vector3 size, Transform parent, Material glassMat)
        {
            GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
            win.name = name;
            win.transform.SetParent(parent);
            win.transform.position = pos;
            win.transform.localScale = size;
            SetMaterial(win, glassMat);
        }

        private static void CreateUrbanBuilding(string name, Vector3 pos, Vector3 size, GameObject parent, Material facadeMat)
        {
            GameObject bld = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bld.name = name;
            bld.transform.SetParent(parent.transform);
            bld.transform.position = pos;
            bld.transform.localScale = size;
            SetMaterial(bld, facadeMat);
        }

        private static void CreateCylinderPost(string name, Vector3 pos, float height, float radius, GameObject parent, Material mat)
        {
            GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = name;
            cyl.transform.SetParent(parent.transform);
            cyl.transform.position = pos;
            cyl.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
            SetMaterial(cyl, mat);
        }

        private static void CreateTollBooth(string name, Vector3 pos, Transform parent, Material frameMat, Material glassMat)
        {
            GameObject booth = new GameObject(name);
            booth.transform.SetParent(parent);
            booth.transform.position = pos;

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Booth_Body";
            body.transform.SetParent(booth.transform);
            body.transform.localPosition = new Vector3(0f, 0f, 0f);
            body.transform.localScale = new Vector3(1.6f, 2.6f, 2.4f);
            SetMaterial(body, frameMat);

            GameObject window = GameObject.CreatePrimitive(PrimitiveType.Cube);
            window.name = "Booth_Window";
            window.transform.SetParent(booth.transform);
            window.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            window.transform.localScale = new Vector3(1.65f, 1.0f, 1.8f);
            SetMaterial(window, glassMat);
        }

        private static void CreateQueueRailing(string name, Vector3 pos, Transform parent, Material mat)
        {
            GameObject rail = new GameObject(name);
            rail.transform.SetParent(parent);
            rail.transform.position = pos;

            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Rail_Bar";
            bar.transform.SetParent(rail.transform);
            bar.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            bar.transform.localScale = new Vector3(0.1f, 0.1f, 2.8f);
            SetMaterial(bar, mat);

            CreateCylinderPost("Post_1", pos + new Vector3(0f, 0f, -1.3f), 1.0f, 0.05f, rail, mat);
            CreateCylinderPost("Post_2", pos + new Vector3(0f, 0f, 1.3f), 1.0f, 0.05f, rail, mat);
        }

        private static void CreateStreetlight(string name, Vector3 pos, GameObject parent, Material mat)
        {
            GameObject lightGo = new GameObject(name);
            lightGo.transform.SetParent(parent.transform);
            lightGo.transform.position = pos;

            CreateCylinderPost("Pole", pos + new Vector3(0f, 2.5f, 0f), 5.0f, 0.12f, lightGo, mat);

            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = "Arm";
            arm.transform.SetParent(lightGo.transform);
            arm.transform.position = pos + new Vector3(pos.x > 0 ? -0.8f : 0.8f, 4.9f, 0f);
            arm.transform.localScale = new Vector3(1.6f, 0.12f, 0.12f);
            SetMaterial(arm, mat);

            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lamp.name = "Lamp";
            lamp.transform.SetParent(lightGo.transform);
            lamp.transform.position = pos + new Vector3(pos.x > 0 ? -1.5f : 1.5f, 4.7f, 0f);
            lamp.transform.localScale = new Vector3(0.5f, 0.2f, 0.3f);
            SetMaterial(lamp, mat);
        }

        #endregion
    }
}
