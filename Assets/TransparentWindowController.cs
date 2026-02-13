using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class TransparentWindowController : MonoBehaviour
{
    [Header("Settings")]
    public LayerMask clickLayer;
    public string windowName = "YourProjectName";

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    // --- Win32 API for Mouse Position ---
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int x; public int y; }
    
    [DllImport("user32.dll")]
    static extern bool GetCursorPos(out POINT lpPoint);
    
    [DllImport("user32.dll")]
    static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);
    
    // --- Win32 API for Window ---
    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
    
    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    
    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
    
    [DllImport("dwmapi.dll")]
    static extern int DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS pMargins);
    
    [StructLayout(LayoutKind.Sequential)]
    struct MARGINS { public int cxLeftWidth, cxRightWidth, cyTopHeight, cyBottomHeight; }
    
    const int GWL_EXSTYLE = -20;
    const int WS_EX_LAYERED = 0x80000;
    const int WS_EX_TRANSPARENT = 0x20;
    const int WS_EX_TOPMOST = 0x00000008;
    const uint SWP_FRAMECHANGED = 0x0020;
    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOSIZE = 0x0001;
    
    private IntPtr hwnd;
    private bool isClickThrough = true; // Start as click-through
    
    void Start()
    {
        hwnd = FindWindow(null, windowName);
        
        if (hwnd == IntPtr.Zero)
        {
            Debug.LogError("Window not found! Make sure windowName matches your project name.");
            return;
        }
        
        // Standard transparency setup
        var margins = new MARGINS { cxLeftWidth = -1 };
        DwmExtendFrameIntoClientArea(hwnd, ref margins);
        
        // Start as click-through
        SetWindowStyle(true);
    }
    
    void Update()
    {
        if (hwnd == IntPtr.Zero) return;
        
        // 1. Get GLOBAL mouse position from Windows
        POINT pt;
        if (!GetCursorPos(out pt)) return;
        
        // 2. Convert screen coordinates to Unity window coordinates
        if (!ScreenToClient(hwnd, ref pt)) return;
        
        // 3. Create a ray using the Windows-sourced mouse position
        // Note: Unity Y-axis is inverted compared to Windows Y-axis
        Vector2 mousePos = new Vector2(pt.x, Screen.height - pt.y);
        
        // Check if mouse is within window bounds
        if (pt.x < 0 || pt.x > Screen.width || pt.y < 0 || pt.y > Screen.height)
        {
            if (!isClickThrough)
            {
                SetWindowStyle(true); // Make click-through when outside window
            }
            return;
        }
        
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        
        // 4. Perform Raycast
        RaycastHit hit;
        bool hitSomething = Physics.Raycast(ray, out hit, 1000f, clickLayer);
        
        // Toggle logic: 
        // If we hit something AND currently click-through -> make solid
        // If we didn't hit anything AND currently solid -> make click-through
        if (hitSomething && isClickThrough)
        {
            SetWindowStyle(false); // Make Solid - can interact
            Debug.Log("Hit object: " + hit.collider.name + " - Window is now SOLID");
        }
        else if (!hitSomething && !isClickThrough)
        {
            SetWindowStyle(true); // Make Transparent - click through
            Debug.Log("No hit - Window is now CLICK-THROUGH");
        }
    }
    
    void SetWindowStyle(bool clickThrough)
    {
        isClickThrough = clickThrough;
        
        int exStyle = WS_EX_LAYERED | WS_EX_TOPMOST;
        if (clickThrough) exStyle |= WS_EX_TRANSPARENT;
        
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);
        SetWindowPos(hwnd, new IntPtr(-1), 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_FRAMECHANGED);
    }
#endif
}