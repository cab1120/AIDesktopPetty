using UnityEngine;
using UnityEngine.UI;

public class WindowSizeController : MonoBehaviour
{
    public int expandedWidth = 432;
    public int expandedHeight = 768;

    public int collapsedWidth = 180;
    public int collapsedHeight = 180;
    
    public CanvasScaler canvasScaler;
    
    void Start()
    {
        Collapse();
    }

    public void ToggleWindow(bool isExpanded)
    {
        if (isExpanded)
        {
            Expand();
        }
        else
        {
            Collapse();
        }
    }

    void Expand()
    {
        canvasScaler.enabled = true;
        Screen.SetResolution(
            expandedWidth,
            expandedHeight,
            false
        );
        Debug.Log("biggger");
    }

    void Collapse()
    {
        canvasScaler.enabled = false;
        Screen.SetResolution(
            collapsedWidth,
            collapsedHeight,
            false
        );
        
        Debug.Log("smaller");
    }
}