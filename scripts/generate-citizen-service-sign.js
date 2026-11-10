import fs from 'fs';
import path from 'path';
import sharp from 'sharp';

async function generateSign() {
  const width = 1024;
  const height = 256;

  const svg = `
<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" xmlns="http://www.w3.org/2000/svg">
  <defs>
    <style>
      .bg { fill: #0d1b2a; }
      .outer-border { fill: none; stroke: #c89b3c; stroke-width: 6; }
      .inner-border { fill: none; stroke: #0098a6; stroke-width: 6; }
      .divider { stroke: #3b5b7e; stroke-width: 2; }
      .main-title { font-family: 'Arial', 'Helvetica', sans-serif; font-weight: 900; font-size: 42px; fill: #ffffff; letter-spacing: 2px; text-anchor: middle; }
      .sub-title { font-family: 'Arial', 'Helvetica', sans-serif; font-weight: 800; font-size: 32px; fill: #66c0f4; letter-spacing: 2px; text-anchor: middle; }
      .location { font-family: 'Arial', 'Helvetica', sans-serif; font-weight: 500; font-size: 20px; fill: #d0d8e0; letter-spacing: 2px; text-anchor: middle; }
      .footer-badge { font-family: 'Arial', 'Helvetica', sans-serif; font-weight: 800; font-size: 17px; fill: #e5a93c; letter-spacing: 2px; text-anchor: middle; }
    </style>
  </defs>

  <!-- Background -->
  <rect x="0" y="0" width="${width}" height="${height}" class="bg" />

  <!-- Outer gold border -->
  <rect x="10" y="10" width="${width - 20}" height="${height - 20}" class="outer-border" />

  <!-- Inner cyan border -->
  <rect x="22" y="22" width="${width - 44}" height="${height - 44}" class="inner-border" />

  <!-- Line 1: Main Spanish Title -->
  <text x="512" y="78" class="main-title">ENLACE DE ATENCION CIUDADANA</text>

  <!-- Divider line -->
  <line x1="45" y1="100" x2="979" y2="100" class="divider" />

  <!-- Line 2: English Subtitle -->
  <text x="512" y="144" class="sub-title">CITIZEN SERVICE DESK * MUNICIPAL LIAISON</text>

  <!-- Line 3: Location / Municipal Context -->
  <text x="512" y="182" class="location">AVENIDA BENITO JUAREZ * SERVICIO MUNICIPAL SIMULADO</text>

  <!-- Line 4: Service description badge -->
  <text x="512" y="218" class="footer-badge">ORIENTACION CIUDADANA * CANALIZACION DE REPORTES E INCIDENCIAS</text>
</svg>
`;

  const outputPath = path.resolve(process.cwd(), 'unity-client/Assets/Textures/Environment/T_CitizenService_FasciaSign.png');
  await sharp(Buffer.from(svg))
    .png()
    .toFile(outputPath);

  console.log(`Generated sign texture at: ${outputPath}`);
}

generateSign().catch(err => {
  console.error(err);
  process.exit(1);
});
