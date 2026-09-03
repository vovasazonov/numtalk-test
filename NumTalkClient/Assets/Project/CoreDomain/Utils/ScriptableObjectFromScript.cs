#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.CoreDomain.Utils
{
    public static class ScriptableObjectFromScript
    {
        private const string MENU_PATH = "Assets/Create/ScriptableObject From Script";
        
        [MenuItem(MENU_PATH, true)]
        private static bool ValidateCreate()
        {
            return Selection.objects
                .OfType<MonoScript>()
                .Select(ms => ms.GetClass())
                .Any(t => t != null && t.IsSubclassOf(typeof(ScriptableObject)) && !t.IsAbstract);
        }

        [MenuItem(MENU_PATH)]
        private static void Create()
        {
            var scripts = Selection.objects.OfType<MonoScript>();
            var targetFolder = GetSelectedPathOrFallback();

            foreach (var ms in scripts)
            {
                var t = ms.GetClass();
                if (t == null || !t.IsSubclassOf(typeof(ScriptableObject)) || t.IsAbstract)
                {
                    continue;
                }
                
                var instance = ScriptableObject.CreateInstance(t);
                var path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(targetFolder, $"{t.Name}.asset"));
                AssetDatabase.CreateAsset(instance, path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
        }

        private static string GetSelectedPathOrFallback()
        {
            var path = "Assets";
            foreach (var obj in Selection.GetFiltered(typeof(Object), SelectionMode.Assets))
            {
                var p = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(p)) continue;

                if (Directory.Exists(p)) return p;

                var dir = Path.GetDirectoryName(p);
                if (!string.IsNullOrEmpty(dir)) return dir.Replace("\\", "/");
            }

            return path;
        }
    }
}
#endif