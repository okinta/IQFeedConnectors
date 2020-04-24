using IQFeed.CSharpApiClient.Streaming.Derivative.Messages;
using IQFeed.CSharpApiClient.Streaming.Derivative;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System;

namespace IQFeedConnectors
{
    /// <summary>
    /// Connector that groups work up by ticker and processes events inside separate
    /// threads.
    /// </summary>
    public class QueuedConnector : BaseConnector
    {
        private DerivativeClient<double> client;
        private IReadOnlyCollection<string> Symbols { get; set; }
        private TaskQueue<string, IntervalBarMessage<double>> TaskQueue { get; set; }

        /// <summary>
        /// Connects to IQFeed.
        /// </summary>
        /// <param name="host">The IQFeed host to connect to.</param>
        /// <param name="port">The IQFeed port to connect to.</param>
        public override async Task Connect(string host, int port)
        {
            var ip = IPAddress.Parse(host);
            client = DerivativeClientFactory.CreateNew(ip, port);

            await client.ConnectAsync();
        }

        /// <summary>
        /// Disconnects from IQFeed.
        /// </summary>
        public override async Task Disconnect()
        {
            client.Error -= OnError;
            client.IntervalBar -= ScheduleIntervalBar;
            client.SymbolNotFound -= OnSymbolNotFound;
            client.System -= OnSystemMessage;
            TaskQueue.RunTask -= OnRunTask;

            await client.UnwatchAllAsync();
            await client.DisconnectAsync();
            await TaskQueue.Stop();
            client = null;
            TaskQueue = null;
        }

        /// <summary>
        /// Subscribes IQFeed to the given list of symbols.
        /// </summary>
        /// <param name="symbols">The symbols to subscribe to.</param>
        /// <param name="start">The date to start retrieving data from.</param>
        /// <param name="intervalType">The interval type of the bars.</param>
        /// <param name="interval">The interval for each bar.</param>
        public override async Task Run(
            IEnumerable<string> symbols, DateTime start,
            DerivativeIntervalType intervalType, int interval)
        {
            Symbols = new HashSet<string>(symbols);
            TaskQueue = new TaskQueue<string, IntervalBarMessage<double>>(Symbols);
            TaskQueue.RunTask += OnRunTask;
            TaskQueue.Start();

            client.Error += OnError;
            client.IntervalBar += ScheduleIntervalBar;
            client.SymbolNotFound += OnSymbolNotFound;
            client.System += OnSystemMessage;

            await Symbols.AsyncForEach(async symbol => await client.ReqBarWatchAsync(
                symbol, interval, beginDate: start, intervalType: intervalType));
        }

        /// <summary>
        /// Called when a new bar is receievd from IQFeed. Schedules the triggering of
        /// the BarReceived event.
        /// </summary>
        /// <param name="bar">The bar received from IQFeed.</param>
        private async void ScheduleIntervalBar(IntervalBarMessage<double> bar)
        {
            await TaskQueue.Add(bar.Symbol, bar);
        }

        /// <summary>
        /// Called when a new task from the queue is processed. Triggers the BarReceived
        /// event.
        /// </summary>
        /// <param name="bar">The bar received from IQFeed.</param>
        private void OnRunTask(IntervalBarMessage<double> bar)
        {
            OnIntervalBar(bar);
        }
    }
}
