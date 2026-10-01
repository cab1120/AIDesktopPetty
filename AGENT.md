# AI 入口兼容文件

统一协作规则见 [AGENTS.md](AGENTS.md)，本文件不再维护另一套规则。

Unity 业务/表现代码不得直接 P/Invoke Win32、使用 HWND/GWL/WS/WM 常量、按 Screen.currentResolution 计算显示器边界或直接改窗口样式。窗口操作通过 IWindowService，前台感知通过 WindowsForegroundContextService；当前边界与缺口见 [架构](docs/ARCHITECTURE.md)，实现任务定位见 [指南](docs/IMPLEMENTATION_GUIDE.md)。本文件不维护重复事实。
