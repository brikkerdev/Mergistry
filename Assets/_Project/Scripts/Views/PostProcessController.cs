using UnityEngine;

namespace Mergistry.Views
{
    /// <summary>
    /// Attached to the main camera. Drives _GameTime global shader uniform and
    /// performs bloom post-processing via OnRenderImage.
    ///
    /// Bloom pipeline:
    ///   1. Extract bright pixels  (SH_PP_BloomExtract)
    ///   2. Blur pass 1            (SH_PP_BloomBlur, offset=1)
    ///   3. Blur pass 2            (SH_PP_BloomBlur, offset=2)
    ///   4. Combine                (SH_PP_BloomCombine)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PostProcessController : MonoBehaviour
    {
        [Header("Bloom")]
        [SerializeField] private bool  bloomEnabled    = true;
        [SerializeField] private float bloomThreshold  = 0.80f;
        [SerializeField] private float bloomSoftKnee   = 0.50f;
        [SerializeField] private float bloomIntensity  = 1.50f;

        // Runtime paused flag (set by GameManager when opening menus etc.)
        public static bool TimePaused { get; set; } = false;

        private Material _extractMat;
        private Material _blurMat;
        private Material _combineMat;

        private float _gameTime;

        private static readonly int PropGameTime       = Shader.PropertyToID("_GameTime");
        private static readonly int PropThreshold      = Shader.PropertyToID("_Threshold");
        private static readonly int PropSoftKnee       = Shader.PropertyToID("_SoftKnee");
        private static readonly int PropTexelSize      = Shader.PropertyToID("_TexelSize");
        private static readonly int PropBlurOffset     = Shader.PropertyToID("_BlurOffset");
        private static readonly int PropBloomTex       = Shader.PropertyToID("_BloomTex");
        private static readonly int PropBloomIntensity = Shader.PropertyToID("_BloomIntensity");

        private void Awake()
        {
            _extractMat = new Material(Shader.Find("Mergistry/PP/SH_PP_BloomExtract"));
            _blurMat    = new Material(Shader.Find("Mergistry/PP/SH_PP_BloomBlur"));
            _combineMat = new Material(Shader.Find("Mergistry/PP/SH_PP_BloomCombine"));

            if (_extractMat == null || _blurMat == null || _combineMat == null)
                Debug.LogWarning("[PostProcessController] One or more PP shaders not found.");
        }

        private void Update()
        {
            if (!TimePaused)
                _gameTime += Time.deltaTime;

            Shader.SetGlobalFloat(PropGameTime, _gameTime);
        }

        private void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (!bloomEnabled || _extractMat == null || _blurMat == null || _combineMat == null)
            {
                Graphics.Blit(src, dst);
                return;
            }

            int w = src.width  / 2;
            int h = src.height / 2;

            RenderTexture bright = RenderTexture.GetTemporary(w, h, 0, src.format);
            RenderTexture blurA  = RenderTexture.GetTemporary(w, h, 0, src.format);
            RenderTexture blurB  = RenderTexture.GetTemporary(w, h, 0, src.format);

            // Pass 1: extract bright
            _extractMat.SetFloat(PropThreshold, bloomThreshold);
            _extractMat.SetFloat(PropSoftKnee,  bloomSoftKnee);
            Graphics.Blit(src, bright, _extractMat);

            // Pass 2: blur (Kawase offset=1)
            _blurMat.SetVector(PropTexelSize,  new Vector2(1f / w, 1f / h));
            _blurMat.SetFloat(PropBlurOffset, 1.0f);
            Graphics.Blit(bright, blurA, _blurMat);

            // Pass 3: blur (Kawase offset=2)
            _blurMat.SetFloat(PropBlurOffset, 2.0f);
            Graphics.Blit(blurA, blurB, _blurMat);

            // Pass 4: combine
            _combineMat.SetTexture(PropBloomTex, blurB);
            _combineMat.SetFloat(PropBloomIntensity, bloomIntensity);
            Graphics.Blit(src, dst, _combineMat);

            RenderTexture.ReleaseTemporary(bright);
            RenderTexture.ReleaseTemporary(blurA);
            RenderTexture.ReleaseTemporary(blurB);
        }

        private void OnDestroy()
        {
            if (_extractMat) Destroy(_extractMat);
            if (_blurMat)    Destroy(_blurMat);
            if (_combineMat) Destroy(_combineMat);
        }
    }
}
