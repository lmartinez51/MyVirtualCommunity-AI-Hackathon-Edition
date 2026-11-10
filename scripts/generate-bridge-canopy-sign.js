import fs from 'fs';
import path from 'path';
import sharp from 'sharp';

async function generateBridgeSign() {
  const width = 2048;
  const height = 256;

  // Authentic Republic of Mexico map silhouette path (normalized to 120 x 85 box)
  // Baja California:
  // Starts NW (12, 10), runs SE to Cabo (32, 54), inner coast (27, 44) to (18, 14)
  // Mainland:
  // Sonora/US border (22, 10) to Ciudad Juarez (46, 12), Rio Grande (54, 22), Tamaulipas (68, 32)
  // Pacific coast: Sonora (32, 18), Sinaloa (40, 32), Jalisco (47, 48), Oaxaca (64, 58), Chiapas (76, 62)
  // Gulf coast: Veracruz (66, 44), Tabasco (74, 52), Yucatan Peninsula (82, 38) to (94, 40) to (90, 52) to (78, 56)
  const mexicoPath = `
    M 14,8 L 19,16 L 24,28 L 29,42 L 33,54 L 30,53 L 26,40 L 21,26 L 16,14 Z
    M 21,8 L 32,9 L 45,11 L 49,15 L 56,22 L 62,28 L 68,33
    L 66,40 L 64,46 L 68,50 L 74,52
    L 77,46 L 81,38 L 89,39 L 94,43 L 92,51 L 86,52 L 80,56
    L 76,62 L 72,61 L 64,58 L 56,56 L 49,52 L 44,48 L 40,40
    L 37,32 L 32,22 L 27,15 Z
  `;

  // Left Emblem positioned at center X = 130, Y = 128
  // Right Emblem positioned at center X = 1918, Y = 128
  // "PC-51" positioned at X = 270 (text-anchor: start)
  // "PUENTE INTERNACIONAL PASO DEL NORTE" positioned at X = 1110 (text-anchor: middle)

  const svg = `
<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <style>
      .bg { fill: #f4f3ef; }
      .text-primary {
        font-family: 'Arial Black', 'Impact', 'Trebuchet MS', 'Segoe UI Black', sans-serif;
        font-weight: 900;
        fill: #26221f;
      }
      .emblem-fill {
        fill: #26221f;
      }
      .bracket {
        fill: #26221f;
      }
    </style>

    <!-- Reusable Mexican Republic Emblem with Top and Bottom Chevron Brackets -->
    <g id="mexico-emblem">
      <!-- Upper bracket bar with angled chevron outer tip -->
      <polygon points="0,0 120,0 135,12 15,12" class="bracket" />
      
      <!-- Mexican Republic Map Silhouette in Center (scaled 1.25x) -->
      <g transform="translate(18, 20) scale(1.15)">
        <path d="${mexicoPath}" class="emblem-fill" fill-rule="evenodd" />
      </g>
      
      <!-- Lower bracket bar with angled chevron outer tip -->
      <polygon points="0,105 120,105 135,93 15,93" class="bracket" />
    </g>

    <g id="mexico-emblem-right">
      <!-- Upper bracket bar mirrored -->
      <polygon points="135,0 15,0 0,12 120,12" class="bracket" />
      
      <!-- Mexican Republic Map Silhouette in Center -->
      <g transform="translate(18, 20) scale(1.15)">
        <path d="${mexicoPath}" class="emblem-fill" fill-rule="evenodd" />
      </g>
      
      <!-- Lower bracket bar mirrored -->
      <polygon points="135,105 15,105 0,93 120,93" class="bracket" />
    </g>
  </defs>

  <!-- Concrete Signboard Background (matching bridge fascia) -->
  <rect x="0" y="0" width="${width}" height="${height}" class="bg" />

  <!-- LEFT EMBLEM: Map of Mexico with chevron brackets (X: 55 to 190) -->
  <use href="#mexico-emblem" x="55" y="75" />

  <!-- PC-51: Clear, well-spaced code (X ~ 255 to 400) -->
  <text x="315" y="145" class="text-primary" font-size="52" letter-spacing="4px" text-anchor="middle">PC-51</text>

  <!-- MAIN BRIDGE TITLE: Centered in the middle expanse (X ~ 430 to 1800) -->
  <text x="1115" y="145" class="text-primary" font-size="47" letter-spacing="3.5px" text-anchor="middle">PUENTE INTERNACIONAL PASO DEL NORTE</text>

  <!-- RIGHT EMBLEM: Map of Mexico with chevron brackets (X: 1855 to 1990) -->
  <use href="#mexico-emblem-right" x="1855" y="75" />
</svg>
`;

  const outputPath = path.resolve(process.cwd(), 'unity-client/Assets/Textures/Environment/T_Bridge_Canopy_Sign.png');
  await sharp(Buffer.from(svg))
    .png()
    .toFile(outputPath);

  console.log(`Generated clean bridge sign texture at: ${outputPath}`);
}

generateBridgeSign().catch(err => {
  console.error(err);
  process.exit(1);
});

