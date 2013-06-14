using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading.Tasks;
using System.Windows;

namespace GirlFriendYell
{
    /// <summary>
    /// App.xaml の相互作用ロジック
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            string basePath = Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
#if !NET_V4
            ProfileOptimization.SetProfileRoot(basePath);
            ProfileOptimization.StartProfile("GirlFriendYell.Profile");
#endif
        }
    }
}
