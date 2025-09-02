using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Button_modified : MonoBehaviour
{
    Animator _anim;
    public UnityEvent onPushed;

    // Start is called before the first frame update
    void Start()
    {
        _anim = GetComponent<Animator>();
    }

    public void PushButton()
    {
        _anim.SetTrigger("Pushed"); // Ganti dengan nama trigger animasi yang sesuai
        onPushed.Invoke();
    }
}