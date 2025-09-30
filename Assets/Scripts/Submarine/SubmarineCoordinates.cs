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
    public float acceleration = 1f;  // seberapa cepat speed naik/turun
    public float maxSpeed = 15f;
    public bool isBlocked = false;
    private Rigidbody rb;

    [Header("Stop Settings")]
    public float emergencyStopDeceleration = 5f; // Deceleration rate saat emergency stop

    [Header("Audio")]
    public AudioSource movementAudio;   // audio gerak
    public AudioClip movementClip;

    public AudioSource idleAudio;       // audio idle (speed = 0)
    public AudioClip idleClip;

    private bool isMovingAudioPlaying = false;
    private bool isIdleAudioPlaying = false;

    [Header("Vibration / Shake Settings")]
    public Transform shakeTarget;      // biasanya camera atau objek parent kapal
    public float shakeIntensity = 0.1f; // besar getaran
    public float shakeDuration = 0.2f;  // lama getaran

    private Vector3 originalShakePos;
    private float shakeTimer = 0f;

    public LayerMask wallLayer;
    public float checkRadius = 0.5f;

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

        {
            // ... audio setup seperti sebelumnya
            if (shakeTarget == null)
                shakeTarget = Camera.main.transform; // default ke main camera
            originalShakePos = shakeTarget.localPosition;
        }
    }
    void FixedUpdate()
    {
        // Pesan ini akan muncul di setiap frame fisika
        Debug.Log($"FixedUpdate Status: Speed = {currentSpeed}, isBlocked = {isBlocked}");

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


    [Header("Audio Fade Settings")]
    public float fadeSpeed = 2f; // kecepatan transisi volume

    private void HandleMovementAndIdleAudio()
    {
        HandleShake();

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

    public void ChangeCoordinate(string direction, float speed)
    {
        // hitung posisi berikutnya
        float nextX = currentX;
        float nextZ = currentZ;

        switch (direction)
        {
            case "Left":
                nextX -= speed * Time.deltaTime;
                break;
            case "Right":
                nextX += speed * Time.deltaTime;
                break;
            case "Forward":
                IncreaseSpeed();
                return; // untuk maju mundur tetap pakai IncreaseSpeed/DecreaseSpeed
            case "Backward":
                DecreaseSpeed();
                return;
        }

        // cek tembok sebelum update
        Vector3 nextPos = new Vector3(nextX, 0, nextZ);
        if (!Physics.CheckSphere(nextPos, checkRadius, wallLayer))
        {
            // aman jalan
            currentX = nextX;
            currentZ = nextZ;

            // getar kecil seperti biasa
            TriggerShake(0.01f, 0.1f);
        }
        else
        {
            // ada tembok, misalnya getar lebih besar untuk efek “nabrak”
            TriggerShake(0.05f, 0.2f);
            Debug.Log("Blocked by wall!");
        }
    }

    private void TriggerShake(float intensity = -1f, float duration = -1f)
    {
        // kalau ada parameter dikirim, pakai nilai itu
        if (intensity > 0f) shakeIntensity = intensity;
        if (duration > 0f) shakeDuration = duration;

        shakeTimer = shakeDuration;
    }


    private void IncreaseSpeed()
    {
        currentSpeed += acceleration * Time.deltaTime;
        if (currentSpeed > maxSpeed)
            currentSpeed = maxSpeed;

        TriggerShake(); // getar tiap naik speed
    }

    private void DecreaseSpeed()
    {
        currentSpeed -= acceleration * Time.deltaTime;
        if (currentSpeed < -maxSpeed)
            currentSpeed = -maxSpeed;

        TriggerShake(); // getar tiap turun speed
    }

    private void TriggerShake()
    {
        shakeTimer = shakeDuration;
    }

    private void HandleShake()
    {
        if (shakeTimer > 0)
        {
            // random offset kecil
            Vector3 offset = Random.insideUnitSphere * shakeIntensity;
            shakeTarget.localPosition = originalShakePos + offset;

            shakeTimer -= Time.deltaTime;
        }
        else
        {
            // kembalikan posisi semula
            shakeTarget.localPosition = Vector3.Lerp(shakeTarget.localPosition, originalShakePos, Time.deltaTime * 5f);
        }
    }

    public void EmergencyStop()
    {
        currentSpeed = 0f;
        Debug.Log("Emergency Stop Activated - Speed set to 0");
    }

    public void GradualStop()
    {
        if (currentSpeed > 0)
        {
            currentSpeed -= emergencyStopDeceleration * Time.deltaTime;
            if (currentSpeed < 0) currentSpeed = 0;
        }
        else if (currentSpeed < 0)
        {
            currentSpeed += emergencyStopDeceleration * Time.deltaTime;
            if (currentSpeed > 0) currentSpeed = 0;
        }
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
}
