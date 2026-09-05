Rule:

Unity presentation/business code MUST NOT:
- P/Invoke Win32
- use HWND
- reference GWL/WS/WM constants
- calculate monitor bounds from Screen.currentResolution
- directly manipulate Windows styles

All Windows window operations MUST go through:
IWindowService