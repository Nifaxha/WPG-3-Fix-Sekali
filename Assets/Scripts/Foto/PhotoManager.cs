using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;
using System;

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

    // --- WRONG FEEDBACK (sudah ada) ---
    [Tooltip("Suara yang diputar ketika salah mengambil foto")]
    public AudioClip wrongPhotoSound;

    [Tooltip("Index 0 = salah ke-1, 1 = salah ke-2, 2 = salah ke-3, dst. Jika kosong, pakai fallback.")]
    public AudioClip[] wrongPhotoSounds = new AudioClip[3];

    [Tooltip("AudioSource untuk SFX (jika kosong, akan pakai cameraAudioSource)")]
    public AudioSource sfxAudioSource;

    // === SUCCESS FEEDBACK (BARU & FLEKSIBEL) ===
    [Header("Success Feedback (Audio + Subtitle)")]
    [Tooltip("Kumpulan klip audio untuk foto sukses. Kamu bebas tambah/kurangi di Inspector.")]
    public AudioClip[] successPhotoSounds;

    public enum SuccessSoundMode { First, Random, Sequence, AllAtOnce }
    [Tooltip("Cara memutar klip sukses: First (pakai klip pertama), Random (acak), Sequence (bergiliran), AllAtOnce (tumpuk semua).")]
    public SuccessSoundMode successSoundMode = SuccessSoundMode.Random;

    [Range(0f, 1f)] public float successVolume = 1f;
    [Tooltip("Rentang pitch acak (min,max). Set (1,1) untuk pitch normal.")]
    public Vector2 successPitchRange = new Vector2(1f, 1f);
    [Tooltip("Jeda antar-klip untuk mode Sequence (detik).")]
    public float successSequenceDelay = 0.05f;

    private int _successSeqIndex = 0;   // penunjuk giliran untuk mode Sequence
    private Coroutine _successAudioRoutine;

    // >>> NEW: Subtitle modes <<<
    public enum SuccessSubtitleMode { First, Random, Sequence, AllQueued }
    [Tooltip("Cara memilih subtitle sukses: First/Random/Sequence/AllQueued (tampilkan semua berurutan).")]
    public SuccessSubtitleMode successSubtitleMode = SuccessSubtitleMode.Random;

    private int _successSubtitleSeqIndex = 0;   // pointer untuk mode Sequence
    private Coroutine _successSubtitleRoutine;  // coroutine untuk AllQueued

    [System.Serializable]
    public struct SubtitleLine
    {
        [TextArea(1, 3)] public string text;
        [Tooltip("Durasi tampil (detik) untuk subtitle ini.")]
        public float duration;
    }

    [Tooltip("Daftar subtitle untuk foto sukses (gunakan {location} untuk nama lokasi). Kalau kosong akan pakai defaultSuccessSubtitle.")]
    public SubtitleLine[] successSubtitles;

    [Tooltip("Fallback jika array di atas kosong.")]
    [TextArea(1, 3)] public string defaultSuccessSubtitle = "Target captured successfully: {location}";

    // === EVENTS ===
    public static event System.Action<Vector2> OnPhotoTaken;
    public static event System.Action<int> OnPhotoTakenById;
    public event Action<int> OnPhotoCaptured;

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

    [Header("Mission Complete Settings")]
    [Tooltip("Nama scene yang akan dimuat jika semua photo location sudah berhasil diambil.")]
    public string allPhotosCompleteSceneName = "WinScene";

    [Tooltip("Delay sebelum pindah ke scene setelah semua foto berhasil diambil.")]
    public float delayBeforeWinScene = 2f;

    // ===================== HEALTH UI ======================
    [Header("Health Settings (UI)")]
    [Tooltip("Jumlah health maksimum (disarankan sama dengan Max Default Photo Fails)")]
    public bool useHealthUI = false;
    public int maxHealth = 3;
    private int currentHealth;

    [Tooltip("Susunan ikon hati dari kiri ke kanan")]
    public Image[] healthIcons;
    public Sprite heartFull;
    public Sprite heartEmpty;
    // =====================================================

    // Private variables
    private bool canTakePhoto = true;
    private Vector2 currentPlayerCoordinates;

    // ===================== SUBTITLE (sudah ada) ======================
    [Header("Subtitle Settings")]
    public TMP_Text subtitleTMP;
    public Text subtitleUI;
    [Tooltip("Durasi default subtitle bila per-baris tidak diisi (>0).")]
    public float defaultSubtitleDuration = 2.5f;
    [Tooltip("Kalimat fallback bila tidak ada data.")]
    [TextArea(1, 3)] public string wrongSubtitleFallback = "Bukan target. Ulangi pemotretan.";

    [Tooltip("Daftar subtitle untuk setiap kesalahan (berurutan).")]
    public SubtitleLine[] wrongSubtitles;

    private Coroutine subtitleRoutine;
    // ====================================================

    void MarkPhotoCaptured(int index)
    {
        photoLocations[index].hasBeenPhotographed = true;
        OnPhotoCaptured?.Invoke(index);
    }

    void Awake()
    {
        for (int i = 0; i < photoLocations.Count; i++)
            photoLocations[i].id = i;
    }

    void Start()
    {
        InitializePhotoSystem();
        if (AreAllLocationsPhotographed() && !IsGameOver())
            StartCoroutine(LoadGameOverAfter(0f));

        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);
        if (subtitleUI != null) subtitleUI.gameObject.SetActive(false);
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

        if (sfxAudioSource == null)
            sfxAudioSource = cameraAudioSource;

        defaultPhotoFailCount = 0;

        if (maxHealth != maxDefaultPhotoFails)
        {
            Debug.LogWarning($"[PhotoManager] maxHealth ({maxHealth}) != maxDefaultPhotoFails ({maxDefaultPhotoFails}). Menyamakan keduanya untuk konsistensi.");
            maxHealth = maxDefaultPhotoFails;
        }
        currentHealth = maxHealth;
        UpdateHealthUI();

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

    private AudioClip GetWrongSfxClip(int failNumber)
    {
        if (wrongPhotoSounds != null && wrongPhotoSounds.Length > 0)
        {
            int idx = Mathf.Clamp(failNumber - 1, 0, wrongPhotoSounds.Length - 1);
            var clip = wrongPhotoSounds[idx];
            if (clip != null) return clip;
        }
        return wrongPhotoSound;
    }

    private void PlayWrongSfx(int failNumber)
    {
        if (sfxAudioSource == null) sfxAudioSource = cameraAudioSource;

        var clip = GetWrongSfxClip(failNumber);
        if (sfxAudioSource != null && clip != null)
        {
            sfxAudioSource.PlayOneShot(clip);
            Debug.Log($"[PhotoManager] Wrong SFX -> fail #{failNumber}, clip: {clip?.name}");
        }
        else
        {
            Debug.LogWarning("[PhotoManager] SFX source/clip belum di-assign.");
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
            DisplayWhitePhoto();

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

    // Semua lokasi (yang punya sprite) sudah difoto?
    private bool AreAllLocationsPhotographed()
    {
        if (photoLocations == null || photoLocations.Count == 0) return false;

        foreach (var loc in photoLocations)
        {
            if (loc == null) continue;
            if (loc.photoSprite == null) continue; // slot kosong tidak dihitung
            if (!loc.hasBeenPhotographed) return false;
        }
        return true;
    }

    private IEnumerator LoadAllPhotosCompleteSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!string.IsNullOrEmpty(allPhotosCompleteSceneName))
        {
            Debug.Log($"[PhotoManager] Semua foto berhasil diambil! Pindah ke scene: {allPhotosCompleteSceneName}");
            SceneManager.LoadScene(allPhotosCompleteSceneName);
        }
        else
        {
            Debug.LogWarning("[PhotoManager] allPhotosCompleteSceneName belum diisi di Inspector!");
        }
    }

    private IEnumerator LoadGameOverAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        TriggerGameOver();
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

            // Mainkan feedback & event hanya saat PERTAMA KALI lokasi ini difoto
            if (!location.hasBeenPhotographed)
            {
                location.hasBeenPhotographed = true;
                Debug.Log($"New location discovered: {location.locationName}");

                // Event untuk subsistem lain (radar, dsb.)
                OnPhotoTaken?.Invoke(location.coordinates);
                OnPhotoTakenById?.Invoke(location.id);
                OnPhotoCaptured?.Invoke(location.id);

                // === SUCCESS FEEDBACK: audio fleksibel + subtitle ===
                PlaySuccessFeedback(location.locationName);
            }

            if (AreAllLocationsPhotographed() && !IsGameOver())
            {
                Debug.Log("[PhotoManager] Semua lokasi berhasil difoto. Menuju ke scene kemenangan...");
                StartCoroutine(LoadAllPhotosCompleteSceneAfterDelay(delayBeforeWinScene));
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

    // === SUCCESS FEEDBACK: helper ===
    private void PlaySuccessFeedback(string locationName)
    {
        if (_successAudioRoutine != null)
            StopCoroutine(_successAudioRoutine);

        // 1) Audio sukses sesuai mode
        _successAudioRoutine = StartCoroutine(PlaySuccessSoundsRoutine());

        // 2) Subtitle sukses
        if (_successSubtitleRoutine != null)
        {
            StopCoroutine(_successSubtitleRoutine);
            _successSubtitleRoutine = null;
        }

        // Tidak ada data → fallback
        if (successSubtitles == null || successSubtitles.Length == 0)
        {
            string fb = defaultSuccessSubtitle.Replace("{location}", locationName);
            ShowSubtitle(fb, defaultSubtitleDuration);
            return;
        }

        // Mode ALL QUEUED → tampilkan semua berurutan (tiap item pakai durasinya)
        if (successSubtitleMode == SuccessSubtitleMode.AllQueued)
        {
            _successSubtitleRoutine = StartCoroutine(PlaySuccessSubtitlesQueued(locationName));
            return;
        }
        // Pilih satu baris sesuai mode
        int idx = 0;
        switch (successSubtitleMode)
        {
            case SuccessSubtitleMode.First:
                idx = 0;
                break;

            case SuccessSubtitleMode.Random:
                idx = UnityEngine.Random.Range(0, successSubtitles.Length);
                break;

            case SuccessSubtitleMode.Sequence:
                idx = _successSubtitleSeqIndex % successSubtitles.Length;
                _successSubtitleSeqIndex = (idx + 1) % successSubtitles.Length;
                break;
        }

        var line = successSubtitles[idx];
        string text = string.IsNullOrWhiteSpace(line.text) ? defaultSuccessSubtitle : line.text;
        float dur = (line.duration > 0f) ? line.duration : defaultSubtitleDuration;

        text = text.Replace("{location}", locationName);
        ShowSubtitle(text, dur);
    }

    private IEnumerator PlaySuccessSubtitlesQueued(string locationName)
    {
        // Tampilkan semua item successSubtitles secara berurutan
        for (int i = 0; i < successSubtitles.Length; i++)
        {
            var line = successSubtitles[i];
            string txt = string.IsNullOrWhiteSpace(line.text) ? defaultSuccessSubtitle : line.text;
            float dur = (line.duration > 0f) ? line.duration : defaultSubtitleDuration;

            txt = txt.Replace("{location}", locationName);
            ShowSubtitle(txt, dur);

            // Tunggu durasi baris ini sebelum lanjut baris berikutnya
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        _successSubtitleRoutine = null;
    }


    private IEnumerator PlaySuccessSoundsRoutine()
    {
        if (sfxAudioSource == null) sfxAudioSource = cameraAudioSource;

        if (successPhotoSounds == null || successPhotoSounds.Length == 0 || sfxAudioSource == null)
            yield break;

        // Helper untuk pitch acak
        float pitch = Mathf.Clamp(UnityEngine.Random.Range(successPitchRange.x, successPitchRange.y), 0.1f, 3f);

        switch (successSoundMode)
        {
            case SuccessSoundMode.First:
                {
                    var clip = successPhotoSounds[0];
                    if (clip != null)
                    {
                        float oldVol = sfxAudioSource.volume;
                        float oldPitch = sfxAudioSource.pitch;
                        sfxAudioSource.pitch = pitch;
                        sfxAudioSource.PlayOneShot(clip, successVolume);
                        sfxAudioSource.pitch = oldPitch;
                        sfxAudioSource.volume = oldVol;
                    }
                    break;
                }
            case SuccessSoundMode.Random:
                {
                    int tries = 0;
                    AudioClip clip = null;
                    // cari clip valid (tidak null), maksimum 8 percobaan
                    while (tries < 8 && (clip == null))
                    {
                        var idx = UnityEngine.Random.Range(0, successPhotoSounds.Length);
                        clip = successPhotoSounds[idx];
                        tries++;
                    }
                    if (clip != null)
                    {
                        float oldVol = sfxAudioSource.volume;
                        float oldPitch = sfxAudioSource.pitch;
                        sfxAudioSource.pitch = pitch;
                        sfxAudioSource.PlayOneShot(clip, successVolume);
                        sfxAudioSource.pitch = oldPitch;
                        sfxAudioSource.volume = oldVol;
                    }
                    break;
                }
            case SuccessSoundMode.Sequence:
                {
                    // mainkan SATU klip sesuai giliran (bukan semua); giliran bergeser tiap sukses
                    int safeLen = successPhotoSounds.Length;
                    for (int k = 0; k < safeLen; k++)
                    {
                        int idx = (_successSeqIndex + k) % safeLen;
                        var clip = successPhotoSounds[idx];
                        if (clip != null)
                        {
                            float oldVol = sfxAudioSource.volume;
                            float oldPitch = sfxAudioSource.pitch;
                            sfxAudioSource.pitch = pitch;
                            sfxAudioSource.PlayOneShot(clip, successVolume);
                            sfxAudioSource.pitch = oldPitch;
                            sfxAudioSource.volume = oldVol;

                            _successSeqIndex = (idx + 1) % safeLen; // geser giliran
                            break;
                        }
                    }
                    break;
                }
            case SuccessSoundMode.AllAtOnce:
                {
                    // tumpuk semua klip valid sekaligus (layering)
                    foreach (var clip in successPhotoSounds)
                    {
                        if (clip == null) continue;
                        float oldVol = sfxAudioSource.volume;
                        float oldPitch = sfxAudioSource.pitch;
                        sfxAudioSource.pitch = pitch;
                        sfxAudioSource.PlayOneShot(clip, successVolume);
                        sfxAudioSource.pitch = oldPitch;
                        sfxAudioSource.volume = oldVol;

                        if (successSequenceDelay > 0f)
                            yield return new WaitForSeconds(successSequenceDelay);
                    }
                    break;
                }
        }
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

        // ====== PILIH CLIP BERDASARKAN SALAH KE-BERAPA ======
        int nextFail = Mathf.Min(defaultPhotoFailCount + 1, maxDefaultPhotoFails);

        // Ambil clip sesuai urutan; kalau tidak ada → fallback
        AudioClip clipToPlay = null;
        if (wrongPhotoSounds != null && wrongPhotoSounds.Length > 0)
        {
            int idx = Mathf.Clamp(nextFail - 1, 0, wrongPhotoSounds.Length - 1);
            clipToPlay = wrongPhotoSounds[idx];
        }
        if (clipToPlay == null) clipToPlay = wrongPhotoSound;

        // === MAINKAN SUARA SALAH SESUAI URUTAN ===
        PlayWrongSfx(nextFail);
        var (msg, dur) = GetWrongSubtitle(nextFail);
        ShowSubtitle(msg, dur);

        // ====== NAIKKAN COUNTER & UPDATE UI ======
        defaultPhotoFailCount = nextFail;

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

        // ====== GAME OVER? TUNGGU SFX TERAKHIR DULU ======
        if (defaultPhotoFailCount >= maxDefaultPhotoFails)
        {
            var lastClip = GetWrongSfxClip(defaultPhotoFailCount);
            StartCoroutine(GameOverAfterSfx(lastClip));
        }
    }

    IEnumerator GameOverAfterSfx(AudioClip clip = null)
    {
        if (gameOverTriggered) yield break;
        gameOverTriggered = true;
        canTakePhoto = false;

        if (monitorStatusText != null)
            monitorStatusText.gameObject.SetActive(true);

        onGameOver?.Invoke();

        if (sfxAudioSource == null) sfxAudioSource = cameraAudioSource;
        AudioClip waitClip = clip != null ? clip : wrongPhotoSound;

        float wait = 0f;
        if (sfxAudioSource != null && waitClip != null)
        {
            if (!sfxAudioSource.isPlaying)
                sfxAudioSource.PlayOneShot(waitClip);

            wait = Mathf.Max(wait, waitClip.length);
            float t = 0f;
            while (t < wait && sfxAudioSource != null && sfxAudioSource.isPlaying)
            {
                t += Time.unscaledDeltaTime;   // jangan terpengaruh Time.timeScale
                yield return null;
            }
        }

        if (!string.IsNullOrEmpty(gameOverSceneName))
            SceneManager.LoadScene(gameOverSceneName);
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

    // ===================== HEALTH UI HELPER ======================
    private void UpdateHealthUI()
    {
        if (healthIcons == null || healthIcons.Length == 0) return;

        for (int i = 0; i < healthIcons.Length; i++)
        {
            if (healthIcons[i] == null) continue;

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

    // ===================== SUBTITLE HELPER ======================
    private void ShowSubtitle(string message, float duration = -1f)
    {
        if (string.IsNullOrWhiteSpace(message)) return;

        if (subtitleTMP != null)
        {
            subtitleTMP.text = message;
            subtitleTMP.gameObject.SetActive(true);
        }
        else if (subtitleUI != null)
        {
            subtitleUI.text = message;
            subtitleUI.gameObject.SetActive(true);
        }
        else
        {
            Debug.Log($"[Subtitle] {message}");
            return;
        }

        if (subtitleRoutine != null) StopCoroutine(subtitleRoutine);
        float useDuration = (duration > 0f) ? duration : defaultSubtitleDuration;
        subtitleRoutine = StartCoroutine(HideSubtitleAfter(useDuration));
    }

    private IEnumerator HideSubtitleAfter(float delay)
    {
        float elapsed = 0f;
        while (elapsed < delay)
        {
            if (!AudioListener.pause)
                elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);
        if (subtitleUI != null) subtitleUI.gameObject.SetActive(false);
        subtitleRoutine = null;
    }

    private (string text, float duration) GetWrongSubtitle(int failNumber)
    {
        if (wrongSubtitles != null && wrongSubtitles.Length > 0)
        {
            int idx = Mathf.Clamp(failNumber - 1, 0, wrongSubtitles.Length - 1);
            var line = wrongSubtitles[idx];
            if (!string.IsNullOrWhiteSpace(line.text))
            {
                return (line.text, line.duration > 0 ? line.duration : 2.5f);
            }
        }
        return (wrongSubtitleFallback, 2.5f);
    }
}
