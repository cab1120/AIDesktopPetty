using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomButtonClicker : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public UnityEvent onClick; // 在 Inspector 面板关联点击事件
    
    [Header("Visual Feedback")]
    public Image targetGraphic;
    public Color hoverColor = new Color(0.9f, 0.9f, 0.9f);
    public Color pressedColor = new Color(0.7f, 0.7f, 0.7f);
    private Color normalColor;

    void Start()
    {
        if (targetGraphic == null) targetGraphic = GetComponent<Image>();
        if (targetGraphic != null) normalColor = targetGraphic.color;
    }

    // 供外部（拖拽脚本）调用的模拟点击方法
    public void PerformClick()
    {
        Debug.Log("检测为有效点击，执行事件");
        onClick?.Invoke();
    }

    // 处理基础视觉反馈
    public void OnPointerEnter(PointerEventData eventData) => SetColor(hoverColor);
    public void OnPointerExit(PointerEventData eventData) => SetColor(normalColor);
    public void OnPointerDownVisual() => SetColor(pressedColor);
    public void OnPointerUpVisual() => SetColor(normalColor);

    private void SetColor(Color c) { if (targetGraphic) targetGraphic.color = c; }
}