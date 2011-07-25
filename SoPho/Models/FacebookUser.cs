using System;

namespace SoPho.Models
{
    [Serializable]
    public class FacebookUser
    {
        public string Name { get; set; }
        public string UserId { get; set; }

        public FacebookUser()
        {
            
        }

        public FacebookUser(string name, string id)
        {
            Name = name;
            UserId = id;
        }
    }
}