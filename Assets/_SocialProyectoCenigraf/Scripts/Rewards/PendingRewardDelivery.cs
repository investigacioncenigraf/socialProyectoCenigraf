using System.Collections;
using SocialProyectoCenigraf.Items;
using SocialProyectoCenigraf.Player.Movement;
using SocialProyectoCenigraf.World.Teleport;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SocialProyectoCenigraf.Rewards
{
    /// <summary>
    /// Holds a reward that must be handed over in another scene, once the player gets there.
    /// Used by the maze: the test is passed here, the gem is received back at the temple.
    /// </summary>
    public static class PendingRewardDelivery
    {
        private static RewardDefinition reward;
        private static ItemCatalog itemCatalog;
        private static string itemId;
        private static string repeatItemId;
        private static int repeatItemAmount;
        private static string targetSceneName;
        private static bool listening;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            reward = null;
            itemCatalog = null;
            itemId = null;
            repeatItemId = null;
            repeatItemAmount = 0;
            targetSceneName = null;

            if (listening)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
                listening = false;
            }
        }

        /// <summary>Leaves a reward waiting for the scene the player is travelling to.</summary>
        public static void Schedule(
            RewardDefinition pendingReward,
            string sceneName,
            ItemCatalog catalog = null,
            string pendingItemId = null,
            string pendingRepeatItemId = null,
            int pendingRepeatAmount = 0)
        {
            if (pendingReward == null || string.IsNullOrWhiteSpace(sceneName))
            {
                return;
            }

            reward = pendingReward;
            targetSceneName = SceneNameOf(sceneName);
            itemCatalog = catalog;
            itemId = pendingItemId;
            repeatItemId = pendingRepeatItemId;
            repeatItemAmount = pendingRepeatAmount;

            if (!listening)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                listening = true;
            }
        }

        private static string SceneNameOf(string pathOrName)
        {
            string value = pathOrName.Trim();
            int slash = value.LastIndexOf('/');

            if (slash >= 0)
            {
                value = value.Substring(slash + 1);
            }

            return value.EndsWith(".unity") ? value.Substring(0, value.Length - 6) : value;
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (reward == null || mode != LoadSceneMode.Single || scene.name != targetSceneName)
            {
                return;
            }

            DeliveryRunner.Run(DeliverWhenReady());
        }

        private static IEnumerator DeliverWhenReady()
        {
            // The travel fades out and holds the player: wait until he can move again.
            while (TeleportTravel.IsTraveling)
            {
                yield return null;
            }

            yield return null;

            RewardDefinition pending = reward;
            ItemCatalog catalog = itemCatalog;
            string pendingItem = itemId;
            string repeatItem = repeatItemId;
            int repeatAmount = repeatItemAmount;

            reward = null;
            itemCatalog = null;
            itemId = null;
            repeatItemId = null;
            repeatItemAmount = 0;
            targetSceneName = null;

            // Already earned once: the player keeps the memento and gets paid in nuggets instead.
            bool alreadyOwned = RewardWindow.HasReceived(pending.id) ||
                                (!string.IsNullOrWhiteSpace(pendingItem) &&
                                 PlayerItemInventory.HasItem(pendingItem));

            if (alreadyOwned)
            {
                if (catalog != null && !string.IsNullOrWhiteSpace(repeatItem) && repeatAmount > 0)
                {
                    PlayerItemInventory.TryGrant(catalog, repeatItem, repeatAmount);
                    SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayReward();
                }

                yield break;
            }

            if (catalog != null && !string.IsNullOrWhiteSpace(pendingItem))
            {
                PlayerItemInventory.TryGrant(catalog, pendingItem);
            }

            PlayerMovementController player = Object.FindFirstObjectByType<PlayerMovementController>();
            RewardWindow.GrantAndShow(pending, player);
        }

        /// <summary>Small host so a static class can run a coroutine.</summary>
        private sealed class DeliveryRunner : MonoBehaviour
        {
            private static DeliveryRunner instance;

            public static void Run(IEnumerator routine)
            {
                if (instance == null)
                {
                    GameObject host = new GameObject(nameof(DeliveryRunner));
                    DontDestroyOnLoad(host);
                    host.hideFlags = HideFlags.HideAndDontSave;
                    instance = host.AddComponent<DeliveryRunner>();
                }

                instance.StartCoroutine(routine);
            }
        }
    }
}
