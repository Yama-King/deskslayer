using System;
using DeskSlayer.Combat;
using UnityEngine;

namespace DeskSlayer.Juice
{
    /// <summary>
    /// 掉落視覺回饋的稀有度強度曲線：尺寸、飄移距離、進場彈跳時長、顯示／淡出時長皆隨稀有度遞增，
    /// 供 WeaponDropPopup（原地跳出提示）與 WeaponDropLogEntry（角落記錄清單）共用同一份設定，
    /// 避免兩處各自維護一套不同步的強度數字。比照 WeaponDropConfigSO 的作法採陣列＋查詢方法，
    /// 找不到對應稀有度時回傳 Common 等級的保底強度。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponDropFeedbackConfig", menuName = "DeskSlayer/Juice/Weapon Drop Feedback Config", order = 4)]
    public sealed class WeaponDropFeedbackConfigSO : ScriptableObject
    {
        [Serializable]
        public struct RarityFeedback
        {
            [SerializeField]
            private WeaponRarity _rarity;

            [SerializeField, Min(0.1f), Tooltip("原地跳出提示內容（圖示＋文字）的縮放倍率")]
            private float _contentScale;

            [SerializeField, Min(0f), Tooltip("原地跳出提示往上飄移的總距離（世界座標單位）")]
            private float _floatDistance;

            [SerializeField, Min(0.05f), Tooltip("進場縮放彈跳的時長（秒），原地提示與清單項目共用")]
            private float _punchDuration;

            [SerializeField, Min(0.1f), Tooltip("原地跳出提示的總顯示時長（秒），淡出在尾端播放")]
            private float _displayDuration;

            [SerializeField, Min(0.05f), Tooltip("原地跳出提示的淡出時長（秒），需小於等於總顯示時長")]
            private float _fadeDuration;

            [SerializeField, Min(0.1f), Tooltip("角落記錄清單項目的總存活時長（秒），淡出在尾端播放")]
            private float _logLifetime;

            [SerializeField, Min(0.05f), Tooltip("角落記錄清單項目的淡出時長（秒），需小於等於總存活時長")]
            private float _logFadeDuration;

            public RarityFeedback(
                WeaponRarity rarity,
                float contentScale,
                float floatDistance,
                float punchDuration,
                float displayDuration,
                float fadeDuration,
                float logLifetime,
                float logFadeDuration)
            {
                _rarity = rarity;
                _contentScale = contentScale;
                _floatDistance = floatDistance;
                _punchDuration = punchDuration;
                _displayDuration = displayDuration;
                _fadeDuration = fadeDuration;
                _logLifetime = logLifetime;
                _logFadeDuration = logFadeDuration;
            }

            public WeaponRarity Rarity => _rarity;
            public float ContentScale => _contentScale;
            public float FloatDistance => _floatDistance;
            public float PunchDuration => _punchDuration;
            public float DisplayDuration => _displayDuration;
            public float FadeDuration => _fadeDuration;
            public float LogLifetime => _logLifetime;
            public float LogFadeDuration => _logFadeDuration;
        }

        [SerializeField, Tooltip("各稀有度的掉落視覺回饋強度曲線")]
        private RarityFeedback[] _rarityFeedbacks =
        {
            new RarityFeedback(WeaponRarity.Common, 0.85f, 1.0f, 0.18f, 0.7f, 0.3f, 2.5f, 0.3f),
            new RarityFeedback(WeaponRarity.Rare, 1.0f, 1.2f, 0.22f, 0.85f, 0.35f, 3.0f, 0.35f),
            new RarityFeedback(WeaponRarity.Epic, 1.2f, 1.4f, 0.27f, 1.0f, 0.4f, 3.5f, 0.4f),
            new RarityFeedback(WeaponRarity.Legendary, 1.45f, 1.6f, 0.35f, 1.2f, 0.5f, 4.5f, 0.5f)
        };

        /// <summary>查詢指定稀有度的視覺回饋強度曲線，找不到對應設定時回傳 Common 等級的保底強度。</summary>
        public RarityFeedback GetFeedback(WeaponRarity rarity)
        {
            foreach (RarityFeedback feedback in _rarityFeedbacks)
            {
                if (feedback.Rarity == rarity)
                {
                    return feedback;
                }
            }

            return new RarityFeedback(rarity, 0.85f, 1.0f, 0.18f, 0.7f, 0.3f, 2.5f, 0.3f);
        }
    }
}
