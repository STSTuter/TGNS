using System;

namespace JohnStairs.RPG.Character.Controller.Subcomponents {
    [Serializable]
    public struct InputByPhase {
        /// <summary>
        /// True if the input press started this frame
        /// </summary>
        public bool Start;
        /// <summary>
        /// True if the input was released this frame
        /// </summary>
        public bool Stop;
    }
}
