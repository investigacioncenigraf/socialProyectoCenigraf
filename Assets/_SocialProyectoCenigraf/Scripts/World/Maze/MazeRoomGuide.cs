using System;
using SocialProyectoCenigraf.Debugging.State;
using TMPro;
using UnityEngine;

namespace SocialProyectoCenigraf.World.Maze
{
    /// <summary>One look-and-feel for every room guide. Edited once on the MazeLevelGenerator.</summary>
    [Serializable]
    public sealed class MazeGuideStyle
    {
        [Header("Layout inside the room (local units)")]
        [Tooltip("Centre of the question inside the room. Above the terminal, which is 2 units tall.")]
        public Vector2 questionCenter = new Vector2(0f, 2f);
        [Tooltip("Box of the question. Long questions wrap inside it.")]
        public Vector2 questionBoxSize = new Vector2(5.6f, 2.2f);
        [Tooltip("Gap between the room edge and the first letter of the answer, so the text never " +
                 "touches the edge of the fog that covers its path.")]
        public float answerMarginFromRoom = 0.6f;
        [Tooltip("Width and height of each answer box. Long answers grow away from the room.")]
        public Vector2 answerBoxSize = new Vector2(5.5f, 1.2f);
        public float arrowDistance = 2.7f;
        public float arrowScale = 0.5f;

        [Header("Text")]
        public float questionFontSize = 2f;
        public float answerFontSize = 2f;
        [Tooltip("Outline drawn around the question and the answers so they read over any background.")]
        public Color outlineColor = Color.black;
        [Range(0f, 1f)] public float outlineWidth = 0.25f;

        [Header("Timing")]
        [Min(0f)] public float textFadeSeconds = 0.2f;
        [Min(0f)] public float arrowAppearDelaySeconds = 0.5f;
        [Min(0.05f)] public float arrowBlinkPeriodSeconds = 0.8f;
        [Range(0f, 1f)] public float arrowBlinkMinAlpha = 0.15f;

        [Header("Bubble behind the question")]
        [Tooltip("Optional pixel-art bubble. Empty = a solid rectangle.")]
        public Sprite bubbleSprite;
        public Color bubbleColor = Color.black;
        [Tooltip("Space between the text and the edge of the bubble, in units.")]
        public Vector2 bubblePadding = new Vector2(0.45f, 0.3f);

        [Header("Debug (comando @answer en el chat)")]
        [Tooltip("Colour of the right answer while the answer debug is on.")]
        public Color debugCorrectColor = new Color(0.42f, 0.73f, 0.45f);
        [Tooltip("Colour of the wrong answers while the answer debug is on.")]
        public Color debugWrongColor = new Color(0.84f, 0.42f, 0.42f);

        [Header("Sorting")]
        public string sortingLayerName = "Foreground";
        [Tooltip("Above the fog and above the player, so the question is never covered. Unity only " +
                 "keeps sorting orders between -32768 and 32767: anything beyond wraps around and " +
                 "the text ends up behind the level.")]
        [Range(-32768, 32767)] public int guideSortingOrder = 20000;
    }

    /// <summary>
    /// WayReveal at room scale: while the player is inside the room it shows the question, the
    /// answers (at most the correct one and one false one) and blinking arrows towards them.
    /// Directions without an answer show nothing: their path is closed by a plug.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/Maze/Maze Room Guide")]
    public sealed class MazeRoomGuide : MonoBehaviour
    {
        [SerializeField] private TMP_Text questionText;
        [Tooltip("Indexed by MazeDirection: Up, Right, Down, Left.")]
        [SerializeField] private TMP_Text[] answerTexts = new TMP_Text[4];
        [Tooltip("Indexed by MazeDirection: Up, Right, Down, Left.")]
        [SerializeField] private SpriteRenderer[] arrows = new SpriteRenderer[4];
        [Tooltip("Drawn behind the question so it reads over any background. Created if missing.")]
        [SerializeField] private SpriteRenderer questionBubble;

        [Tooltip("The generator arranges the question, the answers and the arrows from the style on " +
                 "the MazeLevelGenerator. Tick this to arrange them by hand in the Room prefab " +
                 "instead: then only the sorting layer is applied.")]
        [SerializeField] private bool useOwnLayout;

        private readonly bool[] directionUsed = new bool[4];
        private readonly Color[] baseAnswerColors = new Color[4];
        private bool capturedAnswerColors;
        private bool bubbleNeedsFit;
        private MazeDirection correctDirection;
        private bool hasCorrectDirection;
        private MazeGuideStyle style;
        private Transform player;
        private float roomSize = 6f;
        private float textAlpha;
        private float timeInside;
        private float arrowEnvelope;
        private bool hasQuestion;
        private bool unlocked;

        /// <summary>Used by the editor builder to wire the guide.</summary>
        public void Bind(
            TMP_Text question,
            TMP_Text[] answers,
            SpriteRenderer[] arrowRenderers,
            SpriteRenderer bubble = null)
        {
            questionText = question;
            answerTexts = answers;
            arrows = arrowRenderers;
            questionBubble = bubble;
        }

        /// <summary>The guide shows nothing until the room terminal has been used.</summary>
        public void SetUnlocked(bool value)
        {
            unlocked = value;
        }

        public void SetPlayer(Transform target)
        {
            player = target;
        }

        public void ApplyStyle(MazeGuideStyle newStyle, float newRoomSize)
        {
            style = newStyle;
            roomSize = newRoomSize;

            if (style == null)
            {
                return;
            }

            int layerId = SortingLayer.NameToID(style.sortingLayerName);

            if (questionText != null)
            {
                if (!useOwnLayout)
                {
                    questionText.rectTransform.sizeDelta = style.questionBoxSize;
                    questionText.transform.localPosition = style.questionCenter;
                    questionText.fontSize = style.questionFontSize;
                }

                StyleText(questionText, layerId);
            }

            StyleBubble(layerId);

            foreach (MazeDirection direction in MazeDirectionExtensions.All)
            {
                int index = (int)direction;
                TMP_Text answer = answerTexts != null && index < answerTexts.Length ? answerTexts[index] : null;
                SpriteRenderer arrow = arrows != null && index < arrows.Length ? arrows[index] : null;

                if (answer != null)
                {
                    if (!useOwnLayout)
                    {
                        answer.rectTransform.sizeDelta = style.answerBoxSize;
                        answer.transform.localPosition = AnswerPosition(direction);
                        answer.alignment = AnswerAlignment(direction);
                        answer.fontSize = style.answerFontSize;
                    }

                    StyleText(answer, layerId);
                }

                if (arrow != null)
                {
                    if (!useOwnLayout)
                    {
                        arrow.transform.localPosition = direction.WorldVector() * style.arrowDistance;
                        arrow.transform.localRotation = Quaternion.Euler(0f, 0f, direction.ArrowAngle());
                        arrow.transform.localScale = Vector3.one * style.arrowScale;
                    }

                    arrow.sortingLayerID = layerId;
                    arrow.sortingOrder = style.guideSortingOrder;
                }
            }
        }

        /// <summary>Creates the bubble when the Room prefab does not carry one yet.</summary>
        private void StyleBubble(int sortingLayerId)
        {
            if (questionBubble == null)
            {
                GameObject bubbleObject = new GameObject("QuestionBubble");
                bubbleObject.transform.SetParent(transform, false);
                questionBubble = bubbleObject.AddComponent<SpriteRenderer>();
            }

            questionBubble.sprite = style.bubbleSprite != null
                ? style.bubbleSprite
                : MazePlug.SharedWhiteSprite();
            questionBubble.color = style.bubbleColor;
            questionBubble.sortingLayerID = sortingLayerId;

            // Right under the question, over everything else of the maze.
            questionBubble.sortingOrder = style.guideSortingOrder - 1;
        }

        /// <summary>Wraps the bubble around whatever the question ended up measuring.</summary>
        private void FitBubbleToQuestion()
        {
            if (questionBubble == null || questionText == null || style == null)
            {
                return;
            }

            if (!hasQuestion || string.IsNullOrEmpty(questionText.text))
            {
                questionBubble.enabled = false;
                return;
            }

            // A disabled TMP_Text does not build its mesh, so it cannot be measured.
            bool wasEnabled = questionText.enabled;
            questionText.enabled = true;
            questionText.ForceMeshUpdate();

            float width = Mathf.Min(questionText.preferredWidth, style.questionBoxSize.x);
            float height = questionText.preferredHeight;
            questionText.enabled = wasEnabled;

            Vector2 size = new Vector2(width, height) + style.bubblePadding * 2f;
            Vector2 spriteSize = questionBubble.sprite != null
                ? (Vector2)questionBubble.sprite.bounds.size
                : Vector2.one;

            // The question is centred in its own box, so the bubble sits on the same point.
            questionBubble.transform.localPosition = questionText.transform.localPosition;
            questionBubble.transform.localScale = new Vector3(
                size.x / Mathf.Max(spriteSize.x, 0.0001f),
                size.y / Mathf.Max(spriteSize.y, 0.0001f),
                1f);
        }

        /// <summary>Fills the question and the (up to) two answers of this room.</summary>
        public void Configure(MazeRoomPlan plan)
        {
            for (int i = 0; i < directionUsed.Length; i++)
            {
                directionUsed[i] = false;

                if (answerTexts != null && i < answerTexts.Length && answerTexts[i] != null)
                {
                    answerTexts[i].text = string.Empty;
                }
            }

            hasQuestion = plan.HasQuestion;
            hasCorrectDirection = false;
            unlocked = false;

            if (questionText != null)
            {
                questionText.text = plan.HasQuestion ? plan.Question.Prompt : string.Empty;
            }

            if (!plan.HasQuestion)
            {
                return;
            }

            correctDirection = plan.CorrectDirection;
            hasCorrectDirection = true;
            SetAnswer(plan.CorrectDirection, plan.Question.CorrectAnswer);

            for (int i = 0; i < plan.FalseDirections.Count; i++)
            {
                string[] falses = plan.Question.FalseAnswers;
                string value = falses != null && falses.Length > 0 ? falses[i % falses.Length] : string.Empty;
                SetAnswer(plan.FalseDirections[i], value);
            }

            textAlpha = 0f;
            timeInside = 0f;
            arrowEnvelope = 0f;
            ApplyAnswerColors();
            bubbleNeedsFit = true;
            ApplyTextAlpha();
            ApplyArrowAlpha(0f);
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                DebugStore.Instance.StateChanged += HandleDebugStateChanged;
            }

            textAlpha = 0f;
            timeInside = 0f;
            arrowEnvelope = 0f;
            ApplyTextAlpha();
            ApplyArrowAlpha(0f);
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                DebugStore.Instance.StateChanged -= HandleDebugStateChanged;
            }
        }

        private void HandleDebugStateChanged(DebugStateData state, DebugAction action)
        {
            ApplyAnswerColors();
        }

        /// <summary>
        /// With the answer debug on, the right answer turns green and the wrong ones red. Off, every
        /// answer keeps the colour it has in the Room prefab.
        /// </summary>
        private void ApplyAnswerColors()
        {
            if (answerTexts == null || style == null)
            {
                return;
            }

            CaptureAnswerColors();

            bool highlight = Application.isPlaying &&
                             hasQuestion &&
                             DebugStore.Instance.State.HighlightMazeAnswers;

            for (int i = 0; i < answerTexts.Length && i < directionUsed.Length; i++)
            {
                TMP_Text answer = answerTexts[i];

                if (answer == null)
                {
                    continue;
                }

                Color color = baseAnswerColors[i];

                if (highlight)
                {
                    color = hasCorrectDirection && (int)correctDirection == i
                        ? style.debugCorrectColor
                        : style.debugWrongColor;
                }

                color.a = answer.color.a;
                answer.color = color;
            }
        }

        private void CaptureAnswerColors()
        {
            if (capturedAnswerColors || answerTexts == null)
            {
                return;
            }

            for (int i = 0; i < answerTexts.Length && i < baseAnswerColors.Length; i++)
            {
                baseAnswerColors[i] = answerTexts[i] != null ? answerTexts[i].color : Color.white;
            }

            capturedAnswerColors = true;
        }

        private void Update()
        {
            if (!Application.isPlaying || style == null || !hasQuestion)
            {
                return;
            }

            if (bubbleNeedsFit)
            {
                bubbleNeedsFit = false;
                FitBubbleToQuestion();
            }

            bool show = unlocked && player != null && IsInsideRoom(player.position);
            float target = show ? 1f : 0f;
            float fade = style.textFadeSeconds;

            textAlpha = fade <= Mathf.Epsilon
                ? target
                : Mathf.MoveTowards(textAlpha, target, Time.deltaTime / fade);
            ApplyTextAlpha();

            timeInside = show ? timeInside + Time.deltaTime : 0f;
            bool arrowsOn = show && timeInside >= style.arrowAppearDelaySeconds;
            float envelopeTarget = arrowsOn ? 1f : 0f;

            arrowEnvelope = fade <= Mathf.Epsilon
                ? envelopeTarget
                : Mathf.MoveTowards(arrowEnvelope, envelopeTarget, Time.deltaTime / fade);

            float blink = 1f;

            if (arrowsOn)
            {
                float elapsed = timeInside - style.arrowAppearDelaySeconds;
                float wave = 0.5f + 0.5f * Mathf.Cos(
                    2f * Mathf.PI * elapsed / Mathf.Max(0.05f, style.arrowBlinkPeriodSeconds));
                blink = Mathf.Lerp(style.arrowBlinkMinAlpha, 1f, wave);
            }

            ApplyArrowAlpha(arrowEnvelope * blink);
        }

        private bool IsInsideRoom(Vector3 position)
        {
            Vector3 center = transform.position;
            float half = roomSize * 0.5f;
            return Mathf.Abs(position.x - center.x) <= half &&
                   Mathf.Abs(position.y - center.y) <= half;
        }

        private void SetAnswer(MazeDirection direction, string value)
        {
            int index = (int)direction;
            directionUsed[index] = true;

            if (answerTexts != null && index < answerTexts.Length && answerTexts[index] != null)
            {
                answerTexts[index].text = value;
            }
        }

        /// <summary>
        /// Each answer is written inside its own path, over the fog that still covers it. The box
        /// starts at the room edge (plus a margin) and goes outwards, so a long answer grows away
        /// from the player instead of running over the room or off the screen.
        /// </summary>
        private Vector3 AnswerPosition(MazeDirection direction)
        {
            bool vertical = direction == MazeDirection.Up || direction == MazeDirection.Down;
            float boxLength = vertical ? style.answerBoxSize.y : style.answerBoxSize.x;
            float distance = roomSize * 0.5f + style.answerMarginFromRoom + boxLength * 0.5f;
            Vector2 offset = direction.WorldVector() * distance;
            return new Vector3(offset.x, offset.y, 0f);
        }

        /// <summary>The text hugs the side of its box that faces the room, and grows outwards.</summary>
        private static TextAlignmentOptions AnswerAlignment(MazeDirection direction)
        {
            switch (direction)
            {
                case MazeDirection.Up:
                    return TextAlignmentOptions.Bottom;
                case MazeDirection.Down:
                    return TextAlignmentOptions.Top;
                case MazeDirection.Left:
                    return TextAlignmentOptions.Right;
                default:
                    return TextAlignmentOptions.Left;
            }
        }

        private void StyleText(TMP_Text text, int sortingLayerId)
        {
            if (!useOwnLayout)
            {
                ApplyOutline(text);
            }

            Renderer textRenderer = text.GetComponent<Renderer>();

            if (textRenderer != null)
            {
                textRenderer.sortingLayerID = sortingLayerId;
                textRenderer.sortingOrder = style.guideSortingOrder;
            }
        }

        /// <summary>
        /// Outlines the text on its own material instance. Setting it through the TMP_Text property
        /// would touch the font's shared material, which is used by every other text in the game.
        /// </summary>
        private void ApplyOutline(TMP_Text text)
        {
            if (!Application.isPlaying)
            {
                return;
            }

            Material material = text.fontMaterial;

            if (material == null)
            {
                return;
            }

            material.SetFloat(ShaderUtilities.ID_OutlineWidth, style.outlineWidth);
            material.SetColor(ShaderUtilities.ID_OutlineColor, style.outlineColor);
            text.UpdateMeshPadding();
        }

        private void ApplyTextAlpha()
        {
            SetTextAlpha(questionText, textAlpha, hasQuestion);

            if (questionBubble != null)
            {
                bool bubbleVisible = hasQuestion && textAlpha > 0.001f;

                if (questionBubble.enabled != bubbleVisible)
                {
                    questionBubble.enabled = bubbleVisible;
                }

                if (bubbleVisible && style != null)
                {
                    Color color = style.bubbleColor;
                    color.a = style.bubbleColor.a * textAlpha;
                    questionBubble.color = color;
                }
            }


            if (answerTexts == null)
            {
                return;
            }

            for (int i = 0; i < answerTexts.Length && i < directionUsed.Length; i++)
            {
                SetTextAlpha(answerTexts[i], textAlpha, directionUsed[i]);
            }
        }

        private void ApplyArrowAlpha(float alpha)
        {
            if (arrows == null)
            {
                return;
            }

            for (int i = 0; i < arrows.Length && i < directionUsed.Length; i++)
            {
                SpriteRenderer arrow = arrows[i];

                if (arrow == null)
                {
                    continue;
                }

                bool visible = directionUsed[i] && alpha > 0.001f;

                if (arrow.enabled != visible)
                {
                    arrow.enabled = visible;
                }

                if (visible)
                {
                    Color color = arrow.color;
                    color.a = alpha;
                    arrow.color = color;
                }
            }
        }

        private static void SetTextAlpha(TMP_Text text, float alpha, bool used)
        {
            if (text == null)
            {
                return;
            }

            bool visible = used && alpha > 0.001f;

            if (text.enabled != visible)
            {
                text.enabled = visible;
            }

            if (visible)
            {
                text.alpha = alpha;
            }
        }
    }
}
