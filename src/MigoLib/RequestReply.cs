using System.Threading.Tasks;

namespace MigoLib
{
    internal class RequestReply
    {
        public readonly IResultParser Parser;
        private readonly TaskCompletionSource<object> _completionSource;

        public RequestReply(IResultParser parser, TaskCompletionSource<object> completionSource)
        {
            Parser = parser;
            _completionSource = completionSource;
        }

        public void Complete()
        {
            _completionSource.SetResult(Parser.ResultObject);
        }
    }
}