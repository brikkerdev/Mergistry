using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mergistry.UI
{
    /// <summary>
    /// Screen-space black overlay used for fade transitions between states.
    /// Attach to any GameObject; it creates its own Canvas.
    /// </summary>
    public class FadeView : MonoBehaviour
    {
        private Image _image;

        private void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            // No GraphicRaycaster — the overlay must never block input.

            var imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(transform, false);

            var rt         = imgGo.AddComponent<RectTransform>();
            rt.anchorMin   = Vector2.zero;
            rt.anchorMax   = Vector2.one;
            rt.offsetMin   = Vector2.zero;
            rt.offsetMax   = Vector2.zero;

            _image              = imgGo.AddComponent<Image>();
            _image.color        = Color.clear;
            _image.raycastTarget = false; // Never block input
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Fade from transparent to black, then invoke callback.</summary>
        public void FadeOut(float duration, Action onComplete) =>
            StartCoroutine(DoFade(0f, 1f, duration, onComplete));

        /// <summary>Fade from black to transparent, then invoke callback.</summary>
        public void FadeIn(float duration, Action onComplete) =>
            StartCoroutine(DoFade(1f, 0f, duration, onComplete));

        // ── Coroutine ─────────────────────────────────────────────────────────

        private IEnumerator DoFade(float from, float to, float duration, Action onComplete)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed      += Time.deltaTime;
                _image.color  = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, elapsed / duration));
                yield return null;
            }
            _image.color = new Color(0f, 0f, 0f, to);
            onComplete?.Invoke();
        }
    }
}
