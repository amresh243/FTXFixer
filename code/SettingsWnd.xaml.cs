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
 * SettingsWnd.xaml.cs - Pattern and settings window UI implementation.
 * 
 */


using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FTXFixer.code {
  /// <summary> Allows user to add new pattern into pattern list </summary>
  public partial class SettingsWnd : Window, INotifyPropertyChanged {
    /// <summary> Default constructor </summary>
    public SettingsWnd(FTXFixerWnd owner) {
      ftxFixerWnd = owner;
      this.DataContext = this;
      InitializeComponent();
      fbd = new System.Windows.Forms.FolderBrowserDialog();
    }

    /// <summary> Property for existing pattern </summary>
    public string PText {
      get { return _PText; }
      set {
        if(value != _PText) {
          _PText = value;
          RaisePropertyChanged("PText");
        }
      }
    }

    /// <summary> Property for accessing log path location </summary>
    public string LogPath {
      get { return _LogPath; }
      set {
        if(value != _LogPath) {
          _LogPath = value;
          RaisePropertyChanged("LogPath");
        }
      }
    }

    /// <summary> Property to check for logging availability </summary>
    public bool LoggingArticle {
      get { return _LoggingArticle; }
      set {
        if(value != _LoggingArticle) {
          _LoggingArticle = value;
          RaisePropertyChanged("LoggingArticle");
        }
      }
    }

    /// <summary> Property to access input history file </summary>
    public bool LoggingInput {
      get { return _LoggingInput; }
      set {
        if(value != _LoggingInput) {
          _LoggingInput = value;
          RaisePropertyChanged("LoggingInput");
        }
      }
    }

    /// <summary> Property to access output history file </summary>
    public bool LoggingOutput {
      get { return _LoggingOutput; }
      set {
        if(value != _LoggingOutput) {
          _LoggingOutput = value;
          RaisePropertyChanged("LoggingOutput");
        }
      }
    }

    /// <summary> Property to set logging options enabled/disabled </summary>
    public bool LogLocationExists {
      get { return _LogLocationExists; }
      set {
        if(value != _LogLocationExists) {
          _LogLocationExists = value;
          RaisePropertyChanged("LogLocationExists");
        }
      }
    }

    /// <summary> data binding handler </summary>
    /// <param name="propertyName"> property name to trigger changes </param>
    protected void RaisePropertyChanged(string propertyName) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary> Loaded event handler </summary>
    private void OnLoadWnd(object sender, RoutedEventArgs e) {
      btnClearAll.ToolTip = "Warning! This will clear all patterns from pattern list.";
      btnAdd.ToolTip = "Click to add pattern into pattern list.";
      btnClose.ToolTip = "Press Escape or click to close.";
      txtPattern.ToolTip = "Enter new pattern here.";
      txtLogPath.ToolTip = "Click SET button to set logging location.";
      btnLogPath.ToolTip = "Click to set logging location.";
      fbd.Description = "Set logging location...";
      if(Directory.Exists(_LogPath)) {
        txtLogPath.ToolTip = "Log location: " + _LogPath + ".";
        txtLogPath.Foreground = Brushes.Black;
      } else {
        LogPath = "<Click SET button to set log location>";
        txtLogPath.Foreground = Brushes.Gray;
        LoggingArticle = LoggingOutput = LoggingOutput = false;
      }
    }

    /// <summary> Activate event handler, sets focus to pattern box </summary>
    private void OnWndActivate(object sender, EventArgs e) => txtPattern.Focus();

    /// <summary> Click event handler for set log path button, sets log path on selection </summary>
    private void OnSetLogPath(object sender, EventArgs e) {
      if(Directory.Exists(_LogPath))
        fbd.SelectedPath = _LogPath;

      if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
        LogLocationExists = true;
        if(LogPath == fbd.SelectedPath)
          return;

        LogPath = fbd.SelectedPath;
        string strMsg = "Log location: " + _LogPath;
        txtLogPath.ToolTip = strMsg + ".";
        txtLogPath.Foreground = Brushes.Black;
        if(!ftxFixerWnd.AppSettings.SetValue("Log", _LogPath, "Locations"))
          ftxFixerWnd.DisplayActivity("Failed to save log location into INI file, check permissions.", ActivityType.ACTWARNING);

        ftxFixerWnd.DisplayActivity(strMsg, ActivityType.ACTOTHER);
        LoggingArticle = LoggingInput = LoggingOutput = true;
      }
    }

    /// <summary> Checked/Unchecked event handler for logging options checkboxes,
    ///             controls logging options such as, input, output and logs. </summary>
    private void OnLoggingOptions(object sender, RoutedEventArgs e) {
      string[] switchValue = { "Disabled", "Enabled" };
      cbLogging.ToolTip = string.Format("Logging {0}.", switchValue[(_LoggingArticle) ? 1 : 0]);
      cbInputHistory.ToolTip = string.Format("Input history {0}.", switchValue[(_LoggingInput) ? 1 : 0]);
      cbOutputHistory.ToolTip = string.Format("Output history {0}.", switchValue[(_LoggingOutput) ? 1 : 0]);
      if(!Directory.Exists(_LogPath)) {
        LogLocationExists = LoggingArticle = LoggingInput = LoggingOutput = false;
        ftxFixerWnd.DisplayActivity("Log location not set yet, insure to set log location.", ActivityType.ACTWARNING);
        return;
      } else {
        int loggingStatus = (_LoggingArticle == true) ? 1 : 0;
        int inputHistoryStatus = (_LoggingInput == true) ? 1 : 0;
        int outputHistoryStatus = (_LoggingOutput == true) ? 1 : 0;
        string strMsg = string.Format("Logging: {0}, Input History: {1}, Output History: {2}",
                              switchValue[loggingStatus], switchValue[inputHistoryStatus], switchValue[outputHistoryStatus]);
        ftxFixerWnd.DisplayActivity(strMsg, ActivityType.ACTOTHER);
        CheckBox cb = (CheckBox)sender;
        if(cb == cbLogging && !ftxFixerWnd.AppSettings.SetValue("Logging", loggingStatus, "Misc"))
          ftxFixerWnd.DisplayActivity("Failed to save setting into INI file, check permissions.", ActivityType.ACTWARNING);
        else if(cb == cbInputHistory && !ftxFixerWnd.AppSettings.SetValue("InputHistory", inputHistoryStatus, "Misc"))
          ftxFixerWnd.DisplayActivity("Failed to save setting into INI file, check permissions.", ActivityType.ACTWARNING);
        else
          if(!ftxFixerWnd.AppSettings.SetValue("OutputHistory", outputHistoryStatus, "Misc"))
            ftxFixerWnd.DisplayActivity("Failed to save setting into INI file, check permissions.", ActivityType.ACTWARNING);
      }
    }

    /// <summary> KeyDown event handler for textboxes (new and old property),
    ///             pressing enter key adds new pattern into list </summary>
    private void OnEnterKey(object sender, KeyEventArgs e) {
      if(e.Key == Key.Enter && txtPattern.Text.Length != 0) {
        newPattern = txtPattern.Text;
        ftxFixerWnd.AddNewPattern(newPattern);
      }
    }

    /// <summary> KeyDown event handler for pattern window,
    ///             presssing escape closes pattern windos </summary>
    private void OnEscapeKey(object sender, KeyEventArgs e) {
      if(e.Key == Key.Escape)
        this.Hide();
    }

    /// <summary> Removes all pattern from pattern list </summary>
    private void OnClearAllPattern(object sender, RoutedEventArgs e) {
      if(ftxFixerWnd.Patterns.Count == 0)
        return;

      ftxFixerWnd.DisplayActivity("This will clear all patterns from pattern list.", ActivityType.ACTWARNING);
      if(MessageBox.Show(this, "Are you sure to clear all patterns in list?", "Clear All Patterns?", 
                         MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        ftxFixerWnd.ClearPatterns();
      else
        ftxFixerWnd.DisplayActivity("No pattern removed.", ActivityType.ACTOTHER);
    }

    /// <summary> Click event handler for add pattern buttong </summary>
    private void OnAddPattern(object sender, RoutedEventArgs e) {
      if(PText.Length != 0 && ftxFixerWnd != null) {
        newPattern = PText;
        ftxFixerWnd.AddNewPattern(newPattern);
      }
    }

    /// <summary> Keypress event handler for pattern textbox </summary>
    private void DelPressed(object sender, KeyEventArgs e) {
      if(e.Key == Key.Back)
        DelKeyPressed = true;
      else
        DelKeyPressed = false;
    }

    /// <summary> Text change event handler for pattern textbox, autosuggestion </summary>
    private void SuggestPattern(object sender, TextChangedEventArgs e) {
      if(e.Changes == null || e.Changes.Count == 0)
        return;

      var change = e.Changes.FirstOrDefault();
      if(!InProg) {
        InProg = true;
        var culture = new CultureInfo(CultureInfo.CurrentCulture.Name);
        var source = ((TextBox)sender);
        if(((change.AddedLength - change.RemovedLength) > 0 || source.Text.Length > 0) && !DelKeyPressed) {
          if(ftxFixerWnd.Patterns.Any(x => x.Name.StartsWith(source.Text))) {
            string _appendtxt = ftxFixerWnd.Patterns.FirstOrDefault(x => x.Name.StartsWith(source.Text)).Name;
            _appendtxt = _appendtxt.Remove(0, change.Offset + 1);
            source.Text += _appendtxt;
            source.SelectionStart = change.Offset + 1;
            source.SelectionLength = source.Text.Length;
          }
        }

        InProg = false;
      }
    }

    /// <summary> Click event handler for Close button </summary>
    private void OnClosePatternWnd(object sender, RoutedEventArgs e) => Hide();

    public event PropertyChangedEventHandler PropertyChanged;
    private System.Windows.Forms.FolderBrowserDialog fbd = null;
    private FTXFixerWnd ftxFixerWnd = null;
    private string _PText = "";
    private string _LogPath = "";
    private string newPattern = "";
    private bool _LoggingArticle = false;
    private bool _LoggingInput = false;
    private bool _LoggingOutput = false;
    private bool _LogLocationExists = false;
    private bool InProg;
    private bool DelKeyPressed;
  }
}
