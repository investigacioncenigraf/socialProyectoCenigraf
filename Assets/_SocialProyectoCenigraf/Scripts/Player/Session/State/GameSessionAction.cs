namespace SocialProyectoCenigraf.Session.State
{
    public enum GameSessionActionType
    {
        SetSelectedRole,
        ClearSelectedRole
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
        public GameSessionRolePayload RolePayload { get; }

        private GameSessionAction(
            GameSessionActionType type,
            GameSessionRolePayload rolePayload = default)
        {
            Type = type;
            RolePayload = rolePayload;
        }

        public static GameSessionAction SetSelectedRole(string roleId) =>
            new GameSessionAction(
                GameSessionActionType.SetSelectedRole,
                new GameSessionRolePayload(roleId));

        public static GameSessionAction ClearSelectedRole() =>
            new GameSessionAction(GameSessionActionType.ClearSelectedRole);
    }
}
