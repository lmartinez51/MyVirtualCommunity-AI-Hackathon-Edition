using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using MyVirtualCommunity.Client.Api;
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
    ///   4. Bay 4 (Center): "Óptica Fashion" terracotta sign, beige awning, full-height glass Push/Pull doors, dynamic shutter, & Valeria Domínguez
    ///   5. Bay 5 (Transition): "Cambios Santa Fe" terracotta sign, compact beige awning, open red-floor vestibule, armored door, bulletproof window, cash tray, & live hanging LED board
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
            Material rusticStuccoMat = CreateOrGetColorMaterial("Assets/Settings/West_RusticStucco.mat", new Color(0.89f, 0.88f, 0.85f), 0.05f);
            Material darkCorniceMat = CreateOrGetColorMaterial("Assets/Settings/West_DarkCornice.mat", new Color(0.32f, 0.34f, 0.35f), 0.12f);
            Material stoneMasonryMat = CreateOrGetColorMaterial("Assets/Settings/West_StoneMasonry.mat", new Color(0.46f, 0.44f, 0.41f), 0.08f);
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
            Material concreteMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/ConcreteMaterial.mat") ?? weatheredCreamMat;
            Material liveLedBaseMat = CreateOrGetEmissiveLedBaseMaterial("Assets/Settings/West_CambiosSantaFe_LiveLed.mat");
            Material opticaBlackFrameMat = CreateOrGetColorMaterial("Assets/Settings/West_OpticaBlackFrame.mat", new Color(0.09f, 0.10f, 0.11f), 0.38f);
            Material opticaFloorMat = CreateOrGetColorMaterial("Assets/Settings/West_OpticaInteriorFloor.mat", new Color(0.76f, 0.77f, 0.78f), 0.22f);
            Material opticaCounterMat = CreateOrGetColorMaterial("Assets/Settings/West_OpticaCounterGrey.mat", new Color(0.45f, 0.48f, 0.51f), 0.28f);
            Material opticaPinkNoteMat = CreateOrGetColorMaterial("Assets/Settings/West_OpticaPinkNote.mat", new Color(0.95f, 0.24f, 0.52f), 0.10f);
            Material santaFeFloorRedMat = CreateOrGetColorMaterial("Assets/Settings/West_SantaFeFloorRed.mat", new Color(0.56f, 0.11f, 0.09f), 0.16f);
            Material santaFeArmorSteelMat = CreateOrGetColorMaterial("Assets/Settings/West_SantaFeArmorSteel.mat", new Color(0.50f, 0.53f, 0.56f), 0.45f);
            Material clearGlassMat = CreateOrGetTransparentGlassMaterial("Assets/Settings/West_StorefrontClearGlass.mat", new Color(0.24f, 0.34f, 0.40f, 0.32f), 0.90f);
            Material cfeDomeGlassMat = CreateOrGetTransparentGlassMaterial("Assets/Settings/West_CFE_DomeGlass.mat", new Color(0.84f, 0.90f, 0.92f, 0.34f), 0.92f);

            // 2. Setup Texture Materials
            Material signAVentaMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_AVenta.mat", "Assets/Textures/Environment/T_West_Sign_AVenta.png");
            Material signInternacionalMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_Internacional.mat", "Assets/Textures/Environment/T_West_Sign_Internacional.png");
            Material signPharmacyMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_Pharmacy.mat", "Assets/Textures/Environment/T_West_Sign_Pharmacy.png");
            Material posterPharmacyMat = CreateOrGetTextureMaterial("Assets/Settings/West_Poster_Pharmacy.mat", "Assets/Textures/Environment/T_West_Poster_Pharmacy.png");
            Material signOpticaMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_OpticaFashion.mat", "Assets/Textures/Environment/T_West_Sign_OpticaFashion.png");
            Material signSantaFeMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_CambiosSantaFe.mat", "Assets/Textures/Environment/T_West_Sign_CambiosSantaFe.png");
            Material signCucarachaMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_LaCucaracha.mat", "Assets/Textures/Environment/T_West_Sign_LaCucaracha.png");
            Material signTrompoMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_TaqueriaElTrompo.mat", "Assets/Textures/Environment/T_West_Sign_TaqueriaElTrompo.png");
            Material signDentalMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_Dental.mat", "Assets/Textures/Environment/T_West_Sign_Dental.png");
            Material dentalBaseBoxMat = CreateOrGetTextureMaterial("Assets/Settings/West_Dental_BaseBox.mat", "Assets/Textures/Environment/T_West_Dental_BaseBox.png");
            Material archWindowMat = CreateOrGetTextureMaterial("Assets/Settings/West_ArchWindow.mat", "Assets/Textures/Environment/T_West_ArchWindow.png", alphaClip: true);
            Material metalShutterMat = CreateOrGetTextureMaterial("Assets/Settings/West_MetalShutter.mat", "Assets/Textures/Environment/T_West_MetalShutter.png");
            Material ribbonWindowsMat = CreateOrGetTextureMaterial("Assets/Settings/West_RibbonWindows.mat", "Assets/Textures/Environment/T_West_RibbonWindows.png");
            Material openLedMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_OpenLED.mat", "Assets/Textures/Environment/T_West_Sign_OpenLED.png", alphaClip: false, emissive: true);
            Material opticaPosterLentesMat = CreateOrGetTextureMaterial("Assets/Settings/West_Optica_PosterLentes.mat", "Assets/Textures/Environment/T_West_Optica_PosterLentes.png");
            Material opticaAbiertoMat = CreateOrGetTextureMaterial("Assets/Settings/West_Optica_Abierto.mat", "Assets/Textures/Environment/T_West_Optica_Abierto.png");
            Material cfeMeterFaceMat = CreateOrGetTextureMaterial("Assets/Settings/West_CFE_MeterFace.mat", "Assets/Textures/Environment/T_West_CFE_MeterFace.png");

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
                    UnityEngine.Object.DestroyImmediate(hl.gameObject);
                }
            }

            // =========================================================================
            // 4. BUILD THE AUTHENTIC WEST SIDEWALK BLOCK (South -Z / Left to North +Z / Right)
            // Front wall plane sits at X = -12.0m (flush with back edge of Sidewalk_West)
            // =========================================================================

            // Required collider object for CorridorVerificationTest: "Optica_MainBuilding"
            // Recessed behind X = -14.40m so Bay 4 ("Óptica Fashion") has a genuine 2.40m-deep visible glass showroom!
            // Front wall surround slabs (South, Above, North Pier) close flush at X = -12.00m.
            GameObject optMain = CreateBox("Optica_MainBuilding", opticaRoot.transform,
                new Vector3(-19.20f, 3.9f, -8.55f), new Vector3(9.60f, 7.8f, 23.90f), creamStuccoMat, keepCollider: true);

            // Container for all West Block architectural details
            GameObject blockArch = new GameObject("WestBlock_HistoricArchitecture");
            blockArch.transform.SetParent(opticaRoot.transform);
            blockArch.transform.localPosition = Vector3.zero;

            // Front structural wall surround around Bay 4's hollow glass showroom (X = -14.40..-12.00m)
            CreateBox("BuildingA_FrontWall_SouthOfOptica", blockArch.transform,
                new Vector3(-13.20f, 3.90f, -11.15f), new Vector3(2.40f, 7.80f, 18.70f), creamStuccoMat, keepCollider: true);
            CreateBox("BuildingA_FrontWall_AboveOptica", blockArch.transform,
                new Vector3(-13.20f, 5.26f, 0.80f), new Vector3(2.40f, 5.08f, 5.20f), creamStuccoMat, keepCollider: true);
            CreateBox("BuildingA_FrontWall_OpticaNorthPier", blockArch.transform,
                new Vector3(-13.20f, 1.36f, 3.225f), new Vector3(2.40f, 2.72f, 0.35f), creamStuccoMat, keepCollider: true);

            // -------------------------------------------------------------------------
            // BUILDING A: 2-STORY NEOCLASSICAL BUILDING WITH 4 STRAIGHT ARCHES + 1 ANGLED NORTH DIAGONAL ARCH
            // Straight facade spans Z = -20.50m to +3.40m (Length = 23.90m, Center Z = -8.55m, Height = 7.8m)
            // -------------------------------------------------------------------------

            // Top Stepped Dark Grey Stone Parapet Cornice (Y = 7.05m to 7.85m, stops cleanly at Z = +3.45m for diagonal turn)
            CreateBox("BuildingA_Cornice_Band1", blockArch.transform,
                new Vector3(-11.88f, 7.20f, -8.55f), new Vector3(0.28f, 0.26f, 24.00f), darkCorniceMat);
            CreateBox("BuildingA_Cornice_Band2", blockArch.transform,
                new Vector3(-11.84f, 7.48f, -8.55f), new Vector3(0.36f, 0.24f, 24.06f), stoneMasonryMat);
            CreateBox("BuildingA_Cornice_Band3", blockArch.transform,
                new Vector3(-11.80f, 7.74f, -8.55f), new Vector3(0.42f, 0.22f, 24.12f), darkCorniceMat);

            // Decorative Recessed Upper Wall Relief Panels (between arches and top cornice, Y = 6.18m)
            float[] reliefCentersZ = new float[] { -16.2f, -8.3f, -0.4f };
            float[] reliefWidthsZ = new float[] { 6.2f, 6.8f, 5.8f };
            for (int i = 0; i < reliefCentersZ.Length; i++)
            {
                CreateBox($"BuildingA_UpperReliefPanel_{i}", blockArch.transform,
                    new Vector3(-11.96f, 6.18f, reliefCentersZ[i]), new Vector3(0.06f, 0.92f, reliefWidthsZ[i]), weatheredCreamMat);
            }

            // Horizontal Stringcourse Molding Below Arched Windows (Y = 3.85m, stops at Z = +3.45m)
            CreateBox("BuildingA_ArchSillCourse", blockArch.transform,
                new Vector3(-11.92f, 3.85f, -8.55f), new Vector3(0.18f, 0.10f, 24.00f), darkCorniceMat);

            // 4 Straight Semicircular Arched Lunette Windows on Upper Floor (5th arch sits on the diagonal north wall!)
            CreateArchedLunette("ArchWindow_1_AVenta", blockArch.transform, centerZ: -17.6f, centerY: 4.60f, widthZ: 2.7f, heightY: 1.48f, archWindowMat, darkCorniceMat);
            CreateArchedLunette("ArchWindow_2_Internacional", blockArch.transform, centerZ: -11.9f, centerY: 4.68f, widthZ: 4.6f, heightY: 1.65f, archWindowMat, darkCorniceMat);
            CreateArchedLunette("ArchWindow_3_Pharmacy", blockArch.transform, centerZ: -5.4f, centerY: 4.70f, widthZ: 4.8f, heightY: 1.70f, archWindowMat, darkCorniceMat);
            CreateArchedLunette("ArchWindow_4_Optica", blockArch.transform, centerZ: 0.85f, centerY: 4.74f, widthZ: 3.65f, heightY: 1.75f, archWindowMat, darkCorniceMat);

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

            // Dividing Pilaster between Bay 3 & Bay 4 (Z = -2.15m) + Twin CFE Electric Meters (visible on left edge of photo!)
            CreateBox("Pilaster_Bay3_4", blockArch.transform,
                new Vector3(-11.92f, 1.9f, -2.15f), new Vector3(0.18f, 3.4f, 0.55f), creamStuccoMat);
            CreateBox("Pilaster_Bay3_4_Base", blockArch.transform,
                new Vector3(-11.90f, 0.68f, -2.15f), new Vector3(0.20f, 0.96f, 0.58f), darkZocaloMat);
            CreateCfeMeter("CFE_Meter_South_1", blockArch.transform, new Vector3(-11.82f, 1.40f, -2.01f), aluminumMat, cfeDomeGlassMat, cfeMeterFaceMat);
            CreateCfeMeter("CFE_Meter_South_2", blockArch.transform, new Vector3(-11.82f, 1.40f, -2.29f), aluminumMat, cfeDomeGlassMat, cfeMeterFaceMat);

            // --- Bay 4 (Z = -1.80m to +3.05m): "Óptica Fashion" — Full Glass Push/Pull Storefront + Visible Interior + Dynamic Shutter ---
            // 1. Hollow Visible Showroom Interior (X = -14.40..-12.00m, Z = -1.80..+3.05m)
            CreateBox("Optica_InteriorFloor", blockArch.transform,
                new Vector3(-13.20f, 0.10f, 0.625f), new Vector3(2.44f, 0.20f, 4.90f), opticaFloorMat, keepCollider: true);
            CreateBox("Optica_InteriorBackWall", blockArch.transform,
                new Vector3(-14.34f, 1.46f, 0.625f), new Vector3(0.10f, 2.52f, 4.85f), weatheredCreamMat);
            CreateBox("Optica_InteriorCeiling", blockArch.transform,
                new Vector3(-13.20f, 2.70f, 0.625f), new Vector3(2.42f, 0.08f, 4.88f), whiteFrameMat);
            // Back eyewear display shelves (3 rows)
            for (int s = 0; s < 3; s++)
            {
                CreateBox($"Optica_DisplayShelf_{s}", blockArch.transform,
                    new Vector3(-14.12f, 1.05f + s * 0.45f, 0.35f), new Vector3(0.28f, 0.05f, 2.60f), whiteFrameMat);
                CreateBox($"Optica_FramesRow_{s}", blockArch.transform,
                    new Vector3(-14.10f, 1.13f + s * 0.45f, 0.35f), new Vector3(0.14f, 0.08f, 2.35f), opticaBlackFrameMat);
            }
            // Right geometric grey fitting counter + circular tabletop mirror (visible through glass in reference photo!)
            CreateBox("Optica_InteriorCounter", blockArch.transform,
                new Vector3(-13.05f, 0.68f, 1.85f), new Vector3(1.05f, 0.96f, 1.38f), opticaCounterMat);
            CreateBox("Optica_CounterTop", blockArch.transform,
                new Vector3(-13.05f, 1.18f, 1.85f), new Vector3(1.10f, 0.06f, 1.42f), whiteFrameMat);
            var mirrorStand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mirrorStand.name = "Optica_TableMirror_Stand";
            mirrorStand.transform.SetParent(blockArch.transform, false);
            mirrorStand.transform.localPosition = new Vector3(-12.78f, 1.32f, 1.82f);
            mirrorStand.transform.localScale = new Vector3(0.06f, 0.12f, 0.06f);
            SetMaterial(mirrorStand, opticaBlackFrameMat);
            UnityEngine.Object.DestroyImmediate(mirrorStand.GetComponent<Collider>());
            var mirrorDisc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mirrorDisc.name = "Optica_TableMirror_Disc";
            mirrorDisc.transform.SetParent(blockArch.transform, false);
            mirrorDisc.transform.localPosition = new Vector3(-12.78f, 1.50f, 1.82f);
            mirrorDisc.transform.localRotation = Quaternion.Euler(0f, 0f, 80f);
            mirrorDisc.transform.localScale = new Vector3(0.26f, 0.02f, 0.26f);
            SetMaterial(mirrorDisc, aluminumMat);
            UnityEngine.Object.DestroyImmediate(mirrorDisc.GetComponent<Collider>());
            // Left illuminated eyewear display tower (visible in left glass sidelight)
            CreateBox("Optica_LeftDisplayTower", blockArch.transform,
                new Vector3(-12.65f, 1.18f, -1.20f), new Vector3(0.60f, 1.92f, 0.72f), whiteFrameMat);
            CreateBox("Optica_LeftTower_ShelfAccent", blockArch.transform,
                new Vector3(-12.34f, 1.32f, -1.20f), new Vector3(0.04f, 1.35f, 0.56f), yellowPanelMat);

            // 2. Full-Height Glass & Black Metal Storefront + Double Push/Pull Doors (X = -12.02m, Z = -1.80..+3.05m)
            // Outer black perimeter frame
            CreateBox("Optica_Frame_Top", blockArch.transform,
                new Vector3(-12.02f, 2.66f, 0.625f), new Vector3(0.10f, 0.09f, 4.86f), opticaBlackFrameMat);
            CreateBox("Optica_Frame_Bottom", blockArch.transform,
                new Vector3(-12.02f, 0.24f, 0.625f), new Vector3(0.10f, 0.08f, 4.86f), opticaBlackFrameMat);
            // Vertical black mullions separating Left Sidelight | Left Door | Right Door | Right Sidelight
            float[] opticaFrameZs = new float[] { -1.78f, -0.52f, 0.60f, 1.72f, 3.02f };
            for (int i = 0; i < opticaFrameZs.Length; i++)
            {
                CreateBox($"Optica_Frame_Mullion_{i}", blockArch.transform,
                    new Vector3(-12.02f, 1.45f, opticaFrameZs[i]), new Vector3(0.10f, 2.46f, 0.08f), opticaBlackFrameMat);
            }
            // Horizontal mid-rail across doors & sidelights + door top transom
            CreateBox("Optica_Door_MidRail", blockArch.transform,
                new Vector3(-12.02f, 1.24f, 0.60f), new Vector3(0.08f, 0.07f, 2.24f), opticaBlackFrameMat);
            CreateBox("Optica_Door_Transom", blockArch.transform,
                new Vector3(-12.02f, 2.24f, 0.625f), new Vector3(0.08f, 0.07f, 4.80f), opticaBlackFrameMat);
            // Push/Pull vertical aluminum handles on center double glass doors
            CreateBox("Optica_DoorHandle_Left", blockArch.transform,
                new Vector3(-11.94f, 1.25f, 0.48f), new Vector3(0.05f, 0.42f, 0.035f), aluminumMat);
            CreateBox("Optica_DoorHandle_Right", blockArch.transform,
                new Vector3(-11.94f, 1.25f, 0.72f), new Vector3(0.05f, 0.42f, 0.035f), aluminumMat);
            // Transparent glossy glass panes (sidelights + double doors) allowing full interior visibility!
            CreateBox("Optica_GlassPane_Full", blockArch.transform,
                new Vector3(-12.02f, 1.45f, 0.625f), new Vector3(0.025f, 2.38f, 4.76f), clearGlassMat, keepCollider: true);

            // Door signage: hanging "ABIERTO" plaque, small pink notice, and "LENTES DEPORTIVOS" poster (exact photo match!)
            GameObject abiertoQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            abiertoQuad.name = "Optica_AbiertoSign";
            abiertoQuad.transform.SetParent(blockArch.transform, false);
            abiertoQuad.transform.localPosition = new Vector3(-11.98f, 2.02f, 0.05f);
            abiertoQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            abiertoQuad.transform.localScale = new Vector3(0.42f, 0.16f, 1.0f);
            SetMaterial(abiertoQuad, opticaAbiertoMat);
            UnityEngine.Object.DestroyImmediate(abiertoQuad.GetComponent<Collider>());

            CreateBox("Optica_PinkNotice", blockArch.transform,
                new Vector3(-11.98f, 1.66f, 0.30f), new Vector3(0.015f, 0.24f, 0.20f), opticaPinkNoteMat);

            GameObject posterLentes = GameObject.CreatePrimitive(PrimitiveType.Quad);
            posterLentes.name = "Optica_PosterLentes";
            posterLentes.transform.SetParent(blockArch.transform, false);
            posterLentes.transform.localPosition = new Vector3(-11.98f, 0.86f, 0.05f);
            posterLentes.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            posterLentes.transform.localScale = new Vector3(0.68f, 0.84f, 1.0f);
            SetMaterial(posterLentes, opticaPosterLentesMat);
            UnityEngine.Object.DestroyImmediate(posterLentes.GetComponent<Collider>());

            // 3. Dynamic Roll-Up Metal Shutter for Óptica Fashion (automatically lowers when World Core reports off_duty/closed!)
            CreateDynamicRollUpShutter("Optica_RollUpShutter_Dynamic", blockArch.transform,
                wallX: -11.90f, centerZ: 0.625f, widthZ: 4.86f, topY: 2.68f, fullDropHeight: 2.48f,
                alwaysOpen: false, linkedInteractionPointName: "InteractionPoint_OpticaFashion",
                openSignVisual: abiertoQuad, trackMat: aluminumMat, housingMat: darkCorniceMat, shutterMat: metalShutterMat);

            // Beige Wedge Awning ("Optica_Awning") & Terracotta Sign ("Optica_Signboard")
            CreateWestWedgeAwning("Optica_Awning", opticaRoot.transform,
                centerZ: 0.85f, topY: 3.22f, widthZ: 4.8f, projectionX: 1.35f, dropY: 0.74f, beigeAwningMat, awningValanceMat);
            CreateWestSign("Optica_Signboard", opticaRoot.transform,
                centerZ: 0.85f, centerY: 3.58f, widthZ: 4.3f, heightY: 0.68f, terracottaPlaqueMat, signOpticaMat);

            // Dividing Corner Pilaster at Building A's diagonal break (Z = +3.30m) + Central CFE Electric Meter!
            CreateBox("Pilaster_Bay4_5", blockArch.transform,
                new Vector3(-11.95f, 1.9f, 3.30f), new Vector3(0.18f, 3.4f, 0.42f), creamStuccoMat);
            CreateBox("Pilaster_Bay4_5_Base", blockArch.transform,
                new Vector3(-11.93f, 0.68f, 3.30f), new Vector3(0.20f, 0.96f, 0.45f), darkZocaloMat);
            CreateCfeMeter("CFE_Meter_Center", blockArch.transform, new Vector3(-11.84f, 1.40f, 3.30f), aluminumMat, cfeDomeGlassMat, cfeMeterFaceMat);

            // Large Hanging Double-Sided Live LED Currency Exchange Sign (1.05m x 0.62m) cantilevered out over sidewalk on dual metal arms!
            CreateExchangeRateLedSign("CambiosSantaFe_ExchangeLedSign", blockArch.transform,
                pos: new Vector3(-10.85f, 3.82f, 3.45f), cabinetMat: opticaBlackFrameMat, armMat: darkCorniceMat,
                flangeMat: aluminumMat, liveLedBaseMat: liveLedBaseMat);

            // -------------------------------------------------------------------------
            // BUILDING A DIAGONAL NORTH RETURN (-40.24 deg) & RECESSED "Cambios Santa Fe" OPEN PORTAL
            // Connects (-12.00m, +3.40m) to (-14.20m, +6.00m) meeting Building B's South wall!
            // -------------------------------------------------------------------------
            GameObject diagRoot = new GameObject("BuildingA_DiagonalEnd_Root");
            diagRoot.transform.SetParent(blockArch.transform, false);
            diagRoot.transform.localPosition = new Vector3(-13.10f, 0f, 4.70f);
            diagRoot.transform.localRotation = Quaternion.Euler(0f, -40.236f, 0f);

            // Upper angled structural body (Y = 2.75..7.80m, leaving Y = 0.20..2.75m open for the Cambios Santa Fe vestibule!)
            CreateBox("BuildingA_DiagonalMass", diagRoot.transform,
                new Vector3(-2.40f, 5.28f, 0f), new Vector3(4.80f, 5.06f, 3.48f), creamStuccoMat, keepCollider: true);
            // Rear solid core closing behind the vestibule & diagonal return (X = -24.00..-13.70m, Z = +3.40..+6.00m)
            CreateBox("BuildingA_NorthRearFill", blockArch.transform,
                new Vector3(-18.85f, 3.90f, 4.70f), new Vector3(10.30f, 7.80f, 2.65f), creamStuccoMat, keepCollider: true);

            // Stepped Parapet Cornice turning smoothly along the diagonal wall into Building B
            CreateBox("BuildingA_DiagCornice_Band1", diagRoot.transform,
                new Vector3(0.12f, 7.20f, 0f), new Vector3(0.28f, 0.26f, 3.65f), darkCorniceMat);
            CreateBox("BuildingA_DiagCornice_Band2", diagRoot.transform,
                new Vector3(0.16f, 7.48f, 0f), new Vector3(0.36f, 0.24f, 3.70f), stoneMasonryMat);
            CreateBox("BuildingA_DiagCornice_Band3", diagRoot.transform,
                new Vector3(0.20f, 7.74f, 0f), new Vector3(0.42f, 0.22f, 3.76f), darkCorniceMat);
            CreateBox("BuildingA_DiagArchSillCourse", diagRoot.transform,
                new Vector3(0.08f, 3.85f, 0f), new Vector3(0.18f, 0.10f, 3.55f), darkCorniceMat);

            // 5th Semicircular Arched Lunette mounted ON the angled diagonal wall (foreshortened exactly like Street View!)
            CreateArchedLunette("ArchWindow_5_SantaFe", diagRoot.transform,
                centerZ: 0.15f, centerY: 4.66f, widthZ: 1.95f, heightY: 1.62f, archWindowMat, darkCorniceMat, localX: 0.04f);

            // Wall-mounted A/C condenser unit tucked in upper nook against Building B (visible in Street View)
            CreateBox("SantaFe_Nook_ACUnit", blockArch.transform,
                new Vector3(-13.78f, 4.02f, 5.62f), new Vector3(0.42f, 0.56f, 0.62f), weatheredCreamMat);

            // --- Bay 5 (Recessed Open Vestibule, Z = +3.50m to +5.98m, X = -13.65..-12.00m): "Cambios Santa Fe" ---
            // Dark-red painted vestibule floor + yellow caution edge strip along sidewalk threshold (matches photo!)
            CreateBox("SantaFe_NookFloor", blockArch.transform,
                new Vector3(-12.88f, 0.102f, 4.74f), new Vector3(1.66f, 0.204f, 2.52f), santaFeFloorRedMat, keepCollider: true);
            CreateBox("SantaFe_ThresholdYellow", blockArch.transform,
                new Vector3(-12.03f, 0.104f, 4.74f), new Vector3(0.12f, 0.208f, 2.48f), yellowPanelMat);
            // White interior vestibule soffit ceiling & side return walls
            CreateBox("SantaFe_VestibuleCeiling", blockArch.transform,
                new Vector3(-12.85f, 2.74f, 4.74f), new Vector3(1.72f, 0.08f, 2.52f), whiteFrameMat);
            CreateBox("SantaFe_SideWall_South", blockArch.transform,
                new Vector3(-12.85f, 1.46f, 3.50f), new Vector3(1.66f, 2.54f, 0.08f), whiteFrameMat, keepCollider: true);

            // Smooth Recessed Back Wall (X = -13.65m): White upper wall + Bright Red lower wainscot (flush, no bulky counter!)
            CreateBox("SantaFe_BackWall_UpperWhite", blockArch.transform,
                new Vector3(-13.65f, 1.98f, 4.74f), new Vector3(0.12f, 1.56f, 2.50f), whiteFrameMat, keepCollider: true);
            CreateBox("SantaFe_RedCasetillaBase", blockArch.transform,
                new Vector3(-13.64f, 0.70f, 4.74f), new Vector3(0.14f, 1.00f, 2.50f), santaFeRedMat, keepCollider: true);

            // Left Armored Employee Steel Door (Z = +3.72..+4.46m) — heavy grey frame & steel door leaf into rear booth
            CreateBox("SantaFe_ArmoredDoor_Frame", blockArch.transform,
                new Vector3(-13.57f, 1.32f, 4.08f), new Vector3(0.08f, 2.22f, 0.84f), aluminumMat);
            CreateBox("SantaFe_ArmoredDoor_Leaf", blockArch.transform,
                new Vector3(-13.55f, 1.30f, 4.08f), new Vector3(0.06f, 2.12f, 0.72f), santaFeArmorSteelMat);
            CreateBox("SantaFe_ArmoredDoor_Handle", blockArch.transform,
                new Vector3(-13.50f, 1.18f, 4.34f), new Vector3(0.06f, 0.16f, 0.04f), opticaBlackFrameMat);

            // Center Reinforced Bulletproof Teller Window (Z = +4.68..+5.36m) + Recessed Stainless-Steel Cash Pass-Through Tray!
            CreateBox("SantaFe_TellerCabinDark", blockArch.transform,
                new Vector3(-13.60f, 1.66f, 5.02f), new Vector3(0.14f, 0.90f, 0.68f), interiorDarkMat);
            CreateBox("SantaFe_TellerFrame_Top", blockArch.transform,
                new Vector3(-13.54f, 2.13f, 5.02f), new Vector3(0.10f, 0.06f, 0.76f), aluminumMat);
            CreateBox("SantaFe_TellerFrame_L", blockArch.transform,
                new Vector3(-13.54f, 1.66f, 4.66f), new Vector3(0.10f, 0.96f, 0.06f), aluminumMat);
            CreateBox("SantaFe_TellerFrame_R", blockArch.transform,
                new Vector3(-13.54f, 1.66f, 5.38f), new Vector3(0.10f, 0.96f, 0.06f), aluminumMat);
            CreateBox("SantaFe_ServiceWindow", blockArch.transform,
                new Vector3(-13.54f, 1.72f, 5.02f), new Vector3(0.03f, 0.78f, 0.66f), clearGlassMat);
            // Recessed stainless-steel cash pass-through tray right underneath bulletproof glass (Y = 1.12..1.28m)
            CreateBox("SantaFe_CashTray_Recess", blockArch.transform,
                new Vector3(-13.55f, 1.24f, 5.02f), new Vector3(0.12f, 0.14f, 0.66f), interiorDarkMat);
            CreateBox("SantaFe_CashTray_MetalLip", blockArch.transform,
                new Vector3(-13.48f, 1.16f, 5.02f), new Vector3(0.20f, 0.05f, 0.74f), aluminumMat);

            // Glowing Neon-Red "OPEN" LED Sign mounted to the right of the teller window (exact photo match!)
            CreateBox("SantaFe_OpenLED_Cabinet", blockArch.transform,
                new Vector3(-13.56f, 1.56f, 5.68f), new Vector3(0.06f, 0.19f, 0.44f), opticaBlackFrameMat);
            GameObject openLedQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            openLedQuad.name = "SantaFe_OpenLED_Sign";
            openLedQuad.transform.SetParent(blockArch.transform, false);
            openLedQuad.transform.localPosition = new Vector3(-13.525f, 1.56f, 5.68f);
            openLedQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            openLedQuad.transform.localScale = new Vector3(0.41f, 0.165f, 1.0f);
            SetMaterial(openLedQuad, openLedMat);
            UnityEngine.Object.DestroyImmediate(openLedQuad.GetComponent<Collider>());

            // Exterior Roll-Up Metal Shutter Assembly for Cambios Santa Fe (alwaysOpen = true for 24h service!)
            CreateDynamicRollUpShutter("SantaFe_RollUpShutter_Dynamic", blockArch.transform,
                wallX: -12.06f, centerZ: 4.74f, widthZ: 2.46f, topY: 2.68f, fullDropHeight: 2.48f,
                alwaysOpen: true, linkedInteractionPointName: string.Empty,
                openSignVisual: null, trackMat: whiteFrameMat, housingMat: darkCorniceMat, shutterMat: metalShutterMat);

            // Beige Wedge Awning spanning across the recessed Cambios Santa Fe mouth
            CreateWestWedgeAwning("Awning_Bay5_SantaFe", blockArch.transform,
                centerZ: 4.76f, topY: 3.18f, widthZ: 2.42f, projectionX: 1.48f, dropY: 0.68f, beigeAwningMat, awningValanceMat, wallX: -12.20f);
            // Corrected Terracotta Sign "Cambios Santa Fe" (2-line white text, clear of diagonal sill bar!)
            CreateWestSign("Sign_SantaFe", blockArch.transform,
                centerZ: 4.92f, centerY: 3.64f, widthZ: 1.98f, heightY: 0.78f, terracottaPlaqueMat, signSantaFeMat, frontOffsetX: -12.18f);

            // -------------------------------------------------------------------------
            // BUILDING B: 3-STORY HISTORIC CRENELLATED BUILDING ("Bar La Cucaracha" & "Taquería El Trompo")
            // Compacted to authentic vertical proportions: Z = +6.00m to +15.00m (Length = 9.00m, Center Z = +10.50m, Height = 8.6m)
            // -------------------------------------------------------------------------
            CreateBox("BuildingB_CucarachaMass", blockArch.transform,
                new Vector3(-18.0f, 4.30f, 10.50f), new Vector3(12.0f, 8.60f, 9.00f), rusticStuccoMat, keepCollider: true);

            // Top Crenellated Castle Parapet (8 evenly spaced merlons matching Street View)
            CreateBox("BuildingB_ParapetTrim", blockArch.transform,
                new Vector3(-11.90f, 8.55f, 10.50f), new Vector3(0.24f, 0.14f, 9.10f), stoneMasonryMat);
            int merlonCount = 8;
            for (int i = 0; i < merlonCount; i++)
            {
                float merlonZ = 6.38f + i * 1.18f;
                CreateBox($"BuildingB_Merlon_{i}", blockArch.transform,
                    new Vector3(-11.92f, 8.82f, merlonZ), new Vector3(0.22f, 0.40f, 0.62f), rusticStuccoMat);
                CreateBox($"BuildingB_MerlonCap_{i}", blockArch.transform,
                    new Vector3(-11.90f, 9.04f, merlonZ), new Vector3(0.26f, 0.06f, 0.66f), stoneMasonryMat);
            }

            // 3rd Floor Exposed Stone/Block Band & Casement Windows (Y = 6.92m, Z = +6.75m to +14.25m)
            CreateBox("BuildingB_StoneMasonryBand", blockArch.transform,
                new Vector3(-11.95f, 6.92f, 10.50f), new Vector3(0.12f, 1.48f, 7.50f), stoneMasonryMat);
            // Left 3rd-Floor Window (Z = +7.85m)
            CreateBox("BuildingB_Floor3_WinLeft", blockArch.transform,
                new Vector3(-11.88f, 6.92f, 7.85f), new Vector3(0.08f, 1.38f, 1.95f), darkGlassMat);
            CreateBox("BuildingB_Floor3_WinLeft_MullionV", blockArch.transform,
                new Vector3(-11.85f, 6.92f, 7.85f), new Vector3(0.08f, 1.38f, 0.06f), aluminumMat);
            CreateBox("BuildingB_Floor3_WinLeft_MullionH", blockArch.transform,
                new Vector3(-11.85f, 7.22f, 7.85f), new Vector3(0.08f, 0.06f, 1.95f), aluminumMat);
            // Center Small 3rd-Floor Vent Window (Z = +10.45m)
            CreateBox("BuildingB_Floor3_WinMid", blockArch.transform,
                new Vector3(-11.88f, 7.12f, 10.45f), new Vector3(0.08f, 0.72f, 0.76f), darkGlassMat);
            // Right 3rd-Floor Window (Z = +13.15m)
            CreateBox("BuildingB_Floor3_WinRight", blockArch.transform,
                new Vector3(-11.88f, 6.92f, 13.15f), new Vector3(0.08f, 1.38f, 2.05f), darkGlassMat);
            CreateBox("BuildingB_Floor3_WinRight_MullionV", blockArch.transform,
                new Vector3(-11.85f, 6.92f, 13.15f), new Vector3(0.08f, 1.38f, 0.06f), aluminumMat);
            CreateBox("BuildingB_Floor3_WinRight_MullionH", blockArch.transform,
                new Vector3(-11.85f, 7.22f, 13.15f), new Vector3(0.08f, 0.06f, 2.05f), aluminumMat);

            // Horizontal Mid-Facade Cornice Ledge above 2nd Floor Ribbon Windows (Y = 5.58m)
            CreateBox("BuildingB_MidCorniceLedge", blockArch.transform,
                new Vector3(-11.88f, 5.58f, 10.50f), new Vector3(0.26f, 0.14f, 9.05f), darkCorniceMat);

            // 2nd Floor Continuous Multi-Pane Window Ribbon with White Security Grilles (Y = 4.82m, Z = +6.65m to +14.35m)
            GameObject ribbonQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ribbonQuad.name = "BuildingB_Floor2_RibbonWindows";
            ribbonQuad.transform.SetParent(blockArch.transform);
            ribbonQuad.transform.localPosition = new Vector3(-11.92f, 4.82f, 10.50f);
            ribbonQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Faces East (+X)
            ribbonQuad.transform.localScale = new Vector3(7.70f, 1.12f, 1.0f);
            SetMaterial(ribbonQuad, ribbonWindowsMat);
            UnityEngine.Object.DestroyImmediate(ribbonQuad.GetComponent<Collider>());

            // --- Bay 6 (Z = +6.00m to +10.20m, center Z = +8.10m): "Bar La Cucaracha" ---
            GameObject shutterQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shutterQuad.name = "LaCucaracha_RollUpShutter";
            shutterQuad.transform.SetParent(blockArch.transform);
            shutterQuad.transform.localPosition = new Vector3(-11.92f, 1.42f, 8.10f);
            shutterQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Faces East (+X)
            shutterQuad.transform.localScale = new Vector3(3.15f, 2.45f, 1.0f);
            SetMaterial(shutterQuad, metalShutterMat);
            UnityEngine.Object.DestroyImmediate(shutterQuad.GetComponent<Collider>());
            CreateBox("LaCucaracha_Pillar_Left", blockArch.transform,
                new Vector3(-11.94f, 1.45f, 6.26f), new Vector3(0.14f, 2.55f, 0.52f), whiteFrameMat);
            CreateBox("LaCucaracha_Pillar_Right", blockArch.transform,
                new Vector3(-11.94f, 1.45f, 9.95f), new Vector3(0.14f, 2.55f, 0.55f), whiteFrameMat);
            CreateWestWedgeAwning("Awning_Bay6_LaCucaracha", blockArch.transform,
                centerZ: 8.10f, topY: 3.16f, widthZ: 3.95f, projectionX: 1.25f, dropY: 0.68f, beigeAwningMat, awningValanceMat);
            CreateWestSign("Sign_LaCucaracha", blockArch.transform,
                centerZ: 8.10f, centerY: 3.56f, widthZ: 3.85f, heightY: 0.64f, terracottaPlaqueMat, signCucarachaMat);

            // --- Bay 7 (Z = +10.20m to +15.00m, center Z = +12.60m): "Taquería El Trompo" ---
            CreateBox("ElTrompo_PortalRecess", blockArch.transform,
                new Vector3(-12.35f, 1.42f, 11.95f), new Vector3(0.8f, 2.45f, 2.25f), interiorDarkMat);
            CreateBox("ElTrompo_WoodDoorFrame", blockArch.transform,
                new Vector3(-12.10f, 1.40f, 12.75f), new Vector3(0.15f, 2.40f, 0.72f), woodDoorMat);
            CreateBox("ElTrompo_BrickWall_Right", blockArch.transform,
                new Vector3(-11.94f, 1.42f, 13.85f), new Vector3(0.14f, 2.45f, 1.65f), exposedBrickMat);
            CreateBox("ElTrompo_BrickWindow", blockArch.transform,
                new Vector3(-11.88f, 1.68f, 13.85f), new Vector3(0.08f, 0.82f, 0.68f), darkGlassMat);
            CreateBox("ElTrompo_BrickJamb_Left", blockArch.transform,
                new Vector3(-11.94f, 1.42f, 10.55f), new Vector3(0.14f, 2.45f, 0.52f), exposedBrickMat);
            CreateWestWedgeAwning("Awning_Bay7_TaqueriaElTrompo", blockArch.transform,
                centerZ: 12.60f, topY: 3.16f, widthZ: 4.45f, projectionX: 1.25f, dropY: 0.68f, beigeAwningMat, awningValanceMat);
            CreateWestSign("Sign_TaqueriaElTrompo", blockArch.transform,
                centerZ: 12.60f, centerY: 3.56f, widthZ: 4.25f, heightY: 0.64f, terracottaPlaqueMat, signTrompoMat);

            // -------------------------------------------------------------------------
            // BUILDING C: RECESSED SINGLE-STORY CORNER CLINIC WITH CANTILEVERED CANOPY — "Dental Jarquín"
            // Recessed East facade at X = -13.50m (Z = +15.00m to +20.30m), 45 deg chamfer to (-15.00m, +21.80m),
            // and North wing along Calle Gardenias. Open skyline above cantilevered white/cream canopy matches Street View!
            // -------------------------------------------------------------------------

            // Sidewalk concrete forecourt slab filling the 1.50m recess in front of Dental Jarquín (X = -13.50..-12.00)
            CreateBox("Dental_RecessedForecourt_Floor", blockArch.transform,
                new Vector3(-12.75f, 0.10f, 17.85f), new Vector3(1.60f, 0.20f, 5.70f), concreteMat, keepCollider: true);

            // 1. Recessed Single-Story Main Body (split cleanly behind 45 deg chamfer so no square corner pokes through!)
            CreateBox("Dental_MainBody", blockArch.transform,
                new Vector3(-22.75f, 2.15f, 17.65f), new Vector3(18.50f, 4.30f, 5.30f), creamStuccoMat, keepCollider: true);
            CreateBox("Dental_MainBody_NorthWing", blockArch.transform,
                new Vector3(-23.50f, 2.15f, 18.40f), new Vector3(17.00f, 4.30f, 6.80f), creamStuccoMat, keepCollider: true);

            // 2. East Facade facing Av. Benito Juárez (X = -13.50m, Z in [+15.00m, +20.30m])
            CreateBox("Dental_East_Zocalo", blockArch.transform,
                new Vector3(-13.44f, 0.30f, 17.65f), new Vector3(0.14f, 0.60f, 5.30f), darkZocaloMat);
            // Commercial glass storefront & white-framed entrance (replaces lone closed grey shutter)
            CreateBox("Dental_East_StorefrontGlass", blockArch.transform,
                new Vector3(-13.45f, 1.75f, 17.65f), new Vector3(0.08f, 2.30f, 4.40f), darkGlassMat);
            float[] dentalEastMullions = new float[] { 15.45f, 16.55f, 17.65f, 18.75f, 19.85f };
            for (int i = 0; i < dentalEastMullions.Length; i++)
            {
                CreateBox($"Dental_East_Mullion_{i}", blockArch.transform,
                    new Vector3(-13.41f, 1.75f, dentalEastMullions[i]), new Vector3(0.12f, 2.34f, 0.09f), whiteFrameMat);
            }
            CreateBox("Dental_East_TransomBar", blockArch.transform,
                new Vector3(-13.41f, 2.90f, 17.65f), new Vector3(0.14f, 0.12f, 4.50f), darkCorniceMat);
            CreateBox("Dental_East_DoorMidBar", blockArch.transform,
                new Vector3(-13.41f, 1.15f, 17.65f), new Vector3(0.12f, 0.08f, 4.45f), whiteFrameMat);

            // Prominent Terracotta Sign "Dental Jarquín" facing directly East (+X) toward Av. Benito Juárez (matches Street View!)
            CreateWestSign("Sign_DentalJarquin_EastFront", blockArch.transform,
                centerZ: 17.65f, centerY: 3.56f, widthZ: 3.55f, heightY: 0.78f, terracottaPlaqueMat, signDentalMat, frontOffsetX: -13.36f);

            // 3. Authentic 45° Chamfered Corner (Ochave) facing Calle Gardenias intersection
            // Connects (-13.50m, +20.30m) to (-15.00m, +21.80m) -> center (-14.25m, 2.15m, +21.05m), rotY = -45°
            GameObject chamferRoot = new GameObject("Dental_ChamferCorner");
            chamferRoot.transform.SetParent(blockArch.transform);
            chamferRoot.transform.localPosition = new Vector3(-14.25f, 2.15f, 21.05f);
            chamferRoot.transform.localRotation = Quaternion.Euler(0f, -45f, 0f);

            CreateBox("Dental_Chamfer_Wall", chamferRoot.transform,
                new Vector3(-0.60f, 0f, 0f), new Vector3(1.25f, 4.30f, 2.12f), creamStuccoMat, keepCollider: true);
            CreateBox("Dental_Chamfer_Zocalo", chamferRoot.transform,
                new Vector3(0.06f, -1.85f, 0f), new Vector3(0.12f, 0.60f, 2.12f), darkZocaloMat);
            CreateBox("Dental_Chamfer_DisplayRecess", chamferRoot.transform,
                new Vector3(0.06f, -0.35f, 0f), new Vector3(0.10f, 1.95f, 1.25f), darkGlassMat);
            CreateBox("Dental_Chamfer_DisplayFrame", chamferRoot.transform,
                new Vector3(0.09f, -0.35f, 0f), new Vector3(0.06f, 2.05f, 1.30f), whiteFrameMat);
            CreateAngledSign("Sign_DentalJarquin", chamferRoot.transform,
                localCenterY: 1.42f, widthZ: 1.45f, heightY: 0.54f, terracottaPlaqueMat, signDentalMat);

            // 4. North Facade along Calle Gardenias (Z = +21.80m, X in [-15.0m, -32.0m])
            CreateBox("Dental_North_Zocalo", blockArch.transform,
                new Vector3(-23.50f, 0.30f, 21.72f), new Vector3(17.00f, 0.60f, 0.14f), darkZocaloMat);
            CreateBox("Dental_North_Antepecho", blockArch.transform,
                new Vector3(-23.50f, 0.90f, 21.72f), new Vector3(17.00f, 0.60f, 0.14f), creamStuccoMat);

            GameObject northGlass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            northGlass.name = "Dental_North_RibbonWindow";
            northGlass.transform.SetParent(blockArch.transform);
            northGlass.transform.localPosition = new Vector3(-22.50f, 1.92f, 21.74f);
            northGlass.transform.localRotation = Quaternion.identity;
            northGlass.transform.localScale = new Vector3(14.50f, 1.45f, 1.0f);
            SetMaterial(northGlass, darkGlassMat);
            UnityEngine.Object.DestroyImmediate(northGlass.GetComponent<Collider>());

            CreateBox("Dental_North_WindowSill", blockArch.transform,
                new Vector3(-22.50f, 1.18f, 21.76f), new Vector3(14.60f, 0.08f, 0.12f), darkCorniceMat);
            CreateBox("Dental_North_WindowLintel", blockArch.transform,
                new Vector3(-22.50f, 2.68f, 21.76f), new Vector3(14.60f, 0.08f, 0.12f), darkCorniceMat);
            for (int m = 0; m <= 9; m++)
            {
                float mullionX = -15.30f - m * 1.60f;
                CreateBox($"Dental_North_Mullion_{m}", blockArch.transform,
                    new Vector3(mullionX, 1.92f, 21.76f), new Vector3(0.08f, 1.45f, 0.12f), darkCorniceMat);
            }
            CreateBox("Dental_North_UpperFascia", blockArch.transform,
                new Vector3(-23.50f, 3.48f, 21.72f), new Vector3(17.00f, 1.50f, 0.14f), creamStuccoMat);

            // 5. Iconic Wide White/Cream Cantilevered Continuous Concrete Canopy (Y = 4.30m)
            // Projects generously over the recessed forecourt (X = -13.50m to -11.30m) matching Street View!
            CreateBox("Dental_Canopy_East", blockArch.transform,
                new Vector3(-12.40f, 4.32f, 17.65f), new Vector3(2.20f, 0.24f, 5.35f), whiteFrameMat);
            CreateBox("Dental_Canopy_Chamfer", chamferRoot.transform,
                new Vector3(1.10f, 2.17f, 0f), new Vector3(2.20f, 0.24f, 3.30f), whiteFrameMat);
            CreateBox("Dental_Canopy_North", blockArch.transform,
                new Vector3(-23.50f, 4.32f, 22.55f), new Vector3(17.00f, 0.24f, 1.50f), whiteFrameMat);

            CreateBox("Dental_Canopy_East_Fascia", blockArch.transform,
                new Vector3(-11.28f, 4.32f, 17.65f), new Vector3(0.08f, 0.28f, 5.40f), darkCorniceMat);
            CreateBox("Dental_Canopy_Chamfer_Fascia", chamferRoot.transform,
                new Vector3(2.20f, 2.17f, 0f), new Vector3(0.08f, 0.28f, 3.32f), darkCorniceMat);
            CreateBox("Dental_Canopy_North_Fascia", blockArch.transform,
                new Vector3(-23.50f, 4.32f, 23.32f), new Vector3(17.05f, 0.28f, 0.08f), darkCorniceMat);

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

        [MenuItem("MyVirtualCommunity/Verify West North Block (Santa Fe & Dental Jarquín)")]
        public static void VerifyWestNorthBlock()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[WestNorthVerify] FAIL: Could not open {ScenePath}");
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

            GameObject optMain = GameObject.Find("Optica_MainBuilding");
            Check("Building A straight facade ends cleanly at Z=+3.40m for diagonal return",
                optMain != null && optMain.TryGetComponent<Renderer>(out Renderer optR) &&
                Mathf.Abs(optR.bounds.max.z - 3.40f) < 0.10f);

            GameObject diagRoot = GameObject.Find("BuildingA_DiagonalEnd_Root");
            Check("Building A diagonal north wall, angled 5th arch, and Cambios Santa Fe nook exist",
                diagRoot != null &&
                GameObject.Find("ArchWindow_5_SantaFe") != null &&
                GameObject.Find("SantaFe_RedCasetillaBase") != null &&
                GameObject.Find("Sign_SantaFe") != null &&
                GameObject.Find("SantaFe_NookFloor") != null);

            GameObject bldgB = GameObject.Find("BuildingB_CucarachaMass");
            Check("Building B compacted to authentic ~9.0m width (Z=+6.00..+15.00m)",
                bldgB != null && bldgB.TryGetComponent<Renderer>(out Renderer br) &&
                Mathf.Abs(br.bounds.size.z - 9.00f) < 0.10f &&
                Mathf.Abs(br.bounds.max.z - 15.00f) < 0.10f);

            GameObject dentalBody = GameObject.Find("Dental_MainBody");
            Check("Dental Jarquín recessed to X=-13.50m with prominent East-facing sign & cantilevered canopy",
                dentalBody != null && dentalBody.TryGetComponent<Renderer>(out Renderer dr) &&
                Mathf.Abs(dr.bounds.max.x - (-13.50f)) < 0.10f &&
                GameObject.Find("Sign_DentalJarquin_EastFront") != null &&
                GameObject.Find("Dental_Canopy_East") != null &&
                GameObject.Find("BuildingC_HistoricUpperMass") == null);

            GameObject kiosk = GameObject.Find("CitizenServiceDesk");
            Check("Temporary CitizenServiceDesk relocated forward cleanly to (-7.60, 0, 18.80)",
                kiosk != null && Vector3.Distance(kiosk.transform.position, new Vector3(-7.6f, 0.0f, 18.8f)) < 0.10f);

            Check("3D CFE electric meters exist on center and south pilasters",
                GameObject.Find("CFE_Meter_Center") != null &&
                GameObject.Find("CFE_Meter_South_1") != null &&
                GameObject.Find("CFE_Meter_South_2") != null);

            Check("Cambios Santa Fe armored door, bulletproof window, cash tray, and OPEN LED sign exist",
                GameObject.Find("SantaFe_ArmoredDoor_Leaf") != null &&
                GameObject.Find("SantaFe_ServiceWindow") != null &&
                GameObject.Find("SantaFe_CashTray_MetalLip") != null &&
                GameObject.Find("SantaFe_OpenLED_Sign") != null);

            Check("Óptica Fashion full-glass Push/Pull storefront & interior exist (legacy opaque green removed)",
                GameObject.Find("Optica_GlassPane_Full") != null &&
                GameObject.Find("Optica_InteriorCounter") != null &&
                GameObject.Find("Optica_PosterLentes") != null &&
                GameObject.Find("Optica_TealPanel_Left") == null);

            var optShutter = GameObject.Find("Optica_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            var sfShutter = GameObject.Find("SantaFe_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            bool shutterBehaviorOk = false;
            if (optShutter != null && sfShutter != null && !optShutter.alwaysOpen && sfShutter.alwaysOpen)
            {
                optShutter.SetClosedImmediate(true);
                bool closedWhenOffDuty = optShutter.IsClosed && optShutter.ClosedProgress >= 0.99f &&
                    StorefrontShutterController.EvaluateClosedFromAvailability("off_duty");
                optShutter.SetClosedImmediate(false);
                bool openWhenAvailable = !optShutter.IsClosed && optShutter.ClosedProgress <= 0.01f &&
                    !StorefrontShutterController.EvaluateClosedFromAvailability("available");
                sfShutter.SetClosedImmediate(true); // alwaysOpen must force it open
                bool santaFeStaysOpen = !sfShutter.IsClosed;
                shutterBehaviorOk = closedWhenOffDuty && openWhenAvailable && santaFeStaysOpen;
            }
            Check("Dynamic roll-up shutters wired cleanly (Óptica auto-closes on off_duty; Santa Fe always open 24h)", shutterBehaviorOk);

            var ledCtrl = GameObject.Find("CambiosSantaFe_ExchangeLedSign")?.GetComponent<ExchangeRateLedSignController>();
            bool ledOk = false;
            if (ledCtrl != null)
            {
                ledCtrl.ApplyAuthoritativeExchangeRate(new WorldExchangeRateObservationDto
                {
                    status = "available",
                    freshness = "live",
                    baseCurrency = "USD",
                    quoteCurrency = "MXN",
                    buyRate = 17.50f,
                    sellRate = 18.50f,
                    hasBuyRate = true,
                    hasSellRate = true
                });
                ledCtrl.ForceSlide(false);
                bool compraOk = ledCtrl.HasLiveQuote && !ledCtrl.ShowingSellSlide && Mathf.Approximately(ledCtrl.LatestBuyRate, 17.50f);
                ledCtrl.ForceSlide(true);
                bool ventaOk = ledCtrl.ShowingSellSlide && Mathf.Approximately(ledCtrl.LatestSellRate, 18.50f);
                ledOk = compraOk && ventaOk;
            }
            Check("Hanging double-sided Exchange Rate LED sign alternates COMPRA (green) & VENTA (yellow)", ledOk);

            Check("Flagpole above LED sign completely removed (Optica_MexicanFlag absent)",
                GameObject.Find("Optica_MexicanFlag") == null && GameObject.Find("Flag_Pole") == null);

            GameObject ledSignGo = GameObject.Find("CambiosSantaFe_ExchangeLedSign");
            bool armsAndGlowOk = ledSignGo != null &&
                ledSignGo.transform.position.x > -11.00f &&
                ledSignGo.transform.Find("MountArm_Top") != null &&
                ledSignGo.transform.Find("MountArm_Bottom") != null &&
                ledCtrl != null &&
                ledCtrl.baseLedMaterial != null &&
                ledCtrl.baseLedMaterial.IsKeywordEnabled("_EMISSION") &&
                ledCtrl.glowLights != null &&
                ledCtrl.glowLights.Length == 2 &&
                ledCtrl.glowLights[0] != null &&
                ledCtrl.glowLights[1] != null;
            Check("LED sign cantilevered on dual metal arms with HDR emissive material & dual night glow lights", armsAndGlowOk);

            Debug.Log($"[WestNorthVerify] SUMMARY: {passed}/{total} checks passed.");
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

            // Preview realistic live FX readout (17.50 / 18.50) during visual verification captures
            var ledCtrl = GameObject.Find("CambiosSantaFe_ExchangeLedSign")?.GetComponent<ExchangeRateLedSignController>();
            if (ledCtrl != null)
            {
                ledCtrl.ApplyAuthoritativeExchangeRate(new WorldExchangeRateObservationDto
                {
                    status = "available",
                    freshness = "live",
                    baseCurrency = "USD",
                    quoteCurrency = "MXN",
                    buyRate = 17.50f,
                    sellRate = 18.50f,
                    hasBuyRate = true,
                    hasSellRate = true
                });
                ledCtrl.SetNightPreviewOverride(false);
                ledCtrl.ForceSlide(false); // COMPRA (green)
            }

            var optShutter = GameObject.Find("Optica_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            if (optShutter != null) optShutter.SetClosedImmediate(false);

            GameObject camGo = new GameObject("WestBlockVerifyCam");
            Camera cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.Skybox;

            // Preserve scene lighting so we can also render an accurate nighttime LED glow shot and restore cleanly
            Light mainSun = null;
            foreach (var l in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type == LightType.Directional) { mainSun = l; break; }
            }
            float origSunIntensity = mainSun != null ? mainSun.intensity : 1f;
            Color origSunColor = mainSun != null ? mainSun.color : Color.white;
            float origAmbientIntensity = RenderSettings.ambientIntensity;
            Color origAmbientSky = RenderSettings.ambientSkyColor;
            Color origAmbientEq = RenderSettings.ambientEquatorColor;
            Color origAmbientGround = RenderSettings.ambientGroundColor;
            CameraClearFlags origClearFlags = cam.clearFlags;

            try
            {
                // Shot 1: Daytime Frontal Close-Up (confirms flagpole removed, dual arms holding LED sign out over sidewalk)
                cam.fieldOfView = 48f;
                cam.transform.position = new Vector3(-4.5f, 1.85f, 2.2f);
                cam.transform.LookAt(new Vector3(-12.2f, 2.15f, 2.6f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "optica_santafe_detail_match.png")); // URP warmup pass
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "optica_santafe_detail_match.png"));

                // Shot 2: Daytime Sidewalk Angle matching real photo (shows BOTH metal arms + sign protruding past awning + COMPRA green)
                cam.fieldOfView = 46f;
                cam.transform.position = new Vector3(-9.5f, 2.05f, -0.8f);
                cam.transform.LookAt(new Vector3(-11.05f, 3.65f, 3.55f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "santafe_led_compra_green.png"));

                // Switch to Nighttime Lighting to verify blazing HDR LED brightness & green/yellow glow cast at night!
                if (mainSun != null)
                {
                    mainSun.intensity = 0.08f;
                    mainSun.color = new Color(0.28f, 0.36f, 0.58f);
                }
                RenderSettings.ambientIntensity = 0.14f;
                RenderSettings.ambientSkyColor = new Color(0.05f, 0.07f, 0.14f);
                RenderSettings.ambientEquatorColor = new Color(0.04f, 0.05f, 0.10f);
                RenderSettings.ambientGroundColor = new Color(0.02f, 0.03f, 0.05f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.02f, 0.03f, 0.07f, 1f);

                // Shot 3: Nighttime COMPRA (Green) blazing LED glow from sidewalk
                if (ledCtrl != null)
                {
                    ledCtrl.SetNightPreviewOverride(true);
                    ledCtrl.ForceSlide(false);
                }
                cam.fieldOfView = 48f;
                cam.transform.position = new Vector3(-8.2f, 1.95f, -1.2f);
                cam.transform.LookAt(new Vector3(-11.2f, 3.35f, 3.4f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "santafe_led_night_glow.png"));

                // Shot 4: Nighttime VENTA (Yellow) blazing LED glow + Óptica Fashion shutter closed verification!
                if (ledCtrl != null) ledCtrl.ForceSlide(true);
                if (optShutter != null) optShutter.SetClosedImmediate(true);
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-4.8f, 1.95f, -0.8f);
                cam.transform.LookAt(new Vector3(-11.6f, 2.55f, 2.6f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "optica_closed_and_led_venta.png"));
            }
            finally
            {
                if (mainSun != null)
                {
                    mainSun.intensity = origSunIntensity;
                    mainSun.color = origSunColor;
                }
                RenderSettings.ambientIntensity = origAmbientIntensity;
                RenderSettings.ambientSkyColor = origAmbientSky;
                RenderSettings.ambientEquatorColor = origAmbientEq;
                RenderSettings.ambientGroundColor = origAmbientGround;
                cam.clearFlags = origClearFlags;
                if (ledCtrl != null) ledCtrl.SetNightPreviewOverride(null);
                if (optShutter != null) optShutter.SetClosedImmediate(false);
                UnityEngine.Object.DestroyImmediate(camGo);
            }
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

        private static void CreateArchedLunette(string name, Transform parent, float centerZ, float centerY, float widthZ, float heightY, Material archTexMat, Material sillMat, float localX = -11.96f)
        {
            GameObject archRoot = new GameObject(name);
            archRoot.transform.SetParent(parent, false);
            archRoot.transform.localPosition = new Vector3(localX, centerY, centerZ);
            archRoot.transform.localRotation = Quaternion.identity;

            // Textured Semicircular Arched Lunette Quad facing East (+X local) with alpha clipping outside arch
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Arch_FaceQuad";
            quad.transform.SetParent(archRoot.transform, false);
            quad.transform.localPosition = new Vector3(0.02f, 0f, 0f);
            quad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Normal points +X (East)
            quad.transform.localScale = new Vector3(widthZ, heightY, 1.0f);
            SetMaterial(quad, archTexMat);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());

            // 3D Protruding Stone Sill at Base of Arch
            CreateBox("Arch_StoneSill", archRoot.transform,
                new Vector3(0.04f, -heightY * 0.48f, 0f), new Vector3(0.12f, 0.10f, widthZ + 0.16f), sillMat);
        }

        private static void CreateWestWedgeAwning(string name, Transform parent, float centerZ, float topY, float widthZ, float projectionX, float dropY, Material canvasMat, Material valanceMat, float wallX = -12.0f)
        {
            GameObject awningRoot = new GameObject(name);
            awningRoot.transform.SetParent(parent, false);
            awningRoot.transform.localPosition = new Vector3(wallX, topY, centerZ);

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

        private static void CreateCfeMeter(string name, Transform parent, Vector3 mountPos, Material metalMat, Material glassDomeMat, Material faceMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = mountPos;

            // Galvanized metal square base socket mounted flush to pilaster
            CreateBox("CFE_SocketBase", root.transform, Vector3.zero, new Vector3(0.06f, 0.27f, 0.25f), metalMat);

            // Circular metallic bezel ring
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "CFE_BezelRing";
            ring.transform.SetParent(root.transform, false);
            ring.transform.localPosition = new Vector3(0.035f, 0f, 0f);
            ring.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            ring.transform.localScale = new Vector3(0.205f, 0.015f, 0.205f);
            SetMaterial(ring, metalMat);
            UnityEngine.Object.DestroyImmediate(ring.GetComponent<Collider>());

            // Circular dial face quad facing East (+X)
            var dial = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dial.name = "CFE_DialFace";
            dial.transform.SetParent(root.transform, false);
            dial.transform.localPosition = new Vector3(0.052f, 0f, 0f);
            dial.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            dial.transform.localScale = new Vector3(0.18f, 0.18f, 1.0f);
            SetMaterial(dial, faceMat);
            UnityEngine.Object.DestroyImmediate(dial.GetComponent<Collider>());

            // Translucent glass watt-hour meter dome protruding outward
            var dome = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dome.name = "CFE_GlassDome";
            dome.transform.SetParent(root.transform, false);
            dome.transform.localPosition = new Vector3(0.085f, 0f, 0f);
            dome.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            dome.transform.localScale = new Vector3(0.19f, 0.045f, 0.19f);
            SetMaterial(dome, glassDomeMat);
            UnityEngine.Object.DestroyImmediate(dome.GetComponent<Collider>());
        }

        private static void CreateDynamicRollUpShutter(string name, Transform parent, float wallX, float centerZ, float widthZ,
            float topY, float fullDropHeight, bool alwaysOpen, string linkedInteractionPointName, GameObject openSignVisual,
            Material trackMat, Material housingMat, Material shutterMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(wallX, 0f, centerZ);

            // Side vertical steel guide tracks flanking the doorway
            float halfW = widthZ * 0.5f;
            CreateBox("Shutter_Track_South", root.transform,
                new Vector3(0f, topY - fullDropHeight * 0.5f, -halfW), new Vector3(0.07f, fullDropHeight, 0.06f), trackMat);
            CreateBox("Shutter_Track_North", root.transform,
                new Vector3(0f, topY - fullDropHeight * 0.5f, halfW), new Vector3(0.07f, fullDropHeight, 0.06f), trackMat);

            // Overhead metal roll-up drum housing box tucked right behind the awning
            CreateBox("Shutter_RollHousing", root.transform,
                new Vector3(-0.04f, topY + 0.14f, 0f), new Vector3(0.26f, 0.28f, widthZ + 0.10f), housingMat);

            // Corrugated metal curtain (rolls down vertically from topY down to sidewalk when closed)
            GameObject curtain = GameObject.CreatePrimitive(PrimitiveType.Cube);
            curtain.name = "Shutter_Curtain";
            curtain.transform.SetParent(root.transform, false);
            curtain.transform.localPosition = new Vector3(0.01f, topY - 0.04f, 0f);
            curtain.transform.localScale = new Vector3(0.04f, 0.08f, widthZ - 0.06f);
            SetMaterial(curtain, shutterMat);
            // Keep curtain inactive when rolled up (managed dynamically by StorefrontShutterController)
            curtain.SetActive(false);

            var ctrl = root.AddComponent<StorefrontShutterController>();
            ctrl.alwaysOpen = alwaysOpen;
            ctrl.linkedInteractionPointName = linkedInteractionPointName;
            ctrl.shutterCurtain = curtain.transform;
            ctrl.openSignVisual = openSignVisual;
            ctrl.fullDropHeight = fullDropHeight;
            ctrl.rolledMinHeight = 0.08f;
            ctrl.topAnchorLocalY = topY;
            ctrl.SetClosedImmediate(false);
        }

        private static void CreateExchangeRateLedSign(string name, Transform parent, Vector3 pos, Material cabinetMat, Material armMat, Material flangeMat, Material liveLedBaseMat)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;

            // Two clearly visible horizontal square-steel support arms projecting from Building A wall (local X = -1.15m)
            // out to the inner side of the LED cabinet (local X = -0.525m), holding the sign cleanly out over the sidewalk!
            CreateBox("WallFlange_Top", root.transform, new Vector3(-1.14f, 0.23f, 0f), new Vector3(0.045f, 0.15f, 0.15f), flangeMat);
            CreateBox("WallFlange_Bottom", root.transform, new Vector3(-1.14f, -0.23f, 0f), new Vector3(0.045f, 0.15f, 0.15f), flangeMat);

            CreateBox("MountArm_Top", root.transform, new Vector3(-0.84f, 0.23f, 0f), new Vector3(0.68f, 0.055f, 0.055f), armMat);
            CreateBox("MountArm_Bottom", root.transform, new Vector3(-0.84f, -0.23f, 0f), new Vector3(0.68f, 0.055f, 0.055f), armMat);

            CreateBox("CabinetClamp_Top", root.transform, new Vector3(-0.535f, 0.23f, 0f), new Vector3(0.045f, 0.11f, 0.11f), flangeMat);
            CreateBox("CabinetClamp_Bottom", root.transform, new Vector3(-0.535f, -0.23f, 0f), new Vector3(0.045f, 0.11f, 0.11f), flangeMat);

            // Large matte-black double-sided outdoor LED cabinet (1.05m wide x 0.62m tall x 0.14m thick)
            CreateBox("LED_Cabinet", root.transform, Vector3.zero, new Vector3(1.05f, 0.62f, 0.14f), cabinetMat);

            // South-facing LED screen quad (-Z, visible walking North from Av. Juárez!)
            var southQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            southQuad.name = "LED_Face_South";
            southQuad.transform.SetParent(root.transform, false);
            southQuad.transform.localPosition = new Vector3(0f, 0f, -0.075f);
            southQuad.transform.localRotation = Quaternion.identity;
            southQuad.transform.localScale = new Vector3(0.98f, 0.56f, 1.0f);
            SetMaterial(southQuad, liveLedBaseMat);
            UnityEngine.Object.DestroyImmediate(southQuad.GetComponent<Collider>());

            // North-facing LED screen quad (+Z, visible walking South from Calle Gardenias!)
            var northQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            northQuad.name = "LED_Face_North";
            northQuad.transform.SetParent(root.transform, false);
            northQuad.transform.localPosition = new Vector3(0f, 0f, 0.075f);
            northQuad.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            northQuad.transform.localScale = new Vector3(0.98f, 0.56f, 1.0f);
            SetMaterial(northQuad, liveLedBaseMat);
            UnityEngine.Object.DestroyImmediate(northQuad.GetComponent<Collider>());

            // Dual real-time URP PointLights casting vivid green (COMPRA) / yellow (VENTA) glow at night!
            Light southLight = CreateLedGlowLight("LED_GlowLight_South", root.transform, new Vector3(0f, -0.06f, -0.42f));
            Light northLight = CreateLedGlowLight("LED_GlowLight_North", root.transform, new Vector3(0f, -0.06f, 0.42f));

            var ctrl = root.AddComponent<ExchangeRateLedSignController>();
            ctrl.baseLedMaterial = liveLedBaseMat;
            ctrl.slideIntervalSeconds = 4.0f;
            ctrl.pollIntervalSeconds = 300.0f;
            ctrl.displayFaceRenderers = new Renderer[]
            {
                southQuad.GetComponent<Renderer>(),
                northQuad.GetComponent<Renderer>()
            };
            ctrl.glowLights = new Light[] { southLight, northLight };
        }

        private static Light CreateLedGlowLight(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.18f, 1.0f, 0.36f, 1f);
            light.intensity = 0.0f;
            light.range = 4.6f;
            light.enabled = false;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel;
            return light;
        }

        private static Material CreateOrGetEmissiveLedBaseMaterial(string path)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", new Color(0.10f, 0.10f, 0.10f, 1.0f));
            mat.SetColor("_Color", new Color(0.10f, 0.10f, 0.10f, 1.0f));
            mat.SetColor("_EmissionColor", new Color(2.6f, 2.6f, 2.6f, 1.0f));
            mat.SetFloat("_Smoothness", 0.18f);
            mat.SetFloat("_Metallic", 0.0f);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetTransparentGlassMaterial(string path, Color color, float smoothness)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetFloat("_Surface", 1.0f); // Transparent
            mat.SetFloat("_Blend", 0.0f);   // Alpha
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0.0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0.08f);
            EditorUtility.SetDirty(mat);
            return mat;
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

        private static Material CreateOrGetTextureMaterial(string matPath, string texPath, bool alphaClip = false, bool emissive = false)
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
                    mat.SetColor("_EmissionColor", new Color(2.8f, 0.45f, 0.25f));
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                }
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetFloat("_Smoothness", emissive ? 0.35f : 0.12f);
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
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            rt.antiAliasing = 4;
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
