using UnityEngine;
using UnityEngine.UI;

public class PetToggleUI : MonoBehaviour
{
    public static PetToggleUI instance;
    public GameObject petIcon;
    public GameObject chatPanel;

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
        chatPanel.SetActive(false);
    }

    public void ToggleUI()
    {
        isExpanded = !isExpanded;

        if (isExpanded)
        {
            petIcon.SetActive(false);
            chatPanel.SetActive(true);
            DesktopPetLayoutController
                .Instance
                .ApplyLayout(
                    DesktopPetLayoutMode
                        .Chat
                );
            
            RelationshipService.OnOpenPetPanel(); // 更新好感度
            InteractionEventService.RecordPetExpanded(); // 记录展开事件

        }
        else
        {
            petIcon.SetActive(true);
            chatPanel.SetActive(false);
            DesktopPetLayoutController
                .Instance
                .ApplyLayout(
                    DesktopPetLayoutMode
                        .PetCollapsed
                );
            
            InteractionEventService.RecordPetCollapsed(); // 记录关闭事件
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