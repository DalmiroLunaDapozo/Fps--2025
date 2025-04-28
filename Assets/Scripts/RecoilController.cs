using UnityEngine;
using Cinemachine;

public class RecoilController : MonoBehaviour
{
    public CinemachineVirtualCamera cinemachineCamera;
    public float recoilAmount = 2f;  // How much the camera kicks up
    public float recoilSpeed = 10f;  // Speed of recoil kick
    public float recoverySpeed = 5f; // Speed of recoil recovery

    private float recoilOffset = 0f; // Current recoil position
    private float targetRecoil = 0f; // Target recoil amount

    private void Update()
    {
        // Smoothly apply recoil and recovery
        recoilOffset = Mathf.Lerp(recoilOffset, targetRecoil, Time.deltaTime * recoilSpeed);
        targetRecoil = Mathf.Lerp(targetRecoil, 0, Time.deltaTime * recoverySpeed);

        // Apply the recoil to camera rotation (pitch)
        Vector3 newRotation = transform.localEulerAngles;
        newRotation.x -= recoilOffset;
        transform.localEulerAngles = newRotation;
    }

    // Call this function when shooting
    public void ApplyRecoil()
    {
        targetRecoil += recoilAmount;
    }
}
