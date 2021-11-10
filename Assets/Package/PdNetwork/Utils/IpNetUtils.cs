using System;
using System.Net;
using System.Net.Sockets;

namespace PdNetwork.Utils
{
    public static class IpNetUtils
    {
        public static IPEndPoint MakeEndPoint(string hostStr, int port)
        {
            return new IPEndPoint(ResolveAddress(hostStr), port);
        }
        
        public static IPAddress ResolveAddress(string hostStr)
        {
            if(hostStr == "localhost")
                return IPAddress.Loopback;
            
            IPAddress ipAddress;
            if (!IPAddress.TryParse(hostStr, out ipAddress))
            {
                if (ipAddress == null)
                    ipAddress = ResolveAddress(hostStr, AddressFamily.InterNetwork);
            }
            if (ipAddress == null)
                throw new ArgumentException("Invalid address: " + hostStr);

            return ipAddress;
        }
        
        private static IPAddress ResolveAddress(string hostStr, AddressFamily addressFamily)
        {
            IPAddress[] addresses = ResolveAddresses(hostStr);
            foreach (IPAddress ip in addresses)
            {
                if (ip.AddressFamily == addressFamily)
                {
                    return ip;
                }
            }
            return null;
        }
        
        private static IPAddress[] ResolveAddresses(string hostStr)
        {
            var host = Dns.GetHostEntry(hostStr);
            return host.AddressList;
        }
    }
}