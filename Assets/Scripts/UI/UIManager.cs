using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public TMP_InputField inputField;
    public Transform chatContent;    // 拖入 Scroll View -> Content
    public ScrollRect scrollRect;    // 拖入 Scroll View
    public GameObject userBubblePrefab; // 用户气泡预制体
    public GameObject aiBubblePrefab;   // AI气泡预制体
    
    public AIChat aiChat;

    public void OnSendButtonClick()
    {
        string userInput = inputField.text;
        if (string.IsNullOrEmpty(userInput)) return;

        // 1. 生成用户气泡
        CreateBubble(userBubblePrefab, userInput);
        inputField.text = "";

        // 2. 发送 AI 请求
        StartCoroutine(aiChat.SendMessage(userInput, (reply) => {
            // 3. 生成 AI 气泡
            CreateBubble(aiBubblePrefab, reply);
        }));
    }

    private void CreateBubble(GameObject prefab, string message)
    {
        GameObject go = Instantiate(prefab, chatContent);
        
        MessageUI ui = go.GetComponent<MessageUI>();
        
        if (ui != null && ui.messageText != null)
        {
            ui.messageText.text = message;
        }
        
        StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom()// 强制刷新并滚动到底部
    {
        // 等待两帧，确保 Layout Group 计算完成
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;
    }
}