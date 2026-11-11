using SocialProyectoCenigraf.Controls;
using SocialProyectoCenigraf.Dialogue;
using SocialProyectoCenigraf.Mobile;
using SocialProyectoCenigraf.Player.Movement;
using SocialProyectoCenigraf.Rewards;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SocialProyectoCenigraf.World.NPC
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/NPC/Full Screen Overlay Interaction")]
    public sealed class NPCFullscreenOverlayInteraction : MonoBehaviour, IMobileInteractionTarget
    {
        [Header("Overlay")]
        [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.6f);

        [Header("References")]
        [SerializeField] private NPCInteractionZone interactionZone;
        [SerializeField] private NPCInteractionIndicatorController indicatorController;

        public Vector3 InteractionPosition => interactionZone != null
            ? interactionZone.transform.position
            : transform.position;

        private void OnEnable()
        {
            FindReferences();
            MobileInteractionTargets.Register(this);
        }

        private void OnDisable()
        {
            MobileInteractionTargets.Unregister(this);
            indicatorController?.Hide();
        }

        private void Update()
        {
            bool unavailable = NPCFullscreenOverlay.IsOpen || DialoguePresenter.IsOpen ||
                               DialogoOfficial.IsOpen || RewardWindow.IsOpen;
            indicatorController?.SetVisible(
                interactionZone != null && interactionZone.HasPlayerInside && !unavailable);

            if (GameplayInputLock.IsLocked || unavailable ||
                Time.frameCount == NPCFullscreenOverlay.LastClosedFrame || Keyboard.current == null ||
                !Keyboard.current.eKey.wasPressedThisFrame)
                return;

            Interact();
        }

        public bool Interact()
        {
            if (GameplayInputLock.IsLocked || NPCFullscreenOverlay.IsOpen ||
                Time.frameCount == NPCFullscreenOverlay.LastClosedFrame ||
                DialoguePresenter.IsOpen || DialogoOfficial.IsOpen || RewardWindow.IsOpen ||
                interactionZone == null || !interactionZone.TryGetLocalPlayer(out _))
                return false;

            bool opened = NPCFullscreenOverlay.Show(backgroundColor);
            if (opened)
            {
                indicatorController?.Hide();
                SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayInteraction();
            }

            return opened;
        }

        public bool CanInteractWith(PlayerMovementController player)
        {
            return !GameplayInputLock.IsLocked && !NPCFullscreenOverlay.IsOpen &&
                   Time.frameCount != NPCFullscreenOverlay.LastClosedFrame &&
                   player != null && player.AcceptsLocalInput && player.isActiveAndEnabled &&
                   interactionZone != null && interactionZone.Contains(player) &&
                   !DialoguePresenter.IsOpen && !DialogoOfficial.IsOpen && !RewardWindow.IsOpen;
        }

        public void InteractWith(PlayerMovementController player)
        {
            if (CanInteractWith(player)) Interact();
        }

        private void FindReferences()
        {
            if (interactionZone == null)
                interactionZone = GetComponentInChildren<NPCInteractionZone>(true);
            if (indicatorController == null)
                indicatorController = GetComponent<NPCInteractionIndicatorController>();
        }

#if UNITY_EDITOR
        private void OnValidate() => FindReferences();
#endif
    }
}
