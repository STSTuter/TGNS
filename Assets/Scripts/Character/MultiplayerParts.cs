using FYP.PlayFabIntegration;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace FYP.Character
{
    public class MultiplayerParts : NetworkBehaviour
    {
        [SerializeField]
        PlayfabCharacter playfabCharacter;
        public override void OnNetworkSpawn()
        {
            
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {

            if (IsLocalPlayer)
            {
                Debug.Log("Client sent body data");
                ChangeCharacter_ServerRPC(playfabCharacter.SetData());
            }
        }

        [ServerRpc]
        void ChangeCharacter_ServerRPC(string bodyData)
        {
            Debug.Log("Server RPC body data");
            ChangeCharacter_ClientRPC(bodyData);
        }

        [ClientRpc]
        void ChangeCharacter_ClientRPC(string bodyData)
        {
            if (!IsLocalPlayer)
            {
                Debug.Log("Client recieved body data");
                Debug.Log(bodyData);
                playfabCharacter.SetDataToCharacter(bodyData);
            }
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }
}