using UnityEngine;

namespace Ashveil
{
    public static class WorldBuilder
    {
        public static MeshCollider TerrainCol;
        public static Mesh TerrainMesh;
        public static float WorldSize = 10000f;
        public static float WaterHeight = 12f;

        public static void Build(Transform root)
        {
            BuildLights(root);
            BuildTerrain(root);
            BuildWater(root);
            ScatterRocks(root);
            BuildConcordBase(root, new Vector3(-220f, 0f, -80f));
            BuildTharneBase(root, new Vector3(520f, 0f, 480f));
        }

        static void BuildLights(Transform root)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.78f, 0.58f, 0.35f);
            RenderSettings.fogStartDistance = 900f;
            RenderSettings.fogEndDistance = 5200f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.85f, 0.7f, 0.45f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.4f, 0.28f);
            RenderSettings.ambientGroundColor = new Color(0.18f, 0.12f, 0.08f);
            var sky = Palette.Skybox();
            if (sky != null) RenderSettings.skybox = sky;

            var sun = New("TwinSunA", root);
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.88f, 0.65f);
            l.intensity = 1.15f;
            l.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(38f, 140f, 0f);

            var sun2 = New("TwinSunB", root);
            var l2 = sun2.AddComponent<Light>();
            l2.type = LightType.Directional;
            l2.color = new Color(1f, 0.55f, 0.28f);
            l2.intensity = 0.35f;
            sun2.transform.rotation = Quaternion.Euler(22f, 210f, 0f);
        }

        static void BuildTerrain(Transform root)
        {
            int res = 256;
            var verts = new Vector3[res * res];
            var cols = new Color[res * res];
            var uvs = new Vector2[res * res];
            var tris = new int[(res - 1) * (res - 1) * 6];
            float half = WorldSize * 0.5f;

            for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                float u = x / (float)(res - 1);
                float v = z / (float)(res - 1);
                float wx = Mathf.Lerp(-half, half, u);
                float wz = Mathf.Lerp(-half, half, v);
                float h = Height(wx, wz);
                int i = z * res + x;
                verts[i] = new Vector3(wx, h, wz);
                uvs[i] = new Vector2(u * 18f, v * 18f);
                float t = Mathf.InverseLerp(WaterHeight - 4f, 90f, h);
                cols[i] = Color.Lerp(new Color(0.35f, 0.28f, 0.16f), Palette.TerrainHigh, t);
                if (h < WaterHeight + 2f) cols[i] = Color.Lerp(new Color(0.28f, 0.32f, 0.18f), cols[i], 0.4f);
            }

            int tidx = 0;
            for (int z = 0; z < res - 1; z++)
            for (int x = 0; x < res - 1; x++)
            {
                int i = z * res + x;
                tris[tidx++] = i;
                tris[tidx++] = i + res;
                tris[tidx++] = i + 1;
                tris[tidx++] = i + 1;
                tris[tidx++] = i + res;
                tris[tidx++] = i + res + 1;
            }

            var mesh = new Mesh { name = "Kessara", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            TerrainMesh = mesh;

            var go = New("Terrain", root);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Palette.Colored(Color.white);
            var mc = go.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            TerrainCol = mc;
            go.isStatic = true;
        }

        public static float Height(float x, float z)
        {
            float n = Mathf.PerlinNoise((x + 2400) * 0.00038f, (z + 900) * 0.00038f);
            float n2 = Mathf.PerlinNoise((x + 80) * 0.00115f, (z + 200) * 0.00115f);
            float n3 = Mathf.PerlinNoise((x + 12) * 0.0038f, (z + 40) * 0.0038f);
            float ridge = 1f - Mathf.Abs(Mathf.PerlinNoise(x * 0.00022f, z * 0.00022f) * 2f - 1f);
            float h = n * 160f + n2 * 55f + n3 * 18f + ridge * 140f;
            float crater = Vector2.Distance(new Vector2(x, z), new Vector2(900, 1400));
            h += Mathf.Clamp01(1f - crater / 520f) * -55f;
            float edge = Mathf.Max(
                Mathf.InverseLerp(4200f, 4900f, Mathf.Abs(x)),
                Mathf.InverseLerp(4200f, 4900f, Mathf.Abs(z)));
            h -= edge * 90f;
            return Mathf.Max(2f, h);
        }

        public static Vector3 OnGround(float x, float z, float extraY = 0f)
        {
            return new Vector3(x, Height(x, z) + extraY, z);
        }

        static void BuildWater(Transform root)
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            water.transform.SetParent(root, false);
            water.transform.position = new Vector3(0f, WaterHeight, 0f);
            water.transform.localScale = Vector3.one * (WorldSize / 10f * 1.05f);
            Object.Destroy(water.GetComponent<Collider>());
            Palette.ApplyColor(water.GetComponent<Renderer>(), Palette.Water, true);
        }

        static void ScatterRocks(Transform root)
        {
            var rng = new System.Random(2419);
            var rocks = New("Rocks", root).transform;
            for (int i = 0; i < 140; i++)
            {
                float x = (float)(rng.NextDouble() * 2 - 1) * 4200f;
                float z = (float)(rng.NextDouble() * 2 - 1) * 4200f;
                var p = OnGround(x, z, 0f);
                if (p.y < WaterHeight + 4f) continue;
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Rock";
                rock.transform.SetParent(rocks, false);
                rock.transform.position = p;
                rock.transform.localScale = new Vector3(8 + rng.Next(18), 5 + rng.Next(16), 8 + rng.Next(18));
                rock.transform.rotation = Quaternion.Euler(0, rng.Next(360), rng.Next(12));
                Palette.ApplyColor(rock.GetComponent<Renderer>(), Palette.Rock);
            }
        }

        static void BuildConcordBase(Transform root, Vector3 approx)
        {
            var p = OnGround(approx.x, approx.z, 0f);
            Hangar("ConcordHangar", root, p + Vector3.right * 30f, Palette.ConcordHull, Palette.ConcordAccent, false);
            Pad(root, p, Palette.ConcordAccent);
            Tower("ConcordTower", root, p + new Vector3(-40, 0, 25), Faction.Concord, false);
        }

        static void BuildTharneBase(Transform root, Vector3 approx)
        {
            var p = OnGround(approx.x, approx.z, 0f);
            Hangar("TharneHangar", root, p, Palette.TharneHull, Palette.TharneAccent, false);
            Radar("TharneRadar", root, p + new Vector3(50, 0, -20), true);
            Turret("AA-1", root, p + new Vector3(80, 0, 40));
            Turret("AA-2", root, p + new Vector3(-70, 0, 55));
            Turret("AA-3", root, p + new Vector3(20, 0, -90));
            Turret("AA-4", root, p + new Vector3(-40, 0, -50));
            Tank("T-1", root, p + new Vector3(120, 0, 10));
            Tank("T-2", root, p + new Vector3(-100, 0, 80));
        }

        static void Pad(Transform root, Vector3 p, Color c)
        {
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Pad";
            pad.transform.SetParent(root, false);
            pad.transform.position = p + Vector3.up * 0.4f;
            pad.transform.localScale = new Vector3(48, 0.8f, 48);
            Palette.ApplyColor(pad.GetComponent<Renderer>(), Color.Lerp(c, Palette.Rock, 0.6f));
        }

        static void Hangar(string name, Transform root, Vector3 p, Color hull, Color accent, bool objective)
        {
            p = OnGround(p.x, p.z, 8f);
            var go = New(name, root);
            go.transform.position = p;
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(42, 16, 28);
            Palette.ApplyColor(body.GetComponent<Renderer>(), hull);
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.SetParent(go.transform, false);
            stripe.transform.localPosition = new Vector3(0, 8.2f, 0);
            stripe.transform.localScale = new Vector3(44, 1.2f, 6);
            Palette.ApplyColor(stripe.GetComponent<Renderer>(), accent);
            var unit = go.AddComponent<CombatUnit>();
            unit.DisplayName = objective ? "Ангар Тарна" : "Ангар Конкорда";
            unit.Faction = objective ? Faction.Tharne : Faction.Concord;
            unit.Kind = UnitKind.Structure;
            unit.MaxHealth = 220;
            unit.Health = 220;
            unit.Airborne = false;
            unit.Radius = 22f;
            unit.Objective = objective;
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(42, 16, 28);
        }

        public static GameObject Fighter(string name, Transform root, Vector3 pos, Quaternion rot, Faction faction, bool player)
        {
            var go = New(name, root);
            go.transform.SetPositionAndRotation(pos, rot);
            bool fed = faction == Faction.Concord;
            Color hull = fed ? Palette.ConcordHull : Palette.TharneHull;
            Color accent = fed ? Palette.ConcordAccent : Palette.TharneAccent;

            Part(go.transform, PrimitiveType.Capsule, new Vector3(0, 0, 0.4f), new Vector3(1.4f, 2.6f, 1.4f), Quaternion.Euler(90, 0, 0), hull);
            Part(go.transform, PrimitiveType.Cube, new Vector3(0, -0.15f, -1.6f), new Vector3(1.1f, 0.7f, 2.4f), Quaternion.identity, hull);
            Part(go.transform, PrimitiveType.Cube, new Vector3(0, 0.05f, 0.2f), fed ? new Vector3(7.4f, 0.12f, 1.8f) : new Vector3(6.2f, 0.18f, 2.4f), Quaternion.identity, hull);
            Part(go.transform, PrimitiveType.Cube, new Vector3(0, 0.4f, -2.3f), new Vector3(2.4f, 0.1f, 0.9f), Quaternion.identity, hull);
            Part(go.transform, PrimitiveType.Cube, new Vector3(0, 0.55f, 0.6f), new Vector3(1.05f, 0.55f, 1.6f), Quaternion.identity, new Color(0.15f, 0.35f, 0.45f, 0.7f));
            Part(go.transform, PrimitiveType.Cylinder, new Vector3(-1.1f, -0.25f, -2.1f), new Vector3(0.45f, 0.7f, 0.45f), Quaternion.Euler(90, 0, 0), accent);
            Part(go.transform, PrimitiveType.Cylinder, new Vector3(1.1f, -0.25f, -2.1f), new Vector3(0.45f, 0.7f, 0.45f), Quaternion.Euler(90, 0, 0), accent);

            var unit = go.AddComponent<CombatUnit>();
            unit.DisplayName = player ? "AX-7 «Wisp»" : (fed ? "AX-7" : "Перехватчик Тарна");
            unit.Faction = faction;
            unit.Kind = UnitKind.Fighter;
            unit.MaxHealth = player ? 140f : 70f;
            unit.Health = unit.MaxHealth;
            unit.IsPlayer = player;
            unit.Airborne = true;
            unit.Radius = 4.5f;
            unit.Regenerates = true;
            unit.RegenPerSec = player ? 3f : 2f;
            unit.Armor = player ? 0.12f : 0.05f;
            unit.Objective = !fed && !player;

            var col = go.AddComponent<CapsuleCollider>();
            col.direction = 2;
            col.radius = 1.6f;
            col.height = 7.2f;
            col.center = Vector3.zero;

            var rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            var craft = go.AddComponent<CraftController>();
            craft.Unit = unit;
            var muzzle = New("Muzzle", go.transform);
            muzzle.transform.localPosition = new Vector3(0, 0, 3.4f);
            craft.GunMuzzle = muzzle.transform;
            return go;
        }

        public static void Turret(string name, Transform root, Vector3 approx)
        {
            var p = OnGround(approx.x, approx.z, 1.5f);
            var go = New(name, root);
            go.transform.position = p;
            Part(go.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(2.4f, 1.2f, 2.4f), Quaternion.identity, Palette.TharneHull);
            Part(go.transform, PrimitiveType.Cube, new Vector3(0, 2.2f, 1.4f), new Vector3(0.45f, 0.45f, 4.2f), Quaternion.identity, Palette.TharneAccent);
            var unit = go.AddComponent<CombatUnit>();
            unit.DisplayName = "Зенитная установка";
            unit.Faction = Faction.Tharne;
            unit.Kind = UnitKind.Turret;
            unit.MaxHealth = 80;
            unit.Health = 80;
            unit.Airborne = false;
            unit.Radius = 4f;
            unit.Objective = false;
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(4, 5, 4);
            col.center = new Vector3(0, 1.5f, 0);
            go.AddComponent<GroundDefense>().Unit = unit;
        }

        public static void Tank(string name, Transform root, Vector3 approx)
        {
            var p = OnGround(approx.x, approx.z, 1.2f);
            var go = New(name, root);
            go.transform.position = p;
            Part(go.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(5.5f, 1.4f, 3.2f), Quaternion.identity, Palette.TharneHull);
            Part(go.transform, PrimitiveType.Cube, new Vector3(0, 1.3f, 0.4f), new Vector3(2.2f, 1.1f, 2.2f), Quaternion.identity, Palette.TharneAccent);
            var unit = go.AddComponent<CombatUnit>();
            unit.DisplayName = "Штурмовой танк";
            unit.Faction = Faction.Tharne;
            unit.Kind = UnitKind.Tank;
            unit.MaxHealth = 95;
            unit.Health = 95;
            unit.Airborne = false;
            unit.Radius = 4f;
            unit.Objective = false;
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(5.5f, 2.4f, 3.2f);
            go.AddComponent<GroundDefense>().Unit = unit;
        }

        public static void Radar(string name, Transform root, Vector3 approx, bool objective)
        {
            var p = OnGround(approx.x, approx.z, 6f);
            var go = New(name, root);
            go.transform.position = p;
            Part(go.transform, PrimitiveType.Cylinder, Vector3.zero, new Vector3(1.4f, 6f, 1.4f), Quaternion.identity, Palette.TharneHull);
            Part(go.transform, PrimitiveType.Sphere, new Vector3(0, 7.2f, 0), new Vector3(8f, 1.2f, 8f), Quaternion.identity, Palette.TharneAccent);
            var unit = go.AddComponent<CombatUnit>();
            unit.DisplayName = "Радар «Эмбер-Рич»";
            unit.Faction = Faction.Tharne;
            unit.Kind = UnitKind.Radar;
            unit.MaxHealth = 160;
            unit.Health = 160;
            unit.Airborne = false;
            unit.Radius = 8f;
            unit.Objective = objective;
            var col = go.AddComponent<CapsuleCollider>();
            col.height = 14f;
            col.radius = 4f;
        }

        public static void Tower(string name, Transform root, Vector3 approx, Faction faction, bool objective)
        {
            var p = OnGround(approx.x, approx.z, 10f);
            var go = New(name, root);
            go.transform.position = p;
            Part(go.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(6, 20, 6), Quaternion.identity, Palette.ConcordHull);
            var unit = go.AddComponent<CombatUnit>();
            unit.DisplayName = "Вышка Конкорда";
            unit.Faction = faction;
            unit.Kind = UnitKind.Structure;
            unit.MaxHealth = 180;
            unit.Health = 180;
            unit.Airborne = false;
            unit.Radius = 8f;
            unit.Objective = objective;
            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(6, 20, 6);
        }

        static void Part(Transform parent, PrimitiveType type, Vector3 local, Vector3 scale, Quaternion rot, Color color)
        {
            var p = GameObject.CreatePrimitive(type);
            p.transform.SetParent(parent, false);
            p.transform.localPosition = local;
            p.transform.localRotation = rot;
            p.transform.localScale = scale;
            Object.Destroy(p.GetComponent<Collider>());
            Palette.ApplyColor(p.GetComponent<Renderer>(), color);
        }

        static GameObject New(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
