using UnityEngine;

public class ControlPanelController : MonoBehaviour
{
    public GameObject controlPanel;
    public GameObject userManagePanel;
    public GameObject characterManagePanel;
    public GameObject petPanel;
    public WindowSizeController windowSize;

    public void OnClickUserManage()
    {
        if (!GlobalSession.IsAdmin())
            return;

        controlPanel.SetActive(false);
        windowSize.ToggleWindow(true);
        userManagePanel.SetActive(true);
    }

    public void OnClickCharacterManage()
    {
        if (GlobalSession.IsGuest())
            return;

        controlPanel.SetActive(false);
        windowSize.ToggleWindow(true);
        characterManagePanel.SetActive(true);
    }

    public void OnClickBack()
    {
        petPanel.SetActive(true);
        controlPanel.SetActive(false);
    }
}