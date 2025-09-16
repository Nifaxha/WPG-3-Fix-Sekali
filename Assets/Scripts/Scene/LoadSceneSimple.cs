using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadSceneSimple : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loadingScreen;
    public Slider slider;
    public Text progressText;

    [Header("Trigger Settings")]
    public bool playerIsClose;
    public int sceneToLoad;

    [Header("Confirmation Settings")]
    public bool showConfirmation = true;
    public GameObject confirmationPanel;
    public Button yesButton;
    public Button noButton;

    private void Start()
    {
        if (yesButton != null) yesButton.onClick.AddListener(ConfirmLoad);
        if (noButton != null) noButton.onClick.AddListener(CancelLoad);

        if (confirmationPanel != null) confirmationPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && playerIsClose)
        {
            if (showConfirmation && confirmationPanel != null)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                confirmationPanel.SetActive(true);
                Time.timeScale = 0f;
            }
            else
            {
                LoadLevel(sceneToLoad);
            }
        }
    }

    private void ConfirmLoad()
    {
        Time.timeScale = 1f;
        confirmationPanel.SetActive(false);
        LoadLevel(sceneToLoad);
    }

    private void CancelLoad()
    {
        Time.timeScale = 1f;
        confirmationPanel.SetActive(false);
    }

    public void LoadLevel(int sceneIndex)
    {
        StartCoroutine(LoadAsynchronously(sceneIndex));
    }

    private IEnumerator LoadAsynchronously(int sceneIndex)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
        if (loadingScreen != null) loadingScreen.SetActive(true);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            if (slider != null) slider.value = progress;
            if (progressText != null) progressText.text = (progress * 100f).ToString("F0") + "%";

            yield return null;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerIsClose = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerIsClose = false;
    }
}
