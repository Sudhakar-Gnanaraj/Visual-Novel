using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ShutterClose : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _panelA;
    [SerializeField] private RectTransform _panelB;
    [SerializeField] private Canvas _shutterCanvas;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Transform _frameTransform;
    [SerializeField] private SpriteRenderer _frameSpriteRenderer;

    [Header("Shutter Settings")]
    [SerializeField] private float shutterSpeed = 2f;

    [Header("Post Shutter Zoom Settings")]
    [SerializeField] private float zoomTargetSize = 0.1f;
    [SerializeField] private float zoomSpeed      = 5f;

    private bool _isAnimating = false;

    private void Start()
    {
        _panelA.gameObject.SetActive(false);
        _panelB.gameObject.SetActive(false);
    }

    private void SetupCanvas()
    {
        Bounds bounds = _frameSpriteRenderer.bounds;
        _shutterCanvas.transform.position = new Vector3(
            bounds.center.x,
            bounds.center.y,
            bounds.center.z - 0.1f
        );

        RectTransform canvasRT = _shutterCanvas.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(bounds.size.x, bounds.size.y);
        _shutterCanvas.transform.localScale = Vector3.one;
    }

    private void SetupVertical(Bounds bounds)
    {
        float halfH = bounds.size.y / 2f;

        _panelA.anchorMin        = new Vector2(0f, 0.5f);
        _panelA.anchorMax        = new Vector2(1f, 0.5f);
        _panelA.pivot            = new Vector2(0.5f, 0f);
        _panelA.sizeDelta        = new Vector2(0f, halfH);
        _panelA.anchoredPosition = new Vector2(0f, 0f);

        _panelB.anchorMin        = new Vector2(0f, 0.5f);
        _panelB.anchorMax        = new Vector2(1f, 0.5f);
        _panelB.pivot            = new Vector2(0.5f, 1f);
        _panelB.sizeDelta        = new Vector2(0f, halfH);
        _panelB.anchoredPosition = new Vector2(0f, 0f);
    }

    private void SetupHorizontal(Bounds bounds)
    {
        float halfW = bounds.size.x / 2f;

        _panelA.anchorMin        = new Vector2(0.5f, 0f);
        _panelA.anchorMax        = new Vector2(0.5f, 1f);
        _panelA.pivot            = new Vector2(1f, 0.5f);
        _panelA.sizeDelta        = new Vector2(halfW, 0f);
        _panelA.anchoredPosition = new Vector2(0f, 0f);

        _panelB.anchorMin        = new Vector2(0.5f, 0f);
        _panelB.anchorMax        = new Vector2(0.5f, 1f);
        _panelB.pivot            = new Vector2(0f, 0.5f);
        _panelB.sizeDelta        = new Vector2(halfW, 0f);
        _panelB.anchoredPosition = new Vector2(0f, 0f);
    }

    private GameObject CreateBlackCover()
    {
        Bounds bounds = _frameSpriteRenderer.bounds;

        GameObject cover       = new GameObject("BlackCover");
        cover.transform.position = new Vector3(
            bounds.center.x,
            bounds.center.y,
            _frameSpriteRenderer.transform.position.z - 0.2f
        );

        SpriteRenderer sr      = cover.AddComponent<SpriteRenderer>();
        sr.color               = Color.black;
        sr.sortingLayerName    = "AboveUI";
        sr.sortingOrder        = 99;

        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();

        sr.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f),
            1f
        );

        // Fixed size matching frame bounds — never changes
        cover.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, 1f);

        return cover;
    }

    public IEnumerator PlayAndLoad(string sceneName, Camera frameCamera, float frameCameraStartOrtho)
    {
        if (_isAnimating) yield break;
        _isAnimating = true;

        bool isPortrait = PhotoDataStore.Instance != null && PhotoDataStore.Instance.IsPortrait;

        Bounds bounds = _frameSpriteRenderer.bounds;
        SetupCanvas();

        Vector2 aStart, aEnd, bStart, bEnd;

        if (isPortrait)
        {
            SetupHorizontal(bounds);
            float halfW = bounds.size.x / 2f;
            aStart = new Vector2(-halfW, 0f);
            aEnd   = new Vector2( 0f,   0f);
            bStart = new Vector2( halfW, 0f);
            bEnd   = new Vector2( 0f,   0f);
        }
        else
        {
            SetupVertical(bounds);
            float halfH = bounds.size.y / 2f;
            aStart = new Vector2(0f,  halfH);
            aEnd   = new Vector2(0f,  0f);
            bStart = new Vector2(0f, -halfH);
            bEnd   = new Vector2(0f,  0f);
        }

        _panelA.gameObject.SetActive(true);
        _panelB.gameObject.SetActive(true);

        _panelA.anchoredPosition = aStart;
        _panelB.anchoredPosition = bStart;

        // Animate shutter closing
        float progress = 0f;
        while (progress < 1f)
        {
            progress += shutterSpeed * Time.deltaTime;
            progress  = Mathf.Clamp01(progress);

            float eased = Mathf.SmoothStep(0f, 1f, progress);
            _panelA.anchoredPosition = Vector2.Lerp(aStart, aEnd, eased);
            _panelB.anchoredPosition = Vector2.Lerp(bStart, bEnd, eased);

            _shutterCanvas.transform.position = new Vector3(
                _frameTransform.position.x,
                _frameTransform.position.y,
                _frameTransform.position.z - 0.1f
            );

            yield return null;
        }

        _panelA.anchoredPosition = aEnd;
        _panelB.anchoredPosition = bEnd;

        // Create black cover — fixed at frame position and size
        GameObject blackCover = CreateBlackCover();

        // Store frame center for zoom target
        Vector3 frameCenterWorld = new Vector3(
            _frameTransform.position.x,
            _frameTransform.position.y,
            _mainCamera.transform.position.z
        );

        // Fast zoom main camera into frame center
        float zoomProgress = 0f;
        float startOrtho   = _mainCamera.orthographicSize;
        Vector3 startPos   = _mainCamera.transform.position;

        while (zoomProgress < 1f)
        {
            zoomProgress += zoomSpeed * Time.deltaTime;
            zoomProgress  = Mathf.Clamp01(zoomProgress);

            float eased = Mathf.SmoothStep(0f, 1f, zoomProgress);

            _mainCamera.orthographicSize   = Mathf.Lerp(startOrtho,  zoomTargetSize,   eased);
            _mainCamera.transform.position = Vector3.Lerp(startPos, frameCenterWorld, eased);

            // Black cover stays completely fixed — no position or scale updates
            yield return null;
        }

        _mainCamera.orthographicSize   = zoomTargetSize;
        _mainCamera.transform.position = frameCenterWorld;

        //Destroy(blackCover);
        SceneManager.LoadScene(sceneName);
    }
}