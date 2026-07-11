using System.Reflection;
using DeskSlayer.Combat;
using UnityEditor;
using UnityEngine;

namespace DeskSlayer.EditorTools
{
    /// <summary>
    /// 開發用武器授予工具：Play Mode 下按一個按鈕就能讓 WeaponInventoryService.Instance 直接收下
    /// 指定武器，不需要實際打怪等機率掉落，方便快速測試合成／碎片等收集系統。放在 Editor 資料夾底下，
    /// Unity 會自動排除在正式版本之外，不會被打包進 itch.io 的下載版 exe。
    /// </summary>
    public sealed class WeaponDebugWindow : EditorWindow
    {
        private WeaponDatabaseSO _database;
        private WeaponDropConfigSO _dropConfig;
        private string _searchFilter = string.Empty;
        private Vector2 _scrollPosition;

        [MenuItem("DeskSlayer/Debug/Weapon Grant Tool")]
        private static void Open()
        {
            GetWindow<WeaponDebugWindow>("武器授予工具");
        }

        private void OnEnable()
        {
            if (_database == null)
            {
                _database = FindFirstAsset<WeaponDatabaseSO>();
            }

            if (_dropConfig == null)
            {
                _dropConfig = FindFirstAsset<WeaponDropConfigSO>();
            }
        }

        private void OnGUI()
        {
            _database = (WeaponDatabaseSO)EditorGUILayout.ObjectField("Weapon Database", _database, typeof(WeaponDatabaseSO), false);
            _dropConfig = (WeaponDropConfigSO)EditorGUILayout.ObjectField("Weapon Drop Config", _dropConfig, typeof(WeaponDropConfigSO), false);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("需要進入 Play Mode 才能授予武器，WeaponInventoryService.Instance 只在執行期存在。", MessageType.Info);
                return;
            }

            WeaponInventoryService inventoryService = WeaponInventoryService.Instance;
            if (inventoryService == null)
            {
                EditorGUILayout.HelpBox("場景中找不到執行中的 WeaponInventoryService 實例。", MessageType.Warning);
                return;
            }

            if (_database == null || _dropConfig == null)
            {
                EditorGUILayout.HelpBox("請指定 WeaponDatabaseSO 與 WeaponDropConfigSO 資產。", MessageType.Warning);
                return;
            }

            _searchFilter = EditorGUILayout.TextField("搜尋武器名稱", _searchFilter);

            EditorGUILayout.Space();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            foreach (WeaponDataSO weapon in _database.AllWeapons)
            {
                if (weapon == null || !MatchesFilter(weapon))
                {
                    continue;
                }

                DrawWeaponRow(inventoryService, _dropConfig, weapon);
            }

            EditorGUILayout.EndScrollView();
        }

        private bool MatchesFilter(WeaponDataSO weapon)
        {
            if (string.IsNullOrEmpty(_searchFilter))
            {
                return true;
            }

            return weapon.WeaponName.Contains(_searchFilter);
        }

        private static void DrawWeaponRow(WeaponInventoryService inventoryService, WeaponDropConfigSO dropConfig, WeaponDataSO weapon)
        {
            EditorGUILayout.BeginHorizontal("box");

            bool owned = inventoryService.IsOwned(weapon);
            string status = owned
                ? $"Lv.{inventoryService.GetOwnedInstance(weapon).UpgradeLevel}"
                : "未擁有";

            EditorGUILayout.LabelField(
                $"{weapon.WeaponName}（{weapon.Family}/{weapon.Rarity}/變體{weapon.Variant}）　{status}");

            if (GUILayout.Button("授予", GUILayout.Width(60)))
            {
                SimulateDrop(inventoryService, weapon);
            }

            if (GUILayout.Button("灌滿級", GUILayout.Width(60)))
            {
                ForceMaxLevel(inventoryService, weapon);
            }

            if (!owned)
            {
                int shardCount = inventoryService.GetShardCount(weapon.Family, weapon.Rarity);
                int shardCost = dropConfig.GetShardExchangeCost(weapon.Rarity);
                EditorGUILayout.LabelField($"碎片 {shardCount}/{shardCost}", GUILayout.Width(80));

                GUI.enabled = inventoryService.CanExchangeShard(weapon);
                if (GUILayout.Button("兌換", GUILayout.Width(60)))
                {
                    bool exchanged = inventoryService.TryExchangeShard(weapon);
                    Debug.Log($"[WeaponDebugWindow] 兌換「{weapon.WeaponName}」，結果：{exchanged}");
                }

                GUI.enabled = true;
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 完整比照 WeaponDropDispatcher.HandleDeath 的真實判斷順序：已封頂的武器改累積成碎片，
        /// 否則才走既有的新武器／重複品邏輯。單純呼叫 AddDrop 沒辦法測到碎片累積，因為封頂判斷
        /// 是掉落端的責任，不在 WeaponInventoryService.AddDrop 內部。
        /// </summary>
        private static void SimulateDrop(WeaponInventoryService inventoryService, WeaponDataSO weapon)
        {
            WeaponDropOutcome outcome;
            if (inventoryService.IsWeaponMaxed(weapon))
            {
                inventoryService.AddShard(weapon.Family, weapon.Rarity);
                outcome = WeaponDropOutcome.ShardConverted;
            }
            else
            {
                outcome = inventoryService.AddDrop(weapon);
            }

            Debug.Log($"[WeaponDebugWindow] 授予「{weapon.WeaponName}」，結果：{outcome}");
        }

        /// <summary>
        /// 略過合成材料消耗，直接把武器等級灌到封頂，方便測試「武器碎片」這類必須先讓武器封頂才會
        /// 觸發的系統。合成等級刻意沒有公開的 setter（只能用 LevelUp 一次加一級），這裡用反射直接改
        /// 內部欄位，僅限這個 Editor 專用工具使用，不影響正式的合成流程。
        /// </summary>
        private static void ForceMaxLevel(WeaponInventoryService inventoryService, WeaponDataSO weapon)
        {
            if (!inventoryService.IsOwned(weapon))
            {
                inventoryService.AddDrop(weapon);
            }

            WeaponInstance instance = inventoryService.GetOwnedInstance(weapon);
            FieldInfo levelField = typeof(WeaponInstance).GetField("_upgradeLevel", BindingFlags.NonPublic | BindingFlags.Instance);
            levelField.SetValue(instance, weapon.MaxUpgradeLevel);

            Debug.Log($"[WeaponDebugWindow] 「{weapon.WeaponName}」已強制灌到滿級 {weapon.MaxUpgradeLevel}");
        }

        private static T FindFirstAsset<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }
    }
}
