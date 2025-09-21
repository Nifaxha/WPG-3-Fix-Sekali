using UnityEngine;
using System.Collections.Generic;

public class RadarUIController : MonoBehaviour
{
    public Transform player;
    public RadarDetector detector;
    public GameObject blipPrefab;
    public float blipScale = 2f; // Sesuaikan untuk memperbesar/memperkecil blip

    private List<GameObject> activeBlips = new List<GameObject>();

    void Update()
    {
        // Hapus blip yang sudah ada
        foreach (var blip in activeBlips)
        {
            Destroy(blip);
        }
        activeBlips.Clear();

        // Loop melalui objek yang terdeteksi dari detektor
        foreach (var detectedObject in detector.detectedObjects)
        {
            // Jika objek masih aktif, buat blip baru
            if (detectedObject != null)
            {
                // Hitung posisi relatif di dunia 3D
                Vector3 relativePos = detectedObject.transform.position - player.position;

                // Hitung posisi 2D untuk UI
                Vector2 blipPosition = new Vector2(relativePos.x * blipScale, relativePos.z * blipScale);

                // Buat dan atur posisi blip
                GameObject newBlip = Instantiate(blipPrefab, transform);
                newBlip.transform.localPosition = blipPosition;
                activeBlips.Add(newBlip);
            }
        }
    }
}