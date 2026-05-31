using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChatHistoryPanel : MonoBehaviour
{
    public CanvasScaler canvasScaler;
    
    [Header("Panels")]
    public GameObject chatPanel;
    public GameObject historyPanel;

    [Header("Search UI")]
    public TMP_InputField contentInput;
    public TMP_InputField userNameInput;
    public TMP_InputField characterNameInput;
    public TMP_InputField startDateInput; // 格式：yyyy-MM-dd
    public TMP_InputField endDateInput;   // 格式：yyyy-MM-dd
    public TMP_Dropdown senderDropdown;   // 全部/User/Assistant

    [Header("List UI")]
    public Transform listContent;
    public GameObject historyItemPrefab;

    private readonly List<ChatHistoryItemUI> currentItems =
        new List<ChatHistoryItemUI>();

    public void Open()
    {
        chatPanel.SetActive(false);
        historyPanel.SetActive(true);
        canvasScaler.enabled = false;
        LoadRecent();
    }

    public void Close()
    {
        historyPanel.SetActive(false);
        chatPanel.SetActive(true);
        canvasScaler.enabled = true;
    }

    public void LoadRecent()
    {
        List<ChatMessageData> messages =
            ChatMessageService.GetRecent(5);

        RefreshList(messages);
    }

    public void OnSearchButtonClick()
    {
        ChatMessageSearchCondition condition =
            BuildSearchConditionFromUI();

        List<ChatMessageData> results =
            ChatMessageService.Search(condition);

        RefreshList(results);
    }

    public void OnClearSearchButtonClick()
    {
        contentInput.text = "";
        userNameInput.text = "";
        characterNameInput.text = "";
        startDateInput.text = "";
        endDateInput.text = "";

        if (senderDropdown != null)
            senderDropdown.value = 0;

        LoadRecent();
    }

    public void OnDeleteSelectedButtonClick()
    {
        List<string> selectedIds = new List<string>();

        foreach (ChatHistoryItemUI item in currentItems)
        {
            if (item != null && item.IsSelected)
            {
                selectedIds.Add(item.MessageId);
            }
        }

        ChatMessageService.DeleteSelected(selectedIds);

        OnSearchButtonClick();
    }

    private ChatMessageSearchCondition BuildSearchConditionFromUI()
    {
        ChatMessageSearchCondition condition =
            new ChatMessageSearchCondition();

        condition.ContentKeyword = contentInput.text;
        condition.UserNameKeyword = userNameInput.text;
        condition.CharacterNameKeyword = characterNameInput.text;

        if (senderDropdown != null)
        {
            string option = senderDropdown.options[senderDropdown.value].text;

            if (option == "User" || option == "Assistant")
            {
                condition.Sender = option;
            }
        }

        if (!string.IsNullOrEmpty(startDateInput.text))
        {
            if (DateTime.TryParse(startDateInput.text, out DateTime startDate))
            {
                condition.StartTicks = startDate.Date.Ticks;
            }
            else
            {
                Debug.LogWarning("开始日期格式错误，应为 yyyy-MM-dd");
            }
        }

        if (!string.IsNullOrEmpty(endDateInput.text))
        {
            if (DateTime.TryParse(endDateInput.text, out DateTime endDate))
            {
                condition.EndTicks = endDate.Date
                    .AddDays(1)
                    .AddTicks(-1)
                    .Ticks;
            }
            else
            {
                Debug.LogWarning("结束日期格式错误，应为 yyyy-MM-dd");
            }
        }

        return condition;
    }

    private void RefreshList(List<ChatMessageData> messages)
    {
        ClearList();

        foreach (ChatMessageData message in messages)
        {
            GameObject go = Instantiate(historyItemPrefab, listContent);

            ChatHistoryItemUI item =
                go.GetComponent<ChatHistoryItemUI>();

            item.Init(message);

            currentItems.Add(item);
        }
    }

    private void ClearList()
    {
        currentItems.Clear();

        for (int i = listContent.childCount - 1; i >= 0; i--)
        {
            Destroy(listContent.GetChild(i).gameObject);
        }
    }
}