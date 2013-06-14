﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GirlFriendYell
{
    #region クラス定義

    public enum Attr 
    {
        Sweet,
        Cool,
        Pop,
    }

    public enum Rare
    {
        N=1,
        HN=2,
        R=3,
        HR=4,
        SR=5,
        SSR=6,
        LG=7,
    }

    public class TargetInfo
    {
        public int Lv { set; get; }
<<<<<<< HEAD
        /// <summary>成長</summary>
        public int Progress { set; get; }
        /// <summary>現在Lvの経験値</summary>
        public int Exp { set; get; }
        /// <summary>総経験値</summary>
        public int TotalExp { set; get; }
        /// <summary>声援Lv</summary>
        public int SkillLv { set; get; }
        /// <summary>レア</summary>
        public Rare Rare { set; get; }
        /// <summary>使用ガル</summary>
        public int Gall { set; get; }
        /// <summary>属性</summary>
=======
        public int Exp { set; get; }
        public int TotalExp { set; get; }
        public int Progress { set; get; }
        public int SkillLv { set; get; }
        public Rare Rare { set; get; }
        public int Gall { set; get; }
>>>>>>> ebd62ab... no message
        public Attr Attr { set; get; }

        public void Copy(TargetInfo info)
        {
            info.SkillLv = this.SkillLv;
            info.Rare = this.Rare;
            info.Lv = this.Lv;
            info.Exp = this.Exp;
            info.Progress = this.Progress;
            info.Gall = this.Gall;
            info.Attr = this.Attr;
<<<<<<< HEAD
            info.TotalExp = this.TotalExp;
=======
>>>>>>> ebd62ab... no message
        }
    }

    public class CalcEvent : EventArgs
    {
        public int ID { private set; get; }

        public CalcEvent(int id)
        {
            ID = id;
        }
    }

    public class YellInfo
    {
        /// <summary>レア</summary>
        public Rare Rare{set;get;}
        /// <summary>進展</summary>
        public int Rank { set; get; }
        /// <summary>コスト</summary>
        public int Cost { set; get; }
        /// <summary>属性</summary>
        public Attr Attr { set; get; }
        /// <summary>声援有無</summary>
        public bool HasSkill { set; get; }

    }
    #endregion

    public class YellUtility
    {
        #region コントロール
        public static void SetComboBox<T>(ComboBox comboBox)
        {
            var enumStr = Enum.GetNames(typeof(T));
            var enumVal = Enum.GetValues(typeof(T));
            Dictionary<T, string> dic = new Dictionary<T, string>();
            for (int i = 0; i < enumStr.Length; i++)
            {
                dic.Add((T)enumVal.GetValue(i), enumStr[i]);
            }
            comboBox.SelectedValuePath = "Key";
            comboBox.DisplayMemberPath = "Value";
            comboBox.ItemsSource = dic;
        }

        public static void SetComboBoxString<T>(ComboBox comboBox, bool isEmpty=false)
        {
            var enumStr = Enum.GetNames(typeof(T));
            Dictionary<string, string> dic = new Dictionary<string, string>();
            if (isEmpty)
            {
                dic.Add("", "");
            }
            for (int i = 0; i < enumStr.Length; i++)
            {
                dic.Add(enumStr[i], enumStr[i]);
            }
            comboBox.SelectedValuePath = "Key";
            comboBox.DisplayMemberPath = "Value";
            comboBox.ItemsSource = dic;
        }
        #endregion

        #region 計算
        public static DsYellInfo Info{set;get;}

        /// <summary>
        /// レア毎の最大Lv取得
        /// </summary>
        /// <param name="rare"></param>
        /// <returns></returns>
        public static int GetMaxLv(Rare rare)
        {
            switch (rare)
            {
                case Rare.N: return 20;
                case Rare.HN: return 30;
                case Rare.R: return 40;
                case Rare.HR: return 50;
                case Rare.SR: return 60;
                case Rare.SSR: return 70;
                case Rare.LG: return 80;
            }
            return 1;
        }

        /// <summary>
        /// LvUP計算
        /// </summary>
        /// <param name="baseRare"></param>
        /// <param name="baseLv"></param>
        /// <param name="baseExp"></param>
        /// <param name="yellList"></param>
        /// <param name="lv"></param>
        /// <param name="exp"></param>
        /// <param name="progress"></param>
        public static void CalcLv(TargetInfo baseInfo,bool isSuccess, List<YellInfo> yellList, out int lv, out int exp, out int progress, out int totalExp)
        {
            int tmpLv = baseInfo.Lv;

            progress = 0;
            int maxLv = GetMaxLv(baseInfo.Rare);
            //最大レベルまで達した場合
            if (maxLv <= tmpLv)
            {
                lv = baseInfo.Lv;
                progress = 100;
                exp = 0;
                totalExp = 0;
                return;
            }

            totalExp = yellList.Sum(y => CalcExp(baseInfo, isSuccess, y));
            int currentExp = baseInfo.Exp + totalExp;

            int needExp =0;
            while (true)
            {
                //Lvアップに必要な経験値の取得
                needExp = Info.LvExp.FirstOrDefault(r => r.Lv == tmpLv).Exp;
                //必要経験値が足りない場合は終了
                if (needExp > currentExp) break;
                //LvUp処理
                tmpLv++;
                //最大Lvを超えた場合は
                if (maxLv <= tmpLv)
                {
                    tmpLv = maxLv;
                    currentExp = needExp;
                    break;
                }
                currentExp = currentExp - needExp;
            }

            lv = tmpLv;
            exp = currentExp;
            progress = (int)Math.Truncate((double)currentExp * 100 / (double)needExp);
        }

        /// <summary>
        /// 取得経験値の計算
        /// </summary>
        /// <param name="baseInfo"></param>
        /// <param name="isSuccess"></param>
        /// <param name="yellInfo"></param>
        /// <returns></returns>
        public static int CalcExp(TargetInfo baseInfo, bool isSuccess, YellInfo yellInfo)
        {
            //基礎経験値の取得
            int exp =0;
            string rare = yellInfo.Rare.ToString();
            DsYellInfo.BaseExpRow expRow = Info.BaseExp.FirstOrDefault(r => r.レア == rare && r.進展 == yellInfo.Rank);
            if (expRow != null)
            {
                exp = expRow.経験値 + ((yellInfo.Cost-1) * 30);
                //大成功の場合
                if (isSuccess)
                {
                    exp = (int)Math.Truncate((double)exp * 1.5);
                }
                //同属性の場合
                if (baseInfo.Attr == yellInfo.Attr)
                {
                    exp = (int)Math.Truncate((double)exp * 1.2) - 1;
                }
            }
            return exp;
        }

        /// <summary>
        /// 次Lvアップに必要な経験値の取得
        /// </summary>
        /// <param name="baseInfo"></param>
        /// <returns></returns>
        public static int GetNextExp(int lv)
        {
            return Info.LvExp.FirstOrDefault(r => r.Lv == lv).Exp;
        }

        /// <summary>
        /// 次Lvアップに必要な経験値の取得
        /// </summary>
        /// <param name="baseInfo"></param>
        /// <returns></returns>
        public static int GetNextExp(Rare rare,int lv)
        {
            int maxLv = GetMaxLv(rare);
            //最大レベルまで到達していた場合
            if (maxLv == lv) return 0;
            return Info.LvExp.FirstOrDefault(r => r.Lv == lv).Exp;
        }

        /// <summary>
        /// 総経験値取得
        /// </summary>
        /// <param name="lv"></param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static int GetTotalExp(int lv, int progress)
        {
            int exp = Info.LvExp.Where(r => r.Lv < lv).Sum(r => r.Exp);
            exp = exp + (int)Math.Truncate(Info.LvExp.First(r => r.Lv == lv).Exp * progress / 100d);
            return exp;
        }

        /// <summary>
        /// 成長取得
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="lv"></param>
        /// <param name="progress"></param>
        public static void GetProgress(int exp, out int lv, out int progress)
        {
            int tmpLv = 1;
            int currentExp = exp;
            int needExp = 0;
            while (true)
            {
                //Lvアップに必要な経験値の取得
                needExp = Info.LvExp.FirstOrDefault(r => r.Lv == tmpLv).Exp;
                //必要経験値が足りない場合は終了
                if (needExp > currentExp) break;
                //LvUp処理
                tmpLv++;
                currentExp = currentExp - needExp;
            }

            lv = tmpLv;
            progress = (int)Math.Truncate((double)currentExp * 100 / (double)needExp);
        }

        /// <summary>
        /// 声援UP率計算
        /// </summary>
        /// <param name="baseRare"></param>
        /// <param name="baseLv"></param>
        /// <param name="yellList"></param>
        /// <returns></returns>
        public static int CalcSkill(Rare baseRare, int baseSkillLv, List<YellInfo> yellList)
        {
            //Lv10の場合
            if (baseSkillLv >= 10)
            {
                return 0;
            }

            double result = 0;
            double basePer;
            basePer = (int)baseRare < (int)Rare.HR ? 95 - (baseSkillLv * 5) : 100;
             foreach (YellInfo yell in yellList.Where(y => y.HasSkill))
            {
                double per = basePer / (baseSkillLv + 3d) / Math.Pow(2, (int)baseRare - (int)yell.Rare - 1);
                result += per;
            }

             return (int)Math.Ceiling(result);
        }
        #endregion
    }
}
