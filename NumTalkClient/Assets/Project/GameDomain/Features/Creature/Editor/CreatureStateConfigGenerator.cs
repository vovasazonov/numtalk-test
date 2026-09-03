using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Project.GameDomain.Features.Creature.Scripts;
using UnityEditor;
using UnityEngine;

namespace Project.GameDomain.Features.Creature.Editor
{
    /// <summary>
    /// Generates one populated <see cref="CreatureStateConfig"/> asset per creature.
    /// For each creature folder under Sprites/, it matches a sheet to each CreatureState by
    /// filename, loads the sliced sub-sprites, and assigns rows to DownRight/DownLeft/UpRight/UpLeft.
    /// Also maintains a CreatureStateConfigDatabase asset for runtime Type -> config lookup.
    /// Run via menu: IdleStory/Features/Creature/Generate State Configs.
    /// </summary>
    public static class CreatureStateConfigGenerator
    {
        private const string SpritesRoot = "Assets/Project/GameDomain/Features/Creature/Sprites";
        private const string DataRoot = "Assets/Project/GameDomain/Features/Creature/Data";
        private const int Cell = 32;
        private const int DownRightRow = 0;
        private const int DownLeftRow = 1;
        private const int UpRightRow = 2;
        private const int UpLeftRow = 3;

        // state -> filename keyword. A sheet may serve several states: a single "FlyIdle"
        // sheet is matched by both Fly and Idle.
        private static readonly (CreatureState state, string keyword)[] StateKeywords =
        {
            (CreatureState.Fly, "Fly"),
            (CreatureState.Idle, "Idle"),
            (CreatureState.Walk, "Walk"),
            (CreatureState.Jump, "Jump"),
            (CreatureState.Attack, "Attack"),
            (CreatureState.Slash, "Slash"),
            (CreatureState.Thrust, "Thrust"),
            (CreatureState.Swing, "Swing"),
            (CreatureState.TwoHanded, "TwoHanded"),
            (CreatureState.Ranged, "Ranged"),
            (CreatureState.Dmg, "Dmg"),
            (CreatureState.Die, "Die"),
        };

        [MenuItem("IdleStory/Features/Creature/Generate State Configs")]
        private static void Generate()
        {
            EnsureDataFolder();

            CreatureType[] types = (CreatureType[])Enum.GetValues(typeof(CreatureType));
            int created = 0;
            int updated = 0;
            int missing = 0;
            List<CreatureStateConfig> all = new();

            try
            {
                AssetDatabase.StartAssetEditing();

                for (int i = 0; i < types.Length; i++)
                {
                    CreatureType type = types[i];
                    if (type == CreatureType.None)
                    {
                        continue;
                    }

                    EditorUtility.DisplayProgressBar("Generating creature configs", type.ToString(), (float)i / types.Length);

                    string folder = $"{SpritesRoot}/{type}";
                    if (!AssetDatabase.IsValidFolder(folder))
                    {
                        missing++;
                        Debug.LogWarning($"[CreatureConfig] No sprite folder for {type} (expected '{folder}').");
                        continue;
                    }

                    List<string> sheets = AssetDatabase.FindAssets("t:Texture2D", new[] { folder })
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .Distinct()
                        .ToList();

                    string assetPath = $"{DataRoot}/{type}.asset";
                    CreatureStateConfig config = AssetDatabase.LoadAssetAtPath<CreatureStateConfig>(assetPath);
                    bool isNew = config == null;
                    if (isNew)
                    {
                        config = ScriptableObject.CreateInstance<CreatureStateConfig>();
                    }

                    config.Type = type;
                    config.States = BuildStates(sheets);

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(config, assetPath);
                        created++;
                    }
                    else
                    {
                        EditorUtility.SetDirty(config);
                        updated++;
                    }

                    all.Add(config);
                }

                BuildDatabase(all);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[CreatureConfig] Done. Created {created}, updated {updated}, missing sprite folders {missing}.");
        }

        private static void BuildDatabase(List<CreatureStateConfig> configs)
        {
            string dbPath = $"{DataRoot}/CreatureStateConfigDatabase.asset";
            CreatureStateConfigDatabase db = AssetDatabase.LoadAssetAtPath<CreatureStateConfigDatabase>(dbPath);
            bool isNew = db == null;
            if (isNew)
            {
                db = ScriptableObject.CreateInstance<CreatureStateConfigDatabase>();
            }

            db.Configs = configs;

            if (isNew)
            {
                AssetDatabase.CreateAsset(db, dbPath);
            }
            else
            {
                EditorUtility.SetDirty(db);
            }
        }

        private static List<CreatureStateConfig.StateSprites> BuildStates(List<string> sheets)
        {
            List<CreatureStateConfig.StateSprites> result = new();

            foreach ((CreatureState state, string keyword) in StateKeywords)
            {
                CreatureStateConfig.StateSprites entry = new() { State = state, IsOneShot = IsOneShot(state) };

                string sheet = PickSheet(sheets, keyword, isShadowRequired: false);
                if (sheet != null)
                {
                    SplitRows(sheet, out Sprite[] downRight, out Sprite[] downLeft, out Sprite[] upRight, out Sprite[] upLeft);
                    entry.DownRight = downRight;
                    entry.DownLeft = downLeft;
                    entry.UpRight = upRight;
                    entry.UpLeft = upLeft;
                }

                string shadowSheet = PickSheet(sheets, keyword, isShadowRequired: true) ?? PickShadowForBody(sheets, sheet);
                if (shadowSheet != null)
                {
                    SplitRows(shadowSheet, out Sprite[] shadowDownRight, out Sprite[] shadowDownLeft, out Sprite[] shadowUpRight, out Sprite[] shadowUpLeft);
                    entry.ShadowDownRight = shadowDownRight;
                    entry.ShadowDownLeft = shadowDownLeft;
                    entry.ShadowUpRight = shadowUpRight;
                    entry.ShadowUpLeft = shadowUpLeft;
                }

                result.Add(entry);
            }

            ApplyAttackFallback(result);
            ApplyMovementFallback(result);

            // Store in CreatureState order for a tidy inspector.
            result.Sort((a, b) => ((int)a.State).CompareTo((int)b.State));
            return result;
        }

        // Weapon-type attacks (Slash/Thrust/Swing/TwoHanded/Ranged) reuse the base Attack
        // animation for any creature that has no dedicated sheet for that attack type.
        private static void ApplyAttackFallback(List<CreatureStateConfig.StateSprites> states)
        {
            CreatureStateConfig.StateSprites baseAttack = states.Find(e => e.State == CreatureState.Attack);
            if (baseAttack == null || baseAttack.DownRight.Length == 0)
            {
                return;
            }

            foreach (CreatureStateConfig.StateSprites entry in states)
            {
                if (!IsWeaponAttack(entry.State) || entry.DownRight.Length > 0)
                {
                    continue;
                }

                entry.DownRight = baseAttack.DownRight;
                entry.DownLeft = baseAttack.DownLeft;
                entry.UpRight = baseAttack.UpRight;
                entry.UpLeft = baseAttack.UpLeft;
                entry.ShadowDownRight = baseAttack.ShadowDownRight;
                entry.ShadowDownLeft = baseAttack.ShadowDownLeft;
                entry.ShadowUpRight = baseAttack.ShadowUpRight;
                entry.ShadowUpLeft = baseAttack.ShadowUpLeft;
            }
        }

        // Creatures without a dedicated Walk sheet reuse Fly (flying creatures) or Jump
        // (hopping creatures); creatures without a Jump sheet reuse Walk.
        private static void ApplyMovementFallback(List<CreatureStateConfig.StateSprites> states)
        {
            CreatureStateConfig.StateSprites walk = states.Find(e => e.State == CreatureState.Walk);
            CreatureStateConfig.StateSprites jump = states.Find(e => e.State == CreatureState.Jump);
            CreatureStateConfig.StateSprites fly = states.Find(e => e.State == CreatureState.Fly);

            if (IsEmpty(walk))
            {
                if (!IsEmpty(fly))
                {
                    CopyFrames(fly, walk);
                }
                else if (!IsEmpty(jump))
                {
                    CopyFrames(jump, walk);
                }
            }

            if (IsEmpty(jump) && !IsEmpty(walk))
            {
                CopyFrames(walk, jump);
            }
        }

        private static bool IsEmpty(CreatureStateConfig.StateSprites entry)
        {
            return entry == null || entry.DownRight.Length == 0;
        }

        private static void CopyFrames(CreatureStateConfig.StateSprites from, CreatureStateConfig.StateSprites to)
        {
            to.DownRight = from.DownRight;
            to.DownLeft = from.DownLeft;
            to.UpRight = from.UpRight;
            to.UpLeft = from.UpLeft;
            to.ShadowDownRight = from.ShadowDownRight;
            to.ShadowDownLeft = from.ShadowDownLeft;
            to.ShadowUpRight = from.ShadowUpRight;
            to.ShadowUpLeft = from.ShadowUpLeft;
        }

        private static bool IsWeaponAttack(CreatureState state)
        {
            return state == CreatureState.Slash
                || state == CreatureState.Thrust
                || state == CreatureState.Swing
                || state == CreatureState.TwoHanded
                || state == CreatureState.Ranged;
        }

        private static bool IsOneShot(CreatureState state)
        {
            return state == CreatureState.Dmg
                || state == CreatureState.Attack
                || state == CreatureState.Die
                || IsWeaponAttack(state);
        }

        private static string PickSheet(List<string> sheets, string keyword, bool isShadowRequired)
        {
            string best = null;
            int bestLen = int.MaxValue;

            foreach (string path in sheets)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                // Keep shadow sheets (Shadows/ subfolder) and body sheets in separate passes.
                bool isShadow = name.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase) >= 0;
                if (isShadow != isShadowRequired)
                {
                    continue;
                }

                // Prefer the "plainest" match (shortest name): "Attack" over "ChargedAttack", "Die" over "SoulDie".
                if (name.Length < bestLen)
                {
                    best = path;
                    bestLen = name.Length;
                }
            }

            return best;
        }

        // A sheet serving several states may have a shadow named after only part of the body
        // sheet name: body "BatFlyIdle" has shadow "BatFlyShadow". When no shadow matches the
        // state keyword, fall back to the shadow whose base name prefixes the body sheet name.
        private static string PickShadowForBody(List<string> sheets, string bodySheetPath)
        {
            if (bodySheetPath == null)
            {
                return null;
            }

            string bodyName = Path.GetFileNameWithoutExtension(bodySheetPath);
            string best = null;
            int bestLen = -1;

            foreach (string path in sheets)
            {
                string name = Path.GetFileNameWithoutExtension(path);
                int shadowIndex = name.IndexOf("Shadow", StringComparison.OrdinalIgnoreCase);
                if (shadowIndex < 0)
                {
                    continue;
                }

                string baseName = name[..shadowIndex];
                if (!bodyName.StartsWith(baseName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (baseName.Length > bestLen)
                {
                    best = path;
                    bestLen = baseName.Length;
                }
            }

            return best;
        }

        private static void SplitRows(string sheetPath, out Sprite[] downRight, out Sprite[] downLeft, out Sprite[] upRight, out Sprite[] upLeft)
        {
            downRight = Array.Empty<Sprite>();
            downLeft = Array.Empty<Sprite>();
            upRight = Array.Empty<Sprite>();
            upLeft = Array.Empty<Sprite>();

            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            if (tex == null)
            {
                return;
            }

            int cols = Mathf.Max(1, tex.width / Cell);
            int rows = Mathf.Max(1, tex.height / Cell);

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(sheetPath)
                .OfType<Sprite>()
                .OrderBy(s => ParseIndex(s.name))
                .ToArray();

            if (sprites.Length == 0)
            {
                return;
            }

            downRight = RowFrames(sprites, RowOrFirst(DownRightRow, rows), cols);
            downLeft = RowFrames(sprites, RowOrFirst(DownLeftRow, rows), cols);
            upRight = RowFrames(sprites, RowOrFirst(UpRightRow, rows), cols);
            upLeft = RowFrames(sprites, RowOrFirst(UpLeftRow, rows), cols);
        }

        private static int RowOrFirst(int row, int rows)
        {
            return row < rows ? row : 0;
        }

        private static Sprite[] RowFrames(Sprite[] sprites, int rowIndex, int cols)
        {
            List<Sprite> frames = new();
            int start = rowIndex * cols;
            for (int col = 0; col < cols; col++)
            {
                int idx = start + col;
                if (idx >= sprites.Length)
                {
                    break;
                }

                frames.Add(sprites[idx]);
            }

            return frames.ToArray();
        }

        private static int ParseIndex(string spriteName)
        {
            int underscore = spriteName.LastIndexOf('_');
            if (underscore >= 0 && int.TryParse(spriteName[(underscore + 1)..], out int idx))
            {
                return idx;
            }

            return 0;
        }

        private static void EnsureDataFolder()
        {
            if (AssetDatabase.IsValidFolder(DataRoot))
            {
                return;
            }

            string parent = Path.GetDirectoryName(DataRoot)!.Replace("\\", "/");
            AssetDatabase.CreateFolder(parent, Path.GetFileName(DataRoot));
        }
    }
}
