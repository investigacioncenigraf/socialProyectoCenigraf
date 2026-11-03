using SocialProyectoCenigraf.Controls;
using SocialProyectoCenigraf.Dialogue;
using SocialProyectoCenigraf.Mobile;
using SocialProyectoCenigraf.Player.Movement;
using SocialProyectoCenigraf.Rewards;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.World.NPC
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/NPC/Interaccion de dialogo")]
    public sealed class NPCDialogueInteractionController : MonoBehaviour,
        IMobileInteractionTarget
    {
        [Header("Dialogue")]
        [SerializeField] private DialogueCatalog dialogueCatalog;
        [SerializeField] private string dialogueId;

        [Header("References")]
        [SerializeField] private NPCInteractionZone interactionZone;
        [SerializeField]
        private NPCInteractionIndicatorController indicatorController;

        public Vector3 InteractionPosition => interactionZone != null
            ? interactionZone.transform.position
            : transform.position;

        private void Awake()
        {
            FindReferences();
        }

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
            bool dialogueUnavailable =
                DialoguePresenter.IsOpen || RewardWindow.IsOpen;
            indicatorController?.SetVisible(
                interactionZone != null &&
                interactionZone.HasPlayerInside &&
                !dialogueUnavailable);

            if (GameplayInputLock.IsLocked || dialogueUnavailable ||
                Time.frameCount == DialoguePresenter.LastClosedFrame ||
                Time.frameCount == RewardWindow.LastClosedFrame ||
                Keyboard.current == null ||
                !Keyboard.current.eKey.wasPressedThisFrame ||
                IsWritingInInputField())
            {
                return;
            }

            Interact();
        }

        public bool Interact()
        {
            if (GameplayInputLock.IsLocked || DialoguePresenter.IsOpen ||
                RewardWindow.IsOpen || interactionZone == null ||
                !interactionZone.TryGetLocalPlayer(out PlayerMovementController player) ||
                dialogueCatalog == null || string.IsNullOrWhiteSpace(dialogueId) ||
                Time.frameCount == DialoguePresenter.LastClosedFrame ||
                Time.frameCount == RewardWindow.LastClosedFrame)
            {
                return false;
            }

            bool opened = DialoguePresenter.TryOpen(
                dialogueCatalog,
                dialogueId,
                player);

            if (opened)
            {
                indicatorController?.Hide();
                SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayInteraction();
            }

            return opened;
        }

        public bool CanInteractWith(PlayerMovementController player)
        {
            return !GameplayInputLock.IsLocked &&
                   player != null &&
                   player.AcceptsLocalInput &&
                   player.isActiveAndEnabled &&
                   interactionZone != null &&
                   interactionZone.Contains(player) &&
                   !DialoguePresenter.IsOpen &&
                   !RewardWindow.IsOpen &&
                   dialogueCatalog != null &&
                   dialogueCatalog.TryGet(dialogueId, out _);
        }

        public void InteractWith(PlayerMovementController player)
        {
            if (CanInteractWith(player))
            {
                Interact();
            }
        }

        private void FindReferences()
        {
            if (interactionZone == null)
            {
                interactionZone = GetComponentInChildren<NPCInteractionZone>(true);
            }

            if (indicatorController == null)
            {
                indicatorController =
                    GetComponent<NPCInteractionIndicatorController>();
            }
        }

        private static bool IsWritingInInputField()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            return selected != null &&
                   (selected.GetComponentInParent<TMP_InputField>() != null ||
                    selected.GetComponentInParent<InputField>() != null);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindReferences();
        }
#endif
    }
}
