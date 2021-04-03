using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageClient
{
    public class SendMessage
    {
        public string Username { get; set; }
        public string Content { get; set; }
        public DateTime Time { get; set; }

        public SendMessage(string Username, string Content, DateTime Time)
        {
            this.Username = Username;
            this.Content = Content;
            this.Time = Time;
        }
    }
}
