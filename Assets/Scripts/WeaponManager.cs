using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class WeaponManager : NetworkBehaviour
{
    public List<Weapon> allWeapons;                   // Assigned in Inspector: all possible weapons
    public List<Weapon> unlockedWeapons = new();      // Only the currently unlocked weapons
    public int currentWeaponIndex = 0;
    private Weapon currentWeapon;

    private PlayerControls inputActions;
    private FPSShoot shootController;
    private FPSMovement movement;

    public UIManager pickupUI;

    void Awake()
    {
        inputActions = new PlayerControls();
        shootController = GetComponent<FPSShoot>();
        movement = GetComponent<FPSMovement>();
    }

    void OnEnable()
    {
        inputActions.Enable();
    }

    void OnDisable()
    {
        inputActions.Disable();
    }

    void Start()
    {
        // Deactivate all weapons
        foreach (var weapon in allWeapons)
        {
            if (weapon != null)
                weapon.gameObject.SetActive(false);
        }

        // Unlock the first weapon only
        if (allWeapons.Count > 0)
        {
            Weapon startingWeapon = allWeapons[0];
            unlockedWeapons.Add(startingWeapon);
            EquipWeapon(0);
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        float scroll = inputActions.Player.ChangeWeapon.ReadValue<float>();

        if (scroll > 0.1f)
        {
            ScrollWeapon(1);
        }
        else if (scroll < -0.1f)
        {
            ScrollWeapon(-1);
        }
    }

    void ScrollWeapon(int direction)
    {
        if (unlockedWeapons.Count <= 1) return;

        currentWeaponIndex += direction;

        if (currentWeaponIndex >= unlockedWeapons.Count) currentWeaponIndex = 0;
        else if (currentWeaponIndex < 0) currentWeaponIndex = unlockedWeapons.Count - 1;

        CmdEquipWeapon(currentWeaponIndex);
    }

    [Command]
    void CmdEquipWeapon(int index)
    {
        RpcEquipWeapon(index);
        EquipWeapon(index);
    }

    [ClientRpc]
    void RpcEquipWeapon(int index)
    {
        EquipWeapon(index);
    }

    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= unlockedWeapons.Count) return;

        if (currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(false);
            var oldSway = currentWeapon.GetComponent<SwayGun>();
            if (oldSway != null)
                oldSway.enabled = false;
        }

        currentWeaponIndex = index;
        currentWeapon = unlockedWeapons[index];
        currentWeapon.gameObject.SetActive(true);

        if (shootController != null)
        {
            shootController.SetWeaponData(currentWeapon);
            shootController.ResetFireTimer();
        }

        if (movement != null)
        {
            movement.SetRecoil(currentWeapon);
        }

        var newSway = currentWeapon.GetComponent<SwayGun>();
        if (newSway != null)
        {
            newSway.enabled = true;
            newSway.ReinitializeSway();
        }
    }

    public Weapon GetCurrentWeapon()
    {
        return currentWeapon;
    }

    [Command]
    public void CmdPickupWeaponByIndex(int index)
    {
        RpcUnlockWeapon(index);
    }

    [ClientRpc]
    void RpcUnlockWeapon(int index)
    {
        if (index < 0 || index >= allWeapons.Count) return;

        Weapon weaponToUnlock = allWeapons[index];
        if (unlockedWeapons.Contains(weaponToUnlock)) return;

        weaponToUnlock.gameObject.SetActive(false); // Keep it disabled until equipped
        unlockedWeapons.Add(weaponToUnlock);

        // If this is the first weapon picked up after start, auto-equip it
        if (unlockedWeapons.Count == 1)
        {
            EquipWeapon(0);
        }

        if (isLocalPlayer && pickupUI != null)
        {
            pickupUI.ShowPickupMessage($"Picked up: {weaponToUnlock.weaponName}");
        }
    }

    [Command]
    public void CmdPickupWeaponAndDestroy(int index, NetworkIdentity pickup)
    {
        RpcUnlockWeapon(index);

        // Now destroy the pickup across all clients
        if (pickup != null)
            NetworkServer.Destroy(pickup.gameObject);
    }
}
