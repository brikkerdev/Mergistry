using Mergistry.Core;
using Mergistry.Services;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Mergistry.Boot
{
    /// <summary>
    /// Entry point. Lives in Boot scene. Registers services, then loads Game scene additively.
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        private const string GameScene = "Game";

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            RegisterServices();
        }

        private void Start()
        {
            Debug.Log("[Bootstrapper] Loading Game scene additively...");
            SceneManager.LoadSceneAsync(GameScene, LoadSceneMode.Additive);
        }

        private static void RegisterServices()
        {
            ServiceLocator.Clear();
            EventBus.Clear();
            ServiceLocator.Register<IDistillationService>(new DistillationService());
        }
    }
}
