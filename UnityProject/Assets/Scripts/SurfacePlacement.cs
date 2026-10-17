using UnityEngine;
using UnityEngine.Scripting;
using WebXR;

namespace Shield
{
    // Hit poses and the camera rig share a stable 'local' XR reference space.
    // Only an explicit XR select changes the placed object's world transform.
    [DefaultExecutionOrder(10001)]
    public sealed class SurfacePlacement : MonoBehaviour
    {
        public ViewerSpaceObject preview;
        public Transform referenceSpace;
        public Transform model;
        public Transform reticle;
        public float baseOffset;
        public bool IsPlaced { get; private set; }
        public bool HasValidHit { get; private set; }
        bool inAR;
        Vector3 hitPosition;
        float lastHitTime;

        void OnEnable()
        {
            WebXRManager.OnXRChange += HandleXRChange;
            WebXRManager.OnViewerHitTestUpdate += UpdateHit;
        }
        void OnDisable()
        {
            WebXRManager.OnXRChange -= HandleXRChange;
            WebXRManager.OnViewerHitTestUpdate -= UpdateHit;
            ClearHit();
        }
        void HandleXRChange(WebXRState state, int views, Rect left, Rect right) => SetARMode(state == WebXRState.AR);

        public void SetARMode(bool active)
        {
            inAR = active;
            IsPlaced = false;
            ClearHit();
            preview.followViewer = !active;
            model.localPosition = active ? Vector3.up * baseOffset : Vector3.zero;
            model.gameObject.SetActive(!active);
            if (WebXRManager.Instance && WebXRManager.Instance.XRState == WebXRState.AR) {
                if (active) WebXRManager.Instance.StartViewerHitTest();
                else WebXRManager.Instance.StopViewerHitTest();
            }
        }

        public void UpdateHit(WebXRHitPoseData pose)
        {
            var normal = referenceSpace.rotation * (pose.rotation * Vector3.up);
            // Floor/table placement: reject walls and steep slopes.
            if (!inAR || !pose.available || Vector3.Dot(normal, Vector3.up) < 0.85f ||
                !Finite(pose.position.x) || !Finite(pose.position.y) || !Finite(pose.position.z)) {
                ClearHit();
                return;
            }
            HasValidHit = true;
            lastHitTime = Time.unscaledTime;
            hitPosition = referenceSpace.TransformPoint(pose.position);
            reticle.SetPositionAndRotation(hitPosition + normal * 0.003f, referenceSpace.rotation * pose.rotation);
            reticle.gameObject.SetActive(true);
        }

        [Preserve] // Called by the XRSession 'select' bridge via Unity SendMessage.
        public void SelectSurface()
        {
            if (!inAR || !HasValidHit || Time.unscaledTime - lastHitTime > 0.5f) return;
            var camera = preview.cameraSet.GetCamera(WebXRCamera.CameraID.LeftAR).transform;
            var awayFromViewer = Vector3.ProjectOnPlane(hitPosition - camera.position, Vector3.up);
            var rotation = awayFromViewer.sqrMagnitude > 0.0001f ?
                Quaternion.LookRotation(awayFromViewer, Vector3.up) : Quaternion.identity;
            transform.SetPositionAndRotation(hitPosition, rotation);
            model.gameObject.SetActive(true);
            IsPlaced = true;
        }

        void LateUpdate()
        {
            if (inAR && HasValidHit && Time.unscaledTime - lastHitTime > 0.5f) ClearHit();
        }
        void OnApplicationPause(bool paused) { if (paused) ClearHit(); }
        void ClearHit()
        {
            HasValidHit = false;
            if (reticle) reticle.gameObject.SetActive(false);
        }
        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
