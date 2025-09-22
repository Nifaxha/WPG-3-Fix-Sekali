using UnityEngine;
using TMPro;

public class SubmarineCoordinates : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI textX;
    public TextMeshProUGUI textZ;
    public TextMeshProUGUI textSpeed;

    [Header("Current Coordinates")]
    public float currentX = 0f;
    public float currentZ = 0f;

    [Header("Movement Settings")]
    public float currentSpeed = 0f;
    public float acceleration = 1f;  // seberapa cepat speed naik/turun
    public float maxSpeed = 15f;

    void Update()
    {
        // Pergerakan otomatis berdasarkan speed
        currentZ += currentSpeed * Time.deltaTime;
        UpdateCoordinateText();
    }

    public void ChangeCoordinate(string direction, float speed)
    {
        switch (direction)
        {
            case "Left":
                currentX -= speed * Time.deltaTime;
                break;
            case "Right":
                currentX += speed * Time.deltaTime;
                break;
            case "Forward":
                IncreaseSpeed();
                break;
            case "Backward":
                DecreaseSpeed();
                break;
        }

        //WrapCoordinateX();
    }

    private void IncreaseSpeed()
    {
        currentSpeed += acceleration * Time.deltaTime;
        if (currentSpeed > maxSpeed)
            currentSpeed = maxSpeed;
    }

    private void DecreaseSpeed()
    {
        currentSpeed -= acceleration * Time.deltaTime;
        if (currentSpeed < -maxSpeed)
            currentSpeed = -maxSpeed;
    }

    //private void WrapCoordinateX()
    //{
    //    currentX = Mathf.Repeat(currentX, 360f);
    //}

    private void UpdateCoordinateText()
    {
        if (textX != null)
            textX.text = $"X: {currentX:F2}";

        if (textZ != null)
            textZ.text = $"Z: {currentZ:F2}";

        if (textSpeed != null)
            textSpeed.text = $"Knot: {currentSpeed:F2}";
    }
}