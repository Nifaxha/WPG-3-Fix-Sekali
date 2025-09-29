using System.Collections;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    [Header("Pengaturan Getaran")]
    public float duration = 0.5f; // Durasi getaran dalam detik
    public float magnitude = 0.1f; // Kekuatan getaran

    private Vector3 originalPosition;

    // Fungsi ini akan dipanggil oleh script DummyCollision
    public void StartShake()
    {
        // Simpan posisi awal kamera sebelum getaran
        originalPosition = transform.localPosition;
        StartCoroutine(Shake());
    }

    private IEnumerator Shake()
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // Hasilkan posisi acak di sekitar titik 0
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            // Terapkan getaran ke posisi lokal kamera
            transform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);

            elapsed += Time.deltaTime;

            // Tunggu frame berikutnya
            yield return null;
        }

        // Setelah selesai, kembalikan kamera ke posisi semula
        transform.localPosition = originalPosition;
    }
}