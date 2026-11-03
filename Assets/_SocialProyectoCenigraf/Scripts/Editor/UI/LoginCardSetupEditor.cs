using TMPro;
using SocialProyectoCenigraf.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.Editor.UI
{
    public static class LoginCardSetupEditor
    {
        private const string MenuPath =
            "Tools/Cenigraf/UI/Construir Login Card";
        private const string LoginScenePath =
            "Assets/_SocialProyectoCenigraf/Scenes/Salas/Login.unity";

        private static readonly Color BorderColor =
            new Color32(21, 157, 219, 255);
        private static readonly Color SurfaceColor =
            new Color32(237, 252, 255, 173);
        private static readonly Color TextColor =
            new Color32(24, 36, 45, 255);
        private static readonly Color MutedTextColor =
            new Color32(75, 92, 102, 255);
        private static readonly Color FieldColor = Color.white;
        private static readonly Color LoginColor =
            new Color32(159, 207, 105, 255);

        [MenuItem(MenuPath)]
        public static void BuildLoginCard()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (Application.isBatchMode &&
                (!scene.IsValid() || scene.name != "Login"))
            {
                scene = EditorSceneManager.OpenScene(
                    LoginScenePath,
                    OpenSceneMode.Single);
            }

            if (!scene.IsValid() || scene.name != "Login")
            {
                EditorUtility.DisplayDialog(
                    "Login Card",
                    "Abre la escena Login antes de ejecutar esta herramienta.",
                    "Entendido");
                return;
            }

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                Debug.LogError("No existe una fuente predeterminada de TextMesh Pro.");
                return;
            }

            Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");

            Canvas canvas = Object.FindFirstObjectByType<Canvas>(
                FindObjectsInactive.Include);
            if (canvas == null)
            {
                canvas = CreateCanvas();
            }
            ConfigureCanvas(canvas);
            EnsureEventSystem();

            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            RectTransform safeArea = FindOrCreateRect(canvasRect, "SafeArea");
            Stretch(safeArea, 48f, 48f, 30f, 30f);

            Transform[] existingCards = canvas.GetComponentsInChildren<Transform>(
                true);
            foreach (Transform existingCard in existingCards)
            {
                if (existingCard != null && existingCard.name == "LoginCard")
                {
                    Object.DestroyImmediate(existingCard.gameObject);
                }
            }

            Transform legacyUi = canvasRect.Find("UI");
            if (legacyUi != null)
            {
                Object.DestroyImmediate(legacyUi.gameObject);
            }

            RectTransform card = CreateRect("LoginCard", safeArea);
            card.anchorMin = new Vector2(1f, 0.5f);
            card.anchorMax = new Vector2(1f, 0.5f);
            card.pivot = new Vector2(1f, 0.5f);
            card.anchoredPosition = new Vector2(-22f, 0f);
            card.sizeDelta = new Vector2(700f, 940f);

            RectTransform visual = CreateImage(
                "Visual", card, BorderColor, roundedSprite, true);
            Stretch(visual);
            visual.GetComponent<Image>().raycastTarget = false;

            Material blurMaterial = GetOrCreateBlurMaterial();
            RectTransform blurBackground = CreateImage(
                "BlurBackground", visual, Color.white, roundedSprite, true);
            Stretch(blurBackground, 3f, 3f, 3f, 3f);
            Image blurMaskImage = blurBackground.GetComponent<Image>();
            blurMaskImage.raycastTarget = false;
            Mask blurMask = blurBackground.gameObject.AddComponent<Mask>();
            blurMask.showMaskGraphic = false;

            RawImage sourceSky = FindRawImage(canvas, "RAW_Sky");
            RawImage sourceLandscape = FindRawImage(
                canvas, "RAW_LandScape");

            UIContinuousCarousel skyCarousel = ConfigureCarousel(
                sourceSky,
                "RAW_Sky_Continuation",
                14f,
                UIContinuousCarousel.ResponsiveLayout.CoverParent);
            UIContinuousCarousel landscapeCarousel = ConfigureCarousel(
                sourceLandscape,
                "RAW_LandScape_Continuation",
                42f,
                UIContinuousCarousel.ResponsiveLayout.BottomCoverParent,
                0.42f);

            RawImage blurredSky = CreateBlurLayer(
                "RAW_BlurredSky", blurBackground, blurMaterial);
            RawImage blurredSkyContinuation = CreateBlurLayer(
                "RAW_BlurredSky_Continuation", blurBackground, blurMaterial);
            RawImage blurredLandscape = CreateBlurLayer(
                "RAW_BlurredLandscape", blurBackground, blurMaterial);
            RawImage blurredLandscapeContinuation = CreateBlurLayer(
                "RAW_BlurredLandscape_Continuation",
                blurBackground,
                blurMaterial);

            LoginBackgroundBlur blurController =
                blurBackground.gameObject.AddComponent<LoginBackgroundBlur>();
            blurController.Configure(
                sourceSky,
                skyCarousel != null
                    ? skyCarousel.ContinuationImage
                    : null,
                sourceLandscape,
                landscapeCarousel != null
                    ? landscapeCarousel.ContinuationImage
                    : null,
                blurredSky,
                blurredSkyContinuation,
                blurredLandscape,
                blurredLandscapeContinuation);

            RectTransform surface = CreateImage(
                "Surface", visual, SurfaceColor, roundedSprite, true);
            Stretch(surface, 3f, 3f, 3f, 3f);
            surface.GetComponent<Image>().raycastTarget = false;

            RectTransform content = CreateRect("Content", card);
            Stretch(content, 68f, 68f, 62f, 68f);

            TMP_Text title = CreateText(
                "TXT_Title", content, font, "NIWIMU", 82f,
                FontStyles.Normal, TextAlignmentOptions.Center, TextColor);
            SetRect(title.rectTransform, new Vector2(0f, 326f),
                new Vector2(560f, 130f));
            title.enableAutoSizing = true;
            title.fontSizeMin = 54f;
            title.fontSizeMax = 82f;

            TMP_Text subtitle = CreateText(
                "TXT_Subtitle", content, font,
                "Inicia sesi\u00f3n para continuar", 24f,
                FontStyles.Normal, TextAlignmentOptions.Center,
                MutedTextColor);
            SetRect(subtitle.rectTransform, new Vector2(0f, 250f),
                new Vector2(560f, 42f));

            TMP_InputField usernameInput = CreateInputGroup(
                content,
                font,
                roundedSprite,
                "Username",
                "Usuario",
                "Ingresa tu usuario",
                new Vector2(0f, 105f),
                false,
                false);

            TMP_InputField passwordInput = CreateInputGroup(
                content,
                font,
                roundedSprite,
                "Password",
                "Contrase\u00f1a",
                "Ingresa tu contrase\u00f1a",
                new Vector2(0f, -55f),
                true,
                true);

            TMP_Text error = CreateText(
                "TXT_Error", content, font, string.Empty, 20f,
                FontStyles.Normal, TextAlignmentOptions.Center,
                new Color32(180, 55, 55, 255));
            SetRect(error.rectTransform, new Vector2(0f, -157f),
                new Vector2(560f, 44f));
            error.gameObject.SetActive(false);

            RectTransform loginButton = CreateButton(
                "BTN_Login", content, roundedSprite, LoginColor,
                font, "Iniciar sesi\u00f3n", 30f);
            SetRect(loginButton, new Vector2(0f, -246f),
                new Vector2(300f, 82f));
            Button loginButtonComponent = loginButton.GetComponent<Button>();

            TMP_Text status = CreateText(
                "TXT_Status", content, font, string.Empty, 20f,
                FontStyles.Normal, TextAlignmentOptions.Center,
                MutedTextColor);
            SetRect(status.rectTransform, new Vector2(0f, -310f),
                new Vector2(560f, 40f));
            status.gameObject.SetActive(false);

            LoginController loginController =
                card.gameObject.AddComponent<LoginController>();
            loginController.Configure(
                usernameInput,
                passwordInput,
                loginButtonComponent,
                error,
                status,
                "Sala_Lobby");

            Transform oldOverlay = canvasRect.Find("LoadingOverlay");
            if (oldOverlay != null)
            {
                Object.DestroyImmediate(oldOverlay.gameObject);
            }

            RectTransform loadingOverlay = CreateImage(
                "LoadingOverlay", canvasRect,
                new Color(0f, 0f, 0f, 0.42f), null, false);
            Stretch(loadingOverlay);
            TMP_Text loadingText = CreateText(
                "TXT_Loading", loadingOverlay, font, "Conectando...", 34f,
                FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
            SetRect(loadingText.rectTransform, Vector2.zero,
                new Vector2(420f, 80f));
            loadingOverlay.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = card.gameObject;
            Debug.Log("Login Card creado y escena Login guardada.");
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new GameObject(
                "UI_Login",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Login Canvas");
            return canvasObject.GetComponent<Canvas>();
        }

        private static Material GetOrCreateBlurMaterial()
        {
            const string materialFolder =
                "Assets/_SocialProyectoCenigraf/Asset/Materials/UI";
            const string materialPath =
                materialFolder + "/MAT_UIBackgroundBlur.mat";

            EnsureAssetFolder(
                "Assets/_SocialProyectoCenigraf/Asset/Materials",
                "UI");

            Shader shader = Shader.Find("Cenigraf/UI/BackgroundBlur");
            if (shader == null)
            {
                Debug.LogError("No se encontr\u00f3 el shader de BlurBackground.");
                return null;
            }

            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                materialPath);
            if (material == null)
            {
                material = new Material(shader)
                {
                    name = "MAT_UIBackgroundBlur"
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else if (material.shader != shader)
            {
                material.shader = shader;
            }

            material.SetFloat("_BlurRadius", 10f);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static void EnsureAssetFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(parent))
            {
                string grandParent = parent.Substring(
                    0,
                    parent.LastIndexOf('/'));
                string parentName = parent.Substring(
                    parent.LastIndexOf('/') + 1);
                if (!AssetDatabase.IsValidFolder(parent))
                {
                    AssetDatabase.CreateFolder(grandParent, parentName);
                }
            }

            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static RawImage FindRawImage(Canvas canvas, string name)
        {
            RawImage[] images = canvas.GetComponentsInChildren<RawImage>(true);
            foreach (RawImage image in images)
            {
                if (image.name == name)
                {
                    return image;
                }
            }

            Debug.LogWarning($"No se encontr\u00f3 la capa de fondo '{name}'.");
            return null;
        }

        private static RawImage CreateBlurLayer(
            string name,
            Transform parent,
            Material material)
        {
            RectTransform rect = CreateRect(name, parent);
            RawImage image = rect.gameObject.AddComponent<RawImage>();
            image.material = material;
            image.raycastTarget = false;
            return image;
        }

        private static UIContinuousCarousel ConfigureCarousel(
            RawImage primary,
            string continuationName,
            float speed,
            UIContinuousCarousel.ResponsiveLayout responsiveLayout,
            float minimumHeightRatio = 0.42f)
        {
            if (primary == null)
            {
                Debug.LogWarning(
                    $"No se pudo configurar {continuationName}: " +
                    "no se encontró la imagen principal.");
                return null;
            }

            RectTransform parent = primary.rectTransform.parent
                as RectTransform;
            if (parent == null)
            {
                return null;
            }

            Stretch(parent);

            Transform existing = parent.Find(continuationName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            RectTransform continuationRect = CreateRect(
                continuationName,
                parent);
            CopyRectTransform(primary.rectTransform, continuationRect);

            RawImage continuation =
                continuationRect.gameObject.AddComponent<RawImage>();
            continuation.texture = primary.texture;
            continuation.material = primary.material;
            continuation.color = primary.color;
            continuation.uvRect = primary.uvRect;
            continuation.raycastTarget = false;

            UIContinuousCarousel carousel =
                parent.GetComponent<UIContinuousCarousel>();
            if (carousel == null)
            {
                carousel = parent.gameObject.AddComponent<UIContinuousCarousel>();
            }

            carousel.Configure(
                primary,
                continuation,
                speed,
                responsiveLayout,
                minimumHeightRatio);
            EditorUtility.SetDirty(carousel);
            return carousel;
        }

        private static void CopyRectTransform(
            RectTransform source,
            RectTransform target)
        {
            target.anchorMin = source.anchorMin;
            target.anchorMax = source.anchorMax;
            target.pivot = source.pivot;
            target.anchoredPosition = source.anchoredPosition;
            target.sizeDelta = source.sizeDelta;
            target.localRotation = source.localRotation;
            target.localScale = source.localScale;
        }

        private static void ConfigureCanvas(Canvas canvas)
        {
            canvas.name = "UI_Login";
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>(
                    FindObjectsInactive.Include) != null)
            {
                return;
            }

            GameObject eventSystem = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        }

        private static TMP_InputField CreateInputGroup(
            RectTransform parent,
            TMP_FontAsset font,
            Sprite roundedSprite,
            string id,
            string labelText,
            string placeholderText,
            Vector2 position,
            bool password,
            bool addToggle)
        {
            RectTransform group = CreateRect($"GRP_{id}", parent);
            SetRect(group, position, new Vector2(600f, 132f));

            TMP_Text label = CreateText(
                "TXT_Label", group, font, labelText, 22f,
                FontStyles.Normal, TextAlignmentOptions.BottomRight,
                TextColor);
            SetRect(label.rectTransform, new Vector2(-18f, 50f),
                new Vector2(520f, 34f));

            RectTransform inputRect = CreateImage(
                $"FLD_{id}", group, FieldColor, roundedSprite, true);
            SetRect(inputRect, new Vector2(-16f, -8f),
                new Vector2(548f, 76f));
            Image inputImage = inputRect.GetComponent<Image>();

            TMP_InputField input = inputRect.gameObject.AddComponent<TMP_InputField>();
            input.targetGraphic = inputImage;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = password
                ? TMP_InputField.ContentType.Password
                : TMP_InputField.ContentType.Standard;
            input.characterLimit = 80;

            RectTransform textArea = CreateRect("Text Area", inputRect);
            Stretch(textArea, 24f, 24f, 10f, 10f);
            textArea.gameObject.AddComponent<RectMask2D>();

            TMP_Text placeholder = CreateText(
                "Placeholder", textArea, font, placeholderText, 23f,
                FontStyles.Italic, TextAlignmentOptions.MidlineLeft,
                new Color32(126, 139, 146, 180));
            Stretch(placeholder.rectTransform);

            TMP_Text inputText = CreateText(
                "Text", textArea, font, string.Empty, 24f,
                FontStyles.Normal, TextAlignmentOptions.MidlineLeft,
                TextColor);
            Stretch(inputText.rectTransform);

            input.textViewport = textArea;
            input.textComponent = inputText;
            input.placeholder = placeholder;

            if (addToggle)
            {
                RectTransform toggle = CreateButton(
                    "BTN_TogglePassword", group, roundedSprite,
                    SurfaceColor, font, "Ver", 17f);
                toggle.anchorMin = new Vector2(1f, 0.5f);
                toggle.anchorMax = new Vector2(1f, 0.5f);
                toggle.pivot = new Vector2(0.5f, 0.5f);
                toggle.anchoredPosition = new Vector2(-2f, -8f);
                toggle.sizeDelta = new Vector2(52f, 52f);
            }

            return input;
        }

        private static RectTransform CreateButton(
            string name,
            RectTransform parent,
            Sprite roundedSprite,
            Color color,
            TMP_FontAsset font,
            string text,
            float fontSize)
        {
            RectTransform rect = CreateImage(
                name, parent, color, roundedSprite, true);
            Image image = rect.GetComponent<Image>();
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.disabledColor = new Color(0.65f, 0.65f, 0.65f, 0.55f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            TMP_Text label = CreateText(
                "TXT_Title", rect, font, text, fontSize,
                FontStyles.Normal, TextAlignmentOptions.Center, TextColor);
            Stretch(label.rectTransform, 12f, 12f, 8f, 8f);
            return rect;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string text,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font;
            label.text = text;
            label.fontSize = fontSize;
            label.fontStyle = style;
            label.alignment = alignment;
            label.color = color;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            return label;
        }

        private static RectTransform FindOrCreateRect(
            Transform parent,
            string name)
        {
            Transform existing = parent.Find(name);
            return existing != null
                ? existing.GetComponent<RectTransform>()
                : CreateRect(name, parent);
        }

        private static RectTransform CreateImage(
            string name,
            Transform parent,
            Color color,
            Sprite sprite,
            bool sliced)
        {
            RectTransform rect = CreateRect(name, parent);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = sliced && sprite != null
                ? Image.Type.Sliced
                : Image.Type.Simple;
            return rect;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(gameObject, $"Create {name}");
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(
            RectTransform rect,
            float left = 0f,
            float right = 0f,
            float top = 0f,
            float bottom = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }
    }
}
