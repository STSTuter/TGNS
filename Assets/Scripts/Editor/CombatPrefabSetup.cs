using TGNS.Combat;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace TGNS.EditorTools
{
    /// <summary>
    /// One-shot (re-runnable) setup that wires the melee combat components
    /// onto Assets/Prefabs/NetworkPlayer.prefab: Health on the root, a
    /// MeleeWeapon hitbox parented to a hand bone, and PlayerCombat tying
    /// the two together. Safe to re-run - it reuses existing components and
    /// child objects instead of duplicating them.
    /// </summary>
    public static class CombatPrefabSetup
    {
        private const string PrefabPath = "Assets/Prefabs/NetworkPlayer.prefab";
        private const string WeaponHitboxName = "MeleeWeaponHitbox";
        private static readonly string[] PreferredHandBoneNames = { "Hand_R", "hand_r", "RightHand", "Hand R" };

        [MenuItem("Tools/TGNS/Apply Combat Setup to NetworkPlayer")]
        public static void Apply()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"CombatPrefabSetup: could not load prefab at {PrefabPath}");
                return;
            }

            try
            {
                Health health = prefabRoot.GetComponent<Health>();
                if (health == null)
                {
                    health = prefabRoot.AddComponent<Health>();
                }

                Transform handBone = FindHandBone(prefabRoot.transform);
                Transform weaponParent = handBone != null ? handBone : prefabRoot.transform;

                Transform existingHitbox = weaponParent.Find(WeaponHitboxName);
                GameObject hitboxObject = existingHitbox != null ? existingHitbox.gameObject : new GameObject(WeaponHitboxName);
                hitboxObject.transform.SetParent(weaponParent, false);
                if (existingHitbox == null)
                {
                    hitboxObject.transform.localPosition = Vector3.zero;
                    hitboxObject.transform.localRotation = Quaternion.identity;
                }

                BoxCollider hitboxCollider = hitboxObject.GetComponent<BoxCollider>();
                if (hitboxCollider == null)
                {
                    hitboxCollider = hitboxObject.AddComponent<BoxCollider>();
                    hitboxCollider.isTrigger = true;
                    hitboxCollider.size = new Vector3(0.2f, 0.2f, 0.5f);
                    hitboxCollider.center = new Vector3(0f, 0f, 0.25f);
                }

                MeleeWeapon weapon = hitboxObject.GetComponent<MeleeWeapon>();
                if (weapon == null)
                {
                    weapon = hitboxObject.AddComponent<MeleeWeapon>();
                }

                SetPrivateField(weapon, "ownerHealth", health);
                SetPrivateField(weapon, "hitCollider", hitboxCollider);

                PlayerCombat combat = prefabRoot.GetComponent<PlayerCombat>();
                if (combat == null)
                {
                    combat = prefabRoot.AddComponent<PlayerCombat>();
                }

                SetPrivateField(combat, "weapon", weapon);

                if (hitboxObject.GetComponent<NetworkTransform>() == null && prefabRoot.GetComponent<Unity.Netcode.NetworkObject>() != null)
                {
                    // The hitbox rides along with the animated hand bone; it does not need
                    // its own NetworkObject/NetworkTransform, it is purely a server-side trigger
                    // parented under the already-replicated player hierarchy.
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log("CombatPrefabSetup: NetworkPlayer.prefab updated with Health/PlayerCombat/MeleeWeapon.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }

        private static Transform FindHandBone(Transform root)
        {
            foreach (string boneName in PreferredHandBoneNames)
            {
                Transform found = FindRecursive(root, boneName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static Transform FindRecursive(Transform parent, string name)
        {
            if (parent.name == name)
            {
                return parent;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform result = FindRecursive(parent.GetChild(i), name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void SetPrivateField(Object target, string fieldName, Object value)
        {
            System.Reflection.FieldInfo field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

            if (field == null)
            {
                Debug.LogError($"CombatPrefabSetup: field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }

            field.SetValue(target, value);
        }
    }
}
