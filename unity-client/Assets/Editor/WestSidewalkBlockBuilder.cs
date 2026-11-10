using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using MyVirtualCommunity.Client.World;
using MyVirtualCommunity.Character;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Reconstructs the West Sidewalk (Acera Poniente) commercial block along Avenida Benito Juárez
    /// to faithfully match the real-world Centro Histórico photograph (from South/Left -Z to North/Right +Z):
    /// - Building A (2-Story Neoclassical Stucco with 5 Semicircular Arched Lunettes & Stepped Cornice):
    ///   1. Bay 1 (Far Left): "A VENTA" neon green sign, beige wedge awning, aluminum glass storefront
    ///   2. Bay 2 (Mid-Left): "Curiosidades Internacional", beige wedge awning, golden-yellow display panels
    ///   3. Bay 3 (Center-Left): "Farma | PHARMACY" two-tone yellow/terracotta sign, yellow window poster, open shop
    ///   4. Bay 4 (Center): "Óptica Fashion" terracotta sign, beige awning, teal/green door, Mexican flag, & Valeria Domínguez
    ///   5. Bay 5 (Transition): "Alimentos Santa Fe" terracotta sign, compact beige awning, bright red booth (casetilla)
    /// - Building B (3-Story Historic Crenellated Building with Stone Masonry & Ribbon Windows):
    ///   6. Bay 6: "Bar La Cucaracha" terracotta sign, beige wedge awning, white corrugated roll-up metal shutter
    ///   7. Bay 7: "Taqueria El Trompo" terracotta sign, beige wedge awning, exposed red-brick portal & open doorway
    /// - Building C (Chamfered North-West Corner Wing with Cantilevered Canopy):
    ///   8. Bay 8: "Dental Jarquín" terracotta sign on 45° chamfer, continuous cantilevered canopy, ribbon window, metal shutter
    /// </summary>
    public static class WestSidewalkBlockBuilder
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Rebuild Authentic West Sidewalk Block & Capture Verification")]
        public static void RebuildAndVerify()
        {
            RebuildWestSidewalkBlock();
            CaptureVerificationScreenshots();
        }

        [MenuItem("MyVirtualCommunity/Rebuild Authentic West Sidewalk Block")]
        public static void RebuildWestSidewalkBlock()
        {
            Debug.Log("[WestSidewalkBlockBuilder] Starting Authentic West Sidewalk Block Reconstruction...");

            AssetDatabase.Refresh();

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[WestSidewalkBlockBuilder] Failed to open scene {ScenePath}");
                return;
            }

            // 1. Setup Color Materials
            Material creamStuccoMat = CreateOrGetColorMaterial("Assets/Settings/West_CreamStucco.mat", new Color(0.90f, 0.88f, 0.84f), 0.08f);
            Material weatheredCreamMat = CreateOrGetColorMaterial("Assets/Settings/West_WeatheredCream.mat", new Color(0.84f, 0.81f, 0.76f), 0.05f);
            Material rusticStuccoMat = CreateOrGetColorMaterial("Assets/Settings/West_RusticStucco.mat", new Color(0.87f, 0.85f, 0.81f), 0.05f);
            Material darkCorniceMat = CreateOrGetColorMaterial("Assets/Settings/West_DarkCornice.mat", new Color(0.32f, 0.34f, 0.35f), 0.12f);
            Material stoneMasonryMat = CreateOrGetColorMaterial("Assets/Settings/West_StoneMasonry.mat", new Color(0.38f, 0.35f, 0.32f), 0.08f);
            Material darkZocaloMat = CreateOrGetColorMaterial("Assets/Settings/West_DarkZocalo.mat", new Color(0.22f, 0.25f, 0.27f), 0.12f);
            Material beigeAwningMat = CreateOrGetColorMaterial("Assets/Settings/West_BeigeAwning.mat", new Color(0.76f, 0.65f, 0.50f), 0.08f);
            Material awningValanceMat = CreateOrGetColorMaterial("Assets/Settings/West_AwningValance.mat", new Color(0.68f, 0.57f, 0.43f), 0.08f);
            Material terracottaPlaqueMat = CreateOrGetColorMaterial("Assets/Settings/West_TerracottaPlaque.mat", new Color(0.48f, 0.21f, 0.13f), 0.15f);
            Material darkGlassMat = CreateOrGetColorMaterial("Assets/Settings/West_DarkGlass.mat", new Color(0.10f, 0.13f, 0.16f), 0.82f);
            Material interiorDarkMat = CreateOrGetColorMaterial("Assets/Settings/West_InteriorDark.mat", new Color(0.07f, 0.08f, 0.09f), 0.05f);
            Material whiteFrameMat = CreateOrGetColorMaterial("Assets/Settings/West_WhiteFrame.mat", new Color(0.93f, 0.94f, 0.95f), 0.30f);
            Material aluminumMat = CreateOrGetColorMaterial("Assets/Settings/West_Aluminum.mat", new Color(0.62f, 0.65f, 0.68f), 0.60f);
            Material yellowPanelMat = CreateOrGetColorMaterial("Assets/Settings/West_YellowPanel.mat", new Color(0.82f, 0.62f, 0.18f), 0.25f);
            Material opticaTealMat = CreateOrGetColorMaterial("Assets/Settings/West_OpticaTeal.mat", new Color(0.08f, 0.46f, 0.36f), 0.30f);
            Material santaFeRedMat = CreateOrGetColorMaterial("Assets/Settings/West_SantaFeRed.mat", new Color(0.74f, 0.12f, 0.10f), 0.28f);
            Material exposedBrickMat = CreateOrGetColorMaterial("Assets/Settings/West_ExposedBrick.mat", new Color(0.48f, 0.18f, 0.14f), 0.10f);
            Material woodDoorMat = CreateOrGetColorMaterial("Assets/Settings/West_WoodDoor.mat", new Color(0.34f, 0.18f, 0.10f), 0.20f);
            Material flagGreenMat = CreateOrGetColorMaterial("Assets/Settings/West_FlagGreen.mat", new Color(0.05f, 0.42f, 0.22f), 0.25f);

            // 2. Setup Texture Materials
            Material signAVentaMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_AVenta.mat", "Assets/Textures/Environment/T_West_Sign_AVenta.png");
            Material signInternacionalMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_Internacional.mat", "Assets/Textures/Environment/T_West_Sign_Internacional.png");
            Material signPharmacyMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_Pharmacy.mat", "Assets/Textures/Environment/T_West_Sign_Pharmacy.png");
            Material posterPharmacyMat = CreateOrGetTextureMaterial("Assets/Settings/West_Poster_Pharmacy.mat", "Assets/Textures/Environment/T_West_Poster_Pharmacy.png");
            Material signOpticaMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_OpticaFashion.mat", "Assets/Textures/Environment/T_West_Sign_OpticaFashion.png");
            Material signSantaFeMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_SantaFe.mat", "Assets/Textures/Environment/T_West_Sign_SantaFe.png");
            Material signCucarachaMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_LaCucaracha.mat", "Assets/Textures/Environment/T_West_Sign_LaCucaracha.png");
            Material signTrompoMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_TaqueriaElTrompo.mat", "Assets/Textures/Environment/T_West_Sign_TaqueriaElTrompo.png");
            Material signDentalMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_Dental.mat", "Assets/Textures/Environment/T_West_Sign_Dental.png");
            Material dentalBaseBoxMat = CreateOrGetTextureMaterial("Assets/Settings/West_Dental_BaseBox.mat", "Assets/Textures/Environment/T_West_Dental_BaseBox.png");
            Material archWindowMat = CreateOrGetTextureMaterial("Assets/Settings/West_ArchWindow.mat", "Assets/Textures/Environment/T_West_ArchWindow.png", alphaClip: true);
            Material metalShutterMat = CreateOrGetTextureMaterial("Assets/Settings/West_MetalShutter.mat", "Assets/Textures/Environment/T_West_MetalShutter.png");
            Material ribbonWindowsMat = CreateOrGetTextureMaterial("Assets/Settings/West_RibbonWindows.mat", "Assets/Textures/Environment/T_West_RibbonWindows.png");

            // 3. Locate Buildings_Corridor & Storefront_OpticaFashion
            GameObject buildingsRoot = GameObject.Find("Buildings_Corridor");
            if (buildingsRoot == null)
            {
                buildingsRoot = new GameObject("Buildings_Corridor");
            }

            // Remove old generic flanking West buildings so our authentic block takes their place
            GameObject oldWestNorth = GameObject.Find("UrbanBuilding_West_North");
            if (oldWestNorth != null) UnityEngine.Object.DestroyImmediate(oldWestNorth);

            GameObject oldWestSouth = GameObject.Find("UrbanBuilding_West_South");
            if (oldWestSouth != null) UnityEngine.Object.DestroyImmediate(oldWestSouth);

            GameObject opticaRoot = GameObject.Find("Storefront_OpticaFashion");
            if (opticaRoot == null)
            {
                opticaRoot = new GameObject("Storefront_OpticaFashion");
                opticaRoot.transform.SetParent(buildingsRoot.transform);
                opticaRoot.transform.position = Vector3.zero;
            }

            // Preserve InteractionPoint_OpticaFashion and ValeriaDominguez_Attendant, destroy old visual children
            for (int i = opticaRoot.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = opticaRoot.transform.GetChild(i);
                if (child.name != "InteractionPoint_OpticaFashion" && child.name != "ValeriaDominguez_Attendant")
                {
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
                }
            }

            // Ensure Valeria Domínguez is positioned cleanly on the sidewalk right in front of Óptica Fashion (Z = +1.5m)
            Transform valeriaTr = opticaRoot.transform.Find("ValeriaDominguez_Attendant");
            if (valeriaTr != null)
            {
                valeriaTr.localPosition = new Vector3(-10.5f, 0.20f, 1.5f);
                valeriaTr.localRotation = Quaternion.Euler(0f, 90f, 0f); // Face East (+X) toward street
            }
            Transform interactTr = opticaRoot.transform.Find("InteractionPoint_OpticaFashion");
            if (interactTr != null)
            {
                interactTr.localPosition = new Vector3(-10.5f, 1.0f, 1.5f);
                Transform hl = interactTr.Find("Highlight_Indicator");
                if (hl != null)
                {
                    hl.localPosition = new Vector3(0f, -0.785f, 0f);
                    hl.localScale = new Vector3(1.6f, 0.02f, 1.6f);
                }
            }

            // =========================================================================
            // 4. BUILD THE AUTHENTIC WEST SIDEWALK BLOCK (South -Z / Left to North +Z / Right)
            // Front wall plane sits at X = -12.0m (flush with back edge of Sidewalk_West)
            // =========================================================================

            // Required collider object for CorridorVerificationTest: "Optica_MainBuilding"
            // Covers Building A (The 2-story Neoclassical Building with 5 Arches, Z = -20.5m to +6.0m, center Z = -7.25m, H = 7.8m)
            GameObject optMain = CreateBox("Optica_MainBuilding", opticaRoot.transform,
                new Vector3(-18.0f, 3.9f, -7.25f), new Vector3(12.0f, 7.8f, 26.5f), creamStuccoMat, keepCollider: true);

            // Container for all West Block architectural details
            GameObject blockArch = new GameObject("WestBlock_HistoricArchitecture");
            blockArch.transform.SetParent(opticaRoot.transform);
            blockArch.transform.localPosition = Vector3.zero;

            // -------------------------------------------------------------------------
            // BUILDING A: 2-STORY NEOCLASSICAL / ART DECO BUILDING WITH 5 ARCHED LUNETTES
            // Spans Z = -20.5m to +6.0m (Length = 26.5m, Center Z = -7.25m, Height = 7.8m)
            // -------------------------------------------------------------------------

            // Top Stepped Dark Grey Stone Parapet Cornice (Y = 7.05m to 7.85m)
            CreateBox("BuildingA_Cornice_Band1", blockArch.transform,
                new Vector3(-11.88f, 7.20f, -7.25f), new Vector3(0.28f, 0.26f, 26.6f), darkCorniceMat);
            CreateBox("BuildingA_Cornice_Band2", blockArch.transform,
                new Vector3(-11.84f, 7.48f, -7.25f), new Vector3(0.36f, 0.24f, 26.7f), stoneMasonryMat);
            CreateBox("BuildingA_Cornice_Band3", blockArch.transform,
                new Vector3(-11.80f, 7.74f, -7.25f), new Vector3(0.42f, 0.22f, 26.8f), darkCorniceMat);

            // Decorative Recessed Upper Wall Relief Panels (between arches and top cornice, Y = 6.18m)
            float[] reliefCentersZ = new float[] { -16.2f, -8.3f, -0.2f };
            float[] reliefWidthsZ = new float[] { 6.2f, 6.8f, 6.2f };
            for (int i = 0; i < reliefCentersZ.Length; i++)
            {
                CreateBox($"BuildingA_UpperReliefPanel_{i}", blockArch.transform,
                    new Vector3(-11.96f, 6.18f, reliefCentersZ[i]), new Vector3(0.06f, 0.92f, reliefWidthsZ[i]), weatheredCreamMat);
            }

            // Horizontal Stringcourse Molding Below Arched Windows (Y = 3.85m)
            CreateBox("BuildingA_ArchSillCourse", blockArch.transform,
                new Vector3(-11.92f, 3.85f, -7.25f), new Vector3(0.18f, 0.10f, 26.6f), darkCorniceMat);

            // 5 Semicircular Arched Lunette Windows on Upper Floor (alpha-clipped semicircular stone arches!)
            CreateArchedLunette("ArchWindow_1_AVenta", blockArch.transform, centerZ: -17.6f, centerY: 4.60f, widthZ: 2.7f, heightY: 1.48f, archWindowMat, darkCorniceMat);
            CreateArchedLunette("ArchWindow_2_Internacional", blockArch.transform, centerZ: -11.9f, centerY: 4.68f, widthZ: 4.6f, heightY: 1.65f, archWindowMat, darkCorniceMat);
            CreateArchedLunette("ArchWindow_3_Pharmacy", blockArch.transform, centerZ: -5.4f, centerY: 4.70f, widthZ: 4.8f, heightY: 1.70f, archWindowMat, darkCorniceMat);
            CreateArchedLunette("ArchWindow_4_Optica", blockArch.transform, centerZ: 0.9f, centerY: 4.74f, widthZ: 3.8f, heightY: 1.75f, archWindowMat, darkCorniceMat);
            CreateArchedLunette("ArchWindow_5_SantaFe", blockArch.transform, centerZ: 4.65f, centerY: 4.64f, widthZ: 1.85f, heightY: 1.52f, archWindowMat, darkCorniceMat);

            // --- Bay 1 (Far Left, Z = -20.2m to -15.1m, center Z = -17.65m): "A VENTA" ---
            CreateBox("Bay1_StorefrontGlass", blockArch.transform,
                new Vector3(-11.96f, 1.32f, -17.65f), new Vector3(0.08f, 2.25f, 4.2f), darkGlassMat);
            // Aluminum Door & Window Mullions
            float[] bay1Mullions = new float[] { -19.65f, -18.35f, -16.95f, -15.65f };
            for (int i = 0; i < bay1Mullions.Length; i++)
            {
                CreateBox($"Bay1_Mullion_{i}", blockArch.transform,
                    new Vector3(-11.93f, 1.32f, bay1Mullions[i]), new Vector3(0.12f, 2.25f, 0.08f), aluminumMat);
            }
            CreateBox("Bay1_LowerKickplate", blockArch.transform,
                new Vector3(-11.94f, 0.36f, -17.65f), new Vector3(0.10f, 0.32f, 4.2f), aluminumMat);
            // Glowing Green "A VENTA" Sign clearly visible right below the Bay 1 Awning valance!
            CreateWestSign("Sign_AVenta", blockArch.transform,
                centerZ: -17.65f, centerY: 2.34f, widthZ: 3.7f, heightY: 0.46f, darkZocaloMat, signAVentaMat, frontOffsetX: -11.35f);
            // Beige Wedge Awning for Bay 1
            CreateWestWedgeAwning("Awning_Bay1_AVenta", blockArch.transform,
                centerZ: -17.65f, topY: 3.24f, widthZ: 4.4f, projectionX: 1.18f, dropY: 0.54f, beigeAwningMat, awningValanceMat);

            // Dividing Pilaster between Bay 1 & Bay 2 (Z = -15.1m)
            CreateBox("Pilaster_Bay1_2", blockArch.transform,
                new Vector3(-11.93f, 1.9f, -15.1f), new Vector3(0.16f, 3.4f, 0.38f), creamStuccoMat);
            CreateBox("Pilaster_Bay1_2_Base", blockArch.transform,
                new Vector3(-11.91f, 0.50f, -15.1f), new Vector3(0.18f, 0.60f, 0.42f), darkZocaloMat);

            // --- Bay 2 (Mid-Left, Z = -14.9m to -8.8m, center Z = -11.85m): "Curiosidades Internacional" ---
            CreateBox("Bay2_BackingFrame", blockArch.transform,
                new Vector3(-11.96f, 1.40f, -11.85f), new Vector3(0.08f, 2.40f, 5.4f), darkGlassMat);
            float[] yellowPanelZs = new float[] { -13.65f, -12.35f, -10.95f };
            for (int i = 0; i < yellowPanelZs.Length; i++)
            {
                CreateBox($"Bay2_YellowDoorPanel_{i}", blockArch.transform,
                    new Vector3(-11.94f, 1.28f, yellowPanelZs[i]), new Vector3(0.10f, 2.15f, 1.05f), yellowPanelMat);
            }
            CreateBox("Bay2_RightGlassPane", blockArch.transform,
                new Vector3(-11.94f, 1.28f, -9.75f), new Vector3(0.09f, 2.15f, 0.95f), darkGlassMat);
            CreateBox("Bay2_ZocaloBase", blockArch.transform,
                new Vector3(-11.92f, 0.32f, -9.75f), new Vector3(0.12f, 0.26f, 1.1f), darkZocaloMat);
            // Beige Wedge Awning
            CreateWestWedgeAwning("Awning_Bay2_Internacional", blockArch.transform,
                centerZ: -11.85f, topY: 3.16f, widthZ: 5.7f, projectionX: 1.35f, dropY: 0.72f, beigeAwningMat, awningValanceMat);
            // Terracotta Sign "Curiosidades Internacional"
            CreateWestSign("Sign_Internacional", blockArch.transform,
                centerZ: -11.85f, centerY: 3.48f, widthZ: 4.2f, heightY: 0.62f, terracottaPlaqueMat, signInternacionalMat);

            // Dividing Pilaster between Bay 2 & Bay 3 (Z = -8.75m)
            CreateBox("Pilaster_Bay2_3", blockArch.transform,
                new Vector3(-11.93f, 1.9f, -8.75f), new Vector3(0.16f, 3.4f, 0.40f), creamStuccoMat);
            CreateBox("Pilaster_Bay2_3_Base", blockArch.transform,
                new Vector3(-11.91f, 0.50f, -8.75f), new Vector3(0.18f, 0.60f, 0.44f), darkZocaloMat);

            // --- Bay 3 (Center-Left, Z = -8.5m to -2.3m, center Z = -5.4m): "Farma | PHARMACY" ---
            CreateBox("Bay3_OpenInteriorRecess", blockArch.transform,
                new Vector3(-12.35f, 1.42f, -4.1f), new Vector3(0.8f, 2.45f, 3.2f), interiorDarkMat);
            CreateBox("Bay3_InteriorCounter", blockArch.transform,
                new Vector3(-12.25f, 0.68f, -3.9f), new Vector3(0.45f, 0.95f, 2.2f), terracottaPlaqueMat);
            // Left Window with Large Yellow Promotional Poster (Z = -8.3m to -5.6m)
            CreateBox("Bay3_LeftWindowGlass", blockArch.transform,
                new Vector3(-11.96f, 1.42f, -6.95f), new Vector3(0.08f, 2.40f, 2.6f), darkGlassMat);
            GameObject pharmPoster = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pharmPoster.name = "Bay3_YellowPromotionalPoster";
            pharmPoster.transform.SetParent(blockArch.transform);
            pharmPoster.transform.localPosition = new Vector3(-11.89f, 1.38f, -6.95f);
            pharmPoster.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Faces East (+X)
            pharmPoster.transform.localScale = new Vector3(1.55f, 1.65f, 1.0f);
            SetMaterial(pharmPoster, posterPharmacyMat);
            UnityEngine.Object.DestroyImmediate(pharmPoster.GetComponent<Collider>());
            // Beige Wedge Awning
            CreateWestWedgeAwning("Awning_Bay3_Pharmacy", blockArch.transform,
                centerZ: -5.4f, topY: 3.16f, widthZ: 5.9f, projectionX: 1.38f, dropY: 0.74f, beigeAwningMat, awningValanceMat);
            // Two-Tone Terracotta & Bright Yellow Sign "Farma | PHARMACY"
            CreateWestSign("Sign_Pharmacy", blockArch.transform,
                centerZ: -5.4f, centerY: 3.52f, widthZ: 4.1f, heightY: 0.68f, terracottaPlaqueMat, signPharmacyMat);

            // Dividing Pilaster between Bay 3 & Bay 4 (Z = -2.15m)
            CreateBox("Pilaster_Bay3_4", blockArch.transform,
                new Vector3(-11.92f, 1.9f, -2.15f), new Vector3(0.18f, 3.4f, 0.55f), creamStuccoMat);
            CreateBox("Pilaster_Bay3_4_Base", blockArch.transform,
                new Vector3(-11.90f, 0.62f, -2.15f), new Vector3(0.20f, 0.85f, 0.58f), darkZocaloMat);

            // --- Bay 4 (Center, Z = -1.8m to +3.4m, center Z = +0.85m): "Óptica Fashion" (Valeria Domínguez at Z = +1.5m) ---
            CreateBox("Optica_PortalRecess", blockArch.transform,
                new Vector3(-12.25f, 1.42f, 0.85f), new Vector3(0.55f, 2.45f, 3.8f), interiorDarkMat);
            // Iconic Turquoise/Teal-Green Lower Door & Booth Panels
            CreateBox("Optica_TealPanel_Left", blockArch.transform,
                new Vector3(-11.98f, 0.85f, -0.20f), new Vector3(0.12f, 1.30f, 1.25f), opticaTealMat);
            CreateBox("Optica_TealPanel_Right", blockArch.transform,
                new Vector3(-11.98f, 0.72f, 1.90f), new Vector3(0.12f, 1.05f, 1.25f), opticaTealMat);
            CreateBox("Optica_DisplayGlass_Left", blockArch.transform,
                new Vector3(-11.98f, 1.95f, -0.20f), new Vector3(0.08f, 0.95f, 1.25f), darkGlassMat);
            CreateBox("Optica_DisplayGlass_Right", blockArch.transform,
                new Vector3(-11.98f, 1.85f, 1.90f), new Vector3(0.08f, 1.15f, 1.25f), darkGlassMat);
            // Beige Wedge Awning ("Optica_Awning")
            CreateWestWedgeAwning("Optica_Awning", opticaRoot.transform,
                centerZ: 0.85f, topY: 3.22f, widthZ: 4.8f, projectionX: 1.35f, dropY: 0.74f, beigeAwningMat, awningValanceMat);
            // Terracotta Sign "Óptica Fashion" ("Optica_Signboard")
            CreateWestSign("Optica_Signboard", opticaRoot.transform,
                centerZ: 0.85f, centerY: 3.58f, widthZ: 4.3f, heightY: 0.68f, terracottaPlaqueMat, signOpticaMat);

            // Diagonal Projecting Mexican Flag above right corner of Óptica Fashion (Z = +3.15m, Y = 3.95m)
            CreateMexicanFlag("Optica_MexicanFlag", blockArch.transform,
                new Vector3(-11.82f, 3.95f, 3.15f), aluminumMat, flagGreenMat, whiteFrameMat, santaFeRedMat);

            // Dividing Pilaster between Bay 4 (Óptica) and Bay 5 (Santa Fe Red Booth) at Z = +3.4m
            CreateBox("Pilaster_Bay4_5", blockArch.transform,
                new Vector3(-11.91f, 1.9f, 3.4f), new Vector3(0.20f, 3.4f, 0.60f), creamStuccoMat);
            CreateBox("Pilaster_Bay4_5_Base", blockArch.transform,
                new Vector3(-11.89f, 0.62f, 3.4f), new Vector3(0.22f, 0.85f, 0.64f), darkZocaloMat);

            // --- Bay 5 (Transition Bay, Z = +3.7m to +5.9m, center Z = +4.8m): "Alimentos Santa Fe" & Red Casetilla ---
            CreateBox("SantaFe_RedCasetillaBase", blockArch.transform,
                new Vector3(-11.92f, 0.81f, 4.8f), new Vector3(0.20f, 1.22f, 2.0f), santaFeRedMat);
            CreateBox("SantaFe_RedCounterLip", blockArch.transform,
                new Vector3(-11.78f, 1.42f, 4.8f), new Vector3(0.32f, 0.08f, 2.1f), santaFeRedMat);
            CreateBox("SantaFe_UpperWhiteWall", blockArch.transform,
                new Vector3(-11.95f, 2.05f, 4.8f), new Vector3(0.12f, 1.20f, 2.0f), whiteFrameMat);
            CreateBox("SantaFe_ServiceWindow", blockArch.transform,
                new Vector3(-11.88f, 1.92f, 4.8f), new Vector3(0.08f, 0.68f, 0.88f), darkGlassMat);
            CreateWestWedgeAwning("Awning_Bay5_SantaFe", blockArch.transform,
                centerZ: 4.8f, topY: 3.28f, widthZ: 2.3f, projectionX: 1.18f, dropY: 0.64f, beigeAwningMat, awningValanceMat);
            CreateWestSign("Sign_SantaFe", blockArch.transform,
                centerZ: 4.8f, centerY: 3.68f, widthZ: 2.25f, heightY: 0.64f, terracottaPlaqueMat, signSantaFeMat);

            // -------------------------------------------------------------------------
            // BUILDING B: 3-STORY HISTORIC CRENELLATED BUILDING ("Bar La Cucaracha" & "Taqueria El Trompo")
            // Spans Z = +6.0m to +17.2m (Length = 11.2m, Center Z = +11.6m, Height = 8.6m)
            // -------------------------------------------------------------------------
            CreateBox("BuildingB_CucarachaMass", blockArch.transform,
                new Vector3(-18.0f, 4.3f, 11.6f), new Vector3(12.0f, 8.6f, 11.2f), rusticStuccoMat, keepCollider: true);

            // Top Crenellated Castle Parapet (Almenas, Y = 8.55m to 9.05m)
            CreateBox("BuildingB_ParapetTrim", blockArch.transform,
                new Vector3(-11.90f, 8.55f, 11.6f), new Vector3(0.24f, 0.14f, 11.3f), stoneMasonryMat);
            int merlonCount = 10;
            for (int i = 0; i < merlonCount; i++)
            {
                float merlonZ = 6.4f + i * 1.12f;
                CreateBox($"BuildingB_Merlon_{i}", blockArch.transform,
                    new Vector3(-11.92f, 8.85f, merlonZ), new Vector3(0.22f, 0.45f, 0.60f), rusticStuccoMat);
                CreateBox($"BuildingB_MerlonCap_{i}", blockArch.transform,
                    new Vector3(-11.90f, 9.08f, merlonZ), new Vector3(0.26f, 0.06f, 0.64f), stoneMasonryMat);
            }

            // 3rd Floor Rustic Stone Masonry Band & Casement Windows (Y = 6.2m to 7.65m, Z = +7.5m to +15.7m)
            CreateBox("BuildingB_StoneMasonryBand", blockArch.transform,
                new Vector3(-11.95f, 6.92f, 11.6f), new Vector3(0.12f, 1.48f, 8.2f), stoneMasonryMat);
            // Left 3rd-Floor Window (Z = +8.65m)
            CreateBox("BuildingB_Floor3_WinLeft", blockArch.transform,
                new Vector3(-11.88f, 6.92f, 8.65f), new Vector3(0.08f, 1.38f, 2.0f), darkGlassMat);
            CreateBox("BuildingB_Floor3_WinLeft_MullionV", blockArch.transform,
                new Vector3(-11.85f, 6.92f, 8.65f), new Vector3(0.08f, 1.38f, 0.06f), aluminumMat);
            CreateBox("BuildingB_Floor3_WinLeft_MullionH", blockArch.transform,
                new Vector3(-11.85f, 7.22f, 8.65f), new Vector3(0.08f, 0.06f, 2.0f), aluminumMat);
            // Center Small 3rd-Floor Vent Window (Z = +11.6m)
            CreateBox("BuildingB_Floor3_WinMid", blockArch.transform,
                new Vector3(-11.88f, 7.12f, 11.6f), new Vector3(0.08f, 0.72f, 0.85f), darkGlassMat);
            // Right 3rd-Floor Window (Z = +14.5m)
            CreateBox("BuildingB_Floor3_WinRight", blockArch.transform,
                new Vector3(-11.88f, 6.92f, 14.5f), new Vector3(0.08f, 1.38f, 2.1f), darkGlassMat);
            CreateBox("BuildingB_Floor3_WinRight_MullionV", blockArch.transform,
                new Vector3(-11.85f, 6.92f, 14.5f), new Vector3(0.08f, 1.38f, 0.06f), aluminumMat);
            CreateBox("BuildingB_Floor3_WinRight_MullionH", blockArch.transform,
                new Vector3(-11.85f, 7.22f, 14.5f), new Vector3(0.08f, 0.06f, 2.1f), aluminumMat);

            // Horizontal Mid-Facade Cornice Ledge above 2nd Floor Ribbon Windows (Y = 5.58m)
            CreateBox("BuildingB_MidCorniceLedge", blockArch.transform,
                new Vector3(-11.88f, 5.58f, 11.6f), new Vector3(0.26f, 0.14f, 11.2f), darkCorniceMat);

            // 2nd Floor Continuous Multi-Pane Window Ribbon with White Security Grilles (Y = 4.82m, Z = +7.3m to +15.9m)
            GameObject ribbonQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ribbonQuad.name = "BuildingB_Floor2_RibbonWindows";
            ribbonQuad.transform.SetParent(blockArch.transform);
            ribbonQuad.transform.localPosition = new Vector3(-11.92f, 4.82f, 11.6f);
            ribbonQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Faces East (+X)
            ribbonQuad.transform.localScale = new Vector3(8.6f, 1.12f, 1.0f);
            SetMaterial(ribbonQuad, ribbonWindowsMat);
            UnityEngine.Object.DestroyImmediate(ribbonQuad.GetComponent<Collider>());

            // --- Bay 6 (Z = +6.1m to +11.4m, center Z = +8.75m): "Bar La Cucaracha" ---
            GameObject shutterQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shutterQuad.name = "LaCucaracha_RollUpShutter";
            shutterQuad.transform.SetParent(blockArch.transform);
            shutterQuad.transform.localPosition = new Vector3(-11.92f, 1.42f, 8.75f);
            shutterQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Faces East (+X)
            shutterQuad.transform.localScale = new Vector3(3.65f, 2.45f, 1.0f);
            SetMaterial(shutterQuad, metalShutterMat);
            UnityEngine.Object.DestroyImmediate(shutterQuad.GetComponent<Collider>());
            CreateBox("LaCucaracha_Pillar_Left", blockArch.transform,
                new Vector3(-11.94f, 1.45f, 6.50f), new Vector3(0.14f, 2.55f, 0.85f), whiteFrameMat);
            CreateBox("LaCucaracha_Pillar_Right", blockArch.transform,
                new Vector3(-11.94f, 1.45f, 11.00f), new Vector3(0.14f, 2.55f, 0.85f), whiteFrameMat);
            CreateWestWedgeAwning("Awning_Bay6_LaCucaracha", blockArch.transform,
                centerZ: 8.75f, topY: 3.16f, widthZ: 4.5f, projectionX: 1.25f, dropY: 0.68f, beigeAwningMat, awningValanceMat);
            CreateWestSign("Sign_LaCucaracha", blockArch.transform,
                centerZ: 8.75f, centerY: 3.56f, widthZ: 4.4f, heightY: 0.68f, terracottaPlaqueMat, signCucarachaMat);

            // --- Bay 7 (Z = +11.4m to +17.0m, center Z = +14.2m): "Taqueria El Trompo" ---
            CreateBox("ElTrompo_PortalRecess", blockArch.transform,
                new Vector3(-12.35f, 1.42f, 13.5f), new Vector3(0.8f, 2.45f, 2.6f), interiorDarkMat);
            CreateBox("ElTrompo_WoodDoorFrame", blockArch.transform,
                new Vector3(-12.10f, 1.40f, 14.4f), new Vector3(0.15f, 2.40f, 0.85f), woodDoorMat);
            CreateBox("ElTrompo_BrickWall_Right", blockArch.transform,
                new Vector3(-11.94f, 1.42f, 15.45f), new Vector3(0.14f, 2.45f, 1.75f), exposedBrickMat);
            CreateBox("ElTrompo_BrickWindow", blockArch.transform,
                new Vector3(-11.88f, 1.68f, 15.45f), new Vector3(0.08f, 0.85f, 0.72f), darkGlassMat);
            CreateBox("ElTrompo_BrickJamb_Left", blockArch.transform,
                new Vector3(-11.94f, 1.42f, 12.00f), new Vector3(0.14f, 2.45f, 0.55f), exposedBrickMat);
            CreateWestWedgeAwning("Awning_Bay7_TaqueriaElTrompo", blockArch.transform,
                centerZ: 14.2f, topY: 3.16f, widthZ: 4.8f, projectionX: 1.25f, dropY: 0.68f, beigeAwningMat, awningValanceMat);
            CreateWestSign("Sign_TaqueriaElTrompo", blockArch.transform,
                centerZ: 14.2f, centerY: 3.56f, widthZ: 4.6f, heightY: 0.68f, terracottaPlaqueMat, signTrompoMat);

            // -------------------------------------------------------------------------
            // BUILDING C: AUTHENTIC CORNER CLINIC WITH 45° CHAMFER & CANTILEVERED CANOPY — "Dental Jarquín"
            // Spans Z = +16.8m to +21.8m on Av. Juárez, 45° chamfer to Gardenias, and North wing along Gardenias (X = -13.5m to -24.0m)
            // -------------------------------------------------------------------------

            // 1. Historic Upper Mass (Taller rustic stepped background building, Y = 4.2m to 8.4m, recessed)
            CreateBox("BuildingC_HistoricUpperMass", blockArch.transform,
                new Vector3(-22.5f, 6.30f, 18.8f), new Vector3(19.0f, 4.20f, 4.4f), rusticStuccoMat, keepCollider: true);
            CreateBox("BuildingC_HistoricTopCoping", blockArch.transform,
                new Vector3(-22.50f, 8.42f, 18.8f), new Vector3(19.2f, 0.16f, 4.5f), darkCorniceMat);

            // 2. Ground Floor Commercial Body of Dental Jarquín (Single story, Y = 0 to 4.20m)
            // Main rectangular body extending west along Calle Gardenias to X = -32.0m
            CreateBox("Dental_MainBody", blockArch.transform,
                new Vector3(-22.50f, 2.10f, 19.30f), new Vector3(19.0f, 4.20f, 4.60f), creamStuccoMat, keepCollider: true);

            // East Facade facing Av. Benito Juárez (X = -12.00m, Z in [16.8m, 20.3m])
            // Dark concrete zócalo at base
            CreateBox("Dental_East_Zocalo", blockArch.transform,
                new Vector3(-11.94f, 0.30f, 18.60f), new Vector3(0.14f, 0.60f, 3.60f), darkZocaloMat);
            // Commercial entrance with framed roll-up metal shutter (cortina metálica)
            GameObject dentalShutter = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dentalShutter.name = "Dental_East_RollUpShutter";
            dentalShutter.transform.SetParent(blockArch.transform);
            dentalShutter.transform.localPosition = new Vector3(-11.92f, 2.05f, 18.55f);
            dentalShutter.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Faces East (+X)
            dentalShutter.transform.localScale = new Vector3(2.60f, 2.60f, 1.0f);
            SetMaterial(dentalShutter, metalShutterMat);
            UnityEngine.Object.DestroyImmediate(dentalShutter.GetComponent<Collider>());

            // Dark frame surrounding the shutter
            CreateBox("Dental_East_ShutterFrame_Top", blockArch.transform,
                new Vector3(-11.90f, 3.40f, 18.55f), new Vector3(0.18f, 0.16f, 2.80f), darkCorniceMat);
            CreateBox("Dental_East_ShutterFrame_L", blockArch.transform,
                new Vector3(-11.90f, 2.05f, 17.20f), new Vector3(0.18f, 2.60f, 0.16f), darkCorniceMat);
            CreateBox("Dental_East_ShutterFrame_R", blockArch.transform,
                new Vector3(-11.90f, 2.05f, 19.90f), new Vector3(0.18f, 2.60f, 0.16f), darkCorniceMat);
            // Upper fascia above shutter
            CreateBox("Dental_East_UpperFascia", blockArch.transform,
                new Vector3(-11.94f, 3.82f, 18.60f), new Vector3(0.14f, 0.76f, 3.60f), creamStuccoMat);

            // 3. Authentic 45° Chamfered Corner (Ochave) facing the intersection
            // Connects (X=-12.00, Z=20.30) to (X=-13.50, Z=21.80)
            // Center: (-12.75f, 2.10f, 21.05f), rotation: -45° (normal points outward to +X/+Z intersection)
            GameObject chamferRoot = new GameObject("Dental_ChamferCorner");
            chamferRoot.transform.SetParent(blockArch.transform);
            chamferRoot.transform.localPosition = new Vector3(-12.75f, 2.10f, 21.05f);
            chamferRoot.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);

            // Chamfer wall mass
            CreateBox("Dental_Chamfer_Wall", chamferRoot.transform,
                Vector3.zero, new Vector3(0.20f, 4.20f, 2.00f), creamStuccoMat, keepCollider: true);

            // Chamfer zócalo at base
            CreateBox("Dental_Chamfer_Zocalo", chamferRoot.transform,
                new Vector3(0.04f, -1.80f, 0f), new Vector3(0.14f, 0.60f, 2.00f), darkZocaloMat);

            // Chamfer commercial display panel / lower storefront
            CreateBox("Dental_Chamfer_DisplayRecess", chamferRoot.transform,
                new Vector3(0.05f, -0.40f, 0f), new Vector3(0.10f, 1.90f, 1.15f), darkGlassMat);
            CreateBox("Dental_Chamfer_DisplayFrame", chamferRoot.transform,
                new Vector3(0.08f, -0.40f, 0f), new Vector3(0.06f, 2.00f, 1.20f), whiteFrameMat);

            // Official Commercial Sign "Dental Jarquín" mounted on Chamfer Fascia (Y = 3.65m)
            CreateAngledSign("Sign_DentalJarquin", chamferRoot.transform,
                localCenterY: 1.55f, widthZ: 1.30f, heightY: 0.52f, terracottaPlaqueMat, signDentalMat);

            // 4. North Facade along Calle Gardenias (Z = 21.80m, X in [-13.5m, -32.0m])
            // Dark concrete zócalo along Gardenias
            CreateBox("Dental_North_Zocalo", blockArch.transform,
                new Vector3(-22.75f, 0.30f, 21.72f), new Vector3(18.50f, 0.60f, 0.14f), darkZocaloMat);
            // Low antepecho under ribbon window
            CreateBox("Dental_North_Antepecho", blockArch.transform,
                new Vector3(-22.75f, 0.90f, 21.72f), new Vector3(18.50f, 0.60f, 0.14f), creamStuccoMat);

            // Continuous Horizontal Ribbon Window (Ventanal Corrido) facing Calle Gardenias
            // Spans X = -14.0m to -29.0m (Length = 15.0m, Height = 1.45m from Y = 1.20m to 2.65m)
            GameObject northGlass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            northGlass.name = "Dental_North_RibbonWindow";
            northGlass.transform.SetParent(blockArch.transform);
            northGlass.transform.localPosition = new Vector3(-21.50f, 1.92f, 21.74f);
            northGlass.transform.localRotation = Quaternion.identity; // Faces North (+Z) toward Calle Gardenias
            northGlass.transform.localScale = new Vector3(15.00f, 1.45f, 1.0f);
            SetMaterial(northGlass, darkGlassMat);
            UnityEngine.Object.DestroyImmediate(northGlass.GetComponent<Collider>());

            // Dark window frame and mullions along the ribbon window
            CreateBox("Dental_North_WindowSill", blockArch.transform,
                new Vector3(-21.50f, 1.18f, 21.76f), new Vector3(15.10f, 0.08f, 0.12f), darkCorniceMat);
            CreateBox("Dental_North_WindowLintel", blockArch.transform,
                new Vector3(-21.50f, 2.68f, 21.76f), new Vector3(15.10f, 0.08f, 0.12f), darkCorniceMat);
            for (int m = 0; m <= 10; m++)
            {
                float mullionX = -14.0f - m * 1.50f;
                CreateBox($"Dental_North_Mullion_{m}", blockArch.transform,
                    new Vector3(mullionX, 1.92f, 21.76f), new Vector3(0.08f, 1.45f, 0.12f), darkCorniceMat);
            }

            // Upper fascia above ribbon window
            CreateBox("Dental_North_UpperFascia", blockArch.transform,
                new Vector3(-22.75f, 3.45f, 21.72f), new Vector3(18.50f, 1.45f, 0.14f), creamStuccoMat);

            // 5. Iconic Cantilevered Continuous Concrete Canopy (La Gran Marquesina Volada de Concreto)
            // Losa gruesa de concreto de 24cm de espesor a Y = 4.25m con voladizo de 1.35m sobre banqueta
            // Sección Este (Av. Juárez): X in [-12.0m, -10.65m], Z in [16.8m, 20.3m]
            CreateBox("Dental_Canopy_East", blockArch.transform,
                new Vector3(-11.32f, 4.25f, 18.55f), new Vector3(1.35f, 0.24f, 3.50f), weatheredCreamMat);
            // Sección Chaflán (Esquina a 45°):
            CreateBox("Dental_Canopy_Chamfer", chamferRoot.transform,
                new Vector3(0.70f, 2.15f, 0f), new Vector3(1.40f, 0.24f, 2.45f), weatheredCreamMat);
            // Sección Norte (Calle Gardenias): X in [-13.5m, -32.0m], Z in [21.8m, 23.15m]
            CreateBox("Dental_Canopy_North", blockArch.transform,
                new Vector3(-22.75f, 4.25f, 22.47f), new Vector3(18.50f, 0.24f, 1.35f), weatheredCreamMat);

            // Fascia perimetral / gotero oscuro de la marquesina
            CreateBox("Dental_Canopy_East_Fascia", blockArch.transform,
                new Vector3(-10.60f, 4.25f, 18.55f), new Vector3(0.08f, 0.28f, 3.55f), darkCorniceMat);
            CreateBox("Dental_Canopy_Chamfer_Fascia", chamferRoot.transform,
                new Vector3(1.40f, 2.15f, 0f), new Vector3(0.08f, 0.28f, 2.48f), darkCorniceMat);
            CreateBox("Dental_Canopy_North_Fascia", blockArch.transform,
                new Vector3(-22.75f, 4.25f, 23.19f), new Vector3(18.55f, 0.28f, 0.08f), darkCorniceMat);

            // 6. Realistic Roof Accessories (Antena Parabólica y Condensador A/C)
            // Compresor de A/C en la azotea sobre Calle Gardenias
            CreateBox("Dental_Roof_AC_Unit", blockArch.transform,
                new Vector3(-16.50f, 4.70f, 20.80f), new Vector3(0.95f, 0.65f, 0.65f), aluminumMat);
            CreateBox("Dental_Roof_AC_FanGrille", blockArch.transform,
                new Vector3(-16.50f, 4.70f, 21.14f), new Vector3(0.60f, 0.50f, 0.04f), darkCorniceMat);

            // Antena parabólica (Dish) en la azotea
            CreateRoofSatelliteDish("Dental_Roof_SatelliteDish", blockArch.transform,
                new Vector3(-18.50f, 4.40f, 20.50f), aluminumMat, darkCorniceMat);

            // Move West Sidewalk legacy streetlamps to the North & South ends of the block
            // so all 8 historic bays are 100% unobstructed in gameplay and screenshots
            GameObject lampW1 = GameObject.Find("Streetlight_W1");
            if (lampW1 != null) lampW1.transform.position = new Vector3(-6.8f, 0f, -22.5f);

            GameObject lampW2 = GameObject.Find("Streetlight_W2");
            if (lampW2 != null) lampW2.transform.position = new Vector3(-7.25f, 0.20f, 34.5f);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log($"[WestSidewalkBlockBuilder] Authentic West Sidewalk Block successfully saved to '{ScenePath}'!");
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

            // Temporarily hide CitizenServiceDesk and East sidewalk streetlamps behind the camera during pure architectural comparison shots 1-3
            // so the user can inspect all 8 storefronts 1:1 against the reference photograph, then restore for Shot 4!
            GameObject citizenDesk = GameObject.Find("CitizenServiceDesk");
            GameObject eastCenterLamp = GameObject.Find("Streetlight_East_BenavidesCenter");
            GameObject eastLamp1 = GameObject.Find("Streetlight_E1");
            GameObject eastLamp2 = GameObject.Find("Streetlight_E2");
            GameObject westNorthLamp = GameObject.Find("Streetlight_West_NorthCorner");

            bool deskWasActive = citizenDesk != null && citizenDesk.activeSelf;
            bool eastCenterWasActive = eastCenterLamp != null && eastCenterLamp.activeSelf;
            bool e1WasActive = eastLamp1 != null && eastLamp1.activeSelf;
            bool e2WasActive = eastLamp2 != null && eastLamp2.activeSelf;
            bool westNorthWasActive = westNorthLamp != null && westNorthLamp.activeSelf;

            if (citizenDesk != null) citizenDesk.SetActive(false);
            if (eastCenterLamp != null) eastCenterLamp.SetActive(false);
            if (eastLamp1 != null) eastLamp1.SetActive(false);
            if (eastLamp2 != null) eastLamp2.SetActive(false);
            if (westNorthLamp != null) westNorthLamp.SetActive(false);

            GameObject camGo = new GameObject("WestBlockVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Shot 1: Wide Panoramic View from across the street (all 8 bays from A VENTA to Dental Juárez!)
            cam.fieldOfView = 62f;
            cam.transform.position = new Vector3(10.2f, 2.45f, 0.2f);
            cam.transform.rotation = Quaternion.Euler(-4.0f, -90f, 0f); // Facing directly West (-X)
            // Warm-up render pass so URP shader variants are 100% compiled before saving Shot 1
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "west_block_panoramic.png"));
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "west_block_panoramic.png"));

            // Shot 2: Close-Up of Building A (5 Arched Lunettes, A VENTA, Internacional, PHARMACY, Óptica Fashion + Valeria, Santa Fe)
            cam.fieldOfView = 52f;
            cam.transform.position = new Vector3(3.8f, 2.2f, -5.8f);
            cam.transform.rotation = Quaternion.Euler(-4.5f, -90f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "west_block_left_arches_optica.png"));

            // Shot 3: Close-Up of Building B & C (Óptica Fashion, Santa Fe Red Booth, Bar La Cucaracha, Taquería El Trompo, Dental Jarquín)
            cam.fieldOfView = 52f;
            cam.transform.position = new Vector3(3.8f, 2.2f, 9.8f);
            cam.transform.rotation = Quaternion.Euler(-5.0f, -90f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "west_block_right_cucaracha_trompo.png"));

            // Shot 5: Dedicated Architectural Close-Up of Dental Jarquín (45° Chamfer, Sign, Cantilevered Canopy, Ribbon Window)
            cam.fieldOfView = 50f;
            cam.transform.position = new Vector3(-8.50f, 2.40f, 25.30f);
            cam.transform.rotation = Quaternion.Euler(-2.0f, -135f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "west_block_dental_jarquin_closeup.png"));

            // Restore CitizenServiceDesk & East streetlamps and capture Shot 4 showing full corridor integration
            if (citizenDesk != null) citizenDesk.SetActive(deskWasActive);

            cam.fieldOfView = 60f;
            cam.transform.position = new Vector3(9.5f, 2.35f, 2.0f);
            cam.transform.rotation = Quaternion.Euler(-4.0f, -90f, 0f);
            RenderToFile(cam, 1920, 1080, Path.Combine(outDir, "west_block_full_corridor_context.png"));

            if (eastCenterLamp != null) eastCenterLamp.SetActive(eastCenterWasActive);
            if (eastLamp1 != null) eastLamp1.SetActive(e1WasActive);
            if (eastLamp2 != null) eastLamp2.SetActive(e2WasActive);
            if (westNorthLamp != null) westNorthLamp.SetActive(westNorthWasActive);

            UnityEngine.Object.DestroyImmediate(camGo);
            Debug.Log("[WestSidewalkBlockBuilder] Verification screenshots generated successfully!");
        }

        #region Architectural Helper Builders

        private static GameObject CreateBox(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat, bool keepCollider = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
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

        private static void CreateArchedLunette(string name, Transform parent, float centerZ, float centerY, float widthZ, float heightY, Material archTexMat, Material sillMat)
        {
            GameObject archRoot = new GameObject(name);
            archRoot.transform.SetParent(parent);
            archRoot.transform.localPosition = new Vector3(-11.96f, centerY, centerZ);

            // Textured Semicircular Arched Lunette Quad facing East (+X) with alpha clipping outside arch
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Arch_FaceQuad";
            quad.transform.SetParent(archRoot.transform);
            quad.transform.localPosition = new Vector3(0.02f, 0f, 0f);
            quad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Normal points +X (East)
            quad.transform.localScale = new Vector3(widthZ, heightY, 1.0f);
            SetMaterial(quad, archTexMat);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());

            // 3D Protruding Stone Sill at Base of Arch
            CreateBox("Arch_StoneSill", archRoot.transform,
                new Vector3(0.04f, -heightY * 0.48f, 0f), new Vector3(0.12f, 0.10f, widthZ + 0.16f), sillMat);
        }

        private static void CreateWestWedgeAwning(string name, Transform parent, float centerZ, float topY, float widthZ, float projectionX, float dropY, Material canvasMat, Material valanceMat)
        {
            GameObject awningRoot = new GameObject(name);
            awningRoot.transform.SetParent(parent);
            awningRoot.transform.localPosition = new Vector3(-12.0f, topY, centerZ);

            float slopeLen = Mathf.Sqrt(projectionX * projectionX + dropY * dropY);
            float angleDeg = -Mathf.Atan2(dropY, projectionX) * Mathf.Rad2Deg;

            GameObject slopePanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slopePanel.name = "Canvas_Slope";
            slopePanel.transform.SetParent(awningRoot.transform);
            slopePanel.transform.localPosition = new Vector3(projectionX * 0.5f, -dropY * 0.5f, 0f);
            slopePanel.transform.localRotation = Quaternion.Euler(0f, 0f, angleDeg);
            slopePanel.transform.localScale = new Vector3(slopeLen, 0.05f, widthZ);
            SetMaterial(slopePanel, canvasMat);
            UnityEngine.Object.DestroyImmediate(slopePanel.GetComponent<Collider>());

            GameObject valance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            valance.name = "Canvas_Valance";
            valance.transform.SetParent(awningRoot.transform);
            valance.transform.localPosition = new Vector3(projectionX - 0.02f, -dropY - 0.09f, 0f);
            valance.transform.localScale = new Vector3(0.04f, 0.18f, widthZ);
            SetMaterial(valance, valanceMat);
            UnityEngine.Object.DestroyImmediate(valance.GetComponent<Collider>());

            float[] sideZs = new float[] { -widthZ * 0.5f + 0.02f, widthZ * 0.5f - 0.02f };
            for (int i = 0; i < sideZs.Length; i++)
            {
                GameObject sideCheek = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sideCheek.name = $"Canvas_Side_{i}";
                sideCheek.transform.SetParent(awningRoot.transform);
                sideCheek.transform.localPosition = new Vector3(projectionX * 0.42f, -dropY * 0.62f, sideZs[i]);
                sideCheek.transform.localScale = new Vector3(projectionX * 0.82f, dropY * 0.72f, 0.04f);
                SetMaterial(sideCheek, valanceMat);
                UnityEngine.Object.DestroyImmediate(sideCheek.GetComponent<Collider>());
            }
        }

        private static void CreateWestSign(string name, Transform parent, float centerZ, float centerY, float widthZ, float heightY, Material plaqueMat, Material faceTexMat, float frontOffsetX = -11.90f)
        {
            GameObject signRoot = new GameObject(name);
            signRoot.transform.SetParent(parent);
            signRoot.transform.localPosition = new Vector3(frontOffsetX, centerY, centerZ);

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Sign_BackingPlaque";
            box.transform.SetParent(signRoot.transform);
            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = new Vector3(0.14f, heightY, widthZ);
            SetMaterial(box, plaqueMat);
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sign_FrontFace";
            quad.transform.SetParent(signRoot.transform);
            quad.transform.localPosition = new Vector3(0.075f, 0f, 0f);
            quad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Normal points +X (East)
            quad.transform.localScale = new Vector3(widthZ - 0.04f, heightY - 0.04f, 1.0f);
            SetMaterial(quad, faceTexMat);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        private static void CreateMexicanFlag(string name, Transform parent, Vector3 mountPos, Material poleMat, Material greenMat, Material whiteMat, Material redMat)
        {
            GameObject flagRoot = new GameObject(name);
            flagRoot.transform.SetParent(parent);
            flagRoot.transform.localPosition = mountPos;
            flagRoot.transform.localRotation = Quaternion.Euler(-25f, 35f, -35f);

            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Flag_Pole";
            pole.transform.SetParent(flagRoot.transform);
            pole.transform.localPosition = new Vector3(0.35f, 0.35f, 0f);
            pole.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            pole.transform.localScale = new Vector3(0.03f, 0.55f, 0.03f);
            SetMaterial(pole, poleMat);
            UnityEngine.Object.DestroyImmediate(pole.GetComponent<Collider>());

            CreateBox("Flag_Green", flagRoot.transform, new Vector3(0.45f, 0.32f, -0.14f), new Vector3(0.03f, 0.34f, 0.16f), greenMat);
            CreateBox("Flag_White", flagRoot.transform, new Vector3(0.52f, 0.36f, 0.0f), new Vector3(0.03f, 0.34f, 0.16f), whiteMat);
            CreateBox("Flag_Red", flagRoot.transform, new Vector3(0.59f, 0.40f, 0.14f), new Vector3(0.03f, 0.34f, 0.16f), redMat);
        }

        private static void CreateAngledSign(string name, Transform parent, float localCenterY, float widthZ, float heightY, Material plaqueMat, Material faceTexMat)
        {
            GameObject signRoot = new GameObject(name);
            signRoot.transform.SetParent(parent, false);
            signRoot.transform.localPosition = new Vector3(0.12f, localCenterY, 0f);
            signRoot.transform.localRotation = Quaternion.identity;

            GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Sign_BackingPlaque";
            box.transform.SetParent(signRoot.transform, false);
            box.transform.localPosition = Vector3.zero;
            box.transform.localRotation = Quaternion.identity;
            box.transform.localScale = new Vector3(0.08f, heightY, widthZ);
            SetMaterial(box, plaqueMat);
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Sign_FrontFace";
            quad.transform.SetParent(signRoot.transform, false);
            quad.transform.localPosition = new Vector3(0.045f, 0f, 0f);
            quad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Normal points outward (+X local)
            quad.transform.localScale = new Vector3(widthZ - 0.04f, heightY - 0.04f, 1.0f);
            SetMaterial(quad, faceTexMat);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());
        }

        private static void CreateRoofSatelliteDish(string name, Transform parent, Vector3 localPos, Material metalMat, Material darkMat)
        {
            GameObject dishRoot = new GameObject(name);
            dishRoot.transform.SetParent(parent);
            dishRoot.transform.localPosition = localPos;

            // Mast
            GameObject mast = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mast.name = "Mast";
            mast.transform.SetParent(dishRoot.transform);
            mast.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            mast.transform.localScale = new Vector3(0.06f, 0.35f, 0.06f);
            SetMaterial(mast, darkMat);
            UnityEngine.Object.DestroyImmediate(mast.GetComponent<Collider>());

            // Dish reflector
            GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dish.name = "DishReflector";
            dish.transform.SetParent(dishRoot.transform);
            dish.transform.localPosition = new Vector3(0f, 0.70f, 0.05f);
            dish.transform.localRotation = Quaternion.Euler(-35f, 25f, 0f);
            dish.transform.localScale = new Vector3(0.70f, 0.04f, 0.70f);
            SetMaterial(dish, metalMat);
            UnityEngine.Object.DestroyImmediate(dish.GetComponent<Collider>());

            // LNB Arm
            GameObject arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arm.name = "LNB_Arm";
            arm.transform.SetParent(dishRoot.transform);
            arm.transform.localPosition = new Vector3(0f, 0.60f, 0.28f);
            arm.transform.localRotation = Quaternion.Euler(45f, 25f, 0f);
            arm.transform.localScale = new Vector3(0.02f, 0.22f, 0.02f);
            SetMaterial(arm, darkMat);
            UnityEngine.Object.DestroyImmediate(arm.GetComponent<Collider>());
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

        private static Material CreateOrGetTextureMaterial(string matPath, string texPath, bool alphaClip = false)
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
            if (alphaClip)
            {
                mat.SetFloat("_AlphaClip", 1.0f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                mat.SetFloat("_AlphaClip", 0.0f);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }
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
            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(savePath, bytes);

            cam.targetTexture = null;
            RenderTexture.active = null;
            UnityEngine.Object.DestroyImmediate(rt);
            UnityEngine.Object.DestroyImmediate(tex);

            Debug.Log($"[WestSidewalkBlockBuilder] Saved screenshot: {savePath}");
        }

        #endregion
    }
}
