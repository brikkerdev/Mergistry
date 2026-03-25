using System;
using Mergistry.UI.Popups;
using UnityEngine;

namespace Mergistry.UI.Screens
{
    /// <summary>
    /// World-space result screen shown on victory or defeat.
    /// Follows the same pattern as SlotReplacePopup: quads + BoxColliders + SlotClickHandler.
    /// Attach to a GameObject centered on the scene. Activates/deactivates itself.
    /// </summary>
    public class ResultScreenView : MonoBehaviour
    {
        public event Action OnRetryClicked;
        public event Action OnMenuClicked;

        private TextMesh _titleText;
        private TextMesh _subtitleText;

        private void Awake()
        {
            Build();
            gameObject.SetActive(false);
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void Show(bool victory, int fightIndex)
        {
            if (victory)
            {
                _titleText.text    = "Победа!";
                _subtitleText.text = "Этаж пройден";
            }
            else
            {
                _titleText.text    = "Вы пали";
                _subtitleText.text = $"Этаж 1, бой {fightIndex + 1}";
            }
            gameObject.SetActive(true);
        }

        public void Hide() => gameObject.SetActive(false);

        // ── Build ────────────────────────────────────────────────────────────

        private void Build()
        {
            // Full-screen dark overlay (large enough to cover any camera size)
            MakeQuad("Overlay", transform, Vector3.zero,
                new Vector3(20f, 20f, 1f), new Color(0.03f, 0.04f, 0.12f));

            // Panel
            MakeQuad("Panel", transform, new Vector3(0f, 0.3f, -0.05f),
                new Vector3(4.8f, 3.6f, 1f), new Color(0.10f, 0.11f, 0.22f));

            MakeQuad("PanelBorder", transform, new Vector3(0f, 0.3f, -0.04f),
                new Vector3(5.0f, 3.8f, 1f), new Color(0.35f, 0.40f, 0.65f));

            // Title TextMesh
            _titleText = MakeLabel("Title", transform,
                new Vector3(0f, 1.4f, -0.1f), 0.040f, 72);
            _titleText.text  = "Результат";
            _titleText.color = new Color(0.95f, 0.90f, 0.60f);

            // Subtitle TextMesh
            _subtitleText = MakeLabel("Subtitle", transform,
                new Vector3(0f, 0.65f, -0.1f), 0.022f, 48);
            _subtitleText.text  = "";
            _subtitleText.color = new Color(0.70f, 0.70f, 0.88f);

            // Retry button
            BuildButton("RetryBtn", transform, new Vector3(0f, -0.25f, -0.1f),
                new Vector3(3.2f, 0.60f, 1f),
                "Ещё раз",
                new Color(0.22f, 0.50f, 0.80f),
                () => OnRetryClicked?.Invoke());

            // Menu button
            BuildButton("MenuBtn", transform, new Vector3(0f, -1.10f, -0.1f),
                new Vector3(3.2f, 0.60f, 1f),
                "Меню",
                new Color(0.22f, 0.22f, 0.40f),
                () => OnMenuClicked?.Invoke());
        }

        private void BuildButton(string goName, Transform parent, Vector3 localPos,
            Vector3 size, string label, Color faceColor, Action onClick)
        {
            var btn = new GameObject(goName);
            btn.transform.SetParent(parent);
            btn.transform.localPosition = localPos;

            // Border (slightly larger)
            MakeQuad("Border", btn.transform, new Vector3(0f, 0f, 0.01f),
                new Vector3(size.x + 0.12f, size.y + 0.10f, 1f),
                new Color(0.45f, 0.50f, 0.72f));

            // Face
            MakeQuad("Face", btn.transform, Vector3.zero, size, faceColor);

            // Label
            var lbl = MakeLabel("Label", btn.transform,
                new Vector3(0f, 0f, -0.01f), 0.020f, 56);
            lbl.text  = label;
            lbl.color = Color.white;

            // Click detection
            var col  = btn.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 0.1f);

            var handler = btn.AddComponent<SlotClickHandler>();
            handler.OnClicked = () => onClick?.Invoke();
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void MakeQuad(string goName, Transform parent,
            Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = goName;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale    = scale;
            Destroy(go.GetComponent<MeshCollider>());

            var rend = go.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Unlit/Color")) { color = color };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;
        }

        private static TextMesh MakeLabel(string goName, Transform parent,
            Vector3 localPos, float scale, int fontSize)
        {
            var go = new GameObject(goName);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale    = Vector3.one * scale;

            var tm       = go.AddComponent<TextMesh>();
            tm.fontSize  = fontSize;
            tm.fontStyle = FontStyle.Bold;
            tm.color     = Color.white;
            tm.anchor    = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            return tm;
        }
    }
}
