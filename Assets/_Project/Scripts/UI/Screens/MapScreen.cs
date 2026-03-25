using System;
using System.Collections.Generic;
using Mergistry.Data;
using Mergistry.Models.Combat;
using Mergistry.Models.Map;
using Mergistry.UI.Popups;
using UnityEngine;

namespace Mergistry.UI.Screens
{
    /// <summary>
    /// World-space floor-map screen. Renders MapNode circles with path lines.
    /// Content is rebuilt each Show() call to reflect updated visited/accessible state.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        public event Action<int> OnNodeClicked;

        private const float RowSpacing = 1.15f;
        private const float ColSpacing = 1.65f;
        private const float BottomY    = -2.3f;

        private readonly List<GameObject> _built = new List<GameObject>();

        private void Awake() => gameObject.SetActive(false);

        // ── Public API ────────────────────────────────────────────────────────

        public void Show(FloorMapModel map, int floor, int currentNodeId = -1)
        {
            Clear();
            RenderMap(map, floor, currentNodeId);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            Clear();
            gameObject.SetActive(false);
        }

        // ── Rendering ─────────────────────────────────────────────────────────

        private void Clear()
        {
            foreach (var go in _built)
                if (go != null) Destroy(go);
            _built.Clear();
        }

        private void RenderMap(FloorMapModel map, int floor, int currentNodeId)
        {
            // ── Background ────────────────────────────────────────────────────
            _built.Add(MakeQuadGo("Overlay", transform, Vector3.zero,
                new Vector3(20f, 20f, 1f), new Color(0.05f, 0.06f, 0.16f)));

            // ── Title ─────────────────────────────────────────────────────────
            var title = MakeLabelGo("Title", transform,
                new Vector3(0f, 3.1f, -0.1f), 0.035f, 150);
            title.GetComponent<TextMesh>().text  = $"Этаж {floor + 1}";
            title.GetComponent<TextMesh>().color = new Color(0.9f, 0.85f, 0.5f);
            _built.Add(title);

            // ── Calculate world positions ─────────────────────────────────────
            var positions = new Dictionary<int, Vector3>();
            foreach (var node in map.Nodes)
            {
                float x = node.TotalCols > 1
                    ? -(node.TotalCols - 1) * ColSpacing * 0.5f + node.Col * ColSpacing
                    : 0f;
                float y = BottomY + node.Row * RowSpacing;
                positions[node.Id] = new Vector3(x, y, -0.1f);
            }

            // ── Paths (drawn first, behind nodes) ─────────────────────────────
            foreach (var node in map.Nodes)
            {
                foreach (int nextId in node.NextNodeIds)
                {
                    if (!positions.ContainsKey(nextId)) continue;
                    _built.Add(BuildPath(positions[node.Id], positions[nextId]));
                }
            }

            // ── Nodes ─────────────────────────────────────────────────────────
            foreach (var node in map.Nodes)
                _built.Add(BuildNode(node, positions[node.Id]));

            // ── "YOU ARE HERE" marker ───────────────────────────────────────
            Vector3 herePos;
            if (currentNodeId >= 0 && positions.ContainsKey(currentNodeId))
            {
                // Place below the last completed node
                herePos = positions[currentNodeId] + new Vector3(0f, -0.6f, 0f);
            }
            else
            {
                // At entrance (below row 0)
                herePos = new Vector3(0f, BottomY - 0.6f, -0.1f);
            }

            var here = MakeLabelGo("Here", transform, herePos, 0.018f, 150);
            here.GetComponent<TextMesh>().text  = "▲  ТЫ ЗДЕСЬ";
            here.GetComponent<TextMesh>().color = new Color(0.55f, 0.9f, 0.55f);
            _built.Add(here);
        }

        private GameObject BuildPath(Vector3 from, Vector3 to)
        {
            var go = new GameObject("Path");
            go.transform.SetParent(transform);

            Vector3 mid   = (from + to) * 0.5f;
            float   dist  = Vector3.Distance(from, to);
            float   angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;

            go.transform.localPosition = new Vector3(mid.x, mid.y, 0.02f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            go.transform.localScale    = Vector3.one;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "Line";
            quad.transform.SetParent(go.transform);
            quad.transform.localPosition = Vector3.zero;
            quad.transform.localScale    = new Vector3(dist, 0.07f, 1f);
            Destroy(quad.GetComponent<MeshCollider>());

            var rend = quad.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Unlit/Color"))
                { color = new Color(0.28f, 0.30f, 0.52f) };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;

            return go;
        }

        private GameObject BuildNode(MapNode node, Vector3 worldPos)
        {
            var go = new GameObject($"Node_{node.Id}_{node.Type}");
            go.transform.SetParent(transform);
            go.transform.localPosition = worldPos;

            bool active  = node.IsAccessible && !node.IsVisited;
            bool visited = node.IsVisited;

            // Border (slightly larger, drawn behind face)
            Color borderColor = active  ? new Color(0.78f, 0.85f, 1.00f)
                              : visited ? new Color(0.22f, 0.22f, 0.32f)
                                        : new Color(0.18f, 0.18f, 0.28f);
            MakeChildQuad("Border", go.transform, new Vector3(0f, 0f, 0.02f),
                new Vector3(1.02f, 1.02f, 1f), borderColor);

            // Face
            Color faceColor = visited ? new Color(0.10f, 0.10f, 0.18f)
                            : active  ? NodeFaceColor(node.Type)
                                      : new Color(0.07f, 0.07f, 0.14f);
            MakeChildQuad("Face", go.transform, Vector3.zero,
                new Vector3(0.86f, 0.86f, 1f), faceColor);

            // Icon line
            var iconGo = MakeLabelGo("Icon", go.transform,
                new Vector3(0f, 0.10f, -0.05f), 0.024f, 150);
            var iconTm = iconGo.GetComponent<TextMesh>();
            iconTm.text  = visited ? "OK" : NodeIcon(node.Type);
            iconTm.color = active ? Color.white
                         : visited ? new Color(0.35f, 0.35f, 0.45f)
                                   : new Color(0.25f, 0.25f, 0.38f);

            // Name line (small)
            var nameGo = MakeLabelGo("Name", go.transform,
                new Vector3(0f, -0.14f, -0.05f), 0.013f, 150);
            var nameTm = nameGo.GetComponent<TextMesh>();
            nameTm.text  = visited ? "" : NodeName(node.Type);
            nameTm.color = active ? new Color(0.68f, 0.72f, 0.88f)
                                  : new Color(0.22f, 0.22f, 0.32f);

            // A7: room modifier badge (bottom-right corner)
            if (!visited && node.CombatSetup != null &&
                node.CombatSetup.Modifier != RoomModifierType.None)
            {
                var modGo = MakeLabelGo("Modifier", go.transform,
                    new Vector3(0.30f, -0.30f, -0.08f), 0.013f, 80);
                var modTm = modGo.GetComponent<TextMesh>();
                modTm.text      = ModifierBadge(node.CombatSetup.Modifier);
                modTm.color     = ModifierColor(node.CombatSetup.Modifier);
                modTm.alignment = TextAlignment.Right;
            }

            // Click collider only for accessible unvisited nodes
            if (active)
            {
                var col  = go.AddComponent<BoxCollider>();
                col.size = new Vector3(0.86f, 0.86f, 0.2f);

                var handler     = go.AddComponent<SlotClickHandler>();
                int capturedId  = node.Id;
                handler.OnClicked = () => OnNodeClicked?.Invoke(capturedId);
            }

            return go;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static Color NodeFaceColor(MapNodeType type) => type switch
        {
            MapNodeType.Elite => new Color(0.55f, 0.32f, 0.07f),
            MapNodeType.Event => new Color(0.10f, 0.30f, 0.54f),
            MapNodeType.Boss  => new Color(0.48f, 0.07f, 0.07f),
            _                 => new Color(0.14f, 0.26f, 0.50f)
        };

        private static string NodeIcon(MapNodeType type) => type switch
        {
            MapNodeType.Elite => "ЭЛИТА",
            MapNodeType.Event => "?",
            MapNodeType.Boss  => "БОСС",
            _                 => "БОЙ"
        };

        private static string NodeName(MapNodeType type) => type switch
        {
            MapNodeType.Elite => "",
            MapNodeType.Event => "",
            MapNodeType.Boss  => "",
            _                 => ""
        };

        // A7: room modifier badge text and color
        private static string ModifierBadge(RoomModifierType mod) => mod switch
        {
            RoomModifierType.Flooded => "~",
            RoomModifierType.Burning => "^",
            RoomModifierType.Pits    => "o",
            _                        => ""
        };

        private static Color ModifierColor(RoomModifierType mod) => mod switch
        {
            RoomModifierType.Flooded => new Color(0.25f, 0.65f, 1.00f),
            RoomModifierType.Burning => new Color(1.00f, 0.50f, 0.10f),
            RoomModifierType.Pits    => new Color(0.55f, 0.45f, 0.70f),
            _                        => Color.white
        };

        private static void MakeChildQuad(string name, Transform parent,
            Vector3 localPos, Vector3 scale, Color color)
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
        }

        private static GameObject MakeQuadGo(string name, Transform parent,
            Vector3 localPos, Vector3 scale, Color color)
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

        private static GameObject MakeLabelGo(string name, Transform parent,
            Vector3 localPos, float scale, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale    = Vector3.one * scale;
            var tm       = go.AddComponent<TextMesh>();
            tm.fontSize  = fontSize;
            tm.fontStyle = FontStyle.Bold;
            tm.color     = Color.white;
            tm.anchor    = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            return go;
        }
    }
}
