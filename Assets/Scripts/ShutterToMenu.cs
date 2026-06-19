using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ShutterToMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform _panelA;  // top
    [SerializeField] private RectTransform _panelB;  // bottom

    [Header("Settings")]
    [SerializeField] private float shutterSpeed = 2f;

    private bool _isAnimating = false;

    private void Start()
    {
        _panelA.gameObject.SetActive(false);
        _panelB.gameObject.SetActive(false);
    }

    private void SetupVertical()
    {
        _panelA.anchorMin        = new Vector2(0f, 1f);
        _panelA.anchorMax        = new Vector2(1f, 1f);
        _panelA.pivot            = new Vector2(0.5f, 1f);
        _panelA.sizeDelta        = new Vector2(0f, Screen.height / 2f);

        _panelB.anchorMin        = new Vector2(0f, 0f);
        _panelB.anchorMax        = new Vector2(1f, 0f);
        _panelB.pivot            = new Vector2(0.5f, 0f);
        _panelB.sizeDelta        = new Vector2(0f, Screen.height / 2f);
    }

    public void GoToMainMenu(string mainMenuScene)
    {
        if (_isAnimating) return;
        StartCoroutine(PlayAndLoad(mainMenuScene));
    }

    private IEnumerator PlayAndLoad(string sceneName)
    {
        _isAnimating = true;

        SetupVertical();

        Vector2 aStart = new Vector2(0f,  Screen.height / 2f);
        Vector2 aEnd   = new Vector2(0f,  0f);
        Vector2 bStart = new Vector2(0f, -Screen.height / 2f);
        Vector2 bEnd   = new Vector2(0f,  0f);

        _panelA.gameObject.SetActive(true);
        _panelB.gameObject.SetActive(true);

        _panelA.anchoredPosition = aStart;
        _panelB.anchoredPosition = bStart;

        float progress = 0f;
        while (progress < 1f)
        {
            progress += shutterSpeed * Time.unscaledDeltaTime;
            progress  = Mathf.Clamp01(progress);

            float eased = Mathf.SmoothStep(0f, 1f, progress);
            _panelA.anchoredPosition = Vector2.Lerp(aStart, aEnd, eased);
            _panelB.anchoredPosition = Vector2.Lerp(bStart, bEnd, eased);

            yield return null;
        }

        _panelA.anchoredPosition = aEnd;
        _panelB.anchoredPosition = bEnd;

        yield return new WaitForSecondsRealtime(0.1f);

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
}