using UnityEngine;
using UnityEngine.InputSystem;

// DEBUG ONLY: exercises the interaction flow with the Quest controller.
//   B (right controller), dispatched by state:
//       Idle          -> StartExperimentWithUI
//       WaitingForYes -> NotifyYesHeard
//       ShowingPanel  -> AdvanceToNextAvatar (start the next avatar)
//   X (left controller) -> GetAvatarData (logs gender / tattoo)
public class DebugControllerTester : MonoBehaviour
{
    [SerializeField] AvatarInteractionManager manager;

    // B on the right Quest controller = secondaryButton (right hand)
    InputAction _bButton = new InputAction(
        type: InputActionType.Button,
        binding: "<XRController>{RightHand}/secondaryButton");

    // X on the left Quest controller = primaryButton (left hand)
    InputAction _xButton = new InputAction(
        type: InputActionType.Button,
        binding: "<XRController>{LeftHand}/primaryButton");

    void OnEnable()
    {
        _bButton.Enable();
        _xButton.Enable();
    }

    void OnDisable()
    {
        _bButton.Disable();
        _xButton.Disable();
    }

    void Update()
    {
        if (manager == null) return;

        if (_bButton.WasPressedThisFrame()) HandleB();
        if (_xButton.WasPressedThisFrame()) HandleX();
    }

    void HandleB()
    {
        // ShowingPanel is checked FIRST: only after the avatar's animation
        // (which ends in ShowingPanel) can B start the next avatar.
        if (manager.IsShowingPanel)
        {
            Debug.Log("[DebugController] B -> AdvanceToNextAvatar (start next avatar)");
            manager.AdvanceToNextAvatar();
        }
        else if (manager.IsWaitingForYes)
        {
            Debug.Log("[DebugController] B -> NotifyYesHeard");
            manager.NotifyYesHeard();
        }
        else if (manager.IsIdle)
        {
            Debug.Log("[DebugController] B -> StartExperimentWithUI");
            manager.StartExperimentWithUI();
        }
        else
        {
            Debug.Log("[DebugController] B ignored (not an actionable state)");
        }
    }

    void HandleX()
    {
        AvatarData data = manager.GetAvatarData();
        if (data != null)
            Debug.Log($"[DebugController] X -> gender='{data.gender}', tattoo='{data.tatoo}'");
        else
            Debug.Log("[DebugController] X -> no active avatar");
    }
}