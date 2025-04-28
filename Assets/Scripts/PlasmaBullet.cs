using UnityEngine;
using Mirror;

public class PlasmaBullet : Projectile
{
    private void Awake()
    {
        weapon = FindObjectOfType<PlasmaGun>();
    }

    // This method is responsible for dealing with the explosion
    [Server]
    override protected void Explode()
    {
        if (explosionPrefab != null)
            NetworkServer.Spawn(Instantiate(explosionPrefab, transform.position, Quaternion.identity));

        Collider[] hitObjects = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider hit in hitObjects)
        {
            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
            {
                // Always apply explosion force, even to the shooter
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
            else
            {
                FPSMovement playerController = hit.GetComponentInParent<FPSMovement>();
                if (playerController != null)
                {
                    playerController.RocketJump(transform.position, explosionForce);
                }
            }

            // Now handle damage separately
            NetworkIdentity hitIdentity = hit.GetComponentInParent<NetworkIdentity>();

            // Only damage others, not yourself
            if (hitIdentity != null && hitIdentity == shooterIdentity)
                continue; // Skip damaging the shooter

            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int damage = weapon != null ? Random.Range(weapon.maxDamage, weapon.minDamage) : 10;
                damageable.TakeDamage(damage, gameObject);

                DamageAccumulator accumulator = hit.GetComponent<DamageAccumulator>();
                if (accumulator != null)
                {
                    accumulator.AddDamage(damage, transform.position);
                }

                PlayerController playerController = hit.gameObject.GetComponentInParent<PlayerController>();

                if (playerController != null && playerController.health <= 0)
                {
                    if (shooterIdentity != null)
                    {
                        PlayerController shooterController = shooterIdentity.GetComponent<PlayerController>();
                        if (shooterController != null)
                        {
                            shooterController.AddKill();
                            Debug.Log("Kill confirmed by explosion!");
                        }
                    }
                }

                if (shooterIdentity != null)
                {
                    var shooterScript = shooterIdentity.GetComponent<FPSShoot>();
                    if (shooterScript != null)
                    {
                        shooterScript.TargetShowHitMarker(shooterIdentity.connectionToClient);
                    }
                }

                Debug.Log("Damage dealt by explosion: " + damage);
            }
        }

        NetworkServer.Destroy(gameObject);
    }


    // Helper method to handle kill counting logic
    private void HandleKill(GameObject target)
    {
        PlayerController playerController = target.GetComponentInParent<PlayerController>();
        if (playerController != null && playerController.GetHealth() <= 0)
        {
            playerController.AddKill(); // Increment kill count
            Debug.Log("Kill confirmed for player: " + target.name);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.8f); // Orange with some transparency
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}
