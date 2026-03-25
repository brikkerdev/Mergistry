using System;
using System.Collections;
using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Visual for a combat enemy using SDF shaders or placeholder quads.
    /// A2: adds MushroomBomb (timer label + color lerp), MagnetGolem, ArmoredBeetle (armor diamonds).
    /// </summary>
    public class EnemyView : MonoBehaviour
    {
        // ── Palette ──────────────────────────────────────────────────────────
        private static readonly Color SkeletonColor      = new Color(0.90f, 0.90f, 0.90f);
        private static readonly Color SpiderColor        = new Color(0.50f, 0.50f, 0.55f);
        private static readonly Color MushroomYellow     = new Color(0.92f, 0.80f, 0.20f);
        private static readonly Color MushroomRed        = new Color(0.90f, 0.15f, 0.10f);
        private static readonly Color GolemColor         = new Color(0.55f, 0.60f, 0.70f);
        private static readonly Color BeetleColor        = new Color(0.20f, 0.55f, 0.30f);
        private static readonly Color ArmorDiamondColor  = new Color(0.80f, 0.85f, 0.95f);
        private static readonly Color MoveIconColor      = new Color(0.95f, 0.85f, 0.20f);
        private static readonly Color AttackIconColor    = new Color(0.90f, 0.20f, 0.15f);
        private static readonly Color CountdownIconColor = new Color(0.95f, 0.65f, 0.10f);
        private static readonly Color ExplodeIconColor   = new Color(1.00f, 0.10f, 0.00f);
        private static readonly Color PullIconColor      = new Color(0.30f, 0.70f, 1.00f);
        private static readonly Color TeleportIconColor  = new Color(0.70f, 0.20f, 1.00f);
        private static readonly Color ReviveIconColor    = new Color(0.20f, 0.90f, 0.50f);
        private static readonly Color MirrorSlimeColor   = new Color(0.50f, 0.80f, 0.90f);
        private static readonly Color PhantomColor       = new Color(0.75f, 0.60f, 0.95f);
        private static readonly Color NecromancerColor   = new Color(0.30f, 0.15f, 0.50f);

        public int EntityId { get; private set; }

        private MeshRenderer      _bodyRenderer;
        private Color             _baseBodyColor;
        private EnemyType         _type;

        // Intent icon
        private GameObject        _intentIcon;
        private MeshRenderer      _intentIconRenderer;
        private TextMesh          _intentText;

        // MushroomBomb: timer display
        private TextMesh          _timerText;
        private float             _bombTimerLerp;   // 0 = yellow, 1 = red

        // ArmoredBeetle: armor diamonds
        private readonly List<GameObject> _armorDiamonds = new List<GameObject>();
        private int _maxArmor;

        // ── Initialization ───────────────────────────────────────────────────

        public void Initialize(EnemyCombatModel enemy, Vector3 worldPos)
        {
            EntityId = enemy.EntityId;
            _type    = enemy.Type;
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);

            switch (enemy.Type)
            {
                case EnemyType.Skeleton:      BuildSkeleton();     break;
                case EnemyType.Spider:        BuildSpider();       break;
                case EnemyType.MushroomBomb:  BuildMushroomBomb(); break;
                case EnemyType.MagnetGolem:   BuildMagnetGolem();  break;
                case EnemyType.ArmoredBeetle:
                    BuildArmoredBeetle();
                    BuildArmorIndicators(enemy.ArmorPoints);
                    break;
                case EnemyType.MirrorSlime:   BuildMirrorSlime();   break;
                case EnemyType.Phantom:       BuildPhantom();       break;
                case EnemyType.Necromancer:   BuildNecromancer();   break;
            }

            BuildIntentIcon();
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void PlaceAt(Vector3 worldPos)
        {
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }

        public void PlayHitFlash()
        {
            StopCoroutine("HitFlashRoutine");
            StartCoroutine(HitFlashRoutine());
        }

        public void PlayDeathFade(Action onComplete)
        {
            StartCoroutine(DeathFadeRoutine(onComplete));
        }

        /// <summary>Tween to a new world position with a squash effect (for push).</summary>
        public void PlayPushAnimation(Vector3 targetPos, Action onComplete = null)
        {
            StartCoroutine(PushRoutine(targetPos, onComplete));
        }

        public void SetIntent(EnemyIntent intent)
        {
            if (intent == null)
            {
                _intentIcon.SetActive(false);
                HideTimer();
                return;
            }

            _intentIcon.SetActive(true);

            switch (intent.Type)
            {
                case IntentType.Attack:
                    _intentIconRenderer.material.color = AttackIconColor;
                    _intentText.text = "X";
                    HideTimer();
                    break;
                case IntentType.Move:
                    _intentIconRenderer.material.color = MoveIconColor;
                    _intentText.text = ">";
                    HideTimer();
                    break;
                case IntentType.Countdown:
                    _intentIconRenderer.material.color = CountdownIconColor;
                    _intentText.text = intent.CountdownValue.ToString();
                    ShowTimer(intent.CountdownValue);
                    UpdateBombColor(intent.CountdownValue);
                    break;
                case IntentType.Explode:
                    _intentIconRenderer.material.color = ExplodeIconColor;
                    _intentText.text = "💥";
                    UpdateBombColor(0);
                    break;
                case IntentType.Pull:
                    _intentIconRenderer.material.color = PullIconColor;
                    _intentText.text = "<";
                    HideTimer();
                    break;
                case IntentType.Teleport:
                    _intentIconRenderer.material.color = TeleportIconColor;
                    _intentText.text = "~";
                    HideTimer();
                    break;
                case IntentType.Revive:
                    _intentIconRenderer.material.color = ReviveIconColor;
                    _intentText.text = "+";
                    HideTimer();
                    break;
            }
        }

        /// <summary>Updates the armor indicator diamonds (called when armor changes).</summary>
        public void UpdateArmor(int armorRemaining)
        {
            for (int i = 0; i < _armorDiamonds.Count; i++)
                _armorDiamonds[i].SetActive(i < armorRemaining);
        }

        // ── Build ────────────────────────────────────────────────────────────

        private void BuildSkeleton()
        {
            _baseBodyColor = SkeletonColor;
            var shader = Shader.Find("Mergistry/SH_Skeleton") ?? Shader.Find("Unlit/Color");
            var go     = MakeQuad("SkeletonQuad", Vector3.zero, Vector3.one * 0.90f, _baseBodyColor, shader);
            _bodyRenderer = go.GetComponent<MeshRenderer>();
        }

        private void BuildSpider()
        {
            _baseBodyColor = SpiderColor;
            var shader = Shader.Find("Mergistry/SH_Spider") ?? Shader.Find("Unlit/Color");
            var go     = MakeQuad("SpiderQuad", Vector3.zero, Vector3.one * 1.00f, _baseBodyColor, shader);
            _bodyRenderer = go.GetComponent<MeshRenderer>();
        }

        private void BuildMushroomBomb()
        {
            // Placeholder: half-circle body (rectangle) + round cap
            _baseBodyColor = MushroomYellow;
            var shader = Shader.Find("Unlit/Color");

            // Cap (top semicircle placeholder = wider quad)
            MakeQuad("CapQuad", new Vector3(0f, 0.18f, 0f), new Vector3(0.85f, 0.55f, 1f),
                     _baseBodyColor, shader, transform);

            // Stem (lower rectangle)
            var stem = MakeQuad("StemQuad", new Vector3(0f, -0.22f, 0f), new Vector3(0.35f, 0.40f, 1f),
                                _baseBodyColor, shader, transform);
            _bodyRenderer = stem.GetComponent<MeshRenderer>();

            // Timer label above head
            var timerGo = new GameObject("TimerLabel");
            timerGo.transform.SetParent(transform);
            timerGo.transform.localPosition = new Vector3(0f, 0.55f, -0.05f);
            timerGo.transform.localScale    = Vector3.one * 0.35f;
            _timerText           = timerGo.AddComponent<TextMesh>();
            _timerText.text      = "3";
            _timerText.fontSize  = 20;
            _timerText.alignment = TextAlignment.Center;
            _timerText.anchor    = TextAnchor.MiddleCenter;
            _timerText.color     = Color.white;
        }

        private void BuildMagnetGolem()
        {
            _baseBodyColor = GolemColor;
            var shader = Shader.Find("Mergistry/SH_MagnetGolem") ?? Shader.Find("Unlit/Color");
            var go     = MakeQuad("GolemQuad", Vector3.zero, Vector3.one * 0.95f, _baseBodyColor, shader);
            _bodyRenderer = go.GetComponent<MeshRenderer>();
        }

        private void BuildArmoredBeetle()
        {
            _baseBodyColor = BeetleColor;
            var shader = Shader.Find("Mergistry/SH_ArmoredBeetle") ?? Shader.Find("Unlit/Color");
            var go     = MakeQuad("BeetleQuad", Vector3.zero, Vector3.one * 0.90f, _baseBodyColor, shader);
            _bodyRenderer = go.GetComponent<MeshRenderer>();
        }

        private void BuildMirrorSlime()
        {
            _baseBodyColor = MirrorSlimeColor;
            // Metaball-style: two overlapping circles as placeholder
            var shader = Shader.Find("Unlit/Color");
            var go     = MakeQuad("SlimeBody", new Vector3(0f, 0.05f, 0f), new Vector3(0.85f, 0.75f, 1f),
                                  _baseBodyColor, shader);
            _bodyRenderer = go.GetComponent<MeshRenderer>();
            MakeQuad("SlimeBlob", new Vector3(0.20f, -0.15f, 0.01f), new Vector3(0.45f, 0.45f, 1f),
                     new Color(MirrorSlimeColor.r * 0.85f, MirrorSlimeColor.g * 0.85f, MirrorSlimeColor.b, 1f),
                     shader, transform);
        }

        private void BuildPhantom()
        {
            _baseBodyColor = PhantomColor;
            var shader = Shader.Find("Unlit/Color");
            // Ghost shape: upper body + wispy bottom
            var go = MakeQuad("PhantomBody", new Vector3(0f, 0.10f, 0f), new Vector3(0.80f, 0.80f, 1f),
                               _baseBodyColor, shader);
            _bodyRenderer = go.GetComponent<MeshRenderer>();
            MakeQuad("PhantomTail", new Vector3(0f, -0.30f, 0.01f), new Vector3(0.55f, 0.30f, 1f),
                     new Color(PhantomColor.r, PhantomColor.g, PhantomColor.b, 0.6f),
                     Shader.Find("Sprites/Default") ?? shader, transform);
        }

        private void BuildNecromancer()
        {
            _baseBodyColor = NecromancerColor;
            var shader = Shader.Find("Unlit/Color");
            // Robed figure: tall narrow quad + hood top
            var go = MakeQuad("NecroRobe", new Vector3(0f, -0.05f, 0f), new Vector3(0.60f, 0.90f, 1f),
                               _baseBodyColor, shader);
            _bodyRenderer = go.GetComponent<MeshRenderer>();
            MakeQuad("NecroHood", new Vector3(0f, 0.45f, 0.01f), new Vector3(0.70f, 0.35f, 1f),
                     new Color(NecromancerColor.r * 1.4f, NecromancerColor.g * 1.4f, NecromancerColor.b * 1.4f, 1f),
                     shader, transform);
        }

        private void BuildArmorIndicators(int maxArmor)
        {
            _maxArmor = maxArmor;
            float spacing = 0.22f;
            float startX  = -(maxArmor - 1) * spacing * 0.5f;

            for (int i = 0; i < maxArmor; i++)
            {
                var diamond = MakeQuad($"ArmorDiamond_{i}",
                    new Vector3(startX + i * spacing, -0.55f, -0.02f),
                    new Vector3(0.18f, 0.18f, 1f),
                    ArmorDiamondColor,
                    Shader.Find("Unlit/Color"),
                    transform);
                diamond.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                _armorDiamonds.Add(diamond);
            }
        }

        private void BuildIntentIcon()
        {
            _intentIcon = new GameObject("IntentIcon");
            _intentIcon.transform.SetParent(transform);
            _intentIcon.transform.localPosition = new Vector3(0f, 0.60f, -0.03f);

            var bg = MakeQuad("IntentBg", Vector3.zero, Vector3.one * 0.30f,
                              AttackIconColor, Shader.Find("Unlit/Color"), _intentIcon.transform);
            _intentIconRenderer = bg.GetComponent<MeshRenderer>();

            var textGo = new GameObject("IntentLabel");
            textGo.transform.SetParent(_intentIcon.transform);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            textGo.transform.localScale    = Vector3.one * 0.25f;

            _intentText           = textGo.AddComponent<TextMesh>();
            _intentText.text      = "X";
            _intentText.fontSize  = 16;
            _intentText.alignment = TextAlignment.Center;
            _intentText.anchor    = TextAnchor.MiddleCenter;
            _intentText.color     = Color.white;

            _intentIcon.SetActive(false);
        }

        // ── Timer helpers ────────────────────────────────────────────────────

        private void ShowTimer(int value)
        {
            if (_timerText == null) return;
            _timerText.gameObject.SetActive(true);
            _timerText.text = value.ToString();
        }

        private void HideTimer()
        {
            if (_timerText == null) return;
            _timerText.gameObject.SetActive(false);
        }

        private void UpdateBombColor(int timerValue)
        {
            if (_bodyRenderer == null) return;
            // timerValue 3 → yellow, 0 → red
            float t = 1f - timerValue / 3f;
            _bodyRenderer.material.color = Color.Lerp(MushroomYellow, MushroomRed, t);
        }

        // ── Animations ───────────────────────────────────────────────────────

        private IEnumerator HitFlashRoutine()
        {
            if (_bodyRenderer != null)
            {
                _bodyRenderer.material.color = Color.white;
                yield return new WaitForSeconds(0.1f);
                _bodyRenderer.material.color = _baseBodyColor;
            }
        }

        private IEnumerator DeathFadeRoutine(Action onComplete)
        {
            if (_bodyRenderer == null) { onComplete?.Invoke(); yield break; }

            var   mat      = _bodyRenderer.material;
            float elapsed  = 0f;
            const float duration = 0.3f;
            var   c        = _baseBodyColor;

            var fadeShader = Shader.Find("Sprites/Default");
            if (fadeShader != null) mat.shader = fadeShader;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a      = 1f - elapsed / duration;
                mat.color = c;
                yield return null;
            }

            onComplete?.Invoke();
        }

        private IEnumerator PushRoutine(Vector3 targetPos, Action onComplete)
        {
            const float duration = 0.12f;
            var startPos = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / duration;
                transform.position = Vector3.Lerp(startPos, targetPos,
                    t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t); // ease in-out

                // Squash on impact
                float squash = t > 0.7f ? 1f + 0.25f * (1f - t) : 1f;
                transform.localScale = new Vector3(squash, 1f / squash, 1f);
                yield return null;
            }

            transform.position   = targetPos;
            transform.localScale = Vector3.one;
            onComplete?.Invoke();
        }

        // ── Static helpers ───────────────────────────────────────────────────

        private GameObject MakeQuad(string goName, Vector3 localPos, Vector3 localScale,
                                    Color color, Shader shader, Transform parent = null)
        {
            if (parent == null) parent = transform;
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = goName;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            Destroy(go.GetComponent<MeshCollider>());

            var rend = go.GetComponent<MeshRenderer>();
            rend.material = new Material(shader ?? Shader.Find("Unlit/Color")) { color = color };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;
            return go;
        }
    }
}
