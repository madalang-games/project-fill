using System.Collections.Concurrent;

namespace ProjectFill.Infrastructure.Concurrency;

public sealed class UserSerializer
{
    private readonly ConcurrentDictionary<string, LockState> _locks = new();

    public async Task<IAsyncDisposable> AcquireAsync(string pid, TimeSpan timeout, CancellationToken ct)
    {
        var state = _locks.GetOrAdd(pid, _ => new LockState());
        state.AddReference();

        var acquired = await state.Semaphore.WaitAsync(timeout, ct);
        if (!acquired)
        {
            ReleaseReference(pid, state);
            throw new TimeoutException($"Timed out acquiring user lock for pid {pid}.");
        }

        return new Lease(() => Release(pid, state));
    }

    private void Release(string pid, LockState state)
    {
        state.Semaphore.Release();
        ReleaseReference(pid, state);
    }

    private void ReleaseReference(string pid, LockState state)
    {
        if (state.RemoveReference())
            _locks.TryRemove(new KeyValuePair<string, LockState>(pid, state));
    }

    private sealed class LockState
    {
        private int _references;
        public SemaphoreSlim Semaphore { get; } = new(1, 1);

        public void AddReference() => Interlocked.Increment(ref _references);
        public bool RemoveReference() => Interlocked.Decrement(ref _references) == 0;
    }

    private sealed class Lease : IAsyncDisposable
    {
        private readonly Action _release;
        private int _disposed;

        public Lease(Action release) => _release = release;

        public ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) == 0)
                _release();
            return ValueTask.CompletedTask;
        }
    }
}
