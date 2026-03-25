using System.Collections;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Spawns an expanding circle visual at a world position for potion impact effects.
    /// Uses "Sprites/Default" shader so alpha fade works without a custom shader.
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
    }
}
