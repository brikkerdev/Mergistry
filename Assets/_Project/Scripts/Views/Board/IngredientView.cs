using Mergistry.Data;
using UnityEngine;

namespace Mergistry.Views.Board
{
    public class IngredientView : MonoBehaviour
    {
        public ElementType ElementType { get; private set; }

        private Vector3 _homePosition;
        private bool    _isDragging;
        private float   _timeOffset;

        private const float BobSpeed     = 1.5f;
        private const float BobAmplitude = 0.05f;
        private const float QuadSize     = 0.90f;

        public void Initialize(ElementType elementType, Vector3 worldPosition)
        {
            ElementType   = elementType;
            _homePosition = worldPosition;
            _timeOffset   = Random.value * Mathf.PI * 2f;
            transform.position = worldPosition;

            BuildQuad(elementType);
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

        // ── Build ────────────────────────────────────────────────────────────

        private void BuildQuad(ElementType elem)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "IngredientQuad";
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one * QuadSize;
            Destroy(go.GetComponent<MeshCollider>());

            var shader = Shader.Find("Mergistry/SH_Ingredient");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            var mr = go.GetComponent<MeshRenderer>();
            mr.material = new Material(shader) { color = ElementColor(elem) };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows    = false;
        }

        private static Color ElementColor(ElementType elem) => elem switch
        {
            ElementType.Ignis => new Color(0.90f, 0.30f, 0.20f),
            ElementType.Aqua  => new Color(0.20f, 0.55f, 0.90f),
            ElementType.Toxin => new Color(0.30f, 0.80f, 0.30f),
            ElementType.Lux   => new Color(0.95f, 0.85f, 0.20f),
            ElementType.Umbra => new Color(0.55f, 0.20f, 0.80f),
            _                 => Color.white
        };
    }
}
