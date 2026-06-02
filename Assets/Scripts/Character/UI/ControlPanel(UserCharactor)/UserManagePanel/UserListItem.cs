using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UserListItem : MonoBehaviour
{
    public TMP_Text infoText;
    public Button selectButton;

    private UserData data;
    private UserManagePanelController owner;

    public void Init(UserData user, UserManagePanelController controller)
    {
        data = user;
        owner = controller;

        infoText.text =
            $"用户名：{user.UserName}\n" +
            $"权限：{user.Role}\n" +
            $"创建时间：{new System.DateTime(user.CreatedAtTicks)}\n" +
            $"最后登录：{new System.DateTime(user.LastLoginAtTicks)}";

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() =>
        {
            owner.SelectUser(data);
        });
    }
}