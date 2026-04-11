using UnityEngine;

public class PetToggleUI : MonoBehaviour
{
    public GameObject petIcon;
    public GameObject chatPanel;

    private bool isExpanded = false;

    void Start()
    {
        // 初始状态：只显示头像
        petIcon.SetActive(true);
        chatPanel.SetActive(false);
    }

    public void ToggleUI()
    {
        isExpanded = !isExpanded;

        if (isExpanded)
        {
            petIcon.SetActive(false);
            chatPanel.SetActive(true);
        }
        else
        {
            petIcon.SetActive(true);
            chatPanel.SetActive(false);
        }
    }
}