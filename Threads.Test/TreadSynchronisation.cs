using System.Collections.Concurrent;

namespace Threads.Test;

public class TreadSynchronisation
{
    private ConcurrentQueue<int> myQueue = new ConcurrentQueue<int>();
    private static readonly object locker = new object();

    [Fact]
    public async Task MonitorEnterExit()
    {
        Task<bool> lockWithMonitorTask = Task.Factory.StartNew<bool>(() =>
        {
            LockWithMonitor();
            return true;
        });
        var item = await lockWithMonitorTask;
        LockWithMonitor();
        LockWithLock();

        Assert.Equal(3, myQueue.Count);
    }

    [Fact]
    public async Task MonitorEnterExitWaitAndAwait()
    {
        Task<bool> lockWithMonitorTask = Task.Factory.StartNew<bool>(() =>
        {
            LockWithMonitor();
            return true;
        });
        lockWithMonitorTask.Wait();
        var item = await lockWithMonitorTask;
        LockWithMonitor();
        LockWithLock();

        Assert.Equal(3, myQueue.Count);
    }

    private void LockWithMonitor()
    {
        bool lockTaken = false;

        try
        {
            Monitor.Enter(locker, ref lockTaken);
            myQueue.Enqueue(1);
        }
        finally
        {
            if (lockTaken)
            {
                Monitor.Exit(locker);
            }
        }
    }

    private void LockWithLock()
    {
        lock (locker)
        {
            myQueue.Enqueue(1);
        }
    }

    private ConcurrentBag<int> counter = new ConcurrentBag<int>();

    [Fact]
    public void CreateDeadlockScenario()
    {
        object object1 = new object(),
            object2 = new object();

        Task deadlock = Task.Factory.StartNew(() =>
        {
            lock (object1)
            {
                Thread.Sleep(5000);
                lock (object2)
                {
                    counter.Add(1);
                }
            }
        });

        bool lock1Taken = false,
            lock2Taken = false;
        try
        {
            Monitor.TryEnter(object2, 100, ref lock2Taken);
            if (lock2Taken)
            {
                Monitor.TryEnter(object1, 100, ref lock1Taken);
                if (lock1Taken)
                {
                    counter.Add(1);
                }
            }
        }
        finally
        {
            if (lock1Taken) Monitor.Exit(object1);
            if (lock2Taken) Monitor.Exit(object2);
        }

        Assert.Equal(1, counter.Count);
    }

    [Fact]
    public void MutexIsLock()
    {
        ConcurrentStack<int> stack = new ConcurrentStack<int>();
        var mutex = new Mutex(true, "testingMutex");

        Task myTask = Task.Factory.StartNew(() =>
        {
            if (mutex.WaitOne(10))
            {
                Thread.Sleep(1000);
                stack.Push(1);
                mutex.ReleaseMutex();
            }
        });

        if (mutex.WaitOne(500))
        {
            stack.Push(2);
            mutex.ReleaseMutex();
        }

        Assert.Equal(1, stack.Count);
        stack.TryPop(out int result);
        Assert.Equal(2, result);
    }

    [Fact]
    public async Task SemaphoreAsQueue()
    {
        SemaphoreSlim semaphore = new SemaphoreSlim(5);
        ConcurrentStack<int> stack = new ConcurrentStack<int>();

        int maxStackCount = 0;
        List<Task> stackTasksWithSemaphore = new List<Task>();
        for (int x = 0; x < 10; x++)
        {
            stackTasksWithSemaphore.Add(
                Task.Factory.StartNew(() =>
                {
                    semaphore.Wait();
                    Thread.Sleep(200);
                    stack.Push(x);

                    maxStackCount = Math.Max(maxStackCount, stack.Count);
                    Thread.Sleep(200);

                    semaphore.Release();
                    stack.TryPop(out int result);
                })
            );
            
        }
        await Task.WhenAll(stackTasksWithSemaphore);

        Assert.Equal(5, maxStackCount);
    }
}