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
 * About.xaml.cs - About window UI implementation.
 * 
 */


using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace FTXFixer.code {
  /// <summary> Interaction logic for About Dialog </summary>
  public partial class About : Window, INotifyPropertyChanged {
    public About() {
      this.DataContext = this;
      InitializeComponent();
    }

    /// <summary> Property to access build date </summary>
    public string BuildDate {
      get { return buildDate; }
      set {
        if(value != buildDate) {
          buildDate = value;
          RaisePropertyChanged("BuildDate");
        }
      }
    }

    /// <summary> Property to access build version </summary>
    public string BuildVersion {
      get { return buildVersion; }
      set {
        if(value != buildVersion) {
          buildVersion = value;
          RaisePropertyChanged("BuildVersion");
        }
      }
    }

    /// <summary> Property to access app image </summary>
    public ImageSource AppImage {
      get { return appImage; }
      set {
        if(value != appImage) {
          appImage = value;
          RaisePropertyChanged("AppImage");
        }
      }
    }

    /// <summary> Property to access copyright info </summary>
    public string Copyright {
      get { return copyright; }
      set {
        if(value != copyright) {
          copyright = value;
          RaisePropertyChanged("Copyright");
        }
      }
    }

    /// <summary> Binding notification handler </summary>
    /// <param name="propertyName"> Name of property against which change triggered </param>
    protected void RaisePropertyChanged(string propertyName) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    /// <summary> KeyDown event handler for pattern window,
    ///             presssing escape closes pattern window </summary>
    private void OnEscapeOrEnter(object sender, KeyEventArgs e) {
      if(e.Key == Key.Escape || e.Key == Key.Enter)
        Hide();
    }

    /// <summary> OK button handler, hides about dialog </summary>
    private void OnAboutOK(object sender, RoutedEventArgs e) => Hide();

    public event PropertyChangedEventHandler PropertyChanged;
    private ImageSource appImage;
    private string buildDate;
    private string buildVersion;
    private string copyright;
  }
}
