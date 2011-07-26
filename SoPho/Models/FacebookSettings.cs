using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace SoPho.Models
{
    [Serializable]
    [XmlInclude(typeof(string))]
    public class FacebookSettings 
    {
        public FacebookSettings()
        {
            UserSettings = new ObservableCollection<FacebookUserSetting>();
        }

        public ObservableCollection<FacebookUserSetting> UserSettings { get; set; }
        public string PhotoDirectory { get; set; }
        public int DaysBack { get; set; }
    }
}
