using System.Collections.Generic;
using SocialProyectoCenigraf.Player.Movement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using SocialProyectoCenigraf.Dialogue;
using TMPro;
using SocialProyectoCenigraf.Rewards;
using SocialProyectoCenigraf.Missions;

namespace SocialProyectoCenigraf.World
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class ModularInteraction : MonoBehaviour, SocialProyectoCenigraf.Mobile.IMobileInteractionTarget
    {
        [Header("Aviso de interacción")]
        [SerializeField] private GameObject interactionPrompt;
        [Header("Área en la vista Scene")]
        [SerializeField] private Color areaColor = new Color(0.65f, 0.55f, 0.78f, 0.18f);

        [Header("Diálogo (opcional)")]
        [SerializeField] private DialogueCatalog dialogueCatalog;
        [SerializeField] private string dialogueId;

        [Header("Misión (opcional)")]
        [SerializeField] private bool activateMissionOnEnter = false;
        [SerializeField] private MissionCatalog missionCatalog;
        [SerializeField] private string missionId;

        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        public bool PlayerInRange => occupants.Count > 0;

        private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
        private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
        private void OnEnable() { SetPrompt(false); SocialProyectoCenigraf.Mobile.MobileInteractionTargets.Register(this); }
        private void OnDisable() { SocialProyectoCenigraf.Mobile.MobileInteractionTargets.Unregister(this); occupants.Clear(); SetPrompt(false); }
        private static bool IsPlayer(Collider2D other)
        {
            var player = other.GetComponentInParent<PlayerMovementController>();
            return player != null && player.AcceptsLocalInput && player.isActiveAndEnabled;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsPlayer(other)) return;
            bool firstPlayerCollider = occupants.Count == 0;
            occupants.Add(other);
            if (firstPlayerCollider && activateMissionOnEnter)
                MissionJournal.TryActivate(missionCatalog, missionId);
            SetPrompt(!(DialoguePresenter.IsOpen || RewardWindow.IsOpen));
        }
        private void OnTriggerExit2D(Collider2D other)
        {
            occupants.Remove(other);
            SetPrompt(PlayerInRange && !(DialoguePresenter.IsOpen || RewardWindow.IsOpen));
        }
        private void Update()
        {
            occupants.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy || (!IsPlayer(c) && !(DialoguePresenter.IsOpen || RewardWindow.IsOpen)));
            SetPrompt(PlayerInRange && !(DialoguePresenter.IsOpen || RewardWindow.IsOpen));
            if (!PlayerInRange || (DialoguePresenter.IsOpen || RewardWindow.IsOpen) || dialogueCatalog == null || string.IsNullOrWhiteSpace(dialogueId)
                || (Time.frameCount == DialoguePresenter.LastClosedFrame || Time.frameCount == RewardWindow.LastClosedFrame)
                || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && (selected.GetComponentInParent<TMP_InputField>() != null
                || selected.GetComponentInParent<UnityEngine.UI.InputField>() != null)) return;
            Interact();
        }
        public bool Interact()
        {
            if (!PlayerInRange || (DialoguePresenter.IsOpen || RewardWindow.IsOpen) || dialogueCatalog == null
                || string.IsNullOrWhiteSpace(dialogueId)
                || (Time.frameCount == DialoguePresenter.LastClosedFrame || Time.frameCount == RewardWindow.LastClosedFrame)) return false;
            foreach (var collider in occupants)
            {
                if (collider == null) continue;
                var player = collider.GetComponentInParent<PlayerMovementController>();
                bool opened = DialoguePresenter.TryOpen(dialogueCatalog, dialogueId, player);
                if (opened)
                {
                    SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayInteraction();
                    SetPrompt(false);
                }
                return opened;
            }
            return false;
        }
        public Vector3 InteractionPosition => transform.position;
        public bool CanInteractWith(PlayerMovementController player)
        {
            if (player == null || !player.AcceptsLocalInput || !player.isActiveAndEnabled || DialoguePresenter.IsOpen || RewardWindow.IsOpen
                || dialogueCatalog == null || !dialogueCatalog.TryGet(dialogueId, out _)) return false;
            foreach (var collider in occupants)
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy
                    && collider.GetComponentInParent<PlayerMovementController>() == player) return true;
            return false;
        }
        public void InteractWith(PlayerMovementController player) { if (CanInteractWith(player)) Interact(); }
        private void SetPrompt(bool visible)
        {
            if (interactionPrompt != null && interactionPrompt != gameObject
                && interactionPrompt.activeSelf != visible) interactionPrompt.SetActive(visible);
        }
        private void OnDrawGizmos()
        {
            var area = GetComponent<BoxCollider2D>();
            if (area == null) return;
            Matrix4x4 previous = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = areaColor;
            Gizmos.DrawCube(area.offset, area.size);
            Color outline = areaColor;
            outline.a = 0.75f;
            Gizmos.color = outline;
            Gizmos.DrawWireCube(area.offset, area.size);
            Gizmos.matrix = previous;
        }
    }
}
