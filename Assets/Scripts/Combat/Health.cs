using System;
using JohnStairs.RPG.Character;
using Unity.Netcode;
using UnityEngine;

namespace TGNS.Combat
{
    /// <summary>
    /// Server-authoritative health. Damage is only ever applied on the server
    /// (see MeleeWeapon.OnTriggerEnter), and the resulting value is replicated
    /// to every client via NetworkVariable so UI can bind to it directly.
    /// An Animator/IAnimationHandler is optional - Die() is only forwarded to
    /// it when present, so this also works on non-character targets like a
    /// plain test dummy.
    /// </summary>
    public class Health : NetworkBehaviour
    {
        [SerializeField] private int maxHealth = 100;

        private readonly NetworkVariable<int> _currentHealth =
            new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private bool _dead;
        private IAnimationHandler _animationHandler;

        public int CurrentHealth => _currentHealth.Value;
        public int MaxHealth => maxHealth;
        public bool IsDead => _dead;

        /// <summary>Fires on every client whenever health changes: (previous, current).</summary>
        public event Action<int, int> OnHealthChanged;
        public event Action OnDied;

        private void Awake()
        {
            _animationHandler = GetComponent<IAnimationHandler>();
        }

        public override void OnNetworkSpawn()
        {
            _currentHealth.OnValueChanged += HandleHealthChanged;

            if (IsServer)
            {
                _currentHealth.Value = maxHealth;
            }
        }

        public override void OnNetworkDespawn()
        {
            _currentHealth.OnValueChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int previousValue, int newValue)
        {
            OnHealthChanged?.Invoke(previousValue, newValue);

            if (newValue <= 0 && !_dead)
            {
                _dead = true;
                _animationHandler?.Die();
                OnDied?.Invoke();
            }
        }

        /// <summary>Server-only. Positive values heal, negative values damage.</summary>
        public void ChangeHealth(int delta)
        {
            if (!IsServer || _dead)
            {
                return;
            }

            _currentHealth.Value = Mathf.Clamp(_currentHealth.Value + delta, 0, maxHealth);
        }

        public void ResetHealth()
        {
            if (!IsServer)
            {
                return;
            }

            _dead = false;
            _currentHealth.Value = maxHealth;
        }
    }
}
