using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public static MainMenuManager _;
    [SerializeField] private bool _debugMode;
    public enum MainMenuButtons { play, settings, credits, exit, back };
    public enum CreditsButtons { Back };
    public enum SettingsButtons { back };
    [SerializeField] GameObject _MainMenuContainer;
    [SerializeField] GameObject _CreditsMenuContainer;
    [SerializeField] GameObject _SettingsMenuContainer;
    [SerializeField] private string _sceneToAfterClickingPlay;
    public void Awake()
    {
        if (_ == null)
        {
            _ = this;
        }
        else
        {
            Debug.LogError("There are more than 1 MainMenuManager's in the scene    ");
        }
    }
    private void Start()
    {
        OpenMenu(_MainMenuContainer);

        // Paksa kursor tampil & bebas di Main Menu
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // (opsional) kalau gameplay pernah pause
        Time.timeScale = 1f;
    }
    public void OpenCreditsMenu()
    {
        OpenMenu(_CreditsMenuContainer);
    }
    public void OpenSettingsMenu()
    {
        OpenMenu(_SettingsMenuContainer);
    }
    public void ReturnToMainMenu()
    {
        OpenMenu(_MainMenuContainer);
    }
    public void CreditsButtonClicked(CreditsButtons buttonClicked)
    {
        switch (buttonClicked)
        {
            case CreditsButtons.Back:
                ReturnToMainMenu();
                break;
            default:
                break;
        }
    }
    public void SettingsButtonClicked(SettingsButtons buttonClicked)
    {
        switch (buttonClicked)
        {
            case SettingsButtons.back:
                ReturnToMainMenu();
                break;
            default:
                break;
        }
    }
    public void MainMenuButtonClicked(MainMenuButtons buttonClicked)
    {
        DebugMessage("Button Clicked: " + buttonClicked.ToString());
        switch (buttonClicked)
        {
            case MainMenuButtons.play:
                PlayClicked();
                break;
            case MainMenuButtons.settings:
                OpenSettingsMenu();
                break;
            case MainMenuButtons.credits:
                OpenCreditsMenu();
                break;
            case MainMenuButtons.exit:
                ExitGame();
                break;
            default:
                Debug.Log("Button clicked that wasn't implemented in MainMenuManager method");
                break;
        }
    }
    private void DebugMessage(string message)
    {
        if (_debugMode)
        {
            Debug.Log(message);
        }
    }
    public void PlayClicked()
    {
        SceneManager.LoadScene(_sceneToAfterClickingPlay);
    }
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
            Application.Quit();
#endif
    }
    public void OpenMenu(GameObject menuToOpen)
    {
        _MainMenuContainer.SetActive(menuToOpen == _MainMenuContainer);
        _CreditsMenuContainer.SetActive(menuToOpen == _CreditsMenuContainer);
        _SettingsMenuContainer.SetActive(menuToOpen == _SettingsMenuContainer);
    }
}