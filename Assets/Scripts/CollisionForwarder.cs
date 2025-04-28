using UnityEngine;

public class CollisionForwarder : MonoBehaviour
{
    private PlayerController playerController;

    private void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
    }


    // Optional: for collision-based (non-trigger) handling
    private void OnCollisionEnter(Collision collision)
    {
        if (playerController != null && collision.collider.CompareTag("Projectile"))
        {
            playerController?.Invoke("TakeDamage", 0f); // Adjust as needed
        } 
    }
}