using System;

namespace SocialProyectoCenigraf.Debugging.State
{
    public enum DialoguePresentationMode
    {
        OfficialPopup,
        LegacyCinematic
    }

    /// <summary>Development-only settings shared by debug systems.</summary>
    [Serializable]
    public struct DebugStateData
    {
        public DialoguePresentationMode DialogueMode;

        /// <summary>Paints the maze answers: green the right one, red the wrong ones.</summary>
        public bool HighlightMazeAnswers;

        public static DebugStateData Empty => new DebugStateData
        {
            DialogueMode = DialoguePresentationMode.OfficialPopup,
            HighlightMazeAnswers = false
        };
    }
}
