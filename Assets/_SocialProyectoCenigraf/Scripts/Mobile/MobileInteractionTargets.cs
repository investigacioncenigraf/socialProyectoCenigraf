using System.Collections.Generic;
using SocialProyectoCenigraf.Player.Movement;
using UnityEngine;

namespace SocialProyectoCenigraf.Mobile
{
    public interface IMobileInteractionTarget
    {
        bool CanInteractWith(PlayerMovementController player);
        void InteractWith(PlayerMovementController player);
        Vector3 InteractionPosition { get; }
    }

    public static class MobileInteractionTargets
    {
        private static readonly HashSet<IMobileInteractionTarget> targets = new HashSet<IMobileInteractionTarget>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => targets.Clear();
        public static void Register(IMobileInteractionTarget target) => targets.Add(target);
        public static void Unregister(IMobileInteractionTarget target) => targets.Remove(target);

        public static IMobileInteractionTarget Nearest(PlayerMovementController player)
        {
            if (player == null) return null;
            IMobileInteractionTarget nearest = null;
            float distance = float.PositiveInfinity;
            foreach (var target in targets)
            {
                if (!(target is MonoBehaviour behaviour) || behaviour == null || !behaviour.isActiveAndEnabled
                    || !target.CanInteractWith(player)) continue;
                float candidate = (target.InteractionPosition - player.transform.position).sqrMagnitude;
                if (candidate < distance) { distance = candidate; nearest = target; }
            }
            return nearest;
        }
    }
}
