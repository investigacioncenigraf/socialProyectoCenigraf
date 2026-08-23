using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.State
{
    public sealed class PlayerStateStore : MonoBehaviour
    {
        [Header("Current state (read only at runtime)")]
        [SerializeField] private PlayerStateData state;

        public PlayerStateData State => state;

        public event Action<PlayerStateData, PlayerAction> StateChanged;

        public void Initialize(PlayerStateData initialState)
        {
            state = initialState;
        }

        public void Dispatch(PlayerAction action)
        {
            PlayerStateData nextState = PlayerStateReducer.Reduce(state, action);

            if (StatesAreEqual(nextState, state))
            {
                return;
            }

            state = nextState;
            StateChanged?.Invoke(state, action);
        }

        public void SetYSortEnabled(bool enabled)
        {
            Dispatch(PlayerAction.SetYSortEnabled(enabled));
        }

        public void SetRole(string roleId)
        {
            Dispatch(PlayerAction.SetRole(roleId));
        }

        public void SetSkin(string skinId)
        {
            Dispatch(PlayerAction.SetSkin(skinId));
        }

        public void SetFacingFromMovement(Vector2 movementDirection)
        {
            Dispatch(PlayerAction.SetFacingFromMovement(movementDirection));
        }

        public void SetAnimationSettings(
            int frameDurationMilliseconds,
            int framesPerAnimation)
        {
            Dispatch(PlayerAction.SetAnimationSettings(
                frameDurationMilliseconds,
                framesPerAnimation));
        }

        public void SetForceFrontAnimationOnHorizontalMovement(bool enabled)
        {
            Dispatch(
                PlayerAction.SetForceFrontAnimationOnHorizontalMovement(
                    enabled));
        }

        public void SetAppearanceColors(
            Color headColor,
            Color bodyColor,
            Color handsColor)
        {
            Dispatch(PlayerAction.SetAppearanceColors(
                headColor,
                bodyColor,
                handsColor));
        }

        public void SetColliderSize(Vector2 size)
        {
            Dispatch(PlayerAction.SetColliderSize(size));
        }

        public void SetColliderOffset(Vector2 offset)
        {
            Dispatch(PlayerAction.SetColliderOffset(offset));
        }

        private static bool StatesAreEqual(
            PlayerStateData first,
            PlayerStateData second)
        {
            return first.Position == second.Position &&
                   first.RoleId == second.RoleId &&
                   first.SkinId == second.SkinId &&
                   first.FacingDirection == second.FacingDirection &&
                   first.AnimationFrameDurationMilliseconds ==
                       second.AnimationFrameDurationMilliseconds &&
                   first.FramesPerAnimation == second.FramesPerAnimation &&
                   first.ForceFrontAnimationOnHorizontalMovement ==
                       second.ForceFrontAnimationOnHorizontalMovement &&
                   first.HeadColor == second.HeadColor &&
                   first.BodyColor == second.BodyColor &&
                   first.HandsColor == second.HandsColor &&
                   first.YSortEnabled == second.YSortEnabled &&
                   first.ColliderSize == second.ColliderSize &&
                   first.ColliderOffset == second.ColliderOffset;
        }
    }
}
