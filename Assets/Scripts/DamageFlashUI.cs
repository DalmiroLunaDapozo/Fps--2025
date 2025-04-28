using UnityEngine;
using UnityEngine.UI;

public class DamageFlashUI : MonoBehaviour
{
    public Image flashImage;
    public float flashDuration = 0.5f;
    public Color flashColor = new Color(1, 0, 0, 0.6f);

    private float timer;
    private bool isFlashing;

    private void Awake()
    {
        if (flashImage != null)
            flashImage.color = Color.clear;
    }

    private void Update()
    {
        if (isFlashing)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(flashColor.a, 0, timer / flashDuration);
            flashImage.color = new Color(flashColor.r, flashColor.g, flashColor.b, alpha);

            if (timer >= flashDuration)
            {
                flashImage.color = Color.clear;
                isFlashing = false;
            }
        }
    }

    public void TriggerFlash()
    {
        if (flashImage == null) return;

        flashImage.color = flashColor;
        timer = 0f;
        isFlashing = true;
    }
}
