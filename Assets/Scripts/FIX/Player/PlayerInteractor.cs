using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float interactDistance = 3f; // jarak interaksi
    public LayerMask buttonLayer;       // layer khusus tombol

    private CoordinateButton3D_Interact currentButton;

    void Update()
    {
        // Raycast ke depan kamera
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, buttonLayer))
        {
            currentButton = hit.collider.GetComponent<CoordinateButton3D_Interact>();
        }
        else
        {
            // Kalau tidak menghadap tombol
            if (currentButton != null)
            {
                currentButton.ReleaseButton();
                currentButton = null;
            }
        }

        // Tekan & tahan E
        if (Input.GetKey(KeyCode.E))
        {
            if (currentButton != null)
            {
                currentButton.PressButton();
            }
        }
        else
        {
            if (currentButton != null)
            {
                currentButton.ReleaseButton();
            }
        }
    }
}
