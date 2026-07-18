using UnityEngine;
using DeskSlayer.Combat;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 所有武器分享卡姿勢設定的目錄資產，供分享卡生成流程依「目前裝備武器」查找對應姿勢。
    /// 新增武器的姿勢設定時，只需要把新的 ShareCardPoseSO 資產拖進這裡的陣列，不需要修改程式碼。
    /// </summary>
    [CreateAssetMenu(fileName = "ShareCardPoseDatabase", menuName = "DeskSlayer/ShareCard/Pose Database", order = 2)]
    public sealed class ShareCardPoseDatabaseSO : ScriptableObject
    {
        [SerializeField]
        private ShareCardPoseSO[] _allPoses;

        /// <summary>所有武器分享卡姿勢設定。</summary>
        public ShareCardPoseSO[] AllPoses => _allPoses;

        /// <summary>
        /// 查找指定武器對應的分享卡姿勢設定。找不到時回傳 false，呼叫端應維持角色目前姿勢並記錄警告，
        /// 不得因此中斷分享卡生成流程。
        /// </summary>
        public bool TryGetPose(WeaponDataSO weapon, out ShareCardPoseSO pose)
        {
            pose = null;

            if (weapon == null || _allPoses == null)
            {
                return false;
            }

            foreach (ShareCardPoseSO candidate in _allPoses)
            {
                if (candidate != null && candidate.TargetWeapon == weapon)
                {
                    pose = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
