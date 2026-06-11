using System.IO;
using TMPro;
using UnityEngine;

public class CharacterModifyPanelController : MonoBehaviour
{
    public GameObject panel;

    public TMP_Text titleText;
    public TMP_Text messageText;

    public TMP_InputField characterNameInput;
    public TMP_InputField jsonPathInput;
    public TMP_Text promptPreviewText;
    public UnityEngine.UI.Toggle isActiveToggle;

    private bool isEditMode;
    private CharacterProfileData editingCharacter;
    private CharacterManagePanelController owner;

    private string loadedPromptJson = "";

    public void OpenForAdd(CharacterManagePanelController controller)
    {
        owner = controller;
        isEditMode = false;
        editingCharacter = null;

        titleText.text = "新增角色";
        characterNameInput.text = "";
        jsonPathInput.text = "";
        promptPreviewText.text = "";
        loadedPromptJson = "";
        isActiveToggle.isOn = false;
        messageText.text = "";

        panel.SetActive(true);
    }

    public void OpenForEdit(
        CharacterManagePanelController controller,
        CharacterProfileData character)
    {
        owner = controller;
        isEditMode = true;
        editingCharacter = character;

        titleText.text = "修改角色";
        characterNameInput.text = "";
        jsonPathInput.text = "";
        loadedPromptJson = character.PromptJson;
        promptPreviewText.text = character.PromptJson;
        isActiveToggle.isOn = character.IsActive;
        messageText.text = "空输入表示保持原值";

        panel.SetActive(true);
    }

    public void OnClickLoadJson()
    {
        string path = jsonPathInput.text;

        if (string.IsNullOrWhiteSpace(path))
        {
            messageText.text = "Json 路径不能为空";
            return;
        }

        if (!File.Exists(path))
        {
            messageText.text = "Json 文件不存在";
            return;
        }

        loadedPromptJson = File.ReadAllText(path);
        promptPreviewText.text = loadedPromptJson;

        messageText.text = "Json 读取成功";
    }

    public void OnClickConfirm()
    {
        if (!isEditMode)
        {
            bool success = CharacterRepository.AddCharacter(
                GlobalSession.CurrentUserName,
                characterNameInput.text,
                loadedPromptJson,
                isActiveToggle.isOn,
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
            string newCharacterName =
                string.IsNullOrWhiteSpace(characterNameInput.text)
                    ? editingCharacter.CharacterName
                    : characterNameInput.text;

            string newPromptJson =
                string.IsNullOrWhiteSpace(loadedPromptJson)
                    ? editingCharacter.PromptJson
                    : loadedPromptJson;

            bool success = CharacterRepository.UpdateCharacterByName(
                editingCharacter.UserName,
                editingCharacter.CharacterName,
                newCharacterName,
                newPromptJson,
                isActiveToggle.isOn,
                out string error
            );

            if (!success)
            {
                messageText.text = error;
                return;
            }
        }
        
        panel.SetActive(false);
        GlobalSession.RefreshCurrentCharacterFromDatabase();
        owner.RefreshList("", false);
        owner.ShowMessage("修改成功");
    }

    public void OnClickCancel()
    {
        panel.SetActive(false);
    }
}
