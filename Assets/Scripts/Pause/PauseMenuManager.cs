using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI (World-Space Canvas)")]
    public Canvas pauseCanvas;          // World Space canvas (root)
    public GameObject mainPanel;        // Panel berisi tombol Resume/Settings/Leave
    public GameObject settingsPanel;    // Panel settings (berisi slider volume)

    [Header("Popup Placement")]
    public Transform playerCamera;      // drag Main Camera
    public float distance = 1.2f;       // jarak di depan kamera
    public Vector2 canvasSize = new Vector2(600, 800); // lebar x tinggi canvas (unit world)
    public bool faceCamera = true;

    [Header("Integration")]
    public MonoBehaviour[] controlsToDisable; // PlayerLook / Motor / Interactor dll.
    public GameObject dotCrosshair;     // crosshair/dot (opsional)
    public string mainMenuSceneName = "Main Menu";

    bool isPaused;
    float prevTimeScale = 1f;

    void Start()
    {
        if (!playerCamera && Camera.main) playerCamera = Camera.main.transform;
        if (pauseCanvas)
        {
            pauseCanvas.renderMode = RenderMode.WorldSpace;
            var rt = pauseCanvas.GetComponent<RectTransform>();
            rt.sizeDelta = canvasSize;
            pauseCanvas.gameObject.SetActive(false);
        }
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();

        if (isPaused && pauseCanvas && playerCamera)
        {
            // jaga posisi di depan kamera
            var t = pauseCanvas.transform;
            t.position = playerCamera.position + playerCamera.forward * distance;
            if (faceCamera) t.rotation = Quaternion.LookRotation(playerCamera.forward, playerCamera.up);
        }
    }

    public void TogglePause()
    {
        if (isPaused) Resume();
        else Pause();
    }

    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true; // hentikan semua suara

        if (pauseCanvas) pauseCanvas.gameObject.SetActive(true);
        if (mainPanel) mainPanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);

        if (playerCamera && pauseCanvas)
        {
            var t = pauseCanvas.transform;
            t.position = playerCamera.position + playerCamera.forward * distance;
            if (faceCamera) t.rotation = Quaternion.LookRotation(playerCamera.forward, playerCamera.up);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        if (dotCrosshair) dotCrosshair.SetActive(false);

        foreach (var c in controlsToDisable)
            if (c) c.enabled = false;
    }

    public void Resume()
    {
        if (!isPaused) return;
        isPaused = false;

        Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
        AudioListener.pause = false;

        if (pauseCanvas) pauseCanvas.gameObject.SetActive(false);
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (dotCrosshair) dotCrosshair.SetActive(true);

        foreach (var c in controlsToDisable)
            if (c) c.enabled = true;
    }

    // ===== hooked by buttons =====
    public void OnResumeButton() => Resume();
    public void OnSettingsButton()
    {
        if (!isPaused) return;
        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(true);
    }
    public void OnBackFromSettings()
    {
        if (!isPaused) return;
        if (settingsPanel) settingsPanel.SetActive(false);
        if (mainPanel) mainPanel.SetActive(true);
    }
    public void OnLeaveGameButton()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (!string.IsNullOrEmpty(mainMenuSceneName))
            SceneManager.LoadScene(mainMenuSceneName);
    }
}
