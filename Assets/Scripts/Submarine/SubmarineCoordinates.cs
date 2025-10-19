using UnityEngine;
using TMPro;

public class SubmarineCoordinates : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI textX;
    public TextMeshProUGUI textZ;
    public TextMeshProUGUI textSpeed;

    [Header("Current Coordinates")]
    public float currentX = 0f;
    public float currentZ = 0f;

    [Header("Movement Settings")]
    public float currentSpeed = 0f;
    public float acceleration = 1f;
    public float maxSpeed = 15f;
    public bool isBlocked = false;

    [HideInInspector]
    public Vector3 collisionNormal = Vector3.zero; // Normal dari dinding yang ditabrak

    private Rigidbody rb;

    [Header("Braking Settings")]
    public float brakeDeceleration = 5f;
    public bool lockXWhileBraking = true;
    private bool isBraking = false;
    private float brakeTarget = 0f;

    [Header("Audio")]
    public AudioSource movementAudio;
    public AudioClip movementClip;
    public AudioSource idleAudio;
    public AudioClip idleClip;

    private bool isMovingAudioPlaying = false;
    private bool isIdleAudioPlaying = false;

    [Header("Vibration / Shake Settings")]
    public Transform shakeTarget;
    public float shakeIntensity = 0.1f;
    public float shakeDuration = 0.2f;

    private Vector3 originalShakePos;
    private float shakeTimer = 0f;

    public LayerMask wallLayer;
    public float checkRadius = 0.5f;

    [Header("Audio Fade Settings")]
    public float fadeSpeed = 2f;

    // === NEW: Lock controls when lever is in STOP ===
    [Header("Control Lock (by Lever)")]
    public bool controlsLockedByLever = false;

    // Akses baca-only untuk script lain
    public bool ControlsLocked => controlsLockedByLever;

    // Dipanggil tuas untuk kunci/buka kontrol
    public void LockControlsFromLever(bool locked)
    {
        controlsLockedByLever = locked;
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Setup movement audio
        if (movementAudio == null)
        {
            movementAudio = gameObject.AddComponent<AudioSource>();
            movementAudio.playOnAwake = false;
            movementAudio.loop = true;
            movementAudio.spatialBlend = 1f;
            movementAudio.volume = 0.5f;
        }
        if (movementClip != null)
            movementAudio.clip = movementClip;

        // Setup idle audio
        if (idleAudio == null)
        {
            idleAudio = gameObject.AddComponent<AudioSource>();
            idleAudio.playOnAwake = false;
            idleAudio.loop = true;
            idleAudio.spatialBlend = 1f;
            idleAudio.volume = 0.5f;
        }
        if (idleClip != null)
            idleAudio.clip = idleClip;

        if (shakeTarget == null)
            shakeTarget = Camera.main.transform;
        originalShakePos = shakeTarget.localPosition;
    }

    void FixedUpdate()
    {
        // Kalau sedang ngerem, turunkan speed pelan-pelan menuju 0
        if (isBraking)
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, brakeTarget, brakeDeceleration * Time.fixedDeltaTime);
            if (Mathf.Approximately(currentSpeed, brakeTarget))
            {
                currentSpeed = brakeTarget;
                isBraking = false;
            }
        }

        // Posisi maju-mundur pakai currentSpeed
        currentZ += currentSpeed * Time.fixedDeltaTime;
        Vector3 newPosition = new Vector3(currentX, rb.position.y, currentZ);
        rb.MovePosition(newPosition);
    }

    void Update()
    {
        UpdateCoordinateText();
        HandleMovementAndIdleAudio();
        HandleShake();
    }

    private void HandleMovementAndIdleAudio()
    {
        // target volume berdasarkan speed
        float targetIdleVol = (Mathf.Abs(currentSpeed) <= 0.1f) ? 1f : 0f;
        float targetMoveVol = (Mathf.Abs(currentSpeed) > 0.1f) ? 1f : 0f;

        // pastikan audio main (loop)
        if (idleAudio != null && !idleAudio.isPlaying && idleClip != null)
            idleAudio.Play();
        if (movementAudio != null && !movementAudio.isPlaying && movementClip != null)
            movementAudio.Play();

        // fade volume
        if (idleAudio != null)
            idleAudio.volume = Mathf.Lerp(idleAudio.volume, targetIdleVol, Time.deltaTime * fadeSpeed);
        if (movementAudio != null)
            movementAudio.volume = Mathf.Lerp(movementAudio.volume, targetMoveVol, Time.deltaTime * fadeSpeed);
    }

    // Fungsi baru untuk cek apakah gerakan aman
    private bool IsSafeToMove(Vector3 worldDirection)
    {
        // Jika tidak ada collision, gerakan aman
        if (collisionNormal == Vector3.zero) return true;

        // Hitung dot product antara arah gerakan dan normal dinding
        float dotProduct = Vector3.Dot(worldDirection.normalized, collisionNormal);

        // Jika dot product > -0.1, artinya tidak menuju dinding (aman)
        // Jika dot product < -0.1, artinya menuju dinding (bahaya)
        bool isSafe = dotProduct > -0.1f;

        if (!isSafe)
        {
            Debug.Log($"<color=red>Gerakan DIBLOKIR! Direction: {worldDirection}, Normal: {collisionNormal}, Dot: {dotProduct:F2}</color>");
        }

        return isSafe;
    }

    public void ChangeCoordinate(string direction, float speed)
    {
        // Saat ngerem, kunci belok kiri/kanan agar fokus berhenti
        if (isBraking && lockXWhileBraking && (direction == "Left" || direction == "Right"))
        {
            TriggerShake(0.01f, 0.1f);
            return;
        }

        float nextX = currentX;
        float nextZ = currentZ;
        Vector3 moveDirection = Vector3.zero;

        switch (direction)
        {
            case "Left":
                moveDirection = -transform.right; // Arah kiri dalam world space
                nextX -= speed * Time.deltaTime;
                break;
            case "Right":
                moveDirection = transform.right; // Arah kanan dalam world space
                nextX += speed * Time.deltaTime;
                break;
            case "Forward":
                // Cek apakah gerakan maju aman
                if (isBlocked && !IsSafeToMove(transform.forward))
                {
                    Debug.Log("<color=yellow>Tidak bisa maju! Terhalang dinding.</color>");
                    TriggerShake(0.05f, 0.2f);
                    return;
                }
                IncreaseSpeed();
                return;
            case "Backward":
                // Mundur biasanya selalu aman (menjauhi collision)
                DecreaseSpeed();
                return;
        }

        // Untuk gerakan kiri/kanan, cek collision
        if (direction == "Left" || direction == "Right")
        {
            // Cek dengan collision normal
            if (isBlocked && !IsSafeToMove(moveDirection))
            {
                Debug.Log($"<color=yellow>Tidak bisa bergerak {direction}! Terhalang dinding.</color>");
                TriggerShake(0.05f, 0.2f);
                return;
            }

            // Cek dengan sphere cast (wall layer check)
            Vector3 nextPos = new Vector3(nextX, rb.position.y, nextZ);
            if (!Physics.CheckSphere(nextPos, checkRadius, wallLayer))
            {
                currentX = nextX;
                currentZ = nextZ;
                TriggerShake(0.01f, 0.1f);
            }
            else
            {
                TriggerShake(0.05f, 0.2f);
                Debug.Log("Blocked by wall (sphere check)!");
            }
        }
    }

    public void TriggerShake(float intensity = -1f, float duration = -1f)
    {
        if (intensity > 0f) shakeIntensity = intensity;
        if (duration > 0f) shakeDuration = duration;
        shakeTimer = shakeDuration;
    }

    private void IncreaseSpeed()
    {
        // Jika terblokir dan mencoba maju, cegah
        if (isBlocked && currentSpeed >= 0)
        {
            Debug.Log("<color=orange>Speed tidak bisa ditambah - terhalang dinding!</color>");
            TriggerShake(0.05f, 0.2f);
            return;
        }

        currentSpeed += acceleration * Time.deltaTime;
        if (currentSpeed > maxSpeed)
            currentSpeed = maxSpeed;

        TriggerShake();
    }

    private void DecreaseSpeed()
    {
        currentSpeed -= acceleration * Time.deltaTime;
        if (currentSpeed < -maxSpeed)
            currentSpeed = -maxSpeed;

        TriggerShake();
    }

    private void TriggerShake()
    {
        shakeTimer = shakeDuration;
    }

    private void HandleShake()
    {
        if (shakeTimer > 0)
        {
            Vector3 offset = Random.insideUnitSphere * shakeIntensity;
            shakeTarget.localPosition = originalShakePos + offset;
            shakeTimer -= Time.deltaTime;
        }
        else
        {
            shakeTarget.localPosition = Vector3.Lerp(shakeTarget.localPosition, originalShakePos, Time.deltaTime * 5f);
        }
    }

    public void EmergencyStop()
    {
        isBraking = false;
        brakeTarget = 0f;
        currentSpeed = 0f;
        Debug.Log("Emergency Stop Activated - Speed set to 0");
    }

    public void GradualStop()
    {
        brakeTarget = 0f;
        isBraking = true;
        TriggerShake(0.02f, 0.15f);
        Debug.Log("Gradual Stop Triggered - Braking towards 0");
    }

    public bool IsMoving()
    {
        return Mathf.Abs(currentSpeed) > 0.1f;
    }

    private void UpdateCoordinateText()
    {
        if (textX != null)
            textX.text = $"X: {currentX:F2}";

        if (textZ != null)
            textZ.text = $"Z: {currentZ:F2}";

        if (textSpeed != null)
            textSpeed.text = $"Knot: {currentSpeed:F2}";
    }

    /// <summary>
    /// Dipanggil tuas saat didorong ke depan untuk melanjutkan gerak setelah stop.
    /// </summary>
    public void ResumeFromLever()
    {
        // Lepas kondisi berhenti / blokir
        isBlocked = false;
        isBraking = false;
        brakeTarget = 0f;

        // Reset arah tabrakan agar cek aman bergerak tidak menganggap masih nempel
        collisionNormal = Vector3.zero;

        // NEW: buka kunci kontrol
        controlsLockedByLever = false;

        // Mulai dari diam; biarkan akselerasi mengambil alih
        currentSpeed = 0f;

        TriggerShake(0.01f, 0.1f);
        Debug.Log("ResumeFromLever: Resume movement from lever forward.");
    }

    /// <summary>
    /// Wrapper publik untuk memicu guncangan dari script lain (mis. tabrakan).
    /// </summary>
    public void BumpShake(float intensity = 0.08f, float duration = 0.25f)
    {
        TriggerShake(intensity, duration);
    }
}
