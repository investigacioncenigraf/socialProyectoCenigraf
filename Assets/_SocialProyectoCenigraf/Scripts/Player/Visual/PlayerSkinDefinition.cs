using System;
using SocialProyectoCenigraf.Player.State;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.Visual
{
    [CreateAssetMenu(
        fileName = "PlayerSkinDefinition",
        menuName = "Cenigraf/Player/Skin Definition")]
    public sealed class PlayerSkinDefinition : ScriptableObject
    {
        private const int FramesPerRow = 4;

        [Header("Identity")]
        [SerializeField] private string skinId = PlayerStateData.DefaultSkinId;

        [Header("Layer frames")]
        [Tooltip(
            "Sprite order: IdleFront (0-3), WalkFront (4-7), " +
            "IdleBack (8-11), WalkBack (12-15).")]
        [SerializeField] private Sprite[] shadowFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] leftLegFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] rightLegFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] bodyFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] bodyAccessoryFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] leftHandFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] rightHandFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] headFrames = Array.Empty<Sprite>();

        public string SkinId => skinId;

        public Sprite GetSprite(
            PlayerSkinLayer layer,
            PlayerAnimationType animationType,
            int animationFrame)
        {
            Sprite[] frames = GetFrames(layer);
            int frame = Mathf.Clamp(animationFrame, 0, FramesPerRow - 1);
            int spriteIndex = (int)animationType * FramesPerRow + frame;

            return spriteIndex >= 0 && spriteIndex < frames.Length
                ? frames[spriteIndex]
                : null;
        }

        private Sprite[] GetFrames(PlayerSkinLayer layer)
        {
            return layer switch
            {
                PlayerSkinLayer.Shadow => shadowFrames,
                PlayerSkinLayer.LeftLeg => leftLegFrames,
                PlayerSkinLayer.RightLeg => rightLegFrames,
                PlayerSkinLayer.Body => bodyFrames,
                PlayerSkinLayer.BodyAccessory => bodyAccessoryFrames,
                PlayerSkinLayer.LeftHand => leftHandFrames,
                PlayerSkinLayer.RightHand => rightHandFrames,
                PlayerSkinLayer.Head => headFrames,
                _ => Array.Empty<Sprite>()
            };
        }

    }
}
