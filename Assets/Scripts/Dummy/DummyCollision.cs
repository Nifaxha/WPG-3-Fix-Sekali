using UnityEngine;

public class DummyCollision : MonoBehaviour
{
    public SubmarineCoordinates movementScript;
    public SubmarineHealth healthSystem; // Referensi ke health system

    private int wallCollisionCount = 0;
    private Vector3 lastCollisionNormal = Vector3.zero;

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"<color=red><b>TABRAKAN TERDETEKSI!</b></color> Objek yang ditabrak: {collision.gameObject.name}, Layer: {LayerMask.LayerToName(collision.gameObject.layer)}");

        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            wallCollisionCount++;

            // Ambil normal vector dari collision (arah tegak lurus dinding)
            if (collision.contacts.Length > 0)
            {
                lastCollisionNormal = collision.contacts[0].normal;
                Debug.Log($"<color=magenta>Normal collision: {lastCollisionNormal}</color>");
            }

            Debug.Log($"<color=yellow>Menabrak DINDING. Total collision: {wallCollisionCount}</color>");

            if (movementScript != null)
            {
                movementScript.isBlocked = true;
                movementScript.currentSpeed = 0f;
                movementScript.collisionNormal = lastCollisionNormal;
                Debug.Log("<color=orange>Menghentikan pergerakan dan mengatur isBlocked = true.</color>");
            }
            else
            {
                Debug.LogError("Referensi 'movementScript' di DummyCollision KOSONG!");
            }
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            // Update normal vector setiap frame untuk collision yang berubah
            if (collision.contacts.Length > 0)
            {
                lastCollisionNormal = collision.contacts[0].normal;
            }

            if (movementScript != null)
            {
                movementScript.isBlocked = true;
                movementScript.collisionNormal = lastCollisionNormal;

                // Cek apakah kapal mencoba bergerak ke arah dinding (hanya cek speed)
                if (movementScript.currentSpeed > 0)
                {
                    Vector3 moveDirection = movementScript.transform.forward * movementScript.currentSpeed;

                    // Jika gerakan mengarah ke dinding (dot product negatif), paksa stop
                    float dotProduct = Vector3.Dot(moveDirection.normalized, lastCollisionNormal);
                    if (dotProduct < 0)
                    {
                        movementScript.currentSpeed = 0f;
                        Debug.Log("<color=red>Mencegah gerakan ke arah dinding!</color>");
                    }
                }
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        Debug.Log($"<color=green>KELUAR DARI COLLISION dengan: {collision.gameObject.name}</color>");

        if (collision.gameObject.layer == LayerMask.NameToLayer("Walls"))
        {
            wallCollisionCount--;
            Debug.Log($"<color=cyan>Keluar dari dinding. Sisa collision: {wallCollisionCount}</color>");

            if (wallCollisionCount <= 0)
            {
                wallCollisionCount = 0;

                if (movementScript != null)
                {
                    movementScript.isBlocked = false;
                    movementScript.collisionNormal = Vector3.zero;
                    Debug.Log("<color=lime><b>Kapal bebas! isBlocked = false.</b></color>");
                }
            }
        }
    }
}