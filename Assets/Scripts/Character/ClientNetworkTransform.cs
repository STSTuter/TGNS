using System.Collections;
using System.Collections.Generic;
using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkTransform : NetworkTransform
{
    public override void OnNetworkSpawn()
    {
        CanCommitToTransform = IsOwner;
        base.OnNetworkSpawn();
        DontDestroyOnLoad(gameObject);
    }

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
