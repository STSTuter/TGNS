using System.Reflection;
using JohnStairs.RPG.Character;
using JohnStairs.RPG.Character.Motor;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// One-shot (re-runnable) migration for <c>Assets/Prefabs/NetworkPlayer.prefab</c>:
/// builds the new Animator Controller, drops the broken vendor model, retargets the Animator to the
/// Synty female Humanoid avatar, swaps the vendor <see cref="AnimationHandler"/> for the project
/// driver + head-look, and flips <see cref="NetworkAnimator"/> to Owner authority.
///
/// The 217 KB prefab YAML is mutated through <see cref="PrefabUtility"/> rather than by hand.
/// </summary>
public static class NetworkPlayerPrefabSetup
{
    private const string PrefabPath = "Assets/Prefabs/NetworkPlayer.prefab";
    private const string SyntyFbxPath = "Assets/Synty/PolygonGeneric/Models/Generic_Characters.fbx";
    private const string BrokenModelChild = "HumanMale_Character_FREE";

    // SampleScene's Terrain sits on layer 9, which is absent from RPGMotor.WalkableLayers (195),
    // so the motor never registers as grounded on terrain and jump/fall break. Fold the fix in here.
    private const int TerrainLayer = 9;

    [MenuItem("Tools/TGNS/Apply NetworkPlayer Prefab Changes")]
    public static void Apply()
    {
        AnimatorController controller = NetworkPlayerAnimatorBuilder.Build();
        if (controller == null)
        {
            Debug.LogError("[TGNS] Animator controller build failed - aborting prefab setup.");
            return;
        }

        Avatar avatar = LoadSyntyAvatar();
        if (avatar == null)
        {
            Debug.LogError($"[TGNS] No Humanoid Avatar found in {SyntyFbxPath} - aborting. " +
                           "Confirm the FBX is imported as Humanoid.");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            // 1. Remove the broken vendor model subtree (missing meshes + stray Animator + _M skeleton).
            Transform broken = root.transform.Find(BrokenModelChild);
            if (broken != null)
            {
                Object.DestroyImmediate(broken.gameObject);
                Debug.Log($"[TGNS] Removed '{BrokenModelChild}' subtree.");
            }

            // 2a. There must be exactly one Animator, on the root. Strip any on descendants
            //     (e.g. one hand-added to the Synty child) - two Animators fight over the rig.
            foreach (Animator extra in root.GetComponentsInChildren<Animator>(true))
            {
                if (extra.gameObject != root)
                {
                    Debug.Log($"[TGNS] Removing stray Animator on child '{extra.gameObject.name}'.");
                    Object.DestroyImmediate(extra, true);
                }
            }

            // 2b. Retarget the root Animator.
            Animator animator = root.GetComponent<Animator>();
            animator.avatar = avatar;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // 3. Drop the vendor AnimationHandler.
            AnimationHandler vendorHandler = root.GetComponent<AnimationHandler>();
            if (vendorHandler != null)
            {
                Object.DestroyImmediate(vendorHandler, true);
                Debug.Log("[TGNS] Removed vendor AnimationHandler.");
            }

            // 4. Add the project-owned components.
            if (root.GetComponent<NetworkPlayerAnimationDriver>() == null)
            {
                root.AddComponent<NetworkPlayerAnimationDriver>();
            }
            if (root.GetComponent<HumanoidHeadLook>() == null)
            {
                root.AddComponent<HumanoidHeadLook>();
            }

            // 5. NetworkAnimator -> Owner authority (matches NetworkTransform) + refresh its parameter cache.
            NetworkAnimator networkAnimator = root.GetComponent<NetworkAnimator>();
            if (networkAnimator != null)
            {
                networkAnimator.AuthorityMode = NetworkAnimator.AuthorityModes.Owner;
                RefreshNetworkAnimator(networkAnimator);
            }

            // 6. Grounded detection: make sure the Terrain's layer is walkable so the motor
            //    reports grounded on terrain (otherwise it reads as permanently airborne).
            RPGMotor motor = root.GetComponent<RPGMotor>();
            if (motor != null && (motor.WalkableLayers.value & (1 << TerrainLayer)) == 0)
            {
                motor.WalkableLayers = motor.WalkableLayers.value | (1 << TerrainLayer);
                Debug.Log($"[TGNS] Added layer {TerrainLayer} to RPGMotor.WalkableLayers " +
                          $"(now {motor.WalkableLayers.value}).");
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log("[TGNS] NetworkPlayer.prefab updated. Open it once to let NetworkAnimator " +
                      "rebuild its synced-parameter list from the new controller, then verify in the Inspector.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
    }

    private static Avatar LoadSyntyAvatar()
    {
        foreach (Object o in AssetDatabase.LoadAllAssetRepresentationsAtPath(SyntyFbxPath))
        {
            if (o is Avatar a && a.isValid && a.isHuman)
            {
                return a;
            }
        }
        return null;
    }

    private static void RefreshNetworkAnimator(NetworkAnimator networkAnimator)
    {
        // NGO rebuilds AnimatorParameterEntries in its private OnValidate; invoke it so the stale
        // vendor parameter list is replaced without needing a manual Inspector visit.
        MethodInfo onValidate = typeof(NetworkAnimator)
            .GetMethod("OnValidate", BindingFlags.NonPublic | BindingFlags.Instance);
        try
        {
            onValidate?.Invoke(networkAnimator, null);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[TGNS] Could not auto-refresh NetworkAnimator parameters ({e.Message}). " +
                             "Open NetworkPlayer.prefab in the Inspector to refresh them.");
        }
        EditorUtility.SetDirty(networkAnimator);
    }
}
