using UnityEngine;

public class CameraAiming : MonoBehaviour
{
    public Transform cameraTransform;
    public Animator animator;
    public float pitchSpeed = 2f;

    private float aimPitch;

    void Update()
    {
        // Get the pitch angle (vertical camera angle)
        aimPitch = Mathf.Clamp(cameraTransform.eulerAngles.x, -90f, 90f);

        // Update the animator parameter for AimPitch
        animator.SetFloat("AimPitch", aimPitch);

        // Optionally, you can update AimYaw for horizontal aiming too
    }
}
