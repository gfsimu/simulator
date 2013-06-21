using GirlFriendCommon;
using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GirlFriendYell
{
    /// <summary>
    /// YellSet.xaml の相互作用ロジック
    /// </summary>
    public partial class YellSet : UserControl
    {
        #region　メンバ
        /// <summary>
        /// 計算発生イベント
        /// </summary>
        public event EventHandler<CalcEvent> Calc;

        /// <summary>
        /// エール対象一覧
        /// </summary>
        private DsYellMember dsYellMember = new DsYellMember();

        /// <summary>
        /// エール前情報
        /// </summary>
        private TargetInfo targetInfo;

        /// <summary>
        /// エール後情報
        /// </summary>
        private TargetInfo targetInfoAfter = new TargetInfo();
        #endregion

        #region　プロパティ
        /// <summary>
        /// エール前対象情報
        /// </summary>
        public TargetInfo TargetInfo
        {
            get { return targetInfo; }
        }

        /// <summary>
        /// エール後対象情報
        /// </summary>
        public TargetInfo TargetInfoAfter
        {
            get { return targetInfoAfter; }
        }

        public int ID
        {
            set;
            get;
        }

        /// <summary>
        /// エール素材人数
        /// </summary>
        public int MemberCount
        {
            get { return dsYellMember.Member.Count; }
        }
        #endregion

        public YellSet()
        {
            InitializeComponent();

            DgYellMember.ItemsSource = dsYellMember.Member;
        }

        #region 計算
        /// <summary>
        /// エール計算
        /// </summary>
        public void CalcYell()
        {
            List<YellInfo> yellList = dsYellMember.Member.Where(r => r.対象).Select(
                row =>
                    new YellInfo()
                    {
                        Rare = (Rare)Enum.Parse(typeof(Rare), row.レア),
                        Cost = row.コスト,
                        Rank = row.進展,
                        Attr = (Attr)Enum.Parse(typeof(Attr),row.属性),
                        HasSkill = row.声援,
                    }
            ).ToList();

            //基本情報（レア、属性）更新
            targetInfoAfter.Rare = targetInfo.Rare;
            targetInfoAfter.Attr = targetInfo.Attr;

            //ガル
            int gall = ((targetInfo.Lv < 30) ? yellList.Count * targetInfo.Lv * 50 : yellList.Count * targetInfo.Lv * 100);
            targetInfoAfter.Gall = targetInfo.Gall + gall;
            LblGallAfter.Content = gall.ToString("#,0");

            CalcLv(ChkSuccess.IsChecked ?? false,yellList);
            CalcSkill(yellList);
        }

        /// <summary>
        /// LvUP計算
        /// </summary>
        /// <param name="yellList"></param>
        private void CalcLv(bool isSuccess,List<YellInfo> yellList)
        {
            int lv;
            int exp;
            int progress;
            int totalExp;
            YellUtility.CalcLv(targetInfo, isSuccess, yellList, out lv, out exp, out progress,out totalExp);

            targetInfoAfter.Lv = lv;
            targetInfoAfter.Exp = exp;
            targetInfoAfter.Progress = progress;
            if (progress == 100) progress = 0;
            targetInfoAfter.TotalExp = YellUtility.GetTotalExp(targetInfoAfter.Lv, progress);


            //各位の経験値更新
            foreach (DsYellMember.MemberRow row in dsYellMember.Member)
            {
                CalcExp(isSuccess, row);
            }

            SetLvLabel();
        }
        /// <summary>
        /// 経験値計算（一人）
        /// </summary>
        /// <param name="row"></param>
        private void CalcExp(bool isSuccess,DsYellMember.MemberRow row)
        {
            YellInfo yellInfo = new YellInfo() { 
                Rare = (Rare)Enum.Parse(typeof(Rare), row.レア),
                Attr = (Attr)Enum.Parse(typeof(Attr), row.属性),
                Cost = row.コスト,
                Rank = row.進展,
            } ;
            //UP成功率計算
            int exp = YellUtility.CalcExp(targetInfo, isSuccess, yellInfo);
            row.経験値 = exp;
        }

        /// <summary>
        /// 声援確率計算
        /// </summary>
        private void CalcSkill(List<YellInfo> yellList )
        {
            int lv = targetInfo.SkillLv;
            //UP成功率計算
            int rate= YellUtility.CalcSkill(targetInfo.Rare, targetInfo.SkillLv, yellList);
            if(rate >= 100) lv++;
            targetInfoAfter.SkillLv = lv;

            //各位の確率更新
            foreach (DsYellMember.MemberRow row in dsYellMember.Member)
            {
                CalcSkill(row);
            }

            SetRateLabel(rate);
        }

        /// <summary>
        /// 声援確率計算（一人）
        /// </summary>
        /// <param name="row"></param>
        private void CalcSkill(DsYellMember.MemberRow row)
        {
            List<YellInfo> yellList = new List<YellInfo>() { new YellInfo(){Rare = (Rare)Enum.Parse(typeof(Rare), row.レア), HasSkill=row.声援} };
            int lv = targetInfo.SkillLv;
            //UP成功率計算
            int rate = YellUtility.CalcSkill(targetInfo.Rare, targetInfo.SkillLv, yellList);
            row.声援UP = rate;
        }

        #endregion

        #region 表示
        /// <summary>
        /// 声援アップラベル表示
        /// </summary>
        /// <param name="rate"></param>
        private void SetLvLabel()
        {
            int maxLv = YellUtility.GetMaxLv(targetInfo.Rare);
            //エール前Lv
            LblLvBefore.Content = targetInfo.Lv.ToString() + " (" + targetInfo.Progress.ToString() + "%)";
            //エール後Lv
            if (targetInfoAfter.Lv == maxLv)
            {
                  BdrLvAfter.Background = FindResource("YellLvMaxBackground") as Brush;
            }
            else
            {
                BdrLvAfter.Background = FindResource("YellLvBackground") as Brush;
            }
            LblLvAfter.Content =targetInfoAfter.Lv.ToString();

            //最大レベル
            LblLvAfterMax.Content = maxLv;
            //エール後LvUP
            if (targetInfoAfter.Lv > targetInfo.Lv)
            {
                LblLvAfterLvUP.Foreground = FindResource("RedForeground") as Brush;
            }
            else
            {
                LblLvAfterLvUP.Foreground = FindResource("DefaultForeground") as Brush;
            }
            LblLvAfterLvUP.Content = "+" + (targetInfoAfter.Lv - targetInfo.Lv).ToString();
            //エール後成長率
            LblLvAfterProgress.Content = "(" + targetInfoAfter.Progress.ToString() + "%)";

            //必要経験値
            int nextExp = YellUtility.GetNextExp(targetInfoAfter.Rare,targetInfoAfter.Lv);
            if (nextExp > 0)
            {
                LblNeedExpAfter.Content = (nextExp - targetInfoAfter.Exp).ToString();
            }
            else
            {
                LblNeedExpAfter.Content = string.Empty;
            }
            int needExp = YellUtility.GetTotalExp(YellUtility.GetMaxLv(targetInfo.Rare), 0);

            LblRemainExpAfter.Content = needExp- targetInfoAfter.TotalExp;

            RctRemainExpBar.Width = 50 *targetInfoAfter.TotalExp/ needExp  ;
        }

        /// <summary>
        /// 声援アップラベル表示
        /// </summary>
        /// <param name="rate"></param>
        private void SetRateLabel(int rate)
        {
            //エール前声援Lv
            LblSkillLvBefore.Content = targetInfo.SkillLv.ToString();
            //エール後声援Lv
            LblSkillLvAfter.Content = targetInfoAfter.SkillLv.ToString();
            if (targetInfo.SkillLv != targetInfoAfter.SkillLv)
            {
                    LblSkillLvAfter.Foreground = this.FindResource("EmphasisForeground") as Brush;
            }
            else
            {
                    LblSkillLvAfter.Foreground = this.FindResource("DefaultForeground") as Brush;
            }
            //声援UP確率
            LblSkillUpRateAfter.Content = "(" + rate.ToString() + "%)";
            if (rate > 100)
            {
                BdrSkillUp.Background = this.FindResource("OverBackground") as Brush;
            }
            else
            {
                BdrSkillUp.Background = this.FindResource("YellSkillBackground") as Brush;
            }

        }
        #endregion

        #region　パブリックメンバ

        public void Initialize(TargetInfo target)
        {
            targetInfo = target;
            targetInfo.Copy(targetInfoAfter);
            SetLvLabel();
            SetRateLabel(0);
        }

        /// <summary>
        /// エール素材追加
        /// </summary>
        /// <param name="add"></param>
        public void AddYellMember(DsMember add)
        {
            foreach (var row in add.Member)
            {
                if (dsYellMember.Member.Count == 10)
                {
                    DialogWindow.Show(App.Current.MainWindow , "これ以上追加できません", DialogWindow.MessageType.Error);
                    break;
                }

                dsYellMember.Member.ImportRow(row);

                DsYellMember.MemberRow memberRow = dsYellMember.Member[dsYellMember.Member.Count - 1];
                memberRow.対象 = true;

            }

            //エール再計算
            CalcYell();
        }

        /// <summary>
        /// エール素材削除
        /// </summary>
        public void RemoveYellMember()
        {
            int selectedIndex = DgYellMember.SelectedIndex;
            if (selectedIndex >= 0)
            {
                List<DsYellMember.MemberRow> selectedList = new List<DsYellMember.MemberRow>();

                foreach (DataRowView row in DgYellMember.SelectedItems)
                {
                    selectedList.Add(row.Row as DsYellMember.MemberRow);
                }
                selectedList.ForEach(r => dsYellMember.Member.RemoveMemberRow(r));
                if(dsYellMember.Member.Count > 0)
                {
                    DgYellMember.SelectedIndex = selectedIndex > (dsYellMember.Member.Count - 1) ? dsYellMember.Member.Count - 1 : selectedIndex;
                }

                //エール再計算
                CalcYell();

            }
        }

        /// <summary>
        /// エール素材全削除
        /// </summary>
        public void RemoveAllYellMember()
        {
            dsYellMember.Member.Clear();
            //エール再計算
            CalcYell();

        }
        #endregion

        #region イベント
        /// <summary>
        /// クリアボタン押下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            dsYellMember.Clear();
            CalcYell();

            //計算イベントを発生させる
            if (Calc != null)
            {
                Calc(this, new CalcEvent(this.ID));
            }
        }

        /// <summary>
        /// 大成功チェックボックス
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkSuccess_Click(object sender, RoutedEventArgs e)
        {
            //再計算する
            CalcYell();

            //計算イベントを発生させる
            if (Calc != null)
            {
                Calc(this, new CalcEvent(this.ID));
            }
        }

        /// <summary>
        /// 一覧対象切り替え
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgYellMember_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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
                    DsYellMember.MemberRow cardRow = row.Row as DsYellMember.MemberRow;
                    if (cardRow != null)
                    {
                        //クリックされたのがチェックボックスだった場合は、
                        //バインドされてるbool値を切り替える
                        cardRow.対象 = !cardRow.対象;

                        CalcYell();

                        //計算イベントを発生させる
                        if (Calc != null)
                        {
                            Calc(this, new CalcEvent(this.ID));
                        }
                    }
                }
            }
        }
        #endregion
    }
}
