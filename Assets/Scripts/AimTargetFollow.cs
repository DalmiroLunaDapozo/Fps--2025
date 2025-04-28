using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimTargetFollow : MonoBehaviour
{
    public Transform cameraTransform;   // Camera transform (use the pivot or actual camera)
    public Transform rightHandTarget;   // Right hand target (empty GameObject at weapon's grip)
    public Transform weapon;            // The weapon (for positioning purposes)
    public float followDistance = 0.6f; // Adjust this value based on your setup

    void Update()
    {
        // Make the right hand target follow the camera's forward direction
        rightHandTarget.position = cameraTransform.position + cameraTransform.forward * followDistance;

        // Adjust rotation based on the camera's rotation
        rightHandTarget.rotation = cameraTransform.rotation;

        // Optionally, you can adjust based on weapon rotation, depending on the setup
        if (weapon != null)
        {
            // Set the hand target position at the weapon's grip, adjusted by camera position
            rightHandTarget.position = weapon.position;
        }
    }
}
