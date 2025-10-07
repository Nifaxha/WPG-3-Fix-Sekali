using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;               // NEW: untuk UnityEvent
using UnityEngine.SceneManagement;      // NEW: untuk load scene opsional

[System.Serializable]
public class PhotoLocation
{
    [Header("Location Settings (X,Z coordinates only)")]
    public Vector2 coordinates; // X = World X, Y = World Z
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
    public Canvas monitorCanvas; // Canvas monitor (World Space)
    public Image monitorPhotoDisplay; // Image di dalam monitor canvas (UI merah)
    public Text monitorStatusText; // Status text di monitor (opsional)

    [Header("UI References - FALLBACK ONLY")]
    public Image photoDisplayUI; // UI biasa - HANYA SEBAGAI BACKUP
    public GameObject photoFlashEffect; // Opsional

    [Header("Coordinate System")]
    public MonoBehaviour submarineCoordinatesScript; // Reference ke SubmarineCoordinates

    [Header("Photo Settings")]
    public float photoFlashDuration = 0.2f;
    public float photoDisplayDuration = 3f;

    [Header("Audio Settings")]
    public AudioSource cameraAudioSource;
    public AudioClip cameraShutterSound;

    // Sinyal yang akan dikirim saat foto lokasi baru berhasil diambil
    public static event System.Action<Vector2> OnPhotoTaken;

    // ===================== NEW: Fail/Game Over Settings =====================
    [Header("Fail / Game Over Settings")]
    [Tooltip("Berapa kali mengambil foto default (salah) sebelum Game Over")]
    public int maxDefaultPhotoFails = 3;

    [Tooltip("Nama scene untuk Game Over. Kosongkan jika hanya ingin invoke event.")]
    public string gameOverSceneName = "";

    [Tooltip("Dipanggil saat Game Over (misal: memunculkan UI Game Over)")]
    public UnityEvent onGameOver;

    private int defaultPhotoFailCount = 0;
    // =======================================================================

    // Private variables
    private bool canTakePhoto = true;
    private Vector2 currentPlayerCoordinates;

    void Start()
    {
        InitializePhotoSystem();
    }

    void Update()
    {
        UpdatePlayerCoordinates();
        HandleKeyboardInput();

        // Debug info
        if (Input.GetKeyDown(KeyCode.F1))
        {
            Debug.Log($"Current coordinates: X:{currentPlayerCoordinates.x:F1} Z:{currentPlayerCoordinates.y:F1}");
            Debug.Log($"Can take photo: {canTakePhoto}");
            Debug.Log($"Photo locations count: {photoLocations.Count}");
            Debug.Log($"Default photo fails: {defaultPhotoFailCount}/{maxDefaultPhotoFails}"); // NEW
        }
    }

    void InitializePhotoSystem()
    {
        // PRIORITAS: Monitor display setup
        if (monitorCanvas == null || monitorPhotoDisplay == null)
        {
            Debug.LogError("MONITOR SETUP MISSING!");
            Debug.LogError("Please assign Monitor Canvas and Monitor Photo Display!");
            Debug.LogError("Monitor Canvas should be World Space canvas inside your submarine.");
        }
        else
        {
            Debug.Log("✅ Monitor display system ready!");
            // Hide monitor photo initially
            monitorPhotoDisplay.gameObject.SetActive(false);
            if (monitorStatusText != null)
                monitorStatusText.gameObject.SetActive(false);
        }

        // Hide fallback UI initially (hanya backup)
        if (photoDisplayUI != null)
            photoDisplayUI.gameObject.SetActive(false);

        if (photoFlashEffect != null)
            photoFlashEffect.SetActive(false);

        // Auto-find submarine coordinates if not assigned
        if (submarineCoordinatesScript == null)
        {
            submarineCoordinatesScript = FindObjectOfType(System.Type.GetType("SubmarineCoordinates")) as MonoBehaviour;
            if (submarineCoordinatesScript != null)
                Debug.Log("SubmarineCoordinates found automatically.");
            else
                Debug.LogError("SubmarineCoordinates not found! Please assign it manually.");
        }

        // Auto-find audio source if not assigned
        if (cameraAudioSource == null)
            cameraAudioSource = GetComponent<AudioSource>();

        Debug.Log($"PhotoManager initialized with {photoLocations.Count} photo locations.");
        Debug.Log("Press F1 in play mode for debug info, or E near button to take photo.");

        // NEW: Reset fail counter
        defaultPhotoFailCount = 0;
    }

    void HandleKeyboardInput()
    {
        if (allowKeyboardInput && Input.GetKeyDown(photoKey) && canTakePhoto)
        {
            Debug.Log("Space key pressed - taking photo");
            TakePhoto();
        }
    }

    void UpdatePlayerCoordinates()
    {
        if (submarineCoordinatesScript != null)
        {
            // Ambil koordinat dari SubmarineCoordinates script menggunakan reflection
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

    /// <summary>
    /// Main method to take a photo - can be called from keyboard or button
    /// </summary>
    public void TakePhoto()
    {
        Debug.Log("TakePhoto() called");

        if (!canTakePhoto)
        {
            Debug.Log("Cannot take photo - camera is busy or game over");
            return;
        }

        if (submarineCoordinatesScript == null)
        {
            Debug.LogError("Cannot take photo - SubmarineCoordinates not found!");
            return;
        }

        if (photoDisplayUI == null && monitorPhotoDisplay == null)
        {
            Debug.LogError("Cannot take photo - No display assigned!");
            return;
        }

        Debug.Log("Starting photo sequence...");
        StartCoroutine(PhotoSequence());
    }

    /// <summary>
    /// Alternative method for button calls
    /// </summary>
    public void TakePhotoFromButton()
    {
        Debug.Log("TakePhotoFromButton() called");

        if (!allowButtonInput)
        {
            Debug.Log("Button input is disabled");
            return;
        }

        TakePhoto();
    }

    IEnumerator PhotoSequence()
    {
        canTakePhoto = false;

        Debug.Log($"Taking photo at coordinates: X:{currentPlayerCoordinates.x:F1} Z:{currentPlayerCoordinates.y:F1}");

        // Play camera shutter sound
        PlayCameraSound();

        // Show flash effect
        yield return StartCoroutine(ShowFlashEffect());

        // Check if player is at a photo location
        PhotoLocation foundLocation = CheckPhotoLocation();

        // Display appropriate photo
        if (foundLocation != null)
        {
            Debug.Log($"Displaying location photo: {foundLocation.locationName}");
            DisplayLocationPhoto(foundLocation);
        }
        else
        {
            Debug.Log("Displaying white photo - no location found");
            DisplayWhitePhoto(); // <-- di sini hitung salah + cek Game Over
        }

        // Wait before allowing next photo
        yield return new WaitForSeconds(0.5f);

        // Jika sudah game over, jangan izinkan ambil foto lagi
        if (!IsGameOver())
            canTakePhoto = true;

        Debug.Log("Photo sequence completed");
    }

    void PlayCameraSound()
    {
        if (cameraAudioSource != null && cameraShutterSound != null)
        {
            cameraAudioSource.PlayOneShot(cameraShutterSound);
            Debug.Log("Playing camera sound");
        }
    }

    IEnumerator ShowFlashEffect()
    {
        if (photoFlashEffect != null)
        {
            photoFlashEffect.SetActive(true);
            Debug.Log("Showing flash effect");
            yield return new WaitForSeconds(photoFlashDuration);
            photoFlashEffect.SetActive(false);
        }
    }

    PhotoLocation CheckPhotoLocation()
    {
        Debug.Log("Checking photo locations...");

        foreach (PhotoLocation location in photoLocations)
        {
            if (location.photoSprite == null)
            {
                Debug.LogWarning($"Location {location.locationName} has no photo sprite assigned!");
                continue;
            }

            float distance = Vector2.Distance(currentPlayerCoordinates, location.coordinates);
            Debug.Log($"Distance to {location.locationName}: {distance:F2} (radius: {location.radius})");

            if (distance <= location.radius)
            {
                Debug.Log($"Found photo location: {location.locationName} at distance {distance:F2}");
                return location;
            }
        }

        Debug.Log("No photo location found at current coordinates");
        return null;
    }

    void DisplayLocationPhoto(PhotoLocation location)
    {
        Debug.Log($"Displaying photo for location: {location.locationName}");

        // WAJIB: Tampilkan di monitor canvas (UI merah) - TIDAK ADA FALLBACK
        if (monitorPhotoDisplay != null)
        {
            Debug.Log("🖼 Displaying photo on MONITOR CANVAS (World Space)");
            monitorPhotoDisplay.sprite = location.photoSprite;
            monitorPhotoDisplay.gameObject.SetActive(true);

            // Update status text di monitor jika ada
            if (monitorStatusText != null)
            {
                string status = !location.hasBeenPhotographed ?
                    $"LOCATION DISCOVERED: {location.locationName.ToUpper()}" :
                    $"PHOTOGRAPHED: {location.locationName.ToUpper()}";
                monitorStatusText.text = status;
                monitorStatusText.gameObject.SetActive(true);
            }

            StartCoroutine(HideMonitorPhotoAfterDelay(photoDisplayDuration));

            if (!location.hasBeenPhotographed)
            {
                location.hasBeenPhotographed = true;
                Debug.Log($"New location discovered: {location.locationName}");

                // ================== TAMBAHKAN BARIS INI ==================
                // Kirim sinyal beserta koordinat lokasi yang berhasil difoto
                OnPhotoTaken?.Invoke(location.coordinates);
                // =========================================================
            }
        }
        else
        {
            Debug.LogError("❌ MONITOR PHOTO DISPLAY NOT ASSIGNED!");
            Debug.LogError("Photo cannot be displayed! Please assign Monitor Photo Display in PhotoManager.");

            // EMERGENCY FALLBACK ONLY
            if (photoDisplayUI != null)
            {
                Debug.LogWarning("Using emergency fallback UI (this should not happen in normal use)");
                photoDisplayUI.sprite = location.photoSprite;
                photoDisplayUI.gameObject.SetActive(true);
                StartCoroutine(HidePhotoAfterDelay(photoDisplayDuration));
            }
        }

        if (!location.hasBeenPhotographed)
        {
            location.hasBeenPhotographed = true;
            Debug.Log($"New location discovered: {location.locationName}");
        }
    }

    void DisplayWhitePhoto()
    {
        Debug.Log("Displaying white photo");

        // WAJIB: Tampilkan di monitor canvas (UI merah) - TIDAK ADA FALLBACK
        if (monitorPhotoDisplay != null)
        {
            Debug.Log("🖼 Displaying white photo on MONITOR CANVAS (World Space)");
            monitorPhotoDisplay.sprite = defaultWhitePhoto;
            monitorPhotoDisplay.gameObject.SetActive(true);

            // ===== NEW: Tambahkan hitungan salah + update status =====
            defaultPhotoFailCount = Mathf.Clamp(defaultPhotoFailCount + 1, 0, maxDefaultPhotoFails);

            if (monitorStatusText != null)
            {
                if (defaultPhotoFailCount >= maxDefaultPhotoFails)
                {
                    monitorStatusText.text = $"MISSION FAILED\nWrong Photos: {defaultPhotoFailCount}/{maxDefaultPhotoFails}";
                }
                else
                {
                    monitorStatusText.text = $"NO ANOMALIES DETECTED\nWrong Photos: {defaultPhotoFailCount}/{maxDefaultPhotoFails}";
                }
                monitorStatusText.gameObject.SetActive(true);
            }
            // ========================================================

            StartCoroutine(HideMonitorPhotoAfterDelay(photoDisplayDuration));
        }
        else
        {
            Debug.LogError("❌ MONITOR PHOTO DISPLAY NOT ASSIGNED!");
            Debug.LogError("Photo cannot be displayed! Please assign Monitor Photo Display in PhotoManager.");

            // EMERGENCY FALLBACK ONLY
            if (photoDisplayUI != null)
            {
                Debug.LogWarning("Using emergency fallback UI (this should not happen in normal use)");
                photoDisplayUI.sprite = defaultWhitePhoto;
                photoDisplayUI.gameObject.SetActive(true);
                StartCoroutine(HidePhotoAfterDelay(photoDisplayDuration));
            }

            // Tetap naikkan counter walau fallback
            defaultPhotoFailCount = Mathf.Clamp(defaultPhotoFailCount + 1, 0, maxDefaultPhotoFails);
        }

        // NEW: Cek Game Over setelah menampilkan white photo
        if (defaultPhotoFailCount >= maxDefaultPhotoFails)
        {
            TriggerGameOver();
        }
    }

    IEnumerator HidePhotoAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (photoDisplayUI != null)
        {
            photoDisplayUI.gameObject.SetActive(false);
            Debug.Log("Photo hidden");
        }
    }

    IEnumerator HideMonitorPhotoAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (monitorPhotoDisplay != null)
        {
            monitorPhotoDisplay.gameObject.SetActive(false);
            Debug.Log("Monitor photo hidden");
        }
        if (monitorStatusText != null && !IsGameOver()) // NEW: biar pesan gagal tetap tampil jika game over
        {
            monitorStatusText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Add a photo location programmatically using X,Z coordinates
    /// </summary>
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
        Debug.Log($"Added photo location: {name} at X:{xCoord} Z:{zCoord}");
    }

    /// <summary>
    /// Check if camera is ready to take photo
    /// </summary>
    public bool CanTakePhoto()
    {
        return canTakePhoto;
    }

    /// <summary>
    /// Get current player coordinates
    /// </summary>
    public Vector2 GetPlayerCoordinates()
    {
        return currentPlayerCoordinates;
    }

    // =========================== NEW: Game Over Logic ===========================
    private bool gameOverTriggered = false;

    private bool IsGameOver()
    {
        return gameOverTriggered;
    }

    private void TriggerGameOver()
    {
        if (gameOverTriggered) return;

        gameOverTriggered = true;
        canTakePhoto = false;

        Debug.LogWarning("=== GAME OVER: Too many wrong photos ===");

        // Biarkan status "MISSION FAILED" tetap di layar monitor
        if (monitorStatusText != null)
        {
            monitorStatusText.gameObject.SetActive(true);
        }

        // Invoke event (bisa dihubungkan ke UI Game Over, SFX, dsb)
        onGameOver?.Invoke();

        // Opsional: Load scene Game Over jika nama scene diisi
        if (!string.IsNullOrEmpty(gameOverSceneName))
        {
            // Pastikan scene sudah ditambahkan di Build Settings
            SceneManager.LoadScene(gameOverSceneName);
        }

        // Alternatif jika tak pakai scene:
        // Time.timeScale = 0f; // pause total (opsional)
    }
    // ============================================================================
}
