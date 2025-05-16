using UnityEngine;
using Mirror;
using System.Collections;

public class PickableHolder : NetworkBehaviour
{
    public GameObject pickablePrefab; // The prefab to spawn
    public float spawnInterval = 10f; // How often to try spawning

    private GameObject currentPickable; // Reference to the spawned pickable

    public Transform spawnPoint; // Where to spawn (can just be the holder's transform)

    public Vector3 spawnOffset;
    public Quaternion spawnRotationOffset;

    private void Start()
    {
        if (isServer)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            if (currentPickable == null)
            {
                SpawnPickable();
            }
        }
    }

    [Server]
    private void SpawnPickable()
    {
        currentPickable = Instantiate(pickablePrefab, spawnPoint.position + spawnOffset, Quaternion.identity * spawnRotationOffset);

        // Link back to this holder
        PickableObject pickableScript = currentPickable.GetComponent<PickableObject>();
        if (pickableScript != null)
        {
            pickableScript.holder = this;
        }

        NetworkServer.Spawn(currentPickable);
    }

    [Server]
    public void NotifyPickedUp()
    {
        currentPickable = null; // This allows the respawn timer to resume properly
    }
}
