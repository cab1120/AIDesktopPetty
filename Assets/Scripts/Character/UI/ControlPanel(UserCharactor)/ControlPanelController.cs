using UnityEngine;

public class ControlPanelController : MonoBehaviour
{
    public GameObject controlPanel;
    public GameObject userManagePanel;
    public GameObject characterManagePanel;

    public void OnClickUserManage()
    {
        if (!GlobalSession.IsAdmin())
            return;

        controlPanel.SetActive(false);
        userManagePanel.SetActive(true);
    }

    public void OnClickCharacterManage()
    {
        if (GlobalSession.IsGuest())
            return;

        controlPanel.SetActive(false);
        characterManagePanel.SetActive(true);
    }

    public void OnClickBack()
    {
        controlPanel.SetActive(false);
    }
}