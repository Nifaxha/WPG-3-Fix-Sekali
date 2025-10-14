using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

[System.Serializable]
public class PhotoLocation
{
    public int id;
    [Header("Location Settings (X,Z coordinates only)")]
    public Vector2 coordinates;
    public float radius = 5f;
    public string locationName = "Unknown Location";

    [Header("Photo Data")]
    public Sprite photoSprite;
    public bool hasBeenPhotographed = false;
}

public class PhotoManager : MonoBehaviour
{
    [Header("Photo Locations")]
    public List<PhotoLocation> photoLocations = new List<PhotoLocation>();

    [Header("Default Photo")]
    public Sprite defaultWhitePhoto;

    [Header("Input Settings")]
    public KeyCode photoKey = KeyCode.Space;
    public bool allowKeyboardInput = true;
    public bool allowButtonInput = true;

    [Header("Monitor Integration - PRIORITAS UTAMA")]
    public Canvas monitorCanvas;    
    public Image monitorPhotoDisplay;
    public Text monitorStatusText;

    [Header("UI References - FALLBACK ONLY")]
    public Image photoDisplayUI;
    public GameObject photoFlashEffect;

    [Header("Coordinate System")]
    public MonoBehaviour submarineCoordinatesScript;

    [Header("Photo Settings")]
    public float photoFlashDuration = 0.2f;
    public float photoDisplayDuration = 3f;

    [Header("Audio Settings")]
    public AudioSource cameraAudioSource;
    public AudioClip cameraShutterSound;
    // Sinyal yang akan dikirim saat foto lokasi baru berhasil diambil
    public static event System.Action<Vector2> OnPhotoTaken;
    public static event System.Action<int> OnPhotoTakenById;

    // ===================== FAIL / GAME OVER =====================
    [Header("Fail / Game Over Settings")]
    [Tooltip("Berapa kali mengambil foto default (salah) sebelum Game Over")]
    public int maxDefaultPhotoFails = 3;

    [Tooltip("Nama scene untuk Game Over. Kosongkan jika hanya ingin invoke event.")]
    public string gameOverSceneName = "";

    [Tooltip("Dipanggil saat Game Over (misal: memunculkan UI Game Over)")]
    public UnityEvent onGameOver;

    private int defaultPhotoFailCount = 0;
    private bool gameOverTriggered = false;

    // ===================== NEW: HEALTH UI ======================
    [Header("Health Settings (UI)")]
    [Tooltip("Jumlah health maksimum (disarankan sama dengan Max Default Photo Fails)")]
    public int maxHealth = 3; // NEW
    private int currentHealth; // NEW

    [Tooltip("Susunan ikon hati dari kiri ke kanan")]
    public Image[] healthIcons; // NEW
    public Sprite heartFull;    // NEW
    public Sprite heartEmpty;   // NEW
    // ===========================================================

    // Private variables
    private bool canTakePhoto = true;
    private Vector2 currentPlayerCoordinates;

    void Start()
    {
        // NEW: beri id berurutan agar konsisten dengan urutan list
        for (int i = 0; i < photoLocations.Count; i++)
            photoLocations[i].id = i;

        InitializePhotoSystem();
    }

    void Update()
    {
        UpdatePlayerCoordinates();
        HandleKeyboardInput();

        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"Current coordinates: X:{currentPlayerCoordinates.x:F1} Z:{currentPlayerCoordinates.y:F1}");
            Debug.Log($"Can take photo: {canTakePhoto}");
            Debug.Log($"Photo locations count: {photoLocations.Count}");
            Debug.Log($"Default photo fails: {defaultPhotoFailCount}/{maxDefaultPhotoFails}");
            Debug.Log($"Health: {currentHealth}/{maxHealth}");
        }
    }

    void InitializePhotoSystem()
    {
        if (monitorCanvas == null || monitorPhotoDisplay == null)
        {
            Debug.LogError("MONITOR SETUP MISSING! Assign Monitor Canvas & Monitor Photo Display.");
        }
        else
        {
            monitorPhotoDisplay.gameObject.SetActive(false);
            if (monitorStatusText != null) monitorStatusText.gameObject.SetActive(false);
        }

        if (photoDisplayUI != null) photoDisplayUI.gameObject.SetActive(false);
        if (photoFlashEffect != null) photoFlashEffect.SetActive(false);

        if (submarineCoordinatesScript == null)
        {
            submarineCoordinatesScript = FindObjectOfType(System.Type.GetType("SubmarineCoordinates")) as MonoBehaviour;
            if (submarineCoordinatesScript == null)
                Debug.LogError("SubmarineCoordinates not found! Please assign manually.");
        }

        if (cameraAudioSource == null)
            cameraAudioSource = GetComponent<AudioSource>();

        // Reset counters
        defaultPhotoFailCount = 0;

        // ===== NEW: sync health dengan fail counter =====
        // Disarankan maxHealth == maxDefaultPhotoFails agar konsisten
        if (maxHealth != maxDefaultPhotoFails)
        {
            Debug.LogWarning($"[PhotoManager] maxHealth ({maxHealth}) != maxDefaultPhotoFails ({maxDefaultPhotoFails}). " +
                             $"Menyamakan keduanya untuk konsistensi.");
            maxHealth = maxDefaultPhotoFails;
        }
        currentHealth = maxHealth;
        UpdateHealthUI();
        // =================================================

        Debug.Log($"PhotoManager initialized. Locations: {photoLocations.Count}");
    }

    void HandleKeyboardInput()
    {
        if (allowKeyboardInput && Input.GetKeyDown(photoKey) && canTakePhoto)
        {
            TakePhoto();
        }
    }

    void UpdatePlayerCoordinates()
    {
        if (submarineCoordinatesScript != null)
        {
            var currentXField = submarineCoordinatesScript.GetType().GetField("currentX");
            var currentZField = submarineCoordinatesScript.GetType().GetField("currentZ");

            if (currentXField != null && currentZField != null)
            {
                float x = (float)currentXField.GetValue(submarineCoordinatesScript);
                float z = (float)currentZField.GetValue(submarineCoordinatesScript);
                currentPlayerCoordinates = new Vector2(x, z);
            }
        }
    }

    public void TakePhoto()
    {
        if (!canTakePhoto) return;
        if (submarineCoordinatesScript == null) { Debug.LogError("No SubmarineCoordinates!"); return; }
        if (photoDisplayUI == null && monitorPhotoDisplay == null) { Debug.LogError("No display assigned!"); return; }

        StartCoroutine(PhotoSequence());
    }

    public void TakePhotoFromButton()
    {
        if (!allowButtonInput) return;
        TakePhoto();
    }

    IEnumerator PhotoSequence()
    {
        canTakePhoto = false;

        PlayCameraSound();
        yield return StartCoroutine(ShowFlashEffect());

        PhotoLocation foundLocation = CheckPhotoLocation();

        if (foundLocation != null)
            DisplayLocationPhoto(foundLocation);
        else
            DisplayWhitePhoto(); // salah → kurangi health + cek game over

        yield return new WaitForSeconds(0.5f);

        if (!IsGameOver())
            canTakePhoto = true;
    }

    void PlayCameraSound()
    {
        if (cameraAudioSource != null && cameraShutterSound != null)
            cameraAudioSource.PlayOneShot(cameraShutterSound);
    }

    IEnumerator ShowFlashEffect()
    {
        if (photoFlashEffect != null)
        {
            photoFlashEffect.SetActive(true);
            yield return new WaitForSeconds(photoFlashDuration);
            photoFlashEffect.SetActive(false);
        }
    }

    PhotoLocation CheckPhotoLocation()
    {
        foreach (PhotoLocation location in photoLocations)
        {
            if (location.photoSprite == null) continue;

            float distance = Vector2.Distance(currentPlayerCoordinates, location.coordinates);
            if (distance <= location.radius) return location;
        }
        return null;
    }

    void DisplayLocationPhoto(PhotoLocation location)
    {
        if (monitorPhotoDisplay != null)
        {
            monitorPhotoDisplay.sprite = location.photoSprite;
            monitorPhotoDisplay.gameObject.SetActive(true);

            if (monitorStatusText != null)
            {
                string status = !location.hasBeenPhotographed
                    ? $"LOCATION DISCOVERED: {location.locationName.ToUpper()}"
                    : $"PHOTOGRAPHED: {location.locationName.ToUpper()}";
                monitorStatusText.text = status;
                monitorStatusText.gameObject.SetActive(true);
            }
            if (!location.hasBeenPhotographed)
            {
                location.hasBeenPhotographed = true;
                Debug.Log($"New location discovered: {location.locationName}");

                // Sudah ada:
                OnPhotoTaken?.Invoke(location.coordinates);

                // NEW: cocokkan by ID (anti mismatch)
                OnPhotoTakenById?.Invoke(location.id);
            }
            StartCoroutine(HideMonitorPhotoAfterDelay(photoDisplayDuration));
        }
        else if (photoDisplayUI != null)
        {
            photoDisplayUI.sprite = location.photoSprite;
            photoDisplayUI.gameObject.SetActive(true);
            StartCoroutine(HidePhotoAfterDelay(photoDisplayDuration));
        }

        if (!location.hasBeenPhotographed) location.hasBeenPhotographed = true;
    }

    void DisplayWhitePhoto()
    {
        if (monitorPhotoDisplay != null)
        {
            monitorPhotoDisplay.sprite = defaultWhitePhoto;
            monitorPhotoDisplay.gameObject.SetActive(true);
            StartCoroutine(HideMonitorPhotoAfterDelay(photoDisplayDuration));
        }
        else if (photoDisplayUI != null)
        {
            photoDisplayUI.sprite = defaultWhitePhoto;
            photoDisplayUI.gameObject.SetActive(true);
            StartCoroutine(HidePhotoAfterDelay(photoDisplayDuration));
        }

        // ======= LOGIKA SALAH FOTO =======
        defaultPhotoFailCount = Mathf.Clamp(defaultPhotoFailCount + 1, 0, maxDefaultPhotoFails);

        // Kurangi health 1 step agar sinkron dengan fail counter
        currentHealth = Mathf.Clamp(maxHealth - defaultPhotoFailCount, 0, maxHealth);
        UpdateHealthUI();

        if (monitorStatusText != null)
        {
            if (currentHealth <= 0)
                monitorStatusText.text = $"MISSION FAILED\nWrong Photos: {defaultPhotoFailCount}/{maxDefaultPhotoFails}";
            else
                monitorStatusText.text = $"NO ANOMALIES DETECTED\nWrong Photos: {defaultPhotoFailCount}/{maxDefaultPhotoFails}";
            monitorStatusText.gameObject.SetActive(true);
        }

        if (currentHealth <= 0 || defaultPhotoFailCount >= maxDefaultPhotoFails)
            TriggerGameOver();
        // =================================
    }

    IEnumerator HidePhotoAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (photoDisplayUI != null) photoDisplayUI.gameObject.SetActive(false);
    }

    IEnumerator HideMonitorPhotoAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (monitorPhotoDisplay != null) monitorPhotoDisplay.gameObject.SetActive(false);
        if (monitorStatusText != null && !IsGameOver()) monitorStatusText.gameObject.SetActive(false);
    }

    public void AddPhotoLocation(float xCoord, float zCoord, Sprite sprite, string name, float radius = 5f)
    {
        PhotoLocation newLocation = new PhotoLocation
        {
            coordinates = new Vector2(xCoord, zCoord),
            photoSprite = sprite,
            locationName = name,
            radius = radius,
            hasBeenPhotographed = false
        };
        photoLocations.Add(newLocation);
    }

    public bool CanTakePhoto() => canTakePhoto;
    public Vector2 GetPlayerCoordinates() => currentPlayerCoordinates;

    private bool IsGameOver() => gameOverTriggered;

    private void TriggerGameOver()
    {
        if (gameOverTriggered) return;
        gameOverTriggered = true;
        canTakePhoto = false;

        if (monitorStatusText != null) monitorStatusText.gameObject.SetActive(true);

        onGameOver?.Invoke();

        if (!string.IsNullOrEmpty(gameOverSceneName))
            SceneManager.LoadScene(gameOverSceneName);
        // else: Time.timeScale = 0f; // jika ingin pause
    }

    // ===================== NEW: HEALTH UI HELPER ======================
    private void UpdateHealthUI()
    {
        if (healthIcons == null || healthIcons.Length == 0) return;

        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] == null) continue;

            // i < currentHealth → full, sisanya empty
            bool full = i < currentHealth;

            if (heartFull != null && heartEmpty != null)
            {
                healthIcons[i].sprite = full ? heartFull : heartEmpty;
                healthIcons[i].enabled = true; // pastikan terlihat
            }
            else
            {
                // Fallback: nonaktifkan ikon jika "kosong"
                healthIcons[i].enabled = full;
            }
        }
    }
    // ================================================================
}
