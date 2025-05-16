using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Mirror;

public class FPSMovement : NetworkBehaviour
{
    public CharacterController characterController;
    public Transform cameraPivot;
    public Transform gunTransform;

    public float moveSpeed = 5f;
    public float lookSpeedX = 2f;
    public float lookSpeedY = 2f;
    public float jumpHeight = 2f;
    public float gravity = -9.8f;
    public int numberOfJumps;
    private int currentNumberOfJumps;

    private PlayerControls controls;
    private PlayerController playercontroller;
    private Vector2 moveInput;
    private Vector2 lookInput;
    private float ySpeed;
    private float verticalRotation = 0f; // Track vertical rotation separately
    private bool wasGrounded;

    // **Recoil Variables**
    private float recoilAmount = 2f;  // How much the camera moves up/down with recoil
    private float recoilSpeed = 5f;   // How quickly the camera recovers from recoil
    private float currentRecoil = 0f; // Current recoil applied to the camera

    [SerializeField] private NetworkAnimatorSync animatorSync;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform characterTransform;
    [SerializeField] private Transform model;
    [SerializeField] private float modelYaw;


    [SyncVar(hook = nameof(OnModelYawChanged))]
    private float syncedModelYaw;

    private void Awake()
    {
        controls = new PlayerControls();
        controls.Enable();

        controls.Player.Jump.performed += ctx => HandleJump();

        playercontroller = GetComponent<PlayerController>();
    }

    private void Start()
    {
        currentNumberOfJumps = numberOfJumps;
        animatorSync = GetComponentInChildren<NetworkAnimatorSync>();
    }

    private void OnDisable()
    {
        controls.Player.Jump.performed -= ctx => HandleJump();
        controls.Disable();
    }

    private void Update()
    {
        if (!isLocalPlayer) return;
        if (playercontroller.isDead) return;

        HandleMovement();
        HandleLook();
        ApplyGravity();

        if (isLocalPlayer)
        {
            float desiredYaw = modelYaw;
            CmdSetModelYaw(desiredYaw);
        }

        model.localRotation = Quaternion.Euler(0f, modelYaw, 0f);

        // Apply recoil effect when shooting
        if (currentRecoil != 0f)
        {
            ApplyRecoil();
        }

        if (animatorSync != null)
        {
            bool isRunning = moveInput.magnitude > 0.1f;
            animatorSync.SetRunning(isRunning);
            animatorSync.thirdPersonAnimator.SetBool("IsRunning", isRunning);

            // Pitch: Vertical angle (camera up/down)
            float pitch = verticalRotation;
            pitch = NormalizePitch(pitch);
            pitch = Mathf.Clamp(pitch - 90f, -45f, 45f); // Remap [90,180] -> [0,-90] then clamp

            // Yaw: Horizontal difference between character facing and camera facing
            Vector3 flatForward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 flatCharacterForward = Vector3.ProjectOnPlane(characterTransform.forward, Vector3.up).normalized;
            float yaw = Vector3.SignedAngle(flatCharacterForward, flatForward, Vector3.up);
            yaw = Mathf.Clamp(yaw, -45f, 45f); // Match animation threshold

            // Send to animator
            animatorSync.UpdateAimAngles(pitch, yaw);

            // Local debug only
            animatorSync.thirdPersonAnimator.SetBool("IsGrounded", IsGrounded());
            animatorSync.thirdPersonAnimator.SetBool("IsJumping", ySpeed > 0.1f);
            
        }
    }

    private void OnModelYawChanged(float oldYaw, float newYaw)
    {
        model.localRotation = Quaternion.Euler(0f, newYaw, 0f);
    }
    [Command]
    public void CmdSetModelYaw(float yaw)
    {
        syncedModelYaw = yaw;
    }
    private float NormalizePitch(float pitch)
    {
        pitch %= 360f;
        if (pitch > 180f) pitch -= 360f;
        return pitch;
    }
    private void HandleMovement()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>(); // Movement input

        // Handle the horizontal and vertical movement
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        characterController.Move(move * moveSpeed * Time.deltaTime);
    }

    private void HandleJump()
    {
        if (characterController.isGrounded || currentNumberOfJumps > 0)
        {
            ySpeed = Mathf.Sqrt(jumpHeight * -2f * gravity); // Apply jump force
            currentNumberOfJumps -= 1;
            animatorSync?.SetJumping(true);
        }
    }

    private void HandleLook()
    {
        lookInput = controls.Player.Look.ReadValue<Vector2>();

        // Rotate Player Body (yaw) only, sync this rotation for all players
        if (isLocalPlayer)
        {
            float yaw = lookInput.x * lookSpeedX * Time.deltaTime;
            Quaternion yawRotation = Quaternion.Euler(0, yaw, 0);
            transform.rotation *= yawRotation; // Only affects Y axis (yaw)
        }

        // Rotate Camera Pivot Up/Down (pitch), with clamping
        verticalRotation -= lookInput.y * lookSpeedY * Time.deltaTime;
        verticalRotation = Mathf.Clamp(verticalRotation, 10f, 170f); // Clamp the camera's pitch to 0-180 degrees

        cameraPivot.localRotation = Quaternion.Euler(verticalRotation, 0, 0); // Apply to camera pivot
    }

    private void ApplyGravity()
    {
        bool grounded = IsGrounded();

        if (grounded)
        {
            if (!wasGrounded)
            {
                animatorSync?.SetJumping(false);
            }

            animatorSync?.SetGrounded(true);
            currentNumberOfJumps = numberOfJumps;

            if (ySpeed < 0)
                ySpeed = -0.5f;
        }
        else
        {
            animatorSync?.SetGrounded(false);
            ySpeed += gravity * Time.deltaTime;
        }

        wasGrounded = grounded;

        Vector3 verticalMovement = new Vector3(0, ySpeed, 0);
        characterController.Move(verticalMovement * Time.deltaTime);
    }

    // **Recoil Logic**
    public void TriggerRecoil()
    {
        // Trigger recoil when shooting
        currentRecoil = recoilAmount;  // Set recoil value
    }

    private void ApplyRecoil()
    {
        // Apply recoil to camera's pitch (X-axis rotation)
        verticalRotation -= currentRecoil * Time.deltaTime * recoilSpeed;

        // Clamp the recoil to prevent excessive rotation
        verticalRotation = Mathf.Clamp(verticalRotation, 0f, 180f);

        cameraPivot.localRotation = Quaternion.Euler(verticalRotation, 0, 0); // Apply new rotation to camera pivot

        // Gradually recover recoil over time
        currentRecoil = Mathf.Lerp(currentRecoil, 0f, Time.deltaTime * recoilSpeed);
    }

    public bool IsGrounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, characterController.height / 2 + 0.2f);
    }

    public void StopShooting()
    {
        // Stop recoil if needed
        currentRecoil = 0f;
    }

    public void SetRecoil(Weapon weapon)
    {
        recoilAmount = weapon.recoilAmount;
        recoilSpeed = weapon.recoilSpeed;
    }

    public Vector2 GetMoveInput()
    {
        return moveInput;
    }

    [TargetRpc]
    public void TargetRocketJump(NetworkConnection target, Vector3 explosionPosition, float force)
    {
        Debug.Log($"TargetRocketJump triggered for: {gameObject.name}");

        if (characterController != null)
        {
            // Calculate the direction to move (opposite of explosion)
            Vector3 direction = (transform.position - explosionPosition).normalized;
            Vector3 jumpVelocity = direction * force;

            // Apply rocket jump force (smooth)
            StartCoroutine(ApplyRocketJump(jumpVelocity));
        }
        else
        {
            Debug.LogWarning("No CharacterController found on client for RocketJump.");
        }
    }

    private IEnumerator ApplyRocketJump(Vector3 jumpVelocity)
    {
        // How long the rocket jump force lasts
        float jumpDuration = 0.3f;
        float timeElapsed = 0f;

        // Smoothly apply the jump force over time
        while (timeElapsed < jumpDuration)
        {
            // Gradually decrease the force as time goes on
            float lerpFactor = timeElapsed / jumpDuration;
            Vector3 smoothVelocity = Vector3.Lerp(jumpVelocity, Vector3.zero, lerpFactor); // Smooth transition to zero velocity

            // Apply the smooth velocity to the character controller
            characterController.Move(smoothVelocity * Time.deltaTime);

            // Increment time
            timeElapsed += Time.deltaTime;

            yield return null;
        }

        // After the jump duration, gravity will naturally take over and the player will fall
    }

    public void ApplyLocalRocketJump(Vector3 velocity)
    {
        Debug.Log($"Applying local rocket jump for {gameObject.name}");
        StartCoroutine(ApplyRocketJump(velocity));
    }

    public void RocketJump(Vector3 explosionPosition, float explosionForce)
    {
        Vector3 direction = (transform.position - explosionPosition).normalized;
        Vector3 jumpVelocity = direction * explosionForce;

        // Apply the jump locally
        if (isLocalPlayer)
        {
            StartCoroutine(ApplyRocketJump(jumpVelocity)); // Local application of rocket jump
            CmdApplyRocketJump(jumpVelocity); // Tell the server to apply the jump and sync it
        }
    }

    [Command]
    public void CmdApplyRocketJump(Vector3 jumpVelocity)
    {
        ApplyRocketJump(jumpVelocity); // Apply the force on the server
        RpcSyncRocketJump(jumpVelocity); // Sync the jump across all clients
    }

    [ClientRpc]
    public void RpcSyncRocketJump(Vector3 jumpVelocity)
    {
        if (!isLocalPlayer) // Only apply the rocket jump to the local player
        {
            StartCoroutine(ApplyRocketJump(jumpVelocity)); // Apply the smooth rocket jump
        }
    }



}