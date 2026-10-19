using System.Linq;
using SocialProyectoCenigraf.World.Teleport;
using UnityEditor;
using UnityEngine;
using SocialProyectoCenigraf.Roles;

namespace SocialProyectoCenigraf.Editor
{
    [CustomEditor(typeof(SceneTeleport))]
    public sealed class SceneTeleportEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "destinationScenePath", "restrictByRole", "roleCatalog", "excludedRoleIds", "accessDeniedMessage");
            SerializedProperty path = serializedObject.FindProperty("destinationScenePath");
            var current = AssetDatabase.LoadAssetAtPath<SceneAsset>(path.stringValue);
            EditorGUI.BeginChangeCheck();
            var selected = (SceneAsset)EditorGUILayout.ObjectField("Escena destino", current, typeof(SceneAsset), false);
            if (EditorGUI.EndChangeCheck()) path.stringValue = selected == null ? "" : AssetDatabase.GetAssetPath(selected);
            if (selected != null && !EditorBuildSettings.scenes.Any(s => s.path == path.stringValue && s.enabled))
            {
                EditorGUILayout.HelpBox("La escena destino debe estar habilitada en Build Profiles.", MessageType.Warning);
                if (GUILayout.Button("Habilitar escena destino en Build Profiles"))
                    EnableScene(path.stringValue);
            }
            EditorGUILayout.HelpBox("E dentro del Box Collider 2D activa el viaje. Ajusta Size y Offset en ese collider. El ID se busca solo en la escena destino; si falta o está duplicado se conserva su spawn por defecto.", MessageType.Info);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Permisos de acceso", EditorStyles.boldLabel);
            var restricted=serializedObject.FindProperty("restrictByRole");
            EditorGUILayout.PropertyField(restricted,new GUIContent("Restringir por rol"));
            using(new EditorGUI.DisabledScope(!restricted.boolValue))
            {
                var catalogProperty=serializedObject.FindProperty("roleCatalog");
                EditorGUILayout.PropertyField(catalogProperty,new GUIContent("Catálogo de roles"));
                var excluded=serializedObject.FindProperty("excludedRoleIds");
                var catalog=catalogProperty.objectReferenceValue as RoleCatalog;
                if(catalog!=null)
                {
                    EditorGUILayout.LabelField("Roles excluidos");
                    foreach(var role in catalog.Roles.Where(r=>r!=null))
                    {
                        int index=-1;
                        for(int i=0;i<excluded.arraySize;i++)if(RoleIds.Normalize(excluded.GetArrayElementAtIndex(i).stringValue)==role.Id) { index=i;break; }
                        bool excludedRoleSelected=EditorGUILayout.ToggleLeft(role.DisplayName+" ("+role.Id+")",index>=0);
                        if(excludedRoleSelected && index<0) { int next=excluded.arraySize;excluded.InsertArrayElementAtIndex(next);excluded.GetArrayElementAtIndex(next).stringValue=role.Id; }
                        if(!excludedRoleSelected && index>=0)excluded.DeleteArrayElementAtIndex(index);
                    }
                    for(int i=0;i<excluded.arraySize;i++)
                        if(catalog.FindById(excluded.GetArrayElementAtIndex(i).stringValue)==null)
                            EditorGUILayout.HelpBox("ID excluido fuera del catálogo: "+excluded.GetArrayElementAtIndex(i).stringValue,MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.PropertyField(excluded,new GUIContent("IDs de roles excluidos"),true);
                    if(restricted.boolValue)EditorGUILayout.HelpBox("Asigna un catálogo para seleccionar los roles por nombre.",MessageType.Info);
                }
                EditorGUILayout.PropertyField(serializedObject.FindProperty("accessDeniedMessage"),new GUIContent("Mensaje de acceso denegado"));
            }
            if(restricted.boolValue)EditorGUILayout.HelpBox("Los roles marcados no pueden viajar desde este teleport. Configura cada entrada que lleve a la zona restringida. Sin roles excluidos, todos pueden pasar.",MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
        private static void EnableScene(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            var existing = scenes.Find(s => s.path == path);
            if (existing == null) scenes.Add(new EditorBuildSettingsScene(path, true));
            else existing.enabled = true;
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        [MenuItem("GameObject/2D Object/Scene Teleport", false, 10)]
        private static void CreateTeleport(MenuCommand command)
        {
            var go = new GameObject("Teleport");
            Undo.RegisterCreatedObjectUndo(go, "Create teleport");
            GameObjectUtility.SetParentAndAlign(go, command.context as GameObject);
            var teleport=go.AddComponent<SceneTeleport>();
            var config=new SerializedObject(teleport);
            config.FindProperty("roleCatalog").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RoleCatalog>("Assets/_SocialProyectoCenigraf/Data/Roles/RoleCatalog.asset");
            config.ApplyModifiedPropertiesWithoutUndo();
            Selection.activeGameObject = go;
        }
    }
}
