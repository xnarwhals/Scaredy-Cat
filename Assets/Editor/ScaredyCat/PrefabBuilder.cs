using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;
namespace ScaredyCat.Editor
{
    public static partial class FirstPlayableBuilder
    {
        static GameObject BuildPlayer()
        {
            var root = new GameObject("Player"); root.tag = "Player"; root.layer = 8;
            var body = root.AddComponent<Rigidbody>(); body.mass = 3; body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeRotation; body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var capsule = root.AddComponent<CapsuleCollider>(); capsule.height = .46f; capsule.radius = .17f; capsule.center = Vector3.up * .23f;
            string frictionPath = Content + "/Materials/Cat slide.physicMaterial";
            var friction = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(frictionPath);
            if (!friction) { friction = new PhysicsMaterial("Cat slide") { dynamicFriction = 0, staticFriction = 0, bounciness = 0, frictionCombine = PhysicsMaterialCombine.Minimum }; AssetDatabase.CreateAsset(friction, frictionPath); }
            capsule.sharedMaterial = friction;
            var input = root.AddComponent<GameInput>(); input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            var motor = root.AddComponent<CatMotor>(); motor.settings = movement; motor.input = input;
            motor.head = new GameObject("Head anchor").transform; motor.head.SetParent(root.transform, false); motor.head.localPosition = Vector3.up * .35f;
            var visual = new GameObject("Cat visual"); visual.transform.SetParent(root.transform, false);
            var model = Model("Assets/Models/bubber.fbx", visual.transform); NormalizeModel(model, .42f);
            foreach (var animator in model.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(animator);
            UpgradeMaterials(model); Layer(visual, 8);
            var presentation = root.AddComponent<CatPresentation>(); presentation.visual = visual.transform; presentation.motor = motor;
            var cameraObject = new GameObject("Cat World Camera"); cameraObject.transform.SetParent(root.transform, false); cameraObject.transform.localPosition = motor.head.localPosition;
            var camera = cameraObject.AddComponent<Camera>(); camera.tag = "MainCamera"; camera.nearClipPlane = .025f; camera.farClipPlane = 100; camera.fieldOfView = movement.fieldOfView;
            camera.allowMSAA = false; camera.allowHDR = false; camera.backgroundColor = new Color(.12f, .16f, .18f);
            cameraObject.AddComponent<AudioListener>();
            var director = cameraObject.AddComponent<CameraDirector>(); director.worldCamera = camera; director.cat = motor; director.catVisual = visual;
            var mount = new GameObject("Procedural paw").transform; mount.SetParent(camera.transform, false);
            mount.localPosition = new Vector3(.23f, -.25f, .39f); mount.localRotation = Quaternion.Euler(-15, 20, 57);
            var pawModel = Model("Assets/Models/CatPawHands.fbx", null); NormalizeModel(pawModel, .28f);
            pawModel.transform.SetParent(mount, false); UpgradeMaterials(pawModel); Layer(mount.gameObject, 12);
            foreach (var animator in pawModel.GetComponentsInChildren<Animator>(true)) Object.DestroyImmediate(animator);
            var attack = root.AddComponent<PawAttack>(); attack.input = input; attack.settings = attacks; attack.aim = camera.transform; attack.paw = mount;
            return SavePrefab(root, "Player");
        }
        static GameObject BuildOwner()
        {
            var root = new GameObject("Owner"); root.layer = 10;
            var model = Model(OwnerModel, root.transform); NormalizeModel(model, 1.7f); UpgradeMaterials(model);
            var animator = model.GetComponent<Animator>(); if (!animator) animator = model.AddComponent<Animator>();
            animator.avatar = AssetDatabase.LoadAllAssetsAtPath(OwnerModel).OfType<Avatar>().First(a => a.isHuman);
            animator.runtimeAnimatorController = OwnerController(); animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var agent = root.AddComponent<NavMeshAgent>(); agent.radius = .25f; agent.height = 1.7f; agent.speed = 1.1f; agent.acceleration = 5; agent.angularSpeed = 240; agent.stoppingDistance = .2f;
            agent.enabled = false; // OwnerBrain binds after scene NavMeshSurface instances have enabled.
            var standing = root.AddComponent<CapsuleCollider>(); standing.center = Vector3.up * .85f; standing.height = 1.7f; standing.radius = .25f;
            root.AddComponent<ImpulseAttribution>();
            var brain = root.AddComponent<OwnerBrain>(); var ragdoll = root.AddComponent<OwnerRagdoll>();
            brain.introDance = AssetDatabase.LoadAllAssetsAtPath("Assets/Animations/Male_Rigged_T-Pose@Dancing.fbx").OfType<AnimationClip>().First(clip => clip.name == "Dancing");
            brain.animator = animator; brain.ragdoll = ragdoll; ragdoll.animator = animator; ragdoll.brain = brain; ragdoll.standingCollider = standing;
            HumanBodyBones[] ids = { HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Head,
                HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand,
                HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand,
                HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot,
                HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot };
            HumanBodyBones[] ends = { HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Neck, HumanBodyBones.LastBone,
                HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.LastBone,
                HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, HumanBodyBones.LastBone,
                HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.LastBone,
                HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot, HumanBodyBones.LastBone };
            var bodies = new System.Collections.Generic.List<Rigidbody>();
            for (int i = 0; i < ids.Length; i++)
            {
                var bone = animator.GetBoneTransform(ids[i]); if (!bone) continue;
                var rb = bone.gameObject.AddComponent<Rigidbody>(); rb.mass = i == 0 ? 8 : i < 4 ? 5 : 2;
                rb.isKinematic = true; rb.interpolation = RigidbodyInterpolation.Interpolate; rb.linearDamping = .12f; rb.angularDamping = .5f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                rb.solverIterations = 12; rb.solverVelocityIterations = 4; rb.maxAngularVelocity = 15;
                bone.gameObject.AddComponent<ImpulseAttribution>(); bodies.Add(rb);
                var end = ends[i] == HumanBodyBones.LastBone ? null : animator.GetBoneTransform(ends[i]);
                float scale = Mathf.Max(.0001f, bone.lossyScale.x);
                if (end)
                {
                    var delta = bone.InverseTransformPoint(end.position); var col = bone.gameObject.AddComponent<CapsuleCollider>();
                    var abs = new Vector3(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
                    col.direction = abs.x > abs.y && abs.x > abs.z ? 0 : abs.y > abs.z ? 1 : 2;
                    col.radius = (i < 3 ? .12f : .065f) / scale; col.height = Mathf.Max(col.radius * 2, delta.magnitude * .95f); col.center = delta * .5f; col.enabled = false;
                }
                else
                { var col = bone.gameObject.AddComponent<SphereCollider>(); col.radius = (ids[i] == HumanBodyBones.Head ? .115f : .07f) / scale; col.enabled = false; }
            }
            foreach (var rb in bodies.Skip(1))
            {
                var ancestor = rb.transform.parent;
                while (ancestor && !ancestor.GetComponent<Rigidbody>()) ancestor = ancestor.parent;
                if (!ancestor) continue;
                var joint = rb.gameObject.AddComponent<CharacterJoint>(); joint.connectedBody = ancestor.GetComponent<Rigidbody>();
                joint.axis = Vector3.right; joint.swingAxis = Vector3.forward;
                joint.lowTwistLimit = new SoftJointLimit { limit = -40 }; joint.highTwistLimit = new SoftJointLimit { limit = 40 };
                joint.swing1Limit = new SoftJointLimit { limit = 55 }; joint.swing2Limit = new SoftJointLimit { limit = 35 };
                joint.enableProjection = true; joint.projectionDistance = .05f; joint.projectionAngle = 15; joint.enableCollision = false;
            }
            ragdoll.bones = bodies.ToArray(); ragdoll.hips = animator.GetBoneTransform(HumanBodyBones.Hips);
            Layer(root, 10); return SavePrefab(root, "Owner");
        }
        static GameObject BuildProp(string name, string sourcePath, bool breakable, float height)
        {
            var display = Model(sourcePath, null);
            var filter = display.GetComponentInChildren<MeshFilter>();
            var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(filter.sharedMesh)) as ModelImporter;
            if (importer && !importer.isReadable) { Object.DestroyImmediate(display); importer.isReadable = true; importer.SaveAndReimport(); display = Model(sourcePath, null); filter = display.GetComponentInChildren<MeshFilter>(); }
            NormalizeModel(display, height); UpgradeMaterials(display);
            var sourceMaterial = filter.GetComponent<Renderer>().sharedMaterial;
            var mesh = Object.Instantiate(filter.sharedMesh); var matrix = filter.transform.localToWorldMatrix;
            mesh.vertices = mesh.vertices.Select(matrix.MultiplyPoint3x4).ToArray(); mesh.RecalculateNormals(); mesh.RecalculateBounds();
            mesh = SaveMesh(mesh, name + " intact"); Object.DestroyImmediate(display);
            var root = new GameObject(name); root.layer = 9;
            var body = root.AddComponent<Rigidbody>(); body.mass = breakable ? .8f : .4f; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var collider = root.AddComponent<MeshCollider>(); collider.sharedMesh = mesh; collider.convex = true;
            root.AddComponent<ImpulseAttribution>(); var prop = root.AddComponent<MischiefProp>(); prop.breakable = breakable;
            var intact = new GameObject("Intact"); intact.transform.SetParent(root.transform, false);
            intact.AddComponent<MeshFilter>().sharedMesh = mesh; intact.AddComponent<MeshRenderer>().sharedMaterial = sourceMaterial; prop.intactVisual = intact;
            if (breakable)
            {
                var source = new GameObject("Fracture source"); source.AddComponent<MeshFilter>().sharedMesh = mesh;
                source.AddComponent<MeshRenderer>().sharedMaterial = sourceMaterial; source.AddComponent<Rigidbody>().mass = body.mass;
                var template = new GameObject("Fragment template"); template.AddComponent<MeshFilter>();
                template.AddComponent<MeshRenderer>().sharedMaterials = new[] { sourceMaterial, cutMaterial };
                template.AddComponent<MeshCollider>().convex = true;
                template.AddComponent<Rigidbody>(); template.AddComponent<ImpulseAttribution>();
                var broken = new GameObject("Broken"); broken.transform.SetParent(root.transform, false);
                var randomState = UnityEngine.Random.state; UnityEngine.Random.InitState(name == "Vase" ? 73 : 121);
                Fragmenter.Fracture(source, new FractureOptions { fragmentCount = 12, insideMaterial = cutMaterial, detectFloatingFragments = false }, template, broken.transform);
                UnityEngine.Random.state = randomState;
                int index = 0;
                foreach (var fragment in broken.GetComponentsInChildren<Rigidbody>())
                {
                    fragment.gameObject.layer = 11; fragment.mass = body.mass / broken.transform.childCount; fragment.isKinematic = true;
                    fragment.angularDamping = .3f; fragment.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    var mf = fragment.GetComponent<MeshFilter>(); mf.sharedMesh = SaveMesh(mf.sharedMesh, name + " fragment " + index++);
                    fragment.GetComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                }
                prop.brokenRoot = broken; broken.SetActive(false);
                Object.DestroyImmediate(source); Object.DestroyImmediate(template);
            }
            intact.layer = 9; return SavePrefab(root, name);
        }
        static Mesh SaveMesh(Mesh mesh, string name)
        {
            string path = Content + "/Meshes/" + name + ".asset"; mesh.name = name;
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing) { EditorUtility.CopySerialized(mesh, existing); EditorUtility.SetDirty(existing); Object.DestroyImmediate(mesh); return existing; }
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
        static void UpgradeMaterials(GameObject model)
        {
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                renderer.sharedMaterials = renderer.sharedMaterials.Select(source =>
                {
                    if (!source) return Material("Fallback warm white", new Color(.8f, .76f, .65f));
                    if (source.shader && source.shader.name.StartsWith("Universal Render Pipeline")) return source;
                    string path = Content + "/Materials/" + source.name.Replace('/', '_') + " URP.mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material) return material;
                    material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                    material.SetColor("_BaseColor", source.HasProperty("_Color") ? source.color : Color.white);
                    if (source.HasProperty("_MainTex")) material.SetTexture("_BaseMap", source.GetTexture("_MainTex"));
                    material.SetFloat("_Smoothness", .1f); AssetDatabase.CreateAsset(material, path); return material;
                }).ToArray();
            }
        }
    }
}
