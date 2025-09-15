using UnityEngine;

public class CreditsMenuButtonManager : MonoBehaviour
{
    [SerializeField] private MainMenuManager.CreditsButtons _buttonType;
    public void ButtonClicked()
    {
        MainMenuManager._.CreditsButtonClicked(_buttonType);
    }
}