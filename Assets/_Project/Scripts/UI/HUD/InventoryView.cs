using System;
using Mergistry.Data;
using Mergistry.Models;
using Mergistry.Views.Board;
using UnityEngine;

namespace Mergistry.UI.HUD
{
    /// <summary>
    /// World-space HUD: displays 4 potion inventory slots and a "Go to Battle" button.
    /// Position this GameObject below the board in the scene.
    /// </summary>
    public class InventoryView : MonoBehaviour
    {
        private const int   SlotCount   = InventoryModel.SlotCount;
        private const float SlotSize    = 0.85f;
        private const float SlotSpacing = 1.15f;

        // Per-slot state
        private MeshRenderer[] _slotBodyRenderers;
        private GameObject[][]  _slotDots;   // [slot][dot]
        private const int       MaxLevel = 3;
        private const float     DotSize  = 0.13f;
        private const float     DotGap   = 0.19f;

        public event Action OnGoBattleClicked;

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildSlots();
            BuildBattleButton();
        }

        // ── Public API ───────────────────────────────────────────────────────

        public void Refresh(InventoryModel inventory)
        {
            for (int i = 0; i < SlotCount; i++)
                UpdateSlot(i, inventory.GetSlot(i));
        }

        // ── Build ────────────────────────────────────────────────────────────

        private void BuildSlots()
        {
            _slotBodyRenderers = new MeshRenderer[SlotCount];
            _slotDots          = new GameObject[SlotCount][];

            float totalWidth = (SlotCount - 1) * SlotSpacing;
            float startX     = -totalWidth * 0.5f;

            for (int i = 0; i < SlotCount; i++)
            {
                var slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(transform);
                slot.transform.localPosition = new Vector3(startX + i * SlotSpacing, 0f, 0f);

                // Border
                MakeQuad("Border", slot.transform, new Vector3(0f, 0f, 0.01f),
                    Vector3.one * (SlotSize + 0.08f), new Color(0.40f, 0.45f, 0.60f));

                // Body (empty color)
                var body = MakeQuad("Body", slot.transform, Vector3.zero,
                    Vector3.one * SlotSize, new Color(0.18f, 0.20f, 0.30f));
                _slotBodyRenderers[i] = body.GetComponent<MeshRenderer>();

                // Level dots (hidden by default)
                _slotDots[i] = BuildDots(slot.transform, MaxLevel);
            }
        }

        private void BuildBattleButton()
        {
            var btn = new GameObject("BattleButton");
            btn.transform.SetParent(transform);
            btn.transform.localPosition = new Vector3(0f, -1.1f, 0f);

            // Background
            MakeQuad("BG", btn.transform, new Vector3(0f, 0f, 0.01f),
                new Vector3(3.0f, 0.60f, 1f), new Color(0.25f, 0.25f, 0.42f));

            // Active face
            MakeQuad("Face", btn.transform, Vector3.zero,
                new Vector3(2.88f, 0.50f, 1f), new Color(0.30f, 0.65f, 0.95f));

            // Collider for click
            var col = btn.AddComponent<BoxCollider>();
            col.size = new Vector3(2.9f, 0.55f, 0.1f);

            // Click handler
            var handler = btn.AddComponent<UI.Popups.SlotClickHandler>();
            handler.OnClicked = () => OnGoBattleClicked?.Invoke();
        }

        private static GameObject[] BuildDots(Transform parent, int count)
        {
            var dots    = new GameObject[count];
            float total = (count - 1) * DotGap;

            for (int i = 0; i < count; i++)
            {
                var dot = new GameObject($"Dot_{i}");
                dot.transform.SetParent(parent);
                dot.transform.localPosition = new Vector3(i * DotGap - total * 0.5f, -0.37f, -0.01f);
                dot.transform.localScale    = Vector3.one * DotSize;

                var mf = dot.AddComponent<MeshFilter>();
                mf.mesh = CircleMesh(0.5f, 12);
                var mr = dot.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows    = false;
                mr.material = new Material(Shader.Find("Unlit/Color")) { color = Color.white };

                dot.SetActive(false);
                dots[i] = dot;
            }
            return dots;
        }

        // ── Slot display ─────────────────────────────────────────────────────

        private void UpdateSlot(int index, PotionSlot slot)
        {
            Color bodyColor = slot.IsEmpty
                ? new Color(0.18f, 0.20f, 0.30f)
                : BrewView.GetBrewColor(slot.Type);

            _slotBodyRenderers[index].material.color = bodyColor;

            for (int d = 0; d < MaxLevel; d++)
                _slotDots[index][d].SetActive(!slot.IsEmpty && d < slot.Level);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

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
            rend.material = new Material(Shader.Find("Unlit/Color")) { color = color };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;
            return go;
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
