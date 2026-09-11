using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Moves the locally-owned player to its assigned PlayerSpawnPoints location on spawn.
/// Toggles the CharacterController off while repositioning so it doesn't fight the teleport.
/// </summary>
public class CharacterSpawnPositioner : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            return;
        }

        if (PlayerSpawnPoints.Instance == null)
        {
            Debug.LogWarning("[CharacterSpawnPositioner] No PlayerSpawnPoints in the scene; keeping the prefab's default spawn position.");
            return;
        }

        Vector3 spawnPosition = PlayerSpawnPoints.Instance.GetSpawnPosition(OwnerClientId);
        CharacterController controller = GetComponent<CharacterController>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        transform.position = spawnPosition;

        if (controller != null)
        {
            controller.enabled = true;
        }
    }
}
