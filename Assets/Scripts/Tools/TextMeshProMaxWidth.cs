using UnityEngine;
using TMPro;
using UnityEngine.UI;

[ExecuteInEditMode]
public class TextMeshProMaxWidth : MonoBehaviour
{
    public float maxWidth = 400f; 
    private TextMeshProUGUI textMesh;
    private LayoutElement layoutElement;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        layoutElement = GetComponent<LayoutElement>();
    }

    void Update()
    {
        if (textMesh == null || layoutElement == null) return;
        
        float preferredWidth = textMesh.preferredWidth;
        
        if (preferredWidth >= maxWidth)
        {
            layoutElement.preferredWidth = maxWidth;
        }
        else
        {
            layoutElement.preferredWidth = -1;
        }
    }
}
