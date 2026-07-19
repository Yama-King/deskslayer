using System.Collections.Concurrent;

namespace DeskSlayer.MouseHook
{
    /// <summary>
    /// 包裝 ConcurrentQueue，做為背景 Hook 執行緒與 Unity 主執行緒之間的執行緒安全橋接。
    /// 比照 KeyboardHook.KeyboardEventQueue，職責單純：只負責事件的存放與取出。
    /// </summary>
    public sealed class MouseEventQueue
    {
        private readonly ConcurrentQueue<MouseClickData> _queue = new ConcurrentQueue<MouseClickData>();

        /// <summary>由背景 Hook 執行緒呼叫，將滑鼠點擊事件放入佇列。</summary>
        public void Enqueue(MouseClickData data)
        {
            _queue.Enqueue(data);
        }

        /// <summary>由 Unity 主執行緒呼叫，嘗試取出一筆事件。</summary>
        public bool TryDequeue(out MouseClickData data)
        {
            return _queue.TryDequeue(out data);
        }

        /// <summary>目前佇列中尚未處理的事件數量，供除錯／監控使用。</summary>
        public int Count => _queue.Count;
    }
}
