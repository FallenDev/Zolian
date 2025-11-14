namespace Darkages.Common;

public sealed class AsyncLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Task<IDisposable> _releaserTask;

    public AsyncLock()
    {
        _releaserTask = Task.FromResult((IDisposable)new Releaser(this));
    }

    public Task<IDisposable> LockAsync()
    {
        var wait = _semaphore.WaitAsync();
        return wait.IsCompleted ?
            _releaserTask :
            wait.ContinueWith(
                (_, state) => (IDisposable)new Releaser((AsyncLock)state!),
                this,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
    }

    private sealed class Releaser : IDisposable
    {
        private readonly AsyncLock _toRelease;
        public Releaser(AsyncLock toRelease) => _toRelease = toRelease;
        public void Dispose() => _toRelease._semaphore.Release();
    }
}
