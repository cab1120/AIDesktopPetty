using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterListItem : MonoBehaviour
{
    public TMP_Text infoText;
    public Button selectButton;

    private CharacterProfileData data;
    private CharacterManagePanelController owner;

    public void Init(CharacterProfileData character, CharacterManagePanelController controller)
    {
        data = character;
        owner = controller;

        infoText.text =
            $"角色名：{character.CharacterName}\n" +
            $"所属用户名：{character.UserName}\n" +
            $"是否启用：{character.IsActive}\n" +
            $"Prompt长度：{(character.PromptJson == null ? 0 : character.PromptJson.Length)}\n" +
            $"创建时间：{new System.DateTime(character.CreatedAtTicks)}";

        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() =>
        {
            owner.SelectCharacter(data);
        });
    }
}