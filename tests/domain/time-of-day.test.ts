import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  evaluateWorldEnvironment,
  CANONICAL_PHASE_DEFINITIONS,
  hexToRgb,
  rgbToHex,
  lerpHex,
  lerpAngle,
} from '../../src/domain/time-of-day.js';

describe('Time-of-Day Environmental Presentation Domain', () => {
  const JUAREZ_TZ = 'America/Ciudad_Juarez';

  describe('1. Canonical Phase Definitions & Partition Invariants', () => {
    it('defines exactly 7 canonical phases covering 1440 minutes (24 hours)', () => {
      assert.equal(CANONICAL_PHASE_DEFINITIONS.length, 7);
      const phases = CANONICAL_PHASE_DEFINITIONS.map((p) => p.phase);
      assert.deepEqual(phases, ['dawn', 'morning', 'midday', 'afternoon', 'sunset', 'evening', 'night']);

      // Calculate total duration in minutes
      let totalMinutes = 0;
      for (const def of CANONICAL_PHASE_DEFINITIONS) {
        if (def.phase === 'night') {
          totalMinutes += (1440 - def.startMinutes) + def.endMinutes;
        } else {
          totalMinutes += def.endMinutes - def.startMinutes;
        }
      }
      assert.equal(totalMinutes, 1440);
    });

    it('has non-empty titles and valid hex colors for all phases', () => {
      for (const def of CANONICAL_PHASE_DEFINITIONS) {
        assert.ok(def.title.en);
        assert.ok(def.title.es);
        assert.match(def.sunColorHex, /^#[0-9A-F]{6}$/i);
        assert.match(def.ambientSkyHex, /^#[0-9A-F]{6}$/i);
        assert.match(def.ambientEquatorHex, /^#[0-9A-F]{6}$/i);
        assert.match(def.ambientGroundHex, /^#[0-9A-F]{6}$/i);
        assert.match(def.fogColorHex, /^#[0-9A-F]{6}$/i);
        assert.ok(def.sunIntensity > 0);
        assert.ok(def.ambientIntensity > 0);
      }
    });
  });

  describe('2. Exact Phase Boundaries and Rollover', () => {
    it('evaluates 1s before Dawn (05:29:59) as Night', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T12:29:59.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'night');
      assert.equal(snap.environment.isNight, true);
    });

    it('evaluates exact start of Dawn (05:30:00) as Dawn', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T12:30:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'dawn');
      assert.equal(snap.environment.isNight, false);
      assert.ok(snap.environment.phaseProgress < 0.001);
    });

    it('evaluates 1s before Morning (06:59:59) as Dawn', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T13:59:59.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'dawn');
    });

    it('evaluates exact start of Morning (07:00:00) as Morning', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T14:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'morning');
      assert.ok(snap.environment.phaseProgress < 0.001);
      assert.equal(snap.environment.streetlightsActive, false);
    });

    it('evaluates 1s before Midday (11:29:59) as Morning', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T18:29:59.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'morning');
    });

    it('evaluates exact start of Midday (11:30:00) as Midday', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T18:30:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'midday');
      assert.ok(snap.environment.phaseProgress < 0.001);
      assert.ok(snap.environment.sunIntensity > 1.2);
    });

    it('evaluates 1s before Afternoon (13:59:59) as Midday', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T20:59:59.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'midday');
    });

    it('evaluates exact start of Afternoon (14:00:00) as Afternoon', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T21:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'afternoon');
      assert.ok(snap.environment.phaseProgress < 0.001);
    });

    it('evaluates 1s before Sunset (17:29:59) as Afternoon', () => {
      const snap = evaluateWorldEnvironment('2026-11-05T00:29:59.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'afternoon');
    });

    it('evaluates exact start of Sunset (17:30:00) as Sunset', () => {
      const snap = evaluateWorldEnvironment('2026-11-05T00:30:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'sunset');
      assert.ok(snap.environment.phaseProgress < 0.001);
      assert.equal(snap.environment.streetlightsActive, true);
    });

    it('evaluates 1s before Evening (18:59:59) as Sunset', () => {
      const snap = evaluateWorldEnvironment('2026-11-05T01:59:59.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'sunset');
    });

    it('evaluates exact start of Evening (19:00:00) as Evening', () => {
      const snap = evaluateWorldEnvironment('2026-11-05T02:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'evening');
      assert.ok(snap.environment.phaseProgress < 0.001);
      assert.equal(snap.environment.isNight, true);
      assert.equal(snap.environment.streetlightsActive, true);
    });

    it('evaluates 1s before Night (20:59:59) as Evening', () => {
      const snap = evaluateWorldEnvironment('2026-11-05T03:59:59.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'evening');
    });

    it('evaluates exact start of Night (21:00:00) as Night', () => {
      const snap = evaluateWorldEnvironment('2026-11-05T04:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'night');
      assert.ok(snap.environment.phaseProgress < 0.001);
      assert.equal(snap.environment.isNight, true);
      assert.equal(snap.environment.streetlightsActive, true);
    });

    it('evaluates Midnight rollover (23:59:59 to 00:00:00) seamlessly as Night', () => {
      const snapBefore = evaluateWorldEnvironment('2026-11-05T06:59:59.000Z', JUAREZ_TZ); // 23:59:59 MST
      const snapAfter = evaluateWorldEnvironment('2026-11-05T07:00:00.000Z', JUAREZ_TZ); // 00:00:00 MST
      assert.equal(snapBefore.environment.phase, 'night');
      assert.equal(snapAfter.environment.phase, 'night');
      assert.ok(snapAfter.environment.phaseProgress >= snapBefore.environment.phaseProgress);
      assert.equal(snapAfter.environment.streetlightsActive, true);
    });
  });

  describe('3. Representative Verification Matrix Scenarios', () => {
    it('Scenario 1: Early morning (06:00)', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T13:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'dawn');
      assert.ok(snap.environment.sunPitchDegrees >= 10);
      assert.ok(snap.environment.sunPitchDegrees <= 30);
      assert.equal(snap.environment.streetlightsActive, true);
    });

    it('Scenario 2: Morning (09:00)', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T16:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'morning');
      assert.ok(snap.environment.sunPitchDegrees >= 35);
      assert.equal(snap.environment.streetlightsActive, false);
    });

    it('Scenario 3: Midday (12:30)', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T19:30:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'midday');
      assert.ok(snap.environment.sunPitchDegrees >= 50);
      assert.ok(snap.environment.sunIntensity > 1.2);
    });

    it('Scenario 4: Afternoon (15:30)', () => {
      const snap = evaluateWorldEnvironment('2026-11-04T22:30:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'afternoon');
      assert.ok(snap.environment.sunYawDegrees >= 200);
      assert.equal(snap.environment.streetlightsActive, false);
    });

    it('Scenario 5: Sunset (18:00)', () => {
      const snap = evaluateWorldEnvironment('2026-11-05T01:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'sunset');
      assert.equal(snap.environment.streetlightsActive, true);
      assert.ok(snap.environment.sunPitchDegrees <= 15);
    });

    it('Scenario 6: Evening (20:00)', () => {
      const snap = evaluateWorldEnvironment('2026-11-05T03:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'evening');
      assert.equal(snap.environment.isNight, true);
      assert.equal(snap.environment.streetlightsActive, true);
    });

    it('Scenario 7: Night (23:00)', () => {
      const snap = evaluateWorldEnvironment('2026-11-05T06:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.environment.phase, 'night');
      assert.equal(snap.environment.isNight, true);
      assert.equal(snap.environment.streetlightsActive, true);
      assert.ok(snap.environment.sunIntensity <= 0.35); // Cool moonlight
    });
  });

  describe('4. Timezone Awareness & Daylight Saving Transitions', () => {
    it('handles Summer MDT offset (-06:00) correctly', () => {
      // 2026-06-15T18:00:00Z -> 12:00 MDT (Midday)
      const snap = evaluateWorldEnvironment('2026-06-15T18:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.civilTime.hour, 12);
      assert.equal(snap.environment.phase, 'midday');
      assert.ok(snap.localCivilTime.includes('-06:00'));
    });

    it('handles Winter MST offset (-07:00) correctly', () => {
      // 2026-11-04T19:00:00Z -> 12:00 MST (Midday)
      const snap = evaluateWorldEnvironment('2026-11-04T19:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.civilTime.hour, 12);
      assert.equal(snap.environment.phase, 'midday');
      assert.ok(snap.localCivilTime.includes('-07:00'));
    });

    it('projects local calendar date across UTC midnight', () => {
      // 2026-11-07T02:00:00Z is UTC Saturday, but in Juárez it is Friday 19:00 (Evening)
      const snap = evaluateWorldEnvironment('2026-11-07T02:00:00.000Z', JUAREZ_TZ);
      assert.equal(snap.civilTime.dayOfWeek, 5); // Friday
      assert.equal(snap.civilTime.hour, 19);
      assert.equal(snap.environment.phase, 'evening');
    });
  });

  describe('5. Identical Timestamp Determinism', () => {
    it('produces bit-for-bit identical snapshots when evaluated multiple times', () => {
      const ts = '2026-11-04T17:42:19.456Z';
      const snapA = evaluateWorldEnvironment(ts, JUAREZ_TZ);
      const snapB = evaluateWorldEnvironment(ts, JUAREZ_TZ);
      assert.equal(JSON.stringify(snapA), JSON.stringify(snapB));
    });
  });

  describe('6. Math and Color Interpolation Utilities', () => {
    it('correctly converts hex to rgb and rgb to hex', () => {
      const rgb = hexToRgb('#FFD6AA');
      assert.ok(Math.abs(rgb.r - 1.0) < 0.01);
      assert.ok(Math.abs(rgb.g - 0.839) < 0.01);
      assert.ok(Math.abs(rgb.b - 0.667) < 0.01);
      assert.equal(rgbToHex(rgb), '#FFD6AA');
    });

    it('interpolates colors smoothly without NaN', () => {
      const mid = lerpHex('#000000', '#FFFFFF', 0.5);
      assert.equal(mid, '#808080');
    });

    it('interpolates angles wrapping 360 smoothly', () => {
      const angle = lerpAngle(350, 10, 0.5);
      assert.equal(angle, 0);
    });
  });
});
