using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mergistry.UI.HUD
{
    /// <summary>
    /// World-space HUD: displays 5 heart circles for player HP.
    /// Also owns a screen-space red flash overlay for when the player takes damage.
    /// Position this GameObject at the top of the combat view in the scene.
    /// </summary>
    public class HealthBarView : MonoBehaviour
    {
        private const int   MaxHearts  = 5;
        private const float HeartSize  = 0.38f;
        private const float HeartGap   = 0.52f;

        private static readonly Color HeartFull  = new Color(0.85f, 0.20f, 0.20f);
        private static readonly Color HeartEmpty = new Color(0.28f, 0.20f, 0.28f);

        private MeshRenderer[] _heartRenderers;
        private Image          _flashImage;

        private void Awake()
        {
            BuildHearts();
            BuildFlashOverlay();
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void Refresh(int hp, int maxHp)
        {
            for (int i = 0; i < MaxHearts; i++)
                _heartRenderers[i].material.color = (i < hp) ? HeartFull : HeartEmpty;
        }

        public void PlayDamageFlash()
        {
            StopAllCoroutines();
            StartCoroutine(FlashRoutine());
        }

        // ── Build ────────────────────────────────────────────────────────────

        private void BuildHearts()
        {
            _heartRenderers = new MeshRenderer[MaxHearts];

            float totalWidth = (MaxHearts - 1) * HeartGap;
            float startX     = -totalWidth * 0.5f;

            for (int i = 0; i < MaxHearts; i++)
            {
                var go = new GameObject($"Heart_{i}");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(startX + i * HeartGap, 0f, 0f);
                go.transform.localScale    = Vector3.one * HeartSize;

                var mf  = go.AddComponent<MeshFilter>();
                mf.mesh = CircleMesh(0.5f, 16);

                var mr = go.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows    = false;
                mr.material          = new Material(Shader.Find("Unlit/Color")) { color = HeartFull };

                _heartRenderers[i] = mr;
            }
        }

        private void BuildFlashOverlay()
        {
            var canvasGo = new GameObject("DamageFlashCanvas");
            canvasGo.transform.SetParent(transform);

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            canvasGo.AddComponent<CanvasScaler>();

            var imgGo = new GameObject("FlashImage");
            imgGo.transform.SetParent(canvasGo.transform, false);

            var rt       = imgGo.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            _flashImage              = imgGo.AddComponent<Image>();
            _flashImage.color        = Color.clear;
            _flashImage.raycastTarget = false;
        }

        private IEnumerator FlashRoutine()
        {
            var red = new Color(0.80f, 0.10f, 0.10f, 0.50f);
            _flashImage.color = red;

            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed           += Time.deltaTime;
                _flashImage.color  = Color.Lerp(red, Color.clear, elapsed / 0.2f);
                yield return null;
            }
            _flashImage.color = Color.clear;
        }

        // ── Mesh helper ──────────────────────────────────────────────────────

        private static Mesh CircleMesh(float r, int seg)
        {
            var mesh  = new Mesh { name = "HeartCircle" };
            var verts = new Vector3[seg + 1];
            var tris  = new int[seg * 3];
            verts[0]  = Vector3.zero;
            for (int i = 0; i < seg; i++)
            {
                float a      = 2f * Mathf.PI * i / seg;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = (i + 1) % seg + 1;
                tris[i * 3 + 2] = i + 1;
            }
            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
