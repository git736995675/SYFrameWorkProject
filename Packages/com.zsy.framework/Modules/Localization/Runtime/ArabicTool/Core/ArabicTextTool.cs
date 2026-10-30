using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ZSYFramework.Core.Json;


namespace ZSYFramework.Localization.Arabic
{
  public class ArabicTextTool
  {
private static int INIT_STRING_LENGTH = 128;
private static List<TagSt> sTagList;
private static bool bIsInited = false;

public static void SetNewStringCapacity(int capacity) => ArabicTextTool.INIT_STRING_LENGTH = capacity;

public static string ReverseString(string input)
{
  char[] charArray = input.ToCharArray();
  Array.Reverse((Array) charArray);
  return new string(charArray);
}

public static bool IsArabicChar(char code) => code >= '\u0600' && code <= 'ۿ' || code >= 'ݐ' && code <= 'ݿ' || code >= 'ﭐ' && code <= '﷿' || code >= 'ﹰ' && code <= '\uFEFF';

public static bool HasArabicChar(string str)
{
  foreach (char code in str)
  {
    if (ArabicTextTool.IsArabicChar(code))
      return true;
  }
  return false;
}

private static List<TagSt> TagList
{
  get
  {
    if (ArabicTextTool.sTagList == null)
      ArabicTextTool.Init();
    return ArabicTextTool.sTagList;
  }
}

private static void Init()
{
  if (ArabicTextTool.bIsInited)
    return;
  ArabicTextTool.sTagList = new List<TagSt>()
  {
    new TagSt("<size=", "</size>", -1, 0, -1),
    new TagSt("<color=", "</color>", -1, 0, -1),
    new TagSt("<b>", "</b>", -1, 0, -1),
    new TagSt("<i>", "</i>", -1, 0, -1)
  };
  ArabicTextTool.bIsInited = true;
}

public static bool AddCustomTag(string startTag, string endTag)
{
  if (startTag.IndexOf("<") != 0 || endTag.IndexOf("</") != 0 || endTag[endTag.Length - 1] != '>')
  {
    Debug.LogError((object) "[ArabicTool]Wrong tag style.");
    return false;
  }
  for (int index = 0; index < ArabicTextTool.TagList.Count; ++index)
  {
    TagSt tag = ArabicTextTool.TagList[index];
    if (tag.end.IndexOf(endTag) >= 0)
    {
      Debug.LogError((object) "[ArabicTool]The end tag already exist.");
      return false;
    }
    if (tag.start.IndexOf(startTag) >= 0)
    {
      Debug.LogError((object) "[ArabicTool]The start tag already exist.");
      return false;
    }
  }
  ArabicTextTool.TagList.Add(new TagSt(startTag, endTag, -1, 0, -1));
  return true;
}

private static bool FindMatchEndTag(ref List<string> lineList, ref TagSt tag, int startLine)
{
  for (int index = startLine; index < lineList.Count; ++index)
  {
    string str = lineList[index];
    int num1 = index == startLine ? tag.pos : 0;
    string end = tag.end;
    int startIndex = num1;
    int num2 = str.IndexOf(end, startIndex);
    if (num2 >= 0)
    {
      tag.pos = num2;
      tag.line = index;
      tag.len = tag.end.Length;
      break;
    }
  }
  return tag.line >= 0;
}

private static bool IsStartTag(ref string str)
{
  for (int index = 0; index < ArabicTextTool.TagList.Count; ++index)
  {
    TagSt tag = ArabicTextTool.TagList[index];
    if (str.IndexOf(tag.start) == 0 && str.IndexOf(">") == str.Length - 1)
      return true;
  }
  return false;
}

private static bool IsEndTag(ref string str)
{
  for (int index = 0; index < ArabicTextTool.TagList.Count; ++index)
  {
    TagSt tag = ArabicTextTool.TagList[index];
    if (str.Equals(tag.end))
      return true;
  }
  return false;
}

private static bool FindFirstStartTagInLine(ref string str, int startPos, ref TagSt outTag)
{
  int num1 = -1;
  for (int index = 0; index < ArabicTextTool.TagList.Count; ++index)
  {
    TagSt tag = ArabicTextTool.TagList[index];
    int startIndex = str.IndexOf(tag.start, startPos);
    if (startIndex >= 0)
    {
      int num2 = str.IndexOf(">", startIndex);
      if (num2 >= 0 && (startIndex < num1 || -1 == num1))
      {
        num1 = startIndex;
        outTag.start = str.Substring(startIndex, num2 - startIndex + 1);
        outTag.end = tag.end;
        outTag.pos = startIndex;
        outTag.len = num2 - startIndex + 1;
      }
    }
  }
  return num1 >= 0;
}

public static bool FindNextTag(
  ref string str,
  int startPos,
  ref TagSt outTag,
  ref TagType tagType)
{
  int num;
  for (int startIndex1 = startPos; startIndex1 < str.Length; startIndex1 = num + 1)
  {
    int startIndex2 = str.IndexOf("<", startIndex1);
    num = str.IndexOf(">", startIndex1);
    if (startIndex2 < 0 || num < 0)
      return false;
    for (int index = 0; index < ArabicTextTool.TagList.Count; ++index)
    {
      TagSt tag = ArabicTextTool.TagList[index];
      if (str.IndexOf(tag.start, startIndex1) == startIndex2)
      {
        outTag.start = str.Substring(startIndex2, num - startIndex2 + 1);
        outTag.end = tag.end;
        outTag.pos = startIndex2;
        outTag.len = num - startIndex2 + 1;
        tagType = TagType.TAG_TYPE_START;
        return true;
      }
      if (str.IndexOf(tag.end, startIndex1) == startIndex2)
      {
        outTag.start = tag.start;
        outTag.end = tag.end;
        outTag.pos = startIndex2;
        outTag.len = num - startIndex2 + 1;
        tagType = TagType.TAG_TYPE_END;
        return true;
      }
    }
  }
  return false;
}

private static void PreDealTag(ref List<string> lineList)
{
  List<TagSt> tagStList = new List<TagSt>();
  for (int index1 = 0; index1 < lineList.Count; ++index1)
  {
    int startPos = 0;
    string str1 = lineList[index1];
    int length = str1.Length;
    StringBuilder stringBuilder = new StringBuilder(ArabicTextTool.INIT_STRING_LENGTH);
    stringBuilder.Append(str1);
    do
    {
      TagSt outTag = new TagSt();
      TagType tagType = TagType.TAG_TYPE_UNKNOW;
      if (ArabicTextTool.FindNextTag(ref str1, startPos, ref outTag, ref tagType))
      {
        startPos = outTag.pos + outTag.len;
        outTag.line = index1;
        if (tagType == TagType.TAG_TYPE_START)
          tagStList.Add(outTag);
        else if (TagType.TAG_TYPE_END == tagType)
        {
          if (tagStList.Count > 0)
          {
            TagSt tagSt = tagStList[tagStList.Count - 1];
            if (!tagSt.end.Equals(outTag.end))
            {
              Debug.LogError((object) string.Format("[ArabicTool]The start tag {0} do not match end tag {1}.\n", (object) tagSt.start, (object) outTag.end));
            }
            else
            {
              tagStList.RemoveAt(tagStList.Count - 1);
              if (tagSt.line != outTag.line)
                stringBuilder.Insert(0, tagSt.start);
            }
          }
          else
            Debug.LogError((object) string.Format("[ArabicTool]The end tag[{0}] do not have start tag.\n", (object) outTag.end));
        }
        else
          Debug.LogError((object) "[ArabicTool]Unknow error when find next tag.\n");
      }
      else
        break;
    }
    while (startPos < length);
    if (tagStList.Count > 0)
    {
      string str2 = "";
      string str3 = "";
      for (int index2 = 0; index2 < tagStList.Count; ++index2)
      {
        if (index1 > tagStList[index2].line)
          str2 += tagStList[index2].start;
        if (index1 >= tagStList[index2].line)
          str3 = tagStList[index2].end + str3;
      }
      stringBuilder.Insert(0, str2);
      stringBuilder.Append(str3);
    }
    lineList[index1] = stringBuilder.ToString();
  }
  if (tagStList.Count <= 0)
    return;
  Debug.LogError((object) string.Format("[ArabicTool]There have {0} tag not match.", (object) tagStList.Count));
  tagStList.Clear();
}

private static void SwapTag(ref List<string> list)
{
  List<TagSt> tagStList = new List<TagSt>();
  for (int index = 0; index < list.Count; ++index)
  {
    TagSt outTag = new TagSt();
    TagType tagType = TagType.TAG_TYPE_UNKNOW;
    string str1 = list[index];
    if (ArabicTextTool.FindNextTag(ref str1, 0, ref outTag, ref tagType))
    {
      if (outTag.pos == 0)
      {
        if (tagType == TagType.TAG_TYPE_START)
        {
          outTag.line = index;
          tagStList.Add(outTag);
        }
        else if (TagType.TAG_TYPE_END == tagType)
        {
          if (tagStList.Count > 0)
          {
            TagSt tagSt = tagStList[tagStList.Count - 1];
            if (!tagSt.end.Equals(outTag.end))
            {
              Debug.LogError((object) string.Format("[ArabicTool][SwapTag]The start tag {0} do not match end tag {1}.\n", (object) tagSt.start, (object) outTag.end));
            }
            else
            {
              tagStList.RemoveAt(tagStList.Count - 1);
              string str2 = list[tagSt.line];
              list[tagSt.line] = list[index];
              list[index] = str2;
            }
          }
          else
            Debug.LogError((object) string.Format("[ArabicTool][SwapTag]The end tag[{0}] do not have start tag.\n", (object) outTag.end));
        }
        else
          Debug.LogError((object) "[ArabicTool][SwapTag]Unknow error when find next tag.\n");
      }
      else if (outTag.pos > 0)
        Debug.LogError((object) "[ArabicTool][SwapTag]There have tag in block.\n");
    }
  }
  if (tagStList.Count <= 0)
    return;
  Debug.LogError((object) string.Format("[ArabicTool]There have {0} tag not match.", (object) tagStList.Count));
  tagStList.Clear();
}

private static string ReverseBlock(ref string text) => ArabicTextTool.ReverseString(text);

private static bool IsBeforePunctuation(char uc)
{
  if (uc <= '\'')
  {
    if (uc != '"' && uc != '\'')
      goto label_4;
  }
  else if (uc != '(' && uc != '[' && uc != '{')
    goto label_4;
  return true;
label_4:
  return false;
}

private static bool IsAfterPunctuation(char uc)
{
  if (uc <= ')')
  {
    if (uc != '"' && uc != '\'' && uc != ')')
      goto label_4;
  }
  else if (uc != ':' && uc != ']' && uc != '}')
    goto label_4;
  return true;
label_4:
  return false;
}

private static char ConvertPunctuation(char uc)
{
  switch (uc)
  {
    case '(':
      return ')';
    case ')':
      return '(';
    case '[':
      return ']';
    case ']':
      return '[';
    case '{':
      return '}';
    case '}':
      return '{';
    default:
      return uc;
  }
}

private static void GenerateBlockListOld(
  ref string text,
  ref List<string> retList,
  bool onlyForConvert = false)
{
  int index = 0;
  StringBuilder stringBuilder = new StringBuilder(ArabicTextTool.INIT_STRING_LENGTH);
  bool flag1 = false;
  bool flag2 = false;
  char ch;
  do
  {
    ch = text[index];
    ++index;
    if (!onlyForConvert)
    {
      bool flag3 = false;
      if ('.' == ch && !flag2 && index < text.Length)
      {
        char code = text[index];
        if (ArabicTextTool.IsArabicChar(code))
          flag3 = true;
        else if (' ' == code && index + 1 < text.Length && ArabicTextTool.IsArabicChar(text[index + 1]))
          flag3 = true;
      }
      if (flag3)
      {
        if (stringBuilder.Length > 0)
        {
          string str = stringBuilder.ToString();
          retList.Add(str);
          stringBuilder.Length = 0;
        }
        stringBuilder.Append(ch);
        if (stringBuilder.Length > 0)
        {
          string str = stringBuilder.ToString();
          retList.Add(str);
          stringBuilder.Length = 0;
        }
        flag1 = false;
        flag2 = false;
        goto label_29;
      }
    }
    bool flag4 = false;
    if (!onlyForConvert && ArabicTextTool.IsBeforePunctuation(ch))
    {
      if (index < text.Length && ArabicTextTool.IsArabicChar(text[index]))
      {
        ch = ArabicTextTool.ConvertPunctuation(ch);
        flag4 = true;
      }
    }
    else if (!onlyForConvert && ArabicTextTool.IsAfterPunctuation(ch) && flag2)
    {
      ch = ArabicTextTool.ConvertPunctuation(ch);
      flag4 = true;
    }
    if (ArabicTextTool.IsArabicChar(ch) | flag4)
    {
      if (!flag1)
      {
        if (stringBuilder.Length > 0)
        {
          string str = stringBuilder.ToString();
          retList.Add(str);
          stringBuilder.Length = 0;
        }
        flag1 = true;
      }
      stringBuilder.Append(ch);
      flag2 = true;
    }
    else
    {
      if (flag1)
      {
        if (stringBuilder.Length > 0)
        {
          string str = stringBuilder.ToString();
          retList.Add(str);
          stringBuilder.Length = 0;
        }
        flag1 = false;
      }
      stringBuilder.Append(ch);
      flag2 = false;
    }
label_29:;
  }
  while (index < text.Length && ch != char.MinValue);
  if (stringBuilder.Length <= 0)
    return;
  string str1 = stringBuilder.ToString();
  retList.Add(str1);
  stringBuilder.Length = 0;
}

private static bool IsArabicCharAfterSpace(ref string text, int startIdx)
{
  for (int index = startIdx; index < text.Length; ++index)
  {
    if (' ' != text[index])
      return ArabicTextTool.IsArabicChar(text[index]);
  }
  return false;
}

private static void GenerateBlockList(
  ref string text,
  ref List<string> retList,
  bool needSpacial,
  bool isLinkPunction,
  bool isConvertPunctuation)
{
  int num = 0;
  StringBuilder stringBuilder = new StringBuilder(ArabicTextTool.INIT_STRING_LENGTH);
  bool flag1 = false;
  bool flag2 = false;
  char ch;
  do
  {
    ch = text[num];
    ++num;
    bool flag3 = false;
    if (' ' == ch && ArabicTextTool.IsArabicCharAfterSpace(ref text, num))
      flag3 = true;
    else if (isLinkPunction & flag2 && ArabicTextTool.IsAfterPunctuation(ch))
    {
      if (isConvertPunctuation)
        ch = ArabicTextTool.ConvertPunctuation(ch);
      flag3 = true;
    }
    else if (isLinkPunction && ArabicTextTool.IsBeforePunctuation(ch) && num < text.Length && ArabicTextTool.IsArabicChar(text[num]))
    {
      if (isConvertPunctuation)
        ch = ArabicTextTool.ConvertPunctuation(ch);
      flag3 = true;
    }
    if (ArabicTextTool.IsArabicChar(ch) | flag3)
    {
      if (!flag1)
      {
        if (stringBuilder.Length > 0)
        {
          string str = stringBuilder.ToString();
          retList.Add(str);
          stringBuilder.Length = 0;
        }
        flag1 = true;
      }
      stringBuilder.Append(ch);
      flag2 = true;
    }
    else
    {
      if (flag1)
      {
        if (stringBuilder.Length > 0)
        {
          string str = stringBuilder.ToString();
          retList.Add(str);
          stringBuilder.Length = 0;
        }
        flag1 = false;
      }
      stringBuilder.Append(ch);
      flag2 = false;
    }
  }
  while (num < text.Length && ch != char.MinValue);
  if (stringBuilder.Length <= 0)
    return;
  string str1 = stringBuilder.ToString();
  retList.Add(str1);
  stringBuilder.Length = 0;
}

private static string ReverseSection(ref string section, bool rtl)
{
  StringBuilder stringBuilder = new StringBuilder(ArabicTextTool.INIT_STRING_LENGTH);
  List<string> retList = new List<string>();
  ArabicTextTool.GenerateBlockList(ref section, ref retList, false, true, true);
  for (int index = 0; index < retList.Count; ++index)
  {
    string text = retList[index];
    string str = !ArabicTextTool.HasArabicChar(text) ? text : ArabicTextTool.ReverseBlock(ref text);
    if (rtl)
      stringBuilder.Insert(0, str);
    else
      stringBuilder.Append(str);
  }
  return stringBuilder.ToString();
}

private static void GenerateSectionList(ref string text, ref List<string> retList)
{
  int startIndex1 = 0;
  int startIndex2 = 0;
  int length = text.Length;
  while (startIndex2 < length)
  {
    bool flag = false;
    int startIndex3 = text.IndexOf("<", startIndex2);
    int num = startIndex3;
    if (startIndex3 >= 0)
    {
      num = text.IndexOf(">", startIndex3);
      if (num >= 0)
      {
        for (int index = 0; index < ArabicTextTool.TagList.Count; ++index)
        {
          TagSt tag = ArabicTextTool.TagList[index];
          if (text.IndexOf(tag.start, startIndex3) == startIndex3)
          {
            flag = true;
            break;
          }
          if (text.IndexOf(tag.end, startIndex3) == startIndex3)
          {
            flag = true;
            break;
          }
        }
      }
    }
    if (flag)
    {
      if (startIndex1 < startIndex3)
      {
        string str = text.Substring(startIndex1, startIndex3 - startIndex1);
        retList.Add(str);
      }
      string str1 = text.Substring(startIndex3, num - startIndex3 + 1);
      retList.Add(str1);
      startIndex2 = num + 1;
      startIndex1 = startIndex2;
    }
    else
      ++startIndex2;
  }
  if (startIndex1 >= length)
    return;
  string str2 = text.Substring(startIndex1, length - startIndex1);
  retList.Add(str2);
}

private static void GenerateNodeList(ref string text, ref List<string> nodeList)
{
  List<string> retList1 = new List<string>();
  ArabicTextTool.GenerateSectionList(ref text, ref retList1);
  for (int index = 0; index < retList1.Count; ++index)
  {
    List<string> retList2 = new List<string>();
    string text1 = retList1[index];
    ArabicTextTool.GenerateBlockList(ref text1, ref retList2, true, true, false);
    nodeList.AddRange((IEnumerable<string>) retList2);
  }
}

private static void GenerateSentenceListByNodeList(
  ref List<string> nodeList,
  ref List<string> sentenceList)
{
  StringBuilder stringBuilder1 = new StringBuilder(ArabicTextTool.INIT_STRING_LENGTH);
  int num1 = -1;
  for (int index1 = 0; index1 < nodeList.Count; ++index1)
  {
    if (ArabicTextTool.HasArabicChar(nodeList[index1]))
    {
      int num2 = index1 - 1;
      int num3 = index1 + 1;
      for (int index2 = index1 - 1; index2 >= 0; --index2)
      {
        string str = nodeList[index2];
        if (ArabicTextTool.IsStartTag(ref str))
        {
          num2 = index2 - 1;
          stringBuilder1.Insert(0, str);
        }
        else
        {
          num2 = index2;
          break;
        }
      }
      stringBuilder1.Append(nodeList[index1]);
      for (int index3 = index1 + 1; index3 < nodeList.Count; ++index3)
      {
        string str = nodeList[index3];
        if (ArabicTextTool.IsEndTag(ref str))
        {
          num3 = index3 + 1;
          stringBuilder1.Append(str);
        }
        else
        {
          num3 = index3;
          break;
        }
      }
      if (num2 > num1 && num1 + 1 >= 0 && num1 + 1 < nodeList.Count && num2 >= 0 && num2 < nodeList.Count)
      {
        string str = "";
        for (int index4 = num1 + 1; index4 <= num2; ++index4)
          str += nodeList[index4];
        sentenceList.Add(str);
      }
      sentenceList.Add(stringBuilder1.ToString());
      stringBuilder1.Length = 0;
      num1 = num3 - 1;
      index1 = num3;
    }
  }
  if (num1 >= nodeList.Count - 1)
    return;
  StringBuilder stringBuilder2 = new StringBuilder(ArabicTextTool.INIT_STRING_LENGTH);
  for (int index = num1 + 1; index < nodeList.Count; ++index)
    stringBuilder2.Append(nodeList[index]);
  sentenceList.Add(stringBuilder2.ToString());
}

private static string DealSpacialLine(string text)
{
  bool flag1 = false;
  bool flag2 = false;
  string str1 = text.TrimEnd();
  if (str1.Length > 1 && '.' == str1[str1.Length - 1])
  {
    string str2 = "";
    for (int index = str1.Length - 2; index >= 0; --index)
    {
      char code = str1[index];
      switch (code)
      {
        case '0':
        case '1':
        case '2':
        case '3':
        case '4':
        case '5':
        case '6':
        case '7':
        case '8':
        case '9':
          if (!flag2)
          {
            str2 = code.ToString() + str2;
            break;
          }
          break;
        default:
          flag2 = true;
          if (code != ' ')
          {
            if (ArabicTextTool.IsArabicChar(code))
            {
              flag1 = true;
              goto label_10;
            }
            else
              goto label_10;
          }
          else
            break;
      }
    }
label_10:
    if (str2.Length > 0 & flag1)
    {
      int length = text.LastIndexOf(str2);
      string str3 = text.Substring(0, length);
      string str4 = "";
      if (text.Length > str1.Length)
        str4 = text.Substring(length + str2.Length + 1, text.Length - str1.Length);
      string str5 = str4;
      string str6 = str2;
      return str3 + str5 + "." + str6;
    }
  }
  return text;
}

public static string Reverse(string text, ref List<int> lineinfo, bool rtl)
{
  StringBuilder stringBuilder1 = new StringBuilder(ArabicTextTool.INIT_STRING_LENGTH);
  if (!text.Equals("") && lineinfo.Count > 0)
  {
    List<string> lineList = new List<string>();
    bool flag1 = lineinfo.Count == 1;
    int startIndex1 = 0;
    for (int index = 0; index < lineinfo.Count; ++index)
    {
      int length = lineinfo[index];
      if (length > text.Length - startIndex1)
      {
        Debug.LogError((object) ("[ArabicTool]length is big:" + (object) length + " text.Length:" + (object) (text.Length - startIndex1) + " text total: " + (object) text.Length));
        length = text.Length - startIndex1;
      }
      string str = text.Substring(startIndex1, length);
      if (str.Length > 1 && str[str.Length - 1] == '\n')
        str = str.Substring(0, str.Length - 1);
      lineList.Add(str);
      startIndex1 += length;
    }
    try
    {
      ArabicTextTool.PreDealTag(ref lineList);
    }
    catch (Exception ex)
    {
      Debug.LogError((object) "[ArabicTool]PreDealTag exception.\n");
    }
    for (int index1 = 0; index1 < lineList.Count; ++index1)
    {
      string text1 = lineList[index1];
      StringBuilder stringBuilder2 = new StringBuilder(ArabicTextTool.INIT_STRING_LENGTH);
      List<string> nodeList = new List<string>();
      ArabicTextTool.GenerateNodeList(ref text1, ref nodeList);
      List<string> stringList1 = new List<string>();
      ArabicTextTool.GenerateSentenceListByNodeList(ref nodeList, ref stringList1);
      try
      {
        ArabicTextTool.PreDealTag(ref stringList1);
      }
      catch (Exception ex)
      {
        Debug.LogError((object) "[ArabicTool]PreDealTag exception when for sentence list.\n");
      }
      bool flag2 = false;
      for (int index2 = 0; index2 < stringList1.Count; ++index2)
      {
        string text2 = stringList1[index2];
        StringBuilder stringBuilder3 = new StringBuilder(ArabicTextTool.INIT_STRING_LENGTH);
        stringBuilder3.Append(text2);
        if (ArabicTextTool.HasArabicChar(text2))
        {
          stringBuilder3.Length = 0;
          List<string> stringList2 = new List<string>();
          ArabicTextTool.GenerateSectionList(ref text2, ref stringList2);
          if (rtl)
          {
            try
            {
              ArabicTextTool.SwapTag(ref stringList2);
            }
            catch (Exception ex)
            {
              Debug.LogError((object) "[ArabicTool]SwapTag exception when for sentence list.\n");
            }
          }
          for (int index3 = 0; index3 < stringList2.Count; ++index3)
          {
            string section = stringList2[index3];
            string str = ArabicTextTool.ReverseSection(ref section, rtl);
            if (rtl)
              stringBuilder3.Insert(0, str);
            else
              stringBuilder3.Append(str);
          }
          flag2 = true;
        }
        // else
        // {
        //   if (rtl & flag2)
        //   {
        //     string str1 = stringBuilder3.ToString();
        //     string str2 = str1.Trim();
        //     string str3 = "";
        //     string str4 = "";
        //     if (str1.StartsWith(" "))
        //       str3 = str1.Substring(0, str1.IndexOf(str2));
        //     if (str1.EndsWith(" "))
        //     {
        //       int startIndex2 = str1.IndexOf(str2) + str2.Length;
        //       str4 = str1.Substring(startIndex2, str1.Length - startIndex2);
        //     }
        //     if (str2.Length > 0)
        //     {
        //       int startIndex3 = str2.Length - 1;
        //       if ('.' == str2[str2.Length - 1])
        //         str2 = "." + str2.Remove(startIndex3, 1);
        //       else if ('!' == str2[str2.Length - 1])
        //         str2 = "!" + str2.Remove(startIndex3, 1);
        //     }
        //     if (flag2)
        //     {
        //       stringBuilder3.Append(str4);
        //       stringBuilder3.Append(str2);
        //       stringBuilder3.Append(str3);
        //     }
        //   }
        //   flag2 = false;
        // }
        if (rtl)
          stringBuilder2.Insert(0, (object) stringBuilder3);
        else
          stringBuilder2.Append((object) stringBuilder3);
      }
      string text3 = stringBuilder2.ToString();
      stringBuilder2.Length = 0;
      stringBuilder2.Append(ArabicTextTool.DealSpacialLine(text3));
      // if (!flag1 && index1 != lineList.Count - 1)
      //   stringBuilder2.Append("\n");
      stringBuilder1.Append((object) stringBuilder2);
    }
  }
  return stringBuilder1.ToString();
}

public static string ReverseOnly(string orgStr, string lineInfo)
{
  string[] strArray = lineInfo.Split('|');
  List<int> lineinfo = new List<int>();
  if (strArray.Length != 0)
  {
    for (int index = 0; index < strArray.Length; ++index)
    {
      int result = 0;
      int.TryParse(strArray[index], out result);
      lineinfo.Add(result);
    }
  }
  else
    lineinfo.Add(orgStr.Length);
  return ArabicTextTool.Reverse(orgStr, ref lineinfo, true);
}

public static string ReverseOnly(string orgStr)
{
  if (orgStr == null)
    orgStr = "";
  string[] strArray = orgStr.Split('\n');
  List<int> lineinfo = new List<int>();
  if (strArray.Length != 0)
  {
    for (int index = 0; index < strArray.Length; ++index)
    {
      int length = strArray[index].Length;
      if (index < strArray.Length - 1)
        ++length;
      lineinfo.Add(length);
    }
  }
  else
    lineinfo.Add(orgStr.Length);
  return ArabicTextTool.Reverse(orgStr, ref lineinfo, true);
}

public static string Convert(string text)
{
  if (text == null)
    text = "";
  StringBuilder stringBuilder = new StringBuilder(ArabicTextTool.INIT_STRING_LENGTH);
  if (text.Length > 0 && ArabicTextTool.HasArabicChar(text))
  {
    List<string> retList = new List<string>();
    ArabicTextTool.GenerateBlockList(ref text, ref retList, false, false, false);
    for (int index = 0; index < retList.Count; ++index)
    {
      string str1 = retList[index];
      string str2 = !ArabicTextTool.HasArabicChar(str1) ? str1 : ArabicFixer.Fix(str1, false);
      stringBuilder.Append(str2);
    }
  }
  return stringBuilder.ToString();
}
  }
}
