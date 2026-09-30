using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 普通聊天用例服务。
///
/// 责任：
/// 1. 接受用户输入并创建 ChatTurn。
/// 2. 保证同一 Session 内的 Turn 串行执行。
/// 3. 持有一次聊天从落库到 AI 回复完成的业务生命周期。
/// 4. Session 发生变化时让旧队列失效。
///
/// AIChat 返回成功或失败结果；会话变化会取消旧队列和网络请求。
/// </summary>
public class ConversationService : MonoBehaviour
{
    [SerializeField]
    private AIChat aiChat;

    /// <summary>
    /// AI 回复完成并且仍属于当前 Session 时触发。
    ///
    /// View 只负责监听这个事件并显示结果。
    /// </summary>
    public event Action<ChatTurn, string> AssistantReplyReady;
    public event Action<ChatTurn> TurnCancelled;
    public event Action<ChatTurn> TurnStarted;
    public event Action<ChatTurn, string> TurnFailed;

    private readonly Queue<ChatTurn> pendingTurns =
        new Queue<ChatTurn>();

    private Coroutine queueCoroutine;

    private ChatTurn currentTurn;

    /// <summary>
    /// 当前是否正在处理某一个 Turn。
    /// 主要用于调试和后续 UI 状态显示。
    /// </summary>
    public bool IsProcessing => currentTurn != null;

    /// <summary>
    /// 当前排队等待执行的 Turn 数量。
    /// </summary>
    public int PendingCount => pendingTurns.Count;
    public bool IsPending(ChatTurn turn) => turn != null && pendingTurns.Contains(turn);

    private void OnEnable()
    {
        GlobalSession.SessionVersionChanged +=
            OnSessionVersionChanged;
    }

    private void OnDisable()
    {
        GlobalSession.SessionVersionChanged -=
            OnSessionVersionChanged;
        CancelPendingAndCurrent();
    }

    /// <summary>
    /// 尝试创建并排入一轮新的普通聊天。
    ///
    /// 返回 true 表示本轮输入已经被 ConversationService 接受。
    /// </summary>
    public bool TrySend(
        string input,
        out ChatTurn turn)
    {
        turn = null;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        if (aiChat == null)
        {
            Debug.LogError(
                "ConversationService 未绑定 AIChat。"
            );

            return false;
        }

        SessionSnapshot session =
            GlobalSession.CaptureSnapshot();

        if (!session.IsLoggedIn)
        {
            Debug.LogWarning(
                "无法发送消息：当前没有有效登录 Session。"
            );

            return false;
        }

        turn = ChatTurn.Create(
            session,
            input
        );

        pendingTurns.Enqueue(turn);

        EnsureQueueRunning();

        return true;
    }

    private void EnsureQueueRunning()
    {
        if (queueCoroutine != null)
            return;

        queueCoroutine =
            StartCoroutine(ProcessQueue());
        if (currentTurn == null && pendingTurns.Count == 0)
            queueCoroutine = null;
    }

    /// <summary>
    /// 串行处理聊天队列。
    ///
    /// 一个 Turn 完整结束后，
    /// 才会开始下一个 Turn。
    /// </summary>
    private IEnumerator ProcessQueue()
    {
        try
        {
        while (pendingTurns.Count > 0)
        {
            ChatTurn turn =
                pendingTurns.Dequeue();

            // Turn 还没开始就已经换了 Session，
            // 直接丢弃，不允许旧身份继续进入业务流程。
            if (!GlobalSession.IsCurrent(turn.Session))
            {
                continue;
            }

            currentTurn = turn;

            /*
             * 重要：
             * 用户消息不是在点击发送按钮时立即写库，
             * 而是在真正轮到这个 Turn 执行时写入。
             *
             * 这样排在后面的 Turn 不会提前成为
             * 前一个 Turn 的“未来历史”。
             */
            ChatMessageData userMessage =
                ChatMessageService.SaveUserMessage(
                    turn
                );

            if (userMessage == null)
            {
                Debug.LogError(
                    $"用户消息保存失败。" +
                    $"TurnId={turn.TurnId}"
                );

                currentTurn = null;
                continue;
            }

            RelationshipService.OnUserSendMessage(
                turn.Session,
                turn.Input
            );

            ChatReplyResult reply = null;

            bool callbackInvoked = false;

            yield return StartCoroutine(
                aiChat.GetAIReply(
                    turn,
                    result =>
                    {
                        reply = result;
                        callbackInvoked = true;
                    }
                )
            );

            /*
             * AI 请求期间可能已经发生角色切换。
             *
             * 这时旧请求即使正常返回，
             * 也绝对不能显示、落库或修改新角色关系。
             */
            if (!GlobalSession.IsCurrent(turn.Session))
            {
                currentTurn = null;
                continue;
            }

            // 只有成功且非空的回复可以写入聊天记录。
            if (!callbackInvoked || reply == null || !reply.IsSuccess)
            {
                TurnFailed?.Invoke(turn, reply?.Error ?? "请求未完成");
                currentTurn = null;
                continue;
            }

            TurnStarted?.Invoke(turn);

            ChatMessageData savedReply = ChatMessageService.SaveAssistantMessage(
                turn.Session,
                reply.Text
            );
            if (savedReply == null)
            {
                TurnFailed?.Invoke(turn, "回复保存失败");
                currentTurn = null;
                continue;
            }

            RelationshipService.OnAssistantReplyFinished(
                turn.Session
            );

            AssistantReplyReady?.Invoke(
                turn,
                reply.Text
            );

            currentTurn = null;
        }

        }
        finally
        {
            currentTurn = null;
            queueCoroutine = null;
        }
    }

    /// <summary>
    /// Session 身份改变后，
    /// 尚未开始执行的旧 Turn 不再有继续执行的意义。
    ///
    /// 当前网络请求由 AIChat 中止并释放；旧回复不会落库。
    /// </summary>
    private void OnSessionVersionChanged(
        long newVersion)
    {
        CancelPendingAndCurrent();
    }

    private void CancelPendingAndCurrent()
    {
        while (pendingTurns.Count > 0)
            TurnCancelled?.Invoke(pendingTurns.Dequeue());

        currentTurn = null;
        aiChat?.CancelActiveRequests();
        if (queueCoroutine != null)
        {
            StopCoroutine(queueCoroutine);
            queueCoroutine = null;
        }
    }
}
