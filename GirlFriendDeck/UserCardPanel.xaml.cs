using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GirlFriendDeck
{
    /// <summary>
    /// UserCardPanel.xaml の相互作用ロジック
    /// </summary>
    public partial class UserCardPanel : UserControl
    {
        public UserCardPanel()
        {
            InitializeComponent();

            this.MouseEnter += UserCardPanel_MouseEnter;
            this.MouseLeave += UserCardPanel_MouseLeave;
        }

        void UserCardPanel_MouseLeave(object sender, MouseEventArgs e)
        {
            Panel.SetZIndex(this, 1);
            Panel.SetZIndex(this.ImgCard, 2);
        }

        void UserCardPanel_MouseEnter(object sender, MouseEventArgs e)
        {
            Panel.SetZIndex(this, 99);
            Panel.SetZIndex(this.ImgCard, 100);
        }

        public void SetCardId(string Id, DsDispCard.DispCardRow dispRow, DsSelectCard.CardRow selectRow)
        {
            ImgCard.Source = dispRow.画像 as BitmapImage;
            RenderOptions.SetBitmapScalingMode(ImgCard, BitmapScalingMode.HighQuality);

            LblName.Text = dispRow.名前;
            LblName.ToolTip = dispRow.名前;
            LblCost.Content = dispRow.コスト;
            LblRare.Content = dispRow.レア;
            LblPower.Content = selectRow.発揮値;
            LblSkill.Text = dispRow.スキル;
            LblSkill.ToolTip = dispRow.スキル;
            switch (dispRow.進展)
            {
                case 1:
                    RctRank1.Visibility = Visibility.Visible;
                    RctRank2.Visibility = Visibility.Hidden;
                    RctRank3.Visibility = Visibility.Hidden;
                    break;
                case 2:
                    RctRank1.Visibility = Visibility.Visible;
                    RctRank2.Visibility = Visibility.Visible;
                    RctRank3.Visibility = Visibility.Hidden;
                    break;
                case 3:
                    RctRank1.Visibility = Visibility.Visible;
                    RctRank2.Visibility = Visibility.Visible;
                    RctRank3.Visibility = Visibility.Visible;
                    break;
                default:
                    RctRank1.Visibility = Visibility.Hidden;
                    RctRank2.Visibility = Visibility.Hidden;
                    RctRank3.Visibility = Visibility.Hidden;
                    break;
            }

            if (dispRow.属性 == "Pop")
            {
                BdrBack.Background = FindResource("PopBackground") as Brush;
            }
            else if (dispRow.属性 == "Sweet")
            {
                BdrBack.Background = FindResource("SweetBackground") as Brush;
            }
            else if (dispRow.属性 == "Cool")
            {
                BdrBack.Background = FindResource("CoolBackground") as Brush;
            }
        }
    }
}
