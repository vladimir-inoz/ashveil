using System.Collections.Generic;
using UnityEngine;

namespace Ashveil
{
    public class CockpitHUD : MonoBehaviour
    {
        GUIStyle _label;
        GUIStyle _title;
        GUIStyle _small;
        GUIStyle _center;
        bool _ready;
        Texture2D _white;
        public bool ShowMap;
        public string Radio = "КОНКОРД: Пилот, уничтожьте радар и воздушный патруль у аванпоста «Эмбер-Рич».";

        void Ensure()
        {
            if (_ready) return;
            _white = Palette.White;
            _label = Style(18, Palette.Hud, TextAnchor.UpperLeft, FontStyle.Bold);
            _small = Style(14, Palette.Hud, TextAnchor.UpperLeft, FontStyle.Normal);
            _title = Style(42, Palette.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _center = Style(16, Palette.Hud, TextAnchor.MiddleCenter, FontStyle.Bold);
            _ready = true;
        }

        static GUIStyle Style(int size, Color c, TextAnchor a, FontStyle fs)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = fs,
                alignment = a,
                normal = { textColor = c }
            };
        }

        void OnGUI()
        {
            Ensure();
            var g = GameSession.I;
            if (g == null) return;
            switch (g.State)
            {
                case GameState.Menu: DrawMenu(); break;
                case GameState.Briefing: DrawBriefing(); break;
                case GameState.Playing: DrawFlight(); if (g.Paused) DrawPause(); break;
                case GameState.Win: DrawFlight(); DrawEnd(true); break;
                case GameState.Lose: DrawFlight(); DrawEnd(false); break;
            }
        }

        void DrawMenu()
        {
            Fill(new Color(0.16f, 0.08f, 0.02f, 0.92f));
            GUI.Label(new Rect(0, Screen.height * 0.18f, Screen.width, 70), "A S H V E I L", _title);
            var sub = Style(18, Palette.Hud, TextAnchor.MiddleCenter, FontStyle.Italic);
            GUI.Label(new Rect(0, Screen.height * 0.18f + 58, Screen.width, 30), "KESSARA  ·  2419", sub);
            var body = Style(16, Palette.MenuPanel, TextAnchor.UpperCenter, FontStyle.Normal);
            GUI.Label(new Rect(Screen.width * 0.18f, Screen.height * 0.38f, Screen.width * 0.64f, 140),
                "Конкорд держит колонию на Кессаре. Клан Тарн начал наступление с окраин континента.\nВы — курсант лётной школы. Скиф, кабинный HUD, векторная мышь и воздушный бой.", body);

            if (Button("ВЫЛЕТ", Screen.height * 0.62f)) GameSession.I.StartBriefing();
            if (Button("ВЫХОД", Screen.height * 0.62f + 58)) Application.Quit();
        }

        void DrawBriefing()
        {
            Fill(new Color(0.12f, 0.07f, 0.03f, 0.94f));
            GUI.Label(new Rect(0, 80, Screen.width, 50), "БРИФИНГ  ·  ЭМБЕР-РИЧ", _title);
            var body = Style(17, Palette.MenuPanel, TextAnchor.UpperLeft, FontStyle.Normal);
            GUI.Label(new Rect(Screen.width * 0.18f, 180, Screen.width * 0.64f, 280),
                "Пилот!\n\nПатруль Тарна занял радарный узел у аванпоста «Эмбер-Рич».\nУничтожьте перехватчики и наземные цели: радар, ПВО, танки.\n\nУправление (векторная мышь):\nмышь — тангаж и рысканье  ·  ПКМ — форсаж разворота\nW/S — газ  ·  A/D — крен  ·  Q/E — стрейф  ·  R/F — вертикаль\nЛКМ / Пробел — огонь  ·  Tab — группа оружия  ·  T — ближайшая цель\nF1 кабина  ·  F3 внешняя камера  ·  Esc — пауза", body);
            if (Button("ПОДТВЕРДИТЬ И ВЗЛЕТЕТЬ", Screen.height * 0.78f)) GameSession.I.StartMission();
        }

        void DrawPause()
        {
            GameSession.UpdateCursor();
            Fill(new Color(0, 0, 0, 0.45f));
            GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 40), "ПАУЗА", _title);
            if (Button("ПРОДОЛЖИТЬ", Screen.height * 0.5f))
            {
                GameSession.I.Paused = false;
                if (GameSession.I.Player != null) GameSession.I.Player.ResetStick();
                GameSession.UpdateCursor();
            }
            if (Button("В АНГАР", Screen.height * 0.5f + 58)) GameSession.I.ReturnToMenu();
        }

        void DrawEnd(bool win)
        {
            Fill(new Color(0, 0, 0, 0.5f));
            GUI.Label(new Rect(0, Screen.height * 0.28f, Screen.width, 50),
                win ? "ЗАДАНИЕ ВЫПОЛНЕНО" : "СКИФ СБИТ", _title);
            var s = Style(18, Palette.MenuPanel, TextAnchor.MiddleCenter, FontStyle.Normal);
            GUI.Label(new Rect(0, Screen.height * 0.28f + 60, Screen.width, 40),
                win ? "Аванпост «Эмбер-Рич» подавлен. Конкорд продвигается." : "Пилот потерян. Миссия провалена.", s);
            if (Button("ЕЩЁ РАЗ", Screen.height * 0.55f)) GameSession.I.RestartMission();
            if (Button("В АНГАР", Screen.height * 0.55f + 58)) GameSession.I.ReturnToMenu();
        }

        bool Button(string text, float y)
        {
            var r = new Rect(Screen.width * 0.5f - 180, y, 360, 46);
            var st = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Palette.MenuBg, background = _white },
                hover = { textColor = Palette.MenuBg, background = _white },
                active = { textColor = Palette.MenuBg, background = _white }
            };
            Color old = GUI.backgroundColor;
            GUI.backgroundColor = Palette.Gold;
            bool hit = GUI.Button(r, text, st);
            GUI.backgroundColor = old;
            return hit;
        }

        void Fill(Color c)
        {
            GUI.color = c;
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _white);
            GUI.color = Color.white;
        }

        void DrawFlight()
        {
            var p = GameSession.I.Player;
            if (p == null) return;
            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;

            DrawPitchLadder(p, cx, cy);
            DrawCompass(p);
            DrawReticle(p, cx, cy);
            DrawMousePip(p, cx, cy);
            DrawReadouts(p);
            DrawBars(p);
            DrawRadar(p);
            DrawTargetBox(p);
            DrawRadio();
            DrawHints();
        }

        void DrawPitchLadder(CraftController p, float cx, float cy)
        {
            float pitch = p.PitchAngle;
            GUI.color = Palette.HudDim;
            for (int deg = -40; deg <= 40; deg += 10)
            {
                float y = cy - (deg - pitch) * 4.2f;
                if (y < 80 || y > Screen.height - 80) continue;
                float w = deg == 0 ? 90 : 48;
                GUI.DrawTexture(new Rect(cx - w, y, w * 2, 1), _white);
                GUI.Label(new Rect(cx + w + 6, y - 8, 40, 20), deg.ToString(), _small);
            }
            GUI.color = Color.white;
        }

        void DrawCompass(CraftController p)
        {
            float hdg = p.Heading;
            float w = 360;
            float x0 = Screen.width * 0.5f - w * 0.5f;
            GUI.color = new Color(0, 0, 0, 0.35f);
            GUI.DrawTexture(new Rect(x0, 12, w, 28), _white);
            GUI.color = Palette.Hud;
            string[] ticks = { "С", "СВ", "В", "ЮВ", "Ю", "ЮЗ", "З", "СЗ" };
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f;
                float dx = Mathf.DeltaAngle(hdg, ang);
                float x = Screen.width * 0.5f + dx * 2.2f;
                if (x < x0 + 10 || x > x0 + w - 10) continue;
                GUI.Label(new Rect(x - 14, 12, 28, 24), ticks[i], _center);
            }
            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 1, 38, 2, 8), _white);
            GUI.color = Color.white;
        }

        void DrawReticle(CraftController p, float cx, float cy)
        {
            GUI.color = Palette.Hud;
            GUI.DrawTexture(new Rect(cx - 14, cy - 1, 28, 2), _white);
            GUI.DrawTexture(new Rect(cx - 1, cy - 14, 2, 28), _white);
            DrawCircle(cx, cy, 22, Palette.HudDim, 24);
            if (p.Group == WeaponGroup.Missiles)
            {
                float r = Mathf.Lerp(48, 18, p.LockProgress);
                DrawCircle(cx, cy, r, p.LockProgress >= 1f ? Palette.HudEnemy : Palette.HudWarn, 28);
            }
            GUI.color = Color.white;
        }

        void DrawMousePip(CraftController p, float cx, float cy)
        {
            float px = cx + p.MouseOffset.x * Screen.height * 0.42f;
            float py = cy - p.MouseOffset.y * Screen.height * 0.42f;
            GUI.color = Palette.HudWarn;
            GUI.DrawTexture(new Rect(px - 5, py - 1, 10, 2), _white);
            GUI.DrawTexture(new Rect(px - 1, py - 5, 2, 10), _white);
            GUI.color = Color.white;
        }

        void DrawReadouts(CraftController p)
        {
            GUI.color = Color.white;
            GUI.Label(new Rect(24, Screen.height * 0.42f, 180, 28), $"TAS  {p.TasKmh:0} км/ч", _label);
            GUI.Label(new Rect(24, Screen.height * 0.42f + 26, 180, 24), $"ГАЗ  {p.Throttle * 100:0}%", _small);
            GUI.Label(new Rect(Screen.width - 220, Screen.height * 0.42f, 200, 28), $"ВЫС  {p.transform.position.y:0} м", _label);
            GUI.Label(new Rect(Screen.width - 220, Screen.height * 0.42f + 26, 200, 24), $"В/С  {p.Velocity.y:+0.0} м/с", _small);
            GUI.Label(new Rect(Screen.width - 220, Screen.height * 0.42f + 48, 200, 24), $"AGL {p.Altitude:0}   {p.Heading:000}°", _small);

            string gun = p.Group == WeaponGroup.Plasma ? "ПЛАЗМА" : p.Group == WeaponGroup.Cannon ? "ПУШКА" : "РАКЕТЫ";
            string ammo = p.Group == WeaponGroup.Plasma ? $"{p.Energy:0} э" : p.Group == WeaponGroup.Cannon ? $"{p.CannonAmmo}" : $"{p.Missiles}";
            GUI.Label(new Rect(24, Screen.height - 86, 260, 24), $"ОРУЖИЕ  {gun}   {ammo}", _label);
        }

        void DrawBars(CraftController p)
        {
            DrawBar(24, Screen.height - 54, 180, 10, p.Unit.Health01, Palette.Hud, "БРОНЯ");
            DrawBar(24, Screen.height - 38, 180, 10, p.Energy / p.EnergyMax, Palette.HudWarn, "ЭНЕРГИЯ");
        }

        void DrawBar(float x, float y, float w, float h, float t, Color c, string cap)
        {
            GUI.color = new Color(0, 0, 0, 0.45f);
            GUI.DrawTexture(new Rect(x, y, w, h), _white);
            GUI.color = c;
            GUI.DrawTexture(new Rect(x, y, w * Mathf.Clamp01(t), h), _white);
            GUI.Label(new Rect(x + w + 8, y - 4, 80, 18), cap, _small);
            GUI.color = Color.white;
        }

        void DrawRadar(CraftController p)
        {
            float s = 128;
            float x = Screen.width - 24 - s;
            float y = Screen.height - 24 - s;
            GUI.color = new Color(0, 0.1f, 0, 0.45f);
            GUI.DrawTexture(new Rect(x, y, s, s), _white);
            GUI.color = Palette.HudDim;
            DrawCircle(x + s * 0.5f, y + s * 0.5f, s * 0.48f, Palette.HudDim, 32);
            GUI.DrawTexture(new Rect(x + s * 0.5f - 1, y + 8, 2, s - 16), _white);
            GUI.DrawTexture(new Rect(x + 8, y + s * 0.5f - 1, s - 16, 2), _white);

            float range = 900f;
            foreach (var u in GameSession.I.Units)
            {
                if (u == null || !u.Alive || u.IsPlayer) continue;
                Vector3 d = u.transform.position - p.transform.position;
                Vector3 local = Quaternion.Inverse(p.transform.rotation) * d;
                float rx = x + s * 0.5f + local.x / range * (s * 0.45f);
                float ry = y + s * 0.5f - local.z / range * (s * 0.45f);
                if (rx < x + 4 || rx > x + s - 4 || ry < y + 4 || ry > y + s - 4) continue;
                GUI.color = u.Faction == Faction.Tharne ? Palette.HudEnemy : Palette.HudFriend;
                GUI.DrawTexture(new Rect(rx - 2, ry - 2, 4, 4), _white);
            }
            GUI.color = Palette.Hud;
            GUI.DrawTexture(new Rect(x + s * 0.5f - 3, y + s * 0.5f - 3, 6, 6), _white);
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y - 18, s, 18), "СЕНСОР", _small);
        }

        void DrawTargetBox(CraftController p)
        {
            if (p.Target == null || !p.Target.Alive || p.CockpitCam == null) return;
            var cam = p.CameraMode == 0 ? p.CockpitCam : p.ChaseCam;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(p.Target.transform.position);
            if (sp.z < 0.5f) return;
            sp.y = Screen.height - sp.y;
            float sz = 28f;
            Color c = p.Target.Faction == Faction.Tharne ? Palette.HudEnemy : Palette.HudFriend;
            GUI.color = c;
            DrawBracket(sp.x, sp.y, sz);
            float dist = Vector3.Distance(p.transform.position, p.Target.transform.position);
            GUI.Label(new Rect(sp.x - 70, sp.y - sz - 36, 140, 20), p.Target.DisplayName, _center);
            GUI.Label(new Rect(sp.x - 70, sp.y - sz - 18, 140, 18), $"{dist:0} м", _center);
            GUI.color = new Color(0, 0, 0, 0.5f);
            GUI.DrawTexture(new Rect(sp.x - 30, sp.y + sz + 6, 60, 5), _white);
            GUI.color = c;
            GUI.DrawTexture(new Rect(sp.x - 30, sp.y + sz + 6, 60 * p.Target.Health01, 5), _white);
            GUI.color = Color.white;
        }

        void DrawBracket(float x, float y, float s)
        {
            float t = 2f, l = 10f;
            GUI.DrawTexture(new Rect(x - s, y - s, l, t), _white);
            GUI.DrawTexture(new Rect(x - s, y - s, t, l), _white);
            GUI.DrawTexture(new Rect(x + s - l, y - s, l, t), _white);
            GUI.DrawTexture(new Rect(x + s - t, y - s, t, l), _white);
            GUI.DrawTexture(new Rect(x - s, y + s - t, l, t), _white);
            GUI.DrawTexture(new Rect(x - s, y + s - l, t, l), _white);
            GUI.DrawTexture(new Rect(x + s - l, y + s - t, l, t), _white);
            GUI.DrawTexture(new Rect(x + s - t, y + s - l, t, l), _white);
        }

        void DrawCircle(float x, float y, float r, Color c, int seg)
        {
            GUI.color = c;
            for (int i = 0; i < seg; i++)
            {
                float a0 = i / (float)seg * Mathf.PI * 2f;
                float px = x + Mathf.Cos(a0) * r;
                float py = y + Mathf.Sin(a0) * r;
                GUI.DrawTexture(new Rect(px, py, 2, 2), _white);
            }
            GUI.color = Color.white;
        }

        void DrawRadio()
        {
            GUI.color = new Color(0, 0, 0, 0.35f);
            GUI.DrawTexture(new Rect(220, Screen.height - 78, Screen.width - 420, 54), _white);
            GUI.color = Color.white;
            GUI.Label(new Rect(230, Screen.height - 74, Screen.width - 440, 48), Radio, _small);
        }

        void DrawHints()
        {
            var g = GameSession.I;
            GUI.Label(new Rect(24, 52, 500, 20), $"ЦЕЛИ  воздух {g.AirLeft}   земля {g.GroundLeft}", _small);
        }
    }
}
