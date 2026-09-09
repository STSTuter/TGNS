using JohnStairs.RPG.Combat.Abilities;
using JohnStairs.RPG.Combat.Abilities.Enums;
using UnityEngine;

namespace JohnStairs.RPG.Character.Combat {
    public interface ICharacter {
        int GetInstanceID(); // TODO replace by Unity EntityId
        Transform GetTransform();
        bool IsUsingAbility();
        void StopUsingAbility();
        bool HasTarget();
        void SetTarget(ICharacter target);
        void SetInMotion(bool inMotion);
        void UseAbility(IAbility ability);
        void SetSelectedAbilityArea(Vector3 position);
        float GetMovementSpeedMultiplier();
        Vector3 GetTargetPosition();
        bool IsDead();
        void ApplyAbilityImpact(IAbility ability, ICharacter caster);
        void ReportKill(int xp);
        void ReportDealtHealthImpact(int value, AbilityType abilityType, ICharacter target);
        /// <summary>
        /// Destroys this component and all of its subcomponents
        /// </summary>
        void Destroy();
    }
}
