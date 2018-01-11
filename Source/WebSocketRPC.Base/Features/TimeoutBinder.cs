using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace WebSocketRPC
{
    /// <summary>
    /// Represents a timeout binder which closes a connection after a specified timeout is due.
    /// </summary>
    class TimeoutBinder
    {
        Timer timer;
        Connection connection;
        string closeMessage;

        /// <summary>
        /// Creates new timeout binder.
        /// </summary>
        /// <param name="connection">Connection.</param>
        /// <param name="timeout">Idle timeout which is reset on each received message.</param>
        /// <param name="closeMessage">Close message.</param>
        public TimeoutBinder(Connection connection, TimeSpan timeout, string closeMessage)
        {
            if (timeout.TotalMilliseconds <= 0)
                throw new ArgumentException(nameof(timeout), "The value must be strictly positive.");

            this.connection = connection;
            this.closeMessage = closeMessage;

            timer = new Timer(timeout.TotalMilliseconds);
            timer.AutoReset = false;
            timer.Elapsed += Timer_Elapsed;

            connection.OnOpen += () => { timer.Enabled = true; return Task.FromResult(true); };
            connection.OnReceive += msg =>
            {
                timer.Enabled = false;
                timer.Enabled = true;
                return Task.FromResult(true);
            };
        }

        private async void Timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            timer.Dispose();
            await connection.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation, closeMessage);
        }
    }

    /// <summary>
    /// Idle-timeout binder extension.
    /// </summary>
    public static class TimeoutBinderExtension
    {
        internal static List<TimeoutBinder> timeoutBinders = new List<TimeoutBinder>();

        /// <summary>
        /// Binds a new idle-timeout binder to the provided connection.
        /// <para>A timeout binder closes a connection after a specified timeout is due.</para>
        /// </summary>
        /// <param name="connection">Connection.</param>
        /// <param name="timeout">Idle-timeout. Interval is reset on each received message.</param>
        /// <param name="closeMessage">Message sent if connection is closed due to timeout.</param>
        public static void BindTimeout(this Connection connection, TimeSpan timeout, string closeMessage = "Idle time elapsed.")
        {
            var binder = new TimeoutBinder(connection, timeout, closeMessage);

            lock (timeoutBinders) timeoutBinders.Add(binder);
            connection.OnClose += (s, d) => 
            {
                lock (timeoutBinders) timeoutBinders.Remove(binder);
                return Task.FromResult(true);
            };
        }
    }
}
