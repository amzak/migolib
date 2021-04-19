using System.Buffers;
using System.Collections.Concurrent;
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
        private readonly ConcurrentQueue<object> _results;
        private short _nextToken;
        private volatile bool _isCompleted;

        public StreamedReply(IResultParser parser, CancellationToken token)
        {
            _parser = parser;
            _token = token;
            _token.Register(CancelStream);
            _taskSource = new StreamTaskSource<object>();
            _results = new ConcurrentQueue<object>();
        }

        private void CancelStream()
        {
            if (_taskSource.IsCompleted())
            {
                return;
            }

            var exception = new TaskCanceledException();
            _taskSource.SetException(exception);
        }

        public void Parse(ReadOnlySequence<byte> resultBuffer)
        {
            if (!_parser.TryParse(resultBuffer))
            {
                return;
            }

            if (_taskSource.IsCompleted())
            {
                // _taskSource is not ready to receive new results 
                _results.Enqueue(_parser.ResultObject);
                return;
            }

            _taskSource.SetResult(_parser.ResultObject);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public async ValueTask<bool> MoveNextAsync()
        {
            if (!_results.IsEmpty)
            {
                // pending results first
                _isCompleted = !_token.IsCancellationRequested;
                return _isCompleted;
            }

            if (_token.IsCancellationRequested)
            {
                return false;
            }

            await new ValueTask(_taskSource, _nextToken)
                .ConfigureAwait(false);

            _nextToken = _taskSource.Reset(); 

            var continueStream = _parser.ResultObject != null && !_token.IsCancellationRequested;
            _isCompleted = !continueStream;
            return continueStream;
        }

        public object Current => 
            _results.TryDequeue(out var result) 
                ? result 
                : _parser.ResultObject;

        public IAsyncEnumerator<object> GetAsyncEnumerator(CancellationToken cancellationToken = new CancellationToken())
        {
            return this;
        }

        public bool IsCompleted => _isCompleted;

        public int BufferSize => _results.Count;
    }
}