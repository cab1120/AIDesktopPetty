using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class WindowSnapController : MonoBehaviour
{
#if UNITY_STANDALONE_WIN

    // 获取当前活动窗口的句柄
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    // 获取窗口在屏幕上的矩形区域（左上角和右下角坐标）
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // 设置窗口的位置和大小
    // hWndInsertAfter: Z轴顺序，传 IntPtr.Zero 表示不改变层级
    // uFlags: 行为标志，传 0 表示同时更新位置和大小
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    // 直接查询 Windows 全局键/鼠标状态，不依赖 Unity 输入系统
    // 即使鼠标移出窗口外也能正确检测，解决拖出屏幕后松开检测不到的问题
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    // ✅ 新增：获取鼠标在屏幕上的全局坐标（Windows 坐标系，左上角为原点）
    // 替代 Input.mousePosition，解决窗口隐藏后鼠标坐标失效的问题
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    // Win32 矩形结构体，Sequential 确保内存布局与 C 结构体一致
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    // ✅ 新增：Win32 点结构体，用于接收 GetCursorPos 的结果
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X, Y;
    }

    // 鼠标左键的虚拟键码
    const int VK_LBUTTON = 0x01;

#endif

    // 触发吸附的距离阈值（像素）：窗口边界距屏幕边缘多近时触发吸附
    public int snapDistance = 20;

    // 半隐藏时窗口露出屏幕边缘的像素数
    public int hideOffset = 80;

    // 记录上一帧鼠标左键是否按下，用于检测"松开"的边沿跳变
    private bool wasMouseDown = false;

    // 缓存窗口句柄，避免每帧重复调用 GetActiveWindow
    private IntPtr hwnd;

    // 枚举：当前吸附的边（None 表示没有吸附）
    enum SnapEdge { None, Left, Right, Top, Bottom }

    // 当前吸附状态
    private SnapEdge currentEdge = SnapEdge.None;

    // 当前是否处于半隐藏状态
    private bool isHidden = false;

    //缓存窗口尺寸，隐藏后 GetWindowRect 位置会变，需要用原始尺寸计算
    private int cachedWindowWidth;
    private int cachedWindowHeight;

    //缓存吸附后窗口的 Y 坐标（左右吸附用），防止隐藏后丢失原始位置
    private int cachedSnapY;

    void Start()
    {
#if UNITY_STANDALONE_WIN
        // 游戏启动时获取并缓存窗口句柄
        hwnd = GetActiveWindow();
#endif
    }

    void Update()
    {
#if UNITY_STANDALONE_WIN

        // 取返回值最高位（bit15）：1 = 当前按下，0 = 松开
        bool isMouseDown = (GetAsyncKeyState(VK_LBUTTON) >> 15 & 1) == 1;

        // 上一帧按下、本帧松开 → 检测到鼠标刚刚释放，触发吸附判断
        if (wasMouseDown && !isMouseDown)
        {
            SnapToEdge();
        }

        // 保存本帧状态供下一帧比较
        wasMouseDown = isMouseDown;

        // 每帧检测自动隐藏/显示逻辑
        HandleAutoHide();

#endif
    }

    void SnapToEdge()
    {
#if UNITY_STANDALONE_WIN

        RECT rect;
        GetWindowRect(hwnd, out rect);

        int windowWidth  = rect.Right  - rect.Left;
        int windowHeight = rect.Bottom - rect.Top;

        int screenWidth  = Screen.currentResolution.width;
        int screenHeight = Screen.currentResolution.height;

        // 默认目标位置 = 当前位置（没有吸附时不移动）
        int newX = rect.Left;
        int newY = rect.Top;

        // 重置吸附状态
        currentEdge = SnapEdge.None;

        // --- 水平方向吸附判断 ---

        // 左边：左边界进入阈值范围（包括拖出屏幕左侧的情况）
        if (rect.Left < snapDistance)
        {
            newX = 0;
            currentEdge = SnapEdge.Left;
        }
        // 右边：右边界进入阈值范围（包括拖出屏幕右侧的情况）
        else if (rect.Right > screenWidth - snapDistance)
        {
            newX = screenWidth - windowWidth;
            currentEdge = SnapEdge.Right;
        }

        // --- 垂直方向吸附判断 ---

        // 顶部：上边界进入阈值范围
        if (rect.Top < snapDistance)
        {
            newY = 0;
            currentEdge = SnapEdge.Top;
        }
        // 底部：下边界进入阈值范围
        else if (rect.Bottom > screenHeight - snapDistance)
        {
            newY = screenHeight - windowHeight;
            currentEdge = SnapEdge.Bottom;
        }

        // 保底 Clamp：防止窗口完全跑出屏幕之外
        newX = Mathf.Clamp(newX, 0, screenWidth  - windowWidth);
        newY = Mathf.Clamp(newY, 0, screenHeight - windowHeight);

        // ✅ 新增：吸附完成后缓存窗口尺寸和 Y 坐标
        // 隐藏时窗口会被移出屏幕，GetWindowRect 结果会变，必须在这里保存原始值
        cachedWindowWidth  = windowWidth;
        cachedWindowHeight = windowHeight;
        cachedSnapY        = newY;

        // =====================================================
        // 【扩展点 A】吸附完成，窗口位置已确定
        // 如果需要在吸附时播放动画（如弹跳、缩放等），在这里启动协程：
        // StartCoroutine(PlaySnapAnimation(newX, newY));
        // 如果需要在吸附时切换桌宠形态（如变成迷你版、换贴图），在这里调用：
        // petAnimator.SetTrigger("OnSnap");
        // petStateManager.EnterSnapState(currentEdge);
        // =====================================================

        SetWindowPos(hwnd, IntPtr.Zero,
            newX, newY,
            windowWidth, windowHeight,
            0);

        // ✅ 新增：吸附后重置隐藏状态，防止上一次隐藏状态残留影响本次判断
        isHidden = false;

#endif
    }

    void HandleAutoHide()
    {
#if UNITY_STANDALONE_WIN

        // 没有吸附到任何边则不处理
        if (currentEdge == SnapEdge.None) return;

        // ✅ 修改：改用 GetCursorPos 获取全局鼠标坐标
        // 原来用 Input.mousePosition，窗口隐藏到边缘后鼠标不在窗口内，坐标会失效
        POINT cursor;
        GetCursorPos(out cursor);

        int screenWidth  = Screen.currentResolution.width;
        int screenHeight = Screen.currentResolution.height;

        // --- 右边吸附的自动隐藏/显示 ---
        if (currentEdge == SnapEdge.Right)
        {
            // 鼠标离开右侧区域 → 触发隐藏
            // ✅ 修改：用 cursor.X 替代 mousePos.x，坐标系一致（都是左上角原点）
            if (!isHidden && cursor.X < screenWidth - hideOffset)
            {
                // =====================================================
                // 【扩展点 B】即将隐藏到右边缘
                // 如果需要滑出动画，在此处启动，动画结束后再调用 HideRight
                // StartCoroutine(SlideOutRight());
                // =====================================================

                HideRight();
            }

            // 鼠标贴近右边缘 → 触发显示
            if (isHidden && cursor.X >= screenWidth - hideOffset)
            {
                // =====================================================
                // 【扩展点 C】即将从右边缘弹出
                // 如果需要滑入动画或切换为正常桌宠状态，在此处处理：
                // StartCoroutine(SlideInRight());
                // petStateManager.ExitHideState();
                // =====================================================

                ShowRight();
            }
        }

        // --- 左边吸附的自动隐藏/显示 ---
        if (currentEdge == SnapEdge.Left)
        {
            // 鼠标离开左侧区域 → 触发隐藏
            if (!isHidden && cursor.X > hideOffset)
            {
                // =====================================================
                // 【扩展点 D】即将隐藏到左边缘，同扩展点 B
                // =====================================================

                HideLeft();
            }

            // 鼠标贴近左边缘 → 触发显示
            if (isHidden && cursor.X <= hideOffset)
            {
                // =====================================================
                // 【扩展点 E】即将从左边缘弹出，同扩展点 C
                // =====================================================

                ShowLeft();
            }
        }

        // =====================================================
        // 【扩展点 F】如需支持顶部/底部的自动隐藏，在此处仿照上面补充
        // if (currentEdge == SnapEdge.Top) { ... }
        // if (currentEdge == SnapEdge.Bottom) { ... }
        // =====================================================

#endif
    }

    // 将窗口向右隐藏：只露出 hideOffset 像素在屏幕内
    // ✅ 修改：不再接收 RECT 参数，改用缓存的尺寸和位置，避免隐藏后坐标错乱
    void HideRight()
    {
#if UNITY_STANDALONE_WIN

        int newX = Screen.currentResolution.width - hideOffset;

        SetWindowPos(hwnd, IntPtr.Zero,
            newX, cachedSnapY,
            cachedWindowWidth, cachedWindowHeight,
            0);

        isHidden = true;

        // =====================================================
        // 【扩展点 G】隐藏完成后的回调
        // 可在此切换桌宠为"睡眠/缩小"状态：
        // petAnimator.SetBool("isHidden", true);
        // =====================================================

#endif
    }

    // 将窗口从右侧完全显示出来：左边界对齐屏幕右边减去窗口宽度
    // ✅ 修改：同 HideRight，改用缓存值
    void ShowRight()
    {
#if UNITY_STANDALONE_WIN

        int newX = Screen.currentResolution.width - cachedWindowWidth;

        SetWindowPos(hwnd, IntPtr.Zero,
            newX, cachedSnapY,
            cachedWindowWidth, cachedWindowHeight,
            0);

        isHidden = false;

        // =====================================================
        // 【扩展点 H】显示完成后的回调
        // 可在此恢复桌宠正常状态、播放出现动画：
        // petAnimator.SetBool("isHidden", false);
        // petAnimator.SetTrigger("OnAppear");
        // =====================================================

#endif
    }

    // 将窗口向左隐藏：窗口大部分移出屏幕左侧，只露出 hideOffset 像素
    // ✅ 修改：同 HideRight，改用缓存值
    void HideLeft()
    {
#if UNITY_STANDALONE_WIN

        // newX 为负数，表示窗口左边界在屏幕左侧之外
        int newX = -(cachedWindowWidth - hideOffset);

        SetWindowPos(hwnd, IntPtr.Zero,
            newX, cachedSnapY,
            cachedWindowWidth, cachedWindowHeight,
            0);

        isHidden = true;

        // =====================================================
        // 【扩展点 I】同扩展点 G
        // =====================================================

#endif
    }

    // 将窗口从左侧完全显示出来：左边界对齐屏幕左边（X = 0）
    // ✅ 修改：同 HideRight，改用缓存值
    void ShowLeft()
    {
#if UNITY_STANDALONE_WIN

        SetWindowPos(hwnd, IntPtr.Zero,
            0, cachedSnapY,
            cachedWindowWidth, cachedWindowHeight,
            0);

        isHidden = false;

        // =====================================================
        // 【扩展点 J】同扩展点 H
        // =====================================================

#endif
    }
}