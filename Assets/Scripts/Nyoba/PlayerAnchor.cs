using UnityEngine;

public class PlayerAnchor : MonoBehaviour
{
    public SubmarineCoordinates submarine;

    void Update()
    {
        // Set posisi dunia sesuai koordinat kapal
        transform.position = new Vector3(submarine.currentX, 0f, submarine.currentZ);
    }
}
