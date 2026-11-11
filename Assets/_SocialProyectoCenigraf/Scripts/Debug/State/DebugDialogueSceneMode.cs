using SocialProyectoCenigraf.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SocialProyectoCenigraf.Debugging.State
{
    /// <summary>
    /// Keeps the dialogue mode of DebugStore in step with the text animation the player
    /// chose in the game menu. The mode used to be decided by scene name, which left the
    /// Templo always in the old presentation; now the player's choice rules every scene
    /// without exception. It is applied at start up, on every change and on every scene
    /// load, so nothing can leave a scene out.
    /// </summary>
    internal static class DebugDialogueSceneMode
    {
        private static bool listening;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            listening = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ListenForChanges()
        {
            if (listening) return;

            listening = true;
            SceneManager.activeSceneChanged += ApplyForScene;
            TextAnimationSettings.Changed += Apply;
            Apply();
        }

        private static void ApplyForScene(Scene previous, Scene current) => Apply();

        private static void Apply()
        {
            DialoguePresentationMode mode =
                TextAnimationSettings.Style == TextAnimationStyle.Retro
                    ? DialoguePresentationMode.LegacyCinematic
                    : DialoguePresentationMode.OfficialPopup;
            DebugStore.Instance.Dispatch(DebugAction.SetDialogueMode(mode));
        }
    }
}
