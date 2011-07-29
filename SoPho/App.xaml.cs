using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SoPho.Properties;

namespace SoPho
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        //http://www.rootsilver.com/2007/08/how-to-create-a-consolewindow
        //http://stackoverflow.com/questions/426421/wpf-command-line

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainWindow = new MainWindow();
            if (e.Args.Contains("auto"))
            {
                try
                {
                    //Get a pointer to the forground window.  The idea here is that
                    //IF the user is starting our application from an existing console
                    //shell, that shell will be the uppermost window.  We'll get it
                    //and attach to it
                    IntPtr ptr = GetForegroundWindow();

                    int u;

                    GetWindowThreadProcessId(ptr, out u);

                    Process process = Process.GetProcessById(u);

                    if (process.ProcessName == "cmd") //Is the uppermost window a cmd process?
                    {
                        AttachConsole(process.Id);
                        Console.WriteLine(); //empty puts new line and looks better
                    }
                    else
                    {
                        //no console AND we're in console mode ... create a new console.

                        AllocConsole();
                    }

                    string path = Settings.Default.FacebookUsersSettings.PhotoDirectory;
                    var driverLetter = mainWindow.GetDriveLetter(path);

                    var driveInfo = DriveInfo.GetDrives().FirstOrDefault(x => x.Name == driverLetter);

                    //unfortunately we cannot force the usb device to turn back on http://stackoverflow.com/questions/138394/how-to-programatically-unplug-replug-an-arbitrary-usb-device/138682#138682
                    if (driveInfo == null)
                    {
                        Console.WriteLine("Drive " + driverLetter + " doesn't exist.");
                    }
                    else
                    {
                        var task = mainWindow.DownloadPhotos();
                        WaitWithPumping(task);
                        mainWindow.RemoveDrive();
                    }
                }
                finally
                {
                    FreeConsole();
                }
            }
            else
            {
                mainWindow.ShowDialog();
            }
            Shutdown();
        }

        public static void WaitWithPumping(Task task)
        {
            if (task == null) throw new ArgumentNullException("task");
            var nestedFrame = new DispatcherFrame();
            task.ContinueWith(_ => nestedFrame.Continue = false);
            Dispatcher.PushFrame(nestedFrame);
            //execute this loop until all other tasks are done. it won't block the ui thread
            task.Wait();
        }
    }
}