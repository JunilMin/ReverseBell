using UnityEngine;

namespace ReverseBell.Core
{
    public static class GameBootstrap
    {
        private const string SystemsObjectName = "[Systems]";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;

            if (Object.FindFirstObjectByType<RhythmClock>() != null)
            {
                return;
            }

            var systems = new GameObject(SystemsObjectName);
            systems.AddComponent<RhythmClock>();
        }
    }
}
