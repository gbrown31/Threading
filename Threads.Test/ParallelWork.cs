namespace Threads.Test;

public class ParallelWork
{
    [Fact]
    public void ParallelQuery_Cancelled_ThrowsOperationCancelled()
    {
        IEnumerable<int> million = Enumerable.Range(3, 1000000);

        var cancelationSource = new CancellationTokenSource();

        var primeNumberQuery = (
            from n in million.AsParallel().WithCancellation(cancelationSource.Token)
            where Enumerable.Range(2, (int) Math.Sqrt(n)).All(i => n % i > 0)
            select n
        );

        new Thread(() =>
        {
            Thread.Sleep(10);
            cancelationSource.Cancel();
        }).Start();

        Assert.Throws<OperationCanceledException>(() => primeNumberQuery.ToArray());
    }
}