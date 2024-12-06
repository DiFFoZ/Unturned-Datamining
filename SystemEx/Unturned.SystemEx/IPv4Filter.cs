namespace Unturned.SystemEx;

public struct IPv4Filter
{
    public IPv4Address address;

    public IPv4SubnetMask subnetMask;

    public ushort minPort;

    public ushort maxPort;

    public IPv4Filter(IPv4Address address, IPv4SubnetMask subnetMask, ushort minPort, ushort maxPort)
    {
        this.address = address;
        this.subnetMask = subnetMask;
        this.minPort = minPort;
        this.maxPort = maxPort;
    }

    public void GetAddressRange(out IPv4Address minAddress, out IPv4Address maxAddress)
    {
        minAddress = address;
        uint num = IPv4SubnetMask.SingleAddress.value ^ subnetMask.value;
        maxAddress = new IPv4Address(address.value + num);
    }

    public bool Matches(IPv4Address hostAddress, ushort port)
    {
        if (subnetMask.ContainsHost(address, hostAddress) && port >= minPort)
        {
            return port <= maxPort;
        }
        return false;
    }

    public override string ToString()
    {
        if (minPort == 0 && maxPort == ushort.MaxValue)
        {
            if (subnetMask.IsSingleAddress)
            {
                return address.ToString();
            }
            return $"{address}/{subnetMask.CountNetworkBits}";
        }
        if (minPort == maxPort)
        {
            if (subnetMask.IsSingleAddress)
            {
                return $"{address}:{minPort}";
            }
            return $"{address}/{subnetMask.CountNetworkBits}:{minPort}";
        }
        if (subnetMask.IsSingleAddress)
        {
            return $"{address}:{minPort}-{maxPort}";
        }
        return $"{address}/{subnetMask.CountNetworkBits}:{minPort}-{maxPort}";
    }

    public override bool Equals(object rhs)
    {
        if (rhs is IPv4Filter)
        {
            return this == (IPv4Filter)rhs;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return address.GetHashCode() ^ (subnetMask.GetHashCode() & minPort.GetHashCode()) ^ maxPort.GetHashCode();
    }

    public static bool operator ==(IPv4Filter lhs, IPv4Filter rhs)
    {
        if (lhs.address == rhs.address && lhs.subnetMask == rhs.subnetMask && lhs.minPort == rhs.minPort)
        {
            return lhs.maxPort == rhs.maxPort;
        }
        return false;
    }

    public static bool operator !=(IPv4Filter lhs, IPv4Filter rhs)
    {
        return !(lhs == rhs);
    }

    public bool Equals(IPv4Filter rhs)
    {
        return this == rhs;
    }

    public int CompareTo(IPv4Filter rhs)
    {
        if (address != rhs.address)
        {
            return address.CompareTo(rhs.address);
        }
        if (subnetMask != rhs.subnetMask)
        {
            return subnetMask.CompareTo(rhs.subnetMask);
        }
        if (minPort != rhs.minPort)
        {
            return minPort.CompareTo(rhs.minPort);
        }
        return maxPort.CompareTo(rhs.maxPort);
    }

    public static bool TryParse(string input, out IPv4Filter filter)
    {
        if (string.IsNullOrEmpty(input))
        {
            filter = default(IPv4Filter);
            return false;
        }
        int num = input.LastIndexOf(':');
        if (num < 0)
        {
            int num2 = input.LastIndexOf('/');
            if (num2 < 0)
            {
                filter.subnetMask = IPv4SubnetMask.SingleAddress;
                filter.minPort = 0;
                filter.maxPort = ushort.MaxValue;
                return IPv4Address.TryParse(input, out filter.address);
            }
            bool flag = IPv4SubnetMask.TryParse(input.Substring(num2 + 1), out filter.subnetMask);
            filter.minPort = 0;
            filter.maxPort = ushort.MaxValue;
            bool num3 = IPv4Address.TryParse(input, 0, num2, out filter.address);
            filter.address = filter.subnetMask.MaskRoutingPrefix(filter.address);
            return num3 && flag;
        }
        int num4 = input.LastIndexOf('/');
        if (num4 < 0)
        {
            filter.subnetMask = IPv4SubnetMask.SingleAddress;
            bool flag2 = IPv4Address.TryParsePortRange(input.Substring(num + 1), out filter.minPort, out filter.maxPort);
            return IPv4Address.TryParse(input, 0, num, out filter.address) && flag2;
        }
        string input2 = input.Substring(num4 + 1, num - num4 - 1);
        string input3 = input.Substring(num + 1);
        bool flag3 = IPv4SubnetMask.TryParse(input2, out filter.subnetMask);
        bool flag4 = IPv4Address.TryParsePortRange(input3, out filter.minPort, out filter.maxPort);
        bool num5 = IPv4Address.TryParse(input, 0, num4, out filter.address);
        filter.address = filter.subnetMask.MaskRoutingPrefix(filter.address);
        return num5 && flag3 && flag4;
    }
}
