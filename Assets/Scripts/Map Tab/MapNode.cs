using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MapNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Vector2 coordinate;
    public Image icon;
    public Sprite checkmarkSprite;
    private Sprite defaultSprite;

    private MapController2 controller;
    private bool isCompleted = false;

    void Start()
    {
        controller = FindObjectOfType<MapController2>();
        defaultSprite = icon.sprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isCompleted)
            controller.ShowCoordinate(coordinate);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        controller.HideCoordinate();
    }

    public void CompleteMission()
    {
        isCompleted = true;
        icon.sprite = checkmarkSprite;
        icon.color = Color.green;
    }
}
