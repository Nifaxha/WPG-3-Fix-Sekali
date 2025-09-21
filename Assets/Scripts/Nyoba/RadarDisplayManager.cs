using UnityEngine;
using UnityEngine.UI;

public class RadarDisplayManager : MonoBehaviour
{
    [Header("Auto Create Radar UI")]
    public bool createUIOnStart = true;
    public MeshWallRadar radarSystem; // Reference ke radar system

    [Header("Radar UI Settings")]
    public Vector2 radarPosition = new Vector2(100, 100); // Posisi radar di screen
    public float radarSize = 200f; // Ukuran radar
    public Color radarBackgroundColor = new Color(0f, 0.3f, 0f, 0.8f);
    public Color scanLineColor = Color.green;
    public Color blipColor = Color.green;
    public Color gridColor = new Color(0f, 1f, 0f, 0.3f);

    [Header("References (Will be auto-filled)")]
    public Canvas radarCanvas;
    public RectTransform radarContainer;
    public Image radarBackground;
    public Transform scanLineTransform;
    public GameObject blipPrefab;

    void Start()
    {
        if (createUIOnStart)
        {
            CreateRadarUI();
        }

        // Auto-find radar system jika belum di-set
        if (radarSystem == null)
            radarSystem = FindObjectOfType<MeshWallRadar>();
    }

    [ContextMenu("Create Radar UI")]
    public void CreateRadarUI()
    {
        // 1. BUAT CANVAS
        CreateRadarCanvas();

        // 2. BUAT RADAR CONTAINER
        CreateRadarContainer();

        // 3. BUAT RADAR BACKGROUND
        CreateRadarBackground();

        // 4. BUAT RADAR GRID
        CreateRadarGrid();

        // 5. BUAT SCAN LINE
        CreateScanLine();

        // 6. BUAT BLIP PREFAB
        //CreateBlipPrefab();

        // 7. HUBUNGKAN KE RADAR SYSTEM
        ConnectToRadarSystem();

        Debug.Log("✅ Radar UI berhasil dibuat!");
    }

    void CreateRadarCanvas()
    {
        // Cari canvas yang sudah ada atau buat baru
        radarCanvas = FindObjectOfType<Canvas>();

        if (radarCanvas == null)
        {
            GameObject canvasGO = new GameObject("RadarCanvas");
            radarCanvas = canvasGO.AddComponent<Canvas>();
            radarCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            radarCanvas.sortingOrder = 100; // Tampil di atas UI lain

            // Add CanvasScaler untuk responsive UI
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Add GraphicRaycaster
            canvasGO.AddComponent<GraphicRaycaster>();
        }
    }

    void CreateRadarContainer()
    {
        GameObject containerGO = new GameObject("RadarSystem");
        containerGO.transform.SetParent(radarCanvas.transform, false);

        radarContainer = containerGO.AddComponent<RectTransform>();

        // Posisi radar (top-left corner)
        radarContainer.anchorMin = new Vector2(0f, 1f);
        radarContainer.anchorMax = new Vector2(0f, 1f);
        radarContainer.pivot = new Vector2(0f, 1f);
        radarContainer.anchoredPosition = radarPosition;
        radarContainer.sizeDelta = new Vector2(radarSize, radarSize);
    }

    void CreateRadarBackground()
    {
        GameObject bgGO = new GameObject("RadarBackground");
        bgGO.transform.SetParent(radarContainer, false);

        RectTransform bgRect = bgGO.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        radarBackground = bgGO.AddComponent<Image>();
        radarBackground.color = radarBackgroundColor;
        radarBackground.sprite = CreateCircleSprite();
        radarBackground.type = Image.Type.Simple;
    }

    void CreateRadarGrid()
    {
        GameObject gridGO = new GameObject("RadarGrid");
        gridGO.transform.SetParent(radarContainer, false);

        RectTransform gridRect = gridGO.AddComponent<RectTransform>();
        gridRect.anchorMin = Vector2.zero;
        gridRect.anchorMax = Vector2.one;
        gridRect.offsetMin = Vector2.zero;
        gridRect.offsetMax = Vector2.zero;

        // Buat lingkaran konsentris
        CreateConcentricCircles(gridGO.transform);

        // Buat garis silang
        CreateCrossLines(gridGO.transform);
    }

    void CreateConcentricCircles(Transform parent)
    {
        for (int i = 1; i <= 3; i++)
        {
            GameObject circleGO = new GameObject($"GridCircle_{i}");
            circleGO.transform.SetParent(parent, false);

            RectTransform circleRect = circleGO.AddComponent<RectTransform>();
            circleRect.anchorMin = new Vector2(0.5f, 0.5f);
            circleRect.anchorMax = new Vector2(0.5f, 0.5f);
            circleRect.pivot = new Vector2(0.5f, 0.5f);
            circleRect.anchoredPosition = Vector2.zero;

            float size = (radarSize * i) / 3f;
            circleRect.sizeDelta = new Vector2(size, size);

            Image circleImage = circleGO.AddComponent<Image>();
            circleImage.color = gridColor;
            circleImage.sprite = CreateCircleOutlineSprite();
        }
    }

    void CreateCrossLines(Transform parent)
    {
        // Horizontal line
        CreateGridLine(parent, "HorizontalLine", new Vector2(radarSize, 2f));

        // Vertical line
        CreateGridLine(parent, "VerticalLine", new Vector2(2f, radarSize));
    }

    void CreateGridLine(Transform parent, string name, Vector2 size)
    {
        GameObject lineGO = new GameObject(name);
        lineGO.transform.SetParent(parent, false);

        RectTransform lineRect = lineGO.AddComponent<RectTransform>();
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.pivot = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = size;

        Image lineImage = lineGO.AddComponent<Image>();
        lineImage.color = gridColor;
    }

    void CreateScanLine()
    {
        GameObject scanGO = new GameObject("ScanLine");
        scanGO.transform.SetParent(radarContainer, false);

        RectTransform scanRect = scanGO.AddComponent<RectTransform>();
        scanRect.anchorMin = new Vector2(0.5f, 0.5f);
        scanRect.anchorMax = new Vector2(0.5f, 0.5f);
        scanRect.pivot = new Vector2(0f, 0.5f); // Pivot di ujung kiri untuk rotasi
        scanRect.anchoredPosition = Vector2.zero;
        scanRect.sizeDelta = new Vector2(radarSize * 0.5f, 3f); // Panjang = radius, tinggi = 3px

        Image scanImage = scanGO.AddComponent<Image>();
        scanImage.color = scanLineColor;

        scanLineTransform = scanGO.transform;
    }

    void CreateBlipPrefab()
    {
        GameObject prefabGO = new GameObject("BlipPrefab");

        RectTransform blipRect = prefabGO.AddComponent<RectTransform>();
        blipRect.sizeDelta = new Vector2(6f, 6f); // Ukuran blip 6x6 pixel

        Image blipImage = prefabGO.AddComponent<Image>();
        blipImage.color = blipColor;
        blipImage.sprite = CreateCircleSprite();

        // Buat jadi prefab (inactive)
        prefabGO.SetActive(false);
        blipPrefab = prefabGO;
    }

    void ConnectToRadarSystem()
    {
        if (radarSystem != null)
        {
            // Hubungkan UI ke radar system
            radarSystem.radarDisplay = radarContainer;
            radarSystem.scanLine = scanLineTransform;
            radarSystem.blipPrefab = blipPrefab;
            radarSystem.blipContainer = radarContainer; // Blip akan jadi child radar container
            radarSystem.radarDisplayRadius = radarSize * 0.5f; // Radius = setengah ukuran

            Debug.Log("✅ Radar system berhasil terhubung ke UI!");
        }
        else
        {
            Debug.LogWarning("⚠️ MeshWallRadar script tidak ditemukan! Drag ke radarSystem field.");
        }
    }

    // Helper methods untuk membuat sprite
    Sprite CreateCircleSprite()
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color[] colors = new Color[64 * 64];

        Vector2 center = new Vector2(32f, 32f);
        float radius = 30f;

        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                if (distance <= radius)
                    colors[y * 64 + x] = Color.white;
                else
                    colors[y * 64 + x] = Color.clear;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    Sprite CreateCircleOutlineSprite()
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        Color[] colors = new Color[64 * 64];

        Vector2 center = new Vector2(32f, 32f);
        float outerRadius = 30f;
        float innerRadius = 28f;

        for (int x = 0; x < 64; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                Vector2 pos = new Vector2(x, y);
                float distance = Vector2.Distance(pos, center);

                if (distance <= outerRadius && distance >= innerRadius)
                    colors[y * 64 + x] = Color.white;
                else
                    colors[y * 64 + x] = Color.clear;
            }
        }

        texture.SetPixels(colors);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
    }

    // Method untuk toggle radar visibility
    public void ToggleRadar(bool visible)
    {
        if (radarContainer != null)
            radarContainer.gameObject.SetActive(visible);
    }

    // Method untuk update warna radar
    public void UpdateRadarColors()
    {
        if (radarBackground != null)
            radarBackground.color = radarBackgroundColor;

        // Update warna grid dan scan line juga bisa ditambah di sini
    }
}