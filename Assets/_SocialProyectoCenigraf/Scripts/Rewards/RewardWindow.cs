using System.Collections.Generic;
using SocialProyectoCenigraf.Player.Movement;
using TMPro;
using SocialProyectoCenigraf.Player.State;
using SocialProyectoCenigraf.Session.State;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
namespace SocialProyectoCenigraf.Rewards
{
    public sealed class RewardWindow : MonoBehaviour
    {
        [SerializeField] private GameObject windowContent;
        [SerializeField] private TMP_Text itemName;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Image itemIcon;
        [SerializeField] private Button continueButton;
        [SerializeField] private RewardDefinition testReward;
        [SerializeField] private bool showTestOnStart;
        private static RewardWindow instance;
        private static readonly HashSet<string> received = new HashSet<string>();
        private static readonly HashSet<string> acknowledged = new HashSet<string>();
        private RewardDefinition currentReward;
        private bool grantedReward;
        public static bool IsOpen => instance != null && instance.visible;
        public static int LastClosedFrame { get; private set; } = -1;
        public static bool HasReceived(string id) => !string.IsNullOrEmpty(id) && received.Contains(id);
        private bool visible;
        private PlayerMovementController player;
        private bool restoreMovement;
        private int openedFrame;
        private GameObject previousSelection;
        private GameObject ownedEventSystem;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession() { instance=null;received.Clear();acknowledged.Clear();LastClosedFrame=-1; }
        private void Awake()
        {
            if(instance != null && instance != this) { Destroy(gameObject);return; }
            instance=this;
            continueButton.onClick.AddListener(ConfirmContinue);
            windowContent.SetActive(false);
        }
        private void Start() { if(showTestOnStart) TestReward(); }
        public static bool GrantAndShow(RewardDefinition reward,PlayerMovementController recipient)
        {
            if(reward==null || string.IsNullOrWhiteSpace(reward.id) || acknowledged.Contains(reward.id) || IsOpen) return false;
            if(instance==null)
            {
                var prefab=Resources.Load<RewardWindow>("UI/RewardWindow");
                if(prefab==null) { Debug.LogError("No se encontró Resources/UI/RewardWindow.prefab.");return false; }
                Instantiate(prefab);
            }
            instance.Show(reward,recipient);
            instance.grantedReward=true;
            bool firstReceipt = received.Add(reward.id);
            if(firstReceipt) SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayReward();
            return true;
        }
        public void Show(RewardDefinition reward,PlayerMovementController recipient)
        {
            if(reward==null || visible) return;
            currentReward=reward;grantedReward=false;
            player=recipient;restoreMovement=player!=null && player.enabled;
            if(restoreMovement) { player.enabled=false;player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero; }
            itemName.text=reward.displayName;description.text=reward.description;
            itemIcon.sprite=reward.icon;itemIcon.enabled=reward.icon!=null;
            if(EventSystem.current==null)
                ownedEventSystem=new GameObject("RewardEventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            previousSelection=EventSystem.current.currentSelectedGameObject;
            windowContent.SetActive(true);visible=true;openedFrame=Time.frameCount;
            EventSystem.current.SetSelectedGameObject(continueButton.gameObject);
        }
        private void Update()
        {
            if(!visible || Time.frameCount==openedFrame || Keyboard.current==null) return;
            var keyboard=Keyboard.current;
            if(keyboard.eKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)ConfirmContinue();
            else if(keyboard.escapeKey.wasPressedThisFrame)Close();
        }
        public void ConfirmContinue()
        {
            if(!visible)return;
            if(grantedReward && currentReward!=null)
            {
                if(currentReward.roleOnContinue!=null)
                {
                    var state=player!=null ? player.GetComponent<PlayerStateStore>() : null;
                    if(state==null) { Debug.LogWarning("No se puede asignar el rol sin el jugador receptor.");return; }
                    string roleId=currentReward.roleOnContinue.Id;
                    state.SetRole(roleId);
                    GameSessionStore.Instance.Dispatch(GameSessionAction.SetSelectedRole(roleId));
                }
                acknowledged.Add(currentReward.id);
            }
            SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayInteraction();
            Close();
        }
        public void Close()
        {
            if(!visible) return;
            visible=false;windowContent.SetActive(false);
            if(player!=null && restoreMovement)player.enabled=true;
            player=null;restoreMovement=false;currentReward=null;grantedReward=false;LastClosedFrame=Time.frameCount;
            if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(previousSelection);
            previousSelection=null;
            if(ownedEventSystem!=null) { Destroy(ownedEventSystem);ownedEventSystem=null; }
        }
        [ContextMenu("Probar recompensa: Carnet de aprendiz")]
        public void TestReward()
        {
            if(!Application.isPlaying) { Debug.Log("Prueba la recompensa en modo Play.",this);return; }
            GrantAndShow(testReward,FindFirstObjectByType<PlayerMovementController>());
        }
        private void OnDisable() { if(visible)Close(); }
        private void OnDestroy()
        {
            if(instance!=this)return;
            Close();if(continueButton!=null)continueButton.onClick.RemoveListener(ConfirmContinue);instance=null;
        }
    }
}
