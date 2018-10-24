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
 * FTXProcessor.cs - Handles processing of fulltext files.
 * 
 */


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace FTXFixer.code {
  /// <summary> Processes fulltext input files at input location and writes output against each of them </summary>
  class FTXProcessor {
    /// <summary> Constructor for FTXProcessor </summary>
    /// <param name="fixer"> Main class object to display activity message </param>
    public FTXProcessor(FTXFixerWnd fixer, SettingsWnd settings) {
      fixerWnd = fixer;
      settingWnd = settings;
      crcFixer = new CRCFixer();
      UpdatePatternsInfo();
      RefreshLogLocations();
    }

    /// <summary> Updates patterns and related info </summary>
    private void UpdatePatternsInfo() {
      _Patterns = fixerWnd.SortedPatterns;
      if(_Patterns != null && _Patterns.Count != 0)
        minPatternLength = _Patterns.Min(x => x.Length);
    }

    /// <summary> Refreshes log locations </summary>
    private void RefreshLogLocations() {
      if(settingWnd == null)
        return;

      string strLogPath = settingWnd.LogPath;
      if(Directory.Exists(strLogPath)) {
        InputHistoryFile = strLogPath + "\\" + "FTXINPUT.HST";
        OutputHistoryFile = strLogPath + "\\" + "FTXOUTPUT.HST";
        LogFile = strLogPath + "\\FTXFIXER_" + DateTime.Now.ToString("ddMMyyyy") + ".ANLOG";
      }
    }

    /// <summary> Input path property to access outside </summary>
    public string InputPath {
      get { return _InputPath; }
      set {
        if(_InputPath != value)
          _InputPath = value;
      }
    }

    /// <summary> Output path property to access outside </summary>
    public string OutputPath {
      get { return _OutputPath; }
      set {
        if(_OutputPath != value)
          _OutputPath = value;
      }
    }

    /// <summary> Input file list (F*.M*) at input location </summary>
    public List<string> InputFiles {
      get { return _InputFiles; }
      set {
        if(_InputFiles != value)
          _InputFiles = value;
      }
    }

    /// <summary> To access article log file </summary>
    public string ArticleLog => LogFile;

    /// <summary> Check if give file name already into history file </summary>
    /// <param name="strFile"> name of file to be checked </param>
    /// <param name="hType"> type of history where file entry to be checked </param>
    /// <returns> true if found else false </returns>
    private bool HasHistoryEntry(string strFile, HistoryType hType) {
      string strHistoryFile = (hType == HistoryType.INPUT) ? InputHistoryFile : OutputHistoryFile;
      if(!File.Exists(strHistoryFile))
        return false;

      bool iFound = false;
      try {
        StreamReader hFile = File.OpenText(strHistoryFile);
        string strLine = "";
        while(strLine != null) {
          strLine = hFile.ReadLine();
          if(strLine == null)
            break;

          if(strLine == strFile) {
            iFound = true;
            break;
          }
        }

        hFile.Close();
      } catch { return false; }

      return iFound;
    }

    /// <summary> Check if specified logging available </summary>
    /// <param name="lType"> type of logging to be checked </param>
    /// <param name="showActivity"> Whether to show activity message </param>
    /// <returns> true if valid, else false </returns>
    private bool IsLoggingAllowed(LoggingType lType, bool showActivity) {
      bool isLoggingAllowed = Directory.Exists(settingWnd.LogPath);
      string strLoggingError = "";
      switch(lType) {
        case LoggingType.ARTICLE:
          strLoggingError = "Can't write article level log, check log location permission.";
          isLoggingAllowed = (isLoggingAllowed && settingWnd.LoggingArticle);
          break;
        case LoggingType.INPUTHISTORY:
          strLoggingError = "Can't log input history, check log location permission.";
          isLoggingAllowed = (isLoggingAllowed && settingWnd.LoggingInput);
          break;
        case LoggingType.OUTPUTHISTORY:
          strLoggingError = "Can't log output history, check log location permission.";
          isLoggingAllowed = (isLoggingAllowed && settingWnd.LoggingOutput);
          break;
      }

      try {
        if(isLoggingAllowed) {
          System.Security.AccessControl.DirectorySecurity ds = Directory.GetAccessControl(settingWnd.LogPath);
          return true;
        }
      } catch {
        if(showActivity && isLoggingAllowed)
          ThreadInvoker.Instance.RunByUiThread(() => {
            MessageBox.Show(fixerWnd, strLoggingError, "Logging not possible!", MessageBoxButton.OK, MessageBoxImage.Error);
          });

        return false;
      }

      return isLoggingAllowed;
    }

    /// <summary> History data is flushed at end of processing, not against each file </summary>
    /// <param name="hType"> Type of history for which data to be flushed </param>
    private void FlushHistory(HistoryType hType) {
      List<string> historyBuffer = (hType == HistoryType.INPUT) ? inputHistoryBuffer : outputHistoryBuffer;
      if(historyBuffer.Count == 0)
        return;

      string strHistoryFile = (hType == HistoryType.INPUT) ? InputHistoryFile : OutputHistoryFile;
      string strHstErrorMsg = (hType == HistoryType.INPUT) ? "input" : "output";
      try {
        historyFile = new StreamWriter(strHistoryFile, true);
        foreach(string strFile in historyBuffer)
          historyFile.WriteLine(strFile);

        historyFile.Close();
        historyBuffer.Clear();
      } catch {
        strHstErrorMsg = "Failed to update " + strHstErrorMsg + " history file, check file permission.";
        ThreadInvoker.Instance.RunByUiThread(() => {
          MessageBox.Show(fixerWnd, strHstErrorMsg, "History entry failed!", MessageBoxButton.OK, MessageBoxImage.Error);
        });
      }
    }

    /// <summary> Prepares/closes logging </summary>
    /// <param name="startLogging"> Starts logging if true else false </param>
    private void OpenLogging(bool startLogging) {
      try {
        string strLogHeader = string.Format("+------- FTXFixer version {0} -------+", 
          fixerWnd.GetAssemblyInfo(AssemblyType.VERSION));
        string strDashLine = "+";
        strDashLine += string.Concat(Enumerable.Repeat("-", strLogHeader.Length - 2));
        strDashLine += "+";
        if(startLogging) {
          logFile = new StreamWriter(LogFile, true);
          logFile.WriteLine(strLogHeader);
          logFile.WriteLine("|File Name    | Article   | Patterns | Footer|");
          logFile.WriteLine(strDashLine);
        } else {
          if(logFile == null)
            return;

          logFile.WriteLine(strDashLine);
          logFile.WriteLine();
          logFile.Close();
        }
      } catch {
        string strLoggingError = "Failed to write article level logging, check file permission.";
        if(startLogging)
          ThreadInvoker.Instance.RunByUiThread(() => {
            MessageBox.Show(fixerWnd, strLoggingError, "Article logging failed!", MessageBoxButton.OK, MessageBoxImage.Error);
          });
      }
    }

    /// <summary> Writes all output files with similar name as input files. </summary>
    public void WriteOutputFiles() {
      UpdatePatternsInfo();
      TotalBytesRead = 0;
      Stopwatch stopWatch = new Stopwatch();
      stopWatch.Start();
      RefreshLogLocations();
      LanguageIndex = fixerWnd.LangIndex;
      int failedToProcess = 0;
      int failedToWrite = 0;
      int totalInputFiles = _InputFiles.Count;
      bool inputHistoryEnabled = IsLoggingAllowed(LoggingType.INPUTHISTORY, true);
      bool outputHistoryEnabled = IsLoggingAllowed(LoggingType.OUTPUTHISTORY, true);
      bool articleLoggingEnabled = IsLoggingAllowed(LoggingType.ARTICLE, true);
      if(articleLoggingEnabled)
        OpenLogging(true);

      FileInfo inpFile = null;
      foreach(string strFile in _InputFiles) {
        if(inputHistoryEnabled && HasHistoryEntry(strFile, HistoryType.INPUT)) {
          inpFile = new FileInfo(_InputPath + "\\" + strFile);
          TotalBytesRead += inpFile.Length;
          UpdateProgressValue();
          failedToProcess++;
          continue;
        }

        if(failedToProcess == totalInputFiles)
          break;

        bool iOutputWritten = PrepareOutputFile(strFile, outputHistoryEnabled);
        if(!iOutputWritten) {
          inpFile = new FileInfo(_InputPath + "\\" + strFile);
          TotalBytesRead += inpFile.Length;
          UpdateProgressValue();
          failedToWrite++;
        }

        if(inputHistoryEnabled && iOutputWritten)
          inputHistoryBuffer.Add(strFile);
      }

      TotalBytesRead = fixerWnd.TotalInputSize;
      UpdateProgressValue();
      if(inputHistoryEnabled)
        FlushHistory(HistoryType.INPUT);

      if(outputHistoryEnabled)
        FlushHistory(HistoryType.OUTPUT);

      if(articleLoggingEnabled)
        OpenLogging(false);

      stopWatch.Stop();
      double elapsedTime = (double)stopWatch.ElapsedMilliseconds / 1000;
      ThreadInvoker.Instance.RunByUiThread(() => {
        string strProcessedMsg = "";
        if(failedToProcess != 0) {
          strProcessedMsg = string.Format("{0} successfully processed in {1:0.00} seconds, {2} failed to process. Check input history or permission.",
              totalInputFiles - failedToProcess, elapsedTime, failedToProcess);
          fixerWnd.DisplayActivity(strProcessedMsg, ActivityType.ACTWARNING);
        } else if(failedToWrite != 0) {
          strProcessedMsg = string.Format("{0} successfully written in {1:0.00} seconds, {2} failed to write. Check output history or permission.",
              totalInputFiles - failedToWrite, elapsedTime, failedToWrite);
          fixerWnd.DisplayActivity(strProcessedMsg, ActivityType.ACTWARNING);
        } else {
          strProcessedMsg = string.Format("Processing completed in {0:0.00} seconds, {1} files written successfully.", elapsedTime, totalInputFiles);
          fixerWnd.DisplayActivity(strProcessedMsg, ActivityType.ACTOTHER);
        }

        fixerWnd.SetProcessingMode(false);
      });
    }

    /// <summary> Returns index of specified date part </summary>
    /// <param name="strDatePart"> Part of date (e.g. YY, MM or DD) </param>
    /// <param name="dType"> Type of part (e.g. year, month or date) </param>
    /// <returns> valid index if possible else - 1 </returns>
    private int GetIndexForDatePart(string strDatePart, DatePart dtType) {
      int indexDatePart = -1;
      try {
        switch(dtType) {
          case DatePart.YEAR:
            string strYear = strDatePart.ToUpper();
            for(int i = 0; i < years.Length; i++)
              if(strYear == years[i])
                return i;

            break;
          case DatePart.MONTH:
          case DatePart.DAY:
            int datePartValue = int.Parse(strDatePart);
            indexDatePart = datePartValue - 1;
            break;
        }
      } catch { return -1; }

      return indexDatePart;
    }

    /// <summary> Returns new output file to be written </summary>
    /// <param name="strInputFile"> short intput file name, may not require later </param>
    /// <returns> furnished output file name </returns>
    private string PrepareOutputFileName(string strInputFile) {
      if(strInputFile.Length != 12)
        return "FM" + strInputFile.Substring(2);

      try {
        int yearIndex = GetIndexForDatePart(strInputFile.Substring(2, 2), DatePart.YEAR);
        int monthIndex = GetIndexForDatePart(strInputFile.Substring(4, 2), DatePart.MONTH);
        int dayIndex = GetIndexForDatePart(strInputFile.Substring(6, 2), DatePart.DAY);

        string strOutputFile = "FM";
        if(yearIndex != -1 && monthIndex != -1 && dayIndex != -1) {
          if(yearIndex == 0)
            strOutputFile += "2017";
          else if(yearIndex < 18)
            strOutputFile += ("20" + years[yearIndex]);
          else
            strOutputFile += ("19" + years[yearIndex]);

          strOutputFile += (months[monthIndex].ToString() + days[dayIndex].ToString() + strInputFile.Substring(8, 4));
          return strOutputFile;
        }
      } catch(Exception) { }

      return "FM" + strInputFile.Substring(2);
    }

    /// <summary> Reads specified input file and write output with the same name </summary>
    /// <param name="strInputFile"> Name of input file (short name) </param>
    /// <returns> true if succeeds else false </returns>
    private bool PrepareOutputFile(string strInputFile, bool logOutput) {
      string inputFile = _InputPath + "\\" + strInputFile;
      string strOutputFile = PrepareOutputFileName(strInputFile);
      string outputFile = _OutputPath + "\\" + strOutputFile;
      if(logOutput && HasHistoryEntry(strOutputFile, HistoryType.OUTPUT)) {
        ThreadInvoker.Instance.RunByUiThread(() => {
          fixerWnd.DisplayActivity("Output file " + strOutputFile + " found in processed list, check history.", ActivityType.ACTWARNING);
        });
        return false;
      }

      try {
        inFile = new StreamReader(inputFile, Encoding.Default);
        if(inFile == null) {
          ThreadInvoker.Instance.RunByUiThread(() => {
            fixerWnd.DisplayActivity("Failed to read " + inputFile, ActivityType.ACTERROR);
          });
          return false;
        }
      } catch(Exception ex) {
        ThreadInvoker.Instance.RunByUiThread(() => {
          fixerWnd.DisplayActivity(ex.Message, ActivityType.ACTCRITICAL);
        });
        return false;
      }

      try {
        outFile = new StreamWriter(outputFile, false, inFile.CurrentEncoding);
        if(outFile == null) {
          ThreadInvoker.Instance.RunByUiThread(() => {
            fixerWnd.DisplayActivity("Failed to write " + outputFile, ActivityType.ACTERROR);
          });
          return false;
        }

        ThreadInvoker.Instance.RunByUiThread(() => {
          fixerWnd.DisplayActivity("Writing " + outputFile + "...", ActivityType.ACTPROC);
        });
        return ReadAndWriteToFile(strOutputFile, logOutput);
      } catch(Exception ex) {
        ThreadInvoker.Instance.RunByUiThread(() => {
          fixerWnd.DisplayActivity(ex.Message, ActivityType.ACTCRITICAL);
        });
        if(inFile != null && inFile.BaseStream != null)
          inFile.Close();

        return false;
      }
    }

    /// <summary> Returns true if article doesn't have any data </summary>
    /// <returns></returns>
    private bool IsArticleEmpty() {
      List<string> nonEmptyData = article.Data.Where(p => p.Trim().Length > 0).ToList();
      return !(nonEmptyData.Count > 0);
    }

    /// <summary> Logs empty article </summary>
    /// <param name="strOutFile"> name of current output file </param>
    private void LogEmptyArticle(string strOutFile) {
      try {
        if(logFile != null && article.ID != 0) {
          string strFile = "";
          if(strOutFile != currentOutFile)
            strFile = currentOutFile = strOutFile;

          string strLogLine = string.Format("|{0, -12} | {1, 9} | {2, 8} | {3, 6}|", 
            strFile, article.ID, article.Patterns.Count, "NA");
          logFile.WriteLine(strLogLine);
          article.Reset();
        }
      } catch { }
    }

    /// <summary> Check if data qualify for omitted search </summary>
    /// <param name="strData"> data to be checked </param>
    /// <returns> true if qualifies else false </returns>
    private bool IsQualifyForOmitSearch(string strData) {
      int omitIndex = strData.IndexOf(" omitted]");
      if (omitIndex == -1)
        omitIndex = strData.IndexOf(" omitted.]");

      if (omitIndex == -1)
        return false;

      int openBraceIndex = strData.IndexOf('[');
      if (openBraceIndex < omitIndex && openBraceIndex != -1)
        return true;

      return false;
    }

    /// <summary> Check if data qualify for pattern search </summary>
    /// <param name="strData"> data to be checked </param>
    /// <returns> true if qualifies else false </returns>
    private bool IsQualifyForPatternSearch(string strData) {
      if (strData.Length < minPatternLength)
        return false;

      foreach(Pattern pattern in _Patterns)
        if(strData.Contains(pattern.Name))
          return true;

      return false;
    }

    /// <summary> Reads input file, processes the data and writes into output file. </summary>
    /// <param name="strInputFile"> short input file name, for logging into history </param>
    /// <returns> true if succeeds else false </returns>
    private bool ReadAndWriteToFile(string strOutputFile, bool logOutput) {
      try {
        string strLine = "";
        while(strLine != null) {
          if(!fixerWnd.Processing)
            break;

          strLine = inFile.ReadLine();
          if(strLine == null) {
            if(!IsArticleEmpty())
              FlushArticleData(strOutputFile);
            else
              LogEmptyArticle(strOutputFile);

            break;
          }

          ProgressFrequency++;
          int lineLength = strLine.Length;
          TotalBytesRead += lineLength + 2;
          if(ProgressFrequency == 100)
            UpdateProgressValue();

          string strLowerLine = strLine.TrimEnd().ToLower();
          string strToWrite = strLine;
          if(lineLength > 10) {
            if(IsQualifyForPatternSearch(strLowerLine))
              strToWrite = FindAndReplacePatterns(strLine);
            if(IsQualifyForOmitSearch(strToWrite.ToLower()))
              strToWrite = FindAndReplaceOmitted(strToWrite);
            
          }

          if(strLowerLine.StartsWith(strHeaderPattern)) {
            if(article.Data.Count != 0)
              if(!IsArticleEmpty())
                FlushArticleData(strOutputFile);
              else
                LogEmptyArticle(strOutputFile);

            var crcValues = crcFixer.FixACNLine(strToWrite, strHeaderPattern);
            strToWrite = crcValues.Item1;
            article.ID = crcValues.Item2;
            article.Header = strToWrite;
          } else if(article.Header.Length > 0)
            article.Data.Add(strToWrite);
          else
            outFile.WriteLine(strToWrite);
        }

        CloseIOFiles();
        if(logOutput)
          outputHistoryBuffer.Add(strOutputFile);
      } catch(Exception ex) {
        CloseIOFiles();
        string strExcMsg = ex.Message.ToLower();
        if(strExcMsg.Contains("thread") && strExcMsg.Contains("aborted"))
          ThreadInvoker.Instance.RunByUiThread(() => {
            fixerWnd.DisplayActivity(ex.Message, ActivityType.ACTCRITICAL);
          });

        return false;
      }

      return true;
    }

    /// <summary> Updates progressbar with value </summary>
    private void UpdateProgressValue() {
      if(fixerWnd.TotalInputSize == 0)
        return;

      ProgressFrequency = 0;
      fixerWnd.Dispatcher.BeginInvoke(DispatcherPriority.Input, (Action)(() => {
        fixerWnd.ProgressValue = (TotalBytesRead / fixerWnd.TotalInputSize) * 100;
        fixerWnd.ProgressText = ((int)fixerWnd.ProgressValue).ToString() + "%";
        fixerWnd.Progress.ToolTip = fixerWnd.ProgressText + " completed.";
      }));
    }

    /// <summary> Closes input and output files </summary>
    private void CloseIOFiles() {
      try {
        if(inFile != null && inFile.BaseStream != null)
          inFile.Close();

        if(outFile != null && outFile.BaseStream != null)
          outFile.Close();
      } catch { }
    }

    /// <summary> Handles all paterns from INI </summary>
    /// <param name="strLine"> input string </param>
    /// <returns> re-formatted string </returns>
    private string FindAndReplacePatterns(string strLine) {
      string strToWrite = strLine;
      string strLower = strToWrite.ToLower();
      int lenToWrite = strToWrite.Length;
      for (int i = 0; i < _Patterns.Count; i++) {
        Pattern pattern = _Patterns[i];
        int patternLength = pattern.Length;
        if (lenToWrite < minPatternLength)
          break;

        int startIndex = strLower.IndexOf(pattern.Name);
        if (startIndex >= 0 && startIndex < strLower.Length) {
          strToWrite = strToWrite.Remove(startIndex, patternLength);
          strLower = strToWrite.ToLower();
          lenToWrite = strToWrite.Length;
          if (!article.Patterns.Contains(_Patterns[i]))
            article.Patterns.Add(_Patterns[i]);

          i = -1;
          continue;
        }
      }

      return strToWrite;
    }

    /// <summary> There could be continuous open braces, skip to last if so </summary>
    /// <param name="strLine"> string where search to be performed </param>
    /// <returns> index of '[' </returns>
    public static int GetFinalOpenBraceIndex(string strLine) {
      int startIndex = strLine.IndexOf('[');
      if(startIndex == -1)
        return -1;

      int lenLine = strLine.Length;
      while(startIndex < lenLine && strLine[startIndex] == '[')
        startIndex++;

      return strLine.IndexOf('[', startIndex - 1);
    }

    /// <summary> Handles all unspecified [???? omitted] or [???? omitted.] patern </summary>
    /// <param name="strLine"> input string </param>
    /// <returns> re-formatted string </returns>
    private string FindAndReplaceOmitted(string strLine) {
      string omittedStringWithDot = " omitted.]";
      string omittedString = " omitted]";
      int omittedLength = 0;
      string strToWrite = strLine;
      string strLower = strToWrite.ToLower();
      while(true) {
        int startIndex = GetFinalOpenBraceIndex(strLower);
        if(startIndex == -1)
          return strToWrite;

        int omittedIndex = strLower.IndexOf(omittedStringWithDot, startIndex);
        if(omittedIndex == -1) {
          omittedIndex = strLower.IndexOf(omittedString, startIndex);
          omittedLength = omittedString.Length;
        } else
          omittedLength = omittedStringWithDot.Length;

        if(omittedIndex != -1) {
          int patternLength = omittedIndex + omittedLength - startIndex;
          string patternName = strLower.Substring(startIndex, patternLength);
          Pattern pattern = new Pattern { Name = patternName };
          if(!article.HasPattern(pattern))
            article.Patterns.Add(pattern);

          strToWrite = strToWrite.Remove(startIndex, patternLength);
          strLower = strToWrite.ToLower();
          continue;
        } else
          return strToWrite;
      }
    }

    /// <summary> Writes article data to disk </summary>
    private void FlushArticleData(string strOutFile) {
      try {
        outFile.WriteLine(article.Header);
        foreach(string strLine in article.Data)
          outFile.WriteLine(strLine);

        bool footerAdded = false;
        if(fixerWnd.AddFooter && article.HasOmittedPattern()) {
          footerAdded = true;
          outFile.WriteLine(footers[LanguageIndex]);
        }

        string addedFooter = (footerAdded) ? "Yes" : "No";
        if(logFile != null && article.ID != 0) {
          string strFile = "";
          if(strOutFile != currentOutFile)
            strFile = currentOutFile = strOutFile;

          string strLogLine = string.Format("|{0, -12} | {1, 9} | {2, 8} | {3, 6}|",
            strFile, article.ID, article.Patterns.Count, addedFooter);
          logFile.WriteLine(strLogLine);
        }
      } catch { }

      article.Reset();
    }

    /// <summary> Validates and closes all resources before aborting process </summary>
    public void ReleaseLockedResources() {
      FlushArticleData(currentOutFile);
      CloseIOFiles();
      if(IsLoggingAllowed(LoggingType.INPUTHISTORY, false))
        FlushHistory(HistoryType.INPUT);

      if(IsLoggingAllowed(LoggingType.OUTPUTHISTORY, false))
        FlushHistory(HistoryType.OUTPUT);

      if(IsLoggingAllowed(LoggingType.ARTICLE, false))
        OpenLogging(false);
    }

    private enum HistoryType { INPUT = 0, OUTPUT };
    private enum LoggingType { ARTICLE = 0, INPUTHISTORY, OUTPUTHISTORY };
    private enum DatePart { YEAR = 0, MONTH, DAY };

    private Article article = new Article();
    private List<Pattern> _Patterns = null;
    private List<string> _InputFiles = new List<string>();
    private List<string> inputHistoryBuffer = new List<string>();
    private List<string> outputHistoryBuffer = new List<string>();
    private double TotalBytesRead = 0;
    private string _InputPath = "";
    private string LogFile = "";
    private string _OutputPath = "";
    private string InputHistoryFile = "";
    private string OutputHistoryFile = "";
    private string currentOutFile = "";
    private int LanguageIndex = 0;
    private int ProgressFrequency = 0;
    private int minPatternLength = 0;
    private StreamReader inFile = null;
    private StreamWriter outFile = null;
    private StreamWriter logFile = null;
    private StreamWriter historyFile = null;
    private FTXFixerWnd fixerWnd = null;
    private SettingsWnd settingWnd = null;
    private CRCFixer crcFixer = null;
    private static readonly string strHeaderPattern = "@@@";
    private static readonly char[] days = {'1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F', 'G',
                                           'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V'};
    private static readonly char[] months = { '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C' };
    private static readonly string[] years = { "GS", "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11",
                                               "12", "13", "14", "15", "16", "80", "81", "82", "83", "84", "85", "86", "87",
                                               "88", "89", "90", "91", "92", "93", "94", "95", "96", "97", "98", "99"};
    private static readonly string[] footers = {"----------\n\nPlease note: Illustration(s) are not available due to copyright restrictions.\n\n",
                                                "----------\n\nAviso: Ilustración(es) no disponible(s) por restricción de derechos de autor.\n\n",
                                                "----------\n\nVeuillez noter que l’iIllustration(s) est non disponible(s) en raison des restrictions de droits d'auteur.\n\n",
                                                "----------\n\nPlease note: Some tables or figures were omitted from this article.\n\n"};
  }
}
