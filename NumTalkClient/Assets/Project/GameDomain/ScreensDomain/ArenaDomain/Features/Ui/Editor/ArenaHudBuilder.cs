using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Project.GameDomain.ScreensDomain.ArenaDomain.Features.Ui.Editor
{
    public static class ArenaHudBuilder
    {
        public static Text Label(Transform parent, string name, string text, Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize; label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor; rect.anchoredPosition = position; rect.sizeDelta = size;
            var shadow = go.AddComponent<Shadow>(); shadow.effectColor = new Color(0.08f, 0.18f, 0.22f, 0.75f); shadow.effectDistance = new Vector2(0, -2);
            return label;
        }

        public static void Build()
        {
            const string path = "Assets/Project/GameDomain/ScreensDomain/ArenaDomain/Prefabs/ArenaScreen.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            var hud = root.GetComponentInChildren<Project.GameDomain.ScreensDomain.ArenaDomain.Features.Ui.Scripts.ArenaHudView>(true);
            var data = new SerializedObject(hud);
            var panel = (GameObject)data.FindProperty("_completePanel").objectReferenceValue;
            Transform canvas = root.transform.Find("StyledHud");
            if (canvas == null)
            {
                var go = new GameObject("StyledHud", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                go.transform.SetParent(root.transform, false);
                go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                go.GetComponent<Canvas>().sortingOrder = 101;
                var scaler = go.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
                canvas = go.transform;
            }
            hud.transform.SetParent(canvas, false);
            panel.transform.SetParent(canvas, false);
            Rect(hud.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(30, -26), new Vector2(234, 78), new Vector2(0, 1));
            var bg = hud.GetComponent<Image>();
            if (bg == null) bg = hud.gameObject.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.17f, 0.22f, 0.88f); bg.raycastTarget = false;
            bg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"); bg.type = Image.Type.Sliced;
            Sprite heart = Heart();
            var pips = data.FindProperty("_lifePips");
            for (int i = 0; i < pips.arraySize; i++)
            {
                var pip = (Image)pips.GetArrayElementAtIndex(i).objectReferenceValue;
                pip.sprite = heart; pip.color = new Color(1f, 0.43f, 0.38f); pip.raycastTarget = false;
                Rect(pip.rectTransform, new Vector2(0, 1), new Vector2(30 + i * 32, -23), new Vector2(24, 24), new Vector2(0.5f, 0.5f));
            }
            var coins = (Text)data.FindProperty("_coinLabel").objectReferenceValue;
            Rect(coins.rectTransform, new Vector2(0, 1), new Vector2(18, -43), new Vector2(195, 24), new Vector2(0, 1));
            coins.fontSize = 21; coins.fontStyle = FontStyle.Bold; coins.alignment = TextAnchor.MiddleLeft;
            coins.color = new Color(1f, 0.82f, 0.30f); coins.raycastTarget = false;
            data.FindProperty("_filledColor").colorValue = new Color(1f, 0.43f, 0.38f);
            data.FindProperty("_emptyColor").colorValue = new Color(0.35f, 0.44f, 0.47f, 0.7f);
            data.ApplyModifiedPropertiesWithoutUndo();
            foreach (string name in new[] { "CourseTitle", "CourseSubtitle", "MoveHint", "JumpHint" })
            {
                var old = canvas.Find(name); if (old != null) Object.DestroyImmediate(old.gameObject);
            }
            Label(canvas, "CourseTitle", "SKYBOUND", new Vector2(1, 1), new Vector2(-143, -40), new Vector2(230, 35), 28);
            var subtitle = Label(canvas, "CourseSubtitle", "THE FLOATING TRAIL", new Vector2(1, 1), new Vector2(-143, -68), new Vector2(230, 24), 13);
            subtitle.color = new Color(0.17f, 0.32f, 0.38f);
            Label(canvas, "MoveHint", "DRAG TO MOVE", new Vector2(0, 0), new Vector2(165, 28), new Vector2(280, 30), 15);
            Label(canvas, "JumpHint", "HOLD TO JUMP", new Vector2(1, 0), new Vector2(-165, 28), new Vector2(280, 30), 15);
            ConfigureCompletionPanel(panel);
            panel.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        [MenuItem("NumTalk/Update Finish Banner")]
        public static void UpdateFinishBanner()
        {
            const string path = "Assets/Project/GameDomain/ScreensDomain/ArenaDomain/Prefabs/ArenaScreen.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var hud = root.GetComponentInChildren<Project.GameDomain.ScreensDomain.ArenaDomain.Features.Ui.Scripts.ArenaHudView>(true);
                var data = new SerializedObject(hud);
                ConfigureCompletionPanel((GameObject)data.FindProperty("_completePanel").objectReferenceValue);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigureCompletionPanel(GameObject panel)
        {
            Rect(panel.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0, -26), new Vector2(430, 76), new Vector2(0.5f, 1));
            var background = panel.GetComponent<Image>();
            background.color = new Color(0.04f, 0.13f, 0.19f, 0.86f);
            background.raycastTarget = false;
            foreach (var button in panel.GetComponentsInChildren<Button>(true)) button.gameObject.SetActive(false);
            foreach (var text in panel.GetComponentsInChildren<Text>(true))
            {
                text.raycastTarget = false;
                if (text.name != "Title") continue;
                text.text = "TRAIL COMPLETE!";
                text.color = new Color(1f, 0.85f, 0.35f);
                text.fontStyle = FontStyle.Bold; text.fontSize = 31;
                Rect(text.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(410, 64), new Vector2(0.5f, 0.5f));
            }
        }

        private static void Rect(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size, Vector2 pivot)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = pivot;
            rect.anchoredPosition = position; rect.sizeDelta = size; rect.localScale = Vector3.one;
        }

        private static Sprite Heart()
        {
            const string path = "Assets/Project/GameDomain/ScreensDomain/ArenaDomain/Features/Ui/Art/Heart.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float a = (x - 31.5f) / 26f, b = (y - 29f) / 26f;
                    float f = a*a + b*b - 1f;
                    float edge = f*f*f - a*a*b*b*b;
                    texture.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - edge * 24f)));
                }
                texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite; importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false; importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
