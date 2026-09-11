using JohnStairs.RPG.Character.Controller;
using UnityEngine;

/// <summary>
/// MMORPGController whose mouselook engages the moment the orbit button goes down, instead of waiting for
/// the first mouse movement afterwards.
///
/// RPGController.HandleCameraOrbiting() calls CursorHandler.HideCursor() only while _cameraOrbitingActive is
/// true, and the vendor only flips that flag once OrbitingAmount() is non-zero. The practical effect is that
/// holding right-click left the cursor sitting on screen until you nudged the mouse. Dropping the movement
/// requirement means pressing the orbit button alone is enough to hide and pin the cursor.
///
/// Subclassed rather than edited in place: SetCameraOrbitingActive is virtual for exactly this, and anything
/// changed under Assets/John Stairs is lost on the next package reimport.
/// </summary>
[DefaultExecutionOrder(50)] // DefaultExecutionOrder is not inherited, and this must still beat RPGMotor (100).
public class TGNSMMOController : MMORPGController
{
    protected override void SetCameraOrbitingActive()
    {
        // Unchanged from the vendor: the button may only start orbiting if the press did not land on UI.
        _cameraOrbitingActivation = _cameraOrbitingActivation
                                    || (_inputHandler.ActivateOrbitingStart() && !IsCursorOverUI());

        // Vendor: _cameraOrbitingActive || (_cameraOrbitingActivation && _inputHandler.OrbitingAmount().magnitude > 0)
        _cameraOrbitingActive = _cameraOrbitingActive || _cameraOrbitingActivation;

        if (_inputHandler.ActivateOrbitingStop())
        {
            _cameraOrbitingActivation = false;
            _cameraOrbitingActive = false;
        }
    }
}
