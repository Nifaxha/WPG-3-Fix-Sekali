using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapController : MonoBehaviour
{
    [Header("Referensi Objek")]
    public GameObject mapCanvasObject;    // BARU: Hubungkan GameObject MapCanvas ke sini
    public MonoBehaviour navigationSystem;
    public RectTransform playerMarker;
    public RectTransform mapBackground;
    public GameObject poiMarkerPrefab;
    public Transform poiContainer;

    [Header("Pengaturan Batas Peta (Dunia)")]
    public Vector2 worldMinBounds = new Vector2(-2000, -3000);
    public Vector2 worldMaxBounds = new Vector2(2000, 1000);

    private List<PointOfInterest> pointsOfInterest = new List<PointOfInterest>();
    private System.Reflection.FieldInfo fieldX;
    private System.Reflection.FieldInfo fieldZ;

    private struct PointOfInterest
    {
        public Vector2 worldCoordinates;
        public string name;
        public GameObject markerInstance;
    }

    void Start()
    {
        var type = navigationSystem.GetType();
        fieldX = type.GetField("currentX");
        fieldZ = type.GetField("currentZ");

        if (fieldX == null || fieldZ == null)
        {
            Debug.LogError("Tidak dapat menemukan field 'currentX' atau 'currentZ' di skrip NavigationSystem!");
            this.enabled = false; // Matikan skrip jika field tidak ditemukan
            return;
        }

        // Sembunyikan peta saat game dimulai
        if (mapCanvasObject != null)
        {
            mapCanvasObject.SetActive(false);
        }

        AddPOI(new Vector2(-166, -1545), "Lokasi Foto 1");
        AddPOI(new Vector2(500, -2500), "Anomali");
        AddPOI(new Vector2(1800, 800), "Titik Evakuasi");

        InstantiatePOIMarkers();
    }

    void Update()
    {
        // --- LOGIKA TOGGLE BARU ---
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (mapCanvasObject != null)
            {
                // Toggle (nyalakan jika mati, matikan jika nyala)
                mapCanvasObject.SetActive(!mapCanvasObject.activeSelf);
            }
        }

        // Hanya update posisi marker jika peta sedang aktif
        if (mapCanvasObject != null && mapCanvasObject.activeSelf)
        {
            UpdatePlayerMarkerPosition();
        }
    }

    void AddPOI(Vector2 coords, string poiName)
    {
        pointsOfInterest.Add(new PointOfInterest { worldCoordinates = coords, name = poiName });
    }

    void InstantiatePOIMarkers()
    {
        foreach (var poi in pointsOfInterest)
        {
            GameObject marker = Instantiate(poiMarkerPrefab, poiContainer);
            marker.name = poi.name;
            marker.GetComponent<RectTransform>().anchoredPosition = WorldToMapPosition(poi.worldCoordinates);
        }
    }

    void UpdatePlayerMarkerPosition()
    {
        if (playerMarker == null || navigationSystem == null) return;

        float currentX = (float)fieldX.GetValue(navigationSystem);
        float currentZ = (float)fieldZ.GetValue(navigationSystem);
        Vector2 playerWorldPos = new Vector2(currentX, currentZ);

        playerMarker.anchoredPosition = WorldToMapPosition(playerWorldPos);
    }

    private Vector2 WorldToMapPosition(Vector2 worldPos)
    {
        // BARU: Jepit posisi dunia agar tidak melebihi batas yang ditentukan
        worldPos.x = Mathf.Clamp(worldPos.x, worldMinBounds.x, worldMaxBounds.x);
        worldPos.y = Mathf.Clamp(worldPos.y, worldMinBounds.y, worldMaxBounds.y);

        // Dapatkan ukuran dari background peta
        float mapWidth = mapBackground.rect.width;
        float mapHeight = mapBackground.rect.height;

        // Normalisasi posisi dunia (ubah menjadi nilai antara 0 dan 1)
        float normalizedX = Mathf.InverseLerp(worldMinBounds.x, worldMaxBounds.x, worldPos.x);
        float normalizedY = Mathf.InverseLerp(worldMinBounds.y, worldMaxBounds.y, worldPos.y);

        // Kalikan nilai normalisasi dengan ukuran peta untuk mendapatkan posisi di UI
        float mapX = normalizedX * mapWidth;
        float mapY = normalizedY * mapHeight;

        return new Vector2(mapX, mapY);
    }
}