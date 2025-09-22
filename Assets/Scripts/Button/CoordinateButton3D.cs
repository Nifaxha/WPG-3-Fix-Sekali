using UnityEngine;

public class CoordinateButton3D : MonoBehaviour
{
    public string direction;   // "Forward", "Backward", "Left", "Right"
    public float speed = 15f;
    public float pressDepth = 0.05f; // seberapa dalam tombol masuk

    private bool isHeld = false;
    private SubmarineCoordinates coordSystem;
    private Vector3 originalPos;

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

    void OnMouseDown()
    {
        isHeld = true;
        AnimateButton(true);
    }

    void OnMouseUp()
    {
        isHeld = false;
        AnimateButton(false);
    }

    private void AnimateButton(bool pressed)
    {
        if (pressed)
            transform.localPosition = originalPos + new Vector3(0, -pressDepth, 0); // masuk ke dalam
        else
            transform.localPosition = originalPos; // kembali ke posisi awal
    }
}
