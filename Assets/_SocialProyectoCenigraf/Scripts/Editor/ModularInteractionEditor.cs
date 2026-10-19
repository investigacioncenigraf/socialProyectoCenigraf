using System.Linq;
using SocialProyectoCenigraf.Dialogue;
using SocialProyectoCenigraf.Missions;
using SocialProyectoCenigraf.World;
using UnityEditor;
using UnityEngine;
namespace SocialProyectoCenigraf.Editor
{
    [CustomEditor(typeof(ModularInteraction))]
    public sealed class ModularInteractionEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject,"m_Script","dialogueId","activateMissionOnEnter","missionCatalog","missionId");
            var catalog=serializedObject.FindProperty("dialogueCatalog").objectReferenceValue as DialogueCatalog;
            var id=serializedObject.FindProperty("dialogueId");
            if(catalog != null)
            {
                var ids=(catalog.Dialogues ?? new DialogueSequence[0]).Where(d=>d!=null && !string.IsNullOrWhiteSpace(d.id)).Select(d=>d.id).Distinct().ToArray();
                var labels=new[]{"Sin diálogo"}.Concat(ids).ToArray();
                int index=System.Array.IndexOf(ids,id.stringValue)+1;
                EditorGUI.BeginChangeCheck();
                int selected=EditorGUILayout.Popup("Cadena de diálogo",index,labels);
                if(EditorGUI.EndChangeCheck())id.stringValue=selected==0?"":ids[selected-1];
                if(!string.IsNullOrEmpty(id.stringValue) && !catalog.TryGet(id.stringValue,out _))
                    EditorGUILayout.HelpBox("ID inexistente, duplicado o sin líneas. Revisa el catálogo.",MessageType.Warning);
            }
            else EditorGUILayout.HelpBox("Asigna un Dialogue Catalog para habilitar el diálogo con E. Sin catálogo funciona solo el aviso de interacción.",MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Misión opcional", EditorStyles.boldLabel);
            var enabled=serializedObject.FindProperty("activateMissionOnEnter");
            EditorGUILayout.PropertyField(enabled,new GUIContent("Activar misión al entrar"));
            using(new EditorGUI.DisabledScope(!enabled.boolValue))
            {
                var catalogProperty=serializedObject.FindProperty("missionCatalog");
                EditorGUILayout.PropertyField(catalogProperty,new GUIContent("Catálogo de misiones"));
                var missionCatalog=catalogProperty.objectReferenceValue as MissionCatalog;
                var missionId=serializedObject.FindProperty("missionId");
                if(missionCatalog!=null)
                {
                    var ids=(missionCatalog.Missions ?? new MissionDefinition[0]).Where(m=>m!=null && !string.IsNullOrWhiteSpace(m.id)).Select(m=>m.id).Distinct().ToArray();
                    var labels=new[]{"Seleccionar misión"}.Concat(ids).ToArray();
                    int index=System.Array.IndexOf(ids,missionId.stringValue)+1;
                    EditorGUI.BeginChangeCheck();
                    int selected=EditorGUILayout.Popup("Misión",index,labels);
                    if(EditorGUI.EndChangeCheck())missionId.stringValue=selected==0?"":ids[selected-1];
                    if(enabled.boolValue && !missionCatalog.TryGet(missionId.stringValue,out _))
                        EditorGUILayout.HelpBox("Selecciona una misión con ID único en el catálogo.",MessageType.Warning);
                }
                else if(enabled.boolValue) EditorGUILayout.HelpBox("Asigna un Mission Catalog. El diálogo funciona de forma independiente.",MessageType.Warning);
            }
            if(enabled.boolValue) EditorGUILayout.HelpBox("Se activa al entrar el jugador, sin pulsar E. Cada misión se registra una sola vez por partida y conserva su estado entre escenas.",MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
