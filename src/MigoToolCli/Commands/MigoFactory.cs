using MigoLib;

namespace MigoToolCli.Commands
{
    public class MigoFactory
    {
        public static Migo Create(MigoEndpoint endpoint) 
            => new(Program.LoggerFactory, endpoint.Ip.ToString(), endpoint.Port);
    }
}