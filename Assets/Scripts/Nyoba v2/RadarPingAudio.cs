using UnityEngine;

[DisallowMultipleComponent]
public class RadarPingAudio : MonoBehaviour
{
    [Header("References")]
    public AudioSource source;
    public AudioClip pingClip;
    public PhotoManager photoManager;
    public SubmarineCoordinates submarine;   // baca X/Z & speed dummy

    [Header("Audio")]
    [Range(0f, 1f)] public float volume = 0.65f;
    public bool usePitchScaling = true;
    public float pitchFar = 0.9f;
    public float pitchNear = 1.2f;

    [Header("Proximity Settings")]
    public float farDistance = 300f;
    public float nearDistance = 10f;
    public float intervalFar = 2.0f;
    public float intervalNear = 0.25f;

    [Tooltip("Tahan interval terakhir saat kapal idle agar tidak berubah sendiri.")]
    public bool freezeWhenIdle = true;
    public float idleSpeedThreshold = 0.05f;

    public bool muteWhenNoTarget = true;

    [Tooltip("Batas minimal interval agar tidak mengecil tak terbatas.")]
    public float minIntervalClamp = 0.08f;
    [Tooltip("Kehalusan perubahan interval (SmoothDamp).")]
    public float intervalSmoothTime = 0.25f;

    [Header("Distance Smoothing")]
    [Tooltip("Semakin besar, perubahan jarak makin halus (anti 'goyang' saat gerak/kamera).")]
    public float distanceSmoothSpeed = 2.5f;

    [Header("Reset Behaviour")]
    [Tooltip("Setelah foto sukses, tahan interval FAR sampai kapal menjauh dari target atau target baru terpilih.")]
    public bool holdFarUntilMoveOrNewTarget = true;

    // ---- runtime state ----
    private float _timer;
    private float _currentInterval;
    private float _intervalVel;

    private float _rawNearest = Mathf.Infinity;     // jarak hasil hitung langsung
    private float _smoothedDistance = Mathf.Infinity; // jarak yang sudah dihaluskan
    private float _lastDistance = Mathf.Infinity;

    private bool _holdFar = false;          // tahan jarak FAR setelah foto sukses
    private int _currentTargetIndex = -1;   // target yang sedang diincar
    private int _lastCapturedIndex = -1;    // target yang barusan difoto

    void Awake()
    {
        if (!source)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            // PENTING: jadikan 2D agar tidak "aneh" saat kamera swing
            source.spatialBlend = 0f;
        }
        else
        {
            // pastikan 2D
            source.spatialBlend = 0f;
        }

        _currentInterval = Mathf.Max(intervalFar, intervalNear); // mulai dari kondisi jauh
        _lastDistance = farDistance;
        _smoothedDistance = farDistance;
        _rawNearest = farDistance;
    }

    void OnEnable()
    {
        if (photoManager) photoManager.OnPhotoCaptured += HandlePhotoCaptured;
        PhotoManager.OnPhotoTakenById += HandlePhotoCaptured;   // <--- NEW
    }


    void OnDisable()
    {
        if (photoManager) photoManager.OnPhotoCaptured -= HandlePhotoCaptured;
        PhotoManager.OnPhotoTakenById -= HandlePhotoCaptured;   // <--- NEW
    }

    private void HandlePhotoCaptured(int idx)
    {
        // Reset SEKETIKA saat foto sukses (baik sedang jalan / STOP)
        ForceFarAndReset();
        _lastCapturedIndex = idx;
        _currentTargetIndex = -1; // paksa evaluasi target baru
        if (holdFarUntilMoveOrNewTarget) _holdFar = true;
    }

    private void ForceFarAndReset()
    {
        _lastDistance = farDistance;                           // freeze idle akan menahan 'jauh'
        _smoothedDistance = farDistance;
        _rawNearest = farDistance;

        _currentInterval = Mathf.Max(intervalFar, intervalNear);  // langsung normal
        _timer = 0f;
        _intervalVel = 0f;
    }

    void Update()
    {
        if (!pingClip || photoManager == null || submarine == null) return;

        // --- 1) Posisi & kecepatan dummy ---
        Vector2 playerPos = new Vector2(submarine.currentX, submarine.currentZ);
        float speedAbs = Mathf.Abs(submarine.currentSpeed);

        // --- 2) Cari target terdekat yang valid ---
        float nearest = float.PositiveInfinity;
        int nearestIdx = -1;
        bool any = false;

        for (int i = 0; i < photoManager.photoLocations.Count; i++)
        {
            var loc = photoManager.photoLocations[i];
            if (loc == null || loc.photoSprite == null || loc.hasBeenPhotographed) continue;

            any = true;
            float d = Vector2.Distance(playerPos, loc.coordinates);
            if (d < nearest) { nearest = d; nearestIdx = i; }
        }

        if (!any)
        {
            if (muteWhenNoTarget) return; // tidak ada target aktif
            nearest = farDistance;
            nearestIdx = -1;
        }

        _rawNearest = nearest;

        // --- 3) Logika tahan FAR setelah foto sukses ---
        if (_holdFar)
        {
            // Paksa kondisi jauh (normal) sampai salah satu kondisi ini terpenuhi:
            // 1) Kapal sudah MENJAUH dari target bekas foto, atau
            // 2) Target terdekat berbeda dari yang barusan difoto (target baru terpilih)
            nearest = farDistance;

            bool movedFarEnough = (_rawNearest > nearDistance * 2f); // benar-benar menjauh
            bool newTargetFound = (nearestIdx != -1) && (nearestIdx != _lastCapturedIndex);

            if (movedFarEnough || newTargetFound)
                _holdFar = false;
        }
        else
        {
            // --- 4) Freeze idle biasa ---
            if (freezeWhenIdle && speedAbs < idleSpeedThreshold)
            {
                nearest = _lastDistance;   // tahan jarak terakhir saat idle
            }
            else
            {
                _lastDistance = nearest;   // update jarak hanya saat bergerak
            }
        }

        _currentTargetIndex = nearestIdx;

        // --- 5) Haluskan jarak agar pitch/interval tidak "goyang" saat gerak/kamera ---
        // Saat _holdFar aktif, _smoothedDistance sudah dipaksa jauh di ForceFarAndReset()
        if (!_holdFar)
        {
            // Lerp ke nilai 'nearest' agar tidak fluktuatif tajam
            _smoothedDistance = Mathf.Lerp(_smoothedDistance, nearest, Time.deltaTime * Mathf.Max(0.01f, distanceSmoothSpeed));
        }
        else
        {
            _smoothedDistance = farDistance;
        }

        // --- 6) Map jarak -> interval & haluskan ---
        float t01 = Mathf.Clamp01(Mathf.InverseLerp(nearDistance, farDistance, _smoothedDistance));
        float targetInterval = Mathf.Lerp(intervalNear, intervalFar, t01);
        targetInterval = Mathf.Max(minIntervalClamp, targetInterval);

        _currentInterval = Mathf.SmoothDamp(_currentInterval, targetInterval, ref _intervalVel, intervalSmoothTime);

        // --- 7) Timer aman terhadap perubahan interval ---
        _timer += Time.deltaTime;
        while (_timer >= _currentInterval)
        {
            _timer -= _currentInterval;

            source.volume = volume;
            source.pitch = usePitchScaling ? Mathf.Lerp(pitchNear, pitchFar, t01) : 1f;
            source.PlayOneShot(pingClip);
        }

        // DEBUG (aktifkan jika perlu)
        // Debug.Log($"[RADAR] holdFar={_holdFar} nearestIdx={nearestIdx} lastCap={_lastCapturedIndex} raw={_rawNearest:F1} smooth={_smoothedDistance:F1} spd={speedAbs:F2} int={_currentInterval:F2}");
    }
}
