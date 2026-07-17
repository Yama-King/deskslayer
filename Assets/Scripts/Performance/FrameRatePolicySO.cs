using UnityEngine;

namespace DeskSlayer.Performance
{
    /// <summary>
    /// 桌面陪伴視窗的目標幀率數值設定。背景常駐是本專案的生存要求，沒有使用者互動時
    /// 沒有必要以螢幕更新率全速渲染、跑滿所有逐幀系統。三個數值都是「流暢度 vs 省電」的
    /// 手感取捨，不是能預先算出正解的數字，因此獨立成 ScriptableObject，之後依實機體驗
    /// 調整不需要重新編譯，比照 <see cref="DeskSlayer.DesktopWindow.DesktopWorldPresentationSettingsSO"/>
    /// 的既有慣例。
    /// </summary>
    [CreateAssetMenu(fileName = "FrameRatePolicy", menuName = "DeskSlayer/Performance/Frame Rate Policy", order = 1)]
    public sealed class FrameRatePolicySO : ScriptableObject
    {
        [SerializeField, Range(15, 144), Tooltip("視窗有焦點、或偵測到使用者最近仍在互動（打字/拖曳/縮放）時的目標幀率")]
        private int _activeTargetFrameRate = 60;

        [SerializeField, Range(5, 30), Tooltip("視窗無焦點且超過閒置門檻秒數沒有任何互動時的目標幀率，數值越低越省 CPU/GPU")]
        private int _idleTargetFrameRate = 15;

        [SerializeField, Range(2f, 60f), Tooltip("視窗無焦點時，最後一次互動之後要經過幾秒才視為閒置並降頻")]
        private float _idleThresholdSeconds = 8f;

        public int ActiveTargetFrameRate => _activeTargetFrameRate;
        public int IdleTargetFrameRate => _idleTargetFrameRate;
        public float IdleThresholdSeconds => _idleThresholdSeconds;
    }
}
