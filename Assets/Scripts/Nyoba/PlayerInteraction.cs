using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Pastikan ini ada jika Anda menggunakan komponen UI lain

public class PlayerInteraction : MonoBehaviour
{
    public float playerReach = 3f;
    Interactable currentInteractable;

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
            if (hit.collider.tag == "Interactable")
            {
                Interactable newInteractable = hit.collider.GetComponent<Interactable>();

                if (newInteractable != null && newInteractable.enabled)
                {
                    // Hanya atur interaksi jika kita melihat objek baru
                    if (newInteractable != currentInteractable)
                    {
                        SetNewCurrentInteractable(newInteractable);
                    }
                }
                else
                {
                    DisableCurrentInteractable();
                }
            }
            else
            {
                DisableCurrentInteractable();
            }
        }
        else
        {
            DisableCurrentInteractable();
        }
    }

    void SetNewCurrentInteractable(Interactable newInteractable)
    {
        currentInteractable = newInteractable;
        currentInteractable.EnableOutline();

        // Memanggil metode dari HUDController menggunakan instance
        HUDController.instance.EnableInteractionText(newInteractable.message);
    }

    void DisableCurrentInteractable()
    {
        if (currentInteractable)
        {
            currentInteractable.DisableOutline();
            currentInteractable = null;
        }

        // Memanggil metode dari HUDController menggunakan instance
        HUDController.instance.DisableInteractionText();
    }
}