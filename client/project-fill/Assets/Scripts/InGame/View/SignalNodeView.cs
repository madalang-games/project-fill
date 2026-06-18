using System.Collections;
using TMPro;
using UnityEngine;

namespace Game.InGame.View
{
    // One Signal Panel node: a neon LED that lights when its set is registered.
    // Thin prefab (GO + this component); body is procedural so art swaps via BoardSkin/SpriteSet.
    public class SignalNodeView : MonoBehaviour
    {
        private SpriteRenderer _glow, _rim, _body, _spec, _ring, _connector;
        private TextMeshPro _glyph;
        private bool _built;

        // Base local scales captured at build: the disc/ring are NON-9-sliced sprites, so CreateSprite
        // sets their localScale to fit `size` (≈0.3, NOT 1). The register pop must scale RELATIVE to
        // these — writing Vector3.one would blow the LED up ~3× and spill into neighbors.
        private Vector3 _bodyBase, _ringBase, _glyphBase;

        // Smoothly-approached target colors (neon state transition).
        private Color _tBody, _tRing, _tGlyph, _tConn, _glowColor;
        private bool _pending;
        private float _flash; // register glow burst, decays in Update

        // Skin tokens (from SpriteSet/BoardTheme). Dynamic boards tint the unlit ring/connector
        // toward the neon accent so an idle panel already reads the active skin.
        private Color       _accent  = new(0.21f, 0.84f, 0.95f);
        private BoardFxTier _fx      = BoardFxTier.Static;
        private Color       _offRing = OffRing;
        private Color       _offConn = OffConn;

        // Idle (unlit) colors: the dark base tinted toward this node's signal color so even an
        // un-registered panel reads as a colored objective row (preview which signal it tracks),
        // muted enough never to be mistaken for the lit state.
        private Color _idleBody, _idleRing, _idleGlyph;

        public SignalType Type { get; private set; }
        public bool Lit { get; private set; }

        public SignalNodeView Initialize(SpriteSet sprites, SignalType type, bool showConnector, Vector2 size)
        {
            Type = type;
            if (!_built) Construct(sprites, showConnector, size);
            return this;
        }

        private void Construct(SpriteSet s, bool showConnector, Vector2 size)
        {
            _built = true;
            _glowColor = Type.ToColor();

            _accent = s.Accent;
            _fx     = s.Fx;
            if (_fx == BoardFxTier.Dynamic)
            {
                _offRing = Color.Lerp(OffRing, _accent, 0.35f);
                _offConn = Color.Lerp(OffConn, _accent, 0.25f);
            }

            Color c = Type.ToColor();
            _idleBody  = Color.Lerp(OffBody,  c, 0.14f);
            _idleRing  = Color.Lerp(_offRing, c, 0.32f);
            _idleGlyph = Color.Lerp(OffGlyph, c, 0.40f);

            float connW = size.x * 0.15f;
            float connH = size.y * 0.08f;
            _connector = WorldUtil.CreateSprite(transform, "Connector", null, new Color(0.2f, 0.22f, 0.3f, 1f), new Vector2(connW, connH), sliced: false, sortingOrder: 2);
            _connector.transform.localPosition = new Vector3(-size.x * 0.5f - connW * 0.5f, 0f, 0f);
            _connector.enabled = showConnector;

            // Glow is proportional to the (fixed) LED size so it stays a tight halo, never a big bloom.
            _glow = WorldUtil.CreateSprite(transform, "Glow", s.Glow, new Color(1, 1, 1, 0f), size * 1.2f, sortingOrder: 3);

            // Recessed dark socket behind the disc — a thin darker halo gives the LED an inset,
            // physically-seated look at rest instead of a flat sticker.
            _rim = WorldUtil.CreateSprite(transform, "Rim", s.Disc, RimColor, size * 1.08f, sliced: false, sortingOrder: 3);

            // Circular disc body + concentric ring (idle colors; Update lerps toward targets).
            _body = WorldUtil.CreateSprite(transform, "Body", s.Disc, _idleBody, size, sliced: false, sortingOrder: 4);

            // Glassy specular highlight (upper-left), static — makes the disc read as a domed bulb.
            _spec = WorldUtil.CreateSprite(transform, "Spec", s.Disc, new Color(1f, 1f, 1f, 0.16f), size * 0.42f, sliced: false, sortingOrder: 4);
            _spec.transform.localPosition = new Vector3(-size.x * 0.16f, size.y * 0.18f, 0f);

            _ring = WorldUtil.CreateSprite(transform, "Ring", s.Ring, _idleRing, size, sliced: false, sortingOrder: 5);
            _bodyBase = _body.transform.localScale;
            _ringBase = _ring.transform.localScale;

            _glyph = WorldUtil.CreateLabel(transform, "Glyph", Type.ToLabel(), 24f, size);
            _glyph.color = _idleGlyph;
            _glyph.fontStyle = FontStyles.Bold;
            _glyph.sortingOrder = 6;
            _glyphBase = _glyph.transform.localScale;

            _tBody = _idleBody; _tRing = _idleRing; _tGlyph = _idleGlyph; _tConn = _offConn;
        }

        private static readonly Color OffBody  = new(0.12f, 0.13f, 0.18f);
        private static readonly Color OffRing  = new(0.28f, 0.32f, 0.42f);
        private static readonly Color OffGlyph = new(0.45f, 0.50f, 0.62f);
        private static readonly Color OffConn  = new(0.20f, 0.22f, 0.30f);
        private static readonly Color RimColor = new(0.05f, 0.06f, 0.09f);

        // Scale rgb, keep alpha opaque (Color * float would also fade alpha → transparent body).
        private static Color Dim(Color c, float f) => new(c.r * f, c.g * f, c.b * f, 1f);

        public void SetState(bool lit, bool pending)
        {
            Lit = lit;
            _pending = pending;
            var c = Type.ToColor();
            _glowColor = c;

            if (lit)
            {
                // Body/ring seeds; Update overrides these every frame with the dark↔bright neon pulse.
                _tBody  = Color.Lerp(c, Color.white, 0.30f); // bright neon core
                _tRing  = Color.Lerp(c, Color.white, 0.78f); // white-hot rim
                // Lit glyph recedes (faint + shrunk in Update) — keeps the colorblind legend without
                // competing with the celebratory neon pulse.
                _tGlyph = new Color(0f, 0f, 0f, 0.30f);
                _tConn  = Color.Lerp(c, Color.white, 0.25f);
            }
            else if (pending)
            {
                _tBody  = new Color(0.30f, 0.24f, 0.08f);
                _tRing  = new Color(0.85f, 0.62f, 0.22f);
                _tGlyph = new Color(0.90f, 0.72f, 0.40f);
                _tConn  = OffConn;
            }
            else
            {
                _tBody = _idleBody; _tRing = _idleRing; _tGlyph = _idleGlyph; _tConn = _offConn;
            }
        }

        public Vector3 WorldPos => _body != null ? _body.transform.position : transform.position;

        public void PlayRegister()
        {
            _flash = 0.55f;            // glow burst (decays in Update)
            StartCoroutine(PopRoutine());
        }

        // Scale blink relative to the captured base scale (never Vector3.one — see _bodyBase note).
        private IEnumerator PopRoutine()
        {
            float dur = 0.45f, t = 0f;
            while (t < dur)
            {
                float s = 1f + Mathf.Sin(t / dur * Mathf.PI) * 0.18f;
                _body.transform.localScale = _bodyBase * s;
                _ring.transform.localScale = _ringBase * s;
                t += Time.deltaTime;
                yield return null;
            }
            _body.transform.localScale = _bodyBase;
            _ring.transform.localScale = _ringBase;
        }

        private void Update()
        {
            if (_body == null) return;

            // Smooth neon color transition toward the current state targets.
            float k = 1f - Mathf.Exp(-Time.deltaTime * 10f);

            // One shared neon-pulse envelope (0..1, smoothstepped) so the whole lit node breathes in
            // sync — dark↔bright, like a living neon sign — and the glow/spec ride the same beat.
            float pulse = Mathf.Sin(Time.unscaledTime * 2.4f) * 0.5f + 0.5f;
            pulse = pulse * pulse * (3f - 2f * pulse);

            Color bodyTarget = _tBody, ringTarget = _tRing;
            if (Lit)
            {
                // Completed: pulse the body/ring between a dim neon and a hot near-white version of this
                // signal's color. The dim floor stays clearly colored (never black) so "registered"
                // still reads at the trough; the peak blooms white-hot for the celebratory beat.
                bodyTarget = Color.Lerp(Dim(_glowColor, 0.55f), Color.Lerp(_glowColor, Color.white, 0.42f), pulse);
                ringTarget = Color.Lerp(Dim(_glowColor, 0.85f), Color.Lerp(_glowColor, Color.white, 0.95f), pulse);
            }

            _body.color  = Color.Lerp(_body.color, bodyTarget, k);
            _ring.color  = Color.Lerp(_ring.color, ringTarget, k);
            _glyph.color = Color.Lerp(_glyph.color, _tGlyph, k);
            if (_connector.enabled) _connector.color = Color.Lerp(_connector.color, _tConn, k);

            // Specular highlight brightens at the pulse peak when lit (glassy flare on the breathe),
            // a faint constant sheen at rest.
            float specA = Lit ? Mathf.Lerp(0.10f, 0.55f, pulse) : 0.16f;
            _spec.color = Color.Lerp(_spec.color, new Color(1f, 1f, 1f, specA), k);

            // Lit glyph shrinks toward the background; other states keep full size.
            Vector3 glyphTarget = Lit ? _glyphBase * 0.7f : _glyphBase;
            _glyph.transform.localScale = Vector3.Lerp(_glyph.transform.localScale, glyphTarget, k);

            // Glow: breathing on the same pulse when lit, faint when pending, off otherwise — plus burst.
            _flash = Mathf.MoveTowards(_flash, 0f, Time.deltaTime * 1.4f);
            float litMax = _fx == BoardFxTier.Dynamic ? 0.55f : 0.40f; // premium boards glow brighter
            float baseA = Lit ? Mathf.Lerp(0.12f, litMax, pulse) : (_pending ? 0.06f : 0f);
            float a = Mathf.Clamp01(baseA + _flash);
            _glow.color = Color.Lerp(_glow.color, new Color(_glowColor.r, _glowColor.g, _glowColor.b, a), k);
        }
    }
}
