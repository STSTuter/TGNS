using JohnStairs.RCC.Character.Cam;
using JohnStairs.RCC.Character.Motor;
using JohnStairs.RCC.Character.Motor.Enums;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Swaps the third person orbit stack (RPGCamera, RPGViewFrustum, RPGController) on the networked player
/// prefab for the project's first person stack. RPGMotor is kept, so locomotion tuning and the animator
/// parameters that remote players see stay exactly as they are.
///
/// Re-running the conversion is safe: existing components are reused instead of duplicated.
/// </summary>
public static class FirstPersonPlayerSetup
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Character.prefab";
    private const string MenuPath = "TGNS/Setup/Convert Player Prefab To First Person";

    [MenuItem(MenuPath)]
    public static void ConvertPlayerPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

        if (prefabRoot == null)
        {
            Debug.LogError($"[FirstPersonPlayerSetup] Could not load {PlayerPrefabPath}.");
            return;
        }

        try
        {
            // RPGCamera declares RequireComponent(RPGViewFrustum), so it has to go first
            RemoveComponent<RPGCamera>(prefabRoot);
            RemoveComponent<RPGViewFrustum>(prefabRoot);
            RemoveComponent<RPGController>(prefabRoot);

            FirstPersonCamera firstPersonCamera = GetOrAddComponent<FirstPersonCamera>(prefabRoot);
            FirstPersonController firstPersonController = GetOrAddComponent<FirstPersonController>(prefabRoot);
            // Both enable themselves for the owning client in OnNetworkSpawn, like the RPG scripts they replace
            firstPersonCamera.enabled = false;
            firstPersonController.enabled = false;

            RPGMotor motor = prefabRoot.GetComponent<RPGMotor>();
            if (motor != null)
            {
                // The camera owns body rotation now; the motor must not re-align the character on its own
                motor.AlignWithCamera = CharacterAlignment.Never;
                motor.AlsoRotateCamera = WithCameraRotation.Never;
            }
            else
            {
                Debug.LogWarning($"[FirstPersonPlayerSetup] {PlayerPrefabPath} has no RPGMotor; the first person scripts have nothing to drive.");
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            Debug.Log($"[FirstPersonPlayerSetup] Converted {PlayerPrefabPath} to the first person stack. Review the asset diff before committing.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static void RemoveComponent<T>(GameObject root) where T : Component
    {
        foreach (T component in root.GetComponentsInChildren<T>(true))
        {
            Object.DestroyImmediate(component, true);
        }
    }

    private static T GetOrAddComponent<T>(GameObject root) where T : Component
    {
        T component = root.GetComponent<T>();
        return component != null ? component : root.AddComponent<T>();
    }
}
