using System;
using System.Collections.Generic;
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
            Material opticaBronzeFrameMat = CreateOrGetColorMaterial("Assets/Settings/West_OpticaBronzeFrame.mat", new Color(0.16f, 0.11f, 0.09f), 0.42f);
            Material opticaCounterMat = CreateOrGetColorMaterial("Assets/Settings/West_OpticaCounterGrey.mat", new Color(0.45f, 0.48f, 0.51f), 0.28f);
            Material opticaChairBlueMat = CreateOrGetColorMaterial("Assets/Settings/West_OpticaChairBlue.mat", new Color(0.16f, 0.42f, 0.74f), 0.24f);
            Material opticaTubeLightMat = CreateOrGetColorMaterial("Assets/Settings/West_OpticaTubeLight.mat", new Color(0.98f, 0.99f, 1.0f), 0.40f);
            opticaTubeLightMat.EnableKeyword("_EMISSION");
            opticaTubeLightMat.SetColor("_EmissionColor", new Color(1.9f, 2.0f, 2.1f, 1.0f));
            opticaTubeLightMat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(opticaTubeLightMat);
            Material santaFeFloorRedMat = CreateOrGetColorMaterial("Assets/Settings/West_SantaFeFloorRed.mat", new Color(0.56f, 0.11f, 0.09f), 0.16f);
            Material santaFeArmorSteelMat = CreateOrGetColorMaterial("Assets/Settings/West_SantaFeArmorSteel.mat", new Color(0.50f, 0.53f, 0.56f), 0.45f);
            Material clearGlassMat = CreateOrGetTransparentGlassMaterial("Assets/Settings/West_StorefrontClearGlass.mat", new Color(0.24f, 0.34f, 0.40f, 0.32f), 0.90f);
            Material cfeDomeGlassMat = CreateOrGetTransparentGlassMaterial("Assets/Settings/West_CFE_DomeGlass.mat", new Color(0.84f, 0.90f, 0.92f, 0.34f), 0.92f);
            Material greenShopWallMat = CreateOrGetColorMaterial("Assets/Settings/West_GreenShopInterior.mat", new Color(0.28f, 0.62f, 0.34f), 0.14f);
            Material dentalClinicBlueMat = CreateOrGetColorMaterial("Assets/Settings/West_DentalMexicoPosterBlue.mat", new Color(0.20f, 0.56f, 0.80f), 0.18f);
            Material warmPeachStuccoMat = CreateOrGetColorMaterial("Assets/Settings/West_WarmPeachStucco.mat", new Color(0.84f, 0.72f, 0.60f), 0.08f);
            Material bronzeShutterMat = CreateOrGetColorMaterial("Assets/Settings/West_BronzeShutterMetal.mat", new Color(0.46f, 0.30f, 0.22f), 0.36f);
            Material doormatDarkMat = CreateOrGetColorMaterial("Assets/Settings/West_DoormatDark.mat", new Color(0.14f, 0.15f, 0.16f), 0.04f);

            // Ensure RGBA textures import cleanly with alpha transparency & no compression artifacts
            string[] alphaTexPaths = new string[]
            {
                "Assets/Textures/Environment/T_West_Sign_DentalMexico_3D.png",
                "Assets/Textures/Environment/T_West_FarmaciaJuarez_Window.png",
                "Assets/Textures/Environment/T_West_Optica_Horario.png"
            };
            foreach (string tp in alphaTexPaths)
            {
                if (AssetImporter.GetAtPath(tp) is TextureImporter ti)
                {
                    if (!ti.alphaIsTransparency || ti.wrapMode != TextureWrapMode.Clamp || ti.textureCompression != TextureImporterCompression.Uncompressed)
                    {
                        ti.alphaIsTransparency = true;
                        ti.wrapMode = TextureWrapMode.Clamp;
                        ti.textureCompression = TextureImporterCompression.Uncompressed;
                        ti.SaveAndReimport();
                    }
                }
            }

            // 2. Setup Texture Materials
            Material signFarmaciaJuarezMat = CreateOrGetEmissiveSignTextureMaterial("Assets/Settings/West_Sign_FarmaciaJuarez.mat", "Assets/Textures/Environment/T_West_Sign_FarmaciaJuarez.png", 0.35f);
            Material farmaciaJuarezWindowMat = CreateOrGetTransparentTextureMaterial("Assets/Settings/West_FarmaciaJuarez_Window.mat", "Assets/Textures/Environment/T_West_FarmaciaJuarez_Window.png");
            Material pilasterPharmacyVertMat = CreateOrGetTextureMaterial("Assets/Settings/West_Pilaster_PharmacyVertical.mat", "Assets/Textures/Environment/T_West_Pilaster_PharmacyVertical.png");
            Material dentalDoorMat = CreateOrGetTextureMaterial("Assets/Settings/West_DentalMexico_Door.mat", "Assets/Textures/Environment/T_West_DentalMexico_Door.png");
            Material dentalDividerMat = CreateOrGetTextureMaterial("Assets/Settings/West_DentalMexico_Divider.mat", "Assets/Textures/Environment/T_West_DentalMexico_Divider.png");
            Material dentalWindowMat = CreateOrGetTextureMaterial("Assets/Settings/West_DentalMexico_Window.mat", "Assets/Textures/Environment/T_West_DentalMexico_Window.png");
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
            Material whiteMetalShutterMat = CreateOrGetTextureMaterial("Assets/Settings/West_WhiteMetalShutter.mat", "Assets/Textures/Environment/T_West_MetalShutter.png");
            whiteMetalShutterMat.SetColor("_BaseColor", new Color(0.98f, 0.99f, 1.0f, 1.0f));
            whiteMetalShutterMat.SetColor("_Color", new Color(0.98f, 0.99f, 1.0f, 1.0f));
            whiteMetalShutterMat.SetFloat("_Smoothness", 0.28f);
            EditorUtility.SetDirty(whiteMetalShutterMat);
            Material ribbonWindowsMat = CreateOrGetTextureMaterial("Assets/Settings/West_RibbonWindows.mat", "Assets/Textures/Environment/T_West_RibbonWindows.png");
            Material openLedMat = CreateOrGetTextureMaterial("Assets/Settings/West_Sign_OpenLED.mat", "Assets/Textures/Environment/T_West_Sign_OpenLED.png", alphaClip: false, emissive: true);
            Material opticaFloorMat = CreateOrGetTextureMaterial("Assets/Settings/West_OpticaInteriorFloor.mat", "Assets/Textures/Environment/T_West_Optica_TileFloor.png");
            opticaFloorMat.mainTextureScale = new Vector2(3.5f, 6.0f);
            opticaFloorMat.SetFloat("_Smoothness", 0.30f);
            EditorUtility.SetDirty(opticaFloorMat);
            Material opticaAbiertoMat = CreateOrGetEmissiveSignTextureMaterial("Assets/Settings/West_Optica_Abierto.mat", "Assets/Textures/Environment/T_West_Optica_Abierto.png", 1.85f);
            Material opticaHorarioMat = CreateOrGetTransparentTextureMaterial("Assets/Settings/West_Optica_Horario.mat", "Assets/Textures/Environment/T_West_Optica_Horario.png");
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

            // Required collider object for CorridorVerificationTest & VerifyWestNorthBlock: "Optica_MainBuilding"
            // Increment 5.23.0: Recessed behind X = -19.20m so Bay 4 ("Óptica Fashion") has a genuine hollow
            // 7.18m-deep walkable retail showroom + rear consultation/calibration room (X = -19.20..-12.02m,
            // Z = -1.80..+3.05m, Y = 0.20..3.04m), while preserving the full exterior structural envelope and
            // solid colliders around neighboring shops and upper floor!
            GameObject optMain = CreateBox("Optica_MainBuilding", opticaRoot.transform,
                new Vector3(-21.60f, 3.9f, -4.375f), new Vector3(4.80f, 7.8f, 15.55f), creamStuccoMat, keepCollider: true);

            // Container for all West Block architectural details
            GameObject blockArch = new GameObject("WestBlock_HistoricArchitecture");
            blockArch.transform.SetParent(opticaRoot.transform);
            blockArch.transform.localPosition = Vector3.zero;

            // Structural masonry surrounding the deepened Óptica Fashion hollow cavity (X = -19.20..-14.40m)
            // Straight south wing stops cleanly at Z = -11.20m where the 24.7° South Chamfer begins!
            CreateBox("Optica_MainBuilding_SouthWing", blockArch.transform,
                new Vector3(-16.80f, 3.90f, -6.50f), new Vector3(4.80f, 7.80f, 9.40f), creamStuccoMat, keepCollider: true);
            CreateBox("Optica_MainBuilding_NorthPier", blockArch.transform,
                new Vector3(-16.80f, 3.90f, 3.225f), new Vector3(4.80f, 7.80f, 0.35f), creamStuccoMat, keepCollider: true);
            CreateBox("Optica_MainBuilding_UpperAboveStore", blockArch.transform,
                new Vector3(-16.80f, 5.46f, 0.625f), new Vector3(4.80f, 4.68f, 4.85f), creamStuccoMat, keepCollider: true);

            // Front structural wall surround around Bay 4's hollow glass showroom (X = -14.40..-12.00m)
            // South of Óptica (Z = -11.20..-1.80m, length 9.40m, center -6.50m): upper wall flush at X = -12.00m, ground recessed behind X = -12.45m
            CreateBox("BuildingA_FrontWall_SouthOfOptica_Upper", blockArch.transform,
                new Vector3(-13.20f, 5.275f, -6.50f), new Vector3(2.40f, 5.05f, 9.40f), creamStuccoMat, keepCollider: true);
            CreateBox("BuildingA_FrontWall_SouthOfOptica", blockArch.transform,
                new Vector3(-13.425f, 1.375f, -6.50f), new Vector3(1.95f, 2.75f, 9.40f), creamStuccoMat, keepCollider: true);
            CreateBox("BuildingA_FrontWall_SouthGroundFloor", blockArch.transform,
                new Vector3(-12.22f, 0.10f, -6.50f), new Vector3(0.50f, 0.20f, 9.40f), concreteMat, keepCollider: true);
            CreateBox("BuildingA_FrontWall_SouthGroundCeiling", blockArch.transform,
                new Vector3(-12.22f, 2.74f, -6.50f), new Vector3(0.50f, 0.08f, 9.40f), whiteFrameMat);
            // Upper story above Óptica starts at Y = 3.12m so the reconstructed interior attains 2.84m clear ceiling height (Y=0.20..3.04m)!
            CreateBox("BuildingA_FrontWall_AboveOptica", blockArch.transform,
                new Vector3(-13.20f, 5.46f, 0.80f), new Vector3(2.40f, 4.68f, 5.20f), creamStuccoMat, keepCollider: true);
            CreateBox("BuildingA_FrontWall_OpticaNorthPier", blockArch.transform,
                new Vector3(-13.20f, 1.56f, 3.225f), new Vector3(2.40f, 3.12f, 0.35f), creamStuccoMat, keepCollider: true);

            // -------------------------------------------------------------------------
            // BUILDING A: 2-STORY NEOCLASSICAL BUILDING WITH SYMMETRICAL SOUTH & NORTH DIAGONAL RETURNS
            // Straight facade spans Z = -11.20m to +3.40m (Length = 14.60m, Center Z = -3.90m, Height = 7.8m)
            // South of Z = -11.20m, the facade miters at 24.7° into the 2.75m-wide South Chamfer (Z = -11.20..-13.70m)!
            // -------------------------------------------------------------------------

            // Top Stepped Dark Grey Stone Parapet Cornice along straight East facade (Z = -11.20m to +3.45m)
            CreateBox("BuildingA_Cornice_Band1", blockArch.transform,
                new Vector3(-11.88f, 7.20f, -3.875f), new Vector3(0.28f, 0.26f, 14.65f), darkCorniceMat);
            CreateBox("BuildingA_Cornice_Band2", blockArch.transform,
                new Vector3(-11.84f, 7.48f, -3.875f), new Vector3(0.36f, 0.24f, 14.70f), stoneMasonryMat);
            CreateBox("BuildingA_Cornice_Band3", blockArch.transform,
                new Vector3(-11.80f, 7.74f, -3.875f), new Vector3(0.42f, 0.22f, 14.75f), darkCorniceMat);

            // Decorative Recessed Upper Wall Relief Panels (between arches and top cornice, Y = 6.18m)
            float[] reliefCentersZ = new float[] { -9.91f, -5.37f, 0.85f };
            float[] reliefWidthsZ = new float[] { 2.05f, 6.10f, 4.20f };
            for (int i = 0; i < reliefCentersZ.Length; i++)
            {
                CreateBox($"BuildingA_UpperReliefPanel_{i}", blockArch.transform,
                    new Vector3(-11.96f, 6.18f, reliefCentersZ[i]), new Vector3(0.06f, 0.92f, reliefWidthsZ[i]), weatheredCreamMat);
            }

            // Horizontal Stringcourse Molding Below Arched Windows along straight East facade (Z = -11.20m to +3.45m)
            CreateBox("BuildingA_ArchSillCourse", blockArch.transform,
                new Vector3(-11.92f, 3.85f, -3.875f), new Vector3(0.18f, 0.10f, 14.65f), darkCorniceMat);

            // 4 Straight Semicircular Arched Lunette Windows on Upper Floor (plus angled South chamfer arch & North diagonal arch!)
            CreateArchedLunette("ArchWindow_2_GreenShop", blockArch.transform, centerZ: -9.91f, centerY: 4.66f, widthZ: 2.05f, heightY: 1.48f, archWindowMat, darkCorniceMat);
            CreateArchedLunette("ArchWindow_3_DentalMexico", blockArch.transform, centerZ: -7.06f, centerY: 4.70f, widthZ: 3.10f, heightY: 1.62f, archWindowMat, darkCorniceMat);
            CreateArchedLunette("ArchWindow_4_Pharmacy", blockArch.transform, centerZ: -3.68f, centerY: 4.72f, widthZ: 2.80f, heightY: 1.62f, archWindowMat, darkCorniceMat);
            CreateArchedLunette("ArchWindow_5_Optica", blockArch.transform, centerZ: 0.85f, centerY: 4.74f, widthZ: 3.65f, heightY: 1.75f, archWindowMat, darkCorniceMat);

            // -------------------------------------------------------------------------
            // BUILDING A SOUTH CHAMFER BAY, 2-STORY LATERAL FLANK WALL, 6-DIGIT KEYPAD DOOR
            // & WIDE SEMI-TRIANGULAR SECRET ROOM (Z = -13.70..-11.20m, X = -13.15..-12.00m)
            // Delegated to SecretWedgeRoomBuilder for clean idempotent construction.
            // -------------------------------------------------------------------------
            // Corner vertical framing pier at Building A South chamfer vertex (Z = -11.07m) with vertical "PHARMACY" text!
            CreateBox("Pilaster_BuildingA_SouthCorner", blockArch.transform,
                new Vector3(-11.95f, 2.335f, -11.07f), new Vector3(0.20f, 2.83f, 0.30f), weatheredCreamMat);
            CreateBox("Pilaster_BuildingA_SouthCorner_Base", blockArch.transform,
                new Vector3(-11.93f, 0.56f, -11.07f), new Vector3(0.24f, 0.72f, 0.34f), darkZocaloMat);
            GameObject pharmacyVertQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pharmacyVertQuad.name = "Pilaster_PharmacyVertical_Sign";
            pharmacyVertQuad.transform.SetParent(blockArch.transform, false);
            pharmacyVertQuad.transform.localPosition = new Vector3(-11.845f, 1.66f, -11.07f);
            pharmacyVertQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            pharmacyVertQuad.transform.localScale = new Vector3(0.24f, 1.44f, 1.0f);
            SetMaterial(pharmacyVertQuad, pilasterPharmacyVertMat);
            UnityEngine.Object.DestroyImmediate(pharmacyVertQuad.GetComponent<Collider>());

            // =========================================================================
            // 3 AUTHENTIC ESTABLISHMENTS (Z = -10.92m to -5.44m, total width 5.48m):
            //   1. Farmacia Juárez                (Z = -10.92 .. -8.90m, 2.02m, own beige awning & white FJ Juárez lightbox)
            //   Dividing Pier + Live LED Sign     (Z = -8.90  .. -8.68m, 0.22m, cantilevered COMPRA green / VENTA magenta LED)
            //   2. Casa de Cambio (sin nombre)    (Z = -8.68  .. -7.18m, 1.50m, warm peach-cream plaster, bronze shutter & teller nook)
            //   3. Dental México                  (Z = -7.18  .. -5.44m, 1.74m, black/gold storefront + 3D blue channel letters
            //                                      "DENTAL MEXICO" invading the upper fascia across Casa de Cambio & Dental México!)
            // =========================================================================

            // --- ESTABLISHMENT 1 (Z = -10.92..-8.90m, center -9.91m): "Farmacia Juárez" ---
            CreateBox("FarmaciaJuarez_InteriorBack", blockArch.transform,
                new Vector3(-12.38f, 1.38f, -9.91f), new Vector3(0.08f, 2.36f, 2.00f), whiteFrameMat);
            float[] fjShelfYs = new float[] { 0.84f, 1.34f, 1.84f };
            for (int s = 0; s < fjShelfYs.Length; s++)
            {
                CreateBox($"FarmaciaJuarez_Shelf_{s}", blockArch.transform,
                    new Vector3(-12.24f, fjShelfYs[s], -9.50f), new Vector3(0.24f, 0.04f, 1.08f), aluminumMat);
                CreateBox($"FarmaciaJuarez_StockG_{s}", blockArch.transform,
                    new Vector3(-12.22f, fjShelfYs[s] + 0.13f, -9.80f), new Vector3(0.14f, 0.22f, 0.28f), greenShopWallMat);
                CreateBox($"FarmaciaJuarez_StockB_{s}", blockArch.transform,
                    new Vector3(-12.22f, fjShelfYs[s] + 0.11f, -9.46f), new Vector3(0.14f, 0.18f, 0.24f), dentalClinicBlueMat);
                CreateBox($"FarmaciaJuarez_StockW_{s}", blockArch.transform,
                    new Vector3(-12.22f, fjShelfYs[s] + 0.12f, -9.14f), new Vector3(0.14f, 0.20f, 0.24f), weatheredCreamMat);
            }
            // Aluminum-framed glass entrance door (South) + display window (North)
            CreateBox("FarmaciaJuarez_StorefrontGlass", blockArch.transform,
                new Vector3(-11.96f, 1.38f, -9.91f), new Vector3(0.04f, 2.36f, 2.00f), clearGlassMat);
            float[] fjMullions = new float[] { -10.90f, -10.08f, -8.92f };
            for (int i = 0; i < fjMullions.Length; i++)
            {
                CreateBox($"FarmaciaJuarez_Mullion_{i}", blockArch.transform,
                    new Vector3(-11.94f, 1.38f, fjMullions[i]), new Vector3(0.10f, 2.36f, 0.06f), aluminumMat);
            }
            CreateBox("FarmaciaJuarez_DoorMidRail", blockArch.transform,
                new Vector3(-11.94f, 1.18f, -10.49f), new Vector3(0.07f, 0.05f, 0.80f), aluminumMat);
            CreateBox("FarmaciaJuarez_DoorHandle", blockArch.transform,
                new Vector3(-11.89f, 1.18f, -10.16f), new Vector3(0.04f, 0.42f, 0.03f), aluminumMat);
            CreateBox("FarmaciaJuarez_DoorHeaderBar", blockArch.transform,
                new Vector3(-11.91f, 2.48f, -10.49f), new Vector3(0.14f, 0.24f, 0.86f), opticaBlackFrameMat);
            CreateBox("FarmaciaJuarez_LowerKickplate", blockArch.transform,
                new Vector3(-11.94f, 0.34f, -9.91f), new Vector3(0.08f, 0.28f, 2.00f), aluminumMat);
            // Green/white "FJ Farmacia Juárez" window vinyl graphic overlay on right display glass
            GameObject fjWinDecal = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fjWinDecal.name = "FarmaciaJuarez_WindowDecal";
            fjWinDecal.transform.SetParent(blockArch.transform, false);
            fjWinDecal.transform.localPosition = new Vector3(-11.93f, 1.46f, -9.50f);
            fjWinDecal.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            fjWinDecal.transform.localScale = new Vector3(1.10f, 1.92f, 1.0f);
            SetMaterial(fjWinDecal, farmaciaJuarezWindowMat);
            UnityEngine.Object.DestroyImmediate(fjWinDecal.GetComponent<Collider>());

            // Independent Beige Wedge Awning covering ONLY Farmacia Juárez (clear of South corner pilaster!)
            CreateWestWedgeAwning("Awning_FarmaciaJuarez", blockArch.transform,
                centerZ: -9.89f, topY: 3.16f, widthZ: 1.98f, projectionX: 1.25f, dropY: 0.68f, beigeAwningMat, awningValanceMat);

            // Illuminated White Lightbox Sign "FJ Juárez" directly above Farmacia Juárez's awning
            CreateIlluminatedLightboxSign("Sign_FarmaciaJuarez", blockArch.transform,
                centerZ: -9.90f, centerY: 3.48f, widthZ: 1.82f, heightY: 0.54f,
                cabinetMat: whiteFrameMat, faceTexMat: signFarmaciaJuarezMat,
                nightGlowColor: new Color(0.55f, 1.0f, 0.68f, 1f));

            // Dynamic White Roll-Up Metal Shutter for Farmacia Juárez (synced to backend pharmacy:farmacia-juarez: Mon-Fri 8-20, Sun 8-16, Sat closed!)
            CreateDynamicRollUpShutter("FarmaciaJuarez_RollUpShutter_Dynamic", blockArch.transform,
                wallX: -11.88f, centerZ: -9.91f, widthZ: 1.98f, topY: 2.52f, fullDropHeight: 2.32f,
                alwaysOpen: false, linkedInteractionPointName: "", openSignVisual: null,
                trackMat: whiteFrameMat, housingMat: whiteFrameMat, shutterMat: whiteMetalShutterMat,
                linkedBusinessId: "pharmacy:farmacia-juarez");

            // --- DIVIDING PILASTER + CANTILEVERED LIVE EXCHANGE RATE LED SIGN (Z = -8.79m) ---
            CreateBox("Pilaster_FarmaciaJuarez_Cambio", blockArch.transform,
                new Vector3(-11.94f, 1.92f, -8.79f), new Vector3(0.18f, 3.45f, 0.22f), warmPeachStuccoMat);
            CreateBox("Pilaster_FarmaciaJuarez_Cambio_Base", blockArch.transform,
                new Vector3(-11.92f, 0.56f, -8.79f), new Vector3(0.22f, 0.72f, 0.26f), stoneMasonryMat);

            // Cantilevered double-sided live LED sign (COMPRA green / VENTA magenta-purple as in current street photo!)
            CreateExchangeRateLedSign("CasaCambio_Sur_ExchangeLedSign", blockArch.transform,
                pos: new Vector3(-10.82f, 3.48f, -8.79f),
                cabinetMat: opticaBlackFrameMat, armMat: aluminumMat, flangeMat: darkCorniceMat,
                liveLedBaseMat: liveLedBaseMat,
                layoutMode: ExchangeLedLayoutMode.AlternatingBuyGreenSellMagenta);

            // --- ESTABLISHMENT 2 (Z = -8.68..-7.18m, width 1.50m): Unnamed "Casa de Cambio" ---
            // Warm cream/peach-terracotta plastered entrance with rolled bronze shutter doorway (South) & teller alcove (North)
            CreateBox("CasaCambio_Sur_SouthJamb", blockArch.transform,
                new Vector3(-12.16f, 1.42f, -8.63f), new Vector3(0.50f, 2.48f, 0.12f), warmPeachStuccoMat);
            CreateBox("CasaCambio_Sur_CenterDivider", blockArch.transform,
                new Vector3(-12.16f, 1.42f, -7.98f), new Vector3(0.50f, 2.48f, 0.12f), warmPeachStuccoMat);
            CreateBox("CasaCambio_Sur_NorthJamb", blockArch.transform,
                new Vector3(-12.16f, 1.42f, -7.25f), new Vector3(0.50f, 2.48f, 0.12f), warmPeachStuccoMat);
            // Left narrow doorway (Z = -8.57..-8.04m) with dark interior & rolled-up bronze metal shutter drum at top!
            CreateBox("CasaCambio_Sur_DoorRecess", blockArch.transform,
                new Vector3(-12.38f, 1.38f, -8.305f), new Vector3(0.08f, 2.36f, 0.56f), interiorDarkMat);
            CreateBox("CasaCambio_Sur_DoorTrackS", blockArch.transform,
                new Vector3(-11.96f, 1.24f, -8.55f), new Vector3(0.06f, 2.08f, 0.05f), bronzeShutterMat);
            CreateBox("CasaCambio_Sur_DoorTrackN", blockArch.transform,
                new Vector3(-11.96f, 1.24f, -8.06f), new Vector3(0.06f, 2.08f, 0.05f), bronzeShutterMat);
            var cambioShutterRoll = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cambioShutterRoll.name = "CasaCambio_Sur_RolledShutterDrum";
            cambioShutterRoll.transform.SetParent(blockArch.transform, false);
            cambioShutterRoll.transform.localPosition = new Vector3(-11.96f, 2.38f, -8.305f);
            cambioShutterRoll.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            cambioShutterRoll.transform.localScale = new Vector3(0.24f, 0.30f, 0.24f);
            SetMaterial(cambioShutterRoll, bronzeShutterMat);
            UnityEngine.Object.DestroyImmediate(cambioShutterRoll.GetComponent<Collider>());
            // Right walk-in recessed teller alcove (Z = -7.92..-7.31m) with cream wall, interior counter & dark teller window
            CreateBox("CasaCambio_Sur_AlcoveBackWall", blockArch.transform,
                new Vector3(-12.39f, 1.42f, -7.615f), new Vector3(0.08f, 2.44f, 0.64f), creamStuccoMat);
            CreateBox("CasaCambio_Sur_TellerCounter", blockArch.transform,
                new Vector3(-12.24f, 0.64f, -7.615f), new Vector3(0.28f, 0.88f, 0.60f), weatheredCreamMat);
            CreateBox("CasaCambio_Sur_TellerLedge", blockArch.transform,
                new Vector3(-12.22f, 1.10f, -7.615f), new Vector3(0.32f, 0.05f, 0.62f), darkZocaloMat);
            CreateBox("CasaCambio_Sur_TellerWindow", blockArch.transform,
                new Vector3(-12.34f, 1.56f, -7.70f), new Vector3(0.04f, 0.62f, 0.34f), interiorDarkMat);
            CreateBox("CasaCambio_Sur_HeaderBeam", blockArch.transform,
                new Vector3(-12.02f, 2.58f, -7.94f), new Vector3(0.22f, 0.14f, 1.48f), warmPeachStuccoMat);

            // Dynamic White Roll-Up Metal Shutter for unnamed Casa de Cambio (installed raised/open 24/7 per alwaysOpen = true!)
            CreateDynamicRollUpShutter("CasaCambio_Sur_RollUpShutter_Dynamic", blockArch.transform,
                wallX: -11.88f, centerZ: -7.94f, widthZ: 1.44f, topY: 2.52f, fullDropHeight: 2.32f,
                alwaysOpen: true, linkedInteractionPointName: "", openSignVisual: null,
                trackMat: whiteFrameMat, housingMat: whiteFrameMat, shutterMat: whiteMetalShutterMat,
                linkedBusinessId: "exchange:casa-cambio-juarez-sur");

            // --- ESTABLISHMENT 3 (Z = -7.18..-5.44m, width 1.74m): "Dental México" ---
            // Modern black/gold clinic facade with aluminum framing, bronze rolled-up overhead shutter & black entry doormat
            CreateBox("DentalMexico_BackWall", blockArch.transform,
                new Vector3(-12.14f, 1.38f, -6.31f), new Vector3(0.08f, 2.36f, 1.74f), interiorDarkMat);
            // Left aluminum-framed glass door with modern black/gold HORARIO + tooth vinyl graphic
            GameObject dentalDoorQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dentalDoorQuad.name = "DentalMexico_DoorPanel";
            dentalDoorQuad.transform.SetParent(blockArch.transform, false);
            dentalDoorQuad.transform.localPosition = new Vector3(-11.95f, 1.32f, -6.775f);
            dentalDoorQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            dentalDoorQuad.transform.localScale = new Vector3(0.74f, 2.18f, 1.0f);
            SetMaterial(dentalDoorQuad, dentalDoorMat);
            UnityEngine.Object.DestroyImmediate(dentalDoorQuad.GetComponent<Collider>());
            // Center vertical black divider strip reading "DENTAL MEXICO" vertically in gold
            GameObject dentalDivQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dentalDivQuad.name = "DentalMexico_VerticalDivider";
            dentalDivQuad.transform.SetParent(blockArch.transform, false);
            dentalDivQuad.transform.localPosition = new Vector3(-11.945f, 1.32f, -6.305f);
            dentalDivQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            dentalDivQuad.transform.localScale = new Vector3(0.16f, 2.18f, 1.0f);
            SetMaterial(dentalDivQuad, dentalDividerMat);
            UnityEngine.Object.DestroyImmediate(dentalDivQuad.GetComponent<Collider>());
            // Right display window with modern black/gold services list & smile bands
            GameObject dentalWinQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            dentalWinQuad.name = "DentalMexico_WindowPanel";
            dentalWinQuad.transform.SetParent(blockArch.transform, false);
            dentalWinQuad.transform.localPosition = new Vector3(-11.95f, 1.32f, -5.84f);
            dentalWinQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            dentalWinQuad.transform.localScale = new Vector3(0.73f, 2.18f, 1.0f);
            SetMaterial(dentalWinQuad, dentalWindowMat);
            UnityEngine.Object.DestroyImmediate(dentalWinQuad.GetComponent<Collider>());
            // Crisp silver aluminum storefront mullions, threshold, header & bronze rolled-up security shutter drum
            float[] dmMullions = new float[] { -7.155f, -6.395f, -6.215f, -5.465f };
            for (int i = 0; i < dmMullions.Length; i++)
            {
                CreateBox($"DentalMexico_Mullion_{i}", blockArch.transform,
                    new Vector3(-11.93f, 1.32f, dmMullions[i]), new Vector3(0.08f, 2.24f, 0.05f), aluminumMat);
            }
            CreateBox("DentalMexico_FrameTop", blockArch.transform,
                new Vector3(-11.93f, 2.42f, -6.31f), new Vector3(0.08f, 0.06f, 1.74f), aluminumMat);
            CreateBox("DentalMexico_Threshold", blockArch.transform,
                new Vector3(-11.93f, 0.23f, -6.31f), new Vector3(0.09f, 0.06f, 1.74f), aluminumMat);
            var dmShutterRoll = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            dmShutterRoll.name = "DentalMexico_RolledShutterDrum";
            dmShutterRoll.transform.SetParent(blockArch.transform, false);
            dmShutterRoll.transform.localPosition = new Vector3(-11.95f, 2.54f, -6.31f);
            dmShutterRoll.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            dmShutterRoll.transform.localScale = new Vector3(0.20f, 0.86f, 0.20f);
            SetMaterial(dmShutterRoll, bronzeShutterMat);
            UnityEngine.Object.DestroyImmediate(dmShutterRoll.GetComponent<Collider>());
            // Dark rubber welcome doormat in front of Dental México's door
            CreateBox("DentalMexico_Doormat", blockArch.transform,
                new Vector3(-11.60f, 0.208f, -6.775f), new Vector3(0.56f, 0.016f, 0.78f), doormatDarkMat);

            // Dynamic White Roll-Up Metal Shutter for Dental México (synced to backend dental:dental-mexico: Mon-Fri 9-17, Sat 9-15, Sun closed!)
            CreateDynamicRollUpShutter("DentalMexico_RollUpShutter_Dynamic", blockArch.transform,
                wallX: -11.88f, centerZ: -6.31f, widthZ: 1.72f, topY: 2.52f, fullDropHeight: 2.32f,
                alwaysOpen: false, linkedInteractionPointName: "", openSignVisual: null,
                trackMat: whiteFrameMat, housingMat: whiteFrameMat, shutterMat: whiteMetalShutterMat,
                linkedBusinessId: "dental:dental-mexico");

            // Second Beige Wedge Awning covering Casa de Cambio (sin nombre) + Dental México together!
            CreateWestWedgeAwning("Awning_Bay12_DentalMexico", blockArch.transform,
                centerZ: -7.06f, topY: 3.16f, widthZ: 3.24f, projectionX: 1.32f, dropY: 0.70f, beigeAwningMat, awningValanceMat);

            // NEW 3D Illuminated Channel-Letter Sign "DENTAL MEXICO" (bright electric sky-blue face + black 3D returns,
            // spanning the ENTIRE upper fascia across BOTH the unnamed Casa de Cambio AND Dental México just like real life!)
            CreateExtrudedChannelLetterSign(
                name: "Sign_DentalMexico",
                parent: blockArch.transform,
                wallX: -12.00f,
                centerY: 3.48f,
                centerZ: -7.06f,
                widthZ: 3.08f,
                heightY: 0.50f,
                extrudeDepthX: 0.11f,
                texAssetPath: "Assets/Textures/Environment/T_West_Sign_DentalMexico_3D.png",
                faceMatPath: "Assets/Settings/West_Sign_DentalMexico_3DFace.mat",
                sideMatPath: "Assets/Settings/West_Sign_DentalMexico_3DSide.mat",
                meshAssetPath: "Assets/Settings/DentalMexico_3DReturns_Mesh.asset",
                sideReturnColor: new Color(0.08f, 0.09f, 0.11f),
                nightGlowColor: new Color(0.10f, 0.62f, 1.0f, 1f));

            // Dividing Pilaster between Dental México & Farma PHARMACY (Z = -5.28m) + authentic 3D CFE meter!
            CreateBox("Pilaster_Bay2_3", blockArch.transform,
                new Vector3(-11.93f, 1.9f, -5.28f), new Vector3(0.18f, 3.4f, 0.32f), warmPeachStuccoMat);
            CreateBox("Pilaster_Bay2_3_Base", blockArch.transform,
                new Vector3(-11.91f, 0.56f, -5.28f), new Vector3(0.20f, 0.72f, 0.36f), stoneMasonryMat);
            CreateCfeMeter("CFE_Meter_DentalMexico", blockArch.transform,
                new Vector3(-11.83f, 1.48f, -5.28f), aluminumMat, cfeDomeGlassMat, cfeMeterFaceMat);

            // --- Bay 3 (Compacted to authentic 1-arch width, Z = -5.12m to -2.24m, center Z = -3.68m): "Farma | PHARMACY" ---
            CreateBox("Bay3_OpenInteriorRecess", blockArch.transform,
                new Vector3(-12.35f, 1.42f, -3.02f), new Vector3(0.8f, 2.45f, 1.52f), interiorDarkMat);
            CreateBox("Bay3_InteriorCounter", blockArch.transform,
                new Vector3(-12.25f, 0.68f, -3.00f), new Vector3(0.45f, 0.95f, 1.15f), terracottaPlaqueMat);
            // Left Window with Yellow Promotional Poster (Z = -5.08m to -3.78m)
            CreateBox("Bay3_LeftWindowGlass", blockArch.transform,
                new Vector3(-11.96f, 1.42f, -4.43f), new Vector3(0.08f, 2.40f, 1.30f), darkGlassMat);
            GameObject pharmPoster = GameObject.CreatePrimitive(PrimitiveType.Quad);
            pharmPoster.name = "Bay3_YellowPromotionalPoster";
            pharmPoster.transform.SetParent(blockArch.transform);
            pharmPoster.transform.localPosition = new Vector3(-11.89f, 1.38f, -4.43f);
            pharmPoster.transform.localRotation = Quaternion.Euler(0f, -90f, 0f); // Faces East (+X)
            pharmPoster.transform.localScale = new Vector3(1.06f, 1.50f, 1.0f);
            SetMaterial(pharmPoster, posterPharmacyMat);
            UnityEngine.Object.DestroyImmediate(pharmPoster.GetComponent<Collider>());
            // Beige Wedge Awning
            CreateWestWedgeAwning("Awning_Bay3_Pharmacy", blockArch.transform,
                centerZ: -3.68f, topY: 3.16f, widthZ: 2.86f, projectionX: 1.32f, dropY: 0.70f, beigeAwningMat, awningValanceMat);
            // Two-Tone Terracotta & Bright Yellow Sign "Farma | PHARMACY"
            CreateWestSign("Sign_Pharmacy", blockArch.transform,
                centerZ: -3.68f, centerY: 3.52f, widthZ: 2.68f, heightY: 0.62f, terracottaPlaqueMat, signPharmacyMat);

            // Dividing Pilaster between Bay 3 & Bay 4 (Z = -2.02m) + Twin CFE Electric Meters (visible on left edge of photo!)
            CreateBox("Pilaster_Bay3_4", blockArch.transform,
                new Vector3(-11.92f, 1.9f, -2.02f), new Vector3(0.18f, 3.4f, 0.44f), creamStuccoMat);
            CreateBox("Pilaster_Bay3_4_Base", blockArch.transform,
                new Vector3(-11.90f, 0.68f, -2.02f), new Vector3(0.20f, 0.96f, 0.48f), darkZocaloMat);
            CreateCfeMeter("CFE_Meter_South_1", blockArch.transform, new Vector3(-11.82f, 1.40f, -1.91f), aluminumMat, cfeDomeGlassMat, cfeMeterFaceMat);
            CreateCfeMeter("CFE_Meter_South_2", blockArch.transform, new Vector3(-11.82f, 1.40f, -2.13f), aluminumMat, cfeDomeGlassMat, cfeMeterFaceMat);

            // Build the 2.75m diagonal South Chamfer, 2-story white lateral flank wall, 6-digit keypad door & wide semi-triangular secret room!
            SecretWedgeRoomBuilder.BuildIntoCurrentScene();

            // --- Bay 4 (Z = -1.80m to +3.05m, center Z = +0.625m): "Óptica Fashion" ---
            // Delegated to dedicated idempotent OpticaFashionInteriorBuilder (Increment 5.23.0:
            // full 7.18m-deep walkable showroom + rear consultation/calibration room + inward-swinging accessible door!)
            GameObject abiertoQuad = OpticaFashionInteriorBuilder.BuildOpticaFashionInteriorAndStorefront(
                blockArch.transform, opticaRoot.transform);

            // 3. Dynamic WHITE Roll-Up Metal Shutter for Óptica Fashion (uniformed in white; lowers automatically when World Core reports off_duty/closed!)
            CreateDynamicRollUpShutter("Optica_RollUpShutter_Dynamic", blockArch.transform,
                wallX: -11.88f, centerZ: 0.625f, widthZ: 4.86f, topY: 2.66f, fullDropHeight: 2.46f,
                alwaysOpen: false, linkedInteractionPointName: "InteractionPoint_OpticaFashion",
                openSignVisual: abiertoQuad, trackMat: whiteFrameMat, housingMat: whiteFrameMat, shutterMat: whiteMetalShutterMat);

            // Centered Beige Wedge Awning ("Optica_Awning"), Overhead White Fluorescent Lamp & Smooth Copper-Brown Sign ("Optica_Signboard")
            CreateWestWedgeAwning("Optica_Awning", opticaRoot.transform,
                centerZ: 0.625f, topY: 3.22f, widthZ: 4.86f, projectionX: 1.35f, dropY: 0.74f, beigeAwningMat, awningValanceMat);
            CreateBox("Optica_EntryFluorescentTube", opticaRoot.transform,
                new Vector3(-11.62f, 2.74f, 0.625f), new Vector3(0.05f, 0.05f, 1.85f), opticaTubeLightMat);
            CreateWestSign("Optica_Signboard", opticaRoot.transform,
                centerZ: 0.625f, centerY: 3.58f, widthZ: 4.74f, heightY: 0.68f, terracottaPlaqueMat, signOpticaMat);

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

            // Upper angled structural body (split cleanly at Y=3.12m so nothing protrudes into Óptica Fashion's 2.84m interior ceiling
            // while keeping Cambios Santa Fe's exterior facade 100% solid down to Y=2.74m!)
            CreateBox("BuildingA_DiagonalMass", diagRoot.transform,
                new Vector3(-2.40f, 5.46f, 0f), new Vector3(4.80f, 4.68f, 3.48f), creamStuccoMat, keepCollider: true);
            CreateBox("BuildingA_DiagonalFrontSkirt", diagRoot.transform,
                new Vector3(-0.21f, 2.93f, 0f), new Vector3(0.42f, 0.38f, 3.48f), creamStuccoMat, keepCollider: true);
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

            Check("Óptica Fashion segmented accessible glass storefront, interior, LED ABIERTO & Bienvenidos/HORARIO exist (monolithic glass & posters removed)",
                GameObject.Find("Optica_GlassPane_Full") == null &&
                GameObject.Find("Optica_Glass_SouthSidelight") != null &&
                GameObject.Find("Optica_DoorLeaf_North_Pivot") != null &&
                GameObject.Find("Optica_InteriorCounter") != null &&
                GameObject.Find("Optica_AbiertoSign") != null &&
                GameObject.Find("Optica_HorarioSign") != null &&
                GameObject.Find("Optica_DoorPushBar_Left") != null &&
                GameObject.Find("Optica_DoorPushBar_Right") != null &&
                GameObject.Find("Optica_CalibrationStation") != null &&
                GameObject.Find("Optica_PosterLentes") == null &&
                GameObject.Find("Optica_PinkNotice") == null &&
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

            Check("Building A South continuous flush wall & double-leaf storage door exist (alcove & blue barrel removed)",
                GameObject.Find("BuildingA_SouthBay_ContinuousWall") != null &&
                GameObject.Find("BuildingA_SouthBay_StorageDoor") != null &&
                GameObject.Find("ArchWindow_1_SouthDiag") != null &&
                GameObject.Find("BuildingA_SouthNook_BlueDrum") == null &&
                GameObject.Find("BuildingA_SouthNook_BackWall") == null &&
                GameObject.Find("Sign_DentalMexico") != null &&
                GameObject.Find("Awning_Bay12_DentalMexico") != null);

            var surCambioLed = GameObject.Find("CasaCambio_Sur_ExchangeLedSign")?.GetComponent<ExchangeRateLedSignController>();
            bool surThreeShopsOk =
                GameObject.Find("Sign_FarmaciaJuarez") != null &&
                GameObject.Find("Awning_FarmaciaJuarez") != null &&
                GameObject.Find("Pilaster_PharmacyVertical_Sign") != null &&
                GameObject.Find("CasaCambio_Sur_RolledShutterDrum") != null &&
                GameObject.Find("CasaCambio_Sur_TellerWindow") != null &&
                surCambioLed != null &&
                surCambioLed.layoutMode == ExchangeLedLayoutMode.AlternatingBuyGreenSellMagenta &&
                GameObject.Find("Sign_DentalMexico")?.transform.Find("Sign_DentalMexico_3DReturns") != null &&
                GameObject.Find("DentalMexico_DoorPanel") != null &&
                GameObject.Find("DentalMexico_VerticalDivider") != null &&
                GameObject.Find("DentalMexico_WindowPanel") != null &&
                GameObject.Find("CFE_Meter_DentalMexico") != null;
            Check("Farmacia Juárez, unnamed Casa de Cambio (live green/magenta LED), & 3D Dental México storefront built cleanly", surThreeShopsOk);

            var fjShutter = GameObject.Find("FarmaciaJuarez_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            var ccShutter = GameObject.Find("CasaCambio_Sur_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            var dmShutter = GameObject.Find("DentalMexico_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            bool threeWhiteShuttersOk = false;
            if (fjShutter != null && ccShutter != null && dmShutter != null &&
                !fjShutter.alwaysOpen && ccShutter.alwaysOpen && !dmShutter.alwaysOpen &&
                fjShutter.linkedBusinessId == "pharmacy:farmacia-juarez" &&
                ccShutter.linkedBusinessId == "exchange:casa-cambio-juarez-sur" &&
                dmShutter.linkedBusinessId == "dental:dental-mexico")
            {
                fjShutter.ApplyAuthoritativeBusinessStatus(new BusinessOperationalStatusDto { isOpen = false, status = "CLOSED" });
                bool fjCloses = fjShutter.IsClosed;
                fjShutter.ApplyAuthoritativeBusinessStatus(new BusinessOperationalStatusDto { isOpen = true, status = "OPEN" });
                bool fjOpens = !fjShutter.IsClosed;

                dmShutter.ApplyAuthoritativeBusinessStatus(new BusinessOperationalStatusDto { isOpen = false, status = "CLOSED" });
                bool dmCloses = dmShutter.IsClosed;
                dmShutter.ApplyAuthoritativeBusinessStatus(new BusinessOperationalStatusDto { isOpen = true, status = "OPEN" });
                bool dmOpens = !dmShutter.IsClosed;

                ccShutter.SetClosedImmediate(true); // alwaysOpen=true must keep 24/7 Casa de Cambio open!
                bool ccStaysOpen = !ccShutter.IsClosed;

                threeWhiteShuttersOk = fjCloses && fjOpens && dmCloses && dmOpens && ccStaysOpen;
            }
            Check("White roll-up metal shutters wired to backend schedules (Farmacia Juárez & Dental México auto-close; Casa de Cambio 24/7 open)", threeWhiteShuttersOk);

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
            var sampleFx = new WorldExchangeRateObservationDto
            {
                status = "available",
                freshness = "live",
                baseCurrency = "USD",
                quoteCurrency = "MXN",
                buyRate = 17.50f,
                sellRate = 18.50f,
                hasBuyRate = true,
                hasSellRate = true
            };
            var ledCtrl = GameObject.Find("CambiosSantaFe_ExchangeLedSign")?.GetComponent<ExchangeRateLedSignController>();
            if (ledCtrl != null)
            {
                ledCtrl.ApplyAuthoritativeExchangeRate(sampleFx);
                ledCtrl.SetNightPreviewOverride(false);
                ledCtrl.ForceSlide(false); // COMPRA (green)
            }
            var billemexLedCtrl = GameObject.Find("GrupoBillemex_ExchangeLedSign")?.GetComponent<ExchangeRateLedSignController>();
            if (billemexLedCtrl != null)
            {
                billemexLedCtrl.ApplyAuthoritativeExchangeRate(sampleFx);
                billemexLedCtrl.SetNightPreviewOverride(false);
            }
            var surCambioLedCtrl = GameObject.Find("CasaCambio_Sur_ExchangeLedSign")?.GetComponent<ExchangeRateLedSignController>();
            if (surCambioLedCtrl != null)
            {
                surCambioLedCtrl.ApplyAuthoritativeExchangeRate(sampleFx);
                surCambioLedCtrl.SetNightPreviewOverride(false);
                surCambioLedCtrl.ForceSlide(true); // VENTA 18.50 in bright magenta matching real street photo!
            }

            var optShutter = GameObject.Find("Optica_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            if (optShutter != null) optShutter.SetClosedImmediate(false);
            var fjShutter = GameObject.Find("FarmaciaJuarez_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            var ccShutter = GameObject.Find("CasaCambio_Sur_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            var dmShutter = GameObject.Find("DentalMexico_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            if (fjShutter != null) fjShutter.SetClosedImmediate(false);
            if (ccShutter != null) ccShutter.SetClosedImmediate(false);
            if (dmShutter != null) dmShutter.SetClosedImmediate(false);

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
                // Shot 1: Google Maps Frontal Match of the 4 Shops (Óptica MGR, Farmalivio, Cambios Bonanza, Grupo Billemex!)
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-0.6f, 1.85f, -21.8f);
                cam.transform.LookAt(new Vector3(-12.0f, 2.35f, -21.6f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "sw_four_shops_google_match.png")); // URP warmup pass
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "sw_four_shops_google_match.png"));

                // Shot 2: Oblique Close-Up of Farmalivio (double doors + 3-line quote), Bonanza white shutter, twin CFE & Billemex dual LED!
                cam.fieldOfView = 46f;
                cam.transform.position = new Vector3(-7.6f, 2.05f, -24.5f);
                cam.transform.LookAt(new Vector3(-11.6f, 2.65f, -19.2f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "sw_four_shops_detail_closeup.png"));

                // Shot 3: Wide Street-View Match of Full South-West Block
                cam.fieldOfView = 56f;
                cam.transform.position = new Vector3(4.6f, 1.95f, -16.8f);
                cam.transform.LookAt(new Vector3(-12.0f, 3.05f, -16.5f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "west_south_block_match.png"));

                // Shot 2: Close-Up on Building A South Continuous Wall & Storage Door + México Mágico / La Esquina de Chihuas
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-2.0f, 2.05f, -13.0f);
                cam.transform.LookAt(new Vector3(-12.2f, 2.85f, -13.2f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "west_south_diagonal_closeup.png"));

                // Shot 2B: Sidewalk Close-Up of Flush South Bay Wall + Double-Leaf Metal Storage Door (verifying alcove & blue barrel removed!)
                cam.fieldOfView = 46f;
                cam.transform.position = new Vector3(-6.8f, 1.75f, -12.3f);
                cam.transform.LookAt(new Vector3(-12.0f, 1.70f, -13.1f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "west_south_storage_door_fix.png"));

                // Shot 2C: Frontal Match of the 3 Updated Shops (Farmacia Juárez, unnamed Casa de Cambio + Magenta LED, & 3D Dental México!)
                cam.fieldOfView = 48f;
                cam.transform.position = new Vector3(-2.6f, 1.85f, -9.25f);
                cam.transform.LookAt(new Vector3(-12.0f, 2.30f, -9.25f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "farmacia_cambio_dental_day.png"));

                // Shot 2D: Oblique Street Photo 1 Match (looking South-West from Dental México past 3D blue channel letters & magenta VENTA LED!)
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-8.4f, 1.85f, -5.2f);
                cam.transform.LookAt(new Vector3(-11.4f, 2.45f, -9.6f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "farmacia_cambio_dental_oblique.png"));

                // Shot 3: Corner Transition at Calle Azucenas (Óptica MGR, Farmalivio 3D logo, Cambios Bonanza, & South Azucenas Wall)
                cam.fieldOfView = 52f;
                cam.transform.position = new Vector3(-1.5f, 1.95f, -29.8f);
                cam.transform.LookAt(new Vector3(-12.5f, 2.40f, -22.0f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "west_south_azucenas_corner.png"));

                // Shot 3B: Oblique 3D Close-Up of Farmalivio Extruded Channel Letters flanked by Óptica MGR. & Cambios Bonanza!
                var allGlowCtrls = UnityEngine.Object.FindObjectsByType<IlluminatedSignGlowController>(FindObjectsSortMode.None);
                foreach (var gc in allGlowCtrls) gc.SetNightPreviewOverride(false);

                // Shot 3C: Daytime South-Looking-North Angle (Photo 4 match: Grupo Billemex LED -> LA ESQ DE CH marquee -> MÉXICO MÁGICO circle!)
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-8.4f, 2.05f, -21.2f);
                cam.transform.LookAt(new Vector3(-11.2f, 3.85f, -14.8f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "chihuas_mexico_day_south.png"));

                // Shot 3D: Daytime North-Looking-South Angle (Photos 1 & 5 match: Rosa Mexicano protruding wall + Circle Sign + LA ESQ DE CH marquee!)
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-8.8f, 2.05f, -9.2f);
                cam.transform.LookAt(new Vector3(-11.2f, 3.65f, -14.6f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "chihuas_mexico_day_north.png"));

                // Shot 4: Daytime Frontal Close-Up of Óptica Fashion (Photo 2 match: centered sans-serif sign, wide double doors, LED ABIERTO, Bienvenidos/HORARIO & Empuje!)
                cam.fieldOfView = 46f;
                cam.transform.position = new Vector3(-5.0f, 1.85f, 0.625f);
                cam.transform.LookAt(new Vector3(-12.0f, 2.00f, 0.625f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "optica_fashion_frontal_match.png"));

                // Shot 4B: Daytime Frontal Overview of Óptica Fashion & Cambios Santa Fe
                cam.fieldOfView = 48f;
                cam.transform.position = new Vector3(-4.5f, 1.85f, 2.2f);
                cam.transform.LookAt(new Vector3(-12.2f, 2.15f, 2.6f));
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

                // Shot 5: Nighttime Illuminated Farmalivio + Grupo Billemex + La Esquina de Chihuas + México Mágico + Dental México 3D!
                foreach (var gc in allGlowCtrls) gc.SetNightPreviewOverride(true);
                if (billemexLedCtrl != null) billemexLedCtrl.SetNightPreviewOverride(true);
                if (surCambioLedCtrl != null)
                {
                    surCambioLedCtrl.SetNightPreviewOverride(true);
                    surCambioLedCtrl.ForceSlide(true); // VENTA bright magenta glow at night!
                }

                // Shot 5A: Nighttime Frontal Close-Up of La Esquina de Chihuas (Photo 3) + México Mágico (Photo 2)!
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-6.2f, 2.15f, -14.9f);
                cam.transform.LookAt(new Vector3(-11.8f, 2.60f, -14.9f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "chihuas_mexico_frontal_night.png"));

                // Shot 5B: Nighttime Photo 1 Match (Looking South: Glowing Circular MÉXICO MÁGICO + Rosa Wall + Neon Banner + LA ESQ DE CH Marquee!)
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-8.4f, 2.00f, -9.4f);
                cam.transform.LookAt(new Vector3(-11.1f, 3.45f, -14.8f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "chihuas_mexico_north_night.png"));

                // Shot 5C: Nighttime Full SW Strip (Farmalivio turquoise -> Billemex red -> Chihuas amber -> México Mágico pink!)
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-6.8f, 2.10f, -25.8f);
                cam.transform.LookAt(new Vector3(-11.4f, 3.15f, -18.2f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "sw_four_shops_night_glow.png"));

                // Shot 5D: Nighttime Illuminated Farmacia Juárez + Magenta VENTA LED + Electric-Blue 3D DENTAL MEXICO Channel Letters (with after-hours white shutters CLOSED on Farmacia & Dental, OPEN 24/7 on Casa de Cambio!)
                if (fjShutter != null) fjShutter.SetClosedImmediate(true);
                if (dmShutter != null) dmShutter.SetClosedImmediate(true);
                if (ccShutter != null) ccShutter.SetClosedImmediate(true); // alwaysOpen=true stays open!
                cam.fieldOfView = 50f;
                cam.transform.position = new Vector3(-7.4f, 1.95f, -5.0f);
                cam.transform.LookAt(new Vector3(-11.4f, 2.75f, -9.5f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "farmacia_cambio_dental_night.png"));

                // Shot 5E: Frontal After-Hours Verification (Farmacia Juárez & Dental México white shutters CLOSED; Casa de Cambio 24/7 OPEN!)
                cam.fieldOfView = 48f;
                cam.transform.position = new Vector3(-2.6f, 1.85f, -9.25f);
                cam.transform.LookAt(new Vector3(-12.0f, 2.25f, -9.25f));
                RenderToFile(cam, 1600, 900, Path.Combine(outDir, "farmacia_cambio_dental_shutters_closed.png"));

                if (fjShutter != null) fjShutter.SetClosedImmediate(false);
                if (dmShutter != null) dmShutter.SetClosedImmediate(false);
                if (ccShutter != null) ccShutter.SetClosedImmediate(false);

                foreach (var gc in allGlowCtrls) gc.SetNightPreviewOverride(null);
                if (billemexLedCtrl != null) billemexLedCtrl.SetNightPreviewOverride(null);
                if (surCambioLedCtrl != null) surCambioLedCtrl.SetNightPreviewOverride(null);
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
            Material trackMat, Material housingMat, Material shutterMat, string linkedBusinessId = "")
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
            ctrl.linkedBusinessId = linkedBusinessId ?? "";
            ctrl.linkedInteractionPointName = linkedInteractionPointName ?? "";
            ctrl.shutterCurtain = curtain.transform;
            ctrl.openSignVisual = openSignVisual;
            ctrl.fullDropHeight = fullDropHeight;
            ctrl.rolledMinHeight = 0.08f;
            ctrl.topAnchorLocalY = topY;
            ctrl.SetClosedImmediate(false);
        }

        private static void CreateExchangeRateLedSign(string name, Transform parent, Vector3 pos, Material cabinetMat, Material armMat, Material flangeMat, Material liveLedBaseMat, ExchangeLedLayoutMode layoutMode = ExchangeLedLayoutMode.AlternatingBuySell)
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

            // Dual real-time URP PointLights casting vivid green (COMPRA) / yellow or magenta (VENTA) glow at night!
            Light southLight = CreateLedGlowLight("LED_GlowLight_South", root.transform, new Vector3(0f, -0.06f, -0.42f));
            Light northLight = CreateLedGlowLight("LED_GlowLight_North", root.transform, new Vector3(0f, -0.06f, 0.42f));

            var ctrl = root.AddComponent<ExchangeRateLedSignController>();
            ctrl.layoutMode = layoutMode;
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
            light.renderMode = LightRenderMode.Auto;
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

        private static void CreateIlluminatedLightboxSign(string name, Transform parent, float centerZ, float centerY,
            float widthZ, float heightY, Material cabinetMat, Material faceTexMat, Color nightGlowColor, float frontOffsetX = -11.90f)
        {
            var signRoot = new GameObject(name);
            signRoot.transform.SetParent(parent, false);
            signRoot.transform.localPosition = new Vector3(frontOffsetX, centerY, centerZ);

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Lightbox_Cabinet";
            box.transform.SetParent(signRoot.transform, false);
            box.transform.localPosition = Vector3.zero;
            box.transform.localScale = new Vector3(0.14f, heightY, widthZ);
            SetMaterial(box, cabinetMat);
            UnityEngine.Object.DestroyImmediate(box.GetComponent<Collider>());

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Lightbox_Face";
            quad.transform.SetParent(signRoot.transform, false);
            quad.transform.localPosition = new Vector3(0.075f, 0f, 0f);
            quad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            quad.transform.localScale = new Vector3(widthZ - 0.04f, heightY - 0.04f, 1.0f);
            SetMaterial(quad, faceTexMat);
            UnityEngine.Object.DestroyImmediate(quad.GetComponent<Collider>());

            var lightGo = new GameObject($"{name}_NightLight");
            lightGo.transform.SetParent(signRoot.transform, false);
            lightGo.transform.localPosition = new Vector3(0.45f, -0.05f, 0f);
            var ptLight = lightGo.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.color = nightGlowColor;
            ptLight.intensity = 0.0f;
            ptLight.range = 3.6f;
            ptLight.enabled = false;
            ptLight.shadows = LightShadows.None;
            ptLight.renderMode = LightRenderMode.Auto;

            var glowCtrl = signRoot.AddComponent<IlluminatedSignGlowController>();
            glowCtrl.baseEmissiveMaterial = faceTexMat;
            glowCtrl.emissiveRenderers = new Renderer[] { quad.GetComponent<Renderer>() };
            glowCtrl.nightGlowLights = new Light[] { ptLight };
            glowCtrl.emissionTint = Color.white;
            glowCtrl.dayEmissionIntensity = 0.25f;
            glowCtrl.nightEmissionIntensity = 2.6f;
            glowCtrl.nightLightColor = nightGlowColor;
            glowCtrl.nightLightIntensity = 1.15f;
            glowCtrl.nightLightRange = 3.6f;
        }

        private static void CreateExtrudedChannelLetterSign(
            string name, Transform parent, float wallX, float centerY, float centerZ,
            float widthZ, float heightY, float extrudeDepthX,
            string texAssetPath, string faceMatPath, string sideMatPath, string meshAssetPath,
            Color sideReturnColor, Color nightGlowColor)
        {
            Material faceMat = CreateOrGetAlphaEmissiveMaterial(faceMatPath, texAssetPath);
            Material sideMat = CreateOrGetColorMaterial(sideMatPath, sideReturnColor, 0.34f);

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(wallX, centerY, centerZ);

            // Lightweight solid 3D extruded channel-letter side returns (ShadowCastingMode.Off for GPU thermal safety!)
            string fullPngPath = Path.Combine(Directory.GetParent(Application.dataPath).FullName, texAssetPath.Replace('/', Path.DirectorySeparatorChar));
            Mesh returnsMesh = BuildExtrudedSilhouetteMesh(fullPngPath, meshAssetPath, widthZ, heightY, extrudeDepthX, gridW: 160, gridH: 38);
            if (returnsMesh != null)
            {
                var returnsGo = new GameObject($"{name}_3DReturns");
                returnsGo.transform.SetParent(root.transform, false);
                var mf = returnsGo.AddComponent<MeshFilter>();
                var mr = returnsGo.AddComponent<MeshRenderer>();
                mf.sharedMesh = returnsMesh;
                mr.sharedMaterial = sideMat;
                mr.shadowCastingMode = ShadowCastingMode.Off;
            }

            // Razor-sharp alpha-clipped HDR emissive electric-blue acrylic front face
            var frontQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            frontQuad.name = $"{name}_FrontFace";
            frontQuad.transform.SetParent(root.transform, false);
            frontQuad.transform.localPosition = new Vector3(extrudeDepthX + 0.002f, 0f, 0f);
            frontQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            frontQuad.transform.localScale = new Vector3(widthZ, heightY, 1.0f);
            SetMaterial(frontQuad, faceMat);
            UnityEngine.Object.DestroyImmediate(frontQuad.GetComponent<Collider>());

            // Nighttime electric-blue glow light washing the facade & beige awning cleanly at night
            var lightGo = new GameObject($"{name}_NightGlowLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(extrudeDepthX + 0.45f, -0.05f, 0f);
            var ptLight = lightGo.AddComponent<Light>();
            ptLight.type = LightType.Point;
            ptLight.color = nightGlowColor;
            ptLight.intensity = 0.0f;
            ptLight.range = 4.6f;
            ptLight.enabled = false;
            ptLight.shadows = LightShadows.None;
            ptLight.renderMode = LightRenderMode.Auto;

            var glowCtrl = root.AddComponent<IlluminatedSignGlowController>();
            glowCtrl.baseEmissiveMaterial = faceMat;
            glowCtrl.emissiveRenderers = new Renderer[] { frontQuad.GetComponent<Renderer>() };
            glowCtrl.nightGlowLights = new Light[] { ptLight };
            glowCtrl.emissionTint = Color.white;
            glowCtrl.dayEmissionIntensity = 0.40f;
            glowCtrl.nightEmissionIntensity = 3.8f;
            glowCtrl.nightLightColor = nightGlowColor;
            glowCtrl.nightLightIntensity = 1.65f;
            glowCtrl.nightLightRange = 4.6f;
        }

        private static Mesh BuildExtrudedSilhouetteMesh(string fullPngPath, string meshAssetPath,
            float widthZ, float heightY, float depthX, int gridW, int gridH)
        {
            if (!File.Exists(fullPngPath))
            {
                Debug.LogError($"[WestSidewalkBlockBuilder] Missing silhouette PNG: {fullPngPath}");
                return null;
            }

            byte[] pngBytes = File.ReadAllBytes(fullPngPath);
            var tmpTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!tmpTex.LoadImage(pngBytes))
            {
                UnityEngine.Object.DestroyImmediate(tmpTex);
                return null;
            }

            int srcW = tmpTex.width;
            int srcH = tmpTex.height;
            Color32[] srcPixels = tmpTex.GetPixels32();
            UnityEngine.Object.DestroyImmediate(tmpTex);

            bool[,] solid = new bool[gridW, gridH];
            for (int y = 0; y < gridH; y++)
            {
                int sy = Mathf.Clamp(Mathf.FloorToInt(((y + 0.5f) / gridH) * srcH), 0, srcH - 1);
                for (int x = 0; x < gridW; x++)
                {
                    int sx = Mathf.Clamp(Mathf.FloorToInt(((x + 0.5f) / gridW) * srcW), 0, srcW - 1);
                    solid[x, y] = srcPixels[sy * srcW + sx].a > 110;
                }
            }

            var verts = new List<Vector3>(16384);
            var norms = new List<Vector3>(16384);
            var uvs = new List<Vector2>(16384);
            var tris = new List<int>(24576);

            void AddOrientedSideQuad(Vector3 p00, Vector3 p10, Vector3 p01, Vector3 p11, Vector3 expectedNormal)
            {
                int b = verts.Count;
                verts.Add(p00); verts.Add(p10); verts.Add(p11); verts.Add(p01);
                norms.Add(expectedNormal); norms.Add(expectedNormal); norms.Add(expectedNormal); norms.Add(expectedNormal);
                uvs.Add(Vector2.zero); uvs.Add(Vector2.right); uvs.Add(Vector2.one); uvs.Add(Vector2.up);

                Vector3 cross = Vector3.Cross(p10 - p00, p11 - p00);
                if (Vector3.Dot(cross, expectedNormal) >= 0f)
                {
                    tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
                }
                else
                {
                    tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                    tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
                }
            }

            float cellZ = widthZ / gridW;
            float cellY = heightY / gridH;
            float halfW = widthZ * 0.5f;
            float halfH = heightY * 0.5f;

            for (int y = 0; y < gridH; y++)
            {
                float y0 = y * cellY - halfH;
                float y1 = (y + 1) * cellY - halfH;
                for (int x = 0; x < gridW; x++)
                {
                    if (!solid[x, y]) continue;
                    float z0 = x * cellZ - halfW;
                    float z1 = (x + 1) * cellZ - halfW;

                    if (x == 0 || !solid[x - 1, y])
                    {
                        AddOrientedSideQuad(
                            new Vector3(0f, y0, z0), new Vector3(depthX, y0, z0),
                            new Vector3(0f, y1, z0), new Vector3(depthX, y1, z0),
                            Vector3.back);
                    }
                    if (x == gridW - 1 || !solid[x + 1, y])
                    {
                        AddOrientedSideQuad(
                            new Vector3(0f, y0, z1), new Vector3(depthX, y0, z1),
                            new Vector3(0f, y1, z1), new Vector3(depthX, y1, z1),
                            Vector3.forward);
                    }
                    if (y == 0 || !solid[x, y - 1])
                    {
                        AddOrientedSideQuad(
                            new Vector3(0f, y0, z0), new Vector3(depthX, y0, z0),
                            new Vector3(0f, y0, z1), new Vector3(depthX, y0, z1),
                            Vector3.down);
                    }
                    if (y == gridH - 1 || !solid[x, y + 1])
                    {
                        AddOrientedSideQuad(
                            new Vector3(0f, y1, z0), new Vector3(depthX, y1, z0),
                            new Vector3(0f, y1, z1), new Vector3(depthX, y1, z1),
                            Vector3.up);
                    }
                }
            }

            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshAssetPath);
            Mesh mesh = existing != null ? existing : new Mesh();
            mesh.Clear();
            mesh.name = Path.GetFileNameWithoutExtension(meshAssetPath);
            if (verts.Count > 65000) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();

            if (existing == null) AssetDatabase.CreateAsset(mesh, meshAssetPath);
            else EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssets();
            return mesh;
        }

        private static Material CreateOrGetEmissiveSignTextureMaterial(string matPath, string texPath, float dayEmission = 0.25f)
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
                mat.SetTexture("_EmissionMap", tex);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetColor("_EmissionColor", new Color(dayEmission, dayEmission, dayEmission, 1.0f));
            mat.SetFloat("_Smoothness", 0.35f);
            mat.SetFloat("_AlphaClip", 0.0f);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.renderQueue = (int)RenderQueue.Geometry;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetAlphaEmissiveMaterial(string matPath, string texPath)
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
                mat.SetTexture("_EmissionMap", tex);
            }
            mat.SetColor("_BaseColor", Color.white);
            mat.SetColor("_Color", Color.white);
            mat.SetColor("_EmissionColor", new Color(0.40f, 0.40f, 0.40f, 1.0f));
            mat.SetFloat("_Smoothness", 0.56f);
            mat.SetFloat("_Metallic", 0.05f);
            mat.SetFloat("_AlphaClip", 1.0f);
            mat.SetFloat("_Cutoff", 0.42f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material CreateOrGetTransparentTextureMaterial(string matPath, string texPath)
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
            mat.SetFloat("_Surface", 1.0f);
            mat.SetFloat("_Blend", 0.0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0.0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            mat.SetFloat("_Smoothness", 0.25f);
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
