using UnityEngine;

public class AIContextReactionManager : MonoBehaviour
{
    public DesktopContextManager contextManager;
    public BubbleUIManager bubbleUI;
    public AIChat aiChat;

    private float lastReactionTime;
    public float globalCooldown = 3f;

    private bool requestInFlight;

    void OnEnable()
    {
        contextManager.OnWindowChanged += OnWindowChanged;
    }

    void OnDisable()
    {
        contextManager.OnWindowChanged -= OnWindowChanged;
        aiChat?.CancelBubbleRequests();
        StopAllCoroutines();
        requestInFlight = false;
    }

    void OnWindowChanged(string title, string processName)
    {
        SessionSnapshot session = GlobalSession.CaptureSnapshot();
        if (!GlobalSession.IsCurrent(session))
            return;

        Debug.Log("正在检测：" + title);

        if (Time.time < lastReactionTime + globalCooldown)
        {
            InteractionEventService.RecordBubbleSuppressed(
                session,
                title,
                processName,
                "全局冷却中"
            );
            return;
        }

        bool canTrigger = InteractionEventService.CanTriggerBubble(
            session,
            title,
            processName,
            out string contextKey,
            out string reason
        );

        if (!canTrigger)
        {
            InteractionEventService.RecordBubbleSuppressed(
                session,
                title,
                processName,
                reason
            );
            return;
        }

        if (requestInFlight)
            return;

        lastReactionTime = Time.time;

        InteractionEventService.RecordBubbleRequested(session, title, processName);

        string aiContext =
            $"窗口标题：{title}\n进程名：{processName}";

        StartCoroutine(GenerateReaction(session, title, processName, aiContext));
    }

    private System.Collections.IEnumerator GenerateReaction(
        SessionSnapshot session, string title, string processName, string context)
    {
        requestInFlight = true;
        string reply = null;
        try
        {
            yield return aiChat.GetAIBubbleReply(
                session, context, result => reply = result
            );
            if (!isActiveAndEnabled || !GlobalSession.IsCurrent(session))
                yield break;

            if (string.IsNullOrWhiteSpace(reply))
                yield break;

            if (reply.Contains("[IGNORE]"))
            {
                InteractionEventService.RecordBubbleIgnored(
                    session, title, processName
                );
                yield break;
            }

            bubbleUI.ShowBubble(reply);

            InteractionEventService.RecordBubbleShown(
                session, title, processName, reply
            );
        }
        finally
        {
            requestInFlight = false;
        }
    }
}
