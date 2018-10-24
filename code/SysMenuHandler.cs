/*************************************************************************
 * 
 * CENGAGE CONFIDENTIAL
 * ____________________
 * 
 *  [2017] Cengage Learning 
 *  All Rights Reserved.
 * 
 * NOTICE:  All information contained herein is, and remains
 * the property of Cengage Learning. The intellectual and technical 
 * concepts contained herein are proprietary to Cengage Learning
 * and may be covered by U.S. and Foreign Patents, patents in process, 
 * and are protected by trade secret or copyright law. Dissemination 
 * of this information or reproduction of this material is strictly
 * forbidden unless prior written permission is obtained from 
 * Cengage Learning. (Author - Amresh Kumar)
 * 
 * SysMenuHandler.cs - Custom menu added to sys menu handled here.
 * 
 */


using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace FTXFixer {
  partial class FTXFixerWnd : Window, INotifyPropertyChanged {
    /// <summary> This is the Win32 Interop Handle for this Window </summary>
    private IntPtr Handle => new WindowInteropHelper(this).Handle;

    /// <summary> Inserts our custom menu items in system menu </summary> 
    private void AddSysMenu() {
      // Get the Handle for the Forms System Menu
      IntPtr systemMenuHandle = GetSystemMenu(this.Handle, false);

      // Create our new System Menu items just before the Close menu item
      InsertMenu(systemMenuHandle, 5, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty); // <-- Add a menu seperator
      InsertMenu(systemMenuHandle, 6, MF_BYPOSITION, _PatternAndSettings, "Logs and Pattern Settings\tF2");
      InsertMenu(systemMenuHandle, 7, MF_BYPOSITION, _SettingsSysMenuID, "Open Settings INI\tF3");
      InsertMenu(systemMenuHandle, 8, MF_BYPOSITION, _ReloadSettingsMenuID, "Reload Settings INI\tF4");
      InsertMenu(systemMenuHandle, 9, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty);
      InsertMenu(systemMenuHandle, 10, MF_BYPOSITION, _InputLocationSysMenuID, "Open Input Location\tF6");
      InsertMenu(systemMenuHandle, 11, MF_BYPOSITION, _OutputLocationSysMenuID, "Open Output Location\tF7");
      InsertMenu(systemMenuHandle, 12, MF_BYPOSITION, _LogLocationSysMenuID, "Open Log Location\tF8");
      InsertMenu(systemMenuHandle, 13, MF_BYPOSITION | MF_SEPARATOR, 0, string.Empty);
      InsertMenu(systemMenuHandle, 14, MF_BYPOSITION, _ManualSysMenuID, "Help\tF1");
      InsertMenu(systemMenuHandle, 15, MF_BYPOSITION, _AboutSysMenuID, "About FTXFixer\tF9");

      // Attach our WndProc handler to this Window
      HwndSource source = HwndSource.FromHwnd(this.Handle);
      source.AddHook(new HwndSourceHook(WndProc));
    }

    /// <summary> Message handler, handles our custom menu in this case </summary>
    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
      if(msg == WM_SYSCOMMAND) {
        switch(wParam.ToInt32()) {
          case _PatternAndSettings:
            OnAddPattern(null, null);
            handled = true;
            break;
          case _SettingsSysMenuID:
            OpenSettingsINI();
            handled = true;
            break;
          case _ReloadSettingsMenuID:
            ReloadSettingsINI();
            handled = true;
            break;
          case _InputLocationSysMenuID:
            OpenLocation(_InputPath);
            handled = true;
            break;
          case _OutputLocationSysMenuID:
            OpenLocation(_OutputPath);
            handled = true;
            break;
          case _LogLocationSysMenuID:
            OpenLocation(setWnd.LogPath);
            handled = true;
            break;
          case _ManualSysMenuID:
            ShowManual();
            handled = true;
            break;
          case _AboutSysMenuID:
            ShowAbout();
            handled = true;
            break;
        }
      }

      return IntPtr.Zero;
    }

    /// <summary> Handler for open log, input and output location menu </summary>
    /// <param name="location"> log, input or output location </param>
    private void OpenLocation(string location) {
      if(location.Length == 0 || !Directory.Exists(location)) {
        MessageBox.Show(this, "Location is either not set or invalid.", "Invalid location!", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      Process.Start(location);
    }

    /// <summary> Handler for open settings menu, opens INI file </summary>
    private void OpenSettingsINI() {
      if(!File.Exists("FTXFixer.INI")) {
        MessageBox.Show(this, "Settings file doesn't exist.", "Settings not found!", MessageBoxButton.OK, MessageBoxImage.Warning);
        return;
      }

      Process.Start(@"FTXFixer.INI");
    }

    /// <summary> Reloads settings from INI file </summary>
    private void ReloadSettingsINI() => LoadSettings();

    /// <summary> Enables or disables relvant menus </summary>
    /// <param name="iEnable"> true to enable, false to disable </param>
    private void UpdateMenu(bool iEnable) {
      IntPtr systemMenuHandle = GetSystemMenu(this.Handle, false);
      uint menuStatus = (iEnable) ? MF_ENABLED : MF_GRAYED;
      EnableMenuItem(systemMenuHandle, _PatternAndSettings, MF_BYCOMMAND | menuStatus);
      EnableMenuItem(systemMenuHandle, _ReloadSettingsMenuID, MF_BYCOMMAND | menuStatus);
    }

    /// <summary> return build date of exe </summary>
    /// <param name="assembly"> executing assembly object </param>
    /// <returns> build date </returns>
    private DateTime GetLinkerTime(Assembly assembly, TimeZoneInfo target = null) {
      var filePath = assembly.Location;
      const int c_PeHeaderOffset = 60;
      const int c_LinkerTimestampOffset = 8;
      var buffer = new byte[2048];
      using(var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        stream.Read(buffer, 0, 2048);

      var offset = BitConverter.ToInt32(buffer, c_PeHeaderOffset);
      var secondsSince1970 = BitConverter.ToInt32(buffer, offset + c_LinkerTimestampOffset);
      var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
      var linkTimeUtc = epoch.AddSeconds(secondsSince1970);
      var tz = target ?? TimeZoneInfo.Local;
      var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

      return localTime;
    }

    /// <summary> returns version info of application </summary>
    /// <returns> version info </returns>
    public string GetAssemblyInfo(AssemblyType atype) {
      string aInfo = "";
      Assembly assembly = Assembly.GetExecutingAssembly();
      FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
      switch(atype) {
        case AssemblyType.VERSION:
          aInfo = string.Format("{0}.{1} (r{2})", fvi.FileMajorPart, fvi.FileMinorPart, fvi.FileBuildPart);
          break;
        case AssemblyType.COPYRIGHT:
          aInfo = fvi.LegalCopyright;
          break;
        case AssemblyType.BUILTDATE:
          DateTime buildDate = GetLinkerTime(assembly);
          aInfo = buildDate.ToString("MMM dd, yyyy");
          break;
        case AssemblyType.COMPANY:
          aInfo = fvi.CompanyName;
          break;
        case AssemblyType.PRODUCT:
          aInfo = fvi.ProductName;
          break;
      }

      return aInfo;
    }

    /// <summary> Shows application manual </summary>
    private void ShowManual() {
      try {
        string strHelpFile = "Using FTXFixer.pdf";
        if(File.Exists(strHelpFile))
          Process.Start(strHelpFile);
        else
          MessageBox.Show(this, "Couldn't locate usage guide \"Using FTXFixer.pdf\".", "Usage doc not found!",
              MessageBoxButton.OK, MessageBoxImage.Error);
      } catch {
        MessageBox.Show(this, "Failed to load usage guide \"Using FTXFixer.pdf\".", "Usage doc load error!",
            MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /* Application History
     * Version 1.0 - First build (Amresh Kumar)
     * Version 1.1 - Big changes in design, made it mudular (Amresh Kumar)
     * Version 1.2 - Dev and Unit testing compelted (amresh Kumar)
     * Version 1.3 - Handles multiple patterns in single line and pattern(s) between line (Amresh Kumar)
     * Version 1.4 - Handles patterns without brackets, doesn't identify as article header if space
     *               found with header pattern and doesn't write empty article into output (Amresh Kumar)
     */
    /// <summary> Handler for about menu, show version info and copyright information </summary>
    private void ShowAbout() {
      if(aboutWnd == null)
        aboutWnd = new code.About();

      aboutWnd.Owner = this;
      aboutWnd.AppImage = this.Icon;
      aboutWnd.Copyright = headerborder.ToolTip.ToString();
      aboutWnd.BuildDate = GetAssemblyInfo(AssemblyType.BUILTDATE);
      aboutWnd.BuildVersion = GetAssemblyInfo(AssemblyType.VERSION);
      aboutWnd.ShowDialog();
    }

    #region Win32 API Stuff

    // Define the Win32 API methods we are going to use
    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
    [DllImport("user32.dll")]
    private static extern bool InsertMenu(IntPtr hMenu, Int32 wPosition, Int32 wFlags, Int32 wIDNewItem, string lpNewItem);
    [DllImport("user32.dll")]
    private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

    // Define our Constants we will use
    private const Int32 WM_SYSCOMMAND = 0x112;
    private const Int32 MF_SEPARATOR = 0x800;
    private const Int32 MF_BYPOSITION = 0x400;
    private const Int32 MF_STRING = 0x0;
    private const uint MF_BYCOMMAND = 0x00000000;
    private const uint MF_GRAYED = 0x00000001;
    private const uint MF_ENABLED = 0x00000000;

    #endregion

    // The constants we'll use to identify our custom system menu items
    private const Int32 _PatternAndSettings = 1000;
    private const Int32 _SettingsSysMenuID = 1001;
    private const Int32 _ReloadSettingsMenuID = 1002;
    private const Int32 _InputLocationSysMenuID = 1003;
    private const Int32 _OutputLocationSysMenuID = 1004;
    private const Int32 _LogLocationSysMenuID = 1005;
    private const Int32 _ManualSysMenuID = 1006;
    private const Int32 _AboutSysMenuID = 1007;
  }
}
