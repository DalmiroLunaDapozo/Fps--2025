using UnityEngine;
using Mirror;

public class NetworkTransformInterpolator : NetworkBehaviour
{
    [Tooltip("How fast to interpolate position changes")]
    public float positionLerpSpeed = 15f;

    [Tooltip("How fast to interpolate rotation changes")]
    public float rotationLerpSpeed = 15f;

    private Vector3 serverPosition;
    private Quaternion serverRotation;

    public override void OnStartAuthority()
    {
        // Disable interpolation on the owning client
        enabled = false;
    }

    void Update()
    {
        if (isServer) return; // Server doesn't interpolate
        // Smooth position
        transform.position = Vector3.Lerp(transform.position, serverPosition, Time.deltaTime * positionLerpSpeed);
        // Smooth rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, serverRotation, Time.deltaTime * rotationLerpSpeed);
    }

    public override void OnSerialize(NetworkWriter writer, bool initialState)
    {
        writer.WriteVector3(transform.position);
        writer.WriteQuaternion(transform.rotation);
    }

    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        serverPosition = reader.ReadVector3();
        serverRotation = reader.ReadQuaternion();
    }
}
