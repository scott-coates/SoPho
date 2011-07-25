using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SoPho.Models
{
    [Serializable]
    public class FacebookPictureSetting
    {
        public FacebookUser User { get; set; }
        public bool Selected { get; set; }
    }
}
