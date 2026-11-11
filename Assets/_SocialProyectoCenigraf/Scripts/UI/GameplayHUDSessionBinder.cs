using SocialProyectoCenigraf.Roles;
using SocialProyectoCenigraf.Session.State;
using UnityEngine;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GameplayHUDView))]
    public sealed class GameplayHUDSessionBinder : MonoBehaviour
    {
        private GameplayHUDView view;
        private GameSessionStore store;

        private void Awake()
        {
            view = GetComponent<GameplayHUDView>();
        }

        private void OnEnable()
        {
            store = GameSessionStore.Instance;
            store.StateChanged += HandleStateChanged;
            Render(store.State);
        }

        private void OnDisable()
        {
            if (store != null)
            {
                store.StateChanged -= HandleStateChanged;
                store = null;
            }
        }

        private void HandleStateChanged(
            GameSessionStateData state,
            GameSessionAction action)
        {
            Render(state);
        }

        private void Render(GameSessionStateData state)
        {
            if (view == null)
            {
                return;
            }

            view.PlayerNameText.text = string.IsNullOrWhiteSpace(state.PlayerName)
                ? "Visitante"
                : state.PlayerName;
            view.PlayerLevelText.text = $"Lvl. {state.Level}";
            view.PlayerRoleText.text =
                GetRoleDisplayName(state.SelectedRoleId).ToLowerInvariant();

            float progress = PlayerProgression.GetLevelProgress01(
                state.Experience);
            RectTransform fill = view.PlayerExperienceFill;
            fill.anchorMax = new Vector2(progress, 1f);
            fill.offsetMin = Vector2.zero;
            fill.offsetMax = Vector2.zero;
        }

        private static string GetRoleDisplayName(string roleId)
        {
            switch (RoleIds.Normalize(roleId))
            {
                case RoleIds.Apprentice:
                    return "Aprendiz";
                case RoleIds.Teacher:
                    return "Docente";
                case RoleIds.Administrator:
                    return "Administrativo";
                default:
                    return "Visitante";
            }
        }
    }
}
