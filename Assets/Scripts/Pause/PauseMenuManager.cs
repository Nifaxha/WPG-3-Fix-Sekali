using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI Mode")]
    public bool useWorldSpace = false;
    public Vector3 worldCanvasScale = new Vector3(0.002f, 0.002f, 0.002f);

    [Header("UI (World-Space Canvas)")]
    public Canvas pauseCanvas;
    public GameObject mainPanel;
    public GameObject settingsPanel;

    [Header("Popup Placement")]
    public Transform playerCamera;
    public float distance = 1.2f;
    public Vector2 canvasSize = new Vector2(600, 800);
    public bool faceCamera = true;

    [Header("Integration")]
    public MonoBehaviour[] controlsToDisable;
    public GameObject dotCrosshair;
    public string mainMenuSceneName = "Main Menu";

    bool isPaused;
    float prevTimeScale = 1f;
    Coroutine cursorFixCoroutine;

    void Start()
    {
        if (!playerCamera && Camera.main) playerCamera = Camera.main.transform;

        if (pauseCanvas)
        {
            if (useWorldSpace)
            {
                pauseCanvas.renderMode = RenderMode.WorldSpace;
                var rt = pauseCanvas.GetComponent<RectTransform>();
                rt.sizeDelta = canvasSize;
                pauseCanvas.transform.localScale = worldCanvasScale;

                if (pauseCanvas.worldCamera == null && playerCamera)
                    pauseCanvas.worldCamera = playerCamera.GetComponent<Camera>();
            }

            pauseCanvas.gameObject.SetActive(false);
        }

        if (mainPanel) mainPanel.SetActive(false);
        if (settingsPanel) settingsPanel.SetActive(false);

        // pastikan awalnya cursor terkunci
        SetCursorLocked(true);
    }

    void Update()
    {
        // 🔹 ESC hanya berfungsi untuk membuka Pause (bukan menutup)
        if (Input.GetKeyDown(KeyCode.Escape) && !isPaused)
        {
            Pause();
        }

        if (isPaused && pauseCanvas && playerCamera)
        {
            var t = pauseCanvas.transform;
            t.position = playerCamera.position + playerCamera.forward * distance;
            if (faceCamera)
                t.rotation = Quaternion.LookRotation(playerCamera.forward, playerCamera.up);
        }

        // jika UI pause dimatikan dari luar, otomatis resume
        if (isPaused && pauseCanvas && !pauseCanvas.gameObject.activeSelf)
        {
            ForceResumeCleanup();
        }
    }

    // === UTILS ===
    void SetCursorLocked(bool locked)
    {
        if (locked)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            if (dotCrosshair) dotCrosshair.SetActive(true);
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (dotCrosshair) dotCrosshair.SetActive(false);
        }
    }

    IEnumerator EnforceCursorLockFrames(int frames = 4)
    {
        for (int i = 0; i < frames; i++)
        {
            SetCursorLocked(true);
            yield return null; // paksa di setiap frame
        }
    }

    // === CORE ===
    public void Pause()
    {
        if (isPaused) return;
        isPaused = true;

        prevTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (pauseCanvas) pauseCanvas.gameObject.SetActive(true);
        if (mainPanel) mainPanel.SetActive(true);
        if (settingsPanel) settingsPanel.SetActive(false);

        // atur posisi kalau World Space
        if (pauseCanvas && pauseCanvas.renderMode == RenderMode.WorldSpace && playerCamera)
        {
            var t = pauseCanvas.transform;
            t.position = playerCamera.position + playerCamera.forward * distance;
            if (faceCamera)
                t.rotation = Quaternion.LookRotation(playerCamera.forward, playerCamera.up);
        }

        SetCursorLocked(false); // tampilkan cursor

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

        SetCursorLocked(true); // langsung kunci cursor

        // paksa lock beberapa frame biar gak diubah skrip lain (mis. MapController2)
        if (cursorFixCoroutine != null) StopCoroutine(cursorFixCoroutine);
        cursorFixCoroutine = StartCoroutine(EnforceCursorLockFrames(5));

        foreach (var c in controlsToDisable)
            if (c) c.enabled = true;
    }

    void ForceResumeCleanup()
    {
        isPaused = false;
        Time.timeScale = prevTimeScale <= 0f ? 1f : prevTimeScale;
        AudioListener.pause = false;
        SetCursorLocked(true);
        foreach (var c in controlsToDisable) if (c) c.enabled = true;
    }

    // === UI Buttons ===
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
