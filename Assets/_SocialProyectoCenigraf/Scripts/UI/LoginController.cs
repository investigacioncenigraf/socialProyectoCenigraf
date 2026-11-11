using SocialProyectoCenigraf.Navigation;
using SocialProyectoCenigraf.Session.State;
using SocialProyectoCenigraf.World.Teleport;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    public sealed class LoginController : MonoBehaviour
    {
        [Header("Fields")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;

        [Header("Feedback")]
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private Button loginButton;

        [Header("Entrada")]
        [Tooltip("Segundos del difuminado desde negro al abrir la pantalla de login.")]
        [SerializeField, Min(0f)] private float introFadeSeconds = 1.25f;

        [Tooltip("Segundos en negro antes de que arranque el difuminado.")]
        [SerializeField, Min(0f)] private float introHoldSeconds = 0.15f;

        [Header("Navigation")]
        [SerializeField] private string destinationSceneName = "Sala_Lobby";

        [Tooltip("Segundos que la pantalla de carga se mantiene antes de entrar al Lobby.")]
        [SerializeField, Min(0f)] private float loadingScreenSeconds = 1f;

        private bool isSubmitting;

        // A scene carrying the login card is the login screen. Other systems ask this to
        // stay out of it, such as the scene music and the gameplay menu.
        public static bool ExistsInScene() =>
            FindFirstObjectByType<LoginController>(FindObjectsInactive.Include) != null;

        public void Configure(
            TMP_InputField username,
            TMP_InputField password,
            Button submitButton,
            TMP_Text validationError,
            TMP_Text status,
            string destination)
        {
            usernameInput = username;
            passwordInput = password;
            loginButton = submitButton;
            errorText = validationError;
            statusText = status;
            destinationSceneName = destination;
        }

        private void Awake()
        {
            // Built before the first rendered frame, so the scene opens already in black.
            ScreenFadeIn.FromBlack(introFadeSeconds, introHoldSeconds);

            if (!HasRequiredReferences())
            {
                Debug.LogError(
                    "LoginController no tiene todas sus referencias asignadas.",
                    this);
                enabled = false;
                return;
            }

            loginButton.onClick.AddListener(Submit);
            passwordInput.onSubmit.AddListener(SubmitFromPassword);
            usernameInput.onValueChanged.AddListener(HandleFieldChanged);
            passwordInput.onValueChanged.AddListener(HandleFieldChanged);
            ResetFeedback();
        }

        private void OnDestroy()
        {
            loginButton?.onClick.RemoveListener(Submit);

            if (passwordInput != null)
            {
                passwordInput.onSubmit.RemoveListener(SubmitFromPassword);
                passwordInput.onValueChanged.RemoveListener(HandleFieldChanged);
            }

            usernameInput?.onValueChanged.RemoveListener(HandleFieldChanged);
        }

        public void Submit()
        {
            if (isSubmitting)
            {
                return;
            }

            string username = usernameInput.text?.Trim() ?? string.Empty;
            string password = passwordInput.text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username) &&
                string.IsNullOrWhiteSpace(password))
            {
                ShowError("Ingresa tu usuario y tu contraseña.");
                Focus(usernameInput);
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                ShowError("Ingresa un usuario válido.");
                Focus(usernameInput);
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                ShowError("Ingresa una contraseña válida.");
                Focus(passwordInput);
                return;
            }

            usernameInput.text = username;
            isSubmitting = true;
            loginButton.interactable = false;
            SetVisible(errorText, false);

            if (statusText != null)
            {
                statusText.text = "Ingresando...";
                SetVisible(statusText, true);
            }

            GameSessionStore.Instance.StartVisitorSession(username);

            // Same loading screen as the teleports: black cover, scene load, the hold of
            // loadingScreenSeconds and the fade in. Direct navigation is the fallback.
            if (!TeleportTravel.Begin(
                destinationSceneName,
                string.Empty,
                loadingScreenSeconds))
            {
                SceneNavigationService.Instance.NavigateTo(destinationSceneName);
            }
        }

        private void SubmitFromPassword(string _)
        {
            Submit();
        }

        private void HandleFieldChanged(string _)
        {
            if (!isSubmitting)
            {
                SetVisible(errorText, false);
            }
        }

        private void ShowError(string message)
        {
            errorText.text = message;
            SetVisible(errorText, true);
            SetVisible(statusText, false);
        }

        private void ResetFeedback()
        {
            isSubmitting = false;
            loginButton.interactable = true;
            SetVisible(errorText, false);
            SetVisible(statusText, false);
        }

        private static void Focus(TMP_InputField input)
        {
            EventSystem.current?.SetSelectedGameObject(input.gameObject);
            input.ActivateInputField();
        }

        private static void SetVisible(Component component, bool visible)
        {
            if (component != null)
            {
                component.gameObject.SetActive(visible);
            }
        }

        private bool HasRequiredReferences()
        {
            return usernameInput != null &&
                passwordInput != null &&
                loginButton != null &&
                errorText != null &&
                !string.IsNullOrWhiteSpace(destinationSceneName);
        }
    }
}
