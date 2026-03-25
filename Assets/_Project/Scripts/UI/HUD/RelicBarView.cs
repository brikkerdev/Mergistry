using System.Collections.Generic;
using Mergistry.Data;
using UnityEngine;

namespace Mergistry.UI.HUD
{
    /// <summary>
    /// Displays small colored squares for each acquired relic, positioned near the health bar.
    /// </summary>
    public class RelicBarView : MonoBehaviour
    {
        private const float IconSize = 0.30f;
        private const float IconGap  = 0.40f;

        private readonly List<GameObject> _icons = new List<GameObject>();

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        public void Refresh(IReadOnlyList<RelicType> relics)
        {
            // Clear existing icons
            foreach (var icon in _icons)
                if (icon != null) Destroy(icon);
            _icons.Clear();

            if (relics == null || relics.Count == 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            float totalWidth = (relics.Count - 1) * IconGap;
            float startX     = -totalWidth * 0.5f;

            for (int i = 0; i < relics.Count; i++)
            {
                var go = new GameObject($"RelicIcon_{relics[i]}");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(startX + i * IconGap, 0f, 0f);
                go.transform.localScale    = Vector3.one * IconSize;

                // Colored quad as icon
                var mf = go.AddComponent<MeshFilter>();
                mf.mesh = MakeQuadMesh();

                var mr = go.AddComponent<MeshRenderer>();
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows    = false;
                mr.material = new Material(Shader.Find("Unlit/Color"))
                {
                    color = GetRelicColor(relics[i])
                };

                _icons.Add(go);
            }
        }

        private static Color GetRelicColor(RelicType type)
        {
            return type switch
            {
                RelicType.Thermos => new Color(0.90f, 0.50f, 0.20f),
                RelicType.Lens    => new Color(0.40f, 0.70f, 0.95f),
                RelicType.Flask   => new Color(0.30f, 0.80f, 0.40f),
                RelicType.Cube    => new Color(0.85f, 0.85f, 0.30f),
                RelicType.Prism   => new Color(0.80f, 0.35f, 0.85f),
                _                 => Color.gray
            };
        }

        private static Mesh MakeQuadMesh()
        {
            var mesh = new Mesh { name = "RelicQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
