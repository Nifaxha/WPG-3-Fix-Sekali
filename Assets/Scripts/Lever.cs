using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Lever : MonoBehaviour
{
    bool _leverDown = false;
    Animator _anim;
    public UnityEvent onUp, onDown;

    // Start is called before the first frame update

    void Start()
    {
        _anim = GetComponent<Animator>();
        _leverDown = true;
    }

    public void ToggleLever()
    {
        if (_leverDown)
        {
            _anim.SetTrigger("PullUp");
            onUp.Invoke();
        }
        else 
        {
            _anim.SetTrigger("PullDown");
            onDown.Invoke();
        }
        _leverDown = !_leverDown;
    }
}
