using UnityEditor;
using UnityEngine;

namespace Mergistry.Editor
{
    public static class BuildSetup
    {
        [MenuItem("Mergistry/Setup Build Scenes")]
        public static void SetupBuildScenes()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Boot.unity", true),
                new EditorBuildSettingsScene("Assets/_Project/Scenes/Game.unity", true),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[BuildSetup] Build scenes configured: Boot(0), Game(1)");
        }
    }
}
