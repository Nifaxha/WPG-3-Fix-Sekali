using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshRenderer))]
public class Radar3DController : MonoBehaviour
{
    [Header("Referensi")]
    public MeshWallRadar radarSystem; // Drag script MeshWallRadar Anda ke sini

    [Header("Pengaturan Visual")]
    public Color radarColor = Color.green;
    public float sweepSpeed = 1f;

    private Material radarMaterial;
    private float currentSweepAngle = 0f;

    // Texture untuk menyimpan data posisi blip
    private Texture2D blipTexture;
    private const int BLIP_TEXTURE_WIDTH = 128; // Maksimal 128 blip bisa ditampilkan
    private Color[] blipTextureData = new Color[BLIP_TEXTURE_WIDTH];

    void Start()
    {
        // Ambil material dari objek ini untuk dimodifikasi
        radarMaterial = GetComponent<MeshRenderer>().material;

        // Inisialisasi texture untuk blip
        blipTexture = new Texture2D(BLIP_TEXTURE_WIDTH, 1, TextureFormat.RGBAFloat, false);

        // Hubungkan properti shader dengan script
        radarMaterial.SetColor("_RadarColor", radarColor);
        radarMaterial.SetFloat("_RadarRange", radarSystem.radarRange);
        radarMaterial.SetTexture("_BlipData", blipTexture);
    }

    void Update()
    {
        // 1. Update Garis Sapu (Sweep)
        currentSweepAngle = (currentSweepAngle + sweepSpeed * 360f * Time.deltaTime) % 360f;
        radarMaterial.SetFloat("_SweepAngle", currentSweepAngle);

        // 2. Update Blips
        UpdateBlipTexture();
    }

    void UpdateBlipTexture()
    {
        if (radarSystem == null || radarSystem.submarineCoords == null) return;

        // Kosongkan data texture lama
        System.Array.Clear(blipTextureData, 0, BLIP_TEXTURE_WIDTH);

        Vector2 submarinePos = new Vector2(radarSystem.submarineCoords.currentX, radarSystem.submarineCoords.currentZ);
        List<MeshWallRadar.RadarBlip> activeBlips = radarSystem.GetActiveBlips(); // Anda perlu membuat fungsi ini di MeshWallRadar

        int blipCount = Mathf.Min(activeBlips.Count, BLIP_TEXTURE_WIDTH);
        radarMaterial.SetFloat("_HitCount", blipCount);

        for (int i = 0; i < blipCount; i++)
        {
            // Konversi posisi dunia blip ke posisi relatif terhadap kapal selam (-1 sampai 1)
            Vector2 relativePos = (activeBlips[i].worldPosition - submarinePos) / radarSystem.radarRange;

            // Simpan posisi X dan Z ke dalam channel warna R dan G di texture
            // Simpan sisa waktu blip di channel B
            blipTextureData[i] = new Color(-relativePos.x, relativePos.y, activeBlips[i].remainingTime / radarSystem.blipLifetime, 1);
        }

        // Terapkan data baru ke texture dan kirim ke shader
        blipTexture.SetPixels(blipTextureData);
        blipTexture.Apply();
    }
}