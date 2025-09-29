using UnityEngine;

public class DummyCollision : MonoBehaviour
{
    // Drag script pergerakan dummy Anda (yang mirip SubmarineCoordinates) ke sini
    public SubmarineCoordinates movementScript;

    // Drag Kamera Utama Anda (yang akan kita beri script shaker) ke sini
    public CameraShaker cameraShaker;

    void OnCollisionEnter(Collision collision)
    {
        // Cek jika menabrak dinding di layer "Walls"
        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            Debug.Log("DUMMY MENABRAK DINDING!");

            // 1. Hentikan pergerakan dummy
            if (movementScript != null)
            {
                movementScript.currentSpeed = 0f;
            }

            // 2. Perintahkan kamera untuk bergetar
            if (cameraShaker != null)
            {
                cameraShaker.StartShake();
            }
        }
    }
}