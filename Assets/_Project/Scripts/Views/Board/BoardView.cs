using Mergistry.Data;
using Mergistry.Models;
using UnityEngine;

namespace Mergistry.Views.Board
{
    public class BoardView : MonoBehaviour
    {
        [SerializeField] private float cellSize    = 1.1f;
        [SerializeField] private float cellSpacing = 0.1f;

        private GameObject[,]    _cellObjects      = new GameObject[BoardModel.Size, BoardModel.Size];
        private GameObject[,]    _highlightObjects = new GameObject[BoardModel.Size, BoardModel.Size];
        private IngredientView[,] _ingredientViews = new IngredientView[BoardModel.Size, BoardModel.Size];
        private BrewView[,]       _brewViews        = new BrewView[BoardModel.Size, BoardModel.Size];

        private BoardModel _board;

        // ── Public API ──────────────────────────────────────────────────────

        public void Initialize(BoardModel board)
        {
            Clear();
            _board = board;
            CreateCells();
            CreateIngredients();
        }

        public void Clear()
        {
            foreach (Transform child in transform)
                Destroy(child.gameObject);

            _cellObjects      = new GameObject[BoardModel.Size, BoardModel.Size];
            _highlightObjects = new GameObject[BoardModel.Size, BoardModel.Size];
            _ingredientViews  = new IngredientView[BoardModel.Size, BoardModel.Size];
            _brewViews         = new BrewView[BoardModel.Size, BoardModel.Size];
        }

        public Vector3 GetCellWorldPosition(int x, int y) =>
            transform.position + LocalOffset(x, y);

        public bool TryGetGridPosition(Vector3 worldPos, out int x, out int y)
        {
            float step      = cellSize + cellSpacing;
            float halfBoard = (BoardModel.Size - 1) * step * 0.5f;
            Vector3 local   = worldPos - transform.position;

            x = Mathf.RoundToInt((local.x + halfBoard) / step);
            y = Mathf.RoundToInt((local.y + halfBoard) / step);

            bool inBounds = _board != null && _board.IsInBounds(x, y);
            if (!inBounds) { x = -1; y = -1; }
            return inBounds;
        }

        public IngredientView GetIngredientAt(int x, int y) =>
            (_board != null && _board.IsInBounds(x, y)) ? _ingredientViews[x, y] : null;

        public BrewView GetBrewAt(int x, int y) =>
            (_board != null && _board.IsInBounds(x, y)) ? _brewViews[x, y] : null;

        public void SetHighlight(int x, int y, bool active)
        {
            if (_board != null && _board.IsInBounds(x, y))
                _highlightObjects[x, y]?.SetActive(active);
        }

        public void ClearAllHighlights()
        {
            for (int x = 0; x < BoardModel.Size; x++)
                for (int y = 0; y < BoardModel.Size; y++)
                    SetHighlight(x, y, false);
        }

        // ── Brew/Ingredient manipulation ────────────────────────────────────

        public void RemoveIngredient(int x, int y)
        {
            if (_ingredientViews[x, y] == null) return;
            Destroy(_ingredientViews[x, y].gameObject);
            _ingredientViews[x, y] = null;
        }

        public void RemoveBrew(int x, int y)
        {
            if (_brewViews[x, y] == null) return;
            Destroy(_brewViews[x, y].gameObject);
            _brewViews[x, y] = null;
        }

        public void PlaceBrew(int x, int y, PotionType potionType, ElementType element, int level)
        {
            RemoveBrew(x, y);
            var go   = new GameObject($"Brew_{x}_{y}");
            go.transform.SetParent(transform);
            var view = go.AddComponent<BrewView>();
            view.Initialize(potionType, element, level, GetCellWorldPosition(x, y) + new Vector3(0f, 0f, -0.05f));
            _brewViews[x, y] = view;
        }

        public void UpgradeBrew(int x, int y, int newLevel)
        {
            _brewViews[x, y]?.SetLevel(newLevel);
        }

        // ── Private ─────────────────────────────────────────────────────────

        private void CreateCells()
        {
            for (int x = 0; x < BoardModel.Size; x++)
            {
                for (int y = 0; y < BoardModel.Size; y++)
                {
                    var cell = MakeQuad($"Cell_{x}_{y}", transform,
                        LocalOffset(x, y), Vector3.one * cellSize,
                        new Color(0.12f, 0.13f, 0.18f));
                    _cellObjects[x, y] = cell;

                    // Green highlight border (slightly larger, in front)
                    var hl = MakeQuad($"Highlight_{x}_{y}", cell.transform,
                        new Vector3(0f, 0f, -0.01f), Vector3.one * 1.08f,
                        new Color(0.20f, 0.90f, 0.30f));
                    hl.SetActive(false);
                    _highlightObjects[x, y] = hl;
                }
            }
        }

        private void CreateIngredients()
        {
            for (int x = 0; x < BoardModel.Size; x++)
            {
                for (int y = 0; y < BoardModel.Size; y++)
                {
                    var content = _board.Get(x, y);
                    if (content.Type != CellContentType.Ingredient) continue;

                    var go   = new GameObject($"Ingredient_{x}_{y}");
                    go.transform.SetParent(transform);
                    var view = go.AddComponent<IngredientView>();
                    view.Initialize(content.ElementType, GetCellWorldPosition(x, y) + new Vector3(0f, 0f, -0.05f));
                    _ingredientViews[x, y] = view;
                }
            }
        }

        private Vector3 LocalOffset(int x, int y)
        {
            float step      = cellSize + cellSpacing;
            float halfBoard = (BoardModel.Size - 1) * step * 0.5f;
            return new Vector3(x * step - halfBoard, y * step - halfBoard, 0f);
        }

        private static GameObject MakeQuad(string name, Transform parent,
            Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale    = scale;
            Destroy(go.GetComponent<MeshCollider>());
            var rend = go.GetComponent<Renderer>();
            var mat  = new Material(Shader.Find("Unlit/Color")) { color = color };
            rend.material           = mat;
            rend.shadowCastingMode  = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows     = false;
            return go;
        }
    }
}
