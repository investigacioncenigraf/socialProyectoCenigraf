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
        }

        private void SetRendererColor(
            SpriteRenderer targetRenderer,
            Color layerColor)
        {
            if (targetRenderer != null)
            {
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
                faceColor);
            headRenderer.SetPropertyBlock(headPropertyBlock);
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
