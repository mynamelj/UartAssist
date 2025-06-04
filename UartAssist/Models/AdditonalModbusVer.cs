using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UartAssist.Utils;

namespace UartAssist.Models
{
    public class AdditonalModbusVer : AdditionalBitSettingBase
    {
        public override string Name { get => "CRC-16/MODBUS"; }

        public override bool HeightBitFirst { get; set; } = true;  //默认值

        public override int Index { get; set; } = 0;

        public override string? End { get; set; } = "";

        public override bool IOC => true;

        public override bool OOC => true;

        public override int OxrResult => 0x0000;

        public override int InitValue => 0xFFFF;

        public override int Multinomial => 0x8005;

        public override bool HeightBitEnable => true;

        public override byte[] CRC(byte[] buf)
        {
            //校验的算法
            byte[] result = new byte[2];
            ushort crc = CrcUtils.CRC16(buf);
            if (HeightBitFirst == true)
            {
                result[0] = (byte)((crc >> 8) & 0xFF);
                result[1] = (byte)((crc >> 0) & 0xFF);
            }
            else
            {
                result[0] = (byte)((crc >> 0) & 0xFF);
                result[1] = (byte)((crc >> 8) & 0xFF);
            }
            return result;
        }
    }
}
