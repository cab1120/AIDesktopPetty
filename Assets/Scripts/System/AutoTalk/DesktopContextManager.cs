using System;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;

public class DesktopContextManager : MonoBehaviour
{
#if UNITY_STANDALONE_WIN

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    static extern int GetWindowText(
        IntPtr hWnd,
        StringBuilder text,
        int count);

#endif

    private string lastWindowTitle = "";

    public Action<string> OnWindowChanged;
    
    //测试用
    public TextMeshProUGUI textMeshPro;

    void Start()
    {
        InvokeRepeating(
            nameof(CheckWindow),
            1f,
            3f); // 每3秒检测一次
    }

    const int nChars = 256;

    StringBuilder buffer =
        new StringBuilder(nChars);
    void CheckWindow()
    {
#if UNITY_STANDALONE_WIN

        buffer.Clear();

        IntPtr handle = GetForegroundWindow();

        if (GetWindowText(handle, buffer, nChars) > 0)
        {
            string title = buffer.ToString();

            if (title != lastWindowTitle)
            {
                lastWindowTitle = title;

                Debug.Log("当前窗口: " + title);

                OnWindowChanged?.Invoke(title);
                
                //textMeshPro.text = title;
            }
        }

#endif
    }
    //测试用
    
}