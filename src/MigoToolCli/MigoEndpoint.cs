using System.ComponentModel;
using System.Net;

namespace MigoToolCli
{
    [TypeConverter(typeof(MigoEndpointTypeConverter))]
    public class MigoEndpoint
    {
        public IPAddress Ip { get; set; }
        public ushort Port { get; set; }

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
    }
}