using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CoordinateMapController : MonoBehaviour
{
    [Header("Referensi UI")]
    public GameObject mapPanel;
    public TextMeshProUGUI coordinateDisplayText;
    public RectTransform gridContainer;
    public GameObject coordinateBoxPrefab;

    [Header("Pengaturan Grid")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public int coordinateStep = 500;
    public Vector2 startCoordinates = new Vector2(-2500, -2500);

    // Dictionary untuk menyimpan referensi ke setiap kotak yang dibuat
    private Dictionary<Vector2, GameObject> coordinateBoxObjects = new Dictionary<Vector2, GameObject>();

    void Start()
    {
        GenerateGrid();
        mapPanel.SetActive(false);
    }

    void OnEnable()
    {
        // Berlangganan event 'OnPhotoTaken'
        PhotoManager.OnPhotoTaken += MarkCoordinateAsCompleted;
    }

    void OnDisable()
    {
        // Berhenti berlangganan saat script dimatikan untuk menghindari error
        PhotoManager.OnPhotoTaken -= MarkCoordinateAsCompleted;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            mapPanel.SetActive(!mapPanel.activeSelf);
        }
    }

    void GenerateGrid()
    {
        for (int y = 0; y < gridHeight; y++)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                // Buat instance dari prefab
                GameObject box = Instantiate(coordinateBoxPrefab, gridContainer);

                // Hitung koordinat untuk kotak ini
                Vector2 coords = new Vector2(
                    startCoordinates.x + (x * coordinateStep),
                    startCoordinates.y + (y * coordinateStep)
                );

                // Inisialisasi skrip di dalam kotak
                box.GetComponent<CoordinateBox>().Initialize(this, coords);

                // Simpan referensi ke kotak ini
                coordinateBoxObjects[coords] = box;
            }
        }
    }

    // Fungsi yang dipanggil oleh CoordinateBox saat di-hover
    public void ShowCoordinates(Vector2 coords)
    {
        coordinateDisplayText.text = $"X: {coords.x} | Z: {coords.y}";
    }

    public void HideCoordinates()
    {
        coordinateDisplayText.text = "";
    }

    // Fungsi yang dipanggil saat event OnPhotoTaken terjadi
    public void MarkCoordinateAsCompleted(Vector2 photoWorldCoords)
    {
        Vector2 closestCoord = Vector2.zero;
        float minDistance = float.MaxValue;

        // Cari kotak koordinat terdekat dari lokasi foto
        foreach (Vector2 gridCoord in coordinateBoxObjects.Keys)
        {
            float distance = Vector2.Distance(photoWorldCoords, gridCoord);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestCoord = gridCoord;
            }
        }

        // Cek apakah lokasi foto cukup dekat dengan salah satu titik grid
        if (minDistance < (coordinateStep / 2f))
        {
            // Ambil GameObject kotak yang sesuai
            GameObject boxToMark = coordinateBoxObjects[closestCoord];

            // Cari dan aktifkan gambar centang di dalamnya
            Transform checkmark = boxToMark.transform.Find("Checkmark_Image");
            if (checkmark != null)
            {
                checkmark.gameObject.SetActive(true);
            }
        }
    }
}