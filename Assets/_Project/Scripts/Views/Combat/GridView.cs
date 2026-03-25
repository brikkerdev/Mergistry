using System.Collections.Generic;
using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Renders the 5×5 combat grid. Cells are dark quads with thin separator gaps.
    /// Exposes highlight helpers and world↔grid coordinate conversion.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        private const float CellSize = 1.0f;
        private const float CellGap  = 0.06f;
        private const float CellStep = CellSize + CellGap;

        private static readonly Color CellColor      = new Color(0.13f, 0.15f, 0.20f);
        private static readonly Color HighlightColor = new Color(0.20f, 0.50f, 0.80f);

        private MeshRenderer[,] _highlightRenderers;

        private void Awake() => Build();

        // ── Build ─────────────────────────────────────────────────────────────

        private void Build()
        {
            _highlightRenderers = new MeshRenderer[GridModel.Width, GridModel.Height];

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

                    // Highlight overlay (slightly in front, hidden by default)
                    var hlGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    hlGo.name = $"Hl_{x}_{y}";
                    hlGo.transform.SetParent(cellGo.transform);
                    hlGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
                    hlGo.transform.localScale     = Vector3.one * 0.92f;
                    Destroy(hlGo.GetComponent<MeshCollider>());

                    var hlRend = hlGo.GetComponent<MeshRenderer>();
                    hlRend.material = new Material(Shader.Find("Unlit/Color")) { color = HighlightColor };
                    hlRend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    hlRend.receiveShadows    = false;
                    hlGo.SetActive(false);

                    _highlightRenderers[x, y] = hlRend;
                }
            }
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

        // ── Highlights ────────────────────────────────────────────────────────

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
