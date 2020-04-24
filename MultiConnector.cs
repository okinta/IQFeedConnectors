using IQFeed.CSharpApiClient.Streaming.Derivative;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System;

namespace IQFeedConnectors
{
    /// <summary>
    /// Describes methods to work with IQFeed using multiple simultaneous connections.
    /// </summary>
    public class MultiConnector : BaseConnector
    {
        protected readonly IList<DerivativeClient<double>> clients;
        protected readonly int n;

        private const int defaultN = 2;
        private IReadOnlyCollection<string> Symbols { get; set; }

        /// <summary>
        /// Instantiates the instance with 2 simultaneous connections.
        /// </summary>
        public MultiConnector()
        {
            n = defaultN;
            clients = new List<DerivativeClient<double>>(n);
        }

        /// <summary>
        /// Instantiates the instance with the given number of simultaneous connections.
        /// </summary>
        /// <param name="n"></param>
        /// <exception cref="ArgumentOutOfRangeException">If n is smaller than
        /// 1.</exception>
        public MultiConnector(int n)
        {
            if (n < 1)
            {
                throw new ArgumentOutOfRangeException("must be larger than 0", "n");
            }

            this.n = n; this.n = n;
            clients = new List<DerivativeClient<double>>(n);
        }

        /// <summary>
        /// Connects to IQFeed.
        /// </summary>
        /// <param name="host">The IQFeed host to connect to.</param>
        /// <param name="port">The IQFeed port to connect to.</param>
        public override async Task Connect(string host, int port)
        {
            var ip = IPAddress.Parse(host);

            for (var i = 0; i < n; ++i)
            {
                var client = DerivativeClientFactory.CreateNew(ip, port);
                clients.Add(client);
            }

            await clients.AsyncForEach(async client => await client.ConnectAsync());
        }

        /// <summary>
        /// Disconnects from IQFeed.
        /// </summary>
        public override async Task Disconnect()
        {
            foreach (var client in clients)
            {
                await Diconnect(client);
            }

            clients.Clear();
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
            var chunks = symbols.Split(n);
            var i = 0;
            await chunks.AsyncForEach(async chunk =>
            {
                await Run(clients[i], chunk, start, intervalType, interval);
                ++i;
            });
        }

        /// <summary>
        /// Subscribes the given IQFeed client to the given list of symbols.
        /// </summary>
        /// <param name="client">The IQFeed client to subscribe.</param>
        /// <param name="symbols">The symbols to subscribe to.</param>
        /// <param name="start">The date to start retrieving data from.</param>
        /// <param name="intervalType">The interval type of the bars.</param>
        /// <param name="interval">The interval for each bar.</param>
        private async Task Run(
            DerivativeClient<double> client, IEnumerable<string> symbols, DateTime start,
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

        /// <summary>
        /// Disconnects the given IQFeed client.
        /// </summary>
        /// <param name="client">The IQFeed client to disconnect.</param>
        private async Task Diconnect(DerivativeClient<double> client)
        {
            client.Error -= OnError;
            client.IntervalBar -= OnIntervalBar;
            client.SymbolNotFound -= OnSymbolNotFound;
            client.System -= OnSystemMessage;

            await client.UnwatchAllAsync();
            await client.DisconnectAsync();
        }
    }
}
