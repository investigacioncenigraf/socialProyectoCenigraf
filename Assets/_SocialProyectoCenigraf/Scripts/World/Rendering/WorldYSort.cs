using UnityEngine;

namespace SocialProyectoCenigraf.World.Rendering
{
    [DisallowMultipleComponent]
    public sealed class WorldYSort : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform sortPoint;
        [SerializeField] private Renderer targetRenderer;

        [Header("Sorting")]
        [SerializeField, Min(1)] private int ordersPerUnit = 100;
        [SerializeField] private int orderOffset = 0;
        [SerializeField] private bool updateContinuously = false;
        [SerializeField] private bool sortingEnabled = true;
        [SerializeField] private int disabledSortingOrder = 0;

        public int CurrentSortingOrder { get; private set; }
        public bool SortingEnabled => sortingEnabled;

        private void Reset()
        {
            FindReferences();
        }

        private void Awake()
        {
            FindReferences();

            if (sortPoint == null || targetRenderer == null)
            {
                Debug.LogError(
                    $"{nameof(WorldYSort)} on '{name}' requires a child named " +
                    "'SortPoint' and a Renderer on this object or its children.",
                    this);
                enabled = false;
                return;
            }

            ApplyCurrentMode();
        }

        private void LateUpdate()
        {
            if (sortingEnabled && updateContinuously)
            {
                RefreshSortingOrder();
            }
        }

        public void RefreshSortingOrder()
        {
            CurrentSortingOrder =
                Mathf.RoundToInt(-sortPoint.position.y * ordersPerUnit) +
                orderOffset;

            targetRenderer.sortingOrder = CurrentSortingOrder;
        }

        public void SetSortingEnabled(bool value)
        {
            sortingEnabled = value;
            ApplyCurrentMode();
        }

        private void ApplyCurrentMode()
        {
            if (sortPoint == null || targetRenderer == null)
            {
                return;
            }

            if (sortingEnabled)
            {
                RefreshSortingOrder();
                return;
            }

            CurrentSortingOrder = disabledSortingOrder;
            targetRenderer.sortingOrder = disabledSortingOrder;
        }

        private void FindReferences()
        {
            if (sortPoint == null)
            {
                sortPoint = transform.Find("SortPoint");
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>(true);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            FindReferences();

            if (!Application.isPlaying &&
                sortPoint != null &&
                targetRenderer != null)
            {
                ApplyCurrentMode();
            }
        }
#endif
    }
}
