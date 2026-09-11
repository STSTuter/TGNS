using FYP.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FYP.Combat
{
    public class Health : NetworkBehaviour
    {
        [SerializeField]
        public int maxHealth = 100;
        [SerializeField]
        NetworkVariable<int> currentHealth;
        [SerializeField]
        HPUI hpUI;

        bool isDefeated;
        public override void OnNetworkSpawn()
        {
            ResetHealth();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        [ClientRpc]
        void SetUI_ClientRPC()
        {
            if (IsLocalPlayer)
                hpUI = GameObject.FindGameObjectsWithTag("HPUI")[0].GetComponent<HPUI>();
        }


        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            SetUI_ServerRPC();
        }

        [ServerRpc]
        void SetUI_ServerRPC()
        {
            SetUI_ClientRPC();
        }
            public void ResetHealth()
        {
            if (!IsServer) return;
            currentHealth.Value = maxHealth;
            ChangeHealth(0);
        }

        public void ChangeHealth(int value)
        {
            if (!IsServer) return;
            currentHealth.Value += value;
            if (currentHealth.Value > maxHealth)
                currentHealth.Value = maxHealth;

            if (currentHealth.Value <= 0)
            {
                isDefeated = true;
            }
            ChangeHP_ClientRPC();
        }
        [ClientRpc]
        void ChangeHP_ClientRPC()
        {
            if (IsLocalPlayer && hpUI != null)
                hpUI.healthCircle.fillAmount = currentHealth.Value / (float)maxHealth;
        }
    }
}