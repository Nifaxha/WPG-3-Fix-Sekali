using UnityEngine;

public class SettingsMenuButtonManager : MonoBehaviour
{
    [SerializeField] private MainMenuManager.SettingsButtons _buttonType;
    public void ButtonClicked()
    {
        MainMenuManager._.SettingsButtonClicked(_buttonType);
    }
}