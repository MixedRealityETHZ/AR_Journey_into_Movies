using System.Net;
using System.Net.Sockets;

public static class NetworkUtils
{
    public static string GetLocalIPv4()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());

        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                // 排除 localhost
                if (!ip.ToString().StartsWith("127."))
                    return ip.ToString();
            }
        }

        return "127.0.0.1"; // fallback
    }
}