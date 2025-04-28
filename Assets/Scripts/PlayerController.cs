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

    [SyncVar] private float syncedGunYaw; // Only sync the horizontal rotation (yaw)

    public float pitchClampMin = -80f;
    public float pitchClampMax = 80f;

    public Animator animator;
    public DamagablePlayer damagablePlayer;

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
        if (armaPrimeraPersona != null)
        {
            float yaw = armaPrimeraPersona.transform.eulerAngles.y;
            CmdSendGunRotation(yaw);
        }

        // Still clamp pitch if needed
        float pitch = NormalizeAngle(cameraHolder.transform.localEulerAngles.x);
        pitch = Mathf.Clamp(pitch, pitchClampMin, pitchClampMax);
        float yawPlayer = transform.eulerAngles.y;
        CmdSendLookRotation(pitch, yawPlayer);
    }

    [Command]
    void CmdSendLookRotation(float x, float y)
    {
        // Optional if you're syncing body/camera look separately
    }

    [Command]
    void CmdSendGunRotation(float yaw)
    {
        syncedGunYaw = yaw;
    }

    void LateUpdate()
    {
        if (isLocalPlayer) return;

        // Apply only yaw rotation to the third-person weapon
        if (armaTerceraPersona != null)
        {
            Quaternion targetRotation = Quaternion.Euler(0, syncedGunYaw + 180f, 0);
            armaTerceraPersona.transform.rotation = Quaternion.Slerp(
                armaTerceraPersona.transform.rotation,
                targetRotation,
                Time.deltaTime * 10f
            );
        }
    }

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
