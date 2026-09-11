using JohnStairs.RCC.Inputs;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FYP.Combat
{
    public class PlayerCombat : NetworkBehaviour
    {
        public List<AttackSO> attacks;
        float lastAttackBeginTime;
        float lastAttackEndTime;
        [SerializeField]
        int attackCounter;

        bool isAttacking;

        public MeleeWeapon weapon;
        [SerializeField]
        Animator animator;
        RPGInputActions inputAction;

        private void Awake()
        {
            inputAction = RPGInputManager.GetInputActions();
            inputAction.Character.Attack.performed += Attack;

        }

        private void Attack(InputAction.CallbackContext context)
        {
            if (!IsLocalPlayer)
                return;
            if (Time.time - lastAttackEndTime > 2f && attackCounter <= attacks.Count)
            {
                CancelInvoke("EndComboAttack");
                if (Time.time - lastAttackBeginTime >= 1f)
                {
                    animator.runtimeAnimatorController = attacks[attackCounter].controller;
                    animator.Play("Attack", 1, 0);
                    Attack_ServerRPC(attackCounter);

                    attackCounter++;
                    lastAttackBeginTime = Time.time;
                    isAttacking = true;
                    if (attackCounter >= attacks.Count)
                        attackCounter = 0;
                }
            }
        }

        [ServerRpc]
        void Attack_ServerRPC(int value)
        {
            weapon.EnableCollision();
            Attack_ClientRPC(value);
        }
        [ServerRpc]
        void EndAttack_ServerRPC()
        {
            weapon.DisableCollision();
            EndAttack_ClientRPC();
        }
        [ClientRpc]
        void EndAttack_ClientRPC()
        {
            weapon.DisableCollision();
        }

        [ClientRpc]
        void Attack_ClientRPC(int value)
        {
            if (IsLocalPlayer)
                return;
            animator.runtimeAnimatorController = attacks[value].controller;
            animator.Play("Attack", 1, 0);

        }

        private void Update()
        {
            if (!IsLocalPlayer)
                return;
            if (isAttacking)
                EndAttack();
        }

        void EndAttack()
        {
            if (!IsLocalPlayer)
                return;
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime > 0.9f && animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack"))
            {
                isAttacking = false;
                Debug.Log("EndComboAttack");
                Invoke("EndComboAttack", 1);
                EndAttack_ServerRPC();
            }
        }

        void EndComboAttack()
        {
            if (!IsOwner)
                return;
            Debug.Log("End Combo Attack");
            attackCounter = 0;
            lastAttackEndTime = Time.time;
        }
    }
}