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
        if (icon == null)
        {
            Debug.LogError("[MapNode_Z] Image (icon) tidak ditemukan.", this);
            return;
        }
        if (checkmarkSprite == null)
        {
            Debug.LogError("[MapNode_Z] Checkmark Sprite belum di-assign.", this);
            return;
        }

        // Pastikan kelihatan
        icon.enabled = true;
        icon.raycastTarget = false;                // biar hover tidak menghalangi node lain
        icon.type = Image.Type.Simple;             // hindari 'Filled' = 0
        icon.preserveAspect = true;
        icon.color = new Color(0f, 1f, 0f, 1f);    // full alpha
        icon.sprite = checkmarkSprite;

        // Jika parent pakai Grid/Horizontal/Vertical Layout, SetNativeSize akan di-override.
        // PILIH SALAH SATU:
        // 1) Kalau TIDAK pakai LayoutGroup:
        // icon.SetNativeSize();

        // 2) Kalau pakai GridLayoutGroup (sepertinya kamu pakai 'GridContainer'):
        // pakai sizeDelta sesuai cell (jangan SetNativeSize)
        var rt = icon.rectTransform;
        if (rt != null && rt.sizeDelta.magnitude < 1f)        // kalau kependek/0
            rt.sizeDelta = new Vector2(80, 80);                // samakan dg cell GridLayoutGroup

        gameObject.name = $"Node_{locationId}_DONE";
        Debug.Log($"[MapNode_Z] Node {locationId} ditandai Selesai ✅", this);
    }


}
