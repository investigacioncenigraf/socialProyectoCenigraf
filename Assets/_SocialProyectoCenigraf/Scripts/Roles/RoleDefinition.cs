using System.IO;
using UnityEngine;

namespace SocialProyectoCenigraf.Roles
{
    public enum RoleEntryPoint
    {
        Game = 0,
        Administration = 1
    }

    [CreateAssetMenu(
        fileName = "RoleDefinition",
        menuName = "Social Proyecto Cenigraf/Roles/Role Definition")]
    public sealed class RoleDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable identifier used by the state, for example: student.")]
        [SerializeField] private string id = "new-role";

        [Tooltip("Name presented to the user.")]
        [SerializeField] private string displayName = "Nuevo rol";

        [Header("Presentation")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Color buttonColor = Color.white;

        [Header("Flow")]
        [SerializeField] private RoleEntryPoint entryPoint = RoleEntryPoint.Game;

        [Header("Permissions")]
        [Tooltip("Allows this role to create and save event publications.")]
        [SerializeField] private bool canCreatePublications;

        [Header("Navigation")]
#if UNITY_EDITOR
        [Tooltip("Scene loaded after selecting this role.")]
        [SerializeField] private UnityEditor.SceneAsset destinationScene;
#endif

        [SerializeField, HideInInspector] private string destinationScenePath;

        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public Color ButtonColor => buttonColor;
        public RoleEntryPoint EntryPoint => entryPoint;
        public bool CanCreatePublications => canCreatePublications;
        public string DestinationScenePath => destinationScenePath;
        public string DestinationSceneName =>
            Path.GetFileNameWithoutExtension(destinationScenePath);

#if UNITY_EDITOR
        private void OnValidate()
        {
            id = NormalizeId(id);
            destinationScenePath = destinationScene == null
                ? string.Empty
                : UnityEditor.AssetDatabase.GetAssetPath(destinationScene);
        }

        private static string NormalizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "new-role";
            }

            return value.Trim().ToLowerInvariant().Replace(' ', '-');
        }
#endif
    }
}
