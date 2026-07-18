using UnityEngine;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 分享卡截圖前「隱藏所有操作介面」的唯一權責入口：切換 Inspector 指派清單中每個頂層 Canvas 的
    /// Canvas.enabled，不碰任何面板內部的 CanvasGroup/DOTween 開關狀態，也不需要認識場上有哪些面板
    /// 控制器存在。RestoreAll() 會還原成呼叫 HideAll() 之前每個 Canvas 原本的 enabled 狀態，
    /// 避免把「呼叫前就已經關閉」的 Canvas 誤判成分享卡系統關的、恢復時又誤打開。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShareCardUIVisibilityController : MonoBehaviour
    {
        [SerializeField, Tooltip("生成分享卡時需要隱藏的所有頂層 Canvas（含分享卡自己的 Canvas）")]
        private Canvas[] _canvasesToHide;

        private bool[] _previousEnabledStates;

        /// <summary>隱藏清單中所有 Canvas，並記住呼叫前的 enabled 狀態供 RestoreAll() 還原。</summary>
        public void HideAll()
        {
            if (_canvasesToHide == null)
            {
                return;
            }

            _previousEnabledStates = new bool[_canvasesToHide.Length];

            for (int i = 0; i < _canvasesToHide.Length; i++)
            {
                Canvas canvas = _canvasesToHide[i];
                if (canvas == null)
                {
                    continue;
                }

                _previousEnabledStates[i] = canvas.enabled;
                canvas.enabled = false;
            }
        }

        /// <summary>把清單中所有 Canvas 還原成呼叫 HideAll() 之前記錄的 enabled 狀態。</summary>
        public void RestoreAll()
        {
            if (_canvasesToHide == null || _previousEnabledStates == null)
            {
                return;
            }

            for (int i = 0; i < _canvasesToHide.Length; i++)
            {
                Canvas canvas = _canvasesToHide[i];
                if (canvas == null)
                {
                    continue;
                }

                canvas.enabled = _previousEnabledStates[i];
            }

            _previousEnabledStates = null;
        }
    }
}
