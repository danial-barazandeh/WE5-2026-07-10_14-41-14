using UnityEngine;

public class PanelFollower : MonoBehaviour
{
    [SerializeField] private Transform vrCamera; 
    [SerializeField] private float distance = 1.5f; // 50 cm nach vorne
    [SerializeField] private float heightOffset = -0.4f; // 40 cm nach unten
    [SerializeField] private float smoothSpeed = 5f; 

    void LateUpdate()
    {
        if (vrCamera == null) return;

        // Berechne die Position: Kamera-Position + Blickrichtung + Höhen-Offset
        Vector3 targetPosition = vrCamera.position + (vrCamera.forward * distance) + (Vector3.up * heightOffset);

        // Sofortiges Anheften (auskommentieren, falls du die weiche Bewegung unten nutzt)
        transform.position = targetPosition;

        // Option für weicheres Hinterherkommen (reduziert Motion Sickness)
         transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // Das Panel schaut den User an
        transform.LookAt(transform.position + vrCamera.forward);
    }
}