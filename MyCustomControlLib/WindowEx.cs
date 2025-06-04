using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace MyCustomControlLib
{
    public enum WindowModel{
        Normal,
        Dialog
    }


    public class WindowEx : Window
    {

        private ResourceDictionary resource = new()
        {
            Source = new Uri("pack://application:,,,/MyCustomControlLib;component/Themes/WindowStyles.xaml", UriKind.Absolute)
        };

        //自定义属性
        public static readonly DependencyProperty MenuProperty;

        //用于控制窗口模式
        public static readonly DependencyProperty WindowModelProperty;

        public WindowModel WindowModel
        {
            get => (WindowModel)GetValue(WindowModelProperty);
            set { SetValue(WindowModelProperty,value); }
        }
        //private object? menu;
        public object? Menu { get { return GetValue(MenuProperty); } set { SetValue(MenuProperty, value); } }

        #region 001 - 初始化

        /// <summary>
        /// 使用，自定义控件的方式，定义Window，可以不重复的加载style到APP.xaml
        /// </summary>
        public WindowEx()
        {
            //自动到资源模板中去查找
            this.Style = resource["UartAssistWindowStyle"] as Style;
        }

        //初始模板
        static WindowEx()
        {
            //不使用以默认的样式进行初始化Style

            //依赖项属性
            MenuProperty = DependencyProperty.Register("Menu", typeof(object), typeof(WindowEx));
            WindowModelProperty = DependencyProperty.Register("WindowModel", typeof(WindowModel), typeof(WindowEx),new FrameworkPropertyMetadata(WindowModel.Normal));
        }
        #endregion

        //bool isWiden = false;

        /// <summary>
        /// 窗口被激活，调用
        /// </summary>
        /// <param name="e"></param>
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            //Title = "激活";
            if (titleBorder != null)
            {
                titleBorder.Background = resource["TitleBackgroudActvied"] as SolidColorBrush;
            }
        }

        /// <summary>
        /// 窗口失去激活，调用
        /// </summary>
        /// <param name="e"></param>
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            //Title = "未激活";
            if (titleBorder != null)
            {
                titleBorder.Background = resource["TitleBackgroudDeActvied"] as SolidColorBrush;
            }
        }

        private Border? titleBorder;

        private bool isMax = false;
        private Rect normalRect;

        /// <summary>
        /// 当初始化完毕Style模板的时候，会调用
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if(GetTemplateChild("PART_TitleBorder") is Border titleBorder)
            {
                this.titleBorder = titleBorder;
            }

            //查找各种需要使用的控件，比如关闭按钮
            if (GetTemplateChild("PART_BtnClose") is Button closeButton)
            {
                closeButton.Click += (sender, e) =>
                {
                    this.Close();   //关闭
                };
            }

            //最小化
            if (GetTemplateChild("PART_BtnMini") is Button miniButton)
            {
                miniButton.Click += (sender, e) =>
                {
                    this.WindowState = WindowState.Minimized;
                };

                if (this.WindowModel == WindowModel.Dialog)
                {
                    miniButton.Visibility = Visibility.Hidden;
                }

            }

            //最大化
            if (GetTemplateChild("PART_BtnMax") is Button maxButton)
            {
                maxButton.Click += (sender, e) =>
                {
                    //最大化操作时：如果使用自定义窗口的时候，在计算实际大小的时候，会出现全屏的问题
                    //就不应该使用默认的这个属性进行操作了

                    if (isMax)
                    {
                        this.Left = normalRect.X;
                        this.Top = normalRect.Y;
                        this.Width = normalRect.Width;
                        this.Height = normalRect.Height;
                        this.isMax =false;
                    }
                    else
                    {
                        //1.记录当前窗口的状态
                        normalRect = new Rect(this.Left,this.Top,this.Width,this.Height);

                        this.Top = 0;
                        this.Left = 0;
                        Rect rect=SystemParameters.WorkArea;
                        this.Width = rect.Width;
                        this.Height = rect.Height;
                        this.isMax = true;
                    }
                    //if (this.WindowState == WindowState.Maximized)
                    //{
                    //    this.WindowState = WindowState.Normal;
                    //}
                    //else
                    //{
                    //    this.WindowState = WindowState.Maximized;
                    //}
                };

                if (this.WindowModel == WindowModel.Dialog)
                {
                    maxButton.Visibility = Visibility.Hidden;
                }
            }

            //Topmost
            if(GetTemplateChild("PART_CbTopmost") is CheckBox cbTopmost)
            {
                if (this.WindowModel == WindowModel.Dialog)
                {
                    cbTopmost.Visibility = Visibility.Hidden;
                }
            }

            if(GetTemplateChild("PART_Menu") is ToggleButton menuButton)
            {
                if (this.WindowModel == WindowModel.Dialog)
                {
                    menuButton.Visibility = Visibility.Hidden;
                }
            }

            //拖拽窗口
            if (GetTemplateChild("PART_TitleBar") is Border border)
            {
                border.MouseLeftButtonDown += (sender, e) =>
                {
                    this.DragMove();
                };
            }

            ////拖动缩放窗口
            //if (GetTemplateChild("PART_RectRight") is Rectangle rectRight)
            //{
            //    rectRight.MouseLeftButtonDown += (sender, e) =>
            //    {
            //        isWiden = true;
            //    };
            //    rectRight.MouseLeftButtonUp += (sender, e) =>
            //    {
            //        isWiden = false;
            //        Rectangle rectangle = (Rectangle)sender;
            //        rectangle.ReleaseMouseCapture();
            //    };
            //    rectRight.MouseMove += (sender, e) =>
            //    {
            //        Rectangle rectangle = (Rectangle)sender;
            //        if (isWiden)
            //        {
            //            rectangle.CaptureMouse();
            //            double newWidth = e.GetPosition(this).X + 6;
            //            if (newWidth > 0)
            //            {
            //                this.Width = newWidth;
            //            }
            //        }
            //    };
            //}

            if (GetTemplateChild("PART_ThumbLeft") is Thumb thumbLeft)
            {
                thumbLeft.DragDelta += (sender, e) =>
                {
                    double dw = e.HorizontalChange; //水平方向的变化
                    this.Left += dw;
                    if (this.Width >= dw)
                    {
                        this.Width -= dw;//防止避免出现负数问题
                    }
                    if (this.Width < MinWidth)
                    {
                        this.Width = MinWidth;
                    }
                };
            }

            if (GetTemplateChild("PART_ThumbTop") is Thumb thumbTop)
            {
                thumbTop.DragDelta += (sender, e) =>
                {
                    double dh = e.VerticalChange;
                    this.Top += dh;
                    if (this.Height >= dh)
                    {
                        this.Height -= dh;   //防止避免出现负数问题
                    }

                    if (this.Height < MinHeight)    //为了防止，高度超过最小值，加一个保护
                    {
                        this.Height = MinHeight;
                    }

                    //this.Title = $"dh={dh};Top={Top};Height={Height}";
                };
            }

            if (GetTemplateChild("PART_ThumbRight") is Thumb thumbRight)
            {
                thumbRight.DragDelta += (sender, e) =>
                {
                    double dw = e.HorizontalChange;
                    double width = this.Width;
                    width += dw;
                    if(width> 0)
                    {
                        this.Width = width;
                    }

                    if(this.Width < MinWidth)
                    {
                        this.Width = MinWidth;
                    }
                };
            }

            if (GetTemplateChild("PART_ThumbBottom") is Thumb thumbBottom)
            {
                thumbBottom.DragDelta += (sender, e) =>
                {
                    double dh = e.VerticalChange;
                    double height = this.Height;
                    height += dh;
                    if (height >= 0)
                    {
                        this.Height = height;
                    }

                    if (this.Height < MinHeight)    //为了防止，高度超过最小值，加一个保护
                    {
                        this.Height = MinHeight;
                    }
                };
            }

            if (this.WindowModel == WindowModel.Normal)
            {
                this.MinHeight = 560;
                this.MinWidth = 450;
            }

        }
    }
}
