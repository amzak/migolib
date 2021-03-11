using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigoLib
{
    public class StreamedReply : IAsyncEnumerator<object>, IAsyncEnumerable<object>
    {
        private readonly IResultParser _parser;
        private readonly CancellationToken _token;
        private readonly StreamTaskSource<object> _taskSource;

        public StreamedReply(IResultParser parser, CancellationToken token)
        {
            _parser = parser;
            _token = token;
            _taskSource = new StreamTaskSource<object>();
        }
        
        public void Parse(ReadOnlySequence<byte> resultBuffer)
        {
            if (!_parser.TryParse(resultBuffer))
            {
                return;
            }
            
            _taskSource.SetResult(_parser.ResultObject);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public async ValueTask<bool> MoveNextAsync()
        {
            _taskSource.Reset();
            await new ValueTask(_taskSource, 1)
                .ConfigureAwait(false);
            return _parser.ResultObject != null && !_token.IsCancellationRequested;
        }

        public object Current => _parser.ResultObject;
        
        public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return this;
        }
    }
}