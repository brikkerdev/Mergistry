using System.Collections;
using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Renders the 5×5 combat grid. Cells are dark quads with thin separator gaps.
    /// Exposes highlight helpers (blue = valid targets, red = AoE) and
    /// world↔grid coordinate conversion.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        private const float CellSize = 1.0f;
        private const float CellGap  = 0.06f;
        private const float CellStep = CellSize + CellGap;

        private static readonly Color CellColor      = new Color(0.13f, 0.15f, 0.20f);
        private static readonly Color HighlightBlue  = new Color(0.20f, 0.50f, 0.80f);
        private static readonly Color HighlightRed   = new Color(0.85f, 0.20f, 0.15f);

        // Blue overlays — valid movement/throw targets
        private MeshRenderer[,] _highlightRenderers;
        // Red overlays — AoE preview
        private MeshRenderer[,] _aoeRenderers;

        private void Awake() => Build();

        // ── Build ─────────────────────────────────────────────────────────────

        private void Build()
        {
            _highlightRenderers = new MeshRenderer[GridModel.Width, GridModel.Height];
            _aoeRenderers       = new MeshRenderer[GridModel.Width, GridModel.Height];

            for (int x = 0; x < GridModel.Width; x++)
            {
                for (int y = 0; y < GridModel.Height; y++)
                {
                    var cellGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    cellGo.name = $"Cell_{x}_{y}";
                    cellGo.transform.SetParent(transform);
                    cellGo.transform.localPosition = LocalPos(x, y);
                    cellGo.transform.localScale    = Vector3.one * CellSize;
                    Destroy(cellGo.GetComponent<MeshCollider>());

                    var rend = cellGo.GetComponent<MeshRenderer>();
                    rend.material = new Material(Shader.Find("Unlit/Color")) { color = CellColor };
                    rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    rend.receiveShadows    = false;

                    // Blue highlight overlay (valid targets)
                    _highlightRenderers[x, y] = MakeOverlay(cellGo.transform, "Hl", -0.01f, HighlightBlue);

                    // Red AoE overlay (slightly closer than blue)
                    _aoeRenderers[x, y] = MakeOverlay(cellGo.transform, "Aoe", -0.02f, HighlightRed);
                }
            }
        }

        private static MeshRenderer MakeOverlay(Transform parent, string name, float localZ, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0f, 0f, localZ);
            go.transform.localScale    = Vector3.one * 0.92f;
            Destroy(go.GetComponent<MeshCollider>());

            var rend = go.GetComponent<MeshRenderer>();
            rend.material = new Material(Shader.Find("Unlit/Color")) { color = color };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;
            go.SetActive(false);
            return rend;
        }

        // ── Coordinate conversion ─────────────────────────────────────────────

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return transform.position + (Vector3)LocalPos(gridPos.x, gridPos.y);
        }

        public Vector2Int? WorldToGrid(Vector3 worldPos)
        {
            var local = worldPos - transform.position;
            int x = Mathf.RoundToInt((local.x + OriginOffset()) / CellStep);
            int y = Mathf.RoundToInt((local.y + OriginOffset()) / CellStep);
            if (x >= 0 && x < GridModel.Width && y >= 0 && y < GridModel.Height)
                return new Vector2Int(x, y);
            return null;
        }

        // ── Blue highlights (valid move/throw targets) ────────────────────────

        public void SetHighlights(List<Vector2Int> cells)
        {
            ClearHighlights();
            foreach (var c in cells)
                _highlightRenderers[c.x, c.y].gameObject.SetActive(true);
        }

        public void ClearHighlights()
        {
            for (int x = 0; x < GridModel.Width;  x++)
            for (int y = 0; y < GridModel.Height; y++)
                _highlightRenderers[x, y].gameObject.SetActive(false);
        }

        // ── Red AoE highlights ────────────────────────────────────────────────

        public void SetAoeHighlights(List<Vector2Int> cells)
        {
            ClearAoeHighlights();
            foreach (var c in cells)
                _aoeRenderers[c.x, c.y].gameObject.SetActive(true);
        }

        public void ClearAoeHighlights()
        {
            for (int x = 0; x < GridModel.Width;  x++)
            for (int y = 0; y < GridModel.Height; y++)
                _aoeRenderers[x, y].gameObject.SetActive(false);
        }

        /// <summary>Shows red AoE highlights and auto-clears them after <paramref name="duration"/> seconds.</summary>
        public void SetAoeHighlightsTemporary(List<Vector2Int> cells, float duration = 0.4f)
        {
            SetAoeHighlights(cells);
            StartCoroutine(ClearAoeAfterDelay(duration));
        }

        private IEnumerator ClearAoeAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ClearAoeHighlights();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Vector3 LocalPos(int x, int y)
        {
            float offset = OriginOffset();
            return new Vector3(x * CellStep - offset, y * CellStep - offset, 0f);
        }

        private static float OriginOffset() =>
            (GridModel.Width - 1) * CellStep * 0.5f;
    }
}
