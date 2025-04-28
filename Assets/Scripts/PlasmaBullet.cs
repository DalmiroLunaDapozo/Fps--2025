using UnityEngine;
using Mirror;

public class PlasmaBullet : Projectile
{

    private void Awake()
    {
        weapon = FindObjectOfType<PlasmaGun>();
    }

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
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
            else
            {
                FPSMovement playerController = hit.GetComponentInParent<FPSMovement>();
                if (playerController != null)
                {
                    playerController.RocketJump(transform.position, explosionForce);

                    if (playerController.connectionToClient != null)
                    {
                        playerController.TargetRocketJump(playerController.connectionToClient, transform.position, explosionForce);
                    }
                }
            }

            // Check if it's a player
            NetworkIdentity hitIdentity = hit.GetComponentInParent<NetworkIdentity>();
            if (hitIdentity != null)
            {
                FPSMovement movement = hitIdentity.GetComponent<FPSMovement>();

                if (movement != null)
                {
                    Vector3 direction = (movement.transform.position - transform.position).normalized;

                    if (hitIdentity.isOwned)
                    {
                        movement.ApplyLocalRocketJump(direction);
                    }
                    else
                    {
                        movement.TargetRocketJump(hitIdentity.connectionToClient, transform.position, explosionForce);
                    }
                }
            }

            // Handle damage
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                bool isSelf = (hitIdentity != null && hitIdentity == shooterIdentity);

                if (!isSelf) //  don't damage yourself
                {
                    int damage = weapon != null ? Random.Range(weapon.maxDamage, weapon.minDamage) : 10;
                    damageable.TakeDamage(damage, gameObject);

                    DamageAccumulator accumulator = hit.GetComponent<DamageAccumulator>();
                    if (accumulator != null)
                    {
                        accumulator.AddDamage(damage, transform.position);
                    }

                    PlayerController playerController = hit.GetComponentInParent<PlayerController>();
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
        }

        NetworkServer.Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0.5f, 0, 0.8f);
        Gizmos.DrawSphere(transform.position, explosionRadius);
    }
}
