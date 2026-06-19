using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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

    [Header("Auto Transition")]
    [SerializeField] private bool autoTransition = false;
    [SerializeField] private float autoTransitionDelay = 3f;

    private void Start()
    {
        if (PhotoDataStore.Instance != null)
            PhotoDataStore.Instance.CurrentScene = SceneManager.GetActiveScene().name;

        continueButtonObject.SetActive(false);

        continueButtonObject.GetComponent<Button>().onClick.AddListener(TransitionToNextScene);

        if (autoTransition)
            Invoke(nameof(TransitionToNextScene), autoTransitionDelay);
    }

    public void EnableContinueButton()
    {
        continueButtonObject.SetActive(true);
    }

    public void TransitionToNextScene()
    {
        string nextScene = DetermineNextScene();
        Debug.Log($"Transitioning to: {nextScene}");
        SceneManager.LoadScene(nextScene);
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
}