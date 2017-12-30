using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tests
{
    public partial class Program
    {
        const string address = "localhost:8001/";

        static void RunTest(Func<CancellationTokenSource, Task[]> testFunc)
        {
            var cts = new CancellationTokenSource();
            var tasks = testFunc(cts);

            foreach (var t in tasks)
                t.ContinueWith(_ => cts.Cancel(), TaskContinuationOptions.NotOnRanToCompletion);

            Task.Run(() => 
            {
                //Console.WriteLine("Press [Enter] or [Ctrl+C] to quit.");
                Console.ReadLine();

                cts.Cancel();
                Console.WriteLine("Waiting for the tasks to finish.");
            });

            try
            {
                Task.WaitAll(tasks);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Task completion error: {0}", ex?.Message);
            }
        }

        static void Main(string[] args)
        {
            //RunTest(TestConnectionException);

            //RunTest(TestRpcInitializeException);
            //RunTest(TestRpcUnhandledException);
            //RunTest(TestRpcHandledException);

            //RunTest(TestMaxMessageSize);

            //RunTest(TestTimeout);

            RunTest(TestMultiClient);
        }
    }
}
