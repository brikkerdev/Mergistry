using System;
using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.UI.Popups;
using UnityEngine;

namespace Mergistry.UI.Screens
{
    /// <summary>
    /// Shows 3 relic cards after an elite fight. Player picks one.
    /// </summary>
    public class RelicChoiceScreenView : MonoBehaviour
    {
        public event Action<int> OnRelicChosen;

        private TextMesh _titleText;
        private readonly List<GameObject> _cards = new List<GameObject>();

        private void Awake()
        {
            BuildStaticLayout();
            gameObject.SetActive(false);
        }

        public void Show(List<RelicType> relics)
        {
            ClearCards();
            _titleText.text = "Выберите реликвию";

            // Portrait layout: cards stacked vertically
            float cardHeight = 1.5f;
            float cardGap    = 0.35f;
            float totalHeight = relics.Count * cardHeight + (relics.Count - 1) * cardGap;
            float startY      = totalHeight * 0.5f - cardHeight * 0.5f;

            for (int i = 0; i < relics.Count; i++)
            {
                int idx   = i;
                var entry = RelicDatabase.Get(relics[i]);
                var card  = BuildRelicCard(entry,
                    new Vector3(0f, startY - i * (cardHeight + cardGap), -0.1f),
                    () => OnRelicChosen?.Invoke(idx));
                _cards.Add(card);
            }

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            ClearCards();
            gameObject.SetActive(false);
        }

        private void ClearCards()
        {
            foreach (var card in _cards)
                if (card != null) Destroy(card);
            _cards.Clear();
        }

        private void BuildStaticLayout()
        {
            // Full-screen dark overlay
            MakeQuad("Overlay", transform, Vector3.zero,
                new Vector3(20f, 20f, 1f), new Color(0.04f, 0.05f, 0.14f));

            // Title — larger for portrait
            _titleText = MakeLabel("Title", transform,
                new Vector3(0f, 3.2f, -0.1f), 0.038f, 150);
            _titleText.color = new Color(0.95f, 0.80f, 0.25f);
        }

        private GameObject BuildRelicCard(RelicEntry entry, Vector3 localPos, Action onClick)
        {
            var card = new GameObject($"RelicCard_{entry.Type}");
            card.transform.SetParent(transform);
            card.transform.localPosition = localPos;

            // Wide horizontal card for portrait orientation
            float cardW = 4.6f;
            float cardH = 1.3f;

            // Card border
            MakeQuad("Border", card.transform, new Vector3(0f, 0f, 0.01f),
                new Vector3(cardW + 0.14f, cardH + 0.12f, 1f), new Color(0.50f, 0.40f, 0.70f));

            // Card background
            MakeQuad("BG", card.transform, Vector3.zero,
                new Vector3(cardW, cardH, 1f), new Color(0.12f, 0.10f, 0.22f));

            // Icon placeholder (left side)
            var iconColor = GetRelicColor(entry.Type);
            MakeQuad("Icon", card.transform, new Vector3(-1.6f, 0f, -0.01f),
                new Vector3(0.8f, 0.8f, 1f), iconColor);

            // Name (right of icon, upper)
            var nameLabel = MakeLabel("Name", card.transform,
                new Vector3(0.4f, 0.22f, -0.01f), 0.024f, 150);
            nameLabel.text  = entry.Name;
            nameLabel.color = new Color(0.95f, 0.90f, 0.50f);

            // Description (right of icon, lower)
            var descLabel = MakeLabel("Desc", card.transform,
                new Vector3(0.4f, -0.25f, -0.01f), 0.015f, 150);
            descLabel.text  = entry.Description;
            descLabel.color = new Color(0.70f, 0.72f, 0.85f);

            // Click area
            var col  = card.AddComponent<BoxCollider>();
            col.size = new Vector3(cardW, cardH, 0.1f);

            var handler = card.AddComponent<SlotClickHandler>();
            handler.OnClicked = () => onClick?.Invoke();

            return card;
        }

        private static Color GetRelicColor(RelicType type)
        {
            return type switch
            {
                RelicType.Thermos => new Color(0.90f, 0.50f, 0.20f), // orange
                RelicType.Lens    => new Color(0.40f, 0.70f, 0.95f), // blue
                RelicType.Flask   => new Color(0.30f, 0.80f, 0.40f), // green
                RelicType.Cube    => new Color(0.85f, 0.85f, 0.30f), // yellow
                RelicType.Prism   => new Color(0.80f, 0.35f, 0.85f), // purple
                _                 => Color.gray
            };
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
