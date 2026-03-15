using System.Net;
using System.Net.Sockets;

namespace CleanArchitecture.Infrastructure.Helpers
{
    public class IpHelper
    {
        public static string GetIpAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch
            {
                return "127.0.0.1";
            }
            return "127.0.0.1";
        }
    }
}
