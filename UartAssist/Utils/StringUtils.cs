using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UartAssist.Utils
{
    public static class StringUtils
    {
        #region 字符串转化成16进制格式
        public static string StringToHex(this string str)
        {
            return Str2HexString(str);
        }

        public static string Hex2HexString(byte[] buf)
        {
            StringBuilder sb = new();
            //将buf以字符串的形式，展示到TextBox中
            for (int i = 0; i < buf.Length; i++)
            {
                sb.Append(buf[i].ToString("X2"));
                sb.Append(' ');
            }
            return sb.ToString().Trim();
        }

        /// <summary>
        /// 将字符串，转化成16进制编码格式的字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static string Str2HexString(string str)
        {
            byte[] buf = Encoding.Default.GetBytes(str, 0, str.Length);  //编码转化

            return Hex2HexString(buf);
        } 
        #endregion

        #region 16进制格式转化成字符串
        public static string HexToString(this string hString)
        {
            return HexStr2String(hString);
        }

        //1.16进制格式的字符串，转化成真正的字符串
        private static string HexStr2String(string hString)
        {
            byte[] buf = HexStr2Bytes(hString);
            return Encoding.Default.GetString(buf, 0, buf.Length);  //使用默认编码集（GB2312的）
        }

        /// <summary>
        /// 将16进制格式的字符串，转化成byte[]
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] HexStr2Bytes(string str)
        {
            str = new Regex("[\\s]+").Replace(str.Trim(), " ");    //正则替代,前后空格都需要清掉

            if (string.IsNullOrEmpty(str)) { return Array.Empty<byte>(); }   //防止出现是空内容
            
            //判断字符中，是否包括非16进制字符[0-F]
            if (!Regex.IsMatch(str, "^[0-9a-fA-F\\s]+$")) return Array.Empty<byte>();

            //将内容，先转化成16进制的byte,再发送(在无法决定具体的空格问题，最好是，每2个字符拼合在一起)
            string[] strs = str.Split(" ");
            List<byte> bytes = new();

            for (int i = 0; i < strs.Length; i++)
            {
                if (strs[i].Length > 2) //不符合标准的
                {
                    int len = strs[i].Length / 2;   //算应该有多少个字符
                    if (strs[i].Length % 2 != 0)    //如果是单数
                    {
                        len += 1;
                    }

                    for (int j = 0; j < len; j++)
                    {
                        byte b;

                        if (j == len - 1 && strs[i].Length % 2 != 0)
                        {
                            b = Convert.ToByte(strs[i].Substring(j * 2, 1), 16);
                        }
                        else
                        {
                            b = Convert.ToByte(strs[i].Substring(j * 2, 2), 16);
                        }

                        bytes.Add(b);
                    }
                }
                else
                {
                    bytes.Add(Convert.ToByte(strs[i], 16));
                }
            }
            return bytes.ToArray();
        } 
        #endregion
    }
}
