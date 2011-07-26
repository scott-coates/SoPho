using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Facebook;
using SoPho.Models;
using SoPho.Properties;
using System.Diagnostics;
using System.Threading;

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

                    Settings.Default.FacebookUsersSettings.UserSettings.Add(new FacebookUserSetting
                                                                                {
                                                                                    AccessToken =
                                                                                        fbDialog.Result.AccessToken,
                                                                                    User = new FacebookUser(name, id)
                                                                                });
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
            status.Content = "running...";

            const string queryFormat =
                "SELECT src_big FROM photo WHERE pid IN (SELECT pid FROM photo_tag WHERE subject IN ({0})) AND created >= {1}";

            TimeSpan daysAgo = (DateTime.UtcNow.AddDays(-Settings.Default.FacebookUsersSettings.DaysBack) -
                                new DateTime(1970, 1, 1));
            string seconds = ((int) Math.Round(daysAgo.TotalSeconds)).ToString();
            var queries = new List<string>();

            var picsToGet = new ConcurrentBag<string>();

            //foreach user, build query

            var uiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();

            Task t = Task.Factory.StartNew(() =>
                                               {
                                                   Parallel.ForEach(Settings.Default.FacebookUsersSettings.UserSettings,
                                                                    (userSetting =>
                                                                         {
                                                                             string[] ids =
                                                                                 userSetting.PictureSettings.Select(
                                                                                     x => x.User.Id).ToArray();
                                                                             string query =
                                                                                 string.Format(
                                                                                     queryFormat,
                                                                                     string.Join(",", ids),
                                                                                     seconds);
                                                                             queries.Add(query);
                                                                             var fb =
                                                                                 new FacebookClient(
                                                                                     userSetting.
                                                                                         AccessToken);
                                                                             dynamic result =
                                                                                 fb.Query(query);

                                                                             foreach (var pic in result)
                                                                             {
                                                                                 picsToGet.Add(pic.src_big);
                                                                             }
                                                                         }));
                                               }).ContinueWith(t2 =>
                                                                   {
                                                                       if (t2.Exception != null)
                                                                       {
                                                                           status.Content = t2.Exception.Message;
                                                                       }
                                                                       else
                                                                       {
                                                                           status.Content = "Done downloading";
                                                                       }
                                                                   }, uiTaskScheduler);
        }
    }
}