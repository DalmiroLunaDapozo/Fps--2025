using Mirror;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    public string playerName = "Player";
    public GameObject cameraHolder;              // Camera holder (pivot) assigned in the inspector
    public GameObject armaPrimeraPersona;        // First person weapon
    public GameObject armaTerceraPersona;        // Third person weapon

    [SyncVar] public int health;
    [SyncVar] public int maxHealth;
    [SyncVar] public bool isDead = false;

    [SyncVar(hook = nameof(OnGunYawChanged))] private float syncedGunYaw; // Only sync the horizontal rotation (yaw)

    public float pitchClampMin = -80f;
    public float pitchClampMax = 80f;

    public Animator animator;
    public DamagablePlayer damagablePlayer;

    // Rotation speed multiplier for smooth lerping (degrees per second)
    public float rotationSpeed = 180f; // You can tweak this in Inspector

    // Debug offset if you want to adjust third person weapon yaw
    public float debugYawOffset = 0f;

    // Target rotation yaw for third-person weapon, updated by SyncVar hook
    private float targetYaw;

    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }

    public override void OnStartClient()
    {
        if (!isLocalPlayer)
        {
            cameraHolder.SetActive(false);
            armaPrimeraPersona.SetActive(false);
            armaTerceraPersona.SetActive(true);
        }
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            health = maxHealth;
            cameraHolder.SetActive(false);
            GetComponent<FPSMovement>().enabled = false;
            GetComponent<FPSShoot>().enabled = false;
            damagablePlayer = GetComponentInChildren<DamagablePlayer>();
        }
        else
        {
            cameraHolder.SetActive(true);
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Sync only the yaw of the first-person weapon
        //if (armaPrimeraPersona != null)
        //{
        //    float yaw = armaPrimeraPersona.transform.eulerAngles.y;
        //    CmdSendGunYaw(yaw);
        //}
    }

    [Command]
    void CmdSendGunYaw(float yaw)
    {
        syncedGunYaw = yaw;
    }

    //void LateUpdate()
    //{
    //    if (isLocalPlayer) return;

    //    if (armaTerceraPersona != null)
    //    {
    //        // Smoothly interpolate towards the target yaw
    //        targetYaw = syncedGunYaw + debugYawOffset;

    //        Quaternion currentRot = armaTerceraPersona.transform.localRotation;
    //        Quaternion targetRot = Quaternion.Euler(currentRot.eulerAngles.x, targetYaw, currentRot.eulerAngles.z);

    //        // Smooth rotation with RotateTowards for consistent speed
    //        armaTerceraPersona.transform.localRotation = Quaternion.RotateTowards(
    //            currentRot,
    //            targetRot,
    //            rotationSpeed * Time.deltaTime
    //        );
    //    }
    //}

    public override void OnStartLocalPlayer()
    {
        cameraHolder.SetActive(true);
        armaPrimeraPersona.SetActive(true);
        armaTerceraPersona.SetActive(false);

        base.OnStartLocalPlayer();

        Canvas worldCanvas = FindObjectOfType<Canvas>();
        if (worldCanvas != null && worldCanvas.renderMode == RenderMode.WorldSpace)
        {
            worldCanvas.worldCamera = GetComponentInChildren<Camera>();
        }
    }

    void OnGunYawChanged(float oldYaw, float newYaw)
    {
        // Just update the target yaw, smoothing happens in LateUpdate
        if (!isLocalPlayer && armaTerceraPersona != null)
        {
            targetYaw = newYaw + debugYawOffset;
        }
    }

    public void AddKill()
    {
        damagablePlayer.killCount++;
    }

    public void Heal(int amount)
    {
        health += amount;
        health = Mathf.Min(health, maxHealth); // Max health
        Debug.Log($"Healed by {amount}. Current health: {health}");
    }

    public float GetHealth()
    {
        return health;  // Implement as per your health system
    }
}
