using System;

namespace SocialProyectoCenigraf.Session.State
{
    public static class GameSessionReducer
    {
        public static GameSessionStateData Reduce(
            GameSessionStateData currentState,
            GameSessionAction action)
        {
            switch (action.Type)
            {
                case GameSessionActionType.SetSelectedRole:
                    return currentState.WithSelectedRole(action.RolePayload.RoleId);

                case GameSessionActionType.ClearSelectedRole:
                    return currentState.WithSelectedRole(string.Empty);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(action), action.Type, "Unknown session action.");
            }
        }
    }
}
