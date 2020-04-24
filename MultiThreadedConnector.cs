using IQFeed.CSharpApiClient.Streaming.Derivative.Messages;
using System.Threading.Tasks;
using System;

namespace IQFeedConnectors
{
    /// <summary>
    /// Describes methods to work with IQFeed using multiple simultaneous connections.
    /// Processing of each bar occurs in a separate thread.
    /// </summary>
    public class MultiThreadedConnector : MultiConnector
    {
        /// <summary>
        /// Instantiates the instance with 2 simultaneous connections.
        /// </summary>
        public MultiThreadedConnector() : base()
        {
        }

        /// <summary>
        /// Instantiates the instance with the given number of simultaneous connections.
        /// </summary>
        /// <param name="n"></param>
        /// <exception cref="ArgumentOutOfRangeException">If n is smaller than
        /// 1.</exception>
        public MultiThreadedConnector(int n) : base(n)
        {
        }

        /// <summary>
        /// Called when a new bar is recieved by IQFeed. Triggers the BarReceived event in
        /// a new thread.
        /// </summary>
        /// <param name="bar">The new bar received from IQFeed.</param>
        protected override async void OnIntervalBar(IntervalBarMessage<double> bar)
        {
            await Task.Run(() => base.OnIntervalBar(bar));
        }
    }
}
