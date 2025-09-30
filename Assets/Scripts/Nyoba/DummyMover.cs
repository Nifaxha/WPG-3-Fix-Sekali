using UnityEngine;

public class DummyMover : MonoBehaviour
{
    // drag di Inspector ke script SubmarineCoordinates kamu
    public SubmarineCoordinates submarine;

    // drag di Inspector ke Rigidbody pada GameObject yang sama
    public Rigidbody rb;

    void FixedUpdate()
    {
        if (submarine == null || rb == null) return;

        Vector3 targetPos = new Vector3(submarine.currentX, 0f, submarine.currentZ);
        rb.MovePosition(targetPos);
    }
}
