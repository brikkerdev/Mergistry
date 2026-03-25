using System;
using Mergistry.Data;
using Mergistry.Models;
using Mergistry.UI.Popups;
using Mergistry.Views.Board;
using UnityEngine;

namespace Mergistry.UI.HUD
{
    /// <summary>
    /// World-space HUD: displays 4 potion inventory slots and a "Go to Battle" button.
    /// In combat mode the battle button is hidden and slots become tappable with
    /// cooldown overlay and selection highlight.
    /// </summary>
    public class InventoryView : MonoBehaviour
    {
        private const int   SlotCount   = InventoryModel.SlotCount;
        private const float SlotSize    = 0.85f;
        private const float SlotSpacing = 1.15f;

        // Per-slot renderers
        private MeshRenderer[] _slotBodyRenderers;
        private MeshRenderer[] _slotBorderRenderers;
        private MeshRenderer[] _cooldownOverlays;
        private TextMesh[]     _cooldownLabels;
        private GameObject[][] _slotDots;

        private const int   MaxLevel = 3;
        private const float DotSize  = 0.13f;
        private const float DotGap   = 0.19f;

        // Battle button
        private GameObject _battleButtonGo;

        // Mode state
        private bool _isCombatMode;

        public event Action       OnGoBattleClicked;
        public event Action<int>  OnSlotClicked;

        // ── Unity ────────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildSlots();
            BuildBattleButton();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>Refresh display in distillation mode (no cooldown, no selection).</summary>
        public void Refresh(InventoryModel inventory)
        {
            for (int i = 0; i < SlotCount; i++)
                UpdateSlotDistillation(i, inventory.GetSlot(i));
        }

        /// <summary>Switch between distillation mode (battle button shown) and combat mode.</summary>
        public void SetCombatMode(bool isCombat)
        {
            _isCombatMode = isCombat;
            _battleButtonGo.SetActive(!isCombat);

            // Reset cooldown/selection visuals when leaving combat
            if (!isCombat)
            {
                for (int i = 0; i < SlotCount; i++)
                {
                    _cooldownOverlays[i].gameObject.SetActive(false);
                    _cooldownLabels[i].gameObject.SetActive(false);
                    SetBorderColor(i, NormalBorderColor);
                }
            }
        }

        /// <summary>Refresh display in combat mode — shows cooldown overlay and selected slot highlight.</summary>
        public void RefreshCombat(InventoryModel inventory, int selectedSlot)
        {
            for (int i = 0; i < SlotCount; i++)
                UpdateSlotCombat(i, inventory.GetSlot(i), i == selectedSlot);
        }

        // ── Build ────────────────────────────────────────────────────────────

        private void BuildSlots()
        {
            _slotBodyRenderers   = new MeshRenderer[SlotCount];
            _slotBorderRenderers = new MeshRenderer[SlotCount];
            _cooldownOverlays    = new MeshRenderer[SlotCount];
            _cooldownLabels      = new TextMesh[SlotCount];
            _slotDots            = new GameObject[SlotCount][];

            float totalWidth = (SlotCount - 1) * SlotSpacing;
            float startX     = -totalWidth * 0.5f;

            for (int i = 0; i < SlotCount; i++)
            {
                var slot = new GameObject($"Slot_{i}");
                slot.transform.SetParent(transform);
                slot.transform.localPosition = new Vector3(startX + i * SlotSpacing, 0f, 0f);

                // Border (further from camera = behind body, acts as frame)
                var border = MakeQuad("Border", slot.transform, new Vector3(0f, 0f, 0.01f),
                    Vector3.one * (SlotSize + 0.08f), NormalBorderColor);
                _slotBorderRenderers[i] = border.GetComponent<MeshRenderer>();

                // Body
                var body = MakeQuad("Body", slot.transform, Vector3.zero,
                    Vector3.one * SlotSize, EmptySlotColor);
                _slotBodyRenderers[i] = body.GetComponent<MeshRenderer>();

                // Level dots (hidden by default)
                _slotDots[i] = BuildDots(slot.transform, MaxLevel);

                // Cooldown overlay — dark quad in front of body
                var cdGo = MakeQuad("CooldownOverlay", slot.transform, new Vector3(0f, 0f, -0.015f),
                    Vector3.one * SlotSize, new Color(0.04f, 0.04f, 0.07f));
                _cooldownOverlays[i] = cdGo.GetComponent<MeshRenderer>();
                cdGo.SetActive(false);

                // Cooldown label — TextMesh in front of overlay
                var labelGo = new GameObject("CooldownLabel");
                labelGo.transform.SetParent(slot.transform);
                labelGo.transform.localPosition = new Vector3(0f, 0f, -0.02f);
                labelGo.transform.localScale    = Vector3.one * 0.018f;

                var tm = labelGo.AddComponent<TextMesh>();
                tm.text      = "0";
                tm.fontSize  = 150;
                tm.fontStyle = FontStyle.Bold;
                tm.color     = Color.white;
                tm.anchor    = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;

                _cooldownLabels[i] = tm;
                labelGo.SetActive(false);

                // Collider + click handler (always present, only wired in combat mode)
                var col  = slot.AddComponent<BoxCollider>();
                col.size = new Vector3(SlotSize, SlotSize, 0.1f);

                int capturedIndex = i;
                var handler       = slot.AddComponent<SlotClickHandler>();
                handler.OnClicked = () => OnSlotClicked?.Invoke(capturedIndex);
            }
        }

        private void BuildBattleButton()
        {
            _battleButtonGo = new GameObject("BattleButton");
            _battleButtonGo.transform.SetParent(transform);
            _battleButtonGo.transform.localPosition = new Vector3(0f, -1.1f, 0f);

            MakeQuad("BG", _battleButtonGo.transform, new Vector3(0f, 0f, 0.01f),
                new Vector3(3.0f, 0.60f, 1f), new Color(0.25f, 0.25f, 0.42f));

            MakeQuad("Face", _battleButtonGo.transform, Vector3.zero,
                new Vector3(2.88f, 0.50f, 1f), new Color(0.30f, 0.65f, 0.95f));

            var col  = _battleButtonGo.AddComponent<BoxCollider>();
            col.size = new Vector3(2.9f, 0.55f, 0.1f);

            var handler      = _battleButtonGo.AddComponent<SlotClickHandler>();
            handler.OnClicked = () => OnGoBattleClicked?.Invoke();
        }


        // ── Slot display ─────────────────────────────────────────────────────

        private void UpdateSlotDistillation(int index, PotionSlot slot)
        {
            _slotBodyRenderers[index].material.color = slot.IsEmpty ? EmptySlotColor : BrewView.GetBrewColor(slot.Type);

            for (int d = 0; d < MaxLevel; d++)
                _slotDots[index][d].SetActive(!slot.IsEmpty && d < slot.Level);

            // Ensure combat overlays are hidden
            _cooldownOverlays[index].gameObject.SetActive(false);
            _cooldownLabels[index].gameObject.SetActive(false);
            SetBorderColor(index, NormalBorderColor);
        }

        private void UpdateSlotCombat(int index, PotionSlot slot, bool selected)
        {
            _slotBodyRenderers[index].material.color = slot.IsEmpty ? EmptySlotColor : BrewView.GetBrewColor(slot.Type);

            bool onCooldown = !slot.IsEmpty && slot.CooldownRemaining > 0;

            // Level dots — hidden when on cooldown or empty
            for (int d = 0; d < MaxLevel; d++)
                _slotDots[index][d].SetActive(!slot.IsEmpty && !onCooldown && d < slot.Level);

            // Cooldown overlay
            _cooldownOverlays[index].gameObject.SetActive(onCooldown);
            _cooldownLabels[index].gameObject.SetActive(onCooldown);
            if (onCooldown)
                _cooldownLabels[index].text = slot.CooldownRemaining.ToString();

            // Selection border
            bool canSelect = !slot.IsEmpty && !onCooldown;
            SetBorderColor(index, selected && canSelect ? SelectedBorderColor : NormalBorderColor);
        }

        private void SetBorderColor(int index, Color color)
        {
            _slotBorderRenderers[index].material.color = color;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private static readonly Color EmptySlotColor    = new Color(0.18f, 0.20f, 0.30f);
        private static readonly Color NormalBorderColor = new Color(0.40f, 0.45f, 0.60f);
        private static readonly Color SelectedBorderColor = new Color(1.0f, 0.90f, 0.20f);

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
