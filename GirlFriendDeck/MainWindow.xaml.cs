using GirlFriendCommon;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GirlFriendDeck
{

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string ATTR_POP = "Pop";
        private const string ATTR_COOL = "Cool";
        private const string ATTR_SWEET = "Sweet";

        #region メンバ変数

        #region 共通
        static private string basePath;

        bool isEvent = false;

        Dictionary<DataGrid, GridInfo> dataGridSet = new Dictionary<DataGrid, GridInfo>();

        #endregion

        private static List<string> clubList = new List<string>() { "委員会＆団体", "運動部", "運動部(個人競技) ", "帰宅部", "研究会", "文化部", "文化部(音楽系)", "文化部(日本)" ,"なし"};
        private static List<string> roleList = new List<string>() { "なし", "部長", "副部長", "攻キャプテン", "守キャプテン" };
        private static Dictionary<string, string> attrList = new Dictionary<string, string>() { { ATTR_SWEET, "Sweet" }, { ATTR_COOL, "Cool" }, { ATTR_POP, "Pop" } };

        private CalcType calcType = CalcType.攻援;

        private HashSet<string> AtkBounus = new HashSet<string>();
        private HashSet<string> DefBounus = new HashSet<string>();
        private HashSet<string> GirlNames = new HashSet<string>();
        //高速化用キャッシュ
        private Dictionary<string, DsGirls.GirlsRow> girlsList = new Dictionary<string, DsGirls.GirlsRow>();
        private Dictionary<string, DsCards.CardsRow> cardsList = new Dictionary<string, DsCards.CardsRow>();
        private Dictionary<string, GirlsSelectionBonus> girlsSelectionBonusList = new Dictionary<string, GirlsSelectionBonus>();
        //カード情報
        private DsGirls dsGilrs;

        //表示用所持カード
        private DsDispCard dsDispCard = new DsDispCard();
        //所持カード
        private DsCards dsCards = new DsCards();
        private DsDeckCard dsDeckCard = new DsDeckCard();
        //選択カード
        private DsSelectCard dsMainSelect = new DsSelectCard();
        private DsSelectCard dsSubSelect = new DsSelectCard();

        /// <summary>
        /// ユーザー設定
        /// </summary>
        private DsSetting dsSetting = new DsSetting();
        /// <summary>
        /// ボーナス設定
        /// </summary>
        private DsSystemSetting dsSystemSetting = new DsSystemSetting();
        //デッキ情報
        private DsDeckInfo deckInfo = new DsDeckInfo();
        private Dictionary<CalcType, int> editCalcNumber = new Dictionary<CalcType, int>() { { CalcType.攻援, 1 }, { CalcType.守援, 1 }, { CalcType.イベント, 1 } };
        private static Dictionary<CalcType, string> calcName = new Dictionary<CalcType, string>() { { CalcType.攻援, "Atk" }, { CalcType.守援, "Def" }, { CalcType.イベント, "Event" } };

        /// <summary>
        /// 現在の使用コスト
        /// </summary>
        private int currentCost;
        private int currentPower;
        private int currentBasePower;
        private int subMax;

        /// <summary>
        /// 最大カード数
        /// </summary>
        private int cardMax;
        /// <summary>
        /// 最大コスト
        /// </summary>
        private int costMax;
        //
        private string currentEditID = string.Empty;

        //自動計算共通結果
        CalcCommonResult commonResult;

        private List<SkillInfo> Skills;

        private List<NameInfo> nameList = new List<NameInfo>();

        #region ボーナス基準値
        private static List<int> selectBonus = new List<int>() { 3, 8, 13, 18, 23 };
        private static List<int> eventBonus = new List<int>() { 5, 5, 5, 5, 5 };
        /// <summary> 同属性 </summary>
        private int sameAttributeBonus = 5;
        /// <summary> 備品 </summary>
        private int clubItemBonus = 2;
        /// <summary> 部長 </summary>
        private int clubLeaderBonus = 2;
        /// <summary> 副部長 </summary>
        private int clubSubLeaderBonus = 2;
        /// <summary> 攻援隊長 </summary>
        private int clubAtkBonus = 5;
        /// <summary> 守援隊長 </summary>
        private int clubDefBonus = 5;
        /// <summary> 部室 </summary>
        private int clubSameBonus = 10;
        #endregion

        private Dictionary<string, SelectionBonusInfo> atkSelectionBonusInfo = new Dictionary<string, SelectionBonusInfo>();
        private Dictionary<string, SelectionBonusInfo> defSelectionBonusInfo = new Dictionary<string, SelectionBonusInfo>();

        private List<SelectionBonusInfo> atkBonusList = new List<SelectionBonusInfo>();
        private List<SelectionBonusInfo> defBonusList = new List<SelectionBonusInfo>();

        #endregion

        public enum CalcType
        {
            攻援,
            守援,
            イベント,
            共通,
        }

        #region クラス定義

        class GirlsSelectionBonus
        {
            public GirlsSelectionBonus()
            {
                AtkBonus = new HashSet<string>();
                DefBonus = new HashSet<string>();
            }
            public string Name { set; get; }
            public HashSet<string> AtkBonus { set; get; }
            public HashSet<string> DefBonus { set; get; }
        }

        class BonusInfo
        {
            /// <summary>センバツボーナス</summary>
            public int Selection { set; get; }
            /// <summary>センバツボーナス</summary>
            public List<int> SelectionList { set; get; }
            /// <summary>属性ボーナス</summary>
            public string AttributeTarget { set; get; }
            /// <summary>属性ボーナス</summary>
            public int Attribute { set; get; }
            /// <summary>役職ボーナス</summary>
            public int Role { set; get; }
            /// <summary>備品ボーナス</summary>
            public Dictionary<string, int> Items { set; get; }
            /// <summary>声選抜ボーナス</summary>
            public ActiveSkill Skill { set; get; }

        }

        class PowerInfo
        {
            public string ID { set; get; }
            public int SelectionPower { set; get; }
            public int Power { set; get; }
            public int BasePower { set; get; }
            public int OriginalPower { set; get; }
            public int BonusEtc { set; get; }
            public int BonusSkill { set; get; }
            public int BonusRole { set; get; }
            public int BonusClub { set; get; }
            public int BonusAttr { set; get; }
            public int BonusItem { set; get; }
            public bool IsMain { set; get; }
        }

        class ActiveSkill
        {
            public string Name { set; get; }
            public string ID { set; get; }
            public bool isOwn { set; get; }
            public int Power { set; get; }
            public bool isAll { set; get; }
            public string Attr { set; get; }
        }

        enum SelectionBonusStatus
        {
            なし,
            未使用,
            適用範囲外,
            適用範囲内,
            MAX,
            オーバー,
        }

        class SelectionBonusInfo : DependencyObject
        {
            public string Name { set; get; }
            public int Count { set; get; }
            public int UseCount { set; get; }
            public bool IsEffection { set; get; }

            public static DependencyProperty IsBonusSelectedProperty = DependencyProperty.Register(
                "IsBonusSelected",
                typeof(bool),
                typeof(SelectionBonusInfo),
                new PropertyMetadata(false));

            public bool IsBonusSelected
            {
                set { SetValue(IsBonusSelectedProperty, value); }
                get { return (bool)GetValue(IsBonusSelectedProperty); }
            }

            public SelectionBonusStatus Status
            {
                get
                {
                    if (Count == 0)
                    {
                        return SelectionBonusStatus.なし;
                    }
                    else if (UseCount > 0)
                    {
                        if (UseCount > 5)
                        {
                            return SelectionBonusStatus.オーバー;
                        }
                        else if (UseCount == 5)
                        {
                            return SelectionBonusStatus.MAX;
                        }
                        else if (IsEffection)
                        {
                            return SelectionBonusStatus.適用範囲内;
                        }
                        else
                        {
                            return SelectionBonusStatus.適用範囲外;
                        }
                    }
                    else
                    {
                        return SelectionBonusStatus.未使用;
                    }
                }
            }

            public string Display
            {
                get
                {
                    return UseCount.ToString() + ":" + Name + " (" + Count.ToString() + ")";
                }
            }

            public string DisplayNoUseCount
            {
                get
                {
                    return Name + " (" + Count.ToString() + ")";
                }
            }

            public SelectionBonusInfo Copy()
            {
                SelectionBonusInfo sbi = new SelectionBonusInfo();
                sbi.Name = this.Name;
                sbi.Count = this.Count;
                sbi.IsEffection = this.IsEffection;
                sbi.UseCount = this.UseCount;
                return sbi;
            }
        }

        class CardInfo
        {
            public int Number { set; get; }
            public string ID { set; get; }
            public string Name { set; get; }
            public int Cost { set; get; }
            public int Power { set; get; }
            public int Bonus { set; get; }
        }

        class CardSelectionBonusInfo
        {
            public string ID { set; get; }
            public int Cost { set; get; }
            public int Power { set; get; }
            public List<string> Bonus { set; get; }
        }

        class SelectionBonus
        {
            public int TotalPower { set; get; }
            public int MaxCount { set; get; }
            public int CurrentCount { set; get; }
            public int LastCount { get { return MaxCount - CurrentCount; } }
        }

        /// <summary>
        /// デッキ情報
        /// </summary>
        class DeckInfo
        {
            public int Number { set; get; }
            public string Name { set; get; }
            public CalcType CalcType { set; get; }
            public string Display
            {
                get
                {
                    return (Number.ToString()).PadLeft(2) + ":" + Name;
                }
            }
        }
        /// <summary>
        /// Grid情報
        /// </summary>
        class GridInfo
        {
            public DataGrid Grid { set; get; }
            public Popup Popup { set; get; }
            public CalcType CalcType { set; get; }
            public DataSet DataSet { set; get; }
        }

        /// <summary>
        /// 自動計算スレッド結果
        /// </summary>
        class CalcThreadResult
        {
            public AutoCalcResult Result { set; get; }
            public CalcCommonResult Common { set; get; }
            public AutoCalcInfo CalcInfo { set; get; }
        }

        class CalcCommonResult
        {
            private object lockObj = new object();

            public List<CalcThreadResult> Results { set; get; }
            public int MaxThread { set; get; }
            public int RemainThread;
            public int CalcNum;
            public int DispNum;
            public int MaxCalcNum;
            public List<CardInfo> CalcCards { set; private get; }
            public CardInfo GetCalcCard()
            {
                CardInfo result;
                lock (lockObj)
                {
                    if (CalcCards.Count == 0) return null;
                    result = CalcCards[0];
                    CalcCards.RemoveAt(0);
                }
                return result;
            }
            public bool IsCancel;
        }

        /// <summary>
        /// 自動計算結果
        /// </summary>
        class AutoCalcResult
        {
            public List<string> SelectSubIds { set; get; }
            public int Result { set; get; }
        }

        /// <summary>
        /// 自動計算用パラメーター
        /// </summary>
        class AutoCalParam
        {
            public Dictionary<string, CardInfo> IdCardList { set; get; }
            public Dictionary<string, CardSelectionBonusInfo> CardSelectionBonusInfo { set; get; }
            public Dictionary<int, Dictionary<int, CardInfo>> AddCalcCard { set; get; }
            public int SubMax { set; get; }
            public int MaxCost { set; get; }
            public List<string> MainIds { set; get; }
            public int DispCount { set; get; }
        }

        class AutoCalcInfo
        {
            /// <summary>主センバツトータル </summary>
            public int MainTotal { set; get; }
            /// <summary>副センバツトータル </summary>
            public int SubTotal { set; get; }
            /// <summary>センバツボーナス </summary>
            public Dictionary<string, int> SelectionBonus { set; get; }
            /// <summary>
            /// 発揮値
            /// </summary>
            public int Result
            {
                get
                {
                    if (subResult < 0)
                    {
                        subResult = CalcSubTotal();
                    }
                    return subResult;
                }
            }

            private int subResult = -1;

            /// <summary>最大センバツボーナス（55555）計算済みフラグ </summary>
            private bool isCalcMaxSelectionBonus;
            /// <summary>最大センバツボーナス（55555）フラグ </summary>
            private bool isMaxSelectionBonus;
            /// <summary>センバツボーナス計算済みフラグ </summary>
            private bool isCalcSelectionBonus;
            /// <summary>センバツボーナス </summary>
            private int selectionBonusTotal;
            /// <summary>最大センバツボーナス判定（55555） </summary>
            public bool IsMaxSelectionBonus
            {
                get
                {
                    if (!isCalcMaxSelectionBonus)
                    {
                        isMaxSelectionBonus = (SelectionBonus.Where(b => b.Value >= 5).Count() >= 5);
                        isCalcMaxSelectionBonus = true;
                    }
                    return isMaxSelectionBonus;
                }
            }

            public AutoCalcInfo Copy()
            {
                AutoCalcInfo copy = new AutoCalcInfo();
                copy.MainTotal = MainTotal;
                copy.SubTotal = SubTotal;
                copy.subResult = subResult;
                copy.isCalcMaxSelectionBonus = isCalcMaxSelectionBonus;
                copy.isMaxSelectionBonus = isMaxSelectionBonus;
                copy.isCalcSelectionBonus = isCalcSelectionBonus;
                copy.selectionBonusTotal = selectionBonusTotal;
                Dictionary<string, int> selectionBonus = new Dictionary<string, int>();
                foreach (var kvp in SelectionBonus)
                {
                    selectionBonus.Add(kvp.Key, kvp.Value);
                }
                copy.SelectionBonus = selectionBonus;
                return copy;
            }

            private int CalcSubTotal()
            {
                int bonusTotal = 0;
                if (isCalcMaxSelectionBonus && isMaxSelectionBonus)
                {
                    bonusTotal = selectBonus[4] * 5;
                }
                else
                {
                    if (isCalcSelectionBonus)
                    {
                        bonusTotal = selectionBonusTotal;
                    }
                    else
                    {
                        foreach (var kvp in SelectionBonus.Where(b => b.Value > 0).OrderByDescending(b => b.Value).Take(5))
                        {
                            bonusTotal += selectBonus[(kvp.Value > 5 ? 5 : kvp.Value) - 1];
                        }
                        selectionBonusTotal = bonusTotal;
                        isCalcSelectionBonus = true;
                    }
                }

                return (int)((double)SubTotal * 0.8d + ((double)SubTotal * bonusTotal / 100d));
            }

            public void AddSubCalcInfo(string id, Dictionary<string, CardSelectionBonusInfo> cardSelectionBonusInfo)
            {
                SubTotal += cardSelectionBonusInfo[id].Power;
                AddBonus(SelectionBonus, id, cardSelectionBonusInfo);
                isCalcSelectionBonus = isCalcSelectionBonus && (cardSelectionBonusInfo[id].Bonus.Count == 0);
                subResult = -1;
                isCalcMaxSelectionBonus =isCalcMaxSelectionBonus && isMaxSelectionBonus;
            }
            public void RemoveSubCalcInfo(string id, Dictionary<string, CardSelectionBonusInfo> cardSelectionBonusInfo)
            {
                SubTotal -= cardSelectionBonusInfo[id].Power;
                cardSelectionBonusInfo[id].Bonus.ForEach(b => SelectionBonus[b] = SelectionBonus[b] - 1);
                isCalcSelectionBonus = isCalcSelectionBonus && (cardSelectionBonusInfo[id].Bonus.Count == 0);
                subResult = -1;
                isCalcMaxSelectionBonus = isMaxSelectionBonus && (cardSelectionBonusInfo[id].Bonus.Count == 0);
            }
        }

        #endregion

        public bool IsAttack()
        {
            return (calcType == CalcType.攻援 || calcType == CalcType.イベント);
        }

        public MainWindow()
        {
            basePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);

            InitializeComponent();

            this.GotKeyboardFocus += (s, e) =>
            {
                TextBox text = e.OriginalSource as TextBox;
                if (text != null)
                {
                    text.SelectAll();
                }
            };

#if NET_V4
            this.Title += " for XP(.Net4.0)";
#endif
            dataGridSet.Add(this.DgCards, new GridInfo() { DataSet = dsDispCard, CalcType = CalcType.共通, Grid = DgCards, Popup = PopCardsColumn });
            dataGridSet.Add(this.DgDeckCards, new GridInfo() { DataSet = dsDeckCard, CalcType = CalcType.攻援, Grid = DgDeckCards, Popup = PopDeckCardsColumn });
            dataGridSet.Add(this.DgDeckMain, new GridInfo() { DataSet = dsMainSelect, CalcType = CalcType.攻援, Grid = DgDeckMain, Popup = PopDeckMainColumn });
            dataGridSet.Add(this.DgDeckSub, new GridInfo() { DataSet = dsSubSelect, CalcType = CalcType.攻援, Grid = DgDeckSub, Popup = PopDeckSubColumn });

            dsSetting = new DsSetting();
#if DEBUG_
            File.AppendAllText(Utility.GetFilePath("log.txt"), "[settings.xml]読み込み開始 \r\n");
#endif
            #region 設定
            if (File.Exists(Utility.GetFilePath("settings.xml")))
            {
                dsSetting.ReadXml(Utility.GetFilePath("settings.xml"));
#if DEBUG_
                File.AppendAllText(Utility.GetFilePath("log.txt"), "[settings.xml]読み込み完了 \r\n");
#endif
                bool isChange = false;
                if (dsSetting.Window.Count == 0)
                {
                    DsSetting.WindowRow windowSetting = dsSetting.Window.NewWindowRow();
                    windowSetting.PosX = this.Left;
                    windowSetting.PosY = this.Top;
                    windowSetting.Height = this.Height;
                    windowSetting.Width = this.Width;
                    dsSetting.Window.AddWindowRow(windowSetting);
                    isChange = true;
                }
                if (dsSetting.User[0].IsClubNull())
                {
                    dsSetting.User[0].Club = "なし";
                    isChange = true;
                }
                if (dsSetting.Etc.Count == 0)
                {
                    DsSetting.EtcRow etcRow = dsSetting.Etc.NewEtcRow();
                    etcRow.EditColumn = true;
                    etcRow.ShowGirlPath = string.Empty;
                    dsSetting.Etc.AddEtcRow(etcRow);
                    isChange = true;
                }
                if (dsSetting.Etc[0].IsShowGirlPathNull())
                {
                    DsSetting.EtcRow etcRow = dsSetting.Etc[0];
                    etcRow.ShowGirlPath = string.Empty;
                    isChange = true;
                }
                if (dsSetting.User[0].Role == "攻援隊長")
                {
                    dsSetting.User[0].Role = "攻キャプテン";
                    isChange = true;
                }
                if (dsSetting.User[0].Role == "守援隊長")
                {
                    dsSetting.User[0].Role = "守キャプテン";
                    isChange = true;
                }

                if (isChange)
                {
                    dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
#if DEBUG_
                    File.AppendAllText(Utility.GetFilePath("log.txt"), "[settings.xml]保存 \r\n");
#endif
                }
            }
            else
            {
                DsSetting.UserRow userSetting = dsSetting.User.NewUserRow();
                userSetting.Lv = 1;
                userSetting.Role = "なし";
                userSetting.Club = "なし";
                userSetting.AtkCost = 20;
                userSetting.DefCost = 20;
                userSetting.Attribute = ATTR_SWEET;
                dsSetting.User.AddUserRow(userSetting);

                DsSetting.WindowRow windowSetting = dsSetting.Window.NewWindowRow();
                windowSetting.PosX = this.Left;
                windowSetting.PosY = this.Top;
                windowSetting.Height = this.Height;
                windowSetting.Width = this.Width;
                dsSetting.Window.AddWindowRow(windowSetting);

                DsSetting.EtcRow etcRow = dsSetting.Etc.NewEtcRow();
                etcRow.EditColumn = true;
                etcRow.ShowGirlPath = string.Empty;
                dsSetting.Etc.AddEtcRow(etcRow);

                dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
#if DEBUG_
                File.AppendAllText(Utility.GetFilePath("log.txt"), "[settings.xml]初期化 \r\n");
#endif
            }
            #endregion

            this.Height = this.dsSetting.Window[0].Height;
            this.Width = this.dsSetting.Window[0].Width;
            this.Left = this.dsSetting.Window[0].PosX;
            this.Top = this.dsSetting.Window[0].PosY;
        }

        private void Window_Loaded_1(object sender, RoutedEventArgs e)
        {
            isEvent = false;

            CmbUserClub.ItemsSource = clubList;
            CmbUserClub.SelectedIndex = 0;
            CmbUserRole.ItemsSource = roleList;
            CmbUserRole.SelectedIndex = 0;
            CmbUserAttr.SelectedValuePath = "Key";
            CmbUserAttr.DisplayMemberPath = "Value";
            CmbUserAttr.ItemsSource = attrList;
            CmbUserAttr.SelectedIndex = 0;

            CmbCardGirlFavor.SelectedValuePath = "Key";
            CmbCardGirlFavor.DisplayMemberPath = "Value";
            CmbCardGirlFavor.ItemsSource = new Dictionary<int, string> { { 1, "1" }, { 2, "2" }, { 3, "3" }, { 4, "4" }, { 5, "5" } };

            //DataGrid情報の初期化
            InitDataGridSet();

            #region ユーザー設定復元
            costMax = dsSetting.User[0].AtkCost;

            TxtUserLv.Text = dsSetting.User[0].Lv.ToString();
            SetSubMax(dsSetting.User[0].Lv.ToString());
            CmbUserAttr.SelectedValue = dsSetting.User[0].Attribute;
            TxtUserAttkCost.Text = dsSetting.User[0].AtkCost.ToString();
            LblMaxCost.Content = "/" + dsSetting.User[0].AtkCost.ToString();

            TxtUserDefCost.Text = dsSetting.User[0].DefCost.ToString();

            CmbUserRole.SelectedValue = dsSetting.User[0].Role;
            CmbUserClub.SelectedValue = dsSetting.User[0].Club;
            ChkUserItemRocker.IsChecked = dsSetting.User[0].HasRocker;
            ChkUserItemTv.IsChecked = dsSetting.User[0].HasTV;
            ChkUserItemWhiteBoard.IsChecked = dsSetting.User[0].HasWhiteBoard;

            ChkEtcEditColumn.IsChecked = dsSetting.Etc[0].EditColumn;
            LblShowGirlPath.Text = dsSetting.Etc[0].ShowGirlPath;

            #endregion

            isEvent = true;

            #region ボーナス読み込み
            if (File.Exists(Utility.GetFilePath("system_settings.xml")))
            {
                dsSystemSetting.ReadXml(Utility.GetFilePath("system_settings.xml"));

                DsSystemSetting.BonusRow bonusRow = dsSystemSetting.Bonus[0];
                sameAttributeBonus = bonusRow.同属性;
                clubSameBonus = bonusRow.部室;
                clubItemBonus = bonusRow.備品;
                clubLeaderBonus = bonusRow.部長;
                clubSubLeaderBonus = bonusRow.副部長;
                clubAtkBonus = bonusRow.攻援隊長;
                clubDefBonus = bonusRow.守援隊長;

                //声援
                Skills = new List<SkillInfo>();
                foreach (DsSystemSetting.声援Row skillRow in dsSystemSetting.声援)
                {
                    Skills.Add(new SkillInfo()
                    {
                        Name = skillRow.名前,
                        IsAttack = skillRow.攻援,
                        IsDeffence = skillRow.守援,
                        IsOwn = skillRow.自身,
                        IsDown = skillRow.DOWN,
                        Power = skillRow.Power,
                        AllPower = skillRow.全属Power,
                    });
                }

                //センバツ
                DsSystemSetting.センバツRow selectionRow = dsSystemSetting.センバツ[0];
                selectBonus.Clear();
                for (int i = 0; i < 5; i++)
                {
                    selectBonus.Add(Convert.ToInt32(selectionRow["Lv" + (i + 1).ToString()]));
                }
            }
            else
            {
                //初期設定

                //各種ボーナス
                DsSystemSetting.BonusRow bonusRow = dsSystemSetting.Bonus.NewBonusRow();
                bonusRow.同属性 = sameAttributeBonus;
                bonusRow.部室 = clubSameBonus;
                bonusRow.備品 = clubItemBonus;
                bonusRow.部長 = clubLeaderBonus;
                bonusRow.副部長 = clubSubLeaderBonus;
                bonusRow.攻援隊長 = clubAtkBonus;
                bonusRow.守援隊長 = clubDefBonus;
                dsSystemSetting.Bonus.AddBonusRow(bonusRow);

                Skills = new List<SkillInfo>()
            {
                new SkillInfo(){ Name = "攻守スーパー特大", IsAttack=true, IsDeffence = true, IsOwn = true, IsDown=false, Power = 30,AllPower=30},
                new SkillInfo(){ Name = "攻援スーパー特大", IsAttack=true, IsDeffence = false, IsOwn = true, IsDown=false, Power = 35,AllPower=35},
                new SkillInfo(){ Name = "守援スーパー特大", IsAttack=false, IsDeffence = true, IsOwn = true, IsDown=false, Power = 35,AllPower=35},
                new SkillInfo(){ Name = "攻守特大", IsAttack=true, IsDeffence = true, IsOwn = false, IsDown=false, Power = 20,AllPower=18},
                new SkillInfo(){ Name = "攻援特大", IsAttack=true, IsDeffence = false, IsOwn = false, IsDown=false, Power = 20,AllPower=18},
                new SkillInfo(){ Name = "守援特大", IsAttack=false, IsDeffence = true, IsOwn = false, IsDown=false, Power = 20,AllPower=18},
                new SkillInfo(){ Name = "攻守大", IsAttack=true, IsDeffence = true, IsOwn = false, IsDown=false, Power = 15,AllPower=13},
                new SkillInfo(){ Name = "攻援大", IsAttack=true, IsDeffence = false, IsOwn = false, IsDown=false, Power = 13,AllPower=15},
                new SkillInfo(){ Name = "守援大", IsAttack=false, IsDeffence = true, IsOwn = false, IsDown=false, Power = 13,AllPower=15},
                new SkillInfo(){ Name = "攻守中", IsAttack=true, IsDeffence = true, IsOwn = false, IsDown=false, Power = 8,AllPower=6},
                new SkillInfo(){ Name = "攻援中", IsAttack=true, IsDeffence = false, IsOwn = false, IsDown=false, Power = 10,AllPower=8},
                new SkillInfo(){ Name = "守援中", IsAttack=false, IsDeffence = true, IsOwn = false, IsDown=false, Power = 10,AllPower=8},
                new SkillInfo(){ Name = "攻守小", IsAttack=true, IsDeffence = true, IsOwn = false, IsDown=false, Power = 5,AllPower=3},
                new SkillInfo(){ Name = "攻援小", IsAttack=true, IsDeffence = false, IsOwn = false, IsDown=false, Power = 5,AllPower=3},
                new SkillInfo(){ Name = "守援小", IsAttack=false, IsDeffence = true, IsOwn = false, IsDown=false, Power = 5,AllPower=3},
                new SkillInfo(){ Name = "守援スーパー特大DOWN", IsAttack=true, IsDeffence = false, IsOwn = false, IsDown=true, Power = 0,AllPower=0},
                new SkillInfo(){ Name = "攻援スーパー特大DOWN", IsAttack=false, IsDeffence = true, IsOwn = false, IsDown=true, Power = 0,AllPower=0},
                new SkillInfo(){ Name = "守援大DOWN", IsAttack=true, IsDeffence = false, IsOwn = false, IsDown=true, Power = 0,AllPower=0},
                new SkillInfo(){ Name = "攻援大DOWN", IsAttack=false, IsDeffence = true, IsOwn = false, IsDown=true, Power = 0,AllPower=0},
                new SkillInfo(){ Name = "なし", IsAttack=false, IsDeffence = false, IsOwn = false, IsDown=false, Power = 0,AllPower=0},
           };

                //声援
                Skills.ForEach(s =>
                {
                    DsSystemSetting.声援Row skillRow = dsSystemSetting.声援.New声援Row();
                    skillRow.名前 = s.Name;
                    skillRow.攻援 = s.IsAttack;
                    skillRow.守援 = s.IsDeffence;
                    skillRow.自身 = s.IsOwn;
                    skillRow.DOWN = s.IsDown;
                    skillRow.Power = s.Power;
                    skillRow.全属Power = s.AllPower;
                    dsSystemSetting.声援.Add声援Row(skillRow);
                });

                //センバツ
                DsSystemSetting.センバツRow selectionRow = dsSystemSetting.センバツ.NewセンバツRow();
                for (int i = 0; i < 5; i++)
                {
                    selectionRow["Lv" + (i + 1).ToString()] = selectBonus[i];
                }
                dsSystemSetting.センバツ.AddセンバツRow(selectionRow);

                dsSystemSetting.WriteXml(Utility.GetFilePath("system_settings.xml"));
            }
            #endregion

            CmbCardGirlSkill.DisplayMemberPath = "Name";
            CmbCardGirlSkill.SelectedValuePath = "Name";
            CmbCardGirlSkill.ItemsSource = Skills;

            #region　POPUP画面初期化
            Dictionary<string, string> attrListEmpty = new Dictionary<string, string>();
            attrListEmpty.Add(string.Empty, string.Empty);
            foreach (var kvp in attrList)
            {
                attrListEmpty.Add(kvp.Key, kvp.Value);
            }
            CmbPopSearchDeckAttr.SelectedValuePath = "Key";
            CmbPopSearchDeckAttr.DisplayMemberPath = "Value";
            CmbPopSearchDeckAttr.ItemsSource = attrListEmpty;
            CmbPopSearchDeckAttr.SelectedIndex = 0;

            CmbPopSearchOwnCardAttr.SelectedValuePath = "Key";
            CmbPopSearchOwnCardAttr.DisplayMemberPath = "Value";
            CmbPopSearchOwnCardAttr.ItemsSource = attrListEmpty;
            CmbPopSearchOwnCardAttr.SelectedIndex = 0;
            #endregion

            //画面反映
            #region ボーナス設定画面
            //同属性
            TxtBonusAttr.Text = sameAttributeBonus.ToString();
            //部室
            TxtBonusClubSame.Text = clubSameBonus.ToString();
            //備品
            TxtBonusClubItem.Text = clubItemBonus.ToString();
            //役職
            TxtBonusClubLeader.Text = clubLeaderBonus.ToString();
            TxtBonusClubSubLeader.Text = clubSubLeaderBonus.ToString();
            TxtBonusClubAtk.Text = clubAtkBonus.ToString();
            TxtBonusClubDef.Text = clubDefBonus.ToString();

            //声援
            //攻守スーパー
            var textSkill = new[]{
                new{Text = "SSB", Name="スーパー特大"},
                new{Text = "SB", Name="特大"},
                new{Text = "B", Name="大"},
                new{Text = "M", Name="中"},
                new{Text = "S", Name="小"},
            };
            var targetSkill = new[] {
                new {Text ="Own" ,IsOwn = true,IsAll=false},
                new {Text ="Same" ,IsOwn = false,IsAll=false},
                new {Text ="All" ,IsOwn = false,IsAll=true},
            };
            var typeSkill = new[] { new { Text = "Full", Name = "攻守" }, new { Text = "Half", Name = "攻援" }, new { Text = "Half", Name = "守援" } };

            foreach (var skill in textSkill)
            {
                foreach (var target in targetSkill)
                {
                    foreach (var type in typeSkill)
                    {
                        string textName = "TxtBonusSkill" + "_" + target.Text + "_" + type.Text + "_" + skill.Text;
                        TextBox textBox = FindName(textName) as TextBox;
                        if (textBox != null)
                        {
                            SkillInfo skillInfo = Skills.FirstOrDefault(s => s.Name == (type.Name + skill.Name) && s.IsOwn == target.IsOwn);
                            if (skillInfo != null)
                            {
                                textBox.Text = (target.IsAll ? skillInfo.AllPower : skillInfo.Power).ToString();
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < selectBonus.Count; i++)
            {
                string name = "TxtBonusSelect" + (i + 1).ToString();
                TextBox textBox = FindName(name) as TextBox;
                textBox.Text = selectBonus[i].ToString();
            }
            #endregion

#if DEBUG_
                File.AppendAllText(Utility.GetFilePath("log.txt"), "[gilrs.xml]読み込み開始 \r\n");
#endif
            #region カード情報読み込み
            //カード情報読み込み
            if (File.Exists(Utility.GetFilePath("gilrs.xml")))
            {
                dsGilrs = new DsGirls();
                dsGilrs.ReadXml(Utility.GetFilePath("gilrs.xml"));
#if DEBUG_
                File.AppendAllText(Utility.GetFilePath("log.txt"), "[gilrs.xml]読み込み完了:" + dsGilrs.Girls.Count.ToString() + "\r\n");
#endif
                LoadGilrs();
            }
            #endregion

#if DEBUG_
            File.AppendAllText(Utility.GetFilePath("log.txt"), "[cards.xml]読み込み開始:" + "\r\n");
#endif
            #region 所持カード情報読み込み
            //所持カード情報読み込み
            if (File.Exists(Utility.GetFilePath("cards.xml")))
            {
                try
                {
                    dsCards.ReadXml(Utility.GetFilePath("cards.xml"));
#if DEBUG_
                    File.AppendAllText(Utility.GetFilePath("log.txt"), "[cards.xml]読み込み完了:" + dsCards.Cards.Count.ToString() + "\r\n");
#endif
                }
                catch (Exception)
                {
                    //データ互換対応
                    //読み込みエラーの場合は、旧DataSetで読み込む
                    DsCardsOld old = new DsCardsOld();
                    old.ReadXml(Utility.GetFilePath("cards.xml"));
                    foreach (DsCardsOld.CardsRow row in old.Cards)
                    {
                        if (string.IsNullOrEmpty(row.進展))
                        {
                            row.進展 = "1";
                        }
                    }
                    old.AcceptChanges();
                    old.WriteXml(Utility.GetFilePath("cards.xml"));
#if DEBUG_
                    File.AppendAllText(Utility.GetFilePath("log.txt"), "[cards.xml]変換完了:" + "\r\n");
#endif

                    dsCards.ReadXml(Utility.GetFilePath("cards.xml"));
#if DEBUG_
                    File.AppendAllText(Utility.GetFilePath("log.txt"), "[cards.xml]読み込み完了:" + dsCards.Cards.Count.ToString() + "\r\n");
#endif
                }
                //所持カードデータ互換
                ConvertCards();

                ReadCards();
            }
            else
            {
#if DEBUG_
                File.AppendAllText(Utility.GetFilePath("log.txt"), "[cards.xml]存在しない\r\n");
#endif
            }
            #endregion

            DgCards.ItemsSource = dsDispCard.DispCard.OrderBy(r => r.表示順).AsDataView();
            DgDeckCards.ItemsSource = dsDeckCard.DeckCard.OrderBy(r => r.表示順).AsDataView();
            DgDeckMain.ItemsSource = dsMainSelect.Card;
            DgDeckSub.ItemsSource = dsSubSelect.Card;

            #region デッキ情報読み込み
            if (!Directory.Exists(Utility.GetFilePath("Deck")))
            {
                Directory.CreateDirectory(Utility.GetFilePath("Deck"));
            }
            if (File.Exists(Utility.GetFilePath("deckinfo.xml")))
            {
                deckInfo.ReadXml(Utility.GetFilePath("deckinfo.xml"));
                //互換性対応
                if (deckInfo.DeckInfo.Count > 0 && deckInfo.DeckInfo[0].IsIsCostLimitedNull())
                {
                    foreach(DsDeckInfo.DeckInfoRow row in deckInfo.DeckInfo)
                    {
                        row.IsCostLimited = false;
                        row.LimitedCost = 100;
                    }
                }
                //表示順
                if (deckInfo.DeckInfo.Count > 0 && deckInfo.DeckInfo[0].IsDisplayIndexNull())
                {
                    Dictionary<string,int> displayIndex = new Dictionary<string,int>();
                    foreach (DsDeckInfo.DeckInfoRow row in deckInfo.DeckInfo)
                    {
                        if(!displayIndex.ContainsKey(row.Type))
                        {
                            displayIndex.Add(row.Type,1);
                        }

                        row.DisplayIndex = displayIndex[row.Type];
                        displayIndex[row.Type] = displayIndex[row.Type] + 1;
                    }
                }
            }
            else
            {
                foreach (string type in new[] { "攻援", "守援", "イベント" })
                {
                    for (int i = 1; i <= 5; i++)
                    {
                        DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.NewDeckInfoRow();
                        row.Number = i;
                        row.Name = type + "デッキ" + i.ToString();
                        row.Type = type;
                        row.Lock = false;
                        row.IsCostLimited = false;
                        row.LimitedCost = 100;
                        row.DisplayIndex = i;
                        row.Power = 0;
                        row.MainCount = 0;
                        row.BasePower = 0;
                        row.SubCount = 0;
                        row.Cost = 0;
                        deckInfo.DeckInfo.AddDeckInfoRow(row);
                    }
                }
                deckInfo.AcceptChanges();
                deckInfo.WriteXml(Utility.GetFilePath("deckinfo.xml"));
            }
            #endregion

            //各表示順の先頭を設定する
            foreach (CalcType type in Enum.GetValues(typeof(CalcType)))
            {
                DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.FirstOrDefault(r => r.Type == type.ToString() && r.DisplayIndex == 1);
                if(row != null)
                {
                    editCalcNumber[type] = row.Number;
                }
            }

            //表示順コンボボックス作成
            CreateDeckDisplayIndex(CalcType.攻援);

            CmbDeck.DisplayMemberPath = "Name";
            CmbDeck.SelectedValuePath = "Number";
            CmbDeck.ItemsSource = deckInfo.DeckInfo.Where(r => r.Type == "攻援").OrderBy(r => r.DisplayIndex).AsDataView();
            CmbDeck.SelectedIndex = 0;

            #region 自動保存変換
            ConvertAutoDeckToUserDeck();
            #endregion

            int number = (int)CmbDeck.SelectedValue;
            LoadDeckNumber(CalcType.攻援, number);
            LblMainInfo.Content = dsMainSelect.Card.Count.ToString();
            SetSubInfo(dsSubSelect.Card.Count);

            SetDataGridColumnButton(DgDeckMain, calcType);
            SetDataGridColumnButton(DgDeckSub, calcType);
            SetDataGridColumnButton(DgDeckCards, calcType);
            SetDataGridColumnButton(DgCards, CalcType.共通);
            ReCalcAll();

            ClearCardInput();

            this.TBtnSelectAllDeck.IsChecked = true;
        }

        /// <summary>
        /// Window閉じる処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                //位置とサイズを保存する
                if (dsSetting.Window.Count > 0)
                {
                    DsSetting.WindowRow windowSetting = dsSetting.Window[0];
                    windowSetting.PosX = this.Left;
                    windowSetting.PosY = this.Top;
                    windowSetting.Height = this.Height;
                    windowSetting.Width = this.Width;
                    dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
                }

                //自動保存
                SaveDeckNumber(calcType, editCalcNumber[calcType]);
            }
            catch (Exception ex)
            {
            }
        }

        #region 初期化
        private void LoadGilrs()
        {
            InitGirls();
            DgGirls.ItemsSource = dsGilrs.Girls;
            atkBonusList.Clear();
            foreach (var item in AtkBounus)
            {
                atkBonusList.Add(new SelectionBonusInfo()
                {
                    Name = item,
                    IsBonusSelected = false,
                });
            }
            defBonusList.Clear();
            foreach (var item in DefBounus)
            {
                defBonusList.Add(new SelectionBonusInfo()
                {
                    Name = item,
                    IsBonusSelected = false,
                });
            }
            LstAtkBonus.SelectedValuePath = "Name";
            LstAtkBonus.DisplayMemberPath = "Name";
            LstAtkBonus.ItemsSource = atkBonusList.OrderBy(b => b.IsBonusSelected ? 0 : 1);
            LstDefBonus.SelectedValuePath = "Name";
            LstDefBonus.DisplayMemberPath = "Name";
            LstDefBonus.ItemsSource = defBonusList.OrderBy(b => b.IsBonusSelected ? 0 : 1);
            CmbCardGirlName.SelectedValuePath = "Name";
            CmbCardGirlName.DisplayMemberPath = "Display";
            CmbCardGirlName.ItemsSource = nameList;

            LstGirlsName.SelectedValuePath = "Name";
            LstGirlsName.DisplayMemberPath = "Display";
            LstGirlsName.ItemsSource = nameList;
        }

        private void InitGirls()
        {
            //攻援、守選抜ボーナス取得
            AtkBounus = new HashSet<string>();
            DefBounus = new HashSet<string>();
            girlsSelectionBonusList = new Dictionary<string, GirlsSelectionBonus>();

            foreach (DsGirls.GirlsRow row in dsGilrs.Girls)
            {
                if (!GirlNames.Contains(row.名前))
                {
                    GirlNames.Add(row.名前);
                    nameList.Add(new NameInfo() { Name = row.名前, Hiragana = row.なまえ, Roma = row.Name, Attr = row.属性 });
                }
                if (!girlsList.ContainsKey(row.名前))
                {
                    girlsList.Add(row.名前, row);
                }
                if (!girlsSelectionBonusList.ContainsKey(row.名前))
                {
                    girlsSelectionBonusList.Add(row.名前, new GirlsSelectionBonus() { Name = row.名前 });
                }

                if (row.攻援1 != "-")
                {
                    girlsSelectionBonusList[row.名前].AtkBonus.Add(row.攻援1);
                    if (!AtkBounus.Contains(row.攻援1))
                    {
                        AtkBounus.Add(row.攻援1);
                    }
                }
                if (row.攻援2 != "-")
                {
                    girlsSelectionBonusList[row.名前].AtkBonus.Add(row.攻援2);
                    if (!AtkBounus.Contains(row.攻援2))
                    {
                        AtkBounus.Add(row.攻援2);
                    }
                }
                if (row.攻援3 != "-")
                {
                    girlsSelectionBonusList[row.名前].AtkBonus.Add(row.攻援3);
                    if (!AtkBounus.Contains(row.攻援3))
                    {
                        AtkBounus.Add(row.攻援3);
                    }
                }
                if (row.守援1 != "-")
                {
                    girlsSelectionBonusList[row.名前].DefBonus.Add(row.守援1);
                    if (!DefBounus.Contains(row.守援1))
                    {
                        DefBounus.Add(row.守援1);
                    }
                }
                if (row.守援2 != "-")
                {
                    girlsSelectionBonusList[row.名前].DefBonus.Add(row.守援2);
                    if (!DefBounus.Contains(row.守援2))
                    {
                        DefBounus.Add(row.守援2);
                    }
                }
                if (row.守援3 != "-")
                {
                    girlsSelectionBonusList[row.名前].DefBonus.Add(row.守援3);
                    if (!DefBounus.Contains(row.守援3))
                    {
                        DefBounus.Add(row.守援3);
                    }
                }
            }
        }

        private void InitDataGridSet()
        {
            bool isNewColumn = false;
            foreach (var dset in dataGridSet)
            {
                //
                dset.Value.Grid.MouseEnter += (s, mea) =>
                {
                    if (dsSetting.Etc[0].EditColumn)
                    {
                        Popup p = dataGridSet[(DataGrid)s].Popup;
                        p.IsOpen = true;
                    }
                };
                dset.Value.Grid.MouseLeave += (s, mea) =>
                {
                    Popup p = dataGridSet[(DataGrid)s].Popup;
                    if (!p.IsMouseOver)
                    {
                        p.IsOpen = false;
                    }
                };
                dset.Value.Popup.MouseEnter += (s, mea) =>
                {
                    if (dsSetting.Etc[0].EditColumn)
                    {
                        ((Popup)s).IsOpen = true;
                    }
                };
                dset.Value.Popup.MouseLeave += (s, mea) =>
                {
                    DataGrid dg = dset.Value.Grid;
                    if (!dg.IsMouseOver)
                    {
                        ((Popup)s).IsOpen = false;
                    }
                };

                foreach (DataColumn dc in dset.Value.DataSet.Tables[0].Columns)
                {
                    //設定の初期化
                    foreach (string cType in Enum.GetNames(typeof(CalcType)))
                    {
                        var columnRow = dsSetting.Column.FirstOrDefault(c => c.GridName == dset.Key.Name && c.Type == cType && c.ColumnName == dc.ColumnName);
                        if (columnRow != null)
                        {
                        }
                        else
                        {
                            DataGridColumn dgc = dset.Key.Columns.FirstOrDefault(c => (string)c.Header == dc.ColumnName);
                            if (dgc != null)
                            {
                                columnRow = dsSetting.Column.NewColumnRow();
                                columnRow.ColumnName = dc.ColumnName;
                                columnRow.GridName = dset.Key.Name;
                                columnRow.Type = cType;
                                columnRow.Index = dgc.DisplayIndex < 0 ? dset.Key.Columns.IndexOf(dgc) : dgc.DisplayIndex;
                                var nc = dsSetting.Column.FirstOrDefault(r => r.GridName == columnRow.GridName && r.Type == columnRow.Type && r.Index == columnRow.Index);
                                if (nc != null)
                                {
                                    //同じ番号がいた場合
                                    foreach (DsSetting.ColumnRow cr in dsSetting.Column.Where(r => r.GridName == columnRow.GridName && r.Type == columnRow.Type && r.Index >= columnRow.Index))
                                    {
                                        cr.Index++;
                                    }
                                }

                                columnRow.Width = 100;
                                columnRow.Visibility = dgc.Visibility == Visibility.Visible;
                                if (dc.ColumnName.StartsWith("攻") && cType == CalcType.守援.ToString())
                                {
                                    columnRow.Visibility = false;
                                }
                                if (dc.ColumnName.StartsWith("守") && (cType == CalcType.攻援.ToString() || cType == CalcType.イベント.ToString()))
                                {
                                    columnRow.Visibility = false;
                                }
                                if (dc.ColumnName == "スペシャル" && (cType == CalcType.攻援.ToString() || cType == CalcType.守援.ToString()))
                                {
                                    columnRow.Visibility = false;
                                }
                                dsSetting.Column.AddColumnRow(columnRow);

                                isNewColumn = true;
                            }
                        }
                    }
                }
            }
            if (isNewColumn)
            {
                dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
            }
        }


        private void ReadCards()
        {
            dsDispCard.Clear();
            cardsList.Clear();
            foreach (DsCards.CardsRow cardRow in dsCards.Cards)
            {
                CreateDispCardRow(cardRow);
                CreateDeckCardRow(cardRow);
                if (Convert.ToInt32(cardRow.ID) > cardMax)
                {
                    cardMax = Convert.ToInt32(cardRow.ID);
                }

                if (!cardsList.ContainsKey(cardRow.ID))
                {
                    cardsList.Add(cardRow.ID, cardRow);
                }
            }
        }
        #endregion

        #region データ互換対応
        private void ConvertCards()
        {
            //スペシャルが設定されていない場合
            bool isChange = false;
            if (dsCards.Cards.Count > 0 && dsCards.Cards[0].IsスペシャルNull())
            {
                foreach (DsCards.CardsRow row in dsCards.Cards)
                {
                    if (row.IsスペシャルNull())
                    {
                        row.スペシャル = 0;
                        isChange = true;
                    }
                }
            }
            if (dsCards.Cards.Count > 0 && dsCards.Cards[0].IsLvNull())
            {
                foreach (DsCards.CardsRow row in dsCards.Cards)
                {
                    if (row.IsLvNull())
                    {
                        row.Lv = 1;
                        isChange = true;
                    }
                }
            }
            if (dsCards.Cards.Count > 0 && dsCards.Cards[0].Isボーナス有無Null())
            {
                foreach (DsCards.CardsRow row in dsCards.Cards)
                {
                    if (row.Isボーナス有無Null())
                    {
                        row.ボーナス有無 = false;
                        row.ボーナス = 0;
                        isChange = true;
                    }
                }
            }
            if (dsCards.Cards.Count > 0 && dsCards.Cards[0].IsダミーNull())
            {
                foreach (DsCards.CardsRow row in dsCards.Cards)
                {
                    if (row.IsダミーNull())
                    {
                        row.ダミー = false;
                        row.画像 = string.Empty;
                        row.フリー1 = string.Empty;
                        row.フリー2 = string.Empty;
                        row.フリー3 = string.Empty;
                        isChange = true;
                    }
                }
            }
            if (dsCards.Cards.Count > 0 && dsCards.Cards[0].Is表示順Null())
            {
                int count = 1;
                foreach (DsCards.CardsRow row in dsCards.Cards)
                {
                    if (row.Is表示順Null())
                    {
                        row.表示順 = count++;
                        isChange = true;
                    }
                }
            }
            //経験値、好感度
            if (dsCards.Cards.Count > 0 && dsCards.Cards[0].Is好感度Null())
            {
                foreach (DsCards.CardsRow row in dsCards.Cards)
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
                    if (row.Is成長Null())
                    {
                        //最大レベルの場合は成長を１００にする
                        if (row.Lv == GetMaxLv(row.レア))
                        {
                            row.成長 = 100;
                        }
                        else
                        {
                            row.成長 = 0;
                        }
                        isChange = true;
                    }
                }
            }


            if (isChange)
            {
                dsCards.AcceptChanges();
                dsCards.WriteXml(Utility.GetFilePath("cards.xml"));
#if DEBUG_
                    File.AppendAllText(Utility.GetFilePath("log.txt"), "[cards.xml]補完完了:" + dsCards.Cards.Count.ToString() + "\r\n");
#endif
            }
        }

        private void ConvertAutoDeckToUserDeck()
        {
            if (File.Exists(Utility.GetFilePath("auto_AtkDeck.xml")))
            {
                DsUserDeck deck = new DsUserDeck();
                deck.ReadXml(Utility.GetFilePath("auto_AtkDeck.xml"));
                deck.WriteXml(GetDeckNumberName(CalcType.攻援, 1));
                File.Delete(Utility.GetFilePath("auto_AtkDeck.xml"));
            }
            if (File.Exists(Utility.GetFilePath("auto_DefDeck.xml")))
            {
                DsUserDeck deck = new DsUserDeck();
                deck.ReadXml(Utility.GetFilePath("auto_DefDeck.xml"));
                deck.WriteXml(GetDeckNumberName(CalcType.守援, 1));
                File.Delete(Utility.GetFilePath("auto_DefDeck.xml"));
            }
            if (File.Exists(Utility.GetFilePath("auto_EventDeck.xml")))
            {
                DsUserDeck deck = new DsUserDeck();
                deck.ReadXml(Utility.GetFilePath("auto_EventDeck.xml"));
                deck.WriteXml(GetDeckNumberName(CalcType.イベント, 1));
                File.Delete(Utility.GetFilePath("auto_EventDeck.xml"));
            }
        }
        #endregion

        #region ガールタブ

        /// <summary>
        /// ガール一覧選択
        /// 選択すると、攻援ボーナス、守援ボーナスを先頭にし背景色を変える
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgGirls_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgGirls.SelectedItem != null)
            {
                DsGirls.GirlsRow girlsRow = (DgGirls.SelectedItem as DataRowView).Row as DsGirls.GirlsRow;

                ShowGirl();

                var girlsBonus = girlsSelectionBonusList[girlsRow.名前];
                foreach (var bonus in atkBonusList)
                {
                    if (girlsBonus.AtkBonus.Contains(bonus.Name))
                    {
                        bonus.IsBonusSelected = true;
                    }
                    else
                    {
                        bonus.IsBonusSelected = false;
                    }
                }
                foreach (var bonus in defBonusList)
                {
                    if (girlsBonus.DefBonus.Contains(bonus.Name))
                    {
                        bonus.IsBonusSelected = true;
                    }
                    else
                    {
                        bonus.IsBonusSelected = false;
                    }
                }
                LstAtkBonus.ItemsSource = atkBonusList.OrderBy(b => b.IsBonusSelected ? 0 : 1);
                LstDefBonus.ItemsSource = defBonusList.OrderBy(b => b.IsBonusSelected ? 0 : 1);

            }
            else
            {
                foreach (var bonus in atkBonusList)
                {
                    bonus.IsBonusSelected = false;
                }
                foreach (var bonus in defBonusList)
                {
                    bonus.IsBonusSelected = false;
                }
            }
        }

        private void LstAtkBonus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TxtSearchGirls.Text = string.Empty;
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var atkList = LstAtkBonus.SelectedItems.OfType<SelectionBonusInfo>().Select(b => b.Name).ToList();
                var defList = new List<string>();
                LstDefBonus.SelectedItem = null;

                SearchGirlsBonus(atkList, defList);
            }
            else
            {
                DgGirls.ItemsSource = dsGilrs.Girls;
            }
        }

        private void LstDefBonus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TxtSearchGirls.Text = string.Empty;
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                var atkList = new List<string>();
                var defList = LstDefBonus.SelectedItems.OfType<SelectionBonusInfo>().Select(b => b.Name).ToList();
                LstAtkBonus.SelectedItem = null;

                SearchGirlsBonus(atkList, defList);
            }
            else
            {
                DgGirls.ItemsSource = dsGilrs.Girls;
            }
        }

        private void SearchGirlsBonus(List<string> atkBonus, List<string> defBonus)
        {
            DgGirls.ItemsSource = dsGilrs.Girls.Where(r =>
                atkBonus.Contains(r.攻援1) ||
                atkBonus.Contains(r.攻援2) ||
                atkBonus.Contains(r.攻援3) ||
                defBonus.Contains(r.守援1) ||
                defBonus.Contains(r.守援2) ||
                defBonus.Contains(r.守援3)).AsDataView();
        }

        private void BtnAtkBonusClearSelect_Click(object sender, RoutedEventArgs e)
        {
            LstAtkBonus.SelectedItem = null;
        }

        private void BtnDefBonusClearSelect_Click(object sender, RoutedEventArgs e)
        {
            LstDefBonus.SelectedItem = null;
        }


        private void BtnCardShow_Click_1(object sender, RoutedEventArgs e)
        {
            PopShowGirl.IsOpen = !PopShowGirl.IsOpen;
            if (PopShowGirl.IsOpen)
            {
                BtnCardShow.Style = FindResource("BlueButton") as Style;
            }
            else
            {
                BtnCardShow.Style = FindResource(typeof(Button)) as Style;
            }
            ShowGirl();
        }

        /// <summary>
        /// 立ち絵画像表示
        /// </summary>
        private void ShowGirl()
        {
            if (PopShowGirl.IsOpen)
            {
                if (DgGirls.SelectedItem != null)
                {
                    string baseImage = dsSetting.Etc[0].ShowGirlPath;
                    if (!string.IsNullOrWhiteSpace(baseImage))
                    {
                        DsGirls.GirlsRow girlsRow = (DgGirls.SelectedItem as DataRowView).Row as DsGirls.GirlsRow;
                        string imageName = Path.Combine(baseImage,"profile_"+ girlsRow.ID + ".png");
                        if (File.Exists(imageName))
                        {
                            RenderOptions.SetBitmapScalingMode(ImgShowGirl, BitmapScalingMode.HighQuality);
                            ImgShowGirl.Source = new BitmapImage(new Uri(imageName));
                        }
                        else
                        {
                            ImgShowGirl.Source = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// CSV読込
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCardCSVLoad_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "CSV(*.csv;*.txt)|*.csv;*.txt";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    string[] lines = File.ReadAllLines(dialog.FileName, Encoding.GetEncoding("Shift_JIS"));
                    dsGilrs = new DsGirls();
                    Dictionary<string, int> csvColumn = new Dictionary<string, int>();
                    int lineCount = 0;
                    foreach (string line in lines)
                    {
                        string[] cells = line.Split(',');
                        if (lineCount > 0)
                        {
                            DsGirls.GirlsRow row = dsGilrs.Girls.NewGirlsRow();
                            row.ID = cells[csvColumn["ID"]].Trim('\"');
                            row.名前 = cells[csvColumn["名前"]].Trim('\"');
                            row.なまえ = cells[csvColumn["なまえ"]].Trim('\"');
                            row.Name = cells[csvColumn["Name"]].Trim('\"');
                            row.属性 = cells[csvColumn["属性"]].Trim('\"');
                            row.攻援1 = cells[csvColumn["攻援1"]].Trim('\"');
                            row.攻援2 = cells[csvColumn["攻援2"]].Trim('\"');
                            row.攻援3 = cells[csvColumn["攻援3"]].Trim('\"');
                            row.守援1 = cells[csvColumn["守援1"]].Trim('\"');
                            row.守援2 = cells[csvColumn["守援2"]].Trim('\"');
                            row.守援3 = cells[csvColumn["守援3"]].Trim('\"');
                            row.学年 = cells[csvColumn["学年"]].Trim('\"');
                            row.誕生日 = cells[csvColumn["誕生日"]].Trim('\"');
                            row.身長 = string.IsNullOrEmpty(cells[csvColumn["身長"]].Trim('\"')) ? 0 : Convert.ToInt32(cells[csvColumn["身長"]].Trim('\"'));
                            row.体重 = string.IsNullOrEmpty(cells[csvColumn["体重"]].Trim('\"')) ? 0 : Convert.ToInt32(cells[csvColumn["体重"]].Trim('\"'));
                            row.B = string.IsNullOrEmpty(cells[csvColumn["B"]].Trim('\"')) ? 0 : Convert.ToInt32(cells[csvColumn["B"]].Trim('\"'));
                            row.W = string.IsNullOrEmpty(cells[csvColumn["W"]].Trim('\"')) ? 0 : Convert.ToInt32(cells[csvColumn["W"]].Trim('\"'));
                            row.H = string.IsNullOrEmpty(cells[csvColumn["H"]].Trim('\"')) ? 0 : Convert.ToInt32(cells[csvColumn["H"]].Trim('\"'));
                            row.部活 = cells[csvColumn["部活"]].Trim('\"');
                            row.部室 = cells[csvColumn["部室"]].Trim('\"');
                            row.CV = cells[csvColumn["CV"]].Trim('\"');
                            dsGilrs.Girls.AddGirlsRow(row);
                        }
                        else
                        {
                            for (int i = 0; i < cells.Length; i++)
                            {
                                csvColumn.Add(cells[i].Trim('\"'), i);
                            }
                        }
                        lineCount++;
                    }
                    LoadGilrs();

                    foreach (DsGirls.GirlsRow girlsRow in dsGilrs.Girls)
                    {
                        //表示用カード更新
                        foreach (DsDispCard.DispCardRow dispRow in dsDispCard.DispCard)
                        {
                            if (dispRow.名前 == girlsRow.名前)
                            {
                                dispRow.属性 = girlsRow.属性;
                                dispRow.攻選抜ボーナス1 = girlsRow.攻援1;
                                dispRow.攻選抜ボーナス2 = girlsRow.攻援2;
                                dispRow.攻選抜ボーナス3 = girlsRow.攻援3;
                                dispRow.守選抜ボーナス1 = girlsRow.守援1;
                                dispRow.守選抜ボーナス2 = girlsRow.守援2;
                                dispRow.守選抜ボーナス3 = girlsRow.守援3;

                            }
                        }
                        foreach (DsDeckCard.DeckCardRow deckCardRow in dsDeckCard.DeckCard)
                        {
                            if (deckCardRow.名前 == girlsRow.名前)
                            {
                                deckCardRow.属性 = girlsRow.属性;
                                deckCardRow.攻選抜ボーナス1 = girlsRow.攻援1;
                                deckCardRow.攻選抜ボーナス2 = girlsRow.攻援2;
                                deckCardRow.攻選抜ボーナス3 = girlsRow.攻援3;
                                deckCardRow.守選抜ボーナス1 = girlsRow.守援1;
                                deckCardRow.守選抜ボーナス2 = girlsRow.守援2;
                                deckCardRow.守選抜ボーナス3 = girlsRow.守援3;
                            }
                        }

                        foreach (DsSelectCard.CardRow mainSelectRow in dsMainSelect.Card)
                        {
                            if (mainSelectRow.名前 == girlsRow.名前)
                            {
                                mainSelectRow.属性 = girlsRow.属性;
                                mainSelectRow.攻選抜ボーナス1 = girlsRow.攻援1;
                                mainSelectRow.攻選抜ボーナス2 = girlsRow.攻援2;
                                mainSelectRow.攻選抜ボーナス3 = girlsRow.攻援3;
                                mainSelectRow.守選抜ボーナス1 = girlsRow.守援1;
                                mainSelectRow.守選抜ボーナス2 = girlsRow.守援2;
                                mainSelectRow.守選抜ボーナス3 = girlsRow.守援3;

                            }
                        }

                        foreach (DsSelectCard.CardRow subSelectRow in dsSubSelect.Card)
                        {
                            if (subSelectRow.名前 == girlsRow.名前)
                            {
                                subSelectRow.属性 = girlsRow.属性;
                                subSelectRow.攻選抜ボーナス1 = girlsRow.攻援1;
                                subSelectRow.攻選抜ボーナス2 = girlsRow.攻援2;
                                subSelectRow.攻選抜ボーナス3 = girlsRow.攻援3;
                                subSelectRow.守選抜ボーナス1 = girlsRow.守援1;
                                subSelectRow.守選抜ボーナス2 = girlsRow.守援2;
                                subSelectRow.守選抜ボーナス3 = girlsRow.守援3;
                            }
                        }
                    }
                    ReCalcAll();
                    RefreshDeckCard();

                    ClearCardInput();

                    dsGilrs.WriteXml(Utility.GetFilePath("gilrs.xml"));
                    DialogWindow.Show(this, "CSV読み込み完了", DialogWindow.MessageType.Infomation);
                }
                catch (Exception ex)
                {
                    DialogWindow.Show(this, "CSV読み込みエラー", DialogWindow.MessageType.Error);
                }
            }
        }

        /// <summary>
        /// CSV書き込み
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCardCSVSave_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = "csv";
            dialog.Filter = "CSV|*.csv";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (FileStream fs = File.Open(dialog.FileName, FileMode.OpenOrCreate))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift_JIS")))
                    {
                        //出力対象カラム
                        List<string> columns = new List<string>() {"ID", "名前", "なまえ", "Name", "属性", "攻援1", "攻援2", "攻援3", "守援1", "守援2", "守援3", "学年", "誕生日", "身長", "体重", "B", "W", "H", "部活", "部室", "CV" };
                        StringBuilder sbc = new StringBuilder();
                        columns.ForEach(c =>
                        {
                            sbc.Append('"');
                            sbc.Append(c);
                            sbc.Append('"');
                            sbc.Append(',');
                        });
                        sbc.Length--;
                        sw.WriteLine(sbc.ToString());

                        foreach (DsGirls.GirlsRow row in dsGilrs.Girls)
                        {
                            StringBuilder sb = new StringBuilder();
                            columns.ForEach(c =>
                            {
                                sb.Append('"');
                                sb.Append(row[c]);
                                sb.Append('"');
                                sb.Append(',');
                            });
                            sb.Length--;
                            sw.WriteLine(sb.ToString());
                        }
                    }
                }
                DialogWindow.Show(this, "CSV書き込み完了", DialogWindow.MessageType.Infomation);
            }
        }

        private void TxtSearchGirls_TextChanged(object sender, TextChangedEventArgs e)
        {
            LstAtkBonus.SelectedItem = null;
            LstDefBonus.SelectedItem = null;
            TextBox textBox = sender as TextBox;
            DataGrid grid = DgGirls;
            DataSet ds = dsGilrs;
            Search.SearchName(textBox, grid, ds);
        }

        #endregion

        #region 所持カードタブ

        /// <summary>
        /// 名称検索
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TxtSearchOwnCard_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            DataGrid grid = DgCards;
            DataSet ds = dsDispCard;
            Search.SearchName(textBox, grid, ds);
        }

        private void BtnOwnCardCSVLoad_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "CSV(*.csv;*.txt)|*.csv;*.txt";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    string[] lines = File.ReadAllLines(dialog.FileName, Encoding.GetEncoding("Shift_JIS"));
                    DsCards cards = new DsCards();
                    int lineCount = 0;
                    Dictionary<string, int> csvColumn = new Dictionary<string, int>();
                    cardsList.Clear();
                    foreach (string line in lines)
                    {
                        if (lineCount > 0)
                        {
                            try
                            {
                                string[] cells = line.Split(',');
                                DsCards.CardsRow row = cards.Cards.NewCardsRow();
                                row.ID = cells[csvColumn["ID"]].Trim('\"');
                                row.名前 = cells[csvColumn["名前"]].Trim('\"');
                                row.種別 = cells[csvColumn["種別"]].Trim('\"');
                                row.レア = cells[csvColumn["レア"]].Trim('\"');
                                row.Lv = Convert.ToInt32(cells[csvColumn["Lv"]].Trim('\"'));
                                row.成長 = Convert.ToInt32(cells[csvColumn["成長"]].Trim('\"'));
                                row.進展 = Convert.ToInt32(cells[csvColumn["進展"]].Trim('\"'));
                                row.好感度 = Convert.ToInt32(cells[csvColumn["好感度"]].Trim('\"'));
                                row.コスト = Convert.ToInt32(cells[csvColumn["コスト"]].Trim('\"'));
                                row.攻援 = Convert.ToInt32(cells[csvColumn["攻援"]].Trim('\"'));
                                row.守援 = Convert.ToInt32(cells[csvColumn["守援"]].Trim('\"'));
                                row.スキル = cells[csvColumn["スキル"]].Trim('\"');
                                row.全属性スキル = string.IsNullOrEmpty(cells[csvColumn["全属性スキル"]].Trim('\"')) ? false : Convert.ToBoolean(cells[csvColumn["全属性スキル"]].Trim('\"'));
                                row.スキルLv = string.IsNullOrEmpty(cells[csvColumn["スキルLv"]].Trim('\"')) ? 1 : Convert.ToInt32(cells[csvColumn["スキルLv"]].Trim('\"'));
                                row.スペシャル = string.IsNullOrEmpty(cells[csvColumn["スペシャル"]].Trim('\"')) ? 0 : Convert.ToInt32(cells[csvColumn["スペシャル"]].Trim('\"'));
                                row.ボーナス有無 = string.IsNullOrEmpty(cells[csvColumn["ボーナス有無"]].Trim('\"')) ? false : Convert.ToBoolean(cells[csvColumn["ボーナス有無"]].Trim('\"'));
                                row.ボーナス = string.IsNullOrEmpty(cells[csvColumn["ボーナス"]].Trim('\"')) ? 0 : Convert.ToInt32(cells[csvColumn["ボーナス"]].Trim('\"'));
                                row.ダミー = string.IsNullOrEmpty(cells[csvColumn["ダミー"]].Trim('\"')) ? false : Convert.ToBoolean(cells[csvColumn["ダミー"]].Trim('\"'));
                                row.表示順 = string.IsNullOrEmpty(cells[csvColumn["表示順"]].Trim('\"')) ? 0 : Convert.ToInt32(cells[csvColumn["表示順"]].Trim('\"'));
                                row.フリー1 = cells[csvColumn["フリー1"]].Trim('\"');
                                row.フリー2 = cells[csvColumn["フリー2"]].Trim('\"');
                                row.フリー3 = cells[csvColumn["フリー3"]].Trim('\"');
                                row.画像 = cells[csvColumn["画像"]].Trim('\"');
                                cards.Cards.AddCardsRow(row);

                                if (!cardsList.ContainsKey(row.ID))
                                {
                                    cardsList.Add(row.ID, row);
                                }

                            }
                            catch (Exception ex)
                            {
                            }
                        }
                        else
                        {
                            string[] cells = line.Split(',');
                            for (int i = 0; i < cells.Count(); i++)
                            {
                                csvColumn.Add(cells[i].Trim('\"'), i);
                            }
                        }
                        lineCount++;
                    }

                    //削除
                    var deleteIDs = dsCards.Cards.Where(r => cards.Cards.Count(x => x.ID == r.ID) == 0).Select(r => r.ID).ToList();
                    deleteIDs.ForEach(id =>
                    {
                        DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == id);
                        if (cardsList.ContainsKey(cardRow.ID))
                        {
                            cardsList.Remove(cardRow.ID);
                        }
                        dsCards.Cards.RemoveCardsRow(cardRow);
                        dsCards.AcceptChanges();
                        DeleteCardRow(id);
                    });

                    foreach (DsCards.CardsRow loadCardRow in cards.Cards)
                    {
                        DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == loadCardRow.ID);
                        if (cardRow != null)
                        {
                            //更新
                            foreach (DataColumn column in dsCards.Cards.Columns)
                            {
                                cardRow[column] = loadCardRow[column.ColumnName];
                            }
                            UpdateCardRow(cardRow);
                        }
                        else
                        {
                            //新規追加
                            DsCards.CardsRow newCardRow = dsCards.Cards.NewCardsRow();
                            newCardRow.ID = (++cardMax).ToString();
                            foreach (DataColumn column in dsCards.Cards.Columns)
                            {
                                if (column.ColumnName != "ID")
                                {
                                    newCardRow[column] = loadCardRow[column.ColumnName];
                                }
                            }
                            dsCards.Cards.AddCardsRow(newCardRow);
                            if (!cardsList.ContainsKey(newCardRow.ID))
                            {
                                cardsList.Add(newCardRow.ID, newCardRow);
                            }
                            CreateDispCardRow(newCardRow);
                            CreateDeckCardRow(newCardRow);
                        }
                    }

                    //表示用カード更新
                    DgCards.ItemsSource = dsDispCard.DispCard.OrderBy(r => r.表示順).AsDataView();

                    ReCalcAll();
                    RefreshDeckCard();

                    ClearCardInput();

                    dsCards.WriteXml(Utility.GetFilePath("cards.xml"));

                    DialogWindow.Show(this, "CSV読み込み完了", DialogWindow.MessageType.Infomation);
                }
                catch (Exception ex)
                {
                    DialogWindow.Show(this, "CSV読み込みエラー", DialogWindow.MessageType.Error);
                }
            }

        }

        private void BtnOwnCardCSVSave_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog dialog = new System.Windows.Forms.SaveFileDialog();
            dialog.AddExtension = true;
            dialog.DefaultExt = "csv";
            dialog.Filter = "CSV|*.csv";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                using (FileStream fs = File.Open(dialog.FileName, FileMode.OpenOrCreate))
                {
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.GetEncoding("Shift_JIS")))
                    {
                        List<string> columns = new List<string>() { "ID", "名前", "種別", "レア", "進展", "好感度", "コスト", "Lv", "成長", "攻援", "守援", "スキル", "全属性スキル", "スキルLv", "スペシャル", "ボーナス有無", "ボーナス", "ダミー", "表示順", "フリー1", "フリー2", "フリー3", "画像" };
                        StringBuilder sbc = new StringBuilder();
                        columns.ForEach(c =>
                        {
                            sbc.Append('"');
                            sbc.Append(c);
                            sbc.Append('"');
                            sbc.Append(',');
                        });
                        sbc.Length--;
                        sw.WriteLine(sbc.ToString());

                        foreach (DsCards.CardsRow row in dsCards.Cards)
                        {
                            StringBuilder sb = new StringBuilder();
                            columns.ForEach(c =>
                            {
                                sb.Append('"');
                                sb.Append(row[c]);
                                sb.Append('"');
                                sb.Append(',');
                            });
                            sb.Length--;
                            sw.WriteLine(sb.ToString());
                        }
                    }
                }
                DialogWindow.Show(this, "CSV書き込み完了", DialogWindow.MessageType.Infomation);
            }
        }

        #region カード編集

        private void SelectDgCardsRow()
        {
            DsDispCard.DispCardRow dispRow = (DgCards.SelectedItem as DataRowView).Row as DsDispCard.DispCardRow;
            DsCards.CardsRow row = dsCards.Cards.FirstOrDefault(r => r.ID == dispRow.ID);
            CmbCardGirlName.SelectedValue = row.名前;
            CmbCardGirlRare.Text = row.レア;
            TxtCardGirlType.Text = row.種別;
            CmbCardGirlRank.Text = row.進展.ToString();
            CmbCardGirlFavor.SelectedValue = row.好感度;
            TxtCardGirlCost.Text = row.コスト.ToString();
            TxtCardGirlLv.Text = row.Lv.ToString();
            TxtCardGirlProgress.Text = row.成長.ToString();
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

            currentEditID = dispRow.ID;
            //名前の変更不可
            CmbCardGirlName.IsEnabled = false;
            BtnCardAdd.IsEnabled = false;
            BtnSearchGirlName.IsEnabled = false;
            BtnCardUpd.IsEnabled = true;
        }

        /// <summary>
        /// 所持カード追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCardAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DsCards.CardsRow cardRow = dsCards.Cards.NewCardsRow();
                cardRow.ID = (++cardMax).ToString();
                cardRow.名前 = CmbCardGirlName.SelectedValue.ToString();
                cardRow.コスト = string.IsNullOrEmpty(TxtCardGirlCost.Text) ? 1 : Convert.ToInt32(TxtCardGirlCost.Text);
                cardRow.Lv = string.IsNullOrEmpty(TxtCardGirlLv.Text) ? 1 : Convert.ToInt32(TxtCardGirlLv.Text);
                cardRow.成長 = string.IsNullOrEmpty(TxtCardGirlProgress.Text) ? 0 : Convert.ToInt32(TxtCardGirlProgress.Text);
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

                dsCards.Cards.AddCardsRow(cardRow);

                //表示順再設定
                ReDispOrder(cardRow);

                dsCards.WriteXml(Utility.GetFilePath("cards.xml"));

                if (!cardsList.ContainsKey(cardRow.ID))
                {
                    cardsList.Add(cardRow.ID, cardRow);
                }

                currentEditID = cardMax.ToString();

                CreateDispCardRow(cardRow);
                CreateDeckCardRow(cardRow);

                ReCalcAll();

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

                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == currentEditID);
                if (cardsList.ContainsKey(cardRow.ID))
                {
                    cardsList.Remove(cardRow.ID);
                }
                dsCards.Cards.RemoveCardsRow(cardRow);
                dsCards.AcceptChanges();

                dsCards.WriteXml(Utility.GetFilePath("cards.xml"));

                DeleteCardRow(currentEditID);

                //表示用カード更新
                RefreshDeckCard();

                ReCalcAll();

                DgCards.SelectedItem = null;

                ClearCardInput();
            }
        }

        private void BtnCardGirlEtc_Click(object sender, RoutedEventArgs e)
        {
            PopCardGirlEdit.IsOpen = !PopCardGirlEdit.IsOpen;
            SetCardGirlEtcButton();
        }

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

        private void ReDispOrder(DsCards.CardsRow cardRow)
        {
            //ID違いで同一表示順が存在した場合は振り直しを行う
            DsCards.CardsRow orderRow = dsCards.Cards.FirstOrDefault(r => r.ID != cardRow.ID && r.表示順 == cardRow.表示順);
            if (orderRow != null)
            {
                var reorderList = dsCards.Cards.Where(r => r.表示順 >= cardRow.表示順 && r.ID != cardRow.ID).OrderBy(r=>r.表示順).ToList();
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
                dsCards.AcceptChanges();
            }
        }

        private void BtnCardUpd_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(currentEditID))
            {
                try
                {
                    DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == currentEditID);
                    cardRow.コスト = string.IsNullOrEmpty(TxtCardGirlCost.Text) ? 1 : Convert.ToInt32(TxtCardGirlCost.Text);
                    cardRow.Lv = string.IsNullOrEmpty(TxtCardGirlLv.Text) ? 1 : Convert.ToInt32(TxtCardGirlLv.Text);
                    cardRow.成長 = string.IsNullOrEmpty(TxtCardGirlProgress.Text) ? 0 : Convert.ToInt32(TxtCardGirlProgress.Text);
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
                    cardRow.AcceptChanges();

                    //表示順再設定
                    ReDispOrder(cardRow);

                    dsCards.WriteXml(Utility.GetFilePath("cards.xml"));

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

        private void DeleteCardRow(string editId)
        {
            DsDispCard.DispCardRow dispCardRow = dsDispCard.DispCard.FirstOrDefault(r => r.ID == editId);
            dsDispCard.DispCard.RemoveDispCardRow(dispCardRow);
            dsDispCard.AcceptChanges();
            DsDeckCard.DeckCardRow deckCardRow = dsDeckCard.DeckCard.FirstOrDefault(r => r.ID == editId);
            if (deckCardRow != null)
            {
                dsDeckCard.DeckCard.RemoveDeckCardRow(deckCardRow);
                dsDeckCard.AcceptChanges();
            }
            DsSelectCard.CardRow mainSelectRow = dsMainSelect.Card.FirstOrDefault(r => r.ID == editId);
            if (mainSelectRow != null)
            {
                dsMainSelect.Card.RemoveCardRow(mainSelectRow);
                dsMainSelect.AcceptChanges();
            }
            DsSelectCard.CardRow subSelectRow = dsSubSelect.Card.FirstOrDefault(r => r.ID == editId);
            if (subSelectRow != null)
            {
                dsSubSelect.Card.RemoveCardRow(subSelectRow);
                dsSubSelect.AcceptChanges();
            }
        }

        private void UpdateCardRow(DsCards.CardsRow cardRow)
        {
            string editId = cardRow.ID;
            //表示用カード更新
            DsDispCard.DispCardRow dispRow = dsDispCard.DispCard.FirstOrDefault(r => r.ID == editId);
            DsGirls.GirlsRow girlRow = girlsList[cardRow.名前];
            SetDispCardRow(cardRow, dispRow, girlRow);

            Size scaleSize = new Size(240, 240);
            dispRow.画像 = Images.LoadImage(cardRow.画像, ref  scaleSize);
            dsDispCard.AcceptChanges();

            //デッキカード更新
            DsDeckCard.DeckCardRow deckCardRow = dsDeckCard.DeckCard.FirstOrDefault(r => r.ID == editId);
            if (deckCardRow != null)
            {
                deckCardRow.名前 = GetDispName(cardRow);
                deckCardRow.Lv = cardRow.Lv;
                deckCardRow.成長 = cardRow.成長;
                deckCardRow.コスト = cardRow.コスト;
                deckCardRow.進展 = cardRow.進展;
                deckCardRow.好感度 = cardRow.好感度;
                deckCardRow.レア = cardRow.レア;
                deckCardRow.攻援 = cardRow.攻援;
                deckCardRow.攻コスパ = cardRow.攻援 / cardRow.コスト;
                deckCardRow.攻ボーナス = GetAtkBonus(cardRow);
                deckCardRow.守援 = cardRow.守援;
                deckCardRow.守コスパ = cardRow.守援 / cardRow.コスト;
                deckCardRow.守ボーナス = GetAtkBonus(cardRow);
                deckCardRow.スキル = GetSkillName(cardRow);
                deckCardRow.攻スキル = GetAtkSkillPower(cardRow);
                deckCardRow.守スキル = GetDefSkillPower(cardRow);
                deckCardRow.スペシャル = cardRow.スペシャル;
                deckCardRow.ボーナス = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
                deckCardRow.ダミー = cardRow.ダミー;
                deckCardRow.表示順 = cardRow.表示順;
                deckCardRow.フリー1 = cardRow.フリー1;
                deckCardRow.フリー2 = cardRow.フリー2;
                deckCardRow.フリー3 = cardRow.フリー3;
                deckCardRow.画像 = dispRow.画像;
            }

            DsSelectCard.CardRow mainSelectRow = dsMainSelect.Card.FirstOrDefault(r => r.ID == editId);
            if (mainSelectRow != null)
            {
                mainSelectRow.名前 = GetDispName(cardRow);
                mainSelectRow.Lv = cardRow.Lv;
                mainSelectRow.成長 = cardRow.成長;
                mainSelectRow.進展 = cardRow.進展;
                mainSelectRow.好感度 = cardRow.好感度;
                mainSelectRow.コスト = cardRow.コスト;
                mainSelectRow.レア = cardRow.レア;
                mainSelectRow.応援値 = (IsAttack()) ? cardRow.攻援 : cardRow.守援;
                mainSelectRow.コスパ = (int)(mainSelectRow.応援値 / cardRow.コスト);
                mainSelectRow.スペシャル = cardRow.スペシャル;
                mainSelectRow.スキル = GetSkillName(cardRow);
                mainSelectRow.ボーナス = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
                mainSelectRow.ダミー = cardRow.ダミー;
                mainSelectRow.フリー1 = cardRow.フリー1;
                mainSelectRow.フリー2 = cardRow.フリー2;
                mainSelectRow.フリー3 = cardRow.フリー3;
                mainSelectRow.画像 = dispRow.画像;
            }
            DsSelectCard.CardRow subSelectRow = dsSubSelect.Card.FirstOrDefault(r => r.ID == editId);
            if (subSelectRow != null)
            {
                subSelectRow.名前 = GetDispName(cardRow);
                subSelectRow.Lv = cardRow.Lv;
                subSelectRow.成長 = cardRow.成長;
                subSelectRow.進展 = cardRow.進展;
                subSelectRow.好感度 = cardRow.好感度;
                subSelectRow.コスト = cardRow.コスト;
                subSelectRow.レア = cardRow.レア;
                subSelectRow.応援値 = (IsAttack()) ? cardRow.攻援 : cardRow.守援;
                subSelectRow.コスパ = (int)(subSelectRow.応援値 / cardRow.コスト);
                subSelectRow.スペシャル = cardRow.スペシャル;
                subSelectRow.スキル = GetSkillName(cardRow);
                subSelectRow.ボーナス = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
                subSelectRow.ダミー = cardRow.ダミー;
                subSelectRow.フリー1 = cardRow.フリー1;
                subSelectRow.フリー2 = cardRow.フリー2;
                subSelectRow.フリー3 = cardRow.フリー3;
                subSelectRow.画像 = dispRow.画像;
            }
        }

        private string GetSkillName(DsCards.CardsRow cardRow)
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

        private int GetAtkSkillPower(DsCards.CardsRow cardRow)
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

        private int GetDefSkillPower(DsCards.CardsRow cardRow)
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

        private void ClearCardInput()
        {
            //CmbCardGirlName.SelectedValue = string.Empty;
            CmbCardGirlRare.Text = string.Empty;
            TxtCardGirlType.Text = string.Empty;
            CmbCardGirlRank.Text = string.Empty;
            CmbCardGirlFavor.SelectedValue = 1;
            TxtCardGirlCost.Text = string.Empty;
            TxtCardGirlLv.Text = string.Empty;
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
            TxtCardGirlDispOrder.Text = dsCards.Cards.Count > 0 ?  (dsCards.Cards.Max(r=>r.表示順)+1).ToString() : "1";
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

            List<DsDispCard.DispCardRow> sortedCardList = new List<DsDispCard.DispCardRow>();
            foreach (DataRowView rowView in DgCards.Items)
            {
                sortedCardList.Add(rowView.Row as DsDispCard.DispCardRow);
            }
            for (int i = 0; i < sortedCardList.Count; i++)
            {
                var row = dsCards.Cards.FirstOrDefault(r => r.ID == sortedCardList[i].ID);
                row.表示順 = i + 1;
                UpdateCardRow(row);

            }
            dsCards.WriteXml(Utility.GetFilePath("cards.xml"));

            DialogWindow.Show(this, "表示順を更新しました", DialogWindow.MessageType.Infomation);
        }


        /// <summary>
        /// 所持カード「表示」ボタン押下イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnOwnCardShow_Click(object sender, RoutedEventArgs e)
        {
            PopCardsShow.IsOpen = !PopCardsShow.IsOpen;

            if (PopCardsShow.IsOpen)
            {

                ShowCardPanel fpanel = new ShowCardPanel();
                PnlCardsShow.Children.Add(fpanel);
                int i = 1;
                foreach (DsDispCard.DispCardRow dispRow in dsDispCard.DispCard.OrderBy(r=> r.表示順))
                {
                    ShowCardPanel panel = new ShowCardPanel();

                    panel.SetCardId(i.ToString(), cardsList[dispRow.ID], dispRow);
                    i++;
                    PnlCardsShow.Children.Add(panel);
                }
                ShowCardPanel epanel = new ShowCardPanel();
                PnlCardsShow.Children.Add(epanel);

                BtnOwnCardShow.Style = FindResource("BlueButton") as Style;
            }
            else
            {
                PnlCardsShow.Children.Clear();
                BtnOwnCardShow.Style = FindResource(typeof(Button)) as Style;
            }
        }

        private void ScrollViewer_ScrollChanged_1(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer sv = (ScrollViewer)sender;
            double center = e.HorizontalOffset + (sv.ViewportWidth / 2);

            Visual input = GetParent(PnlCardsShow.InputHitTest(new Point(center, 200)) as FrameworkElement, PnlCardsShow);
            if (input == null)
            {
                input = GetParent(PnlCardsShow.InputHitTest(new Point(center - 20, 200)) as FrameworkElement, PnlCardsShow);
            }
            if (input == null)
            {
                input = GetParent(PnlCardsShow.InputHitTest(new Point(center + 20, 200)) as FrameworkElement, PnlCardsShow);
            }
            if (input != null)
            {
                ShowCardPanel showCardPanel = input as ShowCardPanel;
                int index = PnlCardsShow.Children.IndexOf(showCardPanel);
                double diffX = 0;
                for (int i = -2; i < 3; i++)
                {
                    if ((index + i) >= 0 && (index + i) < (PnlCardsShow.Children.Count - 1))
                    {
                        ShowCardPanel targetPanel = PnlCardsShow.Children[index + i] as ShowCardPanel;

                        double x = (index + i) * 180 + diffX;
                        double diffCenter = Math.Abs(x - center + (targetPanel.ActualWidth / 4));
                        double scale = (180 / diffCenter);
                        if (scale > 0.8) scale = 0.8;
                        if (scale < 0.5) scale = 0.5;
                        if (scale > 0.5)
                        {
                            diffX += 360 * (scale - 0.5);
                        }
                        targetPanel.SetScale(scale, scale);
                    }
                }
            }
        }

        #endregion

        #region デッキタブ

        #region センバツ操作
        /// <summary>
        /// 主センバツクリア
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClearMain_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            foreach (var row in dsMainSelect.Card)
            {
                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == row.ID);
                CreateDeckCardRow(cardRow);
            }
            RefreshDeckCard();

            dsMainSelect.Clear();
            LblMainInfo.Content = dsMainSelect.Card.Count.ToString();
            ReCalcAll();
        }

        /// <summary>
        /// 副センバツクリア
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClearSub_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            foreach (var row in dsSubSelect.Card)
            {
                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == row.ID);
                CreateDeckCardRow(cardRow);
            }
            RefreshDeckCard();

            dsSubSelect.Clear();
            SetSubInfo(dsSubSelect.Card.Count);
            ReCalcAll();

        }

        /// <summary>
        /// 主センバツ、上に移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMainUp_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            if (DgDeckMain.SelectedItem != null)
            {
                //先頭の場合
                if (DgDeckMain.SelectedIndex == 0)
                {
                    return;
                }
                int selectIndex = DgDeckMain.SelectedIndex;
                DsSelectCard newMainCard = new DsSelectCard();
                for (int i = 0; i < dsMainSelect.Card.Count; i++)
                {
                    if (i == selectIndex)
                    {
                        newMainCard.Card.ImportRow(dsMainSelect.Card[i - 1]);
                    }
                    else if (i == (selectIndex - 1))
                    {
                        newMainCard.Card.ImportRow(dsMainSelect.Card[i + 1]);
                    }
                    else
                    {
                        newMainCard.Card.ImportRow(dsMainSelect.Card[i]);
                    }
                }
                dsMainSelect = newMainCard;
                DgDeckMain.ItemsSource = newMainCard.Card.DefaultView;

                ReCalcAll();

                DgDeckMain.SelectedIndex = selectIndex - 1;
            }
        }

        /// <summary>
        /// 主センバツ、下に移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMainDown_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            if (DgDeckMain.SelectedItem != null)
            {
                //最終の場合
                if (DgDeckMain.SelectedIndex == (dsMainSelect.Card.Count - 1))
                {
                    return;
                }
                int selectIndex = DgDeckMain.SelectedIndex;
                DsSelectCard newMainCard = new DsSelectCard();
                for (int i = 0; i < dsMainSelect.Card.Count; i++)
                {
                    if (i == selectIndex)
                    {
                        newMainCard.Card.ImportRow(dsMainSelect.Card[i + 1]);
                    }
                    else if (i == (selectIndex + 1))
                    {
                        newMainCard.Card.ImportRow(dsMainSelect.Card[i - 1]);
                    }
                    else
                    {
                        newMainCard.Card.ImportRow(dsMainSelect.Card[i]);
                    }
                }
                dsMainSelect = newMainCard;
                DgDeckMain.ItemsSource = newMainCard.Card.DefaultView;

                ReCalcAll();

                DgDeckMain.SelectedIndex = selectIndex + 1;
            }

        }

        /// <summary>
        /// 主センバツ追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMainAdd_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            if (DgDeckCards.SelectedItem != null)
            {
                if (dsMainSelect.Card.Count == 5)
                {
                    return;
                }
                string selectedBonus;
                if (LstBonus.SelectedIndex >= 0)
                {
                    selectedBonus = LstBonus.SelectedValue.ToString();
                }
                else
                {
                    selectedBonus = string.Empty;
                }

                DsDeckCard.DeckCardRow selectCardRow = (DgDeckCards.SelectedItem as DataRowView).Row as DsDeckCard.DeckCardRow;
                string id = selectCardRow.ID;
                AddSelect(id, dsMainSelect);

                LblMainInfo.Content = dsMainSelect.Card.Count.ToString();

                ReCalcAll();

                isEvent = false;
                LstBonus.SelectedValue = selectedBonus;
                isEvent = true;
            }
        }

        /// <summary>
        /// 副センバツ追加
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSubAdd_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            if (DgDeckCards.SelectedItem != null)
            {
                string selectedBonus;
                if (LstBonus.SelectedIndex >= 0)
                {
                    selectedBonus = LstBonus.SelectedValue.ToString();
                }
                else
                {
                    selectedBonus = string.Empty;
                }

                DsDeckCard.DeckCardRow selectCardRow = (DgDeckCards.SelectedItem as DataRowView).Row as DsDeckCard.DeckCardRow;
                string id = selectCardRow.ID;
                AddSelect(id, dsSubSelect);

                SetSubInfo(dsSubSelect.Card.Count);

                ReCalcAll();

                isEvent = false;
                LstBonus.SelectedValue = selectedBonus;
                isEvent = true;

            }
        }

        /// <summary>
        /// 副センバツ上に移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSubUp_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            if (DgDeckSub.SelectedItem != null)
            {
                //先頭の場合
                if (DgDeckSub.SelectedIndex == 0)
                {
                    return;
                }
                int selectIndex = DgDeckSub.SelectedIndex;
                DsSelectCard newSubCard = new DsSelectCard();
                for (int i = 0; i < dsSubSelect.Card.Count; i++)
                {
                    if (i == selectIndex)
                    {
                        newSubCard.Card.ImportRow(dsSubSelect.Card[i - 1]);
                    }
                    else if (i == (selectIndex - 1))
                    {
                        newSubCard.Card.ImportRow(dsSubSelect.Card[i + 1]);
                    }
                    else
                    {
                        newSubCard.Card.ImportRow(dsSubSelect.Card[i]);
                    }
                }
                dsSubSelect = newSubCard;
                DgDeckSub.ItemsSource = newSubCard.Card.DefaultView;

                DgDeckSub.SelectedIndex = selectIndex - 1;
            }
        }

        /// <summary>
        /// 副センバツ下に移動
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSubDown_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            if (DgDeckSub.SelectedItem != null)
            {
                //最終の場合
                if (DgDeckSub.SelectedIndex == (dsSubSelect.Card.Count - 1))
                {
                    return;
                }
                int selectIndex = DgDeckSub.SelectedIndex;
                DsSelectCard newSubCard = new DsSelectCard();
                for (int i = 0; i < dsSubSelect.Card.Count; i++)
                {
                    if (i == selectIndex)
                    {
                        newSubCard.Card.ImportRow(dsSubSelect.Card[i + 1]);
                    }
                    else if (i == (selectIndex + 1))
                    {
                        newSubCard.Card.ImportRow(dsSubSelect.Card[i - 1]);
                    }
                    else
                    {
                        newSubCard.Card.ImportRow(dsSubSelect.Card[i]);
                    }
                }
                dsSubSelect = newSubCard;
                DgDeckSub.ItemsSource = newSubCard.Card.DefaultView;

                DgDeckSub.SelectedIndex = selectIndex + 1;
            }
        }

        /// <summary>
        /// 主センバツから取り除く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnMainRemove_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            if (DgDeckMain.SelectedItem != null)
            {
                string selectedBonus;
                if (LstBonus.SelectedIndex >= 0)
                {
                    selectedBonus = LstBonus.SelectedValue.ToString();
                }
                else
                {
                    selectedBonus = string.Empty;
                }

                DsSelectCard.CardRow selectCardRow = (DgDeckMain.SelectedItem as DataRowView).Row as DsSelectCard.CardRow;

                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == selectCardRow.ID);
                CreateDeckCardRow(cardRow);
                //    RefreshDeckCard();

                dsMainSelect.Card.RemoveCardRow(selectCardRow);
                dsMainSelect.AcceptChanges();

                LblMainInfo.Content = dsMainSelect.Card.Count.ToString();

                ReCalcAll();

                isEvent = false;
                LstBonus.SelectedValue = selectedBonus;
                isEvent = true;
            }
        }

        /// <summary>
        /// 副センバツから取り除く
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSubRemove_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            if (DgDeckSub.SelectedItem != null)
            {
                string selectedBonus;
                if (LstBonus.SelectedIndex >= 0)
                {
                    selectedBonus = LstBonus.SelectedValue.ToString();
                }
                else
                {
                    selectedBonus = string.Empty;
                }

                DsSelectCard.CardRow selectCardRow = (DgDeckSub.SelectedItem as DataRowView).Row as DsSelectCard.CardRow;

                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == selectCardRow.ID);
                CreateDeckCardRow(cardRow);

                dsSubSelect.Card.RemoveCardRow(selectCardRow);
                dsSubSelect.AcceptChanges();

                SetSubInfo(dsSubSelect.Card.Count);

                ReCalcAll();

                isEvent = false;
                LstBonus.SelectedValue = selectedBonus;
                isEvent = true;
            }

        }


        #endregion

        /// <summary>
        /// デッキカード選択
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgDeckCards_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgDeckCards.SelectedItem != null)
            {
                DgDeckSub.SelectedItem = null;
                DgDeckMain.SelectedItem = null;
                LstBonus.SelectedItem = null;

                DsDeckCard.DeckCardRow selectCardRow = (DgDeckCards.SelectedItem as DataRowView).Row as DsDeckCard.DeckCardRow;
                SelectBonusList(selectCardRow.ID);
            }
            else
            {
                //選抜ボーナスの選択をクリアする
                List<SelectionBonusInfo> selectionBonusInfo = LstBonus.ItemsSource as List<SelectionBonusInfo>;
                if (selectionBonusInfo != null)
                {
                    foreach (var bonus in selectionBonusInfo)
                    {
                        bonus.IsBonusSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// 主センバツ一覧選択変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgDeckMain_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgDeckMain.SelectedItem != null)
            {
                DgDeckSub.SelectedItem = null;
                DgDeckCards.SelectedItem = null;
                LstBonus.SelectedItem = null;

                DsSelectCard.CardRow selectCardRow = (DgDeckMain.SelectedItem as DataRowView).Row as DsSelectCard.CardRow;

                SelectBonusList(selectCardRow.ID);
            }
            else
            {
                List<SelectionBonusInfo> selectionBonusInfo = LstBonus.ItemsSource as List<SelectionBonusInfo>;
                if (selectionBonusInfo != null)
                {
                    foreach (var bonus in selectionBonusInfo)
                    {
                        bonus.IsBonusSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// 副センバツ一覧選択変更イベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgDeckSub_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgDeckSub.SelectedItem != null)
            {
                DgDeckMain.SelectedItem = null;
                DgDeckCards.SelectedItem = null;
                LstBonus.SelectedItem = null;

                DsSelectCard.CardRow selectCardRow = (DgDeckSub.SelectedItem as DataRowView).Row as DsSelectCard.CardRow;

                SelectBonusList(selectCardRow.ID);
            }
            else
            {
                List<SelectionBonusInfo> selectionBonusInfo = LstBonus.ItemsSource as List<SelectionBonusInfo>;
                if (selectionBonusInfo != null)
                {
                    foreach (var bonus in selectionBonusInfo)
                    {
                        bonus.IsBonusSelected = false;
                    }
                }
            }
        }

        /// <summary>
        /// 選抜ボーナスを選択し、選択表示にする
        /// </summary>
        /// <param name="id"></param>
        private void SelectBonusList(string id)
        {
            List<SelectionBonusInfo> selectionBonusInfo = LstBonus.ItemsSource as List<SelectionBonusInfo>;
            if (selectionBonusInfo != null)
            {
                if (IsAttack())
                {
                    //攻援、イベント時は攻選抜ボーナス
                    DsCards.CardsRow cardRow = cardsList[id];
                    var girlsBonusList = girlsSelectionBonusList[cardRow.名前];
                    foreach (var bonus in selectionBonusInfo)
                    {
                        bonus.IsBonusSelected = girlsBonusList.AtkBonus.Contains(bonus.Name);
                    }
                }
                else
                {
                    //守援時は守選抜ボーナス
                    DsCards.CardsRow cardRow = cardsList[id];
                    var girlsBonusList = girlsSelectionBonusList[cardRow.名前];
                    foreach (var bonus in selectionBonusInfo)
                    {
                        bonus.IsBonusSelected = girlsBonusList.DefBonus.Contains(bonus.Name);
                    }
                }
            }
        }

        /// <summary>
        /// 「攻援」切替
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBtnAtk_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                SaveDeckNumber(calcType, editCalcNumber[calcType]);
                if (TBtnDef.IsChecked == true)
                {
                    TBtnDef.IsChecked = false;
                }
                else if (TBtnEvent.IsChecked == true)
                {
                    TBtnEvent.IsChecked = false;
                }

                calcType = CalcType.攻援;
                //Deck表示順再作成
                CreateDeckDisplayIndex(calcType);
                CmbDeck.ItemsSource = deckInfo.DeckInfo.Where(r => r.Type == calcType.ToString()).OrderBy(r => r.DisplayIndex).AsDataView();
                CmbDeck.SelectedValue = editCalcNumber[calcType];

                SetDataGridColumnButton(DgDeckMain, calcType);
                SetDataGridColumnButton(DgDeckSub, calcType);
                SetDataGridColumnButton(DgDeckCards, calcType);

                LblTotal.Foreground = FindResource("AtkForeground") as Brush;

                LblMainInfo.Content = dsMainSelect.Card.Count.ToString();
                SetSubInfo(dsSubSelect.Card.Count);

                //応援値の書き換え
                foreach (DsSelectCard.CardRow row in dsMainSelect.Card)
                {
                    DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == row.ID);
                    row.応援値 = cardRow.攻援;
                }
                foreach (DsSelectCard.CardRow row in dsSubSelect.Card)
                {
                    DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == row.ID);
                    row.応援値 = cardRow.攻援;
                }
                ReCalcAll();

                BdrStatus.Background = FindResource("AtkBackground") as LinearGradientBrush;
            }
        }

        /// <summary>
        /// 「守援」切替
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TBtnDef_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                SaveDeckNumber(calcType, editCalcNumber[calcType]);

                if (TBtnAtk.IsChecked == true)
                {
                    TBtnAtk.IsChecked = false;
                }
                else if (TBtnEvent.IsChecked == true)
                {
                    TBtnEvent.IsChecked = false;
                }

                calcType = CalcType.守援;
                //Deck表示順再作成
                CreateDeckDisplayIndex(calcType);
                CmbDeck.ItemsSource = deckInfo.DeckInfo.Where(r => r.Type == calcType.ToString()).OrderBy(r => r.DisplayIndex).AsDataView();
                CmbDeck.SelectedValue = editCalcNumber[calcType];
                LblTotal.Foreground = FindResource("DefForeground") as Brush;

                SetDataGridColumnButton(DgDeckMain, calcType);
                SetDataGridColumnButton(DgDeckSub, calcType);
                SetDataGridColumnButton(DgDeckCards, calcType);

                LblMainInfo.Content = dsMainSelect.Card.Count.ToString();
                SetSubInfo(dsSubSelect.Card.Count);

                foreach (DsSelectCard.CardRow row in dsMainSelect.Card)
                {
                    DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == row.ID);
                    row.応援値 = cardRow.守援;
                }
                foreach (DsSelectCard.CardRow row in dsSubSelect.Card)
                {
                    DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == row.ID);
                    row.応援値 = cardRow.守援;
                }

                ReCalcAll();

                BdrStatus.Background = FindResource("DefBackground") as LinearGradientBrush;

            }
        }

        private void TBtnEvent_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsLoaded)
            {
                SaveDeckNumber(calcType, editCalcNumber[calcType]);

                if (TBtnDef.IsChecked == true)
                {
                    TBtnDef.IsChecked = false;
                }
                else if (TBtnAtk.IsChecked == true)
                {
                    TBtnAtk.IsChecked = false;
                }

                calcType = CalcType.イベント;
                //Deck表示順再作成
                CreateDeckDisplayIndex(calcType);
                CmbDeck.ItemsSource = deckInfo.DeckInfo.Where(r => r.Type == calcType.ToString()).OrderBy(r => r.DisplayIndex).AsDataView();
                CmbDeck.SelectedValue = editCalcNumber[calcType];
                LblTotal.Foreground = FindResource("EventForeground") as Brush;

                SetDataGridColumnButton(DgDeckMain, calcType);
                SetDataGridColumnButton(DgDeckSub, calcType);
                SetDataGridColumnButton(DgDeckCards, calcType);

                LblMainInfo.Content = dsMainSelect.Card.Count.ToString();
                SetSubInfo(dsSubSelect.Card.Count);

                //応援値の書き換え
                foreach (DsSelectCard.CardRow row in dsMainSelect.Card)
                {
                    DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == row.ID);
                    row.応援値 = cardRow.攻援;
                }
                foreach (DsSelectCard.CardRow row in dsSubSelect.Card)
                {
                    DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == row.ID);
                    row.応援値 = cardRow.攻援;
                }
                ReCalcAll();

                BdrStatus.Background = FindResource("EventBackground") as LinearGradientBrush;
            }
        }

        private void TBtnDef_Click(object sender, RoutedEventArgs e)
        {
            TBtnDef.IsChecked = true;
        }

        private void TBtnAtk_Click(object sender, RoutedEventArgs e)
        {
            TBtnAtk.IsChecked = true;
        }

        private void TBtnEvent_Click(object sender, RoutedEventArgs e)
        {
            TBtnEvent.IsChecked = true;
        }

        private void LstBonus_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LstBonus.SelectedItem = null;
            ClearSelectionBonusDisplay();
        }

        private void ClearSelectionBonusDisplay()
        {
            foreach (DsSelectCard.CardRow cardRow in dsMainSelect.Card)
            {
                cardRow.選抜選択 = false;
            }
            foreach (DsSelectCard.CardRow cardRow in dsSubSelect.Card)
            {
                cardRow.選抜選択 = false;
            }
            foreach (DsDeckCard.DeckCardRow cardRow in dsDeckCard.DeckCard)
            {
                cardRow.選抜選択 = false;
            }
        }

        private bool IsSelectionBonusSelection(DataRow row)
        {
            if (LstBonus.SelectedItems != null && LstBonus.SelectedItems.Count > 0)
            {
                var bonus = LstBonus.SelectedItems.OfType<SelectionBonusInfo>().ToList();
                HashSet<string> bonusList = new HashSet<string>();
                bonus.ForEach(b => bonusList.Add(b.Name));
                foreach (string bonusName in new string[] {
                    "攻選抜ボーナス1",
                    "攻選抜ボーナス2",
                    "攻選抜ボーナス3",
                    "守選抜ボーナス1",
                    "守選抜ボーナス2",
                    "守選抜ボーナス3",
                })
                {
                    if (bonusList.Contains(row[bonusName] as string)) return true;
                }
            }
            return false;
        }

        private void LstBonus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent)
            {
                ClearSelectionBonusDisplay();
                if (LstBonus.SelectedItems != null && LstBonus.SelectedItems.Count > 0)
                {
                    DgDeckMain.SelectedItem = null;
                    DgDeckSub.SelectedItem = null;
                    DgDeckCards.SelectedItem = null;

                    var bonus = LstBonus.SelectedItems.OfType<SelectionBonusInfo>().ToList();
                    HashSet<string> bonusList = new HashSet<string>();
                    bonus.ForEach(b => bonusList.Add(b.Name));

                    foreach (DsSelectCard.CardRow cardRow in dsMainSelect.Card.Where(r =>
                        bonusList.Contains(r.攻選抜ボーナス1) ||
                        bonusList.Contains(r.攻選抜ボーナス2) ||
                        bonusList.Contains(r.攻選抜ボーナス3) ||
                        bonusList.Contains(r.守選抜ボーナス1) ||
                        bonusList.Contains(r.守選抜ボーナス2) ||
                        bonusList.Contains(r.守選抜ボーナス3)))
                    {
                        cardRow.選抜選択 = true;
                    }
                    foreach (DsSelectCard.CardRow cardRow in dsSubSelect.Card.Where(r =>
                        bonusList.Contains(r.攻選抜ボーナス1) ||
                        bonusList.Contains(r.攻選抜ボーナス2) ||
                        bonusList.Contains(r.攻選抜ボーナス3) ||
                        bonusList.Contains(r.守選抜ボーナス1) ||
                        bonusList.Contains(r.守選抜ボーナス2) ||
                        bonusList.Contains(r.守選抜ボーナス3)))
                    {
                        cardRow.選抜選択 = true;
                    }
                    foreach (DsDeckCard.DeckCardRow cardRow in dsDeckCard.DeckCard.Where(r =>
                        bonusList.Contains(r.攻選抜ボーナス1) ||
                        bonusList.Contains(r.攻選抜ボーナス2) ||
                        bonusList.Contains(r.攻選抜ボーナス3) ||
                        bonusList.Contains(r.守選抜ボーナス1) ||
                        bonusList.Contains(r.守選抜ボーナス2) ||
                        bonusList.Contains(r.守選抜ボーナス3)))
                    {
                        cardRow.選抜選択 = true;
                    }
                }
            }
        }

        /// <summary>
        /// DataGridクリック（チェックボックスクリック）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DgDeckMain_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
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
                    DsSelectCard.CardRow cardRow = row.Row as DsSelectCard.CardRow;
                    if (cardRow != null)
                    {
                        //クリックされたのがチェックボックスだった場合は、
                        //バインドされてるbool値を切り替える
                        cardRow.スキル発動 = !cardRow.スキル発動;

                        ReCalcAll();
                    }
                }
            }
        }

        /// <summary>
        /// 最大コスト取得
        /// </summary>
        /// <returns></returns>
        private int GetCostMax()
        {
            switch (calcType)
            {
                case CalcType.イベント:
                case CalcType.攻援:
                    return dsSetting.User[0].AtkCost;
                case CalcType.守援:
                    return dsSetting.User[0].DefCost;
            }
            return 20;
        }

        #region デッキ追加
        private void AddSelect(string id, DsSelectCard dsSelect)
        {
            DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == id);
            if (cardRow != null)
            {
                DsSelectCard.CardRow addRow = dsSelect.Card.NewCardRow();

                SetSelectCardRow(id, addRow, cardRow);

                dsSelect.Card.AddCardRow(addRow);

                //デッキカードから取り除く
                DsDeckCard.DeckCardRow deckCardRow = dsDeckCard.DeckCard.FirstOrDefault(r => r.ID == id);
                if (deckCardRow != null)
                {
                    dsDeckCard.DeckCard.RemoveDeckCardRow(deckCardRow);
                }
            }
        }

        private void AddCmpSelect(string id, DsSelectCard dsCmp)
        {
            DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == id);
            if (cardRow != null)
            {
                DsSelectCard.CardRow addRow = dsCmp.Card.NewCardRow();

                SetSelectCardRow(id, addRow, cardRow);

                dsCmp.Card.AddCardRow(addRow);
            }
        }

        private void SetSelectCardRow(string id, DsSelectCard.CardRow addRow, DsCards.CardsRow cardRow)
        {
            DsGirls.GirlsRow addGilrsRow = girlsList[cardRow.名前];
            DsDispCard.DispCardRow dispRow = dsDispCard.DispCard.FirstOrDefault(r => r.ID == id);

            addRow.ID = id;
            addRow.名前 = GetDispName(cardRow);
            addRow.姓名 = GetSortName(cardRow);
            addRow.なまえ = addGilrsRow.なまえ;
            addRow.Name = addGilrsRow.Name;
            addRow.属性 = addGilrsRow.属性;
            addRow.レア = cardRow.レア;
            addRow.進展 = cardRow.進展;
            addRow.好感度 = cardRow.好感度;
            addRow.Lv = cardRow.Lv;
            addRow.成長 = cardRow.成長;
            addRow.スキル = GetSkillName(cardRow);
            addRow.コスト = cardRow.コスト;
            addRow.攻スキル = GetAtkSkillPower(cardRow);
            addRow.守スキル = GetDefSkillPower(cardRow);
            addRow.ボーナス = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
            addRow.スペシャル = cardRow.スペシャル;
            addRow.応援値 = (IsAttack()) ? cardRow.攻援 : cardRow.守援;
            addRow.コスパ = (int)(addRow.応援値 / cardRow.コスト);
            addRow.部活 = addGilrsRow.部室;
            addRow.攻選抜ボーナス1 = addGilrsRow.攻援1;
            addRow.攻選抜ボーナス2 = addGilrsRow.攻援2;
            addRow.攻選抜ボーナス3 = addGilrsRow.攻援3;
            addRow.守選抜ボーナス1 = addGilrsRow.守援1;
            addRow.守選抜ボーナス2 = addGilrsRow.守援2;
            addRow.守選抜ボーナス3 = addGilrsRow.守援3;
            addRow.選抜選択 = IsSelectionBonusSelection(addRow);
            addRow.ダミー = cardRow.ダミー;
            addRow.フリー1 = cardRow.フリー1;
            addRow.フリー2 = cardRow.フリー2;
            addRow.フリー3 = cardRow.フリー3;
            addRow.画像 = dispRow.画像;
        }
        #endregion

        #region デッキ関連イベント
        /// <summary>
        /// デッキロッククリックイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkDeckLock_Click(object sender, RoutedEventArgs e)
        {
            int currentNumber = (int)CmbDeck.SelectedValue;
            DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == currentNumber && r.Type == calcType.ToString());
            if (row != null)
            {
                row.Lock = ChkDeckLock.IsChecked ?? false;
                deckInfo.AcceptChanges();
                deckInfo.WriteXml(Utility.GetFilePath("deckinfo.xml"));
            }
        }

        private void ChkDeckLock_Checked(object sender, RoutedEventArgs e)
        {
            ChkLockIco.Fill = FindResource("IcoLock") as Brush;
        }

        private void ChkDeckLock_Unchecked(object sender, RoutedEventArgs e)
        {
            ChkLockIco.Fill = FindResource("IcoUnLock") as Brush;
        }

        private void TxtDeckName_LostFocus(object sender, RoutedEventArgs e)
        {
            int currentNumber = (int)CmbDeck.SelectedValue;
            DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == currentNumber && r.Type == calcType.ToString());
            if (row != null)
            {
                row.Name = TxtDeckName.Text;
                deckInfo.AcceptChanges();
                deckInfo.WriteXml(Utility.GetFilePath("deckinfo.xml"));
            }
        }

        private void TxtDeckLimitedCost_LostFocus(object sender, RoutedEventArgs e)
        {
            int currentNumber = (int)CmbDeck.SelectedValue;
            DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == currentNumber && r.Type == calcType.ToString());
            if (row != null)
            {
                int cost;
                if(!int.TryParse(TxtDeckLimitedCost.Text, out cost))
                {
                    //入力エラーの場合は100に設定しなおす
                    TxtDeckLimitedCost.Text="100";
                    cost=100;
                }

                row.LimitedCost = cost;
                //コスト情報更新
                costMax = cost;
                SetCostInfo();

                deckInfo.AcceptChanges();
                deckInfo.WriteXml(Utility.GetFilePath("deckinfo.xml"));

            }
        }

        /// <summary>
        /// Deckコスト制限チェックボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChkDeckCostLimited_Click(object sender, RoutedEventArgs e)
        {
            int currentNumber = (int)CmbDeck.SelectedValue;
            DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == currentNumber && r.Type == calcType.ToString());
            if (row != null)
            {
                row.IsCostLimited = ChkDeckCostLimited.IsChecked ?? false;
                //コスト制限
                if (row.IsCostLimited)
                {
                    costMax = row.LimitedCost;
                }
                else
                {
                    costMax = GetCostMax();
                }
                //コスト情報更新
                SetCostInfo();

                deckInfo.AcceptChanges();
                deckInfo.WriteXml(Utility.GetFilePath("deckinfo.xml"));
            }
        }

        private void ChkDeckCostLimited_Checked(object sender, RoutedEventArgs e)
        {
            TxtDeckLimitedCost.IsEnabled = true;
        }

        private void ChkDeckCostLimited_Unchecked(object sender, RoutedEventArgs e)
        {
            TxtDeckLimitedCost.IsEnabled = false;
        }

        /// <summary>
        /// デッキ追加ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDeckAdd_Click(object sender, RoutedEventArgs e)
        {
            int maxNumber = deckInfo.DeckInfo.Where(r => r.Type == calcType.ToString()).Max(r => r.Number) + 1;
            int maxDisplayIndex = deckInfo.DeckInfo.Where(r => r.Type == calcType.ToString()).Max(r => r.DisplayIndex) + 1;

            DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.NewDeckInfoRow();
            row.Number = maxNumber;
            row.Name = calcType.ToString() + "デッキ" + maxNumber.ToString();
            row.Type = calcType.ToString();
            row.Lock = false;
            row.IsCostLimited = false;
            row.LimitedCost = 100;
            row.DisplayIndex = maxDisplayIndex;
            deckInfo.DeckInfo.AddDeckInfoRow(row);
            deckInfo.AcceptChanges();
            deckInfo.WriteXml(Utility.GetFilePath("deckinfo.xml"));
            //表示順再作成
            CreateDeckDisplayIndex(calcType);

            SaveDeckNumber(calcType, maxNumber);
            CmbDeck.SelectedValue = maxNumber;

            DialogWindow.Show(this, "[" + row.Name + "]を追加しました", DialogWindow.MessageType.Infomation);
        }

        /// <summary>
        /// デッキ削除ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnDeckDel_Click(object sender, RoutedEventArgs e)
        {
            if (CmbDeck.Items.Count == 1)
            {
                DialogWindow.Show(this, "削除できないデッキです", DialogWindow.MessageType.Error);
                return;
            }
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため削除できません", DialogWindow.MessageType.Error);
                return;
            }
            int currentNumber = (int)CmbDeck.SelectedValue;
            DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == currentNumber && r.Type == calcType.ToString());
            if (row != null)
            {
                if (!DialogWindow.Show(this, "[" + row.Name + "]を削除してもよろしいですか？", "確認", DialogWindow.MessageType.Confirm))
                {
                    return;
                }

                DialogWindow.Show(this, "[" + row.Name + "]を削除しました", DialogWindow.MessageType.Infomation);
                string deckFileName = GetDeckNumberName(calcType, currentNumber);
                if (File.Exists(deckFileName))
                {
                    File.Delete(deckFileName);
                }
                deckInfo.DeckInfo.RemoveDeckInfoRow(row);
                deckInfo.AcceptChanges();

                //表示順更新
                int displayIndex = 1;
                var newList = deckInfo.DeckInfo.Where(r => r.Type == calcType.ToString()).OrderBy(r => r.DisplayIndex).ToList();
                foreach (var newRow in newList)
                {
                    newRow.DisplayIndex = displayIndex++;
                }

                //Deck表示順再作成
                CreateDeckDisplayIndex(calcType);

                deckInfo.WriteXml(Utility.GetFilePath("deckinfo.xml"));
                CmbDeck.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// デッキ選択変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbDeck_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                if (e.RemovedItems.Count > 0)
                {
                    int currentNumber = ((DsDeckInfo.DeckInfoRow)((DataRowView)e.RemovedItems[0]).Row).Number;

                    SaveDeckNumber(calcType, currentNumber);
                }

                int loadNumber = ((DsDeckInfo.DeckInfoRow)((DataRowView)e.AddedItems[0]).Row).Number;
                LoadDeckNumber(calcType, loadNumber);
                editCalcNumber[calcType] = loadNumber;
            }
        }

        /// <summary>
        /// Deck表示順変更
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmbDeckDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent)
            {
                int currentNumber = (int)CmbDeck.SelectedValue;
                int oldDisplayIndex = (int)e.RemovedItems[0];
                int newDisplayIndex = (int)e.AddedItems[0];
                var displayIndexList = Enumerable.Range(1, deckInfo.DeckInfo.Where(r => r.Type == calcType.ToString()).Max(r => r.DisplayIndex)).ToList();
                displayIndexList.Remove(oldDisplayIndex);
                displayIndexList.Insert(newDisplayIndex-1, oldDisplayIndex);
                //全ての表示順を振り直し
                foreach(DsDeckInfo.DeckInfoRow row in deckInfo.DeckInfo.Where(r=> r.Type == calcType.ToString()))
                {
                    row.DisplayIndex = displayIndexList.IndexOf(row.DisplayIndex) + 1;
                }
                deckInfo.AcceptChanges();
                deckInfo.WriteXml(Utility.GetFilePath("deckinfo.xml"));
            }
        }
        #endregion

        #region デッキ詳細
        private void BtnDeckDetail_Click(object sender, RoutedEventArgs e)
        {
            PopShowDetail.IsOpen = !PopShowDetail.IsOpen;

            if (PopShowDetail.IsOpen)
            {
                int subMax = dsSubSelect.Card.Count();
                if (subMax + 5 > GrdShowDetailData.RowDefinitions.Count)
                {
                    int addNum = (subMax + 5) - GrdShowDetailData.RowDefinitions.Count;
                    for (int i = 0; i < addNum; i++)
                    {
                        GrdShowDetailData.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(30) });
                    }
                }

                CreateShowDetailRowHeader();

                LblDeckDetailColumnAttr.Content = dsSetting.User[0].Attribute;
                switch (dsSetting.User[0].Attribute)
                {
                    case ATTR_POP: BdrDeckDetailColumnAttr.Background = FindResource("PopBackground") as Brush; break;
                    case ATTR_COOL: BdrDeckDetailColumnAttr.Background = FindResource("CoolBackground") as Brush; break;
                    case ATTR_SWEET: BdrDeckDetailColumnAttr.Background = FindResource("SweetBackground") as Brush; break;
                }

                LblDeckDetailColumnClub.Text = dsSetting.User[0].Club;
                LblDeckDetailColumnRole.Content = dsSetting.User[0].Role;

                List<string> mainIDs = dsMainSelect.Card.Select(r => r.ID).ToList();
                List<string> subIDs = dsSubSelect.Card.Select(r => r.ID).ToList();

                List<SelectionBonusInfo> selectionBonusInfo;
                BonusInfo bonusInfo = new BonusInfo();
                Dictionary<string, PowerInfo> powerList = new Dictionary<string, PowerInfo>();
                if (calcType == CalcType.攻援)
                {
                    CreateAtkSelectionBonusInfo();
                    currentPower = CalcTotalAtk(mainIDs, subIDs, out powerList, out bonusInfo, out selectionBonusInfo);
                    ClmEventBonusHeader.Width = new GridLength(0);
                    ClmEventBonus.Width = new GridLength(0);
                }
                else if (calcType == CalcType.守援)
                {
                    CreateDefSelectionBonusInfo();
                    currentPower = CalcTotalDef(mainIDs, subIDs, out powerList, out bonusInfo, out selectionBonusInfo);
                    ClmEventBonusHeader.Width = new GridLength(0);
                    ClmEventBonus.Width = new GridLength(0);
                }
                else if (calcType == CalcType.イベント)
                {
                    CreateAtkSelectionBonusInfo();
                    currentPower = CalcTotalEvent(mainIDs, subIDs, out powerList, out bonusInfo, out selectionBonusInfo);
                    ClmEventBonusHeader.Width = new GridLength(50);
                    ClmEventBonus.Width = new GridLength(50);
                }
                int selectionBonusTotal = (int)bonusInfo.Selection;

                int rowNum = 0;
                foreach (var selectRow in dsMainSelect.Card)
                {
                    bool isBottom = (rowNum == 4);
                    CreateShowDetailDataName(selectRow, rowNum, 1, false, isBottom);
                    CreateShowDetailDataCommon(selectRow.レア, rowNum, 2, false, isBottom);
                    CreateShowDetailDataCommon(selectRow.コスト.ToString(), rowNum, 3, false, isBottom);

                    string id = selectRow.ID;
                    CreateShowDetailData(selectionBonusTotal.ToString(), rowNum, 4, false, isBottom);
                    CreateShowDetailData(powerList[id].BonusSkill.ToString(), rowNum, 5, false, isBottom);
                    CreateShowDetailData(powerList[id].BonusAttr.ToString(), rowNum, 6, false, isBottom);
                    CreateShowDetailData(powerList[id].BonusClub.ToString(), rowNum, 7, false, isBottom);
                    //備品
                    switch (selectRow.属性)
                    {
                        case ATTR_POP:
                            CreateShowDetailData(powerList[id].BonusItem.ToString(), rowNum, 8, false, isBottom);
                            CreateShowDetailData("0", rowNum, 9, false, isBottom);
                            CreateShowDetailData("0", rowNum, 10, false, isBottom);
                            break;
                        case ATTR_COOL:
                            CreateShowDetailData("0", rowNum, 8, false, isBottom);
                            CreateShowDetailData(powerList[id].BonusItem.ToString(), rowNum, 9, false, isBottom);
                            CreateShowDetailData("0", rowNum, 10, false, isBottom);
                            break;
                        case ATTR_SWEET:
                            CreateShowDetailData("0", rowNum, 8, false, isBottom);
                            CreateShowDetailData("0", rowNum, 9, false, isBottom);
                            CreateShowDetailData(powerList[id].BonusItem.ToString(), rowNum, 10, false, isBottom);
                            break;
                    }

                    CreateShowDetailData(powerList[id].BonusRole.ToString(), rowNum, 11, false, isBottom);
                    CreateShowDetailData(powerList[id].BonusEtc.ToString(), rowNum, 12, false, isBottom);
                    int bonusTotal = selectionBonusTotal +
                        powerList[id].BonusEtc +
                        powerList[id].BonusAttr +
                        powerList[id].BonusClub +
                        powerList[id].BonusItem +
                        powerList[id].BonusRole +
                        powerList[id].BonusSkill;
                    CreateShowDetailDataCommon(bonusTotal.ToString() + "%", rowNum, 13, false, isBottom);

                    CreateShowDetailDataCommon(selectRow.応援値.ToString(), rowNum, 14, false, isBottom);
                    CreateShowDetailDataCommon(dsSetting.User[0].AtkCost.ToString(), rowNum, 15, false, isBottom);
                    CreateShowDetailDataCommon(selectRow.発揮値.ToString(), rowNum, 16, true, isBottom);

                    rowNum++;
                }
                if (rowNum < 5)
                {
                    rowNum = 5;
                }
                int maxNum = 5 + dsSubSelect.Card.Count;
                foreach (var selectRow in dsSubSelect.Card)
                {
                    bool isBottom = (rowNum == maxNum);
                    CreateShowDetailDataName(selectRow, rowNum, 1, false, isBottom);
                    CreateShowDetailDataCommon(selectRow.レア, rowNum, 2, false, isBottom);
                    CreateShowDetailDataCommon(selectRow.コスト.ToString(), rowNum, 3, false, isBottom);

                    string id = selectRow.ID;
                    CreateShowDetailData(selectionBonusTotal.ToString(), rowNum, 4, true, isBottom);
                    //CreateShowDetailData(powerList[id].BonusSkill.ToString(), rowNum, 5, isBottom);
                    //CreateShowDetailData(powerList[id].BonusAttr.ToString(), rowNum, 6, isBottom);
                    //CreateShowDetailData(powerList[id].BonusClub.ToString(), rowNum, 7, isBottom);
                    ////備品
                    //if (powerList[id].BonusItem > 0)
                    //{
                    //    switch (selectRow.属性)
                    //    {
                    //        case ATTR_POP: CreateShowDetailData(powerList[id].BonusItem.ToString(), rowNum, 8, isBottom); break;
                    //        case ATTR_COOL: CreateShowDetailData(powerList[id].BonusItem.ToString(), rowNum, 9, isBottom); break;
                    //        case ATTR_SWEET: CreateShowDetailData(powerList[id].BonusItem.ToString(), rowNum, 10, isBottom); break;
                    //    }
                    //}

                    //CreateShowDetailData(powerList[id].BonusRole.ToString(), rowNum, 11, isBottom);
                    CreateShowDetailData(powerList[id].BonusEtc.ToString(), rowNum, 12, false, isBottom);
                    int bonusTotal = selectionBonusTotal +
                        powerList[id].BonusEtc +
                        powerList[id].BonusAttr +
                        powerList[id].BonusClub +
                        powerList[id].BonusItem +
                        powerList[id].BonusRole +
                        powerList[id].BonusSkill;
                    CreateShowDetailDataCommon(bonusTotal.ToString() + "%", rowNum, 13, false, isBottom);

                    CreateShowDetailDataCommon(selectRow.応援値.ToString(), rowNum, 14, false, isBottom);
                    CreateShowDetailDataCommon((dsSetting.User[0].AtkCost / 2).ToString(), rowNum, 15, false, isBottom);
                    CreateShowDetailDataCommon(selectRow.発揮値.ToString(), rowNum, 16, true, isBottom);

                    rowNum++;
                }

                BtnDeckDetail.Style = FindResource("BlueButton") as Style;

            }
            else
            {
                GrdShowDetailData.Children.Clear();
                BtnDeckDetail.Style = FindResource(typeof(Button)) as Style;
            }
        }

        private void CreateShowDetailRowHeader()
        {
            Border bdr = new Border();
            bdr.Background = new SolidColorBrush(Color.FromArgb(102, 68, 0, 238));
            bdr.BorderBrush = new SolidColorBrush(Color.FromArgb(204, 192, 192, 192));
            bdr.BorderThickness = new Thickness(2, 2, 2, 2);
            Grid.SetColumn(bdr, 0);
            Grid.SetRowSpan(bdr, 5);
            GrdShowDetailData.Children.Add(bdr);

            Label lbl = new Label();
            lbl.Content = "主センバツ";
            lbl.Style = FindResource("LblShowDetailRowHeader") as Style;
            Grid.SetColumn(lbl, 0);
            Grid.SetRowSpan(lbl, 5);
            GrdShowDetailData.Children.Add(lbl);

            Border subBdr = new Border();
            subBdr.Background = new SolidColorBrush(Color.FromArgb(102, 8, 238, 68));
            subBdr.BorderBrush = new SolidColorBrush(Color.FromArgb(204, 192, 192, 192));
            subBdr.BorderThickness = new Thickness(2, 0, 2, 2);
            Grid.SetColumn(subBdr, 0);
            Grid.SetRow(subBdr, 5);
            Grid.SetRowSpan(subBdr, 50);
            GrdShowDetailData.Children.Add(subBdr);

            Label subLbl = new Label();
            subLbl.Content = "副センバツ";
            subLbl.Style = FindResource("LblShowDetailRowHeader") as Style;
            Grid.SetColumn(subBdr, 0);
            Grid.SetRow(subLbl, 5);
            Grid.SetRowSpan(subLbl, 50);
            GrdShowDetailData.Children.Add(subLbl);
        }

        private void CreateShowDetailDataName(DsSelectCard.CardRow selectRow, int row, int column, bool isEnd, bool isBottom)
        {
            Border bdr = new Border();
            bdr.Style = FindResource("BdrShowDetailDataCommon") as Style;
            double rightThickness = isEnd ? 2 : 0;
            double bottomThickness = isBottom ? 2 : 1;
            bdr.BorderThickness = new Thickness(2, 0, rightThickness, bottomThickness);

            switch (selectRow.属性)
            {
                case ATTR_POP: bdr.Background = FindResource("PopBackground") as Brush; break;
                case ATTR_COOL: bdr.Background = FindResource("CoolBackground") as Brush; break;
                case ATTR_SWEET: bdr.Background = FindResource("SweetBackground") as Brush; break;
            }

            Grid.SetColumn(bdr, column);
            Grid.SetRow(bdr, row);
            GrdShowDetailData.Children.Add(bdr);

            TextBlock txt = new TextBlock();
            txt.FontSize = 9;
            txt.Text = selectRow.名前;
            txt.ToolTip = selectRow.名前;
            txt.Margin = new Thickness(4);
            txt.TextTrimming = TextTrimming.CharacterEllipsis;
            txt.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            txt.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            Grid.SetColumn(txt, column);
            Grid.SetRow(txt, row);
            GrdShowDetailData.Children.Add(txt);
        }

        private void CreateShowDetailDataCommon(string value, int row, int column, bool isEnd, bool isBottom)
        {
            Border bdr = new Border();
            bdr.Style = FindResource("BdrShowDetailDataCommon") as Style;
            double bottomThickness = isBottom ? 2 : 1;
            double rightThickness = isEnd ? 2 : 0;
            bdr.BorderThickness = new Thickness(2, 0, rightThickness, bottomThickness);
            Grid.SetColumn(bdr, column);
            Grid.SetRow(bdr, row);
            GrdShowDetailData.Children.Add(bdr);


            Label lbl = new Label();
            lbl.Content = value;
            if (column >= 12)
            {
                lbl.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
            }
            else
            {
                lbl.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
            }
            lbl.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            Grid.SetColumn(lbl, column);
            Grid.SetRow(lbl, row);
            GrdShowDetailData.Children.Add(lbl);
        }

        private void CreateShowDetailData(string value, int row, int column, bool isEnd, bool isBottom)
        {
            Border bdr = new Border();
            bdr.Style = FindResource("BdrShowDetailData") as Style;
            double bottomThickness = isBottom ? 2 : 1;
            bdr.BorderThickness = new Thickness(2, 0, 0, bottomThickness);
            Grid.SetColumn(bdr, column);
            Grid.SetRow(bdr, row);
            GrdShowDetailData.Children.Add(bdr);

            if (isEnd)
            {
                Border ebdr = new Border();
                ebdr.Style = FindResource("BdrShowDetailData") as Style;
                ebdr.Background = Brushes.Transparent;
                ebdr.BorderThickness = new Thickness(2, 0, 0, 0);
                Grid.SetColumn(ebdr, column + 1);
                Grid.SetRow(ebdr, row);
                GrdShowDetailData.Children.Add(ebdr);
            }

            if (value != "0")
            {
                Label lbl = new Label();
                lbl.Content = value + "%";
                lbl.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                lbl.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                Grid.SetColumn(lbl, column);
                Grid.SetRow(lbl, row);
                GrdShowDetailData.Children.Add(lbl);
            }
        }
        #endregion

        #region デッキ表示
        private void BtnShowDeck_Click_1(object sender, RoutedEventArgs e)
        {
            PopShowDeck.IsOpen = !PopShowDeck.IsOpen;

            if (PopShowDeck.IsOpen)
            {
                foreach (DsSelectCard.CardRow selectCard in dsMainSelect.Card)
                {
                    DsDispCard.DispCardRow dispRow = dsDispCard.DispCard.FirstOrDefault(r => r.ID == selectCard.ID);
                    UserCardPanel panel = new UserCardPanel();
                    panel.ClipToBounds = false;

                    panel.SetCardId(selectCard.ID, dispRow, selectCard);
                    SPnlMainDeck.Children.Add(panel);
                }
                foreach (DsSelectCard.CardRow selectCard in dsSubSelect.Card)
                {
                    DsDispCard.DispCardRow dispRow = dsDispCard.DispCard.FirstOrDefault(r => r.ID == selectCard.ID);
                    UserCardPanel panel = new UserCardPanel();
                    panel.SetCardId(selectCard.ID, dispRow, selectCard);
                    panel.ClipToBounds = false;
                    WPnlSubDeck.Children.Add(panel);
                }
                BtnShowDeck.Style = FindResource("BlueButton") as Style;
            }
            else
            {
                SPnlMainDeck.Children.Clear();
                WPnlSubDeck.Children.Clear();
                BtnShowDeck.Style = FindResource(typeof(Button)) as Style;

            }
        }
        #endregion

        #region デッキ比較
        private void BtnCompareDeck_Click(object sender, RoutedEventArgs e)
        {
            PopShowCompare.IsOpen = !PopShowCompare.IsOpen;
            if (PopShowCompare.IsOpen)
            {
                CmbCmpDeck.DisplayMemberPath = "Name";
                CmbCmpDeck.SelectedValuePath = "Number";
                CmbCmpDeck.ItemsSource = deckInfo.DeckInfo.Where(r => r.Type == calcType.ToString()).OrderBy(r => r.DisplayIndex).AsDataView();
                CmbCmpDeck.SelectedIndex = 0;

                switch (calcType)
                {
                    case CalcType.攻援: LblCmpTotal.Foreground = FindResource("AtkForeground") as Brush; break;
                    case CalcType.守援: LblCmpTotal.Foreground = FindResource("DefForeground") as Brush; break;
                    case CalcType.イベント: LblCmpTotal.Foreground = FindResource("EventForeground") as Brush; break;
                }

                SetDataGridColumn(DgCmpDeckMain, DgDeckMain.Name, calcType);
                SetDataGridColumn(DgCmpDeckSub, DgDeckSub.Name, calcType);

                BtnCompareDeck.Style = FindResource("BlueButton") as Style;
            }
            else
            {
                DgCmpDeckMain.ItemsSource = null;
                DgCmpDeckSub.ItemsSource = null;
                BtnCompareDeck.Style = FindResource(typeof(Button)) as Style;
            }
        }

        private void CmbCmpDeck_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                int loadNumber = ((DsDeckInfo.DeckInfoRow)((DataRowView)e.AddedItems[0]).Row).Number;
                LoadCmpDeckNumber(calcType, loadNumber);
            }
        }

        private void ReCalcCmp(DsSelectCard dsCmpMain, DsSelectCard dsCmpSub)
        {
            List<string> mainIDs = dsCmpMain.Card.Select(r => r.ID).ToList();
            List<string> subIDs = dsCmpSub.Card.Select(r => r.ID).ToList();

            List<SelectionBonusInfo> selectionBonusInfo;
            BonusInfo bonusInfo;
            if (calcType == CalcType.攻援)
            {
                //CalcTotalAtkBase(mainIDs, subIDs, out bonusList, out selectionBonusInfo, out selectionBonusTotal, out skills, out role, out items, out ownKind);
                //発揮値の書き換え
                CreateAtkSelectionBonusInfo();
                Dictionary<string, PowerInfo> atkList = new Dictionary<string, PowerInfo>();
                currentPower = CalcTotalAtk(mainIDs, subIDs, out atkList, out bonusInfo, out selectionBonusInfo);
                LblCmpTotal.Content = currentPower.ToString();

                foreach (DsSelectCard.CardRow row in dsCmpMain.Card)
                {
                    row.発揮値 = atkList[row.ID].Power;
                    row.センバツ値 = atkList[row.ID].SelectionPower;
                }
                foreach (DsSelectCard.CardRow row in dsCmpSub.Card)
                {
                    row.発揮値 = atkList[row.ID].Power;
                    row.センバツ値 = atkList[row.ID].SelectionPower;
                }
            }
            else if (calcType == CalcType.守援)
            {
                //CalcTotalDefBase(mainIDs, subIDs, out bonusList, out selectionBonusInfo, out selectionBonusTotal, out skills, out role, out items, out ownKind);
                CreateDefSelectionBonusInfo();
                //発揮値の書き換え
                Dictionary<string, PowerInfo> defList = new Dictionary<string, PowerInfo>();
                currentPower = CalcTotalDef(mainIDs, subIDs, out defList, out bonusInfo, out selectionBonusInfo);
                LblCmpTotal.Content = currentPower.ToString();
                foreach (DsSelectCard.CardRow row in dsCmpMain.Card)
                {
                    row.発揮値 = defList[row.ID].Power;
                    row.センバツ値 = defList[row.ID].SelectionPower;
                }
                foreach (DsSelectCard.CardRow row in dsCmpSub.Card)
                {
                    row.発揮値 = defList[row.ID].Power;
                    row.センバツ値 = defList[row.ID].SelectionPower;
                }
            }
            else
            {
                //CalcTotalDefBase(mainIDs, subIDs, out bonusList, out selectionBonusInfo, out selectionBonusTotal, out skills, out role, out items, out ownKind);
                //発揮値の書き換え
                CreateAtkSelectionBonusInfo();

                Dictionary<string, PowerInfo> atkList = new Dictionary<string, PowerInfo>();
                currentPower = CalcTotalEvent(mainIDs, subIDs, out atkList, out bonusInfo, out selectionBonusInfo);
                LblCmpTotal.Content = currentPower.ToString();
                foreach (DsSelectCard.CardRow row in dsCmpMain.Card)
                {
                    row.発揮値 = atkList[row.ID].Power;
                    row.センバツ値 = atkList[row.ID].SelectionPower;
                }
                foreach (DsSelectCard.CardRow row in dsCmpSub.Card)
                {
                    row.発揮値 = atkList[row.ID].Power;
                    row.センバツ値 = atkList[row.ID].SelectionPower;
                }
            }
        }

        private void LoadCmpDeckNumber(CalcType calc, int loadNumber)
        {
            DsDeckInfo.DeckInfoRow loadRow = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == loadNumber && r.Type == calcType.ToString());
            DsUserDeck loadDeck = new DsUserDeck();
            string loadFileName = GetDeckNumberName(calc, loadNumber);
            if (File.Exists(loadFileName))
            {
                loadDeck.ReadXml(loadFileName);
            }
            DsSelectCard dsCmpMain = new DsSelectCard();
            DsSelectCard dsCmpSub = new DsSelectCard();
            LoadCmpDeck(loadDeck, dsCmpMain, dsCmpSub);

            ReCalcCmp(dsCmpMain, dsCmpSub);

            DgCmpDeckMain.ItemsSource = dsCmpMain.Card.AsDataView();
            DgCmpDeckSub.ItemsSource = dsCmpSub.Card.AsDataView();
            LblCmpCost.Content = loadRow.Cost;
            LblCmpTotal.Content = loadRow.Power;
        }

        #endregion

        #endregion

        #region Data表示用
        /// <summary>
        /// レア毎の最大Lv取得
        /// </summary>
        /// <param name="rare"></param>
        /// <returns></returns>
        private int GetMaxLv(string rare)
        {
            switch (rare)
            {
                case "N": return 20;
                case "HN": return 30;
                case "R": return 40;
                case "HR": return 50;
                case "SR": return 60;
                case "SSR": return 70;
                case "LG": return 80;
            }
            return 1;
        }


        private string GetDispName(DsCards.CardsRow cardRow)
        {
            return (string.IsNullOrEmpty(cardRow.種別) ? string.Empty : ("[" + cardRow.種別 + "] ")) + cardRow.名前 + (cardRow.進展 == 2 ? "+" : string.Empty);
        }

        private string GetSortName(DsCards.CardsRow cardRow)
        {
            return cardRow.名前 + (cardRow.進展 == 2 ? "+" : " ") + (string.IsNullOrEmpty(cardRow.種別) ? string.Empty : ("[" + cardRow.種別 + "] "));
        }

        private int GetAtkBonus(DsCards.CardsRow cardRow)
        {
            int baseAtk = cardRow.攻援 + (int)Math.Ceiling((double)cardRow.攻援 * (cardRow.ボーナス有無 ? (double)cardRow.ボーナス : 0d) / 100d);
            int bonusAtk = baseAtk + (int)Math.Ceiling(baseAtk * cardRow.スペシャル);
            return baseAtk;
        }

        private int GetDefBonus(DsCards.CardsRow cardRow)
        {
            int baseDef = cardRow.守援 + (int)Math.Ceiling((double)cardRow.守援 * (cardRow.ボーナス有無 ? (double)cardRow.ボーナス : 0d) / 100d);
            int bonusDef = baseDef + (int)Math.Ceiling(baseDef * cardRow.スペシャル);
            return baseDef;
        }

        /// <summary>
        /// 表示用レコード作成
        /// </summary>
        /// <param name="cardRow"></param>
        private void CreateDispCardRow(DsCards.CardsRow cardRow)
        {
            DsDispCard.DispCardRow dispRow = dsDispCard.DispCard.NewDispCardRow();
            DsGirls.GirlsRow girlRow = girlsList[cardRow.名前];

            dispRow.ID = cardRow.ID;
            dispRow.なまえ = girlRow.なまえ;
            SetDispCardRow(cardRow, dispRow, girlRow);

            Dispatcher.BeginInvoke((Action<string>)((id) =>
                {
                    if (cardsList.ContainsKey(id))
                    {
                        DsCards.CardsRow iCardRow = cardsList[id];
                        DsDispCard.DispCardRow iDispRow = dsDispCard.DispCard.FirstOrDefault(r => r.ID == id);
                        Size scaleSize = new Size(240, 240);
                        iDispRow.画像 = Images.LoadImage(iCardRow.画像, ref  scaleSize);
                        DsDeckCard.DeckCardRow deckCardRow = dsDeckCard.DeckCard.FirstOrDefault(r => r.ID == id);
                        if (deckCardRow != null)
                        {
                            deckCardRow.画像 = iDispRow.画像;
                        }
                        DsSelectCard.CardRow selectCardRow = dsMainSelect.Card.FirstOrDefault(r => r.ID == id);
                        if (selectCardRow != null)
                        {
                            selectCardRow.画像 = iDispRow.画像;
                        }
                        selectCardRow = dsSubSelect.Card.FirstOrDefault(r => r.ID == id);
                        if (selectCardRow != null)
                        {
                            selectCardRow.画像 = iDispRow.画像;
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Background, cardRow.ID);
            dsDispCard.DispCard.AddDispCardRow(dispRow);
        }

        /// <summary>
        /// 表示用に設定する
        /// </summary>
        /// <param name="cardRow"></param>
        /// <param name="dispRow"></param>
        /// <param name="girlRow"></param>
        private void SetDispCardRow(DsCards.CardsRow cardRow, DsDispCard.DispCardRow dispRow, DsGirls.GirlsRow girlRow)
        {
            dispRow.名前 = GetDispName(cardRow);
            dispRow.姓名 = GetSortName(cardRow);
            dispRow.Name = girlRow.Name;
            dispRow.属性 = girlRow.属性;
            dispRow.進展 = cardRow.進展;
            dispRow.好感度 = cardRow.好感度;
            dispRow.コスト = cardRow.コスト;
            dispRow.Lv = cardRow.Lv;
            dispRow.成長 = cardRow.成長;
            dispRow.レア = cardRow.レア;
            dispRow.攻援 = cardRow.攻援;
            dispRow.攻コスパ = cardRow.攻援 / cardRow.コスト;
            dispRow.攻ボーナス = GetAtkBonus(cardRow);
            dispRow.守援 = cardRow.守援;
            dispRow.守コスパ = cardRow.守援 / cardRow.コスト;
            dispRow.守ボーナス = GetDefBonus(cardRow);
            dispRow.総攻守 = cardRow.攻援 + cardRow.守援;
            dispRow.スキル = GetSkillName(cardRow);
            dispRow.攻スキル = GetAtkSkillPower(cardRow);
            dispRow.守スキル = GetDefSkillPower(cardRow);
            dispRow.スペシャル = cardRow.スペシャル;
            dispRow.ボーナス = cardRow.ボーナス;
            dispRow.ボーナス有無 = cardRow.ボーナス有無;
            dispRow.攻選抜ボーナス1 = girlRow.攻援1;
            dispRow.攻選抜ボーナス2 = girlRow.攻援2;
            dispRow.攻選抜ボーナス3 = girlRow.攻援3;
            dispRow.守選抜ボーナス1 = girlRow.守援1;
            dispRow.守選抜ボーナス2 = girlRow.守援2;
            dispRow.守選抜ボーナス3 = girlRow.守援3;
            dispRow.部活 = girlRow.部室;
            dispRow.ダミー = cardRow.ダミー;
            dispRow.表示順 = cardRow.表示順;
            dispRow.フリー1 = cardRow.フリー1;
            dispRow.フリー2 = cardRow.フリー2;
            dispRow.フリー3 = cardRow.フリー3;
        }

        private void CreateDeckCardRow(DsCards.CardsRow cardRow)
        {
            DsDeckCard.DeckCardRow deckRow = dsDeckCard.DeckCard.NewDeckCardRow();
            DsDispCard.DispCardRow dispRow = dsDispCard.DispCard.FirstOrDefault(r => r.ID == cardRow.ID);
            DsGirls.GirlsRow girlRow = girlsList[cardRow.名前];

            deckRow.ID = cardRow.ID;
            deckRow.名前 = GetDispName(cardRow);
            deckRow.姓名 = GetSortName(cardRow);
            deckRow.なまえ = girlRow.なまえ;
            deckRow.Name = girlRow.Name;
            deckRow.Lv = cardRow.Lv;
            deckRow.成長 = cardRow.成長;
            deckRow.進展 = cardRow.進展;
            deckRow.好感度 = cardRow.好感度;
            deckRow.属性 = girlRow.属性;
            deckRow.コスト = cardRow.コスト;
            deckRow.レア = cardRow.レア;
            deckRow.攻援 = cardRow.攻援;
            deckRow.攻コスパ = (int)(cardRow.攻援 / cardRow.コスト);
            deckRow.攻ボーナス = GetAtkBonus(cardRow);
            deckRow.守援 = cardRow.守援;
            deckRow.守コスパ = (int)(cardRow.守援 / cardRow.コスト);
            deckRow.守ボーナス = GetDefBonus(cardRow);
            deckRow.スキル = GetSkillName(cardRow);
            deckRow.攻スキル = GetAtkSkillPower(cardRow);
            deckRow.守スキル = GetDefSkillPower(cardRow);
            deckRow.スペシャル = cardRow.スペシャル;
            deckRow.ボーナス = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
            deckRow.部活 = girlRow.部室;
            deckRow.攻選抜ボーナス1 = girlRow.攻援1;
            deckRow.攻選抜ボーナス2 = girlRow.攻援2;
            deckRow.攻選抜ボーナス3 = girlRow.攻援3;
            deckRow.守選抜ボーナス1 = girlRow.守援1;
            deckRow.守選抜ボーナス2 = girlRow.守援2;
            deckRow.守選抜ボーナス3 = girlRow.守援3;
            deckRow.選抜選択 = IsSelectionBonusSelection(deckRow);
            deckRow.ダミー = cardRow.ダミー;
            deckRow.表示順 = cardRow.表示順;
            deckRow.フリー1 = cardRow.フリー1;
            deckRow.フリー2 = cardRow.フリー2;
            deckRow.フリー3 = cardRow.フリー3;
            deckRow.画像 = dispRow.画像;

            dsDeckCard.DeckCard.AddDeckCardRow(deckRow);
        }

        #endregion

        #region 計算
        /// <summary>
        /// 攻選抜ボーナス情報作成
        /// </summary>
        private void CreateAtkSelectionBonusInfo()
        {
            atkSelectionBonusInfo.Clear();
            foreach (string b in AtkBounus)
            {
                SelectionBonusInfo info = new SelectionBonusInfo();
                info.Name = b;
                atkSelectionBonusInfo.Add(b, info);
            }
            foreach (var cardRow in dsCards.Cards)
            {
                var bonusList = girlsSelectionBonusList[cardRow.名前];
                foreach (string bonus in bonusList.AtkBonus)
                {
                    atkSelectionBonusInfo[bonus].Count++;
                }
            }
        }

        /// <summary>
        /// 守選抜ボーナス情報作成
        /// </summary>
        private void CreateDefSelectionBonusInfo()
        {
            defSelectionBonusInfo.Clear();
            foreach (string b in DefBounus)
            {
                SelectionBonusInfo info = new SelectionBonusInfo();
                info.Name = b;
                defSelectionBonusInfo.Add(b, info);
            }
            foreach (var cardRow in dsCards.Cards)
            {
                var bonusList = girlsSelectionBonusList[cardRow.名前];
                foreach (string bonus in bonusList.DefBonus)
                {
                    defSelectionBonusInfo[bonus].Count++;
                }
            }
        }


        /// <summary>
        /// 全計算
        /// </summary>
        private void ReCalcAll()
        {
            List<string> mainIDs = dsMainSelect.Card.Select(r => r.ID).ToList();
            List<string> subIDs = dsSubSelect.Card.Select(r => r.ID).ToList();

            List<SelectionBonusInfo> selectionBonusInfo;
            BonusInfo bonusInfo;
            if (calcType == CalcType.攻援)
            {
                //CalcTotalAtkBase(mainIDs, subIDs, out bonusList, out selectionBonusInfo, out selectionBonusTotal, out skills, out role, out items, out ownKind);
                //発揮値の書き換え
                CreateAtkSelectionBonusInfo();
                Dictionary<string, PowerInfo> atkList = new Dictionary<string, PowerInfo>();
                currentPower = CalcTotalAtk(mainIDs, subIDs, out atkList, out bonusInfo, out selectionBonusInfo);
                LblTotal.Content = currentPower.ToString();
                currentBasePower = atkList.Sum(p => p.Value.BasePower);
                LblTotalDisp.Content = currentBasePower.ToString();
                SetBonusLabel(bonusInfo);

                List<string> tmpMainIDs = new List<string>(mainIDs);
                List<string> tmpSubIDs = new List<string>(subIDs);

                foreach (DsSelectCard.CardRow row in dsMainSelect.Card)
                {
                    row.発揮値 = atkList[row.ID].Power;
                    row.センバツ値 = atkList[row.ID].SelectionPower;
                }
                foreach (DsSelectCard.CardRow row in dsSubSelect.Card)
                {
                    row.発揮値 = atkList[row.ID].Power;
                    row.センバツ値 = atkList[row.ID].SelectionPower;
                    //除外発揮値計算
                    string id = row.ID;
                    int tmpCurrentPower = 0;
                    Dictionary<string, PowerInfo> tmpAtkList = new Dictionary<string, PowerInfo>();
                    List<SelectionBonusInfo> tmpSelectionBonusInfo;
                    BonusInfo tmpBonusInfo;
                    tmpSubIDs.Remove(id);
                    tmpCurrentPower = CalcTotalAtk(tmpMainIDs, tmpSubIDs, out tmpAtkList, out tmpBonusInfo, out tmpSelectionBonusInfo);
                    row.除外総発揮値 = tmpCurrentPower;
                    tmpSubIDs.Add(id);
                }

                foreach (DsDeckCard.DeckCardRow row in dsDeckCard.DeckCard)
                {
                    string id = row.ID;
                    int tmpCurrentPower = 0;
                    Dictionary<string, PowerInfo> tmpAtkList = new Dictionary<string, PowerInfo>();
                    List<SelectionBonusInfo> tmpSelectionBonusInfo;
                    BonusInfo tmpBonusInfo;
                    if (mainIDs.Count >= 5)
                    {
                        row.主センバツ = 0;
                        row.主総発揮値 = 0;
                    }
                    else
                    {
                        tmpMainIDs.Add(id);
                        tmpCurrentPower = CalcTotalAtk(tmpMainIDs, tmpSubIDs, out tmpAtkList, out tmpBonusInfo, out tmpSelectionBonusInfo);
                        row.主センバツ = tmpAtkList[id].Power;
                        row.主総発揮値 = tmpCurrentPower;
                        tmpMainIDs.Remove(id);
                    }
                    if (subIDs.Count >= subMax)
                    {
                        row.副センバツ = 0;
                        row.副総発揮値 = 0;
                    }
                    else
                    {
                        tmpSubIDs.Add(id);
                        tmpCurrentPower = CalcTotalAtk(tmpMainIDs, tmpSubIDs, out tmpAtkList, out tmpBonusInfo, out tmpSelectionBonusInfo);
                        row.副センバツ = tmpAtkList[id].Power;
                        row.副総発揮値 = tmpCurrentPower;
                        tmpSubIDs.Remove(id);
                    }
                }

            }
            else if (calcType == CalcType.守援)
            {
                //CalcTotalDefBase(mainIDs, subIDs, out bonusList, out selectionBonusInfo, out selectionBonusTotal, out skills, out role, out items, out ownKind);
                CreateDefSelectionBonusInfo();
                //発揮値の書き換え
                Dictionary<string, PowerInfo> defList = new Dictionary<string, PowerInfo>();
                currentPower = CalcTotalDef(mainIDs, subIDs, out defList, out bonusInfo, out selectionBonusInfo);
                LblTotal.Content = currentPower.ToString();
                LblTotalDisp.Content = defList.Sum(p => p.Value.BasePower).ToString();
                SetBonusLabel(bonusInfo);
                foreach (DsSelectCard.CardRow row in dsMainSelect.Card)
                {
                    row.発揮値 = defList[row.ID].Power;
                    row.センバツ値 = defList[row.ID].SelectionPower;
                }
                foreach (DsSelectCard.CardRow row in dsSubSelect.Card)
                {
                    row.発揮値 = defList[row.ID].Power;
                    row.センバツ値 = defList[row.ID].SelectionPower;

                    //除外発揮値計算
                    List<string> tmpMainIDs = new List<string>(mainIDs);
                    List<string> tmpSubIDs = new List<string>(subIDs);
                    string id = row.ID;
                    int tmpCurrentPower = 0;
                    Dictionary<string, PowerInfo> tmpAtkList = new Dictionary<string, PowerInfo>();
                    List<SelectionBonusInfo> tmpSelectionBonusInfo;
                    BonusInfo tmpBonusInfo;
                    tmpSubIDs.Remove(id);
                    tmpCurrentPower = CalcTotalDef(tmpMainIDs, tmpSubIDs, out tmpAtkList, out tmpBonusInfo, out tmpSelectionBonusInfo);
                    row.除外総発揮値 = tmpCurrentPower;
                }
                foreach (DsDeckCard.DeckCardRow row in dsDeckCard.DeckCard)
                {
                    string id = row.ID;
                    int tmpCurrentPower = 0;
                    List<string> tmpMainIDs = new List<string>(mainIDs);
                    List<string> tmpSubIDs = new List<string>(subIDs);
                    Dictionary<string, PowerInfo> tmpDefList = new Dictionary<string, PowerInfo>();
                    List<SelectionBonusInfo> tmpSelectionBonusInfo;
                    BonusInfo tmpBonusInfo;
                    if (mainIDs.Count >= 5)
                    {
                        row.主センバツ = 0;
                        row.主総発揮値 = 0;
                    }
                    else
                    {
                        tmpMainIDs.Add(id);
                        tmpCurrentPower = CalcTotalDef(tmpMainIDs, tmpSubIDs, out tmpDefList, out tmpBonusInfo, out tmpSelectionBonusInfo);
                        row.主センバツ = tmpDefList[id].Power;
                        row.主総発揮値 = tmpCurrentPower;
                        tmpMainIDs.Remove(id);
                    }
                    if (subIDs.Count >= subMax)
                    {
                        row.副センバツ = 0;
                        row.副総発揮値 = 0;
                    }
                    else
                    {
                        tmpSubIDs.Add(id);
                        tmpCurrentPower = CalcTotalDef(tmpMainIDs, tmpSubIDs, out tmpDefList, out tmpBonusInfo, out tmpSelectionBonusInfo);
                        row.副センバツ = tmpDefList[id].Power;
                        row.副総発揮値 = tmpCurrentPower;
                    }
                }
            }
            else
            {
                //CalcTotalDefBase(mainIDs, subIDs, out bonusList, out selectionBonusInfo, out selectionBonusTotal, out skills, out role, out items, out ownKind);
                //発揮値の書き換え
                CreateAtkSelectionBonusInfo();

                Dictionary<string, PowerInfo> atkList = new Dictionary<string, PowerInfo>();
                currentPower = CalcTotalEvent(mainIDs, subIDs, out atkList, out bonusInfo, out selectionBonusInfo);
                LblTotal.Content = currentPower.ToString();
                double mainTotal = Math.Ceiling(atkList.Where(p => p.Value.IsMain).Sum(p => p.Value.BasePower) * (double)dsSetting.User[0].AtkCost / 100);
                double subTotal = Math.Ceiling(atkList.Where(p => !p.Value.IsMain).Sum(p => p.Value.BasePower) * (double)dsSetting.User[0].AtkCost / 200);
                LblTotalDisp.Content = Convert.ToInt32(mainTotal + subTotal).ToString();
                SetBonusLabel(bonusInfo);
                foreach (DsSelectCard.CardRow row in dsMainSelect.Card)
                {
                    row.発揮値 = atkList[row.ID].Power;
                    row.センバツ値 = atkList[row.ID].SelectionPower;
                }
                foreach (DsSelectCard.CardRow row in dsSubSelect.Card)
                {
                    row.発揮値 = atkList[row.ID].Power;
                    row.センバツ値 = atkList[row.ID].SelectionPower;

                    //除外発揮値計算
                    List<string> tmpMainIDs = new List<string>(mainIDs);
                    List<string> tmpSubIDs = new List<string>(subIDs);
                    string id = row.ID;
                    int tmpCurrentPower = 0;
                    Dictionary<string, PowerInfo> tmpAtkList = new Dictionary<string, PowerInfo>();
                    List<SelectionBonusInfo> tmpSelectionBonusInfo;
                    BonusInfo tmpBonusInfo;
                    tmpSubIDs.Remove(id);
                    tmpCurrentPower = CalcTotalEvent(tmpMainIDs, tmpSubIDs, out tmpAtkList, out tmpBonusInfo, out tmpSelectionBonusInfo);
                    row.除外総発揮値 = tmpCurrentPower;
                }

                foreach (DsDeckCard.DeckCardRow row in dsDeckCard.DeckCard)
                {
                    string id = row.ID;
                    int tmpCurrentPower = 0;
                    List<string> tmpMainIDs = new List<string>(mainIDs);
                    List<string> tmpSubIDs = new List<string>(subIDs);
                    Dictionary<string, PowerInfo> tmpAtkList = new Dictionary<string, PowerInfo>();
                    List<SelectionBonusInfo> tmpSelectionBonusInfo;
                    BonusInfo tmpBonusInfo;
                    if (mainIDs.Count >= 5)
                    {
                        row.主センバツ = 0;
                        row.主総発揮値 = 0;
                    }
                    else
                    {
                        tmpMainIDs.Add(id);
                        tmpCurrentPower = CalcTotalEvent(tmpMainIDs, tmpSubIDs, out tmpAtkList, out tmpBonusInfo, out tmpSelectionBonusInfo);
                        row.主センバツ = tmpAtkList[id].Power;
                        row.主総発揮値 = tmpCurrentPower;
                        tmpMainIDs.Remove(id);
                    }
                    if (subIDs.Count >= subMax)
                    {
                        row.副センバツ = 0;
                        row.副総発揮値 = 0;
                    }
                    else
                    {
                        tmpSubIDs.Add(id);
                        tmpCurrentPower = CalcTotalEvent(tmpMainIDs, tmpSubIDs, out tmpAtkList, out tmpBonusInfo, out tmpSelectionBonusInfo);
                        row.副センバツ = tmpAtkList[id].Power;
                        row.副総発揮値 = tmpCurrentPower;
                    }
                }
            }
            isEvent = false;
            LstBonus.DisplayMemberPath = "Display";
            LstBonus.SelectedValuePath = "Name";
            LstBonus.ItemsSource = selectionBonusInfo.OrderByDescending(r => r.UseCount).ThenByDescending(r => r.Count).ToList();

            LstPopSearchDeckBonus.DisplayMemberPath = "Display";
            LstPopSearchDeckBonus.SelectedValuePath = "Name";
            LstPopSearchDeckBonus.ItemsSource = selectionBonusInfo.OrderByDescending(r => r.UseCount).ThenByDescending(r => r.Count).ToList();
            isEvent = true;

            //コスト計算
            SetCostInfo();
        }

        private void SetCostInfo()
        {
            int cost = dsMainSelect.Card.Sum(r => r.コスト) + dsSubSelect.Card.Sum(r => r.コスト);
            currentCost = cost;
            if (IsAttack())
            {
                int remainCost = (costMax - currentCost);
                LblRemainCost.Content = remainCost.ToString();
                int costPer = (currentCost * 100 / costMax);
                if (costPer >= 100)
                {
                    costPer = 100;
                    BdrCostBar.BorderThickness = new Thickness(1);
                }
                else
                {
                    BdrCostBar.BorderThickness = new Thickness(1, 1, 0, 1);
                }
                BdrCostBar.Width = costPer;
                if (currentCost > costMax)
                {
                    LblTotalCost.Foreground = FindResource("OverForeground") as SolidColorBrush;
                    LblRemainCost.Foreground = FindResource("OverForeground") as SolidColorBrush;
                }
                else
                {
                    LblTotalCost.Foreground = FindResource("DefaultForeground") as SolidColorBrush;
                    if (remainCost < 10)
                    {
                        LblRemainCost.Foreground = FindResource("WarnForeground") as SolidColorBrush;
                    }
                    else
                    {
                        LblRemainCost.Foreground = FindResource("DefaultForeground") as SolidColorBrush;
                    }
                }
            }
            else
            {
                int remainCost = (costMax - currentCost);
                LblRemainCost.Content = remainCost.ToString();
                int costPer = (currentCost * 100 / costMax);
                if (costPer >= 100)
                {
                    costPer = 100;
                    BdrCostBar.BorderThickness = new Thickness(1);
                }
                else
                {
                    BdrCostBar.BorderThickness = new Thickness(1, 1, 0, 1);
                }
                BdrCostBar.Width = costPer;
                if (currentCost > costMax)
                {
                    LblTotalCost.Foreground = FindResource("OverForeground") as SolidColorBrush;
                    LblRemainCost.Foreground = FindResource("OverForeground") as SolidColorBrush;
                }
                else
                {
                    LblTotalCost.Foreground = FindResource("DefaultForeground") as SolidColorBrush;
                    if (remainCost < 10)
                    {
                        LblRemainCost.Foreground = FindResource("WarnForeground") as SolidColorBrush;
                    }
                    else
                    {
                        LblRemainCost.Foreground = FindResource("DefaultForeground") as SolidColorBrush;
                    }
                }
            }
            LblTotalCost.Content = cost;
            LblMaxCost.Content = "/" + costMax.ToString();
        }

        private void SetBonusLabel(BonusInfo bonusInfo)
        {
            //ボーナス計算
            LblTotalSelectionBonus.Content = bonusInfo.Selection.ToString();
            StringBuilder bonusStr = new StringBuilder();
            foreach (double b in bonusInfo.SelectionList)
            {
                bonusStr.Append(b.ToString());
                bonusStr.Append("+");
            }
            if (bonusStr.Length > 0)
            {
                bonusStr.Length--;
            }
            LblSelectionBonus.Content = bonusStr.ToString();
            double sweetBonus = bonusInfo.Role + bonusInfo.Items[ATTR_SWEET] + (bonusInfo.AttributeTarget == ATTR_SWEET ? bonusInfo.Attribute : 0);
            double sweetBonusSkill = 0;
            if (bonusInfo.Skill != null && !bonusInfo.Skill.isOwn && (bonusInfo.Skill.isAll || bonusInfo.Skill.Attr == ATTR_SWEET))
            {
                sweetBonusSkill = sweetBonus + bonusInfo.Skill.Power;
            }
            LblBonusSweet.Content = "S:" + sweetBonus.ToString() + (sweetBonusSkill > 0 ? "(" + sweetBonusSkill.ToString() + ")" : string.Empty);
            double coolBonus = bonusInfo.Role + bonusInfo.Items[ATTR_COOL] + (bonusInfo.AttributeTarget == ATTR_COOL ? bonusInfo.Attribute : 0);
            double coolBonusSkill = 0;
            if (bonusInfo.Skill != null && !bonusInfo.Skill.isOwn && (bonusInfo.Skill.isAll || bonusInfo.Skill.Attr == ATTR_COOL))
            {
                coolBonusSkill = coolBonus + bonusInfo.Skill.Power;
            }
            LblBonusCool.Content = "C:" + coolBonus.ToString() + (coolBonusSkill > 0 ? "(" + coolBonusSkill.ToString() + ")" : string.Empty);
            double popBonus = bonusInfo.Role + bonusInfo.Items[ATTR_POP] + (bonusInfo.AttributeTarget == ATTR_POP ? bonusInfo.Attribute : 0);
            double popBonusSkill = 0;
            if (bonusInfo.Skill != null && !bonusInfo.Skill.isOwn && (bonusInfo.Skill.isAll || bonusInfo.Skill.Attr == ATTR_POP))
            {
                popBonusSkill = popBonus + bonusInfo.Skill.Power;
            }
            LblBonusPop.Content = "P:" + popBonus.ToString() + (popBonusSkill > 0 ? "(" + popBonusSkill.ToString() + ")" : string.Empty);
        }
        #endregion

        private void SetSubInfo(int count)
        {
            LblSubInfo.Content = count.ToString();
            if (dsSubSelect.Card.Count > subMax)
            {
                LblSubInfo.Foreground = FindResource("OverForeground") as Brush;
            }
            else
            {
                LblSubInfo.Foreground = FindResource("DefaultForeground") as Brush;
            }
        }

        #region 攻援

        private int CalcTotalAtk(List<string> mainIDs, List<string> subIDs, out Dictionary<string, PowerInfo> atkList, out BonusInfo bonusInfo, out List<SelectionBonusInfo> selectionBonusInfo)
        {
            int total = 0;
            atkList = new Dictionary<string, PowerInfo>();

            Dictionary<string, int> bonusList;
            int selectionBonusTotal;
            List<int> selectionBonusPower;
            List<ActiveSkill> skills;
            int role;
            HashSet<string> items;
            string ownKind;
            CalcTotalAtkBase(mainIDs, subIDs, out bonusList, out selectionBonusInfo, out selectionBonusTotal, out selectionBonusPower, out skills, out role, out items, out ownKind);

            bonusInfo = new BonusInfo();
            bonusInfo.Attribute = sameAttributeBonus;
            bonusInfo.AttributeTarget = ownKind;
            bonusInfo.Items = new Dictionary<string, int>();
            foreach (string atr in new string[] { ATTR_POP, ATTR_COOL, ATTR_SWEET })
            {
                bonusInfo.Items.Add(atr, (items.Contains(atr) ? clubItemBonus : 0));
            }
            bonusInfo.Role = role;
            bonusInfo.Selection = selectionBonusTotal;
            bonusInfo.SelectionList = selectionBonusInfo.Where(b => b.UseCount > 0).OrderByDescending(b => b.UseCount).Take(5).Select(b => selectBonus[(b.UseCount > 5 ? 5 : b.UseCount) - 1]).ToList();
            bonusInfo.Skill = skills.Count > 0 ? skills[0] : null;
            //主選抜
            int mainTotal = 0;
            foreach (string id in mainIDs)
            {
                DsCards.CardsRow cardRow = cardsList[id];
                DsGirls.GirlsRow mainGilrsRow = girlsList[cardRow.名前];
                double etcBonus = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
                double mainBaseAtk = Convert.ToDouble(cardRow.攻援);
                double mainBonusAtk = mainBaseAtk + Math.Ceiling(mainBaseAtk * etcBonus / 100d);
                double mainAtk = mainBaseAtk;
                double mainSumAtk = mainAtk;
                double totalMainSkill = 0;
                skills.ForEach(s =>
                    {
                        if (s.isOwn)
                        {
                            //自分自身の場合
                            if (s.ID == id)
                            {
                                totalMainSkill += s.Power;
                            }
                        }
                        else
                        {
                            totalMainSkill += (s.isAll || s.Attr == mainGilrsRow.属性) ? s.Power : 0;
                        }
                    });
                //備品：2%
                double mainItem = items.Contains(mainGilrsRow.属性) ? clubItemBonus : 0;
                //同属性：5%
                double mainAttr = mainGilrsRow.属性 == ownKind ? sameAttributeBonus : 0;
                //同部室
                double mainClub = mainGilrsRow.部室 == dsSetting.User[0].Club ? clubSameBonus : 0;

                mainSumAtk = mainSumAtk
                    + Math.Ceiling(mainAtk * etcBonus / 100d)
                    + Math.Ceiling(mainAtk * totalMainSkill / 100d)
                    + Math.Ceiling(mainAtk * (role + mainItem) / 100d)
                    + Math.Ceiling(mainAtk * mainAttr / 100d)
                    + Math.Ceiling((mainAtk * mainClub / 100d));

                selectionBonusPower.ForEach(p =>
                    {
                        mainSumAtk += Math.Ceiling(mainAtk * p / 100d);
                    });

                atkList.Add(id, new PowerInfo()
                {
                    ID = id,
                    Power = Convert.ToInt32(mainSumAtk),
                    BasePower = Convert.ToInt32(mainAtk),
                    SelectionPower = Convert.ToInt32(mainBonusAtk),
                    BonusEtc = Convert.ToInt32(etcBonus),
                    BonusSkill = Convert.ToInt32(totalMainSkill),
                    BonusRole = Convert.ToInt32(role),
                    BonusClub = Convert.ToInt32(mainClub),
                    BonusAttr = Convert.ToInt32(mainAttr),
                    BonusItem = Convert.ToInt32(mainItem),
                });
                mainTotal += Convert.ToInt32(mainSumAtk);
            }

            //サブ
            int subTotal = 0;
            foreach (string id in subIDs)
            {
                DsCards.CardsRow cardRow = cardsList[id];
                double etcBonus = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
                double subOriginal = Convert.ToDouble(cardRow.攻援);
                double subBonusAtk = Math.Truncate(subOriginal * 0.8 + Math.Ceiling(subOriginal * 0.8 * etcBonus / 100d));
                double subAtk = Convert.ToDouble(cardRow.攻援);
                double subBaseAtk = Math.Truncate(subAtk * 0.8 * (1d + (0.8 * etcBonus / 100d)));
                double subSumAtk = subBaseAtk;

                selectionBonusPower.ForEach(p =>
                {
                    subSumAtk += Math.Ceiling(subAtk * p / 100d);
                });

                atkList.Add(id, new PowerInfo()
                {
                    ID = id,
                    Power = Convert.ToInt32(subSumAtk),
                    BasePower = Convert.ToInt32(cardRow.ボーナス有無 ? subBonusAtk : subBaseAtk),
                    SelectionPower = Convert.ToInt32(subBaseAtk),
                    BonusEtc = Convert.ToInt32(etcBonus),
                });
                subTotal += Convert.ToInt32(subSumAtk);
            }
            total = mainTotal + subTotal;
            return total;
        }

        private void CalcTotalAtkBase(List<string> mainIDs, List<string> subIDs, out Dictionary<string, int> bonusList, out List<SelectionBonusInfo> selectionBonusInfo, out int selectionBonusTotal, out List<int> selectionBonusPower, out List<ActiveSkill> skills, out int role, out HashSet<string> items, out string ownKind)
        {
            //選抜ボーナスの計算
            List<string> allIDs = new List<string>();
            allIDs.AddRange(mainIDs);
            allIDs.AddRange(subIDs);
            bonusList = new Dictionary<string, int>();
            selectionBonusPower = new List<int>();
            selectionBonusInfo = new List<SelectionBonusInfo>();
            Dictionary<string, SelectionBonusInfo> selectionBonusInfoDic = new Dictionary<string, SelectionBonusInfo>();
            foreach (var kvp in atkSelectionBonusInfo)
            {
                selectionBonusInfoDic.Add(kvp.Key, kvp.Value.Copy());
            }

            selectionBonusTotal = 0;
            skills = new List<ActiveSkill>();
            role = 0;
            items = new HashSet<string>();
            ownKind = string.Empty;
            HashSet<string> distinctGilrsBonus = new HashSet<string>();
            foreach (string id in allIDs)
            {
                string tag = GetDistinctGilrsBonus(id);
                if (!distinctGilrsBonus.Contains(tag))
                {
                    distinctGilrsBonus.Add(tag);
                    DsCards.CardsRow cardRow = cardsList[id];
                    var girlsBonusList = girlsSelectionBonusList[cardRow.名前];
                    foreach (string bonusName in girlsBonusList.AtkBonus)
                    {
                        if (!bonusList.ContainsKey(bonusName))
                        {
                            bonusList.Add(bonusName, 0);
                        }
                        bonusList[bonusName] = bonusList[bonusName] + 1;
                    }
                }
            }

            //トップ５の計算
            int bonusMin = 99;
            foreach (var bonus in bonusList.OrderByDescending(b => b.Value).Take(5))
            {
                int power = (selectBonus[(bonus.Value > 5 ? 5 : bonus.Value) - 1]);
                selectionBonusPower.Add(power);
                selectionBonusTotal = selectionBonusTotal + power;
                //ボーナスの最低値を保持する
                if (bonusMin > bonus.Value)
                {
                    bonusMin = bonus.Value;
                }
            }
            foreach (var bonus in bonusList)
            {
                SelectionBonusInfo info = selectionBonusInfoDic[bonus.Key];
                info.UseCount = bonus.Value;
                info.IsEffection = (info.UseCount >= bonusMin);
            }
            selectionBonusInfo = selectionBonusInfoDic.Select(sbi => sbi.Value).ToList();

            //声援
            List<string> skillId = new List<string>();
            if (mainIDs.Count > 0)
            {
                skillId.Add(mainIDs[0]);
            }
            skillId.AddRange(dsMainSelect.Card.Skip(1).Where(r => r.スキル発動).Select(r => r.ID));

            foreach (string topId in skillId)
            {
                DsCards.CardsRow topCardRow = cardsList[topId];
                DsGirls.GirlsRow topGilrsRow = girlsList[topCardRow.名前];
                SkillInfo topSkill = Skills.FirstOrDefault(s => s.Name == topCardRow.スキル);
                ActiveSkill skill = new ActiveSkill()
                {
                    Power = topSkill.IsAttack ? (topCardRow.全属性スキル ? topSkill.AllPower + Convert.ToInt32(topCardRow.スキルLv) - 1 : topSkill.Power + Convert.ToInt32(topCardRow.スキルLv) - 1) : 0,
                    ID = topId,
                    isOwn = topSkill.IsOwn,
                    isAll = topCardRow.全属性スキル,
                    Attr = topGilrsRow.属性,
                    Name = topCardRow.スキル
                };
                skills.Add(skill);
            }

            //役職
            role = 0;
            if (dsSetting.User[0].Role == "部長")
            {
                role = clubLeaderBonus;
            }
            else if (dsSetting.User[0].Role == "副部長")
            {
                role = clubSubLeaderBonus;
            }
            else if (dsSetting.User[0].Role == "攻キャプテン")
            {
                role = clubAtkBonus;
            }

            items = new HashSet<string>();
            if (dsSetting.User[0].HasRocker)
            {
                items.Add(ATTR_SWEET);
            }
            if (dsSetting.User[0].HasWhiteBoard)
            {
                items.Add(ATTR_COOL);
            }
            if (dsSetting.User[0].HasTV)
            {
                items.Add(ATTR_POP);
            }

            //自属性
            ownKind = dsSetting.User[0].Attribute;
        }
        #endregion

        #region 守援

        private int CalcTotalDef(List<string> mainIDs, List<string> subIDs, out Dictionary<string, PowerInfo> defList, out BonusInfo bonusInfo, out List<SelectionBonusInfo> selectionBonusInfo)
        {
            int total = 0;
            defList = new Dictionary<string, PowerInfo>();

            Dictionary<string, int> bonusList;
            int selectionBonusTotal;
            List<int> selectionBonusPower;
            List<ActiveSkill> skills;
            int role;
            HashSet<string> items;
            string ownKind;
            CalcTotalDefBase(mainIDs, subIDs, out bonusList, out selectionBonusInfo, out selectionBonusTotal, out selectionBonusPower, out skills, out role, out items, out ownKind);

            bonusInfo = new BonusInfo();
            bonusInfo.Attribute = sameAttributeBonus;
            bonusInfo.AttributeTarget = ownKind;
            bonusInfo.Items = new Dictionary<string, int>();
            foreach (string atr in new string[] { ATTR_POP, ATTR_COOL, ATTR_SWEET })
            {
                bonusInfo.Items.Add(atr, (items.Contains(atr) ? clubItemBonus : 0));
            }
            bonusInfo.Role = role;
            bonusInfo.Selection = selectionBonusTotal;
            bonusInfo.SelectionList = selectionBonusInfo.Where(b => b.UseCount > 0).OrderByDescending(b => b.UseCount).Take(5).Select(b => selectBonus[(b.UseCount > 5 ? 5 : b.UseCount) - 1]).ToList();
            bonusInfo.Skill = skills.Count > 0 ? skills[0] : null;

            //主選抜
            int mainTotal = 0;
            foreach (string id in mainIDs)
            {
                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == id);
                DsGirls.GirlsRow mainGilrsRow = dsGilrs.Girls.FirstOrDefault(r => r.名前 == cardRow.名前);
                double etcBonus = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
                double mainBaseDef = Convert.ToDouble(cardRow.守援);
                double mainBonusDef = mainBaseDef + Math.Ceiling(mainBaseDef * etcBonus / 100d);
                double mainDef = mainBaseDef;
                double mainSumDef = mainDef;
                double totalMainSkill = 0;
                skills.ForEach(s =>
                {
                    if (s.isOwn)
                    {
                        //自分自身の場合
                        if (s.ID == id)
                        {
                            totalMainSkill += s.Power;
                        }
                    }
                    else
                    {
                        totalMainSkill += (s.isAll || s.Attr == mainGilrsRow.属性) ? s.Power : 0;
                    }
                });
                //備品：2%
                double mainItem = items.Contains(mainGilrsRow.属性) ? clubItemBonus : 0;
                //同属性：5%
                double mainAttr = mainGilrsRow.属性 == ownKind ? sameAttributeBonus : 0;
                //同部室
                double mainClub = mainGilrsRow.部室 == dsSetting.User[0].Club ? clubSameBonus : 0;
                mainSumDef = mainSumDef
                    + Math.Ceiling(mainDef * etcBonus / 100d)
                    + Math.Ceiling(mainDef * totalMainSkill / 100d)
                    + Math.Ceiling(mainDef * (role + mainItem) / 100d)
                    + Math.Ceiling(mainDef * mainAttr / 100d)
                    + Math.Ceiling((mainDef * mainClub / 100d));

                selectionBonusPower.ForEach(p =>
                {
                    mainSumDef += Math.Ceiling(mainDef * p / 100d);
                });
                defList.Add(id, new PowerInfo()
                {
                    ID = id,
                    Power = Convert.ToInt32(mainSumDef),
                    BasePower = Convert.ToInt32(mainBaseDef),
                    SelectionPower = Convert.ToInt32(mainBonusDef),
                    BonusEtc = Convert.ToInt32(etcBonus),
                    BonusSkill = Convert.ToInt32(totalMainSkill),
                    BonusRole = Convert.ToInt32(role),
                    BonusClub = Convert.ToInt32(mainClub),
                    BonusAttr = Convert.ToInt32(mainAttr),
                    BonusItem = Convert.ToInt32(mainItem),
                });
                mainTotal += Convert.ToInt32(mainSumDef);
            }

            //サブ
            int subTotal = 0;
            foreach (string id in subIDs)
            {
                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == id);
                double etcBonus = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
                double subOriginal = Convert.ToDouble(cardRow.守援);
                double subBonusDef = Math.Truncate(subOriginal * 0.8 + Math.Ceiling(subOriginal * 0.8 * etcBonus / 100d));
                double subDef = Convert.ToDouble(cardRow.守援);
                double subBaseDef = Math.Truncate(subDef * 0.8 * (1d + (0.8 * etcBonus / 100d)));
                double subSumDef = subBaseDef;

                selectionBonusPower.ForEach(p =>
                {
                    subSumDef += Math.Ceiling(subDef * p / 100d);
                });
                defList.Add(id, new PowerInfo()
                {
                    ID = id,
                    Power = Convert.ToInt32(subSumDef),
                    BasePower = Convert.ToInt32(cardRow.ボーナス有無 ? subBonusDef : subBaseDef),
                    SelectionPower = Convert.ToInt32(subBaseDef),
                    BonusEtc = Convert.ToInt32(etcBonus),
                });
                subTotal += Convert.ToInt32(subSumDef);
            }
            total = mainTotal + subTotal;
            return total;
        }

        private void CalcTotalDefBase(List<string> mainIDs, List<string> subIDs, out Dictionary<string, int> bonusList, out List<SelectionBonusInfo> selectionBonusInfo, out int selectionBonusTotal, out List<int> selectionBonusPower, out List<ActiveSkill> skills, out int role, out HashSet<string> items, out string ownKind)
        {
            //選抜ボーナスの計算
            List<string> allIDs = new List<string>();
            allIDs.AddRange(mainIDs);
            allIDs.AddRange(subIDs);
            bonusList = new Dictionary<string, int>();
            selectionBonusPower = new List<int>();
            selectionBonusInfo = new List<SelectionBonusInfo>();
            Dictionary<string, SelectionBonusInfo> selectionBonusInfoDic = new Dictionary<string, SelectionBonusInfo>();
            foreach (var kvp in defSelectionBonusInfo)
            {
                selectionBonusInfoDic.Add(kvp.Key, kvp.Value.Copy());
            }
            selectionBonusTotal = 0;
            skills = new List<ActiveSkill>();
            role = 0;
            items = new HashSet<string>();
            ownKind = string.Empty;

            HashSet<string> distinctGilrsBonus = new HashSet<string>();
            foreach (string id in allIDs)
            {
                string tag = GetDistinctGilrsBonus(id);
                if (!distinctGilrsBonus.Contains(tag))
                {
                    distinctGilrsBonus.Add(tag);
                    DsCards.CardsRow cardRow = cardsList[id];
                    var girlsBonusList = girlsSelectionBonusList[cardRow.名前];
                    foreach (string bonusName in girlsBonusList.DefBonus)
                    {
                        if (!bonusList.ContainsKey(bonusName))
                        {
                            bonusList.Add(bonusName, 0);
                        }
                        bonusList[bonusName] = bonusList[bonusName] + 1;
                    }
                }
            }

            //トップ５の計算
            int bonusMin = 99;
            foreach (var bonus in bonusList.OrderByDescending(b => b.Value).Take(5))
            {
                int power = (selectBonus[(bonus.Value > 5 ? 5 : bonus.Value) - 1]);
                selectionBonusPower.Add(power);
                selectionBonusTotal = selectionBonusTotal + power;
                //ボーナスの最低値を保持する
                if (bonusMin > bonus.Value)
                {
                    bonusMin = bonus.Value;
                }
            }
            foreach (var bonus in bonusList)
            {
                SelectionBonusInfo info = selectionBonusInfoDic[bonus.Key];
                info.UseCount = bonus.Value;
                info.IsEffection = (info.UseCount >= bonusMin);
            }
            selectionBonusInfo = selectionBonusInfoDic.Select(sbi => sbi.Value).ToList();

            //声援
            List<string> skillId = new List<string>();
            if (mainIDs.Count > 0)
            {
                skillId.Add(mainIDs[0]);
            }

            skillId.AddRange(dsMainSelect.Card.Skip(1).Where(r => r.スキル発動).Select(r => r.ID));

            foreach (string topId in skillId)
            {
                DsCards.CardsRow topCardRow = cardsList[topId];
                DsGirls.GirlsRow topGilrsRow = girlsList[topCardRow.名前];
                SkillInfo topSkill = Skills.FirstOrDefault(s => s.Name == topCardRow.スキル);
                ActiveSkill skill = new ActiveSkill()
                {
                    Power = topSkill.IsDeffence ? (topCardRow.全属性スキル ? topSkill.AllPower + Convert.ToInt32(topCardRow.スキルLv) - 1 : topSkill.Power + Convert.ToInt32(topCardRow.スキルLv) - 1) : 0,
                    ID = topId,
                    isOwn = topSkill.IsOwn,
                    isAll = topCardRow.全属性スキル,
                    Attr = topGilrsRow.属性,
                    Name = topCardRow.スキル
                };
                skills.Add(skill);
            }

            //役職
            role = 0;
            if (dsSetting.User[0].Role == "部長")
            {
                role = clubLeaderBonus;
            }
            else if (dsSetting.User[0].Role == "副部長")
            {
                role = clubSubLeaderBonus;
            }
            else if (dsSetting.User[0].Role == "守キャプテン")
            {
                role = clubDefBonus;
            }

            items = new HashSet<string>();
            if (dsSetting.User[0].HasRocker)
            {
                items.Add(ATTR_SWEET);
            }
            if (dsSetting.User[0].HasWhiteBoard)
            {
                items.Add(ATTR_COOL);
            }
            if (dsSetting.User[0].HasTV)
            {
                items.Add(ATTR_POP);
            }

            //自属性
            ownKind = dsSetting.User[0].Attribute;
        }
        #endregion

        #region イベント
        private int CalcTotalEvent(List<string> mainIDs, List<string> subIDs, out Dictionary<string, PowerInfo> atkList, out BonusInfo bonusInfo, out List<SelectionBonusInfo> selectionBonusInfo)
        {
            int total = 0;
            atkList = new Dictionary<string, PowerInfo>();

            Dictionary<string, int> bonusList;
            int selectionBonusTotal;
            List<int> selectionBonusPower;
            List<ActiveSkill> skills;
            int role;
            HashSet<string> items;
            string ownKind;
            CalcTotalEventBase(mainIDs, subIDs, out bonusList, out selectionBonusInfo, out selectionBonusTotal, out selectionBonusPower, out skills, out role, out items, out ownKind);

            bonusInfo = new BonusInfo();
            bonusInfo.Attribute = sameAttributeBonus;
            bonusInfo.AttributeTarget = ownKind;
            bonusInfo.Items = new Dictionary<string, int>();
            foreach (string atr in new string[] { ATTR_POP, ATTR_COOL, ATTR_SWEET })
            {
                bonusInfo.Items.Add(atr, (items.Contains(atr) ? clubItemBonus : 0));
            }
            bonusInfo.Role = role;
            bonusInfo.Selection = selectionBonusTotal;
            bonusInfo.SelectionList = selectionBonusInfo.Where(b => b.UseCount > 0).OrderByDescending(b => b.UseCount).Take(5).Select(b => eventBonus[(b.UseCount > 5 ? 5 : b.UseCount) - 1]).ToList();
            bonusInfo.Skill = null;

            //主選抜
            int mainTotal = 0;
            foreach (string id in mainIDs)
            {
                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == id);
                DsGirls.GirlsRow mainGilrsRow = dsGilrs.Girls.FirstOrDefault(r => r.名前 == cardRow.名前);
                double etcBonus = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
                double originalAtk = Convert.ToDouble(cardRow.攻援);
                double bonusAtk = Convert.ToDouble(cardRow.攻援) + Math.Ceiling(cardRow.攻援 * etcBonus / 100);
                double spAtk = Convert.ToDouble(bonusAtk) + (cardRow.スペシャル > 1 ? cardRow.スペシャル * Convert.ToDouble(originalAtk) : 0);
                double baseAtk = spAtk;
                double mainAtk = baseAtk;
                double totalMainSkill = 0;
                skills.ForEach(s =>
                {
                    if (s.isOwn)
                    {
                        //自分自身の場合
                        if (s.ID == id)
                        {
                            totalMainSkill += s.Power;
                        }
                    }
                    else
                    {
                        totalMainSkill += (s.isAll || s.Attr == mainGilrsRow.属性) ? s.Power : 0;
                    }
                });
                //備品：2%
                double mainItem = items.Contains(mainGilrsRow.属性) ? clubItemBonus : 0;
                //同属性：5%
                double mainAttr = mainGilrsRow.属性 == ownKind ? sameAttributeBonus : 0;
                //同部室
                double mainClub = mainGilrsRow.部室 == dsSetting.User[0].Club ? clubSameBonus : 0;

                mainAtk = baseAtk
                    + Math.Ceiling(originalAtk * etcBonus / 100d)
                    + Math.Ceiling(originalAtk * totalMainSkill / 100)
                    + Math.Ceiling(originalAtk * selectionBonusTotal / 100)
                    + Math.Ceiling(originalAtk * (role + mainItem) / 100)
                    + Math.Ceiling(originalAtk * mainAttr / 100);
                mainAtk = Math.Ceiling(mainAtk * ((double)dsSetting.User[0].AtkCost / 100));
                spAtk = Math.Ceiling(spAtk * ((double)dsSetting.User[0].AtkCost / 100));

                atkList.Add(id, new PowerInfo()
                {
                    ID = id,
                    IsMain = true,
                    OriginalPower = Convert.ToInt32(originalAtk),
                    Power = Convert.ToInt32(mainAtk),
                    BasePower = Convert.ToInt32(baseAtk),
                    SelectionPower = Convert.ToInt32(spAtk),
                    BonusEtc = Convert.ToInt32(etcBonus),
                    BonusSkill = Convert.ToInt32(totalMainSkill),
                    BonusRole = Convert.ToInt32(role),
                    BonusClub = Convert.ToInt32(mainClub),
                    BonusAttr = Convert.ToInt32(mainAttr),
                    BonusItem = Convert.ToInt32(mainItem),
                });
                mainTotal += Convert.ToInt32(mainAtk);
            }

            //サブ
            int subTotal = 0;
            foreach (string id in subIDs)
            {
                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == id);
                double etcBonus = cardRow.ボーナス有無 ? cardRow.ボーナス : 0;
                double subOriginalAtk = Convert.ToDouble(cardRow.攻援);
                double subSpAtk = Convert.ToDouble(cardRow.攻援) + (cardRow.スペシャル > 1 ? cardRow.スペシャル * Convert.ToDouble(cardRow.攻援) : 0);
                double subBaseAtk = subSpAtk;
                double subAtk = subBaseAtk;
                double subSumAtk = subBaseAtk;


                selectionBonusPower.ForEach(p =>
                {
                    subSumAtk += Math.Ceiling(subOriginalAtk * p / 100d);
                });
                subSumAtk = Math.Ceiling(subSumAtk * ((double)dsSetting.User[0].AtkCost / 200));
                subSpAtk = Math.Ceiling(subAtk * ((double)dsSetting.User[0].AtkCost / 200));

                atkList.Add(id, new PowerInfo()
                {
                    ID = id,
                    IsMain = false,
                    OriginalPower = Convert.ToInt32(subOriginalAtk),
                    Power = Convert.ToInt32(subSumAtk),
                    BasePower = Convert.ToInt32(subBaseAtk),
                    SelectionPower = Convert.ToInt32(subSpAtk),
                    BonusEtc = Convert.ToInt32(etcBonus),
                });
                subTotal += Convert.ToInt32(subSumAtk);
            }
            total = mainTotal + subTotal;
            return total;
        }

        private void CalcTotalEventBase(List<string> mainIDs, List<string> subIDs, out Dictionary<string, int> bonusList, out List<SelectionBonusInfo> selectionBonusInfo, out int selectionBonusTotal, out List<int> selectionBonusPower, out List<ActiveSkill> skills, out int role, out HashSet<string> items, out string ownKind)
        {
            //選抜ボーナスの計算
            List<string> allIDs = new List<string>();
            allIDs.AddRange(mainIDs);
            allIDs.AddRange(subIDs);
            bonusList = new Dictionary<string, int>();
            selectionBonusPower = new List<int>();
            selectionBonusInfo = new List<SelectionBonusInfo>();
            Dictionary<string, SelectionBonusInfo> selectionBonusInfoDic = new Dictionary<string, SelectionBonusInfo>();
            foreach (var kvp in atkSelectionBonusInfo)
            {
                selectionBonusInfoDic.Add(kvp.Key, kvp.Value.Copy());
            }

            selectionBonusTotal = 0;
            skills = new List<ActiveSkill>();
            role = 0;
            items = new HashSet<string>();
            ownKind = string.Empty;

            HashSet<string> distinctGilrsBonus = new HashSet<string>();
            foreach (string id in allIDs)
            {
                string tag = GetDistinctGilrsBonus(id);
                if (!distinctGilrsBonus.Contains(tag))
                {
                    distinctGilrsBonus.Add(tag);
                    DsCards.CardsRow cardRow = cardsList[id];
                    var girlsBonusList = girlsSelectionBonusList[cardRow.名前];
                    foreach (string bonusName in girlsBonusList.AtkBonus)
                    {
                        if (!bonusList.ContainsKey(bonusName))
                        {
                            bonusList.Add(bonusName, 0);
                        }
                        bonusList[bonusName] = bonusList[bonusName] + 1;
                    }
                }
            }

            //トップ５の計算
            int bonusMin = 99;
            foreach (var bonus in bonusList.OrderByDescending(b => b.Value).Take(5))
            {
                int power = (eventBonus[(bonus.Value > 5 ? 5 : bonus.Value) - 1]);
                selectionBonusPower.Add(power);
                selectionBonusTotal = selectionBonusTotal + power;
                //ボーナスの最低値を保持する
                if (bonusMin > bonus.Value)
                {
                    bonusMin = bonus.Value;
                }
            }
            foreach (var bonus in bonusList)
            {
                SelectionBonusInfo info = selectionBonusInfoDic[bonus.Key];
                info.UseCount = bonus.Value;
                info.IsEffection = (info.UseCount >= bonusMin);
            }
            selectionBonusInfo = selectionBonusInfoDic.Select(sbi => sbi.Value).ToList();

            //声援
            List<string> skillId = new List<string>();
            if (mainIDs.Count > 0)
            {
                skillId.Add(mainIDs[0]);
            }
            skillId.AddRange(dsMainSelect.Card.Where(r => r.スキル発動).Select(r => r.ID));

            foreach (string topId in skillId)
            {
                DsCards.CardsRow topCardRow = cardsList[topId];
                DsGirls.GirlsRow topGilrsRow = girlsList[topCardRow.名前];
                SkillInfo topSkill = Skills.FirstOrDefault(s => s.Name == topCardRow.スキル);
                ActiveSkill skill = new ActiveSkill()
                {
                    Power = topSkill.IsAttack ? (topCardRow.全属性スキル ? topSkill.AllPower + Convert.ToInt32(topCardRow.スキルLv) - 1 : topSkill.Power + Convert.ToInt32(topCardRow.スキルLv) - 1) : 0,
                    ID = topId,
                    isOwn = topSkill.IsOwn,
                    isAll = topCardRow.全属性スキル,
                    Attr = topGilrsRow.属性,
                    Name = topCardRow.スキル
                };
                skills.Add(skill);
            }

            //役職
            role = 0;
            if (dsSetting.User[0].Role == "部長")
            {
                role = clubLeaderBonus;
            }
            else if (dsSetting.User[0].Role == "副部長")
            {
                role = clubSubLeaderBonus;
            }
            else if (dsSetting.User[0].Role == "攻キャプテン")
            {
                role = clubAtkBonus;
            }

            items = new HashSet<string>();
            if (ChkUserItemRocker.IsChecked ?? false)
            {
                items.Add(ATTR_SWEET);
            }
            if (ChkUserItemWhiteBoard.IsChecked ?? false)
            {
                items.Add(ATTR_COOL);
            }
            if (ChkUserItemTv.IsChecked ?? false)
            {
                items.Add(ATTR_POP);
            }

            //自属性
            ownKind = dsSetting.User[0].Attribute;
        }
        #endregion

        private void RefreshDeckCard()
        {
            TBtnSelectAllDeck.IsChecked = true;
            TBtnSelectCoolDeck.IsChecked = false;
            TBtnSelectPopDeck.IsChecked = false;
            TBtnSelectSweetDeck.IsChecked = false;
            DgDeckCards.ItemsSource = dsDeckCard.DeckCard.OrderBy(r=>r.表示順).AsDataView();
            ClearSelectionBonusDisplay();
            LstBonus.SelectedItem = null;
        }

        #region デッキ表示選択
        private void TBtnSelectAllDeck_Checked(object sender, RoutedEventArgs e)
        {
            TBtnSelectCoolDeck.IsChecked = false;
            TBtnSelectSweetDeck.IsChecked = false;
            TBtnSelectPopDeck.IsChecked = false;

            DgDeckCards.ItemsSource = dsDeckCard.DeckCard.OrderBy(r => r.表示順).AsDataView();
            LstBonus.SelectedItem = null;
        }

        private void TBtnSelectSweetDeck_Checked(object sender, RoutedEventArgs e)
        {
            TBtnSelectCoolDeck.IsChecked = false;
            TBtnSelectAllDeck.IsChecked = false;
            TBtnSelectPopDeck.IsChecked = false;
            DgDeckCards.ItemsSource = dsDeckCard.DeckCard.Where(r => r.属性 == ATTR_SWEET).OrderBy(r => r.表示順).AsDataView();
            LstBonus.SelectedItem = null;
        }

        private void TBtnSelectCoolDeck_Checked(object sender, RoutedEventArgs e)
        {
            TBtnSelectAllDeck.IsChecked = false;
            TBtnSelectSweetDeck.IsChecked = false;
            TBtnSelectPopDeck.IsChecked = false;
            DgDeckCards.ItemsSource = dsDeckCard.DeckCard.Where(r => r.属性 == ATTR_COOL).OrderBy(r => r.表示順).AsDataView();
            LstBonus.SelectedItem = null;
        }

        private void TBtnSelectPopDeck_Checked(object sender, RoutedEventArgs e)
        {
            TBtnSelectCoolDeck.IsChecked = false;
            TBtnSelectSweetDeck.IsChecked = false;
            TBtnSelectAllDeck.IsChecked = false;
            DgDeckCards.ItemsSource = dsDeckCard.DeckCard.Where(r => r.属性 == ATTR_POP).OrderBy(r => r.表示順).AsDataView();
            LstBonus.SelectedItem = null;
        }
        #endregion

        #region ユーザー設定
        private void TxtUserLv_LostFocus(object sender, RoutedEventArgs e)
        {
            string userLv = TxtUserLv.Text;

            SetSubMax(userLv);
            int lv;
            if (Int32.TryParse(userLv, out lv))
            {
                dsSetting.User[0].Lv = lv;
                dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
            }
        }

        /// <summary>
        /// 副センバツ最大数計算
        /// </summary>
        /// <param name="userLv"></param>
        private void SetSubMax(string userLv)
        {
            int lv;
            if (Int32.TryParse(userLv, out lv))
            {
                if (lv > 100)
                {
                    subMax = (lv - 100) / 10;

                    subMax += 18;
                }
                else
                {
                    subMax = (lv - 10) / 5;
                }
                if (subMax < 0) subMax = 0;
            }
            else
            {
                subMax = 0;
            }
            LblSubMax.Content = "/" + subMax.ToString();
        }

        private void CmbUserAttr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent && e.AddedItems.Count > 0)
            {
                dsSetting.User[0].Attribute = ((KeyValuePair<string, string>)e.AddedItems[0]).Value;
                ReCalcAll();
                dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
            }
        }

        private void CmbUserRole_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent && e.AddedItems.Count > 0)
            {
                dsSetting.User[0].Role = e.AddedItems[0].ToString();
                ReCalcAll();
                dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
            }
        }

        private void TxtUserAttkCost_LostFocus(object sender, RoutedEventArgs e)
        {
            int atkCost;
            if (int.TryParse(TxtUserAttkCost.Text, out atkCost))
            {
                dsSetting.User[0].AtkCost = atkCost;
                dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
                if (IsAttack())
                {
                    int currentNumber = (int)CmbDeck.SelectedValue;
                    DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == currentNumber && r.Type == calcType.ToString());
                    if (row != null)
                    {
                        //コスト制限
                        if (row.IsCostLimited)
                        {
                            costMax = row.LimitedCost;
                        }
                        else
                        {
                            costMax = atkCost;
                        }
                    }
                    SetCostInfo();
                }
                else
                {
                    //守援の場合はなにもしない　
                }
            }
        }

        private void TxtUserDefCost_LostFocus(object sender, RoutedEventArgs e)
        {
            int defCost;
            if (int.TryParse(TxtUserDefCost.Text, out defCost))
            {
                dsSetting.User[0].DefCost = defCost;
                dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
                if (IsAttack())
                {
                    //攻援の場合はなにもしない　
                }
                else
                {
                    int currentNumber = (int)CmbDeck.SelectedValue;
                    DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == currentNumber && r.Type == calcType.ToString());
                    if (row != null)
                    {
                        //コスト制限
                        if (row.IsCostLimited)
                        {
                            costMax = row.LimitedCost;
                        }
                        else
                        {
                            costMax = defCost;
                        }
                    }
                    SetCostInfo();
                }
            }
        }

        private void ChkUserItemRocker_Click(object sender, RoutedEventArgs e)
        {
            dsSetting.User[0].HasRocker = ChkUserItemRocker.IsChecked ?? false;
            ReCalcAll();
            dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));

        }

        private void ChkUserItemWhiteBoard_Click(object sender, RoutedEventArgs e)
        {
            dsSetting.User[0].HasWhiteBoard = ChkUserItemWhiteBoard.IsChecked ?? false;
            ReCalcAll();
            dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
        }

        private void ChkUserItemTv_Click(object sender, RoutedEventArgs e)
        {
            dsSetting.User[0].HasTV = ChkUserItemTv.IsChecked ?? false;
            ReCalcAll();
            dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));

        }

        private void CmbUserClub_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent && e.AddedItems.Count > 0)
            {
                dsSetting.User[0].Club = e.AddedItems[0].ToString();
                ReCalcAll();
                dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
            }
        }

        private void ChkEtcEditColumn_Click(object sender, RoutedEventArgs e)
        {
            dsSetting.Etc[0].EditColumn = ChkEtcEditColumn.IsChecked ?? false;
            dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
        }

        private void TxtSkill_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (sender as TextBox);
            string textName = textBox.Name;
            int value = string.IsNullOrWhiteSpace(textBox.Text) ? 0 : Convert.ToInt32(textBox.Text);
            string[] skillName = textName.Split('_');

            //攻守スーパー
            var textSkill = new[]{
                new{Text = "SSB", Name="スーパー特大"},
                new{Text = "SB", Name="特大"},
                new{Text = "B", Name="大"},
                new{Text = "M", Name="中"},
                new{Text = "S", Name="小"},
            };
            var targetSkill = new[] {
                new {Text ="Own" ,IsOwn = true,IsAll=false},
                new {Text ="Same" ,IsOwn = false,IsAll=false},
                new {Text ="All" ,IsOwn = false,IsAll=true},
            };
            var typeSkill = new[] { new { Text = "Full", Name = "攻守" }, new { Text = "Half", Name = "攻援" }, new { Text = "Half", Name = "守援" } };

            var skill = textSkill.FirstOrDefault(s => s.Text == skillName[3]);
            var target = targetSkill.FirstOrDefault(s => s.Text == skillName[1]);
            foreach (var type in typeSkill.Where(s => s.Text == skillName[2]))
            {
                SkillInfo skillInfo = Skills.FirstOrDefault(s => s.Name == (type.Name + skill.Name) && s.IsOwn == target.IsOwn);
                if (skillInfo != null)
                {
                    DsSystemSetting.声援Row skillRow = dsSystemSetting.声援.FirstOrDefault(r => r.名前 == (type.Name + skill.Name));
                    if (target.IsAll)
                    {
                        skillInfo.AllPower = value;
                        if (skillRow != null)
                        {
                            skillRow.全属Power = value;
                        }
                    }
                    else
                    {
                        skillInfo.Power = value;
                        if (skillRow != null)
                        {
                            skillRow.Power = value;
                        }
                    }
                }
            }

            dsSystemSetting.AcceptChanges();
            dsSystemSetting.WriteXml(Utility.GetFilePath("system_settings.xml"));
            ReCalcAll();

        }

        private void SetBonusEdit(object sender, ref int bonus, string columnName)
        {
            TextBox textBox = sender as TextBox;
            int value = string.IsNullOrWhiteSpace(textBox.Text) ? 0 : Convert.ToInt32(textBox.Text);

            bonus = value;
            DsSystemSetting.BonusRow row = dsSystemSetting.Bonus[0];
            row[columnName] = value;
            dsSystemSetting.AcceptChanges();
            dsSystemSetting.WriteXml(Utility.GetFilePath("system_settings.xml"));
            ReCalcAll();
        }

        private void TxtBonusAttr_LostFocus_1(object sender, RoutedEventArgs e)
        {
            SetBonusEdit(sender, ref sameAttributeBonus, "同属性");
        }

        private void TxtBonusClubSame_LostFocus_1(object sender, RoutedEventArgs e)
        {
            SetBonusEdit(sender, ref clubSameBonus, "部室");
        }

        private void TxtBonusClubItem_LostFocus_1(object sender, RoutedEventArgs e)
        {
            SetBonusEdit(sender, ref clubItemBonus, "備品");
        }

        private void TxtBonusClubLeader_LostFocus_1(object sender, RoutedEventArgs e)
        {
            SetBonusEdit(sender, ref clubLeaderBonus, "部長");
        }

        private void TxtBonusClubSubLeader_LostFocus_1(object sender, RoutedEventArgs e)
        {
            SetBonusEdit(sender, ref clubSubLeaderBonus, "副部長");
        }

        private void TxtBonusClubAtk_LostFocus_1(object sender, RoutedEventArgs e)
        {
            SetBonusEdit(sender, ref clubAtkBonus, "攻キャプテン");
        }

        private void TxtBonusClubDef_LostFocus_1(object sender, RoutedEventArgs e)
        {
            SetBonusEdit(sender, ref clubDefBonus, "守キャプテン");
        }

        private void TxtBonusSelect_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            int lv = Convert.ToInt32(textBox.Name.Substring(textBox.Name.Length - 1));
            int value = string.IsNullOrWhiteSpace(textBox.Text) ? 0 : Convert.ToInt32(textBox.Text);

            selectBonus[lv - 1] = value;
            DsSystemSetting.センバツRow row = dsSystemSetting.センバツ[0];
            row["Lv" + lv.ToString()] = value;
            dsSystemSetting.AcceptChanges();
            dsSystemSetting.WriteXml(Utility.GetFilePath("system_settings.xml"));
            ReCalcAll();
        }

        private void BtnShowGirlPath_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                dsSetting.Etc[0].ShowGirlPath = dialog.SelectedPath;
                LblShowGirlPath.Text = dialog.SelectedPath;
                dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));
            }
        }

        #endregion

        #region 自動計算
        private void BtnSubAuto_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {

                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }

            Dictionary<string, DsGirls.GirlsRow> idGirlsList = new Dictionary<string, DsGirls.GirlsRow>();
            foreach (var cardRow in dsCards.Cards)
            {
                DsGirls.GirlsRow girlRow = dsGilrs.Girls.FirstOrDefault(r => r.名前 == cardRow.名前);
                idGirlsList.Add(cardRow.ID, girlRow);
            }

            Dictionary<string, List<CardInfo>> tmpBonusCardList = new Dictionary<string, List<CardInfo>>();
            Dictionary<string, CardInfo> idCardList = new Dictionary<string, CardInfo>();

            if (calcType == CalcType.攻援)
            {
                #region 攻援
                CreateAtkSelectionBonusInfo();

                //各ボーナスごとの最大値
                foreach (var cardRow in dsCards.Cards)
                {
                    CardInfo item = new CardInfo()
                    {
                        ID = cardRow.ID,
                        Name = cardRow.名前,
                        Cost = cardRow.コスト,
                        Power = cardRow.攻援,
                    };
                    if (!idCardList.ContainsKey(cardRow.ID))
                    {
                        idCardList.Add(cardRow.ID, item);
                    }
                }

                foreach (string bonusName in AtkBounus)
                {
                    tmpBonusCardList.Add(bonusName, new List<CardInfo>());
                    foreach (var cardRow in dsCards.Cards.Where(r => !r.ダミー))
                    {
                        DsGirls.GirlsRow girlRow = idGirlsList[cardRow.ID];

                        if (girlRow.攻援1 != "-")
                        {
                            if (bonusName == girlRow.攻援1)
                            {
                                tmpBonusCardList[bonusName].Add(idCardList[cardRow.ID]);
                            }
                        }
                        if (girlRow.攻援2 != "-")
                        {
                            if (bonusName == girlRow.攻援2)
                            {
                                tmpBonusCardList[bonusName].Add(idCardList[cardRow.ID]);
                            }
                        }
                        if (girlRow.攻援3 != "-")
                        {
                            if (bonusName == girlRow.攻援3)
                            {
                                tmpBonusCardList[bonusName].Add(idCardList[cardRow.ID]);
                            }
                        }
                    }
                }

                Dictionary<string, SelectionBonus> selectionBonusTotalList = new Dictionary<string, SelectionBonus>();
                Dictionary<string, List<CardInfo>> bonusCardList = new Dictionary<string, List<CardInfo>>();
                Dictionary<string, List<CardInfo>> selectBonusCardList = new Dictionary<string, List<CardInfo>>();

                foreach (var kvp in tmpBonusCardList)
                {
                    List<CardInfo> c = kvp.Value.OrderByDescending(r => r.Power).ToList();
                    bonusCardList.Add(kvp.Key, c);
                    selectBonusCardList.Add(kvp.Key, kvp.Value.OrderByDescending(r => r.Power).Take(5).ToList());
                    selectionBonusTotalList.Add(kvp.Key, new SelectionBonus()
                    {
                        TotalPower = kvp.Value.OrderByDescending(r => r.Power).Take(5).Sum(r => r.Power),
                        MaxCount = c.Count > 5 ? 5 : c.Count
                    });
                }

                //Mainを除外
                foreach (DsSelectCard.CardRow selectCard in dsMainSelect.Card)
                {
                    DsGirls.GirlsRow girlsRow = idGirlsList[selectCard.ID];
                    if (girlsRow.攻援1 != "-")
                    {
                        selectionBonusTotalList[girlsRow.攻援1].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.攻援1].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.攻援1].Remove(item);
                        }
                    }
                    if (girlsRow.攻援2 != "-")
                    {
                        selectionBonusTotalList[girlsRow.攻援2].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.攻援2].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.攻援2].Remove(item);
                        }
                    }
                    if (girlsRow.攻援3 != "-")
                    {
                        selectionBonusTotalList[girlsRow.攻援3].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.攻援3].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.攻援3].Remove(item);
                        }
                    }
                }
                foreach (DsSelectCard.CardRow selectCard in dsSubSelect.Card)
                {
                    DsGirls.GirlsRow girlsRow = idGirlsList[selectCard.ID];
                    if (girlsRow.攻援1 != "-")
                    {
                        selectionBonusTotalList[girlsRow.攻援1].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.攻援1].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.攻援1].Remove(item);
                        }
                    }
                    if (girlsRow.攻援2 != "-")
                    {
                        selectionBonusTotalList[girlsRow.攻援2].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.攻援2].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.攻援2].Remove(item);
                        }
                    }
                    if (girlsRow.攻援3 != "-")
                    {
                        selectionBonusTotalList[girlsRow.攻援3].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.攻援3].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.攻援3].Remove(item);
                        }
                    }
                }

                Dictionary<string, CardInfo> addCardList = new Dictionary<string, CardInfo>();

                int lastCount = subMax - dsSubSelect.Card.Count;
                foreach (var bonus in selectionBonusTotalList
                    .OrderByDescending(b => b.Value.MaxCount)
                    .ThenByDescending(b => b.Value.LastCount)
                    .ThenByDescending(b => b.Value.TotalPower).Take(5))
                {
                    if (lastCount <= 0) break;
                    for (int i = 0; i < selectBonusCardList[bonus.Key].Count; i++)
                    {
                        if (lastCount <= 0) break;

                        string id = selectBonusCardList[bonus.Key][i].ID;
                        addCardList.Add(id, selectBonusCardList[bonus.Key][i]);
                        //重複してるボーナスを取り除く
                        foreach (var name in tmpBonusCardList)
                        {
                            if (bonus.Key == name.Key) continue;
                            var item = selectBonusCardList[name.Key].FirstOrDefault(b => b.ID == id);
                            if (item != null)
                            {
                                selectBonusCardList[name.Key].Remove(item);
                            }
                        }

                        lastCount--;
                    }
                }

                if (lastCount > 0)
                {
                    var lastCardList = dsDeckCard.DeckCard.Where(r => !addCardList.ContainsKey(r.ID) && !r.ダミー).OrderByDescending(r => r.攻援).Take(lastCount).Select(r => r.ID).ToList();
                    lastCardList.ForEach(id => addCardList.Add(id, idCardList[id]));
                }

                //予想発揮値計算
                List<string> mainIds = dsMainSelect.Card.Select(r => r.ID).ToList();
                List<string> selectSubIds = dsSubSelect.Card.Select(r => r.ID).ToList();
                List<string> addSubIds = addCardList.Select(c => c.Key).ToList();
                List<string> subIds = new List<string>();
                subIds.AddRange(addSubIds);
                subIds.AddRange(selectSubIds);
                Dictionary<string, PowerInfo> atkList;
                List<SelectionBonusInfo> selectionBonusInfo;
                BonusInfo bonusInfo;
                int currentAtk = CalcTotalAtk(mainIds, subIds, out atkList, out bonusInfo, out selectionBonusInfo);

                //最後のカードと最強を交換
                int tmpTargetAtk = currentAtk;
                List<string> targetAddSubId = new List<string>();
                List<SelectionBonusInfo> targetBonusInfo = new List<SelectionBonusInfo>();
                HashSet<string> mustIds = new HashSet<string>();
                foreach (string targetId in addSubIds)
                {
                    List<string> currentAddSubId = new List<string>(addSubIds);
                    while (true)
                    {
                        List<string> tmpAddSubIds = new List<string>(currentAddSubId);
                        if (addSubIds.Count == mustIds.Count)
                        {
                            break;
                        }
                        string removeId = idCardList.Where(c => tmpAddSubIds.Contains(c.Key) && !mustIds.Contains(c.Key)).OrderBy(c => c.Value.Power).First().Key;
                        var ordr = dsDeckCard.DeckCard.Where(r => !tmpAddSubIds.Contains(r.ID) && !r.ダミー).OrderByDescending(r => r.攻援);
                        if (ordr.Count() == 0) break;
                        string addId = ordr.First().ID;
                        tmpAddSubIds.Remove(removeId);
                        tmpAddSubIds.Add(addId);
                        List<string> tmpSubIds = new List<string>();
                        tmpSubIds.AddRange(selectSubIds);
                        tmpSubIds.AddRange(tmpAddSubIds);
                        Dictionary<string, PowerInfo> tmpAtkList;
                        List<SelectionBonusInfo> tmpSelectionBonusInfo;
                        BonusInfo tmpBonusInfo;

                        int tmpAtk = CalcTotalAtk(mainIds, tmpSubIds, out tmpAtkList, out bonusInfo, out tmpSelectionBonusInfo);
                        //入れ替えた結果が上回らなかった場合
                        mustIds.Add(removeId);
                        if (tmpAtk <= tmpTargetAtk)
                        {
                            continue;
                        }
                        mustIds.Clear();
                        currentAddSubId = tmpAddSubIds;
                        tmpTargetAtk = tmpAtk;
                        targetAddSubId = tmpAddSubIds;
                        targetBonusInfo = tmpSelectionBonusInfo;
                    }
                }
                if (currentAtk < tmpTargetAtk)
                {
                    foreach (string id in targetAddSubId)
                    {
                        AddSelect(id, dsSubSelect);
                    }
                }
                else
                {
                    foreach (string id in addSubIds)
                    {
                        AddSelect(id, dsSubSelect);
                    }
                }
                #endregion
            }
            else if (calcType == CalcType.守援)
            {
                #region 守援
                CreateDefSelectionBonusInfo();

                foreach (var cardRow in dsCards.Cards)
                {
                    CardInfo item = new CardInfo()
                    {
                        ID = cardRow.ID,
                        Name = cardRow.名前,
                        Cost = cardRow.コスト,
                        Power = cardRow.守援,
                    };
                    if (!idCardList.ContainsKey(cardRow.ID))
                    {
                        idCardList.Add(cardRow.ID, item);
                    }
                }

                foreach (string bonusName in DefBounus)
                {
                    tmpBonusCardList.Add(bonusName, new List<CardInfo>());
                    foreach (var cardRow in dsCards.Cards.Where(r => !r.ダミー))
                    {
                        DsGirls.GirlsRow girlRow = idGirlsList[cardRow.ID];

                        if (girlRow.守援1 != "-")
                        {
                            if (bonusName == girlRow.守援1)
                            {
                                tmpBonusCardList[bonusName].Add(idCardList[cardRow.ID]);
                            }
                        }
                        if (girlRow.守援2 != "-")
                        {
                            if (bonusName == girlRow.守援2)
                            {
                                tmpBonusCardList[bonusName].Add(idCardList[cardRow.ID]);
                            }
                        }
                        if (girlRow.守援3 != "-")
                        {
                            if (bonusName == girlRow.守援3)
                            {
                                tmpBonusCardList[bonusName].Add(idCardList[cardRow.ID]);
                            }
                        }
                    }
                }

                Dictionary<string, SelectionBonus> selectionBonusTotalList = new Dictionary<string, SelectionBonus>();
                Dictionary<string, List<CardInfo>> bonusCardList = new Dictionary<string, List<CardInfo>>();
                Dictionary<string, List<CardInfo>> selectBonusCardList = new Dictionary<string, List<CardInfo>>();

                foreach (var kvp in tmpBonusCardList)
                {
                    List<CardInfo> c = kvp.Value.OrderByDescending(r => r.Power).ToList();
                    bonusCardList.Add(kvp.Key, c);
                    selectBonusCardList.Add(kvp.Key, kvp.Value.OrderByDescending(r => r.Power).Take(5).ToList());
                    selectionBonusTotalList.Add(kvp.Key, new SelectionBonus()
                    {
                        TotalPower = kvp.Value.OrderByDescending(r => r.Power).Take(5).Sum(r => r.Power),
                        MaxCount = c.Count > 5 ? 5 : c.Count
                    });
                }

                //Mainを除外
                foreach (DsSelectCard.CardRow selectCard in dsMainSelect.Card)
                {
                    DsGirls.GirlsRow girlsRow = idGirlsList[selectCard.ID];
                    if (girlsRow.守援1 != "-")
                    {
                        selectionBonusTotalList[girlsRow.守援1].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.守援1].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.守援1].Remove(item);
                        }
                    }
                    if (girlsRow.守援2 != "-")
                    {
                        selectionBonusTotalList[girlsRow.守援2].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.守援2].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.守援2].Remove(item);
                        }
                    }
                    if (girlsRow.守援3 != "-")
                    {
                        selectionBonusTotalList[girlsRow.守援3].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.守援3].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.守援3].Remove(item);
                        }
                    }
                }
                foreach (DsSelectCard.CardRow selectCard in dsSubSelect.Card)
                {
                    DsGirls.GirlsRow girlsRow = idGirlsList[selectCard.ID];
                    if (girlsRow.守援1 != "-")
                    {
                        selectionBonusTotalList[girlsRow.守援1].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.守援1].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.守援1].Remove(item);
                        }
                    }
                    if (girlsRow.守援2 != "-")
                    {
                        selectionBonusTotalList[girlsRow.守援2].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.守援2].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.守援2].Remove(item);
                        }
                    }
                    if (girlsRow.守援3 != "-")
                    {
                        selectionBonusTotalList[girlsRow.守援3].CurrentCount++;
                        var item = selectBonusCardList[girlsRow.守援3].FirstOrDefault(b => b.ID == selectCard.ID);
                        if (item != null)
                        {
                            selectBonusCardList[girlsRow.守援3].Remove(item);
                        }
                    }
                }

                Dictionary<string, CardInfo> addCardList = new Dictionary<string, CardInfo>();

                int lastCount = subMax - dsSubSelect.Card.Count;
                foreach (var bonus in selectionBonusTotalList
                    .OrderByDescending(b => b.Value.MaxCount)
                    .ThenByDescending(b => b.Value.LastCount)
                    .ThenByDescending(b => b.Value.TotalPower).Take(5))
                {
                    if (lastCount <= 0) break;
                    for (int i = 0; i < selectBonusCardList[bonus.Key].Count; i++)
                    {
                        if (lastCount <= 0) break;

                        string id = selectBonusCardList[bonus.Key][i].ID;
                        addCardList.Add(id, selectBonusCardList[bonus.Key][i]);
                        //重複してるボーナスを取り除く
                        foreach (var name in tmpBonusCardList)
                        {
                            if (bonus.Key == name.Key) continue;
                            var item = selectBonusCardList[name.Key].FirstOrDefault(b => b.ID == id);
                            if (item != null)
                            {
                                selectBonusCardList[name.Key].Remove(item);
                            }
                        }

                        lastCount--;
                    }
                }

                if (lastCount > 0)
                {
                    var lastCardList = dsDeckCard.DeckCard.Where(r => !addCardList.ContainsKey(r.ID) && !r.ダミー).OrderByDescending(r => r.守援).Take(lastCount).Select(r => r.ID).ToList();
                    lastCardList.ForEach(id => addCardList.Add(id, idCardList[id]));
                }

                //予想発揮値計算
                List<string> mainIds = dsMainSelect.Card.Select(r => r.ID).ToList();
                List<string> selectSubIds = dsSubSelect.Card.Select(r => r.ID).ToList();
                List<string> addSubIds = addCardList.Select(c => c.Key).ToList();
                List<string> subIds = new List<string>();
                subIds.AddRange(addSubIds);
                subIds.AddRange(selectSubIds);
                Dictionary<string, PowerInfo> DefList;
                List<SelectionBonusInfo> selectionBonusInfo;
                BonusInfo bonusInfo;
                int currentDef = CalcTotalDef(mainIds, subIds, out DefList, out bonusInfo, out selectionBonusInfo);

                //最後のカードと最強を交換
                int tmpTargetDef = currentDef;
                List<string> targetAddSubId = new List<string>();
                List<SelectionBonusInfo> targetBonusInfo = new List<SelectionBonusInfo>();
                HashSet<string> mustIds = new HashSet<string>();
                foreach (string targetId in addSubIds)
                {
                    List<string> currentAddSubId = new List<string>(addSubIds);
                    while (true)
                    {
                        List<string> tmpAddSubIds = new List<string>(currentAddSubId);
                        if (addSubIds.Count == mustIds.Count)
                        {
                            break;
                        }
                        string removeId = idCardList.Where(c => tmpAddSubIds.Contains(c.Key) && !mustIds.Contains(c.Key)).OrderBy(c => c.Value.Power).First().Key;
                        var ordr = dsDeckCard.DeckCard.Where(r => !tmpAddSubIds.Contains(r.ID) && !r.ダミー).OrderByDescending(r => r.守援);
                        if (ordr.Count() == 0) break;
                        string addId = ordr.First().ID;
                        tmpAddSubIds.Remove(removeId);
                        tmpAddSubIds.Add(addId);
                        List<string> tmpSubIds = new List<string>();
                        tmpSubIds.AddRange(selectSubIds);
                        tmpSubIds.AddRange(tmpAddSubIds);
                        Dictionary<string, PowerInfo> tmpDefList;
                        List<SelectionBonusInfo> tmpSelectionBonusInfo;
                        BonusInfo tmpBonusInfo;
                        int tmpDef = CalcTotalDef(mainIds, tmpSubIds, out tmpDefList, out bonusInfo, out tmpSelectionBonusInfo);
                        //入れ替えた結果が上回らなかった場合
                        mustIds.Add(removeId);
                        if (tmpDef <= tmpTargetDef)
                        {
                            continue;
                        }
                        mustIds.Clear();
                        currentAddSubId = tmpAddSubIds;
                        tmpTargetDef = tmpDef;
                        targetAddSubId = tmpAddSubIds;
                        targetBonusInfo = tmpSelectionBonusInfo;
                    }
                }
                if (currentDef < tmpTargetDef)
                {
                    foreach (string id in targetAddSubId)
                    {
                        AddSelect(id, dsSubSelect);
                    }
                }
                else
                {
                    foreach (string id in addSubIds)
                    {
                        AddSelect(id, dsSubSelect);
                    }
                }
                #endregion
            }
            else if (calcType == CalcType.イベント)
            {
                #region イベント
                CreateAtkSelectionBonusInfo();

                //各ボーナスごとの最大値
                foreach (var cardRow in dsCards.Cards.Where(r => !r.ダミー))
                {
                    CardInfo item = new CardInfo()
                    {
                        ID = cardRow.ID,
                        Name = cardRow.名前,
                        Cost = cardRow.コスト,
                        Power = cardRow.攻援,
                    };
                    if (!idCardList.ContainsKey(cardRow.ID))
                    {
                        idCardList.Add(cardRow.ID, item);
                    }
                }

                Dictionary<string, CardInfo> addCardList = new Dictionary<string, CardInfo>();
                Dictionary<string, CardInfo> remainCardList = new Dictionary<string, CardInfo>();
                foreach (var card in idCardList)
                {
                    if (!dsMainSelect.Card.Any(r => r.ID == card.Key) &&
                        !dsSubSelect.Card.Any(r => r.ID == card.Key))
                    {
                        remainCardList.Add(card.Key, card.Value);
                    }
                }

                int lastCount = subMax - dsSubSelect.Card.Count;
                foreach (var card in remainCardList
                    .OrderByDescending(c => c.Value.Power))
                {
                    if (lastCount <= 0) break;
                    addCardList.Add(card.Key, card.Value);
                    lastCount--;
                }

                if (lastCount > 0)
                {
                    var lastCardList = dsDeckCard.DeckCard.Where(r => !addCardList.ContainsKey(r.ID) && !r.ダミー).OrderByDescending(r => r.攻援).Take(lastCount).Select(r => r.ID).ToList();
                    lastCardList.ForEach(id => addCardList.Add(id, idCardList[id]));
                }

                //予想発揮値計算
                List<string> mainIds = dsMainSelect.Card.Select(r => r.ID).ToList();
                List<string> selectSubIds = dsSubSelect.Card.Select(r => r.ID).ToList();
                List<string> addSubIds = addCardList.Select(c => c.Key).ToList();
                List<string> subIds = new List<string>();
                subIds.AddRange(addSubIds);
                subIds.AddRange(selectSubIds);
                Dictionary<string, PowerInfo> atkList;
                List<SelectionBonusInfo> selectionBonusInfo;
                BonusInfo bonusInfo;
                int currentAtk = CalcTotalEvent(mainIds, subIds, out atkList, out bonusInfo, out selectionBonusInfo);

                //最後のカードと最強を交換
                int tmpTargetAtk = currentAtk;
                List<string> targetAddSubId = new List<string>();
                List<SelectionBonusInfo> targetBonusInfo = new List<SelectionBonusInfo>();
                HashSet<string> mustIds = new HashSet<string>();
                foreach (string targetId in addSubIds)
                {
                    List<string> currentAddSubId = new List<string>(addSubIds);
                    while (true)
                    {
                        List<string> tmpAddSubIds = new List<string>(currentAddSubId);
                        if (addSubIds.Count == mustIds.Count)
                        {
                            break;
                        }
                        string removeId = idCardList.Where(c => tmpAddSubIds.Contains(c.Key) && !mustIds.Contains(c.Key)).OrderBy(c => c.Value.Power).First().Key;
                        var ordr = dsDeckCard.DeckCard.Where(r => !tmpAddSubIds.Contains(r.ID) && !r.ダミー).OrderByDescending(r => r.攻援);
                        if (ordr.Count() == 0) break;
                        string addId = ordr.First().ID;
                        tmpAddSubIds.Remove(removeId);
                        tmpAddSubIds.Add(addId);
                        List<string> tmpSubIds = new List<string>();
                        tmpSubIds.AddRange(selectSubIds);
                        tmpSubIds.AddRange(tmpAddSubIds);
                        Dictionary<string, PowerInfo> tmpAtkList;
                        List<SelectionBonusInfo> tmpSelectionBonusInfo;
                        BonusInfo tmpBonusInfo;

                        int tmpAtk = CalcTotalEvent(mainIds, tmpSubIds, out tmpAtkList, out bonusInfo, out tmpSelectionBonusInfo);
                        //入れ替えた結果が上回らなかった場合
                        mustIds.Add(removeId);
                        if (tmpAtk <= tmpTargetAtk)
                        {
                            continue;
                        }
                        mustIds.Clear();
                        currentAddSubId = tmpAddSubIds;
                        tmpTargetAtk = tmpAtk;
                        targetAddSubId = tmpAddSubIds;
                        targetBonusInfo = tmpSelectionBonusInfo;
                    }
                }
                if (currentAtk < tmpTargetAtk)
                {
                    foreach (string id in targetAddSubId)
                    {
                        AddSelect(id, dsSubSelect);
                    }
                }
                else
                {
                    foreach (string id in addSubIds)
                    {
                        AddSelect(id, dsSubSelect);
                    }
                }
                #endregion

            }

            SetSubInfo(dsSubSelect.Card.Count);
            ReCalcAll();
        }
        #endregion

        #region 自動計算(フル)
        /// <summary>
        /// 自動計算（攻援）
        /// </summary>
        /// <param name="param"></param>
        /// <param name="currentCost"></param>
        /// <param name="currentResut"></param>
        /// <param name="calcInfo"></param>
        /// <param name="currentCalcInfo"></param>
        /// <param name="addCard"></param>
        /// <param name="addIds"></param>
        /// <param name="remainCards"></param>
        /// <returns></returns>
        private AutoCalcResult AutoCalcAtk(AutoCalParam param, int currentCost, AutoCalcResult currentResut, ref AutoCalcInfo calcInfo, AutoCalcInfo currentCalcInfo, CardInfo addCard, List<string> addIds, List<CardInfo> remainCards)
        {
            AutoCalcResult result = currentResut;
            if (addIds.Count == subMax)
            {
                return currentResut;
            }
            int totalCost = currentCost + addCard.Cost;
            if (totalCost > param.MaxCost)
            {
                return currentResut;
            }
            int remainCost = param.MaxCost - totalCost;
            List<CardInfo> currentRemainCards = remainCards.Where(c => (c.Number > addCard.Number) && (c.Cost <= remainCost)).OrderBy(c => c.Number).ToList();
            List<string> currentAddIds = addIds;
            currentAddIds.Add(addCard.ID);
            currentCalcInfo.AddSubCalcInfo(addCard.ID, param.CardSelectionBonusInfo);

            //カード枠が埋まったか、残りカードがなくなった場合に計算を行う
            if (currentAddIds.Count == subMax || currentRemainCards.Count == 0)
            {
                int subTotal = calcInfo.Result;
                int currentSubTotal = currentCalcInfo.Result;
                //簡易比較して上回った場合に、本計算を行う
                if (currentSubTotal > subTotal)
                {
                    Dictionary<string, PowerInfo> atkList;
                    BonusInfo bonusInfo;
                    List<SelectionBonusInfo> selectionBonusInfo;
                    int atk = CalcTotalAtk(param.MainIds, currentAddIds, out atkList, out bonusInfo, out selectionBonusInfo);
                    if (atk > currentResut.Result)
                    {
                        result = new AutoCalcResult()
                        {
                            Result = atk,
                            SelectSubIds = new List<string>(currentAddIds),
                        };
                        calcInfo = currentCalcInfo.Copy();
                    }
                }
                currentAddIds.Remove(addCard.ID);
                currentCalcInfo.RemoveSubCalcInfo(addCard.ID, param.CardSelectionBonusInfo);
                return result;
            }
            if ((currentAddIds.Count + 2) == subMax)
            {
                if (currentCalcInfo.IsMaxSelectionBonus || currentRemainCards.First().Bonus == 0)
                {
                    //最強の2枚
                    CardInfo maxCard = null;
                    int maxNumber = 0;
                    for (int n = (addCard.Number + 1); n < currentRemainCards.Last().Number; n++)
                    {
                        if (param.AddCalcCard[n].ContainsKey(remainCost))
                        {
                            if (maxCard == null || maxCard.Power < param.AddCalcCard[n][remainCost].Power)
                            {
                                maxCard = param.AddCalcCard[n][remainCost];
                                maxNumber = n;
                            }
                        }
                        else
                        {
                            if (param.AddCalcCard[n].Count > 0)
                            {
                                var list = param.AddCalcCard[n].Where(c => c.Value.Cost <= remainCost).OrderByDescending(c => c.Value.Power).ToList();
                                if (list.Count > 0)
                                {
                                    if (maxCard == null || maxCard.Power < list[0].Value.Power)
                                    {
                                        maxCard = list[0].Value;
                                        maxNumber = n;
                                    }
                                }
                            }
                        }
                    }
                    if (maxNumber != 0)
                    {
                        CardInfo af = currentRemainCards.FirstOrDefault(c => c.Number == maxNumber);
                        CardInfo ac = currentRemainCards.FirstOrDefault(c => c.Number == maxCard.Number);
                        currentAddIds.Add(af.ID);
                        currentCalcInfo.AddSubCalcInfo(af.ID, param.CardSelectionBonusInfo);
                        totalCost += af.Cost;
                        result = AutoCalcAtk(param, totalCost, result, ref calcInfo, currentCalcInfo, ac, currentAddIds, currentRemainCards);
                        currentAddIds.Remove(af.ID);
                        currentCalcInfo.RemoveSubCalcInfo(af.ID, param.CardSelectionBonusInfo);
                    }
                }
                else
                {
                    foreach (CardInfo card in currentRemainCards)
                    {
                        if (this.commonResult.IsCancel) break; ;
                        result = AutoCalcAtk(param, totalCost, result, ref calcInfo, currentCalcInfo, card, currentAddIds, currentRemainCards);
                    }
                }

            }
            else
                if ((currentAddIds.Count + 1) == subMax)
            {
                if (currentCalcInfo.IsMaxSelectionBonus)
                {
                    //最強の一枚
                    CardInfo maxCard = currentRemainCards.OrderByDescending(c => c.Power).First();
                    result = AutoCalcAtk(param, totalCost, result, ref calcInfo, currentCalcInfo, maxCard, currentAddIds, currentRemainCards);
                }
                else if (currentRemainCards.Any(c => c.Bonus == 0))
                {
                    CardInfo nonBonusCard = currentRemainCards.Where(c => c.Bonus == 0).OrderByDescending(c => c.Power).First();
                    result = AutoCalcAtk(param, totalCost, result, ref calcInfo, currentCalcInfo, nonBonusCard, currentAddIds, currentRemainCards);
                    foreach (CardInfo card in currentRemainCards.Where(c => c.Bonus > 0))
                    {
                        if (this.commonResult.IsCancel) break;
                        result = AutoCalcAtk(param, totalCost, result, ref calcInfo, currentCalcInfo, card, currentAddIds, currentRemainCards);
                    }
                }
                else
                {
                    foreach (CardInfo card in currentRemainCards)
                    {
                        if (this.commonResult.IsCancel) break;
                        result = AutoCalcAtk(param, totalCost, result, ref calcInfo, currentCalcInfo, card, currentAddIds, currentRemainCards);
                    }
                }
            }
            else
            {
                if ((currentAddIds.Count + param.DispCount) == subMax)
                {
                    foreach (CardInfo card in currentRemainCards)
                    {
                        if (this.commonResult.IsCancel) break;
                        result = AutoCalcAtk(param, totalCost, result, ref calcInfo, currentCalcInfo, card, currentAddIds, currentRemainCards);
                        Interlocked.Decrement(ref commonResult.CalcNum);
                        if (commonResult.DispNum == 0)
                        {
                            Interlocked.Increment(ref commonResult.DispNum);
                            Dispatcher.BeginInvoke((Action)(() =>
                                {
                                    try
                                    {
                                        int calcNum = commonResult.CalcNum;
                                        if (calcNum < 0)
                                        {
                                            calcNum = 0;
                                        }
                                        if (commonResult.MaxCalcNum > 0)
                                        {
                                            LblAutoCalcRemain.Content = "残り：" + calcNum.ToString() + "/" + commonResult.MaxCalcNum.ToString();
                                            double per = ((double)(commonResult.MaxCalcNum - calcNum) / (double)commonResult.MaxCalcNum);
                                            double width = per * 200;
                                            if (width < 0) width = 0;
                                            if (width > 200) width = 200;
                                            BdrFullCalcRemain.Width = width;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                    }
                                    Thread.Sleep(40);
                                    Interlocked.Decrement(ref commonResult.DispNum);
                                }), System.Windows.Threading.DispatcherPriority.Input);
                        }
                    };

                }
                else
                {
                    foreach (CardInfo card in currentRemainCards)
                    {
                        if (this.commonResult.IsCancel) break;
                        result = AutoCalcAtk(param, totalCost, result, ref calcInfo, currentCalcInfo, card, currentAddIds, currentRemainCards);
                    }
                }
            }
            currentCalcInfo.RemoveSubCalcInfo(addCard.ID, param.CardSelectionBonusInfo);
            currentAddIds.Remove(addCard.ID);
            return result;
        }

        /// <summary>
        /// 自動計算（守援）
        /// </summary>
        /// <param name="param"></param>
        /// <param name="currentCost"></param>
        /// <param name="currentResut"></param>
        /// <param name="calcInfo"></param>
        /// <param name="currentCalcInfo"></param>
        /// <param name="addCard"></param>
        /// <param name="addIds"></param>
        /// <param name="remainCards"></param>
        /// <returns></returns>
        private AutoCalcResult AutoCalcDef(AutoCalParam param, int currentCost, AutoCalcResult currentResut, ref AutoCalcInfo calcInfo, AutoCalcInfo currentCalcInfo, CardInfo addCard, List<string> addIds, List<CardInfo> remainCards)
        {
            AutoCalcResult result = currentResut;
            if (addIds.Count == subMax)
            {
                return currentResut;
            }
            int totalCost = currentCost + addCard.Cost;
            if (totalCost > param.MaxCost)
            {
                return currentResut;
            }
            int remainCost = param.MaxCost - totalCost;
            List<CardInfo> currentRemainCards = remainCards.Where(c => (c.Number > addCard.Number) && (c.Cost <= remainCost)).OrderBy(c => c.Number).ToList();
            List<string> currentAddIds = addIds;
            currentAddIds.Add(addCard.ID);
            currentCalcInfo.AddSubCalcInfo(addCard.ID, param.CardSelectionBonusInfo);

            //カード枠が埋まったか、残りカードがなくなった場合に計算を行う
            if (currentAddIds.Count == subMax || currentRemainCards.Count == 0)
            {
                int subTotal = calcInfo.Result;
                int currentSubTotal = currentCalcInfo.Result;

                if (currentSubTotal > subTotal)
                {
                    Dictionary<string, PowerInfo> atkList;
                    BonusInfo bonusInfo;
                    List<SelectionBonusInfo> selectionBonusInfo;
                    int atk = CalcTotalDef(param.MainIds, currentAddIds, out atkList, out bonusInfo, out selectionBonusInfo);
                    if (atk > currentResut.Result)
                    {
                        result = new AutoCalcResult()
                        {
                            Result = atk,
                            SelectSubIds = new List<string>(currentAddIds),
                        };
                        calcInfo = currentCalcInfo.Copy();
                    }
                }
                currentCalcInfo.RemoveSubCalcInfo(addCard.ID, param.CardSelectionBonusInfo);
                currentAddIds.Remove(addCard.ID);
                return result;
            }
            //残り２枚になった場合
            if ((currentAddIds.Count + 2) == subMax)
            {
                //選抜ボーナスが埋まったか、残りがボーナスなしの場合は最強の２枚を選択する
                if (currentCalcInfo.IsMaxSelectionBonus || currentRemainCards.First().Bonus == 0)
                {
                    //最強の2枚
                    CardInfo maxCard = null;
                    int maxNumber = 0;
                    for (int n = (addCard.Number + 1); n < currentRemainCards.Last().Number; n++)
                    {
                        if (param.AddCalcCard[n].ContainsKey(remainCost))
                        {
                            if (maxCard == null || maxCard.Power < param.AddCalcCard[n][remainCost].Power)
                            {
                                maxCard = param.AddCalcCard[n][remainCost];
                                maxNumber = n;
                            }
                        }
                        else
                        {
                            if (param.AddCalcCard[n].Count > 0)
                            {
                                var list = param.AddCalcCard[n].Where(c => c.Value.Cost <= remainCost).OrderByDescending(c => c.Value.Power).ToList();
                                if (list.Count > 0)
                                {
                                    if (maxCard == null || maxCard.Power < list[0].Value.Power)
                                    {
                                        maxCard = list[0].Value;
                                        maxNumber = n;
                                    }
                                }
                            }
                        }
                    }
                    if (maxNumber != 0)
                    {
                        CardInfo af = currentRemainCards.FirstOrDefault(c => c.Number == maxNumber);
                        CardInfo ac = currentRemainCards.FirstOrDefault(c => c.Number == maxCard.Number);
                        currentAddIds.Add(af.ID);
                        currentCalcInfo.AddSubCalcInfo(af.ID, param.CardSelectionBonusInfo);
                        totalCost += af.Cost;
                        result = AutoCalcDef(param, totalCost, result, ref calcInfo, currentCalcInfo, ac, currentAddIds, currentRemainCards);
                        currentAddIds.Remove(af.ID);
                        currentCalcInfo.RemoveSubCalcInfo(af.ID, param.CardSelectionBonusInfo);
                    }
                }
                else
                {
                    foreach (CardInfo card in currentRemainCards)
                    {
                        if (this.commonResult.IsCancel) break; ;
                        result = AutoCalcDef(param, totalCost, result, ref calcInfo, currentCalcInfo, card, currentAddIds, currentRemainCards);
                    }
                }

            }
            else if ((currentAddIds.Count + 1) == subMax)
            {
                if (currentCalcInfo.IsMaxSelectionBonus)
                {
                    //最強の一枚
                    CardInfo maxCard = currentRemainCards.OrderByDescending(c => c.Power).First();
                    result = AutoCalcDef(param, totalCost, result, ref calcInfo, currentCalcInfo, maxCard, currentAddIds, currentRemainCards);
                }
                else if (currentRemainCards.Any(c => c.Bonus == 0))
                {
                    CardInfo nonBonusCard = currentRemainCards.Where(c => c.Bonus == 0).OrderByDescending(c => c.Power).First();
                    result = AutoCalcDef(param, totalCost, result, ref calcInfo, currentCalcInfo, nonBonusCard, currentAddIds, currentRemainCards);
                    foreach (CardInfo card in currentRemainCards.Where(c => c.Bonus > 0))
                    {
                        if (this.commonResult.IsCancel) break;
                        result = AutoCalcDef(param, totalCost, result, ref calcInfo, currentCalcInfo, card, currentAddIds, currentRemainCards);
                    }

                }
                else
                {
                    foreach (CardInfo card in currentRemainCards)
                    {
                        if (this.commonResult.IsCancel) break;
                        result = AutoCalcDef(param, totalCost, result, ref calcInfo, currentCalcInfo, card, currentAddIds, currentRemainCards);
                    }
                }
            }
            else
            {
                if ((currentAddIds.Count + param.DispCount) == subMax)
                {
                    foreach (CardInfo card in currentRemainCards)
                    {
                        if (this.commonResult.IsCancel) break;
                        result = AutoCalcDef(param, totalCost, result, ref calcInfo, currentCalcInfo, card, currentAddIds, currentRemainCards);
                        Interlocked.Decrement(ref commonResult.CalcNum);
                        if (commonResult.DispNum == 0)
                        {
                            Interlocked.Increment(ref commonResult.DispNum);
                            Dispatcher.BeginInvoke((Action)(() =>
                            {
                                try
                                {
                                    int calcNum = commonResult.CalcNum;
                                    if (calcNum < 0)
                                    {
                                        calcNum = 0;
                                    }
                                    if (commonResult.MaxCalcNum > 0)
                                    {
                                        LblAutoCalcRemain.Content = "残り：" + calcNum.ToString() + "/" + commonResult.MaxCalcNum.ToString();
                                        double per = ((double)(commonResult.MaxCalcNum - calcNum) / (double)commonResult.MaxCalcNum);
                                        double width = per * 200;
                                        if (width < 0) width = 0;
                                        if (width > 200) width = 200;
                                        BdrFullCalcRemain.Width = width;
                                    }
                                }
                                catch (Exception)
                                {
                                }
                                Thread.Sleep(40);
                                Interlocked.Decrement(ref commonResult.DispNum);
                            }), System.Windows.Threading.DispatcherPriority.Input);
                        }
                    };

                }
                else
                {
                    foreach (CardInfo card in currentRemainCards)
                    {
                        if (this.commonResult.IsCancel) break;
                        result = AutoCalcDef(param, totalCost, result, ref calcInfo, currentCalcInfo, card, currentAddIds, currentRemainCards);
                    }
                }
            }
            currentCalcInfo.RemoveSubCalcInfo(addCard.ID, param.CardSelectionBonusInfo);
            currentAddIds.Remove(addCard.ID);
            return result;
        }

        private static void AddBonus(Dictionary<string, int> bonus, string id, Dictionary<string, CardSelectionBonusInfo> cardSelectionBonusInfo)
        {
            cardSelectionBonusInfo[id].Bonus.ForEach(b => bonus[b] = bonus[b] + 1);
        }

        /// <summary>
        /// Full計算ボタンイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSubFullAuto_Click(object sender, RoutedEventArgs e)
        {
            if (IsDeckLock())
            {
                DialogWindow.Show(this, "デッキがロックされているため編集できません", DialogWindow.MessageType.Error);
                return;
            }
            if (!DialogWindow.Show(this, "時間がかなりかかる可能性がありますがよろしいですか？", "確認", DialogWindow.MessageType.Confirm))
            {
                return;
            }

            Dictionary<string, DsGirls.GirlsRow> idGirlsList = new Dictionary<string, DsGirls.GirlsRow>();
            foreach (var cardRow in dsCards.Cards)
            {
                DsGirls.GirlsRow girlRow = dsGilrs.Girls.FirstOrDefault(r => r.名前 == cardRow.名前);
                idGirlsList.Add(cardRow.ID, girlRow);
            }

            Dictionary<string, CardInfo> idCardList = new Dictionary<string, CardInfo>();

            if (calcType == CalcType.攻援)
            {
                #region 攻援
                foreach (var cardRow in dsCards.Cards)
                {
                    CardInfo item = new CardInfo()
                    {
                        ID = cardRow.ID,
                        Name = cardRow.名前,
                        Cost = cardRow.コスト,
                        Power = cardRow.攻援,
                    };
                    if (!idCardList.ContainsKey(cardRow.ID))
                    {
                        idCardList.Add(cardRow.ID, item);
                    }
                }
                CreateAtkSelectionBonusInfo();
                Dictionary<string, CardSelectionBonusInfo> cardSelectionBonusInfo = new Dictionary<string, CardSelectionBonusInfo>();
                foreach (var idc in idCardList)
                {
                    CardSelectionBonusInfo csbi = new CardSelectionBonusInfo();
                    csbi.ID = idc.Key;
                    csbi.Cost = idc.Value.Cost;
                    csbi.Power = idc.Value.Power;
                    csbi.Bonus = new List<string>();
                    var girlsBonusList = girlsSelectionBonusList[idc.Value.Name];
                    foreach (string bonusName in girlsBonusList.AtkBonus)
                    {
                        csbi.Bonus.Add(bonusName);
                    }
                    cardSelectionBonusInfo.Add(idc.Key, csbi);
                }
                //予想発揮値計算
                List<string> mainIds = dsMainSelect.Card.Select(r => r.ID).ToList();
                List<string> subIds = dsSubSelect.Card.Select(r => r.ID).ToList();
                Dictionary<string, CardInfo> remainCards = new Dictionary<string, CardInfo>();
                foreach (var kvp in idCardList)
                {
                    remainCards.Add(kvp.Key, kvp.Value);
                }

                AutoCalcInfo calcInfo = new AutoCalcInfo();
                calcInfo.MainTotal = cardSelectionBonusInfo.Where(id => mainIds.Contains(id.Key)).Sum(cbi => cbi.Value.Power);
                calcInfo.SubTotal = cardSelectionBonusInfo.Where(id => subIds.Contains(id.Key)).Sum(cbi => cbi.Value.Power);
                calcInfo.SelectionBonus = new Dictionary<string, int>();
                foreach (string bonus in AtkBounus)
                {
                    calcInfo.SelectionBonus.Add(bonus, 0);
                }
                mainIds.ForEach(id => AddBonus(calcInfo.SelectionBonus, id, cardSelectionBonusInfo));
                subIds.ForEach(id => AddBonus(calcInfo.SelectionBonus, id, cardSelectionBonusInfo));

                //Mainを除外
                foreach (string id in mainIds)
                {
                    remainCards.Remove(id);
                }
                //サブを除外
                foreach (string id in subIds)
                {
                    remainCards.Remove(id);
                }
                //ダミーを除外
                foreach (var row in dsCards.Cards.Where(r => r.ダミー))
                {
                    remainCards.Remove(row.ID);
                }

                int totalCost = dsMainSelect.Card.Sum(r => r.コスト) + dsSubSelect.Card.Sum(r => r.コスト);
                AutoCalcResult result = new AutoCalcResult()
                {
                    Result = 0,
                    SelectSubIds = new List<string>(subIds),
                };

                //連番
                int num = 0;
                foreach (var kvp in remainCards.
                    OrderByDescending(c => cardSelectionBonusInfo[c.Key].Bonus.Count).
                    ThenByDescending(c => c.Value.Cost).
                    ThenByDescending(c => c.Value.Power))
                {
                    kvp.Value.Number = num++;
                    kvp.Value.Bonus = cardSelectionBonusInfo[kvp.Key].Bonus.Count;
                }

                Dictionary<int, Dictionary<int, CardInfo>> addCalcCard = new Dictionary<int, Dictionary<int, CardInfo>>();
                if (remainCards.Count > 2)
                {
                    for (int i = 0; i < (remainCards.Count - 1); i++)
                    {
                        CardInfo ci = remainCards.FirstOrDefault(c => c.Value.Number == i).Value;
                        addCalcCard.Add(i, new Dictionary<int, CardInfo>());
                        int imaxCost = ci.Cost + remainCards.Where(c => c.Value.Number > i).Max(c => c.Value.Cost);
                        for (int j = (i + 1); j < remainCards.Count; j++)
                        {
                            CardInfo cj = remainCards.FirstOrDefault(c => c.Value.Number == j).Value;

                            CardInfo v = new CardInfo()
                            {
                                Cost = ci.Cost + cj.Cost,
                                Power = ci.Power + cj.Power,
                                Number = cj.Number,
                            };
                            for (int cost = v.Cost; cost <= imaxCost; cost++)
                            {
                                if (!addCalcCard[i].ContainsKey(cost))
                                {
                                    addCalcCard[i].Add(cost, v);
                                }
                                else if (addCalcCard[i][cost].Power < v.Power)
                                {
                                    addCalcCard[i][cost] = v;
                                }
                            }
                        }
                    }
                }

                List<CardInfo> remainCardList = remainCards.OrderBy(c => c.Value.Number).Select(c => c.Value).ToList();
                List<CardInfo> calcRemainCards = remainCards.OrderBy(c => c.Value.Number).Select(c => c.Value).ToList();
                List<CalcThreadResult> results = new List<CalcThreadResult>();
                int threadNum = (int)Math.Ceiling((double)System.Environment.ProcessorCount * 1.5d);

                decimal tmpCalcNum = 0;
                int calcNum = 0;
                int lastNum = subMax - dsSubSelect.Card.Count;
                int dispCount = 5;
                if (lastNum > dispCount && remainCardList.Count > dispCount)
                {
                    tmpCalcNum = calcRemainCards.Count;
                    for (int i = 0; i < (lastNum - dispCount); i++)
                    {
                        decimal t = tmpCalcNum * (remainCardList.Count - 1 - i) / (i + 2);
                        if (t > int.MaxValue)
                        {
                            dispCount = lastNum - i;
                            break;
                        }
                        else
                        {
                            tmpCalcNum = t;
                        }
                    }
                    calcNum = (int)tmpCalcNum;
                }

                CalcCommonResult commonResult = new CalcCommonResult()
                {
                    CalcCards = calcRemainCards,
                    MaxThread = threadNum,
                    RemainThread = threadNum,
                    Results = results,
                    IsCancel = false,
                    CalcNum = calcNum,
                    MaxCalcNum = calcNum,
                    DispNum = 0,
                };
                this.commonResult = commonResult;

                if (calcNum > 0)
                {
                    LblAutoCalcRemain.Content = "残り：" + commonResult.CalcNum.ToString() + "/" + commonResult.MaxCalcNum.ToString();
                    BdrFullCalcRemain.Width = ((commonResult.MaxCalcNum - commonResult.CalcNum) * 200 / commonResult.MaxCalcNum);
                }
                for (int i = 0; i < threadNum; i++)
                {
                    results.Add(new CalcThreadResult()
                    {
                        Result = result,
                        Common = commonResult,
                        CalcInfo = calcInfo.Copy(),
                    });

                }

                AutoCalParam param = new AutoCalParam()
                {
                    AddCalcCard = addCalcCard,
                    CardSelectionBonusInfo = cardSelectionBonusInfo,
                    DispCount = dispCount,
                    MaxCost = costMax,
                    SubMax = subMax,
                    IdCardList = idCardList,
                    MainIds = mainIds,
                };

                for (int i = 0; i < threadNum; i++)
                {
                    Thread t = new Thread(new ParameterizedThreadStart((o)
                    =>
                    {
                        CalcThreadResult ctr = o as CalcThreadResult;
                        AutoCalcResult acr = ctr.Result;
                        AutoCalcInfo aci = ctr.CalcInfo;
                        AutoCalcInfo currentCalcInfo = ctr.CalcInfo.Copy();
                        while (true)
                        {
                            if (this.commonResult.IsCancel) break;
                            CardInfo card = ctr.Common.GetCalcCard();
                            if (card == null) break;
                            acr = AutoCalcAtk(param, totalCost, acr, ref aci, currentCalcInfo.Copy(), card, new List<string>(subIds), remainCardList);
                        }
                        if (this.commonResult.IsCancel)
                        {
                            Interlocked.Decrement(ref ctr.Common.RemainThread);
                            if (ctr.Common.RemainThread == 0)
                            {
                                Dispatcher.BeginInvoke((Action)(() =>
                                 {
                                     BdrLock.Visibility = Visibility.Collapsed;
                                 }));
                            }
                        }
                        else
                        {
                            ctr.Result = acr;
                            Interlocked.Decrement(ref ctr.Common.RemainThread);
                            if (ctr.Common.RemainThread == 0)
                            {
                                Dispatcher.BeginInvoke((Action)(() =>
                                    {
                                        try
                                        {
                                            AutoCalcResult lastResult = results.OrderByDescending(r => r.Result.Result).First().Result;

                                            foreach (string id in lastResult.SelectSubIds)
                                            {
                                                if (!subIds.Contains(id))
                                                {
                                                    AddSelect(id, dsSubSelect);
                                                }
                                            }

                                            SetSubInfo(dsSubSelect.Card.Count);
                                            ReCalcAll();

                                            BdrLock.Visibility = Visibility.Collapsed;
                                        }
                                        catch (Exception ex)
                                        {
                                            BdrLock.Visibility = Visibility.Collapsed;
                                        }
                                    }));
                            }
                        }
                    }));
                    t.Start(results[i]);
                    if (i < 2)
                    {
                        t.Priority = ThreadPriority.AboveNormal;
                    }
                }
                #endregion
            }
            else if (calcType == CalcType.守援)
            {
                #region 守援
                foreach (var cardRow in dsCards.Cards)
                {
                    CardInfo item = new CardInfo()
                    {
                        ID = cardRow.ID,
                        Name = cardRow.名前,
                        Cost = cardRow.コスト,
                        Power = cardRow.守援,
                    };
                    if (!idCardList.ContainsKey(cardRow.ID))
                    {
                        idCardList.Add(cardRow.ID, item);
                    }
                }
                CreateDefSelectionBonusInfo();
                Dictionary<string, CardSelectionBonusInfo> cardSelectionBonusInfo = new Dictionary<string, CardSelectionBonusInfo>();
                foreach (var idc in idCardList)
                {
                    CardSelectionBonusInfo csbi = new CardSelectionBonusInfo();
                    csbi.ID = idc.Key;
                    csbi.Cost = idc.Value.Cost;
                    csbi.Power = idc.Value.Power;
                    csbi.Bonus = new List<string>();
                    var girlsBonusList = girlsSelectionBonusList[idc.Value.Name];
                    foreach (string bonusName in girlsBonusList.DefBonus)
                    {
                        csbi.Bonus.Add(bonusName);
                    }

                    cardSelectionBonusInfo.Add(idc.Key, csbi);
                }
                //予想発揮値計算
                List<string> mainIds = dsMainSelect.Card.Select(r => r.ID).ToList();
                List<string> subIds = dsSubSelect.Card.Select(r => r.ID).ToList();
                Dictionary<string, CardInfo> remainCards = new Dictionary<string, CardInfo>();
                foreach (var kvp in idCardList)
                {
                    remainCards.Add(kvp.Key, kvp.Value);
                }

                AutoCalcInfo calcInfo = new AutoCalcInfo();
                calcInfo.MainTotal = cardSelectionBonusInfo.Where(id => mainIds.Contains(id.Key)).Sum(cbi => cbi.Value.Power);
                calcInfo.SubTotal = cardSelectionBonusInfo.Where(id => subIds.Contains(id.Key)).Sum(cbi => cbi.Value.Power);
                calcInfo.SelectionBonus = new Dictionary<string, int>();
                foreach (string bonus in DefBounus)
                {
                    calcInfo.SelectionBonus.Add(bonus, 0);
                }
                mainIds.ForEach(id => AddBonus(calcInfo.SelectionBonus, id, cardSelectionBonusInfo));
                subIds.ForEach(id => AddBonus(calcInfo.SelectionBonus, id, cardSelectionBonusInfo));

                foreach (string id in mainIds)
                {
                    remainCards.Remove(id);
                }
                //サブを除外
                foreach (string id in subIds)
                {
                    remainCards.Remove(id);
                }
                //ダミーを除外
                foreach (var row in dsCards.Cards.Where(r => r.ダミー))
                {
                    remainCards.Remove(row.ID);
                }

                int totalCost = dsMainSelect.Card.Sum(r => r.コスト) + dsSubSelect.Card.Sum(r => r.コスト);
                AutoCalcResult result = new AutoCalcResult()
                {
                    Result = 0,
                    SelectSubIds = new List<string>(subIds),
                };

                //連番
                int num = 0;
                foreach (var kvp in remainCards.
                    OrderByDescending(c => cardSelectionBonusInfo[c.Key].Bonus.Count).
                    ThenByDescending(c => c.Value.Cost).
                    ThenByDescending(c => c.Value.Power))
                {
                    kvp.Value.Number = num++;
                    kvp.Value.Bonus = cardSelectionBonusInfo[kvp.Key].Bonus.Count;
                }
                Dictionary<int, Dictionary<int, CardInfo>> addCalcCard = new Dictionary<int, Dictionary<int, CardInfo>>();
                if (remainCards.Count > 2)
                {
                    for (int i = 0; i < (remainCards.Count - 1); i++)
                    {
                        CardInfo ci = remainCards.FirstOrDefault(c => c.Value.Number == i).Value;
                        addCalcCard.Add(i, new Dictionary<int, CardInfo>());
                        int imaxCost = ci.Cost + remainCards.Where(c => c.Value.Number > i).Max(c => c.Value.Cost);
                        for (int j = (i + 1); j < remainCards.Count; j++)
                        {
                            CardInfo cj = remainCards.FirstOrDefault(c => c.Value.Number == j).Value;

                            CardInfo v = new CardInfo()
                            {
                                Cost = ci.Cost + cj.Cost,
                                Power = ci.Power + cj.Power,
                                Number = cj.Number,
                            };
                            for (int cost = v.Cost; cost <= imaxCost; cost++)
                            {
                                if (!addCalcCard[i].ContainsKey(cost))
                                {
                                    addCalcCard[i].Add(cost, v);
                                }
                                else if (addCalcCard[i][cost].Power < v.Power)
                                {
                                    addCalcCard[i][cost] = v;
                                }
                            }
                        }
                    }
                }


                List<CardInfo> remainCardList = remainCards.OrderBy(c => c.Value.Number).Select(c => c.Value).ToList();
                List<CardInfo> calcRemainCards = remainCards.OrderBy(c => c.Value.Number).Select(c => c.Value).ToList();

                List<CalcThreadResult> results = new List<CalcThreadResult>();
                int threadNum = (int)Math.Ceiling((double)System.Environment.ProcessorCount * 1.5d);

                decimal tmpCalcNum = 0;
                int calcNum = 0;
                int lastNum = subMax - dsSubSelect.Card.Count;
                int dispCount = 5;
                if (lastNum > dispCount && remainCardList.Count > dispCount)
                {
                    tmpCalcNum = calcRemainCards.Count;
                    for (int i = 0; i < (lastNum - dispCount); i++)
                    {
                        decimal t = tmpCalcNum * (remainCardList.Count - 1 - i) / (i + 2);
                        if (t > int.MaxValue)
                        {
                            dispCount = lastNum - i;
                            break;
                        }
                        else
                        {
                            tmpCalcNum = t;
                        }
                    }
                    calcNum = (int)tmpCalcNum;
                }

                CalcCommonResult commonResult = new CalcCommonResult()
                {
                    CalcCards = calcRemainCards,
                    MaxThread = threadNum,
                    RemainThread = threadNum,
                    Results = results,
                    IsCancel = false,
                    MaxCalcNum = calcNum,
                    CalcNum = calcNum,
                    DispNum = 0,
                };
                this.commonResult = commonResult;
                if (calcNum > 0)
                {

                    LblAutoCalcRemain.Content = "残り：" + commonResult.CalcNum.ToString() + "/" + commonResult.MaxCalcNum.ToString();
                    BdrFullCalcRemain.Width = ((commonResult.MaxCalcNum - commonResult.CalcNum) * 200 / commonResult.MaxCalcNum);
                }

                for (int i = 0; i < threadNum; i++)
                {
                    results.Add(new CalcThreadResult()
                    {
                        Result = result,
                        Common = commonResult,
                        CalcInfo = calcInfo.Copy(),
                    });

                }

                AutoCalParam param = new AutoCalParam()
                {
                    AddCalcCard = addCalcCard,
                    CardSelectionBonusInfo = cardSelectionBonusInfo,
                    DispCount = dispCount,
                    MaxCost = costMax,
                    SubMax = subMax,
                    IdCardList = idCardList,
                    MainIds = mainIds,
                };

                for (int i = 0; i < threadNum; i++)
                {
                    Thread t = new Thread(new ParameterizedThreadStart((o)
                    =>
                    {
                        CalcThreadResult ctr = o as CalcThreadResult;
                        AutoCalcResult acr = ctr.Result;
                        AutoCalcInfo aci = ctr.CalcInfo;
                        AutoCalcInfo currentCalcInfo = ctr.CalcInfo.Copy();
                        while (true)
                        {
                            if (this.commonResult.IsCancel) break;
                            CardInfo card = ctr.Common.GetCalcCard();
                            if (card == null) break;
                            acr = AutoCalcDef(param, totalCost, acr, ref aci, currentCalcInfo.Copy(), card, new List<string>(subIds), remainCardList);
                        }
                        if (this.commonResult.IsCancel)
                        {
                            Interlocked.Decrement(ref ctr.Common.RemainThread);
                            if (ctr.Common.RemainThread == 0)
                            {
                                Dispatcher.BeginInvoke((Action)(() =>
                                {
                                    BdrLock.Visibility = Visibility.Collapsed;
                                }));
                            }
                        }
                        else
                        {
                            ctr.Result = acr;
                            Interlocked.Decrement(ref ctr.Common.RemainThread);
                            if (ctr.Common.RemainThread == 0)
                            {
                                Dispatcher.BeginInvoke((Action)(() =>
                                {
                                    try
                                    {
                                        AutoCalcResult lastResult = results.OrderByDescending(r => r.Result.Result).First().Result;

                                        foreach (string id in lastResult.SelectSubIds)
                                        {
                                            if (!subIds.Contains(id))
                                            {
                                                AddSelect(id, dsSubSelect);
                                            }
                                        }

                                        SetSubInfo(dsSubSelect.Card.Count);
                                        ReCalcAll();

                                        BdrLock.Visibility = Visibility.Collapsed;
                                    }
                                    catch (Exception ex)
                                    {
                                        BdrLock.Visibility = Visibility.Collapsed;
                                    }
                                }));
                            }
                        }
                    }));
                    t.Start(results[i]);
                }
                #endregion
            }
            else if (calcType == CalcType.イベント)
            {
                return;
            }

            //画面ロック
            BdrLock.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 自動計算キャンセル
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnAutoCalcCansel_Click(object sender, RoutedEventArgs e)
        {
            this.commonResult.IsCancel = true;
        }

        #endregion

        #region デッキ読み込み・書き込み
        DsUserDeck SaveDeck(string deckName, CalcType calcType)
        {
            DsUserDeck userDeck = new DsUserDeck();
            DsUserDeck.デッキ情報Row deckInfo = userDeck.デッキ情報.Newデッキ情報Row();
            deckInfo.名前 = deckName;
            switch (calcType)
            {
                case CalcType.攻援: deckInfo.デッキタイプ = "攻援"; break;
                case CalcType.守援: deckInfo.デッキタイプ = "守援"; break;
                case CalcType.イベント: deckInfo.デッキタイプ = "イベント"; break;
            }
            deckInfo.コスト = currentCost;
            deckInfo.総発揮値 = currentPower;
            //主選抜
            foreach (DsSelectCard.CardRow mainCard in dsMainSelect.Card)
            {
                DsUserDeck.MainDeckRow mainDeckRow = userDeck.MainDeck.NewMainDeckRow();
                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == mainCard.ID);
                mainDeckRow.ID = mainCard.ID;
                mainDeckRow.名前 = mainCard.名前;
                mainDeckRow.コスト = mainCard.コスト;
                mainDeckRow.スキル = mainCard.スキル;
                mainDeckRow.属性 = mainCard.属性;
                mainDeckRow.攻援 = cardRow.攻援;
                mainDeckRow.守援 = cardRow.守援;
                userDeck.MainDeck.AddMainDeckRow(mainDeckRow);
            }
            //副選抜
            foreach (DsSelectCard.CardRow subCard in dsSubSelect.Card)
            {
                DsUserDeck.SubDeckRow subDeckRow = userDeck.SubDeck.NewSubDeckRow();
                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == subCard.ID);
                subDeckRow.ID = subCard.ID;
                subDeckRow.名前 = subCard.名前;
                subDeckRow.コスト = subCard.コスト;
                subDeckRow.スキル = subCard.スキル;
                subDeckRow.属性 = subCard.属性;
                subDeckRow.攻援 = cardRow.攻援;
                subDeckRow.守援 = cardRow.守援;
                userDeck.SubDeck.AddSubDeckRow(subDeckRow);
            }
            return userDeck;
        }

        void LoadDeck(DsUserDeck deck)
        {
            foreach (DsSelectCard.CardRow selectCardRow in dsMainSelect.Card)
            {
                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == selectCardRow.ID);
                CreateDeckCardRow(cardRow);
            }
            foreach (DsSelectCard.CardRow selectCardRow in dsSubSelect.Card)
            {
                DsCards.CardsRow cardRow = dsCards.Cards.FirstOrDefault(r => r.ID == selectCardRow.ID);
                CreateDeckCardRow(cardRow);
            }

            dsMainSelect.Card.Clear();
            dsSubSelect.Card.Clear();
            foreach (DsUserDeck.MainDeckRow mainDeckRow in deck.MainDeck)
            {
                string id = mainDeckRow.ID;
                AddSelect(id, dsMainSelect);
            }
            foreach (DsUserDeck.SubDeckRow subDeckRow in deck.SubDeck)
            {
                string id = subDeckRow.ID;
                AddSelect(id, dsSubSelect);
            }
            dsDeckCard.AcceptChanges();
        }

        void LoadCmpDeck(DsUserDeck deck, DsSelectCard dsCmpMain, DsSelectCard dsCmpSub)
        {
            foreach (DsUserDeck.MainDeckRow mainDeckRow in deck.MainDeck)
            {
                string id = mainDeckRow.ID;
                AddCmpSelect(id, dsCmpMain);
            }
            foreach (DsUserDeck.SubDeckRow subDeckRow in deck.SubDeck)
            {
                string id = subDeckRow.ID;
                AddCmpSelect(id, dsCmpSub);
            }
        }

        #endregion

        #region デッキ
        /// <summary>
        /// デッキ読み込み
        /// </summary>
        /// <param name="calc"></param>
        /// <param name="loadNumber"></param>
        private void LoadDeckNumber(CalcType calc, int loadNumber)
        {
            DsDeckInfo.DeckInfoRow loadRow = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == loadNumber && r.Type == calcType.ToString());
            DsUserDeck loadDeck = new DsUserDeck();
            string loadFileName = GetDeckNumberName(calc, loadNumber);
            if (File.Exists(loadFileName))
            {
                loadDeck.ReadXml(loadFileName);
            }
            LoadDeck(loadDeck);
            if (loadRow.IsLockNull())
            {
                loadRow.Lock = false;
            }
            ChkDeckLock.IsChecked = loadRow.Lock;
            TxtDeckName.Text = loadRow.Name;
            ChkDeckCostLimited.IsChecked = loadRow.IsCostLimited;
            TxtDeckLimitedCost.Text = loadRow.LimitedCost.ToString();
            isEvent = false;
            CmbDeckDisplay.SelectedValue = loadRow.DisplayIndex;
            isEvent = true;

            //最大コスト設定
            if (loadRow.IsCostLimited)
            {
                costMax = loadRow.LimitedCost;
            }
            else
            {
                costMax = GetCostMax();
            }

            LblMainInfo.Content = dsMainSelect.Card.Count.ToString();
            SetSubInfo(dsSubSelect.Card.Count);

            ReCalcAll();
        }

        /// <summary>
        /// デッキ保存
        /// </summary>
        /// <param name="calc"></param>
        /// <param name="currentNumber"></param>
        private void SaveDeckNumber(CalcType calc, int currentNumber)
        {
            DsDeckInfo.DeckInfoRow saveRow = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == currentNumber && r.Type == calc.ToString());
            DsUserDeck saveDeck = SaveDeck(saveRow.Name, calc);
            saveDeck.WriteXml(GetDeckNumberName(calc, currentNumber));
            DsDeckInfo.DeckInfoRow deckInfoRow = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == currentNumber && r.Type == calc.ToString());
            if (deckInfoRow != null)
            {
                deckInfoRow.Power = currentPower;
                deckInfoRow.BasePower = currentBasePower;
                deckInfoRow.Cost = currentCost;
                deckInfoRow.MainCount = dsMainSelect.Card.Count;
                deckInfoRow.SubCount = dsSubSelect.Card.Count;
                deckInfo.WriteXml(Utility.GetFilePath("deckinfo.xml"));
            }
        }

        private string GetDeckNumberName(CalcType calc, int number)
        {
            return Path.Combine(Utility.GetFilePath("Deck"), calcName[calc] + "Deck_" + number.ToString() + ".xml");
        }

        /// <summary>
        /// Deckロック判定
        /// </summary>
        /// <returns></returns>
        private bool IsDeckLock()
        {
            int currentNumber = (int)CmbDeck.SelectedValue;
            DsDeckInfo.DeckInfoRow row = deckInfo.DeckInfo.FirstOrDefault(r => r.Number == currentNumber && r.Type == calcType.ToString());
            if (row != null)
            {
                return row.Lock;
            }
            return false;
        }

        /// <summary>
        /// Deck表示順コンボボックス作成
        /// </summary>
        /// <param name="calc"></param>
        private void CreateDeckDisplayIndex(CalcType calc)
        {
            isEvent = false;
            CmbDeckDisplay.ItemsSource = Enumerable.Range(1, deckInfo.DeckInfo.Where(r =>r.Type == calcType.ToString()).Max(r => r.DisplayIndex));
            isEvent = true;
        }
        #endregion

        #region デッキ選択画面デッキ検索

        private void BtnSearchDeck_Click(object sender, RoutedEventArgs e)
        {
            PopSearchDeck.IsOpen = !PopSearchDeck.IsOpen;
            //閉じた場合は検索クリア
            if (!PopSearchDeck.IsOpen)
            {
                SeachDeckClearAll();
            }
        }

        private void SeachDeckClearAll()
        {
            isEvent = false;
            TxtPopSearchDeckName.Text = string.Empty;
            LstPopSearchDeckBonus.SelectedItem = null;
            TxtPopSearchDeckAtkUp.Text = string.Empty;
            TxtPopSearchDeckAtkDown.Text = string.Empty;
            TxtPopSearchDeckCostDown.Text = string.Empty;
            TxtPopSearchDeckCostUp.Text = string.Empty;
            TxtPopSearchDeckDefDown.Text = string.Empty;
            TxtPopSearchDeckDefUp.Text = string.Empty;
            CmbPopSearchDeckAttr.SelectedValue = string.Empty;
            isEvent = true;
            SearchDeck();
        }

        private void TxtPopSearchDeck_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isEvent)
            {
                TBtnSelectAllDeck.IsChecked = true;

                SearchDeck();
            }
        }

        /// <summary>
        /// デッキカード検索
        /// </summary>
        private void SearchDeck()
        {
            EnumerableRowCollection<DataRow> search = dsDeckCard.Tables[0].AsEnumerable();

            search = Search.SearchText(TxtPopSearchDeckName.Text, search);

            if (LstPopSearchDeckBonus.SelectedItems != null && LstPopSearchDeckBonus.SelectedItems.Count > 0)
            {
                var bonus = LstPopSearchDeckBonus.SelectedItems.OfType<SelectionBonusInfo>().ToList();
                HashSet<string> bonusList = new HashSet<string>();
                bonus.ForEach(b => bonusList.Add(b.Name));
                search = search.Where(r =>
                        bonusList.Contains(r["攻選抜ボーナス1"] as string) ||
                        bonusList.Contains(r["攻選抜ボーナス2"] as string) ||
                        bonusList.Contains(r["攻選抜ボーナス3"] as string) ||
                        bonusList.Contains(r["守選抜ボーナス1"] as string) ||
                        bonusList.Contains(r["守選抜ボーナス2"] as string) ||
                        bonusList.Contains(r["守選抜ボーナス3"] as string));
            }

            var upTextList = new[]{
                new{ Text= TxtPopSearchDeckCostUp, Column="コスト"},
                new{ Text= TxtPopSearchDeckAtkUp, Column="攻援"},
                new{ Text= TxtPopSearchDeckDefUp, Column="守援"},
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
                new{ Text= TxtPopSearchDeckCostDown, Column="コスト"},
                new{ Text= TxtPopSearchDeckAtkDown, Column="攻援"},
                new{ Text= TxtPopSearchDeckDefDown, Column="守援"},
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

            if(!string.IsNullOrEmpty(CmbPopSearchDeckAttr.SelectedValue as string))
            {
                search = search.Where(r => r["属性"] as string == CmbPopSearchDeckAttr.SelectedValue as string);
            }

            DgDeckCards.ItemsSource = search.OrderBy(r => r["表示順"]).AsDataView();
        }

        private void LstPopSearchDeckBonus_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent)
            {
                TBtnSelectAllDeck.IsChecked = true;
                SearchDeck();
            }
        }

        private void BtnPopSearchDeckClearAll_Click(object sender, RoutedEventArgs e)
        {
            SeachDeckClearAll();
        }

        private void BtnPopSearchDeckClearBonus_Click(object sender, RoutedEventArgs e)
        {
            LstPopSearchDeckBonus.SelectedItem = null;
        }

        private void CmbPopSearchDeckAttr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent)
            {
                TBtnSelectAllDeck.IsChecked = true;
                SearchDeck();
            }
        }
        #endregion

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
                //ボーナス表示
                CreateAtkSelectionBonusInfo();
                LstPopSearchOwnCardBonusAtk.DisplayMemberPath = "DisplayNoUseCount";
                LstPopSearchOwnCardBonusAtk.SelectedValuePath = "Name";
                LstPopSearchOwnCardBonusAtk.ItemsSource = atkSelectionBonusInfo.Select(b => b.Value).OrderByDescending(r => r.Count).ToList();

                CreateDefSelectionBonusInfo();
                LstPopSearchOwnCardBonusDef.DisplayMemberPath = "DisplayNoUseCount";
                LstPopSearchOwnCardBonusDef.SelectedValuePath = "Name";
                LstPopSearchOwnCardBonusDef.ItemsSource = defSelectionBonusInfo.Select(b => b.Value).OrderByDescending(r => r.Count).ToList();

            }
        }

        private void SeachOwnCardClearAll()
        {
            isEvent = false;
            TxtPopSearchOwnCardName.Text = string.Empty;
            LstPopSearchOwnCardBonusAtk.SelectedItem = null;
            LstPopSearchOwnCardBonusDef.SelectedItem = null;
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
            EnumerableRowCollection<DataRow> search = dsDispCard.Tables[0].AsEnumerable();

            search = Search.SearchText(TxtPopSearchOwnCardName.Text, search);

            if (LstPopSearchOwnCardBonusAtk.SelectedItems != null && LstPopSearchOwnCardBonusAtk.SelectedItems.Count > 0)
            {
                var bonus = LstPopSearchOwnCardBonusAtk.SelectedItems.OfType<SelectionBonusInfo>().ToList();
                HashSet<string> bonusList = new HashSet<string>();
                bonus.ForEach(b => bonusList.Add(b.Name));
                search = search.Where(r =>
                        bonusList.Contains(r["攻選抜ボーナス1"] as string) ||
                        bonusList.Contains(r["攻選抜ボーナス2"] as string) ||
                        bonusList.Contains(r["攻選抜ボーナス3"] as string));
            }

            if (LstPopSearchOwnCardBonusDef.SelectedItems != null && LstPopSearchOwnCardBonusDef.SelectedItems.Count > 0)
            {
                var bonus = LstPopSearchOwnCardBonusDef.SelectedItems.OfType<SelectionBonusInfo>().ToList();
                HashSet<string> bonusList = new HashSet<string>();
                bonus.ForEach(b => bonusList.Add(b.Name));
                search = search.Where(r =>
                        bonusList.Contains(r["守選抜ボーナス1"] as string) ||
                        bonusList.Contains(r["守選抜ボーナス2"] as string) ||
                        bonusList.Contains(r["守選抜ボーナス3"] as string));
            }


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

            DgCards.ItemsSource = search.OrderBy(r=>r["表示順"]).AsDataView();
        }

        private void LstPopSearchOwnCardBonusAtk_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent)
            {
                //検索条件の重複を防ぐため名称検索をクリアする
                TxtSearchOwnCard.Text = string.Empty;

                //守援ボーナス選択解除
                isEvent = false;
                LstPopSearchOwnCardBonusDef.SelectedItem = null;
                isEvent = true;

                SearchOwnCard();
            }
        }

        private void LstPopSearchOwnCardBonusDef_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (isEvent)
            {
                //検索条件の重複を防ぐため名称検索をクリアする
                TxtSearchOwnCard.Text = string.Empty;

                //攻援ボーナス選択解除
                isEvent = false;
                LstPopSearchOwnCardBonusAtk.SelectedItem = null;
                isEvent = true;

                SearchOwnCard();
            }
        }

        private void BtnPopSearchOwnCardClearAll_Click(object sender, RoutedEventArgs e)
        {
            SeachOwnCardClearAll();
        }

        private void BtnPopSearchOwnCardClearBonusAtk_Click(object sender, RoutedEventArgs e)
        {
            LstPopSearchOwnCardBonusAtk.SelectedItem = null;
        }

        private void BtnPopSearchOwnCardClearBonusDef_Click(object sender, RoutedEventArgs e)
        {
            LstPopSearchOwnCardBonusDef.SelectedItem = null;
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

        #region DataGrid表示切替
        private void SetDataGridColumnButton(DataGrid dg, CalcType calcType)
        {
            dataGridSet[dg].CalcType = calcType;
            List<ToggleButton> tb = GetChilds<ToggleButton>(dataGridSet[dg].Popup.Child);
            tb.ForEach(t => t.IsChecked = (t.Content as string == calcType.ToString()));
        }

        private void SetDataGridColumn(DataGrid dg, string dgName, CalcType calcType)
        {
            var columns = dsSetting.Column.Where(c => c.GridName == dgName && c.Type == calcType.ToString()).OrderBy(c => c.Index);
            foreach (var columnRow in columns)
            {
                var column = dg.Columns.FirstOrDefault(c => (string)c.Header == columnRow.ColumnName);
                if (column != null)
                {
                    column.DisplayIndex = columnRow.Index;
                    column.Visibility = columnRow.Visibility ? Visibility.Visible : Visibility.Collapsed;
                }
            }
        }

        private void TBtn_Click(object sender, RoutedEventArgs e)
        {
            ((ToggleButton)sender).IsChecked = true;
        }

        private void TBtnPopDeck_Checked(object sender, RoutedEventArgs e)
        {
            Popup p = GetParent<Popup>(sender as FrameworkElement);
            if (p != null)
            {
                GridInfo gi = dataGridSet.FirstOrDefault(g => g.Value.Popup == p).Value;
                CalcType cType = (CalcType)Enum.Parse(typeof(CalcType), ((ToggleButton)sender).Content as string);
                gi.CalcType = cType;
                SetDataGridColumn(gi.Grid, gi.Grid.Name, cType);
                List<ToggleButton> tb = GetChilds<ToggleButton>(p.Child);
                tb.ForEach(t => t.IsChecked = (t == sender as ToggleButton));
            }
        }

        private void BtnPopColumnSetting_Click(object sender, RoutedEventArgs e)
        {
            Popup p = GetParent<Popup>(sender as FrameworkElement);
            if (p != null)
            {
                GridInfo gi = dataGridSet.FirstOrDefault(g => g.Value.Popup == p).Value;
                ColumnSettingWindow window = new ColumnSettingWindow();

                dsSetting.AcceptChanges();
                window.GridName = gi.Grid.Name;
                switch (window.GridName)
                {
                    case "DgCards": window.WindowTitle = "所持カード"; break;
                    case "DgDeckMain": window.WindowTitle = "主選抜"; break;
                    case "DgDeckSub": window.WindowTitle = "副選抜"; break;
                    case "DgDeckCards": window.WindowTitle = "選抜選択"; break;
                }
                window.Setting = (DsSetting)dsSetting.Copy();
                window.Owner = this;

                if (window.ShowDialog() == true)
                {
                    foreach (DsSetting.ColumnRow columnRow in window.Setting.Column.Where(r => r.GridName == gi.Grid.Name))
                    {
                        DsSetting.ColumnRow cr = dsSetting.Column.FirstOrDefault(r => r.GridName == columnRow.GridName && r.ColumnName == columnRow.ColumnName && r.Type == columnRow.Type);
                        cr.Visibility = columnRow.Visibility;
                    }

                    dsSetting.AcceptChanges();
                    dsSetting.WriteXml(Utility.GetFilePath("settings.xml"));

                    SetDataGridColumn(gi.Grid, gi.Grid.Name, gi.CalcType);
                }
                else
                {
                    // dsSetting.RejectChanges();
                }
            }
        }
        #endregion

        private string GetDistinctGilrsBonus(string id)
        {
            DsCards.CardsRow cardRow = cardsList[id];
            StringBuilder sb = new StringBuilder();
            sb.Append(cardRow.名前);
            sb.Append("/");
            sb.Append(cardRow.レア);
            sb.Append("/");
            sb.Append(cardRow.コスト);
            sb.Append("/");
            sb.Append(cardRow.進展);
            sb.Append("/");
            sb.Append(cardRow.種別);
            return sb.ToString();
        }

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

        /// <summary>
        /// 親コントロールの取得
        /// </summary>
        /// <param name="ui"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private FrameworkElement GetParent(FrameworkElement ui, FrameworkElement parent)
        {
            while (true)
            {
                if (ui == null) return null;
                if (ui.Parent == parent) return ui;
                ui = ui.Parent as FrameworkElement;
            }
        }

        private static T GetParent<T>(FrameworkElement element) where T : FrameworkElement
        {
            while (true)
            {
                FrameworkElement parent = VisualTreeHelper.GetParent(element) as FrameworkElement;
                if (parent == null)
                {
                    if (element.Parent == null)
                    {
                        return null;
                    }
                    else
                    {
                        parent = element.Parent as FrameworkElement;
                    }
                }
                T parentT = parent as T;
                if (parentT != null)
                {
                    return parentT;
                }
                element = parent;
            }
        }


        /// <summary>
        /// 子コントロールリストの取得
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <returns></returns>
        private static List<T> GetChilds<T>(Visual element) where T : ContentControl
        {
            List<T> result = new List<T>();
            int childCount = VisualTreeHelper.GetChildrenCount(element);
            if (childCount > 0)
            {
                for (int i = 0; i < childCount; i++)
                {
                    Visual child = VisualTreeHelper.GetChild(element, i) as Visual;
                    T childT = child as T;
                    if (childT != null)
                    {
                        result.Add(childT);
                    }
                    else
                    {
                        List<T> searchT = GetChilds<T>(child);
                        result.AddRange(searchT);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 子コントロールの取得
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="element"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private static T GetChild<T>(Visual element, string content) where T : ContentControl
        {
            int childCount = VisualTreeHelper.GetChildrenCount(element);
            if (childCount > 0)
            {
                for (int i = 0; i < childCount; i++)
                {
                    Visual child = VisualTreeHelper.GetChild(element, i) as Visual;
                    T childT = child as T;
                    if (childT != null)
                    {
                        if (childT.Content as string == content)
                        {
                            return childT;
                        }
                    }
                    else
                    {
                        T searchT = GetChild<T>(child, content);
                        if (searchT != null)
                        {
                            return searchT;
                        }
                    }
                }
            }
            return null;
        }
        #endregion

    }
}
