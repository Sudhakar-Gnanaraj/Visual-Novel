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

    [Header("Orientation")]
    public bool requirePortrait = false;

    [Header("Stats")]
    public int bodyContribution;
    public int mindContribution;
    public int charismaContribution;

    [Header("Photo Scene Layout")]
    public Sprite textCardSprite;
    public Vector2 photoPosition;
    public Vector2 textCardPosition;

    public bool IsMatch(Vector2 position, float zoom, bool isPortrait)
    {
        float xMin = Mathf.Min(minPosition.x, maxPosition.x);
        float xMax = Mathf.Max(minPosition.x, maxPosition.x);
        float yMin = Mathf.Min(minPosition.y, maxPosition.y);
        float yMax = Mathf.Max(minPosition.y, maxPosition.y);

        bool positionMatch    = position.x >= xMin && position.x <= xMax &&
                                position.y >= yMin && position.y <= yMax;
        bool zoomMatch        = zoom >= minZoom - 0.01f && zoom <= maxZoom + 0.01f;
        bool orientationMatch = isPortrait == requirePortrait;

        return positionMatch && zoomMatch && orientationMatch;
    }

    public bool IsNear(Vector2 position, float zoom, bool isPortrait)
    {
        float xMin = Mathf.Min(minPosition.x, maxPosition.x);
        float xMax = Mathf.Max(minPosition.x, maxPosition.x);
        float yMin = Mathf.Min(minPosition.y, maxPosition.y);
        float yMax = Mathf.Max(minPosition.y, maxPosition.y);

        bool positionNear     = position.x >= xMin - positionMargin &&
                                position.x <= xMax + positionMargin &&
                                position.y >= yMin - positionMargin &&
                                position.y <= yMax + positionMargin;
        bool zoomNear         = zoom >= minZoom - zoomMargin && zoom <= maxZoom + zoomMargin;
        bool orientationMatch = isPortrait == requirePortrait;

        return positionNear && zoomNear && orientationMatch;
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
    [SerializeField] private Image _landscapeOverlay;
    [SerializeField] private Image _portraitOverlay;

    [Header("Movement Settings")]
    [SerializeField] private float followSpeed = 15f;
    [SerializeField] private bool smoothFollow = true;

    [Header("Slide Settings")]
    [SerializeField] private float slideSpeed = 0.5f;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 0.3f;
    [SerializeField] private float zoomSmoothSpeed = 8f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float rotationZoomResetSpeed = 0.5f; 

    [Header("Photo Opportunities")]
    [SerializeField] private PhotoOpportunity[] photoOpportunities;

    [Header("Feedback")]
    [SerializeField] private float shutterFreezeTime = 0.3f;
    [SerializeField] private Color defaultColor  = Color.white;
    [SerializeField] private Color nearZoneColor = Color.yellow;
    [SerializeField] private Color inZoneColor   = Color.green;

    // Frame state
    private enum FrameState { Hidden, SlidingIn, Active, SlidingOut }
    private FrameState _state = FrameState.Hidden;

    // Orientation
    private enum Orientation { Landscape, Portrait }
    private Orientation _orientation = Orientation.Landscape;

    // Rotation state machine
    private enum RotationState { Idle, ResettingZoom, Rotating }
    private RotationState _rotationState = RotationState.Idle;
    private bool _isRotating = false;

    private float _currentRotation = 0f;
    private float _targetRotation  = 0f;
    private const float LandscapeRotation = 0f;
    private const float PortraitRotation  = 90f;

    private Vector2 _currentPosition;
    private Vector3 _originalScale;

    // Slide
    private float _slideProgress = 0f;
    private float _slideStartX;
    private float _slideCenterX  = 0f;
    private const float SlideOffscreenPadding = 3f;

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
    private float _frameBoundsWidth;
    private float _frameBoundsHeight;
    private Orientation _targetOrientation; 
    public bool IsFrozenForTransition = false;

    private void Start()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main;

        _originalScale       = transform.localScale;
        _mainCameraOrthoSize = _mainCamera.orthographicSize;

        Bounds bounds      = _frameSpriteRenderer.bounds;
        _frameBoundsWidth  = bounds.size.x;
        _frameBoundsHeight = bounds.size.y;

        _defaultFrameOrthoSize        = _frameBoundsHeight / 2f;
        _targetFrameOrthoSize         = _defaultFrameOrthoSize;
        _frameCamera.orthographicSize = _defaultFrameOrthoSize;

        _currentRotation = LandscapeRotation;
        _targetRotation  = LandscapeRotation;
        ApplyRotation(LandscapeRotation);

        _frameSpriteRenderer.enabled  = false;
        _cameraSpriteRenderer.enabled = false;
        _frameCamera.enabled          = false;
        _frameDisplay.enabled         = false;
        _landscapeOverlay.enabled     = false;
        _portraitOverlay.enabled      = false;

        PositionOffscreenRight();
    }

    private void Update()
    {
        if (Mouse.current == null || _isFrozen) return;

        HandleInput();

        // Scroll zoom — only when visible, not rotating
        if (_state != FrameState.Hidden && !_isRotating)
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

        if (_isRotating)
            UpdateRotation();

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

        if (_state != FrameState.Hidden)
        {
            UpdateFrameCameraPosition();
            if (!IsFrozenForTransition)
                UpdateFrameCameraViewport();
        }
    }

    // ── Input ────────────────────────────────────────────────────────────────

    private void HandleInput()
    {
        _justActivatedViewfinder = false;

        bool canInteract = !_isRotating &&
                           _state != FrameState.SlidingIn &&
                           _state != FrameState.SlidingOut;

        if (Mouse.current.leftButton.wasPressedThisFrame &&
            _state == FrameState.Hidden && !_isRotating)
        {
            StartSlideIn();
            _justActivatedViewfinder = true;
        }

        if (Mouse.current.rightButton.wasPressedThisFrame &&
            _state == FrameState.Active && !_isRotating)
        {
            StartSlideOut();
        }

        if (Mouse.current.middleButton.wasPressedThisFrame && canInteract &&
            _state == FrameState.Active)
        {
            StartRotation();
        }

        if (_state == FrameState.Active && canInteract &&
            Mouse.current.leftButton.wasPressedThisFrame &&
            !_justActivatedViewfinder)
        {
            TryTakePhoto();
        }
    }

    // ── Rotation ─────────────────────────────────────────────────────────────

    private void StartRotation()
    {
        _isRotating    = true;
        _rotationState = RotationState.ResettingZoom;

        // Target zoom reset
        _targetFrameOrthoSize = _defaultFrameOrthoSize;

        // Reset colors
        _frameSpriteRenderer.color = defaultColor;
        _landscapeOverlay.color    = defaultColor;
        _portraitOverlay.color     = defaultColor;

        // Store target orientation but DON'T apply it yet
        if (_orientation == Orientation.Landscape)
        {
            _targetOrientation = Orientation.Portrait;
            _targetRotation    = PortraitRotation;
        }
        else
        {
            _targetOrientation = Orientation.Landscape;
            _targetRotation    = LandscapeRotation;
        }
        // _orientation stays unchanged until zoom reset is complete
    }

    private void UpdateRotation()
    {
        switch (_rotationState)
        {
            case RotationState.ResettingZoom:
                _frameCamera.orthographicSize = Mathf.MoveTowards(
                    _frameCamera.orthographicSize,
                    _defaultFrameOrthoSize,
                    rotationZoomResetSpeed * Time.deltaTime
                );
                _currentZoom = _frameCamera.orthographicSize / _defaultFrameOrthoSize;

                if (Mathf.Approximately(_frameCamera.orthographicSize, _defaultFrameOrthoSize))
                {
                    _frameCamera.orthographicSize = _defaultFrameOrthoSize;
                    _currentZoom = 1f;

                    _orientation = _targetOrientation;

                    _landscapeOverlay.enabled = false;
                    _portraitOverlay.enabled  = false;
                    _frameDisplay.enabled     = false;

                    _rotationState = RotationState.Rotating;
                }
                break;

            case RotationState.Rotating:
                _currentRotation = Mathf.MoveTowards(
                    _currentRotation,
                    _targetRotation,
                    rotationSpeed * 60f * Time.deltaTime
                );

                ApplyRotation(_currentRotation);

                if (Mathf.Approximately(_currentRotation, _targetRotation))
                {
                    _currentRotation = _targetRotation;
                    ApplyRotation(_currentRotation);

                    UpdateFrameCameraOrthoSize();

                    if (_frameRenderTexture != null)
                    {
                        _frameCamera.targetTexture = null;
                        _frameRenderTexture.Release();
                        Destroy(_frameRenderTexture);
                        _frameRenderTexture = null;
                    }

                    _frameDisplay.enabled = true;

                    if (_orientation == Orientation.Portrait)
                    {
                        _portraitOverlay.enabled  = true;
                        _landscapeOverlay.enabled = false;
                    }
                    else
                    {
                        _landscapeOverlay.enabled = true;
                        _portraitOverlay.enabled  = false;
                    }

                    _rotationState = RotationState.Idle;
                    _isRotating    = false;
                }
                break;
        }
    }

    private void ApplyRotation(float degrees)
    {
        transform.rotation = Quaternion.Euler(0f, 0f, degrees);
        // Frame camera never rotates — RawImage content always stays upright
    }

    private void UpdateFrameCameraOrthoSize()
    {
        if (_orientation == Orientation.Portrait)
        {
            _defaultFrameOrthoSize        = _frameBoundsWidth / 2f;
            _targetFrameOrthoSize         = _defaultFrameOrthoSize;
            _frameCamera.orthographicSize = _defaultFrameOrthoSize;
        }
        else
        {
            _defaultFrameOrthoSize        = _frameBoundsHeight / 2f;
            _targetFrameOrthoSize         = _defaultFrameOrthoSize;
            _frameCamera.orthographicSize = _defaultFrameOrthoSize;
        }

        _currentZoom = 1f;
    }

    // ── Slide ────────────────────────────────────────────────────────────────

    private void PositionOffscreenRight()
    {
        float camHeight  = _mainCameraOrthoSize;
        float camWidth   = camHeight * ((float)Screen.width / Screen.height);
        float frameHalfW = _frameBoundsWidth / 2f;
        _slideStartX     = camWidth + frameHalfW + SlideOffscreenPadding;
        transform.position = new Vector3(_slideStartX, 0f, transform.position.z);
    }

    private void StartSlideIn()
    {
        _state         = FrameState.SlidingIn;
        _slideProgress = 0f;
        _slideCenterX  = 0f;

        // Always start in landscape
        _orientation     = Orientation.Landscape;
        _currentRotation = LandscapeRotation;
        _targetRotation  = LandscapeRotation;
        ApplyRotation(LandscapeRotation);

        _cameraSpriteRenderer.enabled = true;
        _frameCamera.enabled          = true;
        _frameDisplay.enabled         = true;
        _landscapeOverlay.enabled     = true;
        _portraitOverlay.enabled      = false;

        _frameSpriteRenderer.color = defaultColor;
        _landscapeOverlay.color    = defaultColor;
        _portraitOverlay.color     = defaultColor;

        if (_frameRenderTexture != null)
        {
            _frameCamera.targetTexture = null;
            _frameRenderTexture.Release();
            Destroy(_frameRenderTexture);
            _frameRenderTexture = null;
        }

        // Reset to landscape ortho size
        _defaultFrameOrthoSize        = _frameBoundsHeight / 2f;
        _frameCamera.orthographicSize = _defaultFrameOrthoSize;
        _targetFrameOrthoSize         = _defaultFrameOrthoSize;
        _currentZoom                  = 1f;

        PositionOffscreenRight();
    }

    private void StartSlideOut()
    {
        _state         = FrameState.SlidingOut;
        _slideProgress = 1f;

        _frameSpriteRenderer.color = defaultColor;
        _landscapeOverlay.color    = defaultColor;
        _portraitOverlay.color     = defaultColor;

        float camHeight  = _mainCameraOrthoSize;
        float camWidth   = camHeight * ((float)Screen.width / Screen.height);
        float frameHalfW = _frameBoundsWidth / 2f;
        _slideStartX     = camWidth + frameHalfW + SlideOffscreenPadding;
        _slideCenterX    = transform.position.x;
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
            _cameraSpriteRenderer.enabled = false;
            _frameCamera.enabled          = false;
            _frameDisplay.enabled         = false;
            _landscapeOverlay.enabled     = false;
            _portraitOverlay.enabled      = false;
            _state        = FrameState.Hidden;
            _slideCenterX = 0f;
        }
    }

    // ── Active ───────────────────────────────────────────────────────────────

    private void UpdateActive()
    {
        if (_isRotating) return;
        if (IsFrozenForTransition) return;  // add this

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
        if (IsFrozenForTransition) return;

        _frameCamera.transform.position = new Vector3(
            transform.position.x,
            transform.position.y,
            _frameCamera.transform.position.z
        );
    }

    private void UpdateFrameCameraViewport()
    {
        Vector2 screenCenter = _mainCamera.WorldToScreenPoint(transform.position);

        // Use correct dimensions per orientation
        float worldW = _orientation == Orientation.Portrait ? _frameBoundsHeight : _frameBoundsWidth;
        float worldH = _orientation == Orientation.Portrait ? _frameBoundsWidth  : _frameBoundsHeight;

        Vector2 worldRef   = _mainCamera.WorldToScreenPoint(
            (Vector3)((Vector2)transform.position + new Vector2(worldW / 2f, worldH / 2f))
        );
        float screenWidth  = (worldRef.x - screenCenter.x) * 2f;
        float screenHeight = (worldRef.y - screenCenter.y) * 2f;

        float left   = screenCenter.x - screenWidth  / 2f;
        float bottom = screenCenter.y - screenHeight / 2f;

        // RawImage — always axis-aligned, never rotated
        RectTransform rt    = _frameDisplay.rectTransform;
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.zero;
        rt.anchoredPosition = new Vector2(left, bottom);
        rt.sizeDelta        = new Vector2(screenWidth, screenHeight);
        rt.localRotation    = Quaternion.identity;

        // Landscape overlay
        RectTransform landscapeRt    = _landscapeOverlay.rectTransform;
        landscapeRt.pivot            = new Vector2(0.5f, 0.5f);
        landscapeRt.anchorMin        = Vector2.zero;
        landscapeRt.anchorMax        = Vector2.zero;
        landscapeRt.anchoredPosition = new Vector2(screenCenter.x, screenCenter.y);
        landscapeRt.sizeDelta        = new Vector2(screenWidth, screenHeight);
        landscapeRt.localRotation    = Quaternion.identity;

        // Portrait overlay
        RectTransform portraitRt    = _portraitOverlay.rectTransform;
        portraitRt.pivot            = new Vector2(0.5f, 0.5f);
        portraitRt.anchorMin        = Vector2.zero;
        portraitRt.anchorMax        = Vector2.zero;
        portraitRt.anchoredPosition = new Vector2(screenCenter.x, screenCenter.y);
        portraitRt.sizeDelta        = new Vector2(screenWidth, screenHeight);
        portraitRt.localRotation    = Quaternion.identity;

        // Create render texture once — recreated after orientation change
        if (_frameRenderTexture == null)
        {
            int texW = Mathf.Max(1, Mathf.RoundToInt(screenWidth));
            int texH = Mathf.Max(1, Mathf.RoundToInt(screenHeight));

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

        // Swap dimensions in portrait since frame is rotated 90°
        float halfW = _orientation == Orientation.Portrait
            ? _frameBoundsHeight / 2f
            : _frameBoundsWidth  / 2f;

        float halfH = _orientation == Orientation.Portrait
            ? _frameBoundsWidth  / 2f
            : _frameBoundsHeight / 2f;

        worldPos.x = Mathf.Clamp(worldPos.x, -camWidth  + halfW,  camWidth  - halfW);
        worldPos.y = Mathf.Clamp(worldPos.y, -camHeight + halfH, camHeight - halfH);

        return worldPos;
    }

    // ── Photo ────────────────────────────────────────────────────────────────

    private void TryTakePhoto()
    {
        bool isPortrait = _orientation == Orientation.Portrait;

        foreach (PhotoOpportunity opportunity in photoOpportunities)
        {
            if (opportunity.IsMatch(_currentPosition, _currentZoom, isPortrait))
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
        PhotoDataStore.Instance.IsPortrait          = _orientation == Orientation.Portrait;

        PhotoDataStore.Instance.Body     += opportunity.bodyContribution;
        PhotoDataStore.Instance.Mind     += opportunity.mindContribution;
        PhotoDataStore.Instance.Charisma += opportunity.charismaContribution;

        PhotoDataStore.Instance.PreviousScene = SceneManager.GetActiveScene().name;

        _isFrozen                 = true;
        _landscapeOverlay.enabled = false;
        _portraitOverlay.enabled  = false;

        yield return new WaitForEndOfFrame();
        Texture2D screenshot = CaptureScreenshot();
        PhotoDataStore.Instance.LastPhoto = screenshot;

        // Add to diary
        PhotoDataStore.Instance.AddDiaryEntry();

        ShutterClose shutter = FindObjectOfType<ShutterClose>();
        if (shutter != null)
            yield return StartCoroutine(shutter.PlayAndLoad(
                opportunity.sceneToLoad,
                _frameCamera,
                _frameCamera.orthographicSize
            ));
        else
            SceneManager.LoadScene(opportunity.sceneToLoad);
    }

    private IEnumerator ZoomIntoFrame()
    {
        float startSize = _mainCamera.orthographicSize;
        float endSize   = _frameCamera.orthographicSize;

        Vector3 startPos = _mainCamera.transform.position;
        Vector3 endPos   = new Vector3(
            transform.position.x,
            transform.position.y,
            _mainCamera.transform.position.z
        );

        float progress = 0f;
        float speed    = 1.5f;

        while (progress < 1f)
        {
            progress += speed * Time.deltaTime;
            progress  = Mathf.Clamp01(progress);

            float eased = Mathf.SmoothStep(0f, 1f, progress);

            _mainCamera.orthographicSize  = Mathf.Lerp(startSize, endSize,   eased);
            _mainCamera.transform.position = Vector3.Lerp(startPos, endPos, eased);

            yield return null;
        }

        _mainCamera.orthographicSize   = endSize;
        _mainCamera.transform.position = endPos;
    }

    private Texture2D CaptureScreenshot()
    {
        Vector2 screenCenter = _mainCamera.WorldToScreenPoint(transform.position);

        float worldW = _orientation == Orientation.Portrait ? _frameBoundsHeight : _frameBoundsWidth;
        float worldH = _orientation == Orientation.Portrait ? _frameBoundsWidth  : _frameBoundsHeight;

        Vector2 worldRef   = _mainCamera.WorldToScreenPoint(
            (Vector3)((Vector2)transform.position + new Vector2(worldW / 2f, worldH / 2f))
        );
        float screenWidth  = (worldRef.x - screenCenter.x) * 2f;
        float screenHeight = (worldRef.y - screenCenter.y) * 2f;

        int width  = Mathf.Max(1, Mathf.RoundToInt(screenWidth));
        int height = Mathf.Max(1, Mathf.RoundToInt(screenHeight));

        Debug.Log($"Screenshot size: {width}x{height}, center: {screenCenter}");

        // Render frame camera into a temp texture
        RenderTexture rt           = new RenderTexture(width, height, 24);
        _frameCamera.targetTexture = rt;
        _frameCamera.Render();
        RenderTexture.active = rt;

        Texture2D photo = new Texture2D(width, height, TextureFormat.RGB24, false);
        photo.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        photo.Apply();

        // Check if pixels are black
        Color[] pixels = photo.GetPixels();
        Debug.Log($"First pixel: {pixels[0]}, Center pixel: {pixels[pixels.Length / 2]}");

        // Restore frame camera render texture
        _frameCamera.targetTexture = _frameRenderTexture;
        RenderTexture.active       = null;
        Destroy(rt);

        return photo;
    }

    // ── Indicator ────────────────────────────────────────────────────────────

    private void UpdateInZoneIndicator()
    {
        if (_frameSpriteRenderer == null) return;

        if (_state != FrameState.Active || _isRotating)
        {
            _frameSpriteRenderer.color = defaultColor;
            _landscapeOverlay.color    = defaultColor;
            _portraitOverlay.color     = defaultColor;
            return;
        }

        bool isPortrait = _orientation == Orientation.Portrait;
        bool inZone     = false;
        bool nearZone   = false;

        foreach (PhotoOpportunity opportunity in photoOpportunities)
        {
            if (opportunity.IsMatch(_currentPosition, _currentZoom, isPortrait))
            {
                inZone = true;
                break;
            }
            else if (opportunity.IsNear(_currentPosition, _currentZoom, isPortrait))
                nearZone = true;
        }

        Color targetColor = inZone ? inZoneColor : (nearZone ? nearZoneColor : defaultColor);

        _frameSpriteRenderer.color = Color.Lerp(
            _frameSpriteRenderer.color, targetColor, 10f * Time.deltaTime
        );

        _landscapeOverlay.color = _frameSpriteRenderer.color;
        _portraitOverlay.color  = _frameSpriteRenderer.color;
    }
}