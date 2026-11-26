using System;
using System.Collections.Generic;
using UnityEngine;

namespace MyVirtualCommunity.Client.Progression
{
    /// <summary>
    /// Runtime-safe, read-only localized metadata catalog for existing authored discoveries.
    /// Keeps the Discovery Journal usable even when additive scenes (such as BackroomsLevel1Scene)
    /// are unloaded, without inventing new narrative canon or duplicating gameplay code values.
    /// </summary>
    public static class DiscoveryJournalCatalog
    {
        public sealed class LocalizedEntryMetadata
        {
            public string TitleEs { get; }
            public string TitleEn { get; }
            public string SummaryEs { get; }
            public string SummaryEn { get; }
            public Color AccentColor { get; }

            public LocalizedEntryMetadata(string titleEs, string titleEn, string summaryEs, string summaryEn, Color accentColor)
            {
                TitleEs = titleEs;
                TitleEn = titleEn;
                SummaryEs = summaryEs;
                SummaryEn = summaryEn;
                AccentColor = accentColor;
            }

            public string GetTitle(bool isEs) => isEs ? TitleEs : TitleEn;
            public string GetSummary(bool isEs) => isEs ? SummaryEs : SummaryEn;
        }

        public sealed class LearnedCodeMetadata
        {
            public string TitleEs { get; }
            public string TitleEn { get; }
            public string SummaryEs { get; }
            public string SummaryEn { get; }
            public string AssociatedDoorId { get; }

            public LearnedCodeMetadata(string titleEs, string titleEn, string summaryEs, string summaryEn, string associatedDoorId)
            {
                TitleEs = titleEs;
                TitleEn = titleEn;
                SummaryEs = summaryEs;
                SummaryEn = summaryEn;
                AssociatedDoorId = associatedDoorId;
            }

            public string GetTitle(bool isEs) => isEs ? TitleEs : TitleEn;
            public string GetSummary(bool isEs) => isEs ? SummaryEs : SummaryEn;
        }

        // Authored Backrooms & threshold descriptors mirrored strictly from BackroomsLevel1Builder.cs
        private static readonly Dictionary<string, LocalizedEntryMetadata> EvidenceCatalog =
            new Dictionary<string, LocalizedEntryMetadata>(StringComparer.Ordinal)
            {
                // Underground maintenance clue chain (Sector C-0 -> La Cisterna -> Abasolo Siphon)
                [ProgressionIds.ClueSewerSectorC0] = new LocalizedEntryMetadata(
                    "Marcación de mantenimiento I — Sector C-0",
                    "Maintenance Marking I — Sector C-0",
                    "Una placa industrial deteriorada muestra el primer fragmento de una secuencia: 58. La numeración indica que existen otros registros.",
                    "A weathered industrial plate displays the first fragment of a sequence: 58. Its numbering suggests that other records exist.",
                    new Color(0.98f, 0.80f, 0.34f)),

                [ProgressionIds.ClueCisternElNudo] = new LocalizedEntryMetadata(
                    "Marcación de mantenimiento II — La Cisterna",
                    "Maintenance Marking II — The Cistern",
                    "Una segunda placa conserva el fragmento 19. La secuencia debe respetar el orden de sus marcaciones.",
                    "A second plate preserves fragment 19. The sequence must respect the order of its markings.",
                    new Color(0.98f, 0.80f, 0.34f)),

                [ProgressionIds.ClueAbasoloSiphon] = new LocalizedEntryMetadata(
                    "Marcación de mantenimiento III — Sifón de Abasolo",
                    "Maintenance Marking III — Abasolo Siphon",
                    "El último registro contiene el fragmento 26. Tres marcaciones, dos cifras cada una.",
                    "The final record contains fragment 26. Three markings, two digits each.",
                    new Color(0.98f, 0.80f, 0.34f)),

                // Remanencia Nivel 0 — Hotel de las Palomas environmental narrative discoveries (Implementation 07.6)
                [ProgressionIds.ClueRemanenciaHotelPlan01] = new LocalizedEntryMetadata(
                    "Plano de Obra Inconclusa — Nivel de Acceso",
                    "Unfinished Project Plan — Ground Level",
                    "Plano arquitectónico de obra inconclusa hallado junto a Recepción. Aunque coincide parcialmente con la planta actual, traza un pasillo y una estancia donde hoy existe un muro macizo de concreto; discrepancia sin explicación confirmada.",
                    "Unfinished architectural plan found near Reception. While it partially matches the current ground floor, it maps an extra corridor and room where a solid concrete wall stands today—an unexplained discrepancy.",
                    new Color(0.68f, 0.82f, 0.90f)),

                [ProgressionIds.ClueRemanenciaHotelRegister02] = new LocalizedEntryMetadata(
                    "Hoja de Registro de Habitaciones (Fragmento)",
                    "Room Registration Sheet (Fragment)",
                    "Hoja dañada de registro hallada junto a las habitaciones 109 y 110. Anota cuartos aún vacíos y una habitación inexistente ('111-B') con fechas incompletas anteriores al fin de la obra; contradicción sin causa verificada.",
                    "Damaged room ledger sheet found near Rooms 109 and 110. Lists unfinished empty rooms alongside a non-existent '111-B' entry with incomplete dates recorded before construction ended—an unresolved contradiction.",
                    new Color(0.88f, 0.82f, 0.68f)),

                [ProgressionIds.ClueRemanenciaHotelMaintenance03] = new LocalizedEntryMetadata(
                    "Parte de Mantenimiento — Obra Norte",
                    "Maintenance Log — North Wing Works",
                    "Registro de servicio localizado en el pasillo de mantenimiento del Ala Norte. Reporta manchas grisáceas en juntas secas del concreto, cortes eléctricos sin falla, resanes previos al daño y medidas que no cuadran con los planos.",
                    "Service log located in the North Wing maintenance corridor. Reports greyish stains along dry concrete joints, unsourced power drops, repairs logged before damage occurred, and dimensions that contradict the drawings.",
                    new Color(0.84f, 0.76f, 0.64f)),

                [ProgressionIds.ClueRemanenciaHotelCobaltRumor04] = new LocalizedEntryMetadata(
                    "Nota Anónima — Rumor de Obra",
                    "Anonymous Note — Construction Rumor",
                    "Nota anónima a lápiz sin sustento oficial hallada al fondo del área de servicio. Recoge un rumor no verificado sobre supuesta varilla contaminada con Cobalto-60 dentro de esta versión del hotel; sin pruebas, deja abierto el origen real de las manchas.",
                    "Unsigned pencil note with no official source found deep in the back-of-house service core. Passes along an unverified rumor about allegedly Cobalt-60-contaminated rebar inside this version of the hotel; offers no proof and leaves the stains' true cause open.",
                    new Color(0.86f, 0.74f, 0.56f)),

                [ProgressionIds.ArtifactEvidenceBoardPdn00] = new LocalizedEntryMetadata(
                    "Muro de Evidencias Conspirativas PDN-00",
                    "Conspiracy Evidence Board PDN-00",
                    "Fotografías de Av. Benito Juárez conectadas con hilos rojos hacia un boceto del Espejo Dimensional. Nota amarilla: 'BÓVEDA NIVEL Ω EN ALA NOROESTE • CÓDIGO INVERSO: 6-5-4-3-2-1'.",
                    "Photos of Av. Benito Juárez linked with red yarn to a sketch of the Dimensional Mirror. Yellow note: 'LEVEL-Ω VAULT IN NW WING • REVERSE CODE: 6-5-4-3-2-1'.",
                    new Color(1.0f, 0.88f, 0.28f)),

                [ProgressionIds.ArtifactOjoDelUmbral] = new LocalizedEntryMetadata(
                    "El Ojo del Umbral (Astrolabio Dimensional de Obsidiana)",
                    "The Eye of the Threshold (Obsidian Dimensional Astrolabe)",
                    "Reliquia octagonal de latón grabado y obsidiana con un núcleo de fisura astral. Encaja en la Cuenca Octagonal de la única Puerta Dimensional del laberinto (en el extremo Suroeste).",
                    "Octagonal relic of engraved brass and obsidian with an astral rift core. Fits into the Octagonal Socket of the maze's single Dimensional Portal (in the far Southwest chamber).",
                    new Color(0.32f, 0.94f, 1.0f)),

                [ProgressionIds.PortalUmbralDeLasser] = new LocalizedEntryMetadata(
                    "El Umbral de Lasser — Puerta Dimensional",
                    "The Lasser Threshold — Dimensional Portal",
                    "El único portal dimensional escondido en lo más profundo del laberinto. Su pilar derecho posee una Cuenca Octagonal que requiere 'El Ojo del Umbral' para liberar las cadenas.",
                    "The sole dimensional gateway hidden deep within the maze. Its right pillar holds an Octagonal Socket requiring 'The Eye of the Threshold' to release the chains.",
                    new Color(0.55f, 0.86f, 1.0f)),

                [ProgressionIds.PortalCuencaOctagonal] = new LocalizedEntryMetadata(
                    "Cuenca Octagonal del Umbral (Receptáculo Astral)",
                    "Octagonal Threshold Socket (Astral Receptacle)",
                    "Receptáculo octagonal tallado en latón y obsidiana. Inserta aquí 'El Ojo del Umbral' (resguardado en la Caja Fuerte Nivel Ω del Ala Noroeste) para activar el Portal Dimensional.",
                    "Octagonal brass-and-obsidian receptacle. Insert 'The Eye of the Threshold' (from the NW Wing Level-Ω Vault Safe) here to activate the Dimensional Portal.",
                    new Color(1.0f, 0.78f, 0.32f)),

                ["artifact:mascara-paquime"] = new LocalizedEntryMetadata(
                    "Máscara Ceremonial de Obsidiana y Turquesa (EXP-12 • Paquimé)",
                    "Ceremonial Obsidian & Turquoise Mask (EXP-12 • Paquimé)",
                    "Máscara prehispánica tallada en obsidiana con grecas de Paquimé y ojos de turquesa que brillan hacia el Portal Dimensional.",
                    "Pre-Hispanic obsidian mask with Paquimé step-frets and turquoise eyes glowing toward the Dimensional Portal.",
                    new Color(0.20f, 0.94f, 0.80f)),

                ["artifact:brujula-astral"] = new LocalizedEntryMetadata(
                    "Brújula Astral Anómala (EXP-13 — Apunta hacia el Portal)",
                    "Anomalous Astral Compass (EXP-13 — Points to the Portal)",
                    "Ignora el norte magnético y su aguja cian apunta invariablemente hacia el Suroeste, directo a la Cámara del Umbral Dimensional en el rincón más alejado del laberinto.",
                    "Ignores magnetic north; its glowing cyan needle points unerringly Southwest toward the Dimensional Threshold Chamber in the farthest corner of the maze.",
                    new Color(0.38f, 0.92f, 1.0f)),

                [ProgressionIds.ArtifactShiftClock0307] = new LocalizedEntryMetadata(
                    "Reloj de Turno 03:07",
                    "Shift Clock 03:07",
                    "Reloj checador industrial detenido a las 03:07. Sin importar la hora en Ciudad Juárez ni el turno del corredor fronterizo, sus manecillas jamás avanzan.",
                    "Industrial shift timekeeper frozen at 03:07. Regardless of the local hour in Ciudad Juárez or the border corridor shift, its hands never move.",
                    new Color(1.0f, 0.80f, 0.34f)),

                [ProgressionIds.ArtifactSealedNotebook] = new LocalizedEntryMetadata(
                    "Cuaderno de Páginas Selladas",
                    "Notebook of Sealed Pages",
                    "Cuaderno de campo manchado por la humedad con diagramas técnicos doblados y varias hojas pegadas entre sí. Sus márgenes registran lecturas de un plano que no coincide con la superficie.",
                    "Water-damaged field notebook filled with folded technical diagrams and pages sealed together. Its margins log survey measurements that do not match the surface grid.",
                    new Color(0.82f, 0.84f, 0.90f)),

                [ProgressionIds.ArtifactServiceIntercom] = new LocalizedEntryMetadata(
                    "Intercomunicador de Servicio sin Línea",
                    "Unwired Service Intercom",
                    "Caja de intercomunicación municipal fijada a la columna con el ducto eléctrico cortado. Aunque no existe tendido conectado, el altavoz emite breves chasquidos de portadora en los pasillos vacíos.",
                    "Municipal-style wall intercom box bolted to the column with its conduit sheared off. Though no line is connected, the speaker emits brief carrier clicks across the empty halls.",
                    new Color(0.75f, 0.80f, 0.90f)),

                [ProgressionIds.ArtifactSignalMonitor] = new LocalizedEntryMetadata(
                    "Monitor de Señal sin Origen",
                    "Unsourced Signal Monitor",
                    "Monitor CRT industrial encendido sin cable de alimentación en un nicho del ala Oeste. Sus barras horizontales de interferencia barren la pantalla en un ciclo irregular sin fuente identificable.",
                    "Industrial CRT test monitor glowing without a power cord in a West wing niche. Irregular horizontal scan bars drift across its screen with no traceable broadcast source.",
                    new Color(0.55f, 0.84f, 1.0f)),

                [ProgressionIds.ArtifactSpecimenVitrine] = new LocalizedEntryMetadata(
                    "Vitrina de Muestra: Figura sin Rasgos",
                    "Specimen Vitrine: Featureless Figure",
                    "Vitrina sellada de archivo que resguarda una forma cerámica lisa y sin rostro ni inscripción. Su placa de inventario carece de fecha de ingreso o procedencia.",
                    "Sealed archival vitrine housing a smooth, featureless ceramic form with no markings. Its brass inventory tag carries neither an intake date nor a point of origin.",
                    new Color(0.84f, 0.78f, 0.66f)),

                [ProgressionIds.ArtifactArticulatedWoodAssembly] = new LocalizedEntryMetadata(
                    "Ensamble de Madera Articulado",
                    "Articulated Wooden Assembly",
                    "Estructura articulada de madera y pernos de latón abandonada sobre una caja de archivo. Sus brazos móviles conservan marcas de calibración a lápiz, aunque no corresponde a ningún instrumento convencional.",
                    "Hand-built jointed wooden frame with brass pivot pins left atop an archive crate. Its hinged arms bear pencil calibration marks, yet it matches no standard drafting or survey tool.",
                    new Color(0.86f, 0.68f, 0.42f)),

                [ProgressionIds.ArtifactArchiveCase06] = new LocalizedEntryMetadata(
                    "Caja de Archivo 06",
                    "Archive Case 06",
                    "Contenedor metálico de archivo sin logotipo comercial, asegurado con cierre de acero sobre las cajas húmedas del pasillo Norte. Su etiqueta numerada '06' está en blanco en el reverso.",
                    "Unbranded archival storage case secured with a steel latch atop damp corridor cartons in the North hall. Its numbered index tag '06' bears no sender or contents manifest.",
                    new Color(0.76f, 0.80f, 0.84f)),

                [ProgressionIds.ArtifactSealedCustodyCase] = new LocalizedEntryMetadata(
                    "Estuche de Custodia Sellado",
                    "Sealed Custody Case",
                    "Estuche de madera oscura reforzado con barras de sujeción de acero y precinto metálico de custodia municipal. Al acercarse se percibe un zumbido sordo amortiguado en su interior.",
                    "Dark wood custody chest clamped shut with industrial steel retention bars and a metal transit seal. Up close, a faint muffled resonance vibrates against the casing.",
                    new Color(0.92f, 0.64f, 0.28f)),

                [ProgressionIds.ArtifactAnomalousGeometryAtlas] = new LocalizedEntryMetadata(
                    "Atlas de Geometría Anómala",
                    "Atlas of Anomalous Geometry",
                    "Folio técnico de gran formato colocado sobre un atril de acero junto a la bóveda Nivel Ω. Contiene levantamientos topográficos de pasillos cerrados en bucle y ángulos imposibles.",
                    "Oversized technical archival folio resting on a steel drafting stand beside the Level-Ω vault. Its bound plates chart closed-loop corridors and non-Euclidean interior angles.",
                    new Color(0.95f, 0.68f, 0.24f)),

                [ProgressionIds.ArtifactFracturedInsulator] = new LocalizedEntryMetadata(
                    "Aislador Cerámico Fracturado",
                    "Fractured Ceramic Insulator",
                    "Aislador de porcelana de alta tensión con tres campanas acanaladas y una fractura vítrea en el núcleo. Conserva restos de hollín eléctrico pese a hallarse lejos de cualquier subestación.",
                    "Ribbed high-voltage porcelain insulator split by a glassy fracture seam across its core. It retains traces of arc scorch despite sitting far from any utility line or substation.",
                    new Color(0.92f, 0.88f, 0.78f)),

                [ProgressionIds.ArtifactFoldedTopologyCore] = new LocalizedEntryMetadata(
                    "Núcleo de Topología Plegada",
                    "Folded Topology Core",
                    "Ensamble mecánico asimétrico de placas cerámicas y aluminio montado sobre una cuna de acero. Sus secciones escalonadas se desfasan entre sí como un modelo a escala de un espacio plegado.",
                    "Asymmetric mechanical assembly of stepped ceramic and aluminum plates seated in a steel cradle. Its offset tiers shift out of phase like a scale model of folded architectural space.",
                    new Color(0.74f, 0.86f, 0.96f)),

                [ProgressionIds.ArtifactCoordinatePlottingPlate] = new LocalizedEntryMetadata(
                    "Lámina de Trazado de Coordenadas",
                    "Coordinate Plotting Plate",
                    "Placa cartográfica de pizarra y latón con retícula ortogonal y regla deslizable. Sus coordenadas trazan intersecciones bajo la Avenida Benito Juárez que no figuran en los planos catastrales.",
                    "Slate-and-brass cartographic plotting plate fitted with an orthogonal grid and sliding vernier rule. Its etched intersections map chambers beneath Avenida Benito Juárez absent from civic surveys.",
                    new Color(0.86f, 0.72f, 0.46f)),

                ["reality-bridge:laptop-mystery-terminal"] = new LocalizedEntryMetadata(
                    "Estación Solitaria • Laptop Conectada",
                    "Lone Workstation • Connected Laptop",
                    "El único escritorio y silla en todo este nivel. Su pantalla permanece encendida buscando señal de red.",
                    "The sole desk and chair in this entire level. Its glowing screen searches for an uplink.",
                    new Color(0.45f, 0.82f, 1.0f)),

                ["reality-bridge:fm-radio-receiver"] = new LocalizedEntryMetadata(
                    "Radio FM Analógico • Sintonizador 99.9 MHz",
                    "Analog FM Radio • 99.9 MHz Tuner",
                    "Antiguo receptor FM con dial ámbar encendido en medio del pasillo.",
                    "Vintage FM radio glowing with an amber dial in the hall.",
                    new Color(1.0f, 0.76f, 0.30f)),

                ["reality-bridge:alexa-smart-speaker"] = new LocalizedEntryMetadata(
                    "Altavoz Inteligente en Mueble Esquinero",
                    "Smart Speaker on Corner Stand",
                    "Anillo cian en modo escucha sobre un mueble esquinero solitario.",
                    "Cyan light ring pulsing in listening mode atop a lone corner cabinet.",
                    new Color(0.18f, 0.88f, 1.0f)),

                ["reality-bridge:diario-de-juarez-ipad"] = new LocalizedEntryMetadata(
                    "Tableta en Atril • Portada Digital",
                    "Reading Stand Tablet • Digital Front Page",
                    "Tableta sobre atril mostrando una portada digital dentro del laberinto.",
                    "Tablet on a floor stand displaying a digital front page inside the labyrinth.",
                    new Color(0.96f, 0.96f, 0.96f)),

                ["reality-bridge:juarez-live-telemetry"] = new LocalizedEntryMetadata(
                    "Monitor de Telemetría Mural",
                    "Wall Telemetry Monitor",
                    "Pantalla mural anómala orientada hacia el corredor exterior de Ciudad Juárez.",
                    "Anomalous wall monitor oriented toward the outside Ciudad Juárez corridor.",
                    new Color(0.32f, 0.95f, 0.85f)),

                ["reality-bridge:payphone-voice-line"] = new LocalizedEntryMetadata(
                    "Teléfono Público de Monedas",
                    "Public Coin Payphone",
                    "Caseta metálica de los 90 instalada en un pasillo amarillo sin salida.",
                    "1990s steel payphone mounted in a yellow dead-end corridor.",
                    new Color(1.0f, 0.42f, 0.42f)),

                ["reality-bridge:fideicomiso-cctv-frame"] = new LocalizedEntryMetadata(
                    "Marco Digital CCTV",
                    "Digital CCTV Frame",
                    "Marco metálico colgado en la pared con imágenes granuladas del corredor fronterizo.",
                    "Wall-mounted steel display showing grainy frames of the border corridor.",
                    new Color(0.58f, 0.84f, 1.0f)),

                ["reality-bridge:thermal-teletype-printer"] = new LocalizedEntryMetadata(
                    "Impresora Térmica de Teletipo",
                    "Thermal Teletype Printer",
                    "Imprime tiras continuas de papel sobre la alfombra húmeda del laberinto.",
                    "Spools continuous receipt paper onto the damp carpet of the labyrinth.",
                    new Color(0.88f, 0.94f, 1.0f))
            };

        private static readonly Dictionary<string, LearnedCodeMetadata> LearnedCodeCatalog =
            new Dictionary<string, LearnedCodeMetadata>(StringComparer.Ordinal)
            {
                [ProgressionIds.CodeBuildingASouthFromSewerClues] = new LearnedCodeMetadata(
                    "Secuencia de acceso — Puerta misteriosa (Edificio A Sur)",
                    "Access Sequence — Mysterious Door (Building A South)",
                    "Secuencia de seis cifras reunida a partir de las tres marcaciones subterráneas (I → II → III) para la puerta oculta del Edificio A Sur.",
                    "Six-digit sequence assembled from the three underground markings (I → II → III) for the concealed door at Building A South.",
                    ProgressionIds.DoorBuildingASouth),

                // Legacy pre-clue-chain demo code entry: intentionally does NOT resolve the new live door code
                [ProgressionIds.CodeBuildingASouthSecretDoor] = new LearnedCodeMetadata(
                    "Registro previo — Puerta misteriosa (Edificio A Sur)",
                    "Legacy Access Record — Mysterious Door (Building A South)",
                    "Registro histórico de una versión anterior del acceso al Edificio A Sur. La secuencia actual requiere reunir las tres marcaciones subterráneas.",
                    "Historical record from an earlier build of Building A South access. The current sequence requires gathering the three underground markings.",
                    null),

                [ProgressionIds.CodeOmegaVaultSafe] = new LearnedCodeMetadata(
                    "Código de acceso — Caja Fuerte de Contención (Nivel Ω)",
                    "Access Code — Containment Vault Safe (Level Ω)",
                    "Código de la bóveda Nivel Ω en el Ala Noroeste del laberinto.",
                    "Combination for the Level-Ω containment safe in the Northwest wing of the labyrinth.",
                    ProgressionIds.SafeBackroomsOmegaVault)
            };

        private static readonly Dictionary<string, LocalizedEntryMetadata> PuzzleCatalog =
            new Dictionary<string, LocalizedEntryMetadata>(StringComparer.Ordinal)
            {
                [ProgressionIds.PuzzleBuildingASouthDoor] = new LocalizedEntryMetadata(
                    "Chapa electrónica — Puerta misteriosa (Edificio A Sur)",
                    "Electronic Keypad — Mysterious Door (Building A South)",
                    "Desbloqueaste el acceso a la cámara secreta tras la fachada sur del Edificio A.",
                    "Unlocked access to the secret chamber behind the south facade of Building A.",
                    new Color(0.38f, 0.92f, 0.64f)),

                [ProgressionIds.PuzzleOmegaVaultSafe] = new LocalizedEntryMetadata(
                    "Caja Fuerte de Contención (Nivel Ω)",
                    "Containment Vault Safe (Level Ω)",
                    "Abriste la caja fuerte de contención en el Ala Noroeste que resguardaba El Ojo del Umbral.",
                    "Opened the containment vault safe in the Northwest wing holding The Eye of the Threshold.",
                    new Color(0.38f, 0.92f, 0.64f)),

                [ProgressionIds.PuzzleLasserPortalActivation] = new LocalizedEntryMetadata(
                    "Activación de El Umbral de Lasser",
                    "Activation of The Lasser Threshold",
                    "Colocaste El Ojo del Umbral en la Cuenca Octagonal y liberaste las cadenas del portal dimensional.",
                    "Placed The Eye of the Threshold into the Octagonal Socket and released the chains of the dimensional portal.",
                    new Color(0.42f, 0.92f, 1.0f))
            };

        private static readonly Dictionary<string, LocalizedEntryMetadata> ArtifactCatalog =
            new Dictionary<string, LocalizedEntryMetadata>(StringComparer.Ordinal)
            {
                [ProgressionIds.ArtifactOjoDelUmbral] = new LocalizedEntryMetadata(
                    "El Ojo del Umbral",
                    "The Eye of the Threshold",
                    "Reliquia octagonal de latón grabado y obsidiana con núcleo de fisura astral recuperada de la Caja Fuerte Nivel Ω.",
                    "Octagonal relic of engraved brass and obsidian with an astral rift core recovered from the Level-Ω Vault Safe.",
                    new Color(0.32f, 0.94f, 1.0f))
            };

        private static readonly Dictionary<string, LocalizedEntryMetadata> MilestoneCatalog =
            new Dictionary<string, LocalizedEntryMetadata>(StringComparer.Ordinal)
            {
                [ProgressionIds.MilestoneUndergroundNetworkEntered] = new LocalizedEntryMetadata(
                    "Red subterránea de drenaje explorada",
                    "Underground Storm-Drain Network Entered",
                    "Descendiste bajo las calles de Avenida Benito Juárez hacia los colectores subterráneos.",
                    "Descended beneath the streets of Avenida Benito Juárez into the underground storm drains.",
                    new Color(0.68f, 0.84f, 0.96f)),

                [ProgressionIds.MilestoneRioBravoOutflowOpened] = new LocalizedEntryMetadata(
                    "Reja del desagüe hacia el Río Bravo abierta",
                    "Río Bravo Outflow Gate Opened",
                    "Abriste la reja oxidada al extremo norte del colector hacia la ribera sur del Río Bravo.",
                    "Opened the rusted iron gate at the northern collector headwall onto the south bank of the Río Bravo.",
                    new Color(0.68f, 0.84f, 0.96f)),

                [ProgressionIds.MilestoneSecretRoomKeypadUnlocked] = new LocalizedEntryMetadata(
                    "Puerta misteriosa del Edificio A Sur desbloqueada",
                    "Building A South Mysterious Door Unlocked",
                    "Ingresaste el código correcto en la puerta oculta sobre Avenida Benito Juárez.",
                    "Entered the correct access code on the concealed door along Avenida Benito Juárez.",
                    new Color(0.45f, 0.94f, 0.65f)),

                [ProgressionIds.MilestoneBackroomsEntered] = new LocalizedEntryMetadata(
                    "Ingreso a Remanencia — Nivel 0",
                    "Entered Remanencia — Level 0",
                    "Cruzaste el umbral hacia el laberinto liminal de iluminación fluorescente.",
                    "Crossed the threshold into the fluorescent liminal labyrinth.",
                    new Color(1.0f, 0.88f, 0.42f)),

                [ProgressionIds.MilestoneBackroomsEnteredViaVeil] = new LocalizedEntryMetadata(
                    "Cruce por el Velo del Cuarto Secreto",
                    "Crossed Through the Secret Room Threshold Veil",
                    "Atravesaste el velo oscuro del cuarto secreto en el Edificio A Sur.",
                    "Stepped through the dark threshold veil inside the Building A South secret room.",
                    new Color(0.95f, 0.84f, 0.42f)),

                [ProgressionIds.MilestoneBackroomsEnteredViaAzucenas] = new LocalizedEntryMetadata(
                    "Caída anómala desde Av. Juárez × Azucenas",
                    "Anomalous Drop from Av. Juárez × Azucenas",
                    "Al descender por la alcantarilla de Av. Juárez y Azucenas, la realidad se fracturó hacia el laberinto.",
                    "While descending the Av. Juárez × Azucenas manhole, reality fractured into the labyrinth.",
                    new Color(0.95f, 0.84f, 0.42f)),

                [ProgressionIds.MilestoneEvidenceBoardPdn00Discovered] = new LocalizedEntryMetadata(
                    "Muro de Evidencias PDN-00 descubierto",
                    "Evidence Board PDN-00 Discovered",
                    "Localizaste el muro de evidencias con notas y referencias hacia la bóveda Nivel Ω.",
                    "Located the evidence board containing notes and references pointing to the Level-Ω vault.",
                    new Color(1.0f, 0.88f, 0.28f)),

                [ProgressionIds.MilestoneOmegaSafeUnlocked] = new LocalizedEntryMetadata(
                    "Caja Fuerte de Contención (Nivel Ω) abierta",
                    "Level-Ω Containment Vault Safe Unlocked",
                    "Desbloqueaste la caja fuerte del Ala Noroeste dentro del laberinto.",
                    "Unlocked the Northwest wing containment safe inside the labyrinth.",
                    new Color(0.45f, 0.94f, 0.65f)),

                [ProgressionIds.MilestoneOjoDelUmbralAcquired] = new LocalizedEntryMetadata(
                    "Reliquia obtenida: El Ojo del Umbral",
                    "Relic Acquired: The Eye of the Threshold",
                    "Tomaste el astrolabio octagonal de obsidiana y latón desde la caja fuerte.",
                    "Took the octagonal obsidian-and-brass astrolabe from inside the safe.",
                    new Color(0.32f, 0.94f, 1.0f)),

                [ProgressionIds.MilestoneLasserPortalActivated] = new LocalizedEntryMetadata(
                    "El Umbral de Lasser activado",
                    "The Lasser Threshold Activated",
                    "Encajaste El Ojo del Umbral en la Cuenca Octagonal, liberando las cadenas del portal.",
                    "Fitted The Eye of the Threshold into the Octagonal Socket, releasing the portal chains.",
                    new Color(0.45f, 0.92f, 1.0f)),

                [ProgressionIds.MilestoneReturnedFromBackrooms] = new LocalizedEntryMetadata(
                    "Regreso a Avenida Benito Juárez",
                    "Returned to Avenida Benito Juárez",
                    "Saliste del laberinto por la puerta de retorno y volviste al corredor fronterizo.",
                    "Exited the labyrinth through the return door back onto the border corridor.",
                    new Color(0.74f, 0.88f, 1.0f))
            };

        public static bool TryGetEvidence(string evidenceId, out LocalizedEntryMetadata metadata)
        {
            string canonical = ProgressionIds.CanonicalizeEvidenceId(evidenceId);
            if (string.IsNullOrEmpty(canonical))
            {
                metadata = null;
                return false;
            }
            return EvidenceCatalog.TryGetValue(canonical, out metadata);
        }

        public static LocalizedEntryMetadata GetEvidenceOrFallback(string evidenceId)
        {
            if (TryGetEvidence(evidenceId, out LocalizedEntryMetadata found))
            {
                return found;
            }
            return new LocalizedEntryMetadata(
                "Evidencia registrada",
                "Recorded Evidence",
                "Hallazgo conservado en tu diario personal durante la exploración.",
                "Finding preserved in your personal journal during exploration.",
                new Color(0.80f, 0.85f, 0.92f));
        }

        public static bool TryGetLearnedCode(string codeId, out LearnedCodeMetadata metadata)
        {
            if (string.IsNullOrWhiteSpace(codeId))
            {
                metadata = null;
                return false;
            }
            return LearnedCodeCatalog.TryGetValue(codeId.Trim(), out metadata);
        }

        public static LocalizedEntryMetadata GetPuzzleOrFallback(string puzzleId)
        {
            if (!string.IsNullOrWhiteSpace(puzzleId) && PuzzleCatalog.TryGetValue(puzzleId.Trim(), out LocalizedEntryMetadata found))
            {
                return found;
            }
            return new LocalizedEntryMetadata(
                "Acertijo o mecanismo resuelto",
                "Solved Mechanism or Puzzle",
                "Mecanismo superado y registrado de forma permanente en tu progreso.",
                "Mechanism solved and permanently recorded in your progression.",
                new Color(0.45f, 0.92f, 0.65f));
        }

        public static LocalizedEntryMetadata GetArtifactOrFallback(string artifactId)
        {
            if (!string.IsNullOrWhiteSpace(artifactId) && ArtifactCatalog.TryGetValue(artifactId.Trim(), out LocalizedEntryMetadata found))
            {
                return found;
            }
            return new LocalizedEntryMetadata(
                "Objeto narrativo adquirido",
                "Acquired Narrative Artifact",
                "Objeto conservado en tu registro personal de descubrimientos.",
                "Artifact preserved in your personal discovery record.",
                new Color(0.32f, 0.94f, 1.0f));
        }

        public static LocalizedEntryMetadata GetMilestoneOrFallback(string milestoneId)
        {
            if (!string.IsNullOrWhiteSpace(milestoneId) && MilestoneCatalog.TryGetValue(milestoneId.Trim(), out LocalizedEntryMetadata found))
            {
                return found;
            }
            return new LocalizedEntryMetadata(
                "Hito de exploración registrado",
                "Exploration Milestone Recorded",
                "Lugar o hito alcanzado durante tu recorrido.",
                "Place or milestone reached during your journey.",
                new Color(0.75f, 0.85f, 0.95f));
        }
    }
}
