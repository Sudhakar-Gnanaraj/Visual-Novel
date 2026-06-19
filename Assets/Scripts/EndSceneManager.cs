using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class EndSceneManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button viewDiaryButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Scene Names")]
    [SerializeField] private string diarySceneName  = "DiaryScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Shutter References")]
    [SerializeField] private RectTransform _panelA;
    [SerializeField] private RectTransform _panelB;
    [SerializeField] private CanvasScaler _canvasScaler;

    [Header("Shutter Settings")]
    [SerializeField] private float shutterSpeed = 2f;

    private bool _isTransitioning = false;

    private void Start()
    {
        viewDiaryButton.onClick.AddListener(OnViewDiary);
        mainMenuButton.onClick.AddListener(OnMainMenu);

        if (_panelA != null) _panelA.gameObject.SetActive(false);
        if (_panelB != null) _panelB.gameObject.SetActive(false);
    }

    private void OnViewDiary()
    {
        if (_isTransitioning) return;

        // Set entry reason so diary knows we came from end scene
        if (PhotoDataStore.Instance != null)
        {
            PhotoDataStore.Instance.EntryReason   = DiaryEntryReason.Escape;
            PhotoDataStore.Instance.PreviousScene = SceneManager.GetActiveScene().name;
        }

        StartCoroutine(PlayShutterAndLoad(diarySceneName));
    }

    private void OnMainMenu()
    {
        if (_isTransitioning) return;
        StartCoroutine(PlayShutterAndLoad(mainMenuSceneName));
    }

    private float GetPanelHeight()
    {
        if (_canvasScaler != null &&
            _canvasScaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize)
            return _canvasScaler.referenceResolution.y / 2f;

        return Screen.height / 2f;
    }

    private IEnumerator PlayShutterAndLoad(string sceneName)
    {
        _isTransitioning = true;

        viewDiaryButton.interactable = false;
        mainMenuButton.interactable  = false;

        float halfH = GetPanelHeight();

        _panelA.anchorMin        = new Vector2(0f, 1f);
        _panelA.anchorMax        = new Vector2(1f, 1f);
        _panelA.pivot            = new Vector2(0.5f, 1f);
        _panelA.sizeDelta        = new Vector2(0f, halfH);

        _panelB.anchorMin        = new Vector2(0f, 0f);
        _panelB.anchorMax        = new Vector2(1f, 0f);
        _panelB.pivot            = new Vector2(0.5f, 0f);
        _panelB.sizeDelta        = new Vector2(0f, halfH);

        Vector2 aStart = new Vector2(0f,  halfH);
        Vector2 aEnd   = new Vector2(0f,  0f);
        Vector2 bStart = new Vector2(0f, -halfH);
        Vector2 bEnd   = new Vector2(0f,  0f);

        _panelA.gameObject.SetActive(true);
        _panelB.gameObject.SetActive(true);

        _panelA.anchoredPosition = aStart;
        _panelB.anchoredPosition = bStart;

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

        yield return new WaitForSeconds(0.1f);

        SceneManager.LoadScene(sceneName);
    }
}