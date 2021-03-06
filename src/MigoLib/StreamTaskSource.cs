using System;
using System.Threading.Tasks.Sources;

namespace MigoLib
{
    internal class StreamTaskSource<T> : IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _manualResetValueTaskSource;

        public StreamTaskSource()
        {
            _manualResetValueTaskSource = new ManualResetValueTaskSourceCore<T>
            {
                RunContinuationsAsynchronously = false
            };
        }

        public bool IsCompleted()
        {
            var token = _manualResetValueTaskSource.Version;
            var status = _manualResetValueTaskSource.GetStatus(token);
            return status is ValueTaskSourceStatus.Succeeded 
                or ValueTaskSourceStatus.Canceled;
        }

        public void SetResult(T result) 
            => _manualResetValueTaskSource.SetResult(result);

        public void SetException(Exception ex) 
            => _manualResetValueTaskSource.SetException(ex);

        public void GetResult(short token) 
            => _manualResetValueTaskSource.GetResult(token);

        public ValueTaskSourceStatus GetStatus(short token) 
            => _manualResetValueTaskSource.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) 
            => _manualResetValueTaskSource.OnCompleted(continuation, state, token, flags);

        public short Reset()
        {
            _manualResetValueTaskSource.Reset();
            return _manualResetValueTaskSource.Version;
        }
    }
}