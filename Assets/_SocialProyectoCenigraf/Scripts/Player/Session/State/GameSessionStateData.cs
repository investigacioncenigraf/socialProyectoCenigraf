using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Session.State
{
    [Serializable]
    public struct GameSessionStateData
    {
        public const int DefaultLevel = 1;
        public const int DefaultExperience = 0;
        public const int MinimumExperience = 0;

        [SerializeField] private string playerName;
        [SerializeField] private string selectedRoleId;
        [SerializeField] private int level;
        [SerializeField] private int experience;

        public string PlayerName => playerName?.Trim() ?? string.Empty;
        public string SelectedRoleId => string.IsNullOrWhiteSpace(selectedRoleId)
            ? SocialProyectoCenigraf.Roles.RoleIds.Visitor
            : SocialProyectoCenigraf.Roles.RoleIds.Normalize(selectedRoleId);
        public int Level => Mathf.Max(DefaultLevel, level);
        public int Experience => !HasActiveSession && experience <= 0
            ? DefaultExperience
            : Mathf.Max(MinimumExperience, experience);
        public bool HasActiveSession => !string.IsNullOrWhiteSpace(PlayerName);
        public bool HasSelectedRole => !string.IsNullOrWhiteSpace(selectedRoleId);

        public GameSessionStateData(string selectedRoleId)
            : this(
                string.Empty,
                selectedRoleId,
                DefaultLevel,
                DefaultExperience)
        {
        }

        public GameSessionStateData(
            string playerName,
            string selectedRoleId,
            int level,
            int experience)
        {
            this.playerName = playerName?.Trim() ?? string.Empty;
            this.selectedRoleId =
                SocialProyectoCenigraf.Roles.RoleIds.Normalize(selectedRoleId);
            this.level = Mathf.Max(DefaultLevel, level);
            this.experience = Mathf.Max(MinimumExperience, experience);
        }

        public GameSessionStateData WithSelectedRole(string roleId) =>
            new GameSessionStateData(PlayerName, roleId, Level, Experience);

        public GameSessionStateData WithPlayerName(string value) =>
            new GameSessionStateData(value, SelectedRoleId, Level, Experience);

        public GameSessionStateData WithProgress(int newLevel, int newExperience) =>
            new GameSessionStateData(
                PlayerName,
                SelectedRoleId,
                newLevel,
                newExperience);
    }
}
