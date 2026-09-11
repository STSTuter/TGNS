using JohnStairs.RPG.Character.Controller;
using UnityEditor;

/// <summary>
/// The vendor's [CustomEditor(typeof(MMORPGController))] does not opt into child classes, so without this
/// TGNSMMOController would fall back to the default inspector.
/// </summary>
[CustomEditor(typeof(TGNSMMOController)), CanEditMultipleObjects]
public class TGNSMMOControllerEditor : MMORPGControllerEditor
{
}
