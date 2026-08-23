using System;
using System.Collections.Generic;
using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.Player.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    [DisallowMultipleComponent]
    public sealed class PlayerAppearancePreview : MonoBehaviour
    {
        [Header("State and skin data")]
        [SerializeField] private PlayerStateStore playerStateStore;
        [SerializeField] private PlayerSkinCatalog skinCatalog;
        [SerializeField] private PlayerLayerOrderProfile layerOrderProfile;

        [Header("Preview layers")]
        [SerializeField] private Image shadowImage;
        [SerializeField] private Image leftLegImage;
        [SerializeField] private Image rightLegImage;
        [SerializeField] private Image bodyImage;
        [SerializeField] private Image bodyAccessoryImage;
        [SerializeField] private Image leftHandImage;
        [SerializeField] private Image rightHandImage;
        [SerializeField] private Image headImage;

        private PlayerSkinDefinition activeSkin;
        private int animationFrame = 1;
        private float frameElapsedMilliseconds;
        private Color headColor = Color.white;
        private Color bodyColor = Color.white;
        private Color handsColor = Color.white;
        private bool previewColorOverrideActive;

        public void SetPreviewColors(
            Color newHeadColor,
            Color newBodyColor,
            Color newHandsColor)
        {
            previewColorOverrideActive = true;
            headColor = newHeadColor;
            bodyColor = newBodyColor;
            handsColor = newHandsColor;
            ApplyLayerColors();
        }

        public void ClearPreviewColorOverride()
        {
            previewColorOverrideActive = false;

            if (playerStateStore != null)
            {
                ApplyState(playerStateStore.State, false);
            }
        }

        private void OnEnable()
        {
            if (headImage != null)
            {
                headImage.material = SelectiveHeadTintMaterial.Shared;
            }

            if (playerStateStore != null)
            {
                playerStateStore.StateChanged += HandlePlayerStateChanged;
                ApplyState(playerStateStore.State, true);
            }
        }

        private void Update()
        {
            if (playerStateStore == null || activeSkin == null)
            {
                return;
            }

            PlayerStateData state = playerStateStore.State;
            frameElapsedMilliseconds += Time.unscaledDeltaTime * 1000f;
            int duration = Mathf.Max(
                1,
                state.AnimationFrameDurationMilliseconds);

            if (frameElapsedMilliseconds < duration)
            {
                return;
            }

            frameElapsedMilliseconds %= duration;
            int frameCount = Mathf.Clamp(state.FramesPerAnimation, 1, 4);
            animationFrame = (animationFrame + 1) % frameCount;
            ApplySprites();
        }

        private void OnDisable()
        {
            if (playerStateStore != null)
            {
                playerStateStore.StateChanged -= HandlePlayerStateChanged;
            }
        }

        private void HandlePlayerStateChanged(
            PlayerStateData state,
            PlayerAction action)
        {
            bool skinChanged = activeSkin == null ||
                !string.Equals(
                    activeSkin.SkinId,
                    state.SkinId,
                    StringComparison.OrdinalIgnoreCase);
            ApplyState(state, skinChanged);
        }

        private void ApplyState(PlayerStateData state, bool resolveSkin)
        {
            if (!previewColorOverrideActive)
            {
                headColor = state.HeadColor;
                bodyColor = state.BodyColor;
                handsColor = state.HandsColor;
            }

            if (resolveSkin)
            {
                ResolveSkin(state.SkinId);
                animationFrame = state.FramesPerAnimation > 1 ? 1 : 0;
                frameElapsedMilliseconds = 0f;
            }

            ApplyLayerOrder();
            ApplySprites();
            ApplyLayerColors();
        }

        private void ResolveSkin(string skinId)
        {
            if (skinCatalog != null &&
                skinCatalog.TryGetSkin(skinId, out PlayerSkinDefinition skin))
            {
                activeSkin = skin;
                return;
            }

            activeSkin = null;
            Debug.LogError(
                $"The appearance preview could not find skin '{skinId}'.",
                this);
        }

        private void ApplySprites()
        {
            if (activeSkin == null)
            {
                return;
            }

            SetSprite(shadowImage, PlayerSkinLayer.Shadow);
            SetSprite(leftLegImage, PlayerSkinLayer.LeftLeg);
            SetSprite(rightLegImage, PlayerSkinLayer.RightLeg);
            SetSprite(bodyImage, PlayerSkinLayer.Body);
            SetSprite(bodyAccessoryImage, PlayerSkinLayer.BodyAccessory);
            SetSprite(leftHandImage, PlayerSkinLayer.LeftHand);
            SetSprite(rightHandImage, PlayerSkinLayer.RightHand);
            SetSprite(headImage, PlayerSkinLayer.Head);
        }

        private void SetSprite(Image target, PlayerSkinLayer layer)
        {
            if (target != null)
            {
                target.sprite = activeSkin.GetSprite(
                    layer,
                    PlayerAnimationType.IdleFront,
                    animationFrame);
                target.enabled = target.sprite != null;
            }
        }

        private void ApplyLayerColors()
        {
            SetColor(headImage, headColor);
            SetColor(bodyImage, bodyColor);
            SetColor(bodyAccessoryImage, bodyColor);
            SetColor(leftLegImage, bodyColor);
            SetColor(rightLegImage, bodyColor);
            SetColor(leftHandImage, handsColor);
            SetColor(rightHandImage, handsColor);
            SetColor(shadowImage, Color.white);
        }

        private static void SetColor(Image image, Color color)
        {
            if (image != null)
            {
                image.color = color;
            }
        }

        private void ApplyLayerOrder()
        {
            if (layerOrderProfile == null)
            {
                return;
            }

            List<LayerImage> layers = new()
            {
                new LayerImage(PlayerSkinLayer.Shadow, shadowImage),
                new LayerImage(PlayerSkinLayer.LeftLeg, leftLegImage),
                new LayerImage(PlayerSkinLayer.RightLeg, rightLegImage),
                new LayerImage(PlayerSkinLayer.Body, bodyImage),
                new LayerImage(
                    PlayerSkinLayer.BodyAccessory,
                    bodyAccessoryImage),
                new LayerImage(PlayerSkinLayer.LeftHand, leftHandImage),
                new LayerImage(PlayerSkinLayer.RightHand, rightHandImage),
                new LayerImage(PlayerSkinLayer.Head, headImage)
            };

            layers.Sort((first, second) =>
                layerOrderProfile.GetSortingOrder(
                    PlayerAnimationType.IdleFront,
                    first.Layer).CompareTo(
                    layerOrderProfile.GetSortingOrder(
                        PlayerAnimationType.IdleFront,
                        second.Layer)));

            for (int index = 0; index < layers.Count; index++)
            {
                if (layers[index].Image != null)
                {
                    layers[index].Image.transform.SetSiblingIndex(index);
                }
            }
        }

        private readonly struct LayerImage
        {
            public LayerImage(PlayerSkinLayer layer, Image image)
            {
                Layer = layer;
                Image = image;
            }

            public PlayerSkinLayer Layer { get; }
            public Image Image { get; }
        }
    }
}
