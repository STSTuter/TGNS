using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace FYP.Combat
{
    public class MeleeWeapon : NetworkBehaviour
    {
        [SerializeField]
        int damage = 10;

        [SerializeField]
        BoxCollider collider;
        [SerializeField]
        PlayerCombat palyerRef;

        [SerializeField]
        Health ownHealthRef;

        private void OnEnable()
        {
            palyerRef.weapon = this;
            DisableCollision();
        }

        private void OnTriggerEnter(Collider other)
        {
            other.TryGetComponent(out Health health);
            if (health == null || health == ownHealthRef) return;

            health.ChangeHealth(-damage);
            DisableCollision();
        }

        public void EnableCollision()
        {
            collider.enabled = true;
        }

        public void DisableCollision()
        {
            collider.enabled = false;
        }



    }
}