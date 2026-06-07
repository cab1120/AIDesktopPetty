using UnityEngine;

public class AIContextReactionManager : MonoBehaviour
{
    public DesktopContextManager contextManager;
    public BubbleUIManager bubbleUI;
    public AIChat aiChat;

    private float lastReactionTime;
    public float globalCooldown = 3f;

    private string pendingTitle;
    private string pendingProcessName;

    void OnEnable()
    {
        contextManager.OnWindowChanged += OnWindowChanged;
    }

    void OnDisable()
    {
        contextManager.OnWindowChanged -= OnWindowChanged;
    }

    void OnWindowChanged(string title, string processName)
    {
        Debug.Log("正在检测：" + title);

        if (Time.time < lastReactionTime + globalCooldown)
        {
            InteractionEventService.RecordBubbleSuppressed(
                title,
                processName,
                "全局冷却中"
            );
            return;
        }

        bool canTrigger = InteractionEventService.CanTriggerBubble(
            title,
            processName,
            out string contextKey,
            out string reason
        );

        if (!canTrigger)
        {
            InteractionEventService.RecordBubbleSuppressed(
                title,
                processName,
                reason
            );
            return;
        }

        pendingTitle = title;
        pendingProcessName = processName;

        lastReactionTime = Time.time;

        InteractionEventService.RecordBubbleRequested(title, processName);

        string aiContext =
            $"窗口标题：{title}\n进程名：{processName}";

        StartCoroutine(aiChat.GetAIBubbleReply(
            aiContext,
            OnReactionGenerated
        ));
    }

    void OnReactionGenerated(string reply)
    {
        if (string.IsNullOrWhiteSpace(reply))
        {
            InteractionEventService.RecordBubbleIgnored(
                pendingTitle,
                pendingProcessName
            );
            return;
        }

        if (reply.Contains("[IGNORE]"))
        {
            InteractionEventService.RecordBubbleIgnored(
                pendingTitle,
                pendingProcessName
            );
            return;
        }

        bubbleUI.ShowBubble(reply);

        InteractionEventService.RecordBubbleShown(
            pendingTitle,
            pendingProcessName,
            reply
        );
    }
}