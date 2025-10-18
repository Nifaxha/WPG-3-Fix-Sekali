using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapNode_Z : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Identity")]
    public int locationId;               // <— cocok dengan PhotoLocation.id
    [Header("Koordinat Target (X, Z)")]
    public Vector2 coordinateXZ;         // untuk tooltip X/Z

    [Header("Referensi UI")]
    public Image icon;
    public Sprite checkmarkSprite;

    private Sprite defaultSprite;
    private MapController2 controller;
    private bool isCompleted = false;

    void Awake()
    {
        if (icon == null) icon = GetComponent<Image>();
    }

    void Start()
    {
        controller = FindObjectOfType<MapController2>();
        defaultSprite = icon.sprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isCompleted) controller.ShowCoordinate(coordinateXZ.x, coordinateXZ.y);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller.HideCoordinate();
    }

    public void CompleteMission()
    {
        if (isCompleted) return;
        isCompleted = true;

        if (icon == null) icon = GetComponent<Image>();
        if (icon == null) { Debug.LogError("[MapNode_Z] Image hilang.", this); return; }
        if (checkmarkSprite == null) { Debug.LogError("[MapNode_Z] Checkmark sprite kosong.", this); return; }

        icon.enabled = true;
        icon.raycastTarget = false;
        icon.type = Image.Type.Simple;
        icon.preserveAspect = true;
        icon.color = Color.green;
        icon.sprite = checkmarkSprite;

        // Jangan SetNativeSize kalau pakai layout. Karena manual, boleh ukuran tetap.
        var rt = icon.rectTransform;
        if (rt.sizeDelta.magnitude < 1f) rt.sizeDelta = new Vector2(80, 80);

        gameObject.name = $"Node_{locationId}_DONE";
        Debug.Log($"[MapNode_Z] Node {locationId} ditandai Selesai ✅", this);
    }


}
