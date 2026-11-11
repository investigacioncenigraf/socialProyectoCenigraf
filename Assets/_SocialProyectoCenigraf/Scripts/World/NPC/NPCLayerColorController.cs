using SocialProyectoCenigraf.Player.Visual;
using UnityEngine;

namespace SocialProyectoCenigraf.World.NPC
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class NPCLayerColorController : MonoBehaviour
    {
        private static readonly int FaceColorProperty =
            Shader.PropertyToID("_FaceColor");
        private static readonly int OutlineColorProperty =
            Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineSourceColorProperty =
            Shader.PropertyToID("_OutlineSourceColor");
        private static readonly int OutlineColorToleranceProperty =
            Shader.PropertyToID("_OutlineColorTolerance");

        [Header("Complete character tint")]
        [Tooltip(
            "Changing this value replaces every individual layer color. " +
            "Afterwards, any layer can be customized independently.")]
        [SerializeField] private Color characterColor = Color.white;
        [SerializeField, HideInInspector]
        private Color lastAppliedCharacterColor = Color.white;

        [Header("Individual layer colors")]
        [SerializeField] private Color shadowColor = Color.white;
        [SerializeField] private Color leftLegColor = Color.white;
        [SerializeField] private Color rightLegColor = Color.white;
        [SerializeField] private Color bodyColor = Color.white;
        [SerializeField] private Color bodyAccessoryColor = Color.white;
        [SerializeField] private Color leftHandColor = Color.white;
        [SerializeField] private Color rightHandColor = Color.white;
        [SerializeField] private Color headColor = Color.white;
        [SerializeField] private Color faceColor = Color.white;

        [Header("Layer outline")]
        [Tooltip("Replacement color for every #3C3C3C pixel in every NPC skin layer.")]
        [SerializeField] private Color outlineColor = new Color(64f / 255f, 55f / 255f, 1f, 1f);
        [Tooltip("Source color in the sprite that should be replaced by the outline color.")]
        [SerializeField] private Color outlineSourceColor = new Color(60f / 255f, 60f / 255f, 60f / 255f, 1f);
        [Tooltip("Small color difference allowed when matching the source pixels.")]
        [SerializeField, Range(0f, 0.1f)] private float outlineColorTolerance = 0.015f;

        [Header("Layer renderers")]
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private SpriteRenderer leftLegRenderer;
        [SerializeField] private SpriteRenderer rightLegRenderer;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer bodyAccessoryRenderer;
        [SerializeField] private SpriteRenderer leftHandRenderer;
        [SerializeField] private SpriteRenderer rightHandRenderer;
        [SerializeField] private SpriteRenderer headRenderer;

        public Color CharacterColor => characterColor;
        public Color FaceColor => faceColor;

        private MaterialPropertyBlock headPropertyBlock;
        private MaterialPropertyBlock layerPropertyBlock;
        private float visualOpacity = 1f;

        private void Awake()
        {
            ApplyColors();
        }

        private void OnEnable()
        {
            ApplyColors();
        }

        public void SetCharacterColor(Color color)
        {
            characterColor = color;
            ApplyCharacterColorToEveryLayer();
            ApplyColors();
        }

        public void SetLayerColor(PlayerSkinLayer layer, Color color)
        {
            switch (layer)
            {
                case PlayerSkinLayer.Shadow:
                    shadowColor = color;
                    break;
                case PlayerSkinLayer.LeftLeg:
                    leftLegColor = color;
                    break;
                case PlayerSkinLayer.RightLeg:
                    rightLegColor = color;
                    break;
                case PlayerSkinLayer.Body:
                    bodyColor = color;
                    break;
                case PlayerSkinLayer.BodyAccessory:
                    bodyAccessoryColor = color;
                    break;
                case PlayerSkinLayer.LeftHand:
                    leftHandColor = color;
                    break;
                case PlayerSkinLayer.RightHand:
                    rightHandColor = color;
                    break;
                case PlayerSkinLayer.Head:
                    headColor = color;
                    break;
            }

            ApplyColors();
        }

        public void SetFaceColor(Color color)
        {
            faceColor = color;
            ApplyColors();
        }

        public void SetOutlineColor(Color color)
        {
            outlineColor = color;
            ApplyColors();
        }

        public Color GetLayerColor(PlayerSkinLayer layer)
        {
            return layer switch
            {
                PlayerSkinLayer.Shadow => shadowColor,
                PlayerSkinLayer.LeftLeg => leftLegColor,
                PlayerSkinLayer.RightLeg => rightLegColor,
                PlayerSkinLayer.Body => bodyColor,
                PlayerSkinLayer.BodyAccessory => bodyAccessoryColor,
                PlayerSkinLayer.LeftHand => leftHandColor,
                PlayerSkinLayer.RightHand => rightHandColor,
                PlayerSkinLayer.Head => headColor,
                _ => Color.white
            };
        }

        public void SetGroupedColors(Color head, Color body, Color hands)
        {
            headColor = head;
            faceColor = head;
            bodyColor = body;
            bodyAccessoryColor = body;
            leftLegColor = body;
            rightLegColor = body;
            leftHandColor = hands;
            rightHandColor = hands;
            ApplyColors();
        }

        public void ResetColors()
        {
            characterColor = Color.white;
            ApplyCharacterColorToEveryLayer();
            ApplyColors();
        }

        public void ApplyColors()
        {
            SetRendererColor(shadowRenderer, shadowColor);
            SetRendererColor(leftLegRenderer, leftLegColor);
            SetRendererColor(rightLegRenderer, rightLegColor);
            SetRendererColor(bodyRenderer, bodyColor);
            SetRendererColor(bodyAccessoryRenderer, bodyAccessoryColor);
            SetRendererColor(leftHandRenderer, leftHandColor);
            SetRendererColor(rightHandRenderer, rightHandColor);
            SetRendererColor(headRenderer, headColor);
            SetFaceRendererColor();
            ApplyOutlineColor(shadowRenderer);
            ApplyOutlineColor(leftLegRenderer);
            ApplyOutlineColor(rightLegRenderer);
            ApplyOutlineColor(bodyRenderer);
            ApplyOutlineColor(bodyAccessoryRenderer);
            ApplyOutlineColor(leftHandRenderer);
            ApplyOutlineColor(rightHandRenderer);
            ApplyOutlineColor(headRenderer);
        }

        /// <summary>
        /// Multiplies the alpha of every colored layer, including the face
        /// and outline shader masks. RGB values remain untouched.
        /// </summary>
        public void SetVisualOpacity(float opacity)
        {
            visualOpacity = Mathf.Clamp01(opacity);
            ApplyColors();
        }

        private void SetRendererColor(
            SpriteRenderer targetRenderer,
            Color layerColor)
        {
            if (targetRenderer != null)
            {
                layerColor.a *= visualOpacity;
                targetRenderer.color = layerColor;
            }
        }

        private void SetFaceRendererColor()
        {
            if (headRenderer == null)
            {
                return;
            }

            headPropertyBlock ??= new MaterialPropertyBlock();
            headRenderer.GetPropertyBlock(headPropertyBlock);
            headPropertyBlock.SetColor(
                FaceColorProperty,
                WithVisualOpacity(faceColor));
            headRenderer.SetPropertyBlock(headPropertyBlock);
        }

        private void ApplyOutlineColor(SpriteRenderer targetRenderer)
        {
            if (targetRenderer == null) return;

            layerPropertyBlock ??= new MaterialPropertyBlock();
            targetRenderer.GetPropertyBlock(layerPropertyBlock);
            layerPropertyBlock.SetColor(OutlineColorProperty, WithVisualOpacity(outlineColor));
            layerPropertyBlock.SetColor(OutlineSourceColorProperty, outlineSourceColor.linear);
            layerPropertyBlock.SetFloat(OutlineColorToleranceProperty, outlineColorTolerance);
            targetRenderer.SetPropertyBlock(layerPropertyBlock);
        }

        private Color WithVisualOpacity(Color color)
        {
            color.a *= visualOpacity;
            return color;
        }

        private void ApplyCharacterColorToEveryLayer()
        {
            shadowColor = characterColor;
            leftLegColor = characterColor;
            rightLegColor = characterColor;
            bodyColor = characterColor;
            bodyAccessoryColor = characterColor;
            leftHandColor = characterColor;
            rightHandColor = characterColor;
            headColor = characterColor;
            faceColor = characterColor;
            lastAppliedCharacterColor = characterColor;
        }

        private static bool ColorsMatch(Color first, Color second)
        {
            return Mathf.Approximately(first.r, second.r) &&
                   Mathf.Approximately(first.g, second.g) &&
                   Mathf.Approximately(first.b, second.b) &&
                   Mathf.Approximately(first.a, second.a);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!ColorsMatch(characterColor, lastAppliedCharacterColor))
            {
                ApplyCharacterColorToEveryLayer();
            }

            ApplyColors();
        }
#endif
    }
}
