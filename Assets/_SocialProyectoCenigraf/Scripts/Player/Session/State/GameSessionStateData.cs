using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Session.State
{
    [Serializable]
    public struct GameSessionStateData
    {
        [SerializeField] private string selectedRoleId;

        public string SelectedRoleId => selectedRoleId ?? string.Empty;
        public bool HasSelectedRole => !string.IsNullOrWhiteSpace(selectedRoleId);

        public GameSessionStateData(string selectedRoleId)
        {
            this.selectedRoleId = selectedRoleId ?? string.Empty;
        }

        public GameSessionStateData WithSelectedRole(string roleId) =>
            new GameSessionStateData(roleId);
    }
}
