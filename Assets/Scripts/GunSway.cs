using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwayGun : MonoBehaviour
{
    private Camera playerCamera;
    private FPSMovement movement;

    [Header("Sway Settings")]
    public float swayAmount = 0.1f;   // Amount of sway based on movement
    public float swaySpeed = 1f;      // Speed of the sway (how fast it oscillates)
    public float maxSwayAmount = 0.1f;
    public float minSwayAmount = 0.05f;

    private Vector3 originalPosition;

    private bool isJumping = false;

    // Start is called before the first frame update
    private void Start()
    {
        playerCamera = Camera.main;  // Reference to the camera
        movement = GetComponentInParent<FPSMovement>(); // Get the parent CharacterController
        originalPosition = transform.localPosition;  // Save the original position of the weapon
    }

    // Update is called once per frame
    private void Update()
    {
        if (playerCamera == null || movement == null) return;

        

        // Check horizontal velocity (movement on the X and Z axes)
        Vector3 horizontalVelocity = new Vector3(movement.GetMoveInput().x, 0, movement.GetMoveInput().y);
        float playerSpeed = horizontalVelocity.magnitude;

        // Detect if the player is jumping or falling
        isJumping = movement.IsGrounded() == false;

        // Calculate sway based on movement speed and jumping status
        float swayFactor = Mathf.Clamp(playerSpeed, 0, 1) * swayAmount;

        // If the player is jumping, apply sway, but less than walking
        if (isJumping)
        {
            swayFactor *= 0.5f;  // Reduce sway when jumping
        }

        // Sway calculation: oscillate based on time and speed
        float swayX = Mathf.Sin(Time.time * swaySpeed) * swayFactor;
        float swayY = Mathf.Cos(Time.time * swaySpeed) * swayFactor;

        // Apply the sway to the gun's local position
        transform.localPosition = originalPosition + new Vector3(swayX, swayY, 0);
    }

    // This method allows us to reset the sway when switching weapons
    public void ReinitializeSway()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        originalPosition = transform.localPosition;  // Reset the original position
        swayAmount = maxSwayAmount;  // Reset sway amount to its default maximum value
    }

    // This method allows us to change the sway amount dynamically
    public void UpdateSwayAmount(float newSwayAmount)
    {
        swayAmount = Mathf.Clamp(newSwayAmount, minSwayAmount, maxSwayAmount);
    }
}
