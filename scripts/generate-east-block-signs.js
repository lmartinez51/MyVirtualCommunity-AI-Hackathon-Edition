import fs from 'fs';
import path from 'path';
import sharp from 'sharp';

const outDir = path.resolve(process.cwd(), 'unity-client/Assets/Textures/Environment');

async function generateSign(filename, width, height, svgContent) {
  const fullPath = path.join(outDir, filename);
  await sharp(Buffer.from(svgContent))
    .png()
    .toFile(fullPath);
  console.log(`Generated: ${fullPath}`);
}

async function main() {
  fs.mkdirSync(outDir, { recursive: true });

  // 1. Curiosidades / Casa de Cambio Sign (Far Left)
  await generateSign('T_East_Sign_Curiosidades.png', 512, 256, `
    <svg width="512" height="256" viewBox="0 0 512 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="512" height="256" fill="#733926"/>
      <rect x="8" y="8" width="496" height="240" fill="none" stroke="#592919" stroke-width="8"/>
      <rect x="16" y="16" width="480" height="224" fill="none" stroke="#8c4932" stroke-width="3"/>
      <text x="256" y="112" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="52" fill="#f2ece4" text-anchor="middle" letter-spacing="1px">Curiosidades</text>
      <text x="256" y="182" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="48" fill="#ebd9c8" text-anchor="middle" letter-spacing="2px">Azteca</text>
    </svg>
  `);

  // 2. Minerva Sign (Center-Left Passage)
  await generateSign('T_East_Sign_Minerva.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#7c3821"/>
      <rect x="10" y="10" width="1004" height="236" fill="none" stroke="#5a2614" stroke-width="10"/>
      <rect x="20" y="20" width="984" height="216" fill="none" stroke="#96472c" stroke-width="4"/>
      <text x="512" y="162" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="118" fill="#f7f3eb" text-anchor="middle" letter-spacing="4px">Minerva</text>
    </svg>
  `);

  // 3. Comercial Uno Sign (Right of Minerva)
  await generateSign('T_East_Sign_ComercialUno.png', 512, 256, `
    <svg width="512" height="256" viewBox="0 0 512 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="512" height="256" fill="#7a3720"/>
      <rect x="8" y="8" width="496" height="240" fill="none" stroke="#582513" stroke-width="8"/>
      <rect x="16" y="16" width="480" height="224" fill="none" stroke="#91452b" stroke-width="3"/>
      <text x="256" y="110" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="60" fill="#f5efe6" text-anchor="middle" letter-spacing="1px">Comercial</text>
      <text x="256" y="188" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="64" fill="#f5efe6" text-anchor="middle" letter-spacing="2px">Uno</text>
    </svg>
  `);

  // 4. Farmacias Benavides Sign (Center Main)
  await generateSign('T_East_Sign_Benavides.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#823821"/>
      <rect x="10" y="10" width="1004" height="236" fill="none" stroke="#5e2614" stroke-width="10"/>
      <rect x="20" y="20" width="984" height="216" fill="none" stroke="#9c482e" stroke-width="4"/>
      <text x="512" y="152" font-family="'Trebuchet MS', 'Arial Rounded MT Bold', Arial, sans-serif" font-weight="bold" font-size="82" fill="#f8f4ed" text-anchor="middle" letter-spacing="2px">farmacias benavides</text>
    </svg>
  `);

  // 5. Consultorio Sign (Right of Farmacias Benavides)
  await generateSign('T_East_Sign_Consultorio.png', 512, 256, `
    <svg width="512" height="256" viewBox="0 0 512 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="512" height="256" fill="#7d3620"/>
      <rect x="8" y="8" width="496" height="240" fill="none" stroke="#592413" stroke-width="8"/>
      <rect x="16" y="16" width="480" height="224" fill="none" stroke="#96442b" stroke-width="3"/>
      <text x="256" y="148" font-family="'Trebuchet MS', Arial, sans-serif" font-weight="bold" font-size="62" fill="#f5f0e6" text-anchor="middle" letter-spacing="1.5px">Consultorio</text>
    </svg>
  `);

  // 6. La Michoacana Sign (Center-Right)
  await generateSign('T_East_Sign_LaMichoacana.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#7e3720"/>
      <rect x="10" y="10" width="1004" height="236" fill="none" stroke="#5b2513" stroke-width="10"/>
      <rect x="20" y="20" width="984" height="216" fill="none" stroke="#98452a" stroke-width="4"/>
      <text x="512" y="158" font-family="Georgia, 'Palatino Linotype', serif" font-style="italic" font-weight="bold" font-size="102" fill="#faf6f0" text-anchor="middle" letter-spacing="2px">La Michoacana</text>
    </svg>
  `);

  // 7. Atención Dental / Dentistas Sign (Far Right Upper Floor)
  await generateSign('T_East_Sign_Dentistas.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#a83824"/>
      <rect x="10" y="10" width="1004" height="236" fill="none" stroke="#7d2515" stroke-width="10"/>
      <rect x="18" y="18" width="988" height="220" fill="none" stroke="#c44933" stroke-width="4"/>
      <text x="512" y="106" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="66" fill="#fcf8f2" text-anchor="middle" letter-spacing="2px">Atención Dental</text>
      <text x="512" y="196" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="76" fill="#fcf8f2" text-anchor="middle" letter-spacing="3px">Dentistas</text>
    </svg>
  `);

  // 8. Medical / Pharmacy Wall Posters (Mounted on right dark panel of Farmacias Benavides)
  await generateSign('T_East_Benavides_Posters.png', 512, 384, `
    <svg width="512" height="384" viewBox="0 0 512 384" xmlns="http://www.w3.org/2000/svg">
      <!-- Dark Slate Blue-Grey Wall Background -->
      <rect width="512" height="384" fill="#232c36"/>
      <!-- Left Poster -->
      <rect x="28" y="32" width="205" height="310" fill="#f0f4f7" stroke="#c0c8d0" stroke-width="4"/>
      <rect x="28" y="32" width="205" height="54" fill="#b8332a"/>
      <text x="130" y="68" font-family="Arial, sans-serif" font-weight="bold" font-size="24" fill="#ffffff" text-anchor="middle">CONSULTORIO</text>
      <rect x="46" y="105" width="170" height="18" fill="#1e4d7b"/>
      <rect x="46" y="138" width="150" height="12" fill="#4a5a6a"/>
      <rect x="46" y="162" width="160" height="12" fill="#4a5a6a"/>
      <rect x="46" y="186" width="135" height="12" fill="#4a5a6a"/>
      <rect x="46" y="220" width="170" height="95" fill="#d4e6f1"/>
      <!-- Right Framed Notice -->
      <rect x="268" y="48" width="212" height="245" fill="#e8eef2" stroke="#8a96a3" stroke-width="6"/>
      <rect x="286" y="70" width="176" height="30" fill="#28598a"/>
      <text x="374" y="92" font-family="Arial, sans-serif" font-weight="bold" font-size="18" fill="#ffffff" text-anchor="middle">SERVICIO MÉDICO</text>
      <rect x="292" y="116" width="164" height="10" fill="#5a6b7c"/>
      <rect x="292" y="138" width="150" height="10" fill="#5a6b7c"/>
      <rect x="292" y="160" width="160" height="10" fill="#5a6b7c"/>
      <rect x="292" y="195" width="164" height="72" fill="#c8d8e4"/>
    </svg>
  `);
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});

