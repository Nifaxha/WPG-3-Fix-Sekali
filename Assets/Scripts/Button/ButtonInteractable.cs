using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animator))] // Pastikan ada komponen Animator
public class ButtonInteractable : MonoBehaviour
{
    private Animator animator;
    public UnityEvent onInteraction;

    // Nama parameter di Animator
    public string pressedParameter = "Pushed";
    public string releasedParameter = "Released";

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    // Dipanggil dari skrip pemain saat tombol ditekan
    public void Press()
    {
        animator.SetBool(pressedParameter, true);
        animator.SetBool(releasedParameter, false); // Pastikan parameter release mati
    }

    // Dipanggil dari skrip pemain saat tombol dilepas
    public void Release()
    {
        animator.SetBool(releasedParameter, true);
        animator.SetBool(pressedParameter, false); // Matikan parameter pressed

        // Panggil event saat tombol dilepas
        onInteraction.Invoke();
    }
}