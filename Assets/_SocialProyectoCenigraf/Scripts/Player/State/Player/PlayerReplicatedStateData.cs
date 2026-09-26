using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.State
{
    /// <summary>
    /// Technology-independent public snapshot that a future network adapter can
    /// serialize. Local presentation and physics configuration are intentionally
    /// excluded.
    /// </summary>
    [Serializable]
    public struct PlayerReplicatedStateData
    {
        [SerializeField] private string playerId;
        [SerializeField] private Vector2 position;
        [SerializeField] private string roleId;
        [SerializeField] private string skinId;
        [SerializeField] private PlayerFacingDirection facingDirection;
        [SerializeField] private bool isMoving;
        [SerializeField] private Color headColor;
        [SerializeField] private Color bodyColor;
        [SerializeField] private Color handsColor;

        public string PlayerId => playerId ?? string.Empty;
        public Vector2 Position => position;
        public string RoleId => roleId;
        public string SkinId => skinId;
        public PlayerFacingDirection FacingDirection => facingDirection;
        public bool IsMoving => isMoving;
        public Color HeadColor => headColor;
        public Color BodyColor => bodyColor;
        public Color HandsColor => handsColor;

        public PlayerReplicatedStateData(
            string playerId,
            Vector2 position,
            string roleId,
            string skinId,
            PlayerFacingDirection facingDirection,
            bool isMoving,
            Color headColor,
            Color bodyColor,
            Color handsColor)
        {
            this.playerId = playerId?.Trim() ?? string.Empty;
            this.position = position;
            this.roleId = string.IsNullOrWhiteSpace(roleId)
                ? PlayerStateData.DefaultRoleId
                : roleId.Trim();
            this.skinId = string.IsNullOrWhiteSpace(skinId)
                ? PlayerStateData.DefaultSkinId
                : skinId.Trim();
            this.facingDirection = facingDirection;
            this.isMoving = isMoving;
            this.headColor = headColor;
            this.bodyColor = bodyColor;
            this.handsColor = handsColor;
        }

        public static PlayerReplicatedStateData FromState(PlayerStateData state) =>
            new PlayerReplicatedStateData(
                state.PlayerId,
                state.Position,
                state.RoleId,
                state.SkinId,
                state.FacingDirection,
                state.IsMoving,
                state.HeadColor,
                state.BodyColor,
                state.HandsColor);
    }
}
