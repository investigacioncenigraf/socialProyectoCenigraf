using SocialProyectoCenigraf.Player.Movement;
using SocialProyectoCenigraf.Player.State;
using UnityEngine;

namespace SocialProyectoCenigraf.Player.Visual
{
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerStateStore))]
    [RequireComponent(typeof(PlayerMovementController))]
    public sealed class PlayerLayeredSkinController : MonoBehaviour
    {
        [Header("Skin source")]
        [SerializeField] private PlayerSkinCatalog skinCatalog;
        [SerializeField] private PlayerLayerOrderProfile layerOrderProfile;
        [SerializeField] private Transform skinRoot;

        [Header("Layer renderers")]
        [SerializeField] private SpriteRenderer shadowRenderer;
        [SerializeField] private SpriteRenderer leftLegRenderer;
        [SerializeField] private SpriteRenderer rightLegRenderer;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer bodyAccessoryRenderer;
        [SerializeField] private SpriteRenderer leftHandRenderer;
        [SerializeField] private SpriteRenderer rightHandRenderer;
        [SerializeField] private SpriteRenderer headRenderer;

        private PlayerStateStore store;
        private PlayerMovementController movementController;
        private PlayerSkinDefinition activeSkin;
        private int animationFrame;
        private float frameElapsedMilliseconds;
        private bool wasMoving;

        private void Awake()
        {
            store = GetComponent<PlayerStateStore>();
            movementController = GetComponent<PlayerMovementController>();
        }

        private void OnEnable()
        {
            store.StateChanged += HandleStateChanged;
        }

        private void Start()
        {
            ApplyState(store.State, true);
        }

        private void Update()
        {
            PlayerStateData state = store.State;
            bool isMoving = movementController.MoveInput.sqrMagnitude > 0.0001f;

            if (isMoving != wasMoving)
            {
                wasMoving = isMoving;
                animationFrame = GetStartingFrame(state);
                frameElapsedMilliseconds = 0f;
                ApplySprites(state, isMoving);
            }

            frameElapsedMilliseconds += Time.deltaTime * 1000f;
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
            ApplySprites(state);
        }

        private void OnDisable()
        {
            if (store != null)
            {
                store.StateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(
            PlayerStateData state,
            PlayerAction action)
        {
            bool skinChanged = activeSkin == null ||
                !string.Equals(
                    activeSkin.SkinId,
                    state.SkinId,
                    System.StringComparison.OrdinalIgnoreCase);
            ApplyState(state, skinChanged);
        }

        private void ApplyState(PlayerStateData state, bool resolveSkin)
        {
            if (resolveSkin)
            {
                ResolveActiveSkin(state.SkinId);
                animationFrame = GetStartingFrame(state);
                frameElapsedMilliseconds = 0f;
            }

            ApplySprites(state, movementController.MoveInput.sqrMagnitude > 0.0001f);
        }

        private static int GetStartingFrame(PlayerStateData state)
        {
            return state.FramesPerAnimation > 1 ? 1 : 0;
        }

        private void ResolveActiveSkin(string skinId)
        {
            if (skinCatalog != null &&
                skinCatalog.TryGetSkin(skinId, out PlayerSkinDefinition skin))
            {
                activeSkin = skin;
                return;
            }

            activeSkin = null;
            Debug.LogError(
                $"No player skin with id '{skinId}' exists in the assigned catalog.",
                this);
        }

        private void ApplySprites(PlayerStateData state)
        {
            ApplySprites(
                state,
                movementController.MoveInput.sqrMagnitude > 0.0001f);
        }

        private void ApplySprites(PlayerStateData state, bool isMoving)
        {
            if (activeSkin == null)
            {
                return;
            }

            bool isBackFacing =
                state.FacingDirection == PlayerFacingDirection.UpRight ||
                state.FacingDirection == PlayerFacingDirection.UpLeft;
            bool isFacingLeft =
                state.FacingDirection == PlayerFacingDirection.DownLeft ||
                state.FacingDirection == PlayerFacingDirection.UpLeft;

            PlayerAnimationType animationType = (isBackFacing, isMoving) switch
            {
                (false, false) => PlayerAnimationType.IdleFront,
                (false, true) => PlayerAnimationType.WalkFront,
                (true, false) => PlayerAnimationType.IdleBack,
                (true, true) => PlayerAnimationType.WalkBack
            };

            ApplyHorizontalFacing(isFacingLeft);
            ApplyLayerOrder(animationType);

            SetSprite(shadowRenderer, PlayerSkinLayer.Shadow, animationType);
            SetSprite(leftLegRenderer, PlayerSkinLayer.LeftLeg, animationType);
            SetSprite(rightLegRenderer, PlayerSkinLayer.RightLeg, animationType);
            SetSprite(bodyRenderer, PlayerSkinLayer.Body, animationType);
            SetSprite(
                bodyAccessoryRenderer,
                PlayerSkinLayer.BodyAccessory,
                animationType);
            SetSprite(leftHandRenderer, PlayerSkinLayer.LeftHand, animationType);
            SetSprite(rightHandRenderer, PlayerSkinLayer.RightHand, animationType);
            SetSprite(headRenderer, PlayerSkinLayer.Head, animationType);
        }

        private void ApplyHorizontalFacing(bool isFacingLeft)
        {
            if (skinRoot == null)
            {
                return;
            }

            Vector3 scale = skinRoot.localScale;
            float absoluteScaleX = Mathf.Max(Mathf.Abs(scale.x), Mathf.Epsilon);
            scale.x = isFacingLeft ? -absoluteScaleX : absoluteScaleX;
            skinRoot.localScale = scale;
        }

        private void ApplyLayerOrder(PlayerAnimationType animationType)
        {
            if (layerOrderProfile == null)
            {
                return;
            }

            SetLayerOrder(
                shadowRenderer,
                PlayerSkinLayer.Shadow,
                animationType);
            SetLayerOrder(
                leftLegRenderer,
                PlayerSkinLayer.LeftLeg,
                animationType);
            SetLayerOrder(
                rightLegRenderer,
                PlayerSkinLayer.RightLeg,
                animationType);
            SetLayerOrder(bodyRenderer, PlayerSkinLayer.Body, animationType);
            SetLayerOrder(
                bodyAccessoryRenderer,
                PlayerSkinLayer.BodyAccessory,
                animationType);
            SetLayerOrder(
                leftHandRenderer,
                PlayerSkinLayer.LeftHand,
                animationType);
            SetLayerOrder(
                rightHandRenderer,
                PlayerSkinLayer.RightHand,
                animationType);
            SetLayerOrder(headRenderer, PlayerSkinLayer.Head, animationType);
        }

        private void SetLayerOrder(
            SpriteRenderer targetRenderer,
            PlayerSkinLayer layer,
            PlayerAnimationType animationType)
        {
            if (targetRenderer != null)
            {
                targetRenderer.sortingOrder =
                    layerOrderProfile.GetSortingOrder(animationType, layer);
            }
        }

        private void SetSprite(
            SpriteRenderer targetRenderer,
            PlayerSkinLayer layer,
            PlayerAnimationType animationType)
        {
            if (targetRenderer != null)
            {
                targetRenderer.sprite = activeSkin.GetSprite(
                    layer,
                    animationType,
                    animationFrame);
            }
        }
    }
}
