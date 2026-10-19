using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.Audio
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Button))]
    public sealed class RetroButtonAudio : MonoBehaviour, IPointerEnterHandler
    {
        private Button button;
        private void Awake() => button = GetComponent<Button>();
        private void OnEnable() => button.onClick.AddListener(RetroFeedbackAudio.PlayInteraction);
        private void OnDisable() => button.onClick.RemoveListener(RetroFeedbackAudio.PlayInteraction);

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button.IsActive() && button.IsInteractable()) RetroFeedbackAudio.PlayButtonHover();
        }
    }
}
