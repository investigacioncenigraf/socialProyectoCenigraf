using System.Collections.Generic;
using UnityEngine;

namespace SocialProyectoCenigraf.World.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    [AddComponentMenu("Cenigraf/NPC/Zona de advertencia")]
    public sealed class NPCWarningZone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private NPCPlayerTrackingController trackingController;
        [SerializeField]
        private NPCWarningZoneVisualFade visualFade;

        [Header("Area en la vista Scene")]
        [SerializeField] private Color areaColor = new Color(1f, 0.48f, 0.12f, 0.2f);

        private readonly HashSet<Transform> playersInside = new();

        public bool HasPlayerInside => playersInside.Count > 0;

        private void Awake()
        {
            EnsureReferences();
            EnsureTrigger();
        }

        private void OnDisable()
        {
            if (trackingController != null)
            {
                foreach (Transform player in playersInside)
                {
                    trackingController.ExitWarningZone(player);
                }
            }

            if (visualFade != null)
            {
                for (int i = 0; i < playersInside.Count; i++)
                {
                    visualFade.PlayerExitedWarningZone();
                }
            }

            playersInside.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Transform player = ResolvePlayer(other);

            if (player != null && playersInside.Add(player))
            {
                trackingController?.EnterWarningZone(player);
                visualFade?.PlayerEnteredWarningZone();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Transform player = ResolvePlayer(other);

            if (player != null && playersInside.Remove(player))
            {
                trackingController?.ExitWarningZone(player);
                visualFade?.PlayerExitedWarningZone();
            }
        }

        private static Transform ResolvePlayer(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                return other.transform;
            }

            Rigidbody2D attachedBody = other.attachedRigidbody;
            return attachedBody != null && attachedBody.CompareTag("Player")
                ? attachedBody.transform
                : null;
        }

        private void EnsureReferences()
        {
            if (trackingController == null)
            {
                trackingController =
                    GetComponentInParent<NPCPlayerTrackingController>();
            }

            if (visualFade == null)
            {
                visualFade = GetComponentInParent<NPCWarningZoneVisualFade>();
            }
        }

        private void EnsureTrigger()
        {
            Collider2D zoneCollider = GetComponent<Collider2D>();

            if (zoneCollider != null)
            {
                zoneCollider.isTrigger = true;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureReferences();
            EnsureTrigger();
        }
#endif

        private void OnDrawGizmos()
        {
            BoxCollider2D area = GetComponent<BoxCollider2D>();
            if (area == null) return;

            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = areaColor;
            Gizmos.DrawCube(area.offset, area.size);

            Color outlineColor = areaColor;
            outlineColor.a = Mathf.Max(0.9f, areaColor.a);
            Gizmos.color = outlineColor;
            Gizmos.DrawWireCube(area.offset, area.size);
            Gizmos.matrix = previousMatrix;
        }
    }
}
