using System.ComponentModel;
using System.Net;

namespace MigoLib
{
    [TypeConverter(typeof(MigoEndpointTypeConverter))]
    public struct MigoEndpoint
    {
        public IPAddress Ip { get; }
        public ushort Port { get; }

        public MigoEndpoint(IPAddress ip, ushort port)
        {
            Ip = ip;
            Port = port;
        }

        public MigoEndpoint(string ip, ushort port)
            : this(IPAddress.Parse(ip), port)
        {
        }

        public override string ToString() => $"{Ip}:{Port.ToString()}";

        public void Deconstruct(out IPAddress ip, out ushort port)
        {
            ip = Ip;
            port = Port;
        }
    }
}