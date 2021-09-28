using MigoLib;

namespace MigoToolCli.Commands
{
    public class MigoFactory
    {
        public static Migo Create(MigoEndpoint endpoint, ErrorHandlingPolicy errorHandlingPolicy = default)
        {
            errorHandlingPolicy ??= ErrorHandlingPolicy.Default;
            
            return new(Program.LoggerFactory, endpoint, errorHandlingPolicy);
        }
    }
}