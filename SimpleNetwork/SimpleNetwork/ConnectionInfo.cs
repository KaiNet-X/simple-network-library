using System.Net;

namespace SimpleNetwork
{
    public class ConnectionInfo
    {
        public readonly IPAddress LocalAddress;
        public readonly string LocalHostName;

        public readonly IPAddress RemoteAddress;
        public readonly string RemoteHostName;

        internal ConnectionInfo(IPAddress localA, string localS, IPAddress remA, string remS)
        {
            LocalAddress = localA;
            LocalHostName = localS;
            RemoteAddress = remA;
            RemoteHostName = remS;
        }

    }
}
