using System.Collections.Generic;
using SocialProyectoCenigraf.Player.Movement;
using UnityEngine;
using UnityEngine.Events;

namespace SocialProyectoCenigraf.World.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("Cenigraf/NPC/Zona de interaccion")]
    public sealed class NPCInteractionZone : MonoBehaviour
    {
        [Header("Events")]
        [SerializeField] private UnityEvent onPlayerEntered;
        [SerializeField] private UnityEvent onPlayerExited;

        [Header("Indicator")]
        [SerializeField]
        private NPCInteractionIndicatorController indicatorController;

        private readonly HashSet<Collider2D> playerCollidersInside = new();

        public bool HasPlayerInside => playerCollidersInside.Count > 0;

        public bool TryGetLocalPlayer(out PlayerMovementController player)
        {
            foreach (Collider2D playerCollider in playerCollidersInside)
            {
                player = ResolvePlayer(playerCollider);

                if (player != null)
                {
                    return true;
                }
            }

            player = null;
            return false;
        }

        public bool Contains(PlayerMovementController player)
        {
            if (player == null)
            {
                return false;
            }

            foreach (Collider2D playerCollider in playerCollidersInside)
            {
                if (ResolvePlayer(playerCollider) == player)
                {
                    return true;
                }
            }

            return false;
        }

        private void Awake()
        {
            FindReferences();
            EnsureTrigger();
            indicatorController?.Hide();
        }

        private void OnDisable()
        {
            playerCollidersInside.Clear();
            indicatorController?.Hide();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            bool hadPlayerInside = HasPlayerInside;

            if (ResolvePlayer(other) != null &&
                playerCollidersInside.Add(other) &&
                !hadPlayerInside)
            {
                indicatorController?.Show();
                onPlayerEntered?.Invoke();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!playerCollidersInside.Remove(other))
            {
                return;
            }

            if (!HasPlayerInside)
            {
                indicatorController?.Hide();
                onPlayerExited?.Invoke();
            }
        }

        private static PlayerMovementController ResolvePlayer(Collider2D other)
        {
            if (other == null)
            {
                return null;
            }

            PlayerMovementController player =
                other.GetComponentInParent<PlayerMovementController>();
            return player != null &&
                   player.AcceptsLocalInput &&
                   player.isActiveAndEnabled
                ? player
                : null;
        }

        private void EnsureTrigger()
        {
            Collider2D zoneCollider = GetComponent<Collider2D>();

            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

        private void FindReferences()
        {
            if (indicatorController == null)
            {
                indicatorController =
                    GetComponentInParent<NPCInteractionIndicatorController>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindReferences();
            EnsureTrigger();
        }
#endif
    }
}
