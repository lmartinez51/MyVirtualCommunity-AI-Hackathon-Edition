import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import {
  PASO_DEL_NORTE_CORRIDOR,
  findCorridorEntity,
  findCorridorEntityByBusinessId,
} from '../../src/domain/spatial-corridor.js';

describe('Spatial Corridor Domain Model (Milestone 3: Spatial Slice v0.1)', () => {
  it('defines the authentic Avenida Benito Juárez corridor bounds and center in Ciudad Juárez', () => {
    assert.strictEqual(PASO_DEL_NORTE_CORRIDOR.id, 'corridor:juarez-paso-del-norte');
    assert.strictEqual(
      PASO_DEL_NORTE_CORRIDOR.name.en,
      'Avenida Benito Juárez & Puente Internacional Paso del Norte Corridor'
    );
    assert.strictEqual(
      PASO_DEL_NORTE_CORRIDOR.name.es,
      'Corredor Avenida Benito Juárez y Puente Internacional Paso del Norte'
    );
    assert.strictEqual(PASO_DEL_NORTE_CORRIDOR.city, 'Ciudad Juárez');
    assert.strictEqual(PASO_DEL_NORTE_CORRIDOR.state, 'Chihuahua');
    assert.strictEqual(PASO_DEL_NORTE_CORRIDOR.country, 'México');

    const { center, boundingBox } = PASO_DEL_NORTE_CORRIDOR;
    // Coordinates are finite numbers
    assert.ok(Number.isFinite(center.latitude), 'Center latitude must be finite');
    assert.ok(Number.isFinite(center.longitude), 'Center longitude must be finite');

    // Corridor bounds must enclose Avenida Benito Juárez near the international crossing
    assert.ok(center.latitude >= 31.740 && center.latitude <= 31.750);
    assert.ok(center.longitude >= -106.495 && center.longitude <= -106.480);
    assert.ok(boundingBox.northEast.latitude > boundingBox.southWest.latitude);
    assert.ok(boundingBox.northEast.longitude > boundingBox.southWest.longitude);
  });

  it('contains exactly the three designated landmarks', () => {
    assert.strictEqual(PASO_DEL_NORTE_CORRIDOR.entities.length, 3);
    const ids = PASO_DEL_NORTE_CORRIDOR.entities.map((e) => e.id);
    assert.deepStrictEqual(ids, [
      'infrastructure:puente-paso-del-norte',
      'pharmacy:farmacias-benavides',
      'optical:optica-fashion'
    ]);
  });

  it('properly represents the international bridge as infrastructure (non-commercial)', () => {
    const bridge = findCorridorEntity('infrastructure:puente-paso-del-norte');
    assert.ok(bridge, 'Bridge entity must be found');
    assert.strictEqual(bridge?.entityType, 'infrastructure');
    assert.strictEqual(bridge?.businessId, undefined, 'Bridge must not have a commercial businessId');
    assert.strictEqual(bridge?.canonicalName, 'Puente Internacional Paso del Norte');
    assert.strictEqual(bridge?.popularName, 'Puente Santa Fe');
    assert.ok(bridge?.sources && bridge.sources.length > 0);
    assert.ok(Number.isFinite(bridge?.coordinates.latitude));
    assert.ok(Number.isFinite(bridge?.coordinates.longitude));
    assert.strictEqual(bridge?.coordinates.latitude, 31.74765);
    assert.strictEqual(bridge?.coordinates.longitude, -106.48698);
  });

  it('properly represents Farmacias Benavides with exact canonical name, type, and verified sources', () => {
    const pharmacy = findCorridorEntity('pharmacy:farmacias-benavides');
    assert.ok(pharmacy, 'Farmacias Benavides must be found');
    assert.strictEqual(pharmacy?.entityType, 'pharmacy');
    assert.strictEqual(pharmacy?.businessId, 'pharmacy:farmacias-benavides');
    assert.strictEqual(pharmacy?.canonicalName, 'Farmacias Benavides');
    assert.strictEqual(pharmacy?.address, 'Av. Benito Juárez 610, Chamizal, 32000 Juárez, Chih., México');
    assert.strictEqual(pharmacy?.coordinates.latitude, 31.745488);
    assert.strictEqual(pharmacy?.coordinates.longitude, -106.486276);
    assert.ok(pharmacy?.sources.some(s => s.identifier?.includes('6231329')), 'Must reference official INEGI DENUE ID');
    assert.ok(pharmacy?.sources.some(s => s.identifier?.includes('0x34ac7a36f923b0a1')), 'Must preserve Google reference');
  });

  it('properly represents Óptica Fashion with exact canonical name including accents, type, and verified sources', () => {
    const optica = findCorridorEntity('optical:optica-fashion');
    assert.ok(optica, 'Óptica Fashion must be found');
    assert.strictEqual(optica?.entityType, 'optical');
    assert.strictEqual(optica?.businessId, 'optical:optica-fashion');
    assert.strictEqual(optica?.canonicalName, 'Óptica Fashion');
    assert.strictEqual(optica?.address, 'Av. Benito Juárez 859, Centro, 32000 Juárez, Chih., México');
    assert.strictEqual(optica?.coordinates.latitude, 31.745562);
    assert.strictEqual(optica?.coordinates.longitude, -106.486492);
    assert.ok(optica?.sources.some(s => s.identifier?.includes('0x17281bd8f8ba4b4d')));
  });

  it('findCorridorEntityByBusinessId maps business IDs to corridor entities and returns null for unknown IDs', () => {
    const benavides = findCorridorEntityByBusinessId('pharmacy:farmacias-benavides');
    assert.ok(benavides);
    assert.strictEqual(benavides?.id, 'pharmacy:farmacias-benavides');

    const optica = findCorridorEntityByBusinessId('optical:optica-fashion');
    assert.ok(optica);
    assert.strictEqual(optica?.id, 'optical:optica-fashion');

    const unknown = findCorridorEntityByBusinessId('restaurant:wing-spot');
    assert.strictEqual(unknown, null, 'Wing Spot is not in this spatial corridor');
  });
});
