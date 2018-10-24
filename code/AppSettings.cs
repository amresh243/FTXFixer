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
 * AppSettings.cs - Handles loading and saving of INI file.
 * 
 */


using FTXFixer.code;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace FTXFixer {
  partial class FTXFixerWnd {
    /// <summary> To access app setting outside </summary>
    public INIProcessor AppSettings => settingINI;

    /// <summary> Reads settings from INI file at exe location.
    ///             EXE location should have write access to insure
    ///             settings are saved successfully. </summary>
    private void LoadSettings(bool openLogging = false) {
      settingINI = new INIProcessor("FTXFixer.INI", this);
      if(!settingINI.OK)
        return;

      ReadLocations(openLogging);
      ReadSizeAndMisc();
      ReadPatterns();
    }

    /// <summary> Reads location section from INI file </summary>
    /// <param name="openLogging"> Logging file opened if true (once in app life time) </param>
    private void ReadLocations(bool openLogging) {
      string inputPath = (string)settingINI.GetValue("Input", "Locations");
      string outputPath = (string)settingINI.GetValue("Output", "Locations");
      string logPath = (string)settingINI.GetValue("Log", "Locations");
      string logMsg = "Warning! Log location is not available";
      if(Directory.Exists(logPath)) {
        setWnd.LogPath = logPath;
        logMsg = "Log location is " + logPath;
      } else
        setWnd.LoggingArticle = setWnd.LoggingInput = setWnd.LoggingOutput = false;

      if(openLogging) {
        OpenAppLogging(AppLoggingType.CREATE);
        DisplayActivity(logMsg, ActivityType.ACTOTHER);
      }

      if(Directory.Exists(inputPath)) {
        txtInput.Foreground = Brushes.Black;
        InputPath = inputPath;
        txtInput.ToolTip = "Input location: " + inputPath;
        DisplayActivity("Input location is " + inputPath, ActivityType.ACTOTHER);
      } else
        DisplayActivity("Input location is not available", ActivityType.ACTWARNING);

      if(Directory.Exists(outputPath)) {
        txtOutput.Foreground = Brushes.Black;
        OutputPath = outputPath;
        txtOutput.ToolTip = "Output location: " + outputPath;
        DisplayActivity("Output location is " + outputPath, ActivityType.ACTOTHER);
      } else
        DisplayActivity("Output location is not available.", ActivityType.ACTWARNING);

      if(InputPath == OutputPath) {
        DisplayActivity("Input and output locations are same.", ActivityType.ACTWARNING);
        cbClean.IsChecked = false;
      }
    }

    /// <summary> Reads Size and Misc section from INI file </summary>
    private void ReadSizeAndMisc() {
      double w = 620;
      double h = 440;
      int clean = 0, lg = 0, ih = 0, oh = 0, fi = 0;
      try {
        double.TryParse(settingINI.GetValue("Width", "Size").ToString(), out w);
      } catch { w = 620; }
      try {
        double.TryParse(settingINI.GetValue("Height", "Size").ToString(), out h);
      } catch { h = 440; }
      try {
        int.TryParse(settingINI.GetValue("CleanOutput", "Misc").ToString(), out clean);
      } catch { clean = 0; }
      try {
        int.TryParse(settingINI.GetValue("Logging", "Misc").ToString(), out lg);
      } catch { lg = 0; }
      try {
        int.TryParse(settingINI.GetValue("InputHistory", "Misc").ToString(), out ih);
      } catch { ih = 0; }
      try {
        int.TryParse(settingINI.GetValue("OutputHistory", "Misc").ToString(), out oh);
      } catch { oh = 0; }
      try {
        int.TryParse(settingINI.GetValue("Footer", "Misc").ToString(), out fi);
      } catch { fi = 0; }
      if(w <= 0)
        w = 620;

      if(h <= 0)
        h = 440;

      if(w >= this.MinWidth)
        this.Width = w;

      if(h >= this.MinHeight)
        this.Height = h;

      if(fi >= 0)
        _LangIndex = fi;

      CleanOutput = (clean == 0) ? false : true;
      string strCleanStatus = (CleanOutput) ? "enabled" : "disabled";
      DisplayActivity("Clean existing output is " + strCleanStatus + ".", ActivityType.ACTOTHER);
      if(Directory.Exists(setWnd.LogPath)) {
        setWnd.LoggingArticle = (lg == 0) ? false : true;
        setWnd.LoggingInput = (ih == 0) ? false : true;
        setWnd.LoggingOutput = (oh == 0) ? false : true;
        setWnd.LogLocationExists = true;
      }
    }

    /// <summary> Reads Patterns section from INI file </summary>
    private void ReadPatterns() {
      List<string> pats = settingINI.GetKeyList("Patterns");
      if(pats != null && pats.Count != 0) {
        int defaultPatterns = 0;
        _Patterns.Clear();
        foreach(string str in pats) {
          string newPattern = settingINI.GetValue(str, "Patterns").ToString().Trim().ToLower();
          int lenPattern = newPattern.Length;
          if(lenPattern == 0)
            continue;

          if(IsDefaultPattern(newPattern)) {
            defaultPatterns++;
            continue;
          }

          _Patterns.Add(new Pattern { Name = newPattern });
        }

        SortPatternList();
        btnRemovePattern.IsEnabled = (_Patterns.Count != 0);
        lbPatterns.ToolTip = "Total " + _Patterns.Count + " patterns found.";
        UpdatePatternIDs();
        string strMsg = "Settings successfully loaded with " + _Patterns.Count + " patterns";
        strMsg += (defaultPatterns != 0) ? string.Format(", {0} default patterns skipped.", defaultPatterns) : ".";
        DisplayActivity(strMsg, ActivityType.ACTOTHER);
      } else {
        lbPatterns.ToolTip = "No pattern found, only defaults([?? omitted], [?? omitted.]) will be processed.";
        DisplayActivity(lbPatterns.ToolTip.ToString(), ActivityType.ACTWARNING);
      }
    }

    /// <summary> Saves settings to INI file before quiting </summary>
    private void SaveSettings() {
      if(!settingINI.OK)
        if(!settingINI.CreateNew()) {
          MessageBox.Show(this, "Settings failed to save, check write permission.", "Write Permission Error!",
                                           MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

      settingINI.SetValue("Height", heightToSave, "Size");
      settingINI.SetValue("Width", this.Width, "Size");
      if(Directory.Exists(_InputPath))
        settingINI.SetValue("Input", _InputPath, "Locations");

      if(Directory.Exists(_OutputPath))
        settingINI.SetValue("Output", _OutputPath, "Locations");

      if(Directory.Exists(setWnd.LogPath)) {
        settingINI.SetValue("Log", setWnd.LogPath, "Locations");
        settingINI.SetValue("Logging", (setWnd.LoggingArticle == true) ? 1 : 0, "Misc");
        settingINI.SetValue("InputHistory", (setWnd.LoggingInput == true) ? 1 : 0, "Misc");
        settingINI.SetValue("OutputHistory", (setWnd.LoggingOutput == true) ? 1 : 0, "Misc");
      } else {
        settingINI.SetValue("Logging", 0, "Misc");
        settingINI.SetValue("InputHistory", 0, "Misc");
        settingINI.SetValue("OutputHistory", 0, "Misc");
      }

      Section patternSection = settingINI.GetSection("Patterns");
      if(patternSection != null)
        patternSection.keys.Clear();

      if(_Patterns.Count != 0) {
        int patternID = 1;
        foreach(Pattern pattern in _Patterns) {
          settingINI.SetValue(patternID.ToString(), pattern.Name, "Patterns");
          patternID++;
        }
      }

      settingINI.SetValue("CleanOutput", (_CleanOutput) ? 1 : 0, "Misc");
      settingINI.SetValue("Footer", _LangIndex, "Misc");
    }

    private INIProcessor settingINI = null;
  }
}
