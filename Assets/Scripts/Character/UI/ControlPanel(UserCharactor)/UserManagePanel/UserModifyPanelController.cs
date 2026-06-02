using TMPro;
using UnityEngine;

public class UserModifyPanelController : MonoBehaviour
{
    public GameObject panel;

    public TMP_InputField userNameInput;
    public TMP_InputField passwordInput;
    public TMP_Dropdown roleDropdown;
    public TMP_Text titleText;
    public TMP_Text messageText;

    private bool isEditMode;
    private UserData editingUser;
    private UserManagePanelController owner;

    public void OpenForAdd(UserManagePanelController controller)
    {
        owner = controller;
        isEditMode = false;
        editingUser = null;

        titleText.text = "新增用户";
        userNameInput.text = "";
        passwordInput.text = "";
        roleDropdown.value = 1;
        messageText.text = "";

        panel.SetActive(true);
    }

    public void OpenForEdit(UserManagePanelController controller, UserData user)
    {
        owner = controller;
        isEditMode = true;
        editingUser = user;

        titleText.text = "修改用户";
        userNameInput.text = "";
        passwordInput.text = "";
        roleDropdown.value = RoleToIndex(user.Role);
        messageText.text = "空输入表示保持原值";

        panel.SetActive(true);
    }

    public void OnClickConfirm()
    {
        if (!isEditMode)
        {
            bool success = UserRepository.AddUser(
                userNameInput.text,
                passwordInput.text,
                IndexToRole(roleDropdown.value),
                out string error
            );

            if (!success)
            {
                messageText.text = error;
                return;
            }
        }
        else
        {
            string newUserName = string.IsNullOrWhiteSpace(userNameInput.text)
                ? editingUser.UserName
                : userNameInput.text;

            string newPassword = passwordInput.text;

            string newRole = IndexToRole(roleDropdown.value);

            bool success = UserRepository.UpdateUser(
                editingUser.UserId,
                newUserName,
                newPassword,
                newRole,
                out string error
            );

            if (!success)
            {
                messageText.text = error;
                return;
            }
        }

        panel.SetActive(false);
        owner.RefreshList();
    }

    public void OnClickCancel()
    {
        panel.SetActive(false);
    }

    private string IndexToRole(int index)
    {
        return index switch
        {
            0 => "Admin",
            1 => "User",
            2 => "Guest",
            _ => "User"
        };
    }

    private int RoleToIndex(string role)
    {
        return role switch
        {
            "Admin" => 0,
            "User" => 1,
            "Guest" => 2,
            _ => 1
        };
    }
}