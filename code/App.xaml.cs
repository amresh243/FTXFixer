using Microsoft.Win32;
using System;
using System.Threading;
using System.Windows;

namespace FTXFixer {
  /// <summary> Interaction logic for App.xaml </summary>
  public partial class App : System.Windows.Application {
    private static Mutex mutex = new Mutex(true, "{8F6F0AC4-B9A1-45fd-A8CF-72F04E6BDE8F}");
    private static FTXFixerWnd fixerWnd = null;

    App() => InitializeComponent();

    private static int GetDotNetReleaseKeyFromRegistry() {
      int releaseKey = -1;
      using(RegistryKey ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey("SOFTWARE\\Microsoft\\NET Framework Setup\\NDP\\v4\\Full\\")) {
        releaseKey = Convert.ToInt32(ndpKey.GetValue("Release"));
        return releaseKey;
      }
    }

    // Checking the version using >= will enable forward compatibility,  
    // however you should always compile your code on newer versions of 
    // the framework to ensure your app works the same. 
    private static string GetDotNetVersion() {
      int releaseKey = GetDotNetReleaseKeyFromRegistry();
      if((releaseKey >= 379893))
        return "4.5.2";

      return "0.0.0";
    }

    [STAThread]
    static void Main() {
      try {
        string dotNetVersion = GetDotNetVersion();
        if (dotNetVersion != "4.5.2") {
          string dotNetMsg = ".NET framework 4.5.2 not installed on system.\nPlease visit requirement section of FTXFixer.exe document.\n";
          dotNetMsg += "Application shows some incosistency with .NET 4.6.1.\nIn case of abnormal behavior, try installing higher version.";
          MessageBox.Show(dotNetMsg, "System requirement failed!", MessageBoxButton.OK, MessageBoxImage.Error);
          return;
        }

        if(mutex.WaitOne(TimeSpan.Zero, true)) {
          fixerWnd = new FTXFixerWnd();
          App app = new App();
          app.Run(fixerWnd);
          mutex.ReleaseMutex();
        } else
          MessageBox.Show("FTXFixer.exe is already running, can't run multiple instance.", "Launch failed!", MessageBoxButton.OK, MessageBoxImage.Error);
      } catch(Exception ex) {
        MessageBox.Show(ex.Message, "Exception occured!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }
  }
}
