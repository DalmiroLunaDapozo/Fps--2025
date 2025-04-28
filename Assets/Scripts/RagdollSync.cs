using UnityEngine;
using Mirror;

public class RagdollSync : NetworkBehaviour
{
    public Transform[] bones; // Bones to be synchronized
    private Vector3[] bonePositions;
    private Quaternion[] boneRotations;

    private bool isRagdoll = false; // Track if ragdoll is active

    void Start()
    {
        bonePositions = new Vector3[bones.Length];
        boneRotations = new Quaternion[bones.Length];
    }

    [ClientCallback]
    void LateUpdate()
    {
        if (isServer) return; // Server controls ragdoll and physics

        // If ragdoll is not active, don't update bones manually
        if (!isRagdoll) return;

        // Synchronize bone positions and rotations when ragdoll is active
        for (int i = 0; i < bones.Length; i++)
        {
            bones[i].localPosition = bonePositions[i];
            bones[i].localRotation = boneRotations[i];
        }
    }

    // Call this to enable ragdoll and sync bones from the server
    public void SetRagdollState(bool isActive)
    {
        if (isServer)
        {
            isRagdoll = isActive;
            RpcUpdateBoneTransformsOnRagdoll(isActive);
        }

        if (isActive)
        {
            EnableRagdoll();
        }
        else
        {
            DisableRagdoll();
        }
    }

    // Sync ragdoll bone transforms from the server to the client
    [ClientRpc]
    public void RpcUpdateBoneTransforms(Vector3[] positions, Quaternion[] rotations)
    {
        for (int i = 0; i < bones.Length; i++)
        {
            bonePositions[i] = positions[i];
            boneRotations[i] = rotations[i];
        }
    }

    // Sync ragdoll bone transforms from the server to the client when ragdoll is enabled
    [ClientRpc]
    private void RpcUpdateBoneTransformsOnRagdoll(bool isActive)
    {
        if (isActive)
        {
            Vector3[] positions = new Vector3[bones.Length];
            Quaternion[] rotations = new Quaternion[bones.Length];

            for (int i = 0; i < bones.Length; i++)
            {
                positions[i] = bones[i].localPosition;
                rotations[i] = bones[i].localRotation;
            }

            RpcUpdateBoneTransforms(positions, rotations);
        }
    }

    // Enable ragdoll and activate physics
    private void EnableRagdoll()
    {
        foreach (var bone in bones)
        {
            Rigidbody rb = bone.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;  // Enable physics
                rb.useGravity = true;
            }

            Collider col = bone.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;  // Enable collision
            }
        }
    }

    // Disable ragdoll physics and colliders
    private void DisableRagdoll()
    {
        foreach (var bone in bones)
        {
            Rigidbody rb = bone.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;  // Disable physics
                rb.useGravity = false;
            }

            Collider col = bone.GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = false;  // Disable collision
            }
        }
    }
}
