using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UartAssist.Utils;

namespace UartAssist.Models
{
    public class AdditonalCRC8Ver : AdditionalBitSettingBase
    {
        public override string Name { get => "CRC-8"; }

        public override bool HeightBitFirst { get; set; } = false;  //默认值
        public override bool HeightBitEnable => false;  //不可用高低位

        public override int Index { get; set; } = 0;

        public override string? End { get; set; } = "";

        public override bool IOC => false;

        public override bool OOC => false;

        public override int OxrResult => 0x00;

        public override int InitValue => 0x00;

        public override int Multinomial => 0x07;

        public override byte[] CRC(byte[] buf)
        {
            //校验的算法
            byte[] result = new byte[1] { CrcUtils.CRC8(buf) };
            return result;
        }
    }
}
