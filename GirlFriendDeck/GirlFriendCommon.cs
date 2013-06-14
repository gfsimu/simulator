using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace GirlFriendCommon
{

    #region Command
    internal static class CustomCommands
    {
        public class ClearCommand<T> where T : Control
        {
            private static RoutedUICommand command;

            public static RoutedUICommand Command { get { return command; } }

            static ClearCommand()
            {
                CommandBinding commandBinding = new CommandBinding();
                command = new RoutedUICommand("ClearText", "ClearText", typeof(T));
                commandBinding.Command = command;
                commandBinding.CanExecute += (s, e) =>
                {
                    TextBox text = s as TextBox;
                    if (text != null)
                    {
                        e.CanExecute = !string.IsNullOrEmpty(text.Text);
                    }
                    else
                    {
                        PasswordBox pass = s as PasswordBox;
                        if (pass != null)
                        {
                            e.CanExecute = !string.IsNullOrEmpty(pass.Password);
                        }
                        else
                        {
                            e.CanExecute = false;
                        }
                    }
                };
                commandBinding.Executed += (s, e) =>
                {
                    TextBox text = s as TextBox;
                    if (text != null)
                    {
                        text.Clear();
                        text.ToolTip = null;
                    }
                    else
                    {
                        PasswordBox pass = s as PasswordBox;
                        if (pass != null)
                        {
                            pass.Clear();
                            pass.ToolTip = null;
                        }
                    }

                };
                CommandManager.RegisterClassCommandBinding(typeof(T), commandBinding);
            }
        }

        public static RoutedUICommand ClearText { get { return ClearCommand<TextBox>.Command; } }
        public static RoutedUICommand ClearPassword { get { return ClearCommand<PasswordBox>.Command; } }

    }
    #endregion

    #region クラス定義

    class SkillInfo
    {
        public string Name { set; get; }
        public bool IsAttack { set; get; }
        public bool IsDeffence { set; get; }
        public bool IsOwn { set; get; }
        public bool IsDown { set; get; }
        public int Power { set; get; }
        public int AllPower { set; get; }
    }

    class NameInfo
    {
        public string Name { set; get; }
        public string Hiragana { set; get; }
        public string Roma { set; get; }
        public string Attr { set; get; }
        public string Display
        {
            get
            {
                string display;
                if (string.IsNullOrWhiteSpace(Attr))
                {
                    display = string.Empty;
                }
                else
                {
                    display = Attr.Substring(0, 1);
                }
                return display + ":" + Name;
            }
        }
    }
    #endregion

    static class Utility
    {
        private static string basePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);

        /// <summary>
        /// ファイルパスの取得
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetFilePath(string fileName)
        {
            return Path.Combine(basePath, fileName);
        }

        /// <summary>
        /// DataRowの内容コピー
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public static void CopyRow(DataRow source, DataRow dest)
        {
            foreach (DataColumn column in source.Table.Columns)
            {
                if (dest.Table.Columns.Contains(column.ColumnName))
                {
                    dest[column.ColumnName] = source[column];
                }
            }
        }
    }

    static class Search
    {
        #region 名称検索
        /// <summary>
        /// 名称検索
        /// </summary>
        /// <param name="searchText"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        public static EnumerableRowCollection<DataRow> SearchText(string searchText, EnumerableRowCollection<DataRow> search)
        {
            if (!string.IsNullOrEmpty(searchText))
            {
                string[] originalSearchWord = searchText.Split(new char[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);
                if (originalSearchWord.Length > 0)
                {
                    List<string> searchWord = new List<string>();
                    foreach (string word in originalSearchWord)
                    {
                        searchWord.Add(word.ToUpper());
                    }

                    if (search.Any(r => r.Table.Columns.Contains("Name")))
                    {
                        search = search.Where(r =>
                            searchWord.All(word =>
                            {
                                if (((string)r["名前"]).Contains(word)) return true;
                                if (((string)r["なまえ"]).Contains(word)) return true;
                                if (((string)r["Name"]).Contains(word)) return true;
                                return false;
                            })
                        );
                    }
                    else
                    {
                        search = search.Where(r =>
                            searchWord.All(word =>
                            {
                                if (((string)r["名前"]).Contains(word)) return true;
                                return false;
                            })
                        );
                    }
                }
            }
            return search;
        }

        public static void SearchName(TextBox textBox, DataGrid grid, DataSet ds)
        {
            grid.ItemsSource = SearchText(textBox.Text, ds.Tables[0].AsEnumerable()).AsDataView();
        }

        /// <summary>
        /// 名称検索（リストボックス）
        /// </summary>
        /// <param name="textBox"></param>
        /// <param name="list"></param>
        /// <param name="nameList"></param>
        public static void SearchListName(TextBox textBox, ListBox list, List<NameInfo> nameList)
        {
            if (string.IsNullOrEmpty(textBox.Text))
            {
                list.ItemsSource = nameList;
            }
            else
            {
                string[] originalSearchWord = textBox.Text.Split(new char[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);
                if (originalSearchWord.Length > 0)
                {
                    List<string> searchWord = new List<string>();
                    foreach (string word in originalSearchWord)
                    {
                        searchWord.Add(word.ToUpper());
                    }

                    list.ItemsSource = nameList.Where(r =>
                        searchWord.All(word =>
                        {
                            if (r.Display.Contains(word)) return true;
                            if (r.Name.Contains(word)) return true;
                            if (r.Hiragana.Contains(word)) return true;
                            if (r.Roma.Contains(word)) return true;
                            return false;
                        })
                    );
                }
                else
                {
                    list.ItemsSource = nameList;
                }
            }
        }
        #endregion
    }

    static class Images
    {
        #region イメージ
        /// <summary>
        /// サイズ変更
        /// </summary>
        /// <param name="originalSize"></param>
        /// <param name="maxSize"></param>
        /// <param name="scale"></param>
        /// <returns></returns>
        public static Size GetScaleSize(Size originalSize, Size maxSize, double? scale)
        {
            //Scaleが未指定の場合は等倍
            scale = scale ?? 1d;

            double width = originalSize.Width / scale.Value;
            if (width > maxSize.Width)
            {
                width = maxSize.Width;
                scale = originalSize.Width / width;
            }
            double height = originalSize.Height / scale.Value;
            if (height > maxSize.Height)
            {
                height = maxSize.Height;
                scale = (double)originalSize.Height / height;
                width = (double)originalSize.Width / scale.Value;
            }
            return new Size(width, height);
        }

        /// <summary>
        /// 画像読込
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static BitmapImage LoadImage(string fileName, ref Size size)
        {
            if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
            {
                BitmapImage ss = new BitmapImage();
                ss.BeginInit();
                ss.UriSource = new Uri(fileName);
                ss.EndInit();
                BitmapImage s = new BitmapImage();
                s.BeginInit();
                s.UriSource = new Uri(fileName);
                double dw = size.Width, dh = size.Height;
                Size scaleSize = GetScaleSize(new Size(ss.PixelWidth, ss.PixelHeight), new Size(dw, dh), null);
                s.DecodePixelWidth = (int)scaleSize.Width;
                s.DecodePixelHeight = (int)scaleSize.Height;
                s.EndInit();
                s.Freeze();
                return s;
            }
            else
            {
                return null;
            }
        }
        #endregion
    }
}
