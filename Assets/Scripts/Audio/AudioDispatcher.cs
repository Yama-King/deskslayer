using UnityEngine;
using DeskSlayer.Combat;
using DeskSlayer.Enemy;

namespace DeskSlayer.Audio
{
    /// <summary>
    /// 音效系統的橋接元件：訂閱 TypingEnergySystem 的輕/重攻擊觸發事件與 EnemyController 的受擊事件，
    /// 決定播放哪個 SoundDataSO，交給 AudioManager 播放。
    /// 比照 CombatDispatcher 的作法，AudioManager 只負責播放、不認識任何遊戲邏輯。
    /// </summary>
    [RequireComponent(typeof(TypingEnergySystem))]
    public sealed class AudioDispatcher : MonoBehaviour
    {
        [SerializeField]
        private EnemyController _targetEnemy;

        [SerializeField, Tooltip("武器掉落事件來源，選填，未指定時不播放掉落音效")]
        private WeaponDropDispatcher _weaponDropDispatcher;

        [SerializeField]
        private SoundDataSO _lightAttackSound;

        [SerializeField]
        private SoundDataSO _heavyAttackSound;

        [SerializeField]
        private SoundDataSO _enemyHitSound;

        [SerializeField]
        private SoundDataSO _weaponDropSound;

        private TypingEnergySystem _typingEnergySystem;

        private void Awake()
        {
            _typingEnergySystem = GetComponent<TypingEnergySystem>();
        }

        private void OnEnable()
        {
            _typingEnergySystem.OnLightAttackTriggered += HandleLightAttack;
            _typingEnergySystem.OnHeavyAttackTriggered += HandleHeavyAttack;

            if (_targetEnemy != null)
            {
                _targetEnemy.OnHit += HandleEnemyHit;
            }

            if (_weaponDropDispatcher != null)
            {
                _weaponDropDispatcher.OnWeaponDropped += HandleWeaponDropped;
            }
        }

        private void OnDisable()
        {
            _typingEnergySystem.OnLightAttackTriggered -= HandleLightAttack;
            _typingEnergySystem.OnHeavyAttackTriggered -= HandleHeavyAttack;

            if (_targetEnemy != null)
            {
                _targetEnemy.OnHit -= HandleEnemyHit;
            }

            if (_weaponDropDispatcher != null)
            {
                _weaponDropDispatcher.OnWeaponDropped -= HandleWeaponDropped;
            }
        }

        private void HandleLightAttack(LightWeaponSO weapon)
        {
            AudioManager.Instance?.PlaySound(_lightAttackSound);
        }

        private void HandleHeavyAttack(HeavyWeaponSO weapon)
        {
            AudioManager.Instance?.PlaySound(_heavyAttackSound);
        }

        private void HandleEnemyHit(int damage)
        {
            AudioManager.Instance?.PlaySound(_enemyHitSound);
        }

        private void HandleWeaponDropped(WeaponDataSO weapon, Vector3 dropPosition, WeaponDropOutcome outcome)
        {
            AudioManager.Instance?.PlaySound(_weaponDropSound);
        }
    }
}
