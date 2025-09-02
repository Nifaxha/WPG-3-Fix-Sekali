using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class HUDController : MonoBehaviour
{
    public static HUDController instance;

    private void Awake()
    {
        instance = this;
    }

    [SerializeField] TMP_Text InteractionText;

    public void EnableInteractionText(string text)
    {
        InteractionText.text = text + " (F) ";
        InteractionText.gameObject.SetActive(true);
    }

    public void DisableInteractionText() 
    {
        InteractionText.gameObject.SetActive(false);
    }
}
