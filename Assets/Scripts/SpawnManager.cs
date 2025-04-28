using UnityEngine;
using Mirror;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;

    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points assigned in SpawnManager.");
            return null;
        }

        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    public Transform GetSpawnPoint(int index)
    {
        if (index < 0 || index >= spawnPoints.Length)
            return GetRandomSpawnPoint();
        return spawnPoints[index];
    }
}
