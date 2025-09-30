using UnityEngine;
using System.Collections.Generic;

public class PhysicalBlipController : MonoBehaviour
{
    [Header("Referensi")]
    public MeshWallRadar radarSystem;
    public GameObject blipPrefab;

    [Header("Pengaturan Tampilan")]
    public float radarDisplayRadius = 2.18f;
    public float blipHoverOffset = 0.05f;

    // ---- BARU: Variabel untuk memperbaiki rotasi blip ----
    [Tooltip("Ubah nilai ini (misal: 0, 90, 180, -90) jika arah blip terbalik atau tidak sesuai.")]
    public float blipRotationOffset = 0f;

    private float surfaceYPosition;
    private Dictionary<MeshWallRadar.RadarBlip, GameObject> activeBlipObjects = new Dictionary<MeshWallRadar.RadarBlip, GameObject>();
    private List<MeshWallRadar.RadarBlip> blipsToRemove = new List<MeshWallRadar.RadarBlip>();

    void Start()
    {
        surfaceYPosition = transform.localScale.y / 2f;
    }

    void Update()
    {
        if (radarSystem == null || blipPrefab == null) return;

        List<MeshWallRadar.RadarBlip> currentBlips = radarSystem.GetActiveBlips();
        Vector2 submarinePos = new Vector2(radarSystem.submarineCoords.currentX, radarSystem.submarineCoords.currentZ);

        blipsToRemove.Clear();
        foreach (var pair in activeBlipObjects)
        {
            if (!currentBlips.Contains(pair.Key))
            {
                blipsToRemove.Add(pair.Key);
            }
        }

        foreach (var blip in blipsToRemove)
        {
            if (activeBlipObjects[blip] != null)
            {
                Destroy(activeBlipObjects[blip]);
            }
            activeBlipObjects.Remove(blip);
        }

        foreach (var blip in currentBlips)
        {
            Vector2 relativePos = (blip.worldPosition - submarinePos) / radarSystem.radarRange;

            // ---- DIUBAH: Terapkan rotasi offset pada posisi relatif ----
            // Kita akan memutar vektor posisi relatif berdasarkan offset yang diberikan
            float angleRad = blipRotationOffset * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);
            Vector2 rotatedRelativePos = new Vector2(
                relativePos.x * cos - relativePos.y * sin,
                relativePos.x * sin + relativePos.y * cos
            );
            // --------------------------------------------------------

            Vector3 blipLocalPosition = new Vector3(
                -rotatedRelativePos.x * radarDisplayRadius,
                surfaceYPosition + blipHoverOffset,
                rotatedRelativePos.y * radarDisplayRadius
            );

            if (activeBlipObjects.ContainsKey(blip))
            {
                activeBlipObjects[blip].transform.position = transform.TransformPoint(blipLocalPosition);
            }
            else
            {
                GameObject newBlipObject = Instantiate(blipPrefab, transform);
                newBlipObject.transform.position = transform.TransformPoint(blipLocalPosition);
                activeBlipObjects.Add(blip, newBlipObject);
            }
        }
    }
}