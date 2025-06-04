using Microsoft.Win32;
using MyCustomControlLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using UartAssist.Models;
using UartAssist.Utils;
using UartAssist.Views;
using static System.Net.Mime.MediaTypeNames;

namespace UartAssist
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowEx, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private SerialPort? serialPort; //串口对象
        private readonly int[] BaudRates = { 600, 1200,2400, 4800, 9600, 19200, 38400,115200 }; //定义波特率（int）
        private readonly int[] DataBits = {5,6,7,8 };   //定义数据位,对于C#的串口而已，最后一位，由校验位决定，不由数据位决定

        //统计所有发送的数据
        private int txNum = 0;  
        public int TxNum { get => txNum; set { txNum = value; OnPropertyChanged(nameof(TxNum)); } }

        private int rxNum = 0;
        public int RxNum { get => rxNum; set { rxNum = value; OnPropertyChanged(nameof(RxNum)); } }

        private int txCount = 0;
        public int TxCount { get => txCount; set { txCount = value; OnPropertyChanged(nameof(TxCount)); } }
        private int rxCount = 0;
        public int RxCount { get => rxCount; set { rxCount = value; OnPropertyChanged(nameof(RxCount)); } }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this,new PropertyChangedEventArgs(propertyName));   
        }

        #region 00 - 构造和初始化
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 通常情况下，相关初始化操作，可以放在Loaded事件中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowEx_Loaded(object sender, RoutedEventArgs e)
        {
            //先加载所有的串口名
            CbSerialPorts.ItemsSource = SerialPort.GetPortNames();
            CbBaudRate.ItemsSource = BaudRates;
            CbBaudRate.SelectedItem = 115200;

            //把枚举类型，转化到ItemSource
            CbParity.ItemsSource = Enum.GetNames(typeof(Parity));
            CbDataBits.ItemsSource = DataBits;
            CbDataBits.SelectedItem = 8;

            CbStopBits.ItemsSource = Enum.GetNames(typeof(StopBits));
            CbStopBits.SelectedIndex = 1;

            CbHandshake.ItemsSource = Enum.GetNames(typeof(Handshake));
            CbHandshake.SelectedIndex = 0;

            RtbRxData.Document.LineHeight = 1;
        }

        /// <summary>
        /// 串口名ComboBox下来展开事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbSerialPorts_DropDownOpened(object sender, EventArgs e)
        {
            //需要重新加载所有串口
            CbSerialPorts.ItemsSource = SerialPort.GetPortNames();
        }

        #endregion

        #region 01 - 串口的设置

        /// <summary>
        /// 串口打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            //1.读取相关的配置参数
            string com = CbSerialPorts.Text;
            int baudRate = (int)CbBaudRate.SelectedItem;

            if (Enum.TryParse(CbParity.Text, out Parity parity))
            {
                int dataBits = (int)CbDataBits.SelectedItem;

                if (Enum.TryParse(CbStopBits.Text, out StopBits stopBits))
                {
                    if (Enum.TryParse(CbHandshake.Text, out Handshake handshake))
                    {
                        serialPort ??= new()
                        {
                            PortName = com,   //串口名
                            BaudRate = baudRate,//波特率
                            Parity = parity,  //校验位
                            DataBits = dataBits,    //数据位
                            StopBits = stopBits,  //停止位
                            Handshake = handshake,   //控制流
                        };

                        //对象有没有被占用
                        if (serialPort.IsOpen)
                        {
                            //提示串口已经被打开或者占用
                            try
                            {
                                serialPort.Close();
                                CbSerialPorts.IsEnabled = true;
                                BtnOpen.Content = this.Resources["SpBtnCanOpen"];
                                serialPort = null;  //释放原本的串口资源
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                        else
                        {
                            try
                            {
                                //注册接收委托
                                serialPort.DataReceived += SerialPort_DataReceived;

                                serialPort.Open();  //当打开的时候，就处于待定接收数据的状态了

                                //应当让按钮的内容发生变化
                                CbSerialPorts.IsEnabled = false;

                                BtnOpen.Content = this.Resources["SpBtnCanClose"];

                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(ex.Message);
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region 02 - 数据接收区设置

        /// <summary>
        /// 清空接收数据区
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearRxData_Click(object sender, RoutedEventArgs e)
        {
            PgRxData.Inlines.Clear();
        }

        #endregion

        #region 02 - 数据接收

        /// <summary>
        /// 选中是否保存到文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbRxSaveToFile_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                if (checkBox.IsChecked is true)
                {
                    //要保存到文件，选中要保存文件（或新建文件）
                    SaveFileDialog fileDialog = new()
                    {
                        Filter = "文本文件(Text)|*.txt"
                    };
                    if (fileDialog.ShowDialog() == true)
                    {
                        //SaveFileDialog 不会主动窗口File文件，只会根据输入的路径，返回文件的全路径名
                        checkBox.Tag = fileDialog.FileName;  //可以存在当前的CheckBox中的Tag中
                    }
                    else
                    {
                        checkBox.IsChecked = false;
                    }
                }
            }
        }

        /// <summary>
        /// 处理数据接收
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            //在接收的参数列表中，Event参数不会携带接收的数据
            if (sender is SerialPort serialPort)
            {
                int count = serialPort.BytesToRead; //这个数据长度，不代表对方发送的数据长度
                byte[] data = new byte[count];
                serialPort.Read(data, 0, count);

                RxNum += count; //统计接收的总字节数
                RxCount++;

                //串口的数据接收，是放在独立线程中的，不可以直接调用UI线程对象
                Dispatcher.Invoke(() =>
                {
                    string result = "";
                    if (RbRxAscii.IsChecked is true)
                    {
                        result = Encoding.Default.GetString(data, 0, data.Length);
                    }
                    else if (RbRxHex.IsChecked is true)
                    {
                        result = StringUtils.Hex2HexString(data);
                    }

                    if (CbRxLogModel.IsChecked is true) //按照日志接收模式进行接收
                    {
                        //需要处理发送和接收内容
                        string dateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff");  //[2022-11-12 13:37:31.921]# RECV ASCII>
                        string recvModel = "";
                        if (RbRxAscii.IsChecked is true)
                        {
                            recvModel = "ASCII";
                        }
                        else if (RbRxHex.IsChecked is true)
                        {
                            recvModel = "HEX";
                        }

                        string time = $"[{dateTime}]# RECV {recvModel}>{Environment.NewLine}";
                        result = $"{result}{Environment.NewLine}";

                        if (CbRxShowData.IsChecked != true) //是否在接收框中进行显示
                        {
                            PgRxData.Inlines.Add(new Run(time)   //系统换行，可以自动提取根据系统的需要
                            {
                                Foreground = Brushes.Gray
                            });

                            PgRxData.Inlines.Add(new Run(result)   //追加内容
                            {
                                Foreground = Brushes.Green
                            });
                        }

                        if (CbRxSaveToFile.IsChecked is true)
                        {
                            string fileName = (string)CbRxSaveToFile.Tag;

                            //利用文件流保存到对应的文件
                            if (!File.Exists(fileName))
                            {
                                FileStream fileStream = File.Create(fileName);
                                fileStream.Close();
                            }

                            using StreamWriter writer = new(fileName,true); //使用完毕之后，自动释放(追加操作)

                            writer.Write(time);
                            writer.Write(result);
                        }
                    }
                    else
                    {
                        if (CbRxShowData.IsChecked != true)//是否在接收框中进行显示
                        {
                            PgRxData.Inlines.Add(new Run(result) { Foreground = Brushes.Black, });
                        }

                        if (CbRxSaveToFile.IsChecked is true)
                        {
                            string fileName = (string)CbRxSaveToFile.Tag;

                            //利用文件流保存到对应的文件
                            if (!File.Exists(fileName))
                            {
                                FileStream fileStream = File.Create(fileName);
                                fileStream.Close();
                            }

                            using StreamWriter writer = new(fileName,true); //使用完毕之后，自动释放

                            writer.Write(result);
                        }
                    }

                    if (CbRxAutoReturn.IsChecked is true)   //选中自动换行
                    {
                        if (CbRxShowData.IsChecked != true)//是否在接收框中进行显示
                        {
                            PgRxData.Inlines.Add(new Run(Environment.NewLine));
                        }
                    }

                    //让RichTextBox滚动到最底部
                    if (CbRxShowData.IsChecked != true)//是否在接收框中进行显示
                    {
                        if (CbAutoScrollToEnd.IsChecked is true) RtbRxData.ScrollToEnd();
                    }

                });
            }
        }
        #endregion

        private System.Timers.Timer? timer;

        #region 03 - 数据发送区设置

        /// <summary>
        /// 清空发送数据区
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearTxData_Click(object sender, RoutedEventArgs e)
        {
            TbTxData.Text = "";
        }

        #region 03 -01 - 数据发送的处理过程

        /// <summary>
        /// 发送文本框文本文本改变事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TbTxData_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (RbTxHex.IsChecked != true) return;  //只有在16进制的时候，才进行判断

            if (sender is TextBox textBox)
            {
                var i = e.Changes.First();
                if (i.AddedLength > 0)   //在增加
                {
                    string str = textBox.Text.Substring(i.Offset, i.AddedLength);

                    if (Regex.IsMatch(str, "^[0-9a-fA-F\\s]+$")) return;    //判断符合要求，则不需要后续处理

                    //非16进制
                    textBox.Text = textBox.Text.Remove(i.Offset);
                    textBox.SelectionStart = i.Offset;

                    PopHexErrInfo.IsOpen = true;
                }
            }
        }

        #region ASCII和Hex两个RadioButton的被选中的事件

        private void TxAsciiHexTransform_Checked(object sender, RoutedEventArgs e)
        {
            if (TbTxData == null || CbTxSendFromFile == null) return;   //在初始化的时候，因为该对象还未完成初始化，就调用了选中事件

            if (CbTxSendFromFile.IsChecked == true) return; //如果从外部文件读取数据发送的情况下，不要去切换文本框中的内容

            if (string.IsNullOrEmpty(TbTxData.Text)) return;

            if (RbTxAscii.IsChecked is true)
            {
                //将输入的16进制字符串，转化成字符串
                TbTxData.Text = TbTxData.Text.HexToString();
            }
            else if (RbTxHex.IsChecked is true)
            {
                //将输入的内容，转化成16进制字符串
                TbTxData.Text = TbTxData.Text.StringToHex();
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// 打开附加位设置窗口
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbAdditional_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                //可以使用委托的方式，把需要传出的数据携带出来
                new AdditionalWindow((m, r) =>
                {
                    checkBox.Tag = m;
                    checkBox.IsChecked = r;
                }).ShowDialog();
            }
        }
        #endregion

        #region 03 - 数据发送
        /// <summary>
        /// 数据发送
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            if (serialPort == null || !serialPort.IsOpen)   //没有被打开
            {
                MessageBox.Show("发送失败，串口未连接或已拔出");
            }
            else
            {
                if (string.IsNullOrEmpty(TbTxData.Text))
                {
                    MessageBox.Show("没有发送内容");
                }
                else
                {
                    string txt=TbTxData.Text; //??
                    
                    if (CbTxSendFromFile.IsChecked == true) //从外部文档中读取文件
                    {
                        string fileName = (string)CbTxSendFromFile.Tag;
                        //以读取文本文件为主
                        using StreamReader reader = new(fileName);
                        txt = reader.ReadToEnd();  //全部读取，全部发送
                    }

                    if (CbCircleTime.IsChecked == true)
                    {
                        if (int.TryParse(TbCircleTime.Text, out int time))
                        {
                            if (BtnSend.Content.Equals("发送"))
                            {
                                //开启循环定时发送
                                timer = new()
                                {
                                    Interval = time
                                };

                                timer.Elapsed += (sender, e) =>
                                {
                                    //发送数据
                                    Dispatcher.Invoke(() =>
                                    {
                                        SendData(txt);
                                    });
                                };

                                TbTxData.IsEnabled = false;
                                BtnSend.Content = "停止发送";
                                timer.Start();  //开启定时器
                            }
                            else
                            {
                                if (timer != null) timer.Close();

                                BtnSend.Content = "发送";
                                TbTxData.IsEnabled = true;
                                timer = null;   //让垃圾回收器回收
                            }

                            
                        }
                        else
                        {
                            MessageBox.Show("输入的时间格式不正确！");
                        }
                    }
                    else
                    {
                        SendData(txt);
                    }
                }
            }
        }

        /// <summary>
        /// 读取文件，从文件中进行发送数据
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbTxSendFromFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                if (checkBox.IsChecked == true)
                {
                    //打开文件
                    OpenFileDialog openFileDialog = new();
                    if (openFileDialog.ShowDialog() == true)
                    {
                        string fileName = openFileDialog.FileName;
                        checkBox.Tag = fileName;

                        //拼接说明字符串
                        string info = $"启用外部文件数据源 (839 Bytes):\r\n{fileName} ...";
                        TbTxData.Text = info;
                        TbTxData.IsReadOnly = true; //注意，处于只读状态，是可以选中内容，但是不能编辑
                    }
                    else
                    {
                        checkBox.IsChecked = false; //未打开任何文件的时候，取消勾选
                    }
                }
                else
                {
                    TbTxData.IsReadOnly = false;
                    TbTxData.Text = "";
                }
            }
        }

        private void SendData(string txt)
        {
            if(serialPort==null|| !serialPort.IsOpen) { return; }
            byte[]? buf = null;

            if (RbTxAscii.IsChecked is true)    //如果使用ASCII格式
            {
                buf = System.Text.Encoding.Default.GetBytes(txt);      //做转码
                //serialPort.Write("你好");//注意编码集（C#默认是UTF-8）
            }
            else if (RbTxHex.IsChecked is true)
            {
                buf = StringUtils.HexStr2Bytes(txt);
            }

            if (buf != null)
            {
                if (CbAdditional.IsChecked == true)
                {
                    //需要对发送的buf进行追加位处理
                    AdditionalBitSettingBase additionalBitSettingBase = (AdditionalBitSettingBase)CbAdditional.Tag;
                    byte[] crc = additionalBitSettingBase.CRC(buf);

                    //重新处理新发送的数据
                    List<byte> data = new(buf);
                    data.AddRange(crc);
                    buf = data.ToArray();

                    if (RbTxHex.IsChecked is true)
                    {
                        txt = StringUtils.Hex2HexString(buf);
                    }
                }

                serialPort.Write(buf, 0, buf.Length);       //串口发送数据

                //执行数据统计
                TxNum += buf.Length;
                TxCount++;  //每发送一次，统计加一

                if (CbRxLogModel.IsChecked is true) //按照日志接收模式进行接收
                {
                    //[2022-11-12 13:37:31.921]# RECV ASCII>
                    string dateTime = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff"); 
                    string sendModel = "";
                    if (RbTxAscii.IsChecked is true)
                    {
                        sendModel = "ASCII";
                    }
                    else if (RbTxHex.IsChecked is true)
                    {
                        sendModel = "HEX";
                    }

                    //先添加日志事件
                    PgRxData.Inlines.Add(new Run($"[{dateTime}]# SEND {sendModel}>{Environment.NewLine}")
                    {
                        Foreground = Brushes.Gray
                    });

                    //再添加信息
                    PgRxData.Inlines.Add(new Run($"{txt}{Environment.NewLine}")
                    {
                        Foreground = Brushes.Blue
                    });

                    //根据需要选中是不是换行
                    if (CbRxAutoReturn.IsChecked is true)   //空白行
                    {
                        //注意空白行
                        PgRxData.Inlines.Add(new Run(Environment.NewLine)    
                        {
                            Foreground = Brushes.Black
                        });
                    }

                    //是否自动滚屏(这个地方没有直接判断false是因为可能IsChecked为null)
                    if (CbAutoScrollToEnd.IsChecked is true) RtbRxData.ScrollToEnd();
                }
            }
        }

        #endregion

        #region Hyperlink点击事件
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }

        #endregion

        /// <summary>
        /// 清空数据统计
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClearCount_Click(object sender, RoutedEventArgs e)
        {
            TxNum = TxCount = RxNum = RxCount = 0;  //清空所有统计
        }
    }
}
