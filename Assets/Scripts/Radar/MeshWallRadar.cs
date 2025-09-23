using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MeshWallRadar : MonoBehaviour
{
    [Header("Radar Settings")]
    public SubmarineCoordinates submarineCoords; // Reference ke sistem koordinat Anda
    public float radarRange = 50f; // Jangkauan radar dalam satuan koordinat
    public float scanSpeed = 1f; // Kecepatan scan radar
    public float blipLifetime = 3f; // Berapa lama blip muncul setelah scan

    [Header("Wall Detection")]
    public List<MeshCollider> wallMeshes = new List<MeshCollider>(); // Drag semua dinding ke sini
    public LayerMask wallLayer = 1; // Layer untuk dinding
    public bool autoFindWallMeshes = true; // Otomatis cari mesh dengan layer tertentu

    [Header("Raycast Settings")]
    public int raysPerScan = 5; // Berapa ray per scan (untuk akurasi lebih tinggi)
    public float raySpread = 2f; // Spread ray dalam derajat

    [Header("Visual Components")]
    public Transform scanLine; // Garis scan radar yang berputar
    public GameObject blipPrefab; // Prefab untuk blip/titik radar
    public Transform blipContainer; // Parent untuk semua blip
    public AudioSource radarAudio; // Audio untuk suara radar
    public AudioClip blipSound; // Sound effect untuk blip

    [Header("Radar Display")]
    public RectTransform radarDisplay; // UI Canvas untuk radar
    public float radarDisplayRadius = 100f; // Radius tampilan radar di UI

    private float currentScanAngle = 0f;
    private List<RadarBlip> activeBlips = new List<RadarBlip>();

    [System.Serializable]
    public class RadarBlip
    {
        public GameObject blipObject;
        public float remainingTime;
        public Vector2 worldPosition; // Posisi dalam koordinat world (X,Z)

        public RadarBlip(GameObject obj, float time, Vector2 pos)
        {
            blipObject = obj;
            remainingTime = time;
            worldPosition = pos;
        }
    }

    // Di dalam script MeshWallRadar.cs
    public List<RadarBlip> GetActiveBlips()
    {
        return activeBlips;
    }

    void Start()
    {
        // Cari SubmarineCoordinates jika belum di-set
        if (submarineCoords == null)
            submarineCoords = FindObjectOfType<SubmarineCoordinates>();

        // Setup blip container jika belum ada
        if (blipContainer == null)
        {
            GameObject container = new GameObject("RadarBlips");
            container.transform.SetParent(radarDisplay);
            blipContainer = container.transform;
        }

        // Auto-find wall meshes jika diaktifkan
        if (autoFindWallMeshes)
        {
            FindWallMeshesInScene();
        }
    }

    void Update()
    {
        UpdateScanLine();
        ScanForWalls();
        UpdateBlips();
    }

    void UpdateScanLine()
    {
        // Rotasi garis scan radar
        currentScanAngle += scanSpeed * Time.deltaTime * 360f;
        if (currentScanAngle >= 360f)
            currentScanAngle -= 360f;

        if (scanLine != null)
        {
            scanLine.rotation = Quaternion.Euler(0, 0, -currentScanAngle);
        }
    }

    void FindWallMeshesInScene()
    {
        // Cari semua MeshCollider dengan layer yang sesuai
        MeshCollider[] allMeshColliders = FindObjectsOfType<MeshCollider>();

        wallMeshes.Clear();

        foreach (MeshCollider meshCol in allMeshColliders)
        {
            // Cek apakah di layer yang benar
            if (((1 << meshCol.gameObject.layer) & wallLayer) != 0)
            {
                wallMeshes.Add(meshCol);
                Debug.Log($"Found wall mesh: {meshCol.gameObject.name}");
            }
        }

        Debug.Log($"Auto-found {wallMeshes.Count} wall meshes");
    }

    void ScanForWalls()
    {
        if (submarineCoords == null) return;

        // Posisi kapal selam saat ini dalam 3D space
        Vector3 submarinePos3D = new Vector3(submarineCoords.currentX, 0, submarineCoords.currentZ);

        // Scan dengan multiple rays untuk akurasi lebih tinggi
        for (int i = 0; i < raysPerScan; i++)
        {
            // Hitung sudut untuk setiap ray
            float angleOffset = (i - (raysPerScan - 1) * 0.5f) * raySpread / raysPerScan;
            float rayAngle = currentScanAngle + angleOffset;
            float angleInRadians = rayAngle * Mathf.Deg2Rad;

            // Arah ray dalam 3D space
            Vector3 rayDirection = new Vector3(Mathf.Cos(angleInRadians), 0, Mathf.Sin(angleInRadians));

            // Raycast ke semua wall meshes
            RaycastForWalls(submarinePos3D, rayDirection);
        }
    }

    void RaycastForWalls(Vector3 startPos, Vector3 direction)
    {
        // Method 1: Gunakan Physics.Raycast dengan layer mask
        RaycastHit hit;
        if (Physics.Raycast(startPos, direction, out hit, radarRange, wallLayer))
        {
            Vector2 hitPoint2D = new Vector2(hit.point.x, hit.point.z);
            CreateOrUpdateBlip(hitPoint2D);
            return;
        }

        // Method 2: Manual raycast ke setiap mesh jika Physics.Raycast tidak bekerja
        foreach (MeshCollider meshCol in wallMeshes)
        {
            if (meshCol == null || !meshCol.enabled) continue;

            // Raycast ke mesh collider spesifik
            Ray ray = new Ray(startPos, direction);
            RaycastHit meshHit;

            if (meshCol.Raycast(ray, out meshHit, radarRange))
            {
                Vector2 hitPoint2D = new Vector2(meshHit.point.x, meshHit.point.z);
                CreateOrUpdateBlip(hitPoint2D);
                break; // Ambil hit yang pertama saja
            }
        }
    }

    void CreateOrUpdateBlip(Vector2 worldPos)
    {
        // Cek apakah sudah ada blip untuk posisi ini
        RadarBlip existingBlip = FindBlipAtPosition(worldPos, 2f);

        if (existingBlip != null)
        {
            // Reset waktu blip yang sudah ada
            existingBlip.remainingTime = blipLifetime;
        }
        else
        {
            // Buat blip baru
            GameObject newBlip = Instantiate(blipPrefab, blipContainer);
            Vector2 radarPos = WorldToRadarPosition(worldPos);
            newBlip.GetComponent<RectTransform>().anchoredPosition = radarPos;

            RadarBlip blip = new RadarBlip(newBlip, blipLifetime, worldPos);
            activeBlips.Add(blip);

            // Play sound effect
            if (radarAudio && blipSound)
            {
                radarAudio.PlayOneShot(blipSound);
            }
        }
    }

    void UpdateBlips()
    {
        for (int i = activeBlips.Count - 1; i >= 0; i--)
        {
            RadarBlip blip = activeBlips[i];
            blip.remainingTime -= Time.deltaTime;

            if (blip.remainingTime <= 0f)
            {
                // Hapus blip yang sudah expired
                if (blip.blipObject != null)
                    Destroy(blip.blipObject);
                activeBlips.RemoveAt(i);
            }
            else
            {
                // Update posisi blip (untuk kasus obstacle bergerak)
                Vector2 newRadarPos = WorldToRadarPosition(blip.worldPosition);
                blip.blipObject.GetComponent<RectTransform>().anchoredPosition = newRadarPos;

                // Fade out effect
                float alpha = blip.remainingTime / blipLifetime;
                Image blipImage = blip.blipObject.GetComponent<Image>();
                if (blipImage != null)
                {
                    Color color = blipImage.color;
                    color.a = alpha;
                    blipImage.color = color;
                }
            }
        }
    }

    Vector2 WorldToRadarPosition(Vector2 worldPos)
    {
        if (submarineCoords == null) return Vector2.zero;

        // Konversi posisi world ke posisi radar UI
        Vector2 submarinePos = new Vector2(submarineCoords.currentX, submarineCoords.currentZ);
        Vector2 relativePos = worldPos - submarinePos;

        // Scale ke ukuran radar display
        float scale = radarDisplayRadius / radarRange;
        Vector2 radarPos = relativePos * scale;

        // Batasi dalam lingkaran radar
        if (radarPos.magnitude > radarDisplayRadius)
        {
            radarPos = radarPos.normalized * radarDisplayRadius;
        }

        return radarPos;
    }

    RadarBlip FindBlipAtPosition(Vector2 worldPos, float tolerance)
    {
        foreach (RadarBlip blip in activeBlips)
        {
            if (Vector2.Distance(blip.worldPosition, worldPos) <= tolerance)
                return blip;
        }
        return null;
    }

    // Method utility untuk debugging
    [ContextMenu("Test Raycast All Directions")]
    public void TestRaycastAllDirections()
    {
        if (submarineCoords == null) return;

        Vector3 submarinePos3D = new Vector3(submarineCoords.currentX, 0, submarineCoords.currentZ);

        for (int angle = 0; angle < 360; angle += 10)
        {
            float angleInRadians = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angleInRadians), 0, Mathf.Sin(angleInRadians));

            RaycastHit hit;
            if (Physics.Raycast(submarinePos3D, direction, out hit, radarRange, wallLayer))
            {
                Debug.DrawRay(submarinePos3D, direction * hit.distance, Color.red, 2f);
                Debug.Log($"Hit at angle {angle}: {hit.point}");
            }
            else
            {
                Debug.DrawRay(submarinePos3D, direction * radarRange, Color.green, 2f);
            }
        }
    }

    [ContextMenu("Refresh Wall Meshes")]
    public void RefreshWallMeshes()
    {
        FindWallMeshesInScene();
    }

    void OnDrawGizmosSelected()
    {
        if (submarineCoords == null) return;

        // Visualisasi posisi kapal selam dan radar range
        Vector3 submarinePos3D = new Vector3(submarineCoords.currentX, 0, submarineCoords.currentZ);

        Gizmos.color = Color.green;
        DrawWireCircle(submarinePos3D, radarRange);

        // Visualisasi scan line dengan multiple rays
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;

            for (int i = 0; i < raysPerScan; i++)
            {
                float angleOffset = (i - (raysPerScan - 1) * 0.5f) * raySpread / raysPerScan;
                float rayAngle = currentScanAngle + angleOffset;
                float angleInRadians = rayAngle * Mathf.Deg2Rad;
                Vector3 scanDirection = new Vector3(Mathf.Cos(angleInRadians), 0, Mathf.Sin(angleInRadians));
                Gizmos.DrawRay(submarinePos3D, scanDirection * radarRange);
            }
        }

        // Visualisasi wall meshes
        Gizmos.color = Color.red;
        foreach (MeshCollider meshCol in wallMeshes)
        {
            if (meshCol != null)
            {
                Gizmos.DrawWireMesh(meshCol.sharedMesh, meshCol.transform.position, meshCol.transform.rotation, meshCol.transform.lossyScale);
            }
        }
    }

    // Helper method untuk menggambar wire circle
    void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}