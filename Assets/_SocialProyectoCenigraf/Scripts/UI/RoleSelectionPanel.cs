using SocialProyectoCenigraf.Roles;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SocialProyectoCenigraf.UI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class RoleSelectionPanel : MonoBehaviour
    {
        private const string PreviewRootName = "__RoleButtonsPreview";
        private const float ButtonWidth = 500f;
        private const float ButtonHeight = 88f;

        [SerializeField] private RoleCatalog catalog;
        [SerializeField] private RoleSelectionButton buttonPrefab;
        [SerializeField, Min(0f)] private float spacing = 16f;

#if UNITY_EDITOR
        private bool previewRefreshScheduled;
#endif

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                BuildRuntimeButtons();
                return;
            }

#if UNITY_EDITOR
            SchedulePreviewRefresh();
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                ClearPreview();
            }
#endif
        }

        private void BuildRuntimeButtons()
        {
            if (!HasRequiredReferences(true))
            {
                return;
            }

            ConfigureLayout((RectTransform)transform, catalog.Roles.Count);

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }

            for (int i = 0; i < catalog.Roles.Count; i++)
            {
                RoleDefinition role = catalog.Roles[i];
                if (role == null)
                {
                    continue;
                }

                RoleSelectionButton button = Instantiate(buttonPrefab, transform);
                button.name = $"BTN_Role_{role.Id}";
                button.Configure(role);
            }
        }

        private bool HasRequiredReferences(bool reportError)
        {
            bool hasReferences = catalog != null && buttonPrefab != null;
            if (!hasReferences && reportError)
            {
                Debug.LogError(
                    "RoleSelectionPanel requires a catalog and a button prefab.",
                    this);
            }

            return hasReferences;
        }

        private void ConfigureLayout(RectTransform target, int roleCount)
        {
            VerticalLayoutGroup layout = target.GetComponent<VerticalLayoutGroup>();
            if (layout == null)
            {
                layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            target.anchorMin = new Vector2(0.5f, 0.5f);
            target.anchorMax = new Vector2(0.5f, 0.5f);
            target.pivot = new Vector2(0.5f, 0.5f);
            target.anchoredPosition = Vector2.zero;

            float height = roleCount <= 0
                ? 0f
                : roleCount * ButtonHeight + (roleCount - 1) * spacing;
            target.sizeDelta = new Vector2(ButtonWidth, height);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                SchedulePreviewRefresh();
            }
        }

        private void SchedulePreviewRefresh()
        {
            if (previewRefreshScheduled || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            previewRefreshScheduled = true;
            EditorApplication.delayCall += RefreshPreviewDelayed;
        }

        private void RefreshPreviewDelayed()
        {
            previewRefreshScheduled = false;
            if (this == null || !isActiveAndEnabled || Application.isPlaying)
            {
                return;
            }

            BuildPreview();
        }

        private void BuildPreview()
        {
            ClearPreview();
            if (!HasRequiredReferences(false))
            {
                return;
            }

            GameObject previewObject = new GameObject(
                PreviewRootName,
                typeof(RectTransform));
            previewObject.hideFlags = HideFlags.DontSaveInEditor |
                                      HideFlags.DontSaveInBuild;

            RectTransform previewRoot = previewObject.GetComponent<RectTransform>();
            previewRoot.SetParent(transform, false);
            ConfigureLayout(previewRoot, catalog.Roles.Count);

            for (int i = 0; i < catalog.Roles.Count; i++)
            {
                RoleDefinition role = catalog.Roles[i];
                if (role == null)
                {
                    continue;
                }

                RoleSelectionButton button = Instantiate(buttonPrefab, previewRoot);
                button.name = $"__Preview_BTN_Role_{role.Id}";
                button.Configure(role);
                ApplyPreviewHideFlags(button.gameObject);
            }
        }

        private void ClearPreview()
        {
            Transform previewRoot = transform.Find(PreviewRootName);
            if (previewRoot != null)
            {
                DestroyImmediate(previewRoot.gameObject);
            }
        }

        private static void ApplyPreviewHideFlags(GameObject root)
        {
            HideFlags flags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < descendants.Length; i++)
            {
                descendants[i].gameObject.hideFlags = flags;
            }
        }
#endif
    }
}
