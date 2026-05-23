using TMPro;
using UnityEngine;

public class BubbleUIManager : MonoBehaviour
{
    public GameObject bubbleRoot;

    public TMP_Text bubbleText;

    private Coroutine hideCoroutine;

    void Start()
    {
        bubbleRoot.SetActive(false);
        //ShowBubble("你好呀~");
    }

    public void ShowBubble(string message, float duration = 5f)
    {
        bubbleRoot.SetActive(true);

        bubbleText.text = message;

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
}