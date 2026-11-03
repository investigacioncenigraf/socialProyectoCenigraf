using System.Collections;
using SocialProyectoCenigraf.Controls;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameplayHUDView))]
    public sealed class GameplayHUDChatController : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float fadeDuration = 0.18f;

        private GameplayHUDView view;
        private CanvasGroup panelGroup;
        private Coroutine transition;
        private bool isOpen;

        private void Awake()
        {
            view = GetComponent<GameplayHUDView>();
            panelGroup = view.ChatPanel.GetComponent<CanvasGroup>();
            if (panelGroup == null)
            {
                panelGroup = view.ChatPanel.AddComponent<CanvasGroup>();
            }

            SetClosedImmediately();
        }

        private void OnEnable()
        {
            view.ChatToggleButton.onClick.AddListener(Open);
            view.ChatCloseButton.onClick.AddListener(Close);
            view.ChatDismissButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            view.ChatToggleButton.onClick.RemoveListener(Open);
            view.ChatCloseButton.onClick.RemoveListener(Close);
            view.ChatDismissButton.onClick.RemoveListener(Close);
            GameplayInputLock.Release(this);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (isOpen && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
                return;
            }

            bool enterPressed = keyboard.enterKey.wasPressedThisFrame ||
                keyboard.numpadEnterKey.wasPressedThisFrame;
            if (!enterPressed)
            {
                return;
            }

            if (!isOpen)
            {
                if (IsEditingAnotherInput())
                {
                    return;
                }

                Open();
                return;
            }

            Close();
        }

        public void Open()
        {
            if (isOpen)
            {
                FocusChatInput();
                return;
            }

            isOpen = true;
            GameplayInputLock.Acquire(this);
            view.ChatToggleButton.gameObject.SetActive(false);
            view.ChatDismissButton.gameObject.SetActive(true);
            view.ChatPanel.SetActive(true);
            StartFade(1f, false);
            StartCoroutine(FocusChatInputNextFrame());
        }

        public void Close()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            view.ChatInput.DeactivateInputField();
            if (EventSystem.current?.currentSelectedGameObject ==
                view.ChatInput.gameObject)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
            StartFade(0f, true);
        }

        private void SetClosedImmediately()
        {
            isOpen = false;
            panelGroup.alpha = 0f;
            panelGroup.interactable = false;
            panelGroup.blocksRaycasts = false;
            view.ChatPanel.SetActive(false);
            view.ChatToggleButton.gameObject.SetActive(true);
            view.ChatDismissButton.gameObject.SetActive(false);
            GameplayInputLock.Release(this);
        }

        private void StartFade(float targetAlpha, bool hideWhenFinished)
        {
            if (transition != null)
            {
                StopCoroutine(transition);
            }

            transition = StartCoroutine(FadeTo(
                targetAlpha,
                hideWhenFinished));
        }

        private IEnumerator FadeTo(float targetAlpha, bool hideWhenFinished)
        {
            float initialAlpha = panelGroup.alpha;
            float elapsed = 0f;
            panelGroup.interactable = targetAlpha > 0f;
            panelGroup.blocksRaycasts = targetAlpha > 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / fadeDuration);
                progress = progress * progress * (3f - 2f * progress);
                panelGroup.alpha = Mathf.Lerp(
                    initialAlpha,
                    targetAlpha,
                    progress);
                yield return null;
            }

            panelGroup.alpha = targetAlpha;
            transition = null;

            if (hideWhenFinished && !isOpen)
            {
                panelGroup.interactable = false;
                panelGroup.blocksRaycasts = false;
                view.ChatPanel.SetActive(false);
                view.ChatToggleButton.gameObject.SetActive(true);
                view.ChatDismissButton.gameObject.SetActive(false);
                GameplayInputLock.Release(this);
            }
        }

        private IEnumerator FocusChatInputNextFrame()
        {
            yield return null;
            if (isOpen)
            {
                FocusChatInput();
            }
        }

        private void FocusChatInput()
        {
            EventSystem.current?.SetSelectedGameObject(
                view.ChatInput.gameObject);
            view.ChatInput.ActivateInputField();
        }

        private bool IsEditingAnotherInput()
        {
            GameObject selected = EventSystem.current?.currentSelectedGameObject;
            if (selected == null)
            {
                return false;
            }

            TMP_InputField selectedInput =
                selected.GetComponentInParent<TMP_InputField>();
            return selectedInput != null && selectedInput != view.ChatInput;
        }
    }
}
