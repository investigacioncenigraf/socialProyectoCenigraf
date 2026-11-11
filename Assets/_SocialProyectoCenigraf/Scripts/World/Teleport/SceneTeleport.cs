using System.Collections.Generic;
using SocialProyectoCenigraf.Player.Movement;
using TMPro;
using SocialProyectoCenigraf.Dialogue;
using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.Roles;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SocialProyectoCenigraf.World.Teleport
{
    [System.Serializable]
    public sealed class TeleportAccessRequirement
    {
        [Tooltip("Tipo de objetivo que debe cumplirse para usar el portal.")]
        public DialogueConditionType conditionType;
        [Tooltip("ID del ítem, misión, personaje o diálogo requerido.")]
        public string conditionId;
    }

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

        [Header("Activación y permisos de acceso")]
        [Tooltip("Si está desactivado, el jugador todavía puede interactuar y recibirá el aviso local; no cambiará de escena.")]
        [SerializeField] private bool portalActive = true;
        [Tooltip("Todas las condiciones configuradas deben cumplirse para viajar. Si no, se abre el diálogo local o el aviso de bloqueo.")]
        [SerializeField] private TeleportAccessRequirement[] accessRequirements = new TeleportAccessRequirement[0];
        [SerializeField] private bool restrictByRole;
        [SerializeField] private RoleCatalog roleCatalog;
        [SerializeField] private List<string> excludedRoleIds = new List<string>();
        [SerializeField, TextArea(1, 3)] private string accessDeniedMessage = "Tu rol no tiene permiso para entrar a esta zona.";
        [Tooltip("Texto de pista para un portal inactivo o para requisitos de acceso que aún no se cumplen.")]
        [SerializeField, TextArea(1, 3)] private string requirementsNotMetMessage = "Todavía necesitas cumplir un objetivo para activar este portal.";

        [Header("Diálogo antes de viajar (opcional)")]
        [Tooltip("Si se configura, al interactuar se abre este diálogo y el viaje ocurre al terminarlo.")]
        [SerializeField] private DialogueCatalog travelDialogueCatalog;
        [SerializeField] private string travelDialogueId;

        [Header("Recompensa al llegar (opcional)")]
        [Tooltip("Se entrega en la escena de destino, no en esta.")]
        [SerializeField] private SocialProyectoCenigraf.Rewards.RewardDefinition arrivalReward;
        [SerializeField] private SocialProyectoCenigraf.Items.ItemCatalog arrivalItemCatalog;
        [SerializeField] private string arrivalItemId;
        [Tooltip("Lo que se entrega si el jugador repite la prueba y ya tiene la recompensa.")]
        [SerializeField] private string repeatItemId = "pepita_oro";
        [Min(1)] [SerializeField] private int repeatItemAmount = 5;

        [Header("Diálogo local al bloquear el portal (opcional)")]
        [SerializeField] private DialogueCatalog lockedDialogueCatalog;
        [SerializeField] private string lockedDialogueId;

        private readonly HashSet<Collider2D> occupants = new HashSet<Collider2D>();
        public string TeleportId => (teleportId ?? string.Empty).Trim();
        public string LockedDialogueId => (lockedDialogueId ?? string.Empty).Trim();
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
        public bool IsPortalActive => portalActive;

        /// <summary>Points this portal at another scene from code (used by the maze exit room).</summary>
        public void ConfigureDestination(string scenePath, string arrivalTeleportId)
        {
            destinationScenePath = (scenePath ?? string.Empty).Trim();
            destinationTeleportId = (arrivalTeleportId ?? string.Empty).Trim();
        }

        /// <summary>Sets the dialogue shown before travelling (used by the maze exit room).</summary>
        public void ConfigureTravelDialogue(DialogueCatalog catalog, string dialogueId)
        {
            travelDialogueCatalog = catalog;
            travelDialogueId = (dialogueId ?? string.Empty).Trim();
        }

        /// <summary>Sets what the player receives once he reaches the destination scene.</summary>
        public void ConfigureArrivalReward(
            SocialProyectoCenigraf.Rewards.RewardDefinition reward,
            SocialProyectoCenigraf.Items.ItemCatalog catalog,
            string itemId)
        {
            arrivalReward = reward;
            arrivalItemCatalog = catalog;
            arrivalItemId = (itemId ?? string.Empty).Trim();
        }

        /// <summary>What the player gets when he passes the test again.</summary>
        public void ConfigureRepeatReward(string itemId, int amount)
        {
            repeatItemId = (itemId ?? string.Empty).Trim();
            repeatItemAmount = Mathf.Max(1, amount);
        }

        public void SetPortalActive(bool active)
        {
            portalActive = active;
            if (!active) SetPrompt(false);
        }

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
                && !DialogoOfficial.IsOpen
                && !SocialProyectoCenigraf.Rewards.RewardWindow.IsOpen
                && Time.frameCount != SocialProyectoCenigraf.Rewards.RewardWindow.LastClosedFrame
                && Time.frameCount != SocialProyectoCenigraf.Dialogue.DialoguePresenter.LastClosedFrame
                && Time.frameCount != DialogoOfficial.LastClosedFrame;
            bool available = inRange && !string.IsNullOrWhiteSpace(destinationScenePath);
            SetPrompt(inRange);
            if (SocialProyectoCenigraf.Controls.GameplayInputLock.IsLocked || !available || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
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
            if (SocialProyectoCenigraf.Controls.GameplayInputLock.IsLocked || player == null || !player.AcceptsLocalInput || !player.isActiveAndEnabled || TeleportTravel.IsTraveling
                || SocialProyectoCenigraf.Dialogue.DialoguePresenter.IsOpen
                || DialogoOfficial.IsOpen
                || SocialProyectoCenigraf.Rewards.RewardWindow.IsOpen
                || string.IsNullOrWhiteSpace(destinationScenePath)) return false;
            bool inside = false;
            foreach (var collider in occupants)
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy
                    && collider.GetComponentInParent<PlayerMovementController>() == player) { inside = true; break; }
            if (!inside) return false;
            if (!portalActive)
            {
                ShowAccessDenied(player, string.IsNullOrWhiteSpace(requirementsNotMetMessage)
                    ? "Este portal todavía no está activo."
                    : requirementsNotMetMessage);
                return false;
            }
            var state = player.GetComponent<PlayerStateStore>();
            string roleId = state != null ? state.State.RoleId : PlayerStateData.DefaultRoleId;
            if (!CanAccessRole(roleId))
            {
                ShowAccessDenied(player, string.IsNullOrWhiteSpace(accessDeniedMessage)
                    ? "Tu rol no tiene permiso para entrar a esta zona." : accessDeniedMessage);
                return false;
            }
            if (!HasMetAccessRequirements())
            {
                ShowAccessDenied(player, string.IsNullOrWhiteSpace(requirementsNotMetMessage)
                    ? "Todavía necesitas cumplir un objetivo para activar este portal." : requirementsNotMetMessage);
                return false;
            }
            if (TryOpenTravelDialogue(player))
            {
                return false;
            }

            ScheduleArrivalReward();
            TeleportTravel.Begin(destinationScenePath, (destinationTeleportId ?? string.Empty).Trim());
            if (TeleportTravel.IsTraveling)
                SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayInteraction();
            return TeleportTravel.IsTraveling;
        }
        private bool waitingForTravelDialogue;
        private PlayerMovementController dialoguePlayer;

        private bool TryOpenTravelDialogue(PlayerMovementController player)
        {
            if (travelDialogueCatalog == null ||
                string.IsNullOrWhiteSpace(travelDialogueId) ||
                waitingForTravelDialogue)
            {
                return false;
            }

            if (!DialoguePresenter.TryOpen(travelDialogueCatalog, travelDialogueId.Trim(), player))
            {
                return false;
            }

            waitingForTravelDialogue = true;
            dialoguePlayer = player;
            DialogueProgress.DialogueCompleted += HandleTravelDialogueCompleted;
            return true;
        }

        private void HandleTravelDialogueCompleted(string dialogueId)
        {
            if (!waitingForTravelDialogue ||
                !string.Equals(dialogueId, travelDialogueId.Trim(), System.StringComparison.Ordinal))
            {
                return;
            }

            DialogueProgress.DialogueCompleted -= HandleTravelDialogueCompleted;
            waitingForTravelDialogue = false;

            if (string.IsNullOrWhiteSpace(destinationScenePath))
            {
                return;
            }

            ScheduleArrivalReward();
            TeleportTravel.Begin(destinationScenePath, (destinationTeleportId ?? string.Empty).Trim());
            dialoguePlayer = null;
        }

        private void ScheduleArrivalReward()
        {
            if (arrivalReward == null)
            {
                return;
            }

            SocialProyectoCenigraf.Rewards.PendingRewardDelivery.Schedule(
                arrivalReward,
                destinationScenePath,
                arrivalItemCatalog,
                arrivalItemId,
                repeatItemId,
                repeatItemAmount);
        }

        public Vector3 InteractionPosition => transform.position;
        public bool CanInteractWith(PlayerMovementController player)
        {
            if (SocialProyectoCenigraf.Controls.GameplayInputLock.IsLocked || player == null || !player.AcceptsLocalInput || !player.isActiveAndEnabled || TeleportTravel.IsTraveling
                || SocialProyectoCenigraf.Dialogue.DialoguePresenter.IsOpen
                || DialogoOfficial.IsOpen
                || SocialProyectoCenigraf.Rewards.RewardWindow.IsOpen
                || string.IsNullOrWhiteSpace(destinationScenePath)) return false;
            foreach (var collider in occupants)
                if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy
                    && collider.GetComponentInParent<PlayerMovementController>() == player) return true;
            return false;
        }
        public void InteractWith(PlayerMovementController player) { if (CanInteractWith(player)) TryTravel(player); }

        private bool HasMetAccessRequirements()
        {
            if (accessRequirements == null) return true;

            foreach (TeleportAccessRequirement requirement in accessRequirements)
            {
                if (requirement == null || string.IsNullOrWhiteSpace(requirement.conditionId))
                    continue;

                string id = requirement.conditionId.Trim();
                bool isMet = requirement.conditionType switch
                {
                    DialogueConditionType.ItemOwned => SocialProyectoCenigraf.Items.PlayerItemInventory.HasItem(id),
                    DialogueConditionType.MissionActive => SocialProyectoCenigraf.Missions.MissionJournal.IsActive(id),
                    DialogueConditionType.MissionCompleted => SocialProyectoCenigraf.Missions.MissionJournal.IsCompleted(id),
                    DialogueConditionType.TalkedToCharacter => DialogueProgress.HasTalkedToCharacter(id),
                    DialogueConditionType.DialogueCompleted => DialogueProgress.HasCompletedDialogue(id),
                    DialogueConditionType.DialogueNodeVisited => DialogueProgress.HasVisitedNode(id),
                    _ => false
                };

                if (!isMet) return false;
            }

            return true;
        }

        private void ShowAccessDenied(PlayerMovementController player, string message)
        {
            if (lockedDialogueCatalog != null && !string.IsNullOrWhiteSpace(lockedDialogueId) &&
                DialoguePresenter.TryOpen(lockedDialogueCatalog, lockedDialogueId, player))
            {
                return;
            }

            TeleportAccessNotice.Show(message);
        }

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
