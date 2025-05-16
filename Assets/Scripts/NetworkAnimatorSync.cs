using UnityEngine;
using Mirror;

public class NetworkAnimatorSync : NetworkBehaviour
{
    [SerializeField] public Animator thirdPersonAnimator;

    [SyncVar(hook = nameof(OnRunningChanged))] private bool isRunning;
    [SyncVar(hook = nameof(OnJumpingChanged))] private bool isJumping;
    [SyncVar(hook = nameof(OnGroundedChanged))] private bool isGrounded;
    [SyncVar(hook = nameof(OnAimPitchChanged))] [SerializeField] private float aimPitch;
    [SyncVar(hook = nameof(OnAimYawChanged))] [SerializeField] private float aimYaw;

    private void Awake()
    {
        if (thirdPersonAnimator == null)
        {
            thirdPersonAnimator = GetComponent<Animator>();
        }
    }

    // ---------------- Running ----------------
    public void SetRunning(bool running)
    {
        CmdSetRunning(running);
    }

    [Command]
    private void CmdSetRunning(bool running)
    {
        isRunning = running;
    }

    private void OnRunningChanged(bool _, bool newVal)
    {
        if (isLocalPlayer) return;
        thirdPersonAnimator.SetBool("IsRunning", newVal);
    }

    // ---------------- Jumping ----------------
    public void SetJumping(bool jumping)
    {
        CmdSetJumping(jumping);
    }

    [Command]
    private void CmdSetJumping(bool jumping)
    {
        isJumping = jumping;
    }

    private void OnJumpingChanged(bool _, bool newVal)
    {
        if (isLocalPlayer) return;
        thirdPersonAnimator.SetBool("IsJumping", newVal);
    }

    // ---------------- Grounded ----------------
    public void SetGrounded(bool grounded)
    {
        CmdSetGrounded(grounded);
    }

    [Command]
    private void CmdSetGrounded(bool grounded)
    {
        isGrounded = grounded;
    }

    private void OnGroundedChanged(bool _, bool newVal)
    {
        if (isLocalPlayer) return;
        thirdPersonAnimator.SetBool("IsGrounded", newVal);
    }

    // ---------------- Aim Angles ----------------
    // Called by local player whenever its camera moves:
    public void UpdateAimAngles(float pitch, float yaw)
    {
        if (thirdPersonAnimator != null)
        {
            thirdPersonAnimator.SetFloat("AimPitch", pitch);
            thirdPersonAnimator.SetFloat("AimYaw", yaw);
        }

        if (isLocalPlayer)
        {
            CmdUpdateAimAngles(pitch, yaw);
        }
    }

    [Command]
    void CmdUpdateAimAngles(float p, float y)
    {
        aimPitch = p;
        aimYaw = y;
    }

    void OnAimPitchChanged(float _, float newPitch)
    {
        if (isLocalPlayer) return;
        thirdPersonAnimator.SetFloat("AimPitch", newPitch);
    }

    void OnAimYawChanged(float _, float newYaw)
    {
        if (isLocalPlayer) return;
        thirdPersonAnimator.SetFloat("AimYaw", newYaw);
    }
}
