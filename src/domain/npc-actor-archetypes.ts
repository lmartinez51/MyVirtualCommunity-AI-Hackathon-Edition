/**
 * Canonical NPC Actor Archetype & Witness Classification Domain Model (Increment 5.20.0.2).
 *
 * Core Architectural Invariants:
 * 1. Explicit, machine-readable separation of NPC categories:
 *    - `business_attendant`: Staff assigned to commercial storefronts on scheduled business shifts.
 *    - `public_servant`: Institutional / civic staff performing scheduled public-service duties.
 *    - `community_player_proxy`: Temporary simulated community members participating in the world
 *      (not employees, never governed by work shifts, never off_duty).
 * 2. Orthogonal Witness Designation:
 *    - `isWitness` / `witnessClassification` / `witnessId` explicitly identify whether a canonical actor
 *      holds simulated community witness status grounded in `SYNTHETIC_WITNESS_ROSTER`.
 *    - Archetype and Witness status are independent of runtime activity state, operational status,
 *      location, animation state, display name, or any incident-specific testimony/evidence.
 * 3. Non-Inferred Truth:
 *    - Downstream World Core evaluators, APIs, and Unity clients MUST consume these explicit
 *      authoritative classifications rather than inferring role from names, prefabs, or stations.
 */

import {
  BilingualText,
  SYNTHETIC_WITNESS_ROSTER,
  SyntheticWitnessId,
} from './synthetic-witness.js';

export type NpcActorArchetype =
  | 'business_attendant'
  | 'public_servant'
  | 'community_player_proxy';

export type WitnessClassification = 'none' | 'community_queue_witness';

export const VALID_NPC_ACTOR_ARCHETYPES: readonly NpcActorArchetype[] = Object.freeze([
  'business_attendant',
  'public_servant',
  'community_player_proxy',
]);

export const ACTOR_ARCHETYPE_LABELS: Readonly<Record<NpcActorArchetype, BilingualText>> = Object.freeze({
  business_attendant: Object.freeze({
    en: 'Business Attendant',
    es: 'Encargado de Comercio',
  }),
  public_servant: Object.freeze({
    en: 'Public Servant',
    es: 'Servidor Público',
  }),
  community_player_proxy: Object.freeze({
    en: 'Community Player Proxy',
    es: 'Jugador Comunitario Simulado',
  }),
});

export interface NpcActorClassification {
  readonly npcId: string;
  readonly canonicalName: string;
  readonly actorArchetype: NpcActorArchetype;
  readonly archetypeLabel: BilingualText;
  readonly hasEmploymentSchedule: boolean;
  readonly canEnterOffDuty: boolean;
  readonly isWitness: boolean;
  readonly witnessClassification: WitnessClassification;
  readonly witnessId: SyntheticWitnessId | null;
  readonly assignmentBasis: BilingualText;
}

function buildClassification(params: {
  readonly npcId: string;
  readonly canonicalName: string;
  readonly actorArchetype: NpcActorArchetype;
  readonly isWitness: boolean;
  readonly witnessClassification: WitnessClassification;
  readonly witnessId: SyntheticWitnessId | null;
  readonly assignmentBasis: BilingualText;
}): NpcActorClassification {
  const isProxy = params.actorArchetype === 'community_player_proxy';
  const entry: NpcActorClassification = Object.freeze({
    npcId: params.npcId,
    canonicalName: params.canonicalName,
    actorArchetype: params.actorArchetype,
    archetypeLabel: ACTOR_ARCHETYPE_LABELS[params.actorArchetype],
    hasEmploymentSchedule: !isProxy,
    canEnterOffDuty: !isProxy,
    isWitness: params.isWitness,
    witnessClassification: params.witnessClassification,
    witnessId: params.witnessId,
    assignmentBasis: Object.freeze({ ...params.assignmentBasis }),
  });
  assertValidNpcActorClassification(entry);
  return entry;
}

export function assertValidNpcActorClassification(entry: NpcActorClassification): void {
  if (entry.actorArchetype === 'community_player_proxy') {
    if (entry.hasEmploymentSchedule) {
      throw new Error(`Invalid classification for "${entry.npcId}": community_player_proxy cannot have an employment schedule.`);
    }
    if (entry.canEnterOffDuty) {
      throw new Error(`Invalid classification for "${entry.npcId}": community_player_proxy can NEVER enter off_duty.`);
    }
  } else {
    if (!entry.hasEmploymentSchedule || !entry.canEnterOffDuty) {
      throw new Error(`Invalid classification for "${entry.npcId}": "${entry.actorArchetype}" must allow scheduled duty/off_duty transitions.`);
    }
  }

  if (entry.isWitness) {
    if (entry.witnessClassification === 'none' || !entry.witnessId) {
      throw new Error(`Invalid witness classification for "${entry.npcId}": witness actors require a witnessId and witnessClassification.`);
    }
    if (!(entry.witnessId in SYNTHETIC_WITNESS_ROSTER)) {
      throw new Error(`Invalid witnessId "${entry.witnessId}" for "${entry.npcId}": not present in SYNTHETIC_WITNESS_ROSTER.`);
    }
  } else if (entry.witnessClassification !== 'none' || entry.witnessId !== null) {
    throw new Error(`Invalid non-witness classification for "${entry.npcId}": witnessClassification must be "none" and witnessId null.`);
  }
}

/**
 * Authoritative registry mapping all 7 canonical corridor NPCs to their explicit
 * actor archetype and witness designation.
 */
export const NPC_ACTOR_CLASSIFICATION_ROSTER: Readonly<Record<string, NpcActorClassification>> = Object.freeze({
  'elena-border-concierge': buildClassification({
    npcId: 'elena-border-concierge',
    canonicalName: 'Elena Ramos',
    actorArchetype: 'public_servant',
    isWitness: false,
    witnessClassification: 'none',
    witnessId: null,
    assignmentBasis: {
      en: 'Institutional pedestrian crossing concierge providing official CBP wait-time orientation at the Paso del Norte Bridge Kiosk.',
      es: 'Concierge institucional de orientación peatonal que brinda información oficial de tiempos CBP en el Quiosco del Puente Paso del Norte.',
    },
  }),

  'carlos-farmacia-attendant': buildClassification({
    npcId: 'carlos-farmacia-attendant',
    canonicalName: 'Carlos Méndez',
    actorArchetype: 'business_attendant',
    isWitness: false,
    witnessClassification: 'none',
    witnessId: null,
    assignmentBasis: {
      en: 'Virtual commercial storefront attendant assigned to Farmacias Benavides on Avenida Juárez.',
      es: 'Encargado virtual de comercio asignado a la sucursal de Farmacias Benavides en Avenida Juárez.',
    },
  }),

  'valeria-optica-attendant': buildClassification({
    npcId: 'valeria-optica-attendant',
    canonicalName: 'Valeria Domínguez',
    actorArchetype: 'business_attendant',
    isWitness: false,
    witnessClassification: 'none',
    witnessId: null,
    assignmentBasis: {
      en: 'Virtual commercial storefront attendant assigned to Óptica Fashion on Avenida Juárez.',
      es: 'Encargada virtual de comercio asignada a Óptica Fashion en Avenida Juárez.',
    },
  }),

  'mateo-student': buildClassification({
    npcId: 'mateo-student',
    canonicalName: 'Mateo Navarro',
    actorArchetype: 'community_player_proxy',
    isWitness: true,
    witnessClassification: 'community_queue_witness',
    witnessId: 'mateo-student',
    assignmentBasis: {
      en: 'Temporary simulated community player participating in the Paso del Norte pedestrian bridge queue and recognized as a canonical bridge witness.',
      es: 'Jugador comunitario simulado temporal presente en la fila peatonal del Puente Paso del Norte y reconocido como testigo canónico del cruce.',
    },
  }),

  'sofia-cross-border-retail': buildClassification({
    npcId: 'sofia-cross-border-retail',
    canonicalName: 'Sofía Alarcón',
    actorArchetype: 'community_player_proxy',
    isWitness: true,
    witnessClassification: 'community_queue_witness',
    witnessId: 'sofia-cross-border-retail',
    assignmentBasis: {
      en: 'Temporary simulated community player participating in the Paso del Norte pedestrian bridge queue and recognized as a canonical bridge witness.',
      es: 'Jugadora comunitaria simulada temporal presente en la fila peatonal del Puente Paso del Norte y reconocida como testigo canónica del cruce.',
    },
  }),

  'hector-artisan': buildClassification({
    npcId: 'hector-artisan',
    canonicalName: 'Don Héctor Méndez',
    actorArchetype: 'community_player_proxy',
    isWitness: true,
    witnessClassification: 'community_queue_witness',
    witnessId: 'hector-artisan',
    assignmentBasis: {
      en: 'Temporary simulated community player waiting in the Paso del Norte pedestrian queue and recognized as a canonical bridge witness.',
      es: 'Jugador comunitario simulado temporal en espera dentro de la fila peatonal del Puente Paso del Norte y reconocido como testigo canónico del cruce.',
    },
  }),

  'patricia-citizen-rep': buildClassification({
    npcId: 'patricia-citizen-rep',
    canonicalName: 'Lic. Patricia Morales',
    actorArchetype: 'public_servant',
    isWitness: false,
    witnessClassification: 'none',
    witnessId: null,
    assignmentBasis: {
      en: 'Municipal Citizen Issue Reporting Desk representative performing scheduled civic attention and corridor inspections.',
      es: 'Representante del Módulo Municipal de Atención Ciudadana encargada de recepción de reportes cívicos e inspecciones.',
    },
  }),
});

export const COMMUNITY_PLAYER_PROXY_IDS: readonly string[] = Object.freeze(
  Object.values(NPC_ACTOR_CLASSIFICATION_ROSTER)
    .filter((c) => c.actorArchetype === 'community_player_proxy')
    .map((c) => c.npcId)
);

export const BUSINESS_ATTENDANT_IDS: readonly string[] = Object.freeze(
  Object.values(NPC_ACTOR_CLASSIFICATION_ROSTER)
    .filter((c) => c.actorArchetype === 'business_attendant')
    .map((c) => c.npcId)
);

export const PUBLIC_SERVANT_IDS: readonly string[] = Object.freeze(
  Object.values(NPC_ACTOR_CLASSIFICATION_ROSTER)
    .filter((c) => c.actorArchetype === 'public_servant')
    .map((c) => c.npcId)
);

export const WITNESS_NPC_IDS: readonly string[] = Object.freeze(
  Object.values(NPC_ACTOR_CLASSIFICATION_ROSTER)
    .filter((c) => c.isWitness)
    .map((c) => c.npcId)
);

export function getNpcActorClassification(npcId: string): NpcActorClassification | null {
  if (typeof npcId !== 'string' || npcId.trim() === '') {
    return null;
  }
  return NPC_ACTOR_CLASSIFICATION_ROSTER[npcId.trim()] ?? null;
}

export function isCommunityPlayerProxy(npcId: string): boolean {
  return getNpcActorClassification(npcId)?.actorArchetype === 'community_player_proxy';
}

export function isNpcWitness(npcId: string): boolean {
  return getNpcActorClassification(npcId)?.isWitness === true;
}

export function canActorArchetypeEnterOffDuty(archetype: NpcActorArchetype): boolean {
  return archetype !== 'community_player_proxy';
}
