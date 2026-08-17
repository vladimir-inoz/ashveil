using System.Collections.Generic;
using UnityEngine;
namespace Ashveil
{
    public enum GameState { Menu, Briefing, Playing, Win, Lose }

    [DefaultExecutionOrder(-400)]
    public class GameSession : MonoBehaviour
    {
        public static GameSession I;
        public GameState State = GameState.Menu;
        public bool Paused;
        public CraftController Player;
        public CombatUnit PlayerUnit;
        public readonly List<CombatUnit> Units = new List<CombatUnit>();
        public int AirLeft;
        public int GroundLeft;

        Transform _world;
        Transform _actors;
        CockpitHUD _hud;
        Camera _menuCam;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<GameSession>() != null) return;
            var go = new GameObject("AshveilGame");
            DontDestroyOnLoad(go);
            go.AddComponent<GameSession>();
        }

        void Awake()
        {
            I = this;
            ClearDefaultScene();
            _hud = gameObject.AddComponent<CockpitHUD>();
            EnsureMenuCamera();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        void Update()
        {
            if (State == GameState.Playing)
            {
                if (GameInput.KeyDown(KeyCode.Escape) || GameInput.KeyDown(KeyCode.P))
                {
                    Paused = !Paused;
                    if (Paused && Player != null) Player.ResetStick();
                }
                Time.timeScale = Paused ? 0f : 1f;
                UpdateCursor();
                RefreshCounts();
                if (AirLeft <= 0 && GroundLeft <= 0 && PlayerUnit != null && PlayerUnit.Alive)
                {
                    State = GameState.Win;
                    Paused = true;
                    Time.timeScale = 0f;
                    Cursor.visible = true;
                    _hud.Radio = "КОНКОРД: Отличная работа. Аванпост подавлен.";
                }
            }
            else
            {
                Time.timeScale = 1f;
                UpdateCursor();
            }
        }

        public static void UpdateCursor()
        {
            bool ui = I == null || I.State != GameState.Playing || I.Paused;
            Cursor.visible = ui;
            Cursor.lockState = ui ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public void StartBriefing()
        {
            State = GameState.Briefing;
        }

        public void StartMission()
        {
            try
            {
                BuildWorld();
                State = GameState.Playing;
                Paused = false;
                UpdateCursor();
                _hud.Radio = "КОНКОРД: Пилот, уничтожьте радар и воздушный патруль у аванпоста «Эмбер-Рич».";
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
                EnsureMenuCamera();
                State = GameState.Briefing;
                if (_hud != null) _hud.Radio = "ОШИБКА ЗАГРУЗКИ: " + e.Message;
            }
        }

        public void RestartMission()
        {
            TearDownWorld();
            StartMission();
        }

        public void ReturnToMenu()
        {
            TearDownWorld();
            State = GameState.Menu;
            Paused = false;
            EnsureMenuCamera();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        public void OnPlayerDestroyed()
        {
            State = GameState.Lose;
            Paused = true;
            Time.timeScale = 0f;
            UpdateCursor();
            _hud.Radio = "КОНКОРД: Скиф сбит. Миссия провалена.";
        }

        void BuildWorld()
        {
            TearDownWorld();
            if (_menuCam) _menuCam.enabled = false;

            _world = new GameObject("World").transform;
            _world.SetParent(transform, false);
            WorldBuilder.Build(_world);
            SoundBank.Init(_world);
            _actors = new GameObject("Actors").transform;
            _actors.SetParent(transform, false);

            Vector3 spawn = WorldBuilder.OnGround(-180f, -40f, 140f);
            var playerGo = WorldBuilder.Fighter("Player", _actors, spawn, Quaternion.Euler(0, 35, 0), Faction.Concord, true);
            Player = playerGo.GetComponent<CraftController>();
            PlayerUnit = playerGo.GetComponent<CombatUnit>();

            var cockpit = new GameObject("CockpitCam").AddComponent<Camera>();
            cockpit.transform.SetParent(playerGo.transform, false);
            cockpit.transform.localPosition = new Vector3(0f, 0.55f, 3.6f);
            cockpit.transform.localRotation = Quaternion.identity;
            cockpit.nearClipPlane = 0.05f;
            cockpit.farClipPlane = 18000f;
            cockpit.fieldOfView = 75f;
            cockpit.depth = 2;
            cockpit.clearFlags = CameraClearFlags.Skybox;
            cockpit.backgroundColor = new Color(0.78f, 0.58f, 0.35f);
            AddCockpitFrame(cockpit.transform);

            var chase = new GameObject("ChaseCam").AddComponent<Camera>();
            chase.transform.position = spawn - playerGo.transform.forward * 18f + Vector3.up * 5.5f;
            chase.enabled = false;
            chase.depth = 2;
            chase.nearClipPlane = 0.2f;
            chase.farClipPlane = 18000f;
            chase.clearFlags = CameraClearFlags.Skybox;
            chase.backgroundColor = new Color(0.78f, 0.58f, 0.35f);
            Player.SetupPlayer(cockpit, chase);
            HideCraftFromCockpit(playerGo, cockpit);
            cockpit.gameObject.AddComponent<AudioListener>();
            var menuListener = _menuCam != null ? _menuCam.GetComponent<AudioListener>() : null;
            if (menuListener) menuListener.enabled = false;

            SpawnEnemies();
            RefreshUnits();
            RefreshCounts();
        }

        void SpawnEnemies()
        {
            Vector3 playerPos = Player != null ? Player.transform.position : WorldBuilder.OnGround(-180f, -40f, 55f);
            Vector3 forward = Player != null ? Player.transform.forward : Vector3.forward;

            for (int i = 0; i < 3; i++)
            {
                Vector3 p = playerPos + forward * (280f + i * 90f) + Player.transform.right * (i - 1) * 70f + Vector3.up * 18f;
                var go = WorldBuilder.Fighter("Tharne-Close-" + (i + 1), _actors, p, Quaternion.LookRotation(playerPos - p), Faction.Tharne, false);
                var ai = go.AddComponent<EnemyFighterAI>();
                ai.Init(go.GetComponent<CombatUnit>(), go.GetComponent<CraftController>());
            }

            Vector3 basePos = WorldBuilder.OnGround(620f, 520f, 70f);
            for (int i = 0; i < 2; i++)
            {
                float ang = i * Mathf.PI;
                Vector3 p = basePos + new Vector3(Mathf.Cos(ang) * 90f, 25f, Mathf.Sin(ang) * 90f);
                var go = WorldBuilder.Fighter("Tharne-" + (i + 1), _actors, p, Quaternion.LookRotation(playerPos - p), Faction.Tharne, false);
                var ai = go.AddComponent<EnemyFighterAI>();
                ai.Init(go.GetComponent<CombatUnit>(), go.GetComponent<CraftController>());
            }
        }

        void AddCockpitFrame(Transform cam)
        {
            void Rail(Vector3 pos, Vector3 scale, Color color)
            {
                var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
                r.name = "Rail";
                r.transform.SetParent(cam, false);
                r.transform.localPosition = pos;
                r.transform.localScale = scale;
                Object.Destroy(r.GetComponent<Collider>());
                Palette.ApplyColor(r.GetComponent<Renderer>(), color);
            }
            Rail(new Vector3(0, -0.36f, 0.55f), new Vector3(1.05f, 0.05f, 0.18f), new Color(0.15f, 0.16f, 0.18f));
            Rail(new Vector3(-0.58f, -0.08f, 0.52f), new Vector3(0.05f, 0.42f, 0.14f), new Color(0.15f, 0.16f, 0.18f));
            Rail(new Vector3(0.58f, -0.08f, 0.52f), new Vector3(0.05f, 0.42f, 0.14f), new Color(0.15f, 0.16f, 0.18f));
            Rail(new Vector3(0, 0.38f, 0.54f), new Vector3(1.0f, 0.04f, 0.12f), new Color(0.12f, 0.45f, 0.32f));
        }

        void HideCraftFromCockpit(GameObject craft, Camera cockpit)
        {
            const int craftLayer = 8;
            foreach (var r in craft.GetComponentsInChildren<Renderer>(true))
            {
                if (r.GetComponentInParent<Camera>() != null) continue;
                r.gameObject.layer = craftLayer;
            }
            cockpit.cullingMask &= ~(1 << craftLayer);
        }

        void TearDownWorld()
        {
            Time.timeScale = 1f;
            if (_world) Destroy(_world.gameObject);
            if (_actors) Destroy(_actors.gameObject);
            Player = null;
            PlayerUnit = null;
            Units.Clear();
        }

        void EnsureMenuCamera()
        {
            if (_menuCam == null)
            {
                var go = new GameObject("MenuCam");
                go.transform.SetParent(transform, false);
                _menuCam = go.AddComponent<Camera>();
                _menuCam.clearFlags = CameraClearFlags.SolidColor;
                _menuCam.backgroundColor = Palette.MenuBg;
                _menuCam.depth = 10;
                if (go.GetComponent<AudioListener>() == null)
                    go.AddComponent<AudioListener>();
            }
            _menuCam.enabled = true;
            var al = _menuCam.GetComponent<AudioListener>();
            if (al) al.enabled = true;
        }

        void ClearDefaultScene()
        {
            foreach (var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (cam.transform.root == transform) continue;
                Destroy(cam.gameObject);
            }
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.transform.root == transform) continue;
                Destroy(light.gameObject);
            }
        }

        public void RefreshUnits()
        {
            Units.Clear();
            Units.AddRange(FindObjectsByType<CombatUnit>(FindObjectsSortMode.None));
        }

        void RefreshCounts()
        {
            RefreshUnits();
            AirLeft = 0;
            GroundLeft = 0;
            foreach (var u in Units)
            {
                if (u == null || !u.Alive || u.Faction != Faction.Tharne || !u.Objective) continue;
                if (u.Airborne) AirLeft++;
                else GroundLeft++;
            }
        }

        public void SelectNearest(CraftController from)
        {
            CombatUnit best = null;
            float bestD = float.MaxValue;
            foreach (var u in Units)
            {
                if (!ValidTarget(from, u)) continue;
                float d = Vector3.Distance(from.transform.position, u.transform.position);
                if (d < bestD) { bestD = d; best = u; }
            }
            from.Target = best;
        }

        public void SelectThreat(CraftController from)
        {
            CombatUnit best = null;
            float bestScore = float.MaxValue;
            foreach (var u in Units)
            {
                if (!ValidTarget(from, u) || !u.Airborne) continue;
                float d = Vector3.Distance(from.transform.position, u.transform.position);
                float ang = Vector3.Angle(u.transform.forward, from.transform.position - u.transform.position);
                float score = d * 0.5f + ang;
                if (score < bestScore) { bestScore = score; best = u; }
            }
            from.Target = best != null ? best : from.Target;
        }

        public void SelectNearReticle(CraftController from)
        {
            CombatUnit best = null;
            float bestAng = 25f;
            foreach (var u in Units)
            {
                if (!ValidTarget(from, u)) continue;
                float ang = Vector3.Angle(from.transform.forward, u.transform.position - from.transform.position);
                if (ang < bestAng) { bestAng = ang; best = u; }
            }
            if (best != null) from.Target = best;
        }

        public void CycleTarget(CraftController from, int dir)
        {
            var list = new List<CombatUnit>();
            foreach (var u in Units)
                if (ValidTarget(from, u)) list.Add(u);
            if (list.Count == 0) return;
            int idx = list.IndexOf(from.Target);
            idx = (idx + dir + list.Count) % list.Count;
            from.Target = list[idx];
        }

        bool ValidTarget(CraftController from, CombatUnit u)
        {
            return u != null && u.Alive && !u.IsPlayer && u.Faction != from.Unit.Faction;
        }
    }
}
