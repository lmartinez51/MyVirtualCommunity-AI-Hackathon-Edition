using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Client.World;
using MyVirtualCommunity.Character;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Reconstructs the East Sidewalk commercial block along Avenida Benito Juárez
    /// to faithfully match the real-world Centro Histórico photograph:
    /// 1. Curiosidades Azteca (North-East / Left)
    /// 2. Minerva & Comercial Uno (North-Center / Mid-Left, with projecting vertical flag sign)
    /// 3. Farmacias Benavides & Consultorio (Center, with Carlos Méndez attendant)
    /// 4. La Michoacana (South-Center / Mid-Right, with open parlor & green ice-cream counter)
    /// 5. Atención Dental / Dentistas (South-East / Far Right 2-story building)
    /// </summary>
    public static class EastSidewalkBlockBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Rebuild Authentic East Sidewalk Block & Capture Verification")]
        public static void RebuildAndVerify()
        {
            RebuildEastSidewalkBlock();
            CaptureVerificationScreenshots();
        }

        [MenuItem("MyVirtualCommunity/Rebuild Authentic East Sidewalk Block")]
        public static void RebuildEastSidewalkBlock()
        {
            Debug.Log("[EastSidewalkBlockBuilder] Starting Authentic East Sidewalk Block Reconstruction...");

            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[EastSidewalkBlockBuilder] Failed to open scene {ScenePath}");
                return;
            }

            // 1. Setup Materials
            Material whiteStuccoMat = CreateOrGetColorMaterial("Assets/Settings/East_WhiteStucco.mat", new Color(0.94f, 0.93f, 0.91f), 0.08f);
            Material weatheredStuccoMat = CreateOrGetColorMaterial("Assets/Settings/East_WeatheredStucco.mat", new Color(0.89f, 0.88f, 0.85f), 0.05f);
            Material darkZocaloMat = CreateOrGetColorMaterial("Assets/Settings/East_DarkZocalo.mat", new Color(0.24f, 0.27f, 0.29f), 0.12f);
            Material copingGreyMat = CreateOrGetColorMaterial("Assets/Settings/East_CopingGrey.mat", new Color(0.48f, 0.50f, 0.52f), 0.15f);
            Material beigeAwningMat = CreateOrGetColorMaterial("Assets/Settings/East_BeigeAwning.mat", new Color(0.83f, 0.75f, 0.62f), 0.08f);
            Material awningValanceMat = CreateOrGetColorMaterial("Assets/Settings/East_AwningValance.mat", new Color(0.76f, 0.67f, 0.54f), 0.08f);
            Material terracottaPlaqueMat = CreateOrGetColorMaterial("Assets/Settings/East_TerracottaPlaque.mat", new Color(0.49f, 0.21f, 0.13f), 0.15f);
            Material dentalPlaqueMat = CreateOrGetColorMaterial("Assets/Settings/East_DentalPlaque.mat", new Color(0.65f, 0.22f, 0.14f), 0.18f);
            Material darkGlassMat = CreateOrGetColorMaterial("Assets/Settings/East_DarkGlass.mat", new Color(0.11f, 0.14f, 0.17f), 0.82f);
            Material interiorDarkMat = CreateOrGetColorMaterial("Assets/Settings/East_InteriorDark.mat", new Color(0.08f, 0.09f, 0.11f), 0.05f);
            Material whiteFrameMat = CreateOrGetColorMaterial("Assets/Settings/East_WhiteFrame.mat", new Color(0.95f, 0.96f, 0.97f), 0.35f);
            Material aluminumMat = CreateOrGetColorMaterial("Assets/Settings/East_Aluminum.mat", new Color(0.62f, 0.65f, 0.68f), 0.65f);
            Material navyPanelMat = CreateOrGetColorMaterial("Assets/Settings/East_NavyPanel.mat", new Color(0.14f, 0.18f, 0.22f), 0.15f);
            Material michoacanaGreenMat = CreateOrGetColorMaterial("Assets/Settings/East_MichoacanaGreen.mat", new Color(0.06f, 0.48f, 0.22f), 0.35f);
            Material michoacanaWallMat = CreateOrGetColorMaterial("Assets/Settings/East_MichoacanaWall.mat", new Color(0.78f, 0.84f, 0.76f), 0.10f);
            Material brickTerracottaMat = CreateOrGetColorMaterial("Assets/Settings/East_BrickTerracotta.mat", new Color(0.78f, 0.44f, 0.30f), 0.08f);
            Material brickDarkMat = CreateOrGetColorMaterial("Assets/Settings/East_BrickDark.mat", new Color(0.55f, 0.27f, 0.16f), 0.08f);
            Material flagGreenMat = CreateOrGetColorMaterial("Assets/Settings/East_FlagGreen.mat", new Color(0.08f, 0.42f, 0.24f), 0.3f);
            Material flagRedMat = CreateOrGetColorMaterial("Assets/Settings/East_FlagRed.mat", new Color(0.76f, 0.16f, 0.14f), 0.3f);
            Material orangeAccentMat = CreateOrGetColorMaterial("Assets/Settings/East_OrangeAccent.mat", new Color(0.82f, 0.42f, 0.18f), 0.25f);
            Material warmBeigeStuccoMat = CreateOrGetColorMaterial("Assets/Settings/East_DentalWarmBeige.mat", new Color(0.91f, 0.87f, 0.80f), 0.08f);
            Material blueGlassDoorMat = CreateOrGetColorMaterial("Assets/Settings/East_DentalBlueGlass.mat", new Color(0.24f, 0.42f, 0.56f), 0.65f);
            Material shutterMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/West_MetalShutter.mat")
                ?? CreateOrGetColorMaterial("Assets/Settings/East_MetalShutterFallback.mat", new Color(0.76f, 0.77f, 0.78f), 0.30f);

            // Sign Texture Materials
            Material signCuriosMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_Curiosidades.mat", "Assets/Textures/Environment/T_East_Sign_Curiosidades.png");
            Material signMinervaMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_Minerva.mat", "Assets/Textures/Environment/T_East_Sign_Minerva.png");
            Material signComercialUnoMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_ComercialUno.mat", "Assets/Textures/Environment/T_East_Sign_ComercialUno.png");
            Material signBenavidesMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_Benavides.mat", "Assets/Textures/Environment/T_East_Sign_Benavides.png");
            Material signConsultorioMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_Consultorio.mat", "Assets/Textures/Environment/T_East_Sign_Consultorio.png");
            Material signMichoacanaMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_LaMichoacana.mat", "Assets/Textures/Environment/T_East_Sign_LaMichoacana.png");
            Material signDentistasMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_Dentistas.mat", "Assets/Textures/Environment/T_East_Sign_Dentistas.png");
            Material signCambiosPuenteMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_CambiosPuente.mat", "Assets/Textures/Environment/T_East_Sign_CambiosPuente.png");
            Material signAppleDentalMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_AppleDental.mat", "Assets/Textures/Environment/T_East_Sign_AppleDental.png");
            Material ledExchangeMat = CreateOrGetTextureMaterial("Assets/Settings/East_LED_ExchangeRate.mat", "Assets/Textures/Environment/T_East_LED_ExchangeRate.png", emissive: true);
            Material postersBenavidesMat = CreateOrGetTextureMaterial("Assets/Settings/East_Benavides_Posters.mat", "Assets/Textures/Environment/T_East_Benavides_Posters.png");

            // 2. Locate Buildings_Corridor & Storefront_FarmaciasBenavides
            GameObject buildingsRoot = GameObject.Find("Buildings_Corridor");
            if (buildingsRoot == null)
            {
                buildingsRoot = new GameObject("Buildings_Corridor");
            }

            // Remove old generic flanking East buildings so our continuous block takes their place cleanly
            GameObject oldEastNorth = GameObject.Find("UrbanBuilding_East_North");
            if (oldEastNorth != null) UnityEngine.Object.DestroyImmediate(oldEastNorth);

            GameObject oldEastSouth = GameObject.Find("UrbanBuilding_East_South");
            if (oldEastSouth != null) UnityEngine.Object.DestroyImmediate(oldEastSouth);

            GameObject benavidesRoot = GameObject.Find("Storefront_FarmaciasBenavides");
            if (benavidesRoot == null)
            {
                benavidesRoot = new GameObject("Storefront_FarmaciasBenavides");
                benavidesRoot.transform.SetParent(buildingsRoot.transform);
                benavidesRoot.transform.position = Vector3.zero;
            }

            // Preserve InteractionPoint_FarmaciasBenavides and CarlosMendez_Attendant, destroy old visual children
            for (int i = benavidesRoot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = benavidesRoot.transform.GetChild(i);
                if (child.name != "InteractionPoint_FarmaciasBenavides" && child.name != "CarlosMendez_Attendant")
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            // Ensure Carlos Méndez is positioned cleanly on the sidewalk in front of the pharmacy entrance
            Transform carlosTr = benavidesRoot.transform.Find("CarlosMendez_Attendant");
            if (carlosTr != null)
            {
                carlosTr.localPosition = new Vector3(10.5f, 0.20f, 0.0f);
                carlosTr.localRotation = Quaternion.Euler(0f, -90f, 0f);
            }
            Transform interactTr = benavidesRoot.transform.Find("InteractionPoint_FarmaciasBenavides");
            if (interactTr != null)
            {
                interactTr.localPosition = new Vector3(10.5f, 1.0f, 0.0f);
                Transform hl = interactTr.Find("Highlight_Indicator");
                if (hl != null)
                {
                    UnityEngine.Object.DestroyImmediate(hl.gameObject);
                }
            }

            // =========================================================================
            // 3. BUILD THE 5 AUTHENTIC STOREFRONT MODULES (North +Z to South -Z)
            // Front wall plane sits at X = 12.0m (flush with back edge of Sidewalk_East)
            // =========================================================================

            // Required collider object for CorridorVerificationTest: "Benavides_MainBuilding"
            // Covers the central pharmacy building mass (Z = -3.8m to +5.2m, center Z = +0.7m, H = 5.4m)
            GameObject benMain = CreateBox("Benavides_MainBuilding", benavidesRoot.transform,
                new Vector3(18.0f, 2.7f, 0.7f), new Vector3(12.0f, 5.4f, 9.0f), whiteStuccoMat, keepCollider: true);

            // Container for all East Block architectural details
            GameObject blockArch = new GameObject("EastBlock_HistoricArchitecture");
            blockArch.transform.SetParent(benavidesRoot.transform);
            blockArch.transform.localPosition = Vector3.zero;

            // -------------------------------------------------------------------------
            // MODULE 1 & 2: TALL NORTH-EAST BLOCK ("Curiosidades Azteca", "Minerva", "Comercial Uno")
            // Spans Z = +5.2m to +22.0m (Length = 16.8m, Center Z = +13.6m, Height = 6.8m)
            // -------------------------------------------------------------------------
            CreateBox("NorthBlock_MainMass", blockArch.transform,
                new Vector3(18.0f, 3.4f, 13.6f), new Vector3(12.0f, 6.8f, 16.8f), whiteStuccoMat, keepCollider: true);

            // Upper Weathered Plaster Texture Band (characteristic rough plaster rectangle in upper half of Minerva wall)
            CreateBox("NorthBlock_UpperPlasterBand", blockArch.transform,
                new Vector3(11.98f, 5.75f, 13.6f), new Vector3(0.05f, 1.75f, 15.8f), weatheredStuccoMat);

            // Top Roof Coping Cornice & Ledge (Y = 6.85m)
            CreateBox("NorthBlock_RoofCornice", blockArch.transform,
                new Vector3(11.92f, 6.82f, 13.6f), new Vector3(0.22f, 0.18f, 17.0f), copingGreyMat);
            CreateBox("NorthBlock_RoofSubLip", blockArch.transform,
                new Vector3(11.95f, 6.68f, 13.6f), new Vector3(0.14f, 0.08f, 16.9f), whiteStuccoMat);

            // Vertical Corner Pilaster at North end (Z = +21.85m) and South edge of North Block (Z = +5.35m)
            CreateBox("NorthBlock_Pilaster_North", blockArch.transform,
                new Vector3(11.95f, 3.4f, 21.85f), new Vector3(0.14f, 6.8f, 0.35f), whiteStuccoMat);
            CreateBox("NorthBlock_Pilaster_South", blockArch.transform,
                new Vector3(11.94f, 3.4f, 5.35f), new Vector3(0.16f, 6.8f, 0.38f), whiteStuccoMat);

            // Dark Slate-Grey Zócalo (Baseboard) along North Block (split around Minerva entrance Z = +8.2 to +11.8)
            CreateBox("NorthBlock_Zocalo_Left", blockArch.transform,
                new Vector3(11.96f, 0.62f, 16.9f), new Vector3(0.10f, 0.85f, 10.2f), darkZocaloMat);
            CreateBox("NorthBlock_Zocalo_Right", blockArch.transform,
                new Vector3(11.96f, 0.62f, 6.5f), new Vector3(0.10f, 0.85f, 2.6f), darkZocaloMat);

            // --- Sub-Unit 1A: Curiosidades Azteca (Z = +19.0m) ---
            // Small window below awning
            CreateRecessedWindow("Curiosidades_Window", blockArch.transform,
                centerZ: 18.7f, centerY: 1.85f, widthZ: 1.45f, heightY: 1.15f, darkGlassMat, darkZocaloMat);
            // Beige Canvas Wedge Awning
            CreateWedgeAwning("Curiosidades_Awning", blockArch.transform,
                centerZ: 19.1f, topY: 3.18f, widthZ: 2.75f, projectionX: 1.25f, dropY: 0.72f, beigeAwningMat, awningValanceMat);
            // Terracotta Sign Plaque
            CreateTerracottaSign("Sign_Curiosidades", blockArch.transform,
                centerZ: 19.2f, centerY: 4.02f, widthZ: 1.85f, heightY: 0.72f, terracottaPlaqueMat, signCuriosMat);

            // --- Sub-Unit 1B: Minerva Commercial Passage (Z = +10.0m) ---
            // Recessed open entrance portal (Z = +8.2m to +11.8m, Width = 3.6m)
            CreateBox("Minerva_PortalRecess", blockArch.transform,
                new Vector3(12.35f, 1.48f, 10.0f), new Vector3(0.8f, 2.56f, 3.5f), interiorDarkMat);
            // Portal side jambs & lintel
            CreateBox("Minerva_Jamb_Left", blockArch.transform,
                new Vector3(11.95f, 1.48f, 11.75f), new Vector3(0.16f, 2.56f, 0.22f), darkZocaloMat);
            CreateBox("Minerva_Jamb_Right", blockArch.transform,
                new Vector3(11.95f, 1.48f, 8.15f), new Vector3(0.16f, 2.56f, 0.28f), darkZocaloMat);
            // Subtle sloped concrete entrance apron on sidewalk
            GameObject minervaRamp = CreateBox("Minerva_EntranceRamp", blockArch.transform,
                new Vector3(11.45f, 0.21f, 10.0f), new Vector3(1.1f, 0.04f, 3.4f), copingGreyMat);
            minervaRamp.transform.localRotation = Quaternion.Euler(0f, 0f, 1.5f);
            // Interior glass door partitions inside Minerva portal
            CreateBox("Minerva_InnerFrame_L", blockArch.transform,
                new Vector3(12.15f, 1.45f, 11.1f), new Vector3(0.06f, 2.4f, 0.08f), aluminumMat);
            CreateBox("Minerva_InnerFrame_R", blockArch.transform,
                new Vector3(12.15f, 1.45f, 8.9f), new Vector3(0.06f, 2.4f, 0.08f), aluminumMat);
            // Wide Beige Canvas Wedge Awning for Minerva
            CreateWedgeAwning("Minerva_Awning", blockArch.transform,
                centerZ: 10.0f, topY: 3.18f, widthZ: 4.5f, projectionX: 1.35f, dropY: 0.76f, beigeAwningMat, awningValanceMat);
            // Prominent Terracotta Sign "Minerva"
            CreateTerracottaSign("Sign_Minerva", blockArch.transform,
                centerZ: 10.0f, centerY: 4.05f, widthZ: 3.55f, heightY: 0.82f, terracottaPlaqueMat, signMinervaMat);

            // --- Projecting Vertical Flag Sign (Bandera) between Minerva and Comercial Uno (Z = +7.85m) ---
            CreateBox("FlagSign_Bracket", blockArch.transform,
                new Vector3(11.55f, 3.45f, 7.85f), new Vector3(0.90f, 1.15f, 0.10f), interiorDarkMat);
            CreateBox("FlagSign_TopGreen", blockArch.transform,
                new Vector3(11.48f, 3.72f, 7.85f), new Vector3(0.72f, 0.48f, 0.14f), flagGreenMat);
            CreateBox("FlagSign_BottomRed", blockArch.transform,
                new Vector3(11.48f, 3.18f, 7.85f), new Vector3(0.72f, 0.48f, 0.14f), flagRedMat);
            // Vertical dark post below flag sign
            CreateBox("FlagSign_VerticalPillar", blockArch.transform,
                new Vector3(11.88f, 1.55f, 7.85f), new Vector3(0.28f, 2.7f, 0.28f), darkZocaloMat);

            // --- Sub-Unit 1C: Comercial Uno (Z = +6.45m) ---
            CreateRecessedWindow("ComercialUno_Window", blockArch.transform,
                centerZ: 6.45f, centerY: 1.82f, widthZ: 1.25f, heightY: 0.95f, darkGlassMat, darkZocaloMat);
            CreateWedgeAwning("ComercialUno_Awning", blockArch.transform,
                centerZ: 6.45f, topY: 3.18f, widthZ: 1.85f, projectionX: 1.20f, dropY: 0.72f, beigeAwningMat, awningValanceMat);
            CreateTerracottaSign("Sign_ComercialUno", blockArch.transform,
                centerZ: 6.45f, centerY: 4.05f, widthZ: 2.05f, heightY: 0.80f, terracottaPlaqueMat, signComercialUnoMat);

            // -------------------------------------------------------------------------
            // MODULE 3: CENTER BLOCK — "farmacias benavides" & "Consultorio"
            // Spans Z = -3.8m to +5.2m (Length = 9.0m, Center Z = +0.7m, Height = 5.4m)
            // -------------------------------------------------------------------------
            // Upper Cornice Coping & Moldings at Y = 5.4m
            CreateBox("Benavides_RoofCornice", blockArch.transform,
                new Vector3(11.90f, 5.42f, 0.7f), new Vector3(0.26f, 0.18f, 9.1f), copingGreyMat);
            CreateBox("Benavides_RoofSubLip", blockArch.transform,
                new Vector3(11.94f, 5.26f, 0.7f), new Vector3(0.16f, 0.10f, 9.0f), whiteStuccoMat);

            // Vertical Pilaster dividing Benavides and La Michoacana (Z = -3.8m)
            CreateBox("Pilaster_Benavides_Michoacana", blockArch.transform,
                new Vector3(11.93f, 2.7f, -3.8f), new Vector3(0.18f, 5.4f, 0.42f), whiteStuccoMat);
            // Vertical grey conduit / downspout pipe along pilaster
            CreateBox("Downspout_Pipe", blockArch.transform,
                new Vector3(11.82f, 2.7f, -3.65f), new Vector3(0.05f, 5.3f, 0.05f), copingGreyMat);

            // Storefront Window & Door Assembly (Left & Center of Benavides: Z = -0.7m to +4.8m)
            // Dark glass backing
            CreateBox("Benavides_StorefrontGlass", blockArch.transform,
                new Vector3(11.97f, 1.50f, 2.05f), new Vector3(0.06f, 2.40f, 5.4f), darkGlassMat);
            // White lower sill under left display window (Z = +1.2m to +4.8m)
            CreateBox("Benavides_WindowSill_Left", blockArch.transform,
                new Vector3(11.95f, 0.44f, 3.0f), new Vector3(0.12f, 0.48f, 3.6f), whiteFrameMat);
            // White aluminum door frame (Z = -0.6m to +1.1m)
            CreateBox("Benavides_DoorFrame_L", blockArch.transform,
                new Vector3(11.93f, 1.45f, 1.15f), new Vector3(0.12f, 2.5f, 0.10f), whiteFrameMat);
            CreateBox("Benavides_DoorFrame_R", blockArch.transform,
                new Vector3(11.93f, 1.45f, -0.65f), new Vector3(0.12f, 2.5f, 0.10f), whiteFrameMat);
            CreateBox("Benavides_DoorFrame_Mid", blockArch.transform,
                new Vector3(11.93f, 1.45f, 0.25f), new Vector3(0.10f, 2.5f, 0.07f), whiteFrameMat);
            CreateBox("Benavides_DoorTransom", blockArch.transform,
                new Vector3(11.93f, 2.25f, 2.05f), new Vector3(0.12f, 0.08f, 5.4f), whiteFrameMat);
            CreateBox("Benavides_DoorKickplate", blockArch.transform,
                new Vector3(11.94f, 0.36f, 0.25f), new Vector3(0.11f, 0.32f, 1.7f), whiteFrameMat);
            // Window mullions on left window
            CreateBox("Benavides_Mullion_1", blockArch.transform,
                new Vector3(11.94f, 1.55f, 4.7f), new Vector3(0.10f, 2.3f, 0.08f), whiteFrameMat);
            CreateBox("Benavides_Mullion_2", blockArch.transform,
                new Vector3(11.94f, 1.55f, 2.95f), new Vector3(0.10f, 2.3f, 0.07f), whiteFrameMat);

            // Right Navy-Charcoal Panel with Medical/Consultorio Posters (Z = -3.6m to -0.65m)
            CreateBox("Benavides_RightDarkWall", blockArch.transform,
                new Vector3(11.96f, 1.45f, -2.12f), new Vector3(0.10f, 2.50f, 2.90f), navyPanelMat);
            CreateBox("Benavides_RightZocalo", blockArch.transform,
                new Vector3(11.94f, 0.52f, -2.12f), new Vector3(0.12f, 0.64f, 2.90f), darkZocaloMat);
            // Posters Quad on right panel
            GameObject postersQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            postersQuad.name = "Benavides_MedicalPosters";
            postersQuad.transform.SetParent(blockArch.transform);
            postersQuad.transform.localPosition = new Vector3(11.89f, 1.65f, -2.10f);
            postersQuad.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // Faces West (-X)
            postersQuad.transform.localScale = new Vector3(2.35f, 1.55f, 1.0f);
            SetMaterial(postersQuad, postersBenavidesMat);
            UnityEngine.Object.DestroyImmediate(postersQuad.GetComponent<Collider>());

            // Wide Beige Canvas Wedge Awning ("Benavides_Awning")
            CreateWedgeAwning("Benavides_Awning", benavidesRoot.transform,
                centerZ: 0.7f, topY: 3.18f, widthZ: 8.3f, projectionX: 1.40f, dropY: 0.78f, beigeAwningMat, awningValanceMat);

            // Two Terracotta Signs above Awning: "farmacias benavides" & "Consultorio"
            CreateTerracottaSign("Benavides_Signboard", benavidesRoot.transform,
                centerZ: 1.85f, centerY: 4.05f, widthZ: 4.45f, heightY: 0.84f, terracottaPlaqueMat, signBenavidesMat);
            CreateTerracottaSign("Sign_Consultorio", blockArch.transform,
                centerZ: -1.95f, centerY: 4.05f, widthZ: 2.15f, heightY: 0.82f, terracottaPlaqueMat, signConsultorioMat);

            // -------------------------------------------------------------------------
            // MODULE 4: CENTER-RIGHT BLOCK — "La Michoacana" (authentic compact width)
            // Spans Z = -9.20m to -3.80m (Length = 5.40m, Center Z = -6.50m, Height = 5.4m)
            // -------------------------------------------------------------------------
            CreateBox("Michoacana_MainMass", blockArch.transform,
                new Vector3(18.5f, 2.7f, -6.50f), new Vector3(11.0f, 5.4f, 5.40f), whiteStuccoMat, keepCollider: true);
            // Upper Wall Above Parlor Opening (Y = 2.75m to 5.4m, flush at X = 12.0m)
            CreateBox("Michoacana_UpperFacadeWall", blockArch.transform,
                new Vector3(12.4f, 4.05f, -6.50f), new Vector3(0.8f, 2.7f, 5.40f), whiteStuccoMat);
            // Upper Cornice Coping Ledge (Y = 5.42m)
            CreateBox("Michoacana_RoofCornice", blockArch.transform,
                new Vector3(11.90f, 5.42f, -6.50f), new Vector3(0.26f, 0.18f, 5.40f), copingGreyMat);
            CreateBox("Michoacana_RoofSubLip", blockArch.transform,
                new Vector3(11.94f, 5.26f, -6.50f), new Vector3(0.16f, 0.10f, 5.40f), whiteStuccoMat);

            // Open Parlor Interior Recess (Z = -8.70m to -4.30m)
            CreateBox("Michoacana_InteriorBackWall", blockArch.transform,
                new Vector3(13.6f, 1.45f, -6.50f), new Vector3(0.2f, 2.5f, 4.40f), michoacanaWallMat);
            CreateBox("Michoacana_InteriorSideWall_N", blockArch.transform,
                new Vector3(12.8f, 1.45f, -4.18f), new Vector3(1.6f, 2.5f, 0.28f), whiteStuccoMat);
            CreateBox("Michoacana_InteriorSideWall_S", blockArch.transform,
                new Vector3(12.8f, 1.45f, -8.82f), new Vector3(1.6f, 2.5f, 0.28f), whiteStuccoMat);

            // Green Ice Cream Freezer Counter ("Vitrina de Paletas y Helados")
            CreateBox("Michoacana_GreenCounter", blockArch.transform,
                new Vector3(12.45f, 0.65f, -6.50f), new Vector3(0.85f, 0.90f, 2.95f), michoacanaGreenMat);
            CreateBox("Michoacana_CounterGlassTop", blockArch.transform,
                new Vector3(12.45f, 1.22f, -6.50f), new Vector3(0.80f, 0.25f, 2.85f), darkGlassMat);
            CreateBox("Michoacana_CounterTrim", blockArch.transform,
                new Vector3(12.01f, 1.10f, -6.50f), new Vector3(0.06f, 0.08f, 3.00f), whiteFrameMat);
            // Small Orange/Terracotta Chair at north side of Michoacana opening
            CreateBox("Michoacana_ChairSeat", blockArch.transform,
                new Vector3(12.15f, 0.52f, -4.55f), new Vector3(0.45f, 0.45f, 0.45f), orangeAccentMat);
            CreateBox("Michoacana_ChairBack", blockArch.transform,
                new Vector3(12.32f, 0.88f, -4.55f), new Vector3(0.10f, 0.45f, 0.45f), orangeAccentMat);

            // Beige Canvas Wedge Awning for La Michoacana
            CreateWedgeAwning("Michoacana_Awning", blockArch.transform,
                centerZ: -6.50f, topY: 3.18f, widthZ: 4.85f, projectionX: 1.40f, dropY: 0.78f, beigeAwningMat, awningValanceMat);

            // Terracotta Sign "La Michoacana"
            CreateTerracottaSign("Sign_LaMichoacana", blockArch.transform,
                centerZ: -6.50f, centerY: 4.10f, widthZ: 3.55f, heightY: 0.78f, terracottaPlaqueMat, signMichoacanaMat);

            // Background Rooftop Terracotta & Exposed Brick Building (peeking above La Michoacana roof!)
            CreateBox("BackgroundBrick_StuccoFront", blockArch.transform,
                new Vector3(15.2f, 6.35f, -6.40f), new Vector3(2.8f, 2.1f, 3.6f), brickTerracottaMat);
            CreateBox("BackgroundBrick_RightWing", blockArch.transform,
                new Vector3(14.2f, 6.45f, -8.30f), new Vector3(2.6f, 2.3f, 1.8f), brickDarkMat);
            CreateBox("BackgroundBrick_Cornice", blockArch.transform,
                new Vector3(13.75f, 7.15f, -6.40f), new Vector3(0.2f, 0.18f, 3.7f), whiteStuccoMat);

            // -------------------------------------------------------------------------
            // MODULE 5: SOUTH-EAST 2-STORY COMMERCIAL BLOCK — "Dentistas" + "Cambios Puente" + "APPLE DENTAL CARE"
            // Spans Z = -26.80m to -9.20m (Length = 17.60m, Center Z = -18.00m, Height = 7.50m)
            // Leaves ~2.31m free sidewalk to Calle Azucenas North curb (Z = -29.11m) and passes
            // behind the East column of AvJuarez_DigitalArch_Root (Z = -23.50m) matching reality.
            // -------------------------------------------------------------------------
            CreateBox("DentalBlock_MainMass", blockArch.transform,
                new Vector3(18.0f, 3.75f, -18.00f), new Vector3(12.0f, 7.50f, 17.60f), warmBeigeStuccoMat, keepCollider: true);

            // Prominent Vertical Cream/White Column Pillar at Z = -9.25m (between La Michoacana & Dentistas)
            CreateBox("DentalBlock_LeftColumn", blockArch.transform,
                new Vector3(11.88f, 3.85f, -9.25f), new Vector3(0.28f, 7.70f, 0.88f), warmBeigeStuccoMat);
            CreateBox("DentalBlock_ColumnZocalo", blockArch.transform,
                new Vector3(11.86f, 0.58f, -9.25f), new Vector3(0.30f, 0.78f, 0.90f), copingGreyMat);
            CreateBox("UtilityMeter_1", blockArch.transform,
                new Vector3(11.70f, 1.42f, -9.08f), new Vector3(0.08f, 0.26f, 0.20f), aluminumMat);
            CreateBox("UtilityMeter_2", blockArch.transform,
                new Vector3(11.70f, 1.42f, -9.42f), new Vector3(0.08f, 0.26f, 0.20f), aluminumMat);

            // Continuous Horizontal Mid-Floor Cornice Band (Y = 3.45m) & Roof Coping (Y = 7.55m) wrapping West & South corner
            CreateBox("DentalBlock_MidStringcourse_West", blockArch.transform,
                new Vector3(11.91f, 3.46f, -18.25f), new Vector3(0.22f, 0.16f, 17.20f), terracottaPlaqueMat);
            CreateBox("DentalBlock_MidStringcourse_South", blockArch.transform,
                new Vector3(18.00f, 3.46f, -26.87f), new Vector3(12.18f, 0.16f, 0.18f), terracottaPlaqueMat);
            CreateBox("DentalBlock_RoofCoping", blockArch.transform,
                new Vector3(11.90f, 7.54f, -18.25f), new Vector3(0.26f, 0.18f, 17.25f), terracottaPlaqueMat);
            CreateBox("DentalBlock_RoofCoping_South", blockArch.transform,
                new Vector3(18.00f, 7.54f, -26.87f), new Vector3(12.22f, 0.18f, 0.22f), terracottaPlaqueMat);

            // Rooftop Details: Small Satellite Dish & Light Steel Sign Support Frame above Cambios Puente
            GameObject satDish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            satDish.name = "DentalBlock_SatelliteDish";
            satDish.transform.SetParent(blockArch.transform);
            satDish.transform.localPosition = new Vector3(12.6f, 7.95f, -12.6f);
            satDish.transform.localRotation = Quaternion.Euler(25f, 0f, 65f);
            satDish.transform.localScale = new Vector3(0.85f, 0.08f, 0.85f);
            SetMaterial(satDish, darkZocaloMat);
            UnityEngine.Object.DestroyImmediate(satDish.GetComponent<Collider>());

            CreateBox("DentalBlock_RoofFrame_PostN", blockArch.transform,
                new Vector3(12.40f, 8.10f, -15.60f), new Vector3(0.06f, 1.10f, 0.06f), aluminumMat);
            CreateBox("DentalBlock_RoofFrame_PostS", blockArch.transform,
                new Vector3(12.40f, 8.10f, -17.30f), new Vector3(0.06f, 1.10f, 0.06f), aluminumMat);
            CreateBox("DentalBlock_RoofFrame_TopBar", blockArch.transform,
                new Vector3(12.40f, 8.62f, -16.45f), new Vector3(0.06f, 0.06f, 1.76f), aluminumMat);
            CreateBox("DentalBlock_RoofFrame_MidBar", blockArch.transform,
                new Vector3(12.40f, 8.15f, -16.45f), new Vector3(0.06f, 0.06f, 1.76f), aluminumMat);

            // --- 2ND FLOOR BAY 5A: "Atención Dental / Dentistas" (Z = -14.10 .. -9.70m, center -11.90m) ---
            CreateTerracottaSign("Sign_Dentistas", blockArch.transform,
                centerZ: -11.90f, centerY: 6.46f, widthZ: 3.55f, heightY: 0.82f, dentalPlaqueMat, signDentistasMat);
            CreateBox("Dental_UpperWindow_Glass", blockArch.transform,
                new Vector3(11.96f, 4.74f, -11.90f), new Vector3(0.08f, 1.80f, 2.90f), darkGlassMat);
            CreateBox("Dental_UpperWindow_Sill", blockArch.transform,
                new Vector3(11.92f, 3.82f, -11.90f), new Vector3(0.16f, 0.08f, 3.04f), whiteFrameMat);
            float[] bay5aMullions = new float[] { -13.34f, -11.90f, -10.46f };
            for (int i = 0; i < bay5aMullions.Length; i++)
            {
                CreateBox($"Dental_UpperMullion_{i}", blockArch.transform,
                    new Vector3(11.93f, 4.74f, bay5aMullions[i]), new Vector3(0.10f, 1.82f, 0.06f), aluminumMat);
            }

            // --- 2ND FLOOR BAY 5B: "Cambios Puente" (Z = -18.80 .. -14.10m, center -16.45m) ---
            CreateTerracottaSign("Sign_CambiosPuente", blockArch.transform,
                centerZ: -16.45f, centerY: 6.46f, widthZ: 3.25f, heightY: 0.78f, terracottaPlaqueMat, signCambiosPuenteMat);
            // Twin upper dark windows
            CreateBox("Cambios_UpperWindow1_Glass", blockArch.transform,
                new Vector3(11.96f, 4.74f, -15.65f), new Vector3(0.08f, 1.76f, 1.85f), darkGlassMat);
            CreateBox("Cambios_UpperWindow1_Mullion", blockArch.transform,
                new Vector3(11.93f, 4.74f, -15.65f), new Vector3(0.10f, 1.78f, 0.06f), aluminumMat);
            CreateBox("Cambios_UpperWindow2_Glass", blockArch.transform,
                new Vector3(11.96f, 4.74f, -17.65f), new Vector3(0.08f, 1.76f, 1.45f), darkGlassMat);
            CreateBox("Cambios_UpperWindow2_Mullion", blockArch.transform,
                new Vector3(11.93f, 4.74f, -17.65f), new Vector3(0.10f, 1.78f, 0.06f), aluminumMat);
            CreateBox("Cambios_UpperWindows_Sill", blockArch.transform,
                new Vector3(11.92f, 3.84f, -16.60f), new Vector3(0.16f, 0.08f, 3.90f), whiteFrameMat);

            // Wall-mounted Mini-Split A/C unit + Hanging Double-Sided LED Exchange Rate Display ("17.50 / 18.50")
            CreateBox("Cambios_AC_Condenser", blockArch.transform,
                new Vector3(11.76f, 3.16f, -17.20f), new Vector3(0.38f, 0.52f, 0.68f), whiteFrameMat);
            CreateBox("Cambios_AC_FanGrille", blockArch.transform,
                new Vector3(11.56f, 3.16f, -17.28f), new Vector3(0.03f, 0.36f, 0.36f), darkZocaloMat);
            CreateHangingExchangeBoard("Cambios_LED_ExchangeBoard", blockArch.transform,
                pos: new Vector3(11.12f, 3.48f, -18.55f), darkZocaloMat, ledExchangeMat);

            // --- 2ND FLOOR BAY 5C: "APPLE DENTAL CARE / DENTISTA" & Azucenas Corner (Z = -26.80 .. -18.80m) ---
            CreateTerracottaSign("Sign_AppleDental", blockArch.transform,
                centerZ: -22.65f, centerY: 6.32f, widthZ: 6.45f, heightY: 1.48f, whiteFrameMat, signAppleDentalMat);
            // Two wide upper windows under Apple Dental Care
            CreateBox("AppleDental_UpperWin1_Glass", blockArch.transform,
                new Vector3(11.96f, 4.66f, -20.80f), new Vector3(0.08f, 1.66f, 2.55f), darkGlassMat);
            CreateBox("AppleDental_UpperWin1_Mullion", blockArch.transform,
                new Vector3(11.93f, 4.66f, -20.80f), new Vector3(0.10f, 1.68f, 0.06f), aluminumMat);
            CreateBox("AppleDental_UpperWin2_Glass", blockArch.transform,
                new Vector3(11.96f, 4.66f, -24.45f), new Vector3(0.08f, 1.66f, 2.65f), darkGlassMat);
            CreateBox("AppleDental_UpperWin2_Mullion", blockArch.transform,
                new Vector3(11.93f, 4.66f, -24.45f), new Vector3(0.10f, 1.68f, 0.06f), aluminumMat);
            CreateBox("AppleDental_UpperWindows_Sill", blockArch.transform,
                new Vector3(11.92f, 3.81f, -22.65f), new Vector3(0.16f, 0.08f, 6.45f), whiteFrameMat);

            // South Azucenas Side Wall Details (Z = -26.80m, facing Calle Azucenas)
            CreateBox("DentalBlock_SouthWall_UpperWindow", blockArch.transform,
                new Vector3(15.20f, 4.70f, -26.83f), new Vector3(3.40f, 1.60f, 0.08f), darkGlassMat);
            CreateBox("DentalBlock_SouthWall_WinMullion", blockArch.transform,
                new Vector3(15.20f, 4.70f, -26.85f), new Vector3(0.08f, 1.62f, 0.08f), aluminumMat);
            CreateBox("DentalBlock_SouthWall_Zocalo", blockArch.transform,
                new Vector3(18.00f, 0.38f, -26.84f), new Vector3(12.04f, 0.36f, 0.08f), darkZocaloMat);

            // --- 1ST FLOOR: 4 CONTINUOUS GROUND-FLOOR STOREFRONTS (Z = -26.75 .. -9.75m) ---
            // Local 1 (under Dentistas: Z = -14.10 .. -9.75m) — Blue glazed storefront & door + beige wedge awning
            CreateBox("Dental_GroundGlass", blockArch.transform,
                new Vector3(11.97f, 1.48f, -11.92f), new Vector3(0.06f, 2.45f, 3.95f), blueGlassDoorMat);
            CreateBox("Dental_DoorFrame_L", blockArch.transform,
                new Vector3(11.93f, 1.45f, -10.80f), new Vector3(0.12f, 2.50f, 0.09f), whiteFrameMat);
            CreateBox("Dental_DoorFrame_R", blockArch.transform,
                new Vector3(11.93f, 1.45f, -12.95f), new Vector3(0.12f, 2.50f, 0.09f), whiteFrameMat);
            CreateBox("Dental_DoorMidBar", blockArch.transform,
                new Vector3(11.93f, 1.15f, -11.88f), new Vector3(0.12f, 0.09f, 2.15f), whiteFrameMat);
            CreateBox("Dental_GroundTransom", blockArch.transform,
                new Vector3(11.93f, 2.22f, -11.92f), new Vector3(0.12f, 0.08f, 3.95f), whiteFrameMat);
            CreateWedgeAwning("Dental_Awning", blockArch.transform,
                centerZ: -11.95f, topY: 3.16f, widthZ: 4.15f, projectionX: 1.38f, dropY: 0.76f, beigeAwningMat, awningValanceMat);

            // Local 2 (under Cambios Puente: Z = -18.60 .. -14.20m) — Roll-up shutter + narrow side doorway + beige wedge awning
            CreateBox("Cambios_GroundShutter", blockArch.transform,
                new Vector3(11.96f, 1.42f, -15.85f), new Vector3(0.08f, 2.38f, 2.85f), shutterMat);
            CreateBox("Cambios_SideDoor", blockArch.transform,
                new Vector3(11.96f, 1.38f, -17.90f), new Vector3(0.08f, 2.30f, 0.95f), whiteFrameMat);
            CreateBox("Cambios_GroundPilaster", blockArch.transform,
                new Vector3(11.93f, 1.65f, -14.15f), new Vector3(0.14f, 3.10f, 0.24f), warmBeigeStuccoMat);
            CreateWedgeAwning("Cambios_Awning", blockArch.transform,
                centerZ: -16.38f, topY: 3.16f, widthZ: 4.25f, projectionX: 1.38f, dropY: 0.76f, beigeAwningMat, awningValanceMat);

            // Local 3 (under Apple Dental Care North: Z = -22.65 .. -18.70m) — Security grille + roll-up shutter + beige wedge awning
            CreateBox("AppleDental_Local3_GrilleDoor", blockArch.transform,
                new Vector3(11.96f, 1.42f, -19.85f), new Vector3(0.08f, 2.36f, 1.45f), whiteFrameMat);
            CreateBox("AppleDental_Local3_DoorBacking", blockArch.transform,
                new Vector3(11.99f, 1.42f, -19.85f), new Vector3(0.06f, 2.28f, 1.35f), interiorDarkMat);
            CreateBox("AppleDental_Local3_Shutter", blockArch.transform,
                new Vector3(11.96f, 1.42f, -21.55f), new Vector3(0.08f, 2.38f, 1.75f), shutterMat);
            CreateBox("AppleDental_Local3_Pilaster", blockArch.transform,
                new Vector3(11.93f, 1.65f, -18.65f), new Vector3(0.14f, 3.10f, 0.22f), warmBeigeStuccoMat);
            CreateWedgeAwning("AppleDental_Awning_North", blockArch.transform,
                centerZ: -20.68f, topY: 3.16f, widthZ: 3.85f, projectionX: 1.38f, dropY: 0.76f, beigeAwningMat, awningValanceMat);

            // Local 4 (South-East Corner passing behind Digital Arch East column at Z = -23.50m: Z = -26.70 .. -22.75m)
            CreateBox("AppleDental_CornerShutter", blockArch.transform,
                new Vector3(11.96f, 1.42f, -24.72f), new Vector3(0.08f, 2.38f, 3.45f), shutterMat);
            CreateBox("AppleDental_CornerPilaster", blockArch.transform,
                new Vector3(11.93f, 1.65f, -22.72f), new Vector3(0.14f, 3.10f, 0.22f), warmBeigeStuccoMat);
            // Rounded / Curved Beige Corner Awning (signature barrel awning seen at right edge of real photo)
            CreateCurvedCornerAwning("AppleDental_CornerAwning_Curved", blockArch.transform,
                centerZ: -24.75f, topY: 3.22f, widthZ: 3.95f, projectionX: 1.42f, dropY: 0.84f, beigeAwningMat, awningValanceMat);

            // Continuous clean NE corner concrete infill slab bridging Sidewalk_East (Z=-26.11) & Azucenas_NW_Corner up to building Z=-26.80
            Material concreteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/ConcreteMaterial.mat") ?? copingGreyMat;
            CreateBox("Sidewalk_East_Azucenas_CornerInfill", blockArch.transform,
                new Vector3(17.10f, 0.098f, -27.95f), new Vector3(13.80f, 0.196f, 2.25f), concreteMat, keepCollider: true);
            CreateBox("Sidewalk_East_CornerConnector_West", blockArch.transform,
                new Vector3(10.25f, 0.098f, -26.68f), new Vector3(3.50f, 0.196f, 1.25f), concreteMat, keepCollider: true);

            // -------------------------------------------------------------------------
            // REMOVE PARKING STALLS & METERS -> REPLACE WITH CONTINUOUS SOLID WHITE LANE LINES
            // -------------------------------------------------------------------------
            RebuildAvJuarezContinuousSolidLaneLines();

            // Save East Block & solid lane lines BEFORE calling CorridorStreetlightStandardizer
            // (BuildStandardizedStreetlights reopens ScenePath from disk).
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            // -------------------------------------------------------------------------
            // FOREGROUND STREETLAMP & CORRIDOR STREETLIGHT STANDARDIZATION
            // -------------------------------------------------------------------------
            CorridorStreetlightStandardizer.BuildStandardizedStreetlights();

            Debug.Log($"[EastSidewalkBlockBuilder] Authentic East Sidewalk Block successfully saved to '{ScenePath}'!");
        }

        public static void RebuildAvJuarezContinuousSolidLaneLines()
        {
            // Remove legacy East parallel parking stalls, parking meters, and dashed south lane lines
            string[] legacyNames = new string[]
            {
                "Parking_Lanes_East",
                "Parking_Meters_East",
                "AvJuarez_SolidLaneLines_Root",
                "Road_AvenidaJuarez_SouthMarkings"
            };
            Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Transform tr in allTransforms)
            {
                if (tr == null || !tr.gameObject.scene.IsValid()) continue;
                for (int i = 0; i < legacyNames.Length; i++)
                {
                    if (tr.name == legacyNames[i])
                    {
                        UnityEngine.Object.DestroyImmediate(tr.gameObject);
                        break;
                    }
                }
            }

            GameObject envGo = GameObject.Find("Environment");
            Transform envRoot = envGo != null ? envGo.transform : null;

            Material whiteStripeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/WhiteStripeMaterial.mat")
                ?? AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/CrosswalkWhiteStripe.mat")
                ?? CreateOrGetColorMaterial("Assets/Settings/WhiteStripeMaterial.mat", new Color(0.95f, 0.95f, 0.95f), 0.15f);

            GameObject linesRoot = new GameObject("AvJuarez_SolidLaneLines_Root");
            if (envRoot != null) linesRoot.transform.SetParent(envRoot);
            linesRoot.transform.position = Vector3.zero;

            // Roadway curb-to-curb asphalt: X = -2.60m (Curb_West face) to +6.80m (Curb_East face) -> 9.40m = 3 continuous northbound queue lanes (3.133m each).
            // Solid white continuous lines enforce NO lane-changing once vehicles merge into the international bridge approach queue.
            float[] laneLineXs = new float[] { -2.35f, 0.533f, 3.667f, 6.55f };
            string[] laneLineLabels = new string[] { "WestEdge", "Divider_Lane1_2", "Divider_Lane2_3", "EastEdge" };
            const float lineWidth = 0.12f;
            const float lineHeight = 0.012f;
            const float lineY = 0.012f;

            // Segment 1: Main Commercial Corridor (north of Calle Azucenas Z = -27.20m to south of Calle Gardenias Z = +20.20m)
            const float mainStartZ = -27.20f;
            const float mainEndZ = 20.20f;
            float mainLen = mainEndZ - mainStartZ;
            float mainCenterZ = 0.5f * (mainStartZ + mainEndZ);

            // Segment 2: Approach South of Calle Azucenas (Z = -59.50m to -38.80m)
            const float southStartZ = -59.50f;
            const float southEndZ = -38.80f;
            float southLen = southEndZ - southStartZ;
            float southCenterZ = 0.5f * (southStartZ + southEndZ);

            for (int i = 0; i < laneLineXs.Length; i++)
            {
                CreateBox($"SolidLine_Main_{laneLineLabels[i]}", linesRoot.transform,
                    new Vector3(laneLineXs[i], lineY, mainCenterZ), new Vector3(lineWidth, lineHeight, mainLen), whiteStripeMat);

                CreateBox($"SolidLine_South_{laneLineLabels[i]}", linesRoot.transform,
                    new Vector3(laneLineXs[i], lineY, southCenterZ), new Vector3(lineWidth, lineHeight, southLen), whiteStripeMat);
            }

            Debug.Log("[EastSidewalkBlockBuilder] Removed East parking stalls & meters; built 3-lane continuous solid white bridge approach markings.");
        }

        [MenuItem("MyVirtualCommunity/Verify East Block & Solid Lanes")]
        public static void VerifyEastBlockAndSolidLanes()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[EastBlockVerify] FAIL: Could not open {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            int passed = 0;
            int total = 0;
            void Check(string label, bool ok)
            {
                total++;
                if (ok) { passed++; Debug.Log($"  [PASS] {label}"); }
                else { Debug.LogError($"  [FAIL] {label}"); }
            }

            GameObject mich = GameObject.Find("Michoacana_MainMass");
            Check("La Michoacana compacted to ~5.4m width (-9.20..-3.80m)",
                mich != null && mich.TryGetComponent<Renderer>(out Renderer mr) &&
                Mathf.Abs(mr.bounds.size.z - 5.40f) < 0.10f &&
                Mathf.Abs(mr.bounds.max.z - (-3.80f)) < 0.10f);

            GameObject dental = GameObject.Find("DentalBlock_MainMass");
            Check("2-story South-East commercial block reaches Z=-26.80m (~2.31m sidewalk to Azucenas)",
                dental != null && dental.TryGetComponent<Renderer>(out Renderer dr) &&
                Mathf.Abs(dr.bounds.min.z - (-26.80f)) < 0.10f &&
                Mathf.Abs(dr.bounds.max.z - (-9.20f)) < 0.10f);

            Check("Cambios Puente sign, hanging LED board, and Apple Dental Care sign exist",
                GameObject.Find("Sign_CambiosPuente") != null &&
                GameObject.Find("Cambios_LED_ExchangeBoard") != null &&
                GameObject.Find("Sign_AppleDental") != null &&
                GameObject.Find("AppleDental_CornerAwning_Curved") != null);

            Check("East parallel parking stalls and parking meters removed",
                GameObject.Find("Parking_Lanes_East") == null &&
                GameObject.Find("Parking_Meters_East") == null);

            GameObject linesRoot = GameObject.Find("AvJuarez_SolidLaneLines_Root");
            Check("Av. Juárez continuous solid white 3-lane approach lines built (8 segments)",
                linesRoot != null && linesRoot.transform.childCount == 8);

            GameObject eastSouthLamp = GameObject.Find("Streetlight_East_SouthCorner");
            Check("Standardized East SouthCorner streetlight preserved at (7.35, 0.20, -20.50)",
                eastSouthLamp != null && Vector3.Distance(eastSouthLamp.transform.position, new Vector3(7.35f, 0.20f, -20.50f)) < 0.10f);

            Debug.Log($"[EastBlockVerify] SUMMARY: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }

        public static void CaptureVerificationScreenshots()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.EndsWith("BorderCorridorScene.unity"))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            const string outDir = @"C:\Users\leolm\.gemini\antigravity\brain\4ce9141f-e967-41a8-b6f3-7471781ad5c8";
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            GameObject camGo = new GameObject("EastBlockVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.Skybox;

            try
            {
                // Shot 1: Reference comparison matching user's screenshot & real photo (clear of West streetlight pole)
                cam.fieldOfView = 54f;
                cam.transform.position = new Vector3(-1.8f, 1.85f, -0.5f);
                cam.transform.LookAt(new Vector3(12.0f, 3.3f, -15.5f));
                WarmupCamera(cam, 1600, 900);
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "east_block_ref_comparison.png"));

                // Shot 2: Close-up of Cambios Puente, LED board, Apple Dental Care, curved corner awning & Digital Arch East column
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-2.5f, 2.2f, -29.5f);
                cam.transform.LookAt(new Vector3(12.0f, 3.6f, -19.5f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "east_block_se_arch_corner.png"));

                // Shot 3: Elevated corridor view showing removed parking meters/stalls, continuous 3-lane solid white lines & full East block down to Azucenas
                cam.fieldOfView = 54f;
                cam.transform.position = new Vector3(1.5f, 16.5f, -36.0f);
                cam.transform.LookAt(new Vector3(5.5f, 1.0f, -6.0f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "east_block_lanes_overhead.png"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(camGo);
            }
            Debug.Log("[EastSidewalkBlockBuilder] Verification screenshots generated successfully!");
        }

        #region Architectural Helper Builders

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = localScale;
            SetMaterial(go, mat);
            if (!keepCollider)
            {
                var col = go.GetComponent<Collider>();
                if (col != null) UnityEngine.Object.DestroyImmediate(col);
            }
            return go;
        }

        private static void CreateWedgeAwning(string name, Transform parent, float centerZ, float topY, float widthZ, float projectionX, float dropY, Material canvasMat, Material valanceMat)
        {
            GameObject awningRoot = new GameObject(name);
            awningRoot.transform.SetParent(parent);
            awningRoot.transform.localPosition = new Vector3(12.0f, topY, centerZ);

            // Sloped main canvas roof panel
            float slopeLen = Mathf.Sqrt(projectionX * projectionX + dropY * dropY);
            float angleDeg = Mathf.Atan2(dropY, projectionX) * Mathf.Rad2Deg;

            GameObject slopePanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slopePanel.name = "Canvas_Slope";
            slopePanel.transform.SetParent(awningRoot.transform);
            slopePanel.transform.localPosition = new Vector3(-projectionX * 0.5f, -dropY * 0.5f, 0f);
            slopePanel.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
            slopePanel.transform.localScale = new Vector3(slopeLen, 0.05f, widthZ);
            SetMaterial(slopePanel, canvasMat);
            UnityEngine.Object.DestroyImmediate(slopePanel.GetComponent<Collider>());

            // Front vertical valance flap (hanging lip at the front edge of the awning)
            GameObject valance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            valance.name = "Canvas_Valance";
            valance.transform.SetParent(awningRoot.transform);
            valance.transform.localPosition = new Vector3(-projectionX + 0.02f, -dropY - 0.09f, 0f);
            valance.transform.localScale = new Vector3(0.04f, 0.18f, widthZ);
            SetMaterial(valance, valanceMat);
            UnityEngine.Object.DestroyImmediate(valance.GetComponent<Collider>());

            // Side enclosures (left & right wedge cheeks)
            float[] sideZs = new float[] { -widthZ * 0.5f + 0.02f, widthZ * 0.5f - 0.02f };
            for (int i = 0; i < sideZs.Length; i++)
            {
                GameObject sideCheek = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sideCheek.name = $"Canvas_Side_{i}";
                sideCheek.transform.SetParent(awningRoot.transform);
                sideCheek.transform.localPosition = new Vector3(-projectionX * 0.42f, -dropY * 0.62f, sideZs[i]);
                sideCheek.transform.localScale = new Vector3(projectionX * 0.82f, dropY * 0.72f, 0.04f);
                SetMaterial(sideCheek, valanceMat);
                UnityEngine.Object.DestroyImmediate(sideCheek.GetComponent<Collider>());
            }
        }

        private static void CreateTerracottaSign(string name, Transform parent, float centerZ, float centerY, float widthZ, float heightY, Material plaqueMat, Material faceTexMat)
        {
            GameObject signRoot = new GameObject(name);
            signRoot.transform.SetParent(parent);
            signRoot.transform.localPosition = new Vector3(11.92f, centerY, centerZ);

            // 3D Terracotta Backing Box
            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Sign_BackingPlaque";
            box.transform.SetParent(signRoot.transform);
            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = new Vector3(0.14f, heightY, widthZ);
            SetMaterial(box, plaqueMat);
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());

            // Front Textured Quad (facing West -X)
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sign_FrontFace";
            quad.transform.SetParent(signRoot.transform);
            quad.transform.localPosition = new Vector3(-0.075f, 0f, 0f);
            quad.transform.localRotation = Quaternion.Euler(0f, 90f, 0f); // Normal points -X (West)
            quad.transform.localScale = new Vector3(widthZ - 0.04f, heightY - 0.04f, 1.0f);
            SetMaterial(quad, faceTexMat);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        private static void CreateRecessedWindow(string name, Transform parent, float centerZ, float centerY, float widthZ, float heightY, Material glassMat, Material frameMat)
        {
            CreateBox($"{name}_Frame", parent, new Vector3(11.96f, centerY, centerZ), new Vector3(0.10f, heightY + 0.14f, widthZ + 0.14f), frameMat);
            CreateBox($"{name}_Glass", parent, new Vector3(11.94f, centerY, centerZ), new Vector3(0.08f, heightY, widthZ), glassMat);
            CreateBox($"{name}_Sill", parent, new Vector3(11.91f, centerY - heightY * 0.5f - 0.05f, centerZ), new Vector3(0.16f, 0.08f, widthZ + 0.22f), frameMat);
        }

        private static void CreateCenterStreetlamp(string name, Transform parent, Vector3 pos, Material poleMat, Material capMat, Material lensMat)
        {
            GameObject lampRoot = new GameObject(name);
            lampRoot.transform.SetParent(parent);
            lampRoot.transform.position = pos;

            // Octagonal/Cylindrical base & slender pole (matching the central lamppost in the photo)
            GameObject basePed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basePed.name = "Pole_Base";
            basePed.transform.SetParent(lampRoot.transform);
            basePed.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            basePed.transform.localScale = new Vector3(0.22f, 0.35f, 0.22f);
            SetMaterial(basePed, poleMat);
            UnityEngine.Object.DestroyImmediate(basePed.GetComponent<Collider>());

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Pole_Shaft";
            shaft.transform.SetParent(lampRoot.transform);
            shaft.transform.localPosition = new Vector3(0f, 3.3f, 0f);
            shaft.transform.localScale = new Vector3(0.13f, 3.3f, 0.13f);
            SetMaterial(shaft, poleMat);
            UnityEngine.Object.DestroyImmediate(shaft.GetComponent<Collider>());

            // Conical/Bowl luminaire head at top of pole
            GameObject bowl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            bowl.name = "Luminaire_Bowl";
            bowl.transform.SetParent(lampRoot.transform);
            bowl.transform.localPosition = new Vector3(0f, 6.65f, 0f);
            bowl.transform.localScale = new Vector3(0.55f, 0.12f, 0.55f);
            SetMaterial(bowl, lensMat);
            UnityEngine.Object.DestroyImmediate(bowl.GetComponent<Collider>());

            GameObject cap = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cap.name = "Luminaire_Cap";
            cap.transform.SetParent(lampRoot.transform);
            cap.transform.localPosition = new Vector3(0f, 6.76f, 0f);
            cap.transform.localScale = new Vector3(0.68f, 0.22f, 0.68f);
            SetMaterial(cap, capMat);
            UnityEngine.Object.DestroyImmediate(cap.GetComponent<Collider>());
        }

        private static Material CreateOrGetColorMaterial(string path, Color color, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void CreateHangingExchangeBoard(string name, Transform parent, Vector3 pos, Material cabinetMat, Material ledFaceMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.localPosition = pos;

            // Horizontal steel arm anchoring to East building wall (X = 12.0m)
            CreateBox("MountArm", root.transform, new Vector3(0.46f, 0.28f, 0f), new Vector3(0.92f, 0.07f, 0.07f), cabinetMat);
            // Matte-black LED enclosure box (0.85m wide in X, 0.56m tall in Y, 0.14m thick in Z)
            CreateBox("LED_Cabinet", root.transform, Vector3.zero, new Vector3(0.86f, 0.56f, 0.14f), cabinetMat);

            // South-facing LED display quad (visible to drivers heading North toward bridge!)
            var southFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
            southFace.name = "LED_Face_South";
            southFace.transform.SetParent(root.transform);
            southFace.transform.localPosition = new Vector3(0f, 0f, -0.075f);
            southFace.transform.localRotation = Quaternion.identity; // Faces -Z (South)
            southFace.transform.localScale = new Vector3(0.80f, 0.50f, 1.0f);
            SetMaterial(southFace, ledFaceMat);
            UnityEngine.Object.DestroyImmediate(southFace.GetComponent<Collider>());

            // North-facing LED display quad
            var northFace = GameObject.CreatePrimitive(PrimitiveType.Quad);
            northFace.name = "LED_Face_North";
            northFace.transform.SetParent(root.transform);
            northFace.transform.localPosition = new Vector3(0f, 0f, 0.075f);
            northFace.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Faces +Z (North)
            northFace.transform.localScale = new Vector3(0.80f, 0.50f, 1.0f);
            SetMaterial(northFace, ledFaceMat);
            UnityEngine.Object.DestroyImmediate(northFace.GetComponent<Collider>());
        }

        private static void CreateCurvedCornerAwning(string name, Transform parent, float centerZ, float topY, float widthZ,
            float projectionX, float dropY, Material canvasMat, Material valanceMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.localPosition = new Vector3(12.0f, topY, centerZ);

            // Quarter-ellipse barrel curve built from 6 smooth canvas slats
            const int segs = 6;
            for (int i = 0; i < segs; i++)
            {
                float a0 = (Mathf.PI * 0.5f) * i / segs;
                float a1 = (Mathf.PI * 0.5f) * (i + 1) / segs;
                float x0 = -projectionX * Mathf.Sin(a0);
                float y0 = -dropY * (1f - Mathf.Cos(a0));
                float x1 = -projectionX * Mathf.Sin(a1);
                float y1 = -dropY * (1f - Mathf.Cos(a1));

                Vector2 mid = new Vector2(0.5f * (x0 + x1), 0.5f * (y0 + y1));
                Vector2 delta = new Vector2(x1 - x0, y1 - y0);
                float segLen = delta.magnitude + 0.02f;
                float deg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

                var slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = $"BarrelSlat_{i}";
                slat.transform.SetParent(root.transform);
                slat.transform.localPosition = new Vector3(mid.x, mid.y, 0f);
                slat.transform.localRotation = Quaternion.Euler(0f, 0f, deg);
                slat.transform.localScale = new Vector3(segLen, 0.05f, widthZ);
                SetMaterial(slat, canvasMat);
                UnityEngine.Object.DestroyImmediate(slat.GetComponent<Collider>());
            }

            // Rounded end caps at North & South edges of barrel awning
            float[] sideZs = new float[] { -widthZ * 0.5f + 0.025f, widthZ * 0.5f - 0.025f };
            for (int i = 0; i < sideZs.Length; i++)
            {
                var side = GameObject.CreatePrimitive(PrimitiveType.Cube);
                side.name = $"BarrelEnd_{i}";
                side.transform.SetParent(root.transform);
                side.transform.localPosition = new Vector3(-projectionX * 0.45f, -dropY * 0.58f, sideZs[i]);
                side.transform.localScale = new Vector3(projectionX * 0.88f, dropY * 0.80f, 0.04f);
                SetMaterial(side, valanceMat);
                UnityEngine.Object.DestroyImmediate(side.GetComponent<Collider>());
            }

            // Front valance skirt along bottom edge
            var valance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            valance.name = "BarrelValance";
            valance.transform.SetParent(root.transform);
            valance.transform.localPosition = new Vector3(-projectionX + 0.02f, -dropY - 0.08f, 0f);
            valance.transform.localScale = new Vector3(0.04f, 0.16f, widthZ);
            SetMaterial(valance, valanceMat);
            UnityEngine.Object.DestroyImmediate(valance.GetComponent<Collider>());
        }

        private static Material CreateOrGetTextureMaterial(string matPath, string texPath, bool emissive = false)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.shader = shader;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTexture("_MainTex", tex);
                if (emissive)
                {
                    mat.SetTexture("_EmissionMap", tex);
                    mat.SetColor("_EmissionColor", new Color(0.75f, 0.95f, 0.78f));
                    mat.EnableKeyword("_EMISSION");
                }
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_Smoothness", emissive ? 0.35f : 0.12f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;
        }

        private static void WarmupCamera(Camera cam, int width, int height)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
        }

        private static void RenderToFile(Camera cam, int width, int height, string savePath)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
            var prevRT = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            cam.targetTexture = prevRT;

            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);

            byte[] png = tex.EncodeToPNG();
            UnityEngine.Object.DestroyImmediate(tex);
            File.WriteAllBytes(savePath, png);
            Debug.Log($"[EastSidewalkBlockBuilder] Saved screenshot: {savePath}");
        }

        #endregion
    }
}

