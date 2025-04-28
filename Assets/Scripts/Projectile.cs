using UnityEngine;
using Mirror;

// Projectile Script Fix
public class Projectile : NetworkBehaviour
{
    public GameObject explosionPrefab;
    public float explosionRadius = 5f;
    public float explosionForce = 500f;
    public LayerMask damageableLayers;
    public float customGravityScale = 0.2f;

    public float lifeTime = 4;

    [SerializeField] private GameObject damagePopupPrefab;
    protected Rigidbody rb;

    public Weapon weapon;

    protected int randomDamage;

    [HideInInspector] public NetworkIdentity shooterIdentity; // Ensure this is set

    protected Vector3 spawnPosition;

    protected void Start()
    {
        if (isServer)
        {
            Invoke(nameof(SelfDestruct), lifeTime);
            rb.useGravity = false;
        }
    }

    public override void OnStartServer()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
        }
    }

    void FixedUpdate()
    {
        if (isServer)
        {
            rb.AddForce(Physics.gravity * customGravityScale, ForceMode.Acceleration);
        }
    }

    [Server]
    protected void SelfDestruct()
    {
        if (gameObject != null)
            NetworkServer.Destroy(gameObject);
    }

    [ServerCallback]
    protected virtual void OnCollisionEnter(Collision collision)
    {
        if (!isServer) return;

        NetworkIdentity hitIdentity = collision.gameObject.GetComponentInParent<NetworkIdentity>();
        if (hitIdentity != null && hitIdentity == shooterIdentity)
            return;

        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();

        if (weapon != null)
            randomDamage = Random.Range(weapon.minDamage, weapon.maxDamage); // Ensure min/max are correct
        else
            randomDamage = 10;

        if (damageable != null)
        {
            // Pass shooterIdentity.gameObject instead of this projectile
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

        Explode();
        NetworkServer.Destroy(gameObject);
    }

    protected virtual void Explode()
    {
        if (explosionPrefab != null)
            NetworkServer.Spawn(Instantiate(explosionPrefab, transform.position, Quaternion.identity));
    }
}

