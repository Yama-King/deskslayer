using DeskSlayer.Analytics;
using DeskSlayer.ShareCard;
using UnityEditor;
using UnityEngine;

namespace DeskSlayer.EditorTools
{
    /// <summary>
    /// 開發用戰鬥風格測試工具：Play Mode 下按一個按鈕就能把 PlayStyleAnalyzer 的風格分數強制設成
    /// 落在指定原型象限內的數值，立即測試五種分享卡風格揭曉畫面，不需要真的打字磨出對應的節奏/
    /// 輕重攻擊分數組合。象限座標直接讀取場景裡實際使用的 StyleArchetypeThresholdConfigSO，
    /// 門檻之後如果調整，這裡的測試座標會跟著變、不會用寫死的數字跟真正的判定邏輯脫鉤。
    /// 呼叫 PlayStyleAnalyzer.DebugForceScores（僅 Editor 下才存在）覆蓋顯示端讀到的分數，不觸碰
    /// PlayStyleProfile 內部狀態或存檔資料，關閉覆蓋後分數會恢復成真實打字/攻擊累積出來的數值。
    /// 放在 Editor 資料夾底下，Unity 會自動排除在正式版本之外，不會被打包進 itch.io 的下載版 exe。
    /// </summary>
    public sealed class StyleArchetypeDebugWindow : EditorWindow
    {
        private StyleArchetypeThresholdConfigSO _thresholdConfig;

        [MenuItem("DeskSlayer/Debug/Style Archetype Test Tool")]
        private static void Open()
        {
            GetWindow<StyleArchetypeDebugWindow>("戰鬥風格測試工具");
        }

        private void OnEnable()
        {
            if (_thresholdConfig == null)
            {
                _thresholdConfig = FindFirstAsset<StyleArchetypeThresholdConfigSO>();
            }
        }

        private void OnGUI()
        {
            _thresholdConfig = (StyleArchetypeThresholdConfigSO)EditorGUILayout.ObjectField(
                "Style Archetype Threshold Config", _thresholdConfig, typeof(StyleArchetypeThresholdConfigSO), false);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("需要進入 Play Mode 才能強制設定分數，PlayStyleAnalyzer 只在執行期存在。", MessageType.Info);
                return;
            }

            PlayStyleAnalyzer analyzer = Object.FindFirstObjectByType<PlayStyleAnalyzer>();
            if (analyzer == null)
            {
                EditorGUILayout.HelpBox("場景中找不到執行中的 PlayStyleAnalyzer 實例。", MessageType.Warning);
                return;
            }

            if (_thresholdConfig == null)
            {
                EditorGUILayout.HelpBox("請指定 StyleArchetypeThresholdConfigSO 資產。", MessageType.Warning);
                return;
            }

            float bandMin = _thresholdConfig.BalancedBandwidthMin;
            float bandMax = _thresholdConfig.BalancedBandwidthMax;
            float split = _thresholdConfig.QuadrantSplitValue;
            float bandMid = (bandMin + bandMax) * 0.5f;
            float highMid = (split + 100f) * 0.5f;
            float lowMid = split * 0.5f;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("點一下按鈕，立即強制設定「當日」與「累積」分數（分享卡兩個頁籤都會受影響）：", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            DrawArchetypeButton("平衡型 Balanced", analyzer, bandMid, bandMid);
            DrawArchetypeButton("敏捷刺客型 AgileAssassin（節奏穩定高＋輕攻擊傾向高）", analyzer, highMid, highMid);
            DrawArchetypeButton("沉穩重砲型 SteadyHeavy（節奏穩定高＋輕攻擊傾向低）", analyzer, lowMid, highMid);
            DrawArchetypeButton("靈活遊擊型 FlexibleGuerrilla（節奏穩定低＋輕攻擊傾向高）", analyzer, highMid, lowMid);
            DrawArchetypeButton("狂戰士型 Berserker（節奏穩定低＋輕攻擊傾向低）", analyzer, lowMid, lowMid);

            EditorGUILayout.Space();
            if (GUILayout.Button("清除強制設定，恢復真實分數"))
            {
                analyzer.DebugClearForcedScores();
            }

            EditorGUILayout.Space();
            StyleArchetypeId currentArchetype = StyleArchetypeClassifier.Classify(
                analyzer.DailyRhythmStabilityScore, analyzer.DailyLightAttackTendencyScore, _thresholdConfig);
            EditorGUILayout.HelpBox(
                $"目前分數（當日）：輕攻擊傾向={analyzer.DailyLightAttackTendencyScore:F1}　節奏穩定度={analyzer.DailyRhythmStabilityScore:F1}\n判定結果：{currentArchetype}",
                MessageType.None);
        }

        private static void DrawArchetypeButton(string label, PlayStyleAnalyzer analyzer, float lightScore, float rhythmScore)
        {
            if (GUILayout.Button(label))
            {
                analyzer.DebugForceScores(lightScore, rhythmScore);
            }
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
