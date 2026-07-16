using UnityEngine;

public class CameraLayerController : MonoBehaviour
{

    [SerializeField] private Camera targetCamera;


    [SerializeField] private LayerMask layersWhenEnabled;


    [SerializeField] private bool restorePreviousInsteadOfAll = false;

    private const int EverythingMask = ~0; // -1 = all layers

    private int previousMask;
    private bool hasPreviousMask;

    private void OnEnable()
    {
        if (!ResolveCamera()) return;

        previousMask = targetCamera.cullingMask;
        hasPreviousMask = true;

        targetCamera.cullingMask = layersWhenEnabled.value;
    }

    private void OnDisable()
    {
        if (targetCamera == null) return;

        targetCamera.cullingMask = (restorePreviousInsteadOfAll && hasPreviousMask)
            ? previousMask
            : EverythingMask;
    }

    private bool ResolveCamera()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
        {
            Debug.LogWarning($"[{nameof(CameraLayerController)}] No camera found.", this);
            return false;
        }

        return true;
    }
}
