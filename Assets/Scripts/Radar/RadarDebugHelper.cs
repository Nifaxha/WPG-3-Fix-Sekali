using UnityEngine;

public class RadarDebugHelper : MonoBehaviour
{
    [Header("References")]
    public MeshWallRadar radarSystem;
    public SubmarineCoordinates submarineCoords;

    [Header("Debug Settings")]
    public bool showDebugRays = true;
    public bool showConsoleDebug = true;
    public float debugRayDuration = 0.1f;
    public Color hitRayColor = Color.red;
    public Color missRayColor = Color.green;

    [Header("Manual Test")]
    public LayerMask testLayerMask = 1;
    public float testRange = 300f;

    void Update()
    {
        if (showDebugRays)
        {
            DebugCurrentRadarScan();
        }

        // Manual test dengan key
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestRadarAllDirections();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            TestSingleRaycast();
        }
    }

    void DebugCurrentRadarScan()
    {
        if (submarineCoords == null) return;

        // Posisi kapal selam
        Vector3 submarinePos = new Vector3(submarineCoords.currentX, 0, submarineCoords.currentZ);

        // Simulasi scan angle dari radar
        float currentAngle = Time.time * 360f; // Rotasi sederhana
        float angleInRadians = currentAngle * Mathf.Deg2Rad;
        Vector3 direction = new Vector3(Mathf.Cos(angleInRadians), 0, Mathf.Sin(angleInRadians));

        // Test raycast
        RaycastHit hit;
        if (Physics.Raycast(submarinePos, direction, out hit, testRange, testLayerMask))
        {
            // HIT - gambar ray merah
            Debug.DrawRay(submarinePos, direction * hit.distance, hitRayColor, debugRayDuration);

            if (showConsoleDebug)
            {
                Debug.Log($"🎯 RADAR HIT: {hit.collider.name} at distance {hit.distance:F2}");
                Debug.Log($"   Hit point: {hit.point}");
                Debug.Log($"   Submarine pos: {submarinePos}");
                Debug.Log($"   Object layer: {hit.collider.gameObject.layer}");
            }
        }
        else
        {
            // MISS - gambar ray hijau
            Debug.DrawRay(submarinePos, direction * testRange, missRayColor, debugRayDuration);
        }
    }

    [ContextMenu("Test Radar All Directions")]
    public void TestRadarAllDirections()
    {
        if (submarineCoords == null)
        {
            Debug.LogError("❌ SubmarineCoordinates tidak di-set!");
            return;
        }

        Vector3 submarinePos = new Vector3(submarineCoords.currentX, 0, submarineCoords.currentZ);
        Debug.Log($"🧭 Testing radar dari posisi: {submarinePos}");
        Debug.Log($"🎯 Layer mask: {testLayerMask.value}");
        Debug.Log($"📏 Range: {testRange}");

        int hitCount = 0;

        // Test 36 arah (setiap 10 derajat)
        for (int angle = 0; angle < 360; angle += 10)
        {
            float angleInRadians = angle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angleInRadians), 0, Mathf.Sin(angleInRadians));

            RaycastHit hit;
            if (Physics.Raycast(submarinePos, direction, out hit, testRange, testLayerMask))
            {
                hitCount++;
                Debug.Log($"   ✅ Hit at {angle}°: {hit.collider.name} (distance: {hit.distance:F1})");
                Debug.DrawRay(submarinePos, direction * hit.distance, Color.red, 5f);
            }
        }

        Debug.Log($"📊 HASIL: {hitCount}/36 arah mendeteksi dinding");

        if (hitCount == 0)
        {
            Debug.LogWarning("⚠️ TIDAK ADA DINDING TERDETEKSI! Cek:");
            Debug.LogWarning("   1. Layer dinding sudah benar?");
            Debug.LogWarning("   2. MeshCollider ada dan enabled?");
            Debug.LogWarning("   3. Jarak radar cukup?");
            Debug.LogWarning("   4. Posisi kapal selam benar?");
        }
    }

    [ContextMenu("Test Single Raycast Forward")]
    public void TestSingleRaycast()
    {
        if (submarineCoords == null) return;

        Vector3 submarinePos = new Vector3(submarineCoords.currentX, 0, submarineCoords.currentZ);
        Vector3 forward = Vector3.forward; // Arah Z+

        Debug.Log($"🔍 Single raycast test:");
        Debug.Log($"   From: {submarinePos}");
        Debug.Log($"   Direction: {forward}");
        Debug.Log($"   Range: {testRange}");
        Debug.Log($"   Layer: {testLayerMask.value}");

        RaycastHit hit;
        if (Physics.Raycast(submarinePos, forward, out hit, testRange, testLayerMask))
        {
            Debug.Log($"✅ HIT: {hit.collider.name}");
            Debug.Log($"   Distance: {hit.distance}");
            Debug.Log($"   Point: {hit.point}");
            Debug.Log($"   Layer: {hit.collider.gameObject.layer}");
            Debug.Log($"   Has MeshCollider: {hit.collider is MeshCollider}");

            Debug.DrawRay(submarinePos, forward * hit.distance, Color.red, 10f);
            Debug.DrawRay(hit.point, Vector3.up * 2f, Color.yellow, 10f);
        }
        else
        {
            Debug.LogWarning("❌ NO HIT");
            Debug.DrawRay(submarinePos, forward * testRange, Color.green, 10f);
        }
    }

    [ContextMenu("Check Wall Setup")]
    public void CheckWallSetup()
    {
        Debug.Log("🔧 CHECKING WALL SETUP:");

        // Cek MeshCollider
        MeshCollider[] allMeshColliders = FindObjectsOfType<MeshCollider>();
        Debug.Log($"📊 Found {allMeshColliders.Length} MeshColliders in scene");

        foreach (MeshCollider mc in allMeshColliders)
        {
            Debug.Log($"   🧱 {mc.name}:");
            Debug.Log($"      Layer: {mc.gameObject.layer} ({LayerMask.LayerToName(mc.gameObject.layer)})");
            Debug.Log($"      Enabled: {mc.enabled}");
            Debug.Log($"      Convex: {mc.convex}");
            Debug.Log($"      Has Mesh: {mc.sharedMesh != null}");
            Debug.Log($"      Position: {mc.transform.position}");
        }

        // Cek radar system
        if (radarSystem != null)
        {
            Debug.Log($"🎯 Radar System:");
            Debug.Log($"   Wall Layer: {radarSystem.wallLayer.value}");
            Debug.Log($"   Range: {radarSystem.radarRange}");
            Debug.Log($"   Wall Meshes Count: {radarSystem.wallMeshes.Count}");
        }

        // Cek submarine coords  
        if (submarineCoords != null)
        {
            Debug.Log($"🚢 Submarine:");
            Debug.Log($"   Position: ({submarineCoords.currentX}, {submarineCoords.currentZ})");
        }
    }

    void OnDrawGizmosSelected()
    {
        if (submarineCoords == null) return;

        Vector3 submarinePos = new Vector3(submarineCoords.currentX, 0, submarineCoords.currentZ);

        // Draw submarine position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(submarinePos, 2f);

        // Draw radar range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(submarinePos, testRange);

        // Draw current scan direction
        if (Application.isPlaying)
        {
            float currentAngle = Time.time * 360f;
            float angleInRadians = currentAngle * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Cos(angleInRadians), 0, Mathf.Sin(angleInRadians));

            Gizmos.color = Color.red;
            Gizmos.DrawRay(submarinePos, direction * testRange);
        }
    }
}