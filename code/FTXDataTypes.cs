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
 * FTXDataTypes.cs - Pattern and Article class implementation.
 * 
 */


using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace FTXFixer.code {
  /// <summary> Data type for pattern </summary>
  public class Pattern : INotifyPropertyChanged {
    /// <summary> Property to access ID </summary>
    public string ID {
      get { return _ID; }
      set {
        if(_ID != value) {
          _ID = value;
          RaisePropertyChanged("ID");
        }
      }
    }

    /// <summary> Property to access length of pattern </summary>
    public int Length => _Name.Length;

    /// <summary> Property to access existing pattern </summary>
    public string Name {
      get { return _Name; }
      set {
        if(_Name != value) { 
          _Name = value;
          RaisePropertyChanged("Name");
          int lenName = _Name.Length;
          if(_Name[0] == '[' && (_Name.Contains(" omitted]") || _Name.Contains(" omitted.]")))
            _Omitted = true;
          if(lenName > 1 && _Name[0] == '[' && _Name[lenName - 1] == ']')
            _Color = "Black";
          else
            _Color = "Indigo";
        }
      }
    }

    /// <summary> Returns indigo color if pattern is not standard </summary>
    public string Color => _Color;

    /// <summary> Property to access if a omitted pattern </summary>
    public bool Omitted => _Omitted;

    protected void RaisePropertyChanged(string propertyName) =>
      PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public event PropertyChangedEventHandler PropertyChanged;
    private string _ID = "";
    private string _Name = "";
    private string _Color = "";
    private bool _Omitted = false;
  }

  /// <summary> Data type for article, limited to requirement scope </summary>
  class Article {
    /// <summary> Default constructor </summary>
    public Article() { }

    /// <summary> Property to access article ID </summary>
    public long ID {
      get { return _ID; }
      set {
        if(_ID != value)
          _ID = value;
      }
    }

    /// <summary> Property to access available patterns in article </summary>
    public List<Pattern> Patterns {
      get { return _Patterns; }
      set {
        if(_Patterns != value)
          _Patterns = value;
      }
    }

    /// <summary> Property to access article header </summary>
    public string Header {
      get { return _Header; }
      set {
        if(_Header != value)
          _Header = value;
      }
    }

    /// <summary> Property to access article data </summary>
    public List<string> Data {
      get { return _Data; }
      set {
        if(_Data != value)
          _Data = value;
      }
    }

    /// <summary> Resets article data </summary>
    public void Reset() {
      Header = "";
      Data.Clear();
      Patterns.Clear();
      ID = 0;
    }

    /// <summary> Check if article has any omitted pattern into it's data </summary>
    /// <returns> true if found else false </returns>
    public bool HasOmittedPattern() =>
      _Patterns.Any(x => x.Omitted == true);

    /// <summary> Check if article already has specified pattern </summary>
    /// <param name="patternToFind"> pattern to find </param>
    /// <returns> true if found else false </returns>
    public bool HasPattern(Pattern patternToFind) =>
      _Patterns.Any(x => x.Name == patternToFind.Name);

    /// <summary> Check if article already has specified pattern </summary>
    /// <param name="patternName"> pattern string to find </param>
    /// <returns> true if found else false </returns>
    public bool HasPattern(string patternName) =>
      _Patterns.Any(x => x.Name == patternName);

    private List<Pattern> _Patterns = new List<Pattern>();
    private List<string> _Data = new List<string>();
    private long _ID = 0;
    private string _Header = "";
  }
}
