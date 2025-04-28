using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using System.Collections;

public class FPSShoot : NetworkBehaviour
{
    public Camera playerCamera;
    public Transform gunTransform;
    public Transform muzzleFlashSpawnPoint;
    public GameObject muzzleFlashPrefab;

    private float projectileSpeed = 20f;
    private float fireRate = 0.5f;

    private float nextFireTime = 0f;
    private PlayerControls controls;
    public bool isFiring { get; private set; }

    private Vector3 originalGunPosition;
    private Quaternion originalGunRotation;
    private Vector3 currentRecoilPosition;
    private Vector3 currentRecoilRotation;

    private FPSMovement fpsMovement;
    private PlayerController playerController;
    private WeaponManager weaponManager;

    [SyncVar(hook = nameof(OnWeaponIndexChanged))]
    private int weaponIndex = -1;

    private Weapon actualWeapon;
    private GameObject projectilePrefab;

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Enable();

        fpsMovement = GetComponent<FPSMovement>();
        playerController = GetComponent<PlayerController>();
        weaponManager = GetComponent<WeaponManager>();

        originalGunPosition = gunTransform.localPosition;
        originalGunRotation = gunTransform.localRotation;
    }

    private void Start()
    {
        if (!isLocalPlayer)
        {
            playerCamera.gameObject.SetActive(false);
        }
        else
        {
            playerCamera.enabled = true;
        }

        if (isServer && weaponIndex >= 0)
        {
            SetWeaponData(WeaponDatabase.Instance.GetWeaponByIndex(weaponIndex));
        }
    }

    private void Update()
    {
        if (!isLocalPlayer || playerController.isDead) return;

        if (controls.Player.Shoot.ReadValue<float>() > 0.5f && Time.time >= nextFireTime)
        {
            isFiring = true;
            nextFireTime = Time.time + fireRate;
            Shoot();
          
        }
        else
        {
            isFiring = false;
        }

        gunTransform.localPosition = originalGunPosition + currentRecoilPosition;
        gunTransform.localRotation = originalGunRotation * Quaternion.Euler(currentRecoilRotation);
    }

    public void RequestEquipWeapon(int index)
    {
        if (isLocalPlayer)
        {
            CmdEquipWeapon(index);
        }
    }

    [Command]
    private void CmdEquipWeapon(int index)
    {
        weaponIndex = index;
        SetWeaponData(WeaponDatabase.Instance.GetWeaponByIndex(index));
    }

    private void OnWeaponIndexChanged(int oldIndex, int newIndex)
    {
        if (newIndex >= 0)
        {
            Weapon weapon = WeaponDatabase.Instance.GetWeaponByIndex(newIndex);
            SetWeaponData(weapon);

            if (isServer)
            {
                SetWeaponData(weapon);
            }
        }
    }

    private void Shoot()
    {
        Vector3 direction = playerCamera.transform.forward;

        // Spawn the bullet slightly forward to avoid pushing the shooter
        Vector3 spawnPos = muzzleFlashSpawnPoint != null
            ? muzzleFlashSpawnPoint.position + direction * 0.6f
            : gunTransform.position + direction * 0.6f;

        CmdShootProjectile(spawnPos, direction);
        ShowMuzzleFlash(direction);
    }

    [Command]
    private void CmdShootProjectile(Vector3 spawnPosition, Vector3 direction)
    {
        if (projectilePrefab == null)
        {
            Debug.LogError("projectilePrefab is NULL on the server! Make sure weapon data is assigned.");
            return;
        }

        // Spawn the projectile and disable collisions for now
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));

        // Set the shooter identity before spawning
        var projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.shooterIdentity = connectionToClient.identity;  // Correct assignment
        }

        Collider projectileCollider = projectile.GetComponent<Collider>();
        Collider[] shooterColliders = GetComponentsInChildren<Collider>();

        // Ignore collision between the bullet and the shooter
        foreach (Collider shooterCol in shooterColliders)
        {
            Physics.IgnoreCollision(projectileCollider, shooterCol);
        }

        NetworkServer.Spawn(projectile);

        // Temporarily disable Rigidbody physics for one frame to avoid initial push
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Disable physics initially to avoid pushing
        }

        // Enable physics for projectile after one frame (in the next FixedUpdate)
        StartCoroutine(EnableBulletPhysicsNextFrame(projectile, direction));

        RpcTriggerRecoil();
    }


    [ClientRpc]
    private void RpcTriggerRecoil()
    {
        if (isLocalPlayer && fpsMovement != null)
        {
            fpsMovement.TriggerRecoil();
        }
    }

    private void ShowMuzzleFlash(Vector3 direction)
    {
        if (muzzleFlashPrefab != null && muzzleFlashSpawnPoint != null)
        {
            Quaternion flashRotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0, -90f, 0);
            GameObject flash = Instantiate(muzzleFlashPrefab, muzzleFlashSpawnPoint.position, flashRotation);
            Destroy(flash, 0.05f);
        }
    }

    public void ResetFireTimer()
    {
        nextFireTime = Time.time;
    }

    [TargetRpc]
    public void TargetShowHitMarker(NetworkConnection target)
    {
        FindObjectOfType<CrosshairHit>().ShowHit();
    }

    public void SetWeaponData(Weapon weapon)
    {
        if (weapon == null)
        {
            Debug.LogError("Weapon is null when setting weapon data.");
            return;
        }

        actualWeapon = weapon;
        projectileSpeed = weapon.projectileSpeed;
        fireRate = weapon.fireRate;
        projectilePrefab = weapon.projectilePrefab;
    }

    private void OnDisable()
    {
        controls.Disable();
    }

    private System.Collections.IEnumerator EnableBulletPhysicsNextFrame(GameObject projectile, Vector3 direction)
    {
        yield return new WaitForFixedUpdate();

        if (projectile != null)
        {
            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false; // Re-enable physics after the first frame
                rb.velocity = direction * projectileSpeed;
                rb.useGravity = true; // Enable gravity if needed
            }
        }
    }
}
