using System.Collections;
using System.Collections.Generic;
using JohnStairs.RPG.Character;
using UnityEngine;

namespace JohnStairs.RPG {
    public class Carrier : MonoBehaviour {
        /// <summary>
        /// Start position and rotation
        /// </summary>
        [Tooltip("Start position and rotation")]
        public Transform StartingPose;
        /// <summary>
        /// Target position and rotation
        /// </summary>
        [Tooltip("Target position and rotation")]
        public Transform TargetPose;
        /// <summary>
        /// Time it takes for one way in seconds
        /// </summary>
        [Tooltip("Time it takes for one way in seconds")]
        public float TravelDuration = 2.0f;
        /// <summary>
        /// Time the carrier waits before moving back
        /// </summary>
        [Tooltip("Time the carrier waits before moving back")]
        public float WaitTime = 2.0f;

        /// <summary>
        /// Start pose
        /// </summary>
        protected Transform _start;
        /// <summary>
        /// Current target pose to move to and align with
        /// </summary>
        protected Transform _target;
        /// <summary>
        /// Parameter for interpolation, here: passed time
        /// </summary>
        protected float _t;
        /// <summary>
        /// If true, the carrier is currently waiting at the start/target pose
        /// </summary>
        protected bool _waiting;
        /// <summary>
        /// The collider for detecting new or leaving passengers
        /// </summary>
        protected BoxCollider _boardingTrigger;
        /// <summary>
        /// Set of all passengers where jumps are affected by the carrier
        /// </summary>
        protected HashSet<ITransportable> _specialPassengers;

        protected virtual void Start() {
            _start = StartingPose;
            _target = TargetPose;

            BoxCollider[] boxColliders = GetComponents<BoxCollider>();
            foreach (BoxCollider boxCollider in boxColliders) {
                if (boxCollider.isTrigger) {
                    _boardingTrigger = boxCollider;
                    break;
                }
            }

            if (!_boardingTrigger) {
                Debug.LogWarning("No trigger collider on game object " + name + " found! Please attach a collider with \"Is Trigger\" = true to make the Carrier component work");
            }

            _specialPassengers = new HashSet<ITransportable>();
        }

        // FixedUpdate because of character controller collision detection
        protected virtual void FixedUpdate() {
            if (Vector3.Distance(transform.position, _target.position) < 0.01f
                && Quaternion.Angle(transform.rotation, _target.rotation) < 0.5f
                && !_waiting) {
                if (_target == TargetPose) {
                    // Turning point reached
                    _start = TargetPose;
                    _target = StartingPose;
                } else {
                    // Back at starting point
                    _start = StartingPose;
                    _target = TargetPose;
                }
                // Reset the interpolation parameter
                _t = 0;
                // Pause movement
                StartCoroutine(WaitingCoroutine());
            }

            HashSet<ITransportable> currentPassengers = new();
            // Process the special passengers to check if they are still aboard
            foreach (ITransportable passenger in _specialPassengers) {
                if (IsAboveCarrier(passenger)) {
                    currentPassengers.Add(passenger);
                } else {
                    // No longer above the carrier => no longer consider it as passenger and unparent it
                    UnparentMotor(passenger);
                }
            }
            _specialPassengers = currentPassengers;

            if (_waiting) {
                // Continue waiting
                return;
            }

            _t += Time.deltaTime / TravelDuration;

            Vector3 deltaTranslation = transform.position;
            Quaternion deltaRotation = transform.rotation;
            // Translate and rotate this object
            transform.position = Vector3.Lerp(_start.position, _target.position, _t);
            transform.rotation = Quaternion.Lerp(_start.rotation, _target.rotation, _t);
            // Compute the deltas
            deltaTranslation = transform.position - deltaTranslation;
            deltaRotation = transform.rotation * Quaternion.Inverse(deltaRotation);
        }

        protected virtual IEnumerator WaitingCoroutine() {
            _waiting = true;
            yield return new WaitForSeconds(WaitTime);
            _waiting = false;
        }

        /// <summary>
        /// Checks if the given passenger is still above the carrier
        /// </summary>
        /// <param name="transportable">The ITransportable component of the passenger to check</param>
        /// <returns>True if the passenger is above the boarding trigger collider, otherwise false</returns>
        protected virtual bool IsAboveCarrier(ITransportable transportable) {
            Vector3 triggerSize = _boardingTrigger.size; // local trigger collider size
            Vector3 characterPositionLocal = transform.InverseTransformPoint(transportable.GetTransform().position);

            if (characterPositionLocal.y < _boardingTrigger.center.y - triggerSize.y * 0.5f) {
                // Position is below the box collider => passenger left the carrier
                return false;
            }

            float characterControllerRadius = transportable.GetColliderRadius();

            if (characterPositionLocal.x - characterControllerRadius <= triggerSize.x * 0.5f
                && characterPositionLocal.x + characterControllerRadius >= -triggerSize.x * 0.5f
                && characterPositionLocal.z - characterControllerRadius <= triggerSize.z * 0.5f
                && characterPositionLocal.z + characterControllerRadius >= -triggerSize.z * 0.5f) {
                // Passenger is between the bounds of the trigger collider in the XZ plane
                return true;
            }

            // Passenger must have left the carrier
            return false;
        }

        /// <summary>
        /// Makes the passed ITransportable a child of this carrier game object
        /// </summary>
        /// <param name="transportable">To-be child of this object</param>
        protected virtual void ParentMotor(ITransportable transportable) {
            transportable.GetTransform().SetParent(this.transform);
        }

        /// <summary>
        /// Removes the passed child ITransportable this carrier game object
        /// </summary>
        /// <param name="transportable">Child ITransportable to be unparented</param>
        protected virtual void UnparentMotor(ITransportable transportable) {
            if (transportable.GetTransform().parent == this.transform) {
                transportable.GetTransform().SetParent(null);
            }
        }

        /// <summary>
        /// "OnTriggerEnter happens on the FixedUpdate function when two GameObjects collide" - Unity Documentation
        /// </summary>
        /// <param name="other">Collider that entered the trigger collider</param>
        protected virtual void OnTriggerEnter(Collider other) {
            ITransportable transportable = other.GetComponent<ITransportable>();

            if (transportable != null) {
                ParentMotor(transportable);

                if (transportable.JumpsAffectedByCarrier()) {
                    // Memorize them as we cannot unparent them in OnTriggerExit
                    _specialPassengers.Add(transportable);
                }
            }
        }

        /// <summary>
        /// "OnTriggerExit is called when the Collider other has stopped touching the trigger" - Unity Documentation
        /// </summary>
        /// <param name="other">Left trigger collider</param>
        protected virtual void OnTriggerExit(Collider other) {
            ITransportable transportable = other.GetComponent<ITransportable>();

            if (transportable != null
                && !transportable.JumpsAffectedByCarrier()) {
                UnparentMotor(transportable);
            }
        }
    }
}
