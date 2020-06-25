using IQFeed.CSharpApiClient.Streaming.Derivative;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace IQFeedConnectors
{
    /// <summary>
    /// Describes a basic connection to IQFeed.
    /// </summary>
    public class SimpleConnector : BaseConnector
    {
        private DerivativeClient client;
        private IReadOnlyCollection<string> Symbols { get; set; }

        /// <summary>
        /// Connects to IQFeed.
        /// </summary>
        /// <param name="host">The IQFeed host to connect to.</param>
        /// <param name="port">The IQFeed port to connect to.</param>
        public override async Task Connect(string host, int port)
        {
            client = DerivativeClientFactory.CreateNew(host, port);

            await client.ConnectAsync();
        }

        /// <summary>
        /// Disconnects from IQFeed.
        /// </summary>
        public override async Task Disconnect()
        {
            client.Error -= OnError;
            client.IntervalBar -= OnIntervalBar;
            client.SymbolNotFound -= OnSymbolNotFound;
            client.System -= OnSystemMessage;

            await client.UnwatchAllAsync();
            await client.DisconnectAsync();
            client = null;
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

            client.Error += OnError;
            client.IntervalBar += OnIntervalBar;
            client.SymbolNotFound += OnSymbolNotFound;
            client.System += OnSystemMessage;

            await Symbols.AsyncForEach(async symbol => await client.ReqBarWatchAsync(
                symbol, interval, beginDate: start, intervalType: intervalType));
        }
    }
}
