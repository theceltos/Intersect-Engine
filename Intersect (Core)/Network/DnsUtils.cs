﻿using System.Net;
using System.Net.Sockets;

namespace Intersect.Network;


public static partial class DnsUtils
{

    public static IPAddress Resolve(string hostname)
    {
        return string.IsNullOrEmpty(hostname?.Trim())
            ? new IPAddress(0)
            : Dns.GetHostEntry(hostname.Trim())
                .AddressList?.First(ip => ip?.AddressFamily == AddressFamily.InterNetwork);
    }

}
