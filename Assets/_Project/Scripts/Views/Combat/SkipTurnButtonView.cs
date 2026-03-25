using System;
using Mergistry.UI.Popups;
using UnityEngine;

namespace Mergistry.Views.Combat
{
    /// <summary>
    /// "SKIP" button shown below the combat grid.
    /// Clicking it fires <see cref="OnClicked"/>.
    /// Position this GameObject in the scene (typically y ≈ -3.8 below the grid).
    /// </summary>
    public class SkipTurnButtonView : MonoBehaviour
    {
        public event Action OnClicked;

        private void Awake() => Build();

        private void Build()
        {
            // Background (darker)
            MakeQuad("BG", transform, new Vector3(0f, 0f, 0.01f),
                new Vector3(2.2f, 0.55f, 1f), new Color(0.15f, 0.25f, 0.18f));

            // Face (lighter green)
            MakeQuad("Face", transform, Vector3.zero,
                new Vector3(2.1f, 0.44f, 1f), new Color(0.25f, 0.60f, 0.32f));

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform);
            labelGo.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            labelGo.transform.localScale    = Vector3.one * 0.018f;

            var tm = labelGo.AddComponent<TextMesh>();
            tm.text      = "SKIP";
            tm.fontSize  = 120;
            tm.fontStyle = FontStyle.Bold;
            tm.color     = new Color(0.85f, 1f, 0.88f);
            tm.anchor    = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;

            // Collider + click handler
            var col  = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(2.1f, 0.5f, 0.1f);

            var handler      = gameObject.AddComponent<SlotClickHandler>();
            handler.OnClicked = () => OnClicked?.Invoke();
        }

        private static void MakeQuad(string goName, Transform parent,
            Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = goName;
            go.transform.SetParent(parent);
            go.transform.localPosition = localPos;
            go.transform.localScale    = scale;
            Destroy(go.GetComponent<MeshCollider>());

            var rend = go.GetComponent<Renderer>();
            rend.material = new Material(Shader.Find("Unlit/Color")) { color = color };
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows    = false;
        }
    }
}
