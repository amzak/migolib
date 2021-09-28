using MigoLib.Fake;

namespace MigoLib.Tests
{
    public static class TestEnvironment
    {
        public const string Ip = "127.0.0.1";
        public const ushort Port = 5100;
        
        public static MigoEndpoint Endpoint = new(Ip, Port);
        public static FakeMigo FakeMigo { get; set; }
    }
}