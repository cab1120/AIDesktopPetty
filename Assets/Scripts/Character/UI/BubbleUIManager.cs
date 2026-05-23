using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BubbleUIManager : MonoBehaviour
{
    public GameObject bubbleRoot;

    public TMP_Text bubbleText;
    
    public RectTransform backgroundRect; // 挂有 CSF 的背景图片
    public RectTransform rootLayoutRect; // 挂有 VLG 的最外层空物体

    private Coroutine refreshCoroutine;

    private Coroutine hideCoroutine;

    void Start()
    {
        bubbleRoot.SetActive(false);
        //ShowBubble("你好呀~");
    }

    public void ShowBubble(string message, float duration = 5f)
    {
        bubbleRoot.SetActive(true);

        SetBubbleText(message);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }

        hideCoroutine =  StartCoroutine(HideBubbleAfter(duration));
    }

    System.Collections.IEnumerator HideBubbleAfter(float duration)
    {
        yield return new WaitForSeconds(duration);

        bubbleRoot.SetActive(false);
    }
    
    public void SetBubbleText(string message)
    {
        bubbleText.text = message;
        
        // 开启强制刷新协程
        if (refreshCoroutine != null) StopCoroutine(refreshCoroutine);
        refreshCoroutine = StartCoroutine(RefreshLayoutRoutine());
    }

    IEnumerator RefreshLayoutRoutine()
    {
        // 1. 第一步：等待帧末，确保 Text 组件已经感知到内容变化并计算出 Preferred Size
        yield return new WaitForEndOfFrame();

        // 2. 第二步：自下而上强制重绘布局
        // 规则：先让子物体计算大小，再让父物体根据子物体大小调整自己
        
        // 刷新文本所在的容器（背景）
        if (backgroundRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRect);

        // 刷新最外层的 VLG 容器
        if (rootLayoutRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rootLayoutRect);

        //极端保险：再延迟一帧做最后的微调
        yield return null; 
    }
}