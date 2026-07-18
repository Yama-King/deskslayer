using System;
using System.Collections;
using UnityEngine;
using Kirurobo;
using DeskSlayer.Combat;
using DeskSlayer.Visuals;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 分享卡截圖流程的唯一協調者：依序隱藏操作介面、強制目前裝備武器的角色姿勢、等待渲染完成、
    /// 擷取視窗實際螢幕區域，並保證流程中任一步驟失敗（找不到姿勢設定、擷取失敗、拋出例外）都會
    /// 在 finally 區塊內把操作介面與角色動畫復原，不會卡在「UI 被隱藏」或「動畫卡格」的異常狀態。
    ///
    /// 姿勢恢復刻意重用 PlayerAttackVisualDispatcher 既有的公開方法 OnAttackAnimationEnd()（隱藏兩把
    /// 武器視覺物件），不重造一套恢復邏輯；本體 Animator 的強制姿勢則額外用 Play() 明確跳回 Idle 狀態，
    /// 不依賴 Animator Controller 本身的自動轉場時機，確保恢復是立即且確定的。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShareCardCaptureFlowController : MonoBehaviour
    {
        private const string BodyIdleStateName = "Idle";

        [SerializeField]
        private ShareCardUIVisibilityController _uiVisibilityController;

        [SerializeField]
        private ShareCardPoseDatabaseSO _poseDatabase;

        [SerializeField]
        private WeaponSwitcher _weaponSwitcher;

        [SerializeField]
        private PlayerAttackVisualDispatcher _attackVisualDispatcher;

        [SerializeField]
        private UniWindowController _windowController;

        [SerializeField]
        private Animator _bodyAnimator;

        [SerializeField]
        private GameObject _lightWeaponVisual;

        [SerializeField]
        private Animator _lightWeaponAnimator;

        [SerializeField]
        private GameObject _heavyWeaponVisual;

        [SerializeField]
        private Animator _heavyWeaponAnimator;

        /// <summary>截圖流程進行中時為 true，供觸發按鈕判斷是否要暫時鎖住自己避免重複觸發。</summary>
        public bool IsCapturing { get; private set; }

        /// <summary>擷取成功時發出，帶出擷取到的背景截圖（尚未疊加統計資訊，由 ShareCardComposer 接手合成）。</summary>
        public event Action<Texture2D> OnCaptureSucceeded;

        /// <summary>流程中任一步驟失敗時發出，不帶任何資料。</summary>
        public event Action OnCaptureFailed;

        /// <summary>啟動一次分享卡截圖流程。流程進行中重複呼叫會被忽略，避免多個流程互相干擾。</summary>
        public void BeginCapture()
        {
            if (IsCapturing)
            {
                return;
            }

            StartCoroutine(CaptureRoutine());
        }

        private IEnumerator CaptureRoutine()
        {
            IsCapturing = true;
            Texture2D capturedTexture = null;
            bool captureSucceeded = false;

            try
            {
                _uiVisibilityController.HideAll();
                ForcePoseIfAvailable();

                // 等待至少一次完整影格渲染，確保上面強制跳轉的畫格已經實際被畫出來，
                // 不會擷取到跳轉前的舊畫面。
                yield return null;
                yield return new WaitForEndOfFrame();

                if (_windowController != null)
                {
                    captureSucceeded = ScreenRegionCapture.TryCaptureRegion(
                        _windowController.windowPosition,
                        _windowController.windowSize,
                        out capturedTexture);
                }
                else
                {
                    Debug.LogWarning("[ShareCardCaptureFlowController] 未指派 UniWindowController，無法取得視窗座標。");
                }
            }
            finally
            {
                RestorePose();
                _uiVisibilityController.RestoreAll();
                IsCapturing = false;
            }

            if (captureSucceeded)
            {
                OnCaptureSucceeded?.Invoke(capturedTexture);
            }
            else
            {
                OnCaptureFailed?.Invoke();
            }
        }

        /// <summary>
        /// 依目前裝備武器查找對應的分享卡姿勢設定並強制跳轉。找不到對應設定時記錄警告並直接返回，
        /// 維持角色目前姿勢，不中斷整個截圖流程（規格書第五節的防禦性後備行為）。
        /// </summary>
        private void ForcePoseIfAvailable()
        {
            WeaponDataSO currentWeapon = _weaponSwitcher != null ? _weaponSwitcher.CurrentWeapon : null;
            if (currentWeapon == null)
            {
                Debug.LogWarning("[ShareCardCaptureFlowController] 尚未裝備任何武器，維持目前姿勢。");
                return;
            }

            if (_poseDatabase == null || !_poseDatabase.TryGetPose(currentWeapon, out ShareCardPoseSO pose))
            {
                Debug.LogWarning($"[ShareCardCaptureFlowController] 找不到武器「{currentWeapon.WeaponName}」對應的分享卡姿勢設定，維持目前姿勢。");
                return;
            }

            if (!TryResolveWeaponVisual(currentWeapon, out GameObject activeVisual, out GameObject inactiveVisual, out Animator activeWeaponAnimator))
            {
                Debug.LogWarning($"[ShareCardCaptureFlowController] 武器「{currentWeapon.WeaponName}」不是已知的輕/重武器類型，維持目前姿勢。");
                return;
            }

            inactiveVisual.SetActive(false);
            activeVisual.SetActive(true);
            activeWeaponAnimator.Play(pose.WeaponAnimationStateName, 0, pose.NormalizedTime);

            if (_bodyAnimator != null)
            {
                _bodyAnimator.Play(pose.BodyAnimationStateName, 0, pose.NormalizedTime);
            }
        }

        private bool TryResolveWeaponVisual(WeaponDataSO weapon, out GameObject activeVisual, out GameObject inactiveVisual, out Animator activeAnimator)
        {
            if (weapon is LightWeaponSO)
            {
                activeVisual = _lightWeaponVisual;
                inactiveVisual = _heavyWeaponVisual;
                activeAnimator = _lightWeaponAnimator;
                return activeVisual != null && inactiveVisual != null && activeAnimator != null;
            }

            if (weapon is HeavyWeaponSO)
            {
                activeVisual = _heavyWeaponVisual;
                inactiveVisual = _lightWeaponVisual;
                activeAnimator = _heavyWeaponAnimator;
                return activeVisual != null && inactiveVisual != null && activeAnimator != null;
            }

            activeVisual = null;
            inactiveVisual = null;
            activeAnimator = null;
            return false;
        }

        private void RestorePose()
        {
            if (_attackVisualDispatcher != null)
            {
                _attackVisualDispatcher.OnAttackAnimationEnd();
            }

            if (_bodyAnimator != null)
            {
                _bodyAnimator.Play(BodyIdleStateName, 0, 0f);
            }
        }
    }
}
