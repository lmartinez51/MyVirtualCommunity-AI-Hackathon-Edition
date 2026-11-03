import sharp from 'sharp';
import { decodeMaskToRaster, SegmentationMask } from '@meta-sam/parser';
import { BoundingBox, SamDetectedObject } from './citizen-report.js';

export interface MaskOverlayPaletteItem {
  readonly hex: string;
  readonly r: number;
  readonly g: number;
  readonly b: number;
  readonly fillAlpha: number;
  readonly edgeAlpha: number;
}

export const SAM_OVERLAY_PALETTE: readonly MaskOverlayPaletteItem[] = [
  { hex: '#00E5FF', r: 0, g: 229, b: 255, fillAlpha: 110, edgeAlpha: 240 },   // Cyan
  { hex: '#FF9100', r: 255, g: 145, b: 0, fillAlpha: 110, edgeAlpha: 240 },   // Amber
  { hex: '#76FF03', r: 118, g: 255, b: 3, fillAlpha: 110, edgeAlpha: 240 },   // Lime
  { hex: '#E040FB', r: 224, g: 64, b: 251, fillAlpha: 110, edgeAlpha: 240 }, // Magenta
  { hex: '#FFEA00', r: 255, g: 234, b: 0, fillAlpha: 110, edgeAlpha: 240 },   // Yellow
];

export interface MaskRecordInput {
  readonly objectId: string;
  readonly mask?: SegmentationMask;
  readonly raster?: Uint8Array;
  readonly bounds: {
    readonly left: number;
    readonly top: number;
    readonly right: number;
    readonly bottom: number;
  };
  readonly maskWidth?: number;
  readonly maskHeight?: number;
  readonly confidence?: number;
  readonly label?: string;
}

export interface BoxRecordInput {
  readonly objectId?: string;
  readonly left: number;
  readonly top: number;
  readonly right: number;
  readonly bottom: number;
  readonly confidence?: number;
  readonly label?: string;
}

export interface CompositeMaskResult {
  readonly overlayPngBase64: string | null;
  readonly objects: readonly SamDetectedObject[];
  readonly hasAuthenticMasks: boolean;
}

/**
 * Composites authentic decoded SAM 3.1 mask rasters into a transparent PNG overlay.
 * Each detected object is mapped to a distinct color from SAM_OVERLAY_PALETTE
 * with clear border enhancement and preserved source dimensions.
 */
export async function compositeSamMaskOverlay(
  imageWidth: number,
  imageHeight: number,
  masks: readonly MaskRecordInput[],
  boxes: readonly BoxRecordInput[] = [],
  defaultLabel: string = 'detected_region'
): Promise<CompositeMaskResult> {
  if (imageWidth <= 0 || imageHeight <= 0) {
    return {
      overlayPngBase64: null,
      objects: [],
      hasAuthenticMasks: false,
    };
  }

  // If no masks exist, return empty overlay
  if (!masks || masks.length === 0) {
    // If only boxes exist, construct box-only objects without masks
    const boxObjects: SamDetectedObject[] = boxes.map((b, idx) => {
      const palette = SAM_OVERLAY_PALETTE[idx % SAM_OVERLAY_PALETTE.length] ?? SAM_OVERLAY_PALETTE[0]!;
      const xmin = imageWidth > 0 ? b.left / imageWidth : 0;
      const xmax = imageWidth > 0 ? b.right / imageWidth : 0;
      const ymin = imageHeight > 0 ? b.top / imageHeight : 0;
      const ymax = imageHeight > 0 ? b.bottom / imageHeight : 0;
      return {
        objectId: b.objectId ?? `box_${idx}`,
        label: b.label ?? defaultLabel,
        confidence: b.confidence,
        box: {
          ymin,
          xmin,
          ymax,
          xmax,
          label: b.label ?? defaultLabel,
          confidence: b.confidence,
        },
        colorHex: palette.hex,
      };
    });

    return {
      overlayPngBase64: null,
      objects: boxObjects,
      hasAuthenticMasks: false,
    };
  }

  // RGBA buffer initialized to 0 (100% transparent)
  const rgbaBuffer = Buffer.alloc(imageWidth * imageHeight * 4, 0);
  const detectedObjects: SamDetectedObject[] = [];
  let validMasksCount = 0;

  for (let mIdx = 0; mIdx < masks.length; mIdx++) {
    const m = masks[mIdx];
    if (!m) continue;

    const palette = SAM_OVERLAY_PALETTE[mIdx % SAM_OVERLAY_PALETTE.length] ?? SAM_OVERLAY_PALETTE[0]!;

    let raster: Uint8Array | undefined = m.raster;
    let maskW = m.maskWidth ?? (m.mask ? m.mask.width : m.bounds.right - m.bounds.left);
    let maskH = m.maskHeight ?? (m.mask ? m.mask.height : m.bounds.bottom - m.bounds.top);

    if (!raster && m.mask) {
      try {
        raster = decodeMaskToRaster(m.mask);
        maskW = m.mask.width;
        maskH = m.mask.height;
      } catch {
        continue;
      }
    }

    if (!raster || raster.length === 0) {
      continue;
    }

    validMasksCount++;
    const bounds = m.bounds;

    // Plot pixels into RGBA buffer
    for (let r = 0; r < maskH; r++) {
      for (let c = 0; c < maskW; c++) {
        const rasterIdx = r * maskW + c;
        if (raster[rasterIdx] === 1) {
          const imgX = bounds.left + c;
          const imgY = bounds.top + r;

          if (imgX >= 0 && imgX < imageWidth && imgY >= 0 && imgY < imageHeight) {
            // Check 4-neighbor boundary for crisp edge enhancement
            const isEdge =
              r === 0 ||
              r === maskH - 1 ||
              c === 0 ||
              c === maskW - 1 ||
              raster[(r - 1) * maskW + c] === 0 ||
              raster[(r + 1) * maskW + c] === 0 ||
              raster[r * maskW + (c - 1)] === 0 ||
              raster[r * maskW + (c + 1)] === 0;

            const pixelOffset = (imgY * imageWidth + imgX) * 4;
            rgbaBuffer[pixelOffset] = palette.r;
            rgbaBuffer[pixelOffset + 1] = palette.g;
            rgbaBuffer[pixelOffset + 2] = palette.b;
            rgbaBuffer[pixelOffset + 3] = isEdge ? palette.edgeAlpha : palette.fillAlpha;
          }
        }
      }
    }

    // Normalized bounding box
    const xmin = imageWidth > 0 ? bounds.left / imageWidth : 0;
    const xmax = imageWidth > 0 ? bounds.right / imageWidth : 0;
    const ymin = imageHeight > 0 ? bounds.top / imageHeight : 0;
    const ymax = imageHeight > 0 ? bounds.bottom / imageHeight : 0;

    const bBox: BoundingBox = {
      ymin: Math.max(0, Math.min(1, ymin)),
      xmin: Math.max(0, Math.min(1, xmin)),
      ymax: Math.max(0, Math.min(1, ymax)),
      xmax: Math.max(0, Math.min(1, xmax)),
      label: m.label ?? defaultLabel,
      confidence: m.confidence,
    };

    detectedObjects.push({
      objectId: m.objectId,
      label: m.label ?? defaultLabel,
      confidence: m.confidence,
      box: bBox,
      maskBounds: bounds,
      maskDimensions: { width: maskW, height: maskH },
      colorHex: palette.hex,
    });
  }

  if (validMasksCount === 0) {
    return {
      overlayPngBase64: null,
      objects: [],
      hasAuthenticMasks: false,
    };
  }

  // Encode to PNG buffer using sharp
  const pngBuffer = await sharp(rgbaBuffer, {
    raw: {
      width: imageWidth,
      height: imageHeight,
      channels: 4,
    },
  })
    .png({ compressionLevel: 9 })
    .toBuffer();

  return {
    overlayPngBase64: pngBuffer.toString('base64'),
    objects: detectedObjects,
    hasAuthenticMasks: true,
  };
}
