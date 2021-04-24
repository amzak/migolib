using System;
using System.ComponentModel;
using System.Net;

namespace MigoLib
{
    [TypeConverter(typeof(MigoEndpointTypeConverter))]
    public struct MigoEndpoint : IEquatable<MigoEndpoint>
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

        public MigoEndpoint(string connection)
        {
            var parts = connection.Split(':');
            if (parts.Length != 2)
            {
                throw new ArgumentException("Invalid connection", nameof(connection));
            }

            ushort port = 0;
            var success = IPAddress.TryParse(parts[0], out var ip) 
                          && ushort.TryParse(parts[1], out port);

            if (!success)
            {
                throw new ArgumentException("Can't parse connection, expected 'ip:port'", nameof(connection));
            }

            Ip = ip;
            Port = port;
        }

        public override string ToString() => $"{Ip}:{Port.ToString()}";

        public void Deconstruct(out IPAddress ip, out ushort port)
        {
            ip = Ip;
            port = Port;
        }

        public bool IsValid()
        {
            return Ip != null && Port > 0;
        }

        public static bool IsValid(string connection)
        {
            if (string.IsNullOrEmpty(connection))
            {
                return false;
            }
            
            var parts = connection.Split(':');
            if (parts.Length != 2)
            {
                return false;
            }

            return IPAddress.TryParse(parts[0], out _) && ushort.TryParse(parts[1], out _);
        }

        public bool Equals(MigoEndpoint other)
        {
            return Equals(Ip, other.Ip) && Port == other.Port;
        }

        public override bool Equals(object obj)
        {
            return obj is MigoEndpoint other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Ip, Port);
        }
    }
}