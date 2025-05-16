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
    [SerializeField] private TextMeshProUGUI pickupText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private DamagablePlayer damagablePlayer;
    private PlayerController localPlayer;

    public float pickUpDisplayDuration = 2f;

    private Coroutine currentRoutine;

    [SerializeField] private FPSShoot shootManager;

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

                // Get DamagablePlayer from BodyCollision
                Transform bodyCollision = obj.transform.Find("BodyCollision");
                if (bodyCollision != null)
                {
                    damagablePlayer = bodyCollision.GetComponent<DamagablePlayer>();
                }
                else
                {
                    Debug.LogWarning("BodyCollision not found under local player!");
                }

                // Get the FPSShoot from local player
                shootManager = obj.GetComponent<FPSShoot>();
                if (shootManager == null)
                {
                    Debug.LogWarning("FPSShoot not found on local player!");
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
        ammoText.text = "Ammo: " + shootManager.GetCurrentAmmo();
    }

    public void ShowPickupMessage(string message)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowMessageRoutine(message));
    }

    private IEnumerator ShowMessageRoutine(string message)
    {
        pickupText.text = message;
        pickupText.gameObject.SetActive(true);

        yield return new WaitForSeconds(pickUpDisplayDuration);

        pickupText.gameObject.SetActive(false);
    }
}
