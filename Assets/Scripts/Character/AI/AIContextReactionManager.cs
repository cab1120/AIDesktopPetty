using System;
using UnityEngine;

public class AIContextReactionManager : MonoBehaviour
{
    public DesktopContextManager contextManager;

    public BubbleUIManager bubbleUI;

    /*void Start()
    {
        contextManager.OnWindowChanged
            += HandleWindowChanged;
    }*/

    private void OnEnable()
    {
        contextManager.OnWindowChanged += HandleWindowChanged;
    }

    private void OnDisable()
    {
        contextManager.OnWindowChanged -= HandleWindowChanged;
    }
    void HandleWindowChanged(string title)
    {
        Debug.Log("检测到窗口变化: " + title);

        if (title.Contains("网易云"))
        {
            bubbleUI.ShowBubble("你在听歌呀~");
        }

        else if (title.Contains("Unity"))
        {
            bubbleUI.ShowBubble("又在开发嘛？");
        }

        else if (title.Contains("QQ"))
        {
            bubbleUI.ShowBubble("在聊天吗？");
        }
    }
}