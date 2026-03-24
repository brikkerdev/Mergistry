using Mergistry.Data;
using UnityEngine;

namespace Mergistry.Views.Board
{
    public class IngredientView : MonoBehaviour
    {
        public ElementType ElementType { get; private set; }

        private Vector3 _homePosition;
        private bool _isDragging;
        private float _timeOffset;

        private const float BobSpeed     = 1.5f;
        private const float BobAmplitude = 0.05f;
        private const float CircleRadius  = 0.42f;
        private const int   CircleSegments = 24;

        public void Initialize(ElementType elementType, Vector3 worldPosition)
        {
            ElementType   = elementType;
            _homePosition = worldPosition;
            _timeOffset   = Random.value * Mathf.PI * 2f;
            transform.position = worldPosition;

            BuildMesh();
            ApplyColor(elementType);
        }

        private void Update()
        {
            if (_isDragging) return;
            float bob = Mathf.Sin(Time.time * BobSpeed + _timeOffset) * BobAmplitude;
            transform.position = _homePosition + Vector3.up * bob;
        }

        public void StartDrag()
        {
            _isDragging = true;
            transform.localScale = Vector3.one * 1.2f;
        }

        public void UpdateDragPosition(Vector3 worldPos) =>
            transform.position = new Vector3(worldPos.x, worldPos.y, -0.1f);

        public void EndDrag()
        {
            _isDragging = false;
            transform.localScale = Vector3.one;
            transform.position = _homePosition;
        }

        public void SetHomePosition(Vector3 pos) => _homePosition = pos;

        // ── Mesh ────────────────────────────────────────────────────────────

        private void BuildMesh()
        {
            var mf = gameObject.AddComponent<MeshFilter>();
            mf.mesh = CreateCircle(CircleRadius, CircleSegments);
            var mr = gameObject.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;
        }

        private static Mesh CreateCircle(float r, int seg)
        {
            var mesh  = new Mesh { name = "IngredientCircle" };
            var verts = new Vector3[seg + 1];
            var tris  = new int[seg * 3];
            var uvs   = new Vector2[seg + 1];

            verts[0] = Vector3.zero;
            uvs[0]   = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < seg; i++)
            {
                float a = 2f * Mathf.PI * i / seg;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);
                uvs[i + 1]   = new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f);
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = (i + 1) % seg + 1;
                tris[i * 3 + 2] = i + 1;
            }

            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.uv        = uvs;
            mesh.RecalculateNormals();
            return mesh;
        }

        private void ApplyColor(ElementType elem)
        {
            var rend = GetComponent<MeshRenderer>();
            if (rend == null) return;
            var mat = new Material(Shader.Find("Unlit/Color"));
            mat.color = elem switch
            {
                ElementType.Ignis => new Color(0.90f, 0.30f, 0.20f),
                ElementType.Aqua  => new Color(0.20f, 0.55f, 0.90f),
                ElementType.Toxin => new Color(0.30f, 0.80f, 0.30f),
                ElementType.Lux   => new Color(0.95f, 0.85f, 0.20f),
                ElementType.Umbra => new Color(0.55f, 0.20f, 0.80f),
                _                 => Color.white
            };
            rend.material = mat;
        }
    }
}
