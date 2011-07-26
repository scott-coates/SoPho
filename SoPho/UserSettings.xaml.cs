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
        private readonly FacebookUserSetting _setting;

        public UserSettings(FacebookUserSetting setting)
        {
            _setting = setting;
            //get user
            InitializeComponent();

            var fb = new FacebookClient(setting.AccessToken);

            var picSettings = new List<FacebookPictureSetting>();

            dynamic friends = fb.Get("/me/friends");

            foreach (var friend in friends.data)
            {
                picSettings.Add(new FacebookPictureSetting(new FacebookUser(friend.name, friend.id), false));
            }

            picSettings.Sort((x, y) => string.Compare(x.User.Name, y.User.Name));
            picSettings.Insert(0, new FacebookPictureSetting(new FacebookUser(setting.User.Name, setting.User.Id), false));

            var selectedPics = picSettings.Where(x => setting.PictureSettings.Any(y => y.User.Id == x.User.Id));

            foreach (var sp in selectedPics)
            {
                sp.Selected = true;
            }

            lsUsers.ItemsSource = picSettings;
        }

        private void Button1Click(object sender, RoutedEventArgs e)
        {
            _setting.PictureSettings.Clear();

            var items = (List<FacebookPictureSetting>)lsUsers.ItemsSource;

            foreach (var item in items.Where(x=>x.Selected))
            {
                _setting.PictureSettings.Add(item);
            }

            Properties.Settings.Default.Save();

            DialogResult = true;
        }
    }
}
