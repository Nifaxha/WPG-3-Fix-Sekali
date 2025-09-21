using UnityEngine;
using System.Collections.Generic;

public class RadarDetector : MonoBehaviour
{
    public List<GameObject> detectedObjects = new List<GameObject>();

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Wall") || other.gameObject.CompareTag("Enemy"))
        {
            if (!detectedObjects.Contains(other.gameObject))
            {
                detectedObjects.Add(other.gameObject);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (detectedObjects.Contains(other.gameObject))
        {
            detectedObjects.Remove(other.gameObject);
        }
    }
}