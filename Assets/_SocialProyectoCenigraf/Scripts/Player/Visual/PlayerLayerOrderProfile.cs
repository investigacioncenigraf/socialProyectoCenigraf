using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.Visual
{
    [CreateAssetMenu(
        fileName = "PlayerLayerOrderProfile",
        menuName = "Cenigraf/Player/Layer Order Profile")]
    public sealed class PlayerLayerOrderProfile : ScriptableObject
    {
        private const int SortingOrderStep = 10;

        // Same stacking for every animation. Bottom (0) to top:
        // Shadow, LeftHand, LeftLeg, RightLeg, Body, RightHand, Head.
        // BodyAccessory (not in the spec) sits right above Body.

        [Header("Top to bottom - Idle Front")]
        [SerializeField] private PlayerSkinLayer[] idleFrontTopToBottom =
        {
            PlayerSkinLayer.Head,
            PlayerSkinLayer.RightHand,
            PlayerSkinLayer.BodyAccessory,
            PlayerSkinLayer.Body,
            PlayerSkinLayer.RightLeg,
            PlayerSkinLayer.LeftLeg,
            PlayerSkinLayer.LeftHand,
            PlayerSkinLayer.Shadow
        };

        [Header("Top to bottom - Walk Front")]
        [SerializeField] private PlayerSkinLayer[] walkFrontTopToBottom =
        {
            PlayerSkinLayer.Head,
            PlayerSkinLayer.RightHand,
            PlayerSkinLayer.BodyAccessory,
            PlayerSkinLayer.Body,
            PlayerSkinLayer.RightLeg,
            PlayerSkinLayer.LeftLeg,
            PlayerSkinLayer.LeftHand,
            PlayerSkinLayer.Shadow
        };

        [Header("Top to bottom - Idle Back")]
        [SerializeField] private PlayerSkinLayer[] idleBackTopToBottom =
        {
            PlayerSkinLayer.Head,
            PlayerSkinLayer.RightHand,
            PlayerSkinLayer.BodyAccessory,
            PlayerSkinLayer.Body,
            PlayerSkinLayer.RightLeg,
            PlayerSkinLayer.LeftLeg,
            PlayerSkinLayer.LeftHand,
            PlayerSkinLayer.Shadow
        };

        [Header("Top to bottom - Walk Back")]
        [SerializeField] private PlayerSkinLayer[] walkBackTopToBottom =
        {
            PlayerSkinLayer.Head,
            PlayerSkinLayer.RightHand,
            PlayerSkinLayer.BodyAccessory,
            PlayerSkinLayer.Body,
            PlayerSkinLayer.RightLeg,
            PlayerSkinLayer.LeftLeg,
            PlayerSkinLayer.LeftHand,
            PlayerSkinLayer.Shadow
        };

        public int GetSortingOrder(
            PlayerAnimationType animationType,
            PlayerSkinLayer layer)
        {
            PlayerSkinLayer[] order = GetTopToBottom(animationType);
            int index = Array.IndexOf(order, layer);

            return index >= 0
                ? (order.Length - index) * SortingOrderStep
                : 0;
        }

        private PlayerSkinLayer[] GetTopToBottom(
            PlayerAnimationType animationType)
        {
            return animationType switch
            {
                PlayerAnimationType.IdleFront => idleFrontTopToBottom,
                PlayerAnimationType.WalkFront => walkFrontTopToBottom,
                PlayerAnimationType.IdleBack => idleBackTopToBottom,
                PlayerAnimationType.WalkBack => walkBackTopToBottom,
                _ => idleFrontTopToBottom
            };
        }
    }
}
