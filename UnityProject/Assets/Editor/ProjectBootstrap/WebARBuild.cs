using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SpatialTracking;
using UnityEngine.XR.Management;
using WebXR;

namespace Shield.Editor
{
    public static class WebARBuild
    {
        const string ScenePath = "Assets/Scenes/ViewerSpace.unity";
        [MenuItem("Shield/Recreate WebAR Scene")]
        public static void Configure()
        {
            EditorBuildSettings.RemoveConfigObject("com.unity.input.settings.actions");
            AssetDatabase.DeleteAsset("Assets/Scenes/SampleScene.unity");
            AssetDatabase.DeleteAsset("Assets/InputSystem_Actions.inputactions");
            AssetDatabase.DeleteAsset("Assets/Models/Shield.obj");
            AssetDatabase.DeleteAsset("Assets/Prefabs/Shield.prefab");
            AssetDatabase.DeleteAsset("Assets/Materials/Steel.mat");
            Directory.CreateDirectory("Assets/Scenes");
            Directory.CreateDirectory("Assets/Settings");
            Directory.CreateDirectory("Assets/Prefabs");
            Directory.CreateDirectory("Assets/Materials");
            var webxr = AssetDatabase.LoadAssetAtPath<WebXRSettings>("Assets/Settings/WebXRSettings.asset");
            if (!webxr) {
                webxr = ScriptableObject.CreateInstance<WebXRSettings>();
                AssetDatabase.CreateAsset(webxr, "Assets/Settings/WebXRSettings.asset");
            }
            webxr.ARRequiredReferenceSpace = WebXRSettings.ReferenceSpaceTypes.viewer;
            webxr.AROptionalFeatures = 0;
            webxr.VROptionalFeatures = 0;
            webxr.DisableXRDisplaySubsystem = true;
            webxr.AutoLoadWebXRManager = false;
            webxr.AutoLoadWebXRInputSystem = false;
            EditorBuildSettings.AddConfigObject("WebXR.Settings", webxr, true);
            EditorUtility.SetDirty(webxr);

            XRGeneralSettingsPerBuildTarget perTarget;
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out perTarget)) {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(perTarget, "Assets/Settings/XRGeneralSettings.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, perTarget, true);
            }
            if (!perTarget.HasSettingsForBuildTarget(BuildTargetGroup.WebGL))
                perTarget.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.WebGL);
            if (!perTarget.HasManagerSettingsForBuildTarget(BuildTargetGroup.WebGL))
                perTarget.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.WebGL);
            var general = perTarget.SettingsForBuildTarget(BuildTargetGroup.WebGL);
            general.InitManagerOnStart = true;
            if (!XRPackageMetadataStore.AssignLoader(general.Manager, "WebXR.WebXRLoader", BuildTargetGroup.WebGL))
                throw new Exception("Cannot assign WebXRLoader");
            EditorUtility.SetDirty(general);
            EditorUtility.SetDirty(general.Manager);
            EditorUtility.SetDirty(perTarget);

            PlayerSettings.companyName = "AR Shield";
            PlayerSettings.productName = "Sunny Kitty AR";
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.WebGL.template = "PROJECT:WebXRTemplate";
            // GitHub Pages does not let us configure Content-Encoding per file.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;
            PlayerSettings.WebGL.threadsSupport = false;
            PlayerSettings.WebGL.initialMemorySize = 128;
            PlayerSettings.WebGL.maximumMemorySize = 512;
            PlayerSettings.WebGL.dataCaching = false;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.WebGL, ManagedStrippingLevel.High);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
            QualitySettings.antiAliasing = 2;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.vSyncCount = 0;

            var importer = (ModelImporter)AssetImporter.GetAtPath("Assets/Models/SunnyKitty.obj");
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importNormals = ModelImporterNormals.Import;
            importer.isReadable = false;
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.SaveAndReimport();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.7f, 0.77f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.32f, 0.37f, 0.42f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.18f, 0.22f);
            var manager = new GameObject("WebXRManager");
            manager.AddComponent<WebXRManager>();
            var rig = new GameObject("WebXRCameraSet");
            rig.transform.SetParent(manager.transform, false);
            var main = MakeCamera("Main Camera", rig.transform, false);
            main.tag = "MainCamera";
            main.gameObject.AddComponent<AudioListener>();
            // Same BiRP camera arrangement as De-Panther's WebXRCameraSet prefab.
            var driver = main.gameObject.AddComponent<TrackedPoseDriver>();
            driver.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
            driver.updateType = TrackedPoseDriver.UpdateType.Update;
            var left = MakeCamera("CameraVRL", main.transform, false);
            var right = MakeCamera("CameraVRR", main.transform, false);
            var arLeft = MakeCamera("CameraARL", main.transform, true);
            var arRight = MakeCamera("CameraARR", main.transform, true);
            left.enabled = right.enabled = arLeft.enabled = arRight.enabled = false;
            var cameraSet = main.gameObject.AddComponent<WebXRCamera>();
            var serialized = new SerializedObject(cameraSet);
            SetCamera(serialized, "cameraMain", main);
            SetCamera(serialized, "cameraL", left);
            SetCamera(serialized, "cameraR", right);
            SetCamera(serialized, "cameraARL", arLeft);
            SetCamera(serialized, "cameraARR", arRight);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            foreach (var hand in new[] { WebXRControllerHand.LEFT, WebXRControllerHand.RIGHT }) {
                var controller = new GameObject("WebXRController " + hand);
                controller.transform.SetParent(rig.transform, false);
                controller.AddComponent<WebXRController>().hand = hand;
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Kitty.mat");
            if (!material) {
                material = new Material(Shader.Find("ARShield/KittyUnlit"));
                AssetDatabase.CreateAsset(material, "Assets/Materials/Kitty.mat");
            }
            material.shader = Shader.Find("ARShield/KittyUnlit");
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Models/KittyBaseColor.png");
            EditorUtility.SetDirty(material);
            var modelRoot = new GameObject("Sunny Kitty");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/SunnyKitty.obj"));
            model.transform.SetParent(modelRoot.transform, false);
            // Original GLB's two root rotations cancel; preserve its Y-up pose.
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            var renderers = model.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) {
                bounds.Encapsulate(renderer.bounds);
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            float scale = 0.62f / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            model.transform.localScale *= scale;
            model.transform.localPosition -= bounds.center * scale;
            var prefab = PrefabUtility.SaveAsPrefabAsset(modelRoot, "Assets/Prefabs/SunnyKitty.prefab");
            UnityEngine.Object.DestroyImmediate(modelRoot);
            var anchor = new GameObject("Viewer Space — 1.2 m");
            anchor.transform.SetParent(manager.transform, false);
            anchor.transform.localPosition = new Vector3(0, 0, 1.2f);
            var follow = anchor.AddComponent<Shield.ViewerSpaceObject>();
            follow.cameraSet = cameraSet;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(anchor.transform, false);
            // Exercise actual follow logic with a translated and rotated camera.
            main.transform.SetPositionAndRotation(new Vector3(2, 1, -3), Quaternion.Euler(15, 55, 10));
            follow.SendMessage("LateUpdate");
            var viewport = main.WorldToViewportPoint(anchor.transform.position);
            if (Mathf.Abs(viewport.x - 0.5f) > 0.001f || Mathf.Abs(viewport.y - 0.5f) > 0.001f ||
                Mathf.Abs(viewport.z - follow.distance) > 0.001f)
                throw new Exception("Viewer-space object is not centered: " + viewport);
            main.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            follow.SendMessage("LateUpdate");
            Debug.Log("[Shield] Viewer-space position verified for a moved and rotated camera.");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] {new EditorBuildSettingsScene(ScenePath, true)};
            AssetDatabase.SaveAssets();
            Debug.Log("[Shield] Scene configured. Sunny Kitty bounds: " + bounds.size);
        }

        static Camera MakeCamera(string name, Transform parent, bool ar)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var camera = go.AddComponent<Camera>();
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 20;
            camera.fieldOfView = 55;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ar ? Color.clear : new Color(0.035f, 0.047f, 0.06f, 1);
            camera.allowHDR = false;
            camera.stereoTargetEye = StereoTargetEyeMask.None;
            return camera;
        }
        static void SetCamera(SerializedObject data, string name, Camera value) => data.FindProperty(name).objectReferenceValue = value;
        static void MakeLight(string name, Transform parent, Vector3 rotation, float intensity, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localRotation = Quaternion.Euler(rotation);
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            light.shadows = LightShadows.None;
        }

        [MenuItem("Shield/Build WebGL")]
        public static void Build()
        {
            Configure();
            string output = Path.GetFullPath(Path.Combine(Application.dataPath, "../../WebGL"));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] {ScenePath}, locationPathName = output, target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            Directory.CreateDirectory("../reports");
            File.WriteAllText("../reports/unity-build.json", JsonUtility.ToJson(new BuildEvidence {
                result = report.summary.result.ToString(), bytes = report.summary.totalSize,
                seconds = report.summary.totalTime.TotalSeconds, unity = Application.unityVersion,
                webxr = "0.25.0", compression = "Disabled (GitHub Pages)", decompressionFallback = false, threads = false,
                errors = report.summary.totalErrors, warnings = report.summary.totalWarnings
            }, true));
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("WebGL build failed");
            foreach (var obsolete in Directory.GetFiles(Path.Combine(output, "Build"), "*.gz")) File.Delete(obsolete);
            File.WriteAllText(Path.Combine(output, ".nojekyll"), "");
            File.Copy("../assets/SUNNY-KITTY-LICENSE.md", Path.Combine(output, "SUNNY-KITTY-LICENSE.md"), true);
            Debug.Log("[Shield] Build succeeded: " + report.summary.totalSize + " bytes.");
        }
        [Serializable] class BuildEvidence {
            public string result, unity, webxr, compression;
            public ulong bytes;
            public double seconds;
            public bool decompressionFallback, threads;
            public int errors, warnings;
        }
    }
}
