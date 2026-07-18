using UnityEngine;
using DeskSlayer.Combat;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 單一武器的「分享卡姿勢」設定：指定該武器截圖當下要強制跳到哪個動畫狀態、哪個畫格位置。
    /// 只唯讀參照既有的 WeaponDataSO 資產建立關聯，不在 WeaponDataSO 本身新增任何欄位。
    /// 新增一種武器的分享卡姿勢時，只需要新建一份這個資產、指定 TargetWeapon，
    /// 不需要修改任何既有程式邏輯或既有武器資料結構。
    /// </summary>
    [CreateAssetMenu(fileName = "ShareCardPose", menuName = "DeskSlayer/ShareCard/Pose", order = 1)]
    public sealed class ShareCardPoseSO : ScriptableObject
    {
        [SerializeField, Tooltip("此姿勢設定對應的武器資產（唯讀參照，不修改既有武器資料）")]
        private WeaponDataSO _targetWeapon;

        [SerializeField, Tooltip("武器揮擊 Animator 要強制跳轉的狀態名稱，比照 PlayerAttackVisualDispatcher 的既有慣例（預設 \"Attack\"）")]
        private string _weaponAnimationStateName = "Attack";

        [SerializeField, Tooltip("角色本體 Animator 要強制跳轉的狀態名稱，比照 PlayerAttackVisualDispatcher 的既有慣例（預設 \"Attack\"）")]
        private string _bodyAnimationStateName = "Attack";

        [SerializeField, Range(0f, 1f), Tooltip("要強制跳到的畫格位置（Animator 正規化時間，0~1，本體與武器共用同一個時間點），實際數值需在 Editor 內試播動畫比對畫格後手動微調")]
        private float _normalizedTime;

        /// <summary>此姿勢設定對應的武器資產。</summary>
        public WeaponDataSO TargetWeapon => _targetWeapon;

        /// <summary>武器揮擊 Animator 要強制跳轉的狀態名稱。</summary>
        public string WeaponAnimationStateName => _weaponAnimationStateName;

        /// <summary>角色本體 Animator 要強制跳轉的狀態名稱。</summary>
        public string BodyAnimationStateName => _bodyAnimationStateName;

        /// <summary>要強制跳到的畫格位置（Animator 正規化時間，0~1）。</summary>
        public float NormalizedTime => _normalizedTime;
    }
}
