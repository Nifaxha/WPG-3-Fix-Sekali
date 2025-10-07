using UnityEngine;
using UnityEngine.EventSystems;

public class CoordinateBox : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Vector2 coordinates;
    private CoordinateMapController mapController;

    public void Initialize(CoordinateMapController controller, Vector2 coords)
    {
        mapController = controller;
        coordinates = coords;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        mapController.ShowCoordinates(coordinates);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mapController.HideCoordinates();
    }
}