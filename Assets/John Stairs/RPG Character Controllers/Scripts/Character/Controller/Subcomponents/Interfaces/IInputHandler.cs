using UnityEngine;

namespace JohnStairs.RPG.Character.Controller.Subcomponents {
    public interface IInputHandler {
        /// <summary>
        /// If true, the orbiting input was started in this frame
        /// </summary>
        bool ActivateOrbitingStart();

        /// <summary>
        /// If true, the orbiting input was released in this frame
        /// </summary>
        bool ActivateOrbitingStop();

        /// <summary>
        /// Camera orbiting amount
        /// </summary>
        Vector2 OrbitingAmount();

        /// <summary>
        /// Zoom in/out input axis
        /// </summary>
        float ZoomAmount();

        /// <summary>
        /// Fast zoom to minimum camera distance
        /// </summary>
        bool MinDistanceZoom();

        /// <summary>
        /// Fast zoom out to maximum camera distance
        /// </summary>
        bool MaxDistanceZoom();

        /// <summary>
        /// Gets the current cursor position on screen
        /// </summary>
        Vector2 CursorPosition();

        // Motor
        /// <summary>
        /// 2D axis input for the movement direction
        /// </summary>
        Vector2 Movement();

        /// <summary>
        /// Gets the forward movement input axis
        /// </summary>
        float MoveForward();

        /// <summary>
        /// Horizontal character rotation input
        /// </summary>
        float Rotate();

        /// <summary>
        /// Jump input
        /// </summary>
        bool Jump();

        /// <summary>
        /// If true, the input for aligning the character's facing direction with the camera was pressed in this frame
        /// </summary>
        bool AlignWithCamera();

        /// <summary>
        /// Sprint input
        /// </summary>
        bool Sprint();

        /// <summary>
        /// If true, walking was toggled in this frame
        /// </summary>
        bool ToggleWalking();

        /// <summary>
        /// If true, crouching was toggled in this frame
        /// </summary>
        bool ToggleCrouching();

        /// <summary>
        /// If true, autorunning was toggled in this frame
        /// </summary>
        bool ToggleAutorunning();

        /// <summary>
        /// If true, the ability to fly was toggled in this frame
        /// </summary>
        bool ToggleFlyingAbility();

        /// <summary>
        /// Ascend input (e.g. for flying up or swimming upwards)
        /// </summary>
        bool Ascend();

        /// <summary>
        /// Descend input (e.g. for flying down or swimming downwards)
        /// </summary>
        bool Descend();

        /// <summary>
        /// Select input (e.g. for targeting or confirming)
        /// </summary>
        bool Select();

        /// <summary>
        /// Cancel input (e.g. for stopping actions or closing menus)
        /// </summary>
        bool Cancel();

        /// <summary>
        /// Input states for action bar slots
        /// </summary>
        InputByPhase[] ActionBarSlots();

        /// <summary>
        /// Target lock input
        /// </summary>
        bool LockOnTarget();

        /// <summary>
        /// Cursor visibility toggle input (by default only used by the ARPG Controller)
        /// </summary>
        bool ToggleCursorVisibility();

        /// <summary>
        /// Destroys this component and all of its subcomponents
        /// </summary>
        void Destroy();
    }
}
