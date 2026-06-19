using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[System.Serializable]
public class SceneTransitionRule
{
    public string previousScene;
    public string opportunityName;

    [Header("Stat Thresholds (0 = ignore)")]
    public int minBody;
    public int minMind;
    public int minCharisma;

    public string nextScene;
    public int priority;
}

public class DiarySceneManager : MonoBehaviour
{
    [Header("Transition Rules")]
    [SerializeField] private SceneTransitionRule[] rules;

    [Header("Fallback Scene")]
    [SerializeField] private string defaultNextScene;

    [Header("Continue Button")]
    [SerializeField] private GameObject continueButtonObject;
    [SerializeField] private Image continueButtonImage;

    [Header("Button Sprites")]
    [SerializeField] private Sprite nextSceneSprite;
    [SerializeField] private Sprite returnSceneSprite;

    [Header("Shutter References")]
    [SerializeField] private RectTransform _panelA;
    [SerializeField] private RectTransform _panelB;
    [SerializeField] private CanvasScaler _canvasScaler;

    [Header("Shutter Settings")]
    [SerializeField] private float shutterSpeed = 2f;

    [Header("Auto Transition")]
    [SerializeField] private bool autoTransition = false;
    [SerializeField] private float autoTransitionDelay = 3f;

    [Header("Main Menu Button")]
    [SerializeField] private GameObject mainMenuButtonObject;
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    public bool ContinueButtonActive => continueButtonObject != null && continueButtonObject.activeSelf;
    public bool MainMenuButtonActive => mainMenuButtonObject != null && mainMenuButtonObject.activeSelf;

    private bool _returningToScene = false;
    private bool _isTransitioning  = false;

    private void Start()
    {
        if (PhotoDataStore.Instance != null)
            PhotoDataStore.Instance.CurrentScene = SceneManager.GetActiveScene().name;

        continueButtonObject.SetActive(false);

        continueButtonObject.GetComponent<Button>().onClick.AddListener(OnContinueButton);

        // Hide panels at start
        if (_panelA != null) _panelA.gameObject.SetActive(false);
        if (_panelB != null) _panelB.gameObject.SetActive(false);

        if (autoTransition)
            Invoke(nameof(OnContinueButton), autoTransitionDelay);
    }

    public void EnableContinueButton()
    {
        continueButtonObject.SetActive(true);

        if (PhotoDataStore.Instance != null &&
            PhotoDataStore.Instance.EntryReason == DiaryEntryReason.Escape)
        {
            _returningToScene = true;
            if (continueButtonImage != null && returnSceneSprite != null)
                continueButtonImage.sprite = returnSceneSprite;
        }
        else
        {
            _returningToScene = false;
            if (continueButtonImage != null && nextSceneSprite != null)
                continueButtonImage.sprite = nextSceneSprite;
        }
    }

    private void OnContinueButton()
    {
        if (_isTransitioning) return;

        string targetScene = _returningToScene
            ? PhotoDataStore.Instance?.PreviousScene ?? defaultNextScene
            : DetermineNextScene();

        StartCoroutine(PlayShutterAndLoad(targetScene));
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
        continueButtonObject.SetActive(false);

        float halfH = GetPanelHeight();

        // Setup panels
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

        // Close shutter
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

        // Load without resetting photos
        SceneManager.LoadScene(sceneName);
    }

    private string DetermineNextScene()
    {
        if (PhotoDataStore.Instance == null)
            return defaultNextScene;

        int body       = PhotoDataStore.Instance.Body;
        int mind       = PhotoDataStore.Instance.Mind;
        int charisma   = PhotoDataStore.Instance.Charisma;
        string prev    = PhotoDataStore.Instance.PreviousScene;
        string oppName = PhotoDataStore.Instance.LastOpportunityName;

        System.Array.Sort(rules, (a, b) => b.priority.CompareTo(a.priority));

        foreach (SceneTransitionRule rule in rules)
        {
            bool prevMatch     = string.IsNullOrEmpty(rule.previousScene) ||
                                 rule.previousScene == prev;
            bool oppMatch      = string.IsNullOrEmpty(rule.opportunityName) ||
                                 rule.opportunityName == oppName;
            bool bodyMatch     = body     >= rule.minBody;
            bool mindMatch     = mind     >= rule.minMind;
            bool charismaMatch = charisma >= rule.minCharisma;

            if (prevMatch && oppMatch && bodyMatch && mindMatch && charismaMatch)
                return rule.nextScene;
        }

        return defaultNextScene;
    }

    public void SetButtonsInteractable(bool interactable)
    {
        if (continueButtonObject != null)
            continueButtonObject.SetActive(interactable);

        if (mainMenuButtonObject != null)
            mainMenuButtonObject.SetActive(interactable);
    }

    public void RestoreButtons(bool continueWasActive, bool menuWasActive)
    {
        if (continueButtonObject != null) continueButtonObject.SetActive(continueWasActive);
        if (mainMenuButtonObject != null) mainMenuButtonObject.SetActive(menuWasActive);
    }
}