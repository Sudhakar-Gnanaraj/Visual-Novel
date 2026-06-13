using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

[System.Serializable]
public class PhotoOpportunity
{
    public string opportunityName;
    public string sceneToLoad;

    [Header("Position Bounds (World Space)")]
    public Vector2 minPosition;
    public Vector2 maxPosition;

    [Header("Zoom Range")]
    [Range(0.25f, 1f)] public float minZoom;
    [Range(0.25f, 1f)] public float maxZoom;

    [Header("Proximity Margin")]
    public float positionMargin = 0.5f;
    [Range(0f, 0.2f)] public float zoomMargin = 0.1f;

    [Header("Photo Scene Layout")]
    public Sprite textCardSprite;
    public Vector2 photoPosition;
    public Vector2 textCardPosition;

    public bool IsMatch(Vector2 position, float zoom)
    {
        float xMin = Mathf.Min(minPosition.x, maxPosition.x);
        float xMax = Mathf.Max(minPosition.x, maxPosition.x);
        float yMin = Mathf.Min(minPosition.y, maxPosition.y);
        float yMax = Mathf.Max(minPosition.y, maxPosition.y);

        bool positionMatch = position.x >= xMin && position.x <= xMax &&
                             position.y >= yMin && position.y <= yMax;
        bool zoomMatch = zoom >= minZoom - 0.01f && zoom <= maxZoom + 0.01f;
        return positionMatch && zoomMatch;
    }

    public bool IsNear(Vector2 position, float zoom)
    {
        float xMin = Mathf.Min(minPosition.x, maxPosition.x);
        float xMax = Mathf.Max(minPosition.x, maxPosition.x);
        float yMin = Mathf.Min(minPosition.y, maxPosition.y);
        float yMax = Mathf.Max(minPosition.y, maxPosition.y);

        bool positionNear = position.x >= xMin - positionMargin &&
                            position.x <= xMax + positionMargin &&
                            position.y >= yMin - positionMargin &&
                            position.y <= yMax + positionMargin;
        bool zoomNear = zoom >= minZoom - zoomMargin && zoom <= maxZoom + zoomMargin;
        return positionNear && zoomNear;
    }
}

public class CameraFrame : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer _frameSpriteRenderer;
    [SerializeField] private SpriteRenderer _cameraSpriteRenderer;
    [SerializeField] private Camera _mainCamera;
    [SerializeField] private Camera _frameCamera;
    [SerializeField] private RawImage _frameDisplay;
    [SerializeField] private Image _frameOverlay;      // UI Image on Sort Order 1 canvas

    [Header("Movement Settings")]
    [SerializeField] private float followSpeed = 15f;
    [SerializeField] private bool smoothFollow = true;

    [Header("Slide Settings")]
    [SerializeField] private float slideSpeed = 0.5f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 0.3f;
    [SerializeField] private float zoomSmoothSpeed = 8f;

    [Header("Photo Opportunities")]
    [SerializeField] private PhotoOpportunity[] photoOpportunities;

    [Header("Feedback")]
    [SerializeField] private float shutterFreezeTime = 0.3f;
    [SerializeField] private Color defaultColor  = Color.white;
    [SerializeField] private Color nearZoneColor = Color.yellow;
    [SerializeField] private Color inZoneColor   = Color.green;

    // State
    private enum FrameState { Hidden, SlidingIn, Active, SlidingOut }
    private FrameState _state = FrameState.Hidden;

    private Vector2 _currentPosition;
    private Vector3 _originalScale;

    // Slide
    private float _slideProgress = 0f;
    private float _slideStartX;
    private float _slideCenterX = 0f;
    private const float SlideOffscreenPadding = 1f;

    // Zoom
    private float _defaultFrameOrthoSize;
    private float _mainCameraOrthoSize;
    private float _targetFrameOrthoSize;
    private float _currentZoom = 1f;
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 1f;

    private bool _isFrozen                = false;
    private bool _justActivatedViewfinder = false;
    private RenderTexture _frameRenderTexture;

    private void Start()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        _originalScale       = transform.localScale;
        _mainCameraOrthoSize = _mainCamera.orthographicSize;

        Bounds bounds          = _frameSpriteRenderer.bounds;
        _defaultFrameOrthoSize = bounds.size.y / 2f;
        _targetFrameOrthoSize  = _defaultFrameOrthoSize;
        _frameCamera.orthographicSize = _defaultFrameOrthoSize;

        // Hide everything at start
        _frameSpriteRenderer.enabled  = false;
        _cameraSpriteRenderer.enabled = false;
        _frameCamera.enabled          = false;
        _frameDisplay.enabled         = false;
        _frameOverlay.enabled         = false;

        _frameSpriteRenderer.enabled  = false;
        _cameraSpriteRenderer.enabled = false;

        PositionOffscreenLeft();
    }

    private void Update()
    {
        if (Mouse.current == null || _isFrozen) return;

        HandleInput();

        if (_state != FrameState.Hidden)
        {
            float scroll = Mouse.current.scroll.ReadValue().y;
            if (scroll == 0f)
                scroll = Input.GetAxis("Mouse ScrollWheel") * 10f;

            if (scroll != 0f)
            {
                _targetFrameOrthoSize -= scroll * zoomSpeed;
                _targetFrameOrthoSize  = Mathf.Clamp(
                    _targetFrameOrthoSize,
                    _defaultFrameOrthoSize * MinZoom,
                    _defaultFrameOrthoSize
                );
            }

            _frameCamera.orthographicSize = Mathf.Lerp(
                _frameCamera.orthographicSize,
                _targetFrameOrthoSize,
                zoomSmoothSpeed * Time.deltaTime
            );

            _currentZoom = _frameCamera.orthographicSize / _defaultFrameOrthoSize;
        }

        switch (_state)
        {
            case FrameState.SlidingIn:  UpdateSlideIn();  break;
            case FrameState.Active:     UpdateActive();   break;
            case FrameState.SlidingOut: UpdateSlideOut(); break;
        }

        if (_state != FrameState.Hidden)
        {
            UpdateFrameCameraPosition();
            UpdateFrameCameraViewport();
        }
    }

    // ── Input ────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        _justActivatedViewfinder = false;

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            (_state == FrameState.Hidden || _state == FrameState.SlidingOut))
        {
            StartSlideIn();
            _justActivatedViewfinder = true;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame &&
            (_state == FrameState.Active || _state == FrameState.SlidingIn))
        {
            StartSlideOut();
        }

        if (_state == FrameState.Active &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            !_justActivatedViewfinder)
        {
            TryTakePhoto();
        }
    }

    // ── Slide ────────────────────────────────────────────────────────────────

    private void PositionOffscreenLeft()
    {
        float camHeight  = _mainCameraOrthoSize;
        float camWidth   = camHeight * ((float)Screen.width / Screen.height);
        float frameHalfW = _frameSpriteRenderer.bounds.size.x / 2f;
        _slideStartX     = -camWidth - frameHalfW - SlideOffscreenPadding;
        transform.position = new Vector3(_slideStartX, 0f, transform.position.z);
    }

    private void StartSlideIn()
    {
        _state         = FrameState.SlidingIn;
        _slideProgress = 0f;

        //_frameSpriteRenderer.enabled  = true;
        _cameraSpriteRenderer.enabled = true;
        _frameCamera.enabled          = true;
        _frameDisplay.enabled         = true;
        _frameOverlay.enabled         = true;

        if (_frameRenderTexture != null)
        {
            _frameCamera.targetTexture = null;
            _frameRenderTexture.Release();
            Destroy(_frameRenderTexture);
            _frameRenderTexture = null;
        }

        _frameCamera.orthographicSize = _defaultFrameOrthoSize;
        _targetFrameOrthoSize         = _defaultFrameOrthoSize;
        _currentZoom                  = 1f;

        PositionOffscreenLeft();
    }

    private void StartSlideOut()
    {
        _state         = FrameState.SlidingOut;
        _slideProgress = 1f - _slideProgress;
    }

    private void UpdateSlideIn()
    {
        _slideProgress += slideSpeed * Time.deltaTime;
        _slideProgress  = Mathf.Clamp01(_slideProgress);

        float currentX = Mathf.Lerp(_slideStartX, _slideCenterX, _slideProgress);
        transform.position = new Vector3(currentX, transform.position.y, transform.position.z);

        if (_slideProgress >= 1f)
        {
            transform.position = new Vector3(_slideCenterX, transform.position.y, transform.position.z);
            _state = FrameState.Active;
        }
    }

    private void UpdateSlideOut()
    {
        _slideProgress -= slideSpeed * Time.deltaTime;
        _slideProgress  = Mathf.Clamp01(_slideProgress);

        float currentX = Mathf.Lerp(_slideStartX, _slideCenterX, _slideProgress);
        transform.position = new Vector3(currentX, transform.position.y, transform.position.z);

        if (_slideProgress <= 0f)
        {
            transform.position            = new Vector3(_slideStartX, transform.position.y, transform.position.z);
            //_frameSpriteRenderer.enabled  = false;
            _cameraSpriteRenderer.enabled = false;
            _frameCamera.enabled          = false;
            _frameDisplay.enabled         = false;
            _frameOverlay.enabled         = false;
            _state = FrameState.Hidden;
        }
    }

    // ── Active ───────────────────────────────────────────────────────────────

    private void UpdateActive()
    {
        Vector2 mousePos       = Mouse.current.position.ReadValue();
        Vector3 targetPosition = GetClampedPosition(mousePos);

        if (smoothFollow)
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        else
            transform.position = targetPosition;

        _currentPosition = transform.position;

        UpdateInZoneIndicator();
    }

    // ── Camera ───────────────────────────────────────────────────────────────

    private void UpdateFrameCameraPosition()
    {
        _frameCamera.transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            _frameCamera.transform.position.z
        );
    }

    private void UpdateFrameCameraViewport()
    {
        Bounds bounds = _frameSpriteRenderer.bounds;

        Vector2 screenMin = _mainCamera.WorldToScreenPoint(bounds.min);
        Vector2 screenMax = _mainCamera.WorldToScreenPoint(bounds.max);

        float width  = screenMax.x - screenMin.x;
        float height = screenMax.y - screenMin.y;

        // Position RawImage to match frame sprite bounds on screen
        RectTransform rt    = _frameDisplay.rectTransform;
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.zero;
        rt.anchoredPosition = new Vector2(screenMin.x, screenMin.y);
        rt.sizeDelta        = new Vector2(width, height);

        // Position frame overlay Image to exactly match RawImage — stays fixed, no zoom
        RectTransform overlayRt    = _frameOverlay.rectTransform;
        overlayRt.pivot            = new Vector2(0f, 0f);
        overlayRt.anchorMin        = Vector2.zero;
        overlayRt.anchorMax        = Vector2.zero;
        overlayRt.anchoredPosition = new Vector2(screenMin.x, screenMin.y);
        overlayRt.sizeDelta        = new Vector2(width, height);

        // Update zone indicator color on overlay instead of sprite
        _frameOverlay.color = _frameSpriteRenderer.color;

        // Create render texture once at frame pixel size
        if (_frameRenderTexture == null)
        {
            int texW = Mathf.Max(1, Mathf.RoundToInt(width));
            int texH = Mathf.Max(1, Mathf.RoundToInt(height));

            _frameRenderTexture        = new RenderTexture(texW, texH, 24);
            _frameCamera.targetTexture = _frameRenderTexture;
            _frameDisplay.texture      = _frameRenderTexture;
        }

        _frameDisplay.uvRect = new Rect(0f, 0f, 1f, 1f);
    }

    private Vector3 GetClampedPosition(Vector2 mousePos)
    {
        float camHeight = _mainCameraOrthoSize;
        float camWidth  = camHeight * ((float)Screen.width / Screen.height);

        Vector3 worldPos = _mainCamera.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, 0f)
        );
        worldPos.z = transform.position.z;

        float halfW = _frameSpriteRenderer.bounds.size.x / 2f;
        float halfH = _frameSpriteRenderer.bounds.size.y / 2f;

        worldPos.x = Mathf.Clamp(worldPos.x, -camWidth  + halfW,  camWidth  - halfW);
        worldPos.y = Mathf.Clamp(worldPos.y, -camHeight + halfH, camHeight - halfH);

        return worldPos;
    }

    // ── Photo ────────────────────────────────────────────────────────────────

    private void TryTakePhoto()
    {
        foreach (PhotoOpportunity opportunity in photoOpportunities)
        {
            if (opportunity.IsMatch(_currentPosition, _currentZoom))
            {
                StartCoroutine(CaptureAndLoad(opportunity));
                return;
            }
        }
        Debug.Log("No match found.");
    }

    private IEnumerator CaptureAndLoad(PhotoOpportunity opportunity)
    {
        PhotoDataStore.Instance.LastOpportunityName = opportunity.opportunityName;
        PhotoDataStore.Instance.TextCardSprite      = opportunity.textCardSprite;
        PhotoDataStore.Instance.PhotoPosition       = opportunity.photoPosition;
        PhotoDataStore.Instance.TextCardPosition    = opportunity.textCardPosition;

        _isFrozen                     = true;
        //_frameSpriteRenderer.enabled  = false;
        _cameraSpriteRenderer.enabled = false;
        _frameOverlay.enabled         = false;

        yield return new WaitForEndOfFrame();

        Texture2D screenshot = CaptureScreenshot();
        PhotoDataStore.Instance.LastPhoto = screenshot;

        //_frameSpriteRenderer.enabled  = true;
        _cameraSpriteRenderer.enabled = true;
        _frameOverlay.enabled         = true;

        yield return new WaitForSeconds(shutterFreezeTime);

        SceneManager.LoadScene(opportunity.sceneToLoad);
    }

    private Texture2D CaptureScreenshot()
    {
        Bounds bounds     = _frameSpriteRenderer.bounds;
        Vector2 screenMin = _mainCamera.WorldToScreenPoint(bounds.min);
        Vector2 screenMax = _mainCamera.WorldToScreenPoint(bounds.max);

        int width  = Mathf.RoundToInt(screenMax.x - screenMin.x);
        int height = Mathf.RoundToInt(screenMax.y - screenMin.y);

        RenderTexture rt           = new RenderTexture(width, height, 24);
        _frameCamera.targetTexture = rt;
        _frameCamera.Render();
        RenderTexture.active = rt;

        Texture2D photo = new Texture2D(width, height, TextureFormat.RGB24, false);
        photo.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        photo.Apply();

        _frameCamera.targetTexture = _frameRenderTexture;
        RenderTexture.active       = null;
        Destroy(rt);

        return photo;
    }

    // ── Indicator ────────────────────────────────────────────────────────────

    private void UpdateInZoneIndicator()
    {
        if (_frameSpriteRenderer == null) return;

        bool inZone   = false;
        bool nearZone = false;

        foreach (PhotoOpportunity opportunity in photoOpportunities)
        {
            if (opportunity.IsMatch(_currentPosition, _currentZoom))
            {
                inZone = true;
                break;
            }
            else if (opportunity.IsNear(_currentPosition, _currentZoom))
                nearZone = true;
        }

        Color targetColor = inZone ? inZoneColor : (nearZone ? nearZoneColor : defaultColor);

        // Apply color to both sprite and overlay
        _frameSpriteRenderer.color = Color.Lerp(
            _frameSpriteRenderer.color, targetColor, 10f * Time.deltaTime
        );
    }
}