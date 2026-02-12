using System.Threading.Tasks;

using Darkages.Common;

using Xunit;

namespace ZolianTest.Unit.Common;

public sealed class AsyncLockTests
{
    [Fact]
    public async Task LockAsync_IsMutuallyExclusive()
    {
        var gate = new AsyncLock();

        var entered1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release1 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var entered2 = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var t1 = Task.Run(async () =>
        {
            using (await gate.LockAsync())
            {
                entered1.SetResult();
                await release1.Task.ConfigureAwait(false);
            }
        });

        var t2 = Task.Run(async () =>
        {
            await entered1.Task.ConfigureAwait(false);
            using (await gate.LockAsync())
            {
                entered2.SetResult();
            }
        });

        await entered1.Task.ConfigureAwait(false);

        // Ensure task2 has not entered yet.
        Assert.False(entered2.Task.IsCompleted);

        // Release task1 and ensure task2 can enter.
        release1.SetResult();
        await entered2.Task.ConfigureAwait(false);

        await Task.WhenAll(t1, t2).ConfigureAwait(false);
    }
}
