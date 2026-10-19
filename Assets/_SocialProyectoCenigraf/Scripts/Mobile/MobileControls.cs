using System.Runtime.InteropServices;
using SocialProyectoCenigraf.Dialogue;
using SocialProyectoCenigraf.Player.Movement;
using SocialProyectoCenigraf.Rewards;
using SocialProyectoCenigraf.World.Teleport;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.Mobile
{
    public sealed class MobileControls : MonoBehaviour
    {
        private static MobileControls instance;
        public static bool IsMobileDevice
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return NIWI_IsMobileDevice() != 0;
#else
                return Application.isMobilePlatform;
#endif
            }
        }
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int NIWI_IsMobileDevice();
        [DllImport("__Internal")] private static extern float NIWI_CanvasPixelRatio();
#endif
        public static Vector2 Movement => instance != null && instance.joystick != null
            && instance.joystick.isActiveAndEnabled ? instance.joystick.Value : Vector2.zero;
        private Canvas canvas;
        private RectTransform safeArea;
        private MobileJoystick joystick;
        private Button interactButton;
        private PlayerMovementController player;
        private float nextPlayerSearch;
        private Sprite disc;
        private Texture2D discTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (IsMobileDevice && instance == null) new GameObject("MobileControls").AddComponent<MobileControls>();
        }
        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            BuildInterface();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            player = null; nextPlayerSearch = 0;
            joystick.ResetInput(); canvas.enabled = false;
        }
        private void LateUpdate()
        {
            if (player != null && !player.AcceptsLocalInput) player = null;
            if (player == null && Time.unscaledTime >= nextPlayerSearch)
            {
                foreach (var candidate in FindObjectsByType<PlayerMovementController>(FindObjectsSortMode.None))
                    if (candidate.AcceptsLocalInput) { player = candidate; break; }
                nextPlayerSearch = Time.unscaledTime + 0.25f;
            }
            bool show = player != null && player.gameObject.activeInHierarchy && !TeleportTravel.IsTraveling;
            canvas.enabled = show;
            if (!show) { joystick.ResetInput(); return; }
            ApplySafeArea();
            bool modal = DialoguePresenter.IsOpen || RewardWindow.IsOpen;
            joystick.gameObject.SetActive(!modal && player.isActiveAndEnabled);
            // Rewards already have a touchable Continue button; don't cover it.
            bool available = !RewardWindow.IsOpen && (DialoguePresenter.IsOpen
                || (player.isActiveAndEnabled && MobileInteractionTargets.Nearest(player) != null));
            interactButton.gameObject.SetActive(available);
        }
        private void Interact()
        {
            if (TeleportTravel.IsTraveling || RewardWindow.IsOpen || player == null) return;
            joystick.ResetInput();
            if (DialoguePresenter.IsOpen) { DialoguePresenter.AdvanceCurrent(); return; }
            if (Time.frameCount == DialoguePresenter.LastClosedFrame || Time.frameCount == RewardWindow.LastClosedFrame) return;
            MobileInteractionTargets.Nearest(player)?.InteractWith(player);
        }
        private void ApplySafeArea()
        {
            Rect safe = Screen.safeArea;
            safeArea.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            safeArea.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
            // Use browser logical pixels rather than making the controls huge on tablets.
#if UNITY_WEBGL && !UNITY_EDITOR
            float pixelRatio = NIWI_CanvasPixelRatio();
#elif UNITY_EDITOR
            float pixelRatio = 1f;
#else
            float pixelRatio = Screen.dpi > 0 ? Screen.dpi / 160f : 1f;
#endif
            canvas.scaleFactor = Mathf.Max(0.1f, Mathf.Min(pixelRatio, Mathf.Min(safe.width, safe.height) / 402f));
        }
        private void BuildInterface()
        {
            discTexture = new Texture2D(128,128,TextureFormat.RGBA32,false);
            discTexture.wrapMode = TextureWrapMode.Clamp;
            for (int y=0;y<128;y++) for(int x=0;x<128;x++)
            {
                float distance = Vector2.Distance(new Vector2(x+0.5f,y+0.5f),new Vector2(64,64));
                discTexture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(64-distance)));
            }
            discTexture.Apply();
            disc = Sprite.Create(discTexture,new Rect(0,0,128,128),new Vector2(0.5f,0.5f),128);
            var root = new GameObject("TouchHUD",typeof(RectTransform),typeof(Canvas),typeof(GraphicRaycaster));
            root.transform.SetParent(transform,false);
            canvas = root.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder=3000; canvas.enabled=false;
            safeArea = new GameObject("SafeArea",typeof(RectTransform)).GetComponent<RectTransform>();
            safeArea.SetParent(root.transform,false);safeArea.offsetMin=safeArea.offsetMax=Vector2.zero;
            var pad=Circle("Joystick",safeArea,200,new Color(0.7f,0.7f,0.7f,0.22f),true);
            pad.anchorMin=pad.anchorMax=Vector2.zero;pad.pivot=Vector2.zero;pad.anchoredPosition=new Vector2(16,24);
            var rim=Circle("HandleBorder",pad,100,new Color(0.25f,0.25f,0.25f,0.5f),false);
            Circle("HandleFill",rim,92,new Color(0.6f,0.6f,0.6f,0.6f),false);
            joystick=pad.gameObject.AddComponent<MobileJoystick>();joystick.Handle=rim;
            var action=Circle("Interact",safeArea,100,new Color(0.3f,0.3f,0.3f,0.38f),true);
            action.anchorMin=action.anchorMax=new Vector2(1,0);action.pivot=new Vector2(1,0);action.anchoredPosition=new Vector2(-56,44);
            Circle("ButtonFill",action,92,new Color(0.7f,0.7f,0.7f,0.38f),false);
            interactButton=action.gameObject.AddComponent<Button>();
            interactButton.targetGraphic=action.GetComponent<Image>();
            interactButton.navigation=new UnityEngine.UI.Navigation { mode=UnityEngine.UI.Navigation.Mode.None };
            interactButton.onClick.AddListener(Interact);
            var label=new GameObject("Label",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));
            label.transform.SetParent(action,false);
            var text=label.GetComponent<TextMeshProUGUI>();text.text="E";text.font=TMP_Settings.defaultFontAsset;
            text.fontSize=36;text.alignment=TextAlignmentOptions.Center;text.color=new Color(0.2f,0.2f,0.2f,0.85f);text.raycastTarget=false;
            var rect=(RectTransform)label.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            action.gameObject.SetActive(false);
        }
        private RectTransform Circle(string name,Transform parent,float size,Color color,bool raycast)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
            var rect=(RectTransform)go.transform;rect.SetParent(parent,false);rect.sizeDelta=Vector2.one*size;
            rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);
            var image=go.GetComponent<Image>();image.sprite=disc;image.color=color;image.raycastTarget=raycast;
            return rect;
        }
        private void OnApplicationFocus(bool focused) { if(!focused) joystick?.ResetInput(); }
        private void OnApplicationPause(bool paused) { if(paused) joystick?.ResetInput(); }
        private void OnDestroy()
        {
            SceneManager.sceneLoaded-=OnSceneLoaded;
            if(instance!=this)return;
            instance=null;
            if(disc!=null)Destroy(disc);
            if(discTexture!=null)Destroy(discTexture);
        }
    }
}
