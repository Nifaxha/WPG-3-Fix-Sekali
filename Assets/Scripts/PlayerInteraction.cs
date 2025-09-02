using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float playerReach = 3f;
    Interactable currentInteractable;
    // Start is called before the first frame update
    void Update()
    {
        CheckInteraction();
        if (Input.GetKeyDown(KeyCode.F) && currentInteractable != null) 
        {
            currentInteractable.Interact();
        }
    }

    void CheckInteraction()
    {
        RaycastHit hit;
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        if (Physics.Raycast(ray, out hit, playerReach))
        {
            if (hit.collider.tag == "Interactable") //if looking at an interactable object.
            {
                Interactable newInteractable = hit.collider.GetComponent<Interactable>();
                // if there is a currentInteractable and it is not the newInteractable

                if (newInteractable != currentInteractable)
                {
                    DisableCurrentInteractable(); // Matikan outline objek sebelumnya
                    SetNewCurrentInteractable(newInteractable); // Aktifkan outline objek baru
                }
                else //if new interactable is not enabled
            {
                DisableCurrentInteractable();
            }
            }

            else //if not an interactable
            {
                DisableCurrentInteractable();
            }

        }
    }

    void SetNewCurrentInteractable(Interactable newInteractable)
    {
        currentInteractable = newInteractable;
        currentInteractable.EnableOutline();
    }

    void DisableCurrentInteractable()
    {
        if (currentInteractable)
        {
            currentInteractable.DisableOutline();
            currentInteractable = null;
        }
    }
}
