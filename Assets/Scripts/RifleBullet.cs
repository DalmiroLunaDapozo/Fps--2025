using Mirror;
using UnityEngine;

public class RifleBullet : Projectile
{

    private int finalDamage;

    private void Awake()
    {
        weapon = FindObjectOfType<Rifle>();
    }

    override protected void OnCollisionEnter(Collision collision)
    {

        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();

        if (weapon != null)
        {
            float distanceTraveled = Vector3.Distance(spawnPosition, transform.position);

            // Linear scaling: at 0 distance -> minDamage, at maxEffectiveDistance -> maxDamage
            float maxEffectiveDistance = 50f;
            float t = Mathf.Clamp01(distanceTraveled / maxEffectiveDistance);
            float scaledDamage = Mathf.Lerp(weapon.minDamage, weapon.maxDamage, t);
            finalDamage = Mathf.RoundToInt(scaledDamage);
        }
        else
        {
            finalDamage = 10; // Default fallback
        }

        if (damageable != null)
        {
            // Pass shooterIdentity.gameObject instead of this projectile
            GameObject attacker = shooterIdentity != null ? shooterIdentity.gameObject : gameObject;
            damageable.TakeDamage(finalDamage, attacker);

            DamageAccumulator accumulator = collision.gameObject.GetComponent<DamageAccumulator>();
            if (accumulator != null)
            {
                accumulator.AddDamage(finalDamage, collision.contacts[0].point);
            }

            if (shooterIdentity != null)
            {
                FPSShoot shooterScript = shooterIdentity.GetComponent<FPSShoot>();
                if (shooterScript != null)
                    shooterScript.TargetShowHitMarker(shooterIdentity.connectionToClient);
            }

            Debug.Log("Damage dealt: " + randomDamage);
        }

        Explode();
        NetworkServer.Destroy(gameObject);
    }
}
