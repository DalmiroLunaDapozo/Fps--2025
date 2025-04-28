using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    [HideInInspector]
    public Collider shooterCollider;

    private void Start()
    {
        Collider projectileCollider = GetComponent<Collider>();
        if (shooterCollider != null && projectileCollider != null)
        {
            Physics.IgnoreCollision(projectileCollider, shooterCollider);
        }
    }
}
