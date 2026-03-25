using Mergistry.Data;
using Mergistry.UI.Popups;
using Mergistry.Views.Board;
using UnityEngine;

namespace Mergistry.UI.Screens
{
    /// <summary>
    /// World-space recipe book — paginated, 5 potions per page (3 pages total).
    /// Page 1: base brews. Page 2: recipe brews 1-5. Page 3: recipe brews 6-10.
    /// Designed for portrait ~5.6 × 10 world-unit view.
    /// </summary>
    public class BookScreen : MonoBehaviour
    {
        // ── Panel dimensions (fits portrait screen ~5.6 wu wide) ──────────────
        private const float PanelW   = 4.8f;
        private const float PanelH   = 8.8f;
        private const float PanelZ   = -1.5f;

        private const float TitleY   = 3.9f;
        private const float SubtitleY= 3.15f;
        private const float EntryTopY= 2.55f;
        private const float EntryStep= 0.88f;  // vertical spacing between entries
        private const float NavY     = -3.2f;

        private const float EntryX   = -1.80f; // left edge of entry text
        private const float DotX     = -2.08f; // coloured dot x

        // Text scales (world-space TextMesh)
        private const float ScaleLg  = 0.022f; // title
        private const float ScaleMd  = 0.016f; // section header / entry name
        private const float ScaleSm  = 0.013f; // entry details

        private const int   FntLg    = 150;
        private const int   FntMd    = 150;
        private const int   FntSm    = 150;

        // ── Pages ─────────────────────────────────────────────────────────────
        private static readonly string[] PageTitles =
            { "-- БАЗОВЫЕ ЗЕЛЬЯ --", "-- РЕЦЕПТУРНЫЕ (1-5) --", "-- РЕЦЕПТУРНЫЕ (6-10) --" };

        private const int PagesTotal    = 3;
        private const int EntriesPerPage= 5;

        // ── Runtime ───────────────────────────────────────────────────────────
        private int          _page;          // 0-2
        private TextMesh     _subtitleText;
        private TextMesh     _pageLabel;
        private GameObject[] _entryRoots;   // 5 slots, reused per page

        // ── Public API ────────────────────────────────────────────────────────
        public void Show()   => gameObject.SetActive(true);
        public void Hide()   => gameObject.SetActive(false);
        public void Toggle() { if (gameObject.activeSelf) Hide(); else Show(); }

        // ── Unity ─────────────────────────────────────────────────────────────
        private void Awake()
        {
            transform.position = new Vector3(0f, 0f, PanelZ);
            BuildStaticLayout();
            ShowPage(0);
            gameObject.SetActive(false); // hidden until opened explicitly
        }

        // ── Build static frame ────────────────────────────────────────────────
        private void BuildStaticLayout()
        {
            // Background
            MakeQuad("BG", transform, Vector3.zero, new Vector3(PanelW, PanelH, 1f),
                new Color(0.05f, 0.05f, 0.12f));
            // Border
            MakeQuad("Border", transform, new Vector3(0f, 0f, 0.01f),
                new Vector3(PanelW + 0.10f, PanelH + 0.10f, 1f),
                new Color(0.42f, 0.35f, 0.65f));

            // Title
            MakeText("Title", transform, new Vector3(-0.4f, TitleY, -0.02f),
                "КНИГА РЕЦЕПТОВ", FntLg, ScaleLg, new Color(0.95f, 0.88f, 0.45f),
                TextAnchor.MiddleLeft);

            // Close button
            BuildButton("CloseBtn", transform,
                new Vector3(PanelW * 0.5f - 0.36f, TitleY, -0.03f),
                new Vector3(0.55f, 0.44f, 1f),
                "X", new Color(0.70f, 0.18f, 0.18f), new Color(0.85f, 0.22f, 0.22f),
                () => Hide());

            // Separator
            MakeQuad("Sep", transform,
                new Vector3(0f, TitleY - 0.38f, -0.01f),
                new Vector3(PanelW - 0.3f, 0.022f, 1f),
                new Color(0.38f, 0.32f, 0.58f));

            // Subtitle (changes per page)
            _subtitleText = MakeText("Subtitle", transform,
                new Vector3(0f, SubtitleY, -0.02f),
                "", FntMd, ScaleMd, new Color(0.78f, 0.78f, 0.50f),
                TextAnchor.MiddleCenter);

            // Entry slots (5 reusable containers)
            _entryRoots = new GameObject[EntriesPerPage];
            for (int i = 0; i < EntriesPerPage; i++)
            {
                var root = new GameObject($"EntrySlot_{i}");
                root.transform.SetParent(transform);
                root.transform.localPosition = new Vector3(0f, EntryTopY - i * EntryStep, -0.02f);
                _entryRoots[i] = root;
            }

            // Nav bar: Prev | label | Next
            BuildButton("PrevBtn", transform,
                new Vector3(-1.5f, NavY, -0.03f),
                new Vector3(1.0f, 0.44f, 1f),
                "< НАЗАД", new Color(0.22f, 0.22f, 0.38f), new Color(0.30f, 0.30f, 0.50f),
                () => ShowPage(_page - 1));

            _pageLabel = MakeText("PageLabel", transform,
                new Vector3(0f, NavY, -0.02f),
                "1 / 3", FntSm, ScaleSm, new Color(0.65f, 0.65f, 0.65f),
                TextAnchor.MiddleCenter);

            BuildButton("NextBtn", transform,
                new Vector3(1.5f, NavY, -0.03f),
                new Vector3(1.0f, 0.44f, 1f),
                "ДАЛЕЕ >", new Color(0.22f, 0.22f, 0.38f), new Color(0.30f, 0.30f, 0.50f),
                () => ShowPage(_page + 1));
        }

        // ── Page display ──────────────────────────────────────────────────────
        private void ShowPage(int page)
        {
            _page = Mathf.Clamp(page, 0, PagesTotal - 1);

            _subtitleText.text = PageTitles[_page];
            _pageLabel.text    = $"{_page + 1} / {PagesTotal}";

            int startIndex = _page * EntriesPerPage;

            for (int slot = 0; slot < EntriesPerPage; slot++)
            {
                // Clear previous content
                var root = _entryRoots[slot];
                for (int c = root.transform.childCount - 1; c >= 0; c--)
                    Destroy(root.transform.GetChild(c).gameObject);

                int dataIndex = startIndex + slot;
                if (dataIndex >= PotionDatabase.All.Length) continue;

                var entry = PotionDatabase.All[dataIndex];
                PopulateEntrySlot(root.transform, entry);
            }
        }

        private void PopulateEntrySlot(Transform root, PotionEntry entry)
        {
            // Row background (subtle)
            MakeQuad("RowBG", root, new Vector3(0f, 0f, 0.01f),
                new Vector3(PanelW - 0.3f, 0.76f, 1f),
                new Color(0.10f, 0.10f, 0.20f));

            // Colour dot
            MakeQuad("Dot", root, new Vector3(DotX, 0f, 0f),
                new Vector3(0.24f, 0.24f, 1f),
                BrewView.GetBrewColor(entry.Type));

            // Line 1: Name  (Ing+Ing)
            string line1 = $"{entry.Name}   ({PotionDatabase.ElementAbbr(entry.IngredientA)}+{PotionDatabase.ElementAbbr(entry.IngredientB)})";
            MakeText("Line1", root, new Vector3(EntryX, 0.16f, -0.02f),
                line1, FntMd, ScaleMd, Color.white, TextAnchor.MiddleLeft);

            // Line 2: AoE | Dmg/lvl
            string dmg   = $"{entry.DamagePerLevel}/{entry.DamagePerLevel * 2}/{entry.DamagePerLevel * 3}";
            string line2 = $"{entry.AoEPattern}   |   {dmg} ур.";
            MakeText("Line2", root, new Vector3(EntryX, -0.12f, -0.02f),
                line2, FntSm, ScaleSm, new Color(0.72f, 0.82f, 0.95f), TextAnchor.MiddleLeft);
        }

        // ── Helpers ───────────────────────────────────────────────────────────
        private static TextMesh MakeText(string name, Transform parent, Vector3 localPos,
            string text, int fontSize, float scale, Color color, TextAnchor anchor)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale    = Vector3.one * scale;

            var tm = go.AddComponent<TextMesh>();
            tm.text      = text;
            tm.fontSize  = fontSize;
            tm.color     = color;
            tm.anchor    = anchor;
            tm.alignment = TextAlignment.Left;
            return tm;
        }

        private static GameObject MakeQuad(string name, Transform parent, Vector3 localPos,
            Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale    = scale;
            Destroy(go.GetComponent<MeshCollider>());

            var rend = go.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Unlit/Color")) { color = color };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;
            return go;
        }

        private static void BuildButton(string name, Transform parent, Vector3 localPos,
            Vector3 size, string label, Color bgColor, Color faceColor, System.Action onClick)
        {
            var btn = new GameObject(name);
            btn.transform.SetParent(parent);
            btn.transform.localPosition = localPos;

            MakeQuad("BG",   btn.transform, new Vector3(0f, 0f, 0.01f), size, bgColor);
            MakeQuad("Face", btn.transform, Vector3.zero,
                new Vector3(size.x - 0.08f, size.y - 0.08f, 1f), faceColor);

            MakeText("Label", btn.transform, new Vector3(0f, 0f, -0.01f),
                label, FntSm, ScaleSm, Color.white, TextAnchor.MiddleCenter);

            var col  = btn.AddComponent<BoxCollider>();
            col.size = new Vector3(size.x, size.y, 0.1f);

            var handler      = btn.AddComponent<SlotClickHandler>();
            handler.OnClicked = onClick;
        }
    }
}
