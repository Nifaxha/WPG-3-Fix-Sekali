using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IronLungRadar : MonoBehaviour
{
    [Header("Radar Settings")]
    public SubmarineCoordinates submarineCoords; // Reference ke sistem koordinat Anda
    public float radarRange = 50f; // Jangkauan radar dalam satuan koordinat
    public LayerMask wallLayer = 1; // Layer untuk dinding
    public float scanSpeed = 1f; // Kecepatan scan radar
    public float blipLifetime = 3f; // Berapa lama blip muncul setelah scan

    [Header("Visual Components")]
    public Transform scanLine; // Garis scan radar yang berputar
    public GameObject blipPrefab; // Prefab untuk blip/titik radar
    public Transform blipContainer; // Parent untuk semua blip
    public AudioSource radarAudio; // Audio untuk suara radar
    public AudioClip blipSound; // Sound effect untuk blip

    [Header("Radar Display")]
    public RectTransform radarDisplay; // UI Canvas untuk radar
    public float radarDisplayRadius = 100f; // Radius tampilan radar di UI

    [Header("World Obstacles")]
    public List<WorldObstacle> worldObstacles = new List<WorldObstacle>(); // Daftar obstacle di world

    private float currentScanAngle = 0f;
    private List<RadarBlip> activeBlips = new List<RadarBlip>();

    [System.Serializable]
    public class WorldObstacle
    {
        public string name;
        public ObstacleShape shape = ObstacleShape.Point;

        [Header("Point Obstacle")]
        public Vector2 position; // Untuk obstacle titik
        public float size = 1f;

        [Header("Line/Wall Obstacle")]
        public Vector2 startPoint; // Titik awal dinding
        public Vector2 endPoint;   // Titik akhir dinding
        public float thickness = 1f; // Ketebalan dinding

        [Header("Rectangular Wall")]
        public Vector2 center;     // Pusat rectangle
        public Vector2 dimensions; // Panjang x Lebar
        public float rotation = 0f; // Rotasi dalam derajat

        public ObstacleType type = ObstacleType.Wall;

        public enum ObstacleShape
        {
            Point,      // Obstacle titik (original)
            Line,       // Dinding garis lurus
            Rectangle   // Dinding persegi panjang
        }

        public enum ObstacleType
        {
            Wall,
            Rock,
            Wreck,
            Other
        }
    }

    [System.Serializable]
    public class RadarBlip
    {
        public GameObject blipObject;
        public float remainingTime;
        public Vector2 worldPosition; // Posisi dalam koordinat world (X,Z)

        public RadarBlip(GameObject obj, float time, Vector2 pos)
        {
            blipObject = obj;
            remainingTime = time;
            worldPosition = pos;
        }
    }

    void Start()
    {
        // Cari SubmarineCoordinates jika belum di-set
        if (submarineCoords == null)
            submarineCoords = FindObjectOfType<SubmarineCoordinates>();

        // Setup blip container jika belum ada
        if (blipContainer == null)
        {
            GameObject container = new GameObject("RadarBlips");
            container.transform.SetParent(radarDisplay);
            blipContainer = container.transform;
        }

        // Setup obstacle default jika kosong
        if (worldObstacles.Count == 0)
        {
            SetupDefaultObstacles();
        }
    }

    void Update()
    {
        UpdateScanLine();
        ScanForObstacles();
        UpdateBlips();
    }

    void UpdateScanLine()
    {
        // Rotasi garis scan radar
        currentScanAngle += scanSpeed * Time.deltaTime * 360f;
        if (currentScanAngle >= 360f)
            currentScanAngle -= 360f;

        if (scanLine != null)
        {
            scanLine.rotation = Quaternion.Euler(0, 0, -currentScanAngle);
        }
    }

    void ScanForObstacles()
    {
        if (submarineCoords == null) return;

        // Posisi kapal selam saat ini
        Vector2 submarinePos = new Vector2(submarineCoords.currentX, submarineCoords.currentZ);

        // Hitung arah scan berdasarkan sudut saat ini
        float angleInRadians = currentScanAngle * Mathf.Deg2Rad;
        Vector2 scanDirection = new Vector2(Mathf.Cos(angleInRadians), Mathf.Sin(angleInRadians));

        // Cek setiap obstacle
        foreach (WorldObstacle obstacle in worldObstacles)
        {
            Vector2 hitPoint = Vector2.zero; // Initialize dengan default value
            bool isHit = false;

            switch (obstacle.shape)
            {
                case WorldObstacle.ObstacleShape.Point:
                    isHit = CheckPointObstacle(obstacle, submarinePos, scanDirection, out hitPoint);
                    break;

                case WorldObstacle.ObstacleShape.Line:
                    isHit = CheckLineObstacle(obstacle, submarinePos, scanDirection, out hitPoint);
                    break;

                case WorldObstacle.ObstacleShape.Rectangle:
                    isHit = CheckRectangleObstacle(obstacle, submarinePos, scanDirection, out hitPoint);
                    break;
            }

            if (isHit)
            {
                CreateOrUpdateBlip(hitPoint);
            }
        }
    }

    bool CheckPointObstacle(WorldObstacle obstacle, Vector2 submarinePos, Vector2 scanDirection, out Vector2 hitPoint)
    {
        hitPoint = obstacle.position;

        Vector2 directionToObstacle = obstacle.position - submarinePos;
        float distanceToObstacle = directionToObstacle.magnitude;

        // Skip jika obstacle terlalu jauh
        if (distanceToObstacle > radarRange) return false;

        // Cek apakah obstacle dalam arah scan saat ini
        Vector2 normalizedDirection = directionToObstacle.normalized;
        float dot = Vector2.Dot(scanDirection, normalizedDirection);

        // Toleransi untuk "beam width" radar (cos(5 degrees) ≈ 0.996)
        return dot > 0.996f;
    }

    bool CheckLineObstacle(WorldObstacle obstacle, Vector2 submarinePos, Vector2 scanDirection, out Vector2 hitPoint)
    {
        hitPoint = Vector2.zero;

        // Raycast dari posisi kapal selam ke arah scan
        Vector2 rayEnd = submarinePos + scanDirection * radarRange;

        // Cek intersection antara scan ray dengan line obstacle
        if (LineIntersection(submarinePos, rayEnd, obstacle.startPoint, obstacle.endPoint, out hitPoint))
        {
            // Cek jarak
            float distance = Vector2.Distance(submarinePos, hitPoint);
            return distance <= radarRange;
        }

        return false;
    }

    bool CheckRectangleObstacle(WorldObstacle obstacle, Vector2 submarinePos, Vector2 scanDirection, out Vector2 hitPoint)
    {
        hitPoint = Vector2.zero;

        // Generate 4 corners of rectangle
        Vector2[] corners = GetRectangleCorners(obstacle);

        // Raycast dari posisi kapal selam ke arah scan
        Vector2 rayEnd = submarinePos + scanDirection * radarRange;

        // Cek intersection dengan setiap sisi rectangle
        float closestDistance = float.MaxValue;
        Vector2 closestHit = Vector2.zero;
        bool hasHit = false;

        for (int i = 0; i < 4; i++)
        {
            Vector2 corner1 = corners[i];
            Vector2 corner2 = corners[(i + 1) % 4];

            Vector2 tempHit;
            if (LineIntersection(submarinePos, rayEnd, corner1, corner2, out tempHit))
            {
                float distance = Vector2.Distance(submarinePos, tempHit);
                if (distance < closestDistance && distance <= radarRange)
                {
                    closestDistance = distance;
                    closestHit = tempHit;
                    hasHit = true;
                }
            }
        }

        hitPoint = closestHit;
        return hasHit;
    }

    Vector2[] GetRectangleCorners(WorldObstacle obstacle)
    {
        Vector2[] corners = new Vector2[4];

        // Half dimensions
        float halfWidth = obstacle.dimensions.x * 0.5f;
        float halfHeight = obstacle.dimensions.y * 0.5f;

        // Local corners (before rotation)
        Vector2[] localCorners = {
            new Vector2(-halfWidth, -halfHeight),
            new Vector2(halfWidth, -halfHeight),
            new Vector2(halfWidth, halfHeight),
            new Vector2(-halfWidth, halfHeight)
        };

        // Apply rotation and translation
        float rotationRad = obstacle.rotation * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rotationRad);
        float sin = Mathf.Sin(rotationRad);

        for (int i = 0; i < 4; i++)
        {
            Vector2 local = localCorners[i];
            // Rotate
            Vector2 rotated = new Vector2(
                local.x * cos - local.y * sin,
                local.x * sin + local.y * cos
            );
            // Translate
            corners[i] = rotated + obstacle.center;
        }

        return corners;
    }

    // Helper method untuk line intersection
    bool LineIntersection(Vector2 line1Start, Vector2 line1End, Vector2 line2Start, Vector2 line2End, out Vector2 intersection)
    {
        intersection = Vector2.zero;

        Vector2 dir1 = line1End - line1Start;
        Vector2 dir2 = line2End - line2Start;

        float denominator = dir1.x * dir2.y - dir1.y * dir2.x;

        // Lines are parallel
        if (Mathf.Abs(denominator) < 0.0001f)
            return false;

        Vector2 diff = line2Start - line1Start;
        float t1 = (diff.x * dir2.y - diff.y * dir2.x) / denominator;
        float t2 = (diff.x * dir1.y - diff.y * dir1.x) / denominator;

        // Check if intersection is within both line segments
        if (t1 >= 0f && t1 <= 1f && t2 >= 0f && t2 <= 1f)
        {
            intersection = line1Start + t1 * dir1;
            return true;
        }

        return false;
    }

    void CreateOrUpdateBlip(Vector2 worldPos)
    {
        // Cek apakah sudah ada blip untuk posisi ini
        RadarBlip existingBlip = FindBlipAtPosition(worldPos, 2f);

        if (existingBlip != null)
        {
            // Reset waktu blip yang sudah ada
            existingBlip.remainingTime = blipLifetime;
        }
        else
        {
            // Buat blip baru
            GameObject newBlip = Instantiate(blipPrefab, blipContainer);
            Vector2 radarPos = WorldToRadarPosition(worldPos);
            newBlip.GetComponent<RectTransform>().anchoredPosition = radarPos;

            RadarBlip blip = new RadarBlip(newBlip, blipLifetime, worldPos);
            activeBlips.Add(blip);

            // Play sound effect
            if (radarAudio && blipSound)
            {
                radarAudio.PlayOneShot(blipSound);
            }
        }
    }

    void UpdateBlips()
    {
        for (int i = activeBlips.Count - 1; i >= 0; i--)
        {
            RadarBlip blip = activeBlips[i];
            blip.remainingTime -= Time.deltaTime;

            if (blip.remainingTime <= 0f)
            {
                // Hapus blip yang sudah expired
                if (blip.blipObject != null)
                    Destroy(blip.blipObject);
                activeBlips.RemoveAt(i);
            }
            else
            {
                // Update posisi blip (untuk kasus obstacle bergerak)
                Vector2 newRadarPos = WorldToRadarPosition(blip.worldPosition);
                blip.blipObject.GetComponent<RectTransform>().anchoredPosition = newRadarPos;

                // Fade out effect
                float alpha = blip.remainingTime / blipLifetime;
                Image blipImage = blip.blipObject.GetComponent<Image>();
                if (blipImage != null)
                {
                    Color color = blipImage.color;
                    color.a = alpha;
                    blipImage.color = color;
                }
            }
        }
    }

    Vector2 WorldToRadarPosition(Vector2 worldPos)
    {
        if (submarineCoords == null) return Vector2.zero;

        // Konversi posisi world ke posisi radar UI
        Vector2 submarinePos = new Vector2(submarineCoords.currentX, submarineCoords.currentZ);
        Vector2 relativePos = worldPos - submarinePos;

        // Scale ke ukuran radar display
        float scale = radarDisplayRadius / radarRange;
        Vector2 radarPos = relativePos * scale;

        // Batasi dalam lingkaran radar
        if (radarPos.magnitude > radarDisplayRadius)
        {
            radarPos = radarPos.normalized * radarDisplayRadius;
        }

        return radarPos;
    }

    RadarBlip FindBlipAtPosition(Vector2 worldPos, float tolerance)
    {
        foreach (RadarBlip blip in activeBlips)
        {
            if (Vector2.Distance(blip.worldPosition, worldPos) <= tolerance)
                return blip;
        }
        return null;
    }

    void SetupDefaultObstacles()
    {
        // Contoh obstacle default untuk dinding luas

        // Dinding utara (line obstacle)
        worldObstacles.Add(new WorldObstacle
        {
            name = "North Wall",
            shape = WorldObstacle.ObstacleShape.Line,
            startPoint = new Vector2(-50, 25),
            endPoint = new Vector2(50, 25),
            thickness = 2f,
            type = WorldObstacle.ObstacleType.Wall
        });

        // Dinding selatan (line obstacle)
        worldObstacles.Add(new WorldObstacle
        {
            name = "South Wall",
            shape = WorldObstacle.ObstacleShape.Line,
            startPoint = new Vector2(-50, -25),
            endPoint = new Vector2(50, -25),
            thickness = 2f,
            type = WorldObstacle.ObstacleType.Wall
        });

        // Dinding timur (rectangle obstacle)
        worldObstacles.Add(new WorldObstacle
        {
            name = "East Wall Complex",
            shape = WorldObstacle.ObstacleShape.Rectangle,
            center = new Vector2(25, 0),
            dimensions = new Vector2(2f, 50f),
            rotation = 0f,
            type = WorldObstacle.ObstacleType.Wall
        });

        // Obstacle titik (wreck)
        worldObstacles.Add(new WorldObstacle
        {
            name = "Wreck",
            shape = WorldObstacle.ObstacleShape.Point,
            position = new Vector2(15, 20),
            size = 3f,
            type = WorldObstacle.ObstacleType.Wreck
        });

        Debug.Log("Default obstacles created with different shapes. Edit in Inspector atau hapus SetupDefaultObstacles()");
    }

    // Method untuk menambah obstacle baru dari script lain
    public void AddObstacle(string name, Vector2 position, float size = 1f, WorldObstacle.ObstacleType type = WorldObstacle.ObstacleType.Wall)
    {
        WorldObstacle newObstacle = new WorldObstacle
        {
            name = name,
            shape = WorldObstacle.ObstacleShape.Point,
            position = position,
            size = size,
            type = type
        };
        worldObstacles.Add(newObstacle);
    }

    // Method untuk menambah line obstacle
    public void AddLineObstacle(string name, Vector2 startPoint, Vector2 endPoint, float thickness = 1f, WorldObstacle.ObstacleType type = WorldObstacle.ObstacleType.Wall)
    {
        WorldObstacle newObstacle = new WorldObstacle
        {
            name = name,
            shape = WorldObstacle.ObstacleShape.Line,
            startPoint = startPoint,
            endPoint = endPoint,
            thickness = thickness,
            type = type
        };
        worldObstacles.Add(newObstacle);
    }

    // Method untuk menambah rectangle obstacle
    public void AddRectangleObstacle(string name, Vector2 center, Vector2 dimensions, float rotation = 0f, WorldObstacle.ObstacleType type = WorldObstacle.ObstacleType.Wall)
    {
        WorldObstacle newObstacle = new WorldObstacle
        {
            name = name,
            shape = WorldObstacle.ObstacleShape.Rectangle,
            center = center,
            dimensions = dimensions,
            rotation = rotation,
            type = type
        };
        worldObstacles.Add(newObstacle);
    }

    // Method untuk menghapus obstacle
    public void RemoveObstacle(string name)
    {
        worldObstacles.RemoveAll(obstacle => obstacle.name == name);
    }

    void OnDrawGizmosSelected()
    {
        if (submarineCoords == null) return;

        // Visualisasi posisi kapal selam dan radar range
        Vector3 submarinePos3D = new Vector3(submarineCoords.currentX, 0, submarineCoords.currentZ);

        Gizmos.color = Color.green;
        DrawWireCircle(submarinePos3D, radarRange);

        // Visualisasi scan line
        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            float angleInRadians = currentScanAngle * Mathf.Deg2Rad;
            Vector3 scanDirection = new Vector3(Mathf.Cos(angleInRadians), 0, Mathf.Sin(angleInRadians));
            Gizmos.DrawRay(submarinePos3D, scanDirection * radarRange);
        }

        // Visualisasi obstacles
        Gizmos.color = Color.red;
        foreach (WorldObstacle obstacle in worldObstacles)
        {
            switch (obstacle.shape)
            {
                case WorldObstacle.ObstacleShape.Point:
                    Vector3 obstaclePos3D = new Vector3(obstacle.position.x, 0, obstacle.position.y);
                    Gizmos.DrawWireCube(obstaclePos3D, Vector3.one * obstacle.size);
                    break;

                case WorldObstacle.ObstacleShape.Line:
                    Vector3 start3D = new Vector3(obstacle.startPoint.x, 0, obstacle.startPoint.y);
                    Vector3 end3D = new Vector3(obstacle.endPoint.x, 0, obstacle.endPoint.y);
                    Gizmos.DrawLine(start3D, end3D);
                    // Draw thickness
                    Vector3 perpendicular = Vector3.Cross((end3D - start3D).normalized, Vector3.up) * obstacle.thickness * 0.5f;
                    Gizmos.DrawLine(start3D + perpendicular, end3D + perpendicular);
                    Gizmos.DrawLine(start3D - perpendicular, end3D - perpendicular);
                    break;

                case WorldObstacle.ObstacleShape.Rectangle:
                    // Draw rectangle outline
                    Vector2[] corners = GetRectangleCorners(obstacle);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 corner1 = new Vector3(corners[i].x, 0, corners[i].y);
                        Vector3 corner2 = new Vector3(corners[(i + 1) % 4].x, 0, corners[(i + 1) % 4].y);
                        Gizmos.DrawLine(corner1, corner2);
                    }
                    break;
            }
        }
    }

    // Helper method untuk menggambar wire circle
    void DrawWireCircle(Vector3 center, float radius)
    {
        int segments = 32;
        float angleStep = 360f / segments;

        Vector3 prevPoint = center + new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            Vector3 newPoint = center + new Vector3(
                Mathf.Cos(angle) * radius,
                0,
                Mathf.Sin(angle) * radius
            );

            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }
}