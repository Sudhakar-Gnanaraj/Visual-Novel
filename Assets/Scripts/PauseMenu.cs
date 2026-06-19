using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject overlayObject;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private CameraFrame cameraFrame;
    [SerializeField] private ShutterToMenu shutterToMenu;

    [Header("Optional Diary References")]
    [SerializeField] private DiaryDisplay diaryDisplay;
    [SerializeField] private DiarySceneManager diarySceneManager;

    [Header("Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Input Blocker")]
    [SerializeField] private GameObject inputBlocker;

    private bool _isPaused = false;

    private bool _leftWasActive     = false;
    private bool _rightWasActive    = false;
    private bool _continueWasActive = false;
    private bool _menuWasActive     = false;

    private void Start()
    {
        overlayObject.SetActive(false);
        mainMenuButton.onClick.AddListener(GoToMainMenu);
        resumeButton.onClick.AddListener(Resume);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_isPaused)
            {
                Resume();
                return;
            }

            if (!CanPause()) return;

            Pause();
        }
    }

    private bool CanPause()
    {
        if (cameraFrame == null) return true;

        return !cameraFrame.IsSliding  &&
               !cameraFrame.IsRotating &&
               !cameraFrame.IsFrozen;
    }

    private void Pause()
    {
        _isPaused      = true;
        Time.timeScale = 0f;
        overlayObject.SetActive(true);

        // Block all input beneath overlay
        if (inputBlocker != null)
            inputBlocker.SetActive(true);

        if (diaryDisplay != null)
        {
            _leftWasActive  = diaryDisplay.LeftButtonActive;
            _rightWasActive = diaryDisplay.RightButtonActive;
            diaryDisplay.SetButtonsInteractable(false);
        }

        if (diarySceneManager != null)
        {
            _continueWasActive = diarySceneManager.ContinueButtonActive;
            _menuWasActive     = diarySceneManager.MainMenuButtonActive;
            diarySceneManager.SetButtonsInteractable(false);
        }
    }

    private void Resume()
    {
        _isPaused      = false;
        Time.timeScale = 1f;
        overlayObject.SetActive(false);

        // Remove input blocker
        if (inputBlocker != null)
            inputBlocker.SetActive(false);

        if (diaryDisplay != null)
            diaryDisplay.RestoreButtons(_leftWasActive, _rightWasActive);

        if (diarySceneManager != null)
            diarySceneManager.RestoreButtons(_continueWasActive, _menuWasActive);
    }

    private void SetDiaryButtonsInteractable(bool interactable)
    {
        if (diaryDisplay != null)
        {
            diaryDisplay.SetButtonsInteractable(interactable);
        }

        if (diarySceneManager != null)
        {
            diarySceneManager.SetButtonsInteractable(interactable);
        }
    }

    private void GoToMainMenu()
    {
        overlayObject.SetActive(false);

        if (shutterToMenu != null)
            shutterToMenu.GoToMainMenu(mainMenuSceneName);
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}