using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SocialProyectoCenigraf.Navigation
{
    [DefaultExecutionOrder(-900)]
    [DisallowMultipleComponent]
    public sealed class SceneNavigationService : MonoBehaviour
    {
        private const string BackBinding = "<Keyboard>/b";

        private static SceneNavigationService instance;

        private readonly Stack<string> sceneHistory = new Stack<string>();
        private InputAction backAction;
        private bool isLoadingScene;

        public static SceneNavigationService Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject serviceObject = new GameObject(
                        nameof(SceneNavigationService));
                    instance = serviceObject.AddComponent<SceneNavigationService>();
                }

                return instance;
            }
        }

        public bool CanGoBack => sceneHistory.Count > 0 && !isLoadingScene;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            backAction = new InputAction(
                name: "GoBack",
                type: InputActionType.Button,
                binding: BackBinding);
            backAction.performed += HandleBackPerformed;
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void OnEnable()
        {
            backAction?.Enable();
        }

        private void OnDisable()
        {
            backAction?.Disable();
        }

        private void OnDestroy()
        {
            if (backAction != null)
            {
                backAction.performed -= HandleBackPerformed;
                backAction.Dispose();
                backAction = null;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;

            if (instance == this)
            {
                instance = null;
            }
        }

        public void NavigateTo(string destinationSceneName)
        {
            if (isLoadingScene || !CanLoadScene(destinationSceneName))
            {
                return;
            }

            string currentSceneName = SceneManager.GetActiveScene().name;
            if (currentSceneName == destinationSceneName)
            {
                return;
            }

            sceneHistory.Push(currentSceneName);
            LoadScene(destinationSceneName);
        }

        public void GoBack()
        {
            if (!CanGoBack)
            {
                return;
            }

            string previousSceneName = sceneHistory.Pop();
            if (!CanLoadScene(previousSceneName))
            {
                Debug.LogError(
                    $"Cannot return to scene '{previousSceneName}' because it is not available in Build Settings.",
                    this);
                return;
            }

            LoadScene(previousSceneName);
        }

        private void HandleBackPerformed(InputAction.CallbackContext context)
        {
            GameObject selectedObject = EventSystem.current?.currentSelectedGameObject;
            if (selectedObject != null &&
                selectedObject.GetComponentInParent<TMP_InputField>() != null)
            {
                return;
            }

            GoBack();
        }

        private void LoadScene(string sceneName)
        {
            isLoadingScene = true;
            SceneManager.LoadScene(sceneName);
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            isLoadingScene = false;
        }

        private bool CanLoadScene(string sceneName)
        {
            if (!string.IsNullOrWhiteSpace(sceneName) &&
                Application.CanStreamedLevelBeLoaded(sceneName))
            {
                return true;
            }

            Debug.LogError(
                $"Scene '{sceneName}' is empty or is not enabled in Build Settings.",
                this);
            return false;
        }
    }
}
