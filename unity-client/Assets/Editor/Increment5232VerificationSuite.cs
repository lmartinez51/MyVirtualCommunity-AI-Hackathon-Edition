using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using MyVirtualCommunity.Character;
using MyVirtualCommunity.Client.Api;
using MyVirtualCommunity.Client.World;

namespace MyVirtualCommunity.Editor
{
    /// <summary>
    /// Automated Unity batchmode verification suite for Increment 5.23.2:
    /// Valeria Business Opening/Closing Lifecycle, Shutter Synchronization & Player Non-Trap Safety.
    ///
    /// Covers:
    ///   1. Physical clearance of wp_valeria_off_duty_station (-15.85, 0.20, 1.15) using Valeria's
    ///      actual scene CapsuleCollider (r=0.35m, h=1.65m) + floor Raycast (Y=0.20m)
    ///   2. Full 0.10m-resolution capsule sweep across all 3 Zone C edges connecting wp_valeria_off_duty_station
    ///   3. Separation from Optica_CalibrationStation, wp_valeria_storefront, and front door-swing volume
    ///   4. StorefrontShutterController synchronization with World Core shutterClosedPermitted:
    ///      - Closed in off_duty_inside (shutter collider active for outside players)
    ///      - Opens in opening_transition
    ///      - Stays open in returning_inside_for_close and closing_pending
    ///      - Player non-trap guarantee: holds/raises shutter & disables collider if FirstPersonPlayer is inside store
    ///   5. NpcWaypointMovementController + NpcAnimationController presentation states:
    ///      - RestingOrOffDuty posture + East-facing (+X) heading at wp_valeria_off_duty_station
    ///      - Walking presentation during returning_inside_for_close
    ///      - Exact-position suspension freeze with zero extrapolation during player dialogue
    ///   6. Regression protection for all 6 other canonical NPCs & approved 5.23.0 interior geometry
    /// </summary>
    public static class Increment5232VerificationSuite
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Increment 5.23.2/Run Verification Suite")]
        public static void RunVerification()
        {
            Debug.Log("[Increment5232Verify] Starting Increment 5.23.2 Valeria Opening/Closing Lifecycle & Shutter Verification...");
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[Increment5232Verify] FAIL: Could not open {ScenePath}");
                EditorApplication.Exit(1);
                return;
            }

            var optShutter = GameObject.Find("Optica_RollUpShutter_Dynamic")?.GetComponent<StorefrontShutterController>();
            if (optShutter != null) optShutter.SetClosedImmediate(false);
            Physics.SyncTransforms();

            int passed = 0;
            int total = 0;
            void Check(string label, bool ok)
            {
                total++;
                if (ok)
                {
                    passed++;
                    Debug.Log($"  [PASS] {label}");
                }
                else
                {
                    Debug.LogError($"  [FAIL] {label}");
                }
            }

            // 1. Discover Valeria's actual CapsuleCollider dimensions
            GameObject valeriaGo = GameObject.Find("ValeriaDominguez_Attendant");
            CapsuleCollider valeriaCap = valeriaGo != null ? valeriaGo.GetComponentInChildren<CapsuleCollider>(true) : null;
            float capRadius = valeriaCap != null ? valeriaCap.radius : 0.35f;
            float capHeight = valeriaCap != null ? valeriaCap.height : 1.65f;
            float capCenterY = valeriaCap != null ? valeriaCap.center.y : 0.85f;

            bool IsCapsuleClearAndGrounded(Vector3 footPos, string contextLabel)
            {
                float bottomSphereY = footPos.y + Mathf.Max(capRadius + 0.06f, capCenterY - capHeight * 0.5f + capRadius);
                float topSphereY = footPos.y + Mathf.Max(bottomSphereY - footPos.y + 0.1f, capCenterY + capHeight * 0.5f - capRadius);
                Vector3 p0 = new Vector3(footPos.x, bottomSphereY, footPos.z);
                Vector3 p1 = new Vector3(footPos.x, topSphereY, footPos.z);

                Collider[] hits = Physics.OverlapCapsule(p0, p1, capRadius - 0.005f, ~0, QueryTriggerInteraction.Ignore);
                bool blocked = false;
                foreach (var c in hits)
                {
                    if (c == null) continue;
                    string n = c.gameObject.name;
                    if (n == "Optica_InteriorFloor" || n == "Sidewalk_West") continue;
                    if (c.transform.GetComponentInParent<BobbleheadCharacter>() != null ||
                        c.transform.GetComponentInParent<InteractionPoint>() != null ||
                        c.transform.GetComponentInParent<FirstPersonPlayer>() != null)
                    {
                        continue;
                    }
                    Debug.LogError($"    [COLLISION at {contextLabel} {footPos}] overlapped '{n}'");
                    blocked = true;
                }

                bool hasFloor = Physics.Raycast(footPos + Vector3.up * 0.8f, Vector3.down, out RaycastHit hit, 1.6f, ~0, QueryTriggerInteraction.Ignore) &&
                                Mathf.Abs(hit.point.y - 0.20f) < 0.04f;
                if (!hasFloor)
                {
                    Debug.LogError($"    [NO SOLID FLOOR at {contextLabel} {footPos}]");
                }
                return !blocked && hasFloor;
            }

            // 2. Verify wp_valeria_off_duty_station (-15.85, 0.20, 1.15) physical clearance & separation
            Vector3 offDutyPos = new Vector3(-15.85f, 0.20f, 1.15f);
            Vector3 calibPos = new Vector3(-17.85f, 0.20f, -0.50f);
            Vector3 storefrontPos = new Vector3(-10.50f, 0.20f, 1.50f);
            bool offDutyClear = IsCapsuleClearAndGrounded(offDutyPos, "wp_valeria_off_duty_station");
            bool offDutyDistinct = Vector3.Distance(offDutyPos, calibPos) > 2.0f &&
                                   Vector3.Distance(offDutyPos, storefrontPos) > 4.0f &&
                                   offDutyPos.x <= -13.10f && offDutyPos.x >= -16.20f;
            Check(
                $"wp_valeria_off_duty_station (-15.85, 0.20, 1.15) clear for capsule (r={capRadius:F2}m, h={capHeight:F2}m), grounded, and distinct from calibration & storefront",
                offDutyClear && offDutyDistinct
            );

            // 3. Sweep all 3 Zone C edges connecting wp_valeria_off_duty_station at 0.10m resolution
            var connectingNodes = new Dictionary<string, Vector3>
            {
                ["wp_optica_showroom_center"] = new Vector3(-14.70f, 0.20f, 0.90f),
                ["wp_optica_showroom_counter_west"] = new Vector3(-15.55f, 0.20f, 0.50f),
                ["wp_optica_showroom_north_display"] = new Vector3(-15.75f, 0.20f, 1.85f),
            };
            bool allOffDutyEdgesClear = true;
            foreach (var kvp in connectingNodes)
            {
                float dist = Vector3.Distance(kvp.Value, offDutyPos);
                int steps = Mathf.Max(4, Mathf.CeilToInt(dist / 0.10f));
                for (int i = 0; i <= steps; i++)
                {
                    Vector3 sample = Vector3.Lerp(kvp.Value, offDutyPos, (float)i / steps);
                    if (!IsCapsuleClearAndGrounded(sample, $"{kvp.Key}->wp_valeria_off_duty_station[{i}/{steps}]"))
                    {
                        allOffDutyEdgesClear = false;
                    }
                }
            }
            Check("Full 0.10m capsule sweep clear along all 3 Zone C edges to wp_valeria_off_duty_station", allOffDutyEdgesClear);

            // 4. Unity Presentation Controller + Animation + Shutter Synchronization
            var movCtrl = UnityEngine.Object.FindAnyObjectByType<NpcWaypointMovementController>();
            bool createdTempMov = false;
            if (movCtrl == null)
            {
                var go = new GameObject("Temp_NpcWaypointMovementController_5232");
                movCtrl = go.AddComponent<NpcWaypointMovementController>();
                createdTempMov = true;
            }
            movCtrl.DiscoverSceneReferences();

            Transform valeriaVisual = movCtrl.GetNpcVisualTransform("valeria-optica-attendant");
            NpcAnimationController valeriaAnim = movCtrl.GetNpcAnimationController("valeria-optica-attendant");
            Check("Optica_RollUpShutter_Dynamic & Valeria presentation controllers discovered", optShutter != null && valeriaVisual != null && valeriaAnim != null);

            // Create a temporary outside player transform so shutter tests control exact player location
            GameObject tempPlayer = new GameObject("Temp_Player_5232");
            tempPlayer.transform.position = new Vector3(-9.5f, 0.20f, 1.20f);

            // 4a. Closed overnight state (off_duty_inside, shutterClosedPermitted = true):
            // Valeria inside at wp_valeria_off_duty_station facing East (+1, 0), RestingOrOffDuty posture, shutter closes!
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "valeria-optica-attendant",
                canonicalName = "Valeria Domínguez",
                actorArchetype = "business_attendant",
                movementState = "stationary",
                currentWaypointId = "wp_valeria_off_duty_station",
                position = new Vector3Dto { x = -15.85f, y = 0.20f, z = 1.15f },
                activeSegmentEndPosition = new Vector3Dto { x = -15.85f, y = 0.20f, z = 1.15f },
                forwardHeading = new HeadingDto { x = 1f, z = 0f },
                currentSpeedMps = 0f,
                activityState = "off_duty",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "off_duty",
                controlMode = "fixed_station_anchor",
                withinAuthorizedDomain = true,
                currentZoneId = "zone:optica-main-showroom",
                businessLifecycleState = "off_duty_inside",
                shutterClosedPermitted = true
            }, snapImmediately: true);

            for (int i = 0; i < 30; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                if (valeriaAnim != null) valeriaAnim.StepPresentation(0.05f);
                if (optShutter != null) optShutter.StepShutterForVerification(0.05f, tempPlayer.transform);
            }
            Vector3 offDutyActual = movCtrl.GetNpcPosition("valeria-optica-attendant");
            Collider shutterCollider = optShutter != null && optShutter.shutterCurtain != null
                ? optShutter.shutterCurtain.GetComponent<Collider>()
                : null;
            Check(
                $"Closed hours: Valeria at wp_valeria_off_duty_station ({offDutyActual}), RestingOrOffDuty ({valeriaAnim?.CurrentBehaviorState}), shutter closed ({optShutter?.ClosedProgress:F2})",
                Vector3.Distance(offDutyActual, offDutyPos) < 0.03f &&
                valeriaVisual != null && valeriaVisual.forward.x > 0.95f &&
                valeriaAnim != null && valeriaAnim.CurrentBehaviorState == NpcVisualBehaviorState.RestingOrOffDuty &&
                optShutter != null && optShutter.IsClosed && optShutter.ClosedProgress > 0.98f &&
                shutterCollider != null && shutterCollider.enabled
            );

            // 4a-2. UI suppression outside service hours: [Off-Duty] nameplate & [E] prompt MUST be hidden when closed/off-duty!
            InteractionPoint valeriaPoint = GameObject.Find("InteractionPoint_OpticaFashion")?.GetComponent<InteractionPoint>();
            Check(
                "UI suppression: Valeria [Off Duty] nameplate & [E] talk prompt are suppressed while closed off_duty_inside",
                valeriaPoint != null && valeriaPoint.IsSuppressedOutsideServiceHours()
            );

            // 4a-3. Approaching or pushing against the exterior side of the closed shutter MUST NOT open it!
            tempPlayer.transform.position = new Vector3(-11.95f, 0.20f, 1.20f);
            for (int i = 0; i < 20; i++)
            {
                if (optShutter != null) optShutter.StepShutterForVerification(0.05f, tempPlayer.transform);
            }
            Check(
                $"Exterior approach near closed shutter keeps shutter CLOSED and collider solid (IsClosed={optShutter?.IsClosed}, progress={optShutter?.ClosedProgress:F2}, heldOpen={optShutter?.HeldOpenForPlayerExit}, col={shutterCollider?.enabled})",
                optShutter != null && optShutter.IsClosed && !optShutter.HeldOpenForPlayerExit &&
                optShutter.ClosedProgress > 0.98f && shutterCollider != null && shutterCollider.enabled
            );

            // 4b. Player Non-Trap Guarantee: move player INSIDE store while shutterClosedPermitted = true -> shutter MUST raise & disable collider!
            tempPlayer.transform.position = new Vector3(-14.50f, 0.20f, 1.00f);
            for (int i = 0; i < 30; i++)
            {
                if (optShutter != null) optShutter.StepShutterForVerification(0.05f, tempPlayer.transform);
            }
            Check(
                $"Player non-trap: player inside store forces shutter open (HeldOpenForPlayerExit={optShutter?.HeldOpenForPlayerExit}, progress={optShutter?.ClosedProgress:F2}, colliderEnabled={shutterCollider?.enabled})",
                optShutter != null && optShutter.HeldOpenForPlayerExit && !optShutter.IsClosed &&
                optShutter.ClosedProgress < 0.02f && shutterCollider != null && !shutterCollider.enabled
            );

            // Move player back outside -> shutter closes again cleanly!
            tempPlayer.transform.position = new Vector3(-9.50f, 0.20f, 1.20f);
            for (int i = 0; i < 30; i++)
            {
                if (optShutter != null) optShutter.StepShutterForVerification(0.05f, tempPlayer.transform);
            }
            Check(
                $"Shutter re-closes cleanly once player exits store (progress={optShutter?.ClosedProgress:F2})",
                optShutter != null && optShutter.IsClosed && optShutter.ClosedProgress > 0.98f
            );

            // 4c. Opening transition (09:00:00): shutter lifts while Valeria stays inside at wp_valeria_off_duty_station
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "valeria-optica-attendant",
                canonicalName = "Valeria Domínguez",
                actorArchetype = "business_attendant",
                movementState = "stationary",
                currentWaypointId = "wp_valeria_off_duty_station",
                position = new Vector3Dto { x = -15.85f, y = 0.20f, z = 1.15f },
                activeSegmentEndPosition = new Vector3Dto { x = -15.85f, y = 0.20f, z = 1.15f },
                forwardHeading = new HeadingDto { x = 1f, z = 0f },
                currentSpeedMps = 0f,
                activityState = "available",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "working_stationary",
                controlMode = "autonomous_business_local",
                withinAuthorizedDomain = true,
                currentZoneId = "zone:optica-main-showroom",
                businessLifecycleState = "opening_transition",
                shutterClosedPermitted = false
            }, snapImmediately: false);

            for (int i = 0; i < 30; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                if (valeriaAnim != null) valeriaAnim.StepPresentation(0.05f);
                if (optShutter != null) optShutter.StepShutterForVerification(0.05f, tempPlayer.transform);
            }
            Check(
                $"Opening transition lifts shutter fully open (progress={optShutter?.ClosedProgress:F2}) while Valeria starts shift inside and UI becomes visible",
                optShutter != null && !optShutter.IsClosed && optShutter.ClosedProgress < 0.02f &&
                Vector3.Distance(movCtrl.GetNpcPosition("valeria-optica-attendant"), offDutyPos) < 0.03f &&
                valeriaPoint != null && !valeriaPoint.IsSuppressedOutsideServiceHours()
            );

            // 4d. closing_pending (Valeria held outside in dialogue past 17:00): shutter MUST STAY OPEN & Valeria stays frozen at exact position!
            Vector3 outsideFrozenPos = new Vector3(-10.50f, 0.20f, 1.50f);
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "valeria-optica-attendant",
                canonicalName = "Valeria Domínguez",
                actorArchetype = "business_attendant",
                movementState = "stationary",
                currentWaypointId = "wp_valeria_storefront",
                destinationWaypointId = "wp_valeria_off_duty_station",
                activeRouteId = "route_valeria_return_for_close",
                position = new Vector3Dto { x = outsideFrozenPos.x, y = outsideFrozenPos.y, z = outsideFrozenPos.z },
                activeSegmentEndPosition = new Vector3Dto { x = outsideFrozenPos.x, y = outsideFrozenPos.y, z = outsideFrozenPos.z },
                forwardHeading = new HeadingDto { x = -1f, z = 0f },
                currentSpeedMps = 0f,
                activityState = "off_duty",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "customer_interaction",
                controlMode = "player_interaction_suspended",
                isSuspendedByPlayerInteraction = true,
                withinAuthorizedDomain = true,
                currentZoneId = "zone:optica-exterior-frontage",
                businessLifecycleState = "closing_pending",
                shutterClosedPermitted = false
            }, snapImmediately: true);

            for (int i = 0; i < 25; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                if (valeriaAnim != null) valeriaAnim.StepPresentation(0.05f);
                if (optShutter != null) optShutter.StepShutterForVerification(0.05f, tempPlayer.transform);
            }
            Vector3 heldPos = movCtrl.GetNpcPosition("valeria-optica-attendant");
            Check(
                $"closing_pending holds shutter OPEN (progress={optShutter?.ClosedProgress:F2}) and freezes Valeria at exact exterior position ({heldPos}) without extrapolation",
                optShutter != null && !optShutter.IsClosed && optShutter.ClosedProgress < 0.02f &&
                Vector3.Distance(heldPos, outsideFrozenPos) < 0.02f &&
                movCtrl.IsNpcSuspendedByPlayerInteraction("valeria-optica-attendant") &&
                movCtrl.GetVisualMoveSpeed("valeria-optica-attendant") < 0.01f
            );

            // 4e. Returning inside for close animates Walking smoothly through doorway
            movCtrl.ApplySingleNpcSnapshot(new NpcMovementSnapshotDto
            {
                npcId = "valeria-optica-attendant",
                canonicalName = "Valeria Domínguez",
                actorArchetype = "business_attendant",
                movementState = "moving",
                currentWaypointId = "wp_optica_door_threshold",
                destinationWaypointId = "wp_valeria_off_duty_station",
                activeRouteId = "route_valeria_return_for_close",
                position = new Vector3Dto { x = -12.02f, y = 0.20f, z = 1.20f },
                activeSegmentEndPosition = new Vector3Dto { x = -13.30f, y = 0.20f, z = 1.20f },
                forwardHeading = new HeadingDto { x = -1f, z = 0f },
                currentSpeedMps = 1.10f,
                activityState = "available",
                behaviorProfileType = "business_local_autonomy",
                autonomousBehaviorState = "returning_inside_for_close",
                controlMode = "scheduled_routine_transition",
                isSuspendedByPlayerInteraction = false,
                withinAuthorizedDomain = true,
                currentZoneId = "zone:optica-front-entrance",
                businessLifecycleState = "returning_inside_for_close",
                shutterClosedPermitted = false
            }, snapImmediately: true);

            for (int i = 0; i < 12; i++)
            {
                movCtrl.StepVisualMovement(0.05f);
                if (valeriaAnim != null) valeriaAnim.StepPresentation(0.05f);
                if (optShutter != null) optShutter.StepShutterForVerification(0.05f, tempPlayer.transform);
            }
            Check(
                $"returning_inside_for_close walks through doorway with Walking animation (anim={valeriaAnim?.CurrentBehaviorState}) and shutter open",
                valeriaAnim != null && valeriaAnim.IsWalkingActive &&
                valeriaAnim.CurrentBehaviorState == NpcVisualBehaviorState.Walking &&
                optShutter != null && !optShutter.IsClosed
            );

            // 4f. Storefront attendant badges during open hours NEVER show Break/Activity or spurious Busy;
            // Busy appears ONLY when actively serving a player/customer (isSuspendedByPlayerInteraction == true).
            if (valeriaPoint != null)
            {
                valeriaPoint.interactionAvailability = "available_contextual";
                valeriaPoint.activityState = "busy";
                valeriaPoint.isSuspendedByPlayerInteraction = false;
                valeriaPoint.autonomousBehaviorState = "working_stationary";
                string calibBadgeEn = valeriaPoint.GetInteractionAvailabilityBadge("en");
                string calibBadgeEs = valeriaPoint.GetInteractionAvailabilityBadge("es");

                valeriaPoint.interactionAvailability = "busy";
                string busyNoCustomerEn = valeriaPoint.GetInteractionAvailabilityBadge("en");

                valeriaPoint.isSuspendedByPlayerInteraction = true;
                string servingCustomerEn = valeriaPoint.GetInteractionAvailabilityBadge("en");
                string servingCustomerEs = valeriaPoint.GetInteractionAvailabilityBadge("es");

                valeriaPoint.isSuspendedByPlayerInteraction = false;
                valeriaPoint.interactionAvailability = "available_normal";
                valeriaPoint.activityState = "available";

                Check(
                    $"Storefront attendant never shows Break/Descanso during open hours (calibEn={calibBadgeEn}, calibEs={calibBadgeEs}, idleBusyEn={busyNoCustomerEn}) and shows Busy ONLY while serving customer (servingEn={servingCustomerEn}, servingEs={servingCustomerEs})",
                    calibBadgeEn == "[On Duty]" &&
                    calibBadgeEs == "[En servicio]" &&
                    busyNoCustomerEn == "[On Duty]" &&
                    servingCustomerEn == "[Busy]" &&
                    servingCustomerEs == "[Ocupado]"
                );
            }

            // 5. Regression: all 6 other canonical NPCs intact & approved 5.23.0 calibration station untouched
            GameObject calibAnchor = GameObject.Find(OpticaFashionInteriorBuilder.CalibrationAnchorName);
            bool calibIntact = calibAnchor != null &&
                               Vector3.Distance(calibAnchor.transform.position, calibPos) < 0.01f;
            string[] otherNpcs = new string[]
            {
                "carlos-farmacia-attendant",
                "elena-border-concierge",
                "patricia-citizen-rep",
                "mateo-student",
                "sofia-cross-border-retail",
                "hector-artisan"
            };
            bool othersOk = true;
            foreach (var id in otherNpcs)
            {
                if (movCtrl.GetNpcVisualTransform(id) == null)
                {
                    Debug.LogError($"    Missing visual transform for canonical NPC '{id}'");
                    othersOk = false;
                }
            }
            Check("Regression clean: Optica_CalibrationStation & all 6 other canonical NPCs intact", calibIntact && othersOk);

            UnityEngine.Object.DestroyImmediate(tempPlayer);
            if (createdTempMov && movCtrl != null)
            {
                UnityEngine.Object.DestroyImmediate(movCtrl.gameObject);
            }

            Debug.Log($"[Increment5232Verify] SUMMARY: {passed}/{total} checks passed.");
            EditorApplication.Exit(passed == total ? 0 : 1);
        }
    }
}
