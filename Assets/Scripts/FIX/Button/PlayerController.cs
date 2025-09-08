using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float interactionDistance = 3f; // Jarak maksimum untuk berinteraksi

    void Update()
    {
        // Mendeteksi tombol 'F' ditekan
        if (Input.GetKeyDown(KeyCode.F))
        {
            // Buat Raycast dari kamera pemain ke depan
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            // Periksa apakah Raycast mengenai objek yang memiliki skrip ButtonInteractable
            if (Physics.Raycast(ray, out hit, interactionDistance))
            {
                ButtonInteractable button = hit.collider.GetComponent<ButtonInteractable>();
                if (button != null)
                {
                    // Beri tahu tombol untuk memulai animasi "ditekan"
                    button.Press();
                }
            }
        }

        // Mendeteksi tombol 'F' dilepas
        if (Input.GetKeyUp(KeyCode.F))
        {
            Ray ray = new Ray(transform.position, transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, interactionDistance))
            {
                ButtonInteractable button = hit.collider.GetComponent<ButtonInteractable>();
                if (button != null)
                {
                    // Beri tahu tombol untuk kembali ke animasi "diam"
                    button.Release();
                }
            }
        }
    }
}