using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MyVirtualCommunity.Editor
{
    public static class ZFightingDetector
    {
        private const string ScenePath = "Assets/Scenes/BorderCorridorScene.unity";

        public static void DetectOverlappingFloors()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) return;

            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            Debug.Log($"[ZFightingDetector] Total MeshRenderers in scene: {renderers.Length}");

            List<MeshRenderer> floorRenderers = new List<MeshRenderer>();
            foreach (var r in renderers)
            {
                var b = r.bounds;
                // Look for horizontal surfaces near Y = 0 to 0.5 in the bridge / plaza area (Z > 20)
                if (b.min.y >= -0.1f && b.max.y <= 0.6f && b.max.z >= 20f)
                {
                    floorRenderers.Add(r);
                    Debug.Log($"Floor object: '{r.gameObject.name}' (Parent: {r.transform.parent?.name}) " +
                              $"Bounds Center=({b.center.x:F2}, {b.center.y:F2}, {b.center.z:F2}) " +
                              $"Size=({b.size.x:F2}, {b.size.y:F2}, {b.size.z:F2}) " +
                              $"Y-Range=[{b.min.y:F3}, {b.max.y:F3}] " +
                              $"X-Range=[{b.min.x:F2}, {b.max.x:F2}] " +
                              $"Z-Range=[{b.min.z:F2}, {b.max.z:F2}]");
                }
            }

            // Check pairwise overlap
            for (int i = 0; i < floorRenderers.Count; i++)
            {
                for (int j = i + 1; j < floorRenderers.Count; j++)
                {
                    var b1 = floorRenderers[i].bounds;
                    var b2 = floorRenderers[j].bounds;

                    // If bounds intersect or have very close top surfaces
                    if (b1.Intersects(b2))
                    {
                        float yTopDiff = Mathf.Abs(b1.max.y - b2.max.y);
                        if (yTopDiff < 0.05f)
                        {
                            Debug.LogWarning($"[Z-FIGHTING POTENTIAL] '{floorRenderers[i].name}' and '{floorRenderers[j].name}' " +
                                             $"overlap with top Y diff = {yTopDiff:F4}m! " +
                                             $"Obj1: {floorRenderers[i].name} Ymax={b1.max.y:F3}, " +
                                             $"Obj2: {floorRenderers[j].name} Ymax={b2.max.y:F3}");
                        }
                    }
                }
            }
        }
    }
}

