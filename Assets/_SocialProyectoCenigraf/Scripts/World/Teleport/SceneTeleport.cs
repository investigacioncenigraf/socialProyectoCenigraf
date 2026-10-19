using System.Collections.Generic;
using SocialProyectoCenigraf.Player.Movement;
using TMPro;
using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.Roles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SocialProyectoCenigraf.World.Teleport
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SceneTeleport : MonoBehaviour, SocialProyectoCenigraf.Mobile.IMobileInteractionTarget
    {
        [Header("Identidad de este teleport")]
        [Tooltip("Código de texto único en esta escena. Conserva ceros iniciales, por ejemplo 024.")]
        [SerializeField] private string teleportId = "001";
        [Tooltip("Punto de llegada opcional. Si está vacío se utiliza la posición de este objeto.")]
        [SerializeField] private Transform spawnPoint;

        [Header("Destino (opcional para puntos que solo reciben jugadores)")]
        [SerializeField] private string destinationScenePath;
        [SerializeField] private string destinationTeleportId;
        [SerializeField] private GameObject interactionPrompt;

        [Header("Área en la vista Scene")]
        [SerializeField] private Color areaColor = new Color(0.45f, 0.76f, 0.78f, 0.18f);

        [Header("Permisos de acceso (opcional)")]
        [SerializeField] private bool restrictByRole;
        [SerializeField] private RoleCatalog roleCatalog;
        [SerializeField] private List<string> excludedRoleIds = new List<string>();
        [SerializeField, TextArea(1, 3)] private string accessDeniedMessage = "Tu rol no tiene permiso para entrar a esta zona.";

        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        public string TeleportId => (teleportId ?? string.Empty).Trim();
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;

        private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;
        private void Awake() => GetComponent<BoxCollider2D>().isTrigger = true;
        private void OnEnable() { SetPrompt(false); SocialProyectoCenigraf.Mobile.MobileInteractionTargets.Register(this); }
        private void OnDisable() { SocialProyectoCenigraf.Mobile.MobileInteractionTargets.Unregister(this); occupants.Clear(); SetPrompt(false); }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayer(other)) occupants.Add(other);
        }
        private void OnTriggerExit2D(Collider2D other) => occupants.Remove(other);
        private static bool IsPlayer(Collider2D other)
        {
            var movement = other.GetComponentInParent<PlayerMovementController>();
            return movement != null && movement.AcceptsLocalInput && movement.isActiveAndEnabled;
        }
        private void Update()
        {
            occupants.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy || (!IsPlayer(c) && !SocialProyectoCenigraf.Dialogue.DialoguePresenter.IsOpen && !SocialProyectoCenigraf.Rewards.RewardWindow.IsOpen));
            bool inRange = occupants.Count > 0 && !TeleportTravel.IsTraveling
                && !SocialProyectoCenigraf.Dialogue.DialoguePresenter.IsOpen
                && !SocialProyectoCenigraf.Rewards.RewardWindow.IsOpen
                && Time.frameCount != SocialProyectoCenigraf.Rewards.RewardWindow.LastClosedFrame
                && Time.frameCount != SocialProyectoCenigraf.Dialogue.DialoguePresenter.LastClosedFrame;
            bool available = inRange && !string.IsNullOrWhiteSpace(destinationScenePath);
            SetPrompt(inRange);
            if (!available || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected != null && (selected.GetComponentInParent<TMP_InputField>() != null
                || selected.GetComponentInParent<UnityEngine.UI.InputField>() != null)) return;
            foreach (var collider in occupants)
            {
                var player = collider.GetComponentInParent<PlayerMovementController>();
                if (player == null || !player.AcceptsLocalInput || !player.isActiveAndEnabled) continue;
                TryTravel(player);
                break;
            }
        }
        public bool CanAccessRole(string roleId)
        {
            if (!restrictByRole || excludedRoleIds == null) return true;
            string effectiveRole = string.IsNullOrWhiteSpace(roleId) ? PlayerStateData.DefaultRoleId : RoleIds.Normalize(roleId);
            foreach (string excluded in excludedRoleIds)
                if (RoleIds.Normalize(excluded) == effectiveRole) return false;
            return true;
        }
        public bool TryTravel(PlayerMovementController player)
        {
            if (player == null || !player.AcceptsLocalInput || !player.isActiveAndEnabled || TeleportTravel.IsTraveling
                || SocialProyectoCenigraf.Dialogue.DialoguePresenter.IsOpen
                || SocialProyectoCenigraf.Rewards.RewardWindow.IsOpen
                || string.IsNullOrWhiteSpace(destinationScenePath)) return false;
            bool inside = false;
            foreach (var collider in occupants)
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy
                    && collider.GetComponentInParent<PlayerMovementController>() == player) { inside = true; break; }
            if (!inside) return false;
            var state = player.GetComponent<PlayerStateStore>();
            string roleId = state != null ? state.State.RoleId : PlayerStateData.DefaultRoleId;
            if (!CanAccessRole(roleId))
            {
                TeleportAccessNotice.Show(string.IsNullOrWhiteSpace(accessDeniedMessage)
                    ? "Tu rol no tiene permiso para entrar a esta zona." : accessDeniedMessage);
                return false;
            }
            TeleportTravel.Begin(destinationScenePath, (destinationTeleportId ?? string.Empty).Trim());
            if (TeleportTravel.IsTraveling)
                SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayInteraction();
            return TeleportTravel.IsTraveling;
        }
        public Vector3 InteractionPosition => transform.position;
        public bool CanInteractWith(PlayerMovementController player)
        {
            if (player == null || !player.AcceptsLocalInput || !player.isActiveAndEnabled || TeleportTravel.IsTraveling
                || SocialProyectoCenigraf.Dialogue.DialoguePresenter.IsOpen
                || SocialProyectoCenigraf.Rewards.RewardWindow.IsOpen
                || string.IsNullOrWhiteSpace(destinationScenePath)) return false;
            foreach (var collider in occupants)
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy
                    && collider.GetComponentInParent<PlayerMovementController>() == player) return true;
            return false;
        }
        public void InteractWith(PlayerMovementController player) { if (CanInteractWith(player)) TryTravel(player); }
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
