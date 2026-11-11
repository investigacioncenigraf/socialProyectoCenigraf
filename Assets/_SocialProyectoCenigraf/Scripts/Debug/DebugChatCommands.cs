using SocialProyectoCenigraf.Debugging.State;

namespace SocialProyectoCenigraf.Debugging
{
    /// <summary>
    /// Commands typed in the chat, all starting with '@', that switch debug state on and off.
    /// They never leave the machine: nothing is sent to the server.
    /// </summary>
    public static class DebugChatCommands
    {
        public const string Prefix = "@";
        private const string AnswerCommand = "@answer";

        /// <summary>
        /// True when the text was a command. <paramref name="reply"/> is what the chat answers.
        /// </summary>
        public static bool TryExecute(string text, out string reply)
        {
            reply = null;

            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string command = text.Trim();

            if (!command.StartsWith(Prefix))
            {
                return false;
            }

            if (!command.ToLowerInvariant().Equals(AnswerCommand))
            {
                reply = $"No conozco el comando {command}. Disponible: {AnswerCommand}.";
                return true;
            }

            bool enabled = !DebugStore.Instance.State.HighlightMazeAnswers;
            DebugStore.Instance.Dispatch(DebugAction.SetMazeAnswerHighlight(enabled));

            reply = enabled
                ? "El modo debug para las respuestas está activo."
                : "El modo debug para las respuestas está inactivo.";
            return true;
        }
    }
}
