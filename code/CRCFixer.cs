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
 * CRCFixer.cs - Fixes article checksum and reads article ID.
 * 
 */


using System;

namespace FTXFixer.code {
  /// <summary> Fixes specified line containing accession number with correct checksum </summary>
  class CRCFixer {
    /// <summary> Default constructor </summary>
    public CRCFixer() { }

    /// <summary> Calculates and returns CRC value against passed string </summary>
    /// <param name="str"> string on which CRC to be computed </param>
    /// <returns> CRC value </returns>
    private string GetCRC(string str) {
      int sum = 0;
      int n = str.Length + 1;
      string checkDigit = "";
      try {
        for(int i = 0; i < str.Length; i++) {
          string _digit = str.Substring(i, 1);
          int digit = Int32.Parse(_digit);
          int _sum = digit * n;
          sum = sum + _sum;
          n--;
        }

        int remainder = (int)Math.IEEERemainder(sum, 11);
        while(remainder < 0)
          remainder = remainder + 11;

        if(remainder == 1)
          checkDigit = "X";
        else if(remainder == 0 | remainder == 11)
          checkDigit = "0";
        else
          checkDigit = "" + (11 - remainder);

      } catch(Exception ex) {
        checkDigit = ex.Message;
        return checkDigit;
      }
      return checkDigit;
    }

    /// <summary> Reads line with pattern and returns fixed data </summary>
    /// <param name="strLine"> input line to be fixed </param>
    /// <param name="strPat"> header pattern to be searched </param>
    /// <param name="artID"> id of article is set during processing </param>
    /// <returns> original line with fixed checksum </returns>
    public Tuple<string, long> FixACNLine(string strLine, string strPat) {
      string strFixedLine = "";
      long artID = 0;
      int replaceCount = strPat.Length;
      strFixedLine = strLine.Remove(0, replaceCount);
      string[] splitStr = strFixedLine.Split(new char[] { ' ' });
      if(splitStr.Length > 1) {
        int AnWithCRCLen = splitStr[0].Length;
        strFixedLine = splitStr[0].Remove(AnWithCRCLen - 1, 1);
        if(strFixedLine.Length > 0)
          while(strFixedLine[0] == '@')
            strFixedLine = strFixedLine.Remove(0, 1);

        string FixedCRC = GetCRC(strFixedLine);
        long.TryParse(strFixedLine, out artID);
        if(FixedCRC.Length > 1)
          return Tuple.Create("", artID);

        strFixedLine = NewHeader + strFixedLine + FixedCRC;
        for(int i = 1; i < splitStr.Length; i++)
          if(splitStr[i].Length == 0)
            strFixedLine += " ";
          else
            strFixedLine = strFixedLine + " " + splitStr[i];
      } else
        strFixedLine = "";

      return Tuple.Create(strFixedLine, artID);
    }

    private static readonly string NewHeader = "@@@@@@@";
  }
}
