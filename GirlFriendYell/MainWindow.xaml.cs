using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
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

using GirlFriendCommon;
using GirlFriendDeck;
using System.Windows.Controls.Primitives;


namespace GirlFriendYell
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ATTR_POP = "Pop";
        private const string ATTR_COOL = "Cool";
        private const string ATTR_SWEET = "Sweet";

        private static Dictionary<Attr, string> attrList = new Dictionary<Attr, string>() { { Attr.Sweet, ATTR_SWEET }, {  Attr.Cool,ATTR_COOL }, { Attr.Pop,ATTR_POP } };
        private Dictionary<string, DsYellCards.CardsRow> cardsList = new Dictionary<string, DsYellCards.CardsRow>();
        private Dictionary<string, DsGirls.GirlsRow> girlsList = new Dictionary<string, DsGirls.GirlsRow>();
        private List<SkillInfo> Skills;
        private List<NameInfo> nameList = new List<NameInfo>();

        /// <summary>
        /// 早見表トグルbuttonリスト
        /// </summary>
        private List<ToggleButton> toggleButtonList; 

        private DsYellInfo dsYellInfo = new DsYellInfo();
        private DsYellCards dsYellCards = new DsYellCards();
        private DsYellDispCards dsDispCards = new DsYellDispCards();
        private DsYellDispList dsYellDispList = new DsYellDispList();

        private DsYellTarget dsYellTarget = new DsYellTarget();

        private TargetInfo targetInfo = new TargetInfo();
        private TargetInfo targetInfoResult = new TargetInfo();

        /// <summary>フリー入力の声援情報</summary>
        private YellInfo freeYellInfo = new YellInfo();

        private bool isInit;
        private bool isEvent;
        private DsMember dsFreeMember = new DsMember();
        private DsDispMember dsDispFreeMember = new DsDispMember();

        /// <summary>フリー入力ID</summary>
        private int freeMax = 1;
        /// <summary>所持カードID</summary>
        private int cardMax = 1;
        /// <summary>現在編集中のCommonID</summary>
        private string currentEditID = string.Empty;

        enum ExpType
        {
            Normal,
            Success,
            Same,
            SameSuccess,
        }
        
        public MainWindow()
        {
            InitializeComponent();
        }

        #region WindowLoaded
        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
#if NET_V4
            this.Title += " for XP(.Net4.0)";
#endif
            Initialize();

            toggleButtonList = new List<ToggleButton>() {
            TBtnListExp,
            TBtnListExpSame,
            TBtnListExpSuccess,
            TBtnListExpSameSuccess,
            TBtnListSkillUp,
            TBtnListSkillUpNum};


            #region コントロール初期化
            CmbBaseAttr.SelectedValuePath = "Key";
            CmbBaseAttr.DisplayMemberPath = "Value";
            CmbBaseAttr.ItemsSource = attrList;
            CmbBaseAttr.SelectedIndex = 0;

            CmbFreeRank.SelectedValuePath = "Key";
            CmbFreeRank.DisplayMemberPath = "Value";
            CmbFreeRank.ItemsSource = new Dictionary<int, string> { { 1, "1" }, { 2, "2" }, { 3, "3" } };
            CmbFreeRank.SelectedIndex = 0;

            CmbCardGirlFavor.SelectedValuePath = "Key";
            CmbCardGirlFavor.DisplayMemberPath = "Value";
            CmbCardGirlFavor.ItemsSource = new Dictionary<int, string> { { 1, "1" }, { 2, "2" }, { 3, "3" }, { 4, "4" }, { 5, "5" } };

            CmbFreeAttr.SelectedValuePath = "Key";
            CmbFreeAttr.DisplayMemberPath = "Value";
            CmbFreeAttr.ItemsSource = attrList;
            CmbFreeAttr.SelectedValue = Attr.Sweet;

            YellUtility.SetComboBox<Rare>(CmbBaseRare);
            YellUtility.SetComboBox<Rare>(CmbFreeRare);
            YellUtility.SetComboBoxString<Rare>(CmbSearchFreeRare,true);
            YellUtility.SetComboBoxString<Attr>(CmbSearchFreeAttr,true);
            YellUtility.SetComboBoxString<Attr>(CmbPopSearchOwnCardAttr, true);

            CmbCardGirlSkill.DisplayMemberPath = "Name";
            CmbCardGirlSkill.SelectedValuePath = "Name";
            CmbCardGirlSkill.ItemsSource = Skills;
            #endregion

            #region エール情報読込
            if (File.Exists(Utility.GetFilePath("yellInfo.xml")))
            {
                //dsYellInfo.ReadXml(Utility.GetFilePath("yellInfo.xml"));
                CreateYellInfo();
                dsYellInfo.WriteXml(Utility.GetFilePath("yellInfo.xml"));
            }
            else
            {
                CreateYellInfo();
                dsYellInfo.WriteXml(Utility.GetFilePath("yellInfo.xml"));
            }
            YellUtility.Info = dsYellInfo;
            CreateYellDispInfo();
            #endregion

            #region カード情報読み込み
            //カード情報読み込み
            if (File.Exists(Utility.GetFilePath("gilrs.xml")))
            {
                LoadGilrs();
            }
            #endregion

            #region 所持カード情報読み込み
            if (File.Exists(Utility.GetFilePath("yellcards.xml")))
            {
                dsYellCards.ReadXml(Utility.GetFilePath("yellcards.xml"));
                #region 互換対応
                bool isChange = false;
                if (dsYellCards.Cards.Count > 0 && dsYellCards.Cards[0].Is好感度Null())
                {
                    foreach (DsYellCards.CardsRow row in dsYellCards.Cards)
                    {
                        if (row.Is好感度Null())
                        {
                            //最終進展の場合は好感度もMAXに初期設定しておく
                            if (row.進展 == 3)
                            {
                                row.好感度 = 5;
                            }
                            else if (row.進展 == 2)
                            {
                                row.好感度 = 3;
                            }
                            else
                            {
                                row.好感度 = 1;
                            }
                            isChange = true;
                        }
                    }
                }
                if (isChange)
                {
                    dsYellCards.AcceptChanges();
                    dsYellCards.WriteXml(Utility.GetFilePath("yellcards.xml"));
                }
                #endregion

                dsDispCards.Clear();
                cardsList.Clear();
                foreach (DsYellCards.CardsRow cardRow in dsYellCards.Cards)
                {
                    CreateDispCardRow(cardRow);
                    if (!string.IsNullOrEmpty(cardRow.YellID) && Convert.ToInt32(cardRow.YellID) > cardMax)
                    {
                        cardMax = Convert.ToInt32(cardRow.YellID);
                    }

                    if (!cardsList.ContainsKey(cardRow.CommonID))
                    {
                        cardsList.Add(cardRow.CommonID, cardRow);
                    }
                }
            }
            DgCards.ItemsSource = dsDispCards.DispCard.OrderBy(r=>r.表示順).AsDataView();
            #endregion

            #region 入力初期化
            CmbBaseRare.SelectedValue = Rare.SR;
            TxtBaseSkillLv.Text = "1";
            TxtBaseLv.Text = "1";
            TxtBaseExp.Text = "0";

            CmbFreeAttr.SelectedValue = Attr.Sweet;
            CmbFreeRare.SelectedValue = Rare.R;
            ChkFreeHasSkill.IsChecked = true;
            TxtFreeCost.Text = "2";
            #endregion

            #region エール対象情報設定
            targetInfo.Lv = 1;
            targetInfo.Progress = 0;
            targetInfo.Exp = 0;
            targetInfo.SkillLv = 1;
            targetInfo.Rare = (Rare)CmbBaseRare.SelectedValue;
            targetInfo.Attr = Attr.Sweet;
            #endregion

            TabItem tab = TabYellMember.SelectedValue as TabItem;
            YellSet yellSet = tab.Content as YellSet;
            yellSet.Initialize(targetInfo);
            yellSet.Calc += set_Calc;
            targetInfoResult = yellSet.TargetInfoAfter;

            #region フリー入力エール情報設定
            freeYellInfo.Attr = Attr.Sweet;
            freeYellInfo.Cost = 2;
            freeYellInfo.HasSkill = true;
            freeYellInfo.Rank = 1;
            freeYellInfo.Rare = Rare.R;
            #endregion

            #region フリー一覧読込
            if (File.Exists(Utility.GetFilePath("yellFreeMember.xml")))
            {
                dsFreeMember.ReadXml(Utility.GetFilePath("yellFreeMember.xml"));
                #region 互換対応
                //IDが設定されていない場合は振りなおす
                if (dsFreeMember.Member.Count > 0 && dsFreeMember.Member[0].IsIDNull())
                {
                    freeMax = 1;
                    foreach (DsMember.MemberRow row in dsFreeMember.Member)
                    {
                        row.ID = freeMax.ToString();
                        freeMax++;
                    }
                }
                else
                {
                    if (dsFreeMember.Member.Count > 0)
                    {
                        freeMax = dsFreeMember.Member.Max(r => Convert.ToInt32(r.ID)) + 1;
                    }
                }
                #endregion

                //表示レコード作成
                foreach (DsMember.MemberRow row in dsFreeMember.Member)
                {
                    AddFreeListDispRow(row);
                }
            }
            #endregion

            //フリー一覧タブ
            DgFreeList.ItemsSource = dsDispFreeMember.Member;
            isInit = true;
            isEvent = true;
            //表示初期化
            ShowResult();

            TBtnListExp.IsChecked = true;
            CreateExpList(ExpType.Normal);
        }

        #endregion

        #region 初期化
        private void Initialize()
        {
            #region スキル
            Skills = Card.GetSkills();
            #endregion
        }

        private void LoadGilrs()
        {
            DsGirls dsGilrs = new DsGirls();
            dsGilrs.ReadXml(Utility.GetFilePath("gilrs.xml"));

            HashSet<string> girlNames = new HashSet<string>();
            foreach (DsGirls.GirlsRow row in dsGilrs.Girls)
            {
                if (!girlNames.Contains(row.名前))
                {
                    girlNames.Add(row.名前);
                    nameList.Add(new NameInfo() { Name = row.名前, Hiragana = row.なまえ, Roma = row.Name, Attr = row.属性 });
                }
                if (!girlsList.ContainsKey(row.名前))
                {
                    girlsList.Add(row.名前, row);
                }
            }
            
            CmbCardGirlName.SelectedValuePath = "Name";
            CmbCardGirlName.DisplayMemberPath = "Display";
            CmbCardGirlName.ItemsSource = nameList;

            LstGirlsName.SelectedValuePath = "Name";
            LstGirlsName.DisplayMemberPath = "Display";
            LstGirlsName.ItemsSource = nameList;

            LstFreeGirlsName.SelectedValuePath = "Name";
            LstFreeGirlsName.DisplayMemberPath = "Display";
            LstFreeGirlsName.ItemsSource = nameList;
        }

        /// <summary>
        /// エール情報作成
        /// </summary>
        private void CreateYellInfo()
        {
            #region 基礎経験値
            foreach (var val in Enum.GetValues(typeof(Rare)))
            {
                Rare rare = (Rare)val;
                for (int i = 1; i <= 3; i++)
                {
                    int baseExp = 0;
                    if (i == 1)
                    {
                        switch (rare)
                        {
                            case Rare.N: baseExp = 72; break;
                            case Rare.HN: baseExp = 136; break;
                            case Rare.R: baseExp = 510; break;
                            case Rare.HR: baseExp = 1558; break;
                            case Rare.SR: baseExp = 3062; break;
                            case Rare.SSR: baseExp = 0; break;
                            case Rare.LG: baseExp = 0; break;
                        }
                    }
                    else if (i == 2)
                    {
                        switch (rare)
                        {
                            case Rare.N: baseExp = 102; break;
                            case Rare.HN: baseExp = 165; break;
                            case Rare.R: baseExp = 540; break;
                            case Rare.HR: baseExp = 1558; break;
                            case Rare.SR: baseExp = 3062; break;
                            case Rare.SSR: baseExp = 0; break;
                            case Rare.LG: baseExp = 0; break;
                        }

                    }
                    else if (i == 3)
                    {
                        switch (rare)
                        {
                            case Rare.HN: baseExp = 195; break;
                            case Rare.R: baseExp = 570; break;
                            case Rare.HR: baseExp = 1618; break;
                            case Rare.SR: baseExp = 3120; break;
                            case Rare.SSR: baseExp = 3062; break;
                            case Rare.LG: baseExp = 0; break;
                        }
                    }

                    DsYellInfo.BaseExpRow baseExpRow = dsYellInfo.BaseExp.NewBaseExpRow();
                    baseExpRow.レア = rare.ToString();
                    baseExpRow.進展 = i;
                    baseExpRow.経験値 = baseExp;
                    dsYellInfo.BaseExp.AddBaseExpRow(baseExpRow);
                }
            }
            #endregion

            #region 必要経験値
            for (int lv = 1; lv <= 80; lv++)
            {
                int exp = 30 + (lv * 30);
                if (lv >= 14) exp = 480;
                if (lv >= 15) exp = 600;
                if (lv >= 35) exp = 750;
                if (lv >= 43) exp = 900;
                if (lv >= 61) exp = 1050;
                if (lv >= 65) exp = 1200;
                if (lv == 80) exp = 1;

                DsYellInfo.LvExpRow lvExpRow = dsYellInfo.LvExp.NewLvExpRow();
                lvExpRow.Lv = lv;
                lvExpRow.Exp = exp;
                dsYellInfo.LvExp.AddLvExpRow(lvExpRow);
            }
            #endregion
        }

        /// <summary>
        /// エール情報作成
        /// </summary>
        private void CreateYellDispInfo()
        {
            DsYellDispInfo dsYellDispInfo = new DsYellDispInfo();
            #region 基礎経験値
            foreach (DsYellInfo.BaseExpRow baseExpRow in dsYellInfo.BaseExp)
            {
                //Nの3進展は存在しない（設定上NH3進展）
                //LGの1、2進展は存在しない
                if ((baseExpRow.レア == "N" && baseExpRow.進展 == 3) ||
                    (baseExpRow.レア == "LG" && baseExpRow.進展 < 3))
                {
                    continue;
                }
                Rare rare = (Rare)Enum.Parse(typeof(Rare), baseExpRow.レア);
                int dispOrder = (int)rare * 3 + (baseExpRow.進展 < 3 ? baseExpRow.進展 : 0);

                var dispRow = dsYellDispInfo.BaseExp.NewBaseExpRow();
                dispRow.レア = baseExpRow.レア;
                dispRow.進展 = baseExpRow.進展;
                dispRow.経験値 = baseExpRow.経験値;
                dispRow.表示順 = dispOrder;
                //SSR,LGは未確認
                //HR+、SR+は未確認
                if ((baseExpRow.レア == "SSR" || baseExpRow.レア == "LG") ||
                    ((baseExpRow.レア == "HR" || baseExpRow.レア == "SR") && baseExpRow.進展 == 2))
                {
                    dispRow.未確認 = true;
                }
                else
                {
                    dispRow.未確認 = false;
                }
                dsYellDispInfo.BaseExp.AddBaseExpRow(dispRow);
            }
            #endregion

            #region LvUp経験値
            int totalExp = 0;
            foreach (DsYellInfo.LvExpRow lvRow in dsYellInfo.LvExp.OrderBy(r=>r.Lv))
            {
                var dispRow = dsYellDispInfo.LvExp.NewLvExpRow();
                dispRow.Lv = lvRow.Lv;
                dispRow.Exp = lvRow.Exp;
                totalExp = totalExp + lvRow.Exp;
                dispRow.TotalExp = totalExp;
                if (lvRow.Lv > 60)
                {
                    dispRow.未確認 = true;
                }
                else
                {
                    dispRow.未確認 = false;
                }
                dsYellDispInfo.LvExp.AddLvExpRow(dispRow);
            }
            #endregion

            DgYellInfoBaseExp.ItemsSource = dsYellDispInfo.BaseExp.OrderBy(r => r.表示順);
            DgYellInfoLvExp.ItemsSource = dsYellDispInfo.LvExp.OrderBy(r => r.Lv);
        }

        #endregion

        #region エールタブ

        #region 追加削除操作
        /// <summary>
        /// エール対象追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSubAdd_Click(object sender, RoutedEventArgs e)
        {
            #region 入力内容取得
            DsMember add = new DsMember();
            //入力チェック
            if ((CmbFreeRare.Text == "N" && (int)CmbFreeRank.SelectedValue == 3) ||
                (CmbFreeRare.Text == "LG" && (int)CmbFreeRank.SelectedValue < 3))
            {
                //N(3)またはLG(1),LG(2)はNG
                DialogWindow.Show(this, "存在しない進展の設定です", DialogWindow.MessageType.Error);
                return;
            }

            if (TbFreeInput.IsSelected)
            {
                //自由入力
                TxtFreeCost.Text = string.IsNullOrEmpty(TxtFreeCost.Text) ? "0" : TxtFreeCost.Text;

                add.Member.AddMemberRow(
                    string.Empty,
                    CmbFreeAttr.SelectedValue.ToString(),
                    Convert.ToInt32(TxtFreeCost.Text),
                    CmbFreeRare.Text,
                    (int)CmbFreeRank.SelectedValue,
                    TxtFreeName.Text,
                    ChkFreeHasSkill.IsChecked ?? false);
            }
            else if (TbFreeList.IsSelected)
            {
                //自由一覧
                if(DgFreeList.SelectedItem == null)
                {
                    DialogWindow.Show(this, "フリー一覧が選択されていません", DialogWindow.MessageType.Error);
                    return;
                }
                add.Member.ImportRow((DgFreeList.SelectedItem as DataRowView).Row);
            }
            #endregion

            #region 登録設定
            TabItem tab = TabYellMember.SelectedValue as TabItem;
            YellSet yellSet = tab.Content as YellSet;
            yellSet.AddYellMember(add);

            int count = yellSet.MemberCount;
            tab.Header = string.Format("No.{0}({1})", TabYellMember.SelectedIndex+1, count);

            #endregion

            ReCalcAfter(TabYellMember.SelectedIndex);
        }

        /// <summary>
        /// エール対象削除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSubRemove_Click(object sender, RoutedEventArgs e)
        {
            #region 削除設定
            TabItem tab = TabYellMember.SelectedValue as TabItem;
            YellSet yellSet = tab.Content as YellSet;
            yellSet.RemoveYellMember();

            int count = yellSet.MemberCount;
            tab.Header = string.Format("No.{0}({1})", TabYellMember.SelectedIndex+1, count);
            #endregion

            ReCalcAfter(TabYellMember.SelectedIndex);
        }
        #endregion

        #region 計算
        /// <summary>
        /// 全セット再計算
        /// </summary>
        private void ReCalcAll()
        {
            //元総経験値
            int maxLv = YellUtility.GetMaxLv(targetInfo.Rare);
            int progress = targetInfo.Progress;
            //最大レベルまで達していた場合は成長を０にして計算する
            if (maxLv == targetInfo.Lv) progress = 0;
            targetInfo.TotalExp = YellUtility.GetTotalExp(targetInfo.Lv, progress);

            foreach (TabItem tab in TabYellMember.Items)
            {
                YellSet yellSet = tab.Content as YellSet;
                int count = yellSet.MemberCount;
                tab.Header = string.Format("No.{0}({1})", TabYellMember.Items.IndexOf(tab) + 1, count);
                yellSet.CalcYell();
            }

            int resultProgress = targetInfoResult.Progress;
            if (maxLv == targetInfoResult.Lv) resultProgress = 0;
            targetInfoResult.TotalExp = YellUtility.GetTotalExp(targetInfoResult.Lv, resultProgress);

            ShowResult();
        }

        /// <summary>
        /// 指定セット以降再計算
        /// </summary>
        /// <param name="index"></param>
        private void ReCalcAfter(int index)
        {
            for (int i = index; i < TabYellMember.Items.Count; i++)
            {
                ((TabYellMember.Items[i] as TabItem).Content as YellSet).CalcYell();
            }

            int maxLv = YellUtility.GetMaxLv(targetInfo.Rare);
            int resultProgress = targetInfoResult.Progress;
            if (maxLv == targetInfoResult.Lv) resultProgress = 0;
            targetInfoResult.TotalExp = YellUtility.GetTotalExp(targetInfoResult.Lv, resultProgress);

            ShowResult();
        }
        #endregion

        #region 表示
        /// <summary>
        /// 結果表示
        /// </summary>
        private void ShowResult()
        {
            #region Lv
            int lvBefore;
            int.TryParse(TxtBaseLv.Text, out lvBefore);
            if (lvBefore <= 0) lvBefore = 1;
            if (lvBefore < targetInfoResult.Lv)
            {
                LblBaseLvStatus.Content = "UP";
                RctBaseLv.Fill = this.FindResource("IcoUp") as Brush;
            }
            else
            {
                LblBaseLvStatus.Content = "STAY";
                RctBaseLv.Fill = this.FindResource("IcoStay") as Brush;
            }

            int maxLv = YellUtility.GetMaxLv(targetInfo.Rare);
            int lvAfter = targetInfoResult.Lv;
            if (lvAfter > maxLv) lvAfter = maxLv;
            RctBarLv.Width = 60 * ((double)lvAfter / (double)maxLv);
            LblBaseLvMax.Content = "/" + maxLv.ToString();
            LblBaseLvAfter.Content = targetInfoResult.Lv.ToString();
            #endregion

            #region 成長度
            int expBefore;
            int.TryParse(TxtBaseExp.Text, out expBefore);
            if (expBefore <= 0) expBefore = 0;
            //元が最大レベルでないかつ、レベルが上がったか成長が増えた場合
            if ((lvBefore < targetInfoResult.Lv || expBefore < targetInfoResult.Progress)
                && (lvBefore != maxLv)
                )
            {
                LblBaseExpStatus.Content = "UP";
                RctBaseExp.Fill = this.FindResource("IcoUp") as Brush;
            }
            else
            {
                LblBaseExpStatus.Content = "STAY";
                RctBaseExp.Fill = this.FindResource("IcoStay") as Brush;
            }
            RctBarExp.Width = 60 * (double)targetInfoResult.Progress / 100d;
            LblBaseExpAfter.Content = targetInfoResult.Progress.ToString();
            #endregion

            #region 声援Lv
            int skillLvBefore;
            int.TryParse(TxtBaseSkillLv.Text, out skillLvBefore);
            if (skillLvBefore <= 0) skillLvBefore = 1;
            if (skillLvBefore < targetInfoResult.SkillLv)
            {
                LblBaseSkillLvStatus.Content = "UP";
                RctBaseSkillLv.Fill = this.FindResource("IcoUp") as Brush;
            }
            else
            {
                LblBaseSkillLvStatus.Content = "STAY";
                RctBaseSkillLv.Fill = this.FindResource("IcoStay") as Brush;
            }
            RctBarSkill.Width = 60 * (double)targetInfoResult.SkillLv / 10;
            LblBaseSkillLvAfter.Content = targetInfoResult.SkillLv.ToString();
            #endregion

            #region ガル
            //ガル
            LblBaseGallAfter.Content = targetInfoResult.Gall.ToString("#,0");
            #endregion

            //フリー情報更新
            ShowFreeYellInfo();
            ShowFreeListYellInfo();

            if (PopBaseTarget.IsOpen)
            {
                //目標情報表示
                ShowPopBaseTargetInfo();
            }
        }
        #endregion

        #region フリー入力タブ
        /// <summary>
        /// フリー一覧追加ボタン
        /// フリー一覧に追加する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddFreeList_Click(object sender, RoutedEventArgs e)
        {
            TxtFreeCost.Text = string.IsNullOrEmpty(TxtFreeCost.Text) ? "0" : TxtFreeCost.Text;

            dsFreeMember.Member.AddMemberRow(
                freeMax.ToString(),
                CmbFreeAttr.SelectedValue.ToString(),
                Convert.ToInt32(TxtFreeCost.Text),
                CmbFreeRare.Text,
                (int)CmbFreeRank.SelectedValue,
                TxtFreeName.Text,
                ChkFreeHasSkill.IsChecked ?? false);

            freeMax++;

            //表示レコード追加
            AddFreeListDispRow(dsFreeMember.Member[dsFreeMember.Member.Count - 1]);

            dsFreeMember.WriteXml(Utility.GetFilePath("yellFreeMember.xml"));

            DialogWindow.Show(this, "フリー一覧に追加しました", DialogWindow.MessageType.Infomation);
        }

        /// <summary>
        /// フリー入力クリアボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClearFree_Click(object sender, RoutedEventArgs e)
        {
            CmbFreeRank.SelectedValue = 1;
            TxtFreeName.Text = string.Empty;

            freeYellInfo.Rank = 1;
        }

        #region フリー入力イベント
        /// <summary>
        /// フリーレア度変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbFreeRare_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInit)
            {
                Rare rare = (Rare)CmbFreeRare.SelectedValue;
                //RareがHN以上の場合は声援有にする
                if (rare > Rare.N)
                {
                    ChkFreeHasSkill.IsChecked = true;
                    freeYellInfo.HasSkill = true;
                }
                else
                {
                    ChkFreeHasSkill.IsChecked = false;
                    freeYellInfo.HasSkill = false;
                }

                freeYellInfo.Rare = rare;
                ShowFreeYellInfo();
            }
        }

        /// <summary>
        /// フリー属性変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbFreeAttr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInit)
            {
                Attr attr = (Attr)CmbFreeAttr.SelectedValue;

                freeYellInfo.Attr = attr;
                ShowFreeYellInfo();
            }
        }

        /// <summary>
        /// フリーコスト変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtFreeCost_TextChanged(object sender, TextChangedEventArgs e)
        {
            int cost;
            if (Int32.TryParse(TxtFreeCost.Text, out cost))
            {
                freeYellInfo.Cost = cost;
            }
            else
            {
                //変換できなかった場合は2
                freeYellInfo.Cost = 2;
            }
            ShowFreeYellInfo();
        }

        /// <summary>
        /// フリー進展変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbFreeRank_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInit)
            {
                int  rank = (int)CmbFreeRank.SelectedValue;

                freeYellInfo.Rank = rank;
                ShowFreeYellInfo();
            }
        }

        /// <summary>
        /// 声援有無変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkFreeHasSkill_Click(object sender, RoutedEventArgs e)
        {
            freeYellInfo.HasSkill = ChkFreeHasSkill.IsChecked ?? false;
            ShowFreeYellInfo();

        }
        #endregion

        #region フリー入力エール情報表示
        /// <summary>
        /// フリー入力エール情報表示
        /// </summary>
        private void ShowFreeYellInfo()
        {
            TabItem selectTab = TabYellMember.SelectedItem as TabItem;
            YellSet selectYellSet = selectTab.Content as YellSet;
            int getExp = YellUtility.CalcExp(selectYellSet.TargetInfo, false, freeYellInfo);
            LblFreeExp.Content = getExp.ToString();
            if (freeYellInfo.HasSkill)
            {
                int skillUpPer = YellUtility.CalcSkill(selectYellSet.TargetInfo.Rare, selectYellSet.TargetInfo.SkillLv, new List<YellInfo>() { freeYellInfo });
                LblFreeSkillUpPer.Content = skillUpPer.ToString() + "%";
            }
            else
            {
                LblFreeSkillUpPer.Content = "0%";
            }
        }
        #endregion

        #region フリー名称入力検索
        /// <summary>
        /// 検索ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSearchFreeGirlName_Click(object sender, RoutedEventArgs e)
        {
            TxtPopSearchFreeGirlName.Text = string.Empty;
            PopSearchFreeGirlName.IsOpen = true;
        }

        private void TxtPopSearchFreeGirlName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            Search.SearchListName(textBox, LstFreeGirlsName, nameList);
        }

        /// <summary>
        /// 名称リストから選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LstFreeGirlsName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //名称と属性を取り付ける
            TxtFreeName.Text = LstFreeGirlsName.SelectedValue as string;
            if (girlsList.ContainsKey(TxtFreeName.Text))
            {
                DsGirls.GirlsRow girlsRow = girlsList[TxtFreeName.Text];
                if (girlsRow.属性 != "-")
                {
                    //属性も変更する
                    Attr atr;
                    if (Enum.TryParse<Attr>(girlsRow.属性, out atr))
                    {
                        CmbFreeAttr.SelectedValue = atr;
                        freeYellInfo.Attr = atr;
                    }
                }
            }

            PopSearchFreeGirlName.IsOpen = false;
            LstFreeGirlsName.SelectedValue = null;
        }
        #endregion

        #endregion

        #region フリー一覧タブ

        #region フリー一覧検索
        /// <summary>
        /// 検索条件クリア
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSearchFreeClearAll_Click(object sender, RoutedEventArgs e)
        {
            isEvent = false;
            CmbSearchFreeAttr.SelectedValue = string.Empty;
            CmbSearchFreeRare.SelectedValue = string.Empty;
            TxtSearchFreeCostDown.Text = string.Empty;
            TxtSearchFreeCostUp.Text = string.Empty;
            ChkSearchFreeSkill.IsChecked = false;

            isEvent = true;
            SearchFreeCard();
        }

        /// <summary>
        /// コンボ変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbSearchFree_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent)
            {
                SearchFreeCard();
            }
        }

        /// <summary>
        /// 文字列検索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtSearchFree_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isEvent)
            {
                SearchFreeCard();
            }
        }

        /// <summary>
        /// チェックボックス検索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkSearchFree_Click(object sender, RoutedEventArgs e)
        {
            if (isEvent)
            {
                SearchFreeCard();
            }
        }

        /// <summary>
        /// フリーカード検索
        /// </summary>
        private void SearchFreeCard()
        {
            EnumerableRowCollection<DataRow> search = dsFreeMember.Tables[0].AsEnumerable();

            search = Search.SearchText(TxtSearchFreeName.Text, search);

            var upTextList = new[]{
                new{ Text= TxtSearchFreeCostUp, Column="コスト"},
            };

            foreach (var t in upTextList)
            {
                if (!string.IsNullOrEmpty(t.Text.Text))
                {
                    int num = 0;
                    if (int.TryParse(t.Text.Text, out num))
                    {
                        search = search.Where(r => Convert.ToInt32(r[t.Column]) >= num);
                    }
                }
            }
            var downTextList = new[]{
                new{ Text= TxtSearchFreeCostDown, Column="コスト"},
            };

            foreach (var t in downTextList)
            {
                if (!string.IsNullOrEmpty(t.Text.Text))
                {
                    int num = 0;
                    if (int.TryParse(t.Text.Text, out num))
                    {
                        search = search.Where(r => Convert.ToInt32(r[t.Column]) <= num);
                    }
                }
            }

            if (!string.IsNullOrEmpty(CmbSearchFreeAttr.SelectedValue as string))
            {
                search = search.Where(r => r["属性"] as string == CmbSearchFreeAttr.SelectedValue as string);
            }
            if (!string.IsNullOrEmpty(CmbSearchFreeRare.SelectedValue as string))
            {
                search = search.Where(r => r["レア"] as string == CmbSearchFreeRare.SelectedValue as string);
            }
            if (ChkSearchFreeSkill.IsChecked == true)
            {
                search = search.Where(r => (bool)r["声援"] == true);
            }
            DgFreeList.ItemsSource = search.AsDataView();
        }
        #endregion

        /// <summary>
        /// フリー一覧から削除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDeleteFreeList_Click(object sender, RoutedEventArgs e)
        {
            if (DgFreeList.SelectedItem == null)
            {
                DialogWindow.Show(this, "フリー一覧の削除対象が選択されていません", DialogWindow.MessageType.Error);
                return;
            }

            var selectedRow = (DgFreeList.SelectedItem as DataRowView).Row as DsDispMember.MemberRow;
            RemoveFreeListDispRow(selectedRow);
            dsFreeMember.WriteXml(Utility.GetFilePath("yellFreeMember.xml"));
        }

        #region フリー一覧表示
        /// <summary>
        /// フリー一覧表示レコード追加
        /// </summary>
        /// <param name="row"></param>
        private void AddFreeListDispRow(DsMember.MemberRow row)
        {
            dsDispFreeMember.Member.ImportRow(row);
            var dispRow = dsDispFreeMember.Member[dsDispFreeMember.Member.Count - 1];

            //エール情報を更新する
            TabItem selectTab = TabYellMember.SelectedItem as TabItem;
            YellSet selectYellSet = selectTab.Content as YellSet;
            ShowFreeListYellInfoRow(selectYellSet.TargetInfo, dispRow);
        }

        /// <summary>
        /// フリー一覧表示レコード削除
        /// </summary>
        /// <param name="row"></param>
        private void RemoveFreeListDispRow(DsDispMember.MemberRow row)
        {
            string id = row.ID;
            var removeRow = dsFreeMember.Member.FirstOrDefault(r => r.ID == id);
            if (removeRow != null)
            {
                dsFreeMember.Member.RemoveMemberRow(removeRow);
            }
            dsDispFreeMember.Member.RemoveMemberRow(row);
        }
        #endregion

        #region フリー一覧エール情報表示
        /// <summary>
        /// フリー入力エール情報表示
        /// </summary>
        private void ShowFreeListYellInfo()
        {
            TabItem selectTab = TabYellMember.SelectedItem as TabItem;
            YellSet selectYellSet = selectTab.Content as YellSet;

            foreach (DsDispMember.MemberRow row in dsDispFreeMember.Member)
            {
                ShowFreeListYellInfoRow(selectYellSet.TargetInfo, row);
            }
        }

        /// <summary>
        /// フリー一覧エール情報表示（1行）
        /// </summary>
        /// <param name="targetInfo"></param>
        /// <param name="row"></param>
        private static void ShowFreeListYellInfoRow(TargetInfo targetInfo, DsDispMember.MemberRow row)
        {
            YellInfo yellInfo = new YellInfo()
            {
                Attr = (Attr)Enum.Parse(typeof(Attr), row.属性),
                Cost = row.コスト,
                Rank = row.進展,
                Rare = (Rare)Enum.Parse(typeof(Rare), row.レア),
                HasSkill = row.声援,
            };

            //経験値
            int getExp = YellUtility.CalcExp(targetInfo, false, yellInfo);
            row.経験値 = getExp;
            //大成功経験値
            getExp = YellUtility.CalcExp(targetInfo, true, yellInfo);
            row.大成功 = getExp;

            if (yellInfo.HasSkill)
            {
                int skillUpPer = YellUtility.CalcSkill(targetInfo.Rare, targetInfo.SkillLv, new List<YellInfo>() { yellInfo });
                row.声援Up = skillUpPer;
            }
            else
            {
                row.声援Up = 0;
            }
        }
        #endregion
        #endregion

        #region 対象情報変更
        /// <summary>
        /// 目標ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnBaseTarget_Click(object sender, RoutedEventArgs e)
        {
            this.PopBaseTarget.IsOpen = !this.PopBaseTarget.IsOpen;
            if (this.PopBaseTarget.IsOpen)
            {
                this.BtnBaseTarget.Style = FindResource("BlueButton") as Style;

                TBtnPopBaseTargetBase.IsChecked = true;
                TBtnPopBaseTargetCurrent.IsChecked = false;

                //ターゲット情報
                int maxLv = YellUtility.GetMaxLv(targetInfo.Rare);
                TxtPopBaseTargetTargetLv.Text = maxLv.ToString();

                //Silder                
                SldPopBaseTargetTargetLv.Minimum = 1;
                SldPopBaseTargetTargetLv.Maximum = maxLv;
                SldPopBaseTargetTargetLv.SmallChange = 1;

                //Targetリスト作成
                CreateTargetList();

                //目標情報表示
                ShowPopBaseTargetInfo();
            }
            else
            {
                this.BtnBaseTarget.Style = FindResource(typeof(Button)) as Style;
            }
        }

        private void ShowPopBaseTargetInfo()
        {
            //元情報をコピーする
            LblPopBaseTargetBaseLv.Content = targetInfo.Lv;
            LblPopBaseTargetBaseProgress.Content = targetInfo.Progress;
            LblPopBaseTargetBaseExp.Content = targetInfo.TotalExp;

            //現在情報
            LblPopBaseTargetCurrentLv.Content = targetInfoResult.Lv;
            LblPopBaseTargetCurrentProgress.Content = targetInfoResult.Progress;
            LblPopBaseTargetCurrentExp.Content = targetInfoResult.TotalExp;

            //ターゲット情報
            int maxLv = YellUtility.GetMaxLv(targetInfo.Rare);
            int targetLv;
            if (!int.TryParse(TxtPopBaseTargetTargetLv.Text, out targetLv))
            {
                targetLv = maxLv;
            }
            if (targetLv > maxLv) targetLv = maxLv;

            int targetExp = YellUtility.GetTotalExp(targetLv, 0);
            LblPopBaseTargetTargetExp.Content = targetExp;

            //必要経験値
            LblPopBaseTargetNeedExp.Content = targetExp - targetInfo.TotalExp;

            //枚数計算
            CalcTarget();
        }

        /// <summary>
        /// 目標枚数計算
        /// </summary>
        /// <param name="attr"></param>
        /// <param name="needExp"></param>
        private void CalcTarget()
        {
            int targetLv;
            if (int.TryParse(TxtPopBaseTargetTargetLv.Text.Trim().TrimStart('0'), out targetLv))
            {
                if (targetLv == 0) targetLv = 1;
                int targetExp = YellUtility.GetTotalExp(targetLv, 0);

                int needExp =0;
                if (TBtnPopBaseTargetBase.IsChecked ?? false)
                {
                    //初期値をもとにした計算
                    needExp = targetExp - targetInfo.TotalExp;
                }
                else if (TBtnPopBaseTargetCurrent.IsChecked ?? false)
                {
                    //初期値をもとにした計算
                    needExp = targetExp - targetInfoResult.TotalExp;
                }

                LblPopBaseTargetNeedExp.Content = needExp;

                foreach (DsYellTarget.TargetRow row in dsYellTarget.Target)
                {
                    YellInfo yellInfo = new YellInfo()
                    {
                        Attr = (Attr)Enum.Parse(typeof(Attr), row.属性),
                        Cost = row.コスト,
                        Rank = row.進展,
                        Rare = (Rare)Enum.Parse(typeof(Rare), row.レア),
                    };

                    int exp = YellUtility.CalcExp(targetInfo, false, yellInfo);
                    int successExp = YellUtility.CalcExp(targetInfo, true, yellInfo);
                    row.経験値 = exp;

                    row.枚数 = (int)Math.Ceiling((double)needExp / exp);
                    row.大成功 = (int)Math.Ceiling((double)needExp / successExp);
                }
            }
        }

        /// <summary>
        /// ターゲット対象作成
        /// </summary>
        private void CreateTargetList()
        {
            dsYellTarget.Clear();
            foreach (string attr in Enum.GetNames(typeof(Attr)))
            {
                dsYellTarget.Target.AddTargetRow(attr, 35, "N", 1, "久保田友季", 0, 0, 0);
            }
            foreach (string attr in Enum.GetNames(typeof(Attr)))
            {
                dsYellTarget.Target.AddTargetRow(attr, 40, "R", 1, "荒井薫", 0, 0, 0);
            }
            foreach (string attr in Enum.GetNames(typeof(Attr)))
            {
                dsYellTarget.Target.AddTargetRow(attr, 2, "N", 1, "", 0, 0, 0);
            }
            foreach (string attr in Enum.GetNames(typeof(Attr)))
            {
                dsYellTarget.Target.AddTargetRow(attr, 6, "HN", 1, "", 0, 0, 0);
            }
            foreach (string attr in Enum.GetNames(typeof(Attr)))
            {
                dsYellTarget.Target.AddTargetRow(attr, 10, "R", 1, "", 0, 0, 0);
            }
            foreach (string attr in Enum.GetNames(typeof(Attr)))
            {
                dsYellTarget.Target.AddTargetRow(attr, 12, "HR", 1, "", 0, 0, 0);
            }
            foreach (string attr in Enum.GetNames(typeof(Attr)))
            {
                dsYellTarget.Target.AddTargetRow(attr, 14, "SR", 1, "", 0, 0, 0);
            }
            DgTarget.ItemsSource = dsYellTarget.Target;
        }

        /// <summary>
        /// 目標Lv変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtPopBaseTargetTargetLv_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// 目標Lv変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtPopBaseTargetTargetLv_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isEvent)
            {
                int targetLv;
                if (int.TryParse(TxtPopBaseTargetTargetLv.Text, out targetLv))
                {
                    if (targetLv == 0) targetLv = 1;
                    int maxLv = YellUtility.GetMaxLv(targetInfo.Rare);
                    if (targetLv > maxLv) targetLv = maxLv;

                    int targetExp = YellUtility.GetTotalExp(targetLv, 0);
                    LblPopBaseTargetTargetExp.Content = targetExp;

                    if (SldPopBaseTargetTargetLv.Value != targetLv)
                    {
                        isEvent = false;
                        SldPopBaseTargetTargetLv.Value = targetLv;
                        isEvent = true;
                    }

                    CalcTarget();
                }
                else
                {
                    LblPopBaseTargetTargetExp.Content = "0";

                    LblPopBaseTargetNeedExp.Content = "0";

                    isEvent = false;
                    SldPopBaseTargetTargetLv.Value = 0;
                    isEvent = true;

                    CalcTarget();
                }
            }
        }

        /// <summary>
        /// 現在値をもとに目標数計算する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBtnPopBaseTargetCurrent_Click(object sender, RoutedEventArgs e)
        {
            TBtnPopBaseTargetBase.IsChecked = false;

            //枚数計算
            CalcTarget();
        }

        /// <summary>
        /// 初期値をもとに目標数計算する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBtnPopBaseTargetBase_Click(object sender, RoutedEventArgs e)
        {
            TBtnPopBaseTargetCurrent.IsChecked = false;

            //枚数計算
            CalcTarget();

        }

        /// <summary>
        /// 対象情報クリア
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnBaseClear_Click(object sender, RoutedEventArgs e)
        {
            LblBaseName.Content = string.Empty;
            ImgBase.Source = null;
            TxtBaseLv.Text = "1";
            TxtBaseExp.Text = "0";
            TxtBaseSkillLv.Text = "1";

            targetInfo.Lv = 1;
            targetInfo.Exp = 0;
            targetInfo.SkillLv = 0;
        }

        /// <summary>
        /// 対象Rare変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbBaseRare_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInit)
            {
                if (e.AddedItems.Count > 0)
                {

                    targetInfo.Rare =((KeyValuePair<Rare,string>)e.AddedItems[0]).Key;
                    // targetInfo.Rare = (Rare)CmbBaseRare.SelectedValue;

                    ReCalcAll();
                }
            }
        }

        /// <summary>
        /// 対象声援Lv変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtBaseSkillLv_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInit)
            {
                int lv;
                int.TryParse(TxtBaseSkillLv.Text,out lv);
                if (lv <= 0) lv = 1;
                targetInfo.SkillLv = lv;

                ReCalcAll();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtBaseLv_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInit)
            {
                int lv;
                int.TryParse(TxtBaseLv.Text, out lv);
                if (lv <= 0) lv = 1;
                targetInfo.Lv = lv;

                ReCalcAll();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtBaseExp_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isInit)
            {
                int progress;
                int.TryParse(TxtBaseExp.Text, out progress);
                if (progress <= 0) progress = 0;
                if (progress > 99) progress = 99;

                int needExp = dsYellInfo.LvExp.FirstOrDefault(r => r.Lv == targetInfo.Lv).Exp;
                int exp = (int)Math.Truncate((double)needExp * (double)progress / 100);

                targetInfo.Exp = exp;
                targetInfo.Progress = progress;

                ReCalcAll();
            }
        }

        /// <summary>
        /// 対象属性変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbBaseAttr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInit)
            {
                targetInfo.Attr = (Attr)CmbBaseAttr.SelectedValue;

                ReCalcAll();
            }
        }

        #endregion

        #region セット操作

        /// <summary>
        /// セット全クリア
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClearSet_Click(object sender, RoutedEventArgs e)
        {
            if (!DialogWindow.Show(this, "全部クリアしてよろしいですか？", "確認", DialogWindow.MessageType.Confirm))
            {
                return;
            }
            isEvent = false;
            //先頭タブ以外を削除する
            for (int i = TabYellMember.Items.Count; i > 1; i--)
            {
                TabItem selectTab = TabYellMember.Items[i-1] as TabItem;
                YellSet selectYellSet = selectTab.Content as YellSet;
                selectYellSet.Calc -= set_Calc;
                TabYellMember.Items.Remove(selectTab);
            }

            isEvent = true;
            TabYellMember.SelectedIndex = 0;

            //先頭タブのクリア
            TabItem tab = TabYellMember.Items[0] as TabItem;
            YellSet yellSet = tab.Content as YellSet;
            yellSet.RemoveAllYellMember();
            this.targetInfoResult= yellSet.TargetInfoAfter;

            //再計算
            ReCalcAll();
        }

        /// <summary>
        /// セット追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAddSet_Click(object sender, RoutedEventArgs e)
        {
            if(TabYellMember.Items.Count == 10)
            {
                DialogWindow.Show(this, "これ以上、セットを追加できません", DialogWindow.MessageType.Error);
                return;
            }

            TabItem lastTab = TabYellMember.Items[TabYellMember.Items.Count - 1] as TabItem;
            TabItem tab = new TabItem();
            tab.Header = string.Format("No.{0}(0)", TabYellMember.Items.Count + 1);
            tab.Width = 70;
            tab.Padding = new Thickness(2, 0, 2, 0);
            tab.HorizontalAlignment = HorizontalAlignment.Left;
            YellSet set = new YellSet();
            set.Initialize((lastTab.Content as YellSet).TargetInfoAfter);
            set.Calc += set_Calc;
            tab.Content = set;
            TabYellMember.Items.Add(tab);
            TabYellMember.SelectedValue = tab;
            this.targetInfoResult = set.TargetInfoAfter;
        }

        void set_Calc(object sender, CalcEvent e)
        {
            ReCalcAll();
        }

        /// <summary>
        /// セット削除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnRemoveSet_Click(object sender, RoutedEventArgs e)
        {
            if (TabYellMember.Items.Count == 1)
            {
                DialogWindow.Show(this, "これ以上、セットを削除できません", DialogWindow.MessageType.Error);
                return;
            }
            int selectedIndex = TabYellMember.SelectedIndex;
            TabItem nextTab = TabYellMember.SelectedIndex != (TabYellMember.Items.Count - 1) ? TabYellMember.Items[TabYellMember.SelectedIndex+1] as TabItem : null;
            TabItem selectTab = TabYellMember.SelectedItem as TabItem;
            YellSet selectYellSet = selectTab.Content as YellSet;
            //削除タブよりひとつ前の情報を、ひとつ後のタブに引き継ぐ
            if (TabYellMember.SelectedIndex == 0)
            {
                (nextTab.Content as YellSet).Initialize(targetInfo);
            }
            else
            {
                if (nextTab != null)
                {
                    TabItem prevTab = TabYellMember.Items[TabYellMember.SelectedIndex - 1] as TabItem;
                    (nextTab.Content as YellSet).Initialize((prevTab.Content as YellSet).TargetInfoAfter);
                }
                else
                {
                    TabItem lastTab = TabYellMember.Items[TabYellMember.Items.Count - 2] as TabItem;
                    this.targetInfoResult = (lastTab.Content as YellSet).TargetInfoAfter;
                }
            }
            selectYellSet.Calc -= set_Calc;
            TabYellMember.Items.Remove(selectTab);
            TabYellMember.SelectedIndex = selectedIndex>= TabYellMember.Items.Count ? selectedIndex-1 : selectedIndex;

            //タブ名書き換え
            for (int i = selectedIndex; i < TabYellMember.Items.Count; i++)
            {
                TabItem tab =(TabYellMember.Items[i] as TabItem); 
                int count= (tab.Content as YellSet).MemberCount;
                tab.Header = string.Format("No.{0}({1})", i+1,count);
            }

            //再計算
            ReCalcAfter(selectedIndex);
        }

        /// <summary>
        /// セット選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabYellMember_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isInit && isEvent)
            {
                //フリーエール情報の更新
                ShowFreeYellInfo();
                ShowFreeListYellInfo();
            }
        }
        #endregion

        #endregion

        #region 所持カードタブ

        #region 所持カード画面デッキ検索
        private void BtnSearchOwnCard_Click(object sender, RoutedEventArgs e)
        {
            PopSearchOwnCard.IsOpen = !PopSearchOwnCard.IsOpen;
            //閉じた場合は検索クリア
            if (!PopSearchOwnCard.IsOpen)
            {
                SeachOwnCardClearAll();
            }
            else
            {
            }
        }

        private void SeachOwnCardClearAll()
        {
            isEvent = false;
            TxtPopSearchOwnCardName.Text = string.Empty;
            TxtPopSearchOwnCardAtkUp.Text = string.Empty;
            TxtPopSearchOwnCardAtkDown.Text = string.Empty;
            TxtPopSearchOwnCardCostDown.Text = string.Empty;
            TxtPopSearchOwnCardCostUp.Text = string.Empty;
            TxtPopSearchOwnCardDefDown.Text = string.Empty;
            TxtPopSearchOwnCardDefUp.Text = string.Empty;
            CmbPopSearchOwnCardAttr.SelectedValue = string.Empty;
            isEvent = true;
            SearchOwnCard();
        }

        private void TxtPopSearchOwnCard_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isEvent)
            {
                //検索条件の重複を防ぐため名称検索をクリアする
                TxtSearchOwnCard.Text = string.Empty;

                SearchOwnCard();
            }
        }

        /// <summary>
        /// 所持カード検索
        /// </summary>
        private void SearchOwnCard()
        {
            EnumerableRowCollection<DataRow> search = dsDispCards.Tables[0].AsEnumerable();

            search = Search.SearchText(TxtPopSearchOwnCardName.Text, search);

            var upTextList = new[]{
                new{ Text= TxtPopSearchOwnCardCostUp, Column="コスト"},
                new{ Text= TxtPopSearchOwnCardAtkUp, Column="攻援"},
                new{ Text= TxtPopSearchOwnCardDefUp, Column="守援"},
            };

            foreach (var t in upTextList)
            {
                if (!string.IsNullOrEmpty(t.Text.Text))
                {
                    int num = 0;
                    if (int.TryParse(t.Text.Text, out num))
                    {
                        search = search.Where(r => Convert.ToInt32(r[t.Column]) >= num);
                    }
                }
            }
            var downTextList = new[]{
                new{ Text= TxtPopSearchOwnCardCostDown, Column="コスト"},
                new{ Text= TxtPopSearchOwnCardAtkDown, Column="攻援"},
                new{ Text= TxtPopSearchOwnCardDefDown, Column="守援"},
            };

            foreach (var t in downTextList)
            {
                if (!string.IsNullOrEmpty(t.Text.Text))
                {
                    int num = 0;
                    if (int.TryParse(t.Text.Text, out num))
                    {
                        search = search.Where(r => Convert.ToInt32(r[t.Column]) <= num);
                    }
                }
            }

            if (!string.IsNullOrEmpty(CmbPopSearchOwnCardAttr.SelectedValue as string))
            {
                search = search.Where(r => r["属性"] as string == CmbPopSearchOwnCardAttr.SelectedValue as string);
            }

            DgCards.ItemsSource = search.OrderBy(r => r["表示順"]).AsDataView();
        }

        private void BtnPopSearchOwnCardClearAll_Click(object sender, RoutedEventArgs e)
        {
            SeachOwnCardClearAll();
        }

        private void CmbPopSearchOwnCardAttr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent)
            {
                //検索条件の重複を防ぐため名称検索をクリアする
                TxtSearchOwnCard.Text = string.Empty;

                SearchOwnCard();
            }
        }
        #endregion

        private void TxtSearchOwnCard_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            DataGrid grid = DgCards;
            DataSet ds = dsDispCards;
           Search.SearchName(textBox, grid, ds);
        }

        /// <summary>
        /// エール対象選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnOwnCardSelect_Click(object sender, RoutedEventArgs e)
        {
            if (DgCards.SelectedItem == null)
            {
                DialogWindow.Show(this, "選択されていません", DialogWindow.MessageType.Error);
                return;
            }
            DsYellDispCards.DispCardRow dispRow = (DgCards.SelectedItem as DataRowView).Row as DsYellDispCards.DispCardRow;
            DsYellCards.CardsRow cardRow = dsYellCards.Cards.FirstOrDefault(r => r.CommonID == dispRow.CommonID);
            DsGirls.GirlsRow girlRow = girlsList[cardRow.名前];

            //エール対象に設定する
            CmbBaseAttr.SelectedValue = (Attr)Enum.Parse(typeof(Attr), girlRow.属性);
            LblBaseName.Content = dispRow.名前;
            ImgBase.Source = dispRow.画像 as BitmapImage;
            CmbBaseRare.SelectedValue = (Rare)Enum.Parse(typeof(Rare), cardRow.レア);
            TxtBaseLv.Text = cardRow.Lv.ToString();
            TxtBaseExp.Text = dispRow.成長.ToString();
            TxtBaseSkillLv.Text = cardRow.スキルLv.ToString();

            TbYell.IsSelected = true;
        }

        /// <summary>
        /// Deckシミュレーターの所持カード情報を読みこむ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnOwnCardLoad_Click(object sender, RoutedEventArgs e)
        {
            #region 所持カード情報読み込み
            //所持カード情報読み込み
            if (File.Exists(Utility.GetFilePath("cards.xml")))
            {
                DsCards dsCards = new DsCards();
                dsCards.ReadXml(Utility.GetFilePath("cards.xml"));

                foreach (DsCards.CardsRow cardRow in dsCards.Cards)
                {
                    DsYellCards.CardsRow yellCardRow = dsYellCards.Cards.FirstOrDefault(r => !r.IsDeckIDNull() && r.DeckID == cardRow.ID);
                    if (cardRow.Is好感度Null())
                    {
                        if (cardRow.進展 == 3)
                        {
                            cardRow.好感度 = 5;
                        }
                        else if (cardRow.進展 == 2)
                        {
                            cardRow.好感度 = 3;
                        }
                        else
                        {
                            cardRow.好感度 = 1;
                        }
                    }
                    if (yellCardRow != null)
                    {
                        //更新
                        Utility.CopyRow(cardRow, yellCardRow);
                        UpdateCardRow(yellCardRow);
                    }
                    else
                    {

                        //新規追加
                        dsYellCards.Cards.ImportRow(cardRow);
                        yellCardRow = dsYellCards.Cards[dsYellCards.Cards.Count - 1];
                        //共通ID
                        yellCardRow.CommonID = "D" + cardRow.ID;
                        yellCardRow.DeckID = cardRow.ID;
                        yellCardRow.YellID = string.Empty;

                        //成長
                        int progress = 0;
                        if (!cardRow.Is成長Null())
                        {
                            int maxLv = YellUtility.GetMaxLv((Rare)Enum.Parse(typeof(Rare), cardRow.レア));
                            //最大レベル出なければ成長を取得する
                            if (maxLv != cardRow.Lv)
                            {
                                progress = cardRow.成長;
                            }
                        }


                        //経験値の初期化
                        yellCardRow.経験値 = YellUtility.GetTotalExp(yellCardRow.Lv, progress);

                        CreateDispCardRow(yellCardRow);

                    }

                    if (!cardsList.ContainsKey(yellCardRow.CommonID))
                    {
                        cardsList.Add(yellCardRow.CommonID, yellCardRow);
                    }
                }
                dsYellCards.WriteXml(Utility.GetFilePath("yellcards.xml"));

                DgCards.ItemsSource = dsDispCards.DispCard.OrderBy(r => r.表示順).AsDataView();
            }
            else
            {
                DialogWindow.Show(this, "デッキシミュレーターのカード情報[cards.xml]が見つかりません", DialogWindow.MessageType.Error);
            }
            #endregion
        }

        private void BtnOwnCardSave_Click(object sender, RoutedEventArgs e)
        {
        }

        #region カード編集

        /// <summary>
        /// 所持カード選択
        /// </summary>
        private void SelectDgCardsRow()
        {
            DsYellDispCards.DispCardRow dispRow = (DgCards.SelectedItem as DataRowView).Row as DsYellDispCards.DispCardRow;
            DsYellCards.CardsRow row = dsYellCards.Cards.FirstOrDefault(r => r.CommonID == dispRow.CommonID);
            CmbCardGirlName.SelectedValue = row.名前;
            CmbCardGirlRare.Text = row.レア;
            TxtCardGirlType.Text = row.種別;
            CmbCardGirlRank.Text = row.進展.ToString();
            CmbCardGirlFavor.SelectedValue = row.好感度;
            TxtCardGirlCost.Text = row.コスト.ToString();
            TxtCardGirlLv.Text = row.Lv.ToString();
            int progress = GetProgress(row);
            TxtCardGirlProgress.Text = progress.ToString();
            TxtCardGirlAtk.Text = row.攻援.ToString();
            TxtCardGirlDef.Text = row.守援.ToString();
            CmbCardGirlSkill.SelectedValue = row.スキル;
            ChkCardGirlAllSkill.IsChecked = row.全属性スキル;
            TxtCardGirlSkillLv.Text = row.スキルLv.ToString();
            TxtCardGirlSpecial.Text = row.スペシャル.ToString();
            TxtCardGirlBonus.Text = row.ボーナス.ToString();
            ChkCardGirlBonus.IsChecked = row.ボーナス有無;
            ChkCardGirlDummy.IsChecked = row.ダミー;
            TxtCardGirlDispOrder.Text = row.表示順.ToString();
            TxtCardGirlFree1.Text = row.フリー1;
            TxtCardGirlFree2.Text = row.フリー2;
            TxtCardGirlFree3.Text = row.フリー3;
            SetCardGirlImage(row.画像);

            SetCardGirlEtcButton();

            currentEditID = dispRow.CommonID;
            //名前の変更不可
            CmbCardGirlName.IsEnabled = false;
            BtnCardAdd.IsEnabled = false;
            BtnSearchGirlName.IsEnabled = false;
            BtnCardUpd.IsEnabled = true;
        }

        /// <summary>
        /// 成長率取得
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private static int GetProgress(DsYellCards.CardsRow row)
        {
            int progress;
            Rare rare = (Rare)Enum.Parse(typeof(Rare), row.レア);
            int maxLv = YellUtility.GetMaxLv(rare);
            if (row.Lv >= maxLv)
            {
                //最大レベル以上の場合は成長を１００
                progress = 100;
            }
            else
            {
                int lv;
                YellUtility.GetProgress(row.経験値, out lv, out progress);
                //実際のレベルと違っていた場合は成長をリセットする
                if (lv != row.Lv)
                {
                    progress = 0;
                }
            }
            return progress;
        }

        /// <summary>
        /// カード追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCardAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DsYellCards.CardsRow cardRow = dsYellCards.Cards.NewCardsRow();
                cardRow.YellID = (++cardMax).ToString();
                cardRow.CommonID = "Y" + cardRow.YellID;
                cardRow.名前 = CmbCardGirlName.SelectedValue.ToString();
                cardRow.コスト = string.IsNullOrEmpty(TxtCardGirlCost.Text) ? 1 : Convert.ToInt32(TxtCardGirlCost.Text);
                cardRow.Lv = string.IsNullOrEmpty(TxtCardGirlLv.Text) ? 1 : Convert.ToInt32(TxtCardGirlLv.Text);
                int progress = string.IsNullOrEmpty(TxtCardGirlProgress.Text) ? 0 : Convert.ToInt32(TxtCardGirlProgress.Text);
                cardRow.経験値 = YellUtility.GetTotalExp(cardRow.Lv ,progress);
                cardRow.スキル = CmbCardGirlSkill.SelectedValue.ToString();
                cardRow.スキルLv = string.IsNullOrEmpty(TxtCardGirlSkillLv.Text) ? 1 : Convert.ToInt32(TxtCardGirlSkillLv.Text);
                cardRow.レア = CmbCardGirlRare.Text;
                cardRow.攻援 = string.IsNullOrEmpty(TxtCardGirlAtk.Text) ? 0 : Convert.ToInt32(TxtCardGirlAtk.Text);
                cardRow.守援 = string.IsNullOrEmpty(TxtCardGirlDef.Text) ? 0 : Convert.ToInt32(TxtCardGirlDef.Text);
                cardRow.種別 = TxtCardGirlType.Text;
                cardRow.進展 = Convert.ToInt32(CmbCardGirlRank.Text);
                cardRow.好感度 = (int)CmbCardGirlFavor.SelectedValue;
                cardRow.全属性スキル = ChkCardGirlAllSkill.IsChecked ?? false;
                cardRow.ボーナス有無 = ChkCardGirlBonus.IsChecked ?? false;
                cardRow.ボーナス = string.IsNullOrEmpty(TxtCardGirlBonus.Text) ? 0 : Convert.ToInt32(TxtCardGirlBonus.Text);
                cardRow.スペシャル = string.IsNullOrEmpty(TxtCardGirlSpecial.Text) ? 0 : Convert.ToDouble(TxtCardGirlSpecial.Text);
                cardRow.ダミー = ChkCardGirlDummy.IsChecked ?? false;
                cardRow.画像 = TxtCardGirlImageName.Text;
                cardRow.表示順 = string.IsNullOrEmpty(TxtCardGirlDispOrder.Text) ? 99999 : Convert.ToInt32(TxtCardGirlDispOrder.Text);
                cardRow.フリー1 = TxtCardGirlFree1.Text;
                cardRow.フリー2 = TxtCardGirlFree2.Text;
                cardRow.フリー3 = TxtCardGirlFree3.Text;

                dsYellCards.Cards.AddCardsRow(cardRow);

                //表示順再設定
                ReDispOrder(cardRow);

                dsYellCards.WriteXml(Utility.GetFilePath("yellcards.xml"));

                if (!cardsList.ContainsKey(cardRow.CommonID))
                {
                    cardsList.Add(cardRow.CommonID, cardRow);
                }

                CreateDispCardRow(cardRow);

                ClearCardInput();
            }
            catch (Exception ex)
            {
                DialogWindow.Show(this, "未入力か、不正な値が入力されています", "入力エラー", DialogWindow.MessageType.Error);
            }
        }

        private void BtnCardDel_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentEditID))
            {
                if (!DialogWindow.Show(this, "削除してもよろしいですか？", "確認", DialogWindow.MessageType.Confirm))
                {
                    return;
                }

                DsYellCards.CardsRow cardRow = dsYellCards.Cards.FirstOrDefault(r => r.CommonID == currentEditID);
                if (cardsList.ContainsKey(cardRow.CommonID))
                {
                    cardsList.Remove(cardRow.CommonID);
                }
                dsYellCards.Cards.RemoveCardsRow(cardRow);
                dsYellCards.AcceptChanges();

                dsYellCards.WriteXml(Utility.GetFilePath("yellcards.xml"));

                DeleteCardRow(currentEditID);

                DgCards.SelectedItem = null;

                ClearCardInput();
            }
        }

        private void BtnCardGirlEtc_Click(object sender, RoutedEventArgs e)
        {
            PopCardGirlEdit.IsOpen = !PopCardGirlEdit.IsOpen;
            SetCardGirlEtcButton();
        }

        /// <summary>
        /// その他ボタン表示更新
        /// </summary>
        private void SetCardGirlEtcButton()
        {
            if ((ChkCardGirlDummy.IsChecked ?? false) ||
                !string.IsNullOrEmpty(TxtCardGirlFree1.Text) ||
                !string.IsNullOrEmpty(TxtCardGirlFree2.Text) ||
                !string.IsNullOrEmpty(TxtCardGirlFree3.Text) ||
                (ChkCardGirlBonus.IsChecked ?? false) ||
                (!string.IsNullOrEmpty(TxtCardGirlSpecial.Text) && TxtCardGirlSpecial.Text != "0") ||
                (!string.IsNullOrEmpty(TxtCardGirlBonus.Text) && TxtCardGirlBonus.Text != "0")
            )
            {
                BtnCardGirlEtc.Style = FindResource("BlueButton") as Style;
            }
            else
            {
                BtnCardGirlEtc.Style = FindResource(typeof(Button)) as Style;
            }
        }

        private void ReDispOrder(DsYellCards.CardsRow cardRow)
        {
            //ID違いで同一表示順が存在した場合は振り直しを行う
            DsYellCards.CardsRow orderRow = dsYellCards.Cards.FirstOrDefault(r => r.CommonID != cardRow.CommonID && r.表示順 == cardRow.表示順);
            if (orderRow != null)
            {
                var reorderList = dsYellCards.Cards.Where(r => r.表示順 >= cardRow.表示順 && r.CommonID != cardRow.CommonID).OrderBy(r => r.表示順).ToList();
                int currentDispOrder = cardRow.表示順;
                foreach (var reorderRow in reorderList)
                {
                    //すでに存在している場合は振りなおす
                    if (reorderRow.表示順 == currentDispOrder)
                    {
                        reorderRow.表示順 = ++currentDispOrder;
                        UpdateCardRow(reorderRow);
                    }
                    else
                    {
                        //一致しているのが存在しなくなるまで繰り返す
                        break;
                    }
                }
                dsYellCards.AcceptChanges();
            }
        }

        /// <summary>
        /// カード情報更新ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCardUpd_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentEditID))
            {
                try
                {
                    DsYellCards.CardsRow cardRow = dsYellCards.Cards.FirstOrDefault(r => r.CommonID == currentEditID);
                    cardRow.コスト = string.IsNullOrEmpty(TxtCardGirlCost.Text) ? 1 : Convert.ToInt32(TxtCardGirlCost.Text);
                    cardRow.Lv = string.IsNullOrEmpty(TxtCardGirlLv.Text) ? 1 : Convert.ToInt32(TxtCardGirlLv.Text);
                    cardRow.スキル = CmbCardGirlSkill.SelectedValue.ToString();
                    cardRow.スキルLv = string.IsNullOrEmpty(TxtCardGirlSkillLv.Text) ? 1 : Convert.ToInt32(TxtCardGirlSkillLv.Text);
                    cardRow.レア = CmbCardGirlRare.Text;
                    int progress = string.IsNullOrEmpty(TxtCardGirlProgress.Text) ? 0 : Convert.ToInt32(TxtCardGirlProgress.Text);
                    cardRow.経験値 = YellUtility.GetTotalExp(cardRow.Lv, progress);
                    cardRow.攻援 = string.IsNullOrEmpty(TxtCardGirlAtk.Text) ? 0 : Convert.ToInt32(TxtCardGirlAtk.Text);
                    cardRow.守援 = string.IsNullOrEmpty(TxtCardGirlDef.Text) ? 0 : Convert.ToInt32(TxtCardGirlDef.Text);
                    cardRow.種別 = TxtCardGirlType.Text;
                    cardRow.進展 = Convert.ToInt32(CmbCardGirlRank.Text);
                    cardRow.好感度 = (int)CmbCardGirlFavor.SelectedValue;
                    cardRow.全属性スキル = ChkCardGirlAllSkill.IsChecked ?? false;
                    cardRow.ボーナス有無 = ChkCardGirlBonus.IsChecked ?? false;
                    cardRow.ボーナス = string.IsNullOrEmpty(TxtCardGirlBonus.Text) ? 0 : Convert.ToInt32(TxtCardGirlBonus.Text);
                    cardRow.スペシャル = string.IsNullOrEmpty(TxtCardGirlSpecial.Text) ? 0 : Convert.ToDouble(TxtCardGirlSpecial.Text);
                    cardRow.ダミー = ChkCardGirlDummy.IsChecked ?? false;
                    cardRow.画像 = TxtCardGirlImageName.Text;
                    cardRow.表示順 = string.IsNullOrEmpty(TxtCardGirlDispOrder.Text) ? 99999 : Convert.ToInt32(TxtCardGirlDispOrder.Text);
                    cardRow.フリー1 = TxtCardGirlFree1.Text;
                    cardRow.フリー2 = TxtCardGirlFree2.Text;
                    cardRow.フリー3 = TxtCardGirlFree3.Text;
                    cardRow.AcceptChanges();

                    //表示順再設定
                    ReDispOrder(cardRow);

                    dsYellCards.WriteXml(Utility.GetFilePath("yellcards.xml"));

                    UpdateCardRow(cardRow);

                    ReCalcAll();

                    DgCards.SelectedItem = null;
                    ClearCardInput();
                }
                catch (Exception ex)
                {
                    DialogWindow.Show(this, "未入力か、不正な値が入力されています", "入力エラー", DialogWindow.MessageType.Error);
                }
            }
        }

        private void BtnCardClear_Click(object sender, RoutedEventArgs e)
        {
            DgCards.SelectedItem = null;
            ClearCardInput();
        }

        private void BtnCardGirlImage_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "イメージファイル(*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.wdp;*.tiff)|*.bmp;*.gif;*.jpg;*.jpeg;*.png;*.wdp;*.tiff";
            if (dialog.ShowDialog() == true)
            {
                SetCardGirlImage(dialog.FileName);
            }
            else
            {
                SetCardGirlImage(string.Empty);
            }
        }

        private void SetCardGirlImage(string fileName)
        {
            if (!string.IsNullOrEmpty(fileName))
            {
                TxtCardGirlImageName.Text = fileName;
                TxtCardGirlImageName.ToolTip = fileName;
                Size scaleSize = new Size(240, 240);
                BitmapImage s = Images.LoadImage(fileName, ref  scaleSize);
                if (s != null)
                {
                    this.BtnCardGirlImage.Style = FindResource("BlueButton") as Style;
                    this.BtnCardGirlImage.ToolTip = new Image() { Opacity = 0.8, Width = scaleSize.Width, Height = scaleSize.Height, Source = s };
                }
                else
                {
                    this.BtnCardGirlImage.Style = FindResource("RedButton") as Style;
                    this.BtnCardGirlImage.ToolTip = null;

                }
            }
            else
            {
                TxtCardGirlImageName.Text = string.Empty;
                TxtCardGirlImageName.ToolTip = null;

                this.BtnCardGirlImage.Style = FindResource(typeof(Button)) as Style;
                this.BtnCardGirlImage.ToolTip = null;
            }
        }

        private void BtnSearchGirlName_Click(object sender, RoutedEventArgs e)
        {
            TxtPopSearchGirlName.Text = string.Empty;
            PopSearchGirlName.IsOpen = true;
        }

        private void TxtPopSearchGirlName_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            Search.SearchListName(textBox, LstGirlsName, nameList);

        }

        private void LstGirlsName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CmbCardGirlName.SelectedValue = LstGirlsName.SelectedValue;
            PopSearchGirlName.IsOpen = false;
            LstGirlsName.SelectedValue = null;
        }

        private void DgCards_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgCards.SelectedItem != null)
            {
                SelectDgCardsRow();
            }
        }

        private void CreateDispCardRow(DsYellCards.CardsRow cardRow)
        {
            DsYellDispCards.DispCardRow dispRow = dsDispCards.DispCard.NewDispCardRow();
            DsGirls.GirlsRow girlRow = girlsList[cardRow.名前];

            dispRow.CommonID = cardRow.CommonID;
            SetYellDispRow(cardRow, dispRow);

            Dispatcher.BeginInvoke((Action<string>)((id) =>
            {
                if (cardsList.ContainsKey(id))
                {
                    DsYellCards.CardsRow iCardRow = cardsList[id];
                    DsYellDispCards.DispCardRow iDispRow = dsDispCards.DispCard.FirstOrDefault(r => r.CommonID == id);
                    Size scaleSize = new Size(240, 240);
                    iDispRow.画像 = Images.LoadImage(iCardRow.画像, ref  scaleSize);
                }
            }), System.Windows.Threading.DispatcherPriority.Background, cardRow.CommonID);
            dsDispCards.DispCard.AddDispCardRow(dispRow);
        }

        private void DeleteCardRow(string editId)
        {
            DsYellDispCards.DispCardRow dispCardRow = dsDispCards.DispCard.FirstOrDefault(r => r.CommonID == editId);
            dsDispCards.DispCard.RemoveDispCardRow(dispCardRow);
            dsDispCards.AcceptChanges();
        }

        private void UpdateCardRow(DsYellCards.CardsRow cardRow)
        {
            //表示用カード更新
            DsYellDispCards.DispCardRow dispRow = dsDispCards.DispCard.FirstOrDefault(r => r.CommonID == cardRow.CommonID);
            SetYellDispRow(cardRow, dispRow);
            Size scaleSize = new Size(240, 240);
            dispRow.画像 = Images.LoadImage(cardRow.画像, ref  scaleSize);
            dsDispCards.AcceptChanges();

        }

        /// <summary>
        /// 表示用に設定する
        /// </summary>
        /// <param name="cardRow"></param>
        /// <param name="dispRow"></param>
        private void SetYellDispRow(DsYellCards.CardsRow cardRow, DsYellDispCards.DispCardRow dispRow)
        {
            DsGirls.GirlsRow girlRow = girlsList[cardRow.名前];
            dispRow.名前 = GetDispName(cardRow);
            dispRow.姓名 = GetSortName(cardRow);
            dispRow.なまえ = girlRow.なまえ;
            dispRow.属性 = girlRow.属性;
            dispRow.コスト = cardRow.コスト;
            dispRow.Lv = cardRow.Lv;
            int progress = GetProgress(cardRow);
            dispRow.成長 = progress;
            dispRow.経験値 = cardRow.経験値;
            dispRow.進展 = cardRow.進展;
            dispRow.好感度 = cardRow.好感度;
            dispRow.スキル = GetSkillName(cardRow);
            dispRow.攻スキル = GetAtkSkillPower(cardRow);
            dispRow.守スキル = GetDefSkillPower(cardRow);
            dispRow.レア = cardRow.レア;
            dispRow.攻援 = cardRow.攻援;
            dispRow.守援 = cardRow.守援;
            dispRow.スキル = GetSkillName(cardRow);
            dispRow.ダミー = cardRow.ダミー;
            dispRow.表示順 = cardRow.表示順;
            dispRow.フリー1 = cardRow.フリー1;
            dispRow.フリー2 = cardRow.フリー2;
            dispRow.フリー3 = cardRow.フリー3;
        }

        private void ClearCardInput()
        {
            //CmbCardGirlName.SelectedValue = string.Empty;
            CmbCardGirlRare.Text = string.Empty;
            TxtCardGirlType.Text = string.Empty;
            CmbCardGirlRank.Text = string.Empty;
            TxtCardGirlCost.Text = string.Empty;
            TxtCardGirlLv.Text = string.Empty;
            CmbCardGirlFavor.SelectedValue = 1;
            TxtCardGirlProgress.Text = "0";
            TxtCardGirlAtk.Text = string.Empty;
            TxtCardGirlDef.Text = string.Empty;
            CmbCardGirlSkill.SelectedValue = string.Empty;
            ChkCardGirlAllSkill.IsChecked = false;
            TxtCardGirlSkillLv.Text = "1";
            TxtCardGirlSpecial.Text = "0";
            TxtCardGirlBonus.Text = "0";
            ChkCardGirlBonus.IsChecked = false;
            ChkCardGirlDummy.IsChecked = false;
            TxtCardGirlFree1.Text = string.Empty;
            TxtCardGirlFree2.Text = string.Empty;
            TxtCardGirlFree3.Text = string.Empty;
            SetCardGirlImage(string.Empty);
            TxtCardGirlDispOrder.Text = dsYellCards.Cards.Count > 0 ? (dsYellCards.Cards.Max(r => r.表示順) + 1).ToString() : "1";
            SetCardGirlEtcButton();

            currentEditID = string.Empty;
            //名前の変更可
            CmbCardGirlName.IsEnabled = true;
            BtnCardAdd.IsEnabled = true;
            BtnSearchGirlName.IsEnabled = true;
            BtnCardUpd.IsEnabled = false;

            CmbCardGirlName.Focus();
        }
        #endregion

        #region Data表示用
        private string GetDispName(DsYellCards.CardsRow cardRow)
        {
            return (string.IsNullOrEmpty(cardRow.種別) ? string.Empty : ("[" + cardRow.種別 + "] ")) + cardRow.名前 + (cardRow.進展 == 2 ? "+" : string.Empty);
        }

        private string GetSortName(DsYellCards.CardsRow cardRow)
        {
            return cardRow.名前 + (cardRow.進展 == 2 ? "+" : " ") + (string.IsNullOrEmpty(cardRow.種別) ? string.Empty : ("[" + cardRow.種別 + "] "));
        }

        private int GetAtkBonus(DsYellCards.CardsRow cardRow)
        {
            int baseAtk = cardRow.攻援 + (int)Math.Ceiling((double)cardRow.攻援 * (cardRow.ボーナス有無 ? (double)cardRow.ボーナス : 0d) / 100d);
            int bonusAtk = baseAtk + (int)Math.Ceiling(baseAtk * cardRow.スペシャル);
            return baseAtk;
        }

        private int GetDefBonus(DsYellCards.CardsRow cardRow)
        {
            int baseDef = cardRow.守援 + (int)Math.Ceiling((double)cardRow.守援 * (cardRow.ボーナス有無 ? (double)cardRow.ボーナス : 0d) / 100d);
            int bonusDef = baseDef + (int)Math.Ceiling(baseDef * cardRow.スペシャル);
            return baseDef;
        }
        private string GetSkillName(DsYellCards.CardsRow cardRow)
        {
            if (cardRow.スキル == "なし")
            {
                return cardRow.スキル;
            }
            else
            {
                SkillInfo skillInfo = Skills.FirstOrDefault(s => s.Name == cardRow.スキル);
                if (skillInfo.IsDown)
                {
                    return (cardRow.全属性スキル ? "全" : "他") + cardRow.スキル + " Lv:" + cardRow.スキルLv;
                }
                else
                {
                    return (cardRow.全属性スキル ? "全" : skillInfo.IsOwn ? "自" : "同") + cardRow.スキル + " Lv:" + cardRow.スキルLv;
                }
            }
        }

        private int GetAtkSkillPower(DsYellCards.CardsRow cardRow)
        {
            if (cardRow.スキル == "なし")
            {
                return 0;
            }
            else
            {
                SkillInfo skillInfo = Skills.FirstOrDefault(s => s.Name == cardRow.スキル);
                if (skillInfo.IsAttack)
                {
                    int power = (cardRow.全属性スキル ? skillInfo.AllPower : skillInfo.Power) + cardRow.スキルLv - 1;
                    return power;
                }
                else
                {
                    return 0;
                }
            }
        }

        private int GetDefSkillPower(DsYellCards.CardsRow cardRow)
        {
            if (cardRow.スキル == "なし")
            {
                return 0;
            }
            else
            {
                SkillInfo skillInfo = Skills.FirstOrDefault(s => s.Name == cardRow.スキル);
                if (skillInfo.IsDeffence)
                {
                    int power = (cardRow.全属性スキル ? skillInfo.AllPower : skillInfo.Power) + cardRow.スキルLv - 1;
                    return power;
                }
                else
                {
                    return 0;
                }
            }
        }
        #endregion

        /// <summary>
        /// 所持カード表示順
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnOwnCardReDispOrder_Click(object sender, RoutedEventArgs e)
        {
            if (PopSearchOwnCard.IsOpen)
            {
                DialogWindow.Show(this, "検索中は表示順の更新はできません", DialogWindow.MessageType.Error);
                return;
            }

            if (!DialogWindow.Show(this, "現在の並び順で表示順の更新を行いますか？", "確認", DialogWindow.MessageType.Confirm))
            {
                return;
            }

            List<DsYellDispCards.DispCardRow> sortedCardList = new List<DsYellDispCards.DispCardRow>();
            foreach (DataRowView rowView in DgCards.Items)
            {
                sortedCardList.Add(rowView.Row as DsYellDispCards.DispCardRow);
            }
            for (int i = 0; i < sortedCardList.Count; i++)
            {
                var row = dsYellCards.Cards.FirstOrDefault(r => r.CommonID == sortedCardList[i].CommonID);
                row.表示順 = i + 1;
                UpdateCardRow(row);

            }
            dsYellCards.WriteXml(Utility.GetFilePath("yellcards.xml"));

            DialogWindow.Show(this, "表示順を更新しました", DialogWindow.MessageType.Infomation);
        }
        #endregion

        #region 早見表タブ

        #region ボタン選択

        /// <summary>
        /// 早見表トグルbuttonをOffにする
        /// </summary>
        /// <param name="onButton"></param>
        private void ListToggleButtonOff(ToggleButton onButton)
        {
            toggleButtonList.ForEach(b=> b.IsChecked = (b==onButton));
        }

        /// <summary>
        /// 経験値ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBtnListExp_Click(object sender, RoutedEventArgs e)
        {
            //他のボタンをOffにする
            ListToggleButtonOff(TBtnListExp);

            //経験値表を表示する
            GrdListExp.Visibility = System.Windows.Visibility.Visible;
            GrdListSkillUpPer.Visibility = System.Windows.Visibility.Collapsed;

            CreateExpList(ExpType.Normal);
            //強調表示条件がある場合
            if (!string.IsNullOrEmpty(TxtListExpOver.Text) || !string.IsNullOrEmpty(TxtListExpUnder.Text))
            {
                Dispatcher.BeginInvoke((Action)EmphansisExpList, System.Windows.Threading.DispatcherPriority.Background, null);
            }
        }

        /// <summary>
        /// 同属性時の取得経験値ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBtnListExpSame_Click(object sender, RoutedEventArgs e)
        {
            //他のボタンをOffにする
            ListToggleButtonOff(TBtnListExpSame);

            //経験値表を表示する
            GrdListExp.Visibility = System.Windows.Visibility.Visible;
            GrdListSkillUpPer.Visibility = System.Windows.Visibility.Collapsed;

            CreateExpList(ExpType.Same);

            //強調表示条件がある場合
            if (!string.IsNullOrEmpty(TxtListExpOver.Text) || !string.IsNullOrEmpty(TxtListExpUnder.Text))
            {
                Dispatcher.BeginInvoke((Action)EmphansisExpList, System.Windows.Threading.DispatcherPriority.Background, null);
            }
        }

        /// <summary>
        /// 大成功時の取得経験値ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBtnListExpSuccess_Click(object sender, RoutedEventArgs e)
        {
            //他のボタンをOffにする
            ListToggleButtonOff(TBtnListExpSuccess);

            //経験値表を表示する
            GrdListExp.Visibility = System.Windows.Visibility.Visible;
            GrdListSkillUpPer.Visibility = System.Windows.Visibility.Collapsed;

            CreateExpList(ExpType.Success);

            //強調表示条件がある場合
            if (!string.IsNullOrEmpty(TxtListExpOver.Text) || !string.IsNullOrEmpty(TxtListExpUnder.Text))
            {
                Dispatcher.BeginInvoke((Action)EmphansisExpList, System.Windows.Threading.DispatcherPriority.Background, null);
            }
        }

        /// <summary>
        /// 同属性大成功時の取得経験値ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBtnListExpSameSuccess_Click(object sender, RoutedEventArgs e)
        {
            //他のボタンをOffにする
            ListToggleButtonOff(TBtnListExpSameSuccess);

            //経験値表を表示する
            GrdListExp.Visibility = System.Windows.Visibility.Visible;
            GrdListSkillUpPer.Visibility = System.Windows.Visibility.Collapsed;

            CreateExpList(ExpType.SameSuccess);

            //強調表示条件がある場合
            if (!string.IsNullOrEmpty(TxtListExpOver.Text) || !string.IsNullOrEmpty(TxtListExpUnder.Text))
            {
                Dispatcher.BeginInvoke((Action)EmphansisExpList, System.Windows.Threading.DispatcherPriority.Background, null);
            }
        }

        /// <summary>
        /// 声援Up率ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBtnListSkillUp_Click(object sender, RoutedEventArgs e)
        {
            //他のボタンをOffにする
            ListToggleButtonOff(TBtnListSkillUp);

            CreateSkillUpList();

            //経験値表を表示する
            GrdListExp.Visibility = System.Windows.Visibility.Collapsed;
            GrdListSkillUpPer.Visibility = System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// 声援Up枚数ボタンクリック
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBtnListSkillUpNum_Click(object sender, RoutedEventArgs e)
        {
            //他のボタンをOffにする
            ListToggleButtonOff(TBtnListSkillUpNum);

            CreateSkillUpNumList();

            //経験値表を表示する
            GrdListExp.Visibility = System.Windows.Visibility.Collapsed;
            GrdListSkillUpPer.Visibility = System.Windows.Visibility.Visible;
        }
        #endregion

        #region 表作成
        /// <summary>
        /// 声援アップ率表作成
        /// </summary>
        private void CreateSkillUpList()
        {
            dsYellDispList.SkillUpList.Clear();

            foreach (Rare baseRare in Enum.GetValues(typeof(Rare)))
            {
                if (baseRare == Rare.N) continue;
                for (int lv = 1; lv < 10; lv++)
                {

                    DsYellDispList.SkillUpListRow skillUpRow = dsYellDispList.SkillUpList.NewSkillUpListRow();
                    skillUpRow.レア = baseRare.ToString();
                    skillUpRow.声援Lv = lv;

                    foreach (Rare yellRare in Enum.GetValues(typeof(Rare)))
                    {
                        if (yellRare == Rare.N) continue;

                        int skillUpPer = YellUtility.CalcSkill(baseRare, lv, new List<YellInfo>(){new YellInfo()
                        {
                            Rare = yellRare,
                            HasSkill = true,
                        }});

                        skillUpRow[yellRare.ToString()] = skillUpPer;
                    }

                    dsYellDispList.SkillUpList.AddSkillUpListRow(skillUpRow);
                }
            }

            DgSkillUpPerList.ItemsSource = dsYellDispList.SkillUpList;

        }

        /// <summary>
        /// 声援アップ枚数表作成
        /// </summary>
        private void CreateSkillUpNumList()
        {
            dsYellDispList.SkillUpList.Clear();

            foreach (Rare baseRare in Enum.GetValues(typeof(Rare)))
            {
                if (baseRare == Rare.N) continue;
                for (int lv = 1; lv < 10; lv++)
                {

                    DsYellDispList.SkillUpListRow skillUpRow = dsYellDispList.SkillUpList.NewSkillUpListRow();
                    skillUpRow.レア = baseRare.ToString();
                    skillUpRow.声援Lv = lv;

                    foreach (Rare yellRare in Enum.GetValues(typeof(Rare)))
                    {
                        if (yellRare == Rare.N) continue;

                        var yellList = new List<YellInfo>();
                        int num = 1;
                        int skillUpPer =0;
                        while (true)
                        {
                            yellList.Add(new YellInfo()
                            {
                                Rare = yellRare,
                                HasSkill = true,
                            });

                            skillUpPer = YellUtility.CalcSkill(baseRare, lv, yellList);
                            if (skillUpPer >= 100 || num >= 10) break;
                            num++;
                        }
                        if(skillUpPer >= 100)
                        {
                            skillUpRow[yellRare.ToString()] = num;
                        }
                    }

                    dsYellDispList.SkillUpList.AddSkillUpListRow(skillUpRow);
                }
            }

            DgSkillUpPerList.ItemsSource = dsYellDispList.SkillUpList;
        }


        /// <summary>
        /// 経験値取得表作成
        /// </summary>
        private void CreateExpList(ExpType expType)
        {
            dsYellDispList.ExpList.Clear();

            //同属性の場合はSweet、それ以外はCool（Sweet以外）
            TargetInfo targetInfo = new TargetInfo()
            {
                Attr = (expType == ExpType.Same || expType == ExpType.SameSuccess) ? Attr.Sweet : Attr.Cool,
            };
            bool isSuccess =( expType == ExpType.Success || expType == ExpType.SameSuccess);

            for (int cost = 1; cost <= 40; cost++)
            {
                if (cost > 16 && cost != 35 && cost != 40) continue;

                DsYellDispList.ExpListRow expRow = dsYellDispList.ExpList.NewExpListRow();
                expRow.コスト = cost;

                foreach (Rare baseRare in Enum.GetValues(typeof(Rare)))
                {
                    //久保田、荒井はレアまで指定
                    if (cost == 35 && baseRare != Rare.N) continue;
                    if (cost == 40 && baseRare != Rare.R) continue;

                    //各進展毎に作成
                    for (int rank = 1; rank <= 3; rank++)
                    {
                        if ((baseRare == Rare.N && rank == 3) ||
                            (baseRare == Rare.LG && rank < 3)) continue;
                        //久保田、荒井は進展１のみ
                        if (cost == 35 && rank > 1) continue;
                        if (cost == 40 && rank > 1) continue;

                        int exp = YellUtility.CalcExp(targetInfo, isSuccess, new YellInfo()
                         {
                             Rare = baseRare,
                             Rank = rank,
                             Cost = cost,
                             Attr = Attr.Sweet,
                         });
                        string rankStr = string.Empty;
                        if (rank == 2) rankStr = "P";
                        if (rank == 3) rankStr = "PP";

                        expRow[baseRare.ToString() + rankStr] = exp;

                    }
                }
                dsYellDispList.ExpList.AddExpListRow(expRow);
            }
            DgExpList.ItemsSource = dsYellDispList.ExpList;
        }
        #endregion

        #region 強調表示

        #region 経験値
        /// <summary>
        /// 経験値強調表示以上
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtListExpOver_TextChanged(object sender, TextChangedEventArgs e)
        {
            EmphansisExpList();
        }

        /// <summary>
        /// 経験値強調表示以上
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtListExpUnder_TextChanged(object sender, TextChangedEventArgs e)
        {
            EmphansisExpList();
        }

        /// <summary>
        /// 経験値リスト強調表示
        /// </summary>
        private void EmphansisExpList()
        {
            //以上
            int over;
            if(!int.TryParse(TxtListExpOver.Text,out over))
            {
                over =0;
            }
            //以下
            int under;
            if (!int.TryParse(TxtListExpUnder.Text, out under))
            {
                under = 0;
            }

            for (int row = 0; row < DgExpList.Items.Count; row++)
            {
                DataGridRow dgr = DgExpList.ItemContainerGenerator.ContainerFromIndex(row) as DataGridRow;

                for (int i = 1; i < DgExpList.Columns.Count; i++)
                {
                    var dgc = DgExpList.Columns[i].GetCellContent(dgr).Parent as DataGridCell;
                    string dataText = (dgc.Content as TextBlock).Text;
                    if (!string.IsNullOrEmpty(dataText))
                    {
                        int data = Convert.ToInt32(dataText);
                        if (over > 0 && data >= over)
                        {
                            dgc.Style = FindResource("OverEmphasisCell") as Style;
                        }
                        else if (under > 0 && data <= under)
                        {
                            dgc.Style = FindResource("UnderEmphasisCell") as Style;
                        }
                        else
                        {
                            dgc.Style = FindResource("NumberCell") as Style;
                        }
                    }
                }
            }
        }
        #endregion

        #endregion

        #endregion

        #region 共通メソッド
        /// <summary>
        /// DataGridデフォルト降順メソッド
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            //未ソート状態の場合は昇順に設定する。
            //この後のDataGrid部品の方で昇順⇒降順に変換される。
            if (!e.Column.SortDirection.HasValue)
            {
                e.Column.SortDirection = System.ComponentModel.ListSortDirection.Ascending;
            }
        }
        #endregion
    }
}
