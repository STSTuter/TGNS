using Unity.Scripting.LifecycleManagement;
using UnityEngine;

namespace JohnStairs.RPG {
    public partial class SceneManager : MonoBehaviour {
        /// <summary>
        /// Reference to the player character GameObject in the scene
        /// </summary>
        public GameObject PlayerCharacterObject;
        /// <summary>
        /// LayerMask defining which layers are considered ground for abilities
        /// </summary>
        public LayerMask GroundLayers = 64;
        /// <summary>
        /// LayerMask defining which layers are considered characters for combat or interaction
        /// </summary>
        public LayerMask CharacterLayers = 8;
        /// <summary>
        /// LayerMask defining which layers block line of sight for vision checks
        /// </summary>
        public LayerMask SightBlockingLayers = 256;
        /// <summary>
        /// Maximum distance for melee attacks or interactions
        /// </summary>
        public float MeleeRange = 2.0f;
        /// <summary>
        /// Angle in degrees for the character's field of view or line of sight
        /// </summary>
        public float LineOfSightDegrees = 180.0f;

        /// <summary>
        /// Singleton instance of the SceneManager
        /// </summary>
        [AutoStaticsCleanup]
        protected static SceneManager _instance;

        protected virtual void Awake() {
            if (_instance == null) {
                _instance = this;
            } else {
                Debug.LogWarning("Multiple instances of SceneManager detected.");
            }
        }

        protected virtual void Start() {
        }

        protected virtual void Update() {
        }

        public static SceneManager GetInstance() {
            if (_instance == null) {
                GameObject sceneManagerObject = new("Scene Manager");
                _instance = sceneManagerObject.AddComponent<SceneManager>();
            }
            return _instance;
        }
    }
}