using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class DeathScreenUI : MonoBehaviour
{
    public GameObject deathScreenPanel;
    public TextMeshProUGUI respawnText;

    private Coroutine countdownCoroutine;

    private void Awake()
    {
        HideDeathScreen(); // Ensure it's hidden initially
    }

    public void ShowDeathScreen(float respawnTime)
    {
        deathScreenPanel.SetActive(true);

        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(Countdown(respawnTime));
    }

    private IEnumerator Countdown(float time)
    {
        float currentTime = time;
        while (currentTime > 0f)
        {
            respawnText.text = $"Apareciendo en {Mathf.CeilToInt(currentTime)}...";
            yield return new WaitForSeconds(1f);
            currentTime -= 1f;
        }

        HideDeathScreen();
    }

    public void HideDeathScreen()
    {
        deathScreenPanel.SetActive(false);

        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine);
            countdownCoroutine = null;
        }
    }
}
