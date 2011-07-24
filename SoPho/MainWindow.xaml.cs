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
using Facebook;

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
            if (Settings.Default.FacebookUsersSettings == null)
            {
                Settings.Default.FacebookUsersSettings = new Models.FacebookUserSettingCollection();
            }

            lsUsers.ItemsSource = Settings.Default.FacebookUsersSettings;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            var fbDialog = new FacebookDialog();
            fbDialog.ShowDialog();

            if (fbDialog.Result != null)
            {
                if (fbDialog.Result.IsSuccess)
                {
                    var fb = new FacebookClient(fbDialog.Result.AccessToken);

                    dynamic result = fb.Get("/me");
                    string name = result.name;

                    Settings.Default.FacebookUsersSettings.Add(new Models.FacebookUserSetting { AccessToken = fbDialog.Result.AccessToken, Name = name });
                    Settings.Default.Save();
                }
                else
                {
                    MessageBox.Show(fbDialog.Result.ErrorDescription);
                }
            }
        }
    }
}
