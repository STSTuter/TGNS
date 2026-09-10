using JohnStairs.RPG;
using JohnStairs.RPG.Character.Cam;
using JohnStairs.RPG.Character.Controller;
using JohnStairs.RPG.Character.Controller.Subcomponents;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TGNS.EditorTools
{
    /// <summary>
    /// Wires up the vendor's third-person orbit stack (MMORPGController +
    /// InputHandler + CursorHandler + the already-present RPGCamera) onto
    /// Assets/Prefabs/NetworkPlayer.prefab, reusing the existing FPSCamera
    /// Camera object, and points PlayerControlModeSwitcher at everything so
    /// it can gate ownership/mode correctly. Safe to re-run.
    /// </summary>
    public static class OrbitControllerSetup
    {
        private const string PrefabPath = "Assets/Prefabs/NetworkPlayer.prefab";
        private const string InputActionsPath = "Assets/John Stairs/RPG Character Controllers/Input/RPGInputActions.inputactions";

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

                Transform fpsCameraTransform = FindRecursive(prefabRoot.transform, "FPSCamera");
                Camera usedCamera = fpsCameraTransform != null ? fpsCameraTransform.GetComponent<Camera>() : null;
                if (usedCamera != null)
                {
                    rpgCamera.UsedCamera = usedCamera;
                }
                else
                {
                    Debug.LogWarning("OrbitControllerSetup: could not find an 'FPSCamera' child with a Camera component to reuse for RPGCamera.UsedCamera - assign it manually.");
                }

                PlayerInput playerInput = prefabRoot.GetComponent<PlayerInput>();
                if (playerInput == null)
                {
                    playerInput = prefabRoot.AddComponent<PlayerInput>();
                }

                InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
                if (actions != null)
                {
                    playerInput.actions = actions;
                }
                else
                {
                    Debug.LogWarning($"OrbitControllerSetup: could not find input actions asset at {InputActionsPath} - assign PlayerInput.actions manually.");
                }

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

                MMORPGController controller = prefabRoot.GetComponent<MMORPGController>();
                if (controller == null)
                {
                    controller = prefabRoot.AddComponent<MMORPGController>();
                }

                NetworkedFirstPersonController fpsController = prefabRoot.GetComponent<NetworkedFirstPersonController>();
                GameObject fpsCameraObject = fpsCameraTransform != null ? fpsCameraTransform.gameObject : null;

                PlayerControlModeSwitcher switcher = prefabRoot.GetComponent<PlayerControlModeSwitcher>();
                if (switcher == null)
                {
                    switcher = prefabRoot.AddComponent<PlayerControlModeSwitcher>();
                }

                SetPrivateField(switcher, "firstPersonController", fpsController);
                SetPrivateField(switcher, "firstPersonCameraObject", fpsCameraObject);
                SetPrivateField(switcher, "orbitController", controller);
                SetPrivateField(switcher, "orbitCamera", rpgCamera);
                SetPrivateField(switcher, "inputHandler", inputHandler);
                SetPrivateField(switcher, "cursorHandler", cursorHandler);
                SetPrivateField(switcher, "playerInput", playerInput);

                // These start disabled; PlayerControlModeSwitcher.OnNetworkSpawn enables
                // the right set for the owner (defaults to orbit mode) and disables
                // everything for non-owners. Leaving them off here avoids any of this
                // running before ownership is known (e.g. in the Editor outside Play Mode).
                controller.enabled = false;
                rpgCamera.enabled = false;
                inputHandler.enabled = false;
                cursorHandler.enabled = false;
                playerInput.enabled = false;

                PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
                Debug.Log("OrbitControllerSetup: NetworkPlayer.prefab updated with MMORPGController/InputHandler/CursorHandler, RPGCamera reusing FPSCamera, and PlayerControlModeSwitcher wired (starts in orbit mode).");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
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
