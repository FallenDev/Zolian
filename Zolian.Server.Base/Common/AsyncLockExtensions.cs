namespace Darkages.Common;

public static class AsyncLockExtensions
{
    public static async Task<IDisposable?> TryLockAsync(this AsyncLock locker, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            return await locker.LockAsync().WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
