using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
namespace ScaredyCat.Editor
{
    public static partial class FirstPlayableBuilder
    {
        internal const string Content = "Assets/ScaredyCatContent";
        const string OwnerModel = "Assets/Models/Male_Rigged_T-Pose.fbx";
        static MovementSettings movement;
        static AttackSettings attacks;
        static RewardSettings rewards;
        static FeedbackSettings feedback;
        static GameObject playerPrefab, ownerPrefab, vasePrefab, vase2Prefab, movablePrefab;
        static Material cutMaterial, labMaterial, particleMaterial;

        [MenuItem("Scaredy Cat/Build or Rebuild First Playable")]
        public static void BuildAll()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string folder in new[] { "Settings", "Prefabs", "Meshes", "Materials", "Animations", "Navigation" })
                Directory.CreateDirectory(Content + "/" + folder);
            AssetDatabase.Refresh();
            ConfigureLayers();
            movement = Settings<MovementSettings>("Movement"); attacks = Settings<AttackSettings>("Attack");
            rewards = Settings<RewardSettings>("Rewards"); feedback = Settings<FeedbackSettings>("Feedback");
            cutMaterial = Material("Ceramic interior", new Color(.86f, .73f, .53f));
            labMaterial = Material("Lab clay", new Color(.24f, .31f, .34f));
            particleMaterial = Material("Impact sparks", new Color(1, .8f, .4f));
            ConfigureOwnerImports();
            playerPrefab = BuildPlayer(); ownerPrefab = BuildOwner();
            vasePrefab = BuildProp("Vase", "Assets/AssetStore/Prefabs/Decoration/Vase 1.prefab", true, .34f);
            vase2Prefab = BuildProp("Vase Tall", "Assets/AssetStore/Prefabs/Decoration/Vase 2.prefab", true, .42f);
            movablePrefab = BuildProp("Book", "Assets/AssetStore/Prefabs/Decoration/Simple Book.prefab", false, .12f);
            BuildApartment(); BuildLab();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/Apartment.unity", true), new EditorBuildSettingsScene("Assets/Scenes/MechanicsLab.unity", true) };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/Scenes/Apartment.unity");
            Debug.Log("SC_BUILD_OK: First playable, prefabs, baked navigation and MechanicsLab saved.");
        }
        static T Settings<T>(string name) where T : ScriptableObject
        {
            string path = Content + "/Settings/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (!asset) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
            return asset;
        }
        static Material Material(string name, Color color)
        {
            string path = Content + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", color); material.SetFloat("_Smoothness", .15f); AssetDatabase.CreateAsset(material, path);
            }
            return material;
        }
        static void ConfigureLayers()
        {
            var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = manager.FindProperty("layers");
            string[] names = { "Cat", "Mischief", "Owner", "Debris", "Paw" };
            for (int i = 0; i < names.Length; i++) layers.GetArrayElementAtIndex(i + 8).stringValue = names[i];
            manager.ApplyModifiedPropertiesWithoutUndo();
            Physics.IgnoreLayerCollision(8, 11, true); Physics.IgnoreLayerCollision(11, 11, true);
            for (int i = 0; i < 32; i++) Physics.IgnoreLayerCollision(12, i, true);
        }
        static void ConfigureOwnerImports()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(OwnerModel);
            importer.animationType = ModelImporterAnimationType.Human; importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.optimizeGameObjects = false; importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(OwnerModel).OfType<Avatar>().FirstOrDefault();
            if (!avatar || !avatar.isValid || !avatar.isHuman) throw new InvalidOperationException("Owner Humanoid avatar could not be created.");
            foreach (string suffix in new[] { "Idle", "Start Walking", "Standing Up", "Sitting", "Dancing" })
            {
                string path = "Assets/Animations/Male_Rigged_T-Pose@" + suffix + ".fbx";
                var clipImporter = (ModelImporter)AssetImporter.GetAtPath(path);
                clipImporter.animationType = ModelImporterAnimationType.Human;
                clipImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; clipImporter.sourceAvatar = avatar;
                clipImporter.optimizeGameObjects = false;
                var clips = clipImporter.defaultClipAnimations;
                if (clips.Length > 0)
                {
                    clips[0].name = suffix == "Start Walking" ? "Walk (provisional)" : suffix;
                    clips[0].loopTime = suffix == "Idle" || suffix == "Start Walking" || suffix == "Dancing";
                    clips[0].loopPose = clips[0].loopTime;
                    clips[0].lockRootRotation = true; clips[0].lockRootHeightY = true; clips[0].lockRootPositionXZ = true;
                    if (suffix == "Start Walking") { clips[0].firstFrame = 28; clips[0].lastFrame = 58; }
                    clipImporter.clipAnimations = clips;
                }
                clipImporter.SaveAndReimport();
            }
        }
        static AnimationClip Clip(string suffix) => AssetDatabase.LoadAllAssetsAtPath("Assets/Animations/Male_Rigged_T-Pose@" + suffix + ".fbx")
            .OfType<AnimationClip>().First(clip => !clip.name.StartsWith("__preview"));
        static AnimatorController OwnerController()
        {
            string path = Content + "/Animations/Owner.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing && existing.layers.Length > 0 && existing.layers[0].stateMachine.defaultState) return existing;
            if (existing) AssetDatabase.DeleteAsset(path);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            var tree = new BlendTree { name = "Idle Walk", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
            tree.AddChild(Clip("Idle"), 0); tree.AddChild(Clip("Start Walking"), 1.1f);
            AssetDatabase.AddObjectToAsset(tree, controller);
            var state = controller.layers[0].stateMachine.AddState("Locomotion"); state.motion = tree;
            controller.layers[0].stateMachine.defaultState = state;
            // Persist the entire graph before later model reimports can reload this asset.
            EditorUtility.SetDirty(tree); EditorUtility.SetDirty(state);
            EditorUtility.SetDirty(controller.layers[0].stateMachine); EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets(); return controller;
        }
        static GameObject Model(string path, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset) throw new FileNotFoundException(path);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.transform.SetParent(parent, false);
            foreach (var camera in model.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(camera.gameObject);
            foreach (var light in model.GetComponentsInChildren<Light>(true)) Object.DestroyImmediate(light.gameObject);
            return model;
        }
        static Bounds Bounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        static void NormalizeModel(GameObject model, float height)
        {
            var bounds = Bounds(model); model.transform.localScale *= height / Mathf.Max(.00001f, bounds.size.y);
            bounds = Bounds(model); model.transform.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }
        static void Layer(GameObject obj, int layer) { foreach (var t in obj.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer; }
        static GameObject SavePrefab(GameObject root, string name)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, (name == "Player" ? "Assets/Prefabs/Player.prefab" : Content + "/Prefabs/" + name + ".prefab")); Object.DestroyImmediate(root); return prefab;
        }
        [MenuItem("Scaredy Cat/Build macOS Development Player")]
        public static void BuildDesktop()
        {
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/Apartment.unity", "Assets/Scenes/MechanicsLab.unity" },
                locationPathName = "Builds/ScaredyCat.app", target = BuildTarget.StandaloneOSX, options = BuildOptions.Development });
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new InvalidOperationException("Desktop build failed: " + report.summary.result);
            Debug.Log("SC_DESKTOP_BUILD_OK: " + report.summary.totalSize);
        }
    }
}
