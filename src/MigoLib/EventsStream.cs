using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MigoLib
{
    public class EventsStream<T> : IAsyncEnumerator<T>, IAsyncEnumerable<T>
    {
        private readonly CancellationToken _token;
        private readonly StreamTaskSource<T> _taskSource;
        private readonly ConcurrentQueue<T> _results;
        private short _nextToken;
        private volatile bool _isCompleted;

        private T _current;

        public EventsStream(CancellationToken token)
        {
            _token = token;
            _token.Register(CancelStream);
            _taskSource = new StreamTaskSource<T>();
            _results = new ConcurrentQueue<T>();
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

        public void SetNextEvent(T anEvent)
        {
            if (_taskSource.IsCompleted())
            {
                // _taskSource is not ready to receive new results 
                _results.Enqueue(anEvent);
                return;
            }

            _current = anEvent;
            _taskSource.SetResult(anEvent);
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

            var continueStream = !_token.IsCancellationRequested;
            _isCompleted = !continueStream;
            return continueStream;
        }

        public T Current => 
            _results.TryDequeue(out var result) 
                ? result 
                : _current;

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = new())
        {
            return this;
        }

        public bool IsCompleted => _isCompleted;

        public int BufferSize => _results.Count;
    }
}