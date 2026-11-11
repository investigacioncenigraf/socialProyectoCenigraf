namespace SocialProyectoCenigraf.Debugging.State
{
    public enum DebugActionType
    {
        SetDialoguePresentationMode,
        SetMazeAnswerHighlight
    }

    public readonly struct DebugAction
    {
        public DebugActionType Type { get; }
        public DialoguePresentationMode DialogueMode { get; }
        public bool Flag { get; }

        private DebugAction(
            DebugActionType type,
            DialoguePresentationMode dialoguePresentationMode,
            bool flag = false)
        {
            Type = type;
            DialogueMode = dialoguePresentationMode;
            Flag = flag;
        }

        public static DebugAction SetDialogueMode(DialoguePresentationMode mode) =>
            new DebugAction(DebugActionType.SetDialoguePresentationMode, mode);

        public static DebugAction SetMazeAnswerHighlight(bool enabled) =>
            new DebugAction(
                DebugActionType.SetMazeAnswerHighlight,
                DialoguePresentationMode.OfficialPopup,
                enabled);
    }
}
