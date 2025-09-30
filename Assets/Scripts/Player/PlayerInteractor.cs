using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float interactDistance = 3f; // jarak interaksi
    public LayerMask buttonLayer;       // layer khusus tombol

    private MonoBehaviour currentButton; // bisa menyimpan kedua tipe tombol

    void Update()
    {
        // Raycast ke depan kamera
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, buttonLayer))
        {
            // cek kedua tipe tombol
            var coordBtn = hit.collider.GetComponent<CoordinateButton3D_Interact>();
            var stopBtn = hit.collider.GetComponent<StopButton3D_Interact>();

            if (coordBtn != null)
            {
                currentButton = coordBtn;
            }
            else if (stopBtn != null)
            {
                currentButton = stopBtn;
            }
            else
            {
                ReleaseCurrentButton();
                currentButton = null;
            }
        }
        else
        {
            ReleaseCurrentButton();
            currentButton = null;
        }

        // Tekan & tahan E
        if (Input.GetKey(KeyCode.E))
        {
            if (currentButton is CoordinateButton3D_Interact cb)
                cb.PressButton();
            else if (currentButton is StopButton3D_Interact sb)
                sb.PressButton();
        }
        else
        {
            ReleaseCurrentButton();
        }
    }

    void ReleaseCurrentButton()
    {
        if (currentButton is CoordinateButton3D_Interact cb)
            cb.ReleaseButton();
        else if (currentButton is StopButton3D_Interact sb)
            sb.ReleaseButton();
    }
}
