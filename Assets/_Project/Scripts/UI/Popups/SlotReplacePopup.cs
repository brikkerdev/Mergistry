using System;
using Mergistry.Data;
using Mergistry.Models;
using Mergistry.Views.Board;
using UnityEngine;

namespace Mergistry.UI.Popups
{
    /// <summary>
    /// World-space popup shown when inventory is full.
    /// Displays current 4 slots and lets the player tap one to replace it.
    /// </summary>
    public class SlotReplacePopup : MonoBehaviour
    {
        private Action<int> _callback;

        private void Awake() => gameObject.SetActive(false);

        // ── Public API ───────────────────────────────────────────────────────

        public void Show(PotionType incoming, int incomingLevel, InventoryModel inventory, Action<int> onSlotSelected)
        {
            _callback = onSlotSelected;
            BuildPopup(incoming, incomingLevel, inventory);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            foreach (Transform child in transform)
                Destroy(child.gameObject);
        }

        // ── Build ────────────────────────────────────────────────────────────

        private void BuildPopup(PotionType incoming, int incomingLevel, InventoryModel inventory)
        {
            // Dark overlay
            MakeQuad("Overlay", transform, Vector3.zero, new Vector3(9f, 9f, 1f), new Color(0f, 0f, 0f, 0.75f));

            // Panel background
            MakeQuad("Panel", transform, new Vector3(0f, 0f, -0.05f), new Vector3(5.2f, 3.0f, 1f),
                new Color(0.12f, 0.13f, 0.22f));

            // Incoming brew preview (center-top)
            BuildBrewPreview(incoming, incomingLevel, new Vector3(0f, 0.75f, -0.1f));

            // Slot buttons (4 in a row)
            float slotSize    = 0.85f;
            float slotSpacing = 1.1f;
            float startX      = -(InventoryModel.SlotCount - 1) * slotSpacing * 0.5f;

            for (int i = 0; i < InventoryModel.SlotCount; i++)
            {
                var slot = inventory.GetSlot(i);
                float x  = startX + i * slotSpacing;
                BuildSlotButton(i, slot, new Vector3(x, -0.25f, -0.1f), slotSize);
            }

            // Discard button (index -1 = discard incoming brew)
            BuildDiscardButton(new Vector3(0f, -1.2f, -0.1f));
        }

        private void BuildBrewPreview(PotionType type, int level, Vector3 localPos)
        {
            var container = new GameObject("IncomingBrew");
            container.transform.SetParent(transform);
            container.transform.localPosition = localPos;

            Color brewColor = BrewView.GetBrewColor(type);
            MakeQuad("Border", container.transform, new Vector3(0f, 0f, 0f), new Vector3(0.88f, 0.88f, 1f),
                Color.Lerp(brewColor, Color.white, 0.55f));
            MakeQuad("Body", container.transform, new Vector3(0f, 0f, -0.01f), new Vector3(0.66f, 0.76f, 1f), brewColor);

            // Level dots
            float dotSpacing = 0.20f;
            float dotOffset  = (level - 1) * dotSpacing * 0.5f;
            for (int i = 0; i < level; i++)
            {
                var dot = new GameObject($"Dot_{i}");
                dot.transform.SetParent(container.transform);
                dot.transform.localPosition = new Vector3(i * dotSpacing - dotOffset, -0.37f, -0.02f);
                dot.transform.localScale    = Vector3.one * 0.14f;
                var mf = dot.AddComponent<MeshFilter>();
                mf.mesh = CircleMesh(0.5f, 12);
                var mr = dot.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows    = false;
                mr.material = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
            }
        }

        private void BuildSlotButton(int index, PotionSlot slot, Vector3 localPos, float size)
        {
            var btn = new GameObject($"SlotBtn_{index}");
            btn.transform.SetParent(transform);
            btn.transform.localPosition = localPos;

            // Border
            MakeQuad("Border", btn.transform, new Vector3(0f, 0f, 0.01f), Vector3.one * (size + 0.06f),
                new Color(0.60f, 0.65f, 0.80f));

            // Background
            Color bgColor = slot.IsEmpty
                ? new Color(0.20f, 0.22f, 0.32f)
                : BrewView.GetBrewColor(slot.Type);
            MakeQuad("BG", btn.transform, Vector3.zero, Vector3.one * size, bgColor);

            // Level dots (only for non-empty slots)
            if (!slot.IsEmpty)
            {
                const float dotSpacing = 0.19f;
                const float dotSize    = 0.13f;
                float dotOffset = (slot.Level - 1) * dotSpacing * 0.5f;
                for (int i = 0; i < slot.Level; i++)
                {
                    var dot = new GameObject($"Dot_{i}");
                    dot.transform.SetParent(btn.transform);
                    dot.transform.localPosition = new Vector3(i * dotSpacing - dotOffset, -0.32f, -0.01f);
                    dot.transform.localScale    = Vector3.one * dotSize;
                    var mf = dot.AddComponent<MeshFilter>();
                    mf.mesh = CircleMesh(0.5f, 12);
                    var mr = dot.AddComponent<MeshRenderer>();
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    mr.receiveShadows    = false;
                    mr.material = new Material(Shader.Find("Unlit/Color")) { color = Color.white };
                }
            }

            // Collider for click detection
            var col = btn.AddComponent<BoxCollider>();
            col.size = new Vector3(size, size, 0.1f);

            // Click handler
            var handler = btn.AddComponent<SlotClickHandler>();
            int capturedIndex = index;
            handler.OnClicked = () => _callback?.Invoke(capturedIndex);
        }

        private void BuildDiscardButton(Vector3 localPos)
        {
            var btn = new GameObject("DiscardBtn");
            btn.transform.SetParent(transform);
            btn.transform.localPosition = localPos;

            MakeQuad("Border", btn.transform, new Vector3(0f, 0f, 0.01f),
                new Vector3(2.6f, 0.52f, 1f), new Color(0.55f, 0.20f, 0.20f));
            MakeQuad("Face", btn.transform, Vector3.zero,
                new Vector3(2.46f, 0.42f, 1f), new Color(0.75f, 0.25f, 0.25f));

            var col = btn.AddComponent<BoxCollider>();
            col.size = new Vector3(2.5f, 0.45f, 0.1f);

            var handler = btn.AddComponent<SlotClickHandler>();
            handler.OnClicked = () => _callback?.Invoke(-1); // -1 = discard
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static void MakeQuad(string name, Transform parent, Vector3 localPos, Vector3 scale, Color color)
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

        private static Mesh CircleMesh(float r, int seg)
        {
            var mesh  = new Mesh { name = "DotCircle" };
            var verts = new Vector3[seg + 1];
            var tris  = new int[seg * 3];
            verts[0] = Vector3.zero;
            for (int i = 0; i < seg; i++)
            {
                float a = 2f * Mathf.PI * i / seg;
                verts[i + 1]    = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = (i + 1) % seg + 1;
                tris[i * 3 + 2] = i + 1;
            }
            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            return mesh;
        }
    }

}
