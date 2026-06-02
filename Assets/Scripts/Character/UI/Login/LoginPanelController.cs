using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoginPanelController : MonoBehaviour
{
    [Header("Input")]
    public TMP_InputField userNameInput;
    public TMP_InputField passwordInput;
    public TMP_InputField characterNameInput;

    [Header("UI")]
    public TMP_Text messageText;
    public GameObject loginPanel;
    public GameObject desktopPetPanel;

    public void OnClickLogin()
    {
        bool success = AuthService.Login(
            userNameInput.text,
            passwordInput.text,
            characterNameInput.text,
            out string error
        );

        if (!success)
        {
            messageText.text = error;
            return;
        }

        messageText.text = "登录成功";

        loginPanel.SetActive(false);
        desktopPetPanel.SetActive(true);
    }
}