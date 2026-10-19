using System;
using UnityEngine;

namespace SocialProyectoCenigraf.Dialogue
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string speaker;
        [TextArea(2, 6)] public string text;
    }
    [Serializable]
    public sealed class DialogueSequence
    {
        public string id;
        public DialogueLine[] lines;
        public SocialProyectoCenigraf.Rewards.RewardDefinition completionReward;
    }
    [CreateAssetMenu(menuName = "Cenigraf/Dialogue Catalog", fileName = "DialogueCatalog")]
    public sealed class DialogueCatalog : ScriptableObject
    {
        [SerializeField] private DialogueSequence[] dialogues;
        public DialogueSequence[] Dialogues => dialogues;
        public bool TryGet(string id, out DialogueSequence result)
        {
            result = null;
            if (string.IsNullOrWhiteSpace(id) || dialogues == null) return false;
            foreach (var entry in dialogues)
            {
                if (entry == null || entry.id != id) continue;
                if (result != null) { result = null; return false; }
                result = entry;
            }
            return result != null && result.lines != null && result.lines.Length > 0;
        }
    }
}
