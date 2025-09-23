using UnityEngine;
using UnityEngine.Events;

public class PhotoButton3D_Interact : MonoBehaviour
{
    [Header("Button Settings")]
    public float pressDepth = 0.05f; // seberapa dalam tombol masuk
    public float interactDistance = 3f; // jarak maksimum untuk interaksi
    public KeyCode interactKey = KeyCode.E; // Ubah ke E untuk tombol monitor

    [Header("Visual Feedback")]
    public GameObject interactPrompt; // UI prompt "Press E to take photo"
    public Color highlightColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Audio")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonPressSound;

    [Header("Events")]
    public UnityEvent OnButtonPressed; // Event yang akan dipanggil saat tombol ditekan

    private Vector3 originalPos;
    private bool isPressed = false;
    private bool playerInRange = false;
    private Transform player;
    private Renderer buttonRenderer;
    private Color originalButtonColor;

    void Start()
    {
        originalPos = transform.localPosition;

        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        // Get button renderer for highlight effect
        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
            originalButtonColor = buttonRenderer.material.color;

        // Hide interact prompt initially
        if (interactPrompt != null)
            interactPrompt.SetActive(false);
    }

    void Update()
    {
        CheckPlayerDistance();
        HandleInteraction();
    }

    void CheckPlayerDistance()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool wasInRange = playerInRange;
        playerInRange = distance <= interactDistance;

        // Show/hide interact prompt
        if (playerInRange != wasInRange)
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(playerInRange);

            // Change button color for visual feedback
            if (buttonRenderer != null)
            {
                buttonRenderer.material.color = playerInRange ? highlightColor : originalButtonColor;
            }
        }
    }

    void HandleInteraction()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            PressButton();
        }

        if (Input.GetKeyUp(interactKey))
        {
            ReleaseButton();
        }
    }

    public void PressButton()
    {
        if (isPressed) return;

        isPressed = true;
        AnimateButton(true);

        // Play button press sound
        if (buttonAudioSource != null && buttonPressSound != null)
        {
            buttonAudioSource.PlayOneShot(buttonPressSound);
        }

        // Invoke the event (ini akan memanggil method yang di-assign di inspector)
        OnButtonPressed?.Invoke();
    }

    public void ReleaseButton()
    {
        if (!isPressed) return;

        isPressed = false;
        AnimateButton(false);
    }

    private void AnimateButton(bool pressed)
    {
        transform.localPosition = pressed
            ? originalPos + new Vector3(0, -pressDepth, 0)
            : originalPos;
    }

    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}