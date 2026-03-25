using System;
using System.Collections;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Placeholder visual for a combat enemy.
    /// Skeleton: white rectangle with two dot-eyes.
    /// Spider: gray circle.
    /// Includes intent icon (colored quad + text) and hit/death animations.
    /// </summary>
    public class EnemyView : MonoBehaviour
    {
        private static readonly Color SkeletonColor = new Color(0.90f, 0.90f, 0.90f);
        private static readonly Color SpiderColor   = new Color(0.50f, 0.50f, 0.55f);
        private static readonly Color EyeColor      = new Color(0.15f, 0.15f, 0.20f);
        private static readonly Color FlashColor    = Color.white;
        private static readonly Color MoveIconColor   = new Color(0.95f, 0.85f, 0.20f);
        private static readonly Color AttackIconColor = new Color(0.90f, 0.20f, 0.15f);

        public int EntityId { get; private set; }

        private MeshRenderer _bodyRenderer;
        private Color        _baseBodyColor;
        private GameObject   _intentIcon;
        private MeshRenderer _intentIconRenderer;
        private TextMesh     _intentText;

        // ── Initialization ───────────────────────────────────────────────────

        public void Initialize(EnemyCombatModel enemy, Vector3 worldPos)
        {
            EntityId = enemy.EntityId;
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);

            if (enemy.Type == EnemyType.Skeleton)
                BuildSkeleton();
            else
                BuildSpider();

            BuildIntentIcon();
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void PlaceAt(Vector3 worldPos)
        {
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }

        /// <summary>Brief white flash to indicate a hit (0.1 s).</summary>
        public void PlayHitFlash()
        {
            StopCoroutine("HitFlashRoutine");
            StartCoroutine(HitFlashRoutine());
        }

        /// <summary>Fades out over 0.3 s, then calls onComplete.</summary>
        public void PlayDeathFade(Action onComplete)
        {
            StartCoroutine(DeathFadeRoutine(onComplete));
        }

        /// <summary>Updates the intent icon based on the enemy's current intent.</summary>
        public void SetIntent(EnemyIntent intent)
        {
            if (intent == null)
            {
                _intentIcon.SetActive(false);
                return;
            }

            _intentIcon.SetActive(true);
            bool isAttack = intent.Type == IntentType.Attack;
            _intentIconRenderer.material.color = isAttack ? AttackIconColor : MoveIconColor;
            _intentText.text = isAttack ? "X" : ">";
        }

        // ── Build ────────────────────────────────────────────────────────────

        private void BuildSkeleton()
        {
            // Body: white rectangle (0.55 w × 0.65 h)
            var body = CreateQuad("Body", Vector3.zero, new Vector3(0.55f, 0.65f, 1f), SkeletonColor);
            _bodyRenderer  = body.GetComponent<MeshRenderer>();
            _baseBodyColor = SkeletonColor;

            // Eyes: two small dark dots
            CreateQuad("EyeL", new Vector3(-0.12f,  0.10f, -0.01f), Vector3.one * 0.10f, EyeColor);
            CreateQuad("EyeR", new Vector3( 0.12f,  0.10f, -0.01f), Vector3.one * 0.10f, EyeColor);
        }

        private void BuildSpider()
        {
            // Body: gray circle-ish oval (wider than tall)
            var body = CreateQuad("Body", Vector3.zero, new Vector3(0.60f, 0.40f, 1f), SpiderColor);
            _bodyRenderer  = body.GetComponent<MeshRenderer>();
            _baseBodyColor = SpiderColor;

            // Eight small leg stubs
            float[,] legs = {
                {-0.38f,  0.10f}, { 0.38f,  0.10f},
                {-0.38f,  0.00f}, { 0.38f,  0.00f},
                {-0.38f, -0.10f}, { 0.38f, -0.10f},
                {-0.30f, -0.20f}, { 0.30f, -0.20f}
            };
            for (int i = 0; i < legs.GetLength(0); i++)
                CreateQuad($"Leg{i}", new Vector3(legs[i, 0], legs[i, 1], -0.01f),
                            new Vector3(0.14f, 0.06f, 1f), SpiderColor * 0.8f);
        }

        private void BuildIntentIcon()
        {
            _intentIcon = new GameObject("IntentIcon");
            _intentIcon.transform.SetParent(transform);
            _intentIcon.transform.localPosition = new Vector3(0f, 0.55f, -0.03f);

            // Colored background quad
            var bg = CreateQuad("IntentBg", Vector3.zero, Vector3.one * 0.28f, AttackIconColor, _intentIcon.transform);
            _intentIconRenderer = bg.GetComponent<MeshRenderer>();

            // Text label
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

        // ── Animations ───────────────────────────────────────────────────────

        private IEnumerator HitFlashRoutine()
        {
            _bodyRenderer.material.color = FlashColor;
            yield return new WaitForSeconds(0.1f);
            _bodyRenderer.material.color = _baseBodyColor;
        }

        private IEnumerator DeathFadeRoutine(Action onComplete)
        {
            var   mat     = _bodyRenderer.material;
            float elapsed = 0f;
            const float duration = 0.3f;

            // Use Sprites/Default for alpha fade
            mat.shader = Shader.Find("Sprites/Default");
            var c = _baseBodyColor;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = 1f - elapsed / duration;
                mat.color = c;
                yield return null;
            }

            onComplete?.Invoke();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private GameObject CreateQuad(string goName, Vector3 localPos, Vector3 localScale,
                                       Color color, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = goName;
            go.transform.SetParent(parent != null ? parent : transform);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;

            Destroy(go.GetComponent<MeshCollider>());

            var rend = go.GetComponent<MeshRenderer>();
            rend.material = new Material(Shader.Find("Unlit/Color")) { color = color };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;

            return go;
        }
    }
}
