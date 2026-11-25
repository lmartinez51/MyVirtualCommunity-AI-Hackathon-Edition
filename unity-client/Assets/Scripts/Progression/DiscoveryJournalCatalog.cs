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

                ["artifact:reloj-0307"] = new LocalizedEntryMetadata(
                    "Reloj Despertador Detenido a las 03:07 AM (EXP-00 • El Conjuro)",
                    "Alarm Clock Frozen at 03:07 AM (EXP-00 • The Conjuring)",
                    "Todos los relojes dentro de los Backrooms se detienen exactamente a las 03:07 AM sin importar la hora de Ciudad Juárez.",
                    "Every clock inside the Backrooms stops at exactly 03:07 AM regardless of the real time in Ciudad Juárez.",
                    new Color(1.0f, 0.80f, 0.34f)),

                ["artifact:libro-babadook"] = new LocalizedEntryMetadata(
                    "Libro Emergente 'Mister Babadook' (EXP-02)",
                    "'Mister Babadook' Pop-Up Storybook (EXP-02)",
                    "Libro infantil de tapa carbonizada abandonado sobre la alfombra húmeda. Atado con correa para impedir que sus páginas emergentes se abran.",
                    "Charcoal-black pop-up book lying alone on the damp carpet. Strapped shut so its pop-up pages cannot unfold.",
                    new Color(0.82f, 0.84f, 0.90f)),

                ["artifact:telefono-negro"] = new LocalizedEntryMetadata(
                    "El Teléfono Negro con Cable Cortado (EXP-11 • The Black Phone)",
                    "The Black Rotary Phone with Severed Cord (EXP-11 • The Black Phone)",
                    "Teléfono mural de baquelita montado en una columna amarilla del gran salón. Su cable está cortado, pero a veces timbra en el silencio de los pasillos.",
                    "Black Bakelite wall telephone mounted on a yellow column in the great pillared hall. Its cord is severed, yet it still rings in the silence.",
                    new Color(0.75f, 0.80f, 0.90f)),

                ["artifact:cinta-vhs-el-aro"] = new LocalizedEntryMetadata(
                    "Televisor CRT y Cinta VHS '7 DÍAS' (EXP-04 • El Aro / Ringu)",
                    "CRT Television & '7 DAYS' Cursed VHS (EXP-04 • The Ring)",
                    "Televisor de los 90 encendido sin estar enchufado en un nicho del ala Oeste. Reproduce en bucle un eclipse circular de estática. Etiqueta: '7 DÍAS'.",
                    "Unplugged 1990s CRT TV glowing in a West wing niche, looping a circular eclipse of static. Tape label: '7 DAYS'.",
                    new Color(0.55f, 0.84f, 1.0f)),

                ["artifact:muneca-porcelana"] = new LocalizedEntryMetadata(
                    "Vitrina de Contención: Muñeca de Porcelana (EXP-03 • Annabelle)",
                    "Containment Vitrine: Porcelain Conduit Doll (EXP-03 • Annabelle)",
                    "Muñeca de porcelana agrietada encerrada tras cristal bendecido. Letrero superior: 'ADVERTENCIA: POSITIVAMENTE NO ABRIR'.",
                    "Cracked porcelain doll sealed behind blessed glass. Warning plaque: 'WARNING: POSITIVELY DO NOT OPEN'.",
                    new Color(1.0f, 0.36f, 0.38f)),

                ["artifact:titere-espiral"] = new LocalizedEntryMetadata(
                    "Títere de Mejillas en Espiral (EXP-05 • Billy / Saw)",
                    "Spiral-Cheek Ventriloquist Puppet (EXP-05 • Billy / Saw)",
                    "Posado sobre un cajón solitario con smoking negro y corbatín rojo. Su grabadora interna murmura: 'Quiero jugar un juego...'.",
                    "Perched atop a lone wooden crate in black tuxedo and red bow tie. Its internal recorder whispers: 'I want to play a game...'.",
                    new Color(0.95f, 0.22f, 0.24f)),

                ["artifact:muneco-good-guy"] = new LocalizedEntryMetadata(
                    "Caja de Juguete 'Good Guy' con Rasguños Internos (EXP-10 • Chucky)",
                    "'Good Guy' Toy Box with Internal Claw Marks (EXP-10 • Child's Play)",
                    "Caja amarilla asegurada con fleje de acero sobre cajas de archivo húmedas. Hay marcas de rasguños hechas desde el interior.",
                    "Factory yellow toy box strapped in steel atop damp archive boxes. Deep claw marks tear outward from inside.",
                    new Color(1.0f, 0.82f, 0.18f)),

                ["artifact:caja-dybbuk"] = new LocalizedEntryMetadata(
                    "La Caja Dybbuk Sellada con Cadenas (EXP-01 • El Origen del Mal)",
                    "Chained Dybbuk Wine Cabinet (EXP-01 • The Possession)",
                    "Antigua licorera de caoba atada con cadenas de hierro y rodeada por un cerco de sal blanca sobre la alfombra húmeda.",
                    "Antique mahogany wine cabinet bound in iron chains inside a white salt perimeter on the damp carpet.",
                    new Color(0.92f, 0.64f, 0.28f)),

                ["artifact:grimorio-necronomicon"] = new LocalizedEntryMetadata(
                    "Grimorio Encuadernado / Libro de los Muertos (EXP-06 • Evil Dead / Hamunaptra)",
                    "Bound Grimoire / Book of the Dead (EXP-06 • Evil Dead / Hamunaptra)",
                    "Códice antiguo con relieves orgánicos y herrajes egipcios de oro. Permanece sellado junto a la Caja Fuerte Nivel Ω en el Ala Noroeste.",
                    "Ancient codex bound with organic ridges and golden Egyptian clasps, sealed beside the Level-Ω Vault Safe in the NW Wing.",
                    new Color(0.95f, 0.68f, 0.24f)),

                ["artifact:mano-ceramica"] = new LocalizedEntryMetadata(
                    "La Mano Embalsamada de Cerámica (EXP-07 • Háblame / Talk to Me)",
                    "Embalmed Ceramic Hand (EXP-07 • Talk to Me)",
                    "Antebrazo de cerámica blanca cubierto de firmas oscuras. Regla: jamás sostenerla por más de 90 segundos.",
                    "White ceramic forearm covered in dark signatures. Rule: never hold it for longer than 90 seconds.",
                    new Color(0.92f, 0.88f, 0.78f)),

                ["artifact:configuracion-lamento"] = new LocalizedEntryMetadata(
                    "Caja Rompecabezas de Filigrana (EXP-08 • La Configuración del Lamento)",
                    "Filigree Puzzle Box (EXP-08 • The Lament Configuration)",
                    "Cubo mecánico de laca negra y latón grabado con anillos concéntricos, bloqueado a mitad de su secuencia.",
                    "Mechanical cube of black lacquer and etched brass locked mid-sequence to prevent opening a rift.",
                    new Color(1.0f, 0.78f, 0.28f)),

                ["artifact:tablero-ouija"] = new LocalizedEntryMetadata(
                    "Tablero Espiritista y Planchette (EXP-09 • El Exorcista / Ouija)",
                    "Spirit Board & Planchette (EXP-09 • The Exorcist / Ouija)",
                    "Tablero clásico de madera grabada con el puntero clavado sobre 'GOODBYE'.",
                    "Classic engraved spirit board with its planchette pinned over 'GOODBYE'.",
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
                [ProgressionIds.CodeBuildingASouthSecretDoor] = new LearnedCodeMetadata(
                    "Código de acceso — Puerta misteriosa (Edificio A Sur)",
                    "Access Code — Mysterious Door (Building A South)",
                    "Combinación de la chapa electrónica que da acceso al cuarto reservado en el costado sur del Edificio A.",
                    "Electronic keypad combination granting entry to the concealed chamber on the south bay of Building A.",
                    ProgressionIds.DoorBuildingASouth),

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
                    "Ingreso al Nivel 1 (Backrooms)",
                    "Entered Backrooms Level 1",
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
            if (string.IsNullOrWhiteSpace(evidenceId))
            {
                metadata = null;
                return false;
            }
            return EvidenceCatalog.TryGetValue(evidenceId.Trim(), out metadata);
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
