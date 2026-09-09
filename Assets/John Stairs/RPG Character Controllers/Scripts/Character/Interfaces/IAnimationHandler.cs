using JohnStairs.RPG.Character.Motor.Enums;
using JohnStairs.RPG.Combat.Abilities.Enums;

namespace JohnStairs.RPG.Character {
    public interface IAnimationHandler {
        void MovingForward(float forward);
        void Strafing(float strafing);
        void Turning(float turning);
        void SetMovementSpeed(float speed);
        void Grounded(bool grounded);
        void Jump();
        void Sliding(bool sliding);
        void Falling(bool falling);
        void Flying(bool flying);
        void Swimming(bool swimming);
        void ClimbingState(ClimbingState climbingState);
        void ClimbingUp(float up);
        void Crouching(bool crouching);
        // Combat
        void Cast(AbilityAnimationFlow animationFlow);
        void FinishCast(AbilityAnimationFlow animationFlow);
        void Cancel();
        void Die();
    }
}
