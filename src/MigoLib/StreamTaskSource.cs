using System;
using System.Threading.Tasks.Sources;

namespace MigoLib
{
    internal class StreamTaskSource<T> : IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<T> _manualResetValueTaskSource;

        public StreamTaskSource()
        {
            _manualResetValueTaskSource = new ManualResetValueTaskSourceCore<T>();
        }

        public void SetResult(T result) 
            => _manualResetValueTaskSource.SetResult(result);

        public void GetResult(short token) 
            => _manualResetValueTaskSource.GetResult(token);

        public ValueTaskSourceStatus GetStatus(short token) 
            => _manualResetValueTaskSource.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) 
            => _manualResetValueTaskSource.OnCompleted(continuation, state, token, flags);

        public void Reset() => _manualResetValueTaskSource.Reset();
    }
}