using JohnStairs.RPG.Combat.Abilities;

namespace JohnStairs.RPG.Character.Combat {
    public interface IActionBar {
        void Init(IAbility[] abilities);
        void PressSlot(int slot);
        void ReleaseSlot(int slot);
        IAbility GetAbilityInSlot(int slot);
        /// <summary>
        /// Destroys this component and all of its subcomponents
        /// </summary>
        void Destroy();
    }
}
