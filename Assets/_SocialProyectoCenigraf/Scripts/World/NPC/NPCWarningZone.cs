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

            playersInside.Clear();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Transform player = ResolvePlayer(other);

            if (player != null && playersInside.Add(player))
            {
                trackingController?.EnterWarningZone(player);
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            Transform player = ResolvePlayer(other);

            if (player != null && playersInside.Remove(player))
            {
                trackingController?.ExitWarningZone(player);
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
    }
}
