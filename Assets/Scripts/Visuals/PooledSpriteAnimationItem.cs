using System;
using UnityEngine;

namespace DeskSlayer.Visuals
{
    /// <summary>
    /// 單一 Pooled Sprite 幀動畫物件的播放與生命週期管理。不認識池子的內部資料結構，
    /// 動畫播放完成或被強制中止時，只透過 Play() 當下傳入的回呼通知「可以回收了」，
    /// 由呼叫端（PooledSpriteAnimationPool）決定如何處理，比照 WeaponAfterimageItem 的作法。
    /// 三種命中特效（輕攻擊粒子／重攻擊粒子／敵人受擊特效）行為完全相同，共用這一份元件，
    /// 差異只在租借時傳入的幀序列與參數不同。
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PooledSpriteAnimationItem : MonoBehaviour
    {
        private SpriteRenderer _spriteRenderer;

        private Sprite[] _frames;
        private float _frameDuration;
        private int _currentFrameIndex;
        private float _elapsed;
        private bool _isPlaying;

        private Action<PooledSpriteAnimationItem> _onPlaybackEnded;

        private void Awake()
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }

        /// <summary>
        /// 從池子租借時呼叫：掛到指定錨點底下（局部座標歸零，實際定位由錨點的 Transform 決定），
        /// 重置到第一幀並立即開始播放。動畫結束時機以 frames 實際播完為準，不使用固定計時器猜測，
        /// 因為同一特效的不同變體幀數可能不同，固定計時器會跟實際幀數對不上。
        /// 旋轉沿用 localRotation（相對錨點歸零），讓特效的顯示角度完全交給錨點自己的 Transform 決定——
        /// 想旋轉某個特效的顯示角度，直接在 Inspector 轉動對應的錨點即可，不需要改程式碼。
        /// 敵人 Prefab 的根物件為了讓角色面向玩家，本身帶有 180 度 Y 軸旋轉，EnemyHitVfxAnchor 因此需要
        /// 自行在局部旋轉上抵銷這 180 度（見各 Boss Prefab 的錨點設定），才能讓敵人受擊特效預設維持
        /// 素材原始朝向，不會因為敵人朝向玩家的翻轉而跟著鏡射；玩家角色本身沒有這個翻轉，
        /// PlayerHitVfxAnchors 底下的輕/重攻擊錨點不需要額外抵銷，可以直接自由旋轉。
        /// 縮放同理換算成世界座標下的目標大小再除回 localScale（比照 WeaponAfterimageItem.ToLocalScale
        /// 的作法）：錨點是敵人/玩家角色底下的子物件，會繼承角色本身的縮放（例如 Boss1 是 1.78 倍），
        /// scaleMultiplier 若直接當成 localScale，特效實際顯示大小就會被角色縮放連帶放大，
        /// 不同敵人之間特效大小會不一致，也跟 HitVfxSetSO 上「1 代表使用素材原始大小」的欄位說明不符。
        /// </summary>
        public void Play(Sprite[] frames, float frameDuration, float scaleMultiplier, Color tint, Transform anchor,
            Action<PooledSpriteAnimationItem> onPlaybackEnded)
        {
            if (anchor != null)
            {
                transform.SetParent(anchor, worldPositionStays: false);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
            }

            transform.localScale = ToLocalScale(Vector3.one * scaleMultiplier);
            _spriteRenderer.color = tint;

            _frames = frames;
            _frameDuration = Mathf.Max(frameDuration, 0.001f);
            _currentFrameIndex = 0;
            _elapsed = 0f;
            _onPlaybackEnded = onPlaybackEnded;
            _isPlaying = true;

            _spriteRenderer.sprite = frames[0];
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 中途強制中止並歸還池子，不等待動畫自然播完。供敵人即將被銷毀前，池子主動回收目前借出中、
        /// 掛在該敵人底下的特效物件時使用——若不主動處理，特效物件會隨著敵人 GameObject 的 Destroy()
        /// 被動一起銷毀，永久從池子中流失，導致 Pool 容量隨遊玩時間遞減。
        /// </summary>
        public void ForceStopAndRelease()
        {
            if (!_isPlaying)
            {
                return;
            }

            EndPlayback();
        }

        private void Update()
        {
            // 停用狀態（池中閒置）直接跳過，不做任何每幀運算。
            if (!_isPlaying)
            {
                return;
            }

            _elapsed += Time.deltaTime;

            // 用 while 而非 if 逐格步進，避免低幀率時單次 Update 的 deltaTime 一次跨過多個幀間隔，
            // 導致動畫實際播放格數少於 frames 應有的格數。
            while (_isPlaying && _elapsed >= _frameDuration)
            {
                _elapsed -= _frameDuration;
                _currentFrameIndex++;

                if (_currentFrameIndex >= _frames.Length)
                {
                    EndPlayback();
                    break;
                }

                _spriteRenderer.sprite = _frames[_currentFrameIndex];
            }
        }

        private void EndPlayback()
        {
            _isPlaying = false;
            _frames = null;

            Action<PooledSpriteAnimationItem> callback = _onPlaybackEnded;
            _onPlaybackEnded = null;

            gameObject.SetActive(false);
            callback?.Invoke(this);
        }

        /// <summary>
        /// 將「世界座標下想要的縮放」換算成本物件在目前父階層下應設定的 localScale，
        /// 避免父物件（錨點所屬的敵人/玩家角色）本身若非單位縮放，導致特效實際顯示大小被連帶放大或縮小。
        /// </summary>
        private Vector3 ToLocalScale(Vector3 worldScale)
        {
            Vector3 parentLossyScale = transform.parent != null ? transform.parent.lossyScale : Vector3.one;
            return new Vector3(
                parentLossyScale.x != 0f ? worldScale.x / parentLossyScale.x : worldScale.x,
                parentLossyScale.y != 0f ? worldScale.y / parentLossyScale.y : worldScale.y,
                parentLossyScale.z != 0f ? worldScale.z / parentLossyScale.z : worldScale.z);
        }
    }
}
