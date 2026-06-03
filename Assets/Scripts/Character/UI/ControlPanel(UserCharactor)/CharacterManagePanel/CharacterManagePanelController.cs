using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CharacterManagePanelController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject characterManagePanel;
    public GameObject controlPanel;
    public CharacterModifyPanelController modifyPanel;

    [Header("Search")]
    public TMP_InputField searchInput;

    [Header("List")]
    public Transform contentRoot;
    public GameObject characterListItemPrefab;

    [Header("Detail")]
    public TMP_Text selectedInfoText;
    public TMP_Text messageText;
    
    [Header("Controller")]
    public WindowSizeController windowSize;

    private CharacterProfileData selectedCharacter;
    private List<CharacterProfileData> cachedCharacters =
        new List<CharacterProfileData>();

    private void OnEnable()
    {
        RefreshList();
    }

    public void OnClickSearch()
    {
        RefreshList(searchInput.text);
    }

    public void RefreshList(string keyword = "", bool updateMessage = true)
    {
        ClearList();

        if (GlobalSession.IsAdmin())
        {
            cachedCharacters = CharacterRepository.SearchByCharacterName(keyword);
        }
        else
        {
            cachedCharacters = CharacterRepository.SearchByCharacterNameForUser(
                GlobalSession.CurrentUserName,
                keyword
            );
        }

        foreach (var character in cachedCharacters)
        {
            GameObject item = Instantiate(characterListItemPrefab, contentRoot);
            item.GetComponent<CharacterListItem>().Init(character, this);
        }

        selectedCharacter = null;
        selectedInfoText.text = "当前未选择角色";

        if (updateMessage)
        {
            messageText.text = $"已加载 {cachedCharacters.Count} 个角色";
        }
    }

    public void SelectCharacter(CharacterProfileData character)
    {
        selectedCharacter = character;

        selectedInfoText.text =
            $"当前选中角色：\n" +
            $"CharacterId：{character.CharacterId}\n" +
            $"角色名：{character.CharacterName}\n" +
            $"所属用户名：{character.UserName}\n" +
            $"是否启用：{character.IsActive}\n" +
            $"PromptJson：\n{character.PromptJson}\n" +
            $"创建时间：{new System.DateTime(character.CreatedAtTicks)}";
    }

    public void OnClickAdd()
    {
        modifyPanel.OpenForAdd(this);
    }

    public void OnClickModify()
    {
        if (selectedCharacter == null)
        {
            messageText.text = "请先选择一个角色";
            return;
        }

        modifyPanel.OpenForEdit(this, selectedCharacter);
    }

    public void OnClickDelete()
    {
        if (selectedCharacter == null)
        {
            messageText.text = "请先选择一个角色";
            return;
        }

        bool success = CharacterRepository.DeleteCharacterByName(
            selectedCharacter.CharacterName,
            out string error
        );

        if (success)
        {
            RefreshList(searchInput.text, false);
            messageText.text = "删除成功";
            GlobalSession.RefreshCurrentCharacterFromDatabase();
        }
        else
        {
            messageText.text = error;
        }
    }

    public void OnClickBack()
    {
        bool valid = CharacterRepository.ValidateActiveCharacterState(
            GlobalSession.CurrentUserName,
            out string error
        );

        if (!valid)
        {
            messageText.text = error;
            return;
        }
        
        characterManagePanel.SetActive(false);
        windowSize.ToggleWindow(false);
        controlPanel.SetActive(true);
    }

    private void ClearList()
    {
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
    }

    public void ShowMessage(string text)
    {
        messageText.text =  text;
    }
}