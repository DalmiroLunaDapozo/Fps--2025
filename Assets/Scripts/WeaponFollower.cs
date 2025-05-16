using UnityEngine;

public class WeaponFollower : MonoBehaviour
{
    public Transform cameraTransform; // Assign your Cinemachine camera here
    public Vector3 positionOffset;
    public Vector3 rotationOffset;

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // Match position with optional offset
        transform.position = cameraTransform.position + cameraTransform.TransformVector(positionOffset);

        // Lock weapon to camera rotation + slight offset
        transform.rotation = cameraTransform.rotation * Quaternion.Euler(rotationOffset);
    }
}
