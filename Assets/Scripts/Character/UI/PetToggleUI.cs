using UnityEngine;

public class PetToggleUI : MonoBehaviour
{
    public static PetToggleUI instance;
    public GameObject petIcon;
    public GameObject chatPanel;
    public WindowSizeController windowSize;

    public GameObject ExpandButton;
    public GameObject Functions;
    
    private bool isExpanded = false;
    private bool isExtended = false;

    void Awake()
    {
        instance = this;
    }
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
            
            windowSize.ToggleWindow(isExpanded);
        }
        else
        {
            petIcon.SetActive(true);
            chatPanel.SetActive(false);
            
            windowSize.ToggleWindow(isExpanded);
        }
    }

    public void ToggleButton()
    {
        if (isExtended)
        { 
            Functions.SetActive(false);
            ExpandButton.SetActive(true);
        }
        else
        {
            Functions.SetActive(true);
            ExpandButton.SetActive(false);
        }
        
        isExtended = !isExtended;
    }

    public void ExitButton()
    {
        Application.Quit();
    }
}