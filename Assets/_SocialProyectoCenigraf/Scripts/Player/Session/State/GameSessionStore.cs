using System;
using SocialProyectoCenigraf.Roles;
using UnityEngine;

namespace SocialProyectoCenigraf.Session.State
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class GameSessionStore : MonoBehaviour
    {
        private static GameSessionStore instance;

        [SerializeField] private GameSessionStateData state;

        public static GameSessionStore Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject storeObject = new GameObject(nameof(GameSessionStore));
                    instance = storeObject.AddComponent<GameSessionStore>();
                }

                return instance;
            }
        }

        public GameSessionStateData State => state;
        public event Action<GameSessionStateData, GameSessionAction> StateChanged;

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

        public void SelectRole(RoleDefinition role)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            Dispatch(GameSessionAction.SetSelectedRole(role.Id));
        }

        public void Dispatch(GameSessionAction action)
        {
            GameSessionStateData nextState = GameSessionReducer.Reduce(state, action);
            if (nextState.SelectedRoleId == state.SelectedRoleId)
            {
                return;
            }

            state = nextState;
            StateChanged?.Invoke(state, action);
        }
    }
}
