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

    [SerializeField] private float secondaryFireRate = 1.5f;

    private float nextSecondaryFireTime = 0f;
    private bool isZoomed = false;



    private Cinemachine.CinemachineVirtualCamera virtualCamera;
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

        virtualCamera = FindObjectOfType<Cinemachine.CinemachineVirtualCamera>();

        if (virtualCamera == null)
        {
            Debug.LogError("Cinemachine Virtual Camera not found in the scene.");
        }

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

        float primaryInput = controls.Player.Shoot.ReadValue<float>();
        float secondaryInput = controls.Player.SecondaryShoot.ReadValue<float>();

        bool canPrimary = primaryInput > 0.5f && Time.time >= nextFireTime;
        bool canSecondary = secondaryInput > 0.5f && Time.time >= nextSecondaryFireTime;

        // Prioritize primary shooting and block secondary if it's active
        if (canPrimary)
        {
            isFiring = true;
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
        else if (actualWeapon != null && actualWeapon.secondaryActionType == SecondaryActionType.Zoom)
        {
            HandleZoomInput(secondaryInput > 0.5f);
        }
        else if (!primaryInput.Equals(1f) && canSecondary)
        {
            isFiring = true;
            nextSecondaryFireTime = Time.time + actualWeapon.secondaryFireRate;
            HandleSecondaryAction();
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
        if (isLocalPlayer && weaponIndex != index)
        {
            CmdEquipWeapon(index);
        }
    }

    [Command]
    private void CmdEquipWeapon(int index)
    {
        weaponIndex = index;

        Weapon weapon = WeaponDatabase.Instance.GetWeaponByIndex(index);
        if (weapon != null)
        {
            actualWeapon = weapon;
        }
    }
    private void OnWeaponIndexChanged(int oldIndex, int newIndex)
    {
        if (newIndex >= 0)
        {
            Weapon weapon = WeaponDatabase.Instance.GetWeaponByIndex(newIndex);
            SetWeaponData(weapon);

            // If this is running on the server instance, update the server's actualWeapon reference
            if (isServer)
            {
                actualWeapon = weapon;
            }
        }
    }

    private void Shoot()
    {

        if (actualWeapon == null || actualWeapon.currentAmmo <= 0)
        {
            Debug.Log("No ammo!");
            return;
        }

        if (!actualWeapon.hasInfiniteAmmo) actualWeapon.currentAmmo--;

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

        
        if (actualWeapon == null)
        {
            Debug.LogError("ActualWeapon is null on server!");
            return;
        }

        var prefab = actualWeapon.projectilePrefab;
        if (prefab == null)
        {
            Debug.LogError("Projectile prefab is null on server!");
            return;
        }

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

    public int GetCurrentAmmo()
    {
        return actualWeapon != null ? actualWeapon.currentAmmo : 0;
    }

    private void SecondaryShoot()
    {
        if (actualWeapon == null || actualWeapon.currentAmmo <= 0)
        {
            Debug.Log("No ammo for secondary fire!");
            return;
        }

        if (!actualWeapon.hasInfiniteAmmo) actualWeapon.currentAmmo--;

        Vector3 direction = playerCamera.transform.forward;
        Vector3 spawnPos = muzzleFlashSpawnPoint != null
            ? muzzleFlashSpawnPoint.position + direction * 0.6f
            : gunTransform.position + direction * 0.6f;

        CmdShootSecondaryProjectile(spawnPos, direction);
        ShowMuzzleFlash(direction); // Optional: you can make a separate flash for secondary fire
    }

    [Command]
    private void CmdShootSecondaryProjectile(Vector3 spawnPosition, Vector3 direction)
    {
        if (actualWeapon.secondaryProjectilePrefab == null)
        {
            Debug.LogError("Secondary projectile prefab is NULL!");
            return;
        }

        GameObject projectile = Instantiate(actualWeapon.secondaryProjectilePrefab, spawnPosition, Quaternion.LookRotation(direction));

        var projectileScript = projectile.GetComponent<Projectile>();
        if (projectileScript != null)
        {
            projectileScript.shooterIdentity = connectionToClient.identity;
        }

        Collider projectileCollider = projectile.GetComponent<Collider>();
        Collider[] shooterColliders = GetComponentsInChildren<Collider>();

        foreach (Collider shooterCol in shooterColliders)
        {
            Physics.IgnoreCollision(projectileCollider, shooterCol);
        }

        NetworkServer.Spawn(projectile);

        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        StartCoroutine(EnableBulletPhysicsNextFrame(projectile, direction)); // reuse original coroutine

        RpcTriggerRecoil(); // or a separate RpcSecondaryRecoil() if you want different recoil
    }

    private void HandleSecondaryAction()
    {
        switch (actualWeapon.secondaryActionType)
        {
            case SecondaryActionType.ShotgunFire:
                SecondaryShootShotgun();
                break;
            case SecondaryActionType.Zoom:
                ToggleZoom(); // or StartZoom()/StopZoom() based on input phase
                break;
            case SecondaryActionType.GrenadeThrow:
                //ThrowGrenade();
                break;
            case SecondaryActionType.ChargeShot:
                //StartCoroutine(ChargeAndFire());
                break;
            case SecondaryActionType.None:
            default:
                Debug.Log("No secondary action for this weapon.");
                break;
        }
    }

    private void SecondaryShootShotgun()
    {

        if (!isLocalPlayer) return;

        int pelletCount = actualWeapon.GetComponent<MainWeapon>().shotgunPelletCount;
        float spreadAngle = actualWeapon.GetComponent<MainWeapon>().shotgunSpreadAngle; 
        Vector3 origin = muzzleFlashSpawnPoint.position;

        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 spreadDir = GetSpreadDirection(spreadAngle);
            Vector3 spawnPos = origin + spreadDir * 0.6f;

            CmdShootShotgunPellet(spawnPos, spreadDir);
        }

        ShowMuzzleFlash(playerCamera.transform.forward);

       
    }

    [Command]
    private void CmdShootShotgunPellet(Vector3 spawnPos, Vector3 direction)
    {
        if (actualWeapon == null || actualWeapon.secondaryProjectilePrefab == null)
        {
            Debug.LogWarning("Secondary projectile prefab missing.");
            return;
        }

        GameObject pellet = Instantiate(actualWeapon.secondaryProjectilePrefab, spawnPos, Quaternion.LookRotation(direction));

        var proj = pellet.GetComponent<Projectile>();
        if (proj != null)
        {
            proj.shooterIdentity = connectionToClient.identity;
        }

        Collider pelletCol = pellet.GetComponent<Collider>();
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            Physics.IgnoreCollision(pelletCol, col);
        }

        NetworkServer.Spawn(pellet);

        Rigidbody rb = pellet.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = direction * projectileSpeed;
            rb.useGravity = true;
        }
        RpcTriggerRecoil();
        Debug.Log("Spawned shotgun pellet on server at " + spawnPos);
    }

    private Vector3 GetSpreadDirection(float angle)
    {
        Quaternion spreadRotation = Quaternion.Euler(
            Random.Range(-angle, angle),
            Random.Range(-angle, angle),
            0f);
        return spreadRotation * playerCamera.transform.forward;
    }

    private void ToggleZoom()
    {
        isZoomed = !isZoomed;

        if (virtualCamera != null)
        {
            // Set the FOV based on whether the player is zoomed or not
            float targetFOV = isZoomed ? actualWeapon.GetComponent<Rifle>().zoomedFOV : actualWeapon.GetComponent<Rifle>().defaultFOV;
            StartCoroutine(ZoomCoroutine(targetFOV));
        }
    }

    private IEnumerator ZoomCoroutine(float targetFOV)
    {
        if (virtualCamera != null)
        {
            float startFOV = virtualCamera.m_Lens.FieldOfView;
            float elapsedTime = 0f;
            float zoomDuration = 0.3f; // Time it takes to zoom in or out

            while (elapsedTime < zoomDuration)
            {
                virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsedTime / zoomDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            virtualCamera.m_Lens.FieldOfView = targetFOV; // Ensure the final value is exactly the target FOV
        }
    }

    private void HandleZoomInput(bool isPressed)
    {
        if (isZoomed != isPressed)
        {
            isZoomed = isPressed;

            if (virtualCamera != null && actualWeapon != null)
            {
                float targetFOV = isZoomed
                    ? actualWeapon.GetComponent<Rifle>().zoomedFOV
                    : actualWeapon.GetComponent<Rifle>().defaultFOV;

                StartCoroutine(ZoomCoroutine(targetFOV));
            }
        }
    }


}
