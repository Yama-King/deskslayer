using UnityEngine;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 分享卡圖卡合成器：把截圖背景與統計數據交給 ShareCardLayoutBinder 套用後，
    /// 用一台專屬、平常關閉的 Camera 手動渲染一次到 RenderTexture，再讀回像素編碼成 PNG。
    /// Camera 平常 enabled=false（不參與每影格自動渲染），只在合成當下手動呼叫 Render()，
    /// 呼應本專案「背景常駐、CPU 佔用率極低」的核心要求。
    /// 版面本身（背景圖／文字／風格象限）的位置、字型、顏色全部是 Prefab／Inspector 可調整的內容，
    /// 這裡完全不涉及排版數值。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShareCardComposer : MonoBehaviour
    {
        [SerializeField]
        private Camera _captureCamera;

        [SerializeField]
        private RenderTexture _captureRenderTexture;

        [SerializeField]
        private ShareCardLayoutBinder _layoutBinder;

        /// <summary>
        /// 合成一張分享卡並回傳 PNG 位元組。任一必要元件未指派時回傳 false，不拋出例外。
        /// </summary>
        public bool TryCompose(Texture background, ShareCardDisplayData data, out byte[] pngBytes)
        {
            pngBytes = null;

            if (_captureCamera == null || _captureRenderTexture == null || _layoutBinder == null)
            {
                Debug.LogWarning("[ShareCardComposer] 尚未指派攝影機／RenderTexture／版面綁定元件，無法合成圖卡。");
                return false;
            }

            _layoutBinder.Bind(background, data);

            // 手動呼叫 Render() 前強制跑完 Canvas 的排版/圖像重建，確保剛剛 Bind() 套用的最新內容
            // 已經反映在這次渲染結果裡，不會擷取到套用前的舊畫面。
            Canvas.ForceUpdateCanvases();

            _captureCamera.targetTexture = _captureRenderTexture;
            _captureCamera.Render();

            RenderTexture previousActive = RenderTexture.active;
            RenderTexture.active = _captureRenderTexture;

            Texture2D output = new Texture2D(_captureRenderTexture.width, _captureRenderTexture.height, TextureFormat.RGBA32, false);
            output.ReadPixels(new Rect(0, 0, _captureRenderTexture.width, _captureRenderTexture.height), 0, 0);
            output.Apply(false);

            RenderTexture.active = previousActive;

            pngBytes = output.EncodeToPNG();
            Destroy(output);
            return true;
        }
    }
}
