using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.Player.Visual;
using UnityEngine;

namespace SocialProyectoCenigraf.World.NPC
{
    [DisallowMultipleComponent]
    public sealed class NPCLayeredSkinController : MonoBehaviour
    {
        [Header("Skin source")]
        [SerializeField] private PlayerSkinCatalog skinCatalog;
        [SerializeField] private PlayerLayerOrderProfile layerOrderProfile;
        [SerializeField] private string skinId = PlayerStateData.DefaultSkinId;
        [SerializeField] private Transform skinRoot;
        [SerializeField] private NPCLayerColorController layerColorController;

        [Header("Animation")]
        [SerializeField] private PlayerAnimationType animationType =
            PlayerAnimationType.IdleFront;
        [SerializeField] private PlayerFacingDirection facingDirection =
            PlayerFacingDirection.DownRight;
        [SerializeField] private bool playAnimation = true;
        [SerializeField, Min(1)] private int frameDurationMilliseconds = 300;
        [SerializeField, Range(1, 4)] private int framesPerAnimation = 4;
        [SerializeField] private bool startAtFrameOne = true;

        [Header("Layer renderers")]
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private SpriteRenderer leftLegRenderer;
        [SerializeField] private SpriteRenderer rightLegRenderer;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer bodyAccessoryRenderer;
        [SerializeField] private SpriteRenderer leftHandRenderer;
        [SerializeField] private SpriteRenderer rightHandRenderer;
        [SerializeField] private SpriteRenderer headRenderer;

        private PlayerSkinDefinition activeSkin;
        private int animationFrame;
        private float frameElapsedMilliseconds;

        public string SkinId => skinId;
        public PlayerAnimationType AnimationType => animationType;
        public PlayerFacingDirection FacingDirection => facingDirection;
        public bool IsAnimationPlaying => playAnimation;

        private void Awake()
        {
            FindColorController();
            ConfigureHeadMaterial();
            ResolveSkin(true);
            ResetAnimation();
        }

        private void OnEnable()
        {
            if (activeSkin == null)
            {
                ResolveSkin(true);
            }

            ApplyCurrentFrame();
        }

        private void Update()
        {
            if (!playAnimation || activeSkin == null)
            {
                return;
            }

            frameElapsedMilliseconds += Time.deltaTime * 1000f;
            int duration = Mathf.Max(1, frameDurationMilliseconds);

            if (frameElapsedMilliseconds < duration)
            {
                return;
            }

            frameElapsedMilliseconds %= duration;
            animationFrame = (animationFrame + 1) %
                Mathf.Clamp(framesPerAnimation, 1, 4);
            ApplyCurrentFrame();
        }

        public void SetSkin(string newSkinId)
        {
            skinId = string.IsNullOrWhiteSpace(newSkinId)
                ? PlayerStateData.DefaultSkinId
                : newSkinId.Trim();
            ResolveSkin(true);
            ResetAnimation();
        }

        public void SetAnimation(
            PlayerAnimationType newAnimation,
            bool shouldPlay = true)
        {
            animationType = newAnimation;
            playAnimation = shouldPlay;
            ResetAnimation();
        }

        public void SetFacingDirection(PlayerFacingDirection newDirection)
        {
            facingDirection = newDirection;
            ApplyCurrentFrame();
        }

        public void SetMovement(Vector2 movementDirection)
        {
            if (movementDirection.sqrMagnitude <= 0.0001f)
            {
                StopMovement();
                return;
            }

            facingDirection = PlayerFacingDirectionResolver.Resolve(
                facingDirection,
                movementDirection);
            animationType = IsBackFacing(facingDirection)
                ? PlayerAnimationType.WalkBack
                : PlayerAnimationType.WalkFront;
            playAnimation = true;
            ApplyCurrentFrame();
        }

        public void StopMovement()
        {
            animationType = IsBackFacing(facingDirection)
                ? PlayerAnimationType.IdleBack
                : PlayerAnimationType.IdleFront;
            playAnimation = true;
            ResetAnimation();
        }

        public void SetColors(Color head, Color body, Color hands)
        {
            FindColorController();
            layerColorController?.SetGroupedColors(head, body, hands);
        }

        private void ResolveSkin(bool logMissingSkin)
        {
            if (skinCatalog != null &&
                skinCatalog.TryGetSkin(skinId, out PlayerSkinDefinition skin))
            {
                activeSkin = skin;
                return;
            }

            activeSkin = null;
            if (logMissingSkin)
            {
                Debug.LogError(
                    $"No NPC skin with id '{skinId}' exists in the assigned " +
                    "catalog.",
                    this);
            }
        }

        private void ResetAnimation()
        {
            animationFrame = startAtFrameOne && framesPerAnimation > 1 ? 1 : 0;
            frameElapsedMilliseconds = 0f;
            ApplyCurrentFrame();
        }

        private void ApplyCurrentFrame()
        {
            if (activeSkin == null)
            {
                return;
            }

            ApplyHorizontalFacing();
            ApplyLayerOrder();
            FindColorController();
            layerColorController?.ApplyColors();

            SetSprite(shadowRenderer, PlayerSkinLayer.Shadow);
            SetSprite(leftLegRenderer, PlayerSkinLayer.LeftLeg);
            SetSprite(rightLegRenderer, PlayerSkinLayer.RightLeg);
            SetSprite(bodyRenderer, PlayerSkinLayer.Body);
            SetSprite(bodyAccessoryRenderer, PlayerSkinLayer.BodyAccessory);
            SetSprite(leftHandRenderer, PlayerSkinLayer.LeftHand);
            SetSprite(rightHandRenderer, PlayerSkinLayer.RightHand);
            SetSprite(headRenderer, PlayerSkinLayer.Head);
        }

        private void ApplyHorizontalFacing()
        {
            if (skinRoot == null)
            {
                return;
            }

            bool facesLeft =
                facingDirection == PlayerFacingDirection.DownLeft ||
                facingDirection == PlayerFacingDirection.UpLeft;
            Vector3 scale = skinRoot.localScale;
            float absoluteScaleX = Mathf.Max(Mathf.Abs(scale.x), Mathf.Epsilon);
            scale.x = facesLeft ? -absoluteScaleX : absoluteScaleX;
            skinRoot.localScale = scale;
        }

        private void ApplyLayerOrder()
        {
            if (layerOrderProfile == null)
            {
                return;
            }

            SetLayerOrder(shadowRenderer, PlayerSkinLayer.Shadow);
            SetLayerOrder(leftLegRenderer, PlayerSkinLayer.LeftLeg);
            SetLayerOrder(rightLegRenderer, PlayerSkinLayer.RightLeg);
            SetLayerOrder(bodyRenderer, PlayerSkinLayer.Body);
            SetLayerOrder(bodyAccessoryRenderer, PlayerSkinLayer.BodyAccessory);
            SetLayerOrder(leftHandRenderer, PlayerSkinLayer.LeftHand);
            SetLayerOrder(rightHandRenderer, PlayerSkinLayer.RightHand);
            SetLayerOrder(headRenderer, PlayerSkinLayer.Head);
        }

        private void SetLayerOrder(
            SpriteRenderer targetRenderer,
            PlayerSkinLayer layer)
        {
            if (targetRenderer != null)
            {
                targetRenderer.sortingOrder =
                    layerOrderProfile.GetSortingOrder(animationType, layer);
            }
        }

        private void SetSprite(
            SpriteRenderer targetRenderer,
            PlayerSkinLayer layer)
        {
            if (targetRenderer != null)
            {
                targetRenderer.sprite = activeSkin.GetSprite(
                    layer,
                    animationType,
                    animationFrame);
            }
        }

        private void ConfigureHeadMaterial()
        {
            if (headRenderer != null)
            {
                headRenderer.sharedMaterial = SelectiveHeadTintMaterial.Shared;
            }
        }

        private void FindColorController()
        {
            if (layerColorController == null)
            {
                layerColorController = GetComponent<NPCLayerColorController>();
            }
        }

        private static bool IsBackFacing(PlayerFacingDirection direction)
        {
            return direction == PlayerFacingDirection.UpRight ||
                   direction == PlayerFacingDirection.UpLeft;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            frameDurationMilliseconds = Mathf.Max(
                1,
                frameDurationMilliseconds);
            framesPerAnimation = Mathf.Clamp(framesPerAnimation, 1, 4);

            if (!Application.isPlaying)
            {
                FindColorController();
                ConfigureHeadMaterial();
                ResolveSkin(false);
                ResetAnimation();
            }
        }
#endif
    }
}
