using System;
using System.Collections.Generic;
using System.Text;

namespace MessagingServer
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
