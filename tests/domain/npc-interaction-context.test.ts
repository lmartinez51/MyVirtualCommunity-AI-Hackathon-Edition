import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  CANONICAL_NPC_IDS,
  evaluateNpcInteractionContext,
  evaluateRosterInteractionContext,
} from '../../src/domain/npc-interaction-context.js';

describe('Authoritative NPC Interaction Context & Spatial Demeanor Domain Tests', () => {
  it('1. CANONICAL_NPC_IDS contains all 7 corridor characters', () => {
    assert.strictEqual(CANONICAL_NPC_IDS.length, 7);
    const expected = [
      'elena-border-concierge',
      'carlos-farmacia-attendant',
      'valeria-optica-attendant',
      'mateo-student',
      'sofia-cross-border-retail',
      'hector-artisan',
      'patricia-citizen-rep',
    ];
    for (const id of expected) {
      assert.ok(CANONICAL_NPC_IDS.includes(id as any), `Missing ${id}`);
    }
  });

  it('2. evaluateRosterInteractionContext produces valid summary for all 7 NPCs', () => {
    // Wednesday 10:00 AM local time (17:00 UTC)
    const testTime = '2026-11-04T17:00:00.000Z';
    const roster = evaluateRosterInteractionContext(testTime);

    assert.strictEqual(roster.summary.totalCount, 7);
    assert.strictEqual(roster.timeZone, 'America/Ciudad_Juarez');
    assert.strictEqual(roster.evaluatedAt, testTime);
    assert.strictEqual(
      roster.summary.availableNormalCount +
        roster.summary.inTransitCount +
        roster.summary.contextualCount +
        roster.summary.busyCount +
        roster.summary.offDutyCount +
        roster.summary.unavailableCount,
      7
    );

    for (const npcId of CANONICAL_NPC_IDS) {
      const ctx = roster.npcs[npcId];
      assert.ok(ctx, `Context missing for ${npcId}`);
      assert.strictEqual(ctx.npcId, npcId);
      assert.strictEqual(ctx.isDemonstrationContext, true);
      assert.ok(ctx.canonicalName.length > 0);
      assert.ok(ctx.role.en.length > 0);
      assert.ok(ctx.role.es.length > 0);
      assert.ok(ctx.decisionReason.en.length > 0);
      assert.ok(ctx.decisionReason.es.length > 0);
      assert.ok(ctx.dialogueGuidance.en.length > 0);
      assert.ok(ctx.dialogueGuidance.es.length > 0);
      assert.ok(ctx.contextualGreeting.en.length > 0);
      assert.ok(ctx.contextualGreeting.es.length > 0);
    }
  });

  it('3. Precedence 1: Off-Duty NPC evaluates to off_duty / off_duty_courtesy', () => {
    // Carlos at 23:30 local time (next day 06:30 UTC): Pharmacy closed
    const offDutyTime = '2026-11-05T06:30:00.000Z';
    const ctx = evaluateNpcInteractionContext('carlos-farmacia-attendant', offDutyTime);

    assert.ok(ctx);
    assert.strictEqual(ctx.npcId, 'carlos-farmacia-attendant');
    assert.strictEqual(ctx.activityState, 'off_duty');
    assert.strictEqual(ctx.interactionAvailability, 'off_duty');
    assert.strictEqual(ctx.interactionMode, 'off_duty_courtesy');
    assert.strictEqual(ctx.canInitiateConversation, true);
    assert.strictEqual(ctx.isAvailableForNormalService, false);
    assert.strictEqual(ctx.isMoving, false);
    assert.ok(ctx.decisionReason.en.includes('off duty'));
    assert.ok(ctx.contextualGreeting.es.includes('concluyó'));
  });

  it('4. Precedence 2: In-Transit NPC evaluates to available_in_transit / in_transit', () => {
    // Carlos at 14:00:15 local time on Wednesday (21:00:15 UTC): Walking to lunch
    const movingTime = '2026-11-04T21:00:15.000Z';
    const ctx = evaluateNpcInteractionContext('carlos-farmacia-attendant', movingTime);

    assert.ok(ctx);
    assert.strictEqual(ctx.npcId, 'carlos-farmacia-attendant');
    assert.strictEqual(ctx.movementState, 'moving');
    assert.strictEqual(ctx.interactionAvailability, 'available_in_transit');
    assert.strictEqual(ctx.interactionMode, 'in_transit');
    assert.strictEqual(ctx.canInitiateConversation, true);
    assert.strictEqual(ctx.isAvailableForNormalService, false);
    assert.strictEqual(ctx.isMoving, true);
    assert.strictEqual(ctx.destinationWaypointId, 'wp_east_dining_benches');
    assert.ok(ctx.activeRouteId);
    assert.ok(ctx.progression !== null && ctx.progression > 0 && ctx.progression < 1);
    assert.ok(ctx.decisionReason.en.includes('walking'));
    assert.ok(ctx.dialogueGuidance.en.includes('transit'));
    assert.ok(ctx.contextualGreeting.en.includes('walking to'));
  });

  it('5. Precedence 3: Dwell / Break at secondary location evaluates to available_contextual / break_or_dwell', () => {
    // Carlos at 14:15 local time on Wednesday (21:15 UTC): At East Dining Benches taking lunch
    const dwellTime = '2026-11-04T21:15:00.000Z';
    const ctx = evaluateNpcInteractionContext('carlos-farmacia-attendant', dwellTime);

    assert.ok(ctx);
    assert.strictEqual(ctx.npcId, 'carlos-farmacia-attendant');
    assert.strictEqual(ctx.movementState, 'waiting');
    assert.strictEqual(ctx.interactionAvailability, 'available_contextual');
    assert.strictEqual(ctx.interactionMode, 'break_or_dwell');
    assert.strictEqual(ctx.canInitiateConversation, true);
    assert.strictEqual(ctx.isAvailableForNormalService, false);
    assert.strictEqual(ctx.isMoving, false);
    assert.strictEqual(ctx.currentWaypointId, 'wp_east_dining_benches');
    assert.ok(ctx.decisionReason.en.includes('contextual conversation'));
    assert.ok(ctx.contextualGreeting.en.toLowerCase().includes('lunch break'));
  });

  it('6. Precedence 5: Normal service at station evaluates to available_normal / normal_service', () => {
    // Elena Ramos at 10:00 local time on Wednesday (17:00 UTC): At Kiosk helping visitors
    const normalTime = '2026-11-04T17:00:00.000Z';
    const ctx = evaluateNpcInteractionContext('elena-border-concierge', normalTime);

    assert.ok(ctx);
    assert.strictEqual(ctx.npcId, 'elena-border-concierge');
    assert.strictEqual(ctx.activityState, 'available');
    assert.strictEqual(ctx.movementState, 'stationary');
    assert.strictEqual(ctx.interactionAvailability, 'available_normal');
    assert.strictEqual(ctx.interactionMode, 'normal_service');
    assert.strictEqual(ctx.canInitiateConversation, true);
    assert.strictEqual(ctx.isAvailableForNormalService, true);
    assert.strictEqual(ctx.isMoving, false);
    assert.strictEqual(ctx.currentWaypointId, 'wp_elena_kiosk');
    assert.strictEqual(ctx.associatedPlaceId, 'infrastructure:puente-paso-del-norte');
    assert.ok(ctx.contextualGreeting.es.includes('Puente Paso del Norte'));
  });

  it('7. Unknown NPC returns null safely', () => {
    const ctx = evaluateNpcInteractionContext('unknown-npc-id-12345', '2026-11-04T17:00:00.000Z');
    assert.strictEqual(ctx, null);
  });
});
