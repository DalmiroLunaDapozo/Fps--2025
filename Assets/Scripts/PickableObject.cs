using UnityEngine;
using Mirror;

public enum PickableType
{
    Heal,
    Ammo,
    Buff
}

public class PickableObject : NetworkBehaviour
{
    public PickableType pickableType;
    public int amount = 10;

    [SerializeField] private float rotation_speed;

    private PlayerController player;

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return; // Only the server handles pickups

        if (other.CompareTag("Player"))
            player = other.GetComponentInParent<PlayerController>();
        else
            return;

        if (pickableType == PickableType.Heal)
            if (player.GetHealth() == player.maxHealth) return;
           
        ApplyEffect(player);
        NetworkServer.Destroy(gameObject); // Remove the pickup from the network
      
    }

    private void ApplyEffect(PlayerController player)
    {
        switch (pickableType)
        {
            case PickableType.Heal:
                player.Heal(amount);
                break;
            case PickableType.Ammo:
                //player.AddAmmo(amount);
                break;
            case PickableType.Buff:
                //player.ApplyBuff(amount);
                break;
            default:
                Debug.LogWarning("Pickable type not implemented.");
                break;
        }
    }

    private void Update()
    {
        transform.Rotate(Vector3.up * rotation_speed * Time.deltaTime);
    }
}
