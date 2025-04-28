using UnityEngine;

public class WeaponIK : MonoBehaviour
{
    public Animator animator;
    public Transform rightHandTarget;
    public Transform lookTarget; // Usually the center of screen/crosshair
    public bool ikActive = true;

    void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || !ikActive) return;

        if (rightHandTarget != null)
        {
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
            animator.SetIKPosition(AvatarIKGoal.RightHand, rightHandTarget.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, rightHandTarget.rotation);
        }

        if (lookTarget != null)
        {
            animator.SetLookAtWeight(1f, 0.2f, 1f, 1f, 0.5f);
            animator.SetLookAtPosition(lookTarget.position);
        }
    }
}
