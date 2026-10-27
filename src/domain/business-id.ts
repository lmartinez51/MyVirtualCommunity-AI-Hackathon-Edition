/**
 * Strongly typed Business Identifier.
 *
 * Implements a branded string type to prevent stringly-typed identifier confusion
 * across domain boundaries.
 *
 * Format rule:
 * Must be in the format `<category>:<slug>`, e.g. `restaurant:wing-spot` or `store:pasteleria-tia-lupe`.
 * - Category: 2-32 lowercase alphanumeric characters or hyphens.
 * - Slug: 2-64 lowercase alphanumeric characters or hyphens.
 */

declare const BusinessIdBrand: unique symbol;

export type BusinessId = string & { readonly [BusinessIdBrand]: true };

const BUSINESS_ID_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*:[a-z0-9]+(?:-[a-z0-9]+)*$/;

export class InvalidBusinessIdError extends Error {
  constructor(message: string) {
    super(message);
    this.name = 'InvalidBusinessIdError';
  }
}

/**
 * Validates and casts a raw string into a branded BusinessId.
 * Throws InvalidBusinessIdError if the format is invalid.
 */
export function createBusinessId(raw: string): BusinessId {
  if (typeof raw !== 'string') {
    throw new InvalidBusinessIdError(
      `Business ID must be a string, received ${typeof raw}`
    );
  }

  const trimmed = raw.trim();
  if (trimmed.length === 0) {
    throw new InvalidBusinessIdError('Business ID cannot be empty or whitespace');
  }

  if (trimmed.length < 5 || trimmed.length > 100) {
    throw new InvalidBusinessIdError(
      `Business ID length must be between 5 and 100 characters (received ${trimmed.length})`
    );
  }

  if (!BUSINESS_ID_PATTERN.test(trimmed)) {
    throw new InvalidBusinessIdError(
      `Invalid Business ID format "${trimmed}". Expected "<category>:<slug>" in lowercase kebab-case (e.g. "restaurant:wing-spot")`
    );
  }

  return trimmed as BusinessId;
}

/**
 * Safe parser that returns a Result object without throwing.
 */
export function parseBusinessId(
  raw: unknown
): { success: true; data: BusinessId } | { success: false; error: string } {
  try {
    if (typeof raw !== 'string') {
      return { success: false, error: 'Business ID must be a string' };
    }
    const businessId = createBusinessId(raw);
    return { success: true, data: businessId };
  } catch (err) {
    return {
      success: false,
      error: err instanceof Error ? err.message : 'Unknown validation error',
    };
  }
}
