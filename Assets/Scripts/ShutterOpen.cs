using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ShutterOpen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _panelA;
    [SerializeField] private RectTransform _panelB;
    [SerializeField] private Camera _diaryCamera;

    [Header("Shutter Settings")]
    [SerializeField] private float shutterSpeed = 2f;
    [SerializeField] private float shutterDelay = 0.5f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomEndSize = 5f;
    [SerializeField] private float zoomSpeed   = 1f;
    [SerializeField] private float zoomDelay   = 0.3f;

    private float _screenHeight;
    private float _screenWidth;

    private void Start()
    {
        _screenHeight = Screen.height;
        _screenWidth  = Screen.width;

        if (_diaryCamera == null)
            _diaryCamera = Camera.main;

        bool isPortrait = PhotoDataStore.Instance != null && PhotoDataStore.Instance.IsPortrait;

        Vector2 photoPos = PhotoDataStore.Instance != null
            ? PhotoDataStore.Instance.PhotoPosition
            : Vector2.zero;

        _diaryCamera.transform.position = new Vector3(
            photoPos.x,
            photoPos.y,
            _diaryCamera.transform.position.z
        );

        _diaryCamera.orthographicSize = isPortrait ? 1.5f : 1f;

        SetPanelsClosed(isPortrait);

        StartCoroutine(PlayShutterOpen(isPortrait));
    }

    private void SetPanelsClosed(bool isPortrait)
    {
        // Portrait = left/right closed, Landscape = top/bottom closed
        if (isPortrait)
        {
            _panelA.anchorMin        = new Vector2(0f, 0f);
            _panelA.anchorMax        = new Vector2(0f, 1f);
            _panelA.pivot            = new Vector2(0f, 0.5f);
            _panelA.sizeDelta        = new Vector2(_screenWidth / 2f, 0f);
            _panelA.anchoredPosition = new Vector2(0f, 0f);

            _panelB.anchorMin        = new Vector2(1f, 0f);
            _panelB.anchorMax        = new Vector2(1f, 1f);
            _panelB.pivot            = new Vector2(1f, 0.5f);
            _panelB.sizeDelta        = new Vector2(_screenWidth / 2f, 0f);
            _panelB.anchoredPosition = new Vector2(0f, 0f);
        }
        else
        {
            _panelA.anchorMin        = new Vector2(0f, 1f);
            _panelA.anchorMax        = new Vector2(1f, 1f);
            _panelA.pivot            = new Vector2(0.5f, 1f);
            _panelA.sizeDelta        = new Vector2(0f, _screenHeight / 2f);
            _panelA.anchoredPosition = new Vector2(0f, 0f);

            _panelB.anchorMin        = new Vector2(0f, 0f);
            _panelB.anchorMax        = new Vector2(1f, 0f);
            _panelB.pivot            = new Vector2(0.5f, 0f);
            _panelB.sizeDelta        = new Vector2(0f, _screenHeight / 2f);
            _panelB.anchoredPosition = new Vector2(0f, 0f);
        }
    }

    private IEnumerator PlayShutterOpen(bool isPortrait)
    {
        yield return new WaitForSeconds(shutterDelay);

        float progress = 0f;

        Vector2 aStart, aEnd, bStart, bEnd;

        // Portrait opens left/right, landscape opens top/bottom
        if (isPortrait)
        {
            aStart = new Vector2( 0f,              0f);
            aEnd   = new Vector2(-_screenWidth / 2f, 0f);
            bStart = new Vector2( 0f,              0f);
            bEnd   = new Vector2( _screenWidth / 2f, 0f);
        }
        else
        {
            aStart = new Vector2(0f, 0f);
            aEnd   = new Vector2(0f,  _screenHeight / 2f);
            bStart = new Vector2(0f, 0f);
            bEnd   = new Vector2(0f, -_screenHeight / 2f);
        }

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

        yield return new WaitForSeconds(zoomDelay);

        float zoomProgress = 0f;
        float startSize    = _diaryCamera.orthographicSize;
        Vector3 startPos   = _diaryCamera.transform.position;
        Vector3 endPos     = new Vector3(0f, 0f, _diaryCamera.transform.position.z);

        while (zoomProgress < 1f)
        {
            zoomProgress += zoomSpeed * Time.deltaTime;
            zoomProgress  = Mathf.Clamp01(zoomProgress);

            float eased = Mathf.SmoothStep(0f, 1f, zoomProgress);

            _diaryCamera.orthographicSize   = Mathf.Lerp(startSize, zoomEndSize, eased);
            _diaryCamera.transform.position = Vector3.Lerp(startPos, endPos,     eased);

            yield return null;
        }

        _diaryCamera.orthographicSize   = zoomEndSize;
        _diaryCamera.transform.position = endPos;
    }
}