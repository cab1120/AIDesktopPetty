using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class ChatHistoryItemUI : MonoBehaviour
{
    public TMP_Text senderText;
    public TMP_Text contentText;
    public TMP_Text timeText;
    public TMP_Text userName;
    public TMP_Text characterName;

    public Image backgroundImage;

    private ChatMessageData data;
    private bool selected;

    public string MessageId => data?.MessageId;
    public bool IsSelected => selected;

    public void Init(ChatMessageData message)
    {
        data = message;
        selected = false;

        senderText.text = message.Sender;
        contentText.text = message.Content;
        userName.text = message.UserName;
        characterName.text = message.CharacterName;

        timeText.text = new DateTime(message.CreatedAtTicks)
            .ToString("yyyy-MM-dd HH:mm:ss");

        RefreshVisual();
    }

    public void OnClick()
    {
        selected = !selected;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (backgroundImage == null)
            return;

        backgroundImage.color = selected
            ? new Color(0.8f, 0.8f, 0.8f, 1f)
            : Color.white;
    }
}