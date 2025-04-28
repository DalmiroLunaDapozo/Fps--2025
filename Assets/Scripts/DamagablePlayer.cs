using UnityEngine;
using System.Collections;
using Mirror;

public class DamagablePlayer : NetworkBehaviour, IDamageable
{
    private PlayerController playerController;
    private DeathScreenUI deathScreenUI;
    private CharacterController characterController;

    public float respawnTime = 5f;
    public Transform[] spawnPoints;

    private GameObject killerBullet;

    [SerializeField] private Rigidbody[] ragdollRigidbodies;
    [SerializeField] private Collider[] ragdollColliders;
    [SerializeField] private KillScreenUI killerScreen;
    private Collider damageCollider;

    [SyncVar] public int killCount = 0;
    [SyncVar] public int deathCount = 0;

    private void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        characterController = GetComponentInParent<CharacterController>();
        deathScreenUI = FindObjectOfType<DeathScreenUI>();

        InitializeRagdoll();

        if (isServer)
        {
            DisableRagdoll();
        }
    }

    private void InitializeRagdoll()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        damageCollider = GetComponent<Collider>();

        foreach (Rigidbody rb in ragdollRigidbodies)
            rb.isKinematic = true;

        foreach (Collider col in ragdollColliders)
            col.enabled = false;

        if (damageCollider != null)
            damageCollider.enabled = true;
    }

    public void TakeDamage(int amount)
    {
        GameObject attacker = null;
        if (isServer)
            ApplyDamage(amount, attacker);
        else
            CmdRequestDamage(amount, attacker);
    }

    public void TakeDamage(int amount, GameObject attacker)
    {
        if (isServer)
            ApplyDamage(amount, attacker);
        else
            CmdRequestDamage(amount, attacker);
    }

    [Command]
    private void CmdRequestDamage(int amount, GameObject attacker)
    {
        ApplyDamage(amount, attacker);
    }

    [Server]
    private void ApplyDamage(int amount, GameObject attacker)
    {
        if (playerController.isDead) return;

        playerController.health -= amount;

        if (playerController.health <= 0)
        {
            playerController.health = 0;

            if (attacker != null && attacker != this.gameObject)
            {
                killerBullet = attacker;
            }

            Die(killerBullet);
        }

        RpcOnDamageTaken(attacker != null ? attacker.GetComponent<NetworkIdentity>() : null);
    }

    [Server]
    public void Die(GameObject killer)
    {
        playerController.isDead = true;

        RpcOnDeath(killer != null ? killer.GetComponent<NetworkIdentity>() : null);

        EnableRagdoll();
        RpcEnableRagdoll();

        if (killer != null && killer != this.gameObject)
        {
            ApplyDeathForce(killer);

            DamagablePlayer killerPlayer = killer.GetComponentInChildren<DamagablePlayer>();
            if (killerPlayer != null && killerPlayer != this)
            {
                killerPlayer.killCount++;
                killerPlayer.TargetShowKillScreen(killerPlayer.connectionToClient, playerController.playerName); // <-- this is new
            }
        }

        deathCount++;

        StartCoroutine(RespawnAfterDelay(respawnTime));
    }

    [Server]
    private IEnumerator RespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Respawn();
    }

    [ClientRpc]
    private void RpcOnDamageTaken(NetworkIdentity attackerNetId)
    {
        if (!isLocalPlayer) return;

        PlayerController attacker = attackerNetId != null ? attackerNetId.GetComponent<PlayerController>() : null;
        OnDamageTakenFeedback(attacker);
    }

    [ClientRpc]
    private void RpcOnDeath(NetworkIdentity killerNetId)
    {
        if (!isLocalPlayer) return;

        PlayerController killer = killerNetId != null ? killerNetId.GetComponent<PlayerController>() : null;
        deathScreenUI.ShowDeathScreen(respawnTime);
    }

    private void OnDamageTakenFeedback(PlayerController attacker)
    {
        DamageFlashUI flashUI = FindObjectOfType<DamageFlashUI>();
        if (flashUI != null)
            flashUI.TriggerFlash();
    }

    [Server]
    public void Respawn()
    {
        if (!playerController.isDead) return;

        killerBullet = null;

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        DisableRagdoll();

        Transform spawnPoint = SpawnManager.Instance.GetRandomSpawnPoint();
        if (spawnPoint != null)
        {
            Vector3 spawnPos = spawnPoint.position;

            transform.SetPositionAndRotation(spawnPos, spawnPoint.rotation);
            playerController.transform.SetPositionAndRotation(spawnPos, spawnPoint.rotation);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.MovePosition(spawnPos);
            }

            NetworkTransformReliable netTransform = GetComponent<NetworkTransformReliable>();
            if (netTransform != null)
                netTransform.ServerTeleport(spawnPos, spawnPoint.rotation);

            RpcForcePosition(spawnPoint.position, spawnPoint.rotation);
        }

        if (playerController.animator != null)
        {
            playerController.animator.enabled = true;
            playerController.animator.Play("Idle");
        }

        playerController.health = playerController.maxHealth;
        playerController.isDead = false;

        RpcRespawn(spawnPoint.position, spawnPoint.rotation);
    }

    [ClientRpc]
    private void RpcEnableRagdoll()
    {
        if (isServer) return; // Prevent the server from enabling ragdoll

        EnableRagdoll();
        if (playerController.animator != null)
        {
            playerController.animator.enabled = false; // Ensure the animator is disabled during ragdoll
        }
    }

    private void EnableRagdoll()
    {
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        foreach (Collider col in ragdollColliders)
            col.enabled = true;

        if (damageCollider != null)
            damageCollider.enabled = false;

        if (characterController != null)
            characterController.enabled = false;

        if (playerController != null)
            playerController.enabled = false;

        if (playerController.animator != null)
            playerController.animator.enabled = false;
    }

    private void DisableRagdoll()
    {
       

        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (Collider col in ragdollColliders)
        {
            if (col != damageCollider)
                col.enabled = false;
        }

        if (damageCollider != null)
            damageCollider.enabled = true;

        if (characterController != null)
            characterController.enabled = true;

        if (playerController != null)
            playerController.enabled = true;

        if (playerController.animator != null)
        {
            playerController.animator.enabled = true;
            playerController.animator.Play("Idle");
        }
    }

    [Server]
    private void ApplyDeathForce(GameObject killer)
    {
        if (killer == null) return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 direction = (transform.position - killer.transform.position).normalized;
            rb.AddForce(direction * 1f);
        }
    }

    [ClientRpc]
    private void RpcForcePosition(Vector3 position, Quaternion rotation)
    {
        if (!isServer)
        {
            transform.SetPositionAndRotation(position, rotation);
            playerController.transform.SetPositionAndRotation(position, rotation);

            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            // Reset animator to idle state to prevent T-pose
            if (playerController.animator != null)
            {
                playerController.animator.Play("Idle");
            }
        }
    }


    [ClientRpc]
    private void RpcRespawn(Vector3 position, Quaternion rotation)
    {
        StartCoroutine(DelayedEnableAfterRespawn(position, rotation));
    }

    private IEnumerator DelayedEnableAfterRespawn(Vector3 position, Quaternion rotation)
    {
        // Set position and rotation
        transform.SetPositionAndRotation(position, rotation);
        playerController.transform.SetPositionAndRotation(position, rotation);

        // Disable any ragdoll forces
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        yield return null;

        // Enable character controller and player controller after delay
        if (characterController != null)
            characterController.enabled = true;

        if (playerController != null)
            playerController.enabled = true;

        if (playerController.animator != null)
        {
            playerController.animator.enabled = true;
            playerController.animator.Play("Idle");
        }

        // Fix model position and rotation if needed
        Transform model = playerController.transform.Find("Model");
        if (model != null)
        {
            model.localPosition = Vector3.zero;
            model.localRotation = Quaternion.identity;
        }

        if (isLocalPlayer)
        {
            // Handle local camera reset or UI update
        }
    }

    public void AddKill()
    {
        killCount++;
       
    }

    [TargetRpc]
    private void TargetShowKillScreen(NetworkConnection target, string killedPlayerName)
    {
        if (killerScreen != null)
        {
            killerScreen.ShowKillScreen(killedPlayerName);
        }
    }



}
