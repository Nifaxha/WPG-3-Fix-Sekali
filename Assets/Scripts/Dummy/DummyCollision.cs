using UnityEngine;

public class DummyCollision : MonoBehaviour
{
    public SubmarineCoordinates movementScript;
    // public CameraShaker cameraShaker; // Kita nonaktifkan dulu untuk fokus pada tabrakan

    void OnCollisionEnter(Collision collision)
    {
        // Pesan ini akan muncul SETIAP KALI dummy menyentuh collider APAPUN
        Debug.Log($"<color=red><b>TABRAKAN TERDETEKSI!</b></color> Objek yang ditabrak: {collision.gameObject.name}, Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");

        // Cek jika menabrak dinding di layer "Walls"
        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            Debug.Log("<color=yellow>Objek yang ditabrak adalah DINDING.</color>");

            if (movementScript != null)
            {
                if (movementScript.currentSpeed > 0)
                {
                    Debug.Log("<color=orange>Menghentikan pergerakan maju... Mengatur isBlocked = true dan currentSpeed = 0.</color>");
                    movementScript.isBlocked = true;
                    movementScript.currentSpeed = 0f;
                }
                else
                {
                    Debug.Log("Tabrakan terdeteksi, tetapi kapal tidak sedang bergerak maju (speed <= 0), jadi tidak dihentikan.");
                }
            }
            else
            {
                Debug.LogError("Referensi 'movementScript' di DummyCollision KOSONG!");
            }
        }
    }
}