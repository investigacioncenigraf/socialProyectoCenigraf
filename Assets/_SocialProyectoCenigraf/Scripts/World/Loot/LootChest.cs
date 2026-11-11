using System.Collections.Generic;
using System.Text;
using SocialProyectoCenigraf.Controls;
using SocialProyectoCenigraf.Items;
using SocialProyectoCenigraf.Mobile;
using SocialProyectoCenigraf.Player.Movement;
using SocialProyectoCenigraf.World.NPC;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

namespace SocialProyectoCenigraf.World.Loot
{
    /// <summary>
    /// A chest the player opens once. Its level, from 0 to 9, decides how many materials come out,
    /// how rare they are and how many coins come with them.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Cenigraf/Loot/Loot Chest")]
    public sealed class LootChest : MonoBehaviour, IMobileInteractionTarget
    {
        [Header("Contenido")]
        [Tooltip("0 vacío, 9 el mejor botín.")]
        [Range(0, 9)] [SerializeField] private int level = 1;
        [SerializeField] private ChestLootTable lootTable;

        [Header("Referencias")]
        [SerializeField] private SpriteRenderer visual;
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openedSprite;
        [SerializeField] private NPCInteractionZone interactionZone;
        [SerializeField] private NPCInteractionIndicatorController indicator;
        [SerializeField] private TMP_Text lootLabel;
        [Min(0f)] [SerializeField] private float lootLabelSeconds = 3.5f;

        private bool opened;
        private float labelTimer;
        private static Sprite placeholderSprite;

        /// <summary>Raised once, when the player opens the chest.</summary>
        public event System.Action<LootChest> Opened;

        public bool IsOpen => opened;
        public int Level => level;
        public Vector3 InteractionPosition => transform.position;

        /// <summary>Used by code that places chests in a level.</summary>
        public void Configure(int chestLevel, ChestLootTable table)
        {
            level = Mathf.Clamp(chestLevel, 0, 9);
            lootTable = table;
        }

        private void OnEnable() => MobileInteractionTargets.Register(this);

        private void OnDisable()
        {
            MobileInteractionTargets.Unregister(this);
            indicator?.Hide();
        }

        private void Start()
        {
            if (visual != null)
            {
                visual.sprite = closedSprite != null ? closedSprite : PlaceholderSprite();

                if (closedSprite == null)
                {
                    // Visible while the real art is not ready.
                    visual.color = new Color(0.55f, 0.36f, 0.16f);
                    visual.transform.localScale = Vector3.one;
                }
            }

            if (lootLabel != null)
            {
                lootLabel.text = string.Empty;
                lootLabel.enabled = false;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            UpdateLabel();

            if (opened)
            {
                return;
            }

            indicator?.SetVisible(interactionZone != null && interactionZone.HasPlayerInside);

            if (GameplayInputLock.IsLocked ||
                Keyboard.current == null ||
                !Keyboard.current.eKey.wasPressedThisFrame)
            {
                return;
            }

            Open();
        }

        private void UpdateLabel()
        {
            if (lootLabel == null || !lootLabel.enabled)
            {
                return;
            }

            labelTimer -= Time.deltaTime;

            if (labelTimer <= 0f)
            {
                lootLabel.enabled = false;
                return;
            }

            // Fades out over the last second.
            Color color = lootLabel.color;
            color.a = Mathf.Clamp01(labelTimer);
            lootLabel.color = color;
        }

        public bool Open()
        {
            if (opened || interactionZone == null || !interactionZone.TryGetLocalPlayer(out _))
            {
                return false;
            }

            opened = true;
            indicator?.Hide();

            if (visual != null && openedSprite != null)
            {
                visual.sprite = openedSprite;
            }

            string summary = GrantLoot();
            ShowLabel(summary);
            Opened?.Invoke(this);

            if (!string.IsNullOrEmpty(summary))
            {
                SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayReward();
            }
            else
            {
                SocialProyectoCenigraf.Audio.RetroFeedbackAudio.PlayInteraction();
            }

            return true;
        }

        /// <summary>Rolls the contents and puts them in the inventory. Returns what came out.</summary>
        private string GrantLoot()
        {
            if (lootTable == null || lootTable.Catalog == null)
            {
                return string.Empty;
            }

            ItemCatalog catalog = lootTable.Catalog;
            ChestLevelLoot loot = lootTable.GetLevel(level);
            Dictionary<string, int> granted = new Dictionary<string, int>();

            int itemCount = Random.Range(loot.minItems, Mathf.Max(loot.minItems, loot.maxItems) + 1);

            for (int i = 0; i < itemCount; i++)
            {
                ItemDefinition material = PickMaterial(catalog, loot);

                if (material == null)
                {
                    continue;
                }

                if (PlayerItemInventory.TryGrant(catalog, material.id))
                {
                    granted.TryGetValue(material.displayName, out int count);
                    granted[material.displayName] = count + 1;
                }
            }

            int nuggets = Random.Range(loot.minNuggets, Mathf.Max(loot.minNuggets, loot.maxNuggets) + 1);

            foreach (KeyValuePair<string, int> payment in CurrencyExchange.Split(catalog, nuggets))
            {
                if (!catalog.TryGet(payment.Key, out ItemDefinition coin))
                {
                    continue;
                }

                if (PlayerItemInventory.TryGrant(catalog, coin.id, payment.Value))
                {
                    granted[coin.displayName] = payment.Value;
                }
            }

            return Summarize(granted);
        }

        /// <summary>Picks a rarity by weight, then a material of that rarity.</summary>
        private static ItemDefinition PickMaterial(ItemCatalog catalog, ChestLevelLoot loot)
        {
            int total = loot.commonWeight + loot.uncommonWeight + loot.rareWeight + loot.epicWeight;

            if (total <= 0)
            {
                return null;
            }

            int roll = Random.Range(0, total);
            int rarity = 1;

            if (roll < loot.commonWeight)
            {
                rarity = 1;
            }
            else if (roll < loot.commonWeight + loot.uncommonWeight)
            {
                rarity = 2;
            }
            else if (roll < loot.commonWeight + loot.uncommonWeight + loot.rareWeight)
            {
                rarity = 3;
            }
            else
            {
                rarity = 4;
            }

            List<ItemDefinition> candidates = new List<ItemDefinition>();

            foreach (ItemDefinition material in catalog.OfCategory(ItemCategory.Material))
            {
                if (material.tier == rarity)
                {
                    candidates.Add(material);
                }
            }

            return candidates.Count > 0 ? candidates[Random.Range(0, candidates.Count)] : null;
        }

        private static string Summarize(Dictionary<string, int> granted)
        {
            if (granted.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();

            foreach (KeyValuePair<string, int> entry in granted)
            {
                if (builder.Length > 0)
                {
                    builder.Append('\n');
                }

                builder.Append(entry.Value > 1 ? $"{entry.Key} x{entry.Value}" : entry.Key);
            }

            return builder.ToString();
        }

        private void ShowLabel(string summary)
        {
            if (lootLabel == null)
            {
                return;
            }

            lootLabel.text = string.IsNullOrEmpty(summary) ? "Vacío" : summary;
            lootLabel.enabled = true;

            Color color = lootLabel.color;
            color.a = 1f;
            lootLabel.color = color;

            labelTimer = Mathf.Max(0.5f, lootLabelSeconds);
        }

        /// <summary>Leaves the chest in the scene already opened and empty: nothing more to take.</summary>
        public void MarkAsEmptied()
        {
            opened = true;
            level = 0;
            indicator?.Hide();

            if (visual != null && openedSprite != null)
            {
                visual.sprite = openedSprite;
            }

            if (interactionZone != null)
            {
                interactionZone.gameObject.SetActive(false);
            }

            if (lootLabel != null)
            {
                lootLabel.enabled = false;
            }
        }

        private static Sprite PlaceholderSprite()
        {
            if (placeholderSprite == null)
            {
                Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "LootChest_WhitePixel",
                    filterMode = FilterMode.Point,
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();

                placeholderSprite = Sprite.Create(
                    texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                placeholderSprite.name = "LootChest_Placeholder";
                placeholderSprite.hideFlags = HideFlags.HideAndDontSave;
            }

            return placeholderSprite;
        }

        public bool CanInteractWith(PlayerMovementController player)
        {
            return !opened &&
                   !GameplayInputLock.IsLocked &&
                   player != null &&
                   player.AcceptsLocalInput &&
                   interactionZone != null &&
                   interactionZone.Contains(player);
        }

        public void InteractWith(PlayerMovementController player)
        {
            if (CanInteractWith(player))
            {
                Open();
            }
        }

        /// <summary>Used by the editor builder to wire the prefab.</summary>
        public void Bind(
            SpriteRenderer chestVisual,
            NPCInteractionZone zone,
            NPCInteractionIndicatorController indicatorController,
            TMP_Text label)
        {
            visual = chestVisual;
            interactionZone = zone;
            indicator = indicatorController;
            lootLabel = label;
        }
    }
}
