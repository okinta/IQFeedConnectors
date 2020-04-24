using Nito.AsyncEx;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace IQFeedConnectors
{
    /// <summary>
    /// Describes methods to execute work inside a thread pool, grouped by a key.
    /// </summary>
    /// <typeparam name="TKey">The key to group work by.</typeparam>
    /// <typeparam name="TValue">The argument to send to each task.</typeparam>
    internal class TaskQueue<TKey, TValue>
    {
        /// <summary>
        /// Triggered when a new task is ready to run. This is run in a new thread.
        /// </summary>
        public event Action<TValue> RunTask;

        private CancellationTokenSource Cancel { get; } = new CancellationTokenSource();
        private Task Tasks { get; set; }
        private IReadOnlyDictionary<TKey, AsyncProducerConsumerQueue<TValue>> Queue { get; }

        /// <summary>
        /// Instantiates the instance.
        /// </summary>
        /// <param name="keys">The list of keys to create a separate queue for.</param>
        public TaskQueue(IEnumerable<TKey> keys)
        {
            var queue = new Dictionary<TKey, AsyncProducerConsumerQueue<TValue>>();

            foreach (var key in keys)
            {
                queue[key] = new AsyncProducerConsumerQueue<TValue>();
            }

            Queue = queue;
        }

        /// <summary>
        /// Adds a task to be run.
        /// </summary>
        /// <param name="key">The key to group tasks by.</param>
        /// <param name="value">The value to send to the task.</param>
        public async Task Add(TKey key, TValue value)
        {
            await Queue[key].EnqueueAsync(value);
        }

        /// <summary>
        /// Starts running the tasks.
        /// </summary>
        public void Start()
        {
            Tasks = Queue.Keys.AsyncForEach(async key => await Run(Queue[key]));
        }

        /// <summary>
        /// Stops running the tasks. Waits for their completion before returning.
        /// </summary>
        public async Task Stop()
        {
            Cancel.Cancel();
            await Tasks;
        }

        /// <summary>
        /// Pulls a task from the queue and executes it. Returns after the task is
        /// completed.
        /// </summary>
        /// <param name="queue">The queue to pull the task from.</param>
        private async Task Run(AsyncProducerConsumerQueue<TValue> queue)
        {
            while (!Cancel.IsCancellationRequested)
            {
                TValue value;

                try
                {
                    value = await queue.DequeueAsync(Cancel.Token);
                }
                catch
                {
                    return;
                }

                await Run(RunTask, value);
            }
        }

        /// <summary>
        /// Triggers the given action in a new thread. Returns when the action has
        /// completed.
        /// </summary>
        /// <param name="action">The action to trigger.</param>
        /// <param name="value">The value to pass to the action.</param>
        private static async Task Run(Action<TValue> action, TValue value)
        {
            if (action is object)
            {
                await Task.Run(() => action.Invoke(value));
            }
        }
    }
}
