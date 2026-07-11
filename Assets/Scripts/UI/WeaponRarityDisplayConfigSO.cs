using System;
using System.Collections.Generic;
using DeskSlayer.Combat;
using UnityEngine;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 背包 UI 的稀有度顯示樣式設定（顯示名稱、標示顏色、未擁有時的鎖定圖示、永久保底標籤）。
    /// 這些純粹是呈現層的美術參數，WeaponDataSO/WeaponRarity 本身不記錄任何顏色或圖示，
    /// 刻意獨立成一份 UI 專用的 ScriptableObject，避免把美術參數混進戰鬥數據層。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponRarityDisplayConfig", menuName = "DeskSlayer/UI/Weapon Rarity Display Config", order = 1)]
    public sealed class WeaponRarityDisplayConfigSO : ScriptableObject
    {
        [Serializable]
        public struct RarityStyle
        {
            [SerializeField]
            private WeaponRarity _rarity;

            [SerializeField, Tooltip("該稀有度的中文顯示名稱")]
            private string _displayName;

            [SerializeField, Tooltip("該稀有度的標示顏色（例如稀有度色條、文字上色）")]
            private Color _color;

            public RarityStyle(WeaponRarity rarity, string displayName, Color color)
            {
                _rarity = rarity;
                _displayName = displayName;
                _color = color;
            }

            public WeaponRarity Rarity => _rarity;
            public string DisplayName => _displayName;
            public Color Color => _color;
        }

        [SerializeField, Tooltip("各稀有度的顯示名稱與顏色")]
        private RarityStyle[] _rarityStyles =
        {
            new RarityStyle(WeaponRarity.Common, "普通", new Color(0.75f, 0.75f, 0.75f)),
            new RarityStyle(WeaponRarity.Rare, "稀有", new Color(0.3f, 0.55f, 1f)),
            new RarityStyle(WeaponRarity.Epic, "史詩", new Color(0.65f, 0.3f, 0.9f)),
            new RarityStyle(WeaponRarity.Legendary, "傳說", new Color(1f, 0.7f, 0.1f))
        };

        [SerializeField, Tooltip("尚未擁有時顯示的鎖定/剪影圖示")]
        private Sprite _lockedIcon;

        [SerializeField, Tooltip("永久保底武器的額外標籤圖示")]
        private Sprite _permanentStarterBadge;

        /// <summary>尚未擁有時顯示的鎖定/剪影圖示。</summary>
        public Sprite LockedIcon => _lockedIcon;

        /// <summary>永久保底武器的額外標籤圖示。</summary>
        public Sprite PermanentStarterBadge => _permanentStarterBadge;

        /// <summary>查詢指定稀有度的顯示樣式，找不到對應設定時回傳白色、以英文列舉名稱作為顯示名稱的預設樣式。</summary>
        public RarityStyle GetStyle(WeaponRarity rarity)
        {
            foreach (RarityStyle style in _rarityStyles)
            {
                if (style.Rarity == rarity)
                {
                    return style;
                }
            }

            return new RarityStyle(rarity, rarity.ToString(), Color.white);
        }
    }
}
