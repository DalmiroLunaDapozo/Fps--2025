using UnityEngine;

public class CameraSpineFollow : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform cameraTransform; // Assign Main Camera or leave blank to auto-assign
    public HumanBodyBones boneToRotate = HumanBodyBones.UpperChest;

    [Header("Rotation Settings")]
    public float rotationSpeed = 8f;
    public float maxPitchAngle = 15f;
    public float maxYawAngle = 25f; // Limit how far left/right the spine can twist
    public Vector3 aimOffsetEuler = new Vector3(0f, 5f, 0f); // Optional tweak to center weapon

    [SerializeField] private Transform spineBone;
    private Quaternion initialLocalRotation;

    void Start()
    {
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        if (cameraTransform == null)
        {
            Debug.LogError("[UpperBodyAiming] No camera reference found!");
            enabled = false;
            return;
        }

        if (animator == null)
        {
            Debug.LogError("[UpperBodyAiming] No Animator assigned!");
            enabled = false;
            return;
        }

        //spineBone = animator.GetBoneTransform(boneToRotate);

        if (spineBone == null)
        {
            Debug.LogError("[UpperBodyAiming] Could not find bone: " + boneToRotate);
            enabled = false;
            return;
        }

        initialLocalRotation = spineBone.localRotation;
    }

    void LateUpdate()
    {
        if (spineBone == null || cameraTransform == null) return;

        // Get camera direction and convert it to a desired local rotation
        Vector3 targetForward = cameraTransform.forward;
        Quaternion targetRotation = Quaternion.LookRotation(targetForward, Vector3.up);

        // Convert to local space relative to parent
        Quaternion localRotation = Quaternion.Inverse(spineBone.parent.rotation) * targetRotation;
        Vector3 localEuler = localRotation.eulerAngles;

        // Convert angles to -180 to 180 range for proper clamping
        localEuler.x = NormalizeAngle(localEuler.x);
        localEuler.y = NormalizeAngle(localEuler.y);
        localEuler.z = 0f;

        // Clamp pitch and yaw to keep it subtle and centered
        localEuler.x = Mathf.Clamp(localEuler.x, -maxPitchAngle, maxPitchAngle);
        localEuler.y = Mathf.Clamp(localEuler.y, -maxYawAngle, maxYawAngle);

        // Apply aiming offset tweak to center the gun if needed
        localEuler += aimOffsetEuler;

        // Smoothly rotate the spine
        Quaternion targetLocalRotation = Quaternion.Euler(localEuler);
        spineBone.localRotation = Quaternion.Slerp(spineBone.localRotation, targetLocalRotation, Time.deltaTime * rotationSpeed);

        // Debug visuals
        Debug.DrawRay(spineBone.position, spineBone.forward * 0.5f, Color.red);
    }

    float NormalizeAngle(float angle)
    {
        return (angle > 180f) ? angle - 360f : angle;
    }
}
