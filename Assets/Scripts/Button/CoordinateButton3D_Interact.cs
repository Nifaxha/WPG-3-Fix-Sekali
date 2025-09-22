using UnityEngine;

public class CoordinateButton3D_Interact : MonoBehaviour
{
    public string direction;   // "Forward", "Backward", "Left", "Right"
    public float speed = 15f;
    public float pressDepth = 0.05f; // seberapa dalam tombol masuk

    private SubmarineCoordinates coordSystem;
    private Vector3 originalPos;
    private bool isHeld = false;

    void Start()
    {
        coordSystem = FindObjectOfType<SubmarineCoordinates>();
        originalPos = transform.localPosition;
    }

    void Update()
    {
        if (isHeld && coordSystem != null)
        {
            coordSystem.ChangeCoordinate(direction, speed);
        }
    }

    public void PressButton()
    {
        if (isHeld) return;

        isHeld = true;
        AnimateButton(true);
    }

    public void ReleaseButton()
    {
        isHeld = false;
        AnimateButton(false);
    }

    private void AnimateButton(bool pressed)
    {
        transform.localPosition = pressed
            ? originalPos + new Vector3(0, -pressDepth, 0)
            : originalPos;
    }
}