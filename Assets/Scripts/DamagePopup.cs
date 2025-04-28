using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float lifetime = 1f;
    [SerializeField] private Vector3 floatDirection = new Vector3(0, 1, 0);

    public TextMeshProUGUI textMesh;
    private Camera mainCamera;
    private bool stopFloating = false; // Flag to control floating behavior

    private void Start()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        mainCamera = Camera.main;
    }

    public void Setup(int damageAmount)
    {
        textMesh.text = damageAmount.ToString();
    }

    private void Update()
    {
        // Only move if we haven't accumulated damage and should float
        if (!stopFloating)
        {
            transform.position += floatDirection * floatSpeed * Time.deltaTime;
        }

        lifetime -= Time.deltaTime;

        Vector3 dirToCamera = mainCamera.transform.position - transform.position;
        dirToCamera.y = 0; // Ignore the y-axis (don't rotate up/down)

        // If the camera is not at the same position as the popup, rotate smoothly to face it
        if (dirToCamera.sqrMagnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(dirToCamera);

            transform.rotation = Quaternion.Euler(0, targetRotation.eulerAngles.y + 180, 0);
        }

        // If the lifetime has passed, destroy the popup
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    public void StopFloating()
    {
        stopFloating = true; // Stop the floating effect after accumulating damage
    }

    public void ResetLifetime(float newLifetime)
    {
        lifetime = newLifetime; // Reset the lifetime to ensure the popup stays long enough
    }
}
