using UnityEngine;
using System.Collections.Generic;

public class PhysicalBlipController : MonoBehaviour
{
    [Header("Referensi")]
    public MeshWallRadar radarSystem;
    public GameObject blipPrefab; // Drag Prefab Blip 3D Anda ke sini

    [Header("Pengaturan Tampilan")]
    public float radarDisplayRadius = 2.18f; // Sesuaikan dengan setengah dari scale X/Z RadarScreen3D Anda

    // BARU: Variabel untuk mengatur seberapa tinggi blip mengambang di atas permukaan
    public float blipHoverOffset = 0.05f;

    // Variabel untuk menyimpan posisi Y permukaan yang dihitung secara otomatis
    private float surfaceYPosition;

    // Dictionary untuk melacak blip yang aktif
    private Dictionary<MeshWallRadar.RadarBlip, GameObject> activeBlipObjects = new Dictionary<MeshWallRadar.RadarBlip, GameObject>();
    private List<MeshWallRadar.RadarBlip> blipsToRemove = new List<MeshWallRadar.RadarBlip>();

    void Start()
    {
        // BARU: Hitung posisi Y permukaan atas radar secara otomatis
        // Asumsi modelnya adalah silinder standar dengan tinggi 2 unit, jadi puncaknya ada di Y=1.
        // Kita kalikan dengan setengah dari skala Y objek ini untuk menemukan permukaan atasnya.
        surfaceYPosition = transform.localScale.y / 2f;
    }

    void Update()
    {
        if (radarSystem == null || blipPrefab == null) return;

        List<MeshWallRadar.RadarBlip> currentBlips = radarSystem.GetActiveBlips();
        Vector2 submarinePos = new Vector2(radarSystem.submarineCoords.currentX, radarSystem.submarineCoords.currentZ);

        // Tandai blip yang sudah tidak aktif untuk dihapus
        blipsToRemove.Clear();
        foreach (var pair in activeBlipObjects)
        {
            if (!currentBlips.Contains(pair.Key))
            {
                blipsToRemove.Add(pair.Key);
            }
        }

        // Hapus blip yang sudah tidak aktif
        foreach (var blip in blipsToRemove)
        {
            Destroy(activeBlipObjects[blip]);
            activeBlipObjects.Remove(blip);
        }

        // Tambah atau update blip yang aktif
        foreach (var blip in currentBlips)
        {
            // Konversi posisi dunia ke posisi relatif di radar
            Vector2 relativePos = (blip.worldPosition - submarinePos) / radarSystem.radarRange;

            // DIUBAH: Gunakan surfaceYPosition yang sudah dihitung ditambah offset
            Vector3 blipLocalPosition = new Vector3(-relativePos.x * radarDisplayRadius, surfaceYPosition + blipHoverOffset, relativePos.y * radarDisplayRadius);

            if (activeBlipObjects.ContainsKey(blip))
            {
                // Jika blip sudah ada, update posisinya menggunakan koordinat dunia
                activeBlipObjects[blip].transform.position = transform.TransformPoint(blipLocalPosition);
            }
            else
            {
                // Jika blip baru, buat objeknya
                GameObject newBlipObject = Instantiate(blipPrefab, transform);
                // Atur posisinya menggunakan koordinat dunia
                newBlipObject.transform.position = transform.TransformPoint(blipLocalPosition);
                activeBlipObjects.Add(blip, newBlipObject);
            }
        }
    }
}