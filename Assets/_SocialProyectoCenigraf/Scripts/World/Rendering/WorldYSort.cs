using UnityEngine;
using UnityEngine.Rendering;

namespace SocialProyectoCenigraf.World.Rendering
{
    [DisallowMultipleComponent]
    public sealed class WorldYSort : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform sortPoint;
        [SerializeField] private SortingGroup targetSortingGroup;
        [SerializeField] private Renderer targetRenderer;

        [Header("Sorting")]
        [SerializeField, Min(1)] private int ordersPerUnit = 100;
        [SerializeField] private int orderOffset = 0;
        [SerializeField] private bool updateContinuously = false;
        [SerializeField] private bool sortingEnabled = true;
        [SerializeField] private int disabledSortingOrder = 0;

        public int CurrentSortingOrder { get; private set; }
        public bool SortingEnabled => sortingEnabled;
        /// <summary>Sorting orders added per world unit of Y.</summary>
        public int OrdersPerUnit => ordersPerUnit;

        private void Reset()
        {
            FindReferences();
        }

        private void Awake()
        {
            FindReferences();

            if (sortPoint == null || !HasSortingTarget)
            {
                Debug.LogError(
                    $"{nameof(WorldYSort)} on '{name}' requires a child named " +
                    "'SortPoint' and a SortingGroup or Renderer on this object " +
                    "or its children.",
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
            // Unity stores sorting orders as a signed 16 bit value: past that they wrap around
            // and the object is drawn behind everything instead of in front.
            CurrentSortingOrder = Mathf.Clamp(
                Mathf.RoundToInt(-sortPoint.position.y * ordersPerUnit) + orderOffset,
                short.MinValue,
                short.MaxValue);

            SetTargetSortingOrder(CurrentSortingOrder);
        }

        /// <summary>Changes the base order added to the Y-based order (used by generated objects).</summary>
        public void SetOrderOffset(int offset)
        {
            orderOffset = offset;

            if (sortingEnabled && sortPoint != null && HasSortingTarget)
            {
                RefreshSortingOrder();
            }
        }

        public void SetSortingEnabled(bool value)
        {
            sortingEnabled = value;
            ApplyCurrentMode();
        }

        private void ApplyCurrentMode()
        {
            if (sortPoint == null || !HasSortingTarget)
            {
                return;
            }

            if (sortingEnabled)
            {
                RefreshSortingOrder();
                return;
            }

            CurrentSortingOrder = disabledSortingOrder;
            SetTargetSortingOrder(disabledSortingOrder);
        }

        private bool HasSortingTarget =>
            targetSortingGroup != null || targetRenderer != null;

        private void SetTargetSortingOrder(int sortingOrder)
        {
            if (targetSortingGroup != null)
            {
                targetSortingGroup.sortingOrder = sortingOrder;
                return;
            }

            if (targetRenderer != null)
            {
                targetRenderer.sortingOrder = sortingOrder;
            }
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

            if (targetSortingGroup == null)
            {
                targetSortingGroup = GetComponentInChildren<SortingGroup>(true);
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
                HasSortingTarget)
            {
                ApplyCurrentMode();
            }
        }
#endif
    }
}
