using UnityEngine;

public class SimpleRadarTest : MonoBehaviour
{
    public LayerMask wallLayer;
    public float radarRange = 50f;

    void Update()
    {
        // Arah raycast: Lurus ke depan dari objek ini (sumbu Z positif)
        Vector3 forwardDirection = transform.forward;
        Vector3 originPosition = transform.position;

        // Visualisasi Raycast di Scene View agar kita bisa melihatnya
        Debug.DrawRay(originPosition, forwardDirection * radarRange, Color.green);

        // Lakukan Raycast
        RaycastHit hit;
        if (Physics.Raycast(originPosition, forwardDirection, out hit, radarRange, wallLayer))
        {
            // Jika berhasil mengenai sesuatu di layer yang benar
            Debug.Log("<color=lime>? BERHASIL MENGENAI DINDINGUJI!</color> Objek: " + hit.collider.name);

            // Ubah warna visualisasi ray menjadi merah jika berhasil
            Debug.DrawRay(originPosition, forwardDirection * hit.distance, Color.red);
        }
        else
        {
            // Jika tidak mengenai apa-apa
            // (Tidak perlu log di sini agar console tidak penuh)
        }
    }

    // Fungsi ini akan mencetak log setiap detik untuk memastikan script berjalan
    void Start()
    {
        InvokeRepeating("CheckIfRunning", 1.0f, 1.0f);
    }

    void CheckIfRunning()
    {
        Debug.Log("SimpleRadarTest Update() sedang berjalan...");
    }
}   