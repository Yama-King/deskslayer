using DG.Tweening;
using DeskSlayer.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeskSlayer.UI
{
    /// <summary>
    /// 單一稀有度列項：顯示該稀有度的擁有狀態與數值，並提供裝備／合成互動。
    /// 未擁有時只顯示鎖定剪影，不洩漏具體數值；已擁有時讀取 WeaponInstance 的加成後數值。
    /// 合成的可互動判定與執行完全交給 WeaponInventoryService，這裡只負責把結果轉譯成畫面文字。
    /// </summary>
    public sealed class WeaponRaritySlotView : MonoBehaviour
    {
        [SerializeField]
        private Image _icon;

        [SerializeField]
        private Image _rarityColorTag;

        [SerializeField]
        private TextMeshProUGUI _rarityLabel;

        [SerializeField]
        private TextMeshProUGUI _nameLabel;

        [SerializeField]
        private TextMeshProUGUI _levelLabel;

        [SerializeField]
        private TextMeshProUGUI _duplicateLabel;

        [SerializeField]
        private TextMeshProUGUI _damageLabel;

        [SerializeField, Tooltip("永久保底武器的額外標籤區塊，僅在 IsPermanentStarter 時顯示")]
        private GameObject _permanentStarterBadge;

        [SerializeField]
        private Button _equipButton;

        [SerializeField]
        private Button _synthesizeButton;

        [SerializeField, Tooltip("無法合成時顯示原因，例如「還需 X 個」或「已達上限」")]
        private TextMeshProUGUI _synthesizeReasonLabel;

        [SerializeField, Tooltip("尚未擁有時顯示的武器碎片兌換按鈕，碎片數量不足時停用")]
        private Button _shardExchangeButton;

        [SerializeField, Tooltip("顯示碎片累積進度，例如「碎片 3/5」，僅在尚未擁有時顯示")]
        private TextMeshProUGUI _shardProgressLabel;

        private WeaponDataSO _weapon;
        private WeaponInventoryService _inventoryService;
        private WeaponDropConfigSO _dropConfig;
        private WeaponSwitcher _weaponSwitcher;
        private WeaponRarityDisplayConfigSO _displayConfig;
        private bool _isBound;

        /// <summary>綁定這一列要顯示的稀有度資產，並訂閱背包事件以便即時刷新。</summary>
        public void Bind(
            WeaponDataSO weapon,
            WeaponInventoryService inventoryService,
            WeaponDropConfigSO dropConfig,
            WeaponSwitcher weaponSwitcher,
            WeaponRarityDisplayConfigSO displayConfig)
        {
            _weapon = weapon;
            _inventoryService = inventoryService;
            _dropConfig = dropConfig;
            _weaponSwitcher = weaponSwitcher;
            _displayConfig = displayConfig;

            _equipButton.onClick.AddListener(HandleEquipClicked);
            _synthesizeButton.onClick.AddListener(HandleSynthesizeClicked);
            _shardExchangeButton.onClick.AddListener(HandleShardExchangeClicked);
            _inventoryService.OnWeaponObtained += HandleWeaponObtained;
            _inventoryService.OnDuplicateObtained += HandleDuplicateObtained;
            _inventoryService.OnWeaponUpgraded += HandleWeaponUpgraded;
            _inventoryService.OnShardCountChanged += HandleShardCountChanged;
            _isBound = true;

            Refresh();
        }

        private void OnDestroy()
        {
            if (!_isBound)
            {
                return;
            }

            _equipButton.onClick.RemoveListener(HandleEquipClicked);
            _synthesizeButton.onClick.RemoveListener(HandleSynthesizeClicked);
            _shardExchangeButton.onClick.RemoveListener(HandleShardExchangeClicked);
            _inventoryService.OnWeaponObtained -= HandleWeaponObtained;
            _inventoryService.OnDuplicateObtained -= HandleDuplicateObtained;
            _inventoryService.OnWeaponUpgraded -= HandleWeaponUpgraded;
            _inventoryService.OnShardCountChanged -= HandleShardCountChanged;
        }

        private void HandleWeaponObtained(WeaponInstance instance)
        {
            if (instance.Data == _weapon)
            {
                Refresh();
            }
        }

        private void HandleDuplicateObtained(WeaponDataSO weapon, int _)
        {
            if (weapon == _weapon)
            {
                Refresh();
            }
        }

        private void HandleWeaponUpgraded(WeaponInstance instance)
        {
            if (instance.Data != _weapon)
            {
                return;
            }

            Refresh();
            PlaySynthesizeFeedback();
        }

        private void HandleShardCountChanged(WeaponFamily family, WeaponRarity rarity, int newCount)
        {
            if (family == _weapon.Family && rarity == _weapon.Rarity)
            {
                Refresh();
            }
        }

        private void HandleEquipClicked()
        {
            _weaponSwitcher.EquipWeapon(_weapon);
        }

        private void HandleSynthesizeClicked()
        {
            _inventoryService.TryUpgrade(_weapon);
        }

        private void HandleShardExchangeClicked()
        {
            if (_inventoryService.TryExchangeShard(_weapon))
            {
                PlayShardExchangeFeedback();
            }
        }

        private void Refresh()
        {
            WeaponRarityDisplayConfigSO.RarityStyle style = _displayConfig.GetStyle(_weapon.Rarity);
            _rarityLabel.text = style.DisplayName;

            if (_rarityColorTag != null)
            {
                _rarityColorTag.color = style.Color;
            }

            if (_permanentStarterBadge != null)
            {
                _permanentStarterBadge.SetActive(_weapon.IsPermanentStarter);
            }

            bool owned = _inventoryService.IsOwned(_weapon);
            if (!owned)
            {
                RefreshLocked();
                return;
            }

            RefreshOwned();
        }

        private void RefreshLocked()
        {
            _nameLabel.text = "???";
            _icon.sprite = _displayConfig.LockedIcon;
            _icon.color = Color.gray;
            _levelLabel.text = string.Empty;
            _duplicateLabel.text = string.Empty;
            _damageLabel.text = string.Empty;

            // 尚未擁有時，裝備／合成完全沒有意義（沒有實例可裝備、沒有重複品可合成），
            // 直接整組隱藏，改在同一個版位顯示唯一可行的動作——碎片兌換，避免多加一整排
            // 控制項撐爆 DetailContent 固定的版面高度。
            _equipButton.gameObject.SetActive(false);
            _synthesizeButton.gameObject.SetActive(false);
            if (_synthesizeReasonLabel != null)
            {
                _synthesizeReasonLabel.gameObject.SetActive(false);
            }

            _shardExchangeButton.gameObject.SetActive(true);
            if (_shardProgressLabel != null)
            {
                _shardProgressLabel.gameObject.SetActive(true);
            }

            RefreshShardExchangeState();
        }

        private void RefreshOwned()
        {
            WeaponInstance instance = _inventoryService.GetOwnedInstance(_weapon);

            _nameLabel.text = _weapon.WeaponName;
            _icon.sprite = _weapon.Icon;
            _icon.color = Color.white;
            _levelLabel.text = $"Lv.{instance.UpgradeLevel}";

            int duplicateCount = _inventoryService.GetDuplicateCount(_weapon);
            _duplicateLabel.text = $"重複 x{duplicateCount}";
            _damageLabel.text = $"傷害 {Mathf.RoundToInt(_weapon.BaseDamage * instance.DamageMultiplier)}";

            _equipButton.gameObject.SetActive(true);
            _equipButton.interactable = true;
            _synthesizeButton.gameObject.SetActive(true);
            if (_synthesizeReasonLabel != null)
            {
                _synthesizeReasonLabel.gameObject.SetActive(true);
            }

            RefreshSynthesisState(instance, duplicateCount);

            // 已擁有這把武器後，碎片兌換入口就沒有意義了（兌換範圍限定尚未收集的變體），直接關閉。
            _shardExchangeButton.gameObject.SetActive(false);
            if (_shardProgressLabel != null)
            {
                _shardProgressLabel.gameObject.SetActive(false);
            }
        }

        private void RefreshSynthesisState(WeaponInstance instance, int duplicateCount)
        {
            if (instance.IsAtMaxUpgradeLevel)
            {
                _synthesizeButton.interactable = false;
                if (_synthesizeReasonLabel != null)
                {
                    _synthesizeReasonLabel.text = "已達上限";
                }

                return;
            }

            int fusionCost = _dropConfig.GetFusionCost(_weapon.Rarity);
            int missing = fusionCost - duplicateCount;
            if (missing > 0)
            {
                _synthesizeButton.interactable = false;
                if (_synthesizeReasonLabel != null)
                {
                    _synthesizeReasonLabel.text = $"還需 {missing} 個";
                }

                return;
            }

            _synthesizeButton.interactable = true;
            if (_synthesizeReasonLabel != null)
            {
                _synthesizeReasonLabel.text = string.Empty;
            }
        }

        /// <summary>
        /// 依目前碎片數量刷新兌換按鈕的可互動狀態與進度文字。碎片依家族+稀有度累積，
        /// 兌換範圍限定同家族、同稀有度底下這把尚未收集的變體，資格判定完全交給
        /// WeaponInventoryService.CanExchangeShard，這裡只負責把結果轉譯成畫面文字。
        /// </summary>
        private void RefreshShardExchangeState()
        {
            int shardCount = _inventoryService.GetShardCount(_weapon.Family, _weapon.Rarity);
            int shardCost = _dropConfig.GetShardExchangeCost(_weapon.Rarity);

            _shardExchangeButton.interactable = _inventoryService.CanExchangeShard(_weapon);
            if (_shardProgressLabel != null)
            {
                _shardProgressLabel.text = $"碎片 {shardCount}/{shardCost}";
            }
        }

        private void PlaySynthesizeFeedback()
        {
            RectTransform levelTransform = _levelLabel.rectTransform;
            levelTransform.DOKill();
            levelTransform.localScale = Vector3.one * 1.3f;
            levelTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }

        /// <summary>兌換成功、格子從未擁有變已擁有時的進場回饋，跟合成成功的縮放彈跳手法一致。</summary>
        private void PlayShardExchangeFeedback()
        {
            RectTransform iconTransform = _icon.rectTransform;
            iconTransform.DOKill();
            iconTransform.localScale = Vector3.one * 1.3f;
            iconTransform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
        }
    }
}
