using System;
using System.Collections;
using SocialProyectoCenigraf.Controls;
using SocialProyectoCenigraf.Debugging;
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
            view.ChatSendButton.onClick.AddListener(SubmitChatInput);
            view.ChatCloseButton.onClick.AddListener(Close);
            view.ChatDismissButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            view.ChatToggleButton.onClick.RemoveListener(Open);
            view.ChatSendButton.onClick.RemoveListener(SubmitChatInput);
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

            // A command keeps the chat open so its answer can be read.
            if (TryRunCommand())
            {
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

        private void SubmitChatInput()
        {
            TryRunCommand();
        }

        /// <summary>Runs a debug command typed in the chat and writes its answer in the panel.</summary>
        private bool TryRunCommand()
        {
            if (view.ChatInput == null ||
                !DebugChatCommands.TryExecute(view.ChatInput.text, out string reply))
            {
                return false;
            }

            view.ChatInput.text = string.Empty;
            AppendMessage("Debug", reply);
            FocusChatInput();
            return true;
        }

        private void AppendMessage(string sender, string message)
        {
            RectTransform content = view.ChatMessagesContent;

            if (content == null)
            {
                return;
            }

            Transform template = content.Find("ITEM_ChatMessage_Template");

            if (template == null)
            {
                return;
            }

            GameObject item = Instantiate(template.gameObject, content);
            item.name = "ITEM_ChatMessage";
            item.SetActive(true);

            SetChildText(item, "TXT_Sender", sender);
            SetChildText(item, "TXT_Time", DateTime.Now.ToString("HH:mm"));
            SetChildText(item, "TXT_Message", message);

            if (view.ChatMessages != null)
            {
                Canvas.ForceUpdateCanvases();
                view.ChatMessages.verticalNormalizedPosition = 0f;
            }
        }

        private static void SetChildText(GameObject item, string childName, string value)
        {
            Transform child = item.transform.Find(childName);

            if (child == null)
            {
                return;
            }

            TMP_Text text = child.GetComponent<TMP_Text>();

            if (text != null)
            {
                text.text = value;
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
