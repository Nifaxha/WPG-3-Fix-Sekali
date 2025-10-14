using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MapController2 : MonoBehaviour
{
    [Header("UI References")]
    public GameObject mapUI;
    public TextMeshProUGUI coordinateText;
    public GameObject dot;                    // crosshair

    [Header("Auto Node")]
    public MapNode_Z nodePrefab;              // prefab kotak hitam
    public RectTransform nodeParent;          // container di dalam map UI

    [Header("Map Placement")]
    public Vector2 mapOrigin = Vector2.zero;  // world (X,Z) yang dianggap (0,0) peta
    public float mapScale = 1f;               // skala world→UI
    public Vector2 mapOffset = Vector2.zero;  // geser semua node di UI

    private bool isOpen = false;
    private Dictionary<int, MapNode_Z> nodeById = new();

    void Start()
    {
        mapUI.SetActive(false);
        if (coordinateText) coordinateText.gameObject.SetActive(false);
        if (dot) dot.SetActive(true);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        GenerateMapNodes();                   // ← bikin node dari PhotoManager
        PhotoManager.OnPhotoTakenById += HandlePhotoTakenById;
    }

    void OnDestroy()
    {
        PhotoManager.OnPhotoTakenById -= HandlePhotoTakenById;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isOpen = !isOpen;

            mapUI.SetActive(isOpen);
            if (coordinateText) coordinateText.gameObject.SetActive(isOpen);
            if (dot) dot.SetActive(!isOpen);

            Cursor.visible = isOpen;
            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;

            var inputMgr = FindObjectOfType<InputManager>();
            if (inputMgr) inputMgr.enabled = !isOpen;
        }
    }

    public void ShowCoordinate(float x, float z)
    {
        if (!coordinateText) return;
        coordinateText.text = $"X: {x}   Z: {z}";
    }

    public void HideCoordinate()
    {
        if (!coordinateText) return;
        coordinateText.text = "";
    }

    // ====== Auto Generate ======
    private void GenerateMapNodes()
    {
        var pm = FindObjectOfType<PhotoManager>();
        if (pm == null || nodePrefab == null || nodeParent == null)
        {
            Debug.LogWarning("[MapController2] Assign PhotoManager, nodePrefab, dan nodeParent.");
            return;
        }

        nodeById.Clear();
        foreach (var loc in pm.photoLocations)
        {
            var node = Instantiate(nodePrefab, nodeParent);
            node.locationId = loc.id;
            node.coordinateXZ = loc.coordinates;

            // world (X,Z) → UI anchoredPosition
            Vector2 uiPos = (loc.coordinates - mapOrigin) * mapScale + mapOffset;
            node.GetComponent<RectTransform>().anchoredPosition = uiPos;

            nodeById[loc.id] = node;

            // Jika sudah pernah difoto (ketika load/tes), langsung centang
            if (loc.hasBeenPhotographed) node.CompleteMission();
        }

        Debug.Log($"[MapController2] Generated {nodeById.Count} nodes from PhotoManager.");
    }

    // ====== Event Handler by ID ======
    private void HandlePhotoTakenById(int id)
    {
        if (nodeById.TryGetValue(id, out var node))
        {
            node.CompleteMission();
            Debug.Log($"[Map] Node {id} completed ✅");
        }
        else
        {
            Debug.LogWarning($"[Map] No node found for id {id}. Pastikan auto-generate berjalan.");
        }
    }
}
    