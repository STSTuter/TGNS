namespace JohnStairs.RPG.Character {
    public interface IMountAnimationHandler {
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
    }
}
