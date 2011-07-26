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
using SoPho.Models;

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
                Settings.Default.FacebookUsersSettings = new FacebookSettings();
            }

            lsUsers.ItemsSource = Settings.Default.FacebookUsersSettings.UserSettings;
            txtDir.Text = Settings.Default.FacebookUsersSettings.PhotoDirectory;
            txtDays.Text = Settings.Default.FacebookUsersSettings.DaysBack.ToString();
        }

        private void Button1Click(object sender, RoutedEventArgs e)
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
                    string id = result.id;

                    Settings.Default.FacebookUsersSettings.UserSettings.Add(new FacebookUserSetting { AccessToken = fbDialog.Result.AccessToken, User = new FacebookUser(name, id) });
                    Settings.Default.Save();
                }
                else
                {
                    MessageBox.Show(fbDialog.Result.ErrorDescription);
                }
            }
        }

        private void ShowUserSetingsExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var setting = e.Parameter as FacebookUserSetting;
            e.Handled = true;
            var userSettingDialog = new UserSettings(setting);
            userSettingDialog.ShowDialog();
        }

        private void Button2Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.FacebookUsersSettings.PhotoDirectory = txtDir.Text;
            Settings.Default.FacebookUsersSettings.DaysBack = int.Parse(txtDays.Text);
            Settings.Default.Save();
        }

        private void Button3Click(object sender, RoutedEventArgs e)
        {
            //https://api.facebook.com/method/fql.query?format=json&query=SELECT src_big FROM photo WHERE pid IN (SELECT pid FROM photo_tag WHERE subject IN (1310574449,208201781)) AND created >= 1309057174&access_token=166125390126089|ec9ddc6bd6d9ce9eb2f7f32d.1-208201781|cf39JVOoAiPv7-rEKg86rK0ph7k
            const string queryFormat = "SELECT src_big FROM photo WHERE pid IN (SELECT pid FROM photo_tag WHERE subject IN ({0})) AND created >= {1}";

            TimeSpan daysAgo = (DateTime.UtcNow.AddDays(-Settings.Default.FacebookUsersSettings.DaysBack) - new DateTime(1970, 1, 1));
            var seconds = ((int) Math.Round(daysAgo.TotalSeconds)).ToString();
            var queries = new List<string>();

            //foreach user, build query

            foreach(var userSetting in Settings.Default.FacebookUsersSettings.UserSettings)
            {
                string[] ids = userSetting.PictureSettings.Select(x=>x.User.Id).ToArray();
                string query = string.Format(queryFormat, string.Join(",", ids), seconds);
                queries.Add(query);
                var fb = new FacebookClient(userSetting.AccessToken);
                dynamic result = fb.Query(query);

            }
        }
    }
}
