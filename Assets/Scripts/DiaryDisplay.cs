using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DiaryDisplay : MonoBehaviour
{
    [Header("Page Objects")]
    [SerializeField] private GameObject leftPageRoot;
    [SerializeField] private GameObject rightPageRoot;
    [SerializeField] private GameObject turningPageRoot;

    [Header("Page Backgrounds")]
    [SerializeField] private SpriteRenderer leftBackground;
    [SerializeField] private SpriteRenderer rightBackground;
    [SerializeField] private SpriteRenderer turningBackground;

    [Header("Content Renderers")]
    [SerializeField] private SpriteRenderer photoRenderer;
    [SerializeField] private SpriteRenderer textCardRenderer;
    [SerializeField] private SpriteRenderer turningContentRenderer;

    [Header("Spine")]
    [SerializeField] private Transform spineCenter;
    [SerializeField] private float pageOffsetX = 3f;

    [Header("Navigation Buttons")]
    [SerializeField] private GameObject leftButtonObject;
    [SerializeField] private GameObject rightButtonObject;

    [Header("Page Turn Settings")]
    [SerializeField] private float pageTurnSpeed = 2f;

    [Header("Display Settings")]
    [SerializeField] private Vector2 landscapePhotoSize = new Vector2(3f, 2f);
    [SerializeField] private Vector2 portraitPhotoSize  = new Vector2(2f, 3f);
    [SerializeField] private Vector2 textCardSize       = new Vector2(3f, 2f);

    private int _currentSpread   = 0;
    private bool _isTurning      = false;
    private bool _buttonsEnabled = false;

    private Button _leftButton;
    private Button _rightButton;

    private void Start()
    {
        leftButtonObject.SetActive(false);
        rightButtonObject.SetActive(false);

        _leftButton  = leftButtonObject.GetComponent<Button>();
        _rightButton = rightButtonObject.GetComponent<Button>();

        _leftButton.onClick.AddListener(OnLeftButton);
        _rightButton.onClick.AddListener(OnRightButton);

        turningPageRoot.SetActive(false);

        SetSortingOrders();

        if (PhotoDataStore.Instance == null || PhotoDataStore.Instance.DiaryEntries.Count == 0)
        {
            Debug.LogWarning("No diary entries found.");
            return;
        }

        _currentSpread = PhotoDataStore.Instance.DiaryEntries.Count - 1;
        DisplaySpread(_currentSpread);
    }

    private void SetSortingOrders()
    {
        leftBackground.sortingLayerName         = "AboveUI";
        leftBackground.sortingOrder             = 10;
        photoRenderer.sortingLayerName          = "AboveUI";
        photoRenderer.sortingOrder              = 11;

        rightBackground.sortingLayerName        = "AboveUI";
        rightBackground.sortingOrder            = 10;
        textCardRenderer.sortingLayerName       = "AboveUI";
        textCardRenderer.sortingOrder           = 11;

        turningBackground.sortingLayerName      = "AboveUI";
        turningBackground.sortingOrder          = 20;
        turningContentRenderer.sortingLayerName = "AboveUI";
        turningContentRenderer.sortingOrder     = 21;
    }

    public void EnableButtons()
    {
        _buttonsEnabled = true;
        UpdateButtons();
    }

    private void OnRightButton()
    {
        if (_isTurning || !_buttonsEnabled) return;
        if (_currentSpread < PhotoDataStore.Instance.DiaryEntries.Count - 1)
            StartCoroutine(TurnPageForward());
    }

    private void OnLeftButton()
    {
        if (_isTurning || !_buttonsEnabled) return;
        if (_currentSpread > 0)
            StartCoroutine(TurnPageBackward());
    }

    private void UpdateButtons()
    {
        int total = PhotoDataStore.Instance.DiaryEntries.Count;
        leftButtonObject.SetActive(_currentSpread > 0);
        rightButtonObject.SetActive(_currentSpread < total - 1);
    }

    // ── Display ───────────────────────────────────────────────────────────────

    private void DisplaySpread(int index)
    {
        DiaryEntry entry = PhotoDataStore.Instance.DiaryEntries[index];
        DisplayPhotoOn(photoRenderer, entry);
        DisplayTextCardOn(textCardRenderer, entry);
    }

    // ── Forward Turn ──────────────────────────────────────────────────────────

    private IEnumerator TurnPageForward()
    {
        _isTurning = true;
        leftButtonObject.SetActive(false);
        rightButtonObject.SetActive(false);

        int nextSpread          = _currentSpread + 1;
        DiaryEntry currentEntry = PhotoDataStore.Instance.DiaryEntries[_currentSpread];
        DiaryEntry nextEntry    = PhotoDataStore.Instance.DiaryEntries[nextSpread];

        // Turning page starts as current RIGHT page (text card)
        SetupTurningPage(currentEntry, false);
        turningPageRoot.SetActive(true);
        PositionTurningPage(spineCenter.position.x + pageOffsetX);

        // Immediately update RIGHT static page to next entry text card
        DisplayTextCardOn(textCardRenderer, nextEntry);

        // Phase 1 — 0° to 90°
        yield return StartCoroutine(AnimateHalfTurn(
            spineCenter.position.x + pageOffsetX, 0f, -90f
        ));

        // At 90° — swap to next LEFT page (photo), flip horizontally
        SetupTurningPage(nextEntry, true);
        turningContentRenderer.transform.localScale = new Vector3(
            -turningContentRenderer.transform.localScale.x,
            turningContentRenderer.transform.localScale.y,
            turningContentRenderer.transform.localScale.z
        );
        turningBackground.transform.localScale = new Vector3(
            -turningBackground.transform.localScale.x,
            turningBackground.transform.localScale.y,
            turningBackground.transform.localScale.z
        );

        // Phase 2 — 90° to 180°
        yield return StartCoroutine(AnimateHalfTurn(
            spineCenter.position.x + pageOffsetX, -90f, -180f
        ));

        // Turn complete — update LEFT static page to next entry photo
        turningPageRoot.SetActive(false);

        // Reset flips on turning page for next use
        ResetTurningPageFlip();

        _currentSpread = nextSpread;

        // Only update left page after turn completes
        DisplayPhotoOn(photoRenderer, PhotoDataStore.Instance.DiaryEntries[_currentSpread]);

        _isTurning = false;
        if (_buttonsEnabled) UpdateButtons();
    }

    private IEnumerator TurnPageBackward()
    {
        _isTurning = true;
        leftButtonObject.SetActive(false);
        rightButtonObject.SetActive(false);

        int prevSpread          = _currentSpread - 1;
        DiaryEntry currentEntry = PhotoDataStore.Instance.DiaryEntries[_currentSpread];
        DiaryEntry prevEntry    = PhotoDataStore.Instance.DiaryEntries[prevSpread];

        // Turning page starts as current LEFT page (photo)
        SetupTurningPage(currentEntry, true);
        turningPageRoot.SetActive(true);
        PositionTurningPage(spineCenter.position.x - pageOffsetX);

        // Immediately update LEFT static page to prev entry photo
        DisplayPhotoOn(photoRenderer, prevEntry);

        // Phase 1 — 0° to 90°
        yield return StartCoroutine(AnimateHalfTurn(
            spineCenter.position.x - pageOffsetX, 0f, 90f
        ));

        // At 90° — swap to prev RIGHT page (text card), flip horizontally
        SetupTurningPage(prevEntry, false);
        turningContentRenderer.transform.localScale = new Vector3(
            -turningContentRenderer.transform.localScale.x,
            turningContentRenderer.transform.localScale.y,
            turningContentRenderer.transform.localScale.z
        );
        turningBackground.transform.localScale = new Vector3(
            -turningBackground.transform.localScale.x,
            turningBackground.transform.localScale.y,
            turningBackground.transform.localScale.z
        );

        // Phase 2 — 90° to 180°
        yield return StartCoroutine(AnimateHalfTurn(
            spineCenter.position.x - pageOffsetX, 90f, 180f
        ));

        // Turn complete — update RIGHT static page after turn
        turningPageRoot.SetActive(false);

        // Reset flips on turning page for next use
        ResetTurningPageFlip();

        _currentSpread = prevSpread;

        // Only update right page after turn completes
        DisplayTextCardOn(textCardRenderer, PhotoDataStore.Instance.DiaryEntries[_currentSpread]);

        _isTurning = false;
        if (_buttonsEnabled) UpdateButtons();
    }

    private void ResetTurningPageFlip()
    {
        // Reset content flip
        Vector3 contentScale = turningContentRenderer.transform.localScale;
        turningContentRenderer.transform.localScale = new Vector3(
            Mathf.Abs(contentScale.x),
            contentScale.y,
            contentScale.z
        );

        // Reset background flip
        Vector3 bgScale = turningBackground.transform.localScale;
        turningBackground.transform.localScale = new Vector3(
            Mathf.Abs(bgScale.x),
            bgScale.y,
            bgScale.z
        );
    }

    // ── Animation ─────────────────────────────────────────────────────────────

    private void PositionTurningPage(float startX)
    {
        turningPageRoot.transform.position = new Vector3(
            startX,
            spineCenter.position.y,
            spineCenter.position.z
        );
        turningPageRoot.transform.localRotation = Quaternion.identity;
        turningPageRoot.transform.localScale    = Vector3.one;
    }

    private IEnumerator AnimateHalfTurn(float startX, float fromAngle, float toAngle)
    {
        float progress = 0f;

        while (progress < 1f)
        {
            progress += pageTurnSpeed * Time.deltaTime;
            progress  = Mathf.Clamp01(progress);

            float eased    = Mathf.SmoothStep(0f, 1f, progress);
            float angle    = Mathf.Lerp(fromAngle, toAngle, eased);
            float radians  = angle * Mathf.Deg2Rad;
            float cosAngle = Mathf.Cos(radians);

            // Arc around spine
            float newX = spineCenter.position.x +
                         (startX - spineCenter.position.x) * cosAngle;

            turningPageRoot.transform.position = new Vector3(
                newX,
                spineCenter.position.y,
                spineCenter.position.z
            );

            turningPageRoot.transform.localRotation = Quaternion.Euler(0f, angle, 0f);

            turningPageRoot.transform.localScale = new Vector3(
                Mathf.Max(Mathf.Abs(cosAngle), 0.001f),
                1f,
                1f
            );

            yield return null;
        }
    }

    // ── Turning Page Setup ────────────────────────────────────────────────────

    private void SetupTurningPage(DiaryEntry entry, bool showPhoto)
    {
        if (showPhoto)
        {
            if (entry.photo == null) return;
            Texture2D readable = CopyTexture(entry.photo);
            turningContentRenderer.sprite = Sprite.Create(
                readable,
                new Rect(0, 0, readable.width, readable.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
            Vector2 size = entry.isPortrait ? portraitPhotoSize : landscapePhotoSize;
            ScaleRenderer(turningContentRenderer, readable.width, readable.height, size);
        }
        else
        {
            if (entry.textCard == null) return;
            turningContentRenderer.sprite = entry.textCard;
            ScaleRenderer(
                turningContentRenderer,
                (int)entry.textCard.rect.width,
                (int)entry.textCard.rect.height,
                textCardSize
            );
        }
    }

    private void DisplayPhotoOn(SpriteRenderer sr, DiaryEntry entry)
    {
        if (entry.photo == null) return;
        Texture2D readable = CopyTexture(entry.photo);
        sr.sprite = Sprite.Create(
            readable,
            new Rect(0, 0, readable.width, readable.height),
            new Vector2(0.5f, 0.5f),
            100f
        );
        Vector2 size = entry.isPortrait ? portraitPhotoSize : landscapePhotoSize;
        ScaleRenderer(sr, readable.width, readable.height, size);
    }

    private void DisplayTextCardOn(SpriteRenderer sr, DiaryEntry entry)
    {
        if (entry.textCard == null) return;
        sr.sprite = entry.textCard;
        ScaleRenderer(
            sr,
            (int)entry.textCard.rect.width,
            (int)entry.textCard.rect.height,
            textCardSize
        );
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private Texture2D CopyTexture(Texture2D source)
    {
        Texture2D readable = new Texture2D(
            source.width, source.height, TextureFormat.RGB24, false
        );
        readable.SetPixels(source.GetPixels());
        readable.Apply();
        return readable;
    }

    private void ScaleRenderer(SpriteRenderer sr, int texWidth, int texHeight, Vector2 targetSize)
    {
        if (texWidth == 0 || texHeight == 0) return;
        float nativeWidth  = texWidth  / 100f;
        float nativeHeight = texHeight / 100f;
        sr.transform.localScale = new Vector3(
            targetSize.x / nativeWidth,
            targetSize.y / nativeHeight,
            1f
        );
    }
}