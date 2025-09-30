using UnityEngine;

public class StopButton3D_Interact : MonoBehaviour
{
    [Header("Button Settings")]
    public float pressDepth = 0.05f; // seberapa dalam tombol masuk
    public bool emergencyStop = true; // true = instant stop, false = gradual stop

    [Header("Visual Feedback")]
    public Color highlightColor = Color.red; // Warna merah untuk tombol stop
    public Color normalColor = Color.white;

    [Header("Audio")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonPressSound;

    private SubmarineCoordinates coordSystem;
    private Vector3 originalPos;
    private bool isHeld = false;
    private Renderer buttonRenderer;
    private Color originalButtonColor;

    void Start()
    {
        coordSystem = FindObjectOfType<SubmarineCoordinates>();
        originalPos = transform.localPosition;

        // Get button renderer for highlight effect
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
            originalButtonColor = buttonRenderer.material.color;

        // Auto-setup audio source if not assigned
        if (buttonAudioSource == null)
        {
            buttonAudioSource = GetComponent<AudioSource>();
            if (buttonAudioSource == null)
            {
                Debug.LogWarning("No AudioSource found on stop button! Adding AudioSource component.");
                buttonAudioSource = gameObject.AddComponent<AudioSource>();
                buttonAudioSource.playOnAwake = false;
                buttonAudioSource.loop = false;
                buttonAudioSource.spatialBlend = 1f; // 3D sound
            }
        }

        if (coordSystem == null)
            Debug.LogError("SubmarineCoordinates not found! Stop button won't work.");
    }

    void Update()
    {
        // kalau tombol sedang ditekan terus menerus bisa lakukan efek tertentu di sini
    }

    public void PressButton()
    {
        if (isHeld) return;

        isHeld = true;
        AnimateButton(true);

        // Play button press sound
        if (buttonAudioSource != null && buttonPressSound != null)
        {
            buttonAudioSource.PlayOneShot(buttonPressSound);
            Debug.Log("Playing stop button sound");
        }

        // Stop submarine
        if (coordSystem != null)
        {
            if (emergencyStop)
            {
                coordSystem.EmergencyStop();
                Debug.Log("Emergency stop activated!");
            }
            else
            {
                coordSystem.GradualStop();
                Debug.Log("Gradual stop activated!");
            }
        }
        else
        {
            Debug.LogError("Cannot stop - SubmarineCoordinates not found!");
        }
    }

    public void ReleaseButton()
    {
        if (!isHeld) return;

        isHeld = false;
        AnimateButton(false);
    }

    private void AnimateButton(bool pressed)
    {
        transform.localPosition = pressed
            ? originalPos + new Vector3(0, -pressDepth, 0)
            : originalPos;
    }

    // opsional: ubah warna saat di-highlight oleh raycast
    public void SetHighlight(bool highlighted)
    {
        if (buttonRenderer != null)
            buttonRenderer.material.color = highlighted ? highlightColor : originalButtonColor;
    }
}
