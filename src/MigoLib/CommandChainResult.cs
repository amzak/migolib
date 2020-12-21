namespace MigoLib
{
    public class CommandChainResult<T>
    {
        public readonly IResultParser<T> ResultParser;

        public CommandChainResult(IResultParser<T> resultParser)
        {
            ResultParser = resultParser;
        }
    }
}