using UnityEngine;
using Mirror;

public class DamageAccumulator : NetworkBehaviour
{
    public GameObject damagePopupPrefab;
    public float resetTime = 2f;
    public Vector3 offset;

    private int accumulatedDamage = 0;
    private GameObject activePopup;
    private Vector3 initialHitPosition;

    private float timeSinceLastHit = 0f;

    private void Update()
    {
        if (!isServer) return; // Only run accumulation logic on server

        if (activePopup == null) return;

        timeSinceLastHit += Time.deltaTime;

        if (timeSinceLastHit >= resetTime)
        {
            ResetDamage();
        }
    }

    [Server]
    public void AddDamage(int amount, Vector3 hitPoint)
    {
        accumulatedDamage += amount;
        timeSinceLastHit = 0f;

        if (activePopup == null)
        {
            initialHitPosition = hitPoint;
            RpcSpawnPopup(initialHitPosition + offset);
        }

        RpcUpdatePopup(accumulatedDamage);
    }

    [ClientRpc]
    private void RpcSpawnPopup(Vector3 position)
    {
        

        if (activePopup != null)
        {
            
            return;
        }

        Transform canvasTransform = FindObjectOfType<Canvas>()?.transform;
        if (canvasTransform == null)
        {
            //Debug.LogWarning("Canvas not found in scene!");
            return;
        }

        
        activePopup = Instantiate(damagePopupPrefab, position, Quaternion.identity, canvasTransform);
       
    }

    [ClientRpc]
    private void RpcUpdatePopup(int totalDamage)
    {
        if (activePopup != null)
        {
            activePopup.GetComponent<DamagePopup>().Setup(totalDamage);
            activePopup.GetComponent<DamagePopup>().StopFloating();
        }
    }

    [Server]
    private void ResetDamage()
    {
        accumulatedDamage = 0;
        RpcDestroyPopup();
    }

    [ClientRpc]
    private void RpcDestroyPopup()
    {
        if (activePopup != null)
        {
            Destroy(activePopup);
        }
        activePopup = null;
    }
}
