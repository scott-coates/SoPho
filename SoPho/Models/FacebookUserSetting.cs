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
        public string Name { get; set; }
        public string UserId{ get; set; }
        public ObservableCollection<FacebookPictureSetting> PictureSettings { get; set; }
    }
}
