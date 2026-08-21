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
        userManagePanel.SetActive(true);
        DesktopPetLayoutController
            .Instance
            .ApplyLayout(
                DesktopPetLayoutMode.Management
            );
    }

    public void OnClickCharacterManage()
    {
        if (GlobalSession.IsGuest())
            return;

        controlPanel.SetActive(false);
        characterManagePanel.SetActive(true);
        DesktopPetLayoutController
            .Instance
            .ApplyLayout(
                DesktopPetLayoutMode.Management
            );
    }

    public void OnClickBack()
    {
        petPanel.SetActive(true);
        controlPanel.SetActive(false);
    }
}