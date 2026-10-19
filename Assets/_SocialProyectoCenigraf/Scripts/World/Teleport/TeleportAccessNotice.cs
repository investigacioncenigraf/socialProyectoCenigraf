using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SocialProyectoCenigraf.World.Teleport
{
    public sealed class TeleportAccessNotice : MonoBehaviour
    {
        private static TeleportAccessNotice instance;
        private TMP_Text label;
        private float hideAt;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;
        public static void Show(string message)
        {
            if(instance==null)
            {
                var root=new GameObject("TeleportAccessNotice",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
                instance=root.AddComponent<TeleportAccessNotice>();
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2100;
                var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=0.5f;
                var panel=new GameObject("AccessDeniedPanel",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                panel.transform.SetParent(root.transform,false);
                var rect=(RectTransform)panel.transform;rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0);
                rect.pivot=new Vector2(0.5f,0);rect.anchoredPosition=new Vector2(0,60);rect.sizeDelta=new Vector2(1000,110);
                var image=panel.GetComponent<Image>();image.color=new Color(0.12f,0.08f,0.16f,0.96f);image.raycastTarget=false;
                var text=new GameObject("Message",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));
                text.transform.SetParent(panel.transform,false);var tr=(RectTransform)text.transform;
                tr.anchorMin=Vector2.zero;tr.anchorMax=Vector2.one;tr.offsetMin=new Vector2(24,12);tr.offsetMax=new Vector2(-24,-12);
                instance.label=text.GetComponent<TextMeshProUGUI>();instance.label.font=TMP_Settings.defaultFontAsset;
                instance.label.color=new Color(1,0.8f,0.8f);instance.label.fontSize=20;instance.label.enableAutoSizing=true;
                instance.label.fontSizeMin=14;instance.label.fontSizeMax=20;instance.label.alignment=TextAlignmentOptions.Center;
                instance.label.richText=false;instance.label.raycastTarget=false;
            }
            instance.label.text=message;instance.hideAt=Time.unscaledTime+3f;instance.gameObject.SetActive(true);
        }
        private void Update() { if(Time.unscaledTime>=hideAt)gameObject.SetActive(false); }
        private void OnDestroy() { if(instance==this)instance=null; }
    }
}
