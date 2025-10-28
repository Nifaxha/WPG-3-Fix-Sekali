using UnityEngine;

public class RadarPingAudio : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource source;           // optional; kalau null akan dibuat otomatis
    public AudioClip pingClip;           // suara ping
    [Range(0f, 1f)] public float volume = 0.65f;
    public float pitch = 1f;

    [Header("Timing")]
    public bool syncToSweepSpeed = true; // ON: jeda = 1 / sweepSpeed
    [Tooltip("Rotasi per detik (kalau tidak ada script controller).")]
    public float sweepSpeed = 0.25f;     // 0.25 rps = 1 ping tiap 4 detik
    [Tooltip("Override interval detik jika syncToSweepSpeed = OFF.")]
    public float pingInterval = 3f;      // dipakai kalau syncToSweepSpeed = false
    [Tooltip("Jeda awal sebelum ping pertama.")]
    public float startDelay = 0.0f;

    float _timer;

    void Awake()
    {
        if (!source)
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 1f;   // 3D
        }
    }

    void OnEnable()
    {
        _timer = -startDelay;
    }

    void Update()
    {
        if (!pingClip) return;

        // Hitung target interval
        float interval = syncToSweepSpeed
            ? (sweepSpeed > 0f ? 1f / sweepSpeed : 999f)
            : Mathf.Max(0.05f, pingInterval);

        _timer += Time.deltaTime;
        if (_timer >= interval)
        {
            _timer -= interval;

            source.volume = volume;
            source.pitch = pitch;
            source.PlayOneShot(pingClip);
        }
    }

    // Opsional: dipanggil dari controller radar jika nilai sweep speed berubah
    public void SetSweepSpeed(float rps)
    {
        sweepSpeed = Mathf.Max(0f, rps);
    }
}
