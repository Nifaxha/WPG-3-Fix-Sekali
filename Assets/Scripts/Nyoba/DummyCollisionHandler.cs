using UnityEngine;

public class DummyCollisionHandler : MonoBehaviour
{
    public SubmarineCoordinates submarine; // drag SubmarineCoordinates ke sini

    // dipanggil otomatis oleh Unity saat collider dummy menyentuh collider lain
    private void OnCollisionEnter(Collision collision)
    {
        // cek apakah yang ditabrak layer "Wall"
        if (((1 << collision.gameObject.layer) & submarine.wallLayer) != 0)
        {
            // set speed ke 0
            submarine.currentSpeed = 0f;

            // kalau mau efek lain misalnya getar lebih besar
            // submarine.TriggerShake(0.05f, 0.2f);

            Debug.Log("Dummy tabrak dinding -> speed direset 0");
        }
    }
}
