using SocialProyectoCenigraf.Player.State;
using UnityEngine;

namespace SocialProyectoCenigraf.World.NPC
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NPCLayeredSkinController))]
    [AddComponentMenu("Cenigraf/NPC/Seguimiento del jugador")]
    public sealed class NPCPlayerTrackingController : MonoBehaviour
    {
        [Header("Tracking")]
        [SerializeField] private bool trackingEnabled = true;
        [SerializeField] private bool requiresWarningZone = true;
        [SerializeField] private bool findPlayerAutomatically = true;
        [SerializeField] private Transform target;
        [SerializeField, Min(0f)] private float horizontalDeadZone = 0.05f;

        [Header("References")]
        [SerializeField] private NPCLayeredSkinController skinController;

        private bool? wasFacingRight;
        private bool targetInsideWarningZone;

        public bool TrackingEnabled => trackingEnabled;
        public Transform Target => target;

        private void Awake()
        {
            FindReferences();
        }

        private void OnEnable()
        {
            FindReferences();
            if (!requiresWarningZone)
            {
                FindTargetIfNeeded();
            }

            RefreshFacing();
        }

        private void LateUpdate()
        {
            if (!trackingEnabled)
            {
                return;
            }

            if (requiresWarningZone && !targetInsideWarningZone)
            {
                return;
            }

            FindTargetIfNeeded();
            RefreshFacing();
        }

        public void SetTrackingEnabled(bool value)
        {
            trackingEnabled = value;

            if (trackingEnabled)
            {
                RefreshFacing();
            }
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            targetInsideWarningZone = newTarget != null;
            wasFacingRight = null;
            RefreshFacing();
        }

        public void EnterWarningZone(Transform player)
        {
            if (player == null)
            {
                return;
            }

            target = player;
            targetInsideWarningZone = true;
            wasFacingRight = null;
            RefreshFacing();
        }

        public void ExitWarningZone(Transform player)
        {
            if (player == null || target != player)
            {
                return;
            }

            targetInsideWarningZone = false;
            target = null;
            wasFacingRight = null;
        }

        public void RefreshFacing()
        {
            if (!trackingEnabled || target == null || skinController == null ||
                (requiresWarningZone && !targetInsideWarningZone))
            {
                return;
            }

            float horizontalDistance = target.position.x - transform.position.x;

            if (Mathf.Abs(horizontalDistance) <= horizontalDeadZone)
            {
                return;
            }

            bool shouldFaceRight = horizontalDistance > 0f;

            if (wasFacingRight == shouldFaceRight)
            {
                return;
            }

            bool isBackFacing =
                skinController.FacingDirection == PlayerFacingDirection.UpRight ||
                skinController.FacingDirection == PlayerFacingDirection.UpLeft;
            PlayerFacingDirection direction;

            if (isBackFacing)
            {
                direction = shouldFaceRight
                    ? PlayerFacingDirection.UpRight
                    : PlayerFacingDirection.UpLeft;
            }
            else
            {
                direction = shouldFaceRight
                    ? PlayerFacingDirection.DownRight
                    : PlayerFacingDirection.DownLeft;
            }

            skinController.SetFacingDirection(direction);
            wasFacingRight = shouldFaceRight;
        }

        private void FindTargetIfNeeded()
        {
            if (target != null || !findPlayerAutomatically ||
                requiresWarningZone)
            {
                return;
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                target = player.transform;
            }
        }

        private void FindReferences()
        {
            if (skinController == null)
            {
                skinController = GetComponent<NPCLayeredSkinController>();
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            horizontalDeadZone = Mathf.Max(0f, horizontalDeadZone);
            FindReferences();
        }
#endif
    }
}
