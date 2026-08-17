using System.Collections.Generic;
using UnityEngine;

namespace Ashveil
{
    public static class WorldBuilder
    {
        public static MeshCollider TerrainCol;
        public static Mesh TerrainMesh;
        public static float WorldSize = 48000f;
        public static float WaterHeight = 12f;

        public static void Build(Transform root)
        {
            BuildLights(root);
            BuildTerrain(root);
            BuildWater(root);
            ScatterRocks(root);
            ScatterScrub(root);
            BuildConcordBase(root, new Vector3(-220f, 0f, -80f));
            BuildTharneBase(root, new Vector3(520f, 0f, 480f));
        }

        static void BuildLights(Transform root)
        {
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowResolution = ShadowResolution.High;
            QualitySettings.shadowCascades = 4;
            QualitySettings.shadowDistance = 2800f;
            QualitySettings.antiAliasing = 2;
            QualitySettings.pixelLightCount = 2;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.82f, 0.64f, 0.40f);
            RenderSettings.fogStartDistance = 2200f;
            RenderSettings.fogEndDistance = 14500f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.58f, 0.38f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.36f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.16f, 0.11f, 0.07f);
            var sky = Palette.Skybox();
            if (sky != null) RenderSettings.skybox = sky;

            var sun = New("TwinSunA", root);
            var l = sun.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(1f, 0.86f, 0.62f);
            l.intensity = 1.35f;
            l.shadows = LightShadows.Soft;
            l.shadowStrength = 0.78f;
            l.shadowBias = 0.04f;
            l.shadowNormalBias = 0.6f;
            sun.transform.rotation = Quaternion.Euler(42f, 148f, 0f);

            var sun2 = New("TwinSunB", root);
            var l2 = sun2.AddComponent<Light>();
            l2.type = LightType.Directional;
            l2.color = new Color(1f, 0.52f, 0.26f);
            l2.intensity = 0.18f;
            l2.shadows = LightShadows.None;
            sun2.transform.rotation = Quaternion.Euler(22f, 210f, 0f);
        }

        static void BuildTerrain(Transform root)
        {
            const float nearSize = 10000f;
            var nearMat = Palette.Terrain();
            var farMat = Palette.TerrainFar(nearSize * 0.5f - 20f);

            var inner = BuildHeightMesh("TerrainNear", nearSize, 512, 0f, 0.08f);
            var outer = BuildHeightMesh("TerrainFar", WorldSize, 280, 0f, 0f);

            SpawnTerrainChunk(root, inner, nearMat, true);
            SpawnTerrainChunk(root, outer, farMat, true);
            TerrainMesh = inner;
        }

        static void SpawnTerrainChunk(Transform root, Mesh mesh, Material mat, bool collider)
        {
            var go = New(mesh.name, root);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            mr.receiveShadows = true;
            if (collider)
            {
                var mc = go.AddComponent<MeshCollider>();
                mc.sharedMesh = mesh;
                if (TerrainCol == null) TerrainCol = mc;
            }
            go.isStatic = true;
        }

        static Mesh BuildHeightMesh(string name, float size, int res, float holeHalf, float yBias)
        {
            var verts = new Vector3[res * res];
            var uvs = new Vector2[res * res];
            float half = size * 0.5f;
            for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                float u = x / (float)(res - 1);
                float v = z / (float)(res - 1);
                float wx = Mathf.Lerp(-half, half, u);
                float wz = Mathf.Lerp(-half, half, v);
                int i = z * res + x;
                verts[i] = new Vector3(wx, Height(wx, wz) + yBias, wz);
                uvs[i] = new Vector2(wx, wz);
            }

            var tris = new List<int>((res - 1) * (res - 1) * 6);
            for (int z = 0; z < res - 1; z++)
            for (int x = 0; x < res - 1; x++)
            {
                int i = z * res + x;
                float cx = (verts[i].x + verts[i + 1].x) * 0.5f;
                float cz = (verts[i].z + verts[i + res].z) * 0.5f;
                if (holeHalf > 0f && Mathf.Abs(cx) < holeHalf && Mathf.Abs(cz) < holeHalf)
                    continue;
                tris.Add(i);
                tris.Add(i + res);
                tris.Add(i + 1);
                tris.Add(i + 1);
                tris.Add(i + res);
                tris.Add(i + res + 1);
            }

            var mesh = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public static float Height(float x, float z)
        {
            float n = Mathf.PerlinNoise((x + 2400) * 0.00038f, (z + 900) * 0.00038f);
            float n2 = Mathf.PerlinNoise((x + 80) * 0.00115f, (z + 200) * 0.00115f);
            float n3 = Mathf.PerlinNoise((x + 12) * 0.0038f, (z + 40) * 0.0038f);
            float ridge = 1f - Mathf.Abs(Mathf.PerlinNoise(x * 0.00022f, z * 0.00022f) * 2f - 1f);
            float h = n * 160f + n2 * 55f + n3 * 18f + ridge * 140f;
            h += Mathf.PerlinNoise((x + 3f) * 0.018f, (z + 9f) * 0.018f) * 4.2f;
            h += Mathf.PerlinNoise((x + 40f) * 0.055f, (z + 18f) * 0.055f) * 1.4f;
            float crater = Vector2.Distance(new Vector2(x, z), new Vector2(900, 1400));
            h += Mathf.Clamp01(1f - crater / 520f) * -55f;
            float half = WorldSize * 0.5f;
            float edge = Mathf.Max(
                Mathf.InverseLerp(half - 1800f, half, Mathf.Abs(x)),
                Mathf.InverseLerp(half - 1800f, half, Mathf.Abs(z)));
            h -= edge * 40f;
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
            for (int i = 0; i < 420; i++)
            {
                float x = (float)(rng.NextDouble() * 2 - 1) * 9000f;
                float z = (float)(rng.NextDouble() * 2 - 1) * 9000f;
                var p = OnGround(x, z, 0f);
                if (p.y < WaterHeight + 4f) continue;
                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rock.name = "Rock";
                rock.transform.SetParent(rocks, false);
                rock.transform.position = p;
                rock.transform.localScale = new Vector3(6 + rng.Next(22), 4 + rng.Next(14), 6 + rng.Next(22));
                rock.transform.rotation = Quaternion.Euler(rng.Next(18), rng.Next(360), rng.Next(18));
                Object.Destroy(rock.GetComponent<Collider>());
                Palette.ApplyLit(rock.GetComponent<Renderer>(), Color.Lerp(Palette.Rock, Palette.TerrainLow, (float)rng.NextDouble() * 0.45f));
            }
        }

        static void ScatterScrub(Transform root)
        {
            var rng = new System.Random(77);
            var field = New("Scrub", root);
            var variants = new[] { MakeBushMesh(1), MakeBushMesh(2), MakeBushMesh(3) };
            var batches = new List<Matrix4x4>[variants.Length];
            for (int i = 0; i < batches.Length; i++) batches[i] = new List<Matrix4x4>(400);

            for (int c = 0; c < 90; c++)
            {
                float cx = (float)(rng.NextDouble() * 2 - 1) * 5200f;
                float cz = (float)(rng.NextDouble() * 2 - 1) * 5200f;
                if (Mathf.PerlinNoise(cx * 0.0035f, cz * 0.0035f) < 0.42f) continue;
                int count = 5 + rng.Next(12);
                for (int i = 0; i < count; i++)
                {
                    float x = cx + (float)(rng.NextDouble() * 2 - 1) * 28f;
                    float z = cz + (float)(rng.NextDouble() * 2 - 1) * 28f;
                    var p = OnGround(x, z, 0f);
                    if (p.y < WaterHeight + 6f) continue;
                    float s = 0.85f + (float)rng.NextDouble() * 1.7f;
                    var rot = Quaternion.Euler(0f, rng.Next(360), 0f);
                    int vi = rng.Next(variants.Length);
                    batches[vi].Add(Matrix4x4.TRS(p, rot, Vector3.one * s));
                }
            }

            var leaf = Palette.Lit(new Color(0.27f, 0.32f, 0.14f));
            for (int i = 0; i < variants.Length; i++)
            {
                if (batches[i].Count == 0) continue;
                var batch = field.AddComponent<InstanceBatch>();
                batch.Mesh = variants[i];
                batch.Material = leaf;
                batch.Matrices = batches[i].ToArray();
            }
        }

        static Mesh _sphereMesh;
        static Mesh _cylMesh;

        static Mesh SphereMesh
        {
            get
            {
                if (_sphereMesh == null) _sphereMesh = GrabPrimitive(PrimitiveType.Sphere);
                return _sphereMesh;
            }
        }

        static Mesh CylMesh
        {
            get
            {
                if (_cylMesh == null) _cylMesh = GrabPrimitive(PrimitiveType.Cylinder);
                return _cylMesh;
            }
        }

        static Mesh GrabPrimitive(PrimitiveType type)
        {
            var go = GameObject.CreatePrimitive(type);
            var mesh = Object.Instantiate(go.GetComponent<MeshFilter>().sharedMesh);
            Object.Destroy(go);
            mesh.hideFlags = HideFlags.HideAndDontSave;
            return mesh;
        }

        static Mesh MakeBushMesh(int seed)
        {
            var rng = new System.Random(seed * 97 + 11);
            var verts = new List<Vector3>(512);
            var norms = new List<Vector3>(512);
            var tris = new List<int>(1024);
            AppendMesh(verts, norms, tris, CylMesh,
                Matrix4x4.TRS(new Vector3(0f, 0.32f, 0f), Quaternion.identity, new Vector3(0.14f, 0.34f, 0.14f)));

            int branches = 4 + rng.Next(3);
            for (int i = 0; i < branches; i++)
            {
                float yaw = i * 137.5f + (float)rng.NextDouble() * 20f;
                float pitch = 18f + (float)rng.NextDouble() * 28f;
                var rot = Quaternion.Euler(pitch, yaw, 0f);
                var pos = rot * new Vector3(0f, 0.55f, 0f) + Vector3.up * 0.25f;
                AppendMesh(verts, norms, tris, CylMesh,
                    Matrix4x4.TRS(pos, rot, new Vector3(0.05f, 0.28f + (float)rng.NextDouble() * 0.18f, 0.05f)));
            }

            int clumps = 12 + rng.Next(8);
            for (int i = 0; i < clumps; i++)
            {
                float ang = (float)rng.NextDouble() * 6.28318f;
                float rad = 0.12f + (float)rng.NextDouble() * 0.72f;
                float y = 0.55f + (float)rng.NextDouble() * 1.05f;
                var pos = new Vector3(Mathf.Cos(ang) * rad, y, Mathf.Sin(ang) * rad);
                float s = 0.22f + (float)rng.NextDouble() * 0.34f;
                var rot = Quaternion.Euler(rng.Next(50), rng.Next(360), rng.Next(50));
                AppendMesh(verts, norms, tris, SphereMesh,
                    Matrix4x4.TRS(pos, rot, new Vector3(s * 1.35f, s * 0.85f, s * 1.25f)));
            }

            var mesh = new Mesh { name = "Bush" + seed, hideFlags = HideFlags.HideAndDontSave };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AppendMesh(List<Vector3> verts, List<Vector3> norms, List<int> tris, Mesh src, Matrix4x4 m)
        {
            int baseIndex = verts.Count;
            var sv = src.vertices;
            var sn = src.normals;
            var st = src.triangles;
            var nrm = m.inverse.transpose;
            for (int i = 0; i < sv.Length; i++)
            {
                verts.Add(m.MultiplyPoint3x4(sv[i]));
                norms.Add(nrm.MultiplyVector(sn[i]).normalized);
            }
            for (int i = 0; i < st.Length; i++)
                tris.Add(baseIndex + st[i]);
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
            Palette.ApplyLit(pad.GetComponent<Renderer>(), Color.Lerp(c, Palette.Rock, 0.6f));
        }

        static void Hangar(string name, Transform root, Vector3 p, Color hull, Color accent, bool objective)
        {
            p = OnGround(p.x, p.z, 8f);
            var go = New(name, root);
            go.transform.position = p;
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(go.transform, false);
            body.transform.localScale = new Vector3(42, 16, 28);
            Palette.ApplyLit(body.GetComponent<Renderer>(), hull);
            var stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripe.transform.SetParent(go.transform, false);
            stripe.transform.localPosition = new Vector3(0, 8.2f, 0);
            stripe.transform.localScale = new Vector3(44, 1.2f, 6);
            Palette.ApplyLit(stripe.GetComponent<Renderer>(), accent);
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
            Palette.ApplyLit(p.GetComponent<Renderer>(), color);
        }

        static GameObject New(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }
    }

    public class InstanceBatch : MonoBehaviour
    {
        public Mesh Mesh;
        public Material Material;
        public Matrix4x4[] Matrices;
        static readonly Matrix4x4[] Chunk = new Matrix4x4[1023];

        void Update()
        {
            if (Mesh == null || Material == null || Matrices == null || Matrices.Length == 0) return;
            int i = 0;
            while (i < Matrices.Length)
            {
                int n = Mathf.Min(1023, Matrices.Length - i);
                System.Array.Copy(Matrices, i, Chunk, 0, n);
                Graphics.DrawMeshInstanced(Mesh, 0, Material, Chunk, n, null,
                    UnityEngine.Rendering.ShadowCastingMode.On, true);
                i += n;
            }
        }
    }
}
