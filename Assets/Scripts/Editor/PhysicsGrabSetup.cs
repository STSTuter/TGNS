using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Wiring for the physics carry system: adds the carry components to the networked player prefab, and turns
/// ordinary props into grabbable networked rigidbodies.
///
/// Re-running any of these is safe - existing components are reused rather than duplicated.
/// </summary>
public static class PhysicsGrabSetup
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Character.prefab";
    private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    private const string TestPropMaterialFolder = "Assets/Prefabs/Materials";
    private const string TestPropPrefix = "Grab Test ";

    [MenuItem("TGNS/Setup/Add Carry System To Player Prefab")]
    public static void AddCarrySystemToPlayerPrefab()
    {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

        if (prefabRoot == null)
        {
            Debug.LogError($"[{nameof(PhysicsGrabSetup)}] Could not load {PlayerPrefabPath}.");
            return;
        }

        try
        {
            GetOrAdd<PlayerStrength>(prefabRoot);

            // Both self-enable for the owning client in OnNetworkSpawn, like the other first person scripts
            GetOrAdd<PlayerCarry>(prefabRoot).enabled = false;
            GetOrAdd<CarryHud>(prefabRoot).enabled = false;

            if (prefabRoot.GetComponent<FirstPersonCamera>() == null)
            {
                Debug.LogWarning($"[{nameof(PhysicsGrabSetup)}] {PlayerPrefabPath} has no {nameof(FirstPersonCamera)}; " +
                                 "the carry system has no camera to aim from.");
            }

            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PlayerPrefabPath);
            Debug.Log($"[{nameof(PhysicsGrabSetup)}] Added the carry system to {PlayerPrefabPath}. Review the asset diff before committing.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    [MenuItem("TGNS/Setup/Make Selection Grabbable")]
    public static void MakeSelectionGrabbable()
    {
        GameObject[] selection = Selection.gameObjects;

        if (selection.Length == 0)
        {
            Debug.LogWarning($"[{nameof(PhysicsGrabSetup)}] Select the props to convert first.");
            return;
        }

        foreach (GameObject selected in selection)
        {
            string assetPath = AssetDatabase.GetAssetPath(selected);

            if (!string.IsNullOrEmpty(assetPath) && assetPath.EndsWith(".prefab"))
            {
                GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

                try
                {
                    ConfigureGrabbable(prefabRoot);
                    PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(prefabRoot);
                }

                Debug.Log($"[{nameof(PhysicsGrabSetup)}] Made prefab {assetPath} grabbable.", selected);
                continue;
            }

            Undo.RegisterFullObjectHierarchyUndo(selected, "Make Grabbable");
            ConfigureGrabbable(selected);
            EditorUtility.SetDirty(selected);
            EditorSceneManager.MarkSceneDirty(selected.scene);
            Debug.Log($"[{nameof(PhysicsGrabSetup)}] Made scene object {selected.name} grabbable.", selected);
        }
    }

    [MenuItem("TGNS/Setup/Create Grabbable Test Props")]
    public static void CreateGrabbableTestProps()
    {
        RemoveExistingTestProps();
        Vector3 origin = FindTestPropOrigin();

        // Masses chosen around the player prefab's default 40 kg lift capacity and 120 kg grab limit, so one
        // prop lands in each band: carried easily, carried slowly, too heavy to lift but draggable, refused.
        CreateTestProp("Grab Test Crate 5kg", 5f, origin + new Vector3(0f, 0f, -1.5f), new Color(0.78f, 0.62f, 0.36f));
        CreateTestProp("Grab Test Crate 35kg", 35f, origin + new Vector3(0f, 0f, -0.5f), new Color(0.55f, 0.40f, 0.25f));
        CreateTestProp("Grab Test Block 90kg", 90f, origin + new Vector3(0f, 0f, 0.5f), new Color(0.36f, 0.36f, 0.42f));
        CreateTestProp("Grab Test Block 250kg", 250f, origin + new Vector3(0f, 0f, 1.5f), new Color(0.20f, 0.20f, 0.22f));

        Debug.Log($"[{nameof(PhysicsGrabSetup)}] Created four grab test props at {origin}. Save the scene to keep them.");
    }

    /// <summary>
    /// Entry point for a headless run: opens the scene the build settings use, adds the test props and saves.
    /// Not a menu item, because in the Editor the menu item above already works on the open scene.
    /// </summary>
    public static void CreateGrabbableTestPropsInSampleScene()
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);

        if (!scene.IsValid())
        {
            Debug.LogError($"[{nameof(PhysicsGrabSetup)}] Could not open {SampleScenePath}.");
            return;
        }

        CreateGrabbableTestProps();
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[{nameof(PhysicsGrabSetup)}] Saved {SampleScenePath}.");
    }

    /// <summary>
    /// Puts the test props beside the carriage, so they are within walking distance of the thing they are
    /// meant to be loaded into.
    ///
    /// The height is taken from the carriage rather than from a downward raycast: in a headless run the
    /// terrain collider is not necessarily active, and the ray then falls straight through to whatever is
    /// underneath, which buries the props. They are rigidbodies, so a small drop settles them anyway.
    /// </summary>
    /// <returns>World position to build the row of test props around</returns>
    private static Vector3 FindTestPropOrigin()
    {
        GameObject carriage = GameObject.Find("Carriage");

        if (carriage == null)
        {
            Debug.LogWarning($"[{nameof(PhysicsGrabSetup)}] No GameObject named \"Carriage\" in the open scene; " +
                             "placing the test props at the world origin instead.");
            return Vector3.zero;
        }

        return carriage.transform.position + carriage.transform.right * 2.5f + Vector3.up * 0.6f;
    }

    /// <summary>
    /// Deletes props from an earlier run so the menu item can be used repeatedly without stacking duplicates.
    /// </summary>
    private static void RemoveExistingTestProps()
    {
        foreach (Grabbable grabbable in Object.FindObjectsByType<Grabbable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (grabbable.name.StartsWith(TestPropPrefix))
            {
                Undo.DestroyObjectImmediate(grabbable.gameObject);
            }
        }
    }

    /// <summary>
    /// Gives a GameObject everything the carry system and the netcode need: a body to push, an
    /// owner-authoritative transform to replicate it, and the grab bookkeeping.
    /// </summary>
    /// <param name="target">Prop to convert</param>
    private static void ConfigureGrabbable(GameObject target)
    {
        if (target.GetComponentInChildren<Collider>(true) == null)
        {
            Debug.LogWarning($"[{nameof(PhysicsGrabSetup)}] {target.name} has no collider, so it cannot be " +
                             "aimed at or collide with anything. Add one before playtesting.", target);
        }

        GetOrAdd<Rigidbody>(target);

        NetworkObject networkObject = GetOrAdd<NetworkObject>(target);
        // Without this the cargo is destroyed when the carrying client disconnects instead of being dropped
        networkObject.DontDestroyWithOwner = true;

        GetOrAdd<GrabbableNetworkTransform>(target);

        NetworkRigidbody networkRigidbody = GetOrAdd<NetworkRigidbody>(target);
        // Let the NetworkTransform drive the body rather than the transform, so interpolation stays physical
        networkRigidbody.UseRigidBodyForMotion = true;
        networkRigidbody.AutoUpdateKinematicState = true;

        GetOrAdd<Grabbable>(target);
    }

    private static void CreateTestProp(string name, float mass, Vector3 position, Color color)
    {
        GameObject prop = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prop.name = name;
        prop.transform.position = position;
        prop.GetComponent<Renderer>().sharedMaterial = GetOrCreateTestPropMaterial(name, color);

        ConfigureGrabbable(prop);
        prop.GetComponent<Rigidbody>().mass = mass;

        Undo.RegisterCreatedObjectUndo(prop, "Create Grabbable Test Prop");
        EditorSceneManager.MarkSceneDirty(prop.scene);
    }

    /// <summary>
    /// Test prop materials are saved as assets rather than left as scene-only instances, so the scene diff
    /// stays small and the colours survive a reimport.
    /// </summary>
    /// <param name="propName">Name of the prop the material belongs to</param>
    /// <param name="color">Base colour</param>
    /// <returns>Material asset for that prop</returns>
    private static Material GetOrCreateTestPropMaterial(string propName, Color color)
    {
        if (!AssetDatabase.IsValidFolder(TestPropMaterialFolder))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Materials");
        }

        string path = $"{TestPropMaterialFolder}/{propName.Replace(" ", string.Empty)}.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader) { color = color };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
}
