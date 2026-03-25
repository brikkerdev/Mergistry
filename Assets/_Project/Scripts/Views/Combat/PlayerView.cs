using System.Collections;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Renders the player as a bright circle. Includes a ghost circle for move preview.
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        private const float PlayerRadius = 0.38f;
        private const int   CircleSeg    = 24;

        private static readonly Color PlayerColor = new Color(0.90f, 0.85f, 0.40f);
        private static readonly Color GhostColor  = new Color(0.55f, 0.52f, 0.22f);

        private GameObject   _ghost;

        private void Awake()
        {
            BuildCircle(transform, PlayerColor, 0f);
            BuildGhost();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Teleport to position (no animation). Preserves current z.</summary>
        public void PlaceAt(Vector3 worldPos)
        {
            StopAllCoroutines();
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }

        /// <summary>Smoothly tween to position over 0.2 s. Preserves current z.</summary>
        public void MoveTo(Vector3 worldPos)
        {
            StopAllCoroutines();
            StartCoroutine(TweenTo(new Vector3(worldPos.x, worldPos.y, transform.position.z), 0.2f));
        }

        public void ShowGhost(Vector3 worldPos)
        {
            _ghost.SetActive(true);
            _ghost.transform.position = new Vector3(worldPos.x, worldPos.y, -0.3f);
        }

        public void HideGhost() => _ghost.SetActive(false);

        // ── Build ─────────────────────────────────────────────────────────────

        private void BuildGhost()
        {
            _ghost = new GameObject("PlayerGhost");
            // Sibling of this GO so it doesn't move with us during tween
            _ghost.transform.SetParent(transform.parent);
            _ghost.transform.localScale = Vector3.one;
            BuildCircle(_ghost.transform, GhostColor, 0f);
            _ghost.SetActive(false);
        }

        private static void BuildCircle(Transform parent, Color color, float zOffset)
        {
            var go = new GameObject("Circle");
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0f, 0f, zOffset);
            go.transform.localScale    = Vector3.one * (PlayerRadius * 2f);

            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = CircleMesh(0.5f, CircleSeg);
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = new Material(Shader.Find("Unlit/Color")) { color = color };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;
        }

        private IEnumerator TweenTo(Vector3 target, float duration)
        {
            var   start   = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed            += Time.deltaTime;
                transform.position  = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            transform.position = target;
        }

        private static Mesh CircleMesh(float r, int seg)
        {
            var mesh  = new Mesh { name = "PlayerCircle" };
            var verts = new Vector3[seg + 1];
            var tris  = new int[seg * 3];
            verts[0] = Vector3.zero;
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
