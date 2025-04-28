using UnityEngine;
using System.Collections.Generic;

public class WeaponDatabase : MonoBehaviour
{
    public static WeaponDatabase Instance;

    [SerializeField] private List<Weapon> weapons;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject); // Optional
    }

    public Weapon GetWeaponByIndex(int index)
    {
        if (index < 0 || index >= weapons.Count)
        {
            //Debug.LogError($"Weapon index {index} out of range!");
            return null;
        }

        return weapons[index];
    }
}
