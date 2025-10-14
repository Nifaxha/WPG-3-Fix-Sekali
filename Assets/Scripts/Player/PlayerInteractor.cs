using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public float interactDistance = 3f;
    [Tooltip("Pilih layer 'Interactable' di inspector")]
    public LayerMask interactableLayer;

    // simpan target saat ini untuk handle highlight & release
    private MonoBehaviour currentTarget;

    void Update()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance, interactableLayer))
        {
            // Cek objek apa yang kena ray
            var coordBtn = hit.collider.GetComponent<CoordinateButton3D_Interact>();
            var stopBtn = hit.collider.GetComponent<StopButton3D_Interact>();
            var stopLever = hit.collider.GetComponent<StopLever3D_Interact>();

            // Matikan highlight target lama jika berpindah
            if (currentTarget != null && currentTarget != (MonoBehaviour)coordBtn &&
                                         currentTarget != (MonoBehaviour)stopBtn &&
                                         currentTarget != (MonoBehaviour)stopLever)
            {
                ReleaseCurrentTarget(); // juga mematikan highlight
                currentTarget = null;
            }

            // Urutan cek bebas—yang penting SetHighlight & Press sesuai tipenya
            if (coordBtn != null)
            {
                coordBtn.SetHighlight(true);
                currentTarget = coordBtn;

                // tombol koordinat biasanya “ditahan”
                if (Input.GetKey(KeyCode.E))
                    coordBtn.PressButton();
                else if (Input.GetKeyUp(KeyCode.E))
                    coordBtn.ReleaseButton();
            }
            else if (stopBtn != null)
            {
                stopBtn.SetHighlight(true);
                currentTarget = stopBtn;

                // tombol stop (bukan lever) cukup sekali tekan
                if (Input.GetKeyDown(KeyCode.E))
                    stopBtn.PressButton();
                if (Input.GetKeyUp(KeyCode.E))
                    stopBtn.ReleaseButton();
            }
            else if (stopLever != null)
            {
                stopLever.SetHighlight(true);
                currentTarget = stopLever;

                // LEVER: toggle sekali tekan
                if (Input.GetKeyDown(KeyCode.E))
                    stopLever.PressButton();
                // tidak perlu ReleaseButton untuk lever, tapi aman kalau mau ditambah
            }
        }
        else
        {
            // Tidak kena apa-apa → lepas target & matikan highlight
            ReleaseCurrentTarget();
            currentTarget = null;
        }
    }

    void ReleaseCurrentTarget()
    {
        if (currentTarget is CoordinateButton3D_Interact cb)
        {
            cb.SetHighlight(false);
            cb.ReleaseButton();
        }
        else if (currentTarget is StopButton3D_Interact sb)
        {
            sb.SetHighlight(false);
            sb.ReleaseButton();
        }
        else if (currentTarget is StopLever3D_Interact sl)
        {
            sl.SetHighlight(false);
            // lever tidak perlu ReleaseButton
        }
    }
}