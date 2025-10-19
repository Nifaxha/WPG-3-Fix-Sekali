using UnityEngine;

public class CoordinateButton3D_Interact : MonoBehaviour
{
    [Header("Button Settings")]
    public string direction;   // "Forward", "Backward", "Left", "Right"
    public float speed = 15f;
    public float pressDepth = 0.05f; // seberapa dalam tombol masuk

    [Header("Audio Settings")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonPressSound;
    public AudioClip buttonReleaseSound; // Optional
    public bool playAudioOnPress = true;
    public bool playAudioOnRelease = false;

    private SubmarineCoordinates coordSystem;
    private Vector3 originalPos;
    private bool isHeld = false;
    [Header("Highlight (optional)")]
    public Renderer highlightRenderer;
    public Color highlightColor = new Color(1f, 0.85f, 0.2f);
    private Color _normalColor;
    private bool _hasHighlightRenderer;

    void Awake()
    {
        if (!highlightRenderer)
            highlightRenderer = GetComponentInChildren<Renderer>();

        if (highlightRenderer)
        {
            _normalColor = highlightRenderer.material.color;
            _hasHighlightRenderer = true;
        }
    }

    void Start()
    {
        coordSystem = FindObjectOfType<SubmarineCoordinates>();
        originalPos = transform.localPosition;

        // Auto-setup audio source if not assigned
        if (buttonAudioSource == null)
        {
            buttonAudioSource = GetComponent<AudioSource>();
            if (buttonAudioSource == null)
            {
                Debug.LogWarning($"No AudioSource found on {direction} button! Adding AudioSource component.");
                buttonAudioSource = gameObject.AddComponent<AudioSource>();
                buttonAudioSource.playOnAwake = false;
                buttonAudioSource.loop = false;
                buttonAudioSource.spatialBlend = 1f; // 3D sound
                buttonAudioSource.volume = 0.5f;
            }
        }

        if (coordSystem == null)
            Debug.LogError("SubmarineCoordinates not found!");
    }

    void Update()
    {
        if (isHeld && coordSystem != null)
        {
            // NEW: blok jika lever sedang STOP
            if (coordSystem.ControlsLocked) return;

            coordSystem.ChangeCoordinate(direction, speed);
        }
    }

    public void PressButton()
    {
        if (isHeld) return;

        // NEW: blok klik saat lever STOP
        if (coordSystem != null && coordSystem.ControlsLocked) return;

        isHeld = true;
        AnimateButton(true);

        // Play button press sound
        if (playAudioOnPress && buttonAudioSource != null && buttonPressSound != null)
        {
            buttonAudioSource.PlayOneShot(buttonPressSound);
            Debug.Log($"{direction} button pressed - playing sound");
        }
    }

    public void ReleaseButton()
    {
        isHeld = false;
        AnimateButton(false);

        // Play button release sound (optional)
        if (playAudioOnRelease && buttonAudioSource != null && buttonReleaseSound != null)
        {
            buttonAudioSource.PlayOneShot(buttonReleaseSound);
            Debug.Log($"{direction} button released - playing sound");
        }
    }

    private void AnimateButton(bool pressed)
    {
        transform.localPosition = pressed
            ? originalPos + new Vector3(0, -pressDepth, 0)
            : originalPos;
    }

    public void SetHighlight(bool on)
    {
        if (_hasHighlightRenderer)
            highlightRenderer.material.color = on ? highlightColor : _normalColor;
    }
}