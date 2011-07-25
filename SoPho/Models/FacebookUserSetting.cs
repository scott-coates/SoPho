using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace SoPho.Models
{
    [Serializable]
    public class FacebookUserSetting
    {
        public string AccessToken { get; set; }
        public FacebookUser User { get; set; }
        public ObservableCollection<FacebookPictureSetting> PictureSettings { get; set; }
    }
}
