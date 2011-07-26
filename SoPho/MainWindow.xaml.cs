using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Facebook;
using SoPho.Models;
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
            status.Content = "Querying photos...";

            const string queryFormat =
                "SELECT src_big FROM photo WHERE pid IN (SELECT pid FROM photo_tag WHERE subject IN ({0})) AND created >= {1}";

            TimeSpan daysAgo = (DateTime.UtcNow.AddDays(-Settings.Default.FacebookUsersSettings.DaysBack) -
                                new DateTime(1970, 1, 1));
            string seconds = ((int) Math.Round(daysAgo.TotalSeconds)).ToString();
            var queries = new List<string>();

            var picsToGet = new ConcurrentBag<Uri>();

            //foreach user, build query

            TaskScheduler uiTaskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
            Task.Factory.StartNew(() =>
                                  GetPicUrls(queries, picsToGet, seconds, queryFormat))
                .ContinueWith(UpdateStatusAfterQueryingPhotos, uiTaskScheduler)
                .ContinueWith(y => ProcessPics(y, picsToGet))
                .ContinueWith(UpdateStatusAfterProcessingPics, uiTaskScheduler);
        }

        private void UpdateStatusAfterProcessingPics(Task obj)
        {
            if (obj.Exception != null)
            {
                status.Content = obj.Exception.Flatten().Message;
            }
            else
            {
                status.Content = "Done!";
            }
        }

        private static void ProcessPics(Task t, ConcurrentBag<Uri> picsToGet)
        {
            if (t.Exception != null)
            {
                throw t.Exception.Flatten();
            }
            string[] existingFiles = Directory.GetFiles(Settings.Default.FacebookUsersSettings.PhotoDirectory);
            IEnumerable<string> filesToDelete =
                existingFiles.Except(
                    picsToGet.Distinct().Select(
                        x =>
                        Path.Combine(Settings.Default.FacebookUsersSettings.PhotoDirectory,
                                     Path.GetFileName(x.AbsoluteUri))));

            try
            {
                foreach (string file in filesToDelete)
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }

            //delete files not in list
            IEnumerable<Uri> filesToDownload =
                picsToGet.Distinct().Where(
                    x =>
                    !existingFiles.Contains(Path.Combine(Settings.Default.FacebookUsersSettings.PhotoDirectory,
                                                         Path.GetFileName(x.AbsoluteUri))));

            Parallel.ForEach(filesToDownload, DownloadFiles);
        }

        private static void DownloadFiles(Uri x)
        {
            var client = new WebClient();
            client.DownloadFile(x,
                                Path.Combine(
                                    Settings.Default.
                                        FacebookUsersSettings.
                                        PhotoDirectory,
                                    Path.GetFileName(x.AbsoluteUri)));
        }

        private void UpdateStatusAfterQueryingPhotos(Task task)
        {
            if (task.Exception != null)
            {
                status.Content = task.Exception.Flatten().Message;
                throw task.Exception.Flatten();
            }
            else
            {
                status.Content = "Downloading photos...";
            }
        }

        private static void GetPicUrls(List<string> queries, ConcurrentBag<Uri> picsToGet, string seconds,
                                       string queryFormat)
        {
            Parallel.ForEach(Settings.Default.FacebookUsersSettings.UserSettings,
                             (userSetting =>
                                  {
                                      string[] ids = userSetting.PictureSettings.Select(x => x.User.Id).ToArray();
                                      string query = string.Format(queryFormat, string.Join(",", ids), seconds);
                                      queries.Add(query);
                                      var fb = new FacebookClient(userSetting.AccessToken);
                                      dynamic result = fb.Query(query);

                                      foreach (dynamic pic in result)
                                      {
                                          picsToGet.Add(new Uri(pic.src_big));
                                      }
                                  }));
        }
    }
}