using Unity.Netcode;
using UnityEngine;

namespace TGNS.Combat
{
    /// <summary>
    /// Hitbox for melee attacks. The collider is kept disabled outside of the
    /// active-swing window (see PlayerCombat) and hit detection/damage is
    /// server-only so a client can never apply damage by spoofing a trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class MeleeWeapon : NetworkBehaviour
    {
        [SerializeField] private int damage = 10;
        [SerializeField] private Collider hitCollider;

        /// <summary>The Health this weapon belongs to, so wielders can't damage themselves.</summary>
        [SerializeField] private Health ownerHealth;

        private void Awake()
        {
            if (hitCollider == null)
            {
                hitCollider = GetComponent<Collider>();
            }

            hitCollider.isTrigger = true;
            DisableCollision();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer)
            {
                return;
            }

            if (!other.TryGetComponent(out Health targetHealth) || targetHealth == ownerHealth)
            {
                return;
            }

            targetHealth.ChangeHealth(-damage);
            DisableCollision();
        }

        public void EnableCollision()
        {
            hitCollider.enabled = true;
        }

        public void DisableCollision()
        {
            hitCollider.enabled = false;
        }
    }
}
