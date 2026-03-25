using System;
using System.Collections;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Visual for a combat enemy using SDF shaders (SH_Skeleton / SH_Spider).
    /// Each enemy type is drawn with a single SDF quad.
    /// Includes intent icon (colored quad + text) and hit/death animations.
    /// </summary>
    public class EnemyView : MonoBehaviour
    {
        private static readonly Color SkeletonColor    = new Color(0.90f, 0.90f, 0.90f, 1.00f);
        private static readonly Color SpiderColor      = new Color(0.50f, 0.50f, 0.55f, 1.00f);
        private static readonly Color MoveIconColor    = new Color(0.95f, 0.85f, 0.20f);
        private static readonly Color AttackIconColor  = new Color(0.90f, 0.20f, 0.15f);

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

        public void PlayHitFlash()
        {
            StopCoroutine("HitFlashRoutine");
            StartCoroutine(HitFlashRoutine());
        }

        public void PlayDeathFade(Action onComplete)
        {
            StartCoroutine(DeathFadeRoutine(onComplete));
        }

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
            _baseBodyColor = SkeletonColor;

            var shader = Shader.Find("Mergistry/SH_Skeleton");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SkeletonQuad";
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one * 0.90f;
            Destroy(go.GetComponent<MeshCollider>());

            _bodyRenderer = go.GetComponent<MeshRenderer>();
            _bodyRenderer.material = new Material(shader) { color = _baseBodyColor };
            _bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _bodyRenderer.receiveShadows    = false;
        }

        private void BuildSpider()
        {
            _baseBodyColor = SpiderColor;

            var shader = Shader.Find("Mergistry/SH_Spider");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SpiderQuad";
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one * 1.00f;
            Destroy(go.GetComponent<MeshCollider>());

            _bodyRenderer = go.GetComponent<MeshRenderer>();
            _bodyRenderer.material = new Material(shader) { color = _baseBodyColor };
            _bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _bodyRenderer.receiveShadows    = false;
        }

        private void BuildIntentIcon()
        {
            _intentIcon = new GameObject("IntentIcon");
            _intentIcon.transform.SetParent(transform);
            _intentIcon.transform.localPosition = new Vector3(0f, 0.55f, -0.03f);

            var bg = CreateQuad("IntentBg", Vector3.zero, Vector3.one * 0.28f,
                                AttackIconColor, _intentIcon.transform);
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

        // ── Animations ───────────────────────────────────────────────────────

        private IEnumerator HitFlashRoutine()
        {
            _bodyRenderer.material.color = Color.white;
            yield return new WaitForSeconds(0.1f);
            _bodyRenderer.material.color = _baseBodyColor;
        }

        private IEnumerator DeathFadeRoutine(Action onComplete)
        {
            var   mat      = _bodyRenderer.material;
            float elapsed  = 0f;
            const float duration = 0.3f;
            var   c        = _baseBodyColor;

            // Switch to a transparent-capable shader for the fade
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

        // ── Helpers ──────────────────────────────────────────────────────────

        private static GameObject CreateQuad(string goName, Vector3 localPos, Vector3 localScale,
                                             Color color, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = goName;
            go.transform.SetParent(parent);
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
