#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace DeskSlayer.ShareCard
{
    /// <summary>
    /// 對桌面實際螢幕區域做 Win32 GDI 擷取（GetDC + BitBlt），與 Win32LowLevelKeyboardHook 完全獨立、
    /// 不共用任何程式碼。BitBlt 對桌面 DC（GetDC(IntPtr.Zero)）擷取的內容就是「已由 DWM 合成完成」的
    /// 畫面，會自動包含桌面透明視窗穿透顯示的桌面內容，不需要額外處理色鍵/Alpha 合成邏輯。
    ///
    /// DPI 處理：這個專案目前完全沒有宣告 Per-Monitor DPI Awareness（無 app manifest），代表整個行程
    /// （含 UniWinC 原生外掛）在非 100% 顯示縮放環境下，看到的 windowPosition/windowSize 等座標其實是
    /// 被 Windows 做過「DPI 虛擬化」的邏輯座標，不等於螢幕實際的物理像素數。若把整個行程宣告成
    /// DPI-aware，會牽動 DesktopWindowClickThroughMediator 等其他既有座標換算邏輯，風險太高；
    /// 因此這裡改用 SetThreadDpiAwarenessContext 只在「這次擷取」的執行緒層級暫時提升成
    /// PER_MONITOR_AWARE_V2，擷取完立刻還原成呼叫前的 context，把影響範圍鎖死在這個類別內。
    ///
    /// 座標系換算：傳入的 unityPosition/unitySize 沿用 UniWindowController.windowPosition/windowSize
    /// 既有的「原點左下、Y 向上」Unity 慣例座標（比照 DesktopWindowClickThroughMediator 已驗證過的
    /// 座標假設），且是尚未套用 DPI 縮放的邏輯像素。這裡換算成 BitBlt 需要的「原點左上、Y 向下」
    /// 物理像素矩形：X 軸方向相同不需翻轉，Y 軸用主螢幕物理高度換算回邏輯高度後做翻轉。
    /// </summary>
    public static class ScreenRegionCapture
    {
        private const int SM_CYSCREEN = 1;
        private const uint SRCCOPY = 0x00CC0020;
        private const uint CAPTUREBLT = 0x40000000;
        private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [StructLayout(LayoutKind.Sequential)]
        private struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr dpiContext);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForSystem();

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int width, int height);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int width, int height,
            IntPtr hdcSrc, int xSrc, int ySrc, uint rop);

        [DllImport("gdi32.dll")]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint start, uint lines,
            [Out] byte[] lpvBits, ref BITMAPINFOHEADER lpbi, uint usage);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr hdc);

        /// <summary>
        /// 擷取指定的視窗區域（Unity 慣例座標：原點左下、Y 向上、未套用 DPI 縮放的邏輯像素），
        /// 內部處理 DPI 縮放換算與 Win32 座標系翻轉。任何步驟失敗都回傳 false，不拋出例外，
        /// 呼叫端（ShareCardCaptureFlowController）需自行處理擷取失敗的後備行為。
        /// </summary>
        public static bool TryCaptureRegion(Vector2 unityPosition, Vector2 unitySize, out Texture2D texture)
        {
            texture = null;
            IntPtr previousDpiContext = IntPtr.Zero;

            try
            {
                previousDpiContext = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);

                float scale = GetDpiForSystem() / 96f;
                if (scale <= 0f)
                {
                    scale = 1f;
                }

                int primaryPhysicalHeight = GetSystemMetrics(SM_CYSCREEN);
                float primaryLogicalHeight = primaryPhysicalHeight / scale;

                int physicalLeft = Mathf.RoundToInt(unityPosition.x * scale);
                int physicalTop = Mathf.RoundToInt((primaryLogicalHeight - (unityPosition.y + unitySize.y)) * scale);
                int physicalWidth = Mathf.RoundToInt(unitySize.x * scale);
                int physicalHeight = Mathf.RoundToInt(unitySize.y * scale);

                return TryCapturePhysicalRegion(physicalLeft, physicalTop, physicalWidth, physicalHeight, out texture);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                if (previousDpiContext != IntPtr.Zero)
                {
                    SetThreadDpiAwarenessContext(previousDpiContext);
                }
            }
        }

        private static bool TryCapturePhysicalRegion(int left, int top, int width, int height, out Texture2D texture)
        {
            texture = null;

            if (width <= 0 || height <= 0)
            {
                Debug.LogWarning($"[ScreenRegionCapture] 擷取區域尺寸無效（width={width}, height={height}）。");
                return false;
            }

            IntPtr screenDC = IntPtr.Zero;
            IntPtr memoryDC = IntPtr.Zero;
            IntPtr bitmap = IntPtr.Zero;
            IntPtr previousBitmap = IntPtr.Zero;

            try
            {
                screenDC = GetDC(IntPtr.Zero);
                if (screenDC == IntPtr.Zero)
                {
                    Debug.LogWarning("[ScreenRegionCapture] GetDC 取得桌面裝置內容失敗。");
                    return false;
                }

                memoryDC = CreateCompatibleDC(screenDC);
                bitmap = CreateCompatibleBitmap(screenDC, width, height);
                if (memoryDC == IntPtr.Zero || bitmap == IntPtr.Zero)
                {
                    Debug.LogWarning("[ScreenRegionCapture] 建立相容 DC/Bitmap 失敗。");
                    return false;
                }

                previousBitmap = SelectObject(memoryDC, bitmap);

                // CAPTUREBLT 確保分層視窗（WS_EX_LAYERED，透明視窗用的樣式）也會被正確合成擷取進來，
                // 不加這個旗標在部分 Windows 版本上可能只擷取到底層畫面、漏掉分層視窗內容。
                bool blitSucceeded = BitBlt(memoryDC, 0, 0, width, height, screenDC, left, top, SRCCOPY | CAPTUREBLT);
                if (!blitSucceeded)
                {
                    Debug.LogWarning("[ScreenRegionCapture] BitBlt 擷取失敗。");
                    return false;
                }

                return TryConvertBitmapToTexture(memoryDC, bitmap, width, height, out texture);
            }
            finally
            {
                if (previousBitmap != IntPtr.Zero)
                {
                    SelectObject(memoryDC, previousBitmap);
                }

                if (bitmap != IntPtr.Zero)
                {
                    DeleteObject(bitmap);
                }

                if (memoryDC != IntPtr.Zero)
                {
                    DeleteDC(memoryDC);
                }

                if (screenDC != IntPtr.Zero)
                {
                    ReleaseDC(IntPtr.Zero, screenDC);
                }
            }
        }

        /// <summary>
        /// 用 GetDIBits 取出 32bit BGRA 像素資料，換算成 Texture2D 需要的 RGBA 且上下顛倒的像素排列
        /// （Windows DIB 由上而下、Unity Texture2D 由下而上）。
        /// </summary>
        private static bool TryConvertBitmapToTexture(IntPtr memoryDC, IntPtr bitmap, int width, int height, out Texture2D texture)
        {
            texture = null;

            var header = new BITMAPINFOHEADER
            {
                biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                biWidth = width,
                biHeight = -height, // 負值代表 top-down DIB，逐行順序由上而下，方便下面計算翻轉
                biPlanes = 1,
                biBitCount = 32,
                biCompression = 0 // BI_RGB
            };

            byte[] pixelBuffer = new byte[width * height * 4];
            int copiedLines = GetDIBits(memoryDC, bitmap, 0, (uint)height, pixelBuffer, ref header, 0);
            if (copiedLines == 0)
            {
                Debug.LogWarning("[ScreenRegionCapture] GetDIBits 讀取像素資料失敗。");
                return false;
            }

            Color32[] colors = new Color32[width * height];
            for (int srcRow = 0; srcRow < height; srcRow++)
            {
                // DIB 第 0 行是畫面最上面一行；Texture2D 的第 0 行對應畫面最下面一行，這裡做垂直翻轉。
                int destRow = height - 1 - srcRow;
                int srcRowStart = srcRow * width * 4;
                int destRowStart = destRow * width;

                for (int col = 0; col < width; col++)
                {
                    int srcIndex = srcRowStart + col * 4;
                    // BitBlt/GetDIBits 回傳的像素順序是 BGRA，Unity Color32 需要 RGBA。
                    byte b = pixelBuffer[srcIndex];
                    byte g = pixelBuffer[srcIndex + 1];
                    byte r = pixelBuffer[srcIndex + 2];
                    byte a = pixelBuffer[srcIndex + 3];
                    colors[destRowStart + col] = new Color32(r, g, b, a);
                }
            }

            texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.SetPixels32(colors);
            texture.Apply(false);
            return true;
        }
    }
}
#endif
