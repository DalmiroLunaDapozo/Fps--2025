using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class KillScreenUI : MonoBehaviour
{
    [SerializeField] private GameObject killScreenPanel;
    [SerializeField] private TextMeshProUGUI killText;
    [SerializeField] private float displayTime = 3f;

    private void Awake()
    {
        killScreenPanel.SetActive(false);
    }

    public void ShowKillScreen(string killedPlayerName)
    {
        killScreenPanel.SetActive(true);
        killText.text = $"You killed {killedPlayerName}!";
        Invoke(nameof(HideKillScreen), displayTime);
    }

    private void HideKillScreen()
    {
        killScreenPanel.SetActive(false);
    }
}
