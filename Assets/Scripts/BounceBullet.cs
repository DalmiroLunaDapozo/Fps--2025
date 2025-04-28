
using Mirror;
using UnityEngine;

public class BounceBullet : Projectile
{
    public int maxBounces = 3; // How many times the bullet can bounce
    private int currentBounces = 0;
    public float bounceDamping = 0.9f;

    private void Awake()
    {
        weapon = FindObjectOfType<BounceGun>();
    }


    override protected void OnCollisionEnter(Collision collision)
    {
        if (!isServer) return;

        // Ignore collisions with the shooter itself
        NetworkIdentity hitIdentity = collision.gameObject.GetComponentInParent<NetworkIdentity>();
        if (hitIdentity != null && hitIdentity == shooterIdentity)
            return;

        // Deal damage if damageable
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();

        if (weapon != null)
            randomDamage = Random.Range(weapon.minDamage, weapon.maxDamage);
        else
            randomDamage = 10;

        if (damageable != null)
        {
            GameObject attacker = shooterIdentity != null ? shooterIdentity.gameObject : gameObject;
            damageable.TakeDamage(randomDamage, attacker);

            DamageAccumulator accumulator = collision.gameObject.GetComponent<DamageAccumulator>();
            if (accumulator != null)
            {
                accumulator.AddDamage(randomDamage, collision.contacts[0].point);
            }

            if (shooterIdentity != null)
            {
                FPSShoot shooterScript = shooterIdentity.GetComponent<FPSShoot>();
                if (shooterScript != null)
                    shooterScript.TargetShowHitMarker(shooterIdentity.connectionToClient);
            }
        }

        // Handle bouncing
        if (currentBounces < maxBounces)
        {
            Bounce(collision.contacts[0].normal);
            currentBounces++;
        }
        else
        {
            ExplodeAndDestroy();
        }
    }

    private void Bounce(Vector3 collisionNormal)
    {
        if (rb != null)
        {
            Vector3 incomingVelocity = rb.velocity;
            Vector3 reflectedVelocity = Vector3.Reflect(incomingVelocity, collisionNormal);

            // Dynamic damping (slightly more loss per bounce)
            float dynamicDamping = Mathf.Pow(bounceDamping, currentBounces + 1);

            rb.velocity = reflectedVelocity * dynamicDamping;
        }
    }

    private void ExplodeAndDestroy()
    {
        Explode(); // Your explode logic (effects, sounds, etc.)
        NetworkServer.Destroy(gameObject);
    }


}
