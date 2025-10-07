using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CoordinateMapController : MonoBehaviour
{
    [Header("Referensi Utama")]
    public GameObject mapPanel;
    public PhotoManager photoManager;
    public RectTransform nodeContainer;
    public GameObject coordinateNodePrefab;
    public TextMeshProUGUI coordinateDisplayText;

    [Header("Pengaturan Batas Peta (Dunia 3D)")]
    public Vector2 worldMinBounds;
    public Vector2 worldMaxBounds;

    private Dictionary<Vector2, GameObject> nodeObjects = new Dictionary<Vector2, GameObject>();
    private bool isMapVisible = false; // Track status map

    void Awake()
    {
        // Pastikan map panel tersembunyi bahkan sebelum Start()
        if (mapPanel != null)
        {
            mapPanel.SetActive(false);
            isMapVisible = false;
            Debug.Log("Map Panel disembunyikan saat Awake()");
        }
    }

    void Start()
    {
        if (photoManager == null)
        {
            Debug.LogError("PhotoManager belum di-assign di Inspector!", this.gameObject);
            return;
        }

        GenerateNodes();

        // Double check - pastikan map tetap tersembunyi
        if (mapPanel != null)
        {
            mapPanel.SetActive(false);
            isMapVisible = false;
            Debug.Log("Map Panel disembunyikan saat Start()");
        }
    }

    void OnEnable()
    {
        PhotoManager.OnPhotoTaken += MarkCoordinateAsCompleted;
    }

    void OnDisable()
    {
        PhotoManager.OnPhotoTaken -= MarkCoordinateAsCompleted;
    }

    void Update()
    {
        // Toggle map dengan tombol Tab
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ToggleMap();
        }
    }

    void ToggleMap()
    {
        if (mapPanel == null) return;

        isMapVisible = !isMapVisible;
        mapPanel.SetActive(isMapVisible);

        if (isMapVisible)
        {
            Debug.Log("<color=green>Map Panel DIBUKA</color>");
        }
        else
        {
            Debug.Log("<color=yellow>Map Panel DITUTUP</color>");
            // Reset text saat map ditutup
            if (coordinateDisplayText != null)
            {
                HideCoordinates();
            }
        }
    }

    void GenerateNodes()
    {
        // Ambil daftar lokasi foto dari PhotoManager
        foreach (PhotoLocation location in photoManager.photoLocations)
        {
            GameObject node = Instantiate(coordinateNodePrefab, nodeContainer);

            // Hitung posisi node di atas peta
            Vector2 mapPosition = WorldToMapPosition(location.coordinates);
            node.GetComponent<RectTransform>().anchoredPosition = mapPosition;

            // Inisialisasi skrip di dalam node
            CoordinateBox boxScript = node.GetComponent<CoordinateBox>();
            if (boxScript != null)
            {
                boxScript.Initialize(this, location.coordinates);
            }

            // Simpan referensi ke node ini untuk fitur centang
            nodeObjects[location.coordinates] = node;
        }

        Debug.Log($"<color=cyan>Total {nodeObjects.Count} nodes telah di-generate</color>");
    }

    public void ShowCoordinates(Vector2 coords)
    {
        if (coordinateDisplayText != null)
        {
            coordinateDisplayText.text = $"X: {coords.x}  Z: {coords.y}";
        }
    }

    public void HideCoordinates()
    {
        if (coordinateDisplayText != null)
        {
            coordinateDisplayText.text = "HOVER MOUSE OVER NODES FOR MORE INFORMATION";
        }
    }

    public void MarkCoordinateAsCompleted(Vector2 photoWorldCoords)
    {
        Vector2 closestCoord = Vector2.zero;
        float minDistance = float.MaxValue;

        foreach (Vector2 nodeCoord in nodeObjects.Keys)
        {
            float distance = Vector2.Distance(photoWorldCoords, nodeCoord);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCoord = nodeCoord;
            }
        }

        // Cari lokasi terdekat
        PhotoLocation loc = photoManager.photoLocations.Find(p => p.coordinates == closestCoord);
        if (loc != null && minDistance <= loc.radius)
        {
            if (nodeObjects.ContainsKey(closestCoord))
            {
                GameObject nodeToMark = nodeObjects[closestCoord];
                Transform checkmark = nodeToMark.transform.Find("Checkmark_Image");
                if (checkmark != null)
                {
                    checkmark.gameObject.SetActive(true);
                    Debug.Log($"<color=lime>Checkmark aktif untuk koordinat {closestCoord}</color>");
                }
            }
        }
    }

    private Vector2 WorldToMapPosition(Vector2 worldPos)
    {
        worldPos.x = Mathf.Clamp(worldPos.x, worldMinBounds.x, worldMaxBounds.x);
        worldPos.y = Mathf.Clamp(worldPos.y, worldMinBounds.y, worldMaxBounds.y);

        float mapWidth = (nodeContainer as RectTransform).rect.width;
        float mapHeight = (nodeContainer as RectTransform).rect.height;

        float normalizedX = Mathf.InverseLerp(worldMinBounds.x, worldMaxBounds.x, worldPos.x);
        float normalizedY = Mathf.InverseLerp(worldMinBounds.y, worldMaxBounds.y, worldPos.y);

        return new Vector2(normalizedX * mapWidth, normalizedY * mapHeight);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Vector3 bl = new Vector3(worldMinBounds.x, 0, worldMinBounds.y);
        Vector3 tr = new Vector3(worldMaxBounds.x, 0, worldMaxBounds.y);
        Vector3 tl = new Vector3(worldMinBounds.x, 0, worldMaxBounds.y);
        Vector3 br = new Vector3(worldMaxBounds.x, 0, worldMinBounds.y);

        Gizmos.DrawLine(bl, tl);
        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
    }
}