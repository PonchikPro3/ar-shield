using UnityEngine;
using WebXR;

namespace Shield
{
    // WebXRCamera updates the XR pose before LateUpdate. Read the active eye(s)
    // each frame so the object stays in viewer-space across session transitions.
    [DefaultExecutionOrder(10000)]
    public sealed class ViewerSpaceObject : MonoBehaviour
    {
        public WebXRCamera cameraSet;
        [Min(0.2f)] public float distance = 1.2f;

        void Start() => FollowViewer();
        void LateUpdate() => FollowViewer();

        void FollowViewer()
        {
            if (!cameraSet) return;
            var manager = WebXRManager.Instance;
            var state = manager ? manager.XRState : WebXRState.NORMAL;
            var id = state == WebXRState.AR ? WebXRCamera.CameraID.LeftAR :
                state == WebXRState.VR ? WebXRCamera.CameraID.LeftVR : WebXRCamera.CameraID.Main;
            var eye = cameraSet.GetCamera(id).transform;
            Vector3 position = eye.position;
            Quaternion rotation = eye.rotation;
            if (state != WebXRState.NORMAL && manager.ViewsCount > 1) {
                var other = cameraSet.GetCamera(state == WebXRState.AR ?
                    WebXRCamera.CameraID.RightAR : WebXRCamera.CameraID.RightVR).transform;
                position = (position + other.position) * 0.5f;
                rotation = Quaternion.Slerp(rotation, other.rotation, 0.5f);
            }
            transform.SetPositionAndRotation(position + rotation * Vector3.forward * distance, rotation);
        }
    }
}
