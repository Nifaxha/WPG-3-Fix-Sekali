using UnityEngine;
using TMPro;

public class SubmarineCoordinates : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI textX;
    public TextMeshProUGUI textZ;

    [Header("Current Coordinates")]
    public float currentX = 0f;
    public float currentZ = 0f;

    void Start()
    {
        UpdateCoordinateText();
    }

    public void ChangeCoordinate(string direction, float speed)
    {
        switch (direction)
        {
            case "Forward":
                currentZ += speed * Time.deltaTime;
                break;
            case "Backward":
                currentZ -= speed * Time.deltaTime;
                break;
            case "Left":
                currentX -= speed * Time.deltaTime;
                break;
            case "Right":
                currentX += speed * Time.deltaTime;
                break;
        }

        UpdateCoordinateText();
    }

    private void UpdateCoordinateText()
    {
        if (textX != null)
            textX.text = $"X: {currentX:F2}";

        if (textZ != null)
            textZ.text = $"Z: {currentZ:F2}";
    }
}
