using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class WeaponManager : NetworkBehaviour
{
    public List<Weapon> weapons = new List<Weapon>();
    public int currentWeaponIndex = 0;
    private Weapon currentWeapon;

    private PlayerControls inputActions;
    private FPSShoot shootController;
    private FPSMovement movement;

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
        EquipWeapon(currentWeaponIndex);
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
        currentWeaponIndex += direction;

        if (currentWeaponIndex >= weapons.Count) currentWeaponIndex = 0;
        else if (currentWeaponIndex < 0) currentWeaponIndex = weapons.Count - 1;

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
        if (index >= weapons.Count) return;

        if (currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(false);

            SwayGun swayGun = currentWeapon.GetComponent<SwayGun>();
            if (swayGun != null)
                swayGun.enabled = false;
        }

        currentWeaponIndex = index;
        currentWeapon = weapons[index];
        currentWeapon.gameObject.SetActive(true);

        //  This block now runs for ALL instances, not just local player
        if (shootController != null)
        {
            shootController.SetWeaponData(currentWeapon);
            shootController.ResetFireTimer();
        }

        if (movement != null && movement is FPSMovement)
        {
            movement.SetRecoil(currentWeapon);
        }

        SwayGun newSwayGun = currentWeapon.GetComponent<SwayGun>();
        if (newSwayGun != null)
        {
            newSwayGun.enabled = true;
            newSwayGun.ReinitializeSway();
        }
    }

    public Weapon GetCurrentWeapon()
    {
        currentWeapon = weapons[currentWeaponIndex];
        return currentWeapon;
    }
}
