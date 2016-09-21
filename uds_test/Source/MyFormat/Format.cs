using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uds_test.Source.MyFormat
{
    class Format
    {
        public byte[] string_to_hex(string strings)
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

        public string hex_to_string(byte[] hex, string space)
        {
            string strings = "";
            for (int i = 0; i < hex.Length; i++)//逐字节变为16进制字符，并以space隔开
            {
                strings += hex[i].ToString("X2") + space;
            }
            return strings;
        }

        public string hex_to_string(byte[] hex)
        {
            return hex_to_string(hex, "");
        }
    }
}
