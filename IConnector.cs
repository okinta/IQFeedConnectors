using IQFeed.CSharpApiClient.Streaming.Derivative.Messages;
using IQFeed.CSharpApiClient.Streaming.Derivative;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace IQFeedConnectors
{
    /// <summary>
    /// Describes an interface for working with IQFeed.
    /// </summary>
    public interface IConnector
    {
        /// <summary>
        /// Triggered when a bar is received by IQFeed.
        /// </summary>
        public event Action<IntervalBarMessage> BarReceived;

        /// <summary>
        /// Connects to IQFeed.
        /// </summary>
        /// <param name="host">The IQFeed host to connect to.</param>
        /// <param name="port">The IQFeed port to connect to.</param>
        /// <param name="token">The token to check for a cancelation request.</param>
        public Task Connect(string host, int port, CancellationToken token = default);

        /// <summary>
        /// Disconnects from IQFeed.
        /// </summary>
        public Task Disconnect();

        /// <summary>
        /// Subscribes IQFeed to the given list of symbols. Triggers the BarReceived event
        /// when new 1 minute bars are received.
        /// </summary>
        /// <param name="symbols">The symbols to subscribe to.</param>
        /// <param name="start">The date to start retrieving data from.</param>
        public Task Run(IEnumerable<string> symbols, DateTime start);

        /// <summary>
        /// Subscribes IQFeed to the given list of symbols.
        /// </summary>
        /// <param name="symbols">The symbols to subscribe to.</param>
        /// <param name="start">The date to start retrieving data from.</param>
        /// <param name="intervalType">The interval type of the bars.</param>
        /// <param name="interval">The interval for each bar.</param>
        public Task Run(
            IEnumerable<string> symbols, DateTime start,
            DerivativeIntervalType intervalType, int interval);
    }
}
