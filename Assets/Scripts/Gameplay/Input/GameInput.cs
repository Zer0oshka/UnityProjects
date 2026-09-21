using Unity.MP_FPS;
using UnityEngine;
using UnityEngine.InputSystem.Users;

/// <summary>
/// This class is used to initialize the <see cref="FPSInputActions"/> from the InputSystem and access it from the Gameplay and Debug systems.
/// </summary>
public static class GameInput
{
    /// <summary>
    /// This initialization is required in the Editor to avoid the instance from a previous Playmode to stay alive in the next session.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void RuntimeInitializeOnLoad()
    {
        Actions = new InputSystem_Actions();
        Actions.Enable();
    }

    public static InputSystem_Actions Actions { get; private set; } = null!;

    public static void SetGameplayMapsEnabled(bool enabled)
    {
        SetMapsEnabled(Actions, enabled);

        foreach (var user in InputUser.all)
        {
            if (user.valid && user.actions is InputSystem_Actions userActions)
            {
                SetMapsEnabled(userActions, enabled);
            }
        }
    }

    static void SetMapsEnabled(InputSystem_Actions actions, bool enabled)
    {
        if (actions == null)
        {
            return;
        }

        if (enabled)
        {
            actions.Player.Enable();
            actions.FPS.Enable();
        }
        else
        {
            actions.Player.Disable();
            actions.FPS.Disable();
        }
    }
}
