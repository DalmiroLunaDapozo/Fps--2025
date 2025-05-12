using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public float normalFOV = 60f;
    public float zoomedFOV = 30f;
    public float zoomSpeed = 10f;

    private Camera cam;

    private void Awake() => cam = GetComponent<Camera>();

    public void SetZoom(bool zoomed)
    {
        StopAllCoroutines();
        StartCoroutine(Zoom(zoomed ? zoomedFOV : normalFOV));
    }

    private IEnumerator Zoom(float targetFOV)
    {
        while (Mathf.Abs(cam.fieldOfView - targetFOV) > 0.1f)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * zoomSpeed);
            yield return null;
        }

        cam.fieldOfView = targetFOV;
    }
}
