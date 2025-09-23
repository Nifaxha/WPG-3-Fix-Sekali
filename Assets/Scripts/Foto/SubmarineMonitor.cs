using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SubmarineMonitor : MonoBehaviour
{
    [Header("Monitor Settings")]
    public RenderTexture monitorRenderTexture;
    public Camera monitorCamera; // Optional: untuk efek CRT/monitor
    public Material monitorMaterial; // Material dari monitor 3D object

    [Header("Display Components")]
    public Canvas monitorCanvas; // Canvas yang ada di dalam monitor
    public Image photoDisplayImage; // Image component untuk foto
    public Text statusText; // Text untuk status/pesan
    public Text coordinateText; // Text untuk koordinat

    [Header("Monitor Effects")]
    public GameObject staticEffect; // Static/noise effect
    public GameObject scanlineEffect; // Scanline effect
    public AudioSource monitorAudioSource;
    public AudioClip displaySound;
    public AudioClip staticSound;

    [Header("Animation Settings")]
    public float displayFadeTime = 0.5f;
    public float staticDuration = 0.3f;

    private bool isDisplayingPhoto = false;
    private Coroutine currentDisplayCoroutine;

    void Start()
    {
        InitializeMonitor();
    }

    void InitializeMonitor()
    {
        // Hide photo initially
        if (photoDisplayImage != null)
            photoDisplayImage.gameObject.SetActive(false);

        // Hide effects initially
        if (staticEffect != null)
            staticEffect.SetActive(false);

        if (scanlineEffect != null)
            scanlineEffect.SetActive(true); // Scanlines always on for CRT effect

        // Setup render texture if using 3D monitor
        if (monitorRenderTexture != null && monitorMaterial != null)
        {
            monitorMaterial.mainTexture = monitorRenderTexture;
        }
    }

    /// <summary>
    /// Display photo on submarine monitor
    /// </summary>
    public void DisplayPhotoOnMonitor(Sprite photoSprite, string message, float displayDuration = 3f)
    {
        if (currentDisplayCoroutine != null)
            StopCoroutine(currentDisplayCoroutine);

        currentDisplayCoroutine = StartCoroutine(DisplayPhotoSequence(photoSprite, message, displayDuration));
    }

    IEnumerator DisplayPhotoSequence(Sprite photoSprite, string message, float displayDuration)
    {
        isDisplayingPhoto = true;

        // Step 1: Show static effect
        yield return StartCoroutine(ShowStaticEffect());

        // Step 2: Display photo with fade in
        yield return StartCoroutine(FadeInPhoto(photoSprite, message));

        // Step 3: Keep photo displayed
        yield return new WaitForSeconds(displayDuration);

        // Step 4: Fade out photo
        yield return StartCoroutine(FadeOutPhoto());

        isDisplayingPhoto = false;
    }

    IEnumerator ShowStaticEffect()
    {
        if (staticEffect != null)
        {
            staticEffect.SetActive(true);

            // Play static sound
            if (monitorAudioSource != null && staticSound != null)
                monitorAudioSource.PlayOneShot(staticSound);

            yield return new WaitForSeconds(staticDuration);
            staticEffect.SetActive(false);
        }
    }

    IEnumerator FadeInPhoto(Sprite photoSprite, string message)
    {
        if (photoDisplayImage == null) yield break;

        // Set photo sprite
        photoDisplayImage.sprite = photoSprite;
        photoDisplayImage.gameObject.SetActive(true);

        // Set message
        if (statusText != null)
        {
            statusText.text = message;
            statusText.gameObject.SetActive(true);
        }

        // Play display sound
        if (monitorAudioSource != null && displaySound != null)
            monitorAudioSource.PlayOneShot(displaySound);

        // Fade in animation
        CanvasGroup canvasGroup = photoDisplayImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = photoDisplayImage.gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        float elapsed = 0f;

        while (elapsed < displayFadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = elapsed / displayFadeTime;
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    IEnumerator FadeOutPhoto()
    {
        if (photoDisplayImage == null) yield break;

        CanvasGroup canvasGroup = photoDisplayImage.GetComponent<CanvasGroup>();
        if (canvasGroup == null) yield break;

        float elapsed = 0f;

        while (elapsed < displayFadeTime)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = 1f - (elapsed / displayFadeTime);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        photoDisplayImage.gameObject.SetActive(false);

        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Update coordinate display on monitor
    /// </summary>
    public void UpdateCoordinateDisplay(float x, float z, float speed)
    {
        if (coordinateText != null)
        {
            coordinateText.text = $"SONAR COORDINATES\nX: {x:F2}\nZ: {z:F2}\nSPEED: {speed:F2} KNOTS";
        }
    }

    /// <summary>
    /// Show message on monitor
    /// </summary>
    public void ShowStatusMessage(string message, float duration = 2f)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.gameObject.SetActive(true);
            StartCoroutine(HideMessageAfterDelay(duration));
        }
    }

    IEnumerator HideMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (statusText != null)
            statusText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Toggle monitor power
    /// </summary>
    public void ToggleMonitor(bool isOn)
    {
        if (monitorCanvas != null)
            monitorCanvas.gameObject.SetActive(isOn);

        if (scanlineEffect != null)
            scanlineEffect.SetActive(isOn);
    }

    /// <summary>
    /// Check if monitor is currently displaying photo
    /// </summary>
    public bool IsDisplayingPhoto()
    {
        return isDisplayingPhoto;
    }

    /// <summary>
    /// Force clear display
    /// </summary>
    public void ClearDisplay()
    {
        if (currentDisplayCoroutine != null)
        {
            StopCoroutine(currentDisplayCoroutine);
            currentDisplayCoroutine = null;
        }

        if (photoDisplayImage != null)
            photoDisplayImage.gameObject.SetActive(false);

        if (statusText != null)
            statusText.gameObject.SetActive(false);

        if (staticEffect != null)
            staticEffect.SetActive(false);

        isDisplayingPhoto = false;
    }
}