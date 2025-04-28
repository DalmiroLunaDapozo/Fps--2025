using UnityEngine;
using Mirror;
using System.Collections;

public class PickableHolder : NetworkBehaviour
{
    public GameObject pickablePrefab; // The prefab to spawn
    public float spawnInterval = 10f; // How often to try spawning

    private GameObject currentPickable; // Reference to the spawned pickable

    public Transform spawnPoint; // Where to spawn (can just be the holder's transform)

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
        currentPickable = Instantiate(pickablePrefab, spawnPoint.position, spawnPoint.rotation);
        NetworkServer.Spawn(currentPickable);
    }
}
