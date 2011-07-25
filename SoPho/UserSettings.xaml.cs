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
using System.Windows.Shapes;
using Facebook;
using SoPho.Models;

namespace SoPho
{
    /// <summary>
    /// Interaction logic for UserSettings.xaml
    /// </summary>
    public partial class UserSettings : Window
    {
        public UserSettings(FacebookUserSetting setting)
        {
            //get user
            InitializeComponent();

            var fb = new FacebookClient(setting.AccessToken);

            var pictureSettings = new List<FacebookPictureSetting>();


            dynamic result = fb.Get("/me/friends");
            result.ToString();
        }
    }
}
