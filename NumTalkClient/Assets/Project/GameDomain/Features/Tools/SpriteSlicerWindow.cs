using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.Tools
{
    /// <summary>
    /// Editor tool: pick a folder and grid-slice every sprite sheet inside it into
    /// uniform cells (default 32x32) with pixel-art import settings.
    /// Open via menu: IdleStory/Tools/Sprite Slicer.
    /// </summary>
    public sealed class SpriteSlicerWindow : EditorWindow
    {
        private DefaultAsset _folder;
        private int _cellSize = 32;
        private int _pixelsPerUnit = 32;
        private SpriteAlignment _pivot = SpriteAlignment.Center;
        private string _excludeFilter = "Mockup,Premade,Scene,Guideline";

        [MenuItem("IdleStory/Tools/Sprite Slicer")]
        private static void Open()
        {
            SpriteSlicerWindow window = GetWindow<SpriteSlicerWindow>("Sprite Slicer");
            window.minSize = new Vector2(340f, 220f);
            window.TryUseSelectedFolder();
        }

        private void OnEnable() => TryUseSelectedFolder();

        private void TryUseSelectedFolder()
        {
            if (_folder != null)
            {
                return;
            }

            string path = Selection.activeObject != null
                ? AssetDatabase.GetAssetPath(Selection.activeObject)
                : null;

            if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
            {
                _folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Grid-slice all sprites in a folder", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _folder = (DefaultAsset)EditorGUILayout.ObjectField("Folder", _folder, typeof(DefaultAsset), false);
            _cellSize = Mathf.Max(1, EditorGUILayout.IntField("Cell Size (px)", _cellSize));
            _pixelsPerUnit = Mathf.Max(1, EditorGUILayout.IntField("Pixels Per Unit", _pixelsPerUnit));
            _pivot = (SpriteAlignment)EditorGUILayout.EnumPopup("Pivot", _pivot);
            _excludeFilter = EditorGUILayout.TextField(
                new GUIContent("Exclude (name contains)", "Comma-separated. Skips any sheet whose path contains a term, e.g. mockups / premade scenes."),
                _excludeFilter);

            EditorGUILayout.Space();

            string folderPath = _folder != null ? AssetDatabase.GetAssetPath(_folder) : null;
            bool valid = !string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath);

            using (new EditorGUI.DisabledScope(!valid))
            {
                if (GUILayout.Button("Slice All Sprites In Folder", GUILayout.Height(32f)))
                {
                    SliceFolder(folderPath);
                }
            }

            if (!valid)
            {
                EditorGUILayout.HelpBox(
                    "Pick a project folder: drag it into the field above, or select it in the Project window and reopen this window.",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Slices every texture under:\n{folderPath}\ninto {_cellSize}x{_cellSize} cells (Point filter, uncompressed). Runs recursively.",
                    MessageType.None);
            }
        }

        private void SliceFolder(string folderPath)
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            string[] excludes = ParseExcludes(_excludeFilter);
            int sliced = 0;
            int skipped = 0;
            List<string> notDivisible = new();

            try
            {
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (IsExcluded(path, excludes))
                    {
                        skipped++;
                        continue;
                    }

                    bool cancel = EditorUtility.DisplayCancelableProgressBar(
                        "Slicing sprites",
                        $"{Path.GetFileName(path)} ({i + 1}/{guids.Length})",
                        guids.Length == 0 ? 1f : (float)i / guids.Length);

                    if (cancel)
                    {
                        break;
                    }

                    if (SliceTexture(path, notDivisible))
                    {
                        sliced++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }

            string warn = notDivisible.Count > 0
                ? $" | {notDivisible.Count} sheet(s) not divisible by {_cellSize}px (extra pixels ignored): " +
                  string.Join(", ", notDivisible.GetRange(0, Mathf.Min(8, notDivisible.Count))) + " ..."
                : string.Empty;

            Debug.Log($"[Sprite Slicer] Done. Sliced {sliced}, skipped {skipped} of {guids.Length} textures in '{folderPath}'.{warn}");
        }

        private bool SliceTexture(string path, List<string> notDivisible)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return false;
            }

            if (!TryReadPngSize(path, out int width, out int height))
            {
                return false;
            }

            // Decode pixels once so we can skip fully-transparent cells (no empty sprites).
            if (!TryLoadPixels(path, out Color32[] pixels, out int pxWidth, out int pxHeight)
                || pxWidth != width || pxHeight != height)
            {
                pixels = null;
            }

            int cols = width / _cellSize;
            int rows = height / _cellSize;
            if (cols == 0 || rows == 0)
            {
                return false;
            }

            if (width % _cellSize != 0 || height % _cellSize != 0)
            {
                notDivisible.Add(Path.GetFileNameWithoutExtension(path));
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = _pixelsPerUnit;
            importer.wrapMode = TextureWrapMode.Clamp;

            string baseName = Path.GetFileNameWithoutExtension(path);
            List<SpriteMetaData> metas = new(cols * rows);

            // Row-major, top-to-bottom (Unity rects use a bottom-left origin).
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int x = col * _cellSize;
                    int y = height - (row + 1) * _cellSize;

                    if (pixels != null && IsCellEmpty(pixels, width, x, y, _cellSize))
                    {
                        continue;
                    }

                    metas.Add(new SpriteMetaData
                    {
                        name = $"{baseName}_{row * cols + col}",
                        rect = new Rect(x, y, _cellSize, _cellSize),
                        alignment = (int)_pivot,
                        pivot = PivotValue(_pivot),
                    });
                }
            }

#pragma warning disable 618 // SpriteMetaData/spritesheet is obsolete but still the simplest reliable grid-slice path.
            importer.spritesheet = metas.ToArray();
#pragma warning restore 618

            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            return true;
        }

        private static string[] ParseExcludes(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return System.Array.Empty<string>();
            }

            List<string> terms = new();
            foreach (string part in raw.Split(','))
            {
                string term = part.Trim().ToLowerInvariant();
                if (term.Length > 0)
                {
                    terms.Add(term);
                }
            }

            return terms.ToArray();
        }

        private static bool IsExcluded(string assetPath, string[] terms)
        {
            string lower = assetPath.ToLowerInvariant();
            foreach (string term in terms)
            {
                if (lower.Contains(term))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 PivotValue(SpriteAlignment alignment) => alignment switch
        {
            SpriteAlignment.TopLeft => new Vector2(0f, 1f),
            SpriteAlignment.TopCenter => new Vector2(0.5f, 1f),
            SpriteAlignment.TopRight => new Vector2(1f, 1f),
            SpriteAlignment.LeftCenter => new Vector2(0f, 0.5f),
            SpriteAlignment.RightCenter => new Vector2(1f, 0.5f),
            SpriteAlignment.BottomLeft => new Vector2(0f, 0f),
            SpriteAlignment.BottomCenter => new Vector2(0.5f, 0f),
            SpriteAlignment.BottomRight => new Vector2(1f, 0f),
            _ => new Vector2(0.5f, 0.5f),
        };

        private static bool TryLoadPixels(string assetPath, out Color32[] pixels, out int width, out int height)
        {
            pixels = null;
            width = 0;
            height = 0;

            try
            {
                byte[] bytes = File.ReadAllBytes(Path.GetFullPath(assetPath));
                Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(bytes))
                {
                    Object.DestroyImmediate(texture);
                    return false;
                }

                pixels = texture.GetPixels32();
                width = texture.width;
                height = texture.height;
                Object.DestroyImmediate(texture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // GetPixels32 is row-major from the bottom-left, matching the bottom-left rect origin.
        private static bool IsCellEmpty(Color32[] pixels, int textureWidth, int x, int y, int cellSize)
        {
            for (int dy = 0; dy < cellSize; dy++)
            {
                int rowStart = (y + dy) * textureWidth + x;
                for (int dx = 0; dx < cellSize; dx++)
                {
                    if (pixels[rowStart + dx].a != 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool TryReadPngSize(string assetPath, out int width, out int height)
        {
            width = 0;
            height = 0;

            try
            {
                using FileStream fs = File.OpenRead(Path.GetFullPath(assetPath));
                byte[] header = new byte[24];
                if (fs.Read(header, 0, 24) != 24)
                {
                    return false;
                }

                // PNG: 8-byte signature, then IHDR length(4) + type(4) + width(4) + height(4), big-endian.
                width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
                height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
                return width > 0 && height > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}
