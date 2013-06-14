using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GirlFriendDeck
{
    /// <summary>
    /// ColumnSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ColumnSettingWindow : Window
    {
        public string GridName { set; get; }
        public DsSetting Setting { set; get; }

        public string WindowTitle
        {
            set
            {
                this.LblWindowTitle.Content = "カラム設定 ー " + value;
            }
        }


        public ColumnSettingWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            this.DgColumnAll.ItemsSource = Setting.Column.Where(r => r.GridName == GridName && r.Type == GirlFriendDeck.MainWindow.CalcType.共通.ToString()).OrderBy(r => r.Index).AsDataView();
            this.DgColumnAtk.ItemsSource = Setting.Column.Where(r => r.GridName == GridName && r.Type == GirlFriendDeck.MainWindow.CalcType.攻援.ToString()).OrderBy(r => r.Index).AsDataView();
            this.DgColumnDef.ItemsSource = Setting.Column.Where(r => r.GridName == GridName && r.Type == GirlFriendDeck.MainWindow.CalcType.守援.ToString()).OrderBy(r => r.Index).AsDataView();
            this.DgColumnEvent.ItemsSource = Setting.Column.Where(r => r.GridName == GridName && r.Type == GirlFriendDeck.MainWindow.CalcType.イベント.ToString()).OrderBy(r => r.Index).AsDataView();

            CorrectWindowPosition(this);
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        private void BtnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            this.Close();
        }

        private void Dg_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //クリックされたセルを取得
            DataGridCellInfo cell = (sender as DataGrid).CurrentCell;
            if (cell != null && cell.Column is DataGridCheckBoxColumn)
            {
                //セルから行を取得
                DataRowView row = cell.Item as DataRowView;
                if (row != null)
                {
                    //取得した行にバインドされているアイテムを取得
                    DsSetting.ColumnRow columnRow = row.Row as DsSetting.ColumnRow;
                    if (columnRow != null)
                    {
                        //クリックされたのがチェックボックスだった場合は、
                        //バインドされてるbool値を切り替える
                        columnRow.Visibility = !columnRow.Visibility;
                    }
                }
            }
        }

        public static void CorrectWindowPosition(Window window)
        {
            if (window.Owner != null)
            {
                window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                window.Left = window.Owner.Left + (window.Owner.ActualWidth / 2) - (window.ActualWidth / 2);
                window.Top = window.Owner.Top + (window.Owner.ActualHeight / 2) - (window.ActualHeight / 2);
            }

            if (window.Left < 0)
            {
                window.Left = 0;
            }
            if (window.Top < 0)
            {
                window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                window.Top = 0;
            }
            var screenSize = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            //右側にはみ出している場合
            if (screenSize.Width < (window.Left + window.ActualWidth))
            {
                window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                window.Left = screenSize.Width - window.ActualWidth;
            }
            //下側にはみ出している場合
            if (screenSize.Height < (window.Top + window.ActualHeight))
            {
                window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;
                window.Top = screenSize.Height - window.ActualHeight;
            }
        }

        private void Window_Closed_1(object sender, EventArgs e)
        {
            this.DgColumnAll.ItemsSource = null;
            this.DgColumnAtk.ItemsSource = null;
            this.DgColumnDef.ItemsSource = null;
            this.DgColumnEvent.ItemsSource = null;

        }
    }
}
