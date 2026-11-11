using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Debugging.State
{
    /// <summary>
    /// Redux-like store reserved for development/debug systems.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class DebugStore : MonoBehaviour
    {
        private static DebugStore instance;

        [SerializeField] private DebugStateData state;

        public static DebugStore Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject storeObject = new GameObject(nameof(DebugStore));
                    instance = storeObject.AddComponent<DebugStore>();
                }

                return instance;
            }
        }

        public DebugStateData State => state;
        public event Action<DebugStateData, DebugAction> StateChanged;

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
        }

        public void Dispatch(DebugAction action)
        {
            DebugStateData nextState = DebugReducer.Reduce(state, action);
            if (nextState.Equals(state))
            {
                return;
            }

            state = nextState;
            StateChanged?.Invoke(state, action);
        }
    }
}
