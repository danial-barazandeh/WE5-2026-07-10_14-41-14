using UnityEngine;
using UnityEngine.InputSystem;

// DEBUG ONLY: Quest 'A' button acts as "questions finished -> next avatar".
public class DebugQuestAdvance : MonoBehaviour
{
    [SerializeField] AvatarInteractionManager manager;

    // 'A' on Quest = primaryButton on the right-hand controller
    InputAction _advance = new InputAction(
        type: InputActionType.Button,
        binding: "<XRController>{RightHand}/primaryButton");

    void OnEnable() => _advance.Enable();
    void OnDisable() => _advance.Disable();

    void Update()
    {
        if (_advance.WasPressedThisFrame())
        {
            if (manager != null)
                manager.AdvanceToNextAvatar();   // same as finishing the questions
            else
                Debug.LogError("[DebugQuestAdvance] manager not assigned");
        }
    }
}