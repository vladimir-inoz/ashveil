using UnityEngine;

namespace Ashveil
{
    public static class Palette
    {
        public static readonly Color Hud = new Color(0.45f, 1f, 0.55f, 0.92f);
        public static readonly Color HudDim = new Color(0.45f, 1f, 0.55f, 0.35f);
        public static readonly Color HudWarn = new Color(1f, 0.72f, 0.15f, 0.95f);
        public static readonly Color HudEnemy = new Color(1f, 0.28f, 0.18f, 0.95f);
        public static readonly Color HudFriend = new Color(0.35f, 0.75f, 1f, 0.95f);
        public static readonly Color MenuBg = new Color(0.16f, 0.08f, 0.02f, 1f);
        public static readonly Color MenuPanel = new Color(1f, 0.87f, 0.73f, 0.94f);
        public static readonly Color Gold = new Color(0.86f, 0.68f, 0.22f, 1f);
        public static readonly Color TerrainLow = new Color(0.42f, 0.28f, 0.14f);
        public static readonly Color TerrainHigh = new Color(0.62f, 0.42f, 0.22f);
        public static readonly Color Rock = new Color(0.28f, 0.18f, 0.12f);
        public static readonly Color Water = new Color(0.12f, 0.32f, 0.38f, 0.72f);
        public static readonly Color ConcordHull = new Color(0.72f, 0.76f, 0.8f);
        public static readonly Color ConcordAccent = new Color(0.15f, 0.55f, 0.85f);
        public static readonly Color TharneHull = new Color(0.28f, 0.22f, 0.18f);
        public static readonly Color TharneAccent = new Color(0.85f, 0.35f, 0.08f);
        public static readonly Color Plasma = new Color(0.35f, 1f, 0.7f, 1f);
        public static readonly Color Tracer = new Color(1f, 0.85f, 0.35f, 1f);
        public static readonly Color Missile = new Color(1f, 0.45f, 0.1f, 1f);

        static Texture2D _white;
        static Shader _unlitShader;
        static Shader _alphaShader;
        static Shader _skyShader;

        public static Texture2D White
        {
            get
            {
                if (_white == null)
                {
                    _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    _white.SetPixel(0, 0, Color.white);
                    _white.Apply();
                    _white.hideFlags = HideFlags.HideAndDontSave;
                }
                return _white;
            }
        }

        public static Shader UnlitShader
        {
            get
            {
                if (_unlitShader == null)
                    _unlitShader = Resources.Load<Shader>("AshUnlit")
                                   ?? Shader.Find("Ashveil/Unlit")
                                   ?? Shader.Find("Unlit/Color")
                                   ?? Shader.Find("Sprites/Default");
                return _unlitShader;
            }
        }

        public static Shader AlphaShader
        {
            get
            {
                if (_alphaShader == null)
                    _alphaShader = Resources.Load<Shader>("AshUnlitAlpha")
                                   ?? Shader.Find("Ashveil/UnlitAlpha")
                                   ?? UnlitShader;
                return _alphaShader;
            }
        }

        public static Shader SkyShader
        {
            get
            {
                if (_skyShader == null)
                    _skyShader = Resources.Load<Shader>("AshSky")
                                 ?? Shader.Find("Ashveil/Sky")
                                 ?? Shader.Find("Skybox/Procedural");
                return _skyShader;
            }
        }

        public static Material Unlit => Colored(Color.white);

        public static Material Particle => Colored(new Color(1f, 0.55f, 0.12f));

        public static Material Colored(Color c, bool transparent = false)
        {
            var sh = transparent ? AlphaShader : UnlitShader;
            if (sh == null)
                throw new System.Exception("Ashveil shaders missing from Resources.");
            var m = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            m.color = c;
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }

        public static Material Skybox()
        {
            var sh = SkyShader;
            if (sh == null) return null;
            var m = new Material(sh) { hideFlags = HideFlags.HideAndDontSave };
            if (m.HasProperty("_SkyTop")) m.SetColor("_SkyTop", new Color(0.45f, 0.58f, 0.88f));
            if (m.HasProperty("_SkyHorizon")) m.SetColor("_SkyHorizon", new Color(0.95f, 0.62f, 0.28f));
            if (m.HasProperty("_Ground")) m.SetColor("_Ground", new Color(0.38f, 0.24f, 0.12f));
            return m;
        }

        public static void ApplyColor(Renderer r, Color c, bool transparent = false)
        {
            r.sharedMaterial = Colored(c, transparent);
        }
    }
}
