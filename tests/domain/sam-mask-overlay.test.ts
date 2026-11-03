import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import sharp from 'sharp';
import {
  compositeSamMaskOverlay,
  SAM_OVERLAY_PALETTE,
} from '../../src/domain/sam-mask-overlay.js';

describe('SAM 3.1 Mask Overlay & Visual Inspection (Increment 5.11.4)', () => {
  it('1. composites 2 authentic masks with distinct palette colors into a transparent PNG overlay', async () => {
    const imgWidth = 400;
    const imgHeight = 300;

    // Mask 1: 40x30 oval at [left: 40, top: 50, right: 80, bottom: 80]
    const w1 = 40, h1 = 30;
    const r1 = new Uint8Array(w1 * h1);
    for (let y = 0; y < h1; y++) {
      for (let x = 0; x < w1; x++) {
        if (((x - w1 / 2) / (w1 / 2)) ** 2 + ((y - h1 / 2) / (h1 / 2)) ** 2 <= 0.8) {
          r1[y * w1 + x] = 1;
        }
      }
    }

    // Mask 2: 50x40 oval at [left: 200, top: 100, right: 250, bottom: 140]
    const w2 = 50, h2 = 40;
    const r2 = new Uint8Array(w2 * h2);
    for (let y = 0; y < h2; y++) {
      for (let x = 0; x < w2; x++) {
        if (((x - w2 / 2) / (w2 / 2)) ** 2 + ((y - h2 / 2) / (h2 / 2)) ** 2 <= 0.8) {
          r2[y * w2 + x] = 1;
        }
      }
    }

    const res = await compositeSamMaskOverlay(
      imgWidth,
      imgHeight,
      [
        {
          objectId: 'bag_001',
          raster: r1,
          maskWidth: w1,
          maskHeight: h1,
          bounds: { left: 40, top: 50, right: 80, bottom: 80 },
          confidence: 0.95,
          label: 'bolsa_basura',
        },
        {
          objectId: 'bag_002',
          raster: r2,
          maskWidth: w2,
          maskHeight: h2,
          bounds: { left: 200, top: 100, right: 250, bottom: 140 },
          confidence: 0.89,
          label: 'bolsa_basura',
        },
      ],
      [],
      'bolsa_basura'
    );

    assert.equal(res.hasAuthenticMasks, true);
    assert.ok(res.overlayPngBase64 !== null);
    assert.equal(res.objects.length, 2);

    // Object 1 assertions
    const obj1 = res.objects[0]!;
    assert.equal(obj1.objectId, 'bag_001');
    assert.equal(obj1.confidence, 0.95);
    assert.equal(obj1.colorHex, SAM_OVERLAY_PALETTE[0]!.hex);
    assert.equal(obj1.box.xmin, 40 / 400);
    assert.equal(obj1.box.ymin, 50 / 300);
    assert.equal(obj1.box.xmax, 80 / 400);
    assert.equal(obj1.box.ymax, 80 / 300);

    // Object 2 assertions
    const obj2 = res.objects[1]!;
    assert.equal(obj2.objectId, 'bag_002');
    assert.equal(obj2.confidence, 0.89);
    assert.equal(obj2.colorHex, SAM_OVERLAY_PALETTE[1]!.hex);
    assert.notEqual(obj1.colorHex, obj2.colorHex, 'Objects must have distinguishable palette colors');

    // Verify PNG dimensions and transparency
    const pngBuf = Buffer.from(res.overlayPngBase64!, 'base64');
    const metadata = await sharp(pngBuf).metadata();
    assert.equal(metadata.width, imgWidth);
    assert.equal(metadata.height, imgHeight);
    assert.equal(metadata.channels, 4); // RGBA
    assert.equal(metadata.format, 'png');
  });

  it('2. handles boxes-only input without masks by returning hasAuthenticMasks false and null overlay', async () => {
    const res = await compositeSamMaskOverlay(
      640,
      480,
      [], // No masks
      [
        {
          objectId: 'box_0',
          left: 100,
          top: 150,
          right: 300,
          bottom: 350,
          confidence: 0.82,
          label: 'bache',
        },
      ],
      'bache'
    );

    assert.equal(res.hasAuthenticMasks, false);
    assert.equal(res.overlayPngBase64, null);
    assert.equal(res.objects.length, 1);
    assert.equal(res.objects[0]!.box.xmin, 100 / 640);
    assert.equal(res.objects[0]!.box.ymin, 150 / 480);
  });

  it('3. handles zero matches cleanly without fabricating graphics', async () => {
    const res = await compositeSamMaskOverlay(640, 480, [], [], 'pothole');
    assert.equal(res.hasAuthenticMasks, false);
    assert.equal(res.overlayPngBase64, null);
    assert.equal(res.objects.length, 0);
  });

  it('4. handles invalid/empty dimensions safely', async () => {
    const res = await compositeSamMaskOverlay(0, 0, [], [], 'pothole');
    assert.equal(res.hasAuthenticMasks, false);
    assert.equal(res.overlayPngBase64, null);
    assert.equal(res.objects.length, 0);
  });

  it('5. preserves pixel alignment and clamps bounds within image extents', async () => {
    const imgWidth = 200;
    const imgHeight = 100;

    // Mask partially out of bounds: left is 180, right is 220 (width 40, 20 pixels beyond boundary)
    const w = 40, h = 20;
    const raster = new Uint8Array(w * h).fill(1);

    const res = await compositeSamMaskOverlay(
      imgWidth,
      imgHeight,
      [
        {
          objectId: 'clamped_obj',
          raster,
          maskWidth: w,
          maskHeight: h,
          bounds: { left: 180, top: 10, right: 220, bottom: 30 },
          confidence: 0.75,
        },
      ],
      []
    );

    assert.equal(res.hasAuthenticMasks, true);
    assert.ok(res.overlayPngBase64 !== null);
    assert.equal(res.objects[0]!.box.xmax, 1.0, 'Bounding box xmax should be clamped to 1.0');
  });
});

