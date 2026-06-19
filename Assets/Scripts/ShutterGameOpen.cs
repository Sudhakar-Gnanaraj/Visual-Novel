using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShutterGameOpen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _panelA;
    [SerializeField] private RectTransform _panelB;
    [SerializeField] private CanvasScaler _canvasScaler;

    [Header("Settings")]
    [SerializeField] private float shutterSpeed = 2f;
    [SerializeField] private float startDelay   = 0.2f;

    private void Start()
    {
        SetupVertical();
        SetPanelsClosed();
        StartCoroutine(PlayShutterOpen());
    }

    private float GetPanelHeight()
    {
        if (_canvasScaler != null &&
            _canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            return _canvasScaler.referenceResolution.y / 2f;

        return Screen.height / 2f;
    }

    private void SetupVertical()
    {
        float halfH = GetPanelHeight();

        _panelA.anchorMin        = new Vector2(0f, 1f);
        _panelA.anchorMax        = new Vector2(1f, 1f);
        _panelA.pivot            = new Vector2(0.5f, 1f);
        _panelA.sizeDelta        = new Vector2(0f, halfH);

        _panelB.anchorMin        = new Vector2(0f, 0f);
        _panelB.anchorMax        = new Vector2(1f, 0f);
        _panelB.pivot            = new Vector2(0.5f, 0f);
        _panelB.sizeDelta        = new Vector2(0f, halfH);
    }

    private void SetPanelsClosed()
    {
        _panelA.anchoredPosition = new Vector2(0f, 0f);
        _panelB.anchoredPosition = new Vector2(0f, 0f);
    }

    private IEnumerator PlayShutterOpen()
    {
        yield return new WaitForSeconds(startDelay);

        float halfH = GetPanelHeight();

        Vector2 aStart = new Vector2(0f,  0f);
        Vector2 aEnd   = new Vector2(0f,  halfH);
        Vector2 bStart = new Vector2(0f,  0f);
        Vector2 bEnd   = new Vector2(0f, -halfH);

        float progress = 0f;
        while (progress < 1f)
        {
            progress += shutterSpeed * Time.deltaTime;
            progress  = Mathf.Clamp01(progress);

            float eased = Mathf.SmoothStep(0f, 1f, progress);
            _panelA.anchoredPosition = Vector2.Lerp(aStart, aEnd, eased);
            _panelB.anchoredPosition = Vector2.Lerp(bStart, bEnd, eased);

            yield return null;
        }

        _panelA.anchoredPosition = aEnd;
        _panelB.anchoredPosition = bEnd;

        // Hide panels after opening
        _panelA.gameObject.SetActive(false);
        _panelB.gameObject.SetActive(false);
    }
}