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

    private Coroutine currentScrollCoroutine; // 用于管理滚到底部的协程，避免重复启动

    public void OnSendButtonClick()
    {
        string userInput = inputField.text;
        if (string.IsNullOrEmpty(userInput)) return;

        CreateBubble(userBubblePrefab, userInput);
        inputField.text = "";

        // 在发送消息后立即尝试滚动，处理用户消息的布局问题
        StartOrRestartScrollToBottom();

        StartCoroutine(aiChat.SendMessage(userInput, (reply) => {
            CreateBubble(aiBubblePrefab, reply);
            // AI 回复后再次尝试滚动，处理 AI 消息的布局问题
            StartOrRestartScrollToBottom();
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
        // 在这里不立即调用 ScrollToBottom，而是通过 StartOrRestartScrollToBottom 统一管理
        // 并且不需要 ForceUpdateLayout(go) 了，因为 ScrollToBottom 会统一处理所有布局
    }

    // 统一管理滚动到底部的协程，避免多个协程同时运行导致冲突
    private void StartOrRestartScrollToBottom()
    {
        if (currentScrollCoroutine != null)
        {
            StopCoroutine(currentScrollCoroutine);
        }
        currentScrollCoroutine = StartCoroutine(ScrollToBottom());
    }

    IEnumerator ScrollToBottom() // 强制刷新并滚动到底部
    {
        // --- 核心修复：多阶段延迟与刷新 ---

        // 阶段 1：等待一帧，确保新生成的Text组件已经根据内容计算出其“首选大小”。
        // 这是解决“用户消息刚发出时布局崩坏”的关键一步。
        yield return new WaitForEndOfFrame();

        // 阶段 2：强制所有 ContentSizeFitter 和 LayoutGroup 更新。
        // 这会确保新气泡的 RectTransform 尺寸正确，以及其直接父级 LayoutGroup 也随之调整。
        // Canvas.ForceUpdateCanvases() 会触发整个 Canvas 的布局更新，通常很有效。
        Canvas.ForceUpdateCanvases();

        // 阶段 3：强制主 Content 的 LayoutGroup 立即重新计算。
        // 确保 Content 的总高度已经根据所有新气泡的大小调整完毕。
        if (chatContent != null) // 防止 chatContent 为空时报错
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
        }

        // 阶段 4：再次等待一帧。
        // 确保 Unity 的渲染系统完全同步了 Content 的新高度，
        // 这样 scrollRect.verticalNormalizedPosition = 0f 才能指向正确的新底部。
        yield return new WaitForEndOfFrame();

        // 阶段 5：执行滚动。
        // 0f 代表滚动到最底部。
        scrollRect.verticalNormalizedPosition = 0f;

        // 阶段 6：再次强制刷新和等待，作为最终的保障。
        // 有时即使前面都做了，极端情况（如多层嵌套、非常长的文本）仍可能需要额外一帧来稳定。
        Canvas.ForceUpdateCanvases();
        if (chatContent != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatContent.GetComponent<RectTransform>());
        }
        yield return new WaitForEndOfFrame();
        scrollRect.verticalNormalizedPosition = 0f;

        currentScrollCoroutine = null; // 协程执行完毕，清空引用
    }
}