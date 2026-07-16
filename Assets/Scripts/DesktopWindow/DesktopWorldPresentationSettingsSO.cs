using UnityEngine;

namespace DeskSlayer.DesktopWindow
{
    /// <summary>
    /// GameWorldRoot 與常駐錨定按鈕共用的縮放設定。兩者最終需要縮小到適合擺在螢幕角落的尺寸，
    /// 但合理縮放比例是實機試玩後的主觀判斷，不是能預先算出正確答案的數值，因此獨立成
    /// ScriptableObject 讓兩邊（GameWorldDragCoordinator／AnchoredUISyncMover）讀同一份資產、
    /// 天然維持縮放比例一致，不需要各自調參再手動對齊。
    /// </summary>
    [CreateAssetMenu(fileName = "DesktopWorldPresentationSettings", menuName = "DeskSlayer/DesktopWindow/World Presentation Settings", order = 1)]
    public sealed class DesktopWorldPresentationSettingsSO : ScriptableObject
    {
        [SerializeField, Range(0.1f, 1f), Tooltip("GameWorldRoot 與常駐錨定按鈕共用的縮放倍率，起始預設值，實際大小由實機試玩調整")]
        private float _worldScale = 0.5f;

        public float WorldScale => _worldScale;
    }
}
