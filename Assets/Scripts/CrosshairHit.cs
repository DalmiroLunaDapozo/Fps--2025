using UnityEngine;
using UnityEngine.UI;

public class CrosshairHit : MonoBehaviour
{
    public Image hitImage;
    public float displayTime = 0.2f;

    private float timer;

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                hitImage.enabled = false;
            }
        }
    }

    public void ShowHit()
    {
        hitImage.enabled = true;
        timer = displayTime;
    }
}
