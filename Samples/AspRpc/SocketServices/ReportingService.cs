using System;
using System.Threading;
using System.Threading.Tasks;
using WebSocketRPC;

namespace AspRpc
{
    interface IClientUpdate
    {
        void Write(string message);

        void OnStart();

        void OnStop();
    }

    /// <summary>
    /// Reporting service.
    /// </summary>
    class ReportingService
    {
        CancellationTokenSource cts = null;
        Task reportTask = null;

        /// <summary>
        /// Starts the reporting service.
        /// </summary>
        /// <returns>Task.</returns>
        /// <exception cref="NotSupportedException"></exception>
        public async Task Start()
        {
            if (reportTask != null)
                throw new NotSupportedException("The service is running. Please stop it first.");

            await RPC.For<IClientUpdate>().CallAsync(x => x.OnStart());
            cts = new CancellationTokenSource();
            reportTask = startReporting(cts.Token);
        }

        int i = 0;
        async Task startReporting(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await RPC.For<IClientUpdate>().CallAsync(x => x.Write("  Reporting: " + i));
                await Task.Delay(250);
                i++;
            }
        }

        /// <summary>
        /// Gets whether the service is running or not.
        /// </summary>
        /// <returns>True if the service is running, false otherwise.</returns>
        public bool IsRunning()
        {
            return reportTask !=null;
        }

        /// <summary>
        /// Stops the reporting service.
        /// </summary>
        /// <returns>Task</returns>
        /// <exception cref="NotSupportedException"></exception>
        public async Task Stop()
        {
            if (cts == null)
                throw new NotSupportedException("The service is stopped. Please start it first.");

            cts?.Cancel();
            await reportTask;
            reportTask = null;

            await RPC.For<IClientUpdate>().CallAsync(x => x.OnStop());
        }
    }
}
