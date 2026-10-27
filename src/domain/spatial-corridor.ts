/**
 * Provisional Spatial Slice Domain Model (Milestone 3: Spatial Slice v0.1).
 *
 * Models real-world spatial entities along the Avenida Benito Juárez and
 * Puente Internacional Paso del Norte (Puente Santa Fe) corridor in
 * Ciudad Juárez, Chihuahua, Mexico.
 *
 * Architectural & Authority Principles:
 * 1. Decoupled Spatial Boundary & Authority Delineation:
 *    - The pre-existing World Core (`MyVirtualCommunity`) owns logical world topology,
 *      topological containment (Milestone 68: `spatial-contract.ts`), semantic spatial anchors
 *      without physical coordinates (Milestone 70: `physical-mapping.ts`), and the
 *      Blvd. Manuel Gómez Morín pedestrian navigation graph (Milestone 80: `real-world-street-topology.ts`).
 *    - The World Core does NOT currently own WGS84 geographic coordinates or an
 *      Avenida Benito Juárez spatial topology.
 *    - Therefore, the geographic entities and coordinates in this file serve as
 *      PROVISIONAL, READ-ONLY CONFIGURATION for the hackathon application.
 *      They do not introduce duplicate authorities for mutable world state.
 * 2. Real Geographic Coordinates & Provenance:
 *    - Commercial places: Exact marker coordinates are extracted from the user-provided Google Maps
 *      place URLs (parameters `!3d` and `!4d`), clearly distinguished from map viewport centers (`@lat,lon,zoom`).
 *    - International bridge: Positioned at the bridge structure span across the Rio Grande / international
 *      boundary (OSM way/1531523734), with the Mexican entrance toll plaza (Plaza de Cobro C-51, OSM way/436474488)
 *      documented explicitly as the southern terminus of the bridge structure.
 * 3. Distance Relationships:
 *    - Farmacias Benavides <-> Óptica Fashion: 21.92 meters (across Avenida Benito Juárez).
 *    - Businesses <-> Plaza de Cobro C-51: ~37 to 44 meters north.
 *    - Businesses <-> Bridge Structure Span (Rio Grande): ~237 to 247 meters north.
 * 4. Future 3D Engine Compatibility:
 *    - Entities expose clean IDs, canonical names, and geographic coordinates completely
 *      independent of the web mapping renderer (Leaflet), allowing a future 3D client
 *      (e.g., Unity / WebGL) to consume identical spatial facts.
 */

import { BusinessId, createBusinessId } from './business-id.js';

export type SpatialEntityType = 'infrastructure' | 'pharmacy' | 'optical' | 'restaurant';

export interface GeoCoordinates {
  readonly latitude: number;
  readonly longitude: number;
}

export interface SpatialEntitySourceReference {
  readonly name: string;
  readonly url?: string;
  readonly identifier?: string;
}

export interface SpatialCorridorEntity {
  readonly id: string;
  readonly entityType: SpatialEntityType;
  readonly canonicalName: string;
  readonly popularName?: string;
  readonly alternateNames?: readonly string[];
  readonly address: string;
  readonly coordinates: GeoCoordinates;
  readonly businessId?: BusinessId;
  readonly sources: readonly SpatialEntitySourceReference[];
  readonly description: {
    readonly en: string;
    readonly es: string;
  };
}

export interface SpatialCorridorDefinition {
  readonly id: string;
  readonly name: {
    readonly en: string;
    readonly es: string;
  };
  readonly city: string;
  readonly state: string;
  readonly country: string;
  readonly timeZone: string;
  readonly center: GeoCoordinates;
  readonly defaultZoom: number;
  readonly boundingBox: {
    readonly southWest: GeoCoordinates;
    readonly northEast: GeoCoordinates;
  };
  readonly attribution: {
    readonly mapData: string;
    readonly tileProvider: string;
    readonly notice: string;
  };
  readonly entities: readonly SpatialCorridorEntity[];
}

/**
 * Provisional spatial corridor configuration for Avenida Benito Juárez & Puente Santa Fe.
 */
export const PASO_DEL_NORTE_CORRIDOR: SpatialCorridorDefinition = Object.freeze({
  id: 'corridor:juarez-paso-del-norte',
  name: Object.freeze({
    en: 'Avenida Benito Juárez & Puente Internacional Paso del Norte Corridor',
    es: 'Corredor Avenida Benito Juárez y Puente Internacional Paso del Norte',
  }),
  city: 'Ciudad Juárez',
  state: 'Chihuahua',
  country: 'México',
  timeZone: 'America/Ciudad_Juarez',
  center: Object.freeze({
    latitude: 31.7462,
    longitude: -106.4865,
  }),
  defaultZoom: 17,
  boundingBox: Object.freeze({
    southWest: Object.freeze({
      latitude: 31.7430,
      longitude: -106.4900,
    }),
    northEast: Object.freeze({
      latitude: 31.7500,
      longitude: -106.4830,
    }),
  }),
  attribution: Object.freeze({
    mapData: 'OpenStreetMap contributors',
    tileProvider: 'OpenStreetMap France / Esri World Street Map',
    notice: '© OpenStreetMap contributors, ODbL 1.0. https://www.openstreetmap.org/copyright',
  }),
  entities: Object.freeze([
    // 1. Puente Internacional Paso del Norte (Puente Santa Fe)
    Object.freeze({
      id: 'infrastructure:puente-paso-del-norte',
      entityType: 'infrastructure',
      canonicalName: 'Puente Internacional Paso del Norte',
      popularName: 'Puente Santa Fe',
      alternateNames: Object.freeze([
        'Puente Santa Fe',
        'Puente Juárez-Santa Fe',
        'Santa Fe Street Bridge',
        'Paso del Norte Bridge',
      ]),
      address: 'Avenida Benito Juárez s/n, Centro, 32000 Ciudad Juárez, Chih., México',
      coordinates: Object.freeze({
        // Bridge structure span over Rio Grande / international boundary line
        latitude: 31.74765,
        longitude: -106.48698,
      }),
      sources: Object.freeze([
        Object.freeze({
          name: 'Fideicomiso de Puentes Fronterizos de Chihuahua',
          url: 'https://puentesfronterizos.gob.mx/',
          identifier: 'Puente Internacional Paso del Norte / Santa Fe',
        }),
        Object.freeze({
          name: 'OpenStreetMap Bridge Structure Span',
          url: 'https://www.openstreetmap.org/way/1531523734',
          identifier: 'way/1531523734 (Paso del Norte International Bridge structure, lat 31.74765, lon -106.48698)',
        }),
        Object.freeze({
          name: 'OpenStreetMap Mexican Entrance Toll Plaza',
          url: 'https://www.openstreetmap.org/way/436474488',
          identifier: 'way/436474488 (Plaza de Cobro C-51, lat 31.745885, lon -106.486415, 203 m south of bridge span)',
        }),
      ]),
      description: Object.freeze({
        en: 'Historic international border crossing linking Avenida Benito Juárez in downtown Ciudad Juárez with South El Paso Street in El Paso, Texas across the Rio Grande / Río Bravo. The marker represents the physical bridge structure span over the river (31.74765° N, 106.48698° W), located 203 m north of the Mexican toll plaza (Plaza de Cobro C-51) and 237–246 m north of the commercial establishments on Avenida Juárez. (Infrastructure POI — non-commercial).',
        es: 'Histórico cruce fronterizo internacional que conecta la Avenida Benito Juárez en el centro de Ciudad Juárez con South El Paso Street en El Paso, Texas sobre el Río Bravo. El marcador representa la estructura del puente sobre el río (31.74765° N, 106.48698° W), ubicada a 203 m al norte de la caseta de cobro mexicana (Plaza de Cobro C-51) y a 237–246 m al norte de los comercios de Avenida Juárez. (Punto de interés de infraestructura — no comercial).',
      }),
    }),

    // 2. Farmacias Benavides (Av. Benito Juárez 610 / 852)
    Object.freeze({
      id: 'pharmacy:farmacias-benavides',
      entityType: 'pharmacy',
      canonicalName: 'Farmacias Benavides',
      popularName: 'Farmacias Benavides Av. Juárez',
      address: 'Av. Benito Juárez 610, Chamizal, 32000 Juárez, Chih., México',
      coordinates: Object.freeze({
        // Verified independent coordinates from official INEGI DENUE (IDEE: 6231329, lat 31.74548759, lon -106.48627585)
        // Corresponds to branch "FARMACIAS BENAVIDES SUC M100 CHAMIZAL CHI" (Av. Benito Juárez #852 entre Azucenas y Gardenias)
        // Located 3.47 m from the user-provided Google Maps storefront pin (31.745518, -106.486266)
        latitude: 31.745488,
        longitude: -106.486276,
      }),
      businessId: createBusinessId('pharmacy:farmacias-benavides'),
      sources: Object.freeze([
        Object.freeze({
          name: 'INEGI DENUE (Directorio Estadístico Nacional de Unidades Económicas)',
          url: 'https://www.inegi.org.mx/app/mapa/denue/default.aspx?idee=6231329',
          identifier: 'IDEE: 6231329 | CLEE: 08037464112000542001008186S7 (FARMACIAS BENAVIDES SUC M100 CHAMIZAL CHI, lat 31.745488, lon -106.486276, incorporated 2010-07)',
        }),
        Object.freeze({
          name: 'Google Maps Place Reference (User Reference)',
          url: 'https://maps.app.goo.gl/5i87hJyNoYhHDm6XA',
          identifier: '0x86e75c26ea377181:0x34ac7a36f923b0a1 (marker: 31.745518, -106.486266, 3.5m offset from DENUE)',
        }),
        Object.freeze({
          name: 'World Core Authoritative Schedule Registry',
          identifier: 'pharmacy:farmacias-benavides',
        }),
      ]),
      description: Object.freeze({
        en: 'Independently verified retail pharmacy (INEGI DENUE IDEE 6231329: SUC M100 CHAMIZAL) offering prescription medications, personal care items, and convenience goods along the Avenida Benito Juárez corridor near the international border crossing. Located 22.02 m east across the avenue from Óptica Fashion, 46.06 m south of the bridge entrance toll plaza, and 249.45 m south of the bridge structure.',
        es: 'Farmacia de autoservicio verificada independientemente (INEGI DENUE IDEE 6231329: SUC M100 CHAMIZAL) con medicamentos con receta, artículos de cuidado personal y productos de conveniencia sobre Avenida Benito Juárez. Ubicada a 22.02 m al este de Óptica Fashion, a 46.06 m al sur de la caseta de cobro y a 249.45 m al sur del puente.',
      }),
    }),

    // 3. Óptica Fashion (Av. Benito Juárez 859)
    Object.freeze({
      id: 'optical:optica-fashion',
      entityType: 'optical',
      canonicalName: 'Óptica Fashion',
      popularName: 'Óptica Fashion Centro',
      address: 'Av. Benito Juárez 859, Centro, 32000 Juárez, Chih., México',
      coordinates: Object.freeze({
        // Provisional user-supplied coordinates from Google Maps place parameter !3d31.7455616!4d-106.4864918
        // Note: Unresolved in official open datasets (no match in INEGI DENUE or OpenStreetMap named POIs)
        latitude: 31.745562,
        longitude: -106.486492,
      }),
      businessId: createBusinessId('optical:optica-fashion'),
      sources: Object.freeze([
        Object.freeze({
          name: 'Google Maps Place Reference (Provisional — Unresolved in Open Data)',
          url: 'https://maps.app.goo.gl/Si2SQah29Cxm13cRA',
          identifier: '0x86e7591ff3bdd64f:0x17281bd8f8ba4b4d (marker: 31.745562, -106.486492; provisional pending open survey)',
        }),
        Object.freeze({
          name: 'World Core Authoritative Schedule Registry',
          identifier: 'optical:optica-fashion',
        }),
      ]),
      description: Object.freeze({
        en: 'Prescription eyewear, optometry consultations, and frame styling boutique along Avenida Benito Juárez in downtown Ciudad Juárez (provisional user-supplied reference; unresolved in INEGI DENUE / OSM). Located 22.02 m west across the avenue from Farmacias Benavides and 37.00 m south of the bridge entrance toll plaza.',
        es: 'Óptica y consultorio optométrico en Avenida Benito Juárez en el centro de Ciudad Juárez (referencia provisional provista por el usuario; no registrada en INEGI DENUE / OSM). Ubicada a 22.02 m al oeste de Farmacias Benavides y a 37.00 m al sur de la caseta de cobro del puente.',
      }),
    }),
  ]),
});

/**
 * Finds an entity in the corridor by its unique ID.
 */
export function findCorridorEntity(id: string): SpatialCorridorEntity | null {
  if (typeof id !== 'string' || id.trim() === '') {
    return null;
  }
  const cleanId = id.trim();
  return PASO_DEL_NORTE_CORRIDOR.entities.find((e) => e.id === cleanId) ?? null;
}

/**
 * Finds an entity in the corridor by its associated BusinessId.
 */
export function findCorridorEntityByBusinessId(businessId: string): SpatialCorridorEntity | null {
  if (typeof businessId !== 'string' || businessId.trim() === '') {
    return null;
  }
  const cleanId = businessId.trim();
  return PASO_DEL_NORTE_CORRIDOR.entities.find((e) => e.businessId === cleanId) ?? null;
}
