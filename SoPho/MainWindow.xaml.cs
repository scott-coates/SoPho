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
using SoPho.Properties;

namespace SoPho
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Settings.Default.FacebookUsersSettings = new Models.FacebookUserSettingCollection();
            Settings.Default.FacebookUsersSettings.Add(new Models.FacebookUserSetting { AccessToken = "token", Name = "MyName" });
            Settings.Default.Save();
        }
    }
}
