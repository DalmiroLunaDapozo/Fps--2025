using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class WeaponManager : NetworkBehaviour
{
    public List<Weapon> allWeapons;
    public List<Weapon> unlockedWeapons = new();

    [SyncVar(hook = nameof(OnWeaponIndexChanged))]
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

    void OnEnable() => inputActions.Enable();
    void OnDisable() => inputActions.Disable();

    void Start()
    {
        foreach (var weapon in allWeapons)
        {
            if (weapon != null) weapon.gameObject.SetActive(false);
        }

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

        if (scroll > 0.1f) ScrollWeapon(1);
        else if (scroll < -0.1f) ScrollWeapon(-1);
    }

    void ScrollWeapon(int direction)
    {
        if (unlockedWeapons.Count <= 1) return;

        int newIndex = currentWeaponIndex + direction;
        if (newIndex >= unlockedWeapons.Count) newIndex = 0;
        else if (newIndex < 0) newIndex = unlockedWeapons.Count - 1;

        CmdEquipWeapon(newIndex);
    }

    [Command]
    void CmdEquipWeapon(int index)
    {
        currentWeaponIndex = index; // This triggers hook automatically
    }

    void OnWeaponIndexChanged(int oldIndex, int newIndex)
    {
        EquipWeapon(newIndex);

        if (isLocalPlayer && shootController != null)
        {
            shootController.SetWeaponData(unlockedWeapons[newIndex]);
        }
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

    public Weapon GetCurrentWeapon() => currentWeapon;

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

        weaponToUnlock.gameObject.SetActive(false);
        unlockedWeapons.Add(weaponToUnlock);

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
        if (pickup != null) NetworkServer.Destroy(pickup.gameObject);
    }
}
