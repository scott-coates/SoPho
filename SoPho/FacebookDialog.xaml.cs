using System;
using System.Collections.Generic;
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
            Uri fbUri = FacebookOAuthClient.GetLoginUrl(SoPhoConstants.AppId, null,
                                                        new[]
                                                            {
                                                                "user_photo_video_tags", "friends_photo_video_tags",
                                                                "offline_access"
                                                            },  new Dictionary<string, object>
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