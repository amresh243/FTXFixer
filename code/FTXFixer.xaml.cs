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
 * FTXFixer.xaml.cs - Holds UI design implementation.
 * 
 */


using FTXFixer.code;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FTXFixer {
  public enum ActivityType { ACTPROC, ACTERROR, ACTCRITICAL, ACTWARNING, ACTOTHER };
  public enum AppLoggingType { CREATE = 0, OPEN, WRAP, CLOSE };
  public enum AssemblyType { VERSION = 0, COPYRIGHT, BUILTDATE, COMPANY, PRODUCT }

  /// <summary> Interaction logic for FTXFixerWnd.xaml </summary>
  public partial class FTXFixerWnd : Window, INotifyPropertyChanged {
    /// <summary> Default constructor </summary>
    public FTXFixerWnd() {
      _Patterns = new ObservableCollection<Pattern>();
      this.DataContext = this;
      InitializeComponent();
      Uri iconUri = new Uri("pack://application:,,,/textEdit.ico", UriKind.RelativeOrAbsolute);
      this.Icon = BitmapFrame.Create(iconUri);
      ThreadInvoker.Instance.InitDispatcher();
      fbd = new System.Windows.Forms.FolderBrowserDialog();
    }

    /// <summary> Property to access input path outside </summary>
    public string InputPath {
      get { return _InputPath; }
      set {
        if(value != _InputPath) {
          _InputPath = value;
          RaisePropertyChanged("InputPath");
        }
      }
    }

    /// <summary> Property to access output path outside </summary>
    public string OutputPath {
      get { return _OutputPath; }
      set {
        if(value != _OutputPath) {
          _OutputPath = value;
          RaisePropertyChanged("OutputPath");
        }
      }
    }

    /// <summary> Returns total input file size </summary>
    public double TotalInputSize => _TotalInputSize;

    /// <summary> Progress meter display text </summary>
    public string ProgressText {
      get { return _ProgressText; }
      set {
        if(value != _ProgressText) {
          _ProgressText = value;
          RaisePropertyChanged("ProgressText");
        }
      }
    }

    /// <summary> Progress meter display value </summary>
    public double ProgressValue {
      get { return _ProgressValue; }
      set {
        if(value != _ProgressValue) {
          _ProgressValue = value;
          RaisePropertyChanged("ProgressValue");
        }
      }
    }

    /// <summary> Returns progressbar object </summary>
    public ProgressBar Progress => processProgress;

    /// <summary> Property to access current selected language index </summary>
    public int LangIndex => _LangIndex;

    /// <summary> Property to check if footer to be added in article data </summary>
    public bool AddFooter => _AddFooter;

    /// <summary> Property to check if processing in progress from outside </summary>
    public bool Processing {
      get { return _Processing; }
      set {
        if(value != _Processing)
          _Processing = value;
      }
    }

    /// <summary> Property to check value of clean output </summary>
    public bool CleanOutput {
      get { return _CleanOutput; }
      set {
        if(value != _CleanOutput) {
          _CleanOutput = value;
          RaisePropertyChanged("CleanOutput");
        }
      }
    }

    /// <summary> Property to access list of patterns from outside </summary>
    public ObservableCollection<Pattern> Patterns {
      get { return _Patterns; }
      set {
        if(value != _Patterns) {
          _Patterns = value;
          RaisePropertyChanged("Patterns");
        }
      }
    }

    /// <summary> Property to access sorted list of patterns </summary>
    public List<Pattern> SortedPatterns {
      get { return sortedPatterns; }
      set {
        if(value != sortedPatterns)
          sortedPatterns = value;
      }
    }

    /// <summary> Removes all patterns from patter list </summary>
    public void ClearPatterns() {
      lbPatterns.ToolTip = "No pattern found, only defaults([? omitted], [? omitted.]) will be processed.";
      for(int i = _Patterns.Count - 1; i >= 0; i--)
        removedPatterns.Add(_Patterns[i]);

      if(removedPatterns.Count > 0)
        btnUndo.Visibility = Visibility.Visible;

      _Patterns.Clear();
      sortedPatterns.Clear();
      DisplayActivity("All patterns removed from list.", ActivityType.ACTOTHER);
      if(!settingINI.DeleteAllKeys("Patterns"))
        DisplayActivity("Failed to remove patterns from INI file, check permissions.", ActivityType.ACTERROR);
    }

    /// <summary> Check if new pattern is default pattern type </summary>
    /// <param name="newPattern"> pattern to be validated </param>
    /// <returns> true if default else false </returns>
    private bool IsDefaultPattern(string newPattern) {
      if(newPattern[0] != '[' || newPattern[newPattern.Length - 1] != ']')
        return false;

      string[] patternParts = newPattern.Split(' ');
      string lastPart = patternParts[patternParts.Length - 1];
      int openBraceIndex = FTXProcessor.GetFinalOpenBraceIndex(newPattern);
      if(openBraceIndex == 0 && (lastPart == "omitted]" || lastPart == "omitted.]"))
        return true;

      return false;
    }

    /// <summary> comparer function to sort pattern list by length in decending order </summary>
    /// <param name="p1"> first pattern object </param>
    /// <param name="p2"> second pattern object </param>
    /// <returns> 1 or -1 based on comparision </returns>
    private static int ComparePatternByLength(Pattern p1, Pattern p2) {
      if(p1.Length > p2.Length)
        return -1;
      else if(p2.Length > p1.Length)
        return 1;

      return 0;
    }

    /// <summary> Sorts pattern list after every alteration in pattern list </summary>
    private void SortPatternList() {
      if(sortedPatterns == null)
        sortedPatterns = new List<Pattern>();

      sortedPatterns.Clear();
      sortedPatterns.AddRange(_Patterns);
      sortedPatterns.Sort(ComparePatternByLength);
    }

    /// <summary> Refreshes listbox entries with formatted pattern (aka with count) </summary>
    private void UpdatePatternIDs() {
      int patternCount = _Patterns.Count;
      int lenCount = patternCount.ToString().Length;
      for(int i = 0; i < patternCount; i++) {
        Pattern patternItem = Patterns[i];
        patternItem.ID = (i + 1).ToString();
        int lenZero = lenCount - patternItem.ID.Length;
        patternItem.ID = string.Concat(Enumerable.Repeat("0", lenZero)) + patternItem.ID;
      }
    }

    /// <summary> Validates and adds given pattern into pattern list </summary>
    /// <param name="newPattern"> name of pattern to be added </param>
    public void AddNewPattern(string newPattern) {
      newPattern = newPattern.Trim().ToLower();
      if(newPattern.Length == 0 || _Patterns.Any(x => x.Name == newPattern)) {
        DisplayActivity("Adding failed, invalid or duplicate pattern.", ActivityType.ACTWARNING);
        return;
      }

      if(IsDefaultPattern(newPattern)) {
        DisplayActivity("Adding failed, Patterns [?? omitted] and [?? omitted.] are handled by default.",
          ActivityType.ACTWARNING);
        return;
      }

      string patternID = (_Patterns.Count + 1).ToString();
      _Patterns.Add(new Pattern { ID = patternID, Name = newPattern });
      SortPatternList();
      lbPatterns.ToolTip = string.Format("Total {0} patterns found.", patternID);
      btnRemovePattern.IsEnabled = true;
      DisplayActivity("Pattern " + newPattern + " added.", ActivityType.ACTOTHER);
      if(!settingINI.SetValue(patternID, newPattern, "Patterns"))
        DisplayActivity("Failed to add new pattern into INI file, check permissions.", ActivityType.ACTWARNING);
    }

    /// <summary> Changes state of controls during/after processing </summary>
    /// <param name="iEnalbe"> true or false, mostly enabled if true else disabled </param>
    public void ChangeControlsState(bool iEnalbe) {
      btnInput.IsEnabled = btnOutput.IsEnabled = iEnalbe;
      cbClean.IsEnabled = cbFooter.IsEnabled = iEnalbe;
      btnAddPattern.IsEnabled = btnUndo.IsEnabled = iEnalbe;
      miLPS.IsEnabled = miRS.IsEnabled = iEnalbe;
      if(iEnalbe == true) {
        miSP.Header = "Start Processing";
        btnProcess.Content = "Start\nProcessing";
        miSP.Foreground = Brushes.OliveDrab;
        btnProcess.Background = Brushes.OliveDrab;
      } else {
        miSP.Header = "Abort Processing";
        btnProcess.Content = "Abort\nProcessing";
        miSP.Foreground = Brushes.Tomato;
        btnProcess.Background = Brushes.Tomato;
      }

      if(_Patterns.Count != 0)
        btnRemovePattern.IsEnabled = iEnalbe;

      if(cbFooter.IsChecked == true)
        cmbLanguage.IsEnabled = iEnalbe;
    }

    /// <summary> Aborts ongoing processing </summary>
    private void AbortProcessing() {
      if(ftxProcThread != null && ftxProcThread.IsAlive) {
        SetProcessingMode(false);
        ftxProcThread.Abort();
        ftxProcessor.ReleaseLockedResources();
        DisplayActivity("Process aborted by user.", ActivityType.ACTWARNING);
      }
    }

    /// <summary> Sets application in processing mode </summary>
    /// <param name="isStart"> if true, put app into processing mode else into idle mode </param>
    public void SetProcessingMode(bool isStart) {
      if(isStart) {
        btnProcess.ToolTip = "Click to abort processing.";
        this.Cursor = Cursors.Wait;
      } else {
        this.Cursor = Cursors.Arrow;
        btnProcess.ToolTip = "Click to start processing input location.";
        OpenAppLogging(AppLoggingType.WRAP);
      }

      ChangeControlsState(!isStart);
      _Processing = isStart;
    }

    /// <summary> Dsplays message in status bar </summary>
    /// <param name="strMessage"> Message to display </param>
    /// <param name="aType"> message type decide message color </param>
    public void DisplayActivity(string strMessage, ActivityType aType) {
      if(setWnd == null)
        return;

      string strMsgType = "";
      OpenAppLogging(AppLoggingType.OPEN);
      switch(aType) {
        case ActivityType.ACTCRITICAL:
          string strMsg = strMessage.Trim().ToLower();
          strMessage = "Critical Error! " + strMessage;
          if(!strMsg.Contains("thread") && !strMessage.Contains("aborted")) {
            MessageBox.Show(this, strMessage, "Exception Occured!", MessageBoxButton.OK, MessageBoxImage.Error);
            if(loggingAvailable)
              appLog.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToString("HH:MM:SS"), strMessage));
          }

          return;
        case ActivityType.ACTERROR:
          strMsgType = "Error! ";
          tssActivity.Foreground = Brushes.Red;
          break;
        case ActivityType.ACTWARNING:
          strMsgType = "Warning! ";
          tssActivity.Foreground = Brushes.Yellow;
          break;
        case ActivityType.ACTPROC:
          tssActivity.Foreground = Brushes.Blue;
          break;
        case ActivityType.ACTOTHER:
          tssActivity.Foreground = Brushes.Black;
          break;
      }

      tssActivity.Content = strMsgType + strMessage;
      if(loggingAvailable)
        appLog.WriteLine(string.Format("{0} - {1}", DateTime.Now.ToString("HH:mm:ss"), strMsgType + strMessage));

      OpenAppLogging(AppLoggingType.CLOSE);
    }

    /// <summary> Opens application logging </summary>
    public void OpenAppLogging(AppLoggingType alType) {
      try {
        switch(alType) {
          case AppLoggingType.CREATE:
            appLogFIle = (Directory.Exists(setWnd.LogPath)) ? setWnd.LogPath + "\\FTXFIXER.LOG" : "FTXFIXER.LOG";
            appLog = new StreamWriter(appLogFIle);
            if(appLog != null && appLog.BaseStream != null) {
              string strLogHeader = string.Format("FTXFixer.exe application logging on {0}", DateTime.Now.ToString("dd-MM-yyyy"));
              appLog.WriteLine(strLogHeader);
              appLog.WriteLine(string.Concat(Enumerable.Repeat("-", strLogHeader.Length)));
              appLog.WriteLine();
              appLog.Close();
            }

            break;
          case AppLoggingType.OPEN:
            appLog = new StreamWriter(appLogFIle, true);
            loggingAvailable = true;
            break;
          case AppLoggingType.WRAP:
            appLog = new StreamWriter(appLogFIle, true);
            appLog.WriteLine();
            appLog.Close();
            break;
          case AppLoggingType.CLOSE:
            appLog.Close();
            loggingAvailable = false;
            break;
        }

      } catch { loggingAvailable = false; }
    }

    /// <summary> Updates child co-ordinates to center </summary>
    /// <param name="child"> Child for which co-ordinates to be updated </param>
    private void UpdateChildStartupPosition(Window child) {
      child.Left = this.Left + (this.Width - child.Width) / 2;
      child.Top = this.Top + (this.Height - child.Height) / 2;
    }

    /// <summary> Binding notification handler </summary>
    /// <param name="propertyName"> Name of property against which change triggered </param>
    protected void RaisePropertyChanged(string propertyName) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler PropertyChanged;
    private string appLogFIle = "";
    private string _ProgressText = "0%";
    private Thread ftxProcThread = null;
    private List<Pattern> sortedPatterns = null;
    private System.Windows.Forms.FolderBrowserDialog fbd = null;
    private FTXProcessor ftxProcessor = null;
    private SettingsWnd setWnd = null;
    private About aboutWnd = null;
    private ObservableCollection<Pattern> _Patterns = null;
    private List<Pattern> removedPatterns = null;
    private StreamWriter appLog = null;
    private string _InputPath;
    private string _OutputPath;
    private double _TotalInputSize = 0;
    private double _ProgressValue = 0;
    private bool _Processing = false;
    private bool _AddFooter = false;
    private bool _CleanOutput = false;
    private bool sizeChangedForExpander = false;
    private bool loggingAvailable = false;
    private double gridUnitSize = 0;
    private double heightToSave = 0;
    private int _LangIndex = 0;
  }
}
