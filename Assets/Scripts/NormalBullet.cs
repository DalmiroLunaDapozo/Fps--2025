using UnityEngine;
using Mirror;

public class NormalBullet : Projectile
{

    private void Awake()
    {
        weapon = FindObjectOfType<MainWeapon>();    
    }

}
