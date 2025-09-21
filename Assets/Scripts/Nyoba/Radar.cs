using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Radar : MonoBehaviour
{
    public Transform player;              // PlayerAnchor
    public float radarRange = 50f;
    public RectTransform radarBackground;
    public GameObject radarBlipPrefab;
    public string wallTag = "Wall";

    private List<Transform> wallObjects = new List<Transform>();
    private List<GameObject> blipInstances = new List<GameObject>();

    void Start()
    {
        // Cari semua tembok berdasarkan tag
        GameObject[] walls = GameObject.FindGameObjectsWithTag(wallTag);
        foreach (var wall in walls)
        {
            wallObjects.Add(wall.transform);
            blipInstances.Add(Instantiate(radarBlipPrefab, radarBackground));
        }
    }

    void Update()
    {
        for (int i = 0; i < wallObjects.Count; i++)
            UpdateBlip(wallObjects[i], blipInstances[i]);
    }

    void UpdateBlip(Transform wall, GameObject blip)
    {
        Vector3 relative = wall.position - player.position;
        Vector2 relative2D = new Vector2(relative.x, relative.z);

        if (relative2D.magnitude > radarRange)
        {
            blip.SetActive(false);
            return;
        }

        blip.SetActive(true);
        float scaledX = (relative2D.x / radarRange) * (radarBackground.rect.width / 2);
        float scaledY = (relative2D.y / radarRange) * (radarBackground.rect.height / 2);
        blip.GetComponent<RectTransform>().anchoredPosition = new Vector2(scaledX, scaledY);
    }
}
