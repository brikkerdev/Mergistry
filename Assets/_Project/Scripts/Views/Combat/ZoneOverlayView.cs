using Mergistry.Models.Combat;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// Semi-transparent zone overlay rendered on a grid cell.
    /// A8: uses dedicated SDF shaders per zone type for animated fills.
    /// </summary>
    public class ZoneOverlayView : MonoBehaviour
    {
        public ZoneType ZoneType { get; private set; }

        private MeshRenderer _renderer;
        private Material     _mat;

        private static readonly int PropIntensity    = Shader.PropertyToID("_Intensity");
        private static readonly int PropLifetimeNorm = Shader.PropertyToID("_LifetimeNorm");
        private static readonly int PropBubblePhase  = Shader.PropertyToID("_BubblePhase");
        private static readonly int PropCrackProgress= Shader.PropertyToID("_CrackProgress");

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
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows    = false;
            _renderer.sortingOrder      = -1;

            string shaderName = GetShaderName(type);
            var    shader     = Shader.Find(shaderName);

            if (shader != null)
            {
                _mat = new Material(shader);
                _mat.SetFloat(PropIntensity, 1.0f);
                // Ice: animate crack appearing
                if (type == ZoneType.Ice)
                    _mat.SetFloat(PropCrackProgress, 0.0f);
                // Poison: random bubble phase
                if (type == ZoneType.Poison)
                    _mat.SetFloat(PropBubblePhase, Random.value);
                _renderer.material = _mat;

                // Animate Ice crack in
                if (type == ZoneType.Ice)
                    StartCoroutine(AnimateCrack());
            }
            else
            {
                // Fallback: solid colour
                _renderer.material = new Material(Shader.Find("Sprites/Default"))
                {
                    color = GetFallbackColor(type)
                };
            }
        }

        /// <summary>
        /// Call as zone approaches expiry to fade it out.
        /// lifetimeNorm: 0=fresh, 1=about to expire.
        /// </summary>
        public void SetLifetime(float lifetimeNorm)
        {
            if (_mat == null) return;
            _mat.SetFloat(PropIntensity,    Mathf.Lerp(1.0f, 0.3f, lifetimeNorm));
            _mat.SetFloat(PropLifetimeNorm, lifetimeNorm);
        }

        private System.Collections.IEnumerator AnimateCrack()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 2.5f;
                if (_mat != null)
                    _mat.SetFloat(PropCrackProgress, Mathf.Clamp01(t));
                yield return null;
            }
        }

        private static string GetShaderName(ZoneType type) => type switch
        {
            ZoneType.Fire   => "Mergistry/SH_Zone_Fire",
            ZoneType.Water  => "Mergistry/SH_Zone_Water",
            ZoneType.Poison => "Mergistry/SH_Zone_Poison",
            ZoneType.Ice    => "Mergistry/SH_Zone_Ice",
            _               => "Sprites/Default"
        };

        private static Color GetFallbackColor(ZoneType type) => type switch
        {
            ZoneType.Fire   => new Color(1.00f, 0.35f, 0.00f, 0.50f),
            ZoneType.Water  => new Color(0.10f, 0.55f, 1.00f, 0.45f),
            ZoneType.Poison => new Color(0.35f, 0.90f, 0.10f, 0.50f),
            ZoneType.Ice    => new Color(0.60f, 0.88f, 1.00f, 0.55f),
            _               => new Color(1.00f, 1.00f, 1.00f, 0.30f)
        };

        private void OnDestroy()
        {
            if (_mat != null)
                Destroy(_mat);
        }
    }
}
