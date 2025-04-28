using UnityEngine;
using Mirror;

public class NetworkAnimatorSync : NetworkBehaviour
{
    [SerializeField] private Animator thirdPersonAnimator;

    [SyncVar(hook = nameof(OnRunningChanged))]
    private bool isRunning;

    [SyncVar(hook = nameof(OnJumpingChanged))]
    private bool isJumping;

    [SyncVar(hook = nameof(OnGroundedChanged))]
    private bool isGrounded;

    public void SetRunning(bool running)
    {
        if (isServer)
        {
            isRunning = running;
            thirdPersonAnimator.SetBool("IsRunning", running);
        }
        else
        {
            CmdSetRunning(running);
        }
    }

    [Command]
    private void CmdSetRunning(bool running)
    {
        isRunning = running;
        thirdPersonAnimator.SetBool("IsRunning", running);
    }

    private void OnRunningChanged(bool oldVal, bool newVal)
    {
        thirdPersonAnimator.SetBool("IsRunning", newVal);
    }

    public void SetJumping(bool jumping)
    {
        if (isServer)
        {
            isJumping = jumping;
            thirdPersonAnimator.SetBool("IsJumping", jumping);
        }
        else
        {
            CmdSetJumping(jumping);
        }
    }
    public void SetGrounded(bool grounded)
    {
        if (isServer)
        {
            isGrounded = grounded;
            thirdPersonAnimator.SetBool("IsGrounded", grounded);
        }
        else
        {
            CmdSetGrounded(grounded);
        }
    }

    [Command]
    private void CmdSetGrounded(bool grounded)
    {
        isGrounded = grounded;
        thirdPersonAnimator.SetBool("IsGrounded", grounded);
    }

    private void OnGroundedChanged(bool oldVal, bool newVal)
    {
        thirdPersonAnimator.SetBool("IsGrounded", newVal);
    }
    [Command]
    private void CmdSetJumping(bool jumping)
    {
        isJumping = jumping;
        thirdPersonAnimator.SetBool("IsJumping", jumping);
    }

    private void OnJumpingChanged(bool oldVal, bool newVal)
    {
        thirdPersonAnimator.SetBool("IsJumping", newVal);
    }
}
