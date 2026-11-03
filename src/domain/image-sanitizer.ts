import sharp, { Metadata } from 'sharp';
import { createHash } from 'node:crypto';
import { MAX_IMAGE_SIZE_BYTES } from './citizen-report.js';

export class ImageSanitizationError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'ImageSanitizationError';
  }
}

export interface SanitizedImage {
  readonly buffer: Buffer;
  readonly mimeType: 'image/jpeg' | 'image/png';
  readonly format: 'jpeg' | 'png';
  readonly width: number;
  readonly height: number;
  readonly base64: string;
  readonly dataUrl: string;
  readonly sizeBytes: number;
  readonly sha256: string;
}

// Security ceiling: 25 megapixels (e.g. 5000x5000) to protect against decompression bombs
export const MAX_PIXEL_COUNT = 25_000_000;

/**
 * Sanitizes and normalizes photographic evidence:
 * 1. Validates file size (<= 5MB) and authentic image magic bytes.
 * 2. Rejects corrupt or truncated images by performing a full decode.
 * 3. Enforces a maximum pixel count to prevent decompression bomb attacks.
 * 4. Respects and applies EXIF orientation via .rotate(), correcting camera rotation.
 * 5. Strips ALL EXIF, GPS, camera metadata, and personal tags by re-encoding clean pixels.
 * 6. Returns clean normalized JPEG bytes and dimension metadata.
 */
export async function sanitizeAndNormalizeImage(
  rawBuffer: Buffer,
  declaredMimeType?: string,
  _fileName?: string
): Promise<SanitizedImage> {
  if (!rawBuffer || rawBuffer.length === 0) {
    throw new ImageSanitizationError('Image file is empty (0 bytes).');
  }

  if (rawBuffer.length > MAX_IMAGE_SIZE_BYTES) {
    throw new ImageSanitizationError(
      `Image exceeds maximum allowed size of 5 MB (received ${(rawBuffer.length / (1024 * 1024)).toFixed(2)} MB).`
    );
  }

  // Magic bytes inspection
  const isPng =
    rawBuffer.length >= 8 &&
    rawBuffer[0] === 0x89 &&
    rawBuffer[1] === 0x50 &&
    rawBuffer[2] === 0x4e &&
    rawBuffer[3] === 0x47 &&
    rawBuffer[4] === 0x0d &&
    rawBuffer[5] === 0x0a &&
    rawBuffer[6] === 0x1a &&
    rawBuffer[7] === 0x0a;

  const isJpeg =
    rawBuffer.length >= 3 &&
    rawBuffer[0] === 0xff &&
    rawBuffer[1] === 0xd8 &&
    rawBuffer[2] === 0xff;

  if (!isPng && !isJpeg) {
    throw new ImageSanitizationError(
      'Unsupported image format or corrupted file. Only valid PNG and JPEG images are accepted.'
    );
  }

  const expectedMime = isPng ? 'image/png' : 'image/jpeg';
  if (declaredMimeType && declaredMimeType.toLowerCase() !== expectedMime) {
    throw new ImageSanitizationError(
      `Declared MIME type '${declaredMimeType}' does not match verified file format '${isPng ? 'png' : 'jpeg'}'.`
    );
  }

  let metadata: Metadata;
  try {
    const probe = sharp(rawBuffer);
    metadata = await probe.metadata();
  } catch (err) {
    throw new ImageSanitizationError(
      `Failed to decode image data: ${err instanceof Error ? err.message : String(err)}`
    );
  }

  const width = metadata.width ?? 0;
  const height = metadata.height ?? 0;

  if (width <= 0 || height <= 0) {
    throw new ImageSanitizationError('Invalid image dimensions (width or height is zero).');
  }

  if (width * height > MAX_PIXEL_COUNT) {
    throw new ImageSanitizationError(
      `Image resolution (${width}x${height} = ${(width * height) / 1_000_000} MP) exceeds safe limit of 25 MP.`
    );
  }

  // Full re-encoding with sharp:
  // .rotate() without parameters auto-rotates based on EXIF orientation tag.
  // Re-encoding without .withMetadata() completely discards all EXIF, GPS, and camera metadata tags.
  let sanitizedBuffer: Buffer;
  let finalMime: 'image/jpeg' | 'image/png';
  let finalFormat: 'jpeg' | 'png';

  try {
    if (isPng) {
      sanitizedBuffer = await sharp(rawBuffer)
        .rotate()
        .png({ compressionLevel: 8 })
        .toBuffer();
      finalMime = 'image/png';
      finalFormat = 'png';
    } else {
      sanitizedBuffer = await sharp(rawBuffer)
        .rotate()
        .jpeg({ quality: 85, progressive: false })
        .toBuffer();
      finalMime = 'image/jpeg';
      finalFormat = 'jpeg';
    }
  } catch (err) {
    throw new ImageSanitizationError(
      `Image re-encoding and metadata sanitization failed: ${err instanceof Error ? err.message : String(err)}`
    );
  }

  // Get final dimensions post-rotation
  const finalMeta = await sharp(sanitizedBuffer).metadata();
  const finalWidth = finalMeta.width ?? width;
  const finalHeight = finalMeta.height ?? height;

  const sha256 = createHash('sha256').update(sanitizedBuffer).digest('hex');
  const base64 = sanitizedBuffer.toString('base64');
  const dataUrl = `data:${finalMime};base64,${base64}`;

  return {
    buffer: sanitizedBuffer,
    mimeType: finalMime,
    format: finalFormat,
    width: finalWidth,
    height: finalHeight,
    base64,
    dataUrl,
    sizeBytes: sanitizedBuffer.length,
    sha256,
  };
}
