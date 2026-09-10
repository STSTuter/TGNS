using TGNS.Combat;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TGNS.EditorTools
{
    /// <summary>
    /// Drops a few simple damageable cubes into the currently open scene near
    /// PlayerSpawnPoints, purely for testing PlayerCombat/MeleeWeapon/Health
    /// without needing a second player. Each cube is placed directly in the
    /// scene (not a prefab) with an in-scene NetworkObject, the same pattern
    /// Assets/Prefabs/Player.prefab already uses - Netcode spawns in-scene
    /// NetworkObjects automatically when the server starts, no NetworkPrefabsList
    /// registration needed.
    /// </summary>
    public static class DamageTestDummySetup
    {
        private const string DummyRootName = "DamageTestDummies";
        private const int DummyCount = 3;
        private const float Spacing = 2.5f;
        private const float ForwardOffset = 3f;

        [MenuItem("Tools/TGNS/Add Damage Test Dummies To Scene")]
        public static void AddDummies()
        {
            GameObject spawnPoints = GameObject.Find("PlayerSpawnPoints");
            Vector3 originPosition = Vector3.zero;
            Quaternion originRotation = Quaternion.identity;

            if (spawnPoints != null)
            {
                Transform firstSpawn = spawnPoints.transform.childCount > 0 ? spawnPoints.transform.GetChild(0) : spawnPoints.transform;
                originPosition = firstSpawn.position;
                originRotation = firstSpawn.rotation;
            }
            else
            {
                Debug.LogWarning("DamageTestDummySetup: no 'PlayerSpawnPoints' GameObject found in the open scene, placing dummies at the world origin instead.");
            }

            Transform existingRoot = GameObject.Find(DummyRootName)?.transform;
            if (existingRoot != null)
            {
                Object.DestroyImmediate(existingRoot.gameObject);
            }

            GameObject root = new GameObject(DummyRootName);
            Vector3 forward = originRotation * Vector3.forward;
            Vector3 right = originRotation * Vector3.right;
            Vector3 rowStart = originPosition + forward * ForwardOffset - right * (Spacing * (DummyCount - 1) / 2f);

            for (int i = 0; i < DummyCount; i++)
            {
                Vector3 position = rowStart + right * (Spacing * i);
                CreateDummy(root.transform, position, $"DamageTestDummy_{i + 1}");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log($"DamageTestDummySetup: added {DummyCount} damageable cubes under '{DummyRootName}' near PlayerSpawnPoints.");
        }

        private static void CreateDummy(Transform parent, Vector3 position, string name)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.position = position;
            cube.transform.localScale = Vector3.one;

            // A Rigidbody (kinematic - it should never move) ensures trigger
            // callbacks reliably fire against the weapon's moving hitbox collider.
            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            cube.AddComponent<NetworkObject>();
            cube.AddComponent<Health>();
        }
    }
}
