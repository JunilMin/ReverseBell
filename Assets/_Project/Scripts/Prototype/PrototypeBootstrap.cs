using UnityEngine;

namespace ReverseBell.Prototype
{
    public static class PrototypeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (Object.FindFirstObjectByType<PrototypeGame>() != null)
            {
                return;
            }

            var root = new GameObject("[Playable Prototype]");
            root.AddComponent<PrototypeGame>();
        }
    }
}

