using System;
using System.Collections.Generic;
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

namespace GirlFriendCommon
{
    /// <summary>
    /// Window.xaml の相互作用ロジック
    /// </summary>
    public partial class DialogWindow : Window
    {

        public enum MessageType
        {
            Infomation,
            Confirm,
            Warning,
            Error,
        }

        public DialogWindow()
        {
            InitializeComponent();
        }

        public static bool Show(Window owner, string message, string title, MessageType messageType = MessageType.Infomation)
        {
            DialogWindow window = new DialogWindow();
            window.Owner = owner;
            //確認以外の場合は「OK」のみにする
            if (messageType != MessageType.Confirm)
            {
                window.BtnCancel.Visibility = Visibility.Collapsed;
                Grid.SetColumn(window.BtnOK, 1);
                window.BtnOK.HorizontalAlignment = HorizontalAlignment.Right;
            }
            else
            {
                window.BtnOK.Style = window.FindResource("RedButton") as Style;
            }
            window.LblMessage.Text = message;
            window.LblTitle.Content = title;

            window.RctTitleBar.Background = window.FindResource(messageType.ToString() + "TitleBar") as Brush;
            window.BdrMessage.Background = window.FindResource(messageType.ToString() + "Message") as Brush;

            return window.ShowDialog() ?? false;
        }

        public static bool Show(Window owner, string message, MessageType messageType = MessageType.Infomation)
        {
            string title = string.Empty;
            switch (messageType)
            {
                case MessageType.Infomation: title = ""; break;
                case MessageType.Warning: title = "警告"; break;
                case MessageType.Error: title = "エラー"; break;
            }

            return Show(owner, message, title, messageType);
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
