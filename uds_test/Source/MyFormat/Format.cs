using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MyFormat
{
    static class Format
    {
        public static byte[] StringToHex(this string strings)
        {
            byte[] hex = new byte[0];
            try
            {
                strings = strings.Replace(" ", "");     //将原string中的空格删除
                strings = strings.Replace("0x", "");
                strings = strings.Replace("0X", "");
                strings = strings.Replace(",", "");
                if (strings.Length % 2 != 0)
                {
                    strings += "0";
                }
                hex = new byte[strings.Length / 2];
                for (int i = 0; i < hex.Length; i++)
                {
                    hex[i] = Convert.ToByte(strings.Substring(i * 2, 2), 16);
                }
                return hex;
            }
            catch
            {
                return hex;
            }
        }

        public static string HexToStrings(this byte[] hex, string space)
        {
            string strings = "";
            for (int i = 0; i < hex.Length; i++)//逐字节变为16进制字符，并以space隔开
            {
                strings += hex[i].ToString("X2") + space;
            }
            return strings;
        }

        public static string HexToStrings(this byte[] hex)
        {
            return HexToStrings(hex, "");
        }

        public static string RemoveUnwantedSpaces(this string strings)
        {
            strings = Regex.Replace(strings.Trim(), "\\s+", " ");
            return strings;
        }

        public static string InsertSpace(this string str, int lenght)
        {
            int i;
            string strings = "";
            for (i = 0; i < str.Length; i++)
            {
                strings += str[i];
                if (i % lenght != 0 && i != str.Length - 1)
                {
                    strings += " ";
                }
            }
            return strings;
        }

        public static string DtcEscape(this string strings)
        {
            if (strings.Length == 7)
            {
                return strings;
            }

            string code = string.Empty;
            byte dtc_code = Convert.ToByte(strings.Substring(0, 2), 16);
            if ((dtc_code & 0xC0) == 0x00)
            {
                code += "P";
            }
            else if ((dtc_code & 0xC0) == 0x40)
            {
                code += "C";
            }
            else if ((dtc_code & 0xC0) == 0x80)
            {
                code += "B";
            }
            else
            {
                code += "U";
            }

            code += (dtc_code & 0x3F).ToString("X2");
            code += strings.Substring(2, 4);

            return code;
        }
    }
}
