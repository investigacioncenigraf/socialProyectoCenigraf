namespace SocialProyectoCenigraf.Debugging.State
{
    public static class DebugReducer
    {
        public static DebugStateData Reduce(
            DebugStateData currentState,
            DebugAction action)
        {
            switch (action.Type)
            {
                case DebugActionType.SetDialoguePresentationMode:
                    currentState.DialogueMode = action.DialogueMode;
                    break;

                case DebugActionType.SetMazeAnswerHighlight:
                    currentState.HighlightMazeAnswers = action.Flag;
                    break;
            }

            return currentState;
        }
    }
}
