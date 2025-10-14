using UnityEngine;
using TMPro;

public class MapController2 : MonoBehaviour
{
    [Header("References")]
    public GameObject mapUI;
    public TextMeshProUGUI coordinateText;
    public GameObject dot; // tambahkan referensi dot crosshair

    private bool isOpen = false;

    void Start()
    {
        mapUI.SetActive(false);
        coordinateText.gameObject.SetActive(false);
        if (dot) dot.SetActive(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            isOpen = !isOpen;

            mapUI.SetActive(isOpen);
            coordinateText.gameObject.SetActive(isOpen);
            if (dot) dot.SetActive(!isOpen);

            Cursor.visible = isOpen;
            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;

            // Nonaktifkan kontrol player saat buka map
            FindObjectOfType<InputManager>().enabled = !isOpen;
        }
    }

    public void ShowCoordinate(Vector2 coord)
    {
        coordinateText.text = $"X: {coord.x}  Y: {coord.y}";
    }

    public void HideCoordinate()
    {
        coordinateText.text = "";
    }
}
