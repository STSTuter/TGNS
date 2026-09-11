using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FYP.Combat
{
    [CreateAssetMenu(menuName = "Attacks/Attack")]
    public class AttackSO : ScriptableObject
    {
        public AnimatorOverrideController controller;
    }
}
