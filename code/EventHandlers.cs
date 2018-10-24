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
 * EventHandlers.cs - All event handler code handled here.
 * 
 */


using FTXFixer.code;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FTXFixer {
  partial class FTXFixerWnd {
    /// <summary> Loaded even handler, sets tooltips and loads settings. </summary>
    private void OnLoaded(object sender, EventArgs e) {
      headerborder.ToolTip = GetAssemblyInfo(AssemblyType.COPYRIGHT);
      lbPatterns.ToolTip = "No pattern found, only defaults([? omitted], [? omitted.]) will be processed.";
      btnInput.ToolTip = "Click to set input location.";
      txtInput.ToolTip = "Click SET button to set input location.";
      btnOutput.ToolTip = "Click to set output location.";
      txtOutput.ToolTip = "Click SET button to set output location.";
      btnAddPattern.ToolTip = "Click to add new full-text pattern\nor to change logging settings.";
      btnRemovePattern.ToolTip = "Click to remove existing full-text pattern.";
      btnProcess.ToolTip = "Click to start processing input location.";
      statusStrip.ToolTip = "Displays status messages.";
      cmbLanguage.ToolTip = "Select language to add footer in article data.";
      cbFooter.ToolTip = "Footer option disabled.";
      cbClean.ToolTip = "Enable to clean existing output files.";
      processProgress.ToolTip = "Progress meter.";
      btnUndo.ToolTip = "Click to undo last pattern removal.";
      btnMenu.ToolTip = "Click or right click on static area to open menu options.";
      btnRemovePattern.IsEnabled = false;
      InputPath = "<Click SET button to set input location>";
      OutputPath = "<Click SET button to set output location>";
      removedPatterns = new List<Pattern>();
      setWnd = new SettingsWnd(this);
      LoadSettings(true);
      ftxProcessor = new FTXProcessor(this, setWnd) {
        InputPath = _InputPath,
        OutputPath = _OutputPath
      };
      gridUnitSize = r11.ActualHeight;
      heightToSave = this.Height;
      this.Cursor = Cursors.Arrow;
      cmbLanguage.SelectedIndex = _LangIndex;
    }

    /// <summary> Closing event handler, if processing underway, confirm before exit. </summary>
    private void OnClosing(object sender, CancelEventArgs e) {
      if(_Processing && MessageBox.Show(this, "Processing underway, exit anyway?", "Confirm Exit",
             MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
        e.Cancel = true;
      else
        AbortProcessing();
    }

    /// <summary> Closed event handler, destroys pattern window and saves settings. </summary>
    private void OnClosed(object sender, EventArgs e) {
      SaveSettings();
      setWnd.Close();

      try {
        appLog.Close();
      } catch { }
    }

    /// <summary> SizeChanged event handler, saves row size and actual height to save. </summary>
    private void OnResize(object sender, EventArgs e) {
      if(sizeChangedForExpander) {
        sizeChangedForExpander = false;
        return;
      }

      if(!gbInput.IsExpanded && !gbPatterns.IsExpanded) {
        this.Height = this.MinHeight = 175;
        return;
      }

      heightToSave = this.Height;
      gridUnitSize = r11.ActualHeight;
    }

    /// <summary> Click event handler for file browse button </summary>
    private void OnSetInput(object sender, EventArgs e) {
      fbd.Description = "Select intput location...";
      fbd.ShowNewFolderButton = false;
      if(Directory.Exists(_InputPath))
        fbd.SelectedPath = _InputPath;

      if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        if(InputPath == fbd.SelectedPath)
          return;

        InputPath = fbd.SelectedPath;
        ftxProcessor.InputPath = _InputPath;
        string strMsg = "Input location: " + _InputPath;
        txtInput.ToolTip = btnInput.ToolTip = strMsg;
        txtInput.Foreground = System.Windows.Media.Brushes.Black;
        if(!settingINI.SetValue("Input", _InputPath, "Locations"))
          DisplayActivity("Failed to save input location into INI file, check permissions.", ActivityType.ACTWARNING);

        DisplayActivity(strMsg, ActivityType.ACTOTHER);
        if(_InputPath == _OutputPath)
          DisplayActivity("Input and output paths are same.", ActivityType.ACTWARNING);
      }
    }

    /// <summary> Click event handler for file browse button </summary>
    private void OnSetOutput(object sender, EventArgs e) {
      fbd.Description = "Select output location...";
      fbd.ShowNewFolderButton = true;
      if(Directory.Exists(_OutputPath))
        fbd.SelectedPath = _OutputPath;

      if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        if(OutputPath == fbd.SelectedPath)
          return;

        OutputPath = fbd.SelectedPath;
        ftxProcessor.OutputPath = _OutputPath;
        string strMsg = "Output location: " + _OutputPath;
        txtOutput.ToolTip = btnOutput.ToolTip = strMsg;
        txtOutput.Foreground = System.Windows.Media.Brushes.Black;
        if(!settingINI.SetValue("Output", _OutputPath, "Locations"))
          DisplayActivity("Failed to save output location into INI file, check permissions.", ActivityType.ACTWARNING);

        DisplayActivity(strMsg, ActivityType.ACTOTHER);
        if(_InputPath == _OutputPath)
          DisplayActivity("Output and input paths are same.", ActivityType.ACTWARNING);
      }
    }

    /// <summary> Checked and unchecked event handler for "Add Footer" checkbox </summary> 
    private void OnFooterSelection(object sender, EventArgs e) {
      _AddFooter = cmbLanguage.IsEnabled = cbFooter.IsChecked.Value;
      if(_AddFooter) {
        string strFooterLang = cmbLanguage.SelectionBoxItem.ToString();
        DisplayActivity("Footer option enabled.", ActivityType.ACTOTHER);
        DisplayActivity("Footer language selected as " + strFooterLang + ".", ActivityType.ACTOTHER);
        cbFooter.ToolTip = "Footer option enabled.";
        cmbLanguage.ToolTip = "Selected footer language is " + strFooterLang + ".";
      } else {
        DisplayActivity("Footer option disabled.", ActivityType.ACTOTHER);
        cbFooter.ToolTip = "Footer option disabled.";
      }
    }

    /// <summary> DropDownClosed event handler for language combo, displays selected launguage </summary>
    private void OnFooterLanguageChange(object sender, EventArgs e) {
      if(_AddFooter) {
        string strFooterLang = cmbLanguage.SelectionBoxItem.ToString();
        DisplayActivity("Footer language selected as " + strFooterLang + ".", ActivityType.ACTOTHER);
        _LangIndex = cmbLanguage.SelectedIndex;
      }
    }

    /// <summary> CleanOutput checkbox checked and unchecked event handler </summary>
    private void OnCleanOutput(object sender, EventArgs e) {
      int valueToSave = (_CleanOutput) ? 1 : 0;
      string[] cleanStatus = { "disabled.", "enabled." };
      cbClean.ToolTip = "Clean existing output " + cleanStatus[valueToSave];
      DisplayActivity(cbClean.ToolTip.ToString(), ActivityType.ACTOTHER);
      if(!settingINI.SetValue("CleanOutput", valueToSave, "Misc"))
        DisplayActivity("Failed to save setting into INI file, check permissions.", ActivityType.ACTWARNING);
    }

    /// <summary> Click event hander for Undo button, adds last removed pattern </summary>
    private void OnUndo(object sender, EventArgs e) {
      Pattern patternToInsert = removedPatterns.Last();
      patternToInsert.ID = (_Patterns.Count + 1).ToString();
      if(!_Patterns.Any(x => x.Name == patternToInsert.Name)) {
        Patterns.Add(patternToInsert);
        SortPatternList();
        lbPatterns.ToolTip = "Total " + patternToInsert.ID + " patterns found.";
        DisplayActivity(string.Format("Pattern {0} rolled back.", patternToInsert.Name), ActivityType.ACTOTHER);
        if(!settingINI.SetValue(patternToInsert.ID, patternToInsert.Name, "Patterns"))
          DisplayActivity("Failed to add new pattern into INI file, check permissions.", ActivityType.ACTWARNING);
      } else
        DisplayActivity(string.Format("Pattern {0} already exists!", patternToInsert.Name), ActivityType.ACTWARNING);

      removedPatterns.Remove(patternToInsert);
      if(removedPatterns.Count == 0)
        btnUndo.Visibility = Visibility.Hidden;
    }

    /// <summary> Click event handler for remove pattern button, removes selected pattern. 
    ///             Last available pattern removed if not selected any. </summary>
    private void OnRemovePattern(object sender, EventArgs e) {
      if(_Patterns.Count == 0)
        return;

      int selectedPatternID = lbPatterns.SelectedIndex;
      if(selectedPatternID == -1)
        selectedPatternID = _Patterns.Count - 1;

      string patternRemoved = _Patterns[selectedPatternID].Name;
      removedPatterns.Add(_Patterns[selectedPatternID]);
      btnUndo.Visibility = Visibility.Visible;
      _Patterns.RemoveAt(selectedPatternID);
      SortPatternList();
      UpdatePatternIDs();
      DisplayActivity("Pattern " + patternRemoved + " removed.", ActivityType.ACTOTHER);
      if(!settingINI.DeleteKeyAt(selectedPatternID, "Patterns"))
        DisplayActivity("Failed to remove pattern from INI file, check permissions.", ActivityType.ACTERROR);

      lbPatterns.ToolTip = "Total " + _Patterns.Count + " patterns found.";
      if(_Patterns.Count == 0) {
        btnRemovePattern.IsEnabled = false;
        lbPatterns.ToolTip = "No pattern found, only defaults([? omitted], [? omitted.]) will be processed.";
      }
    }

    /// <summary> Click event handler for "Start Processing" button, reads input files,
    ///             processes them based on pattern and writes output files </summary>
    private void OnProcess(object sender, EventArgs e) {
      try {
        if(ftxProcThread != null && ftxProcThread.IsAlive) {
          AbortProcessing();
          return;
        }

        _LangIndex = cmbLanguage.SelectedIndex;
        ftxProcessor.OutputPath = _OutputPath;
        if(!Directory.Exists(_InputPath) || !Directory.Exists(_OutputPath)) {
          DisplayActivity("Input and output location isn't set yet, run aborted.", ActivityType.ACTERROR);
          return;
        }

        DirectoryInfo inputDir = new DirectoryInfo(_InputPath);
        ftxProcessor.InputPath = _InputPath;
        processProgress.Value = 0;
        FileInfo[] files = inputDir.GetFiles("F*.M*");
        if(files.Length == 0) {
          DisplayActivity("Couldn't find any full text file, run aborted.", ActivityType.ACTERROR);
          return;
        }

        ftxProcessor.InputFiles.Clear();
        _TotalInputSize = 0;
        progressValue.Text = "0%";
        foreach(FileInfo file in files) {
          ftxProcessor.InputFiles.Add(file.Name);
          _TotalInputSize += file.Length;
        }

        if(cbClean.IsChecked == true) {
          DirectoryInfo outputDir = new DirectoryInfo(_OutputPath);
          FileInfo[] outFiles = outputDir.GetFiles("FM*.M*");
          foreach(FileInfo file in outFiles) {
            try {
              file.Delete();
            } catch(Exception ex) {
              DisplayActivity(ex.Message, ActivityType.ACTCRITICAL);
              continue;
            }
          }
        }

        SetProcessingMode(true);
        /*ThreadInvoker.Instance.RunByNewThread(() => {
          ftxProcessor.WriteOutputFiles();
        });*/
        ftxProcThread = new Thread(new ThreadStart(ftxProcessor.WriteOutputFiles)) {
          Priority = ThreadPriority.AboveNormal
        };
        ftxProcThread.Start();
        ftxProcThread.Join(100);
      } catch { SetProcessingMode(false); }
    }

    /// <summary> Expanded event handler for expanders, resizes window according </summary>
    private void OnExpanded(object sender, EventArgs e) {
      if(!IsLoaded)
        return;

      sizeChangedForExpander = true;
      Expander exp = (Expander)sender;
      if(sender == gbInput)
        r1.Height = r2.Height = r3.Height = r4.Height = r5.Height = r11.Height;
      else
        r6.Height = r7.Height = r8.Height = r9.Height = r10.Height = r11.Height;

      this.Height += ((5 * gridUnitSize) - gbPatterns.MinHeight);
    }

    /// <summary> Colapsed event handler for expanders, resizes window according  </summary>
    private void OnCollapsed(object sender, EventArgs e) {
      sizeChangedForExpander = true;
      Expander exp = (Expander)sender;
      if(sender == gbInput)
        r1.Height = r2.Height = r3.Height = r4.Height = r5.Height = GridLength.Auto;
      else
        r6.Height = r7.Height = r8.Height = r9.Height = r10.Height = GridLength.Auto;

      this.Height -= ((5 * gridUnitSize) - gbPatterns.MinHeight);
    }

    /// <summary> Click event handler for menu button </summary>
    private void OnShowMenu(object sender, EventArgs e) => AppMenu.IsOpen = true;

    /// <summary> Executes relevant command </summary>
    private void OnFNKeyPress(object sender, KeyEventArgs e) {
      switch(e.Key) {
        case Key.F1:
          ShowManual(null, null);
          break;
        case Key.F2:
          OnAddPattern(null, null);
          break;
        case Key.F3:
          OpenSettingsINI(null, null);
          break;
        case Key.F4:
          ReloadSettingsINI(null, null);
          break;
        case Key.F5:
          OnProcess(null, null);
          break;
        case Key.F6:
          OpenLocation(miOIL, null);
          break;
        case Key.F7:
          OpenLocation(miOOL, null);
          break;
        case Key.F8:
          OpenLocation(miOLL, null);
          break;
        case Key.F9:
          ShowAbout(null, null);
          break;
      }
    }

    /// <summary> Click event handler for add pattern button, brings add pattern window </summary>
    private void OnAddPattern(object sender, EventArgs e) {
      setWnd.Owner = this;
      setWnd.LogLocationExists = Directory.Exists(setWnd.LogPath);
      UpdateChildStartupPosition(setWnd);
      setWnd.ShowDialog();
    }

    /// <summary> Handler for open log, input and output location menu </summary>
    /// <param name="location"> log, input or output location </param>
    private void OpenLocation(object sender, EventArgs e) {
      string location = "";
      MenuItem menu = (MenuItem)sender;
      if(menu == miOIL)
        location = _InputPath;
      else if(menu == miOOL)
        location = _OutputPath;
      else
        location = setWnd.LogPath;

      if(location.Length == 0 || !Directory.Exists(location)) {
        MessageBox.Show(this, "Location is either not set or invalid.", "Invalid location!", MessageBoxButton.OK, MessageBoxImage.Error);
        return;
      }

      Process.Start(location);
    }

    /// <summary> Handler for open settings menu, opens INI file </summary>
    private void OpenSettingsINI(object sender, EventArgs e) {
      if(!File.Exists("FTXFixer.INI")) {
        MessageBox.Show(this, "Settings file doesn't exist.", "Settings not found!", 
          MessageBoxButton.OK, MessageBoxImage.Warning);

        return;
      }

      try {
        Process.Start(@"FTXFixer.INI");
      } catch {
        MessageBox.Show(this, "Settings file failed to open.",
          "Open settings error!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary> Reloads settings from INI file </summary>
    private void ReloadSettingsINI(object sender, EventArgs e) => LoadSettings();

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

    /// <summary> Handler for open article/application log </summary>
    private void OpenLogFile(object sender, EventArgs e) {
      MenuItem menu = (MenuItem)sender;
      string[] logTypes = { "Application", "Article" };
      string[] logFiles = { appLogFIle, ftxProcessor.ArticleLog };
      int logIndex = (menu == miAPPL) ? 0 : 1;
      string logFile = logFiles[logIndex];
      if(!File.Exists(logFile)) {
        MessageBox.Show(this, string.Format("{0} log {1} doesn't exist.", logTypes[logIndex], logFile), 
          "Log not found!", MessageBoxButton.OK, MessageBoxImage.Warning);

        return;
      }

      try {
        Process.Start(logFile);
      } catch {
        MessageBox.Show(this, string.Format("{0} log {1} failed to open.", logTypes[logIndex], logFile),
          "Log open error!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    /// <summary> Shows application manual </summary>
    private void ShowManual(object sender, EventArgs e) {
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
    private void ShowAbout(object sender, EventArgs e) {
      if(aboutWnd == null) {
        aboutWnd = new About {
          Owner = this,
          AppImage = this.Icon,
          Copyright = headerborder.ToolTip.ToString(),
          BuildDate = GetAssemblyInfo(AssemblyType.BUILTDATE),
          BuildVersion = GetAssemblyInfo(AssemblyType.VERSION)
        };
      }

      UpdateChildStartupPosition(aboutWnd);
      aboutWnd.ShowDialog();
    }
  }
}
