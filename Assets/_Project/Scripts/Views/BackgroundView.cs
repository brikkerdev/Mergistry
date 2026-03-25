using UnityEngine;

namespace Mergistry.Views
{
    /// <summary>
    /// Applies SH_Background shader to this quad in Awake.
    /// Attach to a large background quad in the scene (z > 0, behind everything).
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class BackgroundView : MonoBehaviour
    {
        private void Awake()
        {
            var rend = GetComponent<MeshRenderer>();
            var shader = Shader.Find("Mergistry/SH_Background");
            if (shader != null)
                rend.material = new Material(shader);
            else
                rend.material = new Material(Shader.Find("Unlit/Color"))
                    { color = new Color(0.04f, 0.05f, 0.12f) };

            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;

            var col = GetComponent<Collider>();
            if (col != null) Destroy(col);
        }
    }
}
