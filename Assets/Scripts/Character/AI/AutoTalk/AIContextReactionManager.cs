using UnityEngine;

public class AIContextReactionManager
    : MonoBehaviour
{
    public DesktopContextManager contextManager;

    public BubbleUIManager bubbleUI;

    public AIChat aiChat;

    private float lastReactionTime;

    private string lastContext;

    public float cooldown = 3f;

    void OnEnable()
    {
        contextManager.OnWindowChanged += OnWindowChanged;
    }

    void OnDisable()
    {
        contextManager.OnWindowChanged -= OnWindowChanged;
    }

    void OnWindowChanged(string title,string processName)
    {
        Debug.Log("正在检测："+title);

        if (!ContextEvaluator.IsInteresting(title,processName))
        {
            Debug.Log("关键词检测不通过");
            return;
        }


        if (title == lastContext)
        {
            Debug.Log("重复窗口");
            return;
        }
            

        if (Time.time < lastReactionTime + cooldown)
        {
            Debug.Log("时间冷却中"+Time.time+"<"+lastReactionTime+"+"+cooldown);
            return;
        }
        
        Debug.Log("检测通过");
        
        lastContext = title;

        lastReactionTime = Time.time;

        StartCoroutine(aiChat.GetAIBubbleReply(
                title,
                OnReactionGenerated));
    }

    void OnReactionGenerated(string reply)
    {
        if (reply.Contains("[IGNORE]")) return;
        bubbleUI.ShowBubble(reply);
    }
}