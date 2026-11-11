using System;
using System.Text;
using System.Text.RegularExpressions;
using SocialProyectoCenigraf.Session.State;
using UnityEngine;
using UnityEngine.Serialization;

namespace SocialProyectoCenigraf.Dialogue
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string speaker;
        [TextArea(2, 6)] public string text;
    }

    [Serializable]
    public sealed class DialogueChoice
    {
        [Tooltip("Key opcional para respuestas basadas en identificadores; si no hay texto, se convierte de CamelCase a texto visible.")]
        public string key;
        [Tooltip("Texto que verá y seleccionará el jugador.")]
        public string text;
        [Tooltip("ID de línea destino o *END* para cerrar el diálogo.")]
        public string nextNodeId;

        public string DisplayText => !string.IsNullOrWhiteSpace(text)
            ? text.Trim()
            : DialogueTextUtility.FromKey(key);

        public string TargetLineId => string.IsNullOrWhiteSpace(nextNodeId)
            ? DialogueSequence.EndToken
            : nextNodeId.Trim();
    }

    public enum DialogueConditionType
    {
        ItemOwned,
        MissionActive,
        TalkedToCharacter,
        DialogueCompleted,
        MissionCompleted,
        DialogueNodeVisited
    }

    [Serializable]
    public sealed class DialogueConditionalRoute
    {
        public DialogueConditionType conditionType;
        [Tooltip("ID del ítem, misión, personaje o diálogo que se consulta.")]
        public string conditionId;
        [Tooltip("ID de la línea destino si se cumple la condición.")]
        public string targetNodeId;
    }

    public static class DialogueProgress
    {
        public static event Action<string> DialogueCompleted;

        private static readonly System.Collections.Generic.HashSet<string> completedDialogues =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        private static readonly System.Collections.Generic.HashSet<string> talkedCharacters =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        private static readonly System.Collections.Generic.HashSet<string> visitedNodes =
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            completedDialogues.Clear();
            talkedCharacters.Clear();
            visitedNodes.Clear();
        }

        public static bool HasCompletedDialogue(string id) =>
            !string.IsNullOrWhiteSpace(id) && completedDialogues.Contains(id.Trim());

        public static bool HasTalkedToCharacter(string id) =>
            !string.IsNullOrWhiteSpace(id) && talkedCharacters.Contains(id.Trim());

        public static bool HasVisitedNode(string key) =>
            !string.IsNullOrWhiteSpace(key) && visitedNodes.Contains(key.Trim());

        internal static void RecordNodeReached(string dialogueId, string nodeId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId) || string.IsNullOrWhiteSpace(nodeId)) return;
            visitedNodes.Add(NodeVisitKey(dialogueId, nodeId));
        }

        public static string NodeVisitKey(string dialogueId, string nodeId) =>
            $"{dialogueId?.Trim()}|{nodeId?.Trim()}";

        internal static void RecordCompleted(DialogueSequence sequence)
        {
            if (sequence == null) return;
            if (!string.IsNullOrWhiteSpace(sequence.id))
            {
                string id = sequence.id.Trim();
                completedDialogues.Add(id);
                DialogueCompleted?.Invoke(id);
            }
            if (!string.IsNullOrWhiteSpace(sequence.speakerCharacterId))
                talkedCharacters.Add(sequence.speakerCharacterId.Trim());
        }
    }

    [Serializable]
    public sealed class DialogueNode
    {
        [Tooltip("ID único dentro de esta conversación.")]
        public string id;
        public string speaker;
        [TextArea(2, 6)] public string text;
        [Tooltip("Siguiente línea automática. Usa *END* para cerrar el diálogo.")]
        public string nextNodeId;
        [Tooltip("Default puede definir el avance automático; las demás keys se muestran como botones y apuntan a otra línea o *END*.")]
        [FormerlySerializedAs("choices")]
        public DialogueChoice[] options;
        [Tooltip("Rutas condicionales evaluadas al continuar esta línea. Se usa la primera condición verdadera; si ninguna se cumple, se usa el destino por defecto.")]
        public DialogueConditionalRoute[] conditionalRoutes;
        [Tooltip("Recompensa que se entrega al cerrar esta línea con *END*. Tiene prioridad sobre la recompensa general del diálogo.")]
        public SocialProyectoCenigraf.Rewards.RewardDefinition completionReward;
        [Tooltip("Catálogo e ítem que se agregan al inventario si esta línea termina el diálogo.")]
        public SocialProyectoCenigraf.Items.ItemCatalog completionItemCatalog;
        public string completionItemId;
        [Min(1)] public int completionItemQuantity = 1;
        [Tooltip("Escena a la que viaja el jugador si esta línea termina el diálogo, con la misma pantalla de carga de los teleports. Vacío no viaja.")]
        public string travelSceneName;
        [Tooltip("ID del teleport de llegada en esa escena. Vacío deja al jugador en el punto que la escena define.")]
        public string travelTeleportId;

        public bool HasConditionalRoutes => conditionalRoutes != null && conditionalRoutes.Length > 0;

        public string DefaultTargetLineId
        {
            get
            {
                if (options != null)
                {
                    foreach (DialogueChoice choice in options)
                    {
                        if (choice != null && string.Equals(choice.key?.Trim(), "Default", StringComparison.OrdinalIgnoreCase))
                            return choice.TargetLineId;
                    }
                }
                return string.IsNullOrWhiteSpace(nextNodeId)
                    ? DialogueSequence.EndToken
                    : nextNodeId.Trim();
            }
        }

        public DialogueChoice[] VisibleChoices
        {
            get
            {
                if (options == null || options.Length == 0) return Array.Empty<DialogueChoice>();
                var visible = new System.Collections.Generic.List<DialogueChoice>(options.Length);
                foreach (DialogueChoice choice in options)
                {
                    if (choice == null || string.Equals(choice.key?.Trim(), "Default", StringComparison.OrdinalIgnoreCase))
                        continue;
                    visible.Add(choice);
                }
                return visible.ToArray();
            }
        }
    }

    [Serializable]
    public sealed class DialogueSequence
    {
        public const string EndToken = "*END*";
        public string id;
        [Tooltip("ID del personaje que habla. Se registra al completar el diálogo para condiciones Talked To Character.")]
        public string speakerCharacterId;
        [Tooltip("Un solo diálogo: ID de la primera línea/nodo que se mostrará. Si queda vacío, se usa el primer nodo de la lista.")]
        public string startNodeId;
        [Tooltip("Rutas condicionales evaluadas al abrir. Se usa la primera condición verdadera; si ninguna se cumple, se usa startNodeId.")]
        public DialogueConditionalRoute[] conditionalStartRoutes;
        [Tooltip("Líneas y respuestas conectadas que forman este único diálogo; no son diálogos independientes. Si se configura, reemplaza las líneas lineales.")]
        public DialogueNode[] nodes;
        [Tooltip("Formato lineal compatible con conversaciones antiguas.")]
        public DialogueLine[] lines;
        public SocialProyectoCenigraf.Rewards.RewardDefinition completionReward;
        [Tooltip("Catálogo de misiones que se consulta al terminar este diálogo.")]
        public SocialProyectoCenigraf.Missions.MissionCatalog missionCatalog;
        [Tooltip("ID de misión a activar al terminar. Déjalo vacío si el diálogo no inicia una misión.")]
        public string missionId;
        [Tooltip("Catálogo de ítems que se consulta para entregar un objeto al terminar el diálogo.")]
        public SocialProyectoCenigraf.Items.ItemCatalog itemCatalog;
        [Tooltip("ID del ítem que se agrega al inventario al terminar. Déjalo vacío si no entrega un objeto.")]
        public string completionItemId;
        [Min(1)] public int completionItemQuantity = 1;

        public bool UsesNodes => nodes != null && nodes.Length > 0;
    }

    internal static class DialogueTextUtility
    {
        public static string FromKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key)) return "(Respuesta sin texto)";
            string value = key.Trim();
            var result = new StringBuilder(value.Length + 8);
            for (int i = 0; i < value.Length; i++)
            {
                char current = value[i];
                if (i > 0 && char.IsUpper(current) &&
                    (char.IsLower(value[i - 1]) || (i + 1 < value.Length && char.IsLower(value[i + 1]))))
                    result.Append(' ');
                result.Append(current);
            }
            return result.ToString();
        }
    }

    public static class DialoguePlayerNameTokens
    {
        private static readonly Regex tokenPattern = new Regex(
            @"\*\s*(FULLNAME|LASTNAME|NAME)\s*\*",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static string Replace(string text)
        {
            if (string.IsNullOrEmpty(text)) return text ?? string.Empty;

            string fullName = Regex.Replace(
                GameSessionStore.Instance.State.PlayerName ?? string.Empty,
                @"\s+",
                " ").Trim();
            if (string.IsNullOrWhiteSpace(fullName)) fullName = "Aprendiz";

            string[] nameParts = fullName.Split(' ');
            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1
                ? string.Join(" ", nameParts, 1, nameParts.Length - 1)
                : string.Empty;

            return tokenPattern.Replace(text, match =>
            {
                string token = match.Groups[1].Value;
                if (string.Equals(token, "FULLNAME", StringComparison.OrdinalIgnoreCase)) return fullName;
                if (string.Equals(token, "LASTNAME", StringComparison.OrdinalIgnoreCase)) return lastName;
                return firstName;
            });
        }
    }

    public static class DialogueCompletion
    {
        public static void Apply(
            DialogueSequence sequence,
            SocialProyectoCenigraf.Player.Movement.PlayerMovementController recipient,
            DialogueNode terminalNode = null)
        {
            if (sequence == null) return;
            DialogueProgress.RecordCompleted(sequence);
            if (sequence.missionCatalog != null && !string.IsNullOrWhiteSpace(sequence.missionId))
                SocialProyectoCenigraf.Missions.MissionJournal.TryActivate(sequence.missionCatalog, sequence.missionId.Trim());
            if (sequence.itemCatalog != null && !string.IsNullOrWhiteSpace(sequence.completionItemId))
                SocialProyectoCenigraf.Items.PlayerItemInventory.TryGrant(
                    sequence.itemCatalog,
                    sequence.completionItemId.Trim(),
                    Mathf.Max(1, sequence.completionItemQuantity));
            if (terminalNode?.completionItemCatalog != null && !string.IsNullOrWhiteSpace(terminalNode.completionItemId))
                SocialProyectoCenigraf.Items.PlayerItemInventory.TryGrant(
                    terminalNode.completionItemCatalog,
                    terminalNode.completionItemId.Trim(),
                    Mathf.Max(1, terminalNode.completionItemQuantity));
            SocialProyectoCenigraf.Rewards.RewardDefinition reward =
                terminalNode?.completionReward != null ? terminalNode.completionReward : sequence.completionReward;
            bool rewardShown = reward != null &&
                SocialProyectoCenigraf.Rewards.RewardWindow.GrantAndShow(reward, recipient);

            string travelScene = terminalNode != null
                ? (terminalNode.travelSceneName ?? string.Empty).Trim()
                : string.Empty;
            if (travelScene.Length == 0) return;

            // Travelling would unload the scene that holds the reward window, so a line
            // that does both is a mistake worth naming instead of a silent no-op.
            if (rewardShown)
            {
                Debug.LogWarning(
                    $"El diálogo '{sequence.id}' entrega una recompensa y además viaja a '{travelScene}'. Se omite el viaje para no cerrar la ventana de recompensa.");
                return;
            }

            SocialProyectoCenigraf.World.Teleport.TeleportTravel.Begin(
                travelScene,
                (terminalNode.travelTeleportId ?? string.Empty).Trim());
        }
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
            bool hasValidContent = result != null && (result.UsesNodes
                ? IsNodeDialogueValid(result)
                : result.lines != null && result.lines.Length > 0);
            if (!hasValidContent) return false;
            return string.IsNullOrWhiteSpace(result.completionItemId) ||
                   (result.itemCatalog != null &&
                    result.itemCatalog.TryGet(result.completionItemId.Trim(), out _));
        }

        private bool IsNodeDialogueValid(DialogueSequence sequence)
        {
            if (!TryGetStartNode(sequence, out _)) return false;
            if (sequence.conditionalStartRoutes != null)
            {
                foreach (DialogueConditionalRoute route in sequence.conditionalStartRoutes)
                {
                    if (route == null || string.IsNullOrWhiteSpace(route.conditionId) ||
                        string.IsNullOrWhiteSpace(route.targetNodeId) ||
                        !TryGetNode(sequence, route.targetNodeId, out _)) return false;
                }
            }
            var nodeIds = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (DialogueNode node in sequence.nodes)
            {
                if (node == null || string.IsNullOrWhiteSpace(node.id) ||
                    !nodeIds.Add(node.id.Trim())) return false;
                if (!IsValidTarget(sequence, node.DefaultTargetLineId)) return false;
                if (node.options != null)
                {
                    foreach (DialogueChoice choice in node.options)
                    {
                        if (choice != null && !IsValidTarget(sequence, choice.TargetLineId)) return false;
                    }
                }
                if (node.conditionalRoutes != null)
                {
                    foreach (DialogueConditionalRoute route in node.conditionalRoutes)
                    {
                        if (route == null || string.IsNullOrWhiteSpace(route.conditionId) ||
                            string.IsNullOrWhiteSpace(route.targetNodeId) ||
                            !IsValidTarget(sequence, route.targetNodeId)) return false;
                    }
                }
                if (!string.IsNullOrWhiteSpace(node.completionItemId) &&
                    (node.completionItemCatalog == null ||
                     !node.completionItemCatalog.TryGet(node.completionItemId.Trim(), out _))) return false;
            }
            return true;
        }

        private bool IsValidTarget(DialogueSequence sequence, string targetId) =>
            string.IsNullOrWhiteSpace(targetId) ||
            string.Equals(targetId.Trim(), DialogueSequence.EndToken, StringComparison.OrdinalIgnoreCase) ||
            TryGetNode(sequence, targetId, out _);

        public bool TryGetStartNode(DialogueSequence sequence, out DialogueNode node)
        {
            node = null;
            if (sequence?.nodes == null || sequence.nodes.Length == 0) return false;
            string startId = string.IsNullOrWhiteSpace(sequence.startNodeId)
                ? sequence.nodes[0]?.id
                : sequence.startNodeId.Trim();
            return TryGetNode(sequence, startId, out node);
        }

        public bool ResolveStartNode(DialogueSequence sequence, out DialogueNode node)
        {
            if (sequence?.conditionalStartRoutes != null)
            {
                foreach (DialogueConditionalRoute route in sequence.conditionalStartRoutes)
                {
                    if (route != null && IsConditionMet(route))
                        return TryGetNode(sequence, route.targetNodeId, out node);
                }
            }
            return TryGetStartNode(sequence, out node);
        }

        public bool TryGetNode(DialogueSequence sequence, string nodeId, out DialogueNode node)
        {
            node = null;
            if (sequence?.nodes == null || string.IsNullOrWhiteSpace(nodeId)) return false;
            foreach (DialogueNode entry in sequence.nodes)
            {
                if (entry == null || !string.Equals(entry.id?.Trim(), nodeId.Trim(), StringComparison.Ordinal))
                    continue;
                if (node != null)
                {
                    node = null;
                    return false;
                }
                node = entry;
            }
            return node != null;
        }

        public string ResolveNextNode(DialogueSequence sequence, DialogueNode node)
        {
            if (node?.conditionalRoutes != null)
            {
                foreach (DialogueConditionalRoute route in node.conditionalRoutes)
                {
                    if (route != null && IsConditionMet(route)) return route.targetNodeId.Trim();
                }
            }
            return node?.DefaultTargetLineId ?? DialogueSequence.EndToken;
        }

        private static bool IsConditionMet(DialogueConditionalRoute route)
        {
            string id = route.conditionId?.Trim();
            switch (route.conditionType)
            {
                case DialogueConditionType.ItemOwned:
                    return SocialProyectoCenigraf.Items.PlayerItemInventory.HasItem(id);
                case DialogueConditionType.MissionActive:
                    return SocialProyectoCenigraf.Missions.MissionJournal.IsActive(id);
                case DialogueConditionType.MissionCompleted:
                    return SocialProyectoCenigraf.Missions.MissionJournal.IsCompleted(id);
                case DialogueConditionType.TalkedToCharacter:
                    return DialogueProgress.HasTalkedToCharacter(id);
                case DialogueConditionType.DialogueCompleted:
                    return DialogueProgress.HasCompletedDialogue(id);
                case DialogueConditionType.DialogueNodeVisited:
                    return DialogueProgress.HasVisitedNode(id);
                default:
                    return false;
            }
        }
    }
}
