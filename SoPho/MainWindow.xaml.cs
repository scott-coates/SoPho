using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Facebook;
using SoPho.Models;
using SoPho.Properties;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace SoPho
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ManagementEventWatcher _watcher = new ManagementEventWatcher();
        private NotifyIcon _ni;

        public MainWindow()
        {
            InitializeComponent();
            _ni = new System.Windows.Forms.NotifyIcon { Icon = new System.Drawing.Icon("camera.ico"), Visible = true };
            _ni.DoubleClick +=
                delegate
                {
                    Show();
                    WindowState = WindowState.Normal;
                };

            _ni.ContextMenu = new ContextMenu(new[] { new MenuItem("Exit", (obj, arg) =>Application.Current.Shutdown()) });

            if (Settings.Default.FacebookUsersSettings == null)
            {
                Settings.Default.FacebookUsersSettings = new FacebookSettings();
            }

            lsUsers.ItemsSource = Settings.Default.FacebookUsersSettings.UserSettings;
            txtDir.Text = Settings.Default.FacebookUsersSettings.PhotoDirectory;
            txtDays.Text = Settings.Default.FacebookUsersSettings.DaysBack.ToString();
            checkBox1.IsChecked = Settings.Default.FacebookUsersSettings.RemoveMediaAfterDownload;

            var query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
            TaskScheduler ui = TaskScheduler.FromCurrentSynchronizationContext();

            _watcher.EventArrived += (sender, args) =>
                                         {
                                             string path = Settings.Default.FacebookUsersSettings.PhotoDirectory;
                                             var driverLetter = GetDriveLetter(path);

                                             if (driverLetter.TrimEnd('\\') ==
                                                 args.NewEvent.GetPropertyValue("DriveName").ToString())
                                             {
                                                 Task t = DownloadPhotos(ui);
                                                 t.Wait();
                                                 RemoveDrive();
                                             }
                                         };

            _watcher.Query = query;
            _watcher.Start();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                Hide();

            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            //Do some stuff here 
            //Hide Window
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (DispatcherOperationCallback)delegate(object o)
            {
                Hide();
                return null;
            }, null);
            //Do not close application
            e.Cancel = true;


        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _ni.Dispose();
            _watcher.Dispose();
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
            Settings.Default.FacebookUsersSettings.RemoveMediaAfterDownload = checkBox1.IsChecked.GetValueOrDefault();

            Settings.Default.Save();
        }

        private void Button3Click(object sender, RoutedEventArgs e)
        {
            string path = Settings.Default.FacebookUsersSettings.PhotoDirectory;
            var driverLetter = GetDriveLetter(path);

            var driveInfo = DriveInfo.GetDrives().FirstOrDefault(x => x.Name == driverLetter);

            //unfortunately we cannot force the usb device to turn back on http://stackoverflow.com/questions/138394/how-to-programatically-unplug-replug-an-arbitrary-usb-device/138682#138682
            if (driveInfo == null)
            {
                status.Content = "Drive " + driverLetter + " doesn't exist.";
                Console.WriteLine(status.Content);
            }
            else
            {
                Task t = DownloadPhotos();
                App.WaitWithPumping(t);

                RemoveDrive();
            }
        }

        public void RemoveDrive()
        {
            if (Settings.Default.FacebookUsersSettings.RemoveMediaAfterDownload)
            {
                //higher priority http://social.msdn.microsoft.com/forums/en-US/wpf/thread/6fce9b7b-4a13-4c8d-8c3e-562667851baa/
                status.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() =>
                                                                               status.Content = "Removing media"
                                                                        ));
                Console.WriteLine(status.Dispatcher.Invoke(new Func<object>(() => status.Content)));

                string path = Settings.Default.FacebookUsersSettings.PhotoDirectory;
                var driverLetter = GetDriveLetter(path);

                var driveInfo = DriveInfo.GetDrives().First(x => x.Name == driverLetter);
                if (driveInfo.DriveType != DriveType.Removable)
                {
                    status.Dispatcher.Invoke(DispatcherPriority.Render,
                                             new Action(
                                                 () =>
                                                 status.Content =
                                                 "Cannot remove " + driveInfo.DriveType + " disks. The process is done."));
                    Console.WriteLine(status.Dispatcher.Invoke(new Func<object>(() => status.Content)));
                }
                else
                {
                    var processInfo =
                        new ProcessStartInfo(
                            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                                         "external\\sync.exe")) { UseShellExecute = false, Arguments = "-e " + driverLetter };
                    var process = Process.Start(processInfo);

                    if (!process.WaitForExit(10000))
                    {
                        status.Dispatcher.Invoke(DispatcherPriority.Render,
                                                 new Action(() => status.Content = driverLetter + " failed to eject."));
                        Console.WriteLine(status.Dispatcher.Invoke(new Func<object>(() => status.Content)));
                    }
                    else
                    {
                        status.Dispatcher.Invoke(DispatcherPriority.Render,
                                                 new Action(() => status.Content = driverLetter + " has been ejected."));
                        Console.WriteLine(status.Dispatcher.Invoke(new Func<object>(() => status.Content)));
                    }
                }
            }
            status.Dispatcher.Invoke(DispatcherPriority.Render, new Action(() => status.Content = "Done!"));
            Console.WriteLine(status.Dispatcher.Invoke(new Func<object>(() => status.Content)));
        }

        public string GetDriveLetter(string path)
        {
            string driverLetter;
            string tmpPath = path;

            do
            {
                driverLetter = tmpPath;
                tmpPath = Path.GetDirectoryName(tmpPath);
            } while (string.IsNullOrWhiteSpace(tmpPath) == false);
            return driverLetter;
        }

        public Task DownloadPhotos(TaskScheduler uiTaskScheduler = null)
        {
            status.Dispatcher.Invoke(DispatcherPriority.Render, new Action<string>(x => status.Content = x),
                                     "Querying photos...");
            Console.WriteLine(status.Dispatcher.Invoke(new Func<object>(() => status.Content)));

            const string queryFormat =
                "SELECT src_big FROM photo WHERE pid IN (SELECT pid FROM photo_tag WHERE subject IN ({0})) AND created >= {1}";

            TimeSpan daysAgo = (DateTime.UtcNow.AddDays(-Settings.Default.FacebookUsersSettings.DaysBack) -
                                new DateTime(1970, 1, 1));
            string seconds = ((int)Math.Round(daysAgo.TotalSeconds)).ToString();
            var queries = new List<string>();

            var picsToGet = new ConcurrentBag<Uri>();

            //foreach user, build query

            uiTaskScheduler = uiTaskScheduler ?? TaskScheduler.FromCurrentSynchronizationContext();
            var task = Task.Factory.StartNew(() =>
                                             GetPicUrls(queries, picsToGet, seconds, queryFormat))
                .ContinueWith(UpdateStatusAfterQueryingPhotos, uiTaskScheduler)
                .ContinueWith(y => ProcessPics(y, picsToGet))
                .ContinueWith(UpdateStatusAfterProcessingPics, uiTaskScheduler);

            return task;
        }

        private void UpdateStatusAfterProcessingPics(Task obj)
        {
            if (obj.Exception != null)
            {
                status.Content = obj.Exception.Flatten().Message;
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

            Console.WriteLine(status.Content);
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