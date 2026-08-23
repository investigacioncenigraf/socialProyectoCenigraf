#if UNITY_EDITOR
using System.Collections.Generic;
using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.Player.Visual;
using SocialProyectoCenigraf.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.Editor.UI
{
    public static class VentanaEmergenteEsteticoSetupEditor
    {
        private static readonly Color OverlayColor =
            new(62f / 255f, 62f / 255f, 62f / 255f, 0.6f);
        private static readonly Color PanelColor =
            new(204f / 255f, 204f / 255f, 204f / 255f, 1f);
        private static readonly Color AppearanceBackgroundColor =
            new(217f / 255f, 217f / 255f, 217f / 255f, 1f);
        private static readonly Color TitleColor =
            new(62f / 255f, 62f / 255f, 62f / 255f, 1f);
        private static readonly Color SelectionColor =
            new(0f, 151f / 255f, 1f, 1f);
        private static readonly Color[] PaletteColors =
        {
            Color.white,
            new(0.95f, 0.48f, 0.50f, 1f),
            new(0.94f, 0.69f, 0.46f, 1f),
            new(0.69f, 0.90f, 0.39f, 1f),
            new(0.43f, 0.86f, 0.49f, 1f),
            new(0.40f, 0.72f, 0.94f, 1f),
            new(0.38f, 0.52f, 0.88f, 1f),
            new(0.62f, 0.43f, 0.89f, 1f),
            new(0.90f, 0.39f, 0.70f, 1f),
            new(0.34f, 0.34f, 0.34f, 1f)
        };

        private const string SkinCatalogPath =
            "Assets/_SocialProyectoCenigraf/Data/Player/Skins/" +
            "PlayerSkinCatalog.asset";
        private const string LayerOrderProfilePath =
            "Assets/_SocialProyectoCenigraf/Data/Player/Skins/" +
            "Default_PlayerLayerOrderProfile.asset";
        private const string FontAssetPath =
            "Assets/TextMesh Pro/Resources/Fonts & Materials/" +
            "LiberationSans SDF.asset";

        [MenuItem(
            "Tools/Cenigraf/UI/Configure Ventana Emergente Estetico %#e")]
        public static void Configure()
        {
            ConfigureEventSystem();

            GameObject canvasObject = FindOrCreateRoot("UI_Customization");
            RectTransform canvasRect = GetOrAddComponent<RectTransform>(
                canvasObject);
            Stretch(canvasRect);

            Canvas canvas = GetOrAddComponent<Canvas>(canvasObject);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;

            CanvasScaler scaler = GetOrAddComponent<CanvasScaler>(canvasObject);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GetOrAddComponent<GraphicRaycaster>(canvasObject);

            RectTransform window = FindOrCreateRectChild(
                canvasRect,
                "VentanaEmergenteEstetico");
            Stretch(window);

            CanvasGroup windowGroup =
                GetOrAddComponent<CanvasGroup>(window.gameObject);
            windowGroup.alpha = 0f;
            windowGroup.interactable = false;
            windowGroup.blocksRaycasts = false;

            VentanaEmergenteEstetico controller =
                GetOrAddComponent<VentanaEmergenteEstetico>(window.gameObject);
            SerializedObject serializedController = new(controller);
            serializedController.FindProperty("windowGroup").objectReferenceValue =
                windowGroup;
            serializedController.FindProperty("visibleAtStart").boolValue = false;
            serializedController.ApplyModifiedPropertiesWithoutUndo();

            RectTransform overlay = FindOrCreateRectChild(
                window,
                "BackgroundOverlay");
            Stretch(overlay);
            Image overlayImage = GetOrAddComponent<Image>(overlay.gameObject);
            overlayImage.color = OverlayColor;
            overlayImage.raycastTarget = true;

            RectTransform panel = FindOrCreateRectChild(
                window,
                "CentralPanel");
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            panel.sizeDelta = new Vector2(1500f, 750f);
            panel.localScale = Vector3.one;

            Image panelImage = GetOrAddComponent<Image>(panel.gameObject);
            panelImage.color = PanelColor;
            panelImage.raycastTarget = true;

            ConfigureAppearancePreview(panel);

            overlay.SetAsFirstSibling();
            panel.SetAsLastSibling();

            EditorUtility.SetDirty(canvasObject);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Selection.activeGameObject = window.gameObject;

            Debug.Log(
                "VentanaEmergenteEstetico configured. Press M in SceneDemo " +
                "to show or hide it.");
        }

        private static void ConfigureAppearancePreview(RectTransform panel)
        {
            RectTransform windowTitleRect = FindOrCreateRectChild(
                panel,
                "TXT_CustomizationTitle");
            windowTitleRect.anchorMin = new Vector2(0f, 1f);
            windowTitleRect.anchorMax = new Vector2(1f, 1f);
            windowTitleRect.pivot = new Vector2(0.5f, 1f);
            windowTitleRect.anchoredPosition = new Vector2(0f, -20f);
            windowTitleRect.sizeDelta = new Vector2(0f, 72f);
            windowTitleRect.localScale = Vector3.one;

            TextMeshProUGUI windowTitle =
                GetOrAddComponent<TextMeshProUGUI>(
                    windowTitleRect.gameObject);
            windowTitle.text = "Demo Cambio de Aspecto";
            windowTitle.fontSize = 52f;
            windowTitle.fontStyle = FontStyles.Bold;
            windowTitle.alignment = TextAlignmentOptions.Center;
            windowTitle.color = TitleColor;
            windowTitle.raycastTarget = false;

            TMP_FontAsset font =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            if (font != null)
            {
                windowTitle.font = font;
            }

            RectTransform appearanceSection = FindOrCreateRectChild(
                panel,
                "AppearanceSection");
            appearanceSection.anchorMin = new Vector2(1f, 0.5f);
            appearanceSection.anchorMax = new Vector2(1f, 0.5f);
            appearanceSection.pivot = new Vector2(1f, 0.5f);
            appearanceSection.anchoredPosition = new Vector2(-55f, -45f);
            appearanceSection.sizeDelta = new Vector2(500f, 620f);
            appearanceSection.localScale = Vector3.one;

            RectTransform titleRect = FindOrCreateRectChild(
                appearanceSection,
                "TXT_AppearanceTitle");
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0f, 62f);
            titleRect.localScale = Vector3.one;

            TextMeshProUGUI title =
                GetOrAddComponent<TextMeshProUGUI>(titleRect.gameObject);
            title.text = "Aspecto";
            title.fontSize = 48f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.color = TitleColor;
            title.raycastTarget = false;

            if (font != null)
            {
                title.font = font;
            }

            RectTransform previewRect = FindOrCreateRectChild(
                appearanceSection,
                "PlayerAppearancePreview");
            previewRect.anchorMin = new Vector2(0.5f, 0.5f);
            previewRect.anchorMax = new Vector2(0.5f, 0.5f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.anchoredPosition = new Vector2(0f, -5f);
            previewRect.sizeDelta = new Vector2(420f, 420f);
            previewRect.localScale = Vector3.one;

            Image previewBackground =
                GetOrAddComponent<Image>(previewRect.gameObject);
            previewBackground.color = AppearanceBackgroundColor;
            previewBackground.raycastTarget = false;

            RectTransform layerContainer = FindOrCreateRectChild(
                previewRect,
                "LayerContainer");
            Stretch(layerContainer);
            layerContainer.localScale = new Vector3(-1f, 1f, 1f);

            Image shadow = ConfigurePreviewLayer(
                layerContainer,
                "Shadow");
            Image leftLeg = ConfigurePreviewLayer(
                layerContainer,
                "LeftLeg");
            Image rightLeg = ConfigurePreviewLayer(
                layerContainer,
                "RightLeg");
            Image body = ConfigurePreviewLayer(
                layerContainer,
                "Body");
            Image bodyAccessory = ConfigurePreviewLayer(
                layerContainer,
                "BodyAccessory");
            Image leftHand = ConfigurePreviewLayer(
                layerContainer,
                "LeftHand");
            Image rightHand = ConfigurePreviewLayer(
                layerContainer,
                "RightHand");
            Image head = ConfigurePreviewLayer(
                layerContainer,
                "Head");

            PlayerSkinCatalog catalog =
                AssetDatabase.LoadAssetAtPath<PlayerSkinCatalog>(
                    SkinCatalogPath);
            PlayerLayerOrderProfile orderProfile =
                AssetDatabase.LoadAssetAtPath<PlayerLayerOrderProfile>(
                    LayerOrderProfilePath);
            PlayerStateStore stateStore =
                Object.FindFirstObjectByType<PlayerStateStore>(
                    FindObjectsInactive.Include);

            PlayerAppearancePreview previewController =
                GetOrAddComponent<PlayerAppearancePreview>(
                    previewRect.gameObject);
            SerializedObject serializedPreview = new(previewController);
            AssignObject(serializedPreview, "playerStateStore", stateStore);
            AssignObject(serializedPreview, "skinCatalog", catalog);
            AssignObject(serializedPreview, "layerOrderProfile", orderProfile);
            AssignObject(serializedPreview, "shadowImage", shadow);
            AssignObject(serializedPreview, "leftLegImage", leftLeg);
            AssignObject(serializedPreview, "rightLegImage", rightLeg);
            AssignObject(serializedPreview, "bodyImage", body);
            AssignObject(
                serializedPreview,
                "bodyAccessoryImage",
                bodyAccessory);
            AssignObject(serializedPreview, "leftHandImage", leftHand);
            AssignObject(serializedPreview, "rightHandImage", rightHand);
            AssignObject(serializedPreview, "headImage", head);
            serializedPreview.ApplyModifiedPropertiesWithoutUndo();

            PlayerSkinDefinition defaultSkin = null;

            if (catalog != null &&
                catalog.TryGetSkin(
                    PlayerStateData.DefaultSkinId,
                    out defaultSkin))
            {
                const int firstVisibleFrame = 1;
                SetEditorSprite(
                    shadow,
                    defaultSkin,
                    PlayerSkinLayer.Shadow,
                    firstVisibleFrame);
                SetEditorSprite(
                    leftLeg,
                    defaultSkin,
                    PlayerSkinLayer.LeftLeg,
                    firstVisibleFrame);
                SetEditorSprite(
                    rightLeg,
                    defaultSkin,
                    PlayerSkinLayer.RightLeg,
                    firstVisibleFrame);
                SetEditorSprite(
                    body,
                    defaultSkin,
                    PlayerSkinLayer.Body,
                    firstVisibleFrame);
                SetEditorSprite(
                    bodyAccessory,
                    defaultSkin,
                    PlayerSkinLayer.BodyAccessory,
                    firstVisibleFrame);
                SetEditorSprite(
                    leftHand,
                    defaultSkin,
                    PlayerSkinLayer.LeftHand,
                    firstVisibleFrame);
                SetEditorSprite(
                    rightHand,
                    defaultSkin,
                    PlayerSkinLayer.RightHand,
                    firstVisibleFrame);
                SetEditorSprite(
                    head,
                    defaultSkin,
                    PlayerSkinLayer.Head,
                    firstVisibleFrame);
            }

            ApplyEditorLayerOrder(
                orderProfile,
                shadow,
                leftLeg,
                rightLeg,
                body,
                bodyAccessory,
                leftHand,
                rightHand,
                head);

            ConfigureColorCustomization(
                panel,
                stateStore,
                previewController,
                defaultSkin,
                font);
        }

        private static void ConfigureColorCustomization(
            RectTransform panel,
            PlayerStateStore stateStore,
            PlayerAppearancePreview previewController,
            PlayerSkinDefinition defaultSkin,
            TMP_FontAsset font)
        {
            RectTransform palettePanel = FindOrCreateRectChild(
                panel,
                "ColorPalettePanel");
            palettePanel.anchorMin = new Vector2(0f, 0.5f);
            palettePanel.anchorMax = new Vector2(0f, 0.5f);
            palettePanel.pivot = new Vector2(0f, 0.5f);
            palettePanel.anchoredPosition = new Vector2(55f, -45f);
            palettePanel.sizeDelta = new Vector2(830f, 590f);
            palettePanel.localScale = Vector3.one;

            Image paletteBackground =
                GetOrAddComponent<Image>(palettePanel.gameObject);
            paletteBackground.color = new Color(
                AppearanceBackgroundColor.r,
                AppearanceBackgroundColor.g,
                AppearanceBackgroundColor.b,
                0.35f);

            RectTransform viewport = FindOrCreateRectChild(
                palettePanel,
                "Viewport");
            Stretch(viewport);
            viewport.offsetMin = new Vector2(0f, 0f);
            viewport.offsetMax = new Vector2(-28f, 0f);
            Image viewportImage = GetOrAddComponent<Image>(viewport.gameObject);
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
            viewportImage.raycastTarget = true;
            GetOrAddComponent<Mask>(viewport.gameObject).showMaskGraphic = false;

            RectTransform content = FindOrCreateRectChild(
                viewport,
                "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 690f);
            content.localScale = Vector3.one;

            RectTransform scrollbarRect = FindOrCreateRectChild(
                palettePanel,
                "VerticalScrollbar");
            scrollbarRect.anchorMin = new Vector2(1f, 0f);
            scrollbarRect.anchorMax = new Vector2(1f, 1f);
            scrollbarRect.pivot = new Vector2(1f, 0.5f);
            scrollbarRect.anchoredPosition = Vector2.zero;
            scrollbarRect.sizeDelta = new Vector2(20f, 0f);
            Image scrollbarBackground =
                GetOrAddComponent<Image>(scrollbarRect.gameObject);
            scrollbarBackground.color = new Color(0.62f, 0.62f, 0.62f, 0.45f);

            RectTransform slidingArea = FindOrCreateRectChild(
                scrollbarRect,
                "SlidingArea");
            Stretch(slidingArea);
            slidingArea.offsetMin = new Vector2(2f, 2f);
            slidingArea.offsetMax = new Vector2(-2f, -2f);

            RectTransform handle = FindOrCreateRectChild(
                slidingArea,
                "Handle");
            Stretch(handle);
            Image handleImage = GetOrAddComponent<Image>(handle.gameObject);
            handleImage.color = new Color(0.92f, 0.64f, 0.42f, 1f);

            Scrollbar scrollbar =
                GetOrAddComponent<Scrollbar>(scrollbarRect.gameObject);
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.size = 0.45f;

            ScrollRect scrollRect =
                GetOrAddComponent<ScrollRect>(palettePanel.gameObject);
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.verticalScrollbar = scrollbar;
            scrollRect.verticalScrollbarVisibility =
                ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.verticalNormalizedPosition = 1f;

            List<ConfiguredPaletteOption> options = new();
            ConfigurePaletteGroup(
                content,
                "HeadPalette",
                "Cabeza",
                0f,
                AppearanceColorGroup.Head,
                defaultSkin,
                PlayerSkinLayer.Head,
                font,
                options);
            ConfigurePaletteGroup(
                content,
                "BodyPalette",
                "Cuerpo y piernas",
                -225f,
                AppearanceColorGroup.Body,
                defaultSkin,
                PlayerSkinLayer.Body,
                font,
                options);
            ConfigurePaletteGroup(
                content,
                "HandsPalette",
                "Manos",
                -450f,
                AppearanceColorGroup.Hands,
                defaultSkin,
                PlayerSkinLayer.LeftHand,
                font,
                options);

            RectTransform appearanceSection =
                panel.Find("AppearanceSection") as RectTransform;
            RectTransform saveRect = FindOrCreateRectChild(
                appearanceSection,
                "BTN_SaveAppearance");
            saveRect.anchorMin = new Vector2(0.5f, 0f);
            saveRect.anchorMax = new Vector2(0.5f, 0f);
            saveRect.pivot = new Vector2(0.5f, 0f);
            saveRect.anchoredPosition = new Vector2(0f, 5f);
            saveRect.sizeDelta = new Vector2(420f, 72f);
            saveRect.localScale = Vector3.one;

            Image saveImage = GetOrAddComponent<Image>(saveRect.gameObject);
            saveImage.color = new Color(0.83f, 0.83f, 0.83f, 1f);
            Button saveButton = GetOrAddComponent<Button>(saveRect.gameObject);
            saveButton.targetGraphic = saveImage;
            Outline saveOutline = GetOrAddComponent<Outline>(saveRect.gameObject);
            saveOutline.effectColor = TitleColor;
            saveOutline.effectDistance = new Vector2(3f, -3f);

            RectTransform saveLabelRect = FindOrCreateRectChild(
                saveRect,
                "TXT_SaveAppearance");
            Stretch(saveLabelRect);
            TextMeshProUGUI saveLabel =
                GetOrAddComponent<TextMeshProUGUI>(saveLabelRect.gameObject);
            saveLabel.text = "Guardar";
            saveLabel.fontSize = 38f;
            saveLabel.fontStyle = FontStyles.Bold;
            saveLabel.alignment = TextAlignmentOptions.Center;
            saveLabel.color = TitleColor;
            saveLabel.raycastTarget = false;

            if (font != null)
            {
                saveLabel.font = font;
            }

            PlayerAppearanceCustomizationController customizationController =
                GetOrAddComponent<PlayerAppearanceCustomizationController>(
                    panel.gameObject);
            SerializedObject serializedCustomization =
                new(customizationController);
            AssignObject(
                serializedCustomization,
                "playerStateStore",
                stateStore);
            AssignObject(
                serializedCustomization,
                "appearancePreview",
                previewController);
            VentanaEmergenteEstetico customizationWindow =
                panel.GetComponentInParent<VentanaEmergenteEstetico>(true);
            AssignObject(
                serializedCustomization,
                "customizationWindow",
                customizationWindow);
            AssignObject(serializedCustomization, "saveButton", saveButton);

            SerializedProperty optionsProperty =
                serializedCustomization.FindProperty("paletteOptions");
            optionsProperty.arraySize = options.Count;

            for (int index = 0; index < options.Count; index++)
            {
                ConfiguredPaletteOption option = options[index];
                SerializedProperty optionProperty =
                    optionsProperty.GetArrayElementAtIndex(index);
                optionProperty.FindPropertyRelative("Group").enumValueIndex =
                    (int)option.Group;
                optionProperty.FindPropertyRelative("Color").colorValue =
                    option.Color;
                optionProperty.FindPropertyRelative("Button")
                    .objectReferenceValue = option.Button;
                optionProperty.FindPropertyRelative("SelectionOutline")
                    .objectReferenceValue = option.SelectionOutline;

                option.Button.onClick = new Button.ButtonClickedEvent();
                UnityEventTools.AddIntPersistentListener(
                    option.Button.onClick,
                    customizationController.SelectPaletteOption,
                    index);
                EditorUtility.SetDirty(option.Button);
            }

            saveButton.onClick = new Button.ButtonClickedEvent();
            UnityEventTools.AddPersistentListener(
                saveButton.onClick,
                customizationController.SaveAppearance);
            EditorUtility.SetDirty(saveButton);

            serializedCustomization.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigurePaletteGroup(
            RectTransform content,
            string objectName,
            string titleText,
            float positionY,
            AppearanceColorGroup group,
            PlayerSkinDefinition defaultSkin,
            PlayerSkinLayer representativeLayer,
            TMP_FontAsset font,
            List<ConfiguredPaletteOption> options)
        {
            RectTransform groupRect = FindOrCreateRectChild(
                content,
                objectName);
            groupRect.anchorMin = new Vector2(0f, 1f);
            groupRect.anchorMax = new Vector2(1f, 1f);
            groupRect.pivot = new Vector2(0.5f, 1f);
            groupRect.anchoredPosition = new Vector2(0f, positionY);
            groupRect.sizeDelta = new Vector2(0f, 210f);
            groupRect.localScale = Vector3.one;

            RectTransform titleRect = FindOrCreateRectChild(
                groupRect,
                "TXT_Title");
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(14f, 0f);
            titleRect.sizeDelta = new Vector2(-28f, 34f);
            TextMeshProUGUI title =
                GetOrAddComponent<TextMeshProUGUI>(titleRect.gameObject);
            title.text = titleText;
            title.fontSize = 25f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Left;
            title.color = TitleColor;
            title.raycastTarget = false;

            if (font != null)
            {
                title.font = font;
            }

            RectTransform optionsRect = FindOrCreateRectChild(
                groupRect,
                "Options");
            optionsRect.anchorMin = new Vector2(0f, 1f);
            optionsRect.anchorMax = new Vector2(1f, 1f);
            optionsRect.pivot = new Vector2(0.5f, 1f);
            optionsRect.anchoredPosition = new Vector2(0f, -38f);
            optionsRect.sizeDelta = new Vector2(-24f, 166f);
            Image optionsBackground =
                GetOrAddComponent<Image>(optionsRect.gameObject);
            optionsBackground.color = new Color(0.88f, 0.88f, 0.88f, 1f);
            optionsBackground.raycastTarget = false;

            Sprite representativeSprite = defaultSkin != null
                ? defaultSkin.GetSprite(
                    representativeLayer,
                    PlayerAnimationType.IdleFront,
                    1)
                : null;

            for (int index = 0; index < PaletteColors.Length; index++)
            {
                int column = index % 5;
                int row = index / 5;
                RectTransform optionRect = FindOrCreateRectChild(
                    optionsRect,
                    $"Option_{index + 1:00}");
                optionRect.anchorMin = new Vector2(0f, 1f);
                optionRect.anchorMax = new Vector2(0f, 1f);
                optionRect.pivot = new Vector2(0.5f, 0.5f);
                optionRect.anchoredPosition = new Vector2(
                    75f + (column * 135f),
                    -42f - (row * 78f));
                optionRect.sizeDelta = new Vector2(70f, 70f);
                optionRect.localScale = Vector3.one;

                Image optionBackground =
                    GetOrAddComponent<Image>(optionRect.gameObject);
                optionBackground.color = new Color(0.82f, 0.82f, 0.82f, 1f);
                Button button = GetOrAddComponent<Button>(optionRect.gameObject);
                button.targetGraphic = optionBackground;
                Outline selectionOutline =
                    GetOrAddComponent<Outline>(optionRect.gameObject);
                selectionOutline.effectColor = SelectionColor;
                selectionOutline.effectDistance = new Vector2(4f, -4f);
                selectionOutline.enabled = index == 0;

                RectTransform iconRect = FindOrCreateRectChild(
                    optionRect,
                    "LayerPreview");
                Stretch(iconRect);
                iconRect.offsetMin = new Vector2(7f, 7f);
                iconRect.offsetMax = new Vector2(-7f, -7f);
                Image icon = GetOrAddComponent<Image>(iconRect.gameObject);
                icon.sprite = representativeSprite;
                icon.color = PaletteColors[index];
                icon.preserveAspect = true;
                icon.raycastTarget = false;

                options.Add(new ConfiguredPaletteOption(
                    group,
                    PaletteColors[index],
                    button,
                    selectionOutline));
            }
        }

        private readonly struct ConfiguredPaletteOption
        {
            public ConfiguredPaletteOption(
                AppearanceColorGroup group,
                Color color,
                Button button,
                Outline selectionOutline)
            {
                Group = group;
                Color = color;
                Button = button;
                SelectionOutline = selectionOutline;
            }

            public AppearanceColorGroup Group { get; }
            public Color Color { get; }
            public Button Button { get; }
            public Outline SelectionOutline { get; }
        }

        private static Image ConfigurePreviewLayer(
            RectTransform parent,
            string layerName)
        {
            RectTransform layer = FindOrCreateRectChild(parent, layerName);
            Stretch(layer);
            Image image = GetOrAddComponent<Image>(layer.gameObject);
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private static void AssignObject(
            SerializedObject serializedObject,
            string propertyName,
            Object value)
        {
            serializedObject.FindProperty(propertyName).objectReferenceValue =
                value;
        }

        private static void SetEditorSprite(
            Image image,
            PlayerSkinDefinition skin,
            PlayerSkinLayer layer,
            int frame)
        {
            image.sprite = skin.GetSprite(
                layer,
                PlayerAnimationType.IdleFront,
                frame);
            image.enabled = image.sprite != null;
            EditorUtility.SetDirty(image);
        }

        private static void ApplyEditorLayerOrder(
            PlayerLayerOrderProfile orderProfile,
            params Image[] images)
        {
            if (orderProfile == null)
            {
                return;
            }

            PlayerSkinLayer[] layers =
            {
                PlayerSkinLayer.Shadow,
                PlayerSkinLayer.LeftLeg,
                PlayerSkinLayer.RightLeg,
                PlayerSkinLayer.Body,
                PlayerSkinLayer.BodyAccessory,
                PlayerSkinLayer.LeftHand,
                PlayerSkinLayer.RightHand,
                PlayerSkinLayer.Head
            };

            for (int first = 0; first < images.Length - 1; first++)
            {
                for (int second = first + 1;
                     second < images.Length;
                     second++)
                {
                    int firstOrder = orderProfile.GetSortingOrder(
                        PlayerAnimationType.IdleFront,
                        layers[first]);
                    int secondOrder = orderProfile.GetSortingOrder(
                        PlayerAnimationType.IdleFront,
                        layers[second]);

                    if (firstOrder <= secondOrder)
                    {
                        continue;
                    }

                    (images[first], images[second]) =
                        (images[second], images[first]);
                    (layers[first], layers[second]) =
                        (layers[second], layers[first]);
                }
            }

            for (int index = 0; index < images.Length; index++)
            {
                images[index].transform.SetSiblingIndex(index);
            }
        }

        private static GameObject FindOrCreateRoot(string objectName)
        {
            GameObject existing = GameObject.Find(objectName);

            if (existing != null)
            {
                return existing;
            }

            GameObject created = new(objectName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(created, $"Create {objectName}");
            return created;
        }

        private static void ConfigureEventSystem()
        {
            EventSystem eventSystem =
                Object.FindFirstObjectByType<EventSystem>(
                    FindObjectsInactive.Include);
            GameObject eventSystemObject;

            if (eventSystem == null)
            {
                eventSystemObject = new GameObject("EventSystem");
                Undo.RegisterCreatedObjectUndo(
                    eventSystemObject,
                    "Create EventSystem");
                eventSystem = GetOrAddComponent<EventSystem>(
                    eventSystemObject);
            }
            else
            {
                eventSystemObject = eventSystem.gameObject;
            }

            StandaloneInputModule legacyInputModule =
                eventSystemObject.GetComponent<StandaloneInputModule>();

            if (legacyInputModule != null)
            {
                Undo.DestroyObjectImmediate(legacyInputModule);
            }

            InputSystemUIInputModule inputModule =
                GetOrAddComponent<InputSystemUIInputModule>(
                    eventSystemObject);

            if (inputModule.actionsAsset == null)
            {
                inputModule.AssignDefaultActions();
            }

            EditorUtility.SetDirty(eventSystem);
            EditorUtility.SetDirty(inputModule);
        }

        private static RectTransform FindOrCreateRectChild(
            RectTransform parent,
            string childName)
        {
            Transform existing = parent.Find(childName);

            if (existing is RectTransform existingRect)
            {
                return existingRect;
            }

            GameObject child = new(childName, typeof(RectTransform));
            Undo.RegisterCreatedObjectUndo(child, $"Create {childName}");
            RectTransform childRect = child.GetComponent<RectTransform>();
            childRect.SetParent(parent, false);
            return childRect;
        }

        private static T GetOrAddComponent<T>(GameObject target)
            where T : Component
        {
            T component = target.GetComponent<T>();
            return component != null
                ? component
                : Undo.AddComponent<T>(target);
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }
}
#endif
