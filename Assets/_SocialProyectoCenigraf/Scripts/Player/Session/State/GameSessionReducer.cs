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
                case GameSessionActionType.StartSession:
                    int initialExperience = PlayerProgression.NormalizeExperience(
                        action.StartPayload.Experience);
                    return new GameSessionStateData(
                        action.StartPayload.PlayerName,
                        action.StartPayload.RoleId,
                        PlayerProgression.CalculateLevel(initialExperience),
                        initialExperience);

                case GameSessionActionType.SetPlayerName:
                    return currentState.WithPlayerName(
                        action.NamePayload.PlayerName);

                case GameSessionActionType.SetSelectedRole:
                    return currentState.WithSelectedRole(action.RolePayload.RoleId);

                case GameSessionActionType.ClearSelectedRole:
                    return currentState.WithSelectedRole(string.Empty);

                case GameSessionActionType.AddExperience:
                    int addedExperience = PlayerProgression.AddExperience(
                        currentState.Experience,
                        action.ExperiencePayload.Value);
                    return currentState.WithProgress(
                        PlayerProgression.CalculateLevel(addedExperience),
                        addedExperience);

                case GameSessionActionType.SetExperience:
                    int requestedExperience = PlayerProgression.NormalizeExperience(
                        action.ExperiencePayload.Value);
                    return currentState.WithProgress(
                        PlayerProgression.CalculateLevel(requestedExperience),
                        requestedExperience);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(action), action.Type, "Unknown session action.");
            }
        }
    }
}
