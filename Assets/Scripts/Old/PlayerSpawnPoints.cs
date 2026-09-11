using UnityEngine;

/// <summary>
/// Registry of world-placed spawn point Transforms for networked players. Drag empty
/// GameObjects into the "Spawn Points" list on this component (or leave it to auto-collect its
/// own children) instead of hardcoding spawn positions in code. Supports up to MaxPlayers points;
/// extra points beyond that are ignored, and if fewer are configured, client IDs wrap around.
/// </summary>
public class PlayerSpawnPoints : MonoBehaviour
{
    public const int MaxPlayers = 4;

    public static PlayerSpawnPoints Instance { get; private set; }

    [Tooltip("Drag world-placed empty GameObjects here, in join-order. Leave empty to auto-use this object's children instead.")]
    [SerializeField] private Transform[] spawnPoints = new Transform[0];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            AutoCollectChildren();
        }
    }

    private void AutoCollectChildren()
    {
        int childCount = Mathf.Min(transform.childCount, MaxPlayers);
        spawnPoints = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            spawnPoints[i] = transform.GetChild(i);
        }
    }

    /// <summary>
    /// Returns the world position a client should spawn at, keyed by NGO OwnerClientId.
    /// Falls back to the origin (with a warning) if no spawn points are configured.
    /// </summary>
    public Vector3 GetSpawnPosition(ulong ownerClientId)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[PlayerSpawnPoints] No spawn points configured; spawning at the origin.");
            return Vector3.zero;
        }

        int index = (int)(ownerClientId % (ulong)spawnPoints.Length);
        Transform point = spawnPoints[index];

        if (point == null)
        {
            Debug.LogWarning($"[PlayerSpawnPoints] Spawn point index {index} is unassigned; spawning at the origin.");
            return Vector3.zero;
        }

        return point.position;
    }
}
