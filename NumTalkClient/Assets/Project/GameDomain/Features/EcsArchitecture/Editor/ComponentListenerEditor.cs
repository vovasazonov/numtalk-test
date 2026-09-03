using System.IO;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Project.GameDomain.Features.EcsArchitecture.Editor
{
    [CustomEditor(typeof(ComponentListener), true)]
    public sealed class ComponentListenerEditor : UnityEditor.Editor
    {
        private const string GroupName = "ComponentListeners";

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ComponentListener listener = (ComponentListener)target;

            GUILayout.Space(8);

            string prefabPath = AssetDatabase.GetAssetPath(listener);
            bool isPrefabAsset = !string.IsNullOrEmpty(prefabPath) && prefabPath.EndsWith(".prefab");

            using (new EditorGUI.DisabledScope(!isPrefabAsset))
            {
                if (GUILayout.Button("Register Addressable"))
                {
                    RegisterAddressable(prefabPath, listener.GetType().Name);
                }
            }

            if (!isPrefabAsset)
            {
                EditorGUILayout.HelpBox("Open the prefab asset to register it as addressable.", MessageType.Info);
            }
        }

        private static void RegisterAddressable(string prefabPath, string listenerTypeName)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            AddressableAssetGroup group = settings.FindGroup(GroupName);
            if (group == null)
            {
                group = settings.CreateGroup(
                    GroupName,
                    setAsDefaultGroup: false,
                    readOnly: false,
                    postEvent: true,
                    schemasToCopy: settings.DefaultGroup.Schemas);
            }

            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            string address = $"{listenerTypeName}.prefab";

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = address;

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, postEvent: true);
            AssetDatabase.SaveAssets();

            Debug.Log($"Registered '{Path.GetFileName(prefabPath)}' as addressable '{address}' in group '{GroupName}'.");
        }
    }
}
