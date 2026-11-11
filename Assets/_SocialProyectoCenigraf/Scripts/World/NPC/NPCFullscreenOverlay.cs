using SocialProyectoCenigraf.Controls;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.World.NPC
{
    /// <summary>Empty, dismissible full-screen overlay used for rival encounters.</summary>
    public sealed class NPCFullscreenOverlay : MonoBehaviour
    {
        private static NPCFullscreenOverlay instance;

        private Canvas canvas;
        private Image background;
        private bool isOpen;
        private int openedFrame = -1;

        public static bool IsOpen => instance != null && instance.isOpen;
        public static int LastClosedFrame { get; private set; } = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
            LastClosedFrame = -1;
        }

        public static bool Show(Color backgroundColor)
        {
            NPCFullscreenOverlay overlay = GetInstance();
            return overlay != null && overlay.Open(backgroundColor);
        }

        private static NPCFullscreenOverlay GetInstance()
        {
            if (instance != null) return instance;

            GameObject host = new GameObject("NPCFullscreenOverlay");
            return host.AddComponent<NPCFullscreenOverlay>();
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
            BuildInterface();
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnDestroy()
        {
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            GameplayInputLock.Release(this);
            if (instance == this) instance = null;
        }

        private bool Open(Color backgroundColor)
        {
            if (isOpen) return false;

            background.color = backgroundColor;
            canvas.gameObject.SetActive(true);
            isOpen = true;
            openedFrame = Time.frameCount;
            GameplayInputLock.Acquire(this);
            return true;
        }

        private void Close()
        {
            if (!isOpen) return;

            isOpen = false;
            canvas.gameObject.SetActive(false);
            GameplayInputLock.Release(this);
            LastClosedFrame = Time.frameCount;
        }

        private void Update()
        {
            if (!isOpen || Time.frameCount == openedFrame) return;

            bool closeWithKeyboard = Keyboard.current != null &&
                                     (Keyboard.current.escapeKey.wasPressedThisFrame ||
                                      Keyboard.current.enterKey.wasPressedThisFrame ||
                                      Keyboard.current.eKey.wasPressedThisFrame);
            bool closeWithGamepad = Gamepad.current != null &&
                                    Gamepad.current.buttonEast.wasPressedThisFrame;
            bool closeWithTouch = Touchscreen.current != null &&
                                  Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            if (closeWithKeyboard || closeWithGamepad || closeWithTouch) Close();
        }

        private void OnActiveSceneChanged(Scene previous, Scene current) => Close();

        private void BuildInterface()
        {
            GameObject canvasObject = new GameObject(
                "OverlayCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 4000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject backgroundObject = new GameObject(
                "TranslucentBlackBackground",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            RectTransform rect = (RectTransform)backgroundObject.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            background = backgroundObject.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.6f);
            background.raycastTarget = true;
            canvasObject.SetActive(false);
        }
    }
}
