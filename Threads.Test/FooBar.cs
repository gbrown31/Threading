using System.Text;

namespace Threads.Test
{
    public class FoorBarTest
    {
        [Fact]
        public async Task PrintFooBarAsync()
        {
            int expectedWords = 5;
            string foo = "foo", bar = "bar";

            FooBar barf = new FooBar(expectedWords);
            StringBuilder fooBarBuilder = new StringBuilder();

            List<Task> tasks = new List<Task>()
            {
                Task.Run(() => barf.Bar(()=> {fooBarBuilder.Append(bar).Append(System.Environment.NewLine); })),
                Task.Run(() => barf.Foo(()=> {fooBarBuilder.Append(foo);}))
            };

            await Task.WhenAll(tasks);

            string[] fooBars = fooBarBuilder.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

            Assert.Equal(expectedWords, fooBars.Length);
            Assert.All(fooBars, (a) => a.Equals($"{foo}{bar}"));
        }
    }
    public class FooBar
    {
        private int n;
        private readonly SemaphoreSlim semaphoreFoo;
        private readonly SemaphoreSlim semaphoreBar;

        public FooBar(int n)
        {
            this.n = n;
            // 1 thread can access to start
            semaphoreFoo = new SemaphoreSlim(1, 1);

            // 0 thread can access to start, need to call release before it can be used
            semaphoreBar = new SemaphoreSlim(0, 1);
        }

        public void Foo(Action printFoo)
        {
            for (int i = 0; i < n; i++)
            {
                semaphoreFoo.Wait();

                printFoo();
                semaphoreBar.Release(1);
            }
        }

        public void Bar(Action printBar)
        {
            for (int i = 0; i < n; i++)
            {
                semaphoreBar.Wait();

                printBar();
                semaphoreFoo.Release(1);
            }
        }
    }
}
