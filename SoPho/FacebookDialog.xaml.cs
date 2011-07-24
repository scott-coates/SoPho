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
            //facebook logout doesn't work
            //http://cynic.me/2011/06/22/logging-out-of-facebook-with-c-sdk/#comment-156
            //TODO: logout first
            Uri fbUri =  FacebookOAuthClient.GetLoginUrl(SoPho.Models.SoPhoConstants.AppId, null, new[] { "user_photo_video_tags", "friends_photo_video_tags", "offline_access" },  new Dictionary<string, object>
                    {
                        { "response_type", "token" },
                        { "display", "popup" }
                    });

            fbLogin.Navigate(fbUri);
        }

        private void WebBrowser_Navigated(object sender, System.Windows.Forms.WebBrowserNavigatedEventArgs e)
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
