namespace MigoLib
{
    public class CommandChainResult<T>
    {
        private readonly CommandChain _commandChain;
        public readonly IResultParser<T> ResultParser;

        public CommandChainResult(CommandChain commandChain, IResultParser<T> resultParser)
        {
            _commandChain = commandChain;
            ResultParser = resultParser;
        }
    }
}