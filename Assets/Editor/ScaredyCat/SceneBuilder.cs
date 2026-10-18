using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using Object = UnityEngine.Object;
namespace ScaredyCat.Editor
{
    public static partial class FirstPlayableBuilder
    {
        static void BuildApartment()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Apartment.unity");
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Scaredy Cat Systems" || root.name == "Player" || root.name == "Main Camera" || root.name == "HUD" || root.name == "EventSystem") Object.DestroyImmediate(root);
            var apartment = GameObject.Find("Apartment");
            if (!apartment) throw new InvalidOperationException("Apartment environment is missing.");
            var originalVases = apartment.GetComponentsInChildren<MeshFilter>().Where(f => f.name.StartsWith("Vase")).ToArray();
            var vasePositions = originalVases.Select(f => f.transform.position).Concat(
                apartment.GetComponentsInChildren<Transform>().Where(t => t.name.StartsWith("Mischief placement ")).Select(t => t.position)).ToArray();
            for (int i = 0; i < originalVases.Length; i++) Anchor("Mischief placement " + i, originalVases[i].transform.position, apartment.transform);
            foreach (var vase in originalVases) Object.DestroyImmediate(vase.gameObject);
            foreach (var filter in apartment.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.GetComponent<Collider>() && filter.sharedMesh)
                { var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh; }
            }
            var systems = new GameObject("Scaredy Cat Systems");
            var surface = BakeNavigation(systems, "Apartment");
            Physics.SyncTransforms();
            Vector3 spawn = OpenPoint(new Vector3(.15f, 0, -2.2f));
            Vector3 ownerPosition = OpenPoint(new Vector3(-.8f, 0, -5.3f));
            var positions = new List<Vector3>(); positions.AddRange(vasePositions);
            for (float z = -.3f; z >= -6.4f && positions.Count < 16; z -= 1.1f)
            for (float x = -2.2f; x <= 2.3f && positions.Count < 16; x += 1.1f)
            {
                if (!Physics.Raycast(new Vector3(x, 2.7f, z), Vector3.down, out var hit, 3, 1, QueryTriggerInteraction.Ignore)) continue;
                if (hit.point.y > 1.35f || hit.normal.y < .95f || Vector3.Distance(hit.point, spawn) < .8f || Vector3.Distance(hit.point, ownerPosition) < .8f) continue;
                Vector3 point = hit.point + Vector3.up * .025f;
                if (Physics.CheckBox(point + Vector3.up * .24f, new Vector3(.22f, .2f, .22f), Quaternion.identity, 1, QueryTriggerInteraction.Ignore)) continue;
                if (positions.Any(p => Vector3.Distance(p, point) < .6f)) continue;
                positions.Add(point);
            }
            if (positions.Count < 13) throw new InvalidOperationException("Could not place enough reachable apartment props: " + positions.Count);
            WireScene(systems, surface, spawn, ownerPosition, positions, false);
            var warmLight = new GameObject("Warm fill light"); warmLight.transform.SetParent(systems.transform); warmLight.transform.position = new Vector3(0, 2.5f, -3);
            var light = warmLight.AddComponent<Light>(); light.type = LightType.Point; light.range = 7; light.intensity = 2; light.color = new Color(1, .85f, .65f);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.5f, .52f, .55f);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
        }
        static void BuildLab()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var environment = new GameObject("Lab environment");
            Cube("Floor", new Vector3(0, -.15f, 0), new Vector3(20, .3f, 18), environment.transform);
            Cube("Back wall", new Vector3(0, 1.5f, 8.8f), new Vector3(20, 3, .2f), environment.transform);
            Cube("Left wall", new Vector3(-9.8f, 1.5f, 0), new Vector3(.2f, 3, 18), environment.transform);
            for (int i = 0; i < 4; i++) Cube("Jump step " + (i + 1), new Vector3(-6, .15f + i * .14f, -3 + i * 1.3f), new Vector3(1.5f, .3f + i * .28f, .9f), environment.transform);
            var slope = Cube("Walkable slope", new Vector3(-3.5f, .5f, .5f), new Vector3(2, .2f, 3), environment.transform); slope.transform.rotation = Quaternion.Euler(-20, 0, 0);
            Cube("Low ceiling", new Vector3(-6, 1.15f, 3), new Vector3(2.5f, .25f, 2), environment.transform);
            Cube("Attack occlusion wall", new Vector3(0, .7f, 2), new Vector3(1.5f, 1.4f, .15f), environment.transform);
            Cube("Breakable table", new Vector3(3, .4f, 2), new Vector3(2.5f, .8f, 1.3f), environment.transform);
            var sun = new GameObject("Directional Light").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.7f; sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientMode = AmbientMode.Flat; RenderSettings.ambientLight = new Color(.55f, .58f, .62f);
            var systems = new GameObject("Scaredy Cat Systems");
            var surface = BakeNavigation(systems, "MechanicsLab");
            var positions = new List<Vector3>();
            for (int i = 0; i < 14; i++) positions.Add(new Vector3(1.5f + i % 5 * 1.2f, .035f, -4 + i / 5 * 1.5f));
            positions[0] = new Vector3(2.4f, .83f, 2); positions[1] = new Vector3(3.5f, .83f, 2); positions[2] = new Vector3(0, .035f, 2.6f);
            WireScene(systems, surface, new Vector3(-6, .03f, -6), new Vector3(6, 0, 4), positions, true);
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/MechanicsLab.unity");
        }
        static NavMeshSurface BakeNavigation(GameObject root, string name)
        {
            var navObject = new GameObject("Owner navigation"); navObject.transform.SetParent(root.transform, false);
            var surface = navObject.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.layerMask = 1;
            surface.overrideVoxelSize = true; surface.voxelSize = .08f;
            Physics.SyncTransforms(); surface.BuildNavMesh();
            if (!surface.navMeshData) throw new InvalidOperationException("Navigation bake failed.");
            string path = Content + "/Navigation/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path); return surface;
        }
        static Vector3 OpenPoint(Vector3 desired)
        {
            desired.y = 0;
            for (float radius = 0; radius <= 3; radius += .4f)
            for (int i = 0; i < 12; i++)
            {
                Vector3 candidate = desired + new Vector3(Mathf.Cos(i * Mathf.PI / 6), 0, Mathf.Sin(i * Mathf.PI / 6)) * radius;
                if (!NavMesh.SamplePosition(candidate, out var hit, .5f, NavMesh.AllAreas) || Mathf.Abs(hit.position.y) > .3f) continue;
                if (!Physics.CheckCapsule(hit.position + Vector3.up * .32f, hit.position + Vector3.up * 1.4f, .28f, 1, QueryTriggerInteraction.Ignore)) return hit.position + Vector3.up * .025f;
            }
            throw new InvalidOperationException("No clear navigation position near " + desired);
        }
        static void WireScene(GameObject systems, NavMeshSurface surface, Vector3 spawn, Vector3 ownerPosition, List<Vector3> propPositions, bool lab)
        {
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab); player.transform.SetParent(systems.transform); player.transform.position = spawn;
            var cat = player.GetComponent<CatMotor>(); var input = player.GetComponent<GameInput>(); var paw = player.GetComponent<PawAttack>();
            var camera = player.GetComponentInChildren<Camera>(); var director = camera.GetComponent<CameraDirector>();
            var agro = systems.AddComponent<AgroMeter>(); agro.settings = rewards;
            var debris = systems.AddComponent<DebrisBudget>();
            var juice = systems.AddComponent<FeedbackDirector>(); juice.settings = feedback; juice.cat = cat; juice.agro = agro;
            juice.sparks = Particles(systems.transform); paw.feedback = juice; director.feedback = juice;
            var cutscenes = systems.AddComponent<CutsceneManager>(); cutscenes.input = input; cutscenes.cat = cat; cutscenes.director = director;
            var owner = (GameObject)PrefabUtility.InstantiatePrefab(ownerPrefab); owner.transform.SetParent(systems.transform); owner.transform.position = ownerPosition;
            var ragdoll = owner.GetComponent<OwnerRagdoll>(); ragdoll.agro = agro; ragdoll.feedback = juice;
            var brain = owner.GetComponent<OwnerBrain>();
            Vector3[] wishes = lab ? new[] { ownerPosition, new Vector3(7, 0, 6), new Vector3(4, 0, 6) } :
                new[] { ownerPosition, new Vector3(1.3f, 0, -4.7f), new Vector3(.8f, 0, -.7f), new Vector3(-1.3f, 0, -2.8f) };
            brain.wanderPoints = wishes.Select((p, i) => Anchor("Wander " + (i + 1), OpenPoint(p), systems.transform)).ToArray();
            var propsRoot = new GameObject("Mischief props").transform; propsRoot.SetParent(systems.transform, false);
            var props = new List<MischiefProp>();
            for (int i = 0; i < propPositions.Count; i++)
            {
                var source = i < 14 ? (i % 2 == 0 ? vasePrefab : vase2Prefab) : movablePrefab;
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(source); obj.name = source.name + " " + (i + 1); obj.transform.SetParent(propsRoot); obj.transform.position = propPositions[i];
                var prop = obj.GetComponent<MischiefProp>(); prop.agro = agro; prop.debris = debris; prop.feedback = juice; props.Add(prop);
            }
            var intro = new GameObject("Intro - an unfamiliar room").AddComponent<CutsceneSequence>(); intro.transform.SetParent(systems.transform); intro.waitForGrounded = false;
            var spline = new GameObject("Intro spline").AddComponent<SplineContainer>(); spline.transform.SetParent(systems.transform, false);
            Vector3[] path = { spawn + new Vector3(1.8f, 1.9f, -2.5f), spawn + new Vector3(1.5f, 1.45f, -1.7f), spawn + new Vector3(.85f, .75f, -.9f) };
            var curve = new Spline(); foreach (var point in path) curve.Add(new BezierKnot((float3)point), TangentMode.AutoSmooth); spline.Spline = curve;
            intro.shots = new[] { new CameraShot { label = "Bedroom fly-in", spline = spline, lookAt = cat.head, duration = 4, blendIn = .01f, caption = "New home. New human. Absolutely unacceptable." } };
            var focus = new GameObject("Focus - suspicious vase").AddComponent<CutsceneSequence>(); focus.transform.SetParent(systems.transform);
            var focusTarget = Anchor("Vase focus anchor", propPositions[0] + Vector3.up * .18f, props[0].transform);
            Vector3 towardCenter = (new Vector3(0, focusTarget.position.y, lab ? 0 : -3) - focusTarget.position).normalized;
            focus.shots = new[] { new CameraShot { label = "An obvious problem", pose = Anchor("Vase camera pose", focusTarget.position + towardCenter * .8f + Vector3.up * .35f, systems.transform), lookAt = focusTarget, duration = 2, caption = "That looks expensive. What a shame." } };
            var triggerObject = new GameObject("Vase narrative trigger"); triggerObject.transform.SetParent(systems.transform); triggerObject.transform.position = propPositions[0];
            var trigger = triggerObject.AddComponent<SphereCollider>(); trigger.radius = .65f; trigger.isTrigger = true;
            var narrative = triggerObject.AddComponent<NarrativeTrigger>(); narrative.manager = cutscenes; narrative.sequence = focus; narrative.target = props[0];
            Vector3 finalOwner = OpenPoint(lab ? new Vector3(4, 0, 5) : new Vector3(0, 0, -3.8f));
            var acceptanceOwner = Anchor("Acceptance owner", finalOwner, systems.transform);
            var acceptanceCat = Anchor("Acceptance cat", OpenPoint(finalOwner + new Vector3(.65f, 0, -.45f)), systems.transform);
            acceptanceOwner.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(acceptanceCat.position - acceptanceOwner.position, Vector3.up));
            var finalLook = Anchor("Acceptance focus", (acceptanceOwner.position + acceptanceCat.position) * .5f + Vector3.up * .55f, systems.transform);
            var end = new GameObject("Ending - at home").AddComponent<CutsceneSequence>(); end.transform.SetParent(systems.transform); end.waitForGrounded = false;
            end.shots = new[] { new CameraShot { label = "At home", pose = Anchor("Acceptance camera", finalOwner + new Vector3(1.3f, 1.1f, -2), systems.transform), lookAt = finalLook, duration = 4, blendIn = .01f, fieldOfView = 62, caption = "Okay. Maybe this is home." } };
            var flow = systems.AddComponent<GameFlow>(); flow.input = input; flow.agro = agro; flow.cat = cat; flow.owner = ragdoll;
            flow.cutscenes = cutscenes; flow.intro = intro; flow.ending = end; flow.acceptanceCat = acceptanceCat; flow.acceptanceOwner = acceptanceOwner; flow.feedback = juice; flow.mechanicsLab = lab;
            flow.hud = BuildHud(systems.transform, camera, flow, agro, paw, cutscenes);
            if (lab)
            {
                var controls = systems.AddComponent<MechanicsLabControls>(); controls.flow = flow; controls.attack = paw; controls.props = props.ToArray();
                controls.stations = new[] { Anchor("Movement", spawn, systems.transform), Anchor("Destruction", OpenPoint(propPositions[0] + Vector3.back), systems.transform), Anchor("Owner", OpenPoint(ownerPosition + Vector3.back * 1.2f), systems.transform) };
            }
            foreach (var component in player.GetComponentsInChildren<Component>(true)) if (component) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            foreach (var component in owner.GetComponents<Component>()) PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            foreach (var prop in props) PrefabUtility.RecordPrefabInstancePropertyModifications(prop);
            Debug.Log($"SC_SCENE: {(lab ? "MechanicsLab" : "Apartment")} spawn={spawn} owner={ownerPosition} props={props.Count} propReward={props.Sum(p => p.breakable ? 13 : 3)}");
        }
        static Transform Anchor(string name, Vector3 position, Transform parent)
        { var t = new GameObject(name).transform; t.SetParent(parent); t.position = position; return t; }
        static GameObject Cube(string name, Vector3 position, Vector3 size, Transform parent)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent); obj.transform.position = position; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = labMaterial; return obj;
        }
        static ParticleSystem Particles(Transform parent)
        {
            var obj = new GameObject("Impact particles"); obj.transform.SetParent(parent, false); var particles = obj.AddComponent<ParticleSystem>();
            var main = particles.main; main.playOnAwake = false; main.loop = false; main.startLifetime = .25f; main.startSpeed = 1.2f; main.startSize = .035f; main.gravityModifier = .6f; main.maxParticles = 64; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = particles.emission; emission.enabled = false; particles.GetComponent<ParticleSystemRenderer>().sharedMaterial = particleMaterial; return particles;
        }
    }
}
