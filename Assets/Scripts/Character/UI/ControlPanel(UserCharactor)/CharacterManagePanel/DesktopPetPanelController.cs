using UnityEngine;
using TMPro;

public class DesktopPetPanelController : MonoBehaviour
{
    public GameObject controlPanel;

    public GameObject userManageButton;
    public GameObject characterManageButton;

    public TMP_Text permissionMessageText;

    private void OnEnable()
    {
        RefreshPermissionUI();
    }

    private void RefreshPermissionUI()
    {
        userManageButton.SetActive(false);
        characterManageButton.SetActive(false);
        permissionMessageText.text = "";

        if (GlobalSession.IsAdmin())
        {
            userManageButton.SetActive(true);
            characterManageButton.SetActive(true);
        }
        else if (GlobalSession.IsUser())
        {
            characterManageButton.SetActive(true);
        }
        else
        {
            permissionMessageText.text = "当前用户无操作权限";
        }
    }

    public void OnClickOpenControlPanel()
    {
        controlPanel.SetActive(true);
        RefreshPermissionUI();
    }
}