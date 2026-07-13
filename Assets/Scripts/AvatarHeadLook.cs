using UnityEngine;

public class AvatarHeadLook : MonoBehaviour
{
    [SerializeField] Transform headBone;
    [SerializeField] Transform bodyReference;   // chest/spine that defines "forward"
    [SerializeField] Camera targetCamera;

    [Header("Limits relative to body")]
    [SerializeField] float maxYaw = 60f;
    [SerializeField] float maxPitch = 30f;
    [SerializeField] float giveUpYaw = 90f;     // beyond this, return to neutral
    [SerializeField] float followSpeed = 8f;
    [SerializeField] float weightSpeed = 4f;

    public bool lookEnabled = true;

    Quaternion _headOffset;
    float _weight;

    void Start()
    {
        if (targetCamera == null) targetCamera = Camera.main;
        Quaternion restLook = Quaternion.LookRotation(bodyReference.forward, bodyReference.up);
        _headOffset = Quaternion.Inverse(restLook) * headBone.rotation; // assumes head faces forward at rest
    }

    void LateUpdate()
    {
        if (headBone == null || bodyReference == null || targetCamera == null) return;

        Vector3 toCam = (targetCamera.transform.position - headBone.position).normalized;
        Vector3 local = bodyReference.InverseTransformDirection(toCam);

        float yaw = Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg;
        float pitch = Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg;

        bool inRange = lookEnabled && Mathf.Abs(yaw) <= giveUpYaw;
        _weight = Mathf.MoveTowards(_weight, inRange ? 1f : 0f, weightSpeed * Time.deltaTime);

        float cYaw = Mathf.Clamp(yaw, -maxYaw, maxYaw) * Mathf.Deg2Rad;
        float cPitch = Mathf.Clamp(pitch, -maxPitch, maxPitch) * Mathf.Deg2Rad;

        Vector3 clampedLocal = new Vector3(
            Mathf.Sin(cYaw) * Mathf.Cos(cPitch),
            Mathf.Sin(cPitch),
            Mathf.Cos(cYaw) * Mathf.Cos(cPitch));
        Vector3 worldDir = bodyReference.TransformDirection(clampedLocal);

        Quaternion look = Quaternion.LookRotation(worldDir, bodyReference.up) * _headOffset;
        Quaternion rest = Quaternion.LookRotation(bodyReference.forward, bodyReference.up) * _headOffset;
        Quaternion target = Quaternion.Slerp(rest, look, _weight);

        headBone.rotation = Quaternion.Slerp(headBone.rotation, target, followSpeed * Time.deltaTime);
    }
}