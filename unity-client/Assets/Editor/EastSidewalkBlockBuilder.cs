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

            // Sign Texture Materials
            Material signCuriosMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_Curiosidades.mat", "Assets/Textures/Environment/T_East_Sign_Curiosidades.png");
            Material signMinervaMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_Minerva.mat", "Assets/Textures/Environment/T_East_Sign_Minerva.png");
            Material signComercialUnoMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_ComercialUno.mat", "Assets/Textures/Environment/T_East_Sign_ComercialUno.png");
            Material signBenavidesMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_Benavides.mat", "Assets/Textures/Environment/T_East_Sign_Benavides.png");
            Material signConsultorioMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_Consultorio.mat", "Assets/Textures/Environment/T_East_Sign_Consultorio.png");
            Material signMichoacanaMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_LaMichoacana.mat", "Assets/Textures/Environment/T_East_Sign_LaMichoacana.png");
            Material signDentistasMat = CreateOrGetTextureMaterial("Assets/Settings/East_Sign_Dentistas.mat", "Assets/Textures/Environment/T_East_Sign_Dentistas.png");
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
                    hl.localPosition = new Vector3(0f, -0.785f, 0f);
                    hl.localScale = new Vector3(1.6f, 0.02f, 1.6f);
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
            // MODULE 4: CENTER-RIGHT BLOCK — "La Michoacana"
            // Spans Z = -12.2m to -3.8m (Length = 8.4m, Center Z = -8.0m, Height = 5.4m)
            // -------------------------------------------------------------------------
            CreateBox("Michoacana_MainMass", blockArch.transform,
                new Vector3(18.5f, 2.7f, -8.0f), new Vector3(11.0f, 5.4f, 8.4f), whiteStuccoMat, keepCollider: true);
            // Upper Wall Above Parlor Opening (Y = 2.75m to 5.4m, flush at X = 12.0m)
            CreateBox("Michoacana_UpperFacadeWall", blockArch.transform,
                new Vector3(12.4f, 4.05f, -8.0f), new Vector3(0.8f, 2.7f, 8.4f), whiteStuccoMat);
            // Upper Cornice Coping Ledge (Y = 5.42m)
            CreateBox("Michoacana_RoofCornice", blockArch.transform,
                new Vector3(11.90f, 5.42f, -8.0f), new Vector3(0.26f, 0.18f, 8.4f), copingGreyMat);
            CreateBox("Michoacana_RoofSubLip", blockArch.transform,
                new Vector3(11.94f, 5.26f, -8.0f), new Vector3(0.16f, 0.10f, 8.4f), whiteStuccoMat);

            // Open Parlor Interior Recess (Z = -11.2m to -4.4m)
            CreateBox("Michoacana_InteriorBackWall", blockArch.transform,
                new Vector3(13.6f, 1.45f, -7.8f), new Vector3(0.2f, 2.5f, 6.8f), michoacanaWallMat);
            CreateBox("Michoacana_InteriorSideWall_N", blockArch.transform,
                new Vector3(12.8f, 1.45f, -4.3f), new Vector3(1.6f, 2.5f, 0.3f), whiteStuccoMat);
            CreateBox("Michoacana_InteriorSideWall_S", blockArch.transform,
                new Vector3(12.8f, 1.45f, -11.3f), new Vector3(1.6f, 2.5f, 0.3f), whiteStuccoMat);

            // Green Ice Cream Freezer Counter ("Vitrina de Paletas y Helados")
            CreateBox("Michoacana_GreenCounter", blockArch.transform,
                new Vector3(12.45f, 0.65f, -7.4f), new Vector3(0.85f, 0.90f, 3.4f), michoacanaGreenMat);
            CreateBox("Michoacana_CounterGlassTop", blockArch.transform,
                new Vector3(12.45f, 1.22f, -7.4f), new Vector3(0.80f, 0.25f, 3.3f), darkGlassMat);
            CreateBox("Michoacana_CounterTrim", blockArch.transform,
                new Vector3(12.01f, 1.10f, -7.4f), new Vector3(0.06f, 0.08f, 3.45f), whiteFrameMat);
            // Small Orange/Terracotta Chair at left side of Michoacana opening (matching photo!)
            CreateBox("Michoacana_ChairSeat", blockArch.transform,
                new Vector3(12.15f, 0.52f, -4.85f), new Vector3(0.45f, 0.45f, 0.45f), orangeAccentMat);
            CreateBox("Michoacana_ChairBack", blockArch.transform,
                new Vector3(12.32f, 0.88f, -4.85f), new Vector3(0.10f, 0.45f, 0.45f), orangeAccentMat);

            // Wide Beige Canvas Wedge Awning for La Michoacana
            CreateWedgeAwning("Michoacana_Awning", blockArch.transform,
                centerZ: -7.8f, topY: 3.18f, widthZ: 7.4f, projectionX: 1.40f, dropY: 0.78f, beigeAwningMat, awningValanceMat);

            // Terracotta Sign "La Michoacana"
            CreateTerracottaSign("Sign_LaMichoacana", blockArch.transform,
                centerZ: -7.8f, centerY: 4.05f, widthZ: 4.35f, heightY: 0.85f, terracottaPlaqueMat, signMichoacanaMat);

            // Background Rooftop Terracotta & Exposed Brick Building (peeking above La Michoacana roof!)
            CreateBox("BackgroundBrick_StuccoFront", blockArch.transform,
                new Vector3(15.2f, 6.35f, -8.2f), new Vector3(2.8f, 2.1f, 4.4f), brickTerracottaMat);
            CreateBox("BackgroundBrick_RightWing", blockArch.transform,
                new Vector3(14.2f, 6.45f, -11.0f), new Vector3(2.6f, 2.3f, 2.2f), brickDarkMat);
            CreateBox("BackgroundBrick_Cornice", blockArch.transform,
                new Vector3(13.75f, 7.15f, -8.2f), new Vector3(0.2f, 0.18f, 4.5f), whiteStuccoMat);

            // -------------------------------------------------------------------------
            // MODULE 5: SOUTH-EAST 2-STORY BLOCK — "Atención Dental / Dentistas"
            // Spans Z = -21.5m to -12.2m (Length = 9.3m, Center Z = -16.85m, Height = 7.6m)
            // -------------------------------------------------------------------------
            CreateBox("DentalBlock_MainMass", blockArch.transform,
                new Vector3(18.0f, 3.8f, -16.85f), new Vector3(12.0f, 7.6f, 9.3f), whiteStuccoMat, keepCollider: true);

            // Prominent Vertical White Column Pillar at Z = -12.1m (between Michoacana & Dental)
            CreateBox("DentalBlock_LeftColumn", blockArch.transform,
                new Vector3(11.88f, 3.90f, -12.15f), new Vector3(0.28f, 7.8f, 1.15f), whiteStuccoMat);
            CreateBox("DentalBlock_ColumnZocalo", blockArch.transform,
                new Vector3(11.86f, 0.62f, -12.15f), new Vector3(0.30f, 0.85f, 1.17f), copingGreyMat);
            // Twin Electric Utility Meters on Column
            CreateBox("UtilityMeter_1", blockArch.transform,
                new Vector3(11.70f, 1.42f, -11.95f), new Vector3(0.08f, 0.26f, 0.20f), aluminumMat);
            CreateBox("UtilityMeter_2", blockArch.transform,
                new Vector3(11.70f, 1.42f, -12.30f), new Vector3(0.08f, 0.26f, 0.20f), aluminumMat);

            // Top Roof Coping on 2-Story Dental Building (Y = 7.65m)
            CreateBox("DentalBlock_RoofCoping", blockArch.transform,
                new Vector3(11.90f, 7.62f, -17.3f), new Vector3(0.24f, 0.16f, 8.4f), darkZocaloMat);

            // Rooftop Parabolic Satellite Dish (visible at top right of photo)
            GameObject satDish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            satDish.name = "DentalBlock_SatelliteDish";
            satDish.transform.SetParent(blockArch.transform);
            satDish.transform.localPosition = new Vector3(12.6f, 8.05f, -16.5f);
            satDish.transform.localRotation = Quaternion.Euler(25f, 0f, 65f);
            satDish.transform.localScale = new Vector3(0.95f, 0.08f, 0.95f);
            SetMaterial(satDish, darkZocaloMat);
            UnityEngine.Object.DestroyImmediate(satDish.GetComponent<Collider>());

            // Upper Floor 4-Pane Dark Window (Z = -16.8m, Y = 4.65m)
            CreateBox("Dental_UpperWindow_Glass", blockArch.transform,
                new Vector3(11.96f, 4.68f, -16.8f), new Vector3(0.08f, 1.78f, 4.3f), darkGlassMat);
            CreateBox("Dental_UpperWindow_Sill", blockArch.transform,
                new Vector3(11.92f, 3.76f, -16.8f), new Vector3(0.16f, 0.08f, 4.45f), whiteFrameMat);
            // 3 Vertical Mullions dividing upper window into 4 panes
            float[] upperMullionZs = new float[] { -18.92f, -17.85f, -16.80f, -15.75f, -14.68f };
            for (int i = 0; i < upperMullionZs.Length; i++)
            {
                CreateBox($"Dental_UpperMullion_{i}", blockArch.transform,
                    new Vector3(11.93f, 4.68f, upperMullionZs[i]), new Vector3(0.10f, 1.80f, 0.06f), aluminumMat);
            }

            // Upper Floor Terracotta-Red Sign: "Atención Dental / Dentistas" (Y = 6.42m)
            CreateTerracottaSign("Sign_Dentistas", blockArch.transform,
                centerZ: -16.8f, centerY: 6.42f, widthZ: 4.65f, heightY: 0.94f, dentalPlaqueMat, signDentistasMat);

            // Ground Floor Storefront of Dental Building (Z = -13.0m to -20.8m)
            CreateBox("Dental_GroundGlass", blockArch.transform,
                new Vector3(11.97f, 1.48f, -16.8f), new Vector3(0.06f, 2.45f, 7.2f), darkGlassMat);
            // White Aluminum Door & Window Frames
            CreateBox("Dental_DoorFrame_L", blockArch.transform,
                new Vector3(11.93f, 1.45f, -15.8f), new Vector3(0.12f, 2.5f, 0.09f), whiteFrameMat);
            CreateBox("Dental_DoorFrame_R", blockArch.transform,
                new Vector3(11.93f, 1.45f, -17.5f), new Vector3(0.12f, 2.5f, 0.09f), whiteFrameMat);
            CreateBox("Dental_DoorMidBar", blockArch.transform,
                new Vector3(11.93f, 1.15f, -16.65f), new Vector3(0.12f, 0.09f, 1.7f), whiteFrameMat);
            CreateBox("Dental_GroundTransom", blockArch.transform,
                new Vector3(11.93f, 2.22f, -16.8f), new Vector3(0.12f, 0.08f, 7.2f), whiteFrameMat);
            // Warm orange interior display items in right window
            CreateBox("Dental_DisplayItems_Right", blockArch.transform,
                new Vector3(12.02f, 0.65f, -19.0f), new Vector3(0.15f, 0.55f, 1.4f), orangeAccentMat);
            CreateBox("Dental_RightZocalo", blockArch.transform,
                new Vector3(11.94f, 0.42f, -14.2f), new Vector3(0.12f, 0.45f, 2.4f), darkZocaloMat);

            // Wide Beige Canvas Wedge Awning for Ground Floor of Dental Block
            CreateWedgeAwning("Dental_Awning", blockArch.transform,
                centerZ: -16.8f, topY: 3.18f, widthZ: 7.5f, projectionX: 1.40f, dropY: 0.78f, beigeAwningMat, awningValanceMat);

            // -------------------------------------------------------------------------
            // FOREGROUND STREETLAMP & CORRIDOR STREETLIGHT STANDARDIZATION
            // -------------------------------------------------------------------------
            CorridorStreetlightStandardizer.BuildStandardizedStreetlights();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[EastSidewalkBlockBuilder] Authentic East Sidewalk Block successfully saved to '{ScenePath}'!");
        }

        public static void CaptureVerificationScreenshots()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.path.EndsWith("BorderCorridorScene.unity"))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string outDir = Path.Combine(repoRoot, "scratch", "screenshots");
            if (!Directory.Exists(outDir)) Directory.CreateDirectory(outDir);

            GameObject camGo = new GameObject("EastBlockVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Shot 1: Wide Panoramic View from across the street (matching the exact reference photo framing!)
            cam.fieldOfView = 58f;
            cam.transform.position = new Vector3(-8.5f, 2.15f, -0.5f);
            cam.transform.rotation = Quaternion.Euler(-3.5f, 90f, 0f); // Facing directly East (+X) slightly upward
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "east_block_panoramic.png"));

            // Shot 2: Medium Frontal View of Minerva, Comercial Uno, Farmacias Benavides & La Michoacana
            cam.fieldOfView = 50f;
            cam.transform.position = new Vector3(-2.5f, 2.0f, 0.8f);
            cam.transform.rotation = Quaternion.Euler(-4.0f, 90f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "east_block_center_benavides.png"));

            // Shot 3: Angle View of Farmacias Benavides, La Michoacana & 2-Story Dental Building
            cam.fieldOfView = 52f;
            cam.transform.position = new Vector3(-1.5f, 2.0f, -6.5f);
            cam.transform.rotation = Quaternion.Euler(-4.5f, 98f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "east_block_south_michoacana_dental.png"));

            UnityEngine.Object.DestroyImmediate(camGo);
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

        private static Material CreateOrGetTextureMaterial(string matPath, string texPath)
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
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_Smoothness", 0.12f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void SetMaterial(GameObject go, Material mat)
        {
            var rend = go.GetComponent<Renderer>();
            if (rend != null && mat != null) rend.sharedMaterial = mat;
        }

        private static void RenderToFile(Camera cam, int width, int height, string savePath)
        {
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
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

