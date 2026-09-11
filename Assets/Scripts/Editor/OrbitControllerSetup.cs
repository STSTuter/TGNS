using JohnStairs.RPG;
using JohnStairs.RPG.Character.Cam;
using JohnStairs.RPG.Character.Controller;
using JohnStairs.RPG.Character.Controller.Subcomponents;
using JohnStairs.RPG.Character.Motor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TGNS.EditorTools
{
    /// <summary>
    /// Wires the vendor's MMO orbit stack (MMORPGController + InputHandler + CursorHandler + the
    /// already-present RPGCamera) onto Assets/Prefabs/NetworkPlayer.prefab, reusing the existing
    /// camera child, and points NetworkPlayerOwnership at everything so it can gate ownership
    /// correctly. Removes the retired first-person stack. Safe to re-run.
    /// </summary>
    public static class OrbitControllerSetup
    {
        private const string PrefabPath = "Assets/Prefabs/NetworkPlayer.prefab";
        // Project-owned copy of the vendor's RPGInputActions. Kept out of Assets/John Stairs so a package
        // reimport cannot clobber it (see the "reimported controller asset" commit, which rewrote vendor
        // assets in place). Differs from the vendor original only in that Activate Orbiting has lost its
        // <Mouse>/leftButton binding, leaving right-click as the sole mouse orbit trigger and left-click
        // free for Select.
        private const string InputActionsPath = "Assets/Input/TGNSRPGInputActions.inputactions";
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string CameraObjectName = "PlayerCamera";
        private const string LegacyCameraObjectName = "FPSCamera";

        /// <summary>
        /// Keeps RPGCameraLite.EnableFirstPersonMode() (distance &lt; 0.1) from ever triggering, which would
        /// park the camera inside the character's own head mesh.
        /// </summary>
        private const float MinCameraDistance = 1.0f;

        [MenuItem("Tools/TGNS/Apply Orbit Controller Setup to NetworkPlayer")]
        public static void Apply()
        {
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
            if (prefabRoot == null)
            {
                Debug.LogError($"OrbitControllerSetup: could not load prefab at {PrefabPath}");
                return;
            }

            try
            {
                RPGCamera rpgCamera = prefabRoot.GetComponent<RPGCamera>();
                if (rpgCamera == null)
                {
                    Debug.LogError("OrbitControllerSetup: no RPGCamera found on the prefab root. Aborting.");
                    return;
                }

                // --- Camera child -------------------------------------------------------------
                Transform cameraTransform = FindRecursive(prefabRoot.transform, CameraObjectName)
                                            ?? FindRecursive(prefabRoot.transform, LegacyCameraObjectName);
                Camera usedCamera = cameraTransform != null ? cameraTransform.GetComponent<Camera>() : null;
                GameObject cameraObject = cameraTransform != null ? cameraTransform.gameObject : null;

                if (usedCamera != null)
                {
                    rpgCamera.UsedCamera = usedCamera;

                    // No longer a first-person camera.
                    cameraObject.name = CameraObjectName;

                    // NetworkPlayerOwnership disables the scene MainCamera once the local player spawns.
                    // Without this tag Camera.main would then be null for the rest of the session, breaking
                    // RPGCameraLite.DetermineUsedCamera() and RPGController.GetCamera(). Only the owner
                    // camera object is ever active, so exactly one tagged camera exists at runtime.
                    cameraObject.tag = "MainCamera";

                    // Authored off; the owner turns it on.
                    cameraObject.SetActive(false);
                }
                else
                {
                    Debug.LogWarning("OrbitControllerSetup: could not find a PlayerCamera or FPSCamera child "
                                     + "with a Camera component - assign RPGCamera.UsedCamera manually.");
                }

                rpgCamera.MinDistance = MinCameraDistance;

                // --- Input --------------------------------------------------------------------
                PlayerInput playerInput = prefabRoot.GetComponent<PlayerInput>();
                if (playerInput == null)
                {
                    playerInput = prefabRoot.AddComponent<PlayerInput>();
                }

                InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
                if (actions != null)
                {
                    playerInput.actions = actions;

                    // Without a default action map the actions are never enabled, so InputHandler resolves
                    // every action successfully but reads nothing but zeros. Matches the vendor prefab
                    // Prefabs/Characters/Character MMO.prefab.
                    playerInput.defaultActionMap = "Character";
                }
                else
                {
                    Debug.LogWarning($"OrbitControllerSetup: could not find input actions asset at {InputActionsPath} - assign PlayerInput.actions manually.");
                }

                // InputHandler polls actions directly, so PlayerInput must not also broadcast messages.
                playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

                InputHandler inputHandler = prefabRoot.GetComponent<InputHandler>();
                if (inputHandler == null)
                {
                    inputHandler = prefabRoot.AddComponent<InputHandler>();
                }

                CursorHandler cursorHandler = prefabRoot.GetComponent<CursorHandler>();
                if (cursorHandler == null)
                {
                    cursorHandler = prefabRoot.AddComponent<CursorHandler>();
                }

                // --- Controller ---------------------------------------------------------------
                // Drop a plain MMORPGController left over from an earlier run. TGNSMMOController derives from
                // it, so GetComponent<MMORPGController>() matches both - the type check is what separates them.
                foreach (MMORPGController existing in prefabRoot.GetComponents<MMORPGController>())
                {
                    if (!(existing is TGNSMMOController))
                    {
                        Object.DestroyImmediate(existing, true);
                    }
                }

                TGNSMMOController controller = prefabRoot.GetComponent<TGNSMMOController>();
                if (controller == null)
                {
                    controller = prefabRoot.AddComponent<TGNSMMOController>();
                }

                RPGMotor motor = prefabRoot.GetComponent<RPGMotor>();
                if (motor == null)
                {
                    Debug.LogWarning("OrbitControllerSetup: no RPGMotor on the prefab root - the controller will have nothing to drive.");
                }

                // --- Retire the first-person stack --------------------------------------------
                // Handles the case where the scripts still exist...
                RemoveComponentByTypeName(prefabRoot, "NetworkedFirstPersonController");
                RemoveComponentByTypeName(prefabRoot, "PlayerControlModeSwitcher");
                // ...and the case where the .cs files are already gone, which leaves the prefab carrying a
                // null "missing script" component that no type-name check can ever match.
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(prefabRoot);
                if (removed > 0)
                {
                    Debug.Log($"OrbitControllerSetup: removed {removed} missing-script component(s) from the prefab root.");
                }

                // --- Ownership gate -----------------------------------------------------------
                NetworkPlayerOwnership ownership = prefabRoot.GetComponent<NetworkPlayerOwnership>();
                if (ownership == null)
                {
                    ownership = prefabRoot.AddComponent<NetworkPlayerOwnership>();
                }

                SetPrivateField(ownership, "controller", controller);
                SetPrivateField(ownership, "rpgCamera", rpgCamera);
                SetPrivateField(ownership, "inputHandler", inputHandler);
                SetPrivateField(ownership, "cursorHandler", cursorHandler);
                SetPrivateField(ownership, "playerInput", playerInput);
                SetPrivateField(ownership, "motor", motor);
                SetPrivateField(ownership, "cameraObject", cameraObject);

                // Authored disabled so none of this runs before ownership is known (e.g. in the Editor
                // outside Play Mode). NetworkPlayerOwnership.OnNetworkSpawn enables the set for the owner
                // and leaves it off for remote replicas.
                controller.enabled = false;
                rpgCamera.enabled = false;
                inputHandler.enabled = false;
                cursorHandler.enabled = false;
                playerInput.enabled = false;
                if (motor != null)
                {
                    motor.enabled = false;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                AssetDatabase.SaveAssets();
                Debug.Log("OrbitControllerSetup: NetworkPlayer.prefab updated - MMORPGController/InputHandler/"
                          + "CursorHandler/PlayerInput added, RPGCamera bound to the tagged PlayerCamera child, "
                          + "NetworkPlayerOwnership wired, first-person stack removed.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            EnsureSceneManager();
        }

        /// <summary>
        /// RPGController reaches for JohnStairs.RPG.SceneManager.GetInstance() during target selection and
        /// ability area selection. GetInstance() will happily construct one at runtime, but then its layer
        /// masks are whatever the class defaults say. Author one in the gameplay scene instead so the masks
        /// are visible and editable.
        ///
        /// In batch mode no scene is open, so the target scene has to be opened and saved explicitly -
        /// otherwise the object lands in the throwaway untitled scene and is lost on quit. Interactively the
        /// scene is only edited if it is already the open one; saving is left to the user.
        /// </summary>
        private static void EnsureSceneManager()
        {
            if (Application.isBatchMode)
            {
                UnityEngine.SceneManagement.Scene opened = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (AddSceneManagerIfMissing(opened))
                {
                    EditorSceneManager.SaveScene(opened);
                    Debug.Log($"OrbitControllerSetup: added a Scene Manager to {ScenePath} and saved the scene.");
                }
                return;
            }

            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogWarning($"OrbitControllerSetup: {ScenePath} is not the open scene, so its Scene Manager "
                                 + "was not checked. Open it and re-run if it has none.");
                return;
            }

            if (AddSceneManagerIfMissing(scene))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                Debug.Log($"OrbitControllerSetup: added a Scene Manager to '{scene.name}'. Save the scene to keep it.");
            }
        }

        /// <returns>True if a Scene Manager was created, false if the scene already had one.</returns>
        private static bool AddSceneManagerIfMissing(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<JohnStairs.RPG.SceneManager>(true) != null)
                {
                    return false;
                }
            }

            GameObject sceneManagerObject = new GameObject("Scene Manager");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(sceneManagerObject, scene);

            JohnStairs.RPG.SceneManager sceneManager = sceneManagerObject.AddComponent<JohnStairs.RPG.SceneManager>();
            sceneManager.CharacterLayers = 1 << 3;  // the NetworkPlayer layer
            sceneManager.GroundLayers = 1 << 0;     // Default

            return true;
        }

        private static void RemoveComponentByTypeName(GameObject root, string typeName)
        {
            foreach (Component component in root.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name == typeName)
                {
                    Object.DestroyImmediate(component, true);
                }
            }
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
                Debug.LogError($"OrbitControllerSetup: field '{fieldName}' not found on {target.GetType().Name}");
                return;
            }

            field.SetValue(target, value);
        }
    }
}
