using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Mirror;

public class UIManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI life_text;
    [SerializeField] private TextMeshProUGUI deaths_text;
    [SerializeField] private TextMeshProUGUI kills_text;
    [SerializeField] private DamagablePlayer damagablePlayer;
    private PlayerController localPlayer;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Find the local player controller
        foreach (var obj in FindObjectsOfType<PlayerController>())
        {
            if (obj.isLocalPlayer)
            {
                localPlayer = obj;
                Transform bodyCollision = obj.transform.Find("BodyCollision");
                if (bodyCollision != null)
                {
                    damagablePlayer = bodyCollision.GetComponent<DamagablePlayer>();
                }
                else
                {
                    Debug.LogWarning("BodyCollision not found under local player!");
                }
                break;
            }
        }
    }

    private void Update()
    {
        if (localPlayer == null) return;

        life_text.text = "Life: " + localPlayer.health;
        deaths_text.text = "Deaths: " + damagablePlayer.deathCount;
        kills_text.text = "Kills: " + damagablePlayer.killCount;

    }
}
