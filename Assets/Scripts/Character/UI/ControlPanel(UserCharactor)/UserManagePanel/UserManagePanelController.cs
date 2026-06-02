using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UserManagePanelController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject userManagePanel;
    public GameObject controlPanel;
    public UserModifyPanelController modifyPanel;

    [Header("Search")]
    public TMP_InputField searchInput;

    [Header("List")]
    public Transform contentRoot;
    public GameObject userListItemPrefab;

    [Header("Detail")]
    public TMP_Text selectedInfoText;
    public TMP_Text messageText;

    private UserData selectedUser;
    private List<UserData> cachedUsers = new List<UserData>();

    private void OnEnable()
    {
        RefreshList();
    }

    public void OnClickSearch()
    {
        RefreshList(searchInput.text);
    }

    public void RefreshList(string keyword = "")
    {
        ClearList();

        cachedUsers = UserRepository.SearchByUserName(keyword);

        foreach (var user in cachedUsers)
        {
            GameObject item = Instantiate(userListItemPrefab, contentRoot);
            item.GetComponent<UserListItem>().Init(user, this);
        }

        selectedUser = null;
        selectedInfoText.text = "当前未选择用户";
        messageText.text = $"已加载 {cachedUsers.Count} 个用户";
    }

    public void SelectUser(UserData user)
    {
        selectedUser = user;

        selectedInfoText.text =
            $"当前选中用户：\n" +
            $"UserId：{user.UserId}\n" +
            $"用户名：{user.UserName}\n" +
            $"权限：{user.Role}\n" +
            $"密码哈希：{user.PasswordHash}\n" +
            $"创建时间：{new System.DateTime(user.CreatedAtTicks)}\n" +
            $"最后登录：{new System.DateTime(user.LastLoginAtTicks)}";
    }

    public void OnClickAdd()
    {
        modifyPanel.OpenForAdd(this);
    }

    public void OnClickModify()
    {
        if (selectedUser == null)
        {
            messageText.text = "请先选择一个用户";
            return;
        }

        modifyPanel.OpenForEdit(this, selectedUser);
    }

    public void OnClickDelete()
    {
        if (selectedUser == null)
        {
            messageText.text = "请先选择一个用户";
            return;
        }

        bool success = UserRepository.DeleteUser(
            selectedUser.UserId,
            out string error
        );

        messageText.text = success ? "删除成功" : error;

        RefreshList(searchInput.text);
    }

    public void OnClickBack()
    {
        userManagePanel.SetActive(false);
        controlPanel.SetActive(true);
    }

    private void ClearList()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
    }
}