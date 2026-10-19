using UnityEngine;
namespace SocialProyectoCenigraf.Rewards
{
    [CreateAssetMenu(menuName="Cenigraf/Reward",fileName="Reward")]
    public sealed class RewardDefinition : ScriptableObject
    {
        public string id;
        public string displayName;
        [TextArea(2,4)] public string description;
        public Sprite icon;
        [Tooltip("Rol que se asigna al confirmar Continuar. Vacío para recompensas sin cambio de rol.")]
        public SocialProyectoCenigraf.Roles.RoleDefinition roleOnContinue;
    }
}
