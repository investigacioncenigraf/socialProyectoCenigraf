using UnityEngine;

namespace SocialProyectoCenigraf.World.NPC
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/NPC/Indicador de interaccion")]
    public sealed class NPCInteractionIndicatorController : MonoBehaviour
    {
        [Header("Indicator")]
        [SerializeField] private GameObject indicatorObject;
        [SerializeField] private SpriteRenderer indicatorRenderer;

        public bool IsVisible =>
            indicatorObject != null &&
            indicatorObject.activeInHierarchy &&
            indicatorRenderer != null &&
            indicatorRenderer.enabled;

        private void Awake()
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            SetVisible(false);
        }

        private void OnDisable()
        {
            SetVisible(false);
        }

        public void SetVisible(bool visible)
        {
            if (indicatorObject != null && !indicatorObject.activeSelf)
            {
                indicatorObject.SetActive(true);
            }

            if (indicatorRenderer != null)
            {
                indicatorRenderer.enabled = visible;
            }
        }

        public void Show()
        {
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }
    }
}
