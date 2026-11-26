using System;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using MyVirtualCommunity.Client.Progression;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated EditMode verification suite for Implementation 07.3
    /// (Remanencia Nivel 0 — Hotel de las Palomas: Construction-Era Environmental Identity).
    /// Verifies spatial placement of bilingual hotel signage, construction-era MEP infrastructure,
    /// per-room architectural variations (101-106), zero added furniture, zero elevator shaft/door
    /// alterations, zero added point/shadow lights, IP-05 preservation, world-context isolation,
    /// and idempotency of MigrateRemanenciaEnvironmentalIdentityInOpenScene().
    /// </summary>
    [TestFixture]
    public class RemanenciaHotelEnvironmentalIdentityTests
    {
        private const float BaseY = BackroomsLevel1Builder.BaseY;

        [Test]
        public void BilingualHotelSignage_IsPresentInIntendedArchitecturalZonesWithValidTextures()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            Transform envRoot = GameObject.Find(BackroomsLevel1Builder.HotelEnvironmentalIdentityRootName)?.transform;
            Assert.IsNotNull(envRoot, "Hotel_Environmental_Identity root must exist.");
            Transform signGroup = envRoot.Find("Hotel_Identity_Signage");
            Assert.IsNotNull(signGroup, "Hotel_Identity_Signage subgroup must exist.");

            (string signName, Vector2 minXZ, Vector2 maxXZ, string expectedMatPath)[] expectedSigns =
            {
                ("Hotel_Sign_EntranceMarquee",     new Vector2(15.8f,  7.0f), new Vector2(16.8f, 10.6f), BackroomsLevel1Builder.SignHotelNameMatPath),
                ("Hotel_Sign_ReceptionBackdrop",   new Vector2( 7.6f, 11.4f), new Vector2(14.0f, 12.0f), BackroomsLevel1Builder.SignReceptionMatPath),
                ("Hotel_Sign_ElevatorsHeader",     new Vector2( 3.0f,  5.0f), new Vector2( 3.8f,  8.6f), BackroomsLevel1Builder.SignElevatorsMatPath),
                ("Hotel_Sign_ElevatorNotice",      new Vector2( 3.0f,  6.2f), new Vector2( 3.8f,  7.4f), BackroomsLevel1Builder.SignElevatorNoticeMatPath),
                ("Hotel_Sign_GuestRoomsWayfinding",new Vector2( 9.8f, -1.6f), new Vector2(10.8f,  1.2f), BackroomsLevel1Builder.SignGuestRoomsMatPath),
                ("Hotel_Sign_ServiceWing",         new Vector2(-9.8f, -1.6f), new Vector2(-8.8f,  1.2f), BackroomsLevel1Builder.SignServiceMatPath),
                ("Hotel_Sign_SWMaintenance",       new Vector2(-8.6f, -2.1f), new Vector2(-6.2f, -1.3f), BackroomsLevel1Builder.SignMaintenanceMatPath)
            };

            foreach (var s in expectedSigns)
            {
                Transform tf = signGroup.Find(s.signName);
                Assert.IsNotNull(tf, $"Missing signage object '{s.signName}'.");
                Vector3 p = tf.position;
                Assert.IsTrue(
                    p.x >= s.minXZ.x && p.x <= s.maxXZ.x && p.z >= s.minXZ.y && p.z <= s.maxXZ.y,
                    $"Sign '{s.signName}' at ({p.x:F2}, {p.z:F2}) is outside expected architectural zone.");
                Assert.IsNull(tf.GetComponent<Collider>(), $"Sign '{s.signName}' must be non-colliding.");

                Renderer r = tf.GetComponent<Renderer>();
                Assert.IsNotNull(r);
                Material expectedMat = AssetDatabase.LoadAssetAtPath<Material>(s.expectedMatPath);
                Assert.AreEqual(expectedMat, r.sharedMaterial, $"Sign '{s.signName}' has unexpected material.");
                Assert.IsNotNull(expectedMat.GetTexture("_BaseMap"), $"Sign material '{s.expectedMatPath}' must have a baked texture.");
            }

            // Verify approved UTF-8 Spanish/English signage wording constants
            Assert.AreEqual("RECEPCIÓN", BackroomsLevel1Builder.SignReceptionEs);
            Assert.AreEqual("RECEPTION", BackroomsLevel1Builder.SignReceptionEn);
            Assert.AreEqual("FUERA DE SERVICIO", BackroomsLevel1Builder.SignElevatorNoticeEs);
            Assert.AreEqual("OUT OF SERVICE", BackroomsLevel1Builder.SignElevatorNoticeEn);
            Assert.AreEqual("SOLO PERSONAL", BackroomsLevel1Builder.SignServiceEs);
            Assert.AreEqual("STAFF ONLY", BackroomsLevel1Builder.SignServiceEn);
            Assert.AreEqual("ÁREA DE MANTENIMIENTO", BackroomsLevel1Builder.SignMaintenanceEs);
            Assert.AreEqual("MAINTENANCE AREA", BackroomsLevel1Builder.SignMaintenanceEn);

            // Confirm zero duplicate signage GameObjects under Hotel_Identity_Signage
            Assert.AreEqual(10, signGroup.childCount, "Expected exactly 10 signage/bracket/outline objects with zero duplicates.");

            // Confirm all 6 room-number plaques remain present & bound to rooms 101-106
            Transform hotelRoot = GameObject.Find(BackroomsLevel1Builder.HotelArchitectureRootName)?.transform;
            for (int r = 0; r < 6; r++)
            {
                int num = 101 + r;
                Transform plaque = hotelRoot.Find($"Hotel_GuestRoom_{num}/Hotel_Room{num}_CorridorWall_RoomPlaque");
                Assert.IsNotNull(plaque, $"Room plaque for Room {num} must remain intact.");
                Material plaqueMat = AssetDatabase.LoadAssetAtPath<Material>($"Assets/Settings/Remanencia_Level0_RoomPlaque_{num}.mat");
                Assert.AreEqual(plaqueMat, plaque.GetComponent<Renderer>().sharedMaterial);
            }
        }

        [Test]
        public void ConstructionInfrastructureAndRoomVariations_AreProperlyPlacedAndNonObstructive()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());
            Physics.SyncTransforms();

            Transform envRoot = GameObject.Find(BackroomsLevel1Builder.HotelEnvironmentalIdentityRootName)?.transform;
            Assert.IsNotNull(envRoot);

            Transform elec = envRoot.Find("Hotel_Infra_Electrical");
            Transform plumb = envRoot.Find("Hotel_Infra_Plumbing");
            Transform structGrp = envRoot.Find("Hotel_Infra_Structural");
            Transform roomsGrp = envRoot.Find("Hotel_Room_Variations");

            Assert.IsNotNull(elec);
            Assert.IsNotNull(plumb);
            Assert.IsNotNull(structGrp);
            Assert.IsNotNull(roomsGrp);

            // Spatial placement assertions for key MEP & structural details
            Transform tray = elec.Find("Hotel_Elec_CorridorCableTray");
            Assert.IsNotNull(tray);
            Assert.Greater(tray.position.y, BaseY + 2.5f, "Overhead cable tray must remain well above player head height.");

            Transform riserN = plumb.Find("Hotel_Plumb_NorthServiceRiserA");
            Assert.IsNotNull(riserN);
            Assert.Less(riserN.position.x, -9.5f, "Service plumbing riser must sit inside Back-of-House Service Wing.");

            // Every new environmental storytelling object must be strictly non-colliding so paths & artifacts remain unobstructed
            Collider[] envColliders = envRoot.GetComponentsInChildren<Collider>(includeInactive: true);
            Assert.AreEqual(0, envColliders.Length, "Environmental identity details must not introduce movement-blocking colliders.");

            // Every guest room 101-106 has distinct unfinished variation details
            for (int num = 101; num <= 106; num++)
            {
                bool foundForRoom = false;
                string prefix = $"Hotel_Room{num}_";
                for (int i = 0; i < roomsGrp.childCount; i++)
                {
                    if (roomsGrp.GetChild(i).name.StartsWith(prefix, StringComparison.Ordinal))
                    {
                        foundForRoom = true;
                        break;
                    }
                }
                Assert.IsTrue(foundForRoom, $"Expected individual construction variation details for Room {num}.");
            }

            // Confirm ZERO furniture objects and ZERO Cobalt-60/radiation/biological naming inside Hotel_Environmental_Identity
            string[] forbiddenTerms = { "Bed", "Desk", "Chair", "Table", "Sofa", "Couch", "Cabinet", "Lamp", "Dresser", "Counter", "Cobalt", "Radiation", "Biohazard", "Fungal", "Fungus", "Monster" };
            foreach (Transform t in envRoot.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                foreach (string term in forbiddenTerms)
                {
                    Assert.IsTrue(
                        t.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) < 0,
                        $"Prohibited term '{term}' found on environmental object '{t.name}'.");
                }
            }
        }

        [Test]
        public void ElevatorGeometryLightingBudgetIp05AndWorldContext_RemainUntouched()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            Assert.IsNotNull(root);

            // Exact lighting budget preserved (16 fixture point lights + 2 artifact/portal lights = 18 total, 0 shadows)
            Light[] allLights = root.GetComponentsInChildren<Light>(includeInactive: true);
            Assert.AreEqual(18, allLights.Length, "Total scene light count must remain exactly 18.");
            foreach (Light l in allLights)
            {
                Assert.AreEqual(LightShadows.None, l.shadows, $"Light '{l.gameObject.name}' must not cast shadows.");
            }

            // Both elevator bays retain exact closed bi-parting doors (X = 3.14, Z = 5.75 ± 0.37 and 7.85 ± 0.37, solid colliders)
            Transform elev = GameObject.Find("Hotel_Elevator_Core")?.transform;
            Assert.IsNotNull(elev);
            Transform bay1S = elev.Find("Hotel_ElevatorBay_1_DoorLeafS");
            Transform bay2N = elev.Find("Hotel_ElevatorBay_2_DoorLeafN");
            Assert.IsNotNull(bay1S);
            Assert.IsNotNull(bay2N);
            Assert.Less(Vector3.Distance(bay1S.position, new Vector3(3.14f, BaseY + 1.10f, 5.38f)), 0.01f);
            Assert.Less(Vector3.Distance(bay2N.position, new Vector3(3.14f, BaseY + 1.10f, 8.22f)), 0.01f);
            Assert.IsNotNull(bay1S.GetComponent<BoxCollider>());
            Assert.IsNotNull(bay2N.GetComponent<BoxCollider>());

            // IP-05 branded objects intact
            Transform reality = root.transform.Find("Backrooms_RealityBridgeObjects");
            Assert.IsNotNull(reality.Find("RealityBridge_AlexaCornerStand"));
            Assert.IsNotNull(reality.Find("RealityBridge_DiarioJuarezIpad"));
            Assert.IsNotNull(reality.Find("RealityBridge_LaptopWorkstation"));

            // World-context isolation check remains enforced
            GameObject tempTr = new GameObject("TempWorldContextCheck");
            try
            {
                var tr = tempTr.AddComponent<SecretBackroomsTransitionController>();
                Assert.AreEqual(PlayerWorldContext.SurfaceCorridor, tr.CurrentWorldContext);
                Assert.IsFalse(FirstPersonPlayer.ShouldShowEquippedOjoDelUmbralBadge(tr, null, null, cursorLocked: true));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(tempTr);
            }

            Assert.IsTrue(BackroomsLevel1Builder.RunVerificationChecks(), "BackroomsLevel1Builder.RunVerificationChecks() must pass.");
        }

        [Test]
        public void AllHotelSignsAndRoomPlaques_HaveUprightNonMirroredRenderedMeshOrientation()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            (string objName, Vector3 expectedFacingNormal)[] signChecks =
            {
                ("Hotel_Sign_EntranceMarquee",      Vector3.right),
                ("Hotel_Sign_ReceptionBackdrop",    Vector3.back),
                ("Hotel_Sign_ElevatorsHeader",      Vector3.right),
                ("Hotel_Sign_ElevatorNotice",       Vector3.right),
                ("Hotel_Sign_GuestRoomsWayfinding", Vector3.right),
                ("Hotel_Sign_ServiceWing",          Vector3.right),
                ("Hotel_Sign_SWMaintenance",        Vector3.forward),
                ("Hotel_Room101_CorridorWall_RoomPlaque", Vector3.back),
                ("Hotel_Room102_CorridorWall_RoomPlaque", Vector3.back),
                ("Hotel_Room103_CorridorWall_RoomPlaque", Vector3.forward),
                ("Hotel_Room104_CorridorWall_RoomPlaque", Vector3.forward),
                ("Hotel_Room105_CorridorWall_RoomPlaque", Vector3.forward),
                ("Hotel_Room106_CorridorWall_RoomPlaque", Vector3.forward)
            };

            foreach (var check in signChecks)
            {
                GameObject go = GameObject.Find(check.objName);
                Assert.IsNotNull(go, $"Missing sign object '{check.objName}'.");
                MeshFilter mf = go.GetComponent<MeshFilter>();
                Assert.IsNotNull(mf);
                Mesh mesh = mf.sharedMesh;
                Assert.IsNotNull(mesh);

                Vector3[] verts = mesh.vertices;
                Vector3[] norms = mesh.normals;
                Vector2[] uvs = mesh.uv;
                int[] tris = mesh.triangles;

                // Inspect the triangle(s) on the actual rendered face pointing toward expectedFacingNormal
                bool foundFrontFace = false;
                Vector3 texRightSum = Vector3.zero;
                Vector3 texUpSum = Vector3.zero;
                for (int t = 0; t < tris.Length; t += 3)
                {
                    int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
                    Vector3 worldN = go.transform.TransformDirection(norms[i0]).normalized;
                    if (Vector3.Dot(worldN, check.expectedFacingNormal) < 0.9f) continue;

                    Vector3 p0 = go.transform.TransformPoint(verts[i0]);
                    Vector3 p1 = go.transform.TransformPoint(verts[i1]);
                    Vector3 p2 = go.transform.TransformPoint(verts[i2]);
                    Vector2 uv0 = uvs[i0], uv1 = uvs[i1], uv2 = uvs[i2];

                    Vector3 e1 = p1 - p0;
                    Vector3 e2 = p2 - p0;
                    Vector2 d1 = uv1 - uv0;
                    Vector2 d2 = uv2 - uv0;
                    float det = d1.x * d2.y - d1.y * d2.x;
                    if (Mathf.Abs(det) < 1e-6f) continue;

                    Vector3 dPdu = (e1 * d2.y - e2 * d1.y) / det;
                    Vector3 dPdv = (e2 * d1.x - e1 * d2.x) / det;
                    texRightSum += dPdu.normalized;
                    texUpSum += dPdv.normalized;
                    foundFrontFace = true;
                }

                Assert.IsTrue(foundFrontFace, $"No rendered face on '{check.objName}' points toward {check.expectedFacingNormal}.");
                Vector3 worldTexUp = texUpSum.normalized;
                Vector3 worldTexRight = texRightSum.normalized;
                // Looking at the sign along -expectedFacingNormal with camera Up = +Y, screen-right is Cross(+Y, -N) = Cross(N, +Y)
                Vector3 expectedScreenRight = Vector3.Cross(check.expectedFacingNormal, Vector3.up).normalized;

                Debug.Log($"[SignOrientationAudit] {check.objName}: euler={go.transform.eulerAngles} worldTexUp={worldTexUp} (dotUp={Vector3.Dot(worldTexUp, Vector3.up):F2}) worldTexRight={worldTexRight} (dotScreenRight={Vector3.Dot(worldTexRight, expectedScreenRight):F2})");
                Assert.Greater(Vector3.Dot(worldTexUp, Vector3.up), 0.95f,
                    $"Sign '{check.objName}' is UPSIDE DOWN on its rendered front face! worldTexUp={worldTexUp}, euler={go.transform.eulerAngles}");
                Assert.Greater(Vector3.Dot(worldTexRight, expectedScreenRight), 0.95f,
                    $"Sign '{check.objName}' is HORIZONTALLY MIRRORED on its rendered front face! worldTexRight={worldTexRight}, expectedRight={expectedScreenRight}, euler={go.transform.eulerAngles}");
            }
        }

        [Test]
        public void EnvironmentalIdentityMigration_IsIdempotentAndRejectsNonTargetScenes()
        {
            Scene scene = EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);
            Assert.IsTrue(scene.IsValid());

            GameObject root = GameObject.Find("BackroomsLevel1_Root");
            int beforeCount = root.GetComponentsInChildren<Transform>(includeInactive: true).Length;
            int beforeLights = root.GetComponentsInChildren<Light>(includeInactive: true).Length;

            BackroomsLevel1Builder.MigrateRemanenciaEnvironmentalIdentityInOpenScene(scene);

            Assert.AreEqual(beforeCount, root.GetComponentsInChildren<Transform>(includeInactive: true).Length,
                "Re-running environmental identity migration must not duplicate or remove any GameObjects.");
            Assert.AreEqual(beforeLights, root.GetComponentsInChildren<Light>(includeInactive: true).Length,
                "Re-running environmental identity migration must preserve exact light count.");

            EditorSceneManager.OpenScene(BackroomsLevel1Builder.ScenePath, OpenSceneMode.Single);

            Scene emptyScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Assert.Throws<InvalidOperationException>(
                () => BackroomsLevel1Builder.MigrateRemanenciaEnvironmentalIdentityInOpenScene(emptyScene),
                "Environmental identity migration must reject scenes other than BackroomsLevel1Scene.unity.");
        }
    }
}
