using SocialProyectoCenigraf.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.Editor.UI
{
    public static class GameplayHUDSetupEditor
    {
        private const string MenuPath =
            "Tools/Cenigraf/UI/Crear y aplicar Gameplay HUD";
        private const string PrefabFolder =
            "Assets/_SocialProyectoCenigraf/Prefabs/UI/HUD";
        private const string PrefabPath =
            PrefabFolder + "/UI_GameplayHUD.prefab";

        private static readonly string[] TargetScenes =
        {
            "Assets/_SocialProyectoCenigraf/Scenes/Salas/Plaza_Central.unity",
            "Assets/_SocialProyectoCenigraf/Scenes/Salas/Sala_ABC.unity",
            "Assets/_SocialProyectoCenigraf/Scenes/Salas/Sala_Lobby.unity"
        };

        private static readonly Color PanelColor =
            new Color32(22, 34, 48, 214);
        private static readonly Color PanelSoftColor =
            new Color32(30, 45, 61, 220);
        private static readonly Color FieldColor =
            new Color32(12, 23, 35, 210);
        private static readonly Color BorderColor =
            new Color32(207, 225, 237, 88);
        private static readonly Color AccentColor =
            new Color32(65, 210, 225, 255);
        private static readonly Color TextColor =
            new Color32(244, 249, 255, 255);
        private static readonly Color MutedTextColor =
            new Color32(180, 197, 211, 255);

        [MenuItem(MenuPath)]
        public static void BuildAndApply()
        {
            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
            {
                Debug.LogError(
                    "No existe una fuente predeterminada de TextMesh Pro.");
                return;
            }

            EnsureFolder(PrefabFolder);
            GameObject prefabSource = BuildPrefabSource(font);
            GameObject prefabAsset = PrefabUtility.SaveAsPrefabAsset(
                prefabSource,
                PrefabPath);
            Object.DestroyImmediate(prefabSource);

            if (prefabAsset == null)
            {
                Debug.LogError("No se pudo crear el prefab UI_GameplayHUD.");
                return;
            }

            string previousScenePath = SceneManager.GetActiveScene().path;
            foreach (string scenePath in TargetScenes)
            {
                ApplyPrefabToScene(prefabAsset, scenePath);
            }

            if (!Application.isBatchMode &&
                !string.IsNullOrWhiteSpace(previousScenePath))
            {
                EditorSceneManager.OpenScene(
                    previousScenePath,
                    OpenSceneMode.Single);
            }

            AssetDatabase.SaveAssets();
            Debug.Log(
                "Gameplay HUD creado y aplicado a Plaza_Central, " +
                "Sala_ABC y Sala_Lobby.");
        }

        private static GameObject BuildPrefabSource(TMP_FontAsset font)
        {
            GameObject root = new GameObject(
                "UI_GameplayHUD",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(GameplayHUDView),
                typeof(GameplayHUDSessionBinder),
                typeof(GameplayHUDChatController));

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform safeArea = CreateRect("SafeArea", root.transform);
            Stretch(safeArea);
            safeArea.gameObject.AddComponent<UISafeAreaFitter>();

            RectTransform hudRoot = CreateRect("HUD_Root", safeArea);
            Stretch(hudRoot, 28f, 28f, 24f, 24f);

            PlayerSummaryElements player = CreatePlayerSummary(hudRoot, font);
            Button menuButton = CreateCircularButton(
                "BTN_Menu",
                hudRoot,
                font,
                "MENU",
                new Vector2(80f, 80f),
                13f);
            AnchorTopRight(menuButton.GetComponent<RectTransform>(), Vector2.zero);

            Button chatDismissButton = CreateChatDismissButton(hudRoot);
            ChatElements chat = CreateChat(hudRoot, font);

            GameplayHUDResponsiveLayout responsiveLayout =
                hudRoot.gameObject.AddComponent<GameplayHUDResponsiveLayout>();
            responsiveLayout.Configure(
                player.root,
                menuButton.GetComponent<RectTransform>(),
                chat.root);

            GameplayHUDView view = root.GetComponent<GameplayHUDView>();
            view.Configure(
                player.avatar,
                player.name,
                player.level,
                player.experienceFill,
                player.role,
                player.infoButton,
                menuButton,
                chat.panel,
                chat.messages,
                chat.messagesContent,
                chat.input,
                chat.sendButton,
                chat.toggleButton,
                chat.closeButton,
                chatDismissButton,
                chat.notification);

            int uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
            {
                SetLayerRecursively(root, uiLayer);
            }

            return root;
        }

        private static PlayerSummaryElements CreatePlayerSummary(
            RectTransform parent,
            TMP_FontAsset font)
        {
            RectTransform panel = CreateRounded(
                "SECTION_PlayerSummary",
                parent,
                PanelColor,
                56f);
            AnchorTopLeft(panel, Vector2.zero, new Vector2(600f, 112f));
            CreateBorder(panel, 1.5f, 56f);

            RectTransform avatarRect = CreateRounded(
                "IMG_PlayerAvatar",
                panel,
                PanelSoftColor,
                43f);
            avatarRect.anchorMin = new Vector2(0f, 0.5f);
            avatarRect.anchorMax = new Vector2(0f, 0.5f);
            avatarRect.pivot = new Vector2(0.5f, 0.5f);
            avatarRect.anchoredPosition = new Vector2(58f, 0f);
            avatarRect.sizeDelta = new Vector2(86f, 86f);

            TMP_Text avatarPlaceholder = CreateText(
                "TXT_AvatarPlaceholder",
                avatarRect,
                font,
                "IMG",
                14f,
                TextAlignmentOptions.Center,
                MutedTextColor);
            Stretch(avatarPlaceholder.rectTransform);

            TMP_Text name = CreateText(
                "TXT_PlayerName",
                panel,
                font,
                "Nombre del jugador",
                18f,
                TextAlignmentOptions.MidlineLeft,
                TextColor);
            AnchorTopLeft(
                name.rectTransform,
                new Vector2(116f, -12f),
                new Vector2(282f, 32f));

            RectTransform roleTag = CreateRounded(
                "TAG_PlayerRole",
                panel,
                PanelSoftColor,
                20f);
            AnchorTopLeft(
                roleTag,
                new Vector2(408f, -12f),
                new Vector2(135f, 32f));

            TMP_Text role = CreateText(
                "TXT_PlayerRole",
                roleTag,
                font,
                "visitante",
                12f,
                TextAlignmentOptions.Center,
                TextColor);
            Stretch(role.rectTransform, 10f, 10f, 2f, 2f);

            RectTransform experienceTrack = CreateRounded(
                "BAR_PlayerExperience",
                panel,
                FieldColor,
                8f);
            AnchorTopLeft(
                experienceTrack,
                new Vector2(196f, -67f),
                new Vector2(347f, 16f));

            RectTransform experienceFill = CreateRounded(
                "FILL_PlayerExperience",
                experienceTrack,
                AccentColor,
                8f);
            experienceFill.anchorMin = Vector2.zero;
            experienceFill.anchorMax = new Vector2(0.01f, 1f);
            experienceFill.offsetMin = Vector2.zero;
            experienceFill.offsetMax = Vector2.zero;
            CreateBorder(
                experienceTrack,
                1.5f,
                8f,
                BorderColor);

            TMP_Text level = CreateText(
                "TXT_PlayerLevel",
                panel,
                font,
                "Lvl. 1",
                13f,
                TextAlignmentOptions.MidlineLeft,
                MutedTextColor);
            AnchorTopLeft(
                level.rectTransform,
                new Vector2(116f, -61f),
                new Vector2(72f, 28f));

            Button infoButton = CreateCircularButton(
                "BTN_PlayerInfo",
                panel,
                font,
                "i",
                new Vector2(42f, 42f),
                16f);
            RectTransform infoRect = infoButton.GetComponent<RectTransform>();
            infoRect.anchorMin = Vector2.one;
            infoRect.anchorMax = Vector2.one;
            infoRect.pivot = Vector2.one;
            infoRect.anchoredPosition = new Vector2(-13f, -13f);
            infoRect.gameObject.SetActive(false);

            return new PlayerSummaryElements
            {
                root = panel,
                avatar = avatarRect.GetComponent<UIRoundedRectangle>(),
                name = name,
                level = level,
                experienceFill = experienceFill,
                role = role,
                infoButton = infoButton
            };
        }

        private static ChatElements CreateChat(
            RectTransform parent,
            TMP_FontAsset font)
        {
            RectTransform group = CreateRect("GRP_Chat", parent);
            group.anchorMin = new Vector2(1f, 0f);
            group.anchorMax = new Vector2(1f, 0f);
            group.pivot = new Vector2(1f, 0f);
            group.anchoredPosition = Vector2.zero;
            group.sizeDelta = new Vector2(470f, 444f);

            RectTransform panel = CreateRounded(
                "PANEL_Chat",
                group,
                PanelColor,
                20f);
            panel.gameObject.AddComponent<CanvasGroup>();
            panel.anchorMin = new Vector2(1f, 0f);
            panel.anchorMax = new Vector2(1f, 0f);
            panel.pivot = new Vector2(1f, 0f);
            panel.anchoredPosition = new Vector2(0f, 94f);
            panel.sizeDelta = new Vector2(470f, 330f);
            CreateBorder(panel, 1.5f, 20f);

            TMP_Text title = CreateText(
                "TXT_ChatTitle",
                panel,
                font,
                "CHAT",
                15f,
                TextAlignmentOptions.MidlineLeft,
                TextColor);
            AnchorTopLeft(
                title.rectTransform,
                new Vector2(18f, -10f),
                new Vector2(260f, 30f));

            Button closeButton = CreateCircularButton(
                "BTN_CloseChat",
                panel,
                font,
                "X",
                new Vector2(34f, 34f),
                12f);
            RectTransform closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = Vector2.one;
            closeRect.anchorMax = Vector2.one;
            closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-12f, -8f);

            RectTransform viewport = CreateRounded(
                "VIEW_ChatMessages",
                panel,
                FieldColor,
                12f);
            viewport.GetComponent<UIRoundedRectangle>().raycastTarget = true;
            Stretch(viewport, 14f, 14f, 48f, 82f);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;

            VerticalLayoutGroup layout =
                content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter =
                content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = panel.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            CreateMessageTemplate(content, font);

            TMP_InputField input = CreateChatInput(panel, font);
            Button sendButton = CreateCircularButton(
                "BTN_SendChat",
                panel,
                font,
                ">",
                new Vector2(52f, 52f),
                21f);
            RectTransform sendRect = sendButton.GetComponent<RectTransform>();
            sendRect.anchorMin = new Vector2(1f, 0f);
            sendRect.anchorMax = new Vector2(1f, 0f);
            sendRect.pivot = new Vector2(1f, 0f);
            sendRect.anchoredPosition = new Vector2(-14f, 14f);

            Button toggleButton = CreateCircularButton(
                "BTN_ChatToggle",
                group,
                font,
                "CHAT",
                new Vector2(80f, 80f),
                14f);
            RectTransform toggleRect = toggleButton.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(1f, 0f);
            toggleRect.anchorMax = new Vector2(1f, 0f);
            toggleRect.pivot = new Vector2(1f, 0f);
            toggleRect.anchoredPosition = Vector2.zero;

            RectTransform notification = CreateRounded(
                "POP_ChatNotification",
                group,
                PanelSoftColor,
                18f);
            notification.anchorMin = new Vector2(1f, 0f);
            notification.anchorMax = new Vector2(1f, 0f);
            notification.pivot = new Vector2(1f, 0f);
            notification.anchoredPosition = new Vector2(0f, 94f);
            notification.sizeDelta = new Vector2(400f, 58f);
            CreateBorder(notification, 1.5f, 18f);
            TMP_Text notificationText = CreateText(
                "TXT_ChatNotification",
                notification,
                font,
                "Nuevo mensaje",
                13f,
                TextAlignmentOptions.MidlineLeft,
                TextColor);
            Stretch(notificationText.rectTransform, 20f, 20f, 10f, 10f);
            notification.gameObject.SetActive(false);

            return new ChatElements
            {
                root = group,
                panel = panel.gameObject,
                messages = scrollRect,
                messagesContent = content,
                input = input,
                sendButton = sendButton,
                toggleButton = toggleButton,
                closeButton = closeButton,
                notification = notification.gameObject
            };
        }

        private static Button CreateChatDismissButton(RectTransform parent)
        {
            RectTransform rect = CreateRect("BTN_ChatDismissArea", parent);
            Stretch(rect);

            Image image = rect.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            rect.gameObject.SetActive(false);
            return button;
        }

        private static void CreateMessageTemplate(
            RectTransform parent,
            TMP_FontAsset font)
        {
            RectTransform item = CreateRounded(
                "ITEM_ChatMessage_Template",
                parent,
                PanelSoftColor,
                16f);
            item.gameObject.AddComponent<LayoutElement>().preferredHeight = 62f;

            TMP_Text sender = CreateText(
                "TXT_Sender",
                item,
                font,
                "Usuario",
                12f,
                TextAlignmentOptions.TopLeft,
                AccentColor);
            AnchorTopLeft(
                sender.rectTransform,
                new Vector2(12f, -7f),
                new Vector2(220f, 20f));

            TMP_Text time = CreateText(
                "TXT_Time",
                item,
                font,
                "00:00",
                11f,
                TextAlignmentOptions.TopRight,
                MutedTextColor);
            time.rectTransform.anchorMin = new Vector2(1f, 1f);
            time.rectTransform.anchorMax = new Vector2(1f, 1f);
            time.rectTransform.pivot = new Vector2(1f, 1f);
            time.rectTransform.anchoredPosition = new Vector2(-12f, -7f);
            time.rectTransform.sizeDelta = new Vector2(80f, 20f);

            TMP_Text message = CreateText(
                "TXT_Message",
                item,
                font,
                "Mensaje",
                12f,
                TextAlignmentOptions.TopLeft,
                TextColor);
            Stretch(message.rectTransform, 12f, 12f, 28f, 7f);
            item.gameObject.SetActive(false);
        }

        private static TMP_InputField CreateChatInput(
            RectTransform parent,
            TMP_FontAsset font)
        {
            RectTransform field = CreateRounded(
                "FLD_ChatMessage",
                parent,
                FieldColor,
                18f);
            field.anchorMin = Vector2.zero;
            field.anchorMax = Vector2.zero;
            field.pivot = Vector2.zero;
            field.anchoredPosition = new Vector2(14f, 14f);
            field.sizeDelta = new Vector2(376f, 52f);

            TMP_InputField input = field.gameObject.AddComponent<TMP_InputField>();
            UIRoundedRectangle fieldGraphic =
                field.GetComponent<UIRoundedRectangle>();
            fieldGraphic.raycastTarget = true;
            input.targetGraphic = fieldGraphic;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.contentType = TMP_InputField.ContentType.Standard;
            input.characterLimit = 240;

            RectTransform textArea = CreateRect("Text Area", field);
            Stretch(textArea, 18f, 18f, 8f, 8f);
            textArea.gameObject.AddComponent<RectMask2D>();

            TMP_Text placeholder = CreateText(
                "Placeholder",
                textArea,
                font,
                "Escribe un mensaje...",
                13f,
                TextAlignmentOptions.MidlineLeft,
                MutedTextColor);
            Stretch(placeholder.rectTransform);

            TMP_Text text = CreateText(
                "Text",
                textArea,
                font,
                string.Empty,
                13f,
                TextAlignmentOptions.MidlineLeft,
                TextColor);
            Stretch(text.rectTransform);

            input.textViewport = textArea;
            input.textComponent = text;
            input.placeholder = placeholder;
            return input;
        }

        private static Button CreateCircularButton(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string label,
            Vector2 size,
            float fontSize)
        {
            RectTransform rect = CreateRounded(
                name,
                parent,
                PanelSoftColor,
                Mathf.Min(size.x, size.y) * 0.5f);
            rect.sizeDelta = size;
            CreateBorder(rect, 1.5f, Mathf.Min(size.x, size.y) * 0.5f);

            Button button = rect.gameObject.AddComponent<Button>();
            UIRoundedRectangle buttonGraphic =
                rect.GetComponent<UIRoundedRectangle>();
            buttonGraphic.raycastTarget = true;
            button.targetGraphic = buttonGraphic;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            button.colors = colors;

            TMP_Text text = CreateText(
                "TXT_Label",
                rect,
                font,
                label,
                fontSize,
                TextAlignmentOptions.Center,
                TextColor);
            Stretch(text.rectTransform, 8f, 8f, 8f, 8f);
            return button;
        }

        private static RectTransform CreateRounded(
            string name,
            Transform parent,
            Color color,
            float radius)
        {
            RectTransform rect = CreateRect(name, parent);
            rect.gameObject.AddComponent<CanvasRenderer>();
            UIRoundedRectangle graphic =
                rect.gameObject.AddComponent<UIRoundedRectangle>();
            graphic.color = color;
            graphic.raycastTarget = false;
            SetSerializedFloat(graphic, "cornerRadius", radius);
            return rect;
        }

        private static void CreateBorder(
            RectTransform parent,
            float thickness,
            float radius,
            Color? overrideColor = null)
        {
            RectTransform borderRect = CreateRect("Border", parent);
            Stretch(borderRect);
            borderRect.gameObject.AddComponent<CanvasRenderer>();
            UIRoundedInnerBorder border =
                borderRect.gameObject.AddComponent<UIRoundedInnerBorder>();
            border.color = overrideColor ?? BorderColor;
            border.Thickness = thickness;
            SetSerializedFloat(border, "cornerRadius", radius);
            border.raycastTarget = false;
        }

        private static TMP_Text CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            float fontSize,
            TextAlignmentOptions alignment,
            Color color)
        {
            RectTransform rect = CreateRect(name, parent);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static void ApplyPrefabToScene(
            GameObject prefabAsset,
            string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(
                scenePath,
                OpenSceneMode.Single);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == "UI_GameplayHUD")
                {
                    Object.DestroyImmediate(root);
                }
            }

            GameObject instance = PrefabUtility.InstantiatePrefab(
                prefabAsset,
                scene) as GameObject;
            if (instance == null)
            {
                Debug.LogError($"No se pudo agregar el HUD a {scenePath}.");
                return;
            }

            instance.name = "UI_GameplayHUD";
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int index = 1; index < parts.Length; index++)
            {
                string next = current + "/" + parts[index];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[index]);
                }

                current = next;
            }
        }

        private static void AnchorTopLeft(
            RectTransform rect,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void AnchorTopRight(
            RectTransform rect,
            Vector2 position)
        {
            rect.anchorMin = Vector2.one;
            rect.anchorMax = Vector2.one;
            rect.pivot = Vector2.one;
            rect.anchoredPosition = position;
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

        private static void SetSerializedFloat(
            Object target,
            string propertyName,
            float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property =
                serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private struct PlayerSummaryElements
        {
            public RectTransform root;
            public Graphic avatar;
            public TMP_Text name;
            public TMP_Text level;
            public RectTransform experienceFill;
            public TMP_Text role;
            public Button infoButton;
        }

        private struct ChatElements
        {
            public RectTransform root;
            public GameObject panel;
            public ScrollRect messages;
            public RectTransform messagesContent;
            public TMP_InputField input;
            public Button sendButton;
            public Button toggleButton;
            public Button closeButton;
            public GameObject notification;
        }
    }
}
