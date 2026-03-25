using System.Collections;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Spawns an expanding circle visual at a world position for potion impact effects.
    /// Uses "Sprites/Default" shader so alpha fade works without a custom shader.
    /// A7: also provides PlayComboText and PlayCameraShake.
    /// </summary>
    public class EffectView : MonoBehaviour
    {
        private const float EffectDuration = 0.3f;
        private const float StartScale     = 0.15f;
        private const float EndScale       = 1.2f;

        // ── Public API ──────────────────────────────────────────────────────────

        /// <summary>
        /// Plays a single expanding + fading circle at <paramref name="worldPos"/>
        /// with the given <paramref name="color"/>.
        /// </summary>
        public void PlayEffect(Vector3 worldPos, Color color)
        {
            StartCoroutine(EffectCoroutine(worldPos, color));
        }

        /// <summary>
        /// A7: Displays a "COMBO!" text that scales up then fades at the given world position.
        /// </summary>
        public void PlayComboText(Vector3 worldPos)
        {
            StartCoroutine(ComboTextCoroutine(worldPos));
        }

        /// <summary>
        /// A7: Shakes Camera.main by <paramref name="magnitude"/> world units for <paramref name="duration"/> seconds.
        /// </summary>
        public void PlayCameraShake(float magnitude = 0.05f, float duration = 0.2f)
        {
            if (Camera.main != null)
                StartCoroutine(CameraShakeCoroutine(Camera.main, magnitude, duration));
        }

        // ── Internal ────────────────────────────────────────────────────────────

        private IEnumerator EffectCoroutine(Vector3 worldPos, Color color)
        {
            var go   = CreateCircleQuad(worldPos);
            var rend = go.GetComponent<MeshRenderer>();

            // Use Sprites/Default for alpha support
            var mat  = new Material(Shader.Find("Sprites/Default"));
            rend.material = mat;

            float t = 0f;
            while (t < EffectDuration)
            {
                t += Time.deltaTime;
                float pct = t / EffectDuration;

                go.transform.localScale = Vector3.one * Mathf.Lerp(StartScale, EndScale, pct);

                color.a   = 1f - pct;
                mat.color = color;

                yield return null;
            }

            Destroy(go);
        }

        private static GameObject CreateCircleQuad(Vector3 worldPos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "Effect";
            go.transform.position   = worldPos + new Vector3(0f, 0f, -0.05f);
            go.transform.localScale = Vector3.one * StartScale;

            Destroy(go.GetComponent<MeshCollider>());

            var rend = go.GetComponent<MeshRenderer>();
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;

            return go;
        }

        // ── A7: Combo text ──────────────────────────────────────────────────────

        private static IEnumerator ComboTextCoroutine(Vector3 worldPos)
        {
            var go = new GameObject("ComboText");
            go.transform.position = worldPos + new Vector3(0f, 0.3f, -0.15f);

            var tm = go.AddComponent<TextMesh>();
            tm.text      = "COMBO!";
            tm.fontSize  = 60;
            tm.alignment = TextAlignment.Center;
            tm.anchor    = TextAnchor.MiddleCenter;
            tm.color     = new Color(1.0f, 0.85f, 0.15f);

            const float duration = 0.55f;
            float t = 0f;
            var startScale = Vector3.one * 0.010f;
            var peakScale  = Vector3.one * 0.032f;

            while (t < duration)
            {
                t += Time.deltaTime;
                float pct = t / duration;

                // Scale: grow 0→0.5, shrink 0.5→1
                float scalePct  = pct < 0.5f ? pct * 2f : 1f - (pct - 0.5f) * 2f;
                go.transform.localScale = Vector3.Lerp(startScale, peakScale, scalePct);

                // Move upward
                go.transform.position = worldPos + new Vector3(0f, 0.3f + pct * 0.6f, -0.15f);

                // Fade alpha
                float alpha = pct < 0.7f ? 1f : 1f - (pct - 0.7f) / 0.3f;
                tm.color = new Color(1.0f, 0.85f, 0.15f, alpha);

                yield return null;
            }

            Destroy(go);
        }

        // ── A7: Camera shake ────────────────────────────────────────────────────

        private static IEnumerator CameraShakeCoroutine(Camera cam, float magnitude, float duration)
        {
            var originalPos = cam.transform.localPosition;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float pct = 1f - t / duration; // fade out shake
                cam.transform.localPosition = originalPos + new Vector3(
                    Random.Range(-1f, 1f) * magnitude * pct,
                    Random.Range(-1f, 1f) * magnitude * pct,
                    0f);
                yield return null;
            }
            cam.transform.localPosition = originalPos;
        }
    }
}
