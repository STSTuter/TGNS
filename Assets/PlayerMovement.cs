using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owner-authoritative WASD movement for the networked player capsule. Only the owning client
/// reads input and writes its own transform.position; NetworkTransform (AuthorityMode = Owner)
/// replicates that position to the server and every other client. No Rigidbody is used.
/// </summary>
public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            return;
        }

        if (PlayerSpawnPoints.Instance != null)
        {
            transform.position = PlayerSpawnPoints.Instance.GetSpawnPosition(OwnerClientId);
        }
        else
        {
            Debug.LogWarning("[PlayerMovement] No PlayerSpawnPoints in the scene; falling back to a default offset.");
            float startX = OwnerClientId == 0 ? -2f : 2f;
            transform.position = new Vector3(startX, 1f, 0f);
        }
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        Vector2 input = ReadMovementInput();
        Vector3 delta = new Vector3(input.x, 0f, input.y) * moveSpeed * Time.deltaTime;
        transform.position += delta;
    }

    private static Vector2 ReadMovementInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return Vector2.zero;
        }

        Vector2 input = Vector2.zero;
        if (keyboard.aKey.isPressed) input.x -= 1f;
        if (keyboard.dKey.isPressed) input.x += 1f;
        if (keyboard.sKey.isPressed) input.y -= 1f;
        if (keyboard.wKey.isPressed) input.y += 1f;

        return input.sqrMagnitude > 1f ? input.normalized : input;
    }
}
