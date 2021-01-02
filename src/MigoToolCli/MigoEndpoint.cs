using System.ComponentModel;
using System.Net;

namespace MigoToolCli
{
    [TypeConverter(typeof(MigoEndpointTypeConverter))]
    public class MigoEndpoint
    {
        public IPAddress Ip { get; set; }
        public ushort Port { get; set; }
    }
}