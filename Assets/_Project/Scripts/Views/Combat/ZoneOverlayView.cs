using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Semi-transparent colored quad rendered on a grid cell to indicate an active zone.
    /// Sits at Z = -0.025 (in front of floor, behind entities).
    /// A3: Fire (orange), Water (blue), Poison (green).
    /// </summary>
    public class ZoneOverlayView : MonoBehaviour
    {
        public ZoneType ZoneType { get; private set; }

        private MeshRenderer _renderer;

        public void Initialize(ZoneType type, Vector3 worldPos)
        {
            ZoneType           = type;
            transform.position = new Vector3(worldPos.x, worldPos.y, worldPos.z - 0.025f);

            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "ZoneQuad";
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale    = Vector3.one * 0.90f;
            Destroy(go.GetComponent<MeshCollider>());

            _renderer = go.GetComponent<MeshRenderer>();
            _renderer.material = new Material(Shader.Find("Sprites/Default"))
            {
                color = GetZoneColor(type)
            };
            _renderer.sortingOrder      = -1;
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows    = false;
        }

        private static Color GetZoneColor(ZoneType type) => type switch
        {
            ZoneType.Fire   => new Color(1.00f, 0.35f, 0.00f, 0.50f),
            ZoneType.Water  => new Color(0.10f, 0.55f, 1.00f, 0.45f),
            ZoneType.Poison => new Color(0.35f, 0.90f, 0.10f, 0.50f),
            _               => new Color(1.00f, 1.00f, 1.00f, 0.30f)
        };
    }
}
