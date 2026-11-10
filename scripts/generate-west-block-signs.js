import fs from 'fs';
import path from 'path';
import sharp from 'sharp';

const outDir = path.resolve(process.cwd(), 'unity-client/Assets/Textures/Environment');

async function generateTexture(filename, width, height, svgContent) {
  const fullPath = path.join(outDir, filename);
  await sharp(Buffer.from(svgContent))
    .png()
    .toFile(fullPath);
  console.log(`Generated: ${fullPath}`);
}

async function main() {
  fs.mkdirSync(outDir, { recursive: true });

  // 1. Bay 1 (Far Left): "A VENTA" Glowing Green Storefront Sign
  await generateTexture('T_West_Sign_AVenta.png', 512, 192, `
    <svg width="512" height="192" viewBox="0 0 512 192" xmlns="http://www.w3.org/2000/svg">
      <rect width="512" height="192" fill="#18261e"/>
      <rect x="6" y="6" width="500" height="180" fill="none" stroke="#2e4a38" stroke-width="6"/>
      <text x="256" y="128" font-family="Arial, Helvetica, sans-serif" font-weight="900" font-size="92" fill="#5cff78" stroke="#1f7a34" stroke-width="3" text-anchor="middle" letter-spacing="4px">A VENTA</text>
    </svg>
  `);

  // 2. Bay 2: "Foto Estudio / Internacional" Terracotta Sign
  await generateTexture('T_West_Sign_Internacional.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#6d3522"/>
      <rect x="10" y="10" width="1004" height="236" fill="none" stroke="#4f2314" stroke-width="10"/>
      <rect x="20" y="20" width="984" height="216" fill="none" stroke="#8c4830" stroke-width="4"/>
      <text x="512" y="108" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="60" fill="#e8cfa8" text-anchor="middle" letter-spacing="2px">Curiosidades y Regalos</text>
      <text x="512" y="192" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="68" fill="#f3dfc1" text-anchor="middle" letter-spacing="3px">Internacional</text>
    </svg>
  `);

  // 3. Bay 3: "Farma | PHARMACY" Two-Tone Terracotta & Bright Yellow Sign
  await generateTexture('T_West_Sign_Pharmacy.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <!-- Left Terracotta Section -->
      <rect x="0" y="0" width="330" height="256" fill="#8c2d22"/>
      <rect x="8" y="8" width="314" height="240" fill="none" stroke="#5e1b13" stroke-width="8"/>
      <text x="165" y="160" font-family="'Trebuchet MS', Arial, sans-serif" font-weight="bold" font-size="78" fill="#f8efe6" text-anchor="middle">Farma</text>
      <!-- Right Bright Yellow PHARMACY Box -->
      <rect x="330" y="0" width="694" height="256" fill="#f5c418"/>
      <rect x="338" y="8" width="678" height="240" fill="none" stroke="#c48b08" stroke-width="10"/>
      <text x="677" y="166" font-family="Impact, 'Arial Black', Arial, sans-serif" font-weight="900" font-size="112" fill="#b82318" text-anchor="middle" letter-spacing="3px">PHARMACY</text>
    </svg>
  `);

  // 4. Bay 3 Window Yellow Promotional Poster
  await generateTexture('T_West_Poster_Pharmacy.png', 512, 640, `
    <svg width="512" height="640" viewBox="0 0 512 640" xmlns="http://www.w3.org/2000/svg">
      <rect width="512" height="640" fill="#ebbf1c"/>
      <rect x="14" y="14" width="484" height="612" fill="none" stroke="#b32416" stroke-width="10"/>
      <text x="256" y="115" font-family="Arial, sans-serif" font-weight="900" font-size="68" fill="#b32416" text-anchor="middle">SUPER</text>
      <text x="256" y="195" font-family="Arial, sans-serif" font-weight="900" font-size="64" fill="#1a1a1a" text-anchor="middle">OFERTAS</text>
      <rect x="46" y="235" width="420" height="12" fill="#b32416"/>
      <text x="256" y="325" font-family="Arial, sans-serif" font-weight="bold" font-size="48" fill="#1a1a1a" text-anchor="middle">MEDICAMENTOS</text>
      <text x="256" y="395" font-family="Arial, sans-serif" font-weight="bold" font-size="44" fill="#b32416" text-anchor="middle">DESCUENTO</text>
      <rect x="66" y="445" width="380" height="130" fill="#d9a910"/>
      <text x="256" y="530" font-family="Arial, sans-serif" font-weight="900" font-size="64" fill="#1a1a1a" text-anchor="middle">24 HRS</text>
    </svg>
  `);

  // 5. Bay 4 (Center): "Óptica Fashion" Terracotta Sign
  await generateTexture('T_West_Sign_OpticaFashion.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#763822"/>
      <rect x="10" y="10" width="1004" height="236" fill="none" stroke="#522312" stroke-width="10"/>
      <rect x="20" y="20" width="984" height="216" fill="none" stroke="#91482e" stroke-width="4"/>
      <text x="512" y="160" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="104" fill="#f7f1e6" text-anchor="middle" letter-spacing="2px">Óptica Fashion</text>
    </svg>
  `);

  // 6. Bay 5: "Alimentos / Santa Fe" Terracotta Sign
  await generateTexture('T_West_Sign_SantaFe.png', 768, 256, `
    <svg width="768" height="256" viewBox="0 0 768 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="768" height="256" fill="#7c3a23"/>
      <rect x="8" y="8" width="752" height="240" fill="none" stroke="#562514" stroke-width="8"/>
      <rect x="16" y="16" width="736" height="224" fill="none" stroke="#96492e" stroke-width="3"/>
      <text x="384" y="108" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="64" fill="#f5ede0" text-anchor="middle" letter-spacing="2px">Alimentos</text>
      <text x="384" y="192" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="68" fill="#f5ede0" text-anchor="middle" letter-spacing="2px">Santa Fe</text>
    </svg>
  `);

  // 7. Bay 6: "Bar La Cucaracha" Historic Terracotta Sign
  await generateTexture('T_West_Sign_LaCucaracha.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#873823"/>
      <rect x="10" y="10" width="1004" height="236" fill="none" stroke="#5e2313" stroke-width="10"/>
      <rect x="20" y="20" width="984" height="216" fill="none" stroke="#a34931" stroke-width="4"/>
      <text x="512" y="158" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="96" fill="#f8f2e8" text-anchor="middle" letter-spacing="2px">Bar La Cucaracha</text>
    </svg>
  `);

  // 8. Bay 7: "Taqueria El Trompo" Terracotta Sign
  await generateTexture('T_West_Sign_TaqueriaElTrompo.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#8a3922"/>
      <rect x="10" y="10" width="1004" height="236" fill="none" stroke="#5e2413" stroke-width="10"/>
      <rect x="20" y="20" width="984" height="216" fill="none" stroke="#a6492e" stroke-width="4"/>
      <text x="512" y="158" font-family="Georgia, 'Times New Roman', serif" font-weight="bold" font-size="90" fill="#faf4ea" text-anchor="middle" letter-spacing="1.5px">Taqueria El Trompo</text>
    </svg>
  `);

  // 9. Bay 8 (Far Right Corner): "Dental Juárez" Terracotta Sign
  await generateTexture('T_West_Sign_Dental.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#7c3420"/>
      <rect x="10" y="10" width="1004" height="236" fill="none" stroke="#542011" stroke-width="10"/>
      <rect x="20" y="20" width="984" height="216" fill="none" stroke="#99432b" stroke-width="4"/>
      <text x="512" y="162" font-family="Georgia, 'Palatino Linotype', serif" font-style="italic" font-weight="bold" font-size="104" fill="#f5e6d3" text-anchor="middle" letter-spacing="2px">Dental Juárez</text>
    </svg>
  `);

  // 10. Bay 8 Lower Counter Box under Barred Window
  await generateTexture('T_West_Dental_BaseBox.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#8f3620"/>
      <rect x="10" y="10" width="1004" height="236" fill="none" stroke="#612111" stroke-width="12"/>
      <circle cx="140" cy="128" r="68" fill="#e8b968" stroke="#5c2010" stroke-width="6"/>
      <text x="540" y="115" font-family="Georgia, serif" font-weight="bold" font-size="56" fill="#f5e4c8" text-anchor="middle">CLÍNICA DENTAL</text>
      <text x="540" y="188" font-family="Arial, sans-serif" font-weight="bold" font-size="44" fill="#e8c382" text-anchor="middle">LIMPIEZA • EXTRACCIONES</text>
    </svg>
  `);

  // 11. Semicircular Arched Window (Lunette) for Building A Upper Floor (Transparent outside arch!)
  await generateTexture('T_West_ArchWindow.png', 512, 384, `
    <svg width="512" height="384" viewBox="0 0 512 384" xmlns="http://www.w3.org/2000/svg">
      <!-- Outer Dark Grey Stone Arch Surround (Arquivolta) with transparent background outside -->
      <path d="M 14 380 A 242 242 0 0 1 498 380 Z" fill="#525659" stroke="#3b3e40" stroke-width="6"/>
      <!-- Inner Arch Recess Shadow -->
      <path d="M 56 374 A 200 200 0 0 1 456 374 Z" fill="#21262b" stroke="#15181b" stroke-width="5"/>
      <!-- Subtle Sky/Cloud Reflection in Arched Glass -->
      <path d="M 64 368 A 192 192 0 0 1 448 368 Z" fill="#3a4c5e"/>
      <path d="M 78 364 A 178 178 0 0 1 342 198 L 362 364 Z" fill="#6e8294" opacity="0.38"/>
      <!-- Central Vertical Rectangular Window Casement Frame Inside Arch -->
      <rect x="176" y="132" width="160" height="236" fill="#1e2226" stroke="#111315" stroke-width="10"/>
      <rect x="186" y="142" width="68" height="216" fill="#2b3742"/>
      <rect x="262" y="142" width="64" height="216" fill="#32404d"/>
      <!-- Keystone & Voussoir Lines on Grey Arch Ring -->
      <line x1="256" y1="10" x2="256" y2="54" stroke="#36393b" stroke-width="5"/>
      <line x1="145" y1="45" x2="165" y2="82" stroke="#36393b" stroke-width="4"/>
      <line x1="367" y1="45" x2="347" y2="82" stroke="#36393b" stroke-width="4"/>
      <line x1="60" y1="138" x2="96" y2="160" stroke="#36393b" stroke-width="4"/>
      <line x1="452" y1="138" x2="416" y2="160" stroke="#36393b" stroke-width="4"/>
      <!-- Bottom Stone Sill -->
      <rect x="8" y="358" width="496" height="26" fill="#464a4d" stroke="#2f3234" stroke-width="3"/>
    </svg>
  `);

  // 12. White Corrugated Roll-Up Metal Shutter for "Bar La Cucaracha"
  await generateTexture('T_West_MetalShutter.png', 512, 512, `
    <svg width="512" height="512" viewBox="0 0 512 512" xmlns="http://www.w3.org/2000/svg">
      <rect width="512" height="512" fill="#d8dce0"/>
      <!-- Side Guide Channels -->
      <rect x="0" y="0" width="18" height="512" fill="#8c9298"/>
      <rect x="494" y="0" width="18" height="512" fill="#8c9298"/>
      <!-- Horizontal Corrugated Slats -->
      ${Array.from({ length: 32 }, (_, i) => {
        const y = i * 16;
        return `
          <rect x="18" y="${y}" width="476" height="7" fill="#e6eaee"/>
          <rect x="18" y="${y + 7}" width="476" height="6" fill="#c4c9ce"/>
          <rect x="18" y="${y + 13}" width="476" height="3" fill="#9ea4aa"/>
        `;
      }).join('')}
      <!-- Bottom Heavy Steel Bar -->
      <rect x="14" y="488" width="484" height="24" fill="#7c8288"/>
    </svg>
  `);

  // 13. 2nd-Floor Continuous Multi-Pane Window Ribbon with White Security Grilles (Building B)
  await generateTexture('T_West_RibbonWindows.png', 1024, 256, `
    <svg width="1024" height="256" viewBox="0 0 1024 256" xmlns="http://www.w3.org/2000/svg">
      <rect width="1024" height="256" fill="#3a4046"/>
      ${Array.from({ length: 6 }, (_, i) => {
        const x = 12 + i * 168;
        return `
          <!-- Window Bay ${i} -->
          <rect x="${x}" y="14" width="156" height="228" fill="#1e2d3d" stroke="#cfd4d8" stroke-width="6"/>
          <!-- Upper Transom Divider -->
          <line x1="${x}" y1="76" x2="${x + 156}" y2="76" stroke="#cfd4d8" stroke-width="6"/>
          <!-- Vertical Center Mullion -->
          <line x1="${x + 78}" y1="14" x2="${x + 78}" y2="242" stroke="#cfd4d8" stroke-width="5"/>
          <!-- White Security Grille Bars -->
          ${Array.from({ length: 7 }, (__, j) => {
            const bx = x + 18 + j * 20;
            return `<line x1="${bx}" y1="14" x2="${bx}" y2="242" stroke="#e2e6ea" stroke-width="3"/>`;
          }).join('')}
          <line x1="${x}" y1="158" x2="${x + 156}" y2="158" stroke="#e2e6ea" stroke-width="3"/>
        `;
      }).join('')}
    </svg>
  `);
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});

