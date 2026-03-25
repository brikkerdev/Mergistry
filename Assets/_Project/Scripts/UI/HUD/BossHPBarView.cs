using UnityEngine;

namespace Mergistry.UI.HUD
{
    /// <summary>
    /// A6: Horizontal HP bar for boss fights displayed at the top of the screen.
    /// Built programmatically as world-space quads.
    /// </summary>
    public class BossHPBarView : MonoBehaviour
    {
        private const float BarWidth   = 4.0f;
        private const float BarHeight  = 0.28f;

        private static readonly Color BgColor   = new Color(0.10f, 0.08f, 0.15f);
        private static readonly Color FillColor = new Color(0.80f, 0.12f, 0.12f);
        private static readonly Color Phase2Color = new Color(0.90f, 0.45f, 0.05f);

        private MeshRenderer _fillRenderer;
        private GameObject   _fillGo;
        private TextMesh     _labelText;
        private TextMesh     _phaseText;
        private GameObject   _phaseGo;

        private int   _maxHP;
        private float _fullWidth;

        private void Awake() => BuildBar();

        private void BuildBar()
        {
            var cam = Camera.main;
            var topCenter = cam != null
                ? cam.ViewportToWorldPoint(new Vector3(0.5f, 1f, Mathf.Abs(cam.transform.position.z)))
                : Vector3.up * 4f;

            transform.position = new Vector3(topCenter.x, topCenter.y - 0.42f, -0.5f);

            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");

            // Background
            var bgGo  = MakeQuad("BossHPBg", Vector3.zero, new Vector3(BarWidth + 0.08f, BarHeight + 0.08f, 1f), BgColor, shader);

            // Fill bar
            _fillGo      = MakeQuad("BossHPFill", new Vector3(-BarWidth * 0.5f, 0f, -0.01f),
                                    new Vector3(BarWidth, BarHeight * 0.76f, 1f), FillColor, shader);
            _fillGo.transform.localPosition = new Vector3(-BarWidth * 0.5f + BarWidth * 0.5f, 0f, -0.01f);
            _fillRenderer                   = _fillGo.GetComponent<MeshRenderer>();
            _fullWidth                      = BarWidth;

            // Name label
            var labelGo = new GameObject("BossLabel");
            labelGo.transform.SetParent(transform);
            labelGo.transform.localPosition = new Vector3(0f, 0.24f, -0.02f);
            labelGo.transform.localScale    = Vector3.one * 0.014f;
            _labelText           = labelGo.AddComponent<TextMesh>();
            _labelText.fontSize  = 150;
            _labelText.alignment = TextAlignment.Center;
            _labelText.anchor    = TextAnchor.MiddleCenter;
            _labelText.color     = new Color(0.95f, 0.88f, 0.55f);

            // Phase flash text (hidden until phase change)
            _phaseGo = new GameObject("PhaseText");
            _phaseGo.transform.SetParent(transform);
            _phaseGo.transform.localPosition = new Vector3(0f, -0.28f, -0.02f);
            _phaseGo.transform.localScale    = Vector3.one * 0.018f;
            _phaseText           = _phaseGo.AddComponent<TextMesh>();
            _phaseText.fontSize  = 150;
            _phaseText.alignment = TextAlignment.Center;
            _phaseText.anchor    = TextAnchor.MiddleCenter;
            _phaseText.color     = Phase2Color;
            _phaseText.text      = "— PHASE 2 —";
            _phaseGo.SetActive(false);

            gameObject.SetActive(false);
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(int hp, int maxHP, string bossName)
        {
            _maxHP            = maxHP;
            _labelText.text   = bossName;
            gameObject.SetActive(true);
            SetHP(hp, maxHP);
        }

        public void SetHP(int hp, int maxHP)
        {
            float ratio = maxHP > 0 ? Mathf.Clamp01((float)hp / maxHP) : 0f;
            var pos       = _fillGo.transform.localPosition;
            float newW    = _fullWidth * ratio;
            _fillGo.transform.localPosition = new Vector3(-_fullWidth * 0.5f + newW * 0.5f, pos.y, pos.z);
            _fillGo.transform.localScale    = new Vector3(newW, BarHeight * 0.76f, 1f);
        }

        public void ShowPhase2()
        {
            if (_fillRenderer != null)
                _fillRenderer.material.color = Phase2Color;
            _phaseGo.SetActive(true);
            StartCoroutine(FadePhaseText());
        }

        public void Hide() => gameObject.SetActive(false);

        // ── Helpers ───────────────────────────────────────────────────────────

        private System.Collections.IEnumerator FadePhaseText()
        {
            yield return new WaitForSeconds(1.5f);
            _phaseGo.SetActive(false);
        }

        private GameObject MakeQuad(string goName, Vector3 localPos, Vector3 localScale, Color color, Shader shader)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = goName;
            go.transform.SetParent(transform);
            go.transform.localPosition = localPos;
            go.transform.localScale    = localScale;
            Destroy(go.GetComponent<MeshCollider>());
            var rend = go.GetComponent<MeshRenderer>();
            rend.material = new Material(shader) { color = color };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;
            return go;
        }
    }
}
