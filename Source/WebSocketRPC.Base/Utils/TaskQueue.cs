using System;
using System.Threading;
using System.Threading.Tasks;

namespace WebSocketRPC
{
    //taken from: https://stackoverflow.com/questions/25691679/best-way-in-net-to-manage-queue-of-tasks-on-a-separate-single-thread and modified
    class TaskQueue
    {
        private SemaphoreSlim semaphore;
        public TaskQueue()
        {
            semaphore = new SemaphoreSlim(1);
        }

        public async Task Enqueue(Func<Task> func)
        {
            Interlocked.Increment(ref count);
            await semaphore.WaitAsync();
            try
            {
                await func();
            }
            finally
            {
                Interlocked.Decrement(ref count);
                semaphore.Release();
            }
        }

        int count = 0;
        public int Count => count;

        ~TaskQueue()
        {
            //dispose pattern is not needed because' AvailableWaitHandle' is not used
            //ref: https://stackoverflow.com/questions/32033416/do-i-need-to-dispose-a-semaphoreslim
            semaphore.Dispose(); 
        }
    }
}
