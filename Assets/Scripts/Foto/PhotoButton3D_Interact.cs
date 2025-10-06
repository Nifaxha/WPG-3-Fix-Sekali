using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class PhotoButton3D_Interact : MonoBehaviour
{
    [Header("Button Settings")]
    public float pressDepth = 0.05f;
    public float interactDistance = 3f;
    public KeyCode interactKey = KeyCode.E;

    [Header("Cooldown Settings")]
    [Tooltip("Jeda antar penekanan tombol (dalam detik)")]
    public float buttonCooldown = 3f; // <-- ubah sesuai durasi proses foto
    private bool isOnCooldown = false;

    [Header("Visual Feedback")]
    public GameObject interactPrompt;
    public Color highlightColor = Color.yellow;
    public Color normalColor = Color.white;

    [Header("Audio")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonPressSound;

    [Header("Events")]
    public UnityEvent OnButtonPressed;

    private Vector3 originalPos;
    private bool isPressed = false;
    private bool playerInRange = false;
    private Transform player;
    private Renderer buttonRenderer;
    private Color originalButtonColor;

    void Start()
    {
        originalPos = transform.localPosition;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        buttonRenderer = GetComponent<Renderer>();
        if (buttonRenderer != null)
            originalButtonColor = buttonRenderer.material.color;

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

        if (playerInRange != wasInRange)
        {
            if (interactPrompt != null)
                interactPrompt.SetActive(playerInRange && !isOnCooldown);

            if (buttonRenderer != null)
                buttonRenderer.material.color = playerInRange ? highlightColor : originalButtonColor;
        }
    }

    void HandleInteraction()
    {
        if (!playerInRange || isOnCooldown) return;

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
        if (isPressed || isOnCooldown) return;

        isPressed = true;
        AnimateButton(true);

        if (buttonAudioSource != null && buttonPressSound != null)
            buttonAudioSource.PlayOneShot(buttonPressSound);

        OnButtonPressed?.Invoke();
        StartCoroutine(ButtonCooldownRoutine());
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

    IEnumerator ButtonCooldownRoutine()
    {
        isOnCooldown = true;
        if (interactPrompt != null)
            interactPrompt.SetActive(false);

        yield return new WaitForSeconds(buttonCooldown);

        isOnCooldown = false;
        if (playerInRange && interactPrompt != null)
            interactPrompt.SetActive(true);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}
