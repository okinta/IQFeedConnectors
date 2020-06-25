using IQFeed.CSharpApiClient.Streaming.Common.Messages;
using IQFeed.CSharpApiClient.Streaming.Derivative.Messages;
using IQFeed.CSharpApiClient.Streaming.Derivative;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace IQFeedConnectors
{
    /// <summary>
    /// Describes methods for working with IQFeed.
    /// </summary>
    public abstract class BaseConnector : IConnector
    {
        /// <summary>
        /// Triggered when a bar is received by IQFeed.
        /// </summary>
        public event Action<IntervalBarMessage> BarReceived;

        private const DerivativeIntervalType intervalType = DerivativeIntervalType.S;
        private const int interval = 60;
        private readonly IReadOnlyCollection<string> MessagesToIgnore;
        private readonly IReadOnlyDictionary<string, Action<SystemMessage>> MessageMap;

        /// <summary>
        /// Instantiates the instance.
        /// </summary>
        public BaseConnector()
        {
            MessagesToIgnore = new List<string>(GetSystemMessagesToIgnore());
            MessageMap =
                new Dictionary<string, Action<SystemMessage>>(GetSystemMessageHandlers());
        }

        /// <summary>
        /// Connects to IQFeed.
        /// </summary>
        /// <param name="host">The IQFeed host to connect to.</param>
        /// <param name="port">The IQFeed port to connect to.</param>
        public abstract Task Connect(string host, int port);

        /// <summary>
        /// Disconnects from IQFeed.
        /// </summary>
        public abstract Task Disconnect();

        /// <summary>
        /// Subscribes IQFeed to the given list of symbols. Triggers the BarReceived event
        /// when new 1 minute bars are received.
        /// </summary>
        /// <param name="symbols">The symbols to subscribe to.</param>
        /// <param name="start">The date to start retrieving data from.</param>
        public async Task Run(IEnumerable<string> symbols, DateTime start)
        {
            await Run(symbols, start, intervalType, interval);
        }

        /// <summary>
        /// Subscribes IQFeed to the given list of symbols.
        /// </summary>
        /// <param name="symbols">The symbols to subscribe to.</param>
        /// <param name="start">The date to start retrieving data from.</param>
        /// <param name="intervalType">The interval type of the bars.</param>
        /// <param name="interval">The interval for each bar.</param>
        public abstract Task Run(
            IEnumerable<string> symbols, DateTime start,
            DerivativeIntervalType intervalType, int interval);

        /// <summary>
        /// Gets a list of system messages that can safely be ignored.
        /// </summary>
        /// <returns>A list of system messages that can be safely ignored.</returns>
        protected virtual IEnumerable<string> GetSystemMessagesToIgnore()
        {
            return new List<string>
            {
                "SERVER CONNECTED"
            };
        }

        /// <summary>
        /// Gets a list of system message handlers.
        /// </summary>
        /// <returns>A list of system messages and their associated handlers.</returns>
        protected virtual IEnumerable<KeyValuePair<string, Action<SystemMessage>>>
            GetSystemMessageHandlers()
        {
            return new List<KeyValuePair<string, Action<SystemMessage>>>
            {
                new KeyValuePair<string, Action<SystemMessage>>(
                    "SYMBOL LIMIT REACHED", OnSymbolLimitReached)
            };
        }

        /// <summary>
        /// Call to trigger the BarReceived event.
        /// </summary>
        /// <param name="bar">The new bar to send to BarReceived.</param>
        protected virtual void OnIntervalBar(IntervalBarMessage bar)
        {
            BarReceived.SafeTrigger(bar);
        }

        /// <summary>
        /// Call to throw an exception when an IQFeed error is received.
        /// </summary>
        /// <param name="message">The IQFeed error message.</param>
        protected virtual void OnError(ErrorMessage message)
        {
            throw new SystemException(message.Error);
        }

        /// <summary>
        /// Call to throw an exception when IQFeed is unable to find a symbol.
        /// </summary>
        /// <param name="message">The error messaged received from IQFeed.</param>
        protected virtual void OnSymbolNotFound(SymbolNotFoundMessage message)
        {
            throw new SystemException(string.Format(
                "IQFeed could not find symbol {0}", message.Symbol));
        }

        /// <summary>
        /// Call to process an IQFeed system message.
        /// </summary>
        /// <param name="message">The IQFeed system message received.</param>
        protected virtual void OnSystemMessage(SystemMessage message)
        {
            if (MessagesToIgnore.Any(m => m == message.Type))
            {
                return;
            }

            if (MessageMap.TryGetValue(message.Type, out var action))
            {
                action(message);
            }
            else
            {
                OnUnknownSystemMessage(message);
            }
        }

        /// <summary>
        /// Called when IQFeed's symbol limit has been reached. Throws an exception.
        /// </summary>
        /// <param name="message">The message received from IQFeed.</param>
        protected virtual void OnSymbolLimitReached(SystemMessage message)
        {
            var symbol = message.Message.Split(",").GetLast();
            throw new SystemException(string.Format(
                "IQFeed symbol limit reached. Unable to watch: {0}", symbol));
        }

        /// <summary>
        /// Called when an unknown IQFeed system message is received. Throws an exception.
        /// </summary>
        /// <param name="message">The IQFeed system message received.</param>
        protected virtual void OnUnknownSystemMessage(SystemMessage message)
        {
            throw new InvalidOperationException(string.Format(
                "Unknown IQFeed message: {0}", message.Message));
        }
    }
}
