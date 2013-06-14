using System;
using System.Collections.Generic;
using System.IO;
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
    public partial class ShowCardPanel : UserControl
    {
        public ShowCardPanel()
        {
            InitializeComponent();

        }

        public void SetCardId(string Id, DsCards.CardsRow cardRow, DsDispCard.DispCardRow dispRow)
        {

            if (cardRow != null && dispRow != null)
            {
                NameMirror.Content = dispRow.名前;
                Name.Content = dispRow.名前;

                if (!string.IsNullOrEmpty(cardRow.画像) && File.Exists(cardRow.画像))
                {
                    BitmapImage img = new BitmapImage(new Uri(cardRow.画像));

                    ImgCard.Source = img;
                    ImgCardMirror.Source = img;
                    RenderOptions.SetBitmapScalingMode(ImgCard, BitmapScalingMode.HighQuality);
                    RenderOptions.SetBitmapScalingMode(ImgCardMirror, BitmapScalingMode.HighQuality);
                }
                else
                {
                    ImgCard.Source = new BitmapImage(new Uri(@".\NoImage1.png", UriKind.Relative));
                    ImgCardMirror.Source = new BitmapImage(new Uri(@".\NoImage1.png", UriKind.Relative));
                }
            }
        }
        public void SetScale(double x, double y)
        {
            SclPanel.ScaleX = x;
            SclPanel.ScaleY = y;
        }
    }
}
