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
 * INIProcessor.cs - Settings INI processor.
 * 
 */


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace FTXFixer.code {
  ///<summary>key structure</summary>
  public class key {
    ///<summary>Default constructor</summary>
    public key() {
      mstrName = "";
      _Value = null;
      _Comments = new List<string>();
    }

    ///<summary>get or set key name</summary>
    public string Name {
      get { return mstrName; }
      set { mstrName = value; }
    }

    ///<summary>get or set key value</summary>
    public object Value {
      get { return _Value; }
      set { _Value = value; }
    }

    ///<summary>get or set key comment</summary>
    public List<string> Comments {
      get { return _Comments; }
      set {
        if(value == null)
          return;

        for(int i = 0; i < value.Count; i++)
          _Comments.Add(value[i]);
      }
    }

    private string mstrName;
    private object _Value;
    private List<string> _Comments;
  };

  ///<summary>Section structure</summary>
  public class Section : IDictionary {
    ///<summary>Default constructor</summary>
    public Section() {
      mstrName = "";
      mnCount = 0;
      mlKeys = new List<key>();
      mlstrComment = new List<string>();
    }

    ///<summary>get or set section name</summary>
    public string Name {
      get { return mstrName; }
      set { mstrName = value; }
    }

    ///<summary>get or set key list</summary>
    public List<key> keys {
      get { return mlKeys; }
      set {
        mlKeys.Clear();
        if(value == null)
          return;

        for(int i = 0; i < mlKeys.Count; i++)
          mlKeys.Add(value[i]);
      }
    }

    ///<summary>get key count</summary>
    public int keyCount => mlKeys.Count;

    ///<summary>Finds the given key in list</summary>
    ///<param name="keyName">Name of the key which has to be searched</param>
    ///<returns>-1 if not found else index of key</returns>
    public int FindKey(string keyName) {
      for(int i = 0; i < mlKeys.Count; i++)
        if(mlKeys[i].Name == keyName)
          return i;

      return -1;
    }

    ///<summary>get or set section comment</summary>
    public List<string> Comments {
      get { return mlstrComment; }
      set {
        if(value == null)
          return;

        for(int i = 0; i < value.Count; i++)
          mlstrComment.Add(value[i]);
      }
    }

    private List<key> mlKeys;
    private string mstrName;
    private List<string> mlstrComment;
    private int mnCount;

    #region IDictionary Members

    /// <summary>Overloaded Add of IDictionary. Adds a key to list.</summary>
    /// <param name="key">Name of the key</param>
    /// <param name="value">Value of the key</param>
    public void Add(object key, object value) {
      string strKey = key.ToString();
      int nAt = FindKey(strKey);
      if(nAt != -1)
        return;

      key k = new key();
      k.Name = key.ToString();
      k.Value = value;
      mlKeys.Add(k);
    }

    public void Add(key newKey) {
      string strKey = newKey.Name;
      int nAt = FindKey(strKey);
      if(nAt != -1)
        return;

      mlKeys.Add(newKey);
    }

    /// <summary>Overloaded Clear of IDictionary. Clears the list</summary>
    public void Clear() => mlKeys.Clear();

    /// <summary>Checks whether given key exists or not. Member of IDictionary.</summary>
    /// <param name="key">Name of the key</param>
    /// <returns>true if found else false</returns>
    public bool Contains(object key) {
      string keyName = key.ToString();
      int nAt = FindKey(keyName);

      return (nAt == -1) ? false : true;
    }

    /// <summary>Member of IDictionaryEnumerator. Dummy function.</summary>
    /// <returns>null</returns>
    public IDictionaryEnumerator GetEnumerator() => null;

    /// <summary>Dummy member of IDictionaryEnumerator.</summary>
    /// <returns>false</returns>
    public bool IsFixedSize => false;

    /// <summary>Dummy member of IDictionaryEnumerator.</summary>
    /// <returns>false</returns>
    public bool IsReadOnly => false;

    /// <summary>Overloaded property of IDictionaryEnumerator.</summary>
    /// <returns>List of key names</returns>
    public ICollection Keys {
      get {
        List<string> lstKeys = new List<string>();
        for(int i = 0; i < mlKeys.Count; i++)
          lstKeys.Add(mlKeys[i].Name);

        return lstKeys;
      }
    }

    /// <summary>Removes given key. Overload member of IDictionaryEnumerator.</summary>
    /// <param name="key">Name of the key.</param>
    public void Remove(object key) {
      string keyName = key.ToString();
      int nAt = FindKey(keyName);
      if(nAt != -1)
        mlKeys.RemoveAt(nAt);
    }

    /// <summary>Overloaded property of IDictionaryEnumerator.</summary>
    /// <returns>List of values</returns>
    public ICollection Values {
      get {
        List<object> lstValues = new List<object>();
        for(int i = 0; i < mlKeys.Count; i++)
          lstValues.Add(mlKeys[i].Value);

        return lstValues;
      }
    }

    /// <summary>Returns value of given key. Overloaded property of IDictionaryEnumerator.</summary>
    /// <param name="key">Key Name</param>
    /// <returns>Value of given key if found else null.</returns>
    public object this[object key] {
      get {
        string keyName = key.ToString();
        int nAt = FindKey(keyName);
        return (nAt == -1) ? null : mlKeys[nAt].Value;
      }

      set {
        string keyName = key.ToString();
        int nAt = FindKey(keyName);
        if(nAt != -1)
          mlKeys[nAt].Value = value;
      }
    }

    #endregion

    #region ICollection Members
    /// <summary>Overloaded dummy function of ICollection</summary>
    /// <param name="array"></param>
    /// <param name="index"></param>
    public void CopyTo(Array array, int index) {
      if(index < 0 || index >= array.Length || array == null)
        return;

      for(int i = 0; i < index; i++)
        array.SetValue(mlKeys[i], i);
    }

    /// <summary>Returns key count</summary>
    public int Count => mnCount;

    /// <summary>Overloaded dummy function of ICollection</summary>
    /// <returns>false</returns>
    public bool IsSynchronized => false;

    /// <summary>Overloaded dummy function of ICollection</summary>
    /// <returns>null</returns>
    public object SyncRoot => null;

    #endregion

    #region IEnumerable Members

    /// <summary>Dummy overloaded function of IEnumerable.</summary>
    /// <returns>null</returns>
    IEnumerator IEnumerable.GetEnumerator() => null;

    #endregion
  }


  ///<summary>This class is used to read a value for a corressponding key of 
  /// ini or cfg file</summary>
  public class INIProcessor {
    ///<summary>This constructor takes path of the ini or cfg file.
    /// <b>Note:</b> All the lines which starts with # as first character
    /// is being treated as comment in this class.</summary>
    public INIProcessor(string strPath, Window oWner) {
      try {
        Owner = oWner;
        mstrPath = strPath;
        _Sections = new List<Section>();
        if(!File.Exists(strPath) && !CreateNew()) {
          _OK = false;
          return;
        } else
          _OK = true;

        List<string> aBuf = new List<string>();
        string strBuf = "";
        StreamReader stRead = File.OpenText(mstrPath);
        if(stRead == null) {
          _OK = false;
          return;
        }

        while(strBuf != null) {
          strBuf = stRead.ReadLine();
          aBuf.Add(strBuf);
        }

        stRead.Close();
        string strCurSection = "";
        List<string> lstrCurComment = new List<string>();
        Section sc = new Section();
        for(int i = 0; i < aBuf.Count; i++) {
          if(aBuf[i] == null)
            break;

          string strLine = TrimLeft(aBuf[i].ToString());
          if(strLine.Length == 0)
            continue;

          if(strLine.Length != 0 && strLine[0] == '#') {
            lstrCurComment.Add(aBuf[i]);
            continue;
          }

          if(strLine[0] == '[' && strLine[strLine.Length - 1] == ']') {
            sc = new Section();
            if(lstrCurComment.Count != 0) {
              for(int j = 0; j < lstrCurComment.Count; j++)
                sc.Comments.Add(lstrCurComment[j]);

              lstrCurComment.Clear();
            }

            strCurSection = strLine;
            strCurSection = strCurSection.Remove(0, 1);
            strCurSection = strCurSection.Remove(strCurSection.Length - 1);
            sc.Name = strCurSection;
            _Sections.Add(sc);
          }

          if(strLine.Contains("=")) {
            Char[] chars = new Char[1];
            chars[0] = '=';
            String[] kv = strLine.Split(chars);
            if(kv.Length < 2)
              continue;

            if(kv.Length >= 2) {
              string val = "";
              for(int vl = 1; vl < kv.Length; vl++)
                val += kv[vl];

              kv[1] = val;
            }

            if(_Sections.Count == 0)
              _Sections.Add(sc);

            key k = new key();
            if(lstrCurComment.Count != 0) {
              for(int j = 0; j < lstrCurComment.Count; j++)
                k.Comments.Add(lstrCurComment[j]);

              lstrCurComment.Clear();
            }

            k.Name = kv[0];
            k.Value = kv[1];
            sc.keys.Add(k);
          }
        }
      } catch(Exception ex) {
        string strMsg = "Failed to parse INI file.\n" + ex.Message;
        MessageBox.Show(oWner, strMsg, "Parsing Error!", MessageBoxButton.OK, MessageBoxImage.Error);
      }
    }

    ///<summary>Returns value after trimming white space from left of string</summary>
    ///<param name="strLine">source string to be trimmed</param>
    private string TrimLeft(string strLine) {
      if(strLine.Length == 0)
        return strLine;

      int start = 0;
      while(strLine[start] == ' ')
        start++;

      return strLine.Substring(start);
    }

    ///<summary>Returns value of the given key if key found else nullptr</summary>
    ///<param name="strKey">Key for which value is being searched</param>
    ///<remarks>This function looks for the first occurence of given regardless of
    /// the any section.</remarks>
    ///<returns>an object if value found else nullptr</returns>
    public Object GetValue(string strKey) {
      if(strKey == null)
        return null;

      if(!_OK)
        return null;

      for(int i = 0; i < _Sections.Count; i++) {
        int nAt = _Sections[i].FindKey(strKey);
        if(nAt != -1)
          return _Sections[i].keys[nAt].Value;
      }

      return null;
    }

    ///<summary>Returns value of the given key if key found in given section 
    /// else nullptr</summary>
    ///<param name="strKey">Key for which value is being searched.</param>
    ///<param name="strSection">Section is which value is being searched.</param>
    ///<returns>an object if value found else nullptr</returns>
    public Object GetValue(string strKey, string strSection) {
      if(strKey == null || strSection == null)
        return null;

      if(!_OK)
        return null;

      for(int i = 0; i < _Sections.Count; i++) {
        if(_Sections[i].Name == strSection) {
          int nAt = _Sections[i].FindKey(strKey);
          if(nAt == -1)
            return null;

          return _Sections[i].keys[nAt].Value;
        } else
          continue;
      }

      return null;
    }

    ///<summary>Sets the value for a key. Key is added if not found at the last
    /// line (depending of iWriteifNew), but if found and flag is true only value
    /// is changed.</summary>
    ///<param name="strKey">Key for which value is being searched</param>
    ///<param name="Value">Value which has to be set. Can be of any type</param>
    ///<param name="iWriteifNew">true means value is added at last (if key is not 
    /// found else only value is changed), false means only value is changed (only
    /// if key exists)</param>
    ///<returns>true if seccess else false</returns>
    public bool SetValue(string strKey, Object Value, bool iWriteifNew) {
      if(!_OK)
        return false;

      if(strKey == null)
        return false;

      if(iWriteifNew) {
        key k = new key();
        k.Name = strKey;
        k.Value = Value;
        _Sections[0].keys.Add(k);
        return UpdateFile();
      } else {
        int nAt = _Sections[0].FindKey(strKey);
        if(nAt == -1)
          return false;

        _Sections[0].keys[nAt].Value = Value;
        return UpdateFile();
      }
    }

    ///<summary>Sets the value for a key. Key is added if not found in the
    /// given section.</summary>
    ///<param name="strKey">Key for which value is being searched</param>
    ///<param name="Value">Value which has to be set. Can be of any type</param>
    ///<param name="strSection">only value is changed if key exists else key
    /// is added in the given section</param>
    ///<returns>true if seccess else false</returns>
    public bool SetValue(string strKey, Object Value, string strSection) {
      if(!_OK)
        return false;

      if(strKey == null || strSection == null)
        return false;

      bool iFound = false;
      for(int i = 0; i < _Sections.Count; i++) {
        if(_Sections[i].Name == strSection) {
          int nAt = _Sections[i].FindKey(strKey);
          if(nAt == -1) {
            key k = new key();
            k.Name = strKey;
            k.Value = Value;
            _Sections[i].keys.Add(k);
          } else
            _Sections[i].keys[nAt].Value = Value;

          iFound = true;
          return UpdateFile();
        } else
          continue;
      }

      if(!iFound) {
        Section sec = new Section();
        sec.Name = strSection;
        key k = new key();
        k.Name = strKey;
        k.Value = Value;
        sec.keys.Add(k);
        _Sections.Add(sec);
        return UpdateFile();
      }
      return false;
    }

    ///<summary>Returns true if first occurence of the given key found</summary>
    ///<param name="strKey">Name of the key which has to be searched</param>
    ///<returns>true if found else false</returns>
    public bool HasKey(string strKey) {
      if(!_OK)
        return false;

      if(strKey == null)
        return false;

      int nAt = _Sections[0].FindKey(strKey);
      return (nAt == -1) ? false : true;
    }

    ///<summary>Returns true if specified key of specified section found</summary>
    ///<param name="strKey">Name of the key which has to be searched</param>
    ///<param name="strSection">Name of the section in which key has to be searched</param>
    ///<returns>true if found else false</returns>
    public bool HasKey(string strKey, string strSection) {
      if(!_OK)
        return false;

      if(strKey == null || strSection == null)
        return false;

      for(int i = 0; i < _Sections.Count; i++) {
        if(_Sections[i].Name == strSection) {
          int nAt = _Sections[i].FindKey(strKey);
          return (nAt == -1) ? false : true;
        } else
          continue;
      }

      return false;
    }

    ///<summary>Returns true if given section found.</summary>
    ///<param name="strSection">Name of the section which has to be searched</param>
    ///<returns>true if found else false</returns>
    public bool HasSection(string strSection) {
      if(!_OK)
        return false;

      if(strSection == null)
        return false;

      for(int i = 0; i < _Sections.Count; i++)
        if(_Sections[i].Name == strSection)
          return true;

      return false;
    }

    ///<summary>Deletes first occurence of the given key (if found)</summary>
    ///<param name="strKey">Name of the key which has to be deleted</param>
    ///<returns>true if success else false</returns>
    public bool DeleteKey(string strKey) {
      if(!_OK)
        return false;

      if(strKey == null)
        return false;

      int nAt = _Sections[0].FindKey(strKey);
      if(nAt == -1)
        return false;

      _Sections[0].keys.RemoveAt(nAt);
      return UpdateFile();
    }

    ///<summary>Deletes specified key of specified section (if found)</summary>
    ///<param name="strKey">Name of the key which has to be deleted</param>
    ///<param name="strSection">Name of the section in which key has to be deleted</param>
    ///<returns>true if success else false</returns>
    public bool DeleteKey(string strKey, string strSection) {
      if(!_OK)
        return false;

      if(strKey == null || strSection == null)
        return false;

      for(int i = 0; i < _Sections.Count; i++) {
        if(_Sections[i].Name == strSection) {
          int nAt = _Sections[i].FindKey(strKey);
          if(nAt == -1)
            return false;

          _Sections[i].keys.RemoveAt(nAt);
          return UpdateFile();
        } else
          continue;
      }

      return false;
    }

    ///<summary>Deletes key of specified section (if available)</summary>
    ///<param name="nAt">key index which has to be deleted </param>
    ///<param name="strSection">Name of the section in which key has to be deleted</param>
    ///<returns>true if success else false</returns>
    public bool DeleteKeyAt(int nAt, string strSection) {
      if(!_OK)
        return false;

      if(nAt < 0 || strSection == null)
        return false;

      for(int i = 0; i < _Sections.Count; i++) {
        if(_Sections[i].Name == strSection) {
          if(nAt < _Sections[i].keys.Count) {
            _Sections[i].keys.RemoveAt(nAt);
            return UpdateFile();
          } else
            break;
        } else
          continue;
      }

      return false;
    }

    ///<summary>Deletes all keys of specified section (if found)</summary>
    ///<param name="strSection">Name of the section for which keys to be deleted</param>
    //////<returns>true if success else false</returns>
    public bool DeleteAllKeys(string strSection) {
      if(!_OK || strSection == null)
        return false;

      for(int i = 0; i < _Sections.Count; i++) {
        if(_Sections[i].Name == strSection) {
          _Sections[i].keys.Clear();
          return UpdateFile();
        } else
          continue;
      }

      return false;
    }

    ///<summary>Returns sepcified section.</summary>
    ///<param name="strSection">Name of the section </param>
    ///<returns>section if found else null</returns>
    public Section GetSection(string strSection) {
      if(!_OK)
        return null;

      if(strSection == null)
        return null;

      foreach(Section section in _Sections)
        if(section.Name == strSection)
          return section;

      return null;
    }

    ///<summary>Deletes the whole given section.</summary>
    ///<param name="strSection">Name of the section which has to be deleted</param>
    ///<param name="iSectionAlso">If true section will also be deleted else
    /// section will be left out.</param>
    ///<returns>true if seccess else false</returns>
    public bool DeleteSection(string strSection) {
      if(!_OK)
        return false;

      if(strSection == null)
        return false;

      for(int i = 0; i < _Sections.Count; i++) {
        if(_Sections[i].Name == strSection) {
          _Sections.RemoveAt(i);
          return UpdateFile();
        } else
          continue;
      }

      return false;
    }

    ///<summary>Renames the given section with the given new name.</summary>
    ///<param name="strOldName">Name of the old section which has to be renamed</param>
    ///<param name="strNewName">New name of the section</param>
    ///<returns>true if success else false</returns>
    public bool RenameSection(string strOldName, string strNewName) {
      if(!_OK)
        return false;

      if(strOldName == null || strNewName == null)
        return false;

      for(int i = 0; i < _Sections.Count; i++) {
        if(_Sections[i].Name == strOldName) {
          _Sections[i].Name = strNewName;
          return UpdateFile();
        } else
          continue;
      }

      return false;
    }

    ///<summary>Renames the given key with the new name at first occurence.</summary>
    ///<param name="strOldName">Name of the old key which has to be renamed</param>
    ///<param name="strNewName">New name of the key</param>
    ///<returns>true if success else false</returns>
    public bool RenameKey(string strOldName, string strNewName) {
      if(!_OK)
        return false;

      if(strOldName == null || strNewName == null)
        return false;

      int nAt = _Sections[0].FindKey(strOldName);
      if(nAt == -1)
        return false;

      _Sections[0].keys[nAt].Name = strNewName;
      return UpdateFile();
    }

    ///<summary>Renames the given key with the new name in given section.</summary>
    ///<param name="strOld">Name of the old key which has to be renamed</param>
    ///<param name="strNew">New name of the key</param>
    ///<param name="strSection">Name of the section where key has to be renamed</param>
    ///<returns>true if success else false</returns>
    public bool RenameKey(string strOld, string strNew, string strSection) {
      if(!_OK)
        return false;

      if(strOld == null || strNew == null || strSection == null)
        return false;

      for(int i = 0; i < _Sections.Count; i++) {
        if(_Sections[i].Name == strSection) {
          int nAt = _Sections[i].FindKey(strOld);
          if(nAt == -1)
            return false;

          _Sections[i].keys[nAt].Name = strNew;
          return UpdateFile();
        } else
          continue;
      }

      return false;
    }

    ///<summary>Returns all section list of ini/cfg file.</summary>
    ///<returns>section(ArrayList handle) list if found else nullptr</returns>
    public List<string> GetSectionList() {
      if(!_OK)
        return null;

      if(_Sections.Count == 1 && _Sections[0].Name == "")
        return null;

      List<string> aSections = new List<string>();
      for(int i = 0; i < _Sections.Count; i++)
        aSections.Add(_Sections[i].Name);

      return aSections;
    }

    ///<summary>Returns key list of given section.</summary>
    ///<param name="strSection">Name of the section of which key has to be searched</param>
    ///<returns>keylist(ArrayList handle) of given section if found else nullptr</returns>
    public List<string> GetKeyList(string strSection) {
      if(!_OK)
        return null;

      if(strSection == null)
        return null;

      for(int i = 0; i < _Sections.Count; i++) {
        if(_Sections[i].Name == strSection) {
          List<string> aKeys = new List<string>();
          for(int j = 0; j < _Sections[i].keyCount; j++)
            aKeys.Add(_Sections[i].keys[j].Name);

          return aKeys;
        } else
          continue;
      }

      return null;
    }

    /// <summary>Creates a ini file</summary>
    /// <returns>true if created else false.</returns>
    public bool CreateNew() {
      if(_OK)
        return false;

      try {
        FileStream file = new FileStream(mstrPath, FileMode.Create, FileAccess.Write);
        file.Close();
      } catch { return false; }

      _OK = true;
      UpdateFile();

      return true;
    }

    ///<summary>Write buffer to file. But first of all it flushes the file.</summary>
    ///<param name="aBuf">Content which has to be written to file.</param>
    ///<returns>true if success else false</returns>
    private bool UpdateFile() {
      try {
        File.WriteAllText(mstrPath, "");
        TextWriter stWriter = new StreamWriter(mstrPath);
        if(stWriter == null)
          return false;

        for(int i = 0; i < _Sections.Count; i++) {
          Section sec = _Sections[i];
          if(sec.Comments.Count != 0)
            for(int j = 0; j < sec.Comments.Count; j++)
              stWriter.WriteLine(sec.Comments[j]);

          if(sec.Name.Length != 0) {
            if(i != 0)
              stWriter.WriteLine("");

            stWriter.Write("[");
            stWriter.Write(sec.Name);
            stWriter.WriteLine("]");
          }

          for(int j = 0; j < sec.keyCount; j++) {
            key k = sec.keys[j];
            if(k.Comments.Count != 0)
              for(int n = 0; n < k.Comments.Count; n++)
                stWriter.WriteLine(k.Comments[n]);

            stWriter.Write(k.Name);
            stWriter.Write("=");
            stWriter.WriteLine(k.Value);
          }
        }

        stWriter.Close();
      } catch { return false; }

      return true;
    }

    public bool OK => _OK;
    public List<Section> Sections => _Sections;
    private string mstrPath;
    private bool _OK;
    private List<Section> _Sections;
    private Window Owner = null;
  }
}
