using System.Collections.Generic;
using UnityEngine;

namespace SocialProyectoCenigraf.Roles
{
    [CreateAssetMenu(
        fileName = "RoleCatalog",
        menuName = "Social Proyecto Cenigraf/Roles/Role Catalog")]
    public sealed class RoleCatalog : ScriptableObject
    {
        [SerializeField] private List<RoleDefinition> roles = new List<RoleDefinition>();

        public IReadOnlyList<RoleDefinition> Roles => roles;

        public RoleDefinition FindById(string roleId)
        {
            if (string.IsNullOrWhiteSpace(roleId))
            {
                return null;
            }

            for (int i = 0; i < roles.Count; i++)
            {
                RoleDefinition role = roles[i];
                if (role != null && role.Id == roleId)
                {
                    return role;
                }
            }

            return null;
        }
    }
}
