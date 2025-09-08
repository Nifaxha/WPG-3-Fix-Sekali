using UnityEngine;

public class SubmarineController : MonoBehaviour
{
    public float moveSpeed = 2f; // kecepatan per detik
    private Vector3 moveDirection = Vector3.zero;

    void Update()
    {
        // Gerakkan kapal selam sesuai arah
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
    }

    // Fungsi dipanggil dari tombol UI
    public void OnPressForward()
    {
        moveDirection = Vector3.forward;
    }

    public void OnPressBackward()
    {
        moveDirection = Vector3.back;
    }

    public void OnPressLeft()
    {
        moveDirection = Vector3.left;
    }

    public void OnPressRight()
    {
        moveDirection = Vector3.right;
    }

    public void OnPressUp()
    {
        moveDirection = Vector3.up;
    }

    public void OnPressDown()
    {
        moveDirection = Vector3.down;
    }

    // Saat tombol dilepas, hentikan gerakan
    public void OnRelease()
    {
        moveDirection = Vector3.zero;
    }
}
