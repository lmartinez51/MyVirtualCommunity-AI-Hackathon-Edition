using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    public static class KioskElevationInspector
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        [MenuItem("MyVirtualCommunity/Inspect Production Kiosk Elevations")]
        public static void InspectElevations()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Failed to open {ScenePath}");
                return;
            }

            GameObject kioskGo = GameObject.Find("Border_Information_Kiosk");
            if (kioskGo == null)
            {
                Debug.LogError("Border_Information_Kiosk not found in scene!");
                return;
            }

            Debug.Log("=== PRODUCTION KIOSK ELEVATION AUDIT ===");
            Debug.Log($"Kiosk Root: LocalPos={kioskGo.transform.localPosition}, WorldPos={kioskGo.transform.position}, WorldRot={kioskGo.transform.eulerAngles}");

            Transform desk = kioskGo.transform.Find("Kiosk_Desk");
            if (desk != null)
            {
                float bottomY = desk.position.y - desk.lossyScale.y / 2f;
                float topY = desk.position.y + desk.lossyScale.y / 2f;
                Debug.Log($"Kiosk Desk: LocalPos={desk.localPosition}, Scale={desk.localScale}, WorldPos={desk.position}, WorldTopY={topY:F4}, WorldBottomY={bottomY:F4}");
            }

            Transform countertop = kioskGo.transform.Find("Kiosk_Countertop");
            if (countertop != null)
            {
                float bottomY = countertop.position.y - countertop.lossyScale.y / 2f;
                float topY = countertop.position.y + countertop.lossyScale.y / 2f;
                Debug.Log($"Kiosk Countertop: LocalPos={countertop.localPosition}, Scale={countertop.localScale}, WorldPos={countertop.position}, WorldTopY={topY:F4}, WorldBottomY={bottomY:F4}");
            }

            Transform mat = kioskGo.transform.Find("Attendant_Mat");
            if (mat != null)
            {
                float bottomY = mat.position.y - mat.lossyScale.y / 2f;
                float topY = mat.position.y + mat.lossyScale.y / 2f;
                Debug.Log($"Attendant Mat: LocalPos={mat.localPosition}, Scale={mat.localScale}, WorldPos={mat.position}, WorldTopY={topY:F4}, WorldBottomY={bottomY:F4}");
            }

            Transform elena = kioskGo.transform.Find("ElenaRamos_Attendant");
            if (elena != null)
            {
                Debug.Log($"Elena Instance: LocalPos={elena.localPosition}, LocalRot={elena.localEulerAngles}, WorldPos={elena.position}");
                
                // Inspect bones / mesh bounds if present
                var smr = elena.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr != null)
                {
                    Debug.Log($"Elena SMR Bounds: Center={smr.bounds.center}, Min={smr.bounds.min}, Max={smr.bounds.max}");
                }

                // Check bones
                var head = FindChildRecursive(elena.transform, "Head");
                if (head != null) Debug.Log($"Elena Head Bone: WorldPos={head.position}");

                var spine = FindChildRecursive(elena.transform, "Spine2") ?? FindChildRecursive(elena.transform, "Spine1");
                if (spine != null) Debug.Log($"Elena Spine/Chest Bone: WorldPos={spine.position}");
            }

            // Also check sidewalk underneath
            RaycastHit hit;
            if (Physics.Raycast(new Vector3(kioskGo.transform.position.x, 5.0f, kioskGo.transform.position.z), Vector3.down, out hit, 10.0f))
            {
                Debug.Log($"Sidewalk/Ground below Kiosk: Hit={hit.collider.name}, HitWorldY={hit.point.y:F4}");
            }
            else
            {
                Debug.Log("Raycast below Kiosk: No collider hit; checking sidewalk directly.");
                var sidewalk = GameObject.Find("East_Sidewalk") ?? GameObject.Find("Sidewalk_East");
                if (sidewalk != null) Debug.Log($"Sidewalk: WorldPos={sidewalk.transform.position}, TopY={sidewalk.transform.position.y + sidewalk.transform.lossyScale.y / 2f:F4}");
            }

            Debug.Log("=== END KIOSK ELEVATION AUDIT ===");
        }

        private static Transform FindChildRecursive(Transform parent, string name)
        {
            if (parent.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return parent;
            foreach (Transform child in parent)
            {
                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}

