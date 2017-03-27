using System.Collections.Generic;
using Newtonsoft.Json;

namespace Consumer.Models
{
    public class Me
    {
        public string IdentityId { get; set; }
        public string Sub { get; set; }
        [JsonProperty("email_verified")]
        public bool EmailVerified { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string LoginKey { get; set; }
        public string Token { get; set; }
        public List<string> Groups { get; set; }
    }
}