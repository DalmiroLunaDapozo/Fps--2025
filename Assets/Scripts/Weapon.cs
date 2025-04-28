using UnityEngine;
using Mirror;

public abstract class Weapon : NetworkBehaviour
{
    public string weaponName;
    public float fireRate;
    public int maxAmmo;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public int maxDamage;
    public int minDamage;
    public float projectileSpeed;
    public float recoilAmount = 2f;  // How much the camera moves up/down with recoil
    public float recoilSpeed = 5f;
    public bool hasInfiniteAmmo;

    [SyncVar]
    public int currentAmmo;

    public virtual void Start()
    {
        currentAmmo = maxAmmo;
    }

    
}