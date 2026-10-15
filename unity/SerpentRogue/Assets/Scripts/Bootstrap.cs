using UnityEngine;

namespace SerpentRogue
{
    // Starts the game automatically when you press Play, in ANY scene
    // (even an empty one). No GameObjects to set up by hand!
    public static class Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Object.FindFirstObjectByType<GameManager>() != null) return;
            new GameObject("Serpent Rogue").AddComponent<GameManager>();
        }
    }
}
