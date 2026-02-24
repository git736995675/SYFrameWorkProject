
using System;

namespace ArabicTool
{
    public class ArabicFixer
    {
        public static string Fix(string str, bool rtl = true) => ArabicFixer.Fix(str, true, false, rtl);

        public static string Fix(string str, bool showTashkeel, bool useHinduNumbers, bool rtl = true)
        {
            ArabicFixerTool.showTashkeel = showTashkeel;
            ArabicFixerTool.useHinduNumbers = useHinduNumbers;
            if (str.Contains("\n"))
                str = str.Replace("\n", Environment.NewLine);
            if (!str.Contains(Environment.NewLine))
                return ArabicFixerTool.FixLine(str, rtl);
            string[] separator = new string[1]
            {
                Environment.NewLine
            };
            string[] strArray = str.Split(separator, StringSplitOptions.None);
            if (strArray.Length == 0 || strArray.Length == 1)
                return ArabicFixerTool.FixLine(str, rtl);
            string str1 = ArabicFixerTool.FixLine(strArray[0], rtl);
            int index = 1;
            if (strArray.Length > 1)
            {
                for (; index < strArray.Length; ++index)
                    str1 = str1 + Environment.NewLine + ArabicFixerTool.FixLine(strArray[index], rtl);
            }
            return str1;
        }
    }
}