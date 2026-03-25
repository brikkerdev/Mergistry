using System.Collections;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Renders the player using SH_Player SDF shader on a quad.
    /// Includes a ghost quad for move preview.
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        private const float QuadSize = 0.90f;

        private static readonly Color PlayerColor = new Color(0.90f, 0.85f, 0.40f, 1.00f);
        private static readonly Color GhostColor  = new Color(0.55f, 0.52f, 0.22f, 0.65f);

        private GameObject _ghost;

        private void Awake()
        {
            BuildPlayerQuad(transform, PlayerColor, 0f);
            BuildGhost();
        }

        // ── Public API ────────────────────────────────────────────────────────

        public void PlaceAt(Vector3 worldPos)
        {
            StopAllCoroutines();
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }

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
            _ghost.transform.SetParent(transform.parent);
            _ghost.transform.localScale = Vector3.one;
            BuildPlayerQuad(_ghost.transform, GhostColor, 0f);
            _ghost.SetActive(false);
        }

        private static void BuildPlayerQuad(Transform parent, Color color, float zOffset)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "PlayerQuad";
            go.transform.SetParent(parent);
            go.transform.localPosition = new Vector3(0f, 0f, zOffset);
            go.transform.localScale    = Vector3.one * QuadSize;
            Destroy(go.GetComponent<MeshCollider>());

            var shader = Shader.Find("Mergistry/SH_Player");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(shader) { color = color };
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
    }
}
