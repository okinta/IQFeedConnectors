using DnsClient.Protocol;
using DnsClient;
using IQFeed.CSharpApiClient.Streaming.Derivative;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Threading;
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
        public override async Task Connect(
            string host, int port, CancellationToken token = default)
        {
            IPEndPoint ipEndPoint;
            try
            {
                ipEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            }

            // If we are given a domain, perform a DNS lookup to find the host
            catch (FormatException)
            {
                var ip = await GetIpAddress(host, token);
                ipEndPoint = new IPEndPoint(ip, port);
            }

            client = DerivativeClientFactory.CreateNew(ipEndPoint.Address.ToString(), port);

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

        /// <summary>
        /// Performs a DNS query to resolve the given host to an IPAddress.
        /// </summary>
        /// <param name="host">The host to resolve.</param>
        /// <param name="token">The token to check for cancellation requests.</param>
        /// <returns>The resolved IPAddress of the <paramref name="host"/>.</returns>
        private static async Task<IPAddress> GetIpAddress(
            string host, CancellationToken token)
        {
            var lookupClient = new LookupClient();
            var result = await lookupClient.QueryAsync(
                new DnsQuestion(host, QueryType.A), token);

            // Pick a random record
            var record = result.AllRecords
                .OfType<AddressRecord>()
                .OrderBy(x => Guid.NewGuid())
                .First();

            return record.Address;
        }
    }
}
