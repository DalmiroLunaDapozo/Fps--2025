using UnityEngine;
using Mirror;

public class WeaponPickup : NetworkBehaviour
{
    public int weaponIndex; // Index that matches the weapon in allWeapons

    private bool pickedUp = false;

    private void OnTriggerEnter(Collider other)
    {
        if (pickedUp) return;

        WeaponManager manager = other.GetComponentInParent<WeaponManager>();
        if (manager == null || !other.CompareTag("Player")) return;

        if (!manager.isLocalPlayer) return; // Ensure this runs only for local player

        // If player already has the weapon, skip pickup
        if (manager.unlockedWeapons.Contains(manager.allWeapons[weaponIndex])) return;

        pickedUp = true;

        // Ask server to give weapon to all clients
        manager.CmdPickupWeaponByIndex(weaponIndex);

        manager.CmdPickupWeaponAndDestroy(weaponIndex, netIdentity);
    }

    [Command]
    void CmdDestroyPickup()
    {
        // Ensure destruction happens after weapon is unlocked
        RpcDestroyPickup();
    }

    [ClientRpc]
    void RpcDestroyPickup()
    {
        NetworkServer.Destroy(gameObject);
    }
}
