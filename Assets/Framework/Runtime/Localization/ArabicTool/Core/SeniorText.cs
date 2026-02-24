using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ArabicTool
{
  public class SeniorText : Text
  {
    private string m_orgText = "";

    public override string text
    {
      get => this.m_Text;
      set
      {
        string str = value ?? "";
        if (str.Equals(this.m_orgText))
          return;
        this.m_orgText = str;
        this.SetArabText(str);
      }
    }

    public void SetText(string str, float width = 0.0f, float height = 0.0f)
    {
      if (str == null)
        str = "";
      if (str.Equals(this.m_orgText))
        return;
      this.m_orgText = str;
      this.SetArabText(str, width, height);
    }

    public void SetArabText(string str, float width = 0.0f, float height = 0.0f)
    {
      if (str == null)
        str = "";
      if (ArabicTextTool.HasArabicChar(str))
        this.DoSetArabText(str, width, height);
      else
        this.SetRawText(str);
    }

    private void DoSetArabText(string orgStr, float width = 0.0f, float height = 0.0f)
    {
      string str = ArabicTextTool.Convert(orgStr);
      this.horizontalOverflow = HorizontalWrapMode.Wrap;
      VerticalWrapMode verticalOverflow = this.verticalOverflow;
      this.verticalOverflow = VerticalWrapMode.Overflow;
      TextGenerator textGenerator = new TextGenerator(str.Length);
      Vector2 extents = (double) width <= 0.0 || (double) height <= 0.0 ? this.GetComponent<RectTransform>().rect.size : new Vector2(width, height);
      if ((double) extents.x == 0 && (double) extents.y == 0)
      {
          Debug.LogError("[ArabicTool]Text rect is zero.");
      }
      
      if ((double) extents.x == 0)
      {
          extents = new Vector2(Screen.width, extents.y);
      }
      textGenerator.Populate(str, this.GetGenerationSettings(extents));
      UILineInfo[] linesArray = textGenerator.GetLinesArray();
      List<int> lineinfo = new List<int>();
      for (int index = 0; index < linesArray.Length; ++index)
      {
        int startCharIdx = linesArray[index].startCharIdx;
        int num = -1;
        if (index + 1 < linesArray.Length)
          num = linesArray[index + 1].startCharIdx - startCharIdx;
        if (num < 0)
          num = str.Length - startCharIdx;
        lineinfo.Add(num);
      }
      this.horizontalOverflow = HorizontalWrapMode.Overflow;
      this.verticalOverflow = verticalOverflow;
      this.SetRawText(ArabicTextTool.Reverse(str, ref lineinfo, true));
    }

    public void SetRawText(string str)
    {
      if (str == null)
        str = "";
      this.m_Text = str;
      SetVerticesDirty();
      SetLayoutDirty();
    }

    public string GetOrignalText() => this.m_orgText;
  }
}
