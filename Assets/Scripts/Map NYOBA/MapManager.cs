using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MapManager : MonoBehaviour
{
    [Header("Map UI References")]
    public GameObject mapPanel;
    public RectTransform mapBackground;
    public GameObject playerMarkerPrefab;
    public GameObject targetMarkerPrefab;

    [Header("Map Settings")]
    public Vector2 mapSize = new Vector2(1000f, 1000f); // Ukuran dunia game
    public Vector2 mapUISize = new Vector2(400f, 400f); // Ukuran UI map
    public Color mapBackgroundColor = Color.black;
    public float markerSize = 10f;

    [Header("Player Reference")]
    public SubmarineCoordinates submarineCoordinates;

    [Header("Target Coordinates")]
    public List<Vector2> targetCoordinates = new List<Vector2>();

    private bool isMapOpen = false;
    private GameObject playerMarker;
    private List<GameObject> targetMarkers = new List<GameObject>();
    private Image mapBackgroundImage;

    void Start()
    {
        InitializeMap();
        SetupMapBackground();
        CreateMarkers();

        // Map dimulai dalam keadaan tertutup
        mapPanel.SetActive(false);
    }

    void Update()
    {
        // Toggle map dengan Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMap();
        }

        // Update posisi player marker jika map terbuka
        if (isMapOpen && submarineCoordinates != null)
        {
            UpdatePlayerMarkerPosition();
        }
    }

    void InitializeMap()
    {
        if (mapPanel == null)
        {
            Debug.LogError("Map Panel tidak ditemukan! Pastikan sudah di-assign di inspector.");
            return;
        }

        // Pastikan map background ada
        if (mapBackground == null)
        {
            mapBackground = mapPanel.GetComponent<RectTransform>();
        }

        // Set ukuran map UI
        mapBackground.sizeDelta = mapUISize;
    }

    void SetupMapBackground()
    {
        mapBackgroundImage = mapBackground.GetComponent<Image>();
        if (mapBackgroundImage == null)
        {
            mapBackgroundImage = mapBackground.gameObject.AddComponent<Image>();
        }

        mapBackgroundImage.color = mapBackgroundColor;
    }

    void CreateMarkers()
    {
        CreatePlayerMarker();
        CreateTargetMarkers();
    }

    void CreatePlayerMarker()
    {
        if (playerMarkerPrefab != null)
        {
            playerMarker = Instantiate(playerMarkerPrefab, mapBackground);
        }
        else
        {
            // Buat marker sederhana jika prefab tidak ada
            GameObject marker = new GameObject("PlayerMarker");
            marker.transform.SetParent(mapBackground);

            Image markerImage = marker.AddComponent<Image>();
            markerImage.color = Color.green;

            RectTransform rectTransform = marker.GetComponent<RectTransform>();
            rectTransform.sizeDelta = Vector2.one * markerSize;

            playerMarker = marker;
        }
    }

    void CreateTargetMarkers()
    {
        foreach (Vector2 targetCoord in targetCoordinates)
        {
            GameObject targetMarker;

            if (targetMarkerPrefab != null)
            {
                targetMarker = Instantiate(targetMarkerPrefab, mapBackground);
            }
            else
            {
                // Buat marker sederhana jika prefab tidak ada
                targetMarker = new GameObject("TargetMarker");
                targetMarker.transform.SetParent(mapBackground);

                Image markerImage = targetMarker.AddComponent<Image>();
                markerImage.color = Color.red;

                RectTransform rectTransform = targetMarker.GetComponent<RectTransform>();
                rectTransform.sizeDelta = Vector2.one * markerSize;
            }

            // Set posisi target marker
            Vector2 mapPosition = WorldToMapPosition(targetCoord);
            targetMarker.GetComponent<RectTransform>().anchoredPosition = mapPosition;

            targetMarkers.Add(targetMarker);
        }
    }

    void ToggleMap()
    {
        isMapOpen = !isMapOpen;
        mapPanel.SetActive(isMapOpen);

        // Pause/unpause game (opsional)
        Time.timeScale = isMapOpen ? 0f : 1f;

        // Unlock/lock cursor (opsional)
        Cursor.lockState = isMapOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isMapOpen;
    }

    void UpdatePlayerMarkerPosition()
    {
        if (playerMarker != null && submarineCoordinates != null)
        {
            Vector2 worldPos = new Vector2(submarineCoordinates.currentX, submarineCoordinates.currentZ);
            Vector2 mapPos = WorldToMapPosition(worldPos);
            playerMarker.GetComponent<RectTransform>().anchoredPosition = mapPos;
        }
    }

    Vector2 WorldToMapPosition(Vector2 worldPosition)
    {
        // Konversi koordinat dunia ke koordinat map UI
        float normalizedX = (worldPosition.x + mapSize.x / 2f) / mapSize.x;
        float normalizedY = (worldPosition.y + mapSize.y / 2f) / mapSize.y;

        float mapX = (normalizedX - 0.5f) * mapUISize.x;
        float mapY = (normalizedY - 0.5f) * mapUISize.y;

        return new Vector2(mapX, mapY);
    }

    // Method untuk menambah target koordinat secara runtime
    public void AddTargetCoordinate(Vector2 coordinate)
    {
        targetCoordinates.Add(coordinate);

        // Buat marker baru untuk target ini
        GameObject targetMarker;

        if (targetMarkerPrefab != null)
        {
            targetMarker = Instantiate(targetMarkerPrefab, mapBackground);
        }
        else
        {
            targetMarker = new GameObject("TargetMarker");
            targetMarker.transform.SetParent(mapBackground);

            Image markerImage = targetMarker.AddComponent<Image>();
            markerImage.color = Color.red;

            RectTransform rectTransform = targetMarker.GetComponent<RectTransform>();
            rectTransform.sizeDelta = Vector2.one * markerSize;
        }

        Vector2 mapPosition = WorldToMapPosition(coordinate);
        targetMarker.GetComponent<RectTransform>().anchoredPosition = mapPosition;

        targetMarkers.Add(targetMarker);
    }

    // Method untuk menghapus target koordinat
    public void RemoveTargetCoordinate(int index)
    {
        if (index >= 0 && index < targetCoordinates.Count)
        {
            targetCoordinates.RemoveAt(index);

            if (index < targetMarkers.Count)
            {
                Destroy(targetMarkers[index]);
                targetMarkers.RemoveAt(index);
            }
        }
    }

    // Method untuk mendapatkan jarak ke target terdekat
    public float GetDistanceToNearestTarget()
    {
        if (submarineCoordinates == null || targetCoordinates.Count == 0)
            return float.MaxValue;

        Vector2 playerPos = new Vector2(submarineCoordinates.currentX, submarineCoordinates.currentZ);
        float minDistance = float.MaxValue;

        foreach (Vector2 target in targetCoordinates)
        {
            float distance = Vector2.Distance(playerPos, target);
            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        return minDistance;
    }
}