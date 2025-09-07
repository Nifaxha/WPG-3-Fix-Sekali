using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ButtonController : MonoBehaviour
{
    private Animator _animator;

    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            _animator.SetTrigger("Pushed");
        }
        if (Input.GetKeyUp(KeyCode.F))
        {
            _animator.SetTrigger("Released");
        }
    }
}