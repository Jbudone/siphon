using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Siphon.Editor
{
    public static class AssaultSceneBuilder
    {
        public const string ScenePath = "Assets/Siphon/Scenes/AssaultPlayground.unity";
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Siphon/Meshes");
            RenderSetup.Configure();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var forest = Material("Forest", new Color(0.2f, 0.35f, 0.27f));
            var valley = Material("Valley", new Color(0.34f, 0.4f, 0.36f));
            var snow = Material("Snow", new Color(0.75f, 0.86f, 0.9f));
            var blue = Material("Blue team", new Color(0.1f, 0.7f, 1));
            var red = Material("Red team", new Color(1, 0.22f, 0.1f));
            var dark = Material("Industrial", new Color(0.14f, 0.2f, 0.25f));
            var wood = Material("Trunk", new Color(0.28f, 0.2f, 0.14f));
            var leaves = Material("Leaves", new Color(0.08f, 0.26f, 0.2f));
            var ore = Material("Ferrite", new Color(0.9f, 0.65f, 0.17f));
            var rare = Material("Crystal", new Color(0.7f, 0.24f, 0.95f));
            MakeTerrain(new[] { forest, valley, snow });
            var war = new GameObject("War captain").AddComponent<WarDirector>();
            war.gameObject.AddComponent<StandaloneSmokeCheck>();
            war.gameObject.AddComponent<FieldSaveSystem>();
            war.blueMaterial = blue; war.redMaterial = red; war.darkMaterial = dark; war.wildMaterial = rare;
            war.blueSpawn = Point(-7, -178) + Vector3.up;
            war.redSpawn = Point(7, 175) + Vector3.up;
            BuildBase(war, Team.Blue, -185, blue, dark);
            BuildBase(war, Team.Red, 185, red, dark);
            for (int i = 0; i < 5; i++)
            {
                float z = -70 + i * 35;
                float x = i % 2 == 0 ? -40 : 40;
                Box("Grapple mast", Point(x, z) + Vector3.up * 18, new Vector3(3, 36, 3), dark);
                Box("Mast cap", Point(x, z) + Vector3.up * 36, new Vector3(7, 1, 7), i % 2 == 0 ? blue : red);
            }
            Box("Aerial bridge", new Vector3(0, 32, 0), new Vector3(84, 2, 8), dark);
            // Resource locations create safe supply routes and a contested center.
            float[,] deposits = { { -35, -135 }, { 35, -135 }, { -35, 135 }, { 35, 135 },
                { -65, -25 }, { 65, 25 }, { -150, -40 }, { -180, 45 }, { 160, 30 }, { 190, 65 } };
            for (int i = 0; i < deposits.GetLength(0); i++)
            {
                var node = GameObject.CreatePrimitive(i >= 8 ? PrimitiveType.Cube : PrimitiveType.Sphere);
                node.name = i >= 8 ? "Rare crystal deposit" : "Ferrite deposit";
                node.transform.position = Point(deposits[i, 0], deposits[i, 1]) + Vector3.up;
                node.transform.localScale = new Vector3(3, 2.5f, 3);
                node.transform.rotation = Quaternion.Euler(0, i * 23, i >= 8 ? 20 : 0);
                node.GetComponent<Renderer>().sharedMaterial = i >= 8 ? rare : ore;
                var resource = node.AddComponent<ResourceNode>();
                resource.rare = i >= 8;
                resource.kind = i >= 8 ? ResourceKind.Crystal : ResourceKind.Ferrite;
                resource.capacity = resource.remaining = i >= 8 ? 25 : 60;
            }
            var random = new System.Random(442);
            for (int i = 0; i < 55; i++)
            {
                float x = -125 - (float)random.NextDouble() * 150;
                float z = -125 + (float)random.NextDouble() * 250;
                float height = 10 + (float)random.NextDouble() * 12;
                var root = new GameObject("Forest tree").transform;
                root.position = Point(x, z);
                WarDirector.Part(root, PrimitiveType.Cylinder, "Trunk", Vector3.up * height * 0.3f,
                    new Vector3(1.1f, height * 0.3f, 1.1f), wood);
                WarDirector.Part(root, PrimitiveType.Sphere, "Canopy", Vector3.up * height * 0.7f,
                    new Vector3(8, height * 0.7f, 8), leaves, false);
                var tree = root.gameObject.AddComponent<ResourceNode>();
                tree.kind = ResourceKind.Timber; tree.capacity = tree.remaining = 40;
            }
            for (int i = 0; i < 12; i++)
            {
                float x = -118 - (i % 3) * 8;
                float z = i < 6 ? -148 - (i / 3) * 14 : 148 + ((i - 6) / 3) * 14;
                var root = new GameObject("Supply grove").transform; root.position = Point(x, z);
                WarDirector.Part(root, PrimitiveType.Cylinder, "Trunk", Vector3.up * 3, new Vector3(1.1f, 3, 1.1f), wood);
                WarDirector.Part(root, PrimitiveType.Sphere, "Canopy", Vector3.up * 8, new Vector3(7, 7, 7), leaves, false);
                var tree = root.gameObject.AddComponent<ResourceNode>(); tree.kind = ResourceKind.Timber;
                tree.capacity = tree.remaining = 40;
            }
            for (int i = 0; i < 14; i++)
            {
                float x = 140 + (float)random.NextDouble() * 120;
                float z = -120 + (float)random.NextDouble() * 240;
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Ice ridge";
                rock.transform.position = Point(x, z) + Vector3.up * 3;
                rock.transform.localScale = new Vector3(8, 12, 8);
                rock.transform.rotation = Quaternion.Euler(0, i * 27, 15);
                rock.GetComponent<Renderer>().sharedMaterial = snow;
            }
            BuildCamp("Frost vault", Point(228, 92), snow, rare, dark, true);
            BuildCamp("Forest relay ruins", Point(-225, -20), wood, rare, dark, false);
            var player = new GameObject("Player");
            player.layer = 8;
            player.transform.position = war.blueSpawn;
            var controller = player.AddComponent<CharacterController>();
            controller.height = 2; controller.center = Vector3.up; controller.radius = 0.45f;
            controller.skinWidth = 0.04f; controller.minMoveDistance = 0; controller.stepOffset = 0.3f;
            var actor = player.AddComponent<Combatant>();
            actor.kind = EntityKind.Player; actor.team = Team.Blue;
            actor.maximumHealth = actor.health = 100; actor.aimOffset = Vector3.up * 1.2f;
            var camera = new GameObject("View").AddComponent<Camera>();
            camera.transform.SetParent(player.transform, false);
            camera.transform.localPosition = new Vector3(0, 1.7f, 0);
            camera.tag = "MainCamera";
            camera.fieldOfView = 100; camera.farClipPlane = 900; camera.nearClipPlane = 0.08f;
            camera.gameObject.AddComponent<AudioListener>();
            var rifle = AssetDatabase.LoadAssetAtPath<GameObject>(RifleAssetBuilder.PrefabPath);
            if (rifle != null)
            {
                var weapon = (GameObject)PrefabUtility.InstantiatePrefab(rifle, camera.transform);
                weapon.name = "First person weapon";
                weapon.transform.localPosition = new Vector3(0.28f, -0.24f, 0.48f);
            }
            else
            {
            WarDirector.Part(camera.transform, PrimitiveType.Cube, "First person weapon",
                new Vector3(0.28f, -0.24f, 0.48f), new Vector3(0.13f, 0.14f, 0.5f), dark, false);
            WarDirector.Part(camera.transform, PrimitiveType.Cube, "Weapon accent",
                new Vector3(0.28f, -0.15f, 0.5f), new Vector3(0.08f, 0.025f, 0.35f), blue, false);
            }
            var motor = player.AddComponent<PlaygroundPlayer>();
            motor.tuning = AssetDatabase.LoadAssetAtPath<MovementTuning>("Assets/Siphon/Settings/Movement.asset");
            motor.view = camera;
            player.AddComponent<PlayerCombat>();
            player.AddComponent<FieldHud>();
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.1f; sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(40, -35, 0);
            RenderSettings.ambientLight = new Color(0.52f, 0.6f, 0.67f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0.0018f; RenderSettings.fogColor = new Color(0.55f, 0.68f, 0.75f);
            var sky = AssetDatabase.LoadAssetAtPath<Material>("Assets/Siphon/Materials/Field sky.mat");
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, "Assets/Siphon/Materials/Field sky.mat");
            }
            sky.SetColor("_SkyTint", new Color(0.4f, 0.6f, 0.8f));
            sky.SetFloat("_AtmosphereThickness", 0.8f);
            RenderSettings.skybox = sky;
            QualitySettings.shadowDistance = 160;
            NavigationBaker.Bake();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Siphon assault scene generated successfully.");
        }

        private static void BuildCamp(string name, Vector3 center, Material stone, Material crystal, Material dark, bool boss)
        {
            var root = new GameObject(name).transform; root.position = center;
            for (int i = 0; i < 6; i++)
            {
                float angle = (i * 60 + 20) * Mathf.Deg2Rad;
                Vector3 point = center + new Vector3(Mathf.Cos(angle) * 21, 0, Mathf.Sin(angle) * 21);
                point = Point(point.x, point.z);
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cube); pillar.name = name + " ruin";
                pillar.transform.position = point + Vector3.up * (i % 2 == 0 ? 4 : 2);
                pillar.transform.localScale = new Vector3(4, i % 2 == 0 ? 8 : 4, 5);
                pillar.transform.rotation = Quaternion.Euler(0, i * 60, 0);
                pillar.GetComponent<Renderer>().sharedMaterial = stone;
            }
            var cache = GameObject.CreatePrimitive(PrimitiveType.Cube); cache.name = name + " crystal node";
            cache.transform.position = center + Vector3.up; cache.transform.localScale = Vector3.one * 2.5f;
            cache.transform.rotation = Quaternion.Euler(15, 35, 20); cache.GetComponent<Renderer>().sharedMaterial = crystal;
            var resource = cache.AddComponent<ResourceNode>(); resource.kind = ResourceKind.Crystal;
            resource.rare = true; resource.capacity = resource.remaining = boss ? 50 : 15;
            if (!boss) return;
            var warden = new GameObject("Frost Warden"); warden.layer = 8;
            warden.transform.position = center + Vector3.up * 2;
            var body = warden.AddComponent<SphereCollider>(); body.radius = 1.5f; body.center = Vector3.up;
            var actor = warden.AddComponent<Combatant>(); actor.team = Team.Wild; actor.kind = EntityKind.Creature;
            actor.maximumHealth = actor.health = 450; actor.aimOffset = Vector3.up;
            WarDirector.Part(warden.transform, PrimitiveType.Sphere, "Warden reactor", Vector3.up, Vector3.one * 2.5f, crystal, false);
            for (int i = -1; i <= 1; i += 2)
            {
                WarDirector.Part(warden.transform, PrimitiveType.Cube, "Cannon pod", new Vector3(i * 1.7f, 1, 0), new Vector3(1, 1, 2.8f), dark, false);
                WarDirector.Part(warden.transform, PrimitiveType.Cube, "Armor fin", new Vector3(i * 2.2f, 1.4f, -0.5f), new Vector3(0.25f, 2.3f, 2), stone, false);
            }
            var brain = warden.AddComponent<WardenBrain>(); brain.home = center;
        }
        private static void BuildBase(WarDirector war, Team team, float z, Material color, Material dark)
        {
            float heading = team == Team.Blue ? 0 : 180;
            var core = war.CreateStructure(EntityKind.Core, team, Point(0, z), heading, false).GetComponent<Combatant>();
            if (team == Team.Blue) war.blueCore = core; else war.redCore = core;
            war.CreateStructure(EntityKind.Generator, team, Point(12, z), heading, false);
            war.CreateStructure(EntityKind.Processor, team, Point(-10, z + (team == Team.Blue ? -5 : 5)), heading, false);
            float front = z + (team == Team.Blue ? 20 : -20);
            war.CreateStructure(EntityKind.Turret, team, Point(-12, front), heading, false);
            war.CreateStructure(EntityKind.Turret, team, Point(12, front), heading, false);
            war.CreateStructure(EntityKind.ForceField, team, Point(0, front + (team == Team.Blue ? 4 : -4)), heading, false);
            war.CreateStructure(EntityKind.Radar, team, Point(-14, z + (team == Team.Blue ? 5 : -5)), heading, false);
            war.CreateStructure(EntityKind.Trap, team, Point(18, z + (team == Team.Blue ? 28 : -28)), heading, false);
            Box(team + " rear bunker", Point(0, z + (team == Team.Blue ? -12 : 12)) + Vector3.up * 3,
                new Vector3(28, 6, 3), dark);
            Box(team + " west wall", Point(-20, z) + Vector3.up * 2.5f, new Vector3(3, 5, 24), dark);
            Mark(team + " base deck", Point(0, z) + Vector3.up * 0.04f, new Vector3(38, 0.05f, 32), dark);
            Mark(team + " front stripe", Point(0, z + (team == Team.Blue ? 16 : -16)) + Vector3.up * 0.08f,
                new Vector3(38, 0.05f, 0.5f), color);
            Mark(team + " side stripe", Point(19, z) + Vector3.up * 0.08f, new Vector3(0.5f, 0.05f, 32), color);
        }
        private static void Mark(string name, Vector3 position, Vector3 scale, Material material)
        {
            Box(name, position, scale, material);
            Object.DestroyImmediate(GameObject.Find(name).GetComponent<Collider>());
        }
        private static Vector3 Point(float x, float z) => new Vector3(x, Height(x, z), z);
        public static float Height(float x, float z)
        {
            float edge = Mathf.Abs(x);
            float ridge = 20 * Mathf.Exp(-Mathf.Pow((edge - 95) / 42, 2));
            float hills = (Mathf.Sin(z * 0.024f) + Mathf.Sin(x * 0.031f + z * 0.013f)) * 3;
            float centralFlat = Mathf.SmoothStep(0, 1, Mathf.Clamp01((edge - 25) / 50));
            float baseFlat = Mathf.Clamp01((Mathf.Abs(z) - 145) / 25) * (1 - Mathf.Clamp01((edge - 35) / 30));
            return Mathf.Lerp((ridge + hills) * centralFlat, 0, baseFlat);
        }
        private static void MakeTerrain(Material[] materials)
        {
            const int cells = 100;
            const float size = 600;
            var vertices = new Vector3[(cells + 1) * (cells + 1)];
            var uv = new Vector2[vertices.Length];
            var triangles = new System.Collections.Generic.List<int>[]
                { new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>(), new System.Collections.Generic.List<int>() };
            for (int z = 0; z <= cells; z++)
                for (int x = 0; x <= cells; x++)
                {
                    int i = z * (cells + 1) + x;
                    float px = x * size / cells - size * 0.5f, pz = z * size / cells - size * 0.5f;
                    vertices[i] = Point(px, pz); uv[i] = new Vector2(x, z);
                    if (x == cells || z == cells) continue;
                    var list = triangles[px < -110 ? 0 : px > 110 ? 2 : 1];
                    list.Add(i); list.Add(i + cells + 1); list.Add(i + 1);
                    list.Add(i + 1); list.Add(i + cells + 1); list.Add(i + cells + 2);
                }
            const string path = "Assets/Siphon/Meshes/Field.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            bool fresh = mesh == null;
            if (fresh) mesh = new Mesh { name = "Field terrain" }; else mesh.Clear();
            mesh.vertices = vertices; mesh.uv = uv; mesh.subMeshCount = 3;
            for (int i = 0; i < 3; i++) mesh.SetTriangles(triangles[i], i);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            if (fresh) AssetDatabase.CreateAsset(mesh, path); else EditorUtility.SetDirty(mesh);
            var ground = new GameObject("Rolling battlefield");
            ground.layer = 9;
            ground.AddComponent<MeshFilter>().sharedMesh = mesh;
            ground.AddComponent<MeshRenderer>().sharedMaterials = materials;
            ground.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
        private static Material Material(string name, Color color)
        {
            string path = "Assets/Siphon/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find(RenderSetup.LitShader)); AssetDatabase.CreateAsset(material, path); }
            material.color = color;
            material.SetFloat("_Smoothness", 0.15f);
            EditorUtility.SetDirty(material);
            return material;
        }
        private static void Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name; go.transform.position = position; go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
