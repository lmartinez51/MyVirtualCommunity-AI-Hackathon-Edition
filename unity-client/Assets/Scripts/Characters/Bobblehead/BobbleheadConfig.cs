using System;
using UnityEngine;

namespace MyVirtualCommunity.Character
{
    public enum BobbleheadHairType
    {
        StreetBeanie, // Legacy Schema v1/v2 combined option; migrated to CasualShort + BaseballCap in Schema v3
        AttendantBun,
        CasualShort,
        ShortTexturedCrop,
        AfroCurly,
        SidePartedWavy,
        HighPonytail,
        Bald
    }

    public enum BobbleheadHeadwearType
    {
        None = 0,
        BaseballCap = 1,
        WinterBeanie = 2
    }

    public enum BobbleheadFaceShape
    {
        Round,
        Oval,
        Angular
    }

    public enum BobbleheadFacialHair
    {
        None,
        SubtleStubble,
        Mustache,
        Goatee,
        FullBeard
    }

    public enum BobbleheadBodyPresentation
    {
        Original,
        Masculine,
        Feminine
    }

    public enum BobbleheadOutfitType
    {
        StreetwearJacket,
        WingSpotAttendant,
        BorderConcierge
    }

    /// <summary>
    /// Data-driven configuration container for stylized Bobblehead characters.
    /// Provides modular presets for the Player avatar and Elena Morales.
    /// </summary>
    [Serializable]
    public class BobbleheadConfig
    {
        public const string ArchetypeIdentifier = "MVC_BobbleheadBase";

        public string archetypeId = ArchetypeIdentifier;
        public string characterName = "Character";
        public float totalHeight = 1.75f;
        public float headScale = 1.0f;

        // Appearance Variations (Phase C)
        public BobbleheadFaceShape faceShape = BobbleheadFaceShape.Round;
        public BobbleheadFacialHair facialHair = BobbleheadFacialHair.None;
        public BobbleheadBodyPresentation bodyPresentation = BobbleheadBodyPresentation.Original;

        // Skin & Face Colors
        public Color skinColor = new Color(0.92f, 0.77f, 0.67f);
        public Color eyeColor = new Color(0.28f, 0.18f, 0.12f);
        public Color pupilColor = new Color(0.06f, 0.06f, 0.08f);
        public Color eyebrowColor = new Color(0.12f, 0.10f, 0.08f);
        public Color lipColor = new Color(0.78f, 0.45f, 0.42f);
        public Color blushColor = new Color(0.92f, 0.55f, 0.50f, 0.35f);

        // Hair & Headwear (Separated in Schema v3: Hairstyle != Headwear)
        public BobbleheadHairType hairStyle = BobbleheadHairType.CasualShort;
        public BobbleheadHeadwearType headwear = BobbleheadHeadwearType.BaseballCap;
        public Color hairColor = new Color(0.12f, 0.10f, 0.08f);
        public Color headwearColor = new Color(0.18f, 0.22f, 0.26f);
        public Color headwearAccentColor = new Color(0.85f, 0.20f, 0.12f);

        // Clothing
        public BobbleheadOutfitType outfitType = BobbleheadOutfitType.StreetwearJacket;
        public Color outerwearColor = new Color(0.18f, 0.24f, 0.32f);
        public Color innerwearColor = new Color(0.75f, 0.75f, 0.78f);
        public Color pantsColor = new Color(0.22f, 0.25f, 0.28f);
        public Color shoeBaseColor = new Color(0.92f, 0.92f, 0.94f);
        public Color shoeAccentColor = new Color(0.85f, 0.20f, 0.12f);

        // Accessories & Badges
        public bool hasNameBadge = false;
        public string badgeText = "";
        public bool hasGlasses = false;
        public Color glassesColor = new Color(0.85f, 0.72f, 0.25f);
        public bool hasBackpack = false;
        public Color backpackColor = new Color(0.18f, 0.20f, 0.24f);
        public bool hasSatchel = false;
        public Color satchelColor = new Color(0.48f, 0.28f, 0.15f);

        public static BobbleheadConfig CreatePlayerConfig()
        {
            return new BobbleheadConfig
            {
                archetypeId = ArchetypeIdentifier,
                characterName = "Player",
                totalHeight = 1.75f,
                headScale = 1.0f,
                faceShape = BobbleheadFaceShape.Round,
                facialHair = BobbleheadFacialHair.None,
                bodyPresentation = BobbleheadBodyPresentation.Original,
                skinColor = new Color(0.93f, 0.79f, 0.70f),
                eyeColor = new Color(0.25f, 0.18f, 0.12f),
                pupilColor = new Color(0.05f, 0.05f, 0.06f),
                hairColor = new Color(0.14f, 0.11f, 0.09f),
                eyebrowColor = new Color(0.14f, 0.11f, 0.09f),
                lipColor = new Color(0.75f, 0.48f, 0.45f),
                hairStyle = BobbleheadHairType.CasualShort,
                headwear = BobbleheadHeadwearType.BaseballCap,
                headwearColor = new Color(0.20f, 0.24f, 0.28f),      // Charcoal/slate cap & beanie base
                headwearAccentColor = new Color(0.88f, 0.32f, 0.16f),// Subtle orange-red cap emblem / beanie label
                outfitType = BobbleheadOutfitType.StreetwearJacket,
                outerwearColor = new Color(0.16f, 0.24f, 0.32f),     // Navy casual streetwear overshirt
                innerwearColor = new Color(0.78f, 0.80f, 0.82f),     // Heather grey inner tee
                pantsColor = new Color(0.22f, 0.26f, 0.24f),         // Olive-slate streetwear cargos
                shoeBaseColor = new Color(0.95f, 0.95f, 0.96f),      // Retro white sneakers
                shoeAccentColor = new Color(0.85f, 0.20f, 0.15f),    // Red contrast side stripe
                hasNameBadge = false,
                badgeText = ""
            };
        }

        public static BobbleheadConfig CreateElenaConfig()
        {
            return new BobbleheadConfig
            {
                archetypeId = ArchetypeIdentifier,
                characterName = "Elena Morales",
                totalHeight = 1.68f,
                headScale = 0.98f,
                faceShape = BobbleheadFaceShape.Round,
                facialHair = BobbleheadFacialHair.None,
                bodyPresentation = BobbleheadBodyPresentation.Original,
                skinColor = new Color(0.91f, 0.76f, 0.66f),
                eyeColor = new Color(0.18f, 0.12f, 0.08f),          // Dark espresso eyes
                pupilColor = new Color(0.04f, 0.04f, 0.05f),
                hairColor = new Color(0.10f, 0.07f, 0.05f),         // Rich dark hair
                eyebrowColor = new Color(0.10f, 0.07f, 0.05f),
                lipColor = new Color(0.82f, 0.42f, 0.40f),          // Gentle friendly rose lip
                hairStyle = BobbleheadHairType.AttendantBun,
                headwear = BobbleheadHeadwearType.None,
                headwearColor = new Color(0.85f, 0.20f, 0.12f),     // Wing Spot signature red visor/band
                headwearAccentColor = Color.white,
                outfitType = BobbleheadOutfitType.WingSpotAttendant,
                outerwearColor = new Color(0.85f, 0.20f, 0.12f),     // Wing Spot red attendant polo/apron
                innerwearColor = Color.white,                         // Crisp white service collar
                pantsColor = new Color(0.14f, 0.14f, 0.16f),         // Professional charcoal-black trousers
                shoeBaseColor = new Color(0.12f, 0.12f, 0.14f),      // Practical non-slip black shoes
                shoeAccentColor = new Color(0.26f, 0.26f, 0.28f),    // Dark grey rubber sole
                hasNameBadge = true,
                badgeText = "ELENA"
            };
        }

        /// <summary>
        /// Creates the Elena Ramos Border Concierge preset for the Hackathon Edition proof of concept.
        /// Configured with a tailored feminine silhouette, oval face, dark brunette ponytail,
        /// official CBP navy blue blazer, crisp white blouse collar, charcoal slacks, and gold name badge.
        /// </summary>
        public static BobbleheadConfig CreateElenaRamosConfig()
        {
            return new BobbleheadConfig
            {
                archetypeId = ArchetypeIdentifier,
                characterName = "Elena Ramos",
                totalHeight = 1.70f,
                headScale = 0.96f,
                faceShape = BobbleheadFaceShape.Oval,
                facialHair = BobbleheadFacialHair.None,
                bodyPresentation = BobbleheadBodyPresentation.Feminine,
                skinColor = new Color(0.91f, 0.76f, 0.66f),
                eyeColor = new Color(0.18f, 0.12f, 0.08f),          // Dark espresso eyes
                pupilColor = new Color(0.04f, 0.04f, 0.05f),
                hairColor = new Color(0.10f, 0.07f, 0.05f),         // Rich dark brunette
                eyebrowColor = new Color(0.10f, 0.07f, 0.05f),
                lipColor = new Color(0.80f, 0.44f, 0.42f),          // Professional friendly rose
                hairStyle = BobbleheadHairType.HighPonytail,        // Clean professional ponytail
                headwear = BobbleheadHeadwearType.None,              // No cap/visor/beanie
                headwearColor = Color.clear,
                headwearAccentColor = new Color(0.92f, 0.78f, 0.20f), // Gold hair clip matching badge
                outfitType = BobbleheadOutfitType.BorderConcierge,
                outerwearColor = new Color(0.10f, 0.18f, 0.32f),    // Official CBP/Concierge Navy Blue
                innerwearColor = new Color(0.95f, 0.95f, 0.97f),    // Crisp white blouse / collar
                pantsColor = new Color(0.20f, 0.20f, 0.22f),        // Professional charcoal slacks
                shoeBaseColor = new Color(0.10f, 0.10f, 0.12f),     // Professional dark footwear
                shoeAccentColor = new Color(0.24f, 0.24f, 0.28f),
                hasNameBadge = true,
                badgeText = "ELENA"
            };
        }

        public static BobbleheadConfig CreateCarlosMendezConfig()
        {
            return new BobbleheadConfig
            {
                archetypeId = ArchetypeIdentifier,
                characterName = "Carlos Méndez",
                totalHeight = 1.76f,
                headScale = 0.97f,
                faceShape = BobbleheadFaceShape.Angular,
                facialHair = BobbleheadFacialHair.None,
                bodyPresentation = BobbleheadBodyPresentation.Masculine,
                skinColor = new Color(0.90f, 0.74f, 0.63f),
                eyeColor = new Color(0.20f, 0.14f, 0.10f),
                pupilColor = new Color(0.04f, 0.04f, 0.05f),
                hairColor = new Color(0.12f, 0.09f, 0.07f),
                eyebrowColor = new Color(0.12f, 0.09f, 0.07f),
                lipColor = new Color(0.75f, 0.44f, 0.42f),
                hairStyle = BobbleheadHairType.CasualShort,
                headwear = BobbleheadHeadwearType.None,
                outfitType = BobbleheadOutfitType.BorderConcierge,
                outerwearColor = new Color(0.94f, 0.95f, 0.97f),    // Clean white pharmacy lab coat
                innerwearColor = new Color(0.08f, 0.28f, 0.68f),    // Farmacias Benavides royal blue collar
                pantsColor = new Color(0.18f, 0.20f, 0.25f),        // Professional dark navy-slate trousers
                shoeBaseColor = new Color(0.12f, 0.12f, 0.14f),     // Practical black footwear
                shoeAccentColor = new Color(0.24f, 0.24f, 0.28f),
                hasNameBadge = true,
                badgeText = "CARLOS",
                hasGlasses = false,
                hasBackpack = false,
                hasSatchel = false
            };
        }

        public static BobbleheadConfig CreateValeriaDominguezConfig()
        {
            return new BobbleheadConfig
            {
                archetypeId = ArchetypeIdentifier,
                characterName = "Valeria Domínguez",
                totalHeight = 1.68f,
                headScale = 0.96f,
                faceShape = BobbleheadFaceShape.Oval,
                facialHair = BobbleheadFacialHair.None,
                bodyPresentation = BobbleheadBodyPresentation.Feminine,
                skinColor = new Color(0.92f, 0.77f, 0.67f),
                eyeColor = new Color(0.35f, 0.24f, 0.14f),          // Warm hazel eyes
                pupilColor = new Color(0.04f, 0.04f, 0.05f),
                hairColor = new Color(0.16f, 0.10f, 0.08f),         // Rich dark chestnut
                eyebrowColor = new Color(0.16f, 0.10f, 0.08f),
                lipColor = new Color(0.82f, 0.42f, 0.40f),
                blushColor = new Color(0.92f, 0.55f, 0.50f, 0.35f),
                hairStyle = BobbleheadHairType.SidePartedWavy,
                headwear = BobbleheadHeadwearType.None,
                outfitType = BobbleheadOutfitType.BorderConcierge,
                outerwearColor = new Color(0.14f, 0.14f, 0.16f),    // Chic charcoal-black boutique blazer
                innerwearColor = new Color(0.88f, 0.76f, 0.45f),    // Fashion boutique gold silk collar
                pantsColor = new Color(0.16f, 0.16f, 0.18f),        // Tailored black boutique trousers
                shoeBaseColor = new Color(0.10f, 0.10f, 0.12f),     // Black designer footwear
                shoeAccentColor = new Color(0.85f, 0.72f, 0.25f),   // Gold accent sole
                hasNameBadge = true,
                badgeText = "VALERIA",
                hasGlasses = true,                                  // Signature optical eyewear!
                glassesColor = new Color(0.85f, 0.72f, 0.25f),     // Chic designer gold frames
                hasBackpack = false,
                hasSatchel = false
            };
        }

        public static BobbleheadConfig CreateMateoNavarroConfig()
        {
            return new BobbleheadConfig
            {
                archetypeId = ArchetypeIdentifier,
                characterName = "Mateo Navarro",
                totalHeight = 1.74f,
                headScale = 0.98f,
                faceShape = BobbleheadFaceShape.Round,
                facialHair = BobbleheadFacialHair.None,
                bodyPresentation = BobbleheadBodyPresentation.Masculine,
                skinColor = new Color(0.89f, 0.72f, 0.60f),
                eyeColor = new Color(0.22f, 0.15f, 0.10f),
                pupilColor = new Color(0.04f, 0.04f, 0.05f),
                hairColor = new Color(0.14f, 0.10f, 0.08f),
                eyebrowColor = new Color(0.14f, 0.10f, 0.08f),
                lipColor = new Color(0.74f, 0.45f, 0.42f),
                hairStyle = BobbleheadHairType.ShortTexturedCrop,
                headwear = BobbleheadHeadwearType.None,
                outfitType = BobbleheadOutfitType.StreetwearJacket,
                outerwearColor = new Color(0.15f, 0.28f, 0.48f),    // Collegiate navy zip jacket
                innerwearColor = new Color(0.85f, 0.85f, 0.88f),    // Heather grey inner tee
                pantsColor = new Color(0.20f, 0.28f, 0.38f),        // Classic blue denim jeans
                shoeBaseColor = new Color(0.94f, 0.94f, 0.95f),     // Retro white sneakers
                shoeAccentColor = new Color(0.88f, 0.35f, 0.12f),   // UTEP orange accent stripe
                hasNameBadge = false,
                badgeText = "",
                hasGlasses = false,
                hasBackpack = true,                                 // Student commuter backpack!
                backpackColor = new Color(0.18f, 0.20f, 0.24f),
                hasSatchel = false
            };
        }

        public static BobbleheadConfig CreateSofiaAlarconConfig()
        {
            return new BobbleheadConfig
            {
                archetypeId = ArchetypeIdentifier,
                characterName = "Sofia Alarcón",
                totalHeight = 1.67f,
                headScale = 0.96f,
                faceShape = BobbleheadFaceShape.Oval,
                facialHair = BobbleheadFacialHair.None,
                bodyPresentation = BobbleheadBodyPresentation.Feminine,
                skinColor = new Color(0.91f, 0.75f, 0.64f),
                eyeColor = new Color(0.20f, 0.14f, 0.10f),
                pupilColor = new Color(0.04f, 0.04f, 0.05f),
                hairColor = new Color(0.12f, 0.08f, 0.06f),
                eyebrowColor = new Color(0.12f, 0.08f, 0.06f),
                lipColor = new Color(0.82f, 0.40f, 0.42f),          // Warm berry lip
                blushColor = new Color(0.92f, 0.55f, 0.50f, 0.35f),
                hairStyle = BobbleheadHairType.AfroCurly,           // Distinctive curly hair
                headwear = BobbleheadHeadwearType.None,
                outfitType = BobbleheadOutfitType.StreetwearJacket,
                outerwearColor = new Color(0.50f, 0.18f, 0.30f),    // Berry/plum retail worker jacket
                innerwearColor = new Color(0.92f, 0.90f, 0.86f),    // Soft cream scarf/collar
                pantsColor = new Color(0.20f, 0.20f, 0.22f),        // Dark slate trousers
                shoeBaseColor = new Color(0.14f, 0.14f, 0.16f),     // Dark commuter flats/sneakers
                shoeAccentColor = new Color(0.28f, 0.28f, 0.32f),
                hasNameBadge = false,
                badgeText = "",
                hasGlasses = false,
                hasBackpack = false,
                hasSatchel = false
            };
        }

        public static BobbleheadConfig CreateDonHectorMendezConfig()
        {
            return new BobbleheadConfig
            {
                archetypeId = ArchetypeIdentifier,
                characterName = "Don Héctor Méndez",
                totalHeight = 1.72f,
                headScale = 0.97f,
                faceShape = BobbleheadFaceShape.Angular,
                facialHair = BobbleheadFacialHair.Mustache,          // Distinguished mustache!
                bodyPresentation = BobbleheadBodyPresentation.Masculine,
                skinColor = new Color(0.86f, 0.68f, 0.55f),         // Weathered sun-warmed skin tone
                eyeColor = new Color(0.25f, 0.18f, 0.12f),          // Warm dark amber eyes
                pupilColor = new Color(0.04f, 0.04f, 0.05f),
                hairColor = new Color(0.72f, 0.73f, 0.75f),         // Distinguished silver-steel hair!
                eyebrowColor = new Color(0.55f, 0.56f, 0.58f),      // Salt-and-pepper eyebrows!
                lipColor = new Color(0.70f, 0.44f, 0.40f),
                hairStyle = BobbleheadHairType.SidePartedWavy,
                headwear = BobbleheadHeadwearType.None,
                outfitType = BobbleheadOutfitType.StreetwearJacket,
                outerwearColor = new Color(0.44f, 0.26f, 0.14f),    // Warm saddle-leather artisan vest
                innerwearColor = new Color(0.88f, 0.84f, 0.74f),    // Rustic cream craftsman shirt
                pantsColor = new Color(0.30f, 0.25f, 0.20f),        // Khaki-brown artisan trousers
                shoeBaseColor = new Color(0.25f, 0.18f, 0.12f),     // Brown leather work shoes
                shoeAccentColor = new Color(0.36f, 0.26f, 0.18f),
                hasNameBadge = false,
                badgeText = "",
                hasGlasses = false,
                hasBackpack = false,
                hasSatchel = true,                                  // Hand-tooled leather satchel!
                satchelColor = new Color(0.52f, 0.30f, 0.16f)
            };
        }

        public static BobbleheadConfig CreatePatriciaMoralesConfig()
        {
            return new BobbleheadConfig
            {
                archetypeId = ArchetypeIdentifier,
                characterName = "Lic. Patricia Morales",
                totalHeight = 1.68f,
                headScale = 0.98f,
                faceShape = BobbleheadFaceShape.Oval,
                facialHair = BobbleheadFacialHair.None,
                bodyPresentation = BobbleheadBodyPresentation.Feminine,
                skinColor = new Color(0.85f, 0.69f, 0.58f),
                eyeColor = new Color(0.24f, 0.16f, 0.11f),
                pupilColor = new Color(0.04f, 0.04f, 0.05f),
                hairColor = new Color(0.12f, 0.10f, 0.08f),
                eyebrowColor = new Color(0.15f, 0.12f, 0.10f),
                lipColor = new Color(0.72f, 0.42f, 0.45f),
                blushColor = new Color(0.88f, 0.55f, 0.50f, 0.35f),
                hairStyle = BobbleheadHairType.AttendantBun,
                headwear = BobbleheadHeadwearType.None,
                outfitType = BobbleheadOutfitType.BorderConcierge,
                outerwearColor = new Color(0.48f, 0.14f, 0.18f),
                innerwearColor = new Color(0.96f, 0.96f, 0.98f),
                pantsColor = new Color(0.18f, 0.20f, 0.24f),
                shoeBaseColor = new Color(0.12f, 0.12f, 0.14f),
                shoeAccentColor = new Color(0.22f, 0.22f, 0.25f),
                hasNameBadge = true,
                badgeText = "ATENCIÓN",
                hasGlasses = true,
                glassesColor = new Color(0.25f, 0.25f, 0.28f),
                hasBackpack = false,
                hasSatchel = true,
                satchelColor = new Color(0.35f, 0.22f, 0.16f)
            };
        }

        /// <summary>
        /// Creates a deep copy of this BobbleheadConfig instance.
        /// </summary>
        public BobbleheadConfig Clone()
        {
            return new BobbleheadConfig
            {
                archetypeId = this.archetypeId,
                characterName = this.characterName,
                totalHeight = this.totalHeight,
                headScale = this.headScale,
                faceShape = this.faceShape,
                facialHair = this.facialHair,
                bodyPresentation = this.bodyPresentation,
                skinColor = this.skinColor,
                eyeColor = this.eyeColor,
                pupilColor = this.pupilColor,
                eyebrowColor = this.eyebrowColor,
                lipColor = this.lipColor,
                blushColor = this.blushColor,
                hairStyle = this.hairStyle,
                headwear = this.headwear,
                hairColor = this.hairColor,
                headwearColor = this.headwearColor,
                headwearAccentColor = this.headwearAccentColor,
                outfitType = this.outfitType,
                outerwearColor = this.outerwearColor,
                innerwearColor = this.innerwearColor,
                pantsColor = this.pantsColor,
                shoeBaseColor = this.shoeBaseColor,
                shoeAccentColor = this.shoeAccentColor,
                hasNameBadge = this.hasNameBadge,
                badgeText = this.badgeText,
                hasGlasses = this.hasGlasses,
                glassesColor = this.glassesColor,
                hasBackpack = this.hasBackpack,
                backpackColor = this.backpackColor,
                hasSatchel = this.hasSatchel,
                satchelColor = this.satchelColor
            };
        }

        /// <summary>
        /// Copies ONLY color fields from source into this instance.
        /// Leaves characterName, totalHeight, headScale, hairStyle, outfitType, hasNameBadge, and badgeText unchanged.
        /// </summary>
        public void CopyColorsFrom(BobbleheadConfig source)
        {
            if (source == null) return;
            this.skinColor = source.skinColor;
            this.eyeColor = source.eyeColor;
            this.pupilColor = source.pupilColor;
            this.eyebrowColor = source.eyebrowColor;
            this.lipColor = source.lipColor;
            this.blushColor = source.blushColor;
            this.hairColor = source.hairColor;
            this.headwearColor = source.headwearColor;
            this.headwearAccentColor = source.headwearAccentColor;
            this.outerwearColor = source.outerwearColor;
            this.innerwearColor = source.innerwearColor;
            this.pantsColor = source.pantsColor;
            this.shoeBaseColor = source.shoeBaseColor;
            this.shoeAccentColor = source.shoeAccentColor;
            this.glassesColor = source.glassesColor;
            this.backpackColor = source.backpackColor;
            this.satchelColor = source.satchelColor;
        }

        /// <summary>
        /// Compares all color fields with another configuration within a numerical tolerance.
        /// </summary>
        public bool AreColorsEqual(BobbleheadConfig other, float tolerance = 0.002f)
        {
            if (other == null) return false;
            return ColorsMatch(this.skinColor, other.skinColor, tolerance) &&
                   ColorsMatch(this.eyeColor, other.eyeColor, tolerance) &&
                   ColorsMatch(this.pupilColor, other.pupilColor, tolerance) &&
                   ColorsMatch(this.eyebrowColor, other.eyebrowColor, tolerance) &&
                   ColorsMatch(this.lipColor, other.lipColor, tolerance) &&
                   ColorsMatch(this.blushColor, other.blushColor, tolerance) &&
                   ColorsMatch(this.hairColor, other.hairColor, tolerance) &&
                   ColorsMatch(this.headwearColor, other.headwearColor, tolerance) &&
                   ColorsMatch(this.headwearAccentColor, other.headwearAccentColor, tolerance) &&
                   ColorsMatch(this.outerwearColor, other.outerwearColor, tolerance) &&
                   ColorsMatch(this.innerwearColor, other.innerwearColor, tolerance) &&
                   ColorsMatch(this.pantsColor, other.pantsColor, tolerance) &&
                   ColorsMatch(this.shoeBaseColor, other.shoeBaseColor, tolerance) &&
                   ColorsMatch(this.shoeAccentColor, other.shoeAccentColor, tolerance);
        }

        /// <summary>
        /// Copies appearance options (hairStyle, headwear, faceShape, facialHair, bodyPresentation) AND colors from source.
        /// Leaves non-appearance identity fields (characterName, totalHeight, headScale, outfitType, hasNameBadge, badgeText) unchanged.
        /// </summary>
        public void CopyAppearanceFrom(BobbleheadConfig source)
        {
            if (source == null) return;
            this.hairStyle = source.hairStyle;
            this.headwear = source.headwear;
            this.faceShape = source.faceShape;
            this.facialHair = source.facialHair;
            this.bodyPresentation = source.bodyPresentation;
            this.hasGlasses = source.hasGlasses;
            this.hasBackpack = source.hasBackpack;
            this.hasSatchel = source.hasSatchel;
            CopyColorsFrom(source);
        }

        /// <summary>
        /// Compares all appearance fields (hairStyle, headwear, faceShape, facialHair, bodyPresentation) and colors with another configuration.
        /// </summary>
        public bool AreAppearancesEqual(BobbleheadConfig other, float tolerance = 0.002f)
        {
            if (other == null) return false;
            return this.hairStyle == other.hairStyle &&
                   this.headwear == other.headwear &&
                   this.faceShape == other.faceShape &&
                   this.facialHair == other.facialHair &&
                   this.bodyPresentation == other.bodyPresentation &&
                   AreColorsEqual(other, tolerance);
        }

        private static bool ColorsMatch(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) <= tolerance &&
                   Mathf.Abs(a.g - b.g) <= tolerance &&
                   Mathf.Abs(a.b - b.b) <= tolerance &&
                   Mathf.Abs(a.a - b.a) <= tolerance;
        }
    }
}
