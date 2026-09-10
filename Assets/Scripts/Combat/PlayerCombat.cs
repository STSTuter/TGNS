using JohnStairs.RPG.Character;
using JohnStairs.RPG.Combat.Abilities.Enums;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TGNS.Combat
{
    /// <summary>
    /// Owner-authoritative melee attack input. Triggers the vendor
    /// AnimationHandler's existing "Melee Cast" animation (see
    /// AnimationHandler.Cast/FinishCast) and, in the middle of the swing,
    /// arms the MeleeWeapon's hitbox server-side.
    ///
    /// The animation trigger itself is replayed on every client via RPC
    /// rather than relying on NetworkAnimator to pick up a raw
    /// Animator.SetTrigger call (AnimationHandler talks to the Animator
    /// directly, not through NetworkAnimator's own SetTrigger wrapper, so it
    /// would not otherwise replicate).
    /// </summary>
    public class PlayerCombat : NetworkBehaviour
    {
        [SerializeField] private MeleeWeapon weapon;
        [Tooltip("Seconds after the cast trigger before the weapon hitbox is armed.")]
        [SerializeField] private float hitboxDelay = 0.3f;
        [Tooltip("How long the weapon hitbox stays active once armed.")]
        [SerializeField] private float hitboxActiveTime = 0.25f;
        [Tooltip("Minimum time between attacks.")]
        [SerializeField] private float attackCooldown = 0.8f;

        private IAnimationHandler _animationHandler;
        private float _lastAttackTime = float.NegativeInfinity;
        private bool _isAttacking;

        private void Awake()
        {
            _animationHandler = GetComponent<IAnimationHandler>();
        }

        private void Update()
        {
            if (!IsOwner || _isAttacking)
            {
                return;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
                && Time.time - _lastAttackTime >= attackCooldown)
            {
                BeginAttack();
            }
        }

        private void BeginAttack()
        {
            _isAttacking = true;
            _lastAttackTime = Time.time;

            _animationHandler.Cast(AbilityAnimationFlow.Melee);
            PlayCast_ServerRpc();

            Invoke(nameof(ArmWeapon), hitboxDelay);
            Invoke(nameof(FinishAttack), hitboxDelay + hitboxActiveTime);
        }

        private void ArmWeapon()
        {
            ArmWeapon_ServerRpc();
        }

        private void FinishAttack()
        {
            _isAttacking = false;
            _animationHandler.FinishCast(AbilityAnimationFlow.Melee);
            PlayFinishCast_ServerRpc();
            DisarmWeapon_ServerRpc();
        }

        [ServerRpc]
        private void PlayCast_ServerRpc()
        {
            PlayCast_ClientRpc();
        }

        [ClientRpc]
        private void PlayCast_ClientRpc()
        {
            if (IsOwner)
            {
                return;
            }

            _animationHandler.Cast(AbilityAnimationFlow.Melee);
        }

        [ServerRpc]
        private void PlayFinishCast_ServerRpc()
        {
            PlayFinishCast_ClientRpc();
        }

        [ClientRpc]
        private void PlayFinishCast_ClientRpc()
        {
            if (IsOwner)
            {
                return;
            }

            _animationHandler.FinishCast(AbilityAnimationFlow.Melee);
        }

        [ServerRpc]
        private void ArmWeapon_ServerRpc()
        {
            weapon.EnableCollision();
        }

        [ServerRpc]
        private void DisarmWeapon_ServerRpc()
        {
            weapon.DisableCollision();
        }
    }
}
