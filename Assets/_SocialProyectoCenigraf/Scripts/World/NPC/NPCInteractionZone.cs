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

        [Header("Area en la vista Scene")]
        [SerializeField] private Color areaColor = new Color(0.12f, 0.82f, 0.72f, 0.22f);

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

        private void OnDrawGizmos()
        {
            DrawAreaGizmo(areaColor);
        }

        private void DrawAreaGizmo(Color fillColor)
        {
            BoxCollider2D area = GetComponent<BoxCollider2D>();
            if (area == null) return;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = fillColor;
            Gizmos.DrawCube(area.offset, area.size);

            Color outlineColor = fillColor;
            outlineColor.a = Mathf.Max(0.85f, fillColor.a);
            Gizmos.color = outlineColor;
            Gizmos.DrawWireCube(area.offset, area.size);
            Gizmos.matrix = previousMatrix;
        }
    }
}
