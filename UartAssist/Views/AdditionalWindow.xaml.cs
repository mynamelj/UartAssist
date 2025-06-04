using MyCustomControlLib;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using UartAssist.Models;
using UartAssist.Utils;

namespace UartAssist.Views
{
    public delegate void AdditionalActionDelegate(AdditionalBitSettingBase? model,bool result=false);

    /// <summary>
    /// AdditionalWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AdditionalWindow : WindowEx
    {
        private readonly List<AdditionalBitSettingBase> models = new()
        {
            new AdditonalCRC8Ver(),
            new AdditonalModbusVer()
        };

        //委托，可以将数据携带出去
        private AdditionalActionDelegate additionalAction;

        private bool result=false;
        private AdditionalBitSettingBase? model;

        public AdditionalWindow(AdditionalActionDelegate additionalAction)
        {
            InitializeComponent();
            this.additionalAction = additionalAction;
        }

        /// <summary>
        /// 初始化加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowEx_Loaded(object sender, RoutedEventArgs e)
        {
            CbVers.ItemsSource= models;
        }

        /// <summary>
        /// 选中改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CbVers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo) this.DataContext = combo.SelectedItem;
        }

        /// <summary>
        /// 取消按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            model = null;
            result = false;
            this.Close();
        }
        
        /// <summary>
        /// 确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            model = (AdditionalBitSettingBase)this.DataContext;
            result=true;
            this.Close();
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            additionalAction?.Invoke(model, result);
        }

    }
}
