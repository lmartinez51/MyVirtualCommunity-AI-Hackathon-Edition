import { describe, it } from 'node:test';
import assert from 'node:assert';
import {
  NPC_SCHEDULE_ROSTER,
  evaluateNpcActivity,
  evaluateRosterActivity,
  ELENA_SCHEDULE,
  CARLOS_SCHEDULE,
  MATEO_SCHEDULE,
  PATRICIA_SCHEDULE,
} from '../../src/domain/npc-activity.js';

describe('Deterministic NPC Activity, Routines & World Presence Domain', () => {
  const EXPECTED_NPCS = [
    'elena-border-concierge',
    'carlos-farmacia-attendant',
    'valeria-optica-attendant',
    'mateo-student',
    'sofia-cross-border-retail',
    'hector-artisan',
    'patricia-citizen-rep',
  ];

  it('1. Authoritative roster contains all 7 canonical corridor NPCs', () => {
    const keys = Object.keys(NPC_SCHEDULE_ROSTER);
    assert.strictEqual(keys.length, 7);
    for (const expectedId of EXPECTED_NPCS) {
      assert.ok(keys.includes(expectedId), `Roster missing expected NPC: ${expectedId}`);
      const profile = NPC_SCHEDULE_ROSTER[expectedId];
      assert.ok(profile, `Profile must exist for ${expectedId}`);
      assert.strictEqual(profile.npcId, expectedId);
    }
  });

  it('2. Invariant: Every profile is clearly marked as a simulated demonstration schedule', () => {
    for (const profile of Object.values(NPC_SCHEDULE_ROSTER)) {
      assert.strictEqual(profile.isDemonstrationSchedule, true);
      assert.ok(profile.demonstrationNotice.en.length > 0);
      assert.ok(profile.demonstrationNotice.es.length > 0);
      assert.strictEqual(profile.timeZone, 'America/Ciudad_Juarez');
      assert.ok(profile.canonicalName.length > 0);
      assert.ok(profile.role.en.length > 0);
      assert.ok(profile.role.es.length > 0);
    }
  });

  it('3. Elena Ramos deterministic shift transitions across morning, handover, evening, and night', () => {
    // Wednesday 2026-11-04 in Ciudad Juárez (UTC-7):
    // UTC 13:05 = 06:05 Juárez (opens at 06:00)
    // UTC 20:29 = 13:29 Juárez (available)
    // UTC 20:30 = 13:30 Juárez (busy: handover)
    // UTC 21:35 = 14:35 Juárez (available: evening)
    // UTC 06:00 next day = 23:00 Juárez (off_duty)

    // 06:05 Juárez
    const snapMorning = evaluateNpcActivity(ELENA_SCHEDULE, '2026-11-04T13:05:00.000Z');
    assert.strictEqual(snapMorning.state, 'available');
    assert.strictEqual(snapMorning.canInteract, true);
    assert.strictEqual(snapMorning.activityTitle.es, 'Orientando peatones');

    // 13:29 Juárez (Available)
    const snapBeforeHandover = evaluateNpcActivity(ELENA_SCHEDULE, '2026-11-04T20:29:00.000Z');
    assert.strictEqual(snapBeforeHandover.state, 'available');

    // 13:30 Juárez (Handover / Busy)
    const snapHandover = evaluateNpcActivity(ELENA_SCHEDULE, '2026-11-04T20:30:00.000Z');
    assert.strictEqual(snapHandover.state, 'busy');
    assert.strictEqual(snapHandover.canInteract, true);
    assert.strictEqual(snapHandover.activityTitle.es, 'Relevo de turno y conciliación de bitácoras');
    assert.ok(snapHandover.interactionNotice?.es.includes('labores de rutina'));

    // 14:35 Juárez (Back to Available)
    const snapAfternoon = evaluateNpcActivity(ELENA_SCHEDULE, '2026-11-04T21:35:00.000Z');
    assert.strictEqual(snapAfternoon.state, 'available');

    // 23:00 Juárez (Off Duty)
    const snapNight = evaluateNpcActivity(ELENA_SCHEDULE, '2026-11-05T06:00:00.000Z');
    assert.strictEqual(snapNight.state, 'off_duty');
    assert.strictEqual(snapNight.canInteract, false);
    assert.strictEqual(snapNight.activityTitle.es, 'Fuera de servicio');
  });

  it('4. Carlos Méndez adheres strictly to verified Farmacias Benavides operating rules and breaks', () => {
    // Wednesday 2026-11-04 in Ciudad Juárez (UTC-7)
    // 10:00 Juárez (UTC 17:00) -> Available
    const snap10am = evaluateNpcActivity(CARLOS_SCHEDULE, '2026-11-04T17:00:00.000Z');
    assert.strictEqual(snap10am.state, 'available');

    // 14:15 Juárez (UTC 21:15) -> Busy (Lunch & stock)
    const snapLunch = evaluateNpcActivity(CARLOS_SCHEDULE, '2026-11-04T21:15:00.000Z');
    assert.strictEqual(snapLunch.state, 'busy');
    assert.strictEqual(snapLunch.activityTitle.es, 'Almuerzo y verificación de anaqueles');

    // 21:15 Juárez (UTC 04:15 next day) -> Busy (Register closing)
    const snapClose = evaluateNpcActivity(CARLOS_SCHEDULE, '2026-11-05T04:15:00.000Z');
    assert.strictEqual(snapClose.state, 'busy');
    assert.strictEqual(snapClose.activityTitle.es, 'Cierre de caja y preparación de turno nocturno');

    // Sunday 2026-11-08 at 12:00 Juárez (UTC 19:00) -> Closed on Sunday
    const snapSunday = evaluateNpcActivity(CARLOS_SCHEDULE, '2026-11-08T19:00:00.000Z');
    assert.strictEqual(snapSunday.state, 'off_duty');
    assert.strictEqual(snapSunday.activityTitle.es, 'Sucursal cerrada en domingo');
    assert.strictEqual(snapSunday.canInteract, false);
  });

  it('5. Lic. Patricia Morales operates citizen desk on weekdays and rests on weekends', () => {
    // Wednesday 10:00 Juárez (UTC 17:00) -> Available
    const snapWed = evaluateNpcActivity(PATRICIA_SCHEDULE, '2026-11-04T17:00:00.000Z');
    assert.strictEqual(snapWed.state, 'available');
    assert.strictEqual(snapWed.activityTitle.es, 'Recibiendo reportes ciudadanos');

    // Wednesday 12:30 Juárez (UTC 19:30) -> Busy (routing reports to Public Works)
    const snapMidday = evaluateNpcActivity(PATRICIA_SCHEDULE, '2026-11-04T19:30:00.000Z');
    assert.strictEqual(snapMidday.state, 'busy');
    assert.strictEqual(snapMidday.activityTitle.es, 'Canalización técnica de reportes con Obras Públicas');

    // Wednesday 17:00 Juárez (UTC 00:00 next day) -> Off duty
    const snapEve = evaluateNpcActivity(PATRICIA_SCHEDULE, '2026-11-05T00:00:00.000Z');
    assert.strictEqual(snapEve.state, 'off_duty');

    // Saturday 11:00 Juárez (UTC 18:00) -> Weekend closed
    const snapSat = evaluateNpcActivity(PATRICIA_SCHEDULE, '2026-11-07T18:00:00.000Z');
    assert.strictEqual(snapSat.state, 'off_duty');
    assert.strictEqual(snapSat.activityTitle.es, 'Módulo cerrado en sábado');
  });

  it('6. Offline reconstruction invariant: Evaluating past and future timestamps produces pure deterministic output', () => {
    const historicalTime = '2025-05-14T17:00:00.000Z'; // May 2025 (Wednesday 11:00 AM Juárez MDT)
    const run1 = evaluateNpcActivity(MATEO_SCHEDULE, historicalTime);
    const run2 = evaluateNpcActivity(MATEO_SCHEDULE, historicalTime);

    assert.deepStrictEqual(run1, run2);
    // At 11:00 Juárez on a weekday, Mateo is on campus in El Paso (off_duty)
    assert.strictEqual(run1.state, 'off_duty');
    assert.strictEqual(run1.activityTitle.es, 'En clases en El Paso');
  });

  it('7. evaluateRosterActivity aggregates snapshots and accurate summary counts for all 7 NPCs', () => {
    // Wednesday 10:30 Juárez (UTC 17:30)
    const rosterSnap = evaluateRosterActivity(NPC_SCHEDULE_ROSTER, '2026-11-04T17:30:00.000Z');

    assert.strictEqual(rosterSnap.summary.totalCount, 7);
    assert.strictEqual(Object.keys(rosterSnap.npcs).length, 7);
    assert.strictEqual(rosterSnap.isDemonstrationSchedule, true);

    // At 10:30 on Wednesday:
    // - Elena: available (kiosk)
    // - Carlos: available (pharmacy)
    // - Valeria: available (optical)
    // - Mateo: off_duty (on campus in El Paso)
    // - Sofia: available (commuting to retail)
    // - Don Hector: available (exhibiting woodcrafts)
    // - Patricia: available (citizen desk)
    assert.strictEqual(rosterSnap.npcs['elena-border-concierge']?.state, 'available');
    assert.strictEqual(rosterSnap.npcs['carlos-farmacia-attendant']?.state, 'available');
    assert.strictEqual(rosterSnap.npcs['valeria-optica-attendant']?.state, 'available');
    assert.strictEqual(rosterSnap.npcs['hector-artisan']?.state, 'available');
    assert.strictEqual(rosterSnap.npcs['patricia-citizen-rep']?.state, 'available');

    assert.strictEqual(
      rosterSnap.summary.availableCount +
        rosterSnap.summary.busyCount +
        rosterSnap.summary.offDutyCount +
        rosterSnap.summary.unavailableCount,
      7
    );
  });
});
