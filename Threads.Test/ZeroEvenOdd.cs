using System.Collections.Concurrent;
using System.Text;

namespace Threads.Test
{
    public class ZeroEvenOddTests
    {
        [Fact]
        public async Task PrintInOrderAsync()
        {
            string expectedString = "0102030405";

            ZeroEvenOdd sut = new ZeroEvenOdd(5);
            ConcurrentBag<string> messages=  new ConcurrentBag<string>();
            StringBuilder sbNumbers = new StringBuilder();
            List<int> numbers= new List<int>();

            List<Task> tasks = new List<Task> {
                Task.Run(() => sut.Zero((a) => { sbNumbers.Append(a); })),
                Task.Run(() => sut.Even((a) => { sbNumbers.Append(a);})),
                Task.Run(() => sut.Odd((a) => { sbNumbers.Append(a); })),
            };

            await Task.WhenAll(tasks);

            Assert.Equal(expectedString, sbNumbers.ToString());
        }
    }
    internal class ZeroEvenOdd
    {
        private int n;
        private readonly SemaphoreSlim semaphoreZero;
        private readonly SemaphoreSlim semaphoreOdd;
        private readonly SemaphoreSlim semaphoreEVen;

        public ZeroEvenOdd(int n)
        {
            this.n = n;
            // 1 thread can access to start
            semaphoreZero = new SemaphoreSlim(1, 1);
            semaphoreOdd = new SemaphoreSlim(0, 1);
            semaphoreEVen = new SemaphoreSlim(0, 1);
        }

        // printNumber(x) outputs "x", where x is an integer.
        public void Zero(Action<int> printNumber)
        {
            semaphoreZero.Wait();
            printNumber(0);

            semaphoreOdd.Release(1);
        }

        public void Odd(Action<int> printNumber)
        {
            for(int i = 1; i <= n; i++)
            {
                if(i % 2 > 0)
                {
                    semaphoreOdd.Wait();

                    if (i > 1)
                    {
                        printNumber(0);
                    }
                    printNumber(i);

                    semaphoreEVen.Release(1);
                }
            }
        }

        public void Even(Action<int> printNumber)
        {
            for (int i = 1; i <= n; i++)
            {
                if (i % 2 == 0)
                {
                    semaphoreEVen.Wait();
                    printNumber(0);
                    printNumber(i);

                    semaphoreOdd.Release(1);
                }
            }
        }
    }
}
