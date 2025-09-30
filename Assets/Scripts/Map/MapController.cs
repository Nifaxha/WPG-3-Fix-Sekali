using UnityEngine;
using UnityEngine.UI;

public class MapController : MonoBehaviour
{
    [Header("Referensi Objek (Seret dari Hierarchy)")]
    public GameObject mapCanvasObject;
    public MonoBehaviour navigationSystem;
    public RectTransform playerMarker;
    public RectTransform mapBackground;

    [Header("Pengaturan Batas Peta (Dunia 3D)")]
    public Vector2 worldMinBounds = new Vector2(-50, -50);
    public Vector2 worldMaxBounds = new Vector2(50, 50);

    [Header("Opsi Tambahan")]
    public bool useTransformPosition = true; // Toggle untuk pilih sumber posisi
    public bool rotateMarkerWithPlayer = true; // Rotasi marker sesuai player

    private System.Reflection.FieldInfo fieldX;
    private System.Reflection.FieldInfo fieldZ;
    private Transform navigationSystemTransform;

    void Start()
    {
        // Coba ambil field currentX dan currentZ
        var type = navigationSystem.GetType();
        fieldX = type.GetField("currentX");
        fieldZ = type.GetField("currentZ");
        navigationSystemTransform = navigationSystem.transform;

        // Warning jika field tidak ditemukan (bukan error fatal)
        if (fieldX == null || fieldZ == null)
        {
            Debug.LogWarning("Field 'currentX'/'currentZ' tidak ditemukan. Akan menggunakan Transform.position");
            useTransformPosition = true; // Paksa gunakan transform
        }

        if (mapCanvasObject != null)
        {
            mapCanvasObject.SetActive(false);
        }

        // Setup marker anchor ke center
        if (playerMarker != null)
        {
            playerMarker.anchorMin = new Vector2(0, 0);
            playerMarker.anchorMax = new Vector2(0, 0);
            playerMarker.pivot = new Vector2(0.5f, 0.5f);
        }
    }

    void Update()
    {
        // Toggle map dengan Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (mapCanvasObject != null)
            {
                mapCanvasObject.SetActive(!mapCanvasObject.activeSelf);
            }
        }

        // Update marker hanya jika map aktif
        if (mapCanvasObject != null && mapCanvasObject.activeSelf)
        {
            UpdatePlayerMarkerPosition();
        }
    }

    void UpdatePlayerMarkerPosition()
    {
        if (playerMarker == null || navigationSystem == null || navigationSystemTransform == null)
            return;

        float worldX, worldZ;

        // Pilih sumber posisi: Transform atau Variabel Internal
        if (useTransformPosition || fieldX == null || fieldZ == null)
        {
            // GUNAKAN POSISI TRANSFORM (LEBIH AKURAT)
            worldX = navigationSystemTransform.position.x;
            worldZ = navigationSystemTransform.position.z;
        }
        else
        {
            // Gunakan variabel internal
            worldX = (float)fieldX.GetValue(navigationSystem);
            worldZ = (float)fieldZ.GetValue(navigationSystem);
        }

        // Debug posisi
        Debug.Log($"[MAP] Player World Pos: ({worldX:F2}, {worldZ:F2})");

        // Konversi ke posisi map
        Vector2 mapPos = WorldToMapPosition(new Vector2(worldX, worldZ));
        playerMarker.anchoredPosition = mapPos;

        // Rotasi marker sesuai player (opsional)
        if (rotateMarkerWithPlayer)
        {
            // Y rotation di world = Z rotation di UI (top-down view)
            float rotation = navigationSystemTransform.eulerAngles.y;
            playerMarker.localRotation = Quaternion.Euler(0, 0, -rotation);
        }
    }

    private Vector2 WorldToMapPosition(Vector2 worldPos)
    {
        // Clamp posisi dalam batas dunia
        worldPos.x = Mathf.Clamp(worldPos.x, worldMinBounds.x, worldMaxBounds.x);
        worldPos.y = Mathf.Clamp(worldPos.y, worldMinBounds.y, worldMaxBounds.y);

        // Ukuran peta
        float mapWidth = mapBackground.rect.width;
        float mapHeight = mapBackground.rect.height;

        // Normalisasi posisi dunia ke 0-1
        float normalizedX = Mathf.InverseLerp(worldMinBounds.x, worldMaxBounds.x, worldPos.x);
        float normalizedY = Mathf.InverseLerp(worldMinBounds.y, worldMaxBounds.y, worldPos.y);

        // Konversi ke koordinat map (dari center 0,0)
        // Kurangi 0.5 untuk center, lalu kalikan ukuran
        float mapX = (normalizedX - 0.5f) * mapWidth;
        float mapY = (normalizedY - 0.5f) * mapHeight;

        Debug.Log($"[MAP] Normalized: ({normalizedX:F2}, {normalizedY:F2}) -> Map Pos: ({mapX:F2}, {mapY:F2})");

        return new Vector2(mapX, mapY);
    }

    void OnDrawGizmos()
    {
        // Gambar batas dunia di Scene view
        Gizmos.color = Color.cyan;
        Vector3 bottomLeft = new Vector3(worldMinBounds.x, 0, worldMinBounds.y);
        Vector3 topRight = new Vector3(worldMaxBounds.x, 0, worldMaxBounds.y);
        Vector3 topLeft = new Vector3(worldMinBounds.x, 0, worldMaxBounds.y);
        Vector3 bottomRight = new Vector3(worldMaxBounds.x, 0, worldMinBounds.y);

        Gizmos.DrawLine(bottomLeft, topLeft);
        Gizmos.DrawLine(topLeft, topRight);
        Gizmos.DrawLine(topRight, bottomRight);
        Gizmos.DrawLine(bottomRight, bottomLeft);

        // Gambar posisi player di Scene view (jika tersedia)
        if (Application.isPlaying && navigationSystemTransform != null)
        {
            Gizmos.color = Color.red;
            Vector3 playerPos = navigationSystemTransform.position;
            Gizmos.DrawWireSphere(playerPos, 1f);
            Gizmos.DrawLine(playerPos, playerPos + Vector3.up * 3f);
        }
    }
}