using UnityEngine;

namespace Mergistry.UI.HUD
{
    /// <summary>
    /// Displays N action dots above the board.
    /// M1: always shows 3, never decrements.
    /// </summary>
    public class ActionCounterView : MonoBehaviour
    {
        [SerializeField] private int maxActions = 3;

        private GameObject[] _dots;

        private void Awake()
        {
            BuildDots();
            Refresh(maxActions);
        }

        public void Refresh(int remaining)
        {
            remaining = Mathf.Clamp(remaining, 0, maxActions);
            for (int i = 0; i < _dots.Length; i++)
                _dots[i].SetActive(i < remaining);
        }

        private void BuildDots()
        {
            _dots = new GameObject[maxActions];
            float spacing = 0.4f;
            float offset  = (maxActions - 1) * spacing * 0.5f;

            for (int i = 0; i < maxActions; i++)
            {
                var dot = new GameObject($"ActionDot_{i}");
                dot.transform.SetParent(transform);
                dot.transform.localPosition = new Vector3(i * spacing - offset, 0f, 0f);
                dot.transform.localScale    = Vector3.one * 0.28f;

                var mf = dot.AddComponent<MeshFilter>();
                mf.mesh = CircleMesh(0.5f, 16);

                var mr = dot.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows    = false;
                mr.material = new Material(Shader.Find("Unlit/Color"))
                    { color = new Color(0.95f, 0.85f, 0.30f) };

                _dots[i] = dot;
            }
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
