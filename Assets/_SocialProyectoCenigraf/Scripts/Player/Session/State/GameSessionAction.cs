namespace SocialProyectoCenigraf.Session.State
{
    public enum GameSessionActionType
    {
        StartSession,
        SetPlayerName,
        SetSelectedRole,
        ClearSelectedRole,
        AddExperience,
        SetExperience
    }

    public readonly struct GameSessionStartPayload
    {
        public string PlayerName { get; }
        public string RoleId { get; }
        public int Experience { get; }

        public GameSessionStartPayload(
            string playerName,
            string roleId,
            int experience)
        {
            PlayerName = playerName ?? string.Empty;
            RoleId = roleId ?? string.Empty;
            Experience = experience;
        }
    }

    public readonly struct GameSessionNamePayload
    {
        public string PlayerName { get; }

        public GameSessionNamePayload(string playerName)
        {
            PlayerName = playerName ?? string.Empty;
        }
    }

    public readonly struct GameSessionExperiencePayload
    {
        public int Value { get; }

        public GameSessionExperiencePayload(int value)
        {
            Value = value;
        }
    }

    public readonly struct GameSessionRolePayload
    {
        public string RoleId { get; }

        public GameSessionRolePayload(string roleId)
        {
            RoleId = roleId ?? string.Empty;
        }
    }

    public readonly struct GameSessionAction
    {
        public GameSessionActionType Type { get; }
        public GameSessionStartPayload StartPayload { get; }
        public GameSessionNamePayload NamePayload { get; }
        public GameSessionRolePayload RolePayload { get; }
        public GameSessionExperiencePayload ExperiencePayload { get; }

        private GameSessionAction(
            GameSessionActionType type,
            GameSessionStartPayload startPayload = default,
            GameSessionNamePayload namePayload = default,
            GameSessionRolePayload rolePayload = default,
            GameSessionExperiencePayload experiencePayload = default)
        {
            Type = type;
            StartPayload = startPayload;
            NamePayload = namePayload;
            RolePayload = rolePayload;
            ExperiencePayload = experiencePayload;
        }

        public static GameSessionAction StartSession(
            string playerName,
            string roleId,
            int initialExperience = GameSessionStateData.DefaultExperience) =>
            new GameSessionAction(
                GameSessionActionType.StartSession,
                startPayload: new GameSessionStartPayload(
                    playerName,
                    roleId,
                    initialExperience));

        public static GameSessionAction SetPlayerName(string playerName) =>
            new GameSessionAction(
                GameSessionActionType.SetPlayerName,
                namePayload: new GameSessionNamePayload(playerName));

        public static GameSessionAction SetSelectedRole(string roleId) =>
            new GameSessionAction(
                GameSessionActionType.SetSelectedRole,
                rolePayload: new GameSessionRolePayload(roleId));

        public static GameSessionAction ClearSelectedRole() =>
            new GameSessionAction(GameSessionActionType.ClearSelectedRole);

        public static GameSessionAction AddExperience(int amount) =>
            new GameSessionAction(
                GameSessionActionType.AddExperience,
                experiencePayload: new GameSessionExperiencePayload(amount));

        public static GameSessionAction SetExperience(int value) =>
            new GameSessionAction(
                GameSessionActionType.SetExperience,
                experiencePayload: new GameSessionExperiencePayload(value));
    }
}
