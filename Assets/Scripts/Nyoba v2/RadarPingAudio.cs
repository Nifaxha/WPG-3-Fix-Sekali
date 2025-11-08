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

    [Header("Reset Behaviour")]
    [Tooltip("Setelah foto sukses, tahan interval FAR hingga kapal bergerak atau target baru terpilih.")]
    public bool holdFarUntilMoveOrNewTarget = true;

    // ---- runtime state ----
    private float _timer;
    private float _currentInterval;
    private float _intervalVel;
    private float _lastDistance = Mathf.Infinity;

    private bool _holdFar = false;          // tahan jarak FAR setelah foto sukses
    private int _currentTargetIndex = -1;  // target yang sedang diincar
    private int _lastCapturedIndex = -1;  // target yang baru saja difoto

    void Awake()
    {
        if (!source)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;
        }
        _currentInterval = Mathf.Max(intervalFar, intervalNear); // mulai dari kondisi jauh
        _lastDistance = farDistance;
    }

    void OnEnable() { if (photoManager) photoManager.OnPhotoCaptured += HandlePhotoCaptured; }
    void OnDisable() { if (photoManager) photoManager.OnPhotoCaptured -= HandlePhotoCaptured; }

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

        // --- 3) Logika tahan FAR setelah foto sukses ---
        if (_holdFar)
        {
            // Paksa kondisi jauh (normal) sampai salah satu kondisi ini terpenuhi:
            // 1) kapal mulai bergerak, atau
            // 2) target terdekat berbeda dari yang barusan difoto (target baru terpilih)
            nearest = farDistance;
            _lastDistance = farDistance;

            bool moved = speedAbs > idleSpeedThreshold;
            bool newTargetFound = (nearestIdx != -1) && (nearestIdx != _lastCapturedIndex);

            if (moved || newTargetFound)
                _holdFar = false;
        }
        else
        {
            // --- 4) Freeze idle biasa ---
            if (freezeWhenIdle && speedAbs < idleSpeedThreshold)
                nearest = _lastDistance;   // tahan jarak terakhir saat idle
            else
                _lastDistance = nearest;   // update jarak hanya saat bergerak
        }

        _currentTargetIndex = nearestIdx;

        // --- 5) Map jarak -> interval & haluskan ---
        float t01 = Mathf.Clamp01(Mathf.InverseLerp(nearDistance, farDistance, nearest));
        float targetInterval = Mathf.Lerp(intervalNear, intervalFar, t01);
        targetInterval = Mathf.Max(minIntervalClamp, targetInterval);

        _currentInterval = Mathf.SmoothDamp(_currentInterval, targetInterval, ref _intervalVel, intervalSmoothTime);

        // --- 6) Timer aman terhadap perubahan interval ---
        _timer += Time.deltaTime;
        while (_timer >= _currentInterval)
        {
            _timer -= _currentInterval;

            source.volume = volume;
            source.pitch = usePitchScaling ? Mathf.Lerp(pitchNear, pitchFar, t01) : 1f;
            source.PlayOneShot(pingClip);
        }

        // DEBUG (aktifkan jika perlu)
        // Debug.Log($"[RADAR] holdFar={_holdFar} nearestIdx={nearestIdx} lastCap={_lastCapturedIndex} dist={nearest:F1} spd={speedAbs:F2} int={_currentInterval:F2}");
    }
}
