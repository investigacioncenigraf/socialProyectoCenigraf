using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Session.State
{
    [Serializable]
    public struct GameSessionStateData
    {
        [SerializeField] private string selectedRoleId;

        public string SelectedRoleId => SocialProyectoCenigraf.Roles.RoleIds.Normalize(selectedRoleId);
        public bool HasSelectedRole => !string.IsNullOrWhiteSpace(selectedRoleId);

        public GameSessionStateData(string selectedRoleId)
        {
            this.selectedRoleId = SocialProyectoCenigraf.Roles.RoleIds.Normalize(selectedRoleId);
        }

        public GameSessionStateData WithSelectedRole(string roleId) =>
            new GameSessionStateData(roleId);
    }
}
