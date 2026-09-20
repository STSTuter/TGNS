using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Minimal owner-only IMGUI readout for the carry system: a crosshair, what is under it, what is in the
/// player's hands, and why the last grab was refused. It exists so the mechanic can be playtested and
/// tuned before there is a real HUD; delete or replace it once the game has one.
/// </summary>
[RequireComponent(typeof(PlayerCarry))]
public class CarryHud : NetworkBehaviour
{
    [Tooltip("Draw the crosshair and prompts. Turn off to playtest without any overlay.")]
    [SerializeField] private bool showHud = true;

    [Tooltip("How long a refusal message such as \"too heavy\" stays on screen, in seconds.")]
    [SerializeField] private float refusalDuration = 1.5f;

    private PlayerCarry m_Carry;
    private PlayerStrength m_Strength;
    private GUIStyle m_Style;

    protected virtual void Awake()
    {
        m_Carry = GetComponent<PlayerCarry>();
        m_Strength = GetComponent<PlayerStrength>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsClient || !IsOwner)
        {
            return;
        }

        enabled = true;
    }

    protected virtual void OnGUI()
    {
        if (!showHud || !IsSpawned || !IsOwner)
        {
            return;
        }

        m_Style ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 14
        };

        float centreX = Screen.width * 0.5f;
        float centreY = Screen.height * 0.5f;

        // Crosshair: a filled dot, brighter when something grabbable is under it
        Color previous = GUI.color;
        GUI.color = m_Carry.Focused != null ? Color.white : new Color(1f, 1f, 1f, 0.4f);
        GUI.DrawTexture(new Rect(centreX - 2f, centreY - 2f, 4f, 4f), Texture2D.whiteTexture);
        GUI.color = previous;

        string message = GetMessage();

        if (!string.IsNullOrEmpty(message))
        {
            GUI.Label(new Rect(centreX - 300f, centreY + 24f, 600f, 60f), message, m_Style);
        }
    }

    private string GetMessage()
    {
        if (Time.time - m_Carry.LastRefusalTime < refusalDuration)
        {
            switch (m_Carry.LastRefusal)
            {
                case PlayerCarry.GrabRefusal.TooHeavy:
                    return $"Too heavy - you can lift {m_Strength.LiftCapacity:0} kg and drag up to {m_Strength.MaxGrabMass:0} kg";
                case PlayerCarry.GrabRefusal.AlreadyHeld:
                    return "Someone else is carrying that";
                case PlayerCarry.GrabRefusal.OutOfReach:
                    return "Out of reach";
            }
        }

        if (m_Carry.Carried != null)
        {
            string strain = m_Carry.Carried.Mass > m_Strength.LiftCapacity ? " - dragging" : string.Empty;
            return $"{m_Carry.Carried.name}  {m_Carry.Carried.Mass:0.#} kg{strain}\n" +
                   "E drop    Left mouse throw    R + mouse rotate    Scroll distance";
        }

        if (m_Carry.Focused != null)
        {
            string weight = m_Carry.Focused.Mass > m_Strength.MaxGrabMass ? "too heavy to move" : $"{m_Carry.Focused.Mass:0.#} kg";
            return $"E  Pick up {m_Carry.Focused.name}  ({weight})";
        }

        return string.Empty;
    }
}
