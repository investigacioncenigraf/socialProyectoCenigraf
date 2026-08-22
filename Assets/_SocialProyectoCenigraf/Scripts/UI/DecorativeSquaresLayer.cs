using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SocialProyectoCenigraf.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class DecorativeSquaresLayer : MonoBehaviour
    {
        [Serializable]
        private struct SquareDefinition
        {
            public Vector2 anchor;
            public Vector2 offset;
            public Vector2 size;
            public float initialRotation;
            public float rotationSpeed;
            public Color color;

            public SquareDefinition(
                Vector2 anchor,
                Vector2 offset,
                Vector2 size,
                float initialRotation,
                float rotationSpeed,
                Color color)
            {
                this.anchor = anchor;
                this.offset = offset;
                this.size = size;
                this.initialRotation = initialRotation;
                this.rotationSpeed = rotationSpeed;
                this.color = color;
            }
        }

        [Header("Animation")]
        [SerializeField, Min(0f)] private float speedMultiplier = 1.3f;
        [SerializeField, Range(0f, 1f)] private float opacityMultiplier = 0.62f;

        [Header("Squares")]
        [SerializeField] private SquareDefinition[] squares = CreateDefaultSquares();

        private readonly List<RectTransform> generatedSquares = new List<RectTransform>();
        private readonly List<float> rotationSpeeds = new List<float>();
        private GameObject generatedContainer;
        private Texture2D roundedTexture;
        private Sprite roundedSprite;

#if UNITY_EDITOR
        private bool previewRefreshScheduled;
#endif

        private void OnEnable()
        {
            BuildLayer();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            float deltaRotation = Time.unscaledDeltaTime * speedMultiplier;

            for (int i = 0; i < generatedSquares.Count; i++)
            {
                RectTransform square = generatedSquares[i];
                if (square != null)
                {
                    square.Rotate(0f, 0f, rotationSpeeds[i] * deltaRotation);
                }
            }
        }

        private void OnDisable()
        {
            ClearLayer();
        }

        private void OnDestroy()
        {
            ClearLayer();
        }

        private void BuildLayer()
        {
            ClearLayer();

            if (squares == null || squares.Length == 0)
            {
                squares = CreateDefaultSquares();
            }

            generatedContainer = new GameObject(
                Application.isPlaying
                    ? "DecorativeSquares"
                    : "__DecorativeSquaresPreview",
                typeof(RectTransform));
            if (!Application.isPlaying)
            {
                generatedContainer.hideFlags = HideFlags.DontSaveInEditor |
                                               HideFlags.DontSaveInBuild;
            }

            RectTransform container = generatedContainer.GetComponent<RectTransform>();
            container.SetParent(transform, false);
            container.anchorMin = Vector2.zero;
            container.anchorMax = Vector2.one;
            container.offsetMin = Vector2.zero;
            container.offsetMax = Vector2.zero;
            container.SetAsLastSibling();

            roundedSprite = CreateRoundedSprite();

            for (int i = 0; i < squares.Length; i++)
            {
                SquareDefinition definition = squares[i];
                GameObject squareObject = new GameObject(
                    $"Square_{i + 1:00}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                if (!Application.isPlaying)
                {
                    squareObject.hideFlags = HideFlags.DontSaveInEditor |
                                             HideFlags.DontSaveInBuild;
                }

                RectTransform square = squareObject.GetComponent<RectTransform>();
                square.SetParent(container, false);
                square.anchorMin = definition.anchor;
                square.anchorMax = definition.anchor;
                square.pivot = new Vector2(0.5f, 0.5f);
                square.anchoredPosition = definition.offset;
                square.sizeDelta = definition.size;
                square.localRotation = Quaternion.Euler(0f, 0f, definition.initialRotation);

                Image image = squareObject.GetComponent<Image>();
                Color color = definition.color;
                color.a *= opacityMultiplier;
                image.color = color;
                image.raycastTarget = false;
                image.sprite = roundedSprite;
                image.type = Image.Type.Sliced;

                generatedSquares.Add(square);
                rotationSpeeds.Add(definition.rotationSpeed);
            }

        }

        private void ClearLayer()
        {
            generatedSquares.Clear();
            rotationSpeeds.Clear();

            if (generatedContainer != null)
            {
                DestroyGeneratedObject(generatedContainer);
                generatedContainer = null;
            }

            if (roundedSprite != null)
            {
                DestroyGeneratedObject(roundedSprite);
                roundedSprite = null;
            }

            if (roundedTexture != null)
            {
                DestroyGeneratedObject(roundedTexture);
                roundedTexture = null;
            }

        }

        private static void DestroyGeneratedObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }

        private Sprite CreateRoundedSprite()
        {
            const int textureSize = 64;
            const float cornerRadius = 18f;
            const float antialiasWidth = 1.25f;

            roundedTexture = new Texture2D(
                textureSize,
                textureSize,
                TextureFormat.RGBA32,
                false)
            {
                name = "GeneratedRoundedSquareTexture",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color[] pixels = new Color[textureSize * textureSize];
            float halfSize = textureSize * 0.5f;
            Vector2 innerBounds = Vector2.one * (halfSize - cornerRadius);

            for (int y = 0; y < textureSize; y++)
            {
                for (int x = 0; x < textureSize; x++)
                {
                    Vector2 point = new Vector2(
                        Mathf.Abs(x + 0.5f - halfSize),
                        Mathf.Abs(y + 0.5f - halfSize));
                    Vector2 distanceToInnerBounds = point - innerBounds;
                    Vector2 outside = new Vector2(
                        Mathf.Max(distanceToInnerBounds.x, 0f),
                        Mathf.Max(distanceToInnerBounds.y, 0f));
                    float signedDistance = outside.magnitude +
                        Mathf.Min(
                            Mathf.Max(
                                distanceToInnerBounds.x,
                                distanceToInnerBounds.y),
                            0f) - cornerRadius;
                    float alpha = Mathf.Clamp01(
                        0.5f - signedDistance / antialiasWidth);

                    pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            roundedTexture.SetPixels(pixels);
            roundedTexture.Apply(false, true);

            return Sprite.Create(
                roundedTexture,
                new Rect(0f, 0f, textureSize, textureSize),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(
                    cornerRadius,
                    cornerRadius,
                    cornerRadius,
                    cornerRadius));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying || previewRefreshScheduled)
            {
                return;
            }

            previewRefreshScheduled = true;
            EditorApplication.delayCall += RefreshPreviewDelayed;
        }

        private void RefreshPreviewDelayed()
        {
            previewRefreshScheduled = false;
            if (this != null && isActiveAndEnabled && !Application.isPlaying)
            {
                BuildLayer();
            }
        }
#endif

        private static SquareDefinition[] CreateDefaultSquares()
        {
            return new[]
            {
                new SquareDefinition(
                    new Vector2(0.02f, 0.91f),
                    Vector2.zero,
                    new Vector2(680f, 680f),
                    -35f,
                    0.65f,
                    new Color(0.30f, 0.68f, 0.75f, 0.78f)),
                new SquareDefinition(
                    new Vector2(0.37f, 0.90f),
                    Vector2.zero,
                    new Vector2(310f, 310f),
                    -27f,
                    -0.9f,
                    new Color(0.25f, 0.36f, 0.78f, 0.72f)),
                new SquareDefinition(
                    new Vector2(0.93f, 0.68f),
                    Vector2.zero,
                    new Vector2(790f, 790f),
                    -31f,
                    0.45f,
                    new Color(0.78f, 0.39f, 0.40f, 0.68f)),
                new SquareDefinition(
                    new Vector2(0.02f, 0.05f),
                    Vector2.zero,
                    new Vector2(620f, 620f),
                    38f,
                    -0.55f,
                    new Color(0.61f, 0.39f, 0.78f, 0.68f)),
                new SquareDefinition(
                    new Vector2(0.35f, 0.02f),
                    Vector2.zero,
                    new Vector2(330f, 330f),
                    28f,
                    0.8f,
                    new Color(0.62f, 0.66f, 0.31f, 0.68f))
            };
        }
    }
}
