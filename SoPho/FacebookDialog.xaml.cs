using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Forms;
using Facebook;
using SoPho.Models;

namespace SoPho
{
    /// <summary>
    /// Interaction logic for FacebookDialog.xaml
    /// </summary>
    public partial class FacebookDialog : Window
    {
        //using win form browser for this reason:
        //http://social.msdn.microsoft.com/Forums/en/wpf/thread/ffdf22f8-0df4-4ce4-bdee-632e4cbb5fbb

        public FacebookOAuthResult Result { get; private set; }

        public FacebookDialog()
        {
            InitializeComponent();
            //ensure log out
            //http://stackoverflow.com/questions/6240468/logging-out-of-facebook-c-sdk-on-wp7/6513474#6513474
            if (Properties.Settings.Default.FacebookUsersSettings.UserSettings.Any())
            {
                var firstUser = Properties.Settings.Default.FacebookUsersSettings.UserSettings.Last();

                const string logout_format = "http://www.facebook.com/logout.php?api_key={0}&session_key={1}&next=http://www.facebook.com";
                string session = firstUser.AccessToken.Split('|')[1];
                string url = string.Format(logout_format, SoPhoConstants.AppId, session);
                fbLogin.Navigate(url);
            }

            Uri fbUri = FacebookOAuthClient.GetLoginUrl(SoPhoConstants.AppId, null,
                                                        new[]
                                                            {
                                                                "user_photo_video_tags", "friends_photo_video_tags",
                                                                "offline_access"
                                                            }, new Dictionary<string, object>
                                                                   {
                                                                       {"response_type", "token"},
                                                                       {"display", "popup"}
                                                                   });

            fbLogin.Navigate(fbUri);
        }

        private void WebBrowserNavigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            FacebookOAuthResult result;
            if (FacebookOAuthResult.TryParse(e.Url, out result))
            {
                Result = result;
                DialogResult = result.IsSuccess;
            }
            else
            {
                Result = null;
            }
        }
    }
}