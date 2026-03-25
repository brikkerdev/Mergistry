using System;
using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.UI.Popups;
using UnityEngine;

namespace Mergistry.UI.Screens
{
    public class EventScreenView : MonoBehaviour
    {
        public event Action<int> OnChoiceClicked;
        public event Action      OnContinueClicked;

        private TextMesh _titleText;
        private TextMesh _descText;
        private GameObject _continueBtn;
        private readonly List<GameObject> _choiceButtons = new List<GameObject>();

        private void Awake()
        {
            BuildStaticLayout();
            gameObject.SetActive(false);
        }

        public void Show(EventDefinition eventDef)
        {
            ClearChoiceButtons();
            _titleText.text = eventDef.Title;
            _descText.text  = eventDef.Description;
            _continueBtn.SetActive(false);

            // Build choice buttons
            float startY = -0.4f;
            float btnGap = 0.75f;
            for (int i = 0; i < eventDef.Choices.Count; i++)
            {
                int idx = i;
                var choice = eventDef.Choices[i];
                var btn = BuildChoiceButton($"Choice_{i}", transform,
                    new Vector3(0f, startY - i * btnGap, -0.1f),
                    new Vector3(4.0f, 0.55f, 1f),
                    choice.Label,
                    new Color(0.22f, 0.45f, 0.70f),
                    () => OnChoiceClicked?.Invoke(idx));
                _choiceButtons.Add(btn);
            }

            gameObject.SetActive(true);
        }

        public void ShowResult(string resultText)
        {
            ClearChoiceButtons();
            _descText.text = resultText;
            _continueBtn.SetActive(true);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            ClearChoiceButtons();
            gameObject.SetActive(false);
        }

        private void ClearChoiceButtons()
        {
            foreach (var btn in _choiceButtons)
                if (btn != null) Destroy(btn);
            _choiceButtons.Clear();
        }

        private void BuildStaticLayout()
        {
            MakeQuad("Overlay", transform, Vector3.zero,
                new Vector3(20f, 20f, 1f), new Color(0.04f, 0.05f, 0.14f));

            MakeQuad("PanelBorder", transform, new Vector3(0f, 0.3f, -0.04f),
                new Vector3(5.0f, 5.0f, 1f), new Color(0.30f, 0.42f, 0.65f));
            MakeQuad("Panel", transform, new Vector3(0f, 0.3f, -0.05f),
                new Vector3(4.8f, 4.8f, 1f), new Color(0.08f, 0.10f, 0.20f));

            _titleText = MakeLabel("Title", transform,
                new Vector3(0f, 2.1f, -0.1f), 0.032f, 150);
            _titleText.color = new Color(0.9f, 0.75f, 0.3f);

            _descText = MakeLabel("Desc", transform,
                new Vector3(0f, 1.2f, -0.1f), 0.016f, 150);
            _descText.color = new Color(0.75f, 0.78f, 0.92f);

            // Continue button (hidden by default, shown after outcome)
            _continueBtn = BuildChoiceButton("ContinueBtn", transform,
                new Vector3(0f, -1.6f, -0.1f),
                new Vector3(3.2f, 0.6f, 1f), "Продолжить",
                new Color(0.22f, 0.50f, 0.80f),
                () => OnContinueClicked?.Invoke());
            _continueBtn.SetActive(false);
        }

        private GameObject BuildChoiceButton(string goName, Transform parent, Vector3 localPos,
            Vector3 size, string label, Color faceColor, Action onClick)
        {
            var btn = new GameObject(goName);
            btn.transform.SetParent(parent);
            btn.transform.localPosition = localPos;

            MakeQuad("Border", btn.transform, new Vector3(0f, 0f, 0.01f),
                new Vector3(size.x + 0.12f, size.y + 0.10f, 1f),
                new Color(0.45f, 0.50f, 0.72f));
            MakeQuad("Face", btn.transform, Vector3.zero, size, faceColor);

            var lbl = MakeLabel("Label", btn.transform, new Vector3(0f, 0f, -0.01f), 0.018f, 150);
            lbl.text  = label;
            lbl.color = Color.white;

            var col  = btn.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 0.1f);

            var handler = btn.AddComponent<SlotClickHandler>();
            handler.OnClicked = () => onClick?.Invoke();

            return btn;
        }

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
