import assert from 'node:assert/strict';
import { describe, it } from 'node:test';
import {
  createBusinessId,
  InvalidBusinessIdError,
  parseBusinessId,
} from '../../src/domain/business-id.js';

describe('BusinessId Domain Contract', () => {
  it('accepts valid category:slug formatted identifiers', () => {
    const validIds = [
      'restaurant:wing-spot',
      'restaurant:pasteleria-tia-lupe',
      'store:convenience-express',
      'cafe:central-perk-1',
      'shop:artisan-crafts-juarez',
    ];

    for (const raw of validIds) {
      const id = createBusinessId(raw);
      assert.equal(id, raw);

      const parsed = parseBusinessId(raw);
      assert.equal(parsed.success, true);
      if (parsed.success) {
        assert.equal(parsed.data, raw);
      }
    }
  });

  it('rejects empty, blank or whitespace-only strings', () => {
    const invalidInputs = ['', '   ', '\t', '\n'];
    for (const raw of invalidInputs) {
      assert.throws(
        () => createBusinessId(raw),
        InvalidBusinessIdError,
        `Expected error for "${raw}"`
      );

      const parsed = parseBusinessId(raw);
      assert.equal(parsed.success, false);
    }
  });

  it('rejects non-string types', () => {
    // @ts-expect-error testing runtime typing
    assert.throws(() => createBusinessId(null), InvalidBusinessIdError);
    // @ts-expect-error testing runtime typing
    assert.throws(() => createBusinessId(undefined), InvalidBusinessIdError);
    // @ts-expect-error testing runtime typing
    assert.throws(() => createBusinessId(12345), InvalidBusinessIdError);
    // @ts-expect-error testing runtime typing
    assert.throws(() => createBusinessId({}), InvalidBusinessIdError);
  });

  it('rejects identifiers without a colon or with uppercase characters', () => {
    const invalidFormats = [
      'wing-spot',
      'Restaurant:wing-spot',
      'restaurant:WingSpot',
      'restaurant::wing-spot',
      ':wing-spot',
      'restaurant:',
      'restaurant wing-spot',
      'restaurant@wing-spot',
      'restaurant:wing_spot', // only kebab-case allowed
    ];

    for (const raw of invalidFormats) {
      assert.throws(
        () => createBusinessId(raw),
        InvalidBusinessIdError,
        `Expected failure for "${raw}"`
      );

      const parsed = parseBusinessId(raw);
      assert.equal(parsed.success, false);
    }
  });

  it('enforces length boundaries', () => {
    // Too short (less than 5 chars, e.g. "a:b")
    assert.throws(() => createBusinessId('a:b'), InvalidBusinessIdError);

    // Excessively long
    const overlyLong = `restaurant:${'a'.repeat(120)}`;
    assert.throws(() => createBusinessId(overlyLong), InvalidBusinessIdError);
  });
});
