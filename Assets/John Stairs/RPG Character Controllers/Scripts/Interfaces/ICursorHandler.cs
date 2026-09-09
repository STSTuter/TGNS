namespace JohnStairs.RPG {
    public interface ICursorHandler {
        /// <summary>
        /// Show the cursor
        /// </summary>
        void ShowCursor();

        /// <summary>
        /// Hide the cursor
        /// </summary>
        void HideCursor();

        /// <summary>
        /// Checks if the cursor is over a UI element
        /// </summary>
        /// <returns>True if the cursor is over a UI element, otherwise false</returns>
        bool IsCursorOverUI();

        /// <summary>
        /// Destroys this component and all of its subcomponents
        /// </summary>
        void Destroy();
    }
}
