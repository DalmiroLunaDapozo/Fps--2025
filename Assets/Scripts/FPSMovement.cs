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

        // Apply recoil effect when shooting
        if (currentRecoil != 0f)
        {
            ApplyRecoil();
        }

        if (animatorSync != null)
        {
            bool isRunning = moveInput.magnitude > 0.1f;
            animatorSync.SetRunning(isRunning);
        }
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
        verticalRotation = Mathf.Clamp(verticalRotation, 0f, 180f); // Clamp the camera's pitch to 0-180 degrees

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

    public void RocketJump(Vector3 explosionOrigin, float force)
    {
        // Calculate push direction
        Vector3 direction = (transform.position - explosionOrigin).normalized;
        direction.y = 0.5f; // Push upwards more (adjust this number to fine-tune)

        // Apply upward force directly to ySpeed (this is your vertical movement)
        ySpeed = Mathf.Sqrt(force * -2f * gravity);

        // Optional: Apply some horizontal force too
        StartCoroutine(ApplyRocketJumpHorizontal(direction.normalized, force * 0.3f)); // 30% of force sideways
    }

    private IEnumerator ApplyRocketJumpHorizontal(Vector3 horizontalDirection, float force)
    {
        float duration = 0.2f; // push lasts for 0.2s
        float timer = 0f;

        while (timer < duration)
        {
            characterController.Move(horizontalDirection * force * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
    }
}
