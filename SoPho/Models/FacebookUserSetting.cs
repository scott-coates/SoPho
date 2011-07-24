using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SoPho.Models
{
    [Serializable]
    public class FacebookUserSetting
    {
        public string AccessToken { get; set; }
        public string Name { get; set; }
    }
}
