using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using WebXR;

namespace Shield.Editor
{
    public static class PlacementValidation
    {
        public static void Run(SurfacePlacement placement, Camera camera)
        {
            var checks = new List<string>();
            var rig = placement.referenceSpace;
            var oldPosition = rig.position;
            var oldRotation = rig.rotation;
            rig.SetPositionAndRotation(new Vector3(1, 0.2f, -2), Quaternion.Euler(0, 35, 0));
            placement.SetARMode(true);
            placement.SelectSurface();
            Check(!placement.IsPlaced && !placement.model.gameObject.activeSelf, "No placement without a valid hit", checks);
            var hit = new WebXRHitPoseData {available = true, position = new Vector3(0.4f, -0.7f, 1), rotation = Quaternion.identity};
            placement.UpdateHit(hit);
            Check(placement.HasValidHit && placement.reticle.gameObject.activeSelf && !placement.IsPlaced,
                "Valid horizontal hit shows reticle without placing the model", checks);
            placement.SelectSurface();
            var expected = rig.TransformPoint(hit.position);
            Check(placement.IsPlaced && Vector3.Distance(placement.transform.position, expected) < 0.001f,
                "Tap places at hit pose in transformed local reference space", checks);
            var renderers = placement.model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Check(Mathf.Abs(bounds.min.y - expected.y) < 0.002f, "Model base rests on the detected surface", checks);
            var fixedPosition = placement.transform.position;
            var fixedRotation = placement.transform.rotation;
            camera.transform.localPosition = new Vector3(2, 1, -1);
            camera.transform.localRotation = Quaternion.Euler(10, -50, 0);
            placement.preview.SendMessage("LateUpdate");
            Check(Vector3.Distance(placement.transform.position, fixedPosition) < 0.001f &&
                Quaternion.Angle(placement.transform.rotation, fixedRotation) < 0.001f,
                "Placed model stays fixed when the viewer moves and rotates", checks);
            hit.position = new Vector3(-1, -0.4f, 2);
            placement.UpdateHit(hit);
            Check(Vector3.Distance(placement.transform.position, fixedPosition) < 0.001f,
                "Scanning another surface does not move an already placed model", checks);
            placement.SelectSurface();
            expected = rig.TransformPoint(hit.position);
            Check(Vector3.Distance(placement.transform.position, expected) < 0.001f, "Another tap repositions the same model", checks);
            hit.rotation = Quaternion.Euler(90, 0, 0);
            placement.UpdateHit(hit);
            placement.SelectSurface();
            Check(!placement.HasValidHit && !placement.reticle.gameObject.activeSelf &&
                Vector3.Distance(placement.transform.position, expected) < 0.001f, "Wall hit rejected; invalid tap preserves placement", checks);
            hit.rotation = Quaternion.identity; hit.available = false;
            placement.UpdateHit(hit);
            Check(!placement.HasValidHit && placement.IsPlaced, "Lost tracking keeps the existing placement", checks);
            placement.SetARMode(true);
            Check(!placement.IsPlaced && !placement.model.gameObject.activeSelf && !placement.HasValidHit,
                "A new AR session clears old placement and stale hits", checks);
            rig.SetPositionAndRotation(oldPosition, oldRotation);
            camera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            placement.SetARMode(false);
            placement.preview.SendMessage("LateUpdate");
            var viewport = camera.WorldToViewportPoint(placement.transform.position);
            Check(placement.model.gameObject.activeSelf && placement.preview.followViewer &&
                Mathf.Abs(viewport.x - 0.5f) < 0.001f && Mathf.Abs(viewport.y - 0.5f) < 0.001f,
                "Leaving AR restores centered desktop preview", checks);
            Directory.CreateDirectory("../reports");
            File.WriteAllText("../reports/placement-checks.json", JsonUtility.ToJson(new Evidence {
                result = "Passed", checks = checks.ToArray(), scope = "Unity scene logic with simulated XR hit poses; not a physical-device AR test"
            }, true));
            Debug.Log("[Shield] Placement checks passed: " + checks.Count);
        }
        static void Check(bool valid, string name, List<string> checks)
        {
            if (!valid) throw new Exception("Placement validation failed: " + name);
            checks.Add(name);
        }
        [Serializable] class Evidence { public string result, scope; public string[] checks; }
    }
}
