using UnityEngine;

public class RadarMiniArrowUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("RectTransform panah UI (gambar segitiga kecil).")]
    public RectTransform arrowRect;
    [Tooltip("Root kapal / objek yang menentukan heading (rotasi Y).")]
    public Transform submarineRoot;
    [Tooltip("Transform permukaan radar (parent yg jadi acuan up/right/forward).")]
    public Transform radarSurface; // isi dengan transform objek radar (yang pakai shader)

    [Header("Tuning")]
    [Tooltip("Offset kalibrasi (0/90/180/-90) agar panah sejajar garis sweep/tekstur.")]
    public float angleOffsetDeg = 0f;
    [Tooltip("Halusnya rotasi panah UI.")]
    public float smooth = 12f;

    // simpan rotasi target agar halus
    private float _currentAngle;

    void Reset()
    {
        // Coba auto-assign bila kosong
        if (radarSurface == null) radarSurface = transform;
    }

    void LateUpdate()
    {
        if (arrowRect == null || submarineRoot == null || radarSurface == null) return;

        // 1) Ambil heading kapal (rotasi Y dunia)
        float headingY = submarineRoot.eulerAngles.y;

        // 2) Jika radarSurface ikut diputar, kita hitung heading relatif radar
        //    Proyeksikan forward kapal ke bidang radarSurface.up, lalu hitung sudut ke sumbu "atas" radar (forward).
        Vector3 up = radarSurface.up;
        Vector3 fwdRadar = Vector3.ProjectOnPlane(radarSurface.forward, up).normalized;   // sumbu "atas" radar
        Vector3 fwdShip = Vector3.ProjectOnPlane(submarineRoot.forward, up).normalized;  // arah hadap kapal di bidang radar

        // sudut antara dua vektor di bidang (pakai Atan2 agar tanda (CW/CCW) benar)
        float x = Vector3.Dot(fwdShip, radarSurface.right);
        float z = Vector3.Dot(fwdShip, fwdRadar);
        float angleDeg = Mathf.Atan2(x, z) * Mathf.Rad2Deg;

        // 3) Tambah offset kalibrasi (agar sama dengan orientasi sweep shader)
        float targetAngle = angleDeg + angleOffsetDeg;

        // 4) Haluskan rotasi jarum (lerp sudut)
        _currentAngle = Mathf.LerpAngle(_currentAngle, targetAngle, Time.deltaTime * smooth);

        // 5) Terapkan ke UI (Z berputar searah jarum jam; jika kebalik, balik tanda)
        arrowRect.localEulerAngles = new Vector3(0f, 0f, -_currentAngle);

        // 6) Pastikan panah tepat di tengah kanvas
        arrowRect.anchoredPosition = Vector2.zero;
    }
}

public class UnSquashUI : MonoBehaviour
{
    public RectTransform arrowGfx; // child berisi Image
    void LateUpdate()
    {
        if (!arrowGfx) return;
        // balikkan skala global parent
        Vector3 s = transform.lossyScale;
        arrowGfx.localScale = new Vector3(
            s.x != 0 ? 1f / s.x : 1f,
            s.y != 0 ? 1f / s.y : 1f,
            s.z != 0 ? 1f / s.z : 1f
        );
    }
}