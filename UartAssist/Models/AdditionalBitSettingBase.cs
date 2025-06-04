using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UartAssist.Models
{
    public abstract class AdditionalBitSettingBase
    {
        public abstract string Name { get;  }       //校验算法名
        public abstract bool HeightBitFirst { get; set; }   //是否高字节在前
        public abstract bool HeightBitEnable { get; }

        public abstract bool IOC { get; }   //输入反转
        public abstract bool OOC { get; }   //输出反转
        public abstract int OxrResult { get; }//结果异或
        public abstract int InitValue { get;}  //初始值
        public abstract int Multinomial { get;  }//多项式

        public abstract int Index { get; set; }      //校验开始位置
        public abstract string? End { get; set; }    //指令结束符

        public abstract byte[] CRC(byte[] buf);  //自身携带一套算法

        public override string ToString() => Name;

    }
}
