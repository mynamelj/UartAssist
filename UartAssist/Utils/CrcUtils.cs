using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace UartAssist.Utils
{
    public class CrcUtils
    {
        /// <summary>
        /// 移植的CRC 16 校验算法
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static ushort CRC16(byte[] buf)
        {
            ushort crc = 0xFFFF;

            if (buf == null) return 0x00;

            foreach (byte item in buf)
            {
                crc ^= item;
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 1) == 0x01)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                        crc >>= 1;
                }
            }

            return crc;
        }


        /// <summary>
        /// CRC-8校验算法（未经过验证）
        /// </summary>
        /// <param name="buf"></param>
        /// <returns></returns>
        public static byte CRC8(byte[] buf)
        {
            if (buf == null) return 0;

            byte crc = 0;
            for (int j = 0; j < buf.Length; j++)
            {
                crc ^= buf[j];
                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x01) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0x8c;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }
    }
}
