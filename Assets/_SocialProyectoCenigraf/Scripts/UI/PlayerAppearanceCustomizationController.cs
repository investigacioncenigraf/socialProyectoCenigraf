using System;
using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.Player.Visual;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.UI
{
    public enum AppearanceColorGroup
    {
        Head,
        Body,
        Hands
    }

    [Serializable]
    public sealed class AppearancePaletteOption
    {
        public AppearanceColorGroup Group;
        public Color Color = Color.white;
        public Button Button;
        public Outline SelectionOutline;
    }

    [DisallowMultipleComponent]
    public sealed class PlayerAppearanceCustomizationController : MonoBehaviour
    {
        [SerializeField] private PlayerStateStore playerStateStore;
        [SerializeField] private PlayerAppearancePreview appearancePreview;
        [SerializeField] private VentanaEmergenteEstetico customizationWindow;
        [SerializeField] private Button saveButton;
        [SerializeField] private AppearancePaletteOption[] paletteOptions =
            Array.Empty<AppearancePaletteOption>();

        private Color draftHeadColor = Color.white;
        private Color draftBodyColor = Color.white;
        private Color draftHandsColor = Color.white;

        private void Awake()
        {
            if (customizationWindow == null)
            {
                customizationWindow =
                    GetComponentInParent<VentanaEmergenteEstetico>(true);
            }
        }

        private void Start()
        {
            ConfigureHeadPaletteMaterials();
            LoadDraftFromState();
        }

        private void ConfigureHeadPaletteMaterials()
        {
            Material headMaterial = SelectiveHeadTintMaterial.Shared;

            if (headMaterial == null)
            {
                return;
            }

            foreach (AppearancePaletteOption option in paletteOptions)
            {
                if (option?.Group != AppearanceColorGroup.Head ||
                    option.Button == null)
                {
                    continue;
                }

                Transform layerPreview =
                    option.Button.transform.Find("LayerPreview");
                Image previewImage =
                    layerPreview != null
                        ? layerPreview.GetComponent<Image>()
                        : null;

                if (previewImage != null)
                {
                    previewImage.material = headMaterial;
                }
            }
        }

        public void LoadDraftFromState()
        {
            if (playerStateStore == null)
            {
                return;
            }

            PlayerStateData state = playerStateStore.State;
            draftHeadColor = state.HeadColor;
            draftBodyColor = state.BodyColor;
            draftHandsColor = state.HandsColor;
            RefreshPreviewAndSelection();
        }

        public void SelectPaletteOption(int index)
        {
            if (index < 0 || index >= paletteOptions.Length)
            {
                return;
            }

            AppearancePaletteOption option = paletteOptions[index];

            switch (option.Group)
            {
                case AppearanceColorGroup.Head:
                    draftHeadColor = option.Color;
                    break;
                case AppearanceColorGroup.Body:
                    draftBodyColor = option.Color;
                    break;
                case AppearanceColorGroup.Hands:
                    draftHandsColor = option.Color;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            RefreshPreviewAndSelection();
        }

        public void SaveAppearance()
        {
            if (playerStateStore == null)
            {
                return;
            }

            playerStateStore.Dispatch(PlayerAction.SetAppearanceColors(
                draftHeadColor,
                draftBodyColor,
                draftHandsColor));
            appearancePreview?.ClearPreviewColorOverride();
            RefreshSelection();
            customizationWindow?.Hide();
        }

        private void RefreshPreviewAndSelection()
        {
            appearancePreview?.SetPreviewColors(
                draftHeadColor,
                draftBodyColor,
                draftHandsColor);
            RefreshSelection();
        }

        private void RefreshSelection()
        {
            foreach (AppearancePaletteOption option in paletteOptions)
            {
                if (option?.SelectionOutline == null)
                {
                    continue;
                }

                Color selectedColor = option.Group switch
                {
                    AppearanceColorGroup.Head => draftHeadColor,
                    AppearanceColorGroup.Body => draftBodyColor,
                    AppearanceColorGroup.Hands => draftHandsColor,
                    _ => Color.clear
                };
                option.SelectionOutline.enabled =
                    ColorsAreApproximatelyEqual(option.Color, selectedColor);
            }
        }

        private static bool ColorsAreApproximatelyEqual(
            Color first,
            Color second)
        {
            const float tolerance = 0.005f;
            return Mathf.Abs(first.r - second.r) <= tolerance &&
                   Mathf.Abs(first.g - second.g) <= tolerance &&
                   Mathf.Abs(first.b - second.b) <= tolerance &&
                   Mathf.Abs(first.a - second.a) <= tolerance;
        }
    }
}
