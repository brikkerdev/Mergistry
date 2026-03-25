using Mergistry.Data;
using UnityEngine;

namespace Mergistry.Views.Board
{
    /// <summary>
    /// World-space visual for a brew sitting on a board cell.
    /// Uses SH_Brew SDF shader for the flask shape.
    /// </summary>
    public class BrewView : MonoBehaviour
    {
        private const int   MaxLevel   = 3;
        private const float DotSize    = 0.14f;
        private const float DotSpacing = 0.20f;
        private const float BobSpeed   = 1.2f;
        private const float BobAmp     = 0.04f;
        private const float QuadSize   = 1.00f;

        public PotionType  PotionType  { get; private set; }
        public ElementType ElementType { get; private set; }
        public int         Level       { get; private set; }

        private Vector3      _homePosition;
        private float        _timeOffset;
        private GameObject[] _levelDots;

        // ── Public API ───────────────────────────────────────────────────────

        public void Initialize(PotionType potionType, ElementType element, int level, Vector3 worldPosition)
        {
            PotionType    = potionType;
            ElementType   = element;
            _homePosition = worldPosition;
            _timeOffset   = Random.Range(0f, Mathf.PI * 2f);
            transform.position = worldPosition;

            BuildFlask(potionType);
            BuildLevelDots();
            SetLevel(level);
        }

        public void SetLevel(int level)
        {
            Level = Mathf.Clamp(level, 1, MaxLevel);
            for (int i = 0; i < _levelDots.Length; i++)
                _levelDots[i].SetActive(i < Level);
        }

        // ── Unity ────────────────────────────────────────────────────────────

        private void Update()
        {
            float bob = Mathf.Sin(Time.time * BobSpeed + _timeOffset) * BobAmp;
            transform.position = _homePosition + new Vector3(0f, bob, 0f);
        }

        // ── Build ────────────────────────────────────────────────────────────

        private void BuildFlask(PotionType potionType)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "BrewQuad";
            go.transform.SetParent(transform);
            go.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            go.transform.localScale    = Vector3.one * QuadSize;
            Destroy(go.GetComponent<MeshCollider>());

            var shader = Shader.Find("Mergistry/SH_Brew");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(shader) { color = GetBrewColor(potionType) };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;
        }

        private void BuildLevelDots()
        {
            _levelDots = new GameObject[MaxLevel];
            float totalWidth = (MaxLevel - 1) * DotSpacing;

            for (int i = 0; i < MaxLevel; i++)
            {
                var dot = new GameObject($"Dot_{i}");
                dot.transform.SetParent(transform);
                dot.transform.localPosition = new Vector3(i * DotSpacing - totalWidth * 0.5f, -0.37f, -0.02f);
                dot.transform.localScale    = Vector3.one * DotSize;

                var mf  = dot.AddComponent<MeshFilter>();
                mf.mesh = CircleMesh(0.5f, 12);
                var mr  = dot.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows    = false;
                mr.material          = new Material(Shader.Find("Unlit/Color")) { color = Color.white };

                _levelDots[i] = dot;
            }
        }

        // ── Color map ────────────────────────────────────────────────────────

        public static Color GetBrewColor(PotionType type) => type switch
        {
            PotionType.Flame  => new Color(0.90f, 0.35f, 0.10f),
            PotionType.Stream => new Color(0.15f, 0.45f, 0.85f),
            PotionType.Poison => new Color(0.20f, 0.70f, 0.20f),
            PotionType.Steam  => new Color(0.80f, 0.80f, 0.85f),
            PotionType.Napalm => new Color(0.90f, 0.55f, 0.10f),
            PotionType.Acid   => new Color(0.55f, 0.90f, 0.15f),
            _                 => Color.grey,
        };

        // ── Helpers ──────────────────────────────────────────────────────────

        private static Mesh CircleMesh(float r, int seg)
        {
            var mesh  = new Mesh { name = "DotCircle" };
            var verts = new Vector3[seg + 1];
            var tris  = new int[seg * 3];
            verts[0]  = Vector3.zero;
            for (int i = 0; i < seg; i++)
            {
                float a      = 2f * Mathf.PI * i / seg;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
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
